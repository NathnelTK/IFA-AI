using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public interface IActivityService
    {
        Task RecordActivityAsync(Guid learnerId, string activityType, string title, string description, string metadataJson = "{}", CancellationToken ct = default);
        Task<List<LearningActivity>> GetRecentActivitiesAsync(Guid learnerId, int limit = 20, CancellationToken ct = default);
    }
}
