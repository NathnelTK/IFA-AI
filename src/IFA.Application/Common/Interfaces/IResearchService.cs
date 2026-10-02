using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public interface IResearchService
    {
        Task<ResearchPackage> ConductResearchAsync(string topic, Guid? learnerId = null, Guid? courseId = null, CancellationToken ct = default);
        Task<ResearchPackage?> GetResearchForCourseAsync(Guid courseId, CancellationToken ct = default);
    }
}
