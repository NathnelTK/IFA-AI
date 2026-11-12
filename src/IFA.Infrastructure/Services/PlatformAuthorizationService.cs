using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IFA.Infrastructure.Services
{
    public sealed class PlatformAuthorizationService : IPlatformAuthorizationService
    {
        private readonly ApplicationDbContext _db;
        private readonly FeatureOptions _features;

        public PlatformAuthorizationService(
            ApplicationDbContext db,
            IOptions<FeatureOptions> features)
        {
            _db = db;
            _features = features.Value;
        }

        public async Task<bool> HasAccountRoleAsync(
            Guid accountId,
            AccountRoleKind role,
            CancellationToken cancellationToken = default)
        {
            var account = await _db.Learners
                .Where(candidate => candidate.Id == accountId && candidate.DisabledAt == null)
                .Select(candidate => new { candidate.Role })
                .SingleOrDefaultAsync(cancellationToken);
            if (account == null)
            {
                return false;
            }

            if (!_features.Workspaces)
            {
                var legacyRole = role switch
                {
                    AccountRoleKind.Learner => true,
                    AccountRoleKind.Educator => account.Role.Equals("Educator", StringComparison.OrdinalIgnoreCase)
                        || await _db.Courses.AnyAsync(course => course.CreatorLearnerId == accountId, cancellationToken)
                        || await _db.EducatorProjects.AnyAsync(project => project.EducatorLearnerId == accountId, cancellationToken),
                    _ => false
                };
                if (legacyRole) return true;
                return await _db.AccountRoleGrants.AnyAsync(grant =>
                    grant.AccountId == accountId
                    && grant.Role == role
                    && grant.IsActive
                    && grant.RevokedAt == null,
                    cancellationToken);
            }

            return await _db.AccountRoleGrants.AnyAsync(grant =>
                grant.AccountId == accountId
                && grant.Role == role
                && grant.IsActive
                && grant.RevokedAt == null,
                cancellationToken);
        }

        public async Task<bool> CanManageWorkspaceAsync(
            Guid accountId,
            Guid workspaceId,
            CancellationToken cancellationToken = default)
        {
            var isEducator = await HasAccountRoleAsync(accountId, AccountRoleKind.Educator, cancellationToken);
            var isPlatformAdmin = _features.Workspaces
                && await HasAccountRoleAsync(accountId, AccountRoleKind.PlatformAdmin, cancellationToken);
            if (!isEducator && !isPlatformAdmin)
            {
                return false;
            }
            if (!_features.Workspaces)
            {
                return await _db.EducatorWorkspaces.AnyAsync(
                    workspace => workspace.Id == workspaceId && workspace.CreatedByAccountId == accountId,
                    cancellationToken);
            }

            var workspace = await _db.EducatorWorkspaces
                .Where(candidate => candidate.Id == workspaceId)
                .Select(candidate => new { candidate.Type, candidate.Status })
                .SingleOrDefaultAsync(cancellationToken);
            if (workspace == null || workspace.Status != EducatorWorkspaceStatus.Verified)
            {
                return false;
            }
            if (workspace.Type == EducatorWorkspaceType.System)
            {
                return isPlatformAdmin;
            }
            if (!isEducator) return false;
            return await _db.WorkspaceMemberships.AnyAsync(membership =>
                    membership.WorkspaceId == workspaceId
                    && membership.AccountId == accountId
                    && membership.State == WorkspaceMembershipState.Active
                    && (membership.Role == WorkspaceMembershipRole.Owner || membership.Role == WorkspaceMembershipRole.Admin),
                cancellationToken);
        }

        public async Task<bool> CanManageCourseAsync(
            Guid accountId,
            Guid courseId,
            CancellationToken cancellationToken = default)
        {
            var course = await _db.Courses
                .Where(candidate => candidate.Id == courseId)
                .Select(candidate => new { candidate.CreatorLearnerId, candidate.WorkspaceId })
                .SingleOrDefaultAsync(cancellationToken);
            if (course == null)
            {
                return false;
            }
            if (!_features.Workspaces)
            {
                return course.CreatorLearnerId == accountId;
            }
            if (!course.WorkspaceId.HasValue)
            {
                return false;
            }

            var workspace = await _db.EducatorWorkspaces.SingleOrDefaultAsync(
                candidate => candidate.Id == course.WorkspaceId.Value,
                cancellationToken);
            if (workspace == null || workspace.Status != EducatorWorkspaceStatus.Verified)
            {
                return false;
            }
            if (workspace.Type == EducatorWorkspaceType.System)
            {
                return await HasAccountRoleAsync(accountId, AccountRoleKind.PlatformAdmin, cancellationToken);
            }
            if (!await HasAccountRoleAsync(accountId, AccountRoleKind.Educator, cancellationToken))
            {
                return false;
            }

            var membership = await _db.WorkspaceMemberships.SingleOrDefaultAsync(candidate =>
                candidate.WorkspaceId == course.WorkspaceId.Value
                && candidate.AccountId == accountId
                && candidate.State == WorkspaceMembershipState.Active,
                cancellationToken);
            if (membership == null)
            {
                return false;
            }
            if (membership.Role is WorkspaceMembershipRole.Owner or WorkspaceMembershipRole.Admin)
            {
                return true;
            }

            return membership.Role == WorkspaceMembershipRole.Instructor
                && await _db.CourseInstructorAssignments.AnyAsync(assignment =>
                    assignment.CourseId == courseId
                    && assignment.MembershipId == membership.Id
                    && assignment.IsActive,
                    cancellationToken);
        }

        public async Task<bool> CanManageProjectAsync(
            Guid accountId,
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            var project = await _db.EducatorProjects
                .Where(candidate => candidate.Id == projectId)
                .Select(candidate => new
                {
                    candidate.EducatorLearnerId,
                    candidate.WorkspaceId,
                    candidate.CourseId
                })
                .SingleOrDefaultAsync(cancellationToken);
            if (project == null)
            {
                return false;
            }
            if (!_features.Workspaces)
            {
                return project.EducatorLearnerId == accountId;
            }
            if (project.CourseId.HasValue
                && await CanManageCourseAsync(accountId, project.CourseId.Value, cancellationToken))
            {
                return true;
            }
            return project.WorkspaceId.HasValue
                && await CanManageWorkspaceAsync(accountId, project.WorkspaceId.Value, cancellationToken);
        }

        public async Task<IReadOnlyList<Guid>> GetManageableCourseIdsAsync(
            Guid accountId,
            CancellationToken cancellationToken = default)
        {
            if (!_features.Workspaces)
            {
                return await _db.Courses
                    .Where(course => course.CreatorLearnerId == accountId)
                    .Select(course => course.Id)
                    .ToListAsync(cancellationToken);
            }

            var memberships = await _db.WorkspaceMemberships
                .Where(membership => membership.AccountId == accountId
                    && membership.State == WorkspaceMembershipState.Active)
                .Select(membership => new { membership.Id, membership.WorkspaceId, membership.Role })
                .ToListAsync(cancellationToken);
            var administrativeWorkspaceIds = memberships
                .Where(membership => membership.Role is WorkspaceMembershipRole.Owner or WorkspaceMembershipRole.Admin)
                .Select(membership => membership.WorkspaceId)
                .ToArray();
            var instructorMembershipIds = memberships
                .Where(membership => membership.Role == WorkspaceMembershipRole.Instructor)
                .Select(membership => membership.Id)
                .ToArray();
            var workspaceCourses = await _db.Courses
                .Where(course => course.WorkspaceId.HasValue
                    && administrativeWorkspaceIds.Contains(course.WorkspaceId.Value))
                .Select(course => course.Id)
                .ToListAsync(cancellationToken);
            var assignedCourses = await _db.CourseInstructorAssignments
                .Where(assignment => assignment.IsActive
                    && instructorMembershipIds.Contains(assignment.MembershipId))
                .Select(assignment => assignment.CourseId)
                .ToListAsync(cancellationToken);
            return workspaceCourses.Concat(assignedCourses).Distinct().ToArray();
        }

        public async Task<IReadOnlyList<Guid>> GetManageableProjectIdsAsync(
            Guid accountId,
            CancellationToken cancellationToken = default)
        {
            if (!_features.Workspaces)
            {
                return await _db.EducatorProjects
                    .Where(project => project.EducatorLearnerId == accountId)
                    .Select(project => project.Id)
                    .ToListAsync(cancellationToken);
            }
            var courseIds = await GetManageableCourseIdsAsync(accountId, cancellationToken);
            var administrativeWorkspaceIds = await _db.WorkspaceMemberships
                .Where(membership => membership.AccountId == accountId
                    && membership.State == WorkspaceMembershipState.Active
                    && (membership.Role == WorkspaceMembershipRole.Owner || membership.Role == WorkspaceMembershipRole.Admin))
                .Select(membership => membership.WorkspaceId)
                .ToListAsync(cancellationToken);
            return await _db.EducatorProjects
                .Where(project => (project.CourseId.HasValue && courseIds.Contains(project.CourseId.Value))
                    || (project.WorkspaceId.HasValue && administrativeWorkspaceIds.Contains(project.WorkspaceId.Value)))
                .Select(project => project.Id)
                .ToListAsync(cancellationToken);
        }
    }
}
