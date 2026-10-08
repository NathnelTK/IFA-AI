using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IFA.Application.Common.Interfaces
{
    public class TutorMessageRequest
    {
        public Guid? CourseId { get; set; }
        public Guid? ModuleId { get; set; }
        public Guid? LessonId { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<IntakeMessageDto> History { get; set; } = new List<IntakeMessageDto>();
    }

    public class TutorResponse
    {
        public string ReplyMarkdown { get; set; } = string.Empty;
        public List<string> SuggestedFollowUps { get; set; } = new List<string>();
        public string? CodeSnippet { get; set; }
        public string? Language { get; set; }
    }

    public interface IAiTutorService
    {
        Task<TutorResponse> ChatAsync(Guid learnerId, TutorMessageRequest request, CancellationToken ct = default);
    }
}
