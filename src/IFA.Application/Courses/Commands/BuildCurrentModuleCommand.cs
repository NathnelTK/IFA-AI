using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Application.Courses.DTOs;

namespace IFA.Application.Courses.Commands
{
    public class BuildCurrentModuleCommand
    {
        public ModuleSpecificationDto Specification { get; set; } = new();
    }

    public class BuildCurrentModuleCommandHandler
    {
        private readonly IFineTunedCourseBuilderClient _builderClient;

        public BuildCurrentModuleCommandHandler(IFineTunedCourseBuilderClient builderClient)
        {
            _builderClient = builderClient;
        }

        public async Task<GeneratedModuleResult> HandleAsync(
            BuildCurrentModuleCommand command,
            CancellationToken cancellationToken = default)
        {
            return await _builderClient.BuildModuleAsync(command.Specification, cancellationToken);
        }
    }
}
