using System.Text.Json;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ModulesController : BaseApiController
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IApplicationDbContext _context;
        private readonly IPlatformAuthorizationService _authorization;

        public ModulesController(IApplicationDbContext context, IPlatformAuthorizationService authorization)
        {
            _context = context;
            _authorization = authorization;
        }

        [HttpPost("{id:guid}/generate")]
        [EnableRateLimiting("generation")]
        public async Task<IActionResult> GenerateModule(Guid id, CancellationToken cancellationToken)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            if (!IsCurrentUserInRole("Educator"))
            {
                return StatusCode(403, new { message = "Only the owning educator can generate course content." });
            }

            await using var transaction = await CourseAccess.BeginMutationAsync(_context, cancellationToken);
            var module = await _context.Modules
                .Include(candidate => candidate.Course)
                .Include(candidate => candidate.Lessons)
                .Include(candidate => candidate.Quizzes)
                .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
            if (module == null)
            {
                return NotFound(new { message = "Module not found." });
            }
            if (module.Course == null || !await _authorization.CanManageCourseAsync(learnerId, module.CourseId, cancellationToken))
            {
                return StatusCode(403, new { message = "You do not own this course." });
            }
            if (module.Course.IsPublished)
            {
                return Conflict(new { message = "Published content cannot be mutated. Create a new draft release instead." });
            }
            if (module.GenerationStatus == ModuleGenerationStatus.Ready && module.Lessons.Count > 0)
            {
                return Ok(module);
            }
            if (module.Lessons.Count > 0 || module.Quizzes.Count > 0)
            {
                return Conflict(new { message = "This module already has content. Review it instead of appending generated content." });
            }

            var activeJob = await _context.GenerationJobs.FirstOrDefaultAsync(candidate =>
                candidate.CourseId == module.CourseId
                && candidate.ModuleIndex == module.ModuleNumber - 1
                && (candidate.Status == GenerationJob.QueuedStatus || candidate.Status == GenerationJob.RunningStatus),
                cancellationToken);
            if (activeJob != null)
            {
                return Accepted(new
                {
                    jobId = activeJob.Id,
                    projectId = activeJob.ProjectId,
                    status = activeJob.Status,
                    statusUrl = $"/api/educator/projects/{activeJob.ProjectId}/status"
                });
            }

            var project = await _context.EducatorProjects
                .Where(candidate => candidate.CourseId == module.CourseId)
                .OrderByDescending(candidate => candidate.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            var now = DateTime.UtcNow;
            if (project == null)
            {
                project = new EducatorProject
                {
                    EducatorLearnerId = learnerId,
                    CourseId = module.CourseId,
                    Title = module.Course.Title,
                    Description = module.Course.Description,
                    Status = "Generating",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.Add(project);
            }
            else
            {
                project.Status = "Generating";
                project.UpdatedAt = now;
            }

            module.GenerationStatus = ModuleGenerationStatus.Blueprint;
            var job = new GenerationJob
            {
                ProjectId = project.Id,
                CourseId = module.CourseId,
                ModuleIndex = module.ModuleNumber - 1,
                ModuleTitle = module.Title,
                Kind = GenerationJob.JitModuleKind,
                RequestJson = JsonSerializer.Serialize(new QueuedModuleGenerationPayload
                {
                    ModuleId = module.Id,
                    LearnerId = learnerId
                }, JsonOptions),
                Status = GenerationJob.QueuedStatus,
                CreatedAt = now,
                AvailableAt = now,
                CorrelationId = HttpContext.TraceIdentifier,
                CausationId = module.Id.ToString(),
                ActorLearnerId = learnerId
            };
            _context.Add(job);
            await _context.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return Accepted(new
            {
                jobId = job.Id,
                projectId = project.Id,
                status = job.Status,
                statusUrl = $"/api/educator/projects/{project.Id}/status"
            });
        }
    }
}
