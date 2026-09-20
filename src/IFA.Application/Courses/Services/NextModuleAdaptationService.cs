using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Progress.Services;

namespace IFA.Application.Courses.Services
{
    public interface INextModuleAdaptationService
    {
        Task<List<string>> GenerateAdaptationConstraintsAsync(
            AssessmentResultDto assessment,
            int nextModuleNumber,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Adaptive Learning Engine: Next Module Adaptation Service.
    /// Determines pedagogical constraints, remedial focus areas, and pace
    /// adjustments to feed into Model 2 when materializing subsequent modules.
    /// </summary>
    public class NextModuleAdaptationService : INextModuleAdaptationService
    {
        public Task<List<string>> GenerateAdaptationConstraintsAsync(
            AssessmentResultDto assessment,
            int nextModuleNumber,
            CancellationToken cancellationToken = default)
        {
            var constraints = new List<string>();

            if (assessment.NewlyIdentifiedWeakAreas.Count > 0)
            {
                foreach (var weak in assessment.NewlyIdentifiedWeakAreas)
                {
                    constraints.Add($"Learner struggled with '{weak}' in prior quiz. Embed 1 review explanation and 2 targeted code exercises on {weak} before introducing new topics.");
                }
            }

            if (assessment.ScorePercentage >= 90)
            {
                constraints.Add("High competency detected (score >= 90%). Accelerate pace and include advanced architectural patterns and performance optimization tips.");
            }
            else if (assessment.ScorePercentage < 70)
            {
                constraints.Add("Remediation required (score < 70%). Reinforce fundamentals with simplified analogies and step-by-step diagnostic checks.");
            }
            else
            {
                constraints.Add("Standard pedagogical progression pace maintained.");
            }

            return Task.FromResult(constraints);
        }
    }
}
