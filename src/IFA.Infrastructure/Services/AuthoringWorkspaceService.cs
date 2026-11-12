using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IFA.Infrastructure.Services
{
    public sealed class AuthoringWorkspaceService : IAuthoringWorkspaceService
    {
        private readonly ApplicationDbContext _db;
        private readonly FeatureOptions _features;

        public AuthoringWorkspaceService(ApplicationDbContext db, IOptions<FeatureOptions> features)
        {
            _db = db;
            _features = features.Value;
        }

        public async Task<Guid?> ResolveForCreationAsync(
            Guid accountId,
            Guid? requestedWorkspaceId,
            CancellationToken cancellationToken = default)
        {
            if (!_features.Workspaces)
            {
                return null;
            }

            var candidates = await _db.WorkspaceMemberships
                .Where(membership => membership.AccountId == accountId
                    && membership.State == WorkspaceMembershipState.Active
                    && (membership.Role == WorkspaceMembershipRole.Owner || membership.Role == WorkspaceMembershipRole.Admin))
                .Join(_db.EducatorWorkspaces,
                    membership => membership.WorkspaceId,
                    workspace => workspace.Id,
                    (membership, workspace) => new { workspace.Id, workspace.Status, workspace.Type })
                .Where(item => item.Status == EducatorWorkspaceStatus.Verified)
                .ToListAsync(cancellationToken);
            if (requestedWorkspaceId.HasValue)
            {
                if (candidates.All(candidate => candidate.Id != requestedWorkspaceId.Value))
                {
                    throw new InvalidOperationException("The requested workspace is not an active verified authoring workspace for this account.");
                }
                return requestedWorkspaceId.Value;
            }

            if (candidates.Count != 1)
            {
                throw new InvalidOperationException(
                    candidates.Count == 0
                        ? "A verified educator workspace is required before creating courses."
                        : "Choose the workspace that will own this course.");
            }
            return candidates[0].Id;
        }
    }
}
