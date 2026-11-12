using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IFA.Infrastructure.Data
{
    /// <summary>
    /// EF Core implementation of the application persistence contract.
    /// Entity mapping details live in the IEntityTypeConfiguration classes
    /// discovered from this assembly.
    /// </summary>
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Learner> Learners => Set<Learner>();
        public DbSet<LearnerProfile> LearnerProfiles => Set<LearnerProfile>();
        public DbSet<LearnerState> LearnerStates => Set<LearnerState>();
        public DbSet<LearnerSettings> LearnerSettings => Set<LearnerSettings>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Module> Modules => Set<Module>();
        public DbSet<Lesson> Lessons => Set<Lesson>();
        public DbSet<Quiz> Quizzes => Set<Quiz>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<SkillMetric> SkillMetrics => Set<SkillMetric>();
        public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
        public DbSet<CourseShareInvite> CourseShareInvites => Set<CourseShareInvite>();
        public DbSet<ResearchPackage> ResearchPackages => Set<ResearchPackage>();
        public DbSet<ResearchSource> ResearchSources => Set<ResearchSource>();
        public DbSet<AcademicEvidence> AcademicEvidences => Set<AcademicEvidence>();
        public DbSet<PracticalResource> PracticalResources => Set<PracticalResource>();
        public DbSet<VideoResource> VideoResources => Set<VideoResource>();
        public DbSet<Assessment> Assessments => Set<Assessment>();
        public DbSet<AssessmentResponse> AssessmentResponses => Set<AssessmentResponse>();
        public DbSet<LearningActivity> Activities => Set<LearningActivity>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
        public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
        public DbSet<EducatorProject> EducatorProjects => Set<EducatorProject>();
        public DbSet<ProjectSource> ProjectSources => Set<ProjectSource>();
        public DbSet<SourceConflict> SourceConflicts => Set<SourceConflict>();
        public DbSet<CourseBlueprint> CourseBlueprints => Set<CourseBlueprint>();
        public DbSet<GenerationJob> GenerationJobs => Set<GenerationJob>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<LiveSession> LiveSessions => Set<LiveSession>();
        public DbSet<CourseDiscussion> Discussions => Set<CourseDiscussion>();
        public DbSet<Assignment> Assignments => Set<Assignment>();
        public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
        public DbSet<AccountRoleGrant> AccountRoleGrants => Set<AccountRoleGrant>();
        public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
        public DbSet<LegacyTokenExchange> LegacyTokenExchanges => Set<LegacyTokenExchange>();
        public DbSet<AccountSecurityToken> AccountSecurityTokens => Set<AccountSecurityToken>();
        public DbSet<AccountMfaCredential> AccountMfaCredentials => Set<AccountMfaCredential>();
        public DbSet<AccountRecoveryCode> AccountRecoveryCodes => Set<AccountRecoveryCode>();
        public DbSet<OAuthAuthorizationCode> OAuthAuthorizationCodes => Set<OAuthAuthorizationCode>();
        public DbSet<InternalRequestReplay> InternalRequestReplays => Set<InternalRequestReplay>();
        public DbSet<MfaLoginChallenge> MfaLoginChallenges => Set<MfaLoginChallenge>();
        public DbSet<EducatorWorkspace> EducatorWorkspaces => Set<EducatorWorkspace>();
        public DbSet<WorkspaceMembership> WorkspaceMemberships => Set<WorkspaceMembership>();
        public DbSet<WorkspaceInstructorProfile> WorkspaceInstructorProfiles => Set<WorkspaceInstructorProfile>();
        public DbSet<WorkspaceInvitation> WorkspaceInvitations => Set<WorkspaceInvitation>();
        public DbSet<CourseInstructorAssignment> CourseInstructorAssignments => Set<CourseInstructorAssignment>();
        public DbSet<EducatorApplication> EducatorApplications => Set<EducatorApplication>();
        public DbSet<VerificationClaim> VerificationClaims => Set<VerificationClaim>();
        public DbSet<WorkspaceEntitlement> WorkspaceEntitlements => Set<WorkspaceEntitlement>();
        public DbSet<PublicSlugAlias> PublicSlugAliases => Set<PublicSlugAlias>();
        public DbSet<AuthoringProject> AuthoringProjects => Set<AuthoringProject>();
        public DbSet<CourseGenerationRequest> CourseGenerationRequests => Set<CourseGenerationRequest>();
        public DbSet<AuthoringSource> AuthoringSources => Set<AuthoringSource>();
        public DbSet<AuthoringJob> AuthoringJobs => Set<AuthoringJob>();
        public DbSet<LegacyProjectAlias> LegacyProjectAliases => Set<LegacyProjectAlias>();
        public DbSet<CourseTemplateVersion> CourseTemplateVersions => Set<CourseTemplateVersion>();
        public DbSet<Asset> Assets => Set<Asset>();
        public DbSet<UploadSession> UploadSessions => Set<UploadSession>();
        public DbSet<AssetDerivative> AssetDerivatives => Set<AssetDerivative>();
        public DbSet<AssetReference> AssetReferences => Set<AssetReference>();
        public DbSet<AssetProcessingJob> AssetProcessingJobs => Set<AssetProcessingJob>();
        public DbSet<CourseRelease> CourseReleases => Set<CourseRelease>();
        public DbSet<ReleaseModule> ReleaseModules => Set<ReleaseModule>();
        public DbSet<ReleaseLesson> ReleaseLessons => Set<ReleaseLesson>();
        public DbSet<ReleaseQuiz> ReleaseQuizzes => Set<ReleaseQuiz>();
        public DbSet<ReleaseAssignment> ReleaseAssignments => Set<ReleaseAssignment>();
        public DbSet<CourseCohort> CourseCohorts => Set<CourseCohort>();
        public DbSet<ReleaseItemProgress> ReleaseItemProgress => Set<ReleaseItemProgress>();
        public DbSet<ProgressMigrationRecord> ProgressMigrationRecords => Set<ProgressMigrationRecord>();
        public DbSet<ReleaseQuizAttempt> ReleaseQuizAttempts => Set<ReleaseQuizAttempt>();
        public DbSet<EducatorPublicProfile> EducatorPublicProfiles => Set<EducatorPublicProfile>();
        public DbSet<OrganizationSite> OrganizationSites => Set<OrganizationSite>();
        public DbSet<OrganizationSitePage> OrganizationSitePages => Set<OrganizationSitePage>();
        public DbSet<CustomDomain> CustomDomains => Set<CustomDomain>();
        public DbSet<VerifiedExternalWebsite> VerifiedExternalWebsites => Set<VerifiedExternalWebsite>();
        public DbSet<WorkspaceFollow> WorkspaceFollows => Set<WorkspaceFollow>();
        public DbSet<CourseAnnouncement> CourseAnnouncements => Set<CourseAnnouncement>();
        public DbSet<CourseReview> CourseReviews => Set<CourseReview>();
        public DbSet<CourseReviewReply> CourseReviewReplies => Set<CourseReviewReply>();
        public DbSet<CourseReviewReport> CourseReviewReports => Set<CourseReviewReport>();
        public DbSet<ModerationCase> ModerationCases => Set<ModerationCase>();
        public DbSet<CourseCompletionRecord> CourseCompletionRecords => Set<CourseCompletionRecord>();
        public DbSet<CourseCertificate> CourseCertificates => Set<CourseCertificate>();
        public DbSet<CertificateRenderJob> CertificateRenderJobs => Set<CertificateRenderJob>();
        public DbSet<CourseOffer> CourseOffers => Set<CourseOffer>();
        public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();
        public DbSet<ProductActivityEvent> ProductActivityEvents => Set<ProductActivityEvent>();

        public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();

        // IApplicationDbContext now declares IQueryable<T>, and DbSet<T>
        // already IS an IQueryable<T> natively — so these explicit
        // implementations no longer downcast anything. They just forward
        // the same object the interface asked for, unmodified.
        IQueryable<Learner> IApplicationDbContext.Learners => Learners;
        IQueryable<Course> IApplicationDbContext.Courses => Courses;
        IQueryable<Module> IApplicationDbContext.Modules => Modules;
        IQueryable<Lesson> IApplicationDbContext.Lessons => Lessons;
        IQueryable<Quiz> IApplicationDbContext.Quizzes => Quizzes;
        IQueryable<Question> IApplicationDbContext.Questions => Questions;
        IQueryable<SkillMetric> IApplicationDbContext.SkillMetrics => SkillMetrics;
        IQueryable<CourseEnrollment> IApplicationDbContext.CourseEnrollments => CourseEnrollments;
        IQueryable<CourseShareInvite> IApplicationDbContext.CourseShareInvites => CourseShareInvites;
        IQueryable<LessonProgress> IApplicationDbContext.LessonProgress => LessonProgress;
        IQueryable<QuizAttempt> IApplicationDbContext.QuizAttempts => QuizAttempts;
        IQueryable<QuizAnswer> IApplicationDbContext.QuizAnswers => QuizAnswers;
        IQueryable<LearnerProfile> IApplicationDbContext.LearnerProfiles => LearnerProfiles;
        IQueryable<LearnerState> IApplicationDbContext.LearnerStates => LearnerStates;
        IQueryable<LearnerSettings> IApplicationDbContext.LearnerSettings => LearnerSettings;
        IQueryable<ResearchPackage> IApplicationDbContext.ResearchPackages => ResearchPackages;
        IQueryable<ChatSession> IApplicationDbContext.ChatSessions => ChatSessions;
        IQueryable<ChatMessage> IApplicationDbContext.ChatMessages => ChatMessages;
        IQueryable<Notification> IApplicationDbContext.Notifications => Notifications;
        IQueryable<Assessment> IApplicationDbContext.Assessments => Assessments;
        IQueryable<AssessmentResponse> IApplicationDbContext.AssessmentResponses => AssessmentResponses;
        IQueryable<LearningActivity> IApplicationDbContext.Activities => Activities;
        IQueryable<EducatorProject> IApplicationDbContext.EducatorProjects => EducatorProjects;
        IQueryable<ProjectSource> IApplicationDbContext.ProjectSources => ProjectSources;
        IQueryable<SourceConflict> IApplicationDbContext.SourceConflicts => SourceConflicts;
        IQueryable<CourseBlueprint> IApplicationDbContext.CourseBlueprints => CourseBlueprints;
        IQueryable<GenerationJob> IApplicationDbContext.GenerationJobs => GenerationJobs;
        IQueryable<Payment> IApplicationDbContext.Payments => Payments;
        IQueryable<LiveSession> IApplicationDbContext.LiveSessions => LiveSessions;
        IQueryable<CourseDiscussion> IApplicationDbContext.Discussions => Discussions;
        IQueryable<Assignment> IApplicationDbContext.Assignments => Assignments;
        IQueryable<AssignmentSubmission> IApplicationDbContext.AssignmentSubmissions => AssignmentSubmissions;
        IQueryable<OutboxMessage> IApplicationDbContext.OutboxMessages => OutboxMessages;
        IQueryable<AuditEvent> IApplicationDbContext.AuditEvents => AuditEvents;
        IQueryable<AccountRoleGrant> IApplicationDbContext.AccountRoleGrants => AccountRoleGrants;
        IQueryable<AuthSession> IApplicationDbContext.AuthSessions => AuthSessions;
        IQueryable<LegacyTokenExchange> IApplicationDbContext.LegacyTokenExchanges => LegacyTokenExchanges;
        IQueryable<AccountSecurityToken> IApplicationDbContext.AccountSecurityTokens => AccountSecurityTokens;
        IQueryable<AccountMfaCredential> IApplicationDbContext.AccountMfaCredentials => AccountMfaCredentials;
        IQueryable<AccountRecoveryCode> IApplicationDbContext.AccountRecoveryCodes => AccountRecoveryCodes;
        IQueryable<OAuthAuthorizationCode> IApplicationDbContext.OAuthAuthorizationCodes => OAuthAuthorizationCodes;
        IQueryable<InternalRequestReplay> IApplicationDbContext.InternalRequestReplays => InternalRequestReplays;
        IQueryable<MfaLoginChallenge> IApplicationDbContext.MfaLoginChallenges => MfaLoginChallenges;
        IQueryable<EducatorWorkspace> IApplicationDbContext.EducatorWorkspaces => EducatorWorkspaces;
        IQueryable<WorkspaceMembership> IApplicationDbContext.WorkspaceMemberships => WorkspaceMemberships;
        IQueryable<WorkspaceInstructorProfile> IApplicationDbContext.WorkspaceInstructorProfiles => WorkspaceInstructorProfiles;
        IQueryable<WorkspaceInvitation> IApplicationDbContext.WorkspaceInvitations => WorkspaceInvitations;
        IQueryable<CourseInstructorAssignment> IApplicationDbContext.CourseInstructorAssignments => CourseInstructorAssignments;
        IQueryable<EducatorApplication> IApplicationDbContext.EducatorApplications => EducatorApplications;
        IQueryable<VerificationClaim> IApplicationDbContext.VerificationClaims => VerificationClaims;
        IQueryable<WorkspaceEntitlement> IApplicationDbContext.WorkspaceEntitlements => WorkspaceEntitlements;
        IQueryable<PublicSlugAlias> IApplicationDbContext.PublicSlugAliases => PublicSlugAliases;
        IQueryable<AuthoringProject> IApplicationDbContext.AuthoringProjects => AuthoringProjects;
        IQueryable<CourseGenerationRequest> IApplicationDbContext.CourseGenerationRequests => CourseGenerationRequests;
        IQueryable<AuthoringSource> IApplicationDbContext.AuthoringSources => AuthoringSources;
        IQueryable<AuthoringJob> IApplicationDbContext.AuthoringJobs => AuthoringJobs;
        IQueryable<LegacyProjectAlias> IApplicationDbContext.LegacyProjectAliases => LegacyProjectAliases;
        IQueryable<CourseTemplateVersion> IApplicationDbContext.CourseTemplateVersions => CourseTemplateVersions;
        IQueryable<Asset> IApplicationDbContext.Assets => Assets;
        IQueryable<UploadSession> IApplicationDbContext.UploadSessions => UploadSessions;
        IQueryable<AssetDerivative> IApplicationDbContext.AssetDerivatives => AssetDerivatives;
        IQueryable<AssetReference> IApplicationDbContext.AssetReferences => AssetReferences;
        IQueryable<AssetProcessingJob> IApplicationDbContext.AssetProcessingJobs => AssetProcessingJobs;
        IQueryable<CourseRelease> IApplicationDbContext.CourseReleases => CourseReleases;
        IQueryable<ReleaseModule> IApplicationDbContext.ReleaseModules => ReleaseModules;
        IQueryable<ReleaseLesson> IApplicationDbContext.ReleaseLessons => ReleaseLessons;
        IQueryable<ReleaseQuiz> IApplicationDbContext.ReleaseQuizzes => ReleaseQuizzes;
        IQueryable<ReleaseAssignment> IApplicationDbContext.ReleaseAssignments => ReleaseAssignments;
        IQueryable<CourseCohort> IApplicationDbContext.CourseCohorts => CourseCohorts;
        IQueryable<ReleaseItemProgress> IApplicationDbContext.ReleaseItemProgress => ReleaseItemProgress;
        IQueryable<ProgressMigrationRecord> IApplicationDbContext.ProgressMigrationRecords => ProgressMigrationRecords;
        IQueryable<ReleaseQuizAttempt> IApplicationDbContext.ReleaseQuizAttempts => ReleaseQuizAttempts;
        IQueryable<EducatorPublicProfile> IApplicationDbContext.EducatorPublicProfiles => EducatorPublicProfiles;
        IQueryable<OrganizationSite> IApplicationDbContext.OrganizationSites => OrganizationSites;
        IQueryable<OrganizationSitePage> IApplicationDbContext.OrganizationSitePages => OrganizationSitePages;
        IQueryable<CustomDomain> IApplicationDbContext.CustomDomains => CustomDomains;
        IQueryable<VerifiedExternalWebsite> IApplicationDbContext.VerifiedExternalWebsites => VerifiedExternalWebsites;
        IQueryable<WorkspaceFollow> IApplicationDbContext.WorkspaceFollows => WorkspaceFollows;
        IQueryable<CourseAnnouncement> IApplicationDbContext.CourseAnnouncements => CourseAnnouncements;
        IQueryable<CourseReview> IApplicationDbContext.CourseReviews => CourseReviews;
        IQueryable<CourseReviewReply> IApplicationDbContext.CourseReviewReplies => CourseReviewReplies;
        IQueryable<CourseReviewReport> IApplicationDbContext.CourseReviewReports => CourseReviewReports;
        IQueryable<ModerationCase> IApplicationDbContext.ModerationCases => ModerationCases;
        IQueryable<CourseCompletionRecord> IApplicationDbContext.CourseCompletionRecords => CourseCompletionRecords;
        IQueryable<CourseCertificate> IApplicationDbContext.CourseCertificates => CourseCertificates;
        IQueryable<CertificateRenderJob> IApplicationDbContext.CertificateRenderJobs => CertificateRenderJobs;
        IQueryable<CourseOffer> IApplicationDbContext.CourseOffers => CourseOffers;
        IQueryable<PaymentWebhookEvent> IApplicationDbContext.PaymentWebhookEvents => PaymentWebhookEvents;
        IQueryable<ProductActivityEvent> IApplicationDbContext.ProductActivityEvents => ProductActivityEvents;


        public new void Add<TEntity>(TEntity entity) where TEntity : class => Set<TEntity>().Add(entity);
        public new void Remove<TEntity>(TEntity entity) where TEntity : class => Set<TEntity>().Remove(entity);
        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default) =>
            Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
                await operation();
                await transaction.CommitAsync(cancellationToken);
            });

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
