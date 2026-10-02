using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public class QuizSubmissionRequest
    {
        public Guid QuizId { get; set; }
        public Dictionary<Guid, int> Answers { get; set; } = new Dictionary<Guid, int>(); // QuestionId -> SelectedOptionIndex
    }

    public class QuizEvaluationResult
    {
        public Guid AssessmentId { get; set; }
        public int ScorePercentage { get; set; }
        public bool IsPassed { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswersCount { get; set; }
        public List<QuestionFeedbackDto> QuestionFeedbacks { get; set; } = new List<QuestionFeedbackDto>();
        public List<string> MasteredSkills { get; set; } = new List<string>();
        public List<string> WeakSkillsToReinforce { get; set; } = new List<string>();
        public string AdaptiveNextAction { get; set; } = string.Empty;
    }

    public class QuestionFeedbackDto
    {
        public Guid QuestionId { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public int SelectedOptionIndex { get; set; }
        public int CorrectOptionIndex { get; set; }
        public bool IsCorrect { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public string TargetSkillName { get; set; } = string.Empty;
    }

    public interface IAdaptiveEngine
    {
        Task<QuizEvaluationResult> EvaluateAndAdaptAsync(Guid learnerId, QuizSubmissionRequest request, CancellationToken ct = default);
        Task<List<string>> GetAdaptiveConstraintsForNextModuleAsync(Guid learnerId, Guid courseId, CancellationToken ct = default);
    }
}
