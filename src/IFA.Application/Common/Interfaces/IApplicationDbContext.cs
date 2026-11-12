using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        IQueryable<Learner> Learners { get; }
        IQueryable<Course> Courses { get; }
        IQueryable<Module> Modules { get; }
        IQueryable<Lesson> Lessons { get; }
        IQueryable<Quiz> Quizzes { get; }
        IQueryable<Question> Questions { get; }
        IQueryable<SkillMetric> SkillMetrics { get; }
        IQueryable<CourseEnrollment> CourseEnrollments { get; }
        IQueryable<CourseShareInvite> CourseShareInvites { get; }
        IQueryable<QuizAttempt> QuizAttempts { get; }
        IQueryable<QuizAnswer> QuizAnswers { get; }
        IQueryable<LessonProgress> LessonProgress { get; }
        IQueryable<LearnerProfile> LearnerProfiles { get; }
        IQueryable<LearnerState> LearnerStates { get; }
        IQueryable<LearnerSettings> LearnerSettings { get; }
        IQueryable<ResearchPackage> ResearchPackages { get; }
        IQueryable<ChatSession> ChatSessions { get; }
        IQueryable<ChatMessage> ChatMessages { get; }
        IQueryable<Notification> Notifications { get; }
        IQueryable<Assessment> Assessments { get; }
        IQueryable<AssessmentResponse> AssessmentResponses { get; }
        IQueryable<LearningActivity> Activities { get; }
        IQueryable<EducatorProject> EducatorProjects { get; }
        IQueryable<ProjectSource> ProjectSources { get; }
        IQueryable<SourceConflict> SourceConflicts { get; }
        IQueryable<CourseBlueprint> CourseBlueprints { get; }
        IQueryable<GenerationJob> GenerationJobs { get; }
        IQueryable<Payment> Payments { get; }
        IQueryable<LiveSession> LiveSessions { get; }
        IQueryable<CourseDiscussion> Discussions { get; }
        IQueryable<Assignment> Assignments { get; }
        IQueryable<AssignmentSubmission> AssignmentSubmissions { get; }
        IQueryable<OutboxMessage> OutboxMessages { get; }
        IQueryable<AuditEvent> AuditEvents { get; }
        IQueryable<AccountRoleGrant> AccountRoleGrants { get; }
        IQueryable<AuthSession> AuthSessions { get; }
        IQueryable<LegacyTokenExchange> LegacyTokenExchanges { get; }
        IQueryable<AccountSecurityToken> AccountSecurityTokens { get; }
        IQueryable<AccountMfaCredential> AccountMfaCredentials { get; }
        IQueryable<AccountRecoveryCode> AccountRecoveryCodes { get; }
        IQueryable<OAuthAuthorizationCode> OAuthAuthorizationCodes { get; }
        IQueryable<InternalRequestReplay> InternalRequestReplays { get; }
        IQueryable<MfaLoginChallenge> MfaLoginChallenges { get; }
        IQueryable<EducatorWorkspace> EducatorWorkspaces { get; }
        IQueryable<WorkspaceMembership> WorkspaceMemberships { get; }
        IQueryable<WorkspaceInstructorProfile> WorkspaceInstructorProfiles { get; }
        IQueryable<WorkspaceInvitation> WorkspaceInvitations { get; }
        IQueryable<CourseInstructorAssignment> CourseInstructorAssignments { get; }
        IQueryable<EducatorApplication> EducatorApplications { get; }
        IQueryable<VerificationClaim> VerificationClaims { get; }
        IQueryable<WorkspaceEntitlement> WorkspaceEntitlements { get; }
        IQueryable<PublicSlugAlias> PublicSlugAliases { get; }
        IQueryable<AuthoringProject> AuthoringProjects { get; }
        IQueryable<CourseGenerationRequest> CourseGenerationRequests { get; }
        IQueryable<AuthoringSource> AuthoringSources { get; }
        IQueryable<AuthoringJob> AuthoringJobs { get; }
        IQueryable<LegacyProjectAlias> LegacyProjectAliases { get; }
        IQueryable<CourseTemplateVersion> CourseTemplateVersions { get; }
        IQueryable<Asset> Assets { get; }
        IQueryable<UploadSession> UploadSessions { get; }
        IQueryable<AssetDerivative> AssetDerivatives { get; }
        IQueryable<AssetReference> AssetReferences { get; }
        IQueryable<AssetProcessingJob> AssetProcessingJobs { get; }
        IQueryable<CourseRelease> CourseReleases { get; }
        IQueryable<ReleaseModule> ReleaseModules { get; }
        IQueryable<ReleaseLesson> ReleaseLessons { get; }
        IQueryable<ReleaseQuiz> ReleaseQuizzes { get; }
        IQueryable<ReleaseAssignment> ReleaseAssignments { get; }
        IQueryable<CourseCohort> CourseCohorts { get; }
        IQueryable<ReleaseItemProgress> ReleaseItemProgress { get; }
        IQueryable<ProgressMigrationRecord> ProgressMigrationRecords { get; }
        IQueryable<ReleaseQuizAttempt> ReleaseQuizAttempts { get; }
        IQueryable<EducatorPublicProfile> EducatorPublicProfiles { get; }
        IQueryable<OrganizationSite> OrganizationSites { get; }
        IQueryable<OrganizationSitePage> OrganizationSitePages { get; }
        IQueryable<CustomDomain> CustomDomains { get; }
        IQueryable<VerifiedExternalWebsite> VerifiedExternalWebsites { get; }
        IQueryable<WorkspaceFollow> WorkspaceFollows { get; }
        IQueryable<CourseAnnouncement> CourseAnnouncements { get; }
        IQueryable<CourseReview> CourseReviews { get; }
        IQueryable<CourseReviewReply> CourseReviewReplies { get; }
        IQueryable<CourseReviewReport> CourseReviewReports { get; }
        IQueryable<ModerationCase> ModerationCases { get; }
        IQueryable<CourseCompletionRecord> CourseCompletionRecords { get; }
        IQueryable<CourseCertificate> CourseCertificates { get; }
        IQueryable<CertificateRenderJob> CertificateRenderJobs { get; }
        IQueryable<CourseOffer> CourseOffers { get; }
        IQueryable<PaymentWebhookEvent> PaymentWebhookEvents { get; }
        IQueryable<ProductActivityEvent> ProductActivityEvents { get; }

        void Add<TEntity>(TEntity entity) where TEntity : class;
        void Remove<TEntity>(TEntity entity) where TEntity : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
    }
}
