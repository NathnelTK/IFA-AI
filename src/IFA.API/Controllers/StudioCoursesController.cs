using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using IFA.Application.Common.Interfaces;
using IFA.Application.Common.Models;
using IFA.Application.Common.Services;
using IFA.Domain.Entities;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IFA.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/studio/courses")]
    public sealed partial class StudioCoursesController : BaseApiController
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IApplicationDbContext _db;
        private readonly IPlatformAuthorizationService _authorization;
        private readonly IAuthoringWorkspaceService _workspaces;
        private readonly FeatureOptions _features;
        private readonly TimeProvider _timeProvider;
        private readonly ICourseReleaseService _releases;

        public StudioCoursesController(
            IApplicationDbContext db,
            IPlatformAuthorizationService authorization,
            IAuthoringWorkspaceService workspaces,
            IOptions<FeatureOptions> features,
            TimeProvider timeProvider,
            ICourseReleaseService releases)
        {
            _db = db;
            _authorization = authorization;
            _workspaces = workspaces;
            _features = features.Value;
            _timeProvider = timeProvider;
            _releases = releases;
        }

        public sealed class CreateDraftRequest
        {
            [Required, StringLength(16)]
            public string Mode { get; set; } = "Manual";
            public Guid? WorkspaceId { get; set; }
            [StringLength(200)]
            public string Title { get; set; } = string.Empty;
            [StringLength(5000)]
            public string Description { get; set; } = string.Empty;
            [StringLength(8000)]
            public string Prompt { get; set; } = string.Empty;
            [StringLength(100)]
            public string Category { get; set; } = "General";
            [StringLength(300)]
            public string TargetAudience { get; set; } = "All Learners";
            [MaxLength(30)]
            public List<string> LearningObjectives { get; set; } = [];
            [StringLength(100)]
            public string? TemplateId { get; set; }
            [MaxLength(20)]
            public List<string> SourceUrls { get; set; } = [];
            [MaxLength(30)]
            public List<MaterialModuleRequest> MaterialModules { get; set; } = [];
        }

        public sealed class MaterialModuleRequest
        {
            [Required, StringLength(64)]
            public string ModuleKey { get; set; } = string.Empty;
            [Required, StringLength(200)]
            public string Title { get; set; } = string.Empty;
            [MaxLength(20)]
            public List<string> SourceUrls { get; set; } = [];
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] Guid? workspaceId, CancellationToken cancellationToken)
        {
            if (!_features.UnifiedAuthoring) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            if (workspaceId.HasValue && !await _authorization.CanManageWorkspaceAsync(accountId, workspaceId.Value, cancellationToken))
            {
                return Forbid();
            }
            var courseIds = await _authorization.GetManageableCourseIdsAsync(accountId, cancellationToken);
            var projects = await _db.AuthoringProjects
                .Where(project => courseIds.Contains(project.CourseId)
                    && (!workspaceId.HasValue || project.WorkspaceId == workspaceId.Value))
                .Join(_db.Courses,
                    project => project.CourseId,
                    course => course.Id,
                    (project, course) => new
                    {
                        project.Id,
                        project.WorkspaceId,
                        project.CourseId,
                        project.Mode,
                        project.State,
                        project.UpdatedAt,
                        project.Version,
                        course.Title,
                        course.Description,
                        course.Slug,
                        course.CoverImageUrl,
                        course.IsPublished
                    })
                .OrderByDescending(item => item.UpdatedAt)
                .ToListAsync(cancellationToken);
            return Ok(projects);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        {
            if (!_features.UnifiedAuthoring) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var projectId = await ResolveProjectIdAsync(id, cancellationToken);
            if (!projectId.HasValue) return NotFound();
            var project = await _db.AuthoringProjects.SingleAsync(candidate => candidate.Id == projectId.Value, cancellationToken);
            if (!await _authorization.CanManageCourseAsync(accountId, project.CourseId, cancellationToken)) return Forbid();
            var course = await _db.Courses
                .Include(candidate => candidate.Modules.OrderBy(module => module.ModuleNumber))
                .ThenInclude(module => module.Lessons.OrderBy(lesson => lesson.LessonNumber))
                .Include(candidate => candidate.Assignments)
                .SingleAsync(candidate => candidate.Id == project.CourseId, cancellationToken);
            var jobs = await _db.AuthoringJobs
                .Where(job => job.AuthoringProjectId == project.Id)
                .OrderByDescending(job => job.CreatedAt)
                .ToListAsync(cancellationToken);
            return Ok(new { project, course, jobs });
        }

        [HttpPost]
        [EnableRateLimiting("generation")]
        public async Task<IActionResult> Create(
            [FromBody] CreateDraftRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.UnifiedAuthoring) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            if (!await _authorization.HasAccountRoleAsync(accountId, AccountRoleKind.Educator, cancellationToken))
            {
                return Forbid();
            }
            if (!Enum.TryParse<AuthoringMode>(request.Mode, true, out var mode))
            {
                return BadRequest(new { message = "Authoring mode must be Manual, Prompt, Materials, Template, or Voice." });
            }
            var objectives = request.LearningObjectives
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (objectives.Any(value => value.Length > 500))
            {
                return BadRequest(new { message = "Learning objectives must be 500 characters or fewer." });
            }
            CourseTemplateDto? template = null;
            if (mode == AuthoringMode.Template)
            {
                template = CourseTemplateCatalog.GetAll().SingleOrDefault(candidate =>
                    candidate.Id.Equals(request.TemplateId, StringComparison.OrdinalIgnoreCase));
                if (template == null) return BadRequest(new { message = "The selected template does not exist." });
            }
            if (mode is AuthoringMode.Prompt or AuthoringMode.Voice && string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { message = "A generation prompt is required." });
            }
            var materialModules = request.MaterialModules
                .Where(module => !string.IsNullOrWhiteSpace(module.ModuleKey) && !string.IsNullOrWhiteSpace(module.Title))
                .Select(module => new MaterialModuleRequest
                {
                    ModuleKey = module.ModuleKey.Trim(),
                    Title = module.Title.Trim(),
                    SourceUrls = module.SourceUrls
                })
                .ToList();
            var moduleByUrl = materialModules
                .SelectMany(module => module.SourceUrls.Select(url => new { Url = url.Trim(), Module = module }))
                .Where(item => Uri.TryCreate(item.Url, UriKind.Absolute, out var uri)
                    && uri.Scheme is "http" or "https"
                    && string.IsNullOrEmpty(uri.UserInfo))
                .GroupBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Module, StringComparer.OrdinalIgnoreCase);
            var sourceUrls = request.SourceUrls.Concat(moduleByUrl.Keys)
                .Where(value => Uri.TryCreate(value, UriKind.Absolute, out var uri)
                    && uri.Scheme is "http" or "https"
                    && string.IsNullOrEmpty(uri.UserInfo))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Guid? workspaceId;
            try
            {
                workspaceId = await _workspaces.ResolveForCreationAsync(accountId, request.WorkspaceId, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            if (!workspaceId.HasValue)
            {
                return Conflict(new { message = "Unified authoring requires an active workspace." });
            }

            var accountName = await _db.Learners
                .Where(account => account.Id == accountId)
                .Select(account => account.Name)
                .SingleAsync(cancellationToken);
            var courseId = Guid.NewGuid();
            var now = UtcNow();
            var title = FirstValue(request.Title, template?.Title, mode == AuthoringMode.Manual ? "Untitled course" : request.Prompt);
            title = title[..Math.Min(title.Length, 200)];
            var description = FirstValue(request.Description, template?.Description, request.Prompt);
            description = description[..Math.Min(description.Length, 2000)];
            var category = FirstValue(request.Category, template?.Category, "General");
            var audience = FirstValue(request.TargetAudience, template?.TargetAudience, "All Learners");
            if (objectives.Count == 0 && template != null) objectives = template.LearningObjectives.ToList();
            var course = new Course
            {
                Id = courseId,
                WorkspaceId = workspaceId,
                CreatorLearnerId = accountId,
                CreatedByAccountId = accountId,
                UpdatedByAccountId = accountId,
                Title = title,
                Slug = Slug(title, courseId),
                Description = description,
                Overview = description,
                Category = category,
                TargetAudience = audience,
                LearningObjectives = objectives,
                Level = template?.Level ?? "Beginner",
                CompletionRule = template?.CompletionRule ?? "Complete every required lesson and pass required assessments.",
                RequirePassingQuizzes = template?.RequirePassingQuizzes ?? true,
                AccentColor = template?.AccentColor ?? "#1B3D2F",
                ThemeMode = template?.ThemeMode ?? "pine",
                ProviderName = accountName,
                Visibility = "Private",
                IsPublished = false,
                IsPublic = false,
                ShareCode = Guid.NewGuid().ToString("N").ToUpperInvariant(),
                ExternalMaterials = sourceUrls.Count > 0 ? string.Join(';', sourceUrls) : null,
                CreatedAt = now,
                UpdatedAt = now
            };

            if (template != null)
            {
                MaterializeTemplate(course, template, now);
            }
            else if (mode == AuthoringMode.Manual)
            {
                AddInitialManualModule(course, now);
            }

            var projectId = Guid.NewGuid();
            var initialState = mode switch
            {
                AuthoringMode.Manual or AuthoringMode.Template => AuthoringProjectState.Reviewing,
                AuthoringMode.Materials => AuthoringProjectState.Draft,
                _ => AuthoringProjectState.Generating
            };
            var project = new AuthoringProject
            {
                Id = projectId,
                WorkspaceId = workspaceId.Value,
                CourseId = course.Id,
                CreatedByAccountId = accountId,
                LegacyEducatorProjectId = projectId,
                Mode = mode,
                State = initialState,
                Title = course.Title,
                CreatedAt = now,
                UpdatedAt = now
            };
            var legacyProject = new EducatorProject
            {
                Id = projectId,
                EducatorLearnerId = accountId,
                WorkspaceId = workspaceId,
                CourseId = course.Id,
                Title = course.Title,
                Description = course.Description,
                Status = LegacyStatus(initialState),
                CreatedAt = now,
                UpdatedAt = now
            };
            var generationRequest = new CourseGenerationRequest
            {
                AuthoringProjectId = project.Id,
                Prompt = request.Prompt.Trim(),
                Category = category,
                TargetAudience = audience,
                LearningObjectivesJson = JsonSerializer.Serialize(objectives, JsonOptions),
                ConstraintsJson = JsonSerializer.Serialize(new
                {
                    templateSnapshot = template,
                    requestedTitle = request.Title,
                    requestedDescription = request.Description,
                    materialModules = materialModules.Select(module => new
                    {
                        module.ModuleKey,
                        module.Title,
                        sourceCount = module.SourceUrls.Count
                    })
                }, JsonOptions),
                TemplateId = template?.Id,
                TemplateVersion = template == null ? null : 1,
                CreatedAt = now,
                UpdatedAt = now
            };

            await using var transaction = await CourseAccess.BeginMutationAsync(_db, cancellationToken);
            _db.Add(course);
            _db.Add(project);
            _db.Add(legacyProject);
            _db.Add(new LegacyProjectAlias { LegacyProjectId = legacyProject.Id, AuthoringProjectId = project.Id, CreatedAt = now });
            _db.Add(generationRequest);
            _db.Add(new ProductActivityEvent
            {
                EventType = "studio.course_draft.created",
                ActorAccountId = accountId,
                WorkspaceId = workspaceId,
                CourseId = course.Id,
                CorrelationId = HttpContext.TraceIdentifier,
                MetadataJson = JsonSerializer.Serialize(new { Mode = mode.ToString(), ProjectId = project.Id }),
                OccurredAt = now
            });
            if (template != null)
            {
                var existingVersion = await _db.CourseTemplateVersions.AnyAsync(candidate =>
                    candidate.TemplateKey == template.Id && candidate.Version == 1,
                    cancellationToken);
                if (!existingVersion)
                {
                    _db.Add(new CourseTemplateVersion
                    {
                        TemplateKey = template.Id,
                        Version = 1,
                        Name = template.Title,
                        Category = template.Category,
                        DefinitionJson = JsonSerializer.Serialize(template, JsonOptions),
                        CreatedAt = now
                    });
                }
            }

            foreach (var url in sourceUrls)
            {
                var sourceId = Guid.NewGuid();
                _db.Add(new ProjectSource
                {
                    Id = sourceId,
                    ProjectId = legacyProject.Id,
                    Url = url,
                    Type = InferSourceType(url),
                    Title = new Uri(url).Host,
                    Status = "Pending",
                    CreatedAt = now
                });
                _db.Add(new AuthoringSource
                {
                    Id = sourceId,
                    AuthoringProjectId = project.Id,
                    LegacyProjectSourceId = sourceId,
                    Kind = AuthoringSourceKind.Url,
                    Locator = url,
                    Status = "Pending",
                    MetadataJson = moduleByUrl.TryGetValue(url, out var module)
                        ? JsonSerializer.Serialize(new { module.ModuleKey, module.Title }, JsonOptions)
                        : "{}",
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            if (mode is AuthoringMode.Prompt or AuthoringMode.Voice)
            {
                _db.Add(new AuthoringJob
                {
                    AuthoringProjectId = project.Id,
                    Kind = mode == AuthoringMode.Materials ? "AnalyzeSources" : "GenerateCourseDraft",
                    State = AuthoringJobState.Queued,
                    PayloadJson = JsonSerializer.Serialize(new { generationRequestId = generationRequest.Id }, JsonOptions),
                    AvailableAt = now,
                    ActorAccountId = accountId,
                    CorrelationId = HttpContext.TraceIdentifier,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            if (transaction != null) await transaction.CommitAsync(cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = project.Id }, new
            {
                project.Id,
                project.CourseId,
                project.WorkspaceId,
                project.Mode,
                project.State,
                course.Title,
                course.Slug
            });
        }

        [HttpPost("{id:guid}/approve")]
        [EnableRateLimiting("generation")]
        public async Task<IActionResult> ApproveDraft(Guid id, CancellationToken cancellationToken)
        {
            if (!_features.UnifiedAuthoring) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var projectId = await ResolveProjectIdAsync(id, cancellationToken);
            if (!projectId.HasValue) return NotFound();
            var project = await _db.AuthoringProjects.SingleAsync(candidate => candidate.Id == projectId.Value, cancellationToken);
            if (!await _authorization.CanManageCourseAsync(accountId, project.CourseId, cancellationToken)) return Forbid();
            if (project.State is not (AuthoringProjectState.AwaitingApproval or AuthoringProjectState.Reviewing))
            {
                return Conflict(new { message = "This draft is not awaiting generation approval." });
            }
            var course = await _db.Courses
                .Include(candidate => candidate.Modules)
                .ThenInclude(module => module.Lessons)
                .Include(candidate => candidate.Modules)
                .ThenInclude(module => module.Quizzes)
                .SingleAsync(candidate => candidate.Id == project.CourseId, cancellationToken);
            var modules = course.Modules.OrderBy(module => module.ModuleNumber).ToList();
            if (modules.Count == 0) return Conflict(new { message = "Add or generate a course outline before approval." });
            if (modules.Any(module => module.Lessons.Any() || module.Quizzes.Any()))
            {
                project.State = AuthoringProjectState.Reviewing;
                project.UpdatedAt = UtcNow();
                project.Version++;
                await _db.SaveChangesAsync(cancellationToken);
                return Ok(new { project.Id, project.State });
            }
            if (await _db.GenerationJobs.AnyAsync(job =>
                job.CourseId == course.Id && (job.Status == GenerationJob.QueuedStatus || job.Status == GenerationJob.RunningStatus),
                cancellationToken))
            {
                return Conflict(new { message = "Course generation is already active." });
            }

            var now = UtcNow();
            var parent = new AuthoringJob
            {
                AuthoringProjectId = project.Id,
                Kind = "GenerateModules",
                State = AuthoringJobState.Waiting,
                ProgressPercent = 0,
                ActorAccountId = accountId,
                CorrelationId = HttpContext.TraceIdentifier,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Add(parent);
            var legacyProjectId = project.LegacyEducatorProjectId ?? project.Id;
            foreach (var module in modules)
            {
                _db.Add(new GenerationJob
                {
                    ProjectId = legacyProjectId,
                    AuthoringJobId = parent.Id,
                    CourseId = course.Id,
                    ModuleIndex = module.ModuleNumber - 1,
                    ModuleTitle = module.Title,
                    Kind = GenerationJob.JitModuleKind,
                    RequestJson = JsonSerializer.Serialize(new QueuedModuleGenerationPayload
                    {
                        ModuleId = module.Id,
                        LearnerId = accountId
                    }, JsonOptions),
                    Status = GenerationJob.QueuedStatus,
                    CreatedAt = now,
                    AvailableAt = now,
                    CorrelationId = HttpContext.TraceIdentifier,
                    CausationId = parent.Id.ToString(),
                    ActorLearnerId = accountId
                });
            }
            project.State = AuthoringProjectState.Generating;
            project.UpdatedAt = now;
            project.Version++;
            var legacyProject = await _db.EducatorProjects.SingleAsync(candidate => candidate.Id == legacyProjectId, cancellationToken);
            legacyProject.Status = "Generating";
            legacyProject.UpdatedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            return Accepted(new { project.Id, project.State, jobId = parent.Id, modules = modules.Count });
        }

        [HttpPost("{id:guid}/analyze")]
        [EnableRateLimiting("generation")]
        public async Task<IActionResult> Analyze(Guid id, CancellationToken cancellationToken)
        {
            if (!_features.UnifiedAuthoring) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var projectId = await ResolveProjectIdAsync(id, cancellationToken);
            if (!projectId.HasValue) return NotFound();
            var project = await _db.AuthoringProjects.SingleAsync(candidate => candidate.Id == projectId.Value, cancellationToken);
            if (!await _authorization.CanManageCourseAsync(accountId, project.CourseId, cancellationToken)) return Forbid();
            var sources = await _db.AuthoringSources
                .Where(source => source.AuthoringProjectId == project.Id)
                .ToListAsync(cancellationToken);
            if (sources.Count == 0 || sources.Any(source => source.Status != "Extracted"))
            {
                return Conflict(new { message = "All included sources must finish extraction before analysis." });
            }
            if (await _db.AuthoringJobs.AnyAsync(job =>
                job.AuthoringProjectId == project.Id
                && (job.State == AuthoringJobState.Queued || job.State == AuthoringJobState.Running),
                cancellationToken))
            {
                return Conflict(new { message = "An authoring job is already active." });
            }

            var now = UtcNow();
            var job = new AuthoringJob
            {
                AuthoringProjectId = project.Id,
                Kind = "AnalyzeSources",
                State = AuthoringJobState.Queued,
                AvailableAt = now,
                ActorAccountId = accountId,
                CorrelationId = HttpContext.TraceIdentifier,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Add(job);
            project.State = AuthoringProjectState.Analyzing;
            project.UpdatedAt = now;
            project.Version++;
            await _db.SaveChangesAsync(cancellationToken);
            return Accepted(new { project.Id, project.State, jobId = job.Id });
        }

        [HttpPost("{id:guid}/retry")]
        [EnableRateLimiting("generation")]
        public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken)
        {
            if (!_features.UnifiedAuthoring) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var projectId = await ResolveProjectIdAsync(id, cancellationToken);
            if (!projectId.HasValue) return NotFound();
            var project = await _db.AuthoringProjects.SingleAsync(candidate => candidate.Id == projectId.Value, cancellationToken);
            if (!await _authorization.CanManageCourseAsync(accountId, project.CourseId, cancellationToken)) return Forbid();
            var job = await _db.AuthoringJobs
                .Where(candidate => candidate.AuthoringProjectId == project.Id && candidate.State == AuthoringJobState.Failed)
                .OrderByDescending(candidate => candidate.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (job == null) return Conflict(new { message = "There is no failed authoring job to retry." });
            var now = UtcNow();
            job.State = AuthoringJobState.Queued;
            job.AvailableAt = now;
            job.ErrorCode = null;
            job.ErrorDetail = null;
            job.CompletedAt = null;
            job.UpdatedAt = now;
            job.LeaseOwner = null;
            job.LeaseToken = null;
            job.LeaseExpiresAt = null;
            project.State = job.Kind == "AnalyzeSources" ? AuthoringProjectState.Analyzing : AuthoringProjectState.Generating;
            project.FailureCode = null;
            project.FailureDetail = null;
            project.UpdatedAt = now;
            project.Version++;
            await _db.SaveChangesAsync(cancellationToken);
            return Accepted(new { project.Id, project.State, jobId = job.Id });
        }

        [HttpPost("{id:guid}/publish")]
        public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
        {
            if (!_features.UnifiedAuthoring || !_features.ImmutableReleases) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var projectId = await ResolveProjectIdAsync(id, cancellationToken);
            if (!projectId.HasValue) return NotFound();
            var project = await _db.AuthoringProjects.SingleAsync(candidate => candidate.Id == projectId.Value, cancellationToken);
            if (!await _authorization.CanManageCourseAsync(accountId, project.CourseId, cancellationToken)) return Forbid();
            if (project.State is not (AuthoringProjectState.Reviewing or AuthoringProjectState.PublishReady or AuthoringProjectState.Published))
            {
                return Conflict(new { message = "Finish course generation and review before publishing a release." });
            }
            try
            {
                var release = await _releases.PublishAsync(project.CourseId, accountId, cancellationToken);
                return Ok(new
                {
                    release.Id,
                    release.CourseId,
                    release.Version,
                    release.ContentHash,
                    release.PublishedAt
                });
            }
            catch (CourseReleaseValidationException ex)
            {
                return BadRequest(new { message = ex.Message, errors = ex.Errors });
            }
        }

        [HttpPost("{id:guid}/sources/assets/{assetId:guid}")]
        public async Task<IActionResult> AttachAssetSource(
            Guid id,
            Guid assetId,
            [FromQuery, StringLength(64)] string? moduleKey,
            [FromQuery, StringLength(200)] string? moduleTitle,
            CancellationToken cancellationToken)
        {
            if (!_features.UnifiedAuthoring || !_features.AssetUploads) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var projectId = await ResolveProjectIdAsync(id, cancellationToken);
            if (!projectId.HasValue) return NotFound();
            var project = await _db.AuthoringProjects.SingleAsync(candidate => candidate.Id == projectId.Value, cancellationToken);
            if (!await _authorization.CanManageCourseAsync(accountId, project.CourseId, cancellationToken)) return Forbid();
            var asset = await _db.Assets.SingleOrDefaultAsync(candidate => candidate.Id == assetId, cancellationToken);
            if (asset == null
                || asset.WorkspaceId != project.WorkspaceId
                || asset.Purpose != "CourseMaterial"
                || asset.Status != AssetStatus.Clean)
            {
                return Conflict(new { message = "Only a clean course material from this workspace can be attached." });
            }
            if (await _db.AuthoringSources.AnyAsync(source =>
                source.AuthoringProjectId == project.Id && source.AssetId == asset.Id,
                cancellationToken))
            {
                return NoContent();
            }

            var now = UtcNow();
            var derivative = await _db.AssetDerivatives.SingleOrDefaultAsync(candidate =>
                candidate.AssetId == asset.Id && candidate.Kind == "ExtractedText",
                cancellationToken);
            _db.Add(new AuthoringSource
            {
                AuthoringProjectId = project.Id,
                Kind = AuthoringSourceKind.Asset,
                AssetId = asset.Id,
                Locator = $"asset:{asset.Id}",
                Status = derivative == null ? "Pending" : "Extracted",
                ExtractedTextReference = derivative?.StorageKey,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    asset.OriginalFileName,
                    asset.ContentType,
                    asset.SizeBytes,
                    moduleKey,
                    moduleTitle
                }),
                CreatedAt = now,
                UpdatedAt = now
            });
            _db.Add(new AssetReference
            {
                AssetId = asset.Id,
                EntityType = nameof(AuthoringProject),
                EntityId = project.Id,
                Purpose = $"Source:{asset.Id}",
                CreatedByAccountId = accountId,
                CreatedAt = now
            });
            if (derivative == null
                && !await _db.AssetProcessingJobs.AnyAsync(job =>
                    job.AssetId == asset.Id && job.Kind == "Extract" && (job.State == "Queued" || job.State == "Running"),
                    cancellationToken))
            {
                _db.Add(new AssetProcessingJob
                {
                    AssetId = asset.Id,
                    Kind = "Extract",
                    State = "Queued",
                    AvailableAt = now,
                    CorrelationId = HttpContext.TraceIdentifier,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            project.UpdatedAt = now;
            project.Version++;
            await _db.SaveChangesAsync(cancellationToken);
            return Accepted(new { project.Id, assetId = asset.Id, status = derivative == null ? "Extracting" : "Extracted" });
        }

        private async Task<Guid?> ResolveProjectIdAsync(Guid id, CancellationToken cancellationToken)
        {
            if (await _db.AuthoringProjects.AnyAsync(project => project.Id == id, cancellationToken)) return id;
            return await _db.LegacyProjectAliases
                .Where(alias => alias.LegacyProjectId == id)
                .Select(alias => (Guid?)alias.AuthoringProjectId)
                .SingleOrDefaultAsync(cancellationToken);
        }

        private static void MaterializeTemplate(Course course, CourseTemplateDto template, DateTime now)
        {
            var moduleNumber = 1;
            foreach (var moduleTemplate in template.Modules)
            {
                var module = new Module
                {
                    CourseId = course.Id,
                    ModuleNumber = moduleNumber++,
                    Title = moduleTemplate.Title,
                    Summary = moduleTemplate.Summary,
                    EstimatedHours = moduleTemplate.EstimatedHours,
                    GenerationStatus = ModuleGenerationStatus.Ready,
                    GeneratedAt = now
                };
                var lessonNumber = 1;
                foreach (var lessonTemplate in moduleTemplate.Lessons)
                {
                    module.Lessons.Add(new Lesson
                    {
                        ModuleId = module.Id,
                        LessonNumber = lessonNumber++,
                        Title = lessonTemplate.Title,
                        Summary = lessonTemplate.Summary,
                        ContentMarkdown = lessonTemplate.ContentMarkdown,
                        ReadingTimeMinutes = lessonTemplate.ReadingTimeMinutes,
                        CreatedAt = now
                    });
                }
                course.Modules.Add(module);
            }
            if (!string.IsNullOrWhiteSpace(template.SampleAssignmentTitle))
            {
                course.Assignments.Add(new Assignment
                {
                    CourseId = course.Id,
                    Title = template.SampleAssignmentTitle,
                    InstructionsMarkdown = template.SampleAssignmentInstructions,
                    CreatedAt = now
                });
            }
        }

        private static void AddInitialManualModule(Course course, DateTime now)
        {
            var module = new Module
            {
                CourseId = course.Id,
                ModuleNumber = 1,
                Title = "Introduction",
                Summary = "Introduce the course goals and learning path.",
                EstimatedHours = 1,
                GenerationStatus = ModuleGenerationStatus.Ready,
                GeneratedAt = now
            };
            module.Lessons.Add(new Lesson
            {
                ModuleId = module.Id,
                LessonNumber = 1,
                Title = "Welcome and course objectives",
                ContentMarkdown = string.Empty,
                ReadingTimeMinutes = 10,
                CreatedAt = now
            });
            course.Modules.Add(module);
        }

        private static string FirstValue(params string?[] values) =>
            values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

        private static string Slug(string title, Guid id)
        {
            var value = SlugCharacters().Replace(title.Trim().ToLowerInvariant(), "-").Trim('-');
            if (string.IsNullOrWhiteSpace(value)) value = "course";
            value = value[..Math.Min(value.Length, 100)];
            return $"{value}-{id:N}"[..Math.Min(value.Length + 9, 120)];
        }

        private static string InferSourceType(string url)
        {
            var path = new Uri(url).AbsolutePath.ToLowerInvariant();
            if (path.EndsWith(".pdf")) return "Pdf";
            if (path.EndsWith(".ppt") || path.EndsWith(".pptx")) return "Ppt";
            if (url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
                || url.Contains("youtu.be", StringComparison.OrdinalIgnoreCase)) return "Video";
            return "Website";
        }

        private static string LegacyStatus(AuthoringProjectState state) => state switch
        {
            AuthoringProjectState.Analyzing => "Analyzing",
            AuthoringProjectState.Generating => "Generating",
            AuthoringProjectState.AwaitingApproval => "BlueprintReady",
            AuthoringProjectState.Reviewing or AuthoringProjectState.PublishReady => "ReadyForReview",
            AuthoringProjectState.Published => "Published",
            AuthoringProjectState.Failed => "GenerationFailed",
            _ => "Draft"
        };

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

        [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
        private static partial Regex SlugCharacters();
    }
}
