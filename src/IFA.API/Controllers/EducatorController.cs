using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/educator")]
    public class EducatorController : BaseApiController
    {
        private const int MaxResearchSuggestions = 6;

        private readonly IApplicationDbContext _context;
        private readonly ISourceIngestionService _ingestionService;
        private readonly ISourceAnalysisService _analysisService;
        private readonly IBlueprintService _blueprintService;
        private readonly IAcademicResearchService _researchService;
        private readonly IImageResourceService _imageService;
        private readonly ILlmGateway _llmGateway;
        private readonly INotificationService _notificationService;

        public EducatorController(
            IApplicationDbContext context,
            ISourceIngestionService ingestionService,
            ISourceAnalysisService analysisService,
            IBlueprintService blueprintService,
            IAcademicResearchService researchService,
            IImageResourceService imageService,
            ILlmGateway llmGateway,
            INotificationService notificationService)
        {
            _context = context;
            _ingestionService = ingestionService;
            _analysisService = analysisService;
            _blueprintService = blueprintService;
            _researchService = researchService;
            _imageService = imageService;
            _llmGateway = llmGateway;
            _notificationService = notificationService;
        }

        // ------------------------------------------------------------------
        // Shared helpers
        // ------------------------------------------------------------------

        private bool IsEducator()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value
                       ?? User.FindFirst("role")?.Value;
            return string.Equals(role, "Educator", StringComparison.OrdinalIgnoreCase);
        }

        private IActionResult? EducatorGuard()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized(new { message = "Authentication required." });
            }
            if (!IsEducator())
            {
                return StatusCode(403, new { message = "Educator role required." });
            }
            return null;
        }

        private async Task<(EducatorProject? project, IActionResult? error)> LoadOwnedProjectAsync(
            Guid projectId, IApplicationDbContext context, CancellationToken ct)
        {
            var learnerId = await GetCurrentLearnerIdAsync(context);
            var project = await context.EducatorProjects
                .Include(p => p.Sources)
                .Include(p => p.Conflicts)
                .Include(p => p.Blueprint)
                .Include(p => p.GenerationJobs)
                .FirstOrDefaultAsync(p => p.Id == projectId, ct);

            if (project == null) return (null, NotFound(new { message = "Project not found." }));
            if (project.EducatorLearnerId != learnerId)
            {
                return (null, StatusCode(403, new { message = "You do not own this project." }));
            }
            return (project, null);
        }

        private async Task<(Course? course, IActionResult? error)> LoadOwnedCourseAsync(
            Guid courseId, IApplicationDbContext context, CancellationToken ct)
        {
            var learnerId = await GetCurrentLearnerIdAsync(context);
            var course = await context.Courses
                .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
                .FirstOrDefaultAsync(c => c.Id == courseId, ct);

            if (course == null) return (null, NotFound(new { message = "Course not found." }));
            if (course.CreatorLearnerId != learnerId)
            {
                return (null, StatusCode(403, new { message = "You do not own this course." }));
            }
            return (course, null);
        }

        private static string DetectSourceType(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "Website";
            url = url.Trim().ToLowerInvariant();

            if (url.Contains("youtube.com", StringComparison.Ordinal) ||
                url.Contains("youtu.be", StringComparison.Ordinal))
            {
                return "Video";
            }

            var path = url.Contains('?') ? url[..url.IndexOf('?')] : url;
            if (path.EndsWith(".pdf", StringComparison.Ordinal)) return "Pdf";
            if (path.EndsWith(".ppt", StringComparison.Ordinal) || path.EndsWith(".pptx", StringComparison.Ordinal)) return "Ppt";
            if (path.EndsWith(".png", StringComparison.Ordinal) ||
                path.EndsWith(".jpg", StringComparison.Ordinal) ||
                path.EndsWith(".jpeg", StringComparison.Ordinal) ||
                path.EndsWith(".gif", StringComparison.Ordinal) ||
                path.EndsWith(".webp", StringComparison.Ordinal))
            {
                return "Image";
            }
            return "Website";
        }

        private static object SourceDto(ProjectSource s) => new
        {
            id = s.Id,
            url = s.Url,
            type = s.Type,
            title = s.Title,
            description = s.Description,
            status = s.Status,
            failureReason = s.FailureReason,
            addedBy = s.AddedBy,
            included = s.Included
        };

        private static List<Guid> ParseSourceIds(string raw) => raw
            .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();

        private static object ConflictDto(SourceConflict c) => new
        {
            id = c.Id,
            kind = c.Kind,
            sourceIds = ParseSourceIds(c.SourceIds),
            explanation = c.Explanation,
            suggestedResolutions = TryDeserializeList(c.SuggestedResolutions),
            resolution = c.Resolution,
            manualNote = c.ManualNote,
            resolvedBy = c.ResolvedBy
        };

        private static List<string> TryDeserializeList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static CourseBlueprintDocument? ParseBlueprintJson(string json) =>
            string.IsNullOrWhiteSpace(json)
                ? null
                : TryParseBlueprint(json);

        private static CourseBlueprintDocument? TryParseBlueprint(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<CourseBlueprintDocument>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }

        // ------------------------------------------------------------------
        // Projects
        // ------------------------------------------------------------------

        public class CreateProjectRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        private static object ProjectSummaryDto(EducatorProject p) => new
        {
            id = p.Id,
            title = p.Title,
            description = p.Description,
            status = p.Status,
            sourceCount = p.Sources.Count,
            courseId = p.CourseId,
            createdAt = p.CreatedAt,
            updatedAt = p.UpdatedAt
        };

        [HttpGet("projects")]
        public async Task<IActionResult> GetProjects(CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var projects = await _context.EducatorProjects
                .Include(p => p.Sources)
                .Where(p => p.EducatorLearnerId == learnerId)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync(ct);

            return Ok(projects.Select(ProjectSummaryDto).ToList());
        }

        [HttpPost("projects")]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new { message = "Title is required." });
            }

            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var project = new EducatorProject
            {
                Id = Guid.NewGuid(),
                EducatorLearnerId = learnerId,
                Title = request.Title.Trim(),
                Description = (request.Description ?? string.Empty).Trim(),
                Status = "Draft"
            };
            _context.Add(project);
            await _context.SaveChangesAsync(ct);

            return Ok(ProjectSummaryDto(project));
        }

        [HttpGet("projects/{id:guid}")]
        public async Task<IActionResult> GetProject(Guid id, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (project, error) = await LoadOwnedProjectAsync(id, _context, ct);
            if (error != null) return error;

            return Ok(new
            {
                id = project!.Id,
                title = project.Title,
                description = project.Description,
                status = project.Status,
                sources = project.Sources.OrderBy(s => s.CreatedAt).Select(SourceDto).ToList(),
                conflicts = project.Conflicts
                    .OrderBy(c => c.CreatedAt)
                    .Select(ConflictDto)
                    .ToList(),
                blueprint = project.Blueprint == null
                    ? null
                    : new
                    {
                        json = project.Blueprint.Json,
                        approvedAt = project.Blueprint.ApprovedAt
                    },
                generationJobs = project.GenerationJobs
                    .OrderBy(j => j.ModuleIndex)
                    .Select(j => new
                    {
                        id = j.Id,
                        moduleIndex = j.ModuleIndex,
                        moduleTitle = j.ModuleTitle,
                        status = j.Status,
                        error = j.Error
                    })
                    .ToList(),
                courseId = project.CourseId,
                createdAt = project.CreatedAt,
                updatedAt = project.UpdatedAt
            });
        }

        [HttpPut("projects/{id:guid}")]
        public async Task<IActionResult> UpdateProject(Guid id, [FromBody] CreateProjectRequest request, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (project, error) = await LoadOwnedProjectAsync(id, _context, ct);
            if (error != null) return error;

            project!.Title = (request.Title ?? project.Title).Trim();
            project.Description = (request.Description ?? project.Description).Trim();
            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return Ok(ProjectSummaryDto(project));
        }

        [HttpDelete("projects/{id:guid}")]
        public async Task<IActionResult> DeleteProject(Guid id, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (project, error) = await LoadOwnedProjectAsync(id, _context, ct);
            if (error != null) return error;

            foreach (var source in project!.Sources) _context.Remove(source);
            foreach (var conflict in project.Conflicts) _context.Remove(conflict);
            foreach (var job in project.GenerationJobs) _context.Remove(job);
            if (project.Blueprint != null) _context.Remove(project.Blueprint);
            _context.Remove(project);
            await _context.SaveChangesAsync(ct);

            return Ok(new { success = true });
        }

        // ------------------------------------------------------------------
        // Sources
        // ------------------------------------------------------------------

        public class AddSourceRequest
        {
            public string Url { get; set; } = string.Empty;
            public string? Type { get; set; }
            public string? Title { get; set; }
        }

        public class IncludeRequest
        {
            public bool Included { get; set; }
        }

        [HttpPost("projects/{id:guid}/sources")]
        public async Task<IActionResult> AddSource(Guid id, [FromBody] AddSourceRequest request, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (project, error) = await LoadOwnedProjectAsync(id, _context, ct);
            if (error != null) return error;

            var url = (request.Url ?? string.Empty).Trim();
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            {
                return BadRequest(new { message = "A valid http(s) URL is required." });
            }

            var source = new ProjectSource
            {
                Id = Guid.NewGuid(),
                ProjectId = project!.Id,
                Url = url,
                Type = string.IsNullOrWhiteSpace(request.Type) ? DetectSourceType(url) : request.Type.Trim(),
                Title = string.IsNullOrWhiteSpace(request.Title) ? url : request.Title.Trim(),
                Status = "Pending",
                AddedBy = "User",
                Included = true
            };
            _context.Add(source);
            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return Ok(SourceDto(source));
        }

        [HttpDelete("projects/{id:guid}/sources/{sourceId:guid}")]
        public async Task<IActionResult> DeleteSource(Guid id, Guid sourceId, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (project, error) = await LoadOwnedProjectAsync(id, _context, ct);
            if (error != null) return error;

            var source = project!.Sources.FirstOrDefault(s => s.Id == sourceId);
            if (source == null) return NotFound(new { message = "Source not found." });

            _context.Remove(source);
            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return Ok(new { success = true });
        }

        [HttpPatch("projects/{id:guid}/sources/{sourceId:guid}/include")]
        public async Task<IActionResult> SetSourceIncluded(
            Guid id, Guid sourceId, [FromBody] IncludeRequest request, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (project, error) = await LoadOwnedProjectAsync(id, _context, ct);
            if (error != null) return error;

            var source = project!.Sources.FirstOrDefault(s => s.Id == sourceId);
            if (source == null) return NotFound(new { message = "Source not found." });

            source.Included = request.Included;
            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return Ok(SourceDto(source));
        }

        // ------------------------------------------------------------------
        // Research suggestions
        // ------------------------------------------------------------------

        [HttpPost("projects/{id:guid}/research/suggest")]
        public async Task<IActionResult> SuggestResearch(Guid id, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (project, error) = await LoadOwnedProjectAsync(id, _context, ct);
            if (error != null) return error;

            var queryParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(project!.Title)) queryParts.Add(project.Title);
            if (!string.IsNullOrWhiteSpace(project.Description)) queryParts.Add(project.Description);
            foreach (var keyword in project.Sources
                         .Where(s => s.Included)
                         .Select(s => s.Title)
                         .Where(t => !string.IsNullOrWhiteSpace(t))
                         .Take(4))
            {
                queryParts.Add(keyword!);
            }

            var query = string.Join(" ", queryParts).Trim();
            if (query.Length > 300) query = query[..300];

            var results = await _researchService.SearchAsync(query, 12, ct);

            var existingUrls = project.Sources
                .Select(s => s.Url.Trim().TrimEnd('/'))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var suggestions = new List<ProjectSource>();
            foreach (var result in results)
            {
                if (suggestions.Count >= MaxResearchSuggestions) break;
                if (string.IsNullOrWhiteSpace(result.Url)) continue;
                if (existingUrls.Contains(result.Url.Trim().TrimEnd('/'))) continue;
                existingUrls.Add(result.Url);

                var source = new ProjectSource
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    Url = result.Url,
                    Type = DetectSourceType(result.Url),
                    Title = string.IsNullOrWhiteSpace(result.Title) ? result.Url : result.Title,
                    Description = string.IsNullOrWhiteSpace(result.Summary) ? null : result.Summary,
                    Status = "Pending",
                    AddedBy = "Ai",
                    Included = true
                };
                _context.Add(source);
                suggestions.Add(source);
            }

            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return Ok(new { suggestions = suggestions.Select(SourceDto).ToList() });
        }

        // ------------------------------------------------------------------
        // Analyze (ingestion + conflict analysis)
        // ------------------------------------------------------------------

        [HttpPost("projects/{id:guid}/analyze")]
        public async Task<IActionResult> AnalyzeProject(Guid id, [FromQuery] bool force = false, CancellationToken ct = default)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (project, error) = await LoadOwnedProjectAsync(id, _context, ct);
            if (error != null) return error;

            project!.Status = "Analyzing";
            await _context.SaveChangesAsync(ct);

            try
            {
                var pending = project.Sources
                    .Where(s => s.Included && (force || s.Status == "Pending"))
                    .ToList();

                foreach (var source in pending)
                {
                    await _ingestionService.IngestAsync(source, ct);
                }
                await _context.SaveChangesAsync(ct);

                var analysis = await _analysisService.AnalyzeAsync(project, ct);

                project.Status = analysis.Conflicts.Count > 0 ? "AwaitingResolution" : "BlueprintReady";
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);

                var conflicts = await _context.SourceConflicts
                    .Where(c => c.ProjectId == project.Id && c.ResolvedAt == null)
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync(ct);

                return Ok(new
                {
                    conflictCount = conflicts.Count,
                    conflicts = conflicts.Select(ConflictDto).ToList()
                });
            }
            catch (Exception)
            {
                project.Status = "BlueprintReady";
                await _context.SaveChangesAsync(ct);
                throw;
            }
        }

        // ------------------------------------------------------------------
        // Conflicts
        // ------------------------------------------------------------------

        public class ResolveConflictRequest
        {
            public string Resolution { get; set; } = string.Empty;
            public string? ManualNote { get; set; }
        }

        [HttpPost("projects/{id:guid}/conflicts/{conflictId:guid}/resolve")]
        public async Task<IActionResult> ResolveConflict(
            Guid id, Guid conflictId, [FromBody] ResolveConflictRequest request, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (project, error) = await LoadOwnedProjectAsync(id, _context, ct);
            if (error != null) return error;

            var conflict = project!.Conflicts.FirstOrDefault(c => c.Id == conflictId);
            if (conflict == null) return NotFound(new { message = "Conflict not found." });

            var resolution = (request.Resolution ?? string.Empty).Trim();
            if (resolution is not ("Keep" or "Remove" or "Merge" or "ManualNote"))
            {
                return BadRequest(new { message = "Resolution must be Keep, Remove, Merge or ManualNote." });
            }

            conflict.Resolution = resolution;
            conflict.ManualNote = string.IsNullOrWhiteSpace(request.ManualNote) ? null : request.ManualNote.Trim();
            conflict.ResolvedBy = "User";
            conflict.ResolvedAt = DateTime.UtcNow;

            if (resolution == "Remove")
            {
                var involvedIds = ParseSourceIds(conflict.SourceIds).ToHashSet();
                foreach (var source in project.Sources.Where(s => involvedIds.Contains(s.Id)))
                {
                    source.Included = false;
                }
            }

            project.UpdatedAt = DateTime.UtcNow;

            var unresolved = await _context.SourceConflicts
                .CountAsync(c => c.ProjectId == project.Id && c.ResolvedAt == null && c.Id != conflict.Id, ct);

            if (unresolved == 0 && project.Blueprint == null && project.Status != "Generating")
            {
                project.Status = "BlueprintReady";
            }

            await _context.SaveChangesAsync(ct);

            return Ok(new { success = true, status = project.Status });
        }

        // ------------------------------------------------------------------
        // Blueprint
        // ------------------------------------------------------------------

        [HttpPost("projects/{id:guid}/blueprint/generate")]
        public async Task<IActionResult> GenerateBlueprint(Guid id, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (project, error) = await LoadOwnedProjectAsync(id, _context, ct);
            if (error != null) return error;

            var unresolved = await _context.SourceConflicts
                .CountAsync(c => c.ProjectId == id && c.ResolvedAt == null, ct);
            if (unresolved > 0)
            {
                return Conflict(new { message = "All conflicts must be resolved before generating the blueprint." });
            }

            try
            {
                var blueprint = await _blueprintService.GenerateAsync(project!, ct);
                project!.UpdatedAt = DateTime.UtcNow;
                if (project.Status is "Draft" or "AwaitingResolution" or "Researching" or "Analyzing")
                {
                    project.Status = "BlueprintReady";
                }
                await _context.SaveChangesAsync(ct);

                return Ok(new { blueprintJson = blueprint.Json });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(502, new { message = ex.Message });
            }
        }

        public class ApproveBlueprintResult
        {
            public Guid CourseId { get; set; }
            public List<object> Jobs { get; set; } = new();
        }

        [HttpPost("projects/{id:guid}/blueprint/approve")]
        public async Task<IActionResult> ApproveBlueprint(Guid id, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (project, error) = await LoadOwnedProjectAsync(id, _context, ct);
            if (error != null) return error;

            if (project!.Blueprint == null)
            {
                return BadRequest(new { message = "Generate a blueprint first." });
            }
            if (await _context.SourceConflicts.AnyAsync(c => c.ProjectId == id && c.ResolvedAt == null, ct))
            {
                return Conflict(new { message = "All conflicts must be resolved before approving." });
            }
            if (project.CourseId != null &&
                project.Status is "Generating" or "ReadyForReview" or "Published")
            {
                return Conflict(new { message = "A course was already generated for this project." });
            }

            var document = ParseBlueprintJson(project.Blueprint.Json);
            if (document == null || document.Modules.Count == 0)
            {
                return StatusCode(502, new { message = "The stored blueprint is invalid. Regenerate it first." });
            }

            var educator = await _context.Learners
                .FirstOrDefaultAsync(l => l.Id == project.EducatorLearnerId, ct);

            var materials = project.Sources
                .Where(s => s.Included)
                .Select(s => s.Url)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = document.Title,
                Description = document.Description,
                Category = document.Category,
                EstimatedDuration = document.EstimatedDuration,
                ThumbnailUrl = string.Empty,
                ProviderName = string.IsNullOrWhiteSpace(educator?.Name) ? "IFA Educator" : educator!.Name,
                IsPublic = false,
                ShareCode = Guid.NewGuid().ToString("N").Substring(0, 8),
                ExternalMaterials = materials.Count > 0 ? string.Join(";", materials) : null,
                CreatorLearnerId = project.EducatorLearnerId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Add(course);

            var jobs = new List<GenerationJob>();
            for (var i = 0; i < document.Modules.Count; i++)
            {
                var moduleDto = document.Modules[i];
                var module = new Module
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    ModuleNumber = i + 1,
                    Title = moduleDto.Title,
                    Summary = moduleDto.Description,
                    EstimatedHours = Math.Max(1, moduleDto.EstimatedHours),
                    GenerationStatus = ModuleGenerationStatus.Blueprint
                };
                _context.Add(module);

                var job = new GenerationJob
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    CourseId = course.Id,
                    ModuleIndex = i,
                    ModuleTitle = moduleDto.Title,
                    Status = "Queued"
                };
                _context.Add(job);
                jobs.Add(job);
            }

            project.Blueprint.ApprovedAt = DateTime.UtcNow;
            project.CourseId = course.Id;
            project.Status = "Generating";
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return Ok(new
            {
                courseId = course.Id,
                jobs = jobs.OrderBy(j => j.ModuleIndex).Select(j => new
                {
                    id = j.Id,
                    moduleIndex = j.ModuleIndex,
                    moduleTitle = j.ModuleTitle,
                    status = j.Status
                }).ToList()
            });
        }

        [HttpGet("projects/{id:guid}/status")]
        public async Task<IActionResult> GetProjectStatus(Guid id, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (project, error) = await LoadOwnedProjectAsync(id, _context, ct);
            if (error != null) return error;

            return Ok(new
            {
                status = project!.Status,
                jobs = project.GenerationJobs.OrderBy(j => j.ModuleIndex).Select(j => new
                {
                    moduleIndex = j.ModuleIndex,
                    moduleTitle = j.ModuleTitle,
                    status = j.Status,
                    error = j.Error
                }).ToList(),
                completedCount = project.GenerationJobs.Count(j => j.Status == "Done"),
                totalCount = project.GenerationJobs.Count,
                courseId = project.CourseId
            });
        }

        // ------------------------------------------------------------------
        // Courses: cover, module editing, AI revision, publish
        // ------------------------------------------------------------------

        public class CoverRequest
        {
            public string? ImageUrl { get; set; }
        }

        [HttpPost("courses/{courseId:guid}/cover")]
        public async Task<IActionResult> SetCover(Guid courseId, [FromBody] CoverRequest request, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadOwnedCourseAsync(courseId, _context, ct);
            if (error != null) return error;

            var url = (request.ImageUrl ?? string.Empty).Trim();
            if (url.Length > 4096)
            {
                return BadRequest(new { message = "Cover image URL is too long (max 4096 characters)." });
            }

            course!.CoverImageUrl = string.IsNullOrWhiteSpace(url) ? null : url;
            course.CoverImageSource = "User";
            await _context.SaveChangesAsync(ct);

            return Ok(new { coverImageUrl = course.CoverImageUrl });
        }

        [HttpPost("courses/{courseId:guid}/cover/auto")]
        public async Task<IActionResult> AutoCover(Guid courseId, [FromBody] CoverRequest? request, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadOwnedCourseAsync(courseId, _context, ct);
            if (error != null) return error;

            var query = string.IsNullOrWhiteSpace(course!.Category)
                ? course.Title
                : $"{course.Title} {course.Category}";
            var images = await _imageService.SearchImagesAsync(query, 6, ct);
            var candidates = images
                .Select(i => new { url = i.ImageUrl, title = i.Title })
                .ToList();

            // Applying a specific candidate is folded into the same endpoint.
            var applied = (request?.ImageUrl ?? string.Empty).Trim();
            if (applied.Length > 0)
            {
                if (applied.Length > 4096)
                {
                    return BadRequest(new { message = "Cover image URL is too long (max 4096 characters)." });
                }
                course.CoverImageUrl = applied;
                course.CoverImageSource = "Auto";
                await _context.SaveChangesAsync(ct);
                return Ok(new { candidates, coverImageUrl = course.CoverImageUrl });
            }

            return Ok(new { candidates });
        }

        public class LessonUpsertDto
        {
            public Guid? Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public int Order { get; set; }
        }

        public class UpdateModuleRequest
        {
            public string? Title { get; set; }
            public string? Description { get; set; }
            public List<LessonUpsertDto>? Lessons { get; set; }
        }

        [HttpPut("courses/{courseId:guid}/modules/{moduleId:guid}")]
        public async Task<IActionResult> UpdateModule(
            Guid courseId, Guid moduleId, [FromBody] UpdateModuleRequest request, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadOwnedCourseAsync(courseId, _context, ct);
            if (error != null) return error;

            var module = course!.Modules.FirstOrDefault(m => m.Id == moduleId);
            if (module == null) return NotFound(new { message = "Module not found." });

            if (!string.IsNullOrWhiteSpace(request.Title)) module.Title = request.Title.Trim();
            if (!string.IsNullOrWhiteSpace(request.Description)) module.Summary = request.Description.Trim();

            if (request.Lessons != null)
            {
                foreach (var dto in request.Lessons)
                {
                    Lesson? lesson = null;
                    if (dto.Id != null)
                    {
                        lesson = module.Lessons.FirstOrDefault(l => l.Id == dto.Id.Value);
                    }
                    if (lesson == null)
                    {
                        lesson = new Lesson
                        {
                            Id = dto.Id ?? Guid.NewGuid(),
                            ModuleId = module.Id
                        };
                        _context.Add(lesson);
                        module.Lessons.Add(lesson);
                    }
                    lesson.Title = string.IsNullOrWhiteSpace(dto.Title) ? lesson.Title : dto.Title.Trim();
                    lesson.ContentMarkdown = dto.Content ?? lesson.ContentMarkdown;
                    if (dto.Order > 0) lesson.LessonNumber = dto.Order;
                }

                // Re-number in the requested order to keep lessons ordered.
                var ordered = module.Lessons.OrderBy(l => l.LessonNumber).ToList();
                for (var i = 0; i < ordered.Count; i++)
                {
                    ordered[i].LessonNumber = i + 1;
                }
            }

            await _context.SaveChangesAsync(ct);

            return Ok(new
            {
                moduleId = module.Id,
                title = module.Title,
                description = module.Summary,
                lessons = module.Lessons.OrderBy(l => l.LessonNumber).Select(l => new
                {
                    id = l.Id,
                    title = l.Title,
                    content = l.ContentMarkdown,
                    order = l.LessonNumber
                }).ToList()
            });
        }

        public class AiUpdateRequest
        {
            public string Instruction { get; set; } = string.Empty;
        }

        private class ModuleRevisionDto
        {
            public List<RevisionLessonDto>? Lessons { get; set; }
        }

        private class RevisionLessonDto
        {
            public int Index { get; set; }
            public string? Title { get; set; }
            public string? ContentMarkdown { get; set; }
        }

        [HttpPost("courses/{courseId:guid}/modules/{moduleId:guid}/ai-update")]
        public async Task<IActionResult> AiUpdateModule(
            Guid courseId, Guid moduleId, [FromBody] AiUpdateRequest request, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadOwnedCourseAsync(courseId, _context, ct);
            if (error != null) return error;

            var module = course!.Modules.FirstOrDefault(m => m.Id == moduleId);
            if (module == null) return NotFound(new { message = "Module not found." });

            if (string.IsNullOrWhiteSpace(request.Instruction))
            {
                return BadRequest(new { message = "An instruction is required." });
            }

            var lessons = module.Lessons.OrderBy(l => l.LessonNumber).ToList();
            if (lessons.Count == 0)
            {
                return BadRequest(new { message = "This module has no lessons to revise yet." });
            }

            var lessonsBlock = string.Join("\n\n", lessons.Select((l, i) =>
                $"[Lesson {i}]\nTitle: {l.Title}\nContent:\n{l.ContentMarkdown}"));

            var userPrompt =
                $"Course: {course.Title}\nModule: {module.Title}\n" +
                $"Educator instruction: {request.Instruction.Trim()}\n\n" +
                $"LESSONS:\n{lessonsBlock}";

            ModuleRevisionDto? revision = null;
            try
            {
                revision = await _llmGateway.CompleteJsonAsync<ModuleRevisionDto>(
                    PromptRegistry.Educator_ModuleRevisionSystemPrompt, userPrompt, LlmRole.Model3_Builder, ct);
            }
            catch (Exception)
            {
                // handled below
            }

            if (revision?.Lessons == null || revision.Lessons.Count == 0)
            {
                return StatusCode(502, new { message = "The AI could not revise this module. Try a clearer instruction." });
            }

            foreach (var dto in revision.Lessons)
            {
                if (dto.Index < 0 || dto.Index >= lessons.Count) continue;
                var lesson = lessons[dto.Index];
                if (!string.IsNullOrWhiteSpace(dto.Title)) lesson.Title = dto.Title.Trim();
                if (!string.IsNullOrWhiteSpace(dto.ContentMarkdown)) lesson.ContentMarkdown = dto.ContentMarkdown;
            }

            await _context.SaveChangesAsync(ct);

            return Ok(new
            {
                moduleId = module.Id,
                lessons = module.Lessons.OrderBy(l => l.LessonNumber).Select(l => new
                {
                    id = l.Id,
                    title = l.Title,
                    content = l.ContentMarkdown,
                    order = l.LessonNumber
                }).ToList()
            });
        }

        public class PublishRequest
        {
            public bool IsPaid { get; set; }
            public decimal? Price { get; set; }
            public string? Currency { get; set; }
        }

        [HttpPost("courses/{courseId:guid}/publish")]
        public async Task<IActionResult> PublishCourse(Guid courseId, [FromBody] PublishRequest request, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadOwnedCourseAsync(courseId, _context, ct);
            if (error != null) return error;

            course!.IsPublic = true;
            course.IsPaid = request.IsPaid;
            if (request.IsPaid)
            {
                course.PriceAmount = request.Price ?? course.PriceAmount;
                course.Currency = string.IsNullOrWhiteSpace(request.Currency)
                    ? "ETB"
                    : request.Currency.Trim();
                if (course.PriceAmount <= 0)
                {
                    return BadRequest(new { message = "A price above zero is required for paid courses." });
                }
            }
            else
            {
                course.PriceAmount = 0;
            }

            await _context.SaveChangesAsync(ct);

            return Ok(new { isPublic = course.IsPublic, isPaid = course.IsPaid, price = course.PriceAmount });
        }

        // ------------------------------------------------------------------
        // Dashboard & enrollments
        // ------------------------------------------------------------------

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var learnerId = await GetCurrentLearnerIdAsync(_context);

            var courses = await _context.Courses
                .Include(c => c.Enrollments)
                .Where(c => c.CreatorLearnerId == learnerId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(ct);

            var since = DateTime.UtcNow.AddDays(-30).Date;
            var courseIds = courses.Select(c => c.Id).ToList();

            // Learning activity of enrolled learners, bucketed per day per course.
            var enrolledLearnerIdsByCourse = courses.ToDictionary(
                c => c.Id,
                c => c.Enrollments.Select(e => e.LearnerId).ToHashSet());

            var activities = await _context.Activities
                .Where(a => a.CreatedAt >= since)
                .ToListAsync(ct);

            var trafficByCourse = courses.ToDictionary(c => c.Id, _ => new Dictionary<DateTime, (int enrollments, int activity)>());

            foreach (var course in courses)
            {
                var map = trafficByCourse[course.Id];
                for (var day = since; day <= DateTime.UtcNow.Date; day = day.AddDays(1))
                {
                    map[day] = (0, 0);
                }
                foreach (var enrollment in course.Enrollments)
                {
                    var day = enrollment.EnrolledAt.Date;
                    if (map.ContainsKey(day)) map[day] = (map[day].enrollments + 1, map[day].activity);
                }
            }

            foreach (var activity in activities)
            {
                foreach (var course in courses)
                {
                    var learners = enrolledLearnerIdsByCourse[course.Id];
                    if (!learners.Contains(activity.LearnerId)) continue;
                    var map = trafficByCourse[course.Id];
                    var day = activity.CreatedAt.Date;
                    if (map.ContainsKey(day)) map[day] = (map[day].enrollments, map[day].activity + 1);
                }
            }

            var totalEnrollments = courses.Sum(c => c.Enrollments.Count);
            var totalPending = courses.Sum(c => c.Enrollments.Count(e => e.Status == "PendingApproval"));
            var published = courses.Count(c => c.IsPublic);

            var result = courses.Select(c =>
            {
                var active = c.Enrollments.Count(e => e.Status == "Active");
                var total = c.Enrollments.Count;
                return new
                {
                    courseId = c.Id,
                    title = c.Title,
                    isPublic = c.IsPublic,
                    isPaid = c.IsPaid,
                    price = c.PriceAmount,
                    enrollments = total,
                    activeEnrollments = active,
                    pendingApprovals = c.Enrollments.Count(e => e.Status == "PendingApproval"),
                    avgProgress = total == 0 ? 0 : (int)Math.Round((double)c.Enrollments.Average(e => e.ProgressPercentage)),
                    traffic = trafficByCourse[c.Id]
                        .OrderBy(k => k.Key)
                        .Select(k => new { date = k.Key, enrollments = k.Value.enrollments, activity = k.Value.activity })
                        .ToList()
                };
            }).ToList();

            return Ok(new
            {
                courses = result,
                totals = new
                {
                    enrollments = totalEnrollments,
                    pendingApprovals = totalPending,
                    publishedCourses = published
                }
            });
        }

        [HttpGet("enrollments")]
        public async Task<IActionResult> GetEnrollments([FromQuery] string? status, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var learnerId = await GetCurrentLearnerIdAsync(_context);

            var query = _context.CourseEnrollments
                .Include(e => e.Course)
                .Include(e => e.Learner)
                .Where(e => e.Course != null && e.Course.CreatorLearnerId == learnerId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(e => e.Status == status.Trim());
            }

            var enrollments = await query
                .OrderByDescending(e => e.EnrolledAt)
                .ToListAsync(ct);

            return Ok(enrollments.Select(e => new
            {
                enrollmentId = e.Id,
                courseId = e.CourseId,
                courseTitle = e.Course!.Title,
                learnerId = e.LearnerId,
                learnerName = e.Learner?.Name ?? "Unknown",
                status = e.Status,
                enrolledAt = e.EnrolledAt
            }).ToList());
        }

        [HttpPost("enrollments/{enrollmentId:guid}/approve")]
        public async Task<IActionResult> ApproveEnrollment(Guid enrollmentId, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;
            return await SetEnrollmentStatusAsync(enrollmentId, "Active", "enrollment_approved", ct);
        }

        [HttpPost("enrollments/{enrollmentId:guid}/reject")]
        public async Task<IActionResult> RejectEnrollment(Guid enrollmentId, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;
            return await SetEnrollmentStatusAsync(enrollmentId, "Rejected", "enrollment_rejected", ct);
        }

        private async Task<IActionResult> SetEnrollmentStatusAsync(Guid enrollmentId, string status, string notificationType, CancellationToken ct)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var enrollment = await _context.CourseEnrollments
                .Include(e => e.Course)
                .Include(e => e.Learner)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId, ct);

            if (enrollment == null) return NotFound(new { message = "Enrollment not found." });
            if (enrollment.Course == null || enrollment.Course.CreatorLearnerId != learnerId)
            {
                return StatusCode(403, new { message = "You do not own this course." });
            }

            enrollment.Status = status;
            await _context.SaveChangesAsync(ct);

            try
            {
                await _notificationService.SendNotificationAsync(
                    enrollment.LearnerId,
                    notificationType,
                    status == "Active" ? "Enrollment approved" : "Enrollment rejected",
                    status == "Active"
                        ? $"Your enrollment in '{enrollment.Course.Title}' was approved. Happy learning!"
                        : $"Your enrollment request for '{enrollment.Course.Title}' was rejected by the educator.",
                    $"/courses/{enrollment.CourseId}",
                    ct);
            }
            catch
            {
                // Notification is best-effort; the status change already succeeded.
            }

            return Ok(new { enrollmentId = enrollment.Id, status = enrollment.Status });
        }
    }
}
