namespace IFA.Application.Courses.Commands
{
    public class ShareCourseCommand
    {
        public Guid CourseId { get; set; }
        public Guid SharedByLearnerId { get; set; }
        public string? Message { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class ShareCourseResult
    {
        public required string ShareUrl { get; set; }
        public required string ShareCode { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}