using System;

namespace IFA.Domain.Entities
{
    public class AssessmentResponse
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AssessmentId { get; set; }
        public Guid QuestionId { get; set; }
        public int SelectedOptionIndex { get; set; }
        public bool IsCorrect { get; set; }
        public string Feedback { get; set; } = string.Empty;

        // Navigation properties
        public Assessment? Assessment { get; set; }
        public Question? Question { get; set; }
    }
}
