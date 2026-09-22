using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Courses.DTOs;

namespace IFA.Application.Common.Interfaces
{
    public interface IFineTunedCourseBuilderClient
    {
        Task<GeneratedModuleResult> BuildModuleAsync(
            ModuleSpecificationDto spec,
            CancellationToken cancellationToken = default);
    }
}
