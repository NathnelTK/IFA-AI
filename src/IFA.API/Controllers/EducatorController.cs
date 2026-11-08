using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Application.Common.Exceptions;
using IFA.Application.Common.Models;
using IFA.Application.Common.Services;
using IFA.Domain.Entities;
using IFA.Infrastructure.AI;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/educator")]
    public partial class EducatorController : BaseApiController
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
            return IsCurrentUserInRole("Educator");
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
                .Include(c => c.Modules)
                .ThenInclude(m => m.Quizzes)
                .ThenInclude(q => q.Questions)
                .Include(c => c.Assignments)
                .FirstOrDefaultAsync(c => c.Id == courseId, ct);

            if (course == null) return (null, NotFound(new { message = "Course not found." }));
            if (course.CreatorLearnerId != learnerId)
            {
                return (null, StatusCode(403, new { message = "You do not own this course." }));
            }
            return (course, null);
        }

        private async Task<(Course? course, IActionResult? error)> LoadEditableCourseAsync(Guid courseId, CancellationToken ct)
        {
            var (course, error) = await LoadOwnedCourseAsync(courseId, _context, ct);
            if (error != null) return (null, error);
            if (course!.IsPublished)
                return (null, Conflict(new { message = "Unpublish this course before changing its content or settings. Learners cannot see draft edits." }));
            if (course.Modules.Any(m => m.GenerationStatus == ModuleGenerationStatus.Generating))
                return (null, Conflict(new { message = "A module is still being generated. Wait for it to finish before editing the course." }));
            if (await _context.GenerationJobs.AnyAsync(j => j.CourseId == courseId && (j.Status == "Pending" || j.Status == "Queued" || j.Status == "Running"), ct))
                return (null, Conflict(new { message = "Content is still being generated. Wait for generation to finish before editing the curriculum." }));
            return (course, null);
        }

        private static bool IsWebUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) && string.IsNullOrEmpty(uri.UserInfo);

        private static string? SettingsError(string? visibility, string? enrollmentMode, string? accentColor, string? themeMode)
        {
            if (visibility != null && visibility is not ("Public" or "Unlisted" or "Private")) return "Choose Public, Unlisted, or Private visibility.";
            if (enrollmentMode != null && enrollmentMode is not ("Open" or "ApprovalRequired" or "InviteOnly")) return "Choose a supported enrollment mode.";
            if (accentColor != null && !Regex.IsMatch(accentColor, "^#[0-9A-Fa-f]{6}$")) return "Use a six-digit hex accent color.";
            if (themeMode != null && themeMode is not ("pine" or "slate" or "emerald" or "indigo" or "teal" or "amber")) return "Choose a supported theme palette.";
            return null;
        }

        private async Task SyncProjectAsync(Course course, CancellationToken ct)
        {
            var project = await _context.EducatorProjects.FirstOrDefaultAsync(p => p.CourseId == course.Id, ct);
            if (project == null) return;
            project.Title = course.Title;
            project.Description = course.Description;
            project.UpdatedAt = DateTime.UtcNow;
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
            [Required, StringLength(200)]
            public string Title { get; set; } = string.Empty;
            [StringLength(2000)]
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
            if (project!.CourseId.HasValue)
                return Conflict(new { message = "A project linked to a course is retained as generation provenance and cannot be deleted." });
            if (project.GenerationJobs.Any(job => job.Status is "Pending" or "Queued" or "Running"))
                return Conflict(new { message = "Wait for active generation jobs before deleting this project." });

            foreach (var source in project.Sources) _context.Remove(source);
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
            [Required, StringLength(2048)]
            public string Url { get; set; } = string.Empty;
            [StringLength(32)]
            public string? Type { get; set; }
            [StringLength(300)]
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
            if (!IsWebUrl(url))
            {
                return BadRequest(new { message = "A valid http(s) URL is required." });
            }
            if (project!.CourseId.HasValue) return Conflict(new { message = "Approved course sources are frozen. Start a new project for a different source set." });
            if (project.Sources.Count >= 20) return BadRequest(new { message = "A project can contain at most 20 sources." });
            if (request.Type != null && request.Type is not ("Website" or "Pdf" or "Video" or "Image" or "Ppt"))
                return BadRequest(new { message = "Unsupported source type." });

            var source = new ProjectSource
            {
                Id = Guid.NewGuid(),
                ProjectId = project!.Id,
                Url = url,
                Type = string.IsNullOrWhiteSpace(request.Type) ? DetectSourceType(url) : request.Type.Trim(),
                Title = string.IsNullOrWhiteSpace(request.Title) ? url[..Math.Min(url.Length, 300)] : request.Title.Trim(),
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
            if (project!.CourseId.HasValue) return Conflict(new { message = "Approved course sources are frozen." });

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
            if (project!.CourseId.HasValue) return Conflict(new { message = "Approved course sources are frozen." });

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
            if (project!.CourseId.HasValue) return Conflict(new { message = "Approved course sources are frozen." });

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
                if (suggestions.Count >= MaxResearchSuggestions || project.Sources.Count + suggestions.Count >= 20) break;
                if (string.IsNullOrWhiteSpace(result.Url) || result.Url.Length > 2048 || !IsWebUrl(result.Url)) continue;
                if (existingUrls.Contains(result.Url.Trim().TrimEnd('/'))) continue;
                existingUrls.Add(result.Url);

                var source = new ProjectSource
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    Url = result.Url,
                    Type = DetectSourceType(result.Url),
                    Title = string.IsNullOrWhiteSpace(result.Title) ? result.Url[..Math.Min(result.Url.Length, 300)] : result.Title[..Math.Min(result.Title.Length, 300)],
                    Description = string.IsNullOrWhiteSpace(result.Summary) ? null : result.Summary[..Math.Min(result.Summary.Length, 2000)],
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
            if (project!.CourseId.HasValue) return Conflict(new { message = "Approved course sources are frozen." });

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
            catch (InvalidOperationException ex) when (ex is not AiUnavailableException && !ct.IsCancellationRequested)
            {
                project.Status = "AnalysisFailed";
                await _context.SaveChangesAsync(CancellationToken.None);
                return UnprocessableEntity(new { message = ex.Message });
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                project.Status = "AnalysisFailed";
                await _context.SaveChangesAsync(CancellationToken.None);
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
            if (project!.CourseId.HasValue) return Conflict(new { message = "Approved course sources are frozen." });

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

            return Ok(ConflictDto(conflict));
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
            if (project!.CourseId.HasValue) return Conflict(new { message = "This project already has a course. Continue in its editor." });

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
            catch (InvalidOperationException ex) when (ex is not AiUnavailableException)
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

            await using var transaction = await CourseAccess.BeginMutationAsync(_context, ct);
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
            if (project.CourseId != null)
            {
                return Conflict(new { message = "A course was already generated for this project." });
            }

            CourseBlueprintDocument document;
            try
            {
                document = BlueprintService.Parse(project.Blueprint.Json, project.Sources)
                    ?? throw new InvalidOperationException("The stored blueprint or its approved sources are invalid. Regenerate it or complete the course manually.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            var educator = await _context.Learners
                .FirstOrDefaultAsync(l => l.Id == project.EducatorLearnerId, ct);

            var materials = project.Sources
                .Where(s => s.Included)
                .Select(s => s.Url)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (string.Join(";", materials).Length > 4000)
                return BadRequest(new { message = "The combined source URLs exceed the 4000-character course source limit. Use fewer sources." });

            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = document.Title,
                Description = document.Description,
                Category = document.Category,
                EstimatedDuration = document.EstimatedDuration,
                ThumbnailUrl = string.Empty,
                ProviderName = string.IsNullOrWhiteSpace(educator?.Name) ? "Witforge Educator" : educator!.Name,
                IsPublic = false,
                ShareCode = Guid.NewGuid().ToString("N").ToUpperInvariant(),
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
            if (transaction != null) await transaction.CommitAsync(ct);

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

            var (course, error) = await LoadEditableCourseAsync(courseId, ct);
            if (error != null) return error;

            var url = (request.ImageUrl ?? string.Empty).Trim();
            if (url.Length > 2048 || (url.Length > 0 && !IsWebUrl(url)))
            {
                return BadRequest(new { message = "Use an HTTP(S) cover URL of at most 2048 characters." });
            }

            course!.CoverImageUrl = string.IsNullOrWhiteSpace(url) ? null : url;
            course.ThumbnailUrl = url;
            course.CoverImageSource = "User";
            await _context.SaveChangesAsync(ct);

            return Ok(new { coverImageUrl = course.CoverImageUrl });
        }

        [HttpPost("courses/{courseId:guid}/cover/auto")]
        public async Task<IActionResult> AutoCover(Guid courseId, [FromBody] CoverRequest? request, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadEditableCourseAsync(courseId, ct);
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
                if (applied.Length > 2048 || !IsWebUrl(applied))
                {
                    return BadRequest(new { message = "Use an HTTP(S) cover URL of at most 2048 characters." });
                }
                course.CoverImageUrl = applied;
                course.ThumbnailUrl = applied;
                course.CoverImageSource = "Auto";
                await _context.SaveChangesAsync(ct);
                return Ok(new { candidates, coverImageUrl = course.CoverImageUrl });
            }

            return Ok(new { candidates });
        }

        public class LessonUpsertDto
        {
            public Guid? Id { get; set; }
            [Required, StringLength(200)]
            public string Title { get; set; } = string.Empty;
            [StringLength(20000)]
            public string Content { get; set; } = string.Empty;
            [Range(1, 1000)]
            public int Order { get; set; }
            [Range(1, 1440)]
            public int? ReadingTimeMinutes { get; set; }
            [StringLength(32)]
            public string? YouTubeVideoId { get; set; }
        }

        public class UpdateModuleRequest
        {
            [StringLength(200, MinimumLength = 1)]
            public string? Title { get; set; }
            [StringLength(2000)]
            public string? Description { get; set; }
            [MaxLength(100)]
            public List<LessonUpsertDto>? Lessons { get; set; }
        }

        [HttpPut("courses/{courseId:guid}/modules/{moduleId:guid}")]
        public async Task<IActionResult> UpdateModule(
            Guid courseId, Guid moduleId, [FromBody] UpdateModuleRequest request, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadEditableCourseAsync(courseId, ct);
            if (error != null) return error;

            var module = course!.Modules.FirstOrDefault(m => m.Id == moduleId);
            if (module == null) return NotFound(new { message = "Module not found." });

            if (request.Lessons != null)
            {
                var ids = request.Lessons.Where(l => l.Id.HasValue).Select(l => l.Id!.Value).ToList();
                if (ids.Distinct().Count() != ids.Count || ids.Any(id => !module.Lessons.Any(l => l.Id == id)) ||
                    request.Lessons.Select(l => l.Order).Distinct().Count() != request.Lessons.Count ||
                    request.Lessons.Any(l => string.IsNullOrWhiteSpace(l.Title) ||
                        (!string.IsNullOrWhiteSpace(l.YouTubeVideoId) && !Regex.IsMatch(l.YouTubeVideoId, "^[A-Za-z0-9_-]{11}$"))))
                    return BadRequest(new { message = "Lessons must have unique ordering, valid titles/video IDs, and IDs belonging to this module." });
            }

            if (request.Title != null)
            {
                if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { message = "Module title is required." });
                module.Title = request.Title.Trim();
            }
            if (request.Description != null) module.Summary = request.Description.Trim();

            await _context.ExecuteInTransactionAsync(async () =>
            {
                if (request.Lessons != null)
                {
                    // Two saves avoid transient collisions with the unique lesson-order index.
                    var existing = module.Lessons.OrderBy(l => l.LessonNumber).ToList();
                    for (var i = 0; i < existing.Count; i++) existing[i].LessonNumber = -i - 1;
                    await _context.SaveChangesAsync(ct);
                    var ordered = new List<Lesson>();
                    foreach (var dto in request.Lessons.OrderBy(l => l.Order))
                    {
                        var lesson = dto.Id.HasValue ? module.Lessons.First(l => l.Id == dto.Id.Value) : null;
                        if (lesson == null)
                        {
                            lesson = new Lesson { ModuleId = module.Id };
                            module.Lessons.Add(lesson);
                            _context.Add(lesson);
                        }
                        lesson.Title = dto.Title.Trim();
                        lesson.ContentMarkdown = dto.Content ?? string.Empty;
                        if (dto.ReadingTimeMinutes.HasValue) lesson.ReadingTimeMinutes = dto.ReadingTimeMinutes.Value;
                        if (dto.YouTubeVideoId != null) lesson.YouTubeVideoId = string.IsNullOrWhiteSpace(dto.YouTubeVideoId) ? null : dto.YouTubeVideoId;
                        ordered.Add(lesson);
                    }
                    ordered.AddRange(existing.Where(l => !ordered.Contains(l)));
                    for (var i = 0; i < ordered.Count; i++) ordered[i].LessonNumber = i + 1;
                }
                module.GenerationStatus = ModuleGenerationStatus.Ready;
                await SyncProjectAsync(course, ct);
                await _context.SaveChangesAsync(ct);
            }, ct);

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
                    order = l.LessonNumber,
                    readingTimeMinutes = l.ReadingTimeMinutes,
                    youTubeVideoId = l.YouTubeVideoId
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

            var (course, error) = await LoadEditableCourseAsync(courseId, ct);
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
            catch (Exception ex) when (ex is not AiUnavailableException && !ct.IsCancellationRequested)
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
            var ownedCourse = course!;

            var errors = CoursePublication.Validate(ownedCourse);
            if (await _context.GenerationJobs.AnyAsync(j => j.CourseId == courseId && (j.Status == "Pending" || j.Status == "Queued" || j.Status == "Running"), ct))
                errors.Add("Wait for content generation to finish.");
            if (errors.Count > 0) return BadRequest(new { message = "This draft is not ready to publish.", errors });
            if (request.IsPaid)
            {
                var chapa = HttpContext.RequestServices.GetRequiredService<IChapaPaymentService>();
                if (!chapa.IsConfigured)
                    return Conflict(new { message = "Payments are not configured. Publish a free course or configure Chapa separately." });
                if (ownedCourse.EnrollmentMode == "ApprovalRequired")
                    return Conflict(new { message = "Paid approval-based enrollment requires a refund workflow and is not enabled. Use Open or InviteOnly enrollment." });
                if (ownedCourse.EnrollmentLimit.HasValue)
                    return Conflict(new { message = "Paid courses with enrollment limits require expiring seat reservations and are not enabled." });
                ownedCourse.PriceAmount = request.Price ?? ownedCourse.PriceAmount;
                ownedCourse.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "ETB" : request.Currency.Trim().ToUpperInvariant();
                if (ownedCourse.PriceAmount is <= 0 or > 9_999_999_999.99m || decimal.Round(ownedCourse.PriceAmount, 2) != ownedCourse.PriceAmount)
                {
                    return BadRequest(new { message = "Use a positive price with at most two decimal places within the supported range." });
                }
                if (ownedCourse.Currency != "ETB") return BadRequest(new { message = "This Chapa integration currently supports ETB only." });
            }

            else
            {
                ownedCourse.PriceAmount = 0;
            }

            ownedCourse.IsPaid = request.IsPaid;
            ownedCourse.IsPublished = true;
            ownedCourse.IsPublic = ownedCourse.Visibility == "Public";
            if (!ownedCourse.IsPaid)
            {
                var strandedPayments = await _context.CourseEnrollments
                    .Where(e => e.CourseId == courseId && e.Status == "PendingPayment")
                    .ToListAsync(ct);
                foreach (var enrollment in strandedPayments) enrollment.Status = "PendingApproval";
            }

            // Keep the originating studio project in sync so the educator home
            // shows the course as Published instead of ReadyForReview.
            var project = await _context.EducatorProjects
                .FirstOrDefaultAsync(p => p.CourseId == ownedCourse.Id, ct);
            if (project != null)
            {
                project.Status = "Published";
                project.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(ct);

            return Ok(new { isPublished = ownedCourse.IsPublished, isPublic = ownedCourse.IsPublic, isPaid = ownedCourse.IsPaid, price = ownedCourse.PriceAmount });
        }

        [HttpPost("courses/{courseId:guid}/unpublish")]
        public async Task<IActionResult> UnpublishCourse(Guid courseId, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;
            var (course, error) = await LoadOwnedCourseAsync(courseId, _context, ct);
            if (error != null) return error;
            if (course!.IsPaid && await _context.Payments.AnyAsync(payment => payment.CourseId == courseId && payment.Status != "Failed", ct))
                return Conflict(new { message = "A paid course with payment history cannot be unpublished without a refund/versioning workflow. Create a revised course instead." });
            course.IsPublished = false;
            course.IsPublic = false;
            var project = await _context.EducatorProjects.FirstOrDefaultAsync(p => p.CourseId == courseId, ct);
            if (project != null)
            {
                project.Status = "ReadyForReview";
                project.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync(ct);
            return Ok(new { isPublished = false, isPublic = false });
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

            // Engagement is course-scoped evidence: completed lessons, not a
            // learner's unrelated global activity replicated across their courses.
            var lessonEvents = await _context.LessonProgress
                .Where(p => p.LearnerId != learnerId && p.IsCompleted && p.CompletedAt >= since && courseIds.Contains(p.Lesson!.Module!.CourseId))
                .Select(p => new { CourseId = p.Lesson!.Module!.CourseId, CompletedAt = p.CompletedAt!.Value })
                .ToListAsync(ct);
            var assessments = await _context.Assessments
                .Where(a => a.LearnerId != learnerId && courseIds.Contains(a.Quiz!.Module!.CourseId))
                .Select(a => new { CourseId = a.Quiz!.Module!.CourseId, a.ScorePercentage, a.IsPassed, a.SubmittedAt })
                .ToListAsync(ct);
            var submissions = await _context.AssignmentSubmissions
                .Where(s => s.LearnerId != learnerId && courseIds.Contains(s.Assignment!.CourseId))
                .Select(s => new { CourseId = s.Assignment!.CourseId, s.Status })
                .ToListAsync(ct);
            var liveSessions = await _context.LiveSessions
                .Where(s => courseIds.Contains(s.CourseId) && s.Status != "Cancelled")
                .Select(s => new { s.CourseId, s.StartTime, s.DurationMinutes, s.Status })
                .ToListAsync(ct);

            var trafficByCourse = courses.ToDictionary(c => c.Id, _ => new Dictionary<DateTime, (int enrollments, int activity)>());

            foreach (var course in courses)
            {
                var map = trafficByCourse[course.Id];
                for (var day = since; day <= DateTime.UtcNow.Date; day = day.AddDays(1))
                {
                    map[day] = (0, 0);
                }
                foreach (var enrollment in course.Enrollments.Where(e => e.LearnerId != course.CreatorLearnerId && e.Status != "Rejected"))
                {
                    var day = enrollment.EnrolledAt.Date;
                    if (map.ContainsKey(day)) map[day] = (map[day].enrollments + 1, map[day].activity);
                }
            }

            foreach (var activity in lessonEvents)
            {
                var map = trafficByCourse[activity.CourseId];
                var day = activity.CompletedAt.Date;
                if (map.ContainsKey(day)) map[day] = (map[day].enrollments, map[day].activity + 1);
            }

            var totalEnrollments = courses.Sum(c => c.Enrollments.Count(e => e.LearnerId != c.CreatorLearnerId && e.Status != "Rejected"));
            var totalPending = courses.Sum(c => c.Enrollments.Count(e => e.LearnerId != c.CreatorLearnerId && e.Status == "PendingApproval"));
            var published = courses.Count(c => c.IsPublished);

            var result = courses.Select(c =>
            {
                var learnerEnrollments = c.Enrollments.Where(e => e.LearnerId != c.CreatorLearnerId).ToList();
                var active = learnerEnrollments.Count(e => e.Status == "Active");
                var total = learnerEnrollments.Count(e => e.Status != "Rejected");
                var completed = learnerEnrollments.Count(e => e.Status == "Active" && e.ProgressPercentage == 100);
                var courseAssessments = assessments.Where(a => a.CourseId == c.Id).ToList();
                var courseSubmissions = submissions.Where(s => s.CourseId == c.Id).ToList();
                var upcoming = liveSessions.Count(s => s.CourseId == c.Id && s.Status == "Scheduled" && s.StartTime >= DateTime.UtcNow);
                return new
                {
                    courseId = c.Id,
                    title = c.Title,
                    isPublic = c.IsPublic,
                    isPublished = c.IsPublished,
                    visibility = c.Visibility,
                    isPaid = c.IsPaid,
                    price = c.PriceAmount,
                    enrollments = total,
                    activeEnrollments = active,
                    activeLearnersLast30Days = learnerEnrollments.Count(e => e.Status == "Active" && e.LastAccessedAt >= since),
                    pendingApprovals = learnerEnrollments.Count(e => e.Status == "PendingApproval"),
                    avgProgress = active == 0 ? 0 : (int)Math.Round(learnerEnrollments.Where(e => e.Status == "Active").Average(e => e.ProgressPercentage)),
                    completedEnrollments = completed,
                    completionRate = active == 0 ? 0 : (int)Math.Round(100.0 * completed / active),
                    assessmentAttempts = courseAssessments.Count,
                    avgAssessmentScore = courseAssessments.Count == 0 ? (int?)null : (int)Math.Round(courseAssessments.Average(a => a.ScorePercentage)),
                    assessmentPassRate = courseAssessments.Count == 0 ? (int?)null : (int)Math.Round(100.0 * courseAssessments.Count(a => a.IsPassed) / courseAssessments.Count),
                    assignmentSubmissions = courseSubmissions.Count,
                    pendingGrading = courseSubmissions.Count(s => s.Status == "Submitted"),
                    upcomingLiveSessions = upcoming,
                    metricDefinitions = new
                    {
                        activeLearnerWindowDays = 30,
                        completionDenominator = "Active enrollments",
                        assessmentDenominator = "Submitted quiz attempts",
                        engagement = "Completed lessons in the last 30 days"
                    },
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
                    totalCourses = courses.Count,
                    publishedCourses = published,
                    activeLearnersLast30Days = courses.SelectMany(c => c.Enrollments)
                        .Where(e => e.LearnerId != learnerId && e.Status == "Active" && e.LastAccessedAt >= since)
                        .Select(e => e.LearnerId).Distinct().Count(),
                    pendingGrading = submissions.Count(s => s.Status == "Submitted"),
                    upcomingLiveSessions = liveSessions.Count(s => s.Status == "Scheduled" && s.StartTime >= DateTime.UtcNow)
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
                .Where(e => e.Course != null && e.Course.CreatorLearnerId == learnerId && e.LearnerId != learnerId);

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
            await using var transaction = await CourseAccess.BeginMutationAsync(_context, ct);
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

            if (enrollment.Status != "PendingApproval")
                return Conflict(new { message = $"Only a pending approval request can be {status.ToLowerInvariant()}. Current status: {enrollment.Status}." });
            if (status == "Active")
            {
                if (!enrollment.Course.IsPublished) return Conflict(new { message = "Republish the course before approving learners." });
                if (enrollment.Course.EnrollmentLimit.HasValue && await _context.CourseEnrollments
                        .CountAsync(e => e.CourseId == enrollment.CourseId && e.Status == "Active", ct) >= enrollment.Course.EnrollmentLimit.Value)
                    return Conflict(new { message = "This course has reached its active enrollment limit." });
            }
            if (status == "Rejected" && await _context.Payments
                    .AnyAsync(payment => payment.CourseId == enrollment.CourseId && payment.LearnerId == enrollment.LearnerId && payment.Status == "Completed", ct))
                return Conflict(new { message = "A completed payment cannot be rejected until a refund workflow is available." });

            enrollment.Status = status;
            await _context.SaveChangesAsync(ct);
            if (transaction != null) await transaction.CommitAsync(ct);

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

        // ------------------------------------------------------------------
        // Course Templates
        // ------------------------------------------------------------------

        [HttpGet("templates")]
        public IActionResult GetTemplates()
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;
            var templates = CourseTemplateCatalog.GetAll();
            return Ok(templates);
        }

        // ------------------------------------------------------------------
        // Manual Course Creation
        // ------------------------------------------------------------------

        public class CreateManualCourseDto
        {
            [Required, StringLength(200)]
            public string Title { get; set; } = string.Empty;
            [StringLength(2000)]
            public string Description { get; set; } = string.Empty;
            [StringLength(120)]
            public string Category { get; set; } = "Software Engineering";
            [StringLength(160)]
            public string TargetAudience { get; set; } = "General";
            [StringLength(64)]
            public string EstimatedDuration { get; set; } = "4 weeks";
            public string? TemplateId { get; set; }
            public string AccentColor { get; set; } = "#1B3D2F";
            public string ThemeMode { get; set; } = "pine";
            public string EnrollmentMode { get; set; } = "Open";
            public string Visibility { get; set; } = "Public";
            [StringLength(64)]
            public string Level { get; set; } = "Beginner";
            [StringLength(64)]
            public string Language { get; set; } = "English";
            [MaxLength(30)]
            public List<string> LearningObjectives { get; set; } = new();
            public bool RequirePassingQuizzes { get; set; } = true;
            [MaxLength(50)]
            public List<CourseTemplateModuleDto>? Modules { get; set; }
        }

        [HttpPost("courses/manual")]
        public async Task<IActionResult> CreateManualCourse([FromBody] CreateManualCourseDto dto, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var settingsError = SettingsError(dto.Visibility, dto.EnrollmentMode, dto.AccentColor, dto.ThemeMode);
            if (settingsError != null) return BadRequest(new { message = settingsError });
            if (dto.LearningObjectives == null || dto.LearningObjectives.Any(o => o == null || o.Length > 500))
                return BadRequest(new { message = "Use learning objectives of at most 500 characters each." });
            var template = string.IsNullOrWhiteSpace(dto.TemplateId) ? null : CourseTemplateCatalog.GetAll()
                .FirstOrDefault(t => t.Id.Equals(dto.TemplateId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(dto.TemplateId) && template == null)
                return BadRequest(new { message = "The selected course template does not exist." });
            if (template != null && dto.Modules != null)
                return BadRequest(new { message = "Choose a template or a reviewed curriculum, not both." });

            var educatorId = await GetCurrentLearnerIdAsync(_context);
            var course = new Course
            {
                Title = string.IsNullOrWhiteSpace(dto.Title) ? "Untitled Course" : dto.Title.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                Category = string.IsNullOrWhiteSpace(dto.Category) ? "General" : dto.Category.Trim(),
                TargetAudience = string.IsNullOrWhiteSpace(dto.TargetAudience) ? "All Learners" : dto.TargetAudience.Trim(),
                EstimatedDuration = string.IsNullOrWhiteSpace(dto.EstimatedDuration) ? "4 weeks" : dto.EstimatedDuration.Trim(),
                CreatorLearnerId = educatorId,
                IsPublic = false,
                IsPublished = false,
                Visibility = dto.Visibility,
                Level = string.IsNullOrWhiteSpace(dto.Level) ? template?.Level ?? "Beginner" : dto.Level.Trim(),
                Language = string.IsNullOrWhiteSpace(dto.Language) ? "English" : dto.Language.Trim(),
                LearningObjectives = (dto.LearningObjectives.Count == 0 ? template?.LearningObjectives ?? dto.LearningObjectives : dto.LearningObjectives)
                    .Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).Distinct().ToList(),
                RequirePassingQuizzes = dto.RequirePassingQuizzes,
                ProviderName = await _context.Learners.Where(l => l.Id == educatorId).Select(l => l.Name).FirstAsync(ct),
                ShareCode = Guid.NewGuid().ToString("N").ToUpperInvariant(),
                AccentColor = string.IsNullOrWhiteSpace(dto.AccentColor) ? "#1B3D2F" : dto.AccentColor,
                ThemeMode = string.IsNullOrWhiteSpace(dto.ThemeMode) ? "pine" : dto.ThemeMode,
                EnrollmentMode = string.IsNullOrWhiteSpace(dto.EnrollmentMode) ? "Open" : dto.EnrollmentMode,
                CreatedAt = DateTime.UtcNow
            };

            var curriculum = dto.Modules ?? template?.Modules;
            if (curriculum != null)
            {
                if (template != null && string.IsNullOrWhiteSpace(dto.Description)) course.Description = template.Description;
                int modIndex = 1;
                foreach (var modTmpl in curriculum)
                {
                    if (modTmpl.Lessons == null) return BadRequest(new { message = "Each module must include a lesson list." });
                    var mod = new Module
                    {
                        CourseId = course.Id,
                        ModuleNumber = modIndex++,
                        Title = modTmpl.Title.Trim(),
                        Summary = modTmpl.Summary ?? string.Empty,
                        EstimatedHours = modTmpl.EstimatedHours,
                        GenerationStatus = ModuleGenerationStatus.Ready,
                        GeneratedAt = DateTime.UtcNow
                    };
                    int lesIndex = 1;
                    foreach (var lesTmpl in modTmpl.Lessons)
                    {
                        mod.Lessons.Add(new Lesson
                        {
                            ModuleId = mod.Id,
                            LessonNumber = lesIndex++,
                            Title = lesTmpl.Title.Trim(),
                            Summary = lesTmpl.Summary ?? string.Empty,
                            ContentMarkdown = lesTmpl.ContentMarkdown ?? string.Empty,
                            ReadingTimeMinutes = lesTmpl.ReadingTimeMinutes
                        });
                    }
                    course.Modules.Add(mod);
                }
            }
            else
            {
                var initialMod = new Module
                {
                    CourseId = course.Id,
                    ModuleNumber = 1,
                    Title = "Module 1: Introduction & Overview",
                    Summary = "Get started with the fundamentals.",
                    EstimatedHours = 2,
                    GenerationStatus = ModuleGenerationStatus.Ready,
                    GeneratedAt = DateTime.UtcNow
                };
                initialMod.Lessons.Add(new Lesson
                {
                    ModuleId = initialMod.Id,
                    LessonNumber = 1,
                    Title = "Lesson 1: Welcome & Course Objectives",
                    Summary = "Welcome your learners and outline the journey ahead.",
                    ContentMarkdown = string.Empty,
                    ReadingTimeMinutes = 10,
                    CreatedAt = DateTime.UtcNow
                });
                course.Modules.Add(initialMod);
            }

            if (template != null && !string.IsNullOrWhiteSpace(template.SampleAssignmentTitle))
            {
                course.Assignments.Add(new Assignment
                {
                    CourseId = course.Id,
                    Title = template.SampleAssignmentTitle,
                    InstructionsMarkdown = template.SampleAssignmentInstructions
                });
            }

            _context.Add(course);

            var project = new EducatorProject
            {
                EducatorLearnerId = educatorId,
                Title = course.Title,
                Description = course.Description,
                Status = "ReadyForReview",
                CourseId = course.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Add(project);

            await _context.SaveChangesAsync(ct);

            return Ok(new
            {
                courseId = course.Id,
                projectId = project.Id,
                title = course.Title,
                shareCode = course.ShareCode
            });
        }

        // ------------------------------------------------------------------
        // Curriculum Structure (Add/Delete Modules & Lessons)
        // ------------------------------------------------------------------

        public class CreateModuleDto
        {
            [Required, StringLength(200)]
            public string Title { get; set; } = string.Empty;
            [StringLength(2000)]
            public string Summary { get; set; } = string.Empty;
            [Range(1, 1000)]
            public int EstimatedHours { get; set; } = 3;
        }

        [HttpPost("courses/{courseId:guid}/modules")]
        public async Task<IActionResult> AddModule(Guid courseId, [FromBody] CreateModuleDto dto, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadEditableCourseAsync(courseId, ct);
            if (error != null) return error;

            if (course!.Modules.Count >= 50) return BadRequest(new { message = "A course can contain at most 50 modules." });

            var nextNumber = (course!.Modules.Any() ? course.Modules.Max(m => m.ModuleNumber) : 0) + 1;
            var newModule = new Module
            {
                CourseId = courseId,
                ModuleNumber = nextNumber,
                Title = string.IsNullOrWhiteSpace(dto.Title) ? $"Module {nextNumber}" : dto.Title.Trim(),
                Summary = dto.Summary?.Trim() ?? string.Empty,
                EstimatedHours = dto.EstimatedHours > 0 ? dto.EstimatedHours : 3,
                GenerationStatus = ModuleGenerationStatus.Ready,
                GeneratedAt = DateTime.UtcNow
            };

            // Include an initial lesson
            newModule.Lessons.Add(new Lesson
            {
                ModuleId = newModule.Id,
                LessonNumber = 1,
                Title = $"Lesson 1: Introduction to {newModule.Title}",
                Summary = "Overview of this module.",
                ContentMarkdown = string.Empty,
                ReadingTimeMinutes = 10,
                CreatedAt = DateTime.UtcNow
            });

            _context.Add(newModule);
            await _context.SaveChangesAsync(ct);

            return Ok(new
            {
                moduleId = newModule.Id,
                moduleNumber = newModule.ModuleNumber,
                title = newModule.Title
            });
        }

        [HttpDelete("courses/{courseId:guid}/modules/{moduleId:guid}")]
        public async Task<IActionResult> DeleteModule(Guid courseId, Guid moduleId, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadEditableCourseAsync(courseId, ct);
            if (error != null) return error;

            var module = course!.Modules.FirstOrDefault(m => m.Id == moduleId);
            if (module == null) return NotFound(new { message = "Module not found." });

            var lessonIds = module.Lessons.Select(l => l.Id).ToList();
            var quizIds = module.Quizzes.Select(q => q.Id).ToList();
            if (await _context.LessonProgress.AnyAsync(p => lessonIds.Contains(p.LessonId), ct) ||
                await _context.QuizAttempts.AnyAsync(a => quizIds.Contains(a.QuizId), ct) ||
                await _context.Assessments.AnyAsync(a => quizIds.Contains(a.QuizId), ct))
                return Conflict(new { message = "This module has learner history. Keep it to preserve existing progress and assessment results." });

            _context.Remove(module);
            await _context.SaveChangesAsync(ct);

            return Ok(new { success = true, deletedModuleId = moduleId });
        }

        public class CreateLessonDto
        {
            [Required, StringLength(200)]
            public string Title { get; set; } = string.Empty;
            [StringLength(2000)]
            public string Summary { get; set; } = string.Empty;
            [StringLength(20000)]
            public string ContentMarkdown { get; set; } = string.Empty;
            [Range(1, 1440)]
            public int ReadingTimeMinutes { get; set; } = 10;
        }

        [HttpPost("courses/{courseId:guid}/modules/{moduleId:guid}/lessons")]
        public async Task<IActionResult> AddLesson(Guid courseId, Guid moduleId, [FromBody] CreateLessonDto dto, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadEditableCourseAsync(courseId, ct);
            if (error != null) return error;

            var module = course!.Modules.FirstOrDefault(m => m.Id == moduleId);
            if (module == null) return NotFound(new { message = "Module not found." });
            if (module.Lessons.Count >= 100) return BadRequest(new { message = "A module can contain at most 100 lessons." });

            var nextNumber = (module.Lessons.Any() ? module.Lessons.Max(l => l.LessonNumber) : 0) + 1;
            var lesson = new Lesson
            {
                ModuleId = moduleId,
                LessonNumber = nextNumber,
                Title = string.IsNullOrWhiteSpace(dto.Title) ? $"Lesson {nextNumber}" : dto.Title.Trim(),
                Summary = dto.Summary?.Trim() ?? string.Empty,
                ContentMarkdown = dto.ContentMarkdown ?? string.Empty,
                ReadingTimeMinutes = dto.ReadingTimeMinutes > 0 ? dto.ReadingTimeMinutes : 10,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(lesson);
            await _context.SaveChangesAsync(ct);

            return Ok(new
            {
                lessonId = lesson.Id,
                lessonNumber = lesson.LessonNumber,
                title = lesson.Title
            });
        }

        [HttpDelete("courses/{courseId:guid}/modules/{moduleId:guid}/lessons/{lessonId:guid}")]
        public async Task<IActionResult> DeleteLesson(Guid courseId, Guid moduleId, Guid lessonId, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadEditableCourseAsync(courseId, ct);
            if (error != null) return error;

            var module = course!.Modules.FirstOrDefault(m => m.Id == moduleId);
            if (module == null) return NotFound(new { message = "Module not found." });

            var lesson = module.Lessons.FirstOrDefault(l => l.Id == lessonId);
            if (lesson == null) return NotFound(new { message = "Lesson not found." });
            if (await _context.LessonProgress.AnyAsync(p => p.LessonId == lessonId, ct))
                return Conflict(new { message = "This lesson has learner history and cannot be deleted. Its saved content can still be updated." });

            _context.Remove(lesson);
            await _context.SaveChangesAsync(ct);

            return Ok(new { success = true, deletedLessonId = lessonId });
        }

        // ------------------------------------------------------------------
        // Course Branding & Settings
        // ------------------------------------------------------------------

        public class UpdateBrandingDto
        {
            [StringLength(200, MinimumLength = 1)]
            public string? Title { get; set; }
            [StringLength(2000)]
            public string? Description { get; set; }
            [StringLength(120)]
            public string? Category { get; set; }
            [StringLength(160)]
            public string? TargetAudience { get; set; }
            [StringLength(64)]
            public string? EstimatedDuration { get; set; }
            public string? AccentColor { get; set; }
            public string? ThemeMode { get; set; }
            public string? EnrollmentMode { get; set; }
            [Range(1, 100)]
            public int? PassingScorePercentage { get; set; }
            [Range(1, 100)]
            public int? MaxQuizAttempts { get; set; }
            [StringLength(2000)]
            public string? Prerequisites { get; set; }
            [StringLength(4000)]
            public string? InstructorBio { get; set; }
            [StringLength(200)]
            public string? InstructorTitle { get; set; }
            public string? Visibility { get; set; }
            [StringLength(64)]
            public string? Level { get; set; }
            [StringLength(64)]
            public string? Language { get; set; }
            [MaxLength(30)]
            public List<string>? LearningObjectives { get; set; }
            public bool? RequirePassingQuizzes { get; set; }
            // Undefined means unchanged; explicit JSON null clears the optional setting.
            public JsonElement EnrollmentLimit { get; set; }
            public JsonElement EnrollmentOpensAt { get; set; }
            public JsonElement EnrollmentClosesAt { get; set; }
        }

        [HttpPut("courses/{courseId:guid}/branding")]
        public async Task<IActionResult> UpdateBranding(Guid courseId, [FromBody] UpdateBrandingDto dto, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadEditableCourseAsync(courseId, ct);
            if (error != null) return error;

            var settingsError = SettingsError(dto.Visibility, dto.EnrollmentMode, dto.AccentColor, dto.ThemeMode);
            if (settingsError != null) return BadRequest(new { message = settingsError });
            if ((dto.Title != null && string.IsNullOrWhiteSpace(dto.Title)) ||
                dto.LearningObjectives?.Any(o => o == null || o.Length > 500) == true)
                return BadRequest(new { message = "Use a nonempty title and learning objectives of at most 500 characters each." });
            var limit = course!.EnrollmentLimit;
            var opens = course.EnrollmentOpensAt;
            var closes = course.EnrollmentClosesAt;
            if (dto.EnrollmentLimit.ValueKind != JsonValueKind.Undefined)
            {
                if (dto.EnrollmentLimit.ValueKind == JsonValueKind.Null) limit = null;
                else if (dto.EnrollmentLimit.ValueKind == JsonValueKind.Number && dto.EnrollmentLimit.TryGetInt32(out var value) && value is > 0 and <= 1000000) limit = value;
                else return BadRequest(new { message = "Enrollment limit must be a positive number, or null for unlimited." });
            }
            foreach (var (element, isOpen) in new[] { (dto.EnrollmentOpensAt, true), (dto.EnrollmentClosesAt, false) })
            {
                if (element.ValueKind == JsonValueKind.Undefined) continue;
                DateTime? value = null;
                if (element.ValueKind != JsonValueKind.Null)
                {
                    if (element.ValueKind != JsonValueKind.String || !element.TryGetDateTime(out var parsed) || parsed.Kind == DateTimeKind.Unspecified)
                        return BadRequest(new { message = "Enrollment dates must include a time zone, or be null." });
                    value = parsed.ToUniversalTime();
                }
                if (isOpen) opens = value; else closes = value;
            }
            if (opens.HasValue && closes.HasValue && closes <= opens)
                return BadRequest(new { message = "Enrollment closing must be after opening." });
            if (limit.HasValue && limit < await _context.CourseEnrollments.CountAsync(e => e.CourseId == courseId && e.Status == "Active", ct))
                return BadRequest(new { message = "Enrollment limit cannot be lower than the current active enrollment count." });

            if (!string.IsNullOrWhiteSpace(dto.Title)) course!.Title = dto.Title.Trim();
            if (dto.Description != null) course!.Description = dto.Description.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Category)) course!.Category = dto.Category.Trim();
            if (!string.IsNullOrWhiteSpace(dto.TargetAudience)) course!.TargetAudience = dto.TargetAudience.Trim();
            if (!string.IsNullOrWhiteSpace(dto.EstimatedDuration)) course!.EstimatedDuration = dto.EstimatedDuration.Trim();
            if (!string.IsNullOrWhiteSpace(dto.AccentColor)) course!.AccentColor = dto.AccentColor.Trim();
            if (!string.IsNullOrWhiteSpace(dto.ThemeMode)) course!.ThemeMode = dto.ThemeMode.Trim();
            if (!string.IsNullOrWhiteSpace(dto.EnrollmentMode)) course!.EnrollmentMode = dto.EnrollmentMode.Trim();
            if (dto.PassingScorePercentage.HasValue && dto.PassingScorePercentage > 0) course!.PassingScorePercentage = dto.PassingScorePercentage.Value;
            if (dto.MaxQuizAttempts.HasValue && dto.MaxQuizAttempts > 0) course!.MaxQuizAttempts = dto.MaxQuizAttempts.Value;
            if (dto.Prerequisites != null) course!.Prerequisites = dto.Prerequisites.Trim();
            if (dto.InstructorBio != null) course!.InstructorBio = dto.InstructorBio.Trim();
            if (dto.InstructorTitle != null) course!.InstructorTitle = dto.InstructorTitle.Trim();
            if (dto.Visibility != null) course.Visibility = dto.Visibility;
            if (dto.Level != null) course.Level = dto.Level.Trim();
            if (dto.Language != null) course.Language = dto.Language.Trim();
            if (dto.LearningObjectives != null) course.LearningObjectives = dto.LearningObjectives.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).Distinct().ToList();
            if (dto.RequirePassingQuizzes.HasValue) course.RequirePassingQuizzes = dto.RequirePassingQuizzes.Value;
            course.EnrollmentLimit = limit;
            course.EnrollmentOpensAt = opens;
            course.EnrollmentClosesAt = closes;

            await SyncProjectAsync(course, ct);
            await _context.SaveChangesAsync(ct);
            return Ok(new
            {
                course!.Id,
                course.Title,
                course.AccentColor,
                course.ThemeMode,
                course.EnrollmentMode,
                course.PassingScorePercentage
            });
        }

        // ------------------------------------------------------------------
        // Live Sessions (Cohort & Scheduled Classes)
        // ------------------------------------------------------------------

        [HttpGet("courses/{courseId:guid}/live-sessions")]
        public async Task<IActionResult> GetEducatorLiveSessions(Guid courseId, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadOwnedCourseAsync(courseId, _context, ct);
            if (error != null) return error;

            var sessions = await _context.LiveSessions
                .Where(s => s.CourseId == courseId)
                .OrderBy(s => s.StartTime)
                .ToListAsync(ct);

            return Ok(sessions.Select(SessionDto));
        }

        public class CreateLiveSessionDto
        {
            [Required, StringLength(200)]
            public string Title { get; set; } = string.Empty;
            [StringLength(2000)]
            public string Description { get; set; } = string.Empty;
            public DateTimeOffset StartTime { get; set; }
            [Range(1, 1440)]
            public int DurationMinutes { get; set; } = 60;
            [Required, StringLength(64)]
            public string TimeZone { get; set; } = "UTC";
            [Required, StringLength(2048)]
            public string MeetingUrl { get; set; } = string.Empty;
        }

        [HttpPost("courses/{courseId:guid}/live-sessions")]
        public async Task<IActionResult> CreateLiveSession(Guid courseId, [FromBody] CreateLiveSessionDto dto, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadOwnedCourseAsync(courseId, _context, ct);
            if (error != null) return error;
            var validation = ValidateLiveSession(dto);
            if (validation != null) return BadRequest(new { message = validation });

            var session = new LiveSession
            {
                CourseId = courseId,
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                StartTime = dto.StartTime.UtcDateTime,
                DurationMinutes = dto.DurationMinutes,
                TimeZone = dto.TimeZone.Trim(),
                MeetingUrl = dto.MeetingUrl.Trim(),
                Status = "Scheduled",
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(session);
            await _context.SaveChangesAsync(ct);
            await NotifyEnrolledAsync(courseId, "live_session_scheduled", "Live class scheduled",
                $"{course!.Title}: {session.Title} is scheduled for {session.StartTime:u}.", $"/courses/{courseId}", ct);

            return Ok(SessionDto(session));
        }

        [HttpPut("courses/{courseId:guid}/live-sessions/{sessionId:guid}")]
        public async Task<IActionResult> UpdateLiveSession(Guid courseId, Guid sessionId, [FromBody] CreateLiveSessionDto dto, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadOwnedCourseAsync(courseId, _context, ct);
            if (error != null) return error;

            var session = await _context.LiveSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.CourseId == courseId, ct);
            if (session == null) return NotFound(new { message = "Session not found." });
            if (EffectiveSessionStatus(session) != "Scheduled")
                return Conflict(new { message = "Only an upcoming scheduled session can be edited." });
            var validation = ValidateLiveSession(dto);
            if (validation != null) return BadRequest(new { message = validation });

            session.Title = dto.Title.Trim();
            session.Description = dto.Description?.Trim() ?? string.Empty;
            session.StartTime = dto.StartTime.UtcDateTime;
            session.DurationMinutes = dto.DurationMinutes;
            session.TimeZone = dto.TimeZone.Trim();
            session.MeetingUrl = dto.MeetingUrl.Trim();

            await _context.SaveChangesAsync(ct);
            await NotifyEnrolledAsync(courseId, "live_session_updated", "Live class updated",
                $"{course!.Title}: {session.Title} is now scheduled for {session.StartTime:u}.", $"/courses/{courseId}", ct);
            return Ok(SessionDto(session));
        }

        [HttpDelete("courses/{courseId:guid}/live-sessions/{sessionId:guid}")]
        public async Task<IActionResult> DeleteLiveSession(Guid courseId, Guid sessionId, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadOwnedCourseAsync(courseId, _context, ct);
            if (error != null) return error;

            var session = await _context.LiveSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.CourseId == courseId, ct);
            if (session == null) return NotFound(new { message = "Session not found." });

            if (session.Status == "Cancelled") return Ok(SessionDto(session));
            if (EffectiveSessionStatus(session) == "Completed")
                return Conflict(new { message = "A completed session cannot be cancelled." });
            session.Status = "Cancelled";
            await _context.SaveChangesAsync(ct);
            await NotifyEnrolledAsync(courseId, "live_session_cancelled", "Live class cancelled",
                $"{course!.Title}: {session.Title} has been cancelled.", $"/courses/{courseId}", ct);
            return Ok(SessionDto(session));
        }

        // ------------------------------------------------------------------
        // Assignments & Submissions
        // ------------------------------------------------------------------

        [HttpGet("courses/{courseId:guid}/assignments")]
        public async Task<IActionResult> GetEducatorAssignments(Guid courseId, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadOwnedCourseAsync(courseId, _context, ct);
            if (error != null) return error;

            var assignments = await _context.Assignments
                .Include(a => a.Submissions)
                .Where(a => a.CourseId == courseId)
                .OrderBy(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.CourseId,
                    a.Title,
                    a.InstructionsMarkdown,
                    a.DueDaysAfterEnrollment,
                    a.MaxScore,
                    a.AllowedFileTypes,
                    SubmissionMode = "TextOnly",
                    a.CreatedAt,
                    SubmissionsCount = a.Submissions.Count,
                    PendingGradingCount = a.Submissions.Count(s => s.Status == "Submitted")
                })
                .ToListAsync(ct);

            return Ok(assignments);
        }

        public class CreateAssignmentDto
        {
            [Required, StringLength(200)]
            public string Title { get; set; } = string.Empty;
            [Required, StringLength(20000)]
            public string InstructionsMarkdown { get; set; } = string.Empty;
            [Range(0, 3650)]
            public int DueDaysAfterEnrollment { get; set; } = 7;
            [StringLength(20000)]
            public string RubricCriteriaJson { get; set; } = "[]";
            [Range(1, 100000)]
            public int MaxScore { get; set; } = 100;
        }

        [HttpPost("courses/{courseId:guid}/assignments")]
        public async Task<IActionResult> CreateAssignment(Guid courseId, [FromBody] CreateAssignmentDto dto, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadEditableCourseAsync(courseId, ct);
            if (error != null) return error;
            var rubricError = ValidateRubric(dto.RubricCriteriaJson);
            if (rubricError != null) return BadRequest(new { message = rubricError });

            var assignment = new Assignment
            {
                CourseId = courseId,
                Title = dto.Title.Trim(),
                InstructionsMarkdown = dto.InstructionsMarkdown.Trim(),
                DueDaysAfterEnrollment = dto.DueDaysAfterEnrollment,
                RubricCriteriaJson = string.IsNullOrWhiteSpace(dto.RubricCriteriaJson) ? "[]" : dto.RubricCriteriaJson,
                MaxScore = dto.MaxScore,
                AllowedFileTypes = string.Empty,
                MaxFileSizeMb = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(assignment);
            await _context.SaveChangesAsync(ct);

            return Ok(new { assignment.Id, assignment.CourseId, assignment.Title, assignment.InstructionsMarkdown,
                assignment.DueDaysAfterEnrollment, assignment.RubricCriteriaJson, assignment.MaxScore,
                submissionMode = "TextOnly", assignment.CreatedAt });
        }

        [HttpPut("courses/{courseId:guid}/assignments/{assignmentId:guid}")]
        public async Task<IActionResult> UpdateAssignment(Guid courseId, Guid assignmentId, [FromBody] CreateAssignmentDto dto, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;
            var (course, error) = await LoadEditableCourseAsync(courseId, ct);
            if (error != null) return error;
            var assignment = await _context.Assignments.Include(a => a.Submissions)
                .FirstOrDefaultAsync(a => a.Id == assignmentId && a.CourseId == courseId, ct);
            if (assignment == null) return NotFound(new { message = "Assignment not found." });
            if (assignment.Submissions.Count > 0) return Conflict(new { message = "This assignment has learner submissions and cannot be reconfigured." });
            var rubricError = ValidateRubric(dto.RubricCriteriaJson);
            if (rubricError != null) return BadRequest(new { message = rubricError });
            assignment.Title = dto.Title.Trim();
            assignment.InstructionsMarkdown = dto.InstructionsMarkdown.Trim();
            assignment.DueDaysAfterEnrollment = dto.DueDaysAfterEnrollment;
            assignment.RubricCriteriaJson = string.IsNullOrWhiteSpace(dto.RubricCriteriaJson) ? "[]" : dto.RubricCriteriaJson;
            assignment.MaxScore = dto.MaxScore;
            await _context.SaveChangesAsync(ct);
            return Ok(new { assignment.Id, assignment.Title, assignment.InstructionsMarkdown, assignment.DueDaysAfterEnrollment,
                assignment.RubricCriteriaJson, assignment.MaxScore, submissionMode = "TextOnly" });
        }

        [HttpDelete("courses/{courseId:guid}/assignments/{assignmentId:guid}")]
        public async Task<IActionResult> DeleteAssignment(Guid courseId, Guid assignmentId, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;
            var (_, error) = await LoadEditableCourseAsync(courseId, ct);
            if (error != null) return error;
            var assignment = await _context.Assignments.Include(a => a.Submissions)
                .FirstOrDefaultAsync(a => a.Id == assignmentId && a.CourseId == courseId, ct);
            if (assignment == null) return NotFound(new { message = "Assignment not found." });
            if (assignment.Submissions.Count > 0) return Conflict(new { message = "This assignment has learner submissions and cannot be deleted." });
            _context.Remove(assignment);
            await _context.SaveChangesAsync(ct);
            return Ok(new { deletedAssignmentId = assignmentId });
        }

        [HttpGet("courses/{courseId:guid}/assignments/{assignmentId:guid}/submissions")]
        public async Task<IActionResult> GetAssignmentSubmissions(Guid courseId, Guid assignmentId, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var (course, error) = await LoadOwnedCourseAsync(courseId, _context, ct);
            if (error != null) return error;

            var submissions = await _context.AssignmentSubmissions
                .Include(s => s.Learner)
                .Where(s => s.AssignmentId == assignmentId && s.Assignment!.CourseId == courseId)
                .OrderByDescending(s => s.SubmittedAt)
                .Select(s => new
                {
                    s.Id,
                    s.AssignmentId,
                    s.LearnerId,
                    LearnerName = s.Learner != null ? s.Learner.Name : "Learner",
                    LearnerEmail = s.Learner != null ? s.Learner.Email : "",
                    s.SubmissionText,
                    s.FileUrl,
                    s.FileName,
                    s.SubmittedAt,
                    s.Status,
                    s.Score,
                    s.Feedback,
                    s.GradedAt
                })
                .ToListAsync(ct);

            return Ok(submissions);
        }

        public class GradeSubmissionDto
        {
            public int? Score { get; set; }
            [StringLength(4000)]
            public string? Feedback { get; set; }
            public bool RequestResubmission { get; set; }
        }

        [HttpPost("assignments/submissions/{submissionId:guid}/grade")]
        public async Task<IActionResult> GradeSubmission(Guid submissionId, [FromBody] GradeSubmissionDto dto, CancellationToken ct)
        {
            var guard = EducatorGuard();
            if (guard != null) return guard;

            var educatorId = await GetCurrentLearnerIdAsync(_context);
            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a!.Course)
                .FirstOrDefaultAsync(s => s.Id == submissionId, ct);

            if (submission == null) return NotFound(new { message = "Submission not found." });
            if (submission.Assignment?.Course?.CreatorLearnerId != educatorId)
            {
                return StatusCode(403, new { message = "You do not own this course." });
            }
            if (dto.RequestResubmission && string.IsNullOrWhiteSpace(dto.Feedback))
                return BadRequest(new { message = "Explain what the learner should revise." });
            if (!dto.RequestResubmission && (!dto.Score.HasValue || dto.Score < 0 || dto.Score > submission.Assignment.MaxScore))
                return BadRequest(new { message = $"Score must be between 0 and {submission.Assignment.MaxScore}." });

            submission.Score = dto.RequestResubmission ? null : dto.Score;
            submission.Feedback = dto.Feedback?.Trim();
            submission.Status = dto.RequestResubmission ? "ResubmissionRequested" : "Graded";
            submission.GradedAt = DateTime.UtcNow;
            submission.GradedByEducatorId = educatorId;

            await _context.SaveChangesAsync(ct);

            try
            {
                await _notificationService.SendNotificationAsync(
                    submission.LearnerId,
                    dto.RequestResubmission ? "assignment_resubmission" : "assignment_graded",
                    dto.RequestResubmission ? "Assignment revision requested" : "Assignment graded",
                    dto.RequestResubmission
                        ? $"Your instructor requested revisions for '{submission.Assignment?.Title}'."
                        : $"Your submission for '{submission.Assignment?.Title}' received {submission.Score} points.",
                    $"/courses/{submission.Assignment?.CourseId}",
                    ct);
            }
            catch { }

            return Ok(new
            {
                submission.Id,
                submission.Status,
                submission.Score,
                submission.Feedback,
                submission.GradedAt
            });
        }

        private static string EffectiveSessionStatus(LiveSession session)
        {
            if (session.Status != "Scheduled") return session.Status;
            var now = DateTime.UtcNow;
            if (session.StartTime <= now && session.StartTime.AddMinutes(session.DurationMinutes) > now) return "Live";
            return session.StartTime.AddMinutes(session.DurationMinutes) <= now ? "Completed" : "Scheduled";
        }

        private static object SessionDto(LiveSession session) => new
        {
            session.Id, session.CourseId, session.Title, session.Description, session.StartTime,
            session.DurationMinutes, session.TimeZone, session.MeetingUrl,
            Status = EffectiveSessionStatus(session), session.CreatedAt
        };

        private static string? ValidateLiveSession(CreateLiveSessionDto dto)
        {
            if (dto.StartTime == default || dto.StartTime.UtcDateTime <= DateTime.UtcNow)
                return "Choose a future class date and time.";
            if (!IsWebUrl(dto.MeetingUrl)) return "Add a valid HTTP(S) meeting URL.";
            try { _ = TimeZoneInfo.FindSystemTimeZoneById(dto.TimeZone); }
            catch (TimeZoneNotFoundException) { return "Choose a recognized time zone."; }
            catch (InvalidTimeZoneException) { return "Choose a recognized time zone."; }
            return null;
        }

        private static string? ValidateRubric(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                using var document = JsonDocument.Parse(json);
                return document.RootElement.ValueKind == JsonValueKind.Array ? null : "Rubric criteria must be a JSON array.";
            }
            catch (JsonException) { return "Rubric criteria must be valid JSON."; }
        }

        private async Task NotifyEnrolledAsync(Guid courseId, string type, string title, string message, string url, CancellationToken ct)
        {
            var learnerIds = await _context.CourseEnrollments.Where(e => e.CourseId == courseId && e.Status == "Active")
                .Select(e => e.LearnerId).Distinct().ToListAsync(ct);
            foreach (var learnerId in learnerIds)
            {
                try { await _notificationService.SendNotificationAsync(learnerId, type, title, message, url, ct); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch { }
            }
        }
    }
}
