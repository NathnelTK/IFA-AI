using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IFA.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthoringDeliveryCommerceAndSites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CohortId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureCode",
                table: "Payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Payments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeaseExpiresAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeaseOwner",
                table: "Payments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LeaseToken",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextVerificationAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<Guid>(
                name: "OfferId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderTransactionId",
                table: "Payments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnUrl",
                table: "Payments",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationAttemptCount",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VerificationPayloadHash",
                table: "Payments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CohortId",
                table: "LiveSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "LiveSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Announcements",
                table: "LearnerSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "FollowedEducatorUpdates",
                table: "LearnerSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReviewReplies",
                table: "LearnerSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AuthoringJobId",
                table: "GenerationJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TargetAudience",
                table: "Courses",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160);

            migrationBuilder.AddColumn<Guid>(
                name: "ActiveReleaseId",
                table: "Courses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionRule",
                table: "Courses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "Complete every required lesson and pass required assessments.");

            migrationBuilder.AddColumn<Guid>(
                name: "CoverAssetId",
                table: "Courses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CohortId",
                table: "CourseEnrollments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "CourseEnrollments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseId",
                table: "CourseEnrollments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssetId",
                table: "AssignmentSubmissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseAssignmentId",
                table: "AssignmentSubmissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VerifiedSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    QuarantineStorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    CleanStorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    PublicStorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Visibility = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RejectionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_EducatorWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "EducatorWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assets_Learners_OwnerAccountId",
                        column: x => x.OwnerAccountId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuthoringProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    LegacyEducatorProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureDetail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthoringProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthoringProjects_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthoringProjects_EducatorWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "EducatorWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseReleases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    SupersedesReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseReleases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseReleases_CourseReleases_SupersedesReleaseId",
                        column: x => x.SupersedesReleaseId,
                        principalTable: "CourseReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseReleases_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseTemplateVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DefinitionJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseTemplateVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Host = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VerificationToken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextCheckAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeaseOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LeaseToken = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpectedCnameTarget = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisabledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomDomains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomDomains_EducatorWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "EducatorWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModerationCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Priority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AssignedModeratorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Resolution = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EventKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductActivityEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductActivityEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgressMigrationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DetailJson = table.Column<string>(type: "jsonb", nullable: false),
                    MigratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressMigrationRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerifiedExternalWebsites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Host = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VerificationToken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextCheckAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeaseOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LeaseToken = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifiedExternalWebsites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerifiedExternalWebsites_EducatorWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "EducatorWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceFollows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceFollows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceFollows_EducatorWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "EducatorWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceFollows_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetDerivatives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDerivatives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetDerivatives_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetProcessingJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    State = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    AvailableAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeaseOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LeaseToken = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorDetail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetProcessingJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetProcessingJobs_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetReferences_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EducatorPublicProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Headline = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Bio = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    PhotoAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpertiseJson = table.Column<string>(type: "jsonb", nullable: false),
                    LinksJson = table.Column<string>(type: "jsonb", nullable: false),
                    ThemeJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducatorPublicProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducatorPublicProfiles_Assets_PhotoAssetId",
                        column: x => x.PhotoAssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EducatorPublicProfiles_EducatorWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "EducatorWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    LogoAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ThemeJson = table.Column<string>(type: "jsonb", nullable: false),
                    SeoJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationSites_Assets_LogoAssetId",
                        column: x => x.LogoAssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrganizationSites_EducatorWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "EducatorWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UploadSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MultipartUploadId = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UploadedPartsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AbortedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadSessions_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthoringJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthoringProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    State = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    ResultJson = table.Column<string>(type: "jsonb", nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    AvailableAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeaseOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LeaseToken = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorDetail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CausationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ActorAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthoringJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthoringJobs_AuthoringJobs_ParentJobId",
                        column: x => x.ParentJobId,
                        principalTable: "AuthoringJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthoringJobs_AuthoringProjects_AuthoringProjectId",
                        column: x => x.AuthoringProjectId,
                        principalTable: "AuthoringProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthoringSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthoringProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegacyProjectSourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Locator = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExtractedTextReference = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthoringSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthoringSources_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthoringSources_AuthoringProjects_AuthoringProjectId",
                        column: x => x.AuthoringProjectId,
                        principalTable: "AuthoringProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseGenerationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthoringProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetAudience = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    LearningObjectivesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ConstraintsJson = table.Column<string>(type: "jsonb", nullable: false),
                    TemplateId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TemplateVersion = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseGenerationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseGenerationRequests_AuthoringProjects_AuthoringProject~",
                        column: x => x.AuthoringProjectId,
                        principalTable: "AuthoringProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegacyProjectAliases",
                columns: table => new
                {
                    LegacyProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthoringProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyProjectAliases", x => x.LegacyProjectId);
                    table.ForeignKey(
                        name: "FK_LegacyProjectAliases_AuthoringProjects_AuthoringProjectId",
                        column: x => x.AuthoringProjectId,
                        principalTable: "AuthoringProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseCohorts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EnrollmentOpensAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EnrollmentClosesAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EnrollmentLimit = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseCohorts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseCohorts_CourseReleases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "CourseReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseCohorts_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseCompletionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseCompletionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseCompletionRecords_CourseEnrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "CourseEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseCompletionRecords_CourseReleases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "CourseReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Body = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModeratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModeratedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseReviews", x => x.Id);
                    table.CheckConstraint("CK_CourseReviews_Rating", "\"Rating\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_CourseReviews_CourseReleases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "CourseReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseReviews_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseReviews_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseItemProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProgressPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    EvidenceJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseItemProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseItemProgress_CourseEnrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "CourseEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReleaseItemProgress_CourseReleases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "CourseReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EstimatedHours = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseModules_CourseReleases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "CourseReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSitePages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationSiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    NavigationOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SchemaJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSitePages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationSitePages_OrganizationSites_OrganizationSiteId",
                        column: x => x.OrganizationSiteId,
                        principalTable: "OrganizationSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseAnnouncements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CohortId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BodyMarkdown = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    Audience = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseAnnouncements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseAnnouncements_CourseCohorts_CohortId",
                        column: x => x.CohortId,
                        principalTable: "CourseCohorts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CourseAnnouncements_CourseReleases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "CourseReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CourseAnnouncements_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PurchaseType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AvailableFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AvailableUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SeatLimit = table.Column<int>(type: "integer", nullable: true),
                    TermsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseOffers_CourseCohorts_CohortId",
                        column: x => x.CohortId,
                        principalTable: "CourseCohorts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CourseOffers_CourseReleases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "CourseReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseOffers_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletionRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VerificationCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PdfStorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    RenderingErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RenderedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseCertificates_CourseCompletionRecords_CompletionRecord~",
                        column: x => x.CompletionRecordId,
                        principalTable: "CourseCompletionRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseReviewReplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseReviewReplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseReviewReplies_CourseReviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "CourseReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseReviewReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Detail = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseReviewReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseReviewReports_CourseReviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "CourseReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseModuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InstructionsMarkdown = table.Column<string>(type: "text", nullable: false),
                    DueDaysAfterEnrollment = table.Column<int>(type: "integer", nullable: false),
                    MaxScore = table.Column<int>(type: "integer", nullable: false),
                    RubricCriteriaJson = table.Column<string>(type: "jsonb", nullable: false),
                    AllowedFileTypes = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MaxFileSizeMb = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseAssignments_CourseReleases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "CourseReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReleaseAssignments_ReleaseModules_ReleaseModuleId",
                        column: x => x.ReleaseModuleId,
                        principalTable: "ReleaseModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseLessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceLessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ContentMarkdown = table.Column<string>(type: "text", nullable: false),
                    ReadingTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    YouTubeVideoId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AssetReferencesJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseLessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseLessons_ReleaseModules_ReleaseModuleId",
                        column: x => x.ReleaseModuleId,
                        principalTable: "ReleaseModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseQuizzes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceQuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    PassingScorePercentage = table.Column<int>(type: "integer", nullable: false),
                    DefinitionJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseQuizzes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseQuizzes_ReleaseModules_ReleaseModuleId",
                        column: x => x.ReleaseModuleId,
                        principalTable: "ReleaseModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CertificateRenderJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    AvailableAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeaseOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LeaseToken = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorDetail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateRenderJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateRenderJobs_CourseCertificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "CourseCertificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseQuizAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseQuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnswersJson = table.Column<string>(type: "jsonb", nullable: false),
                    ScorePercentage = table.Column<int>(type: "integer", nullable: false),
                    IsPassed = table.Column<bool>(type: "boolean", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseQuizAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseQuizAttempts_CourseEnrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "CourseEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReleaseQuizAttempts_ReleaseQuizzes_ReleaseQuizId",
                        column: x => x.ReleaseQuizId,
                        principalTable: "ReleaseQuizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                UPDATE "Payments"
                SET "IdempotencyKey" = 'legacy:' || "Id"::text,
                    "NextVerificationAt" = CASE
                        WHEN "Status" IN ('Pending', 'Initializing') THEN CURRENT_TIMESTAMP
                        ELSE COALESCE("CompletedAt", "CreatedAt", CURRENT_TIMESTAMP)
                    END
                WHERE "IdempotencyKey" IS NULL OR "IdempotencyKey" = '';

                UPDATE "Courses"
                SET "CompletionRule" = 'Complete every required lesson and pass required assessments.'
                WHERE "CompletionRule" = '';

                -- Cosmetic legacy ratings are not evidence. Real aggregates are
                -- projected from CourseReviews after this migration.
                UPDATE "Courses" SET "Rating" = 0, "ReviewCount" = '0';

                WITH canonical AS (
                    SELECT course."Id" AS "CourseId",
                           selected."Id" AS "LegacyProjectId"
                    FROM "Courses" course
                    LEFT JOIN LATERAL (
                        SELECT project."Id"
                        FROM "EducatorProjects" project
                        LEFT JOIN "CourseBlueprints" blueprint ON blueprint."ProjectId" = project."Id"
                        WHERE project."CourseId" = course."Id"
                        ORDER BY
                            CASE project."Status"
                                WHEN 'Published' THEN 0
                                WHEN 'ReadyForReview' THEN 1
                                WHEN 'Generating' THEN 2
                                WHEN 'BlueprintReady' THEN 3
                                ELSE 4
                            END,
                            CASE WHEN blueprint."ApprovedAt" IS NOT NULL THEN 0 ELSE 1 END,
                            project."UpdatedAt" DESC,
                            project."Id"
                        LIMIT 1
                    ) selected ON true
                )
                INSERT INTO "AuthoringProjects"
                    ("Id", "WorkspaceId", "CourseId", "CreatedByAccountId", "LegacyEducatorProjectId",
                     "Mode", "State", "Title", "CreatedAt", "UpdatedAt", "Version")
                SELECT COALESCE(canonical."LegacyProjectId", md5('authoring-project:' || course."Id"::text)::uuid),
                       course."WorkspaceId",
                       course."Id",
                       course."CreatorLearnerId",
                       canonical."LegacyProjectId",
                       CASE WHEN canonical."LegacyProjectId" IS NOT NULL AND EXISTS (
                           SELECT 1 FROM "ProjectSources" source WHERE source."ProjectId" = canonical."LegacyProjectId")
                           THEN 'Materials' ELSE 'Manual' END,
                       CASE
                           WHEN course."IsPublished" THEN 'Published'
                           WHEN project."Status" = 'GenerationFailed' THEN 'Failed'
                           WHEN project."Status" = 'Generating' THEN 'Generating'
                           WHEN project."Status" = 'BlueprintReady' THEN 'AwaitingApproval'
                           ELSE 'Reviewing'
                       END,
                       left(course."Title", 300),
                       COALESCE(project."CreatedAt", course."CreatedAt", CURRENT_TIMESTAMP),
                       COALESCE(project."UpdatedAt", course."UpdatedAt", course."CreatedAt", CURRENT_TIMESTAMP),
                       0
                FROM "Courses" course
                JOIN canonical ON canonical."CourseId" = course."Id"
                LEFT JOIN "EducatorProjects" project ON project."Id" = canonical."LegacyProjectId"
                WHERE course."WorkspaceId" IS NOT NULL;

                INSERT INTO "LegacyProjectAliases" ("LegacyProjectId", "AuthoringProjectId", "CreatedAt")
                SELECT project."Id", authoring."Id", CURRENT_TIMESTAMP
                FROM "EducatorProjects" project
                JOIN "AuthoringProjects" authoring ON authoring."CourseId" = project."CourseId"
                WHERE project."CourseId" IS NOT NULL;

                INSERT INTO "CourseGenerationRequests"
                    ("Id", "AuthoringProjectId", "Prompt", "Category", "TargetAudience",
                     "LearningObjectivesJson", "ConstraintsJson", "CreatedAt", "UpdatedAt")
                SELECT md5('generation-request:' || authoring."Id"::text)::uuid,
                       authoring."Id",
                       left(course."Description", 8000),
                       left(course."Category", 100),
                       left(course."TargetAudience", 300),
                       to_jsonb(course."LearningObjectives"),
                       '{}'::jsonb,
                       authoring."CreatedAt",
                       authoring."UpdatedAt"
                FROM "AuthoringProjects" authoring
                JOIN "Courses" course ON course."Id" = authoring."CourseId";

                INSERT INTO "AuthoringSources"
                    ("Id", "AuthoringProjectId", "LegacyProjectSourceId", "Kind", "Locator", "Status",
                     "MetadataJson", "ErrorCode", "CreatedAt", "UpdatedAt")
                SELECT source."Id",
                       alias."AuthoringProjectId",
                       source."Id",
                       'Url',
                       source."Url",
                       source."Status",
                       jsonb_build_object('type', source."Type", 'title', source."Title"),
                       CASE WHEN source."Status" = 'Failed' THEN 'legacy_source_failed' ELSE NULL END,
                       source."CreatedAt",
                       COALESCE(source."CreatedAt", CURRENT_TIMESTAMP)
                FROM "ProjectSources" source
                JOIN "LegacyProjectAliases" alias ON alias."LegacyProjectId" = source."ProjectId";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "IdempotencyKey",
                table: "Payments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CohortId",
                table: "Payments",
                column: "CohortId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_IdempotencyKey",
                table: "Payments",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OfferId",
                table: "Payments",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReleaseId",
                table: "Payments",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status_NextVerificationAt",
                table: "Payments",
                columns: new[] { "Status", "NextVerificationAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveSessions_CohortId",
                table: "LiveSessions",
                column: "CohortId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveSessions_ReleaseId",
                table: "LiveSessions",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationJobs_AuthoringJobId",
                table: "GenerationJobs",
                column: "AuthoringJobId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_ActiveReleaseId",
                table: "Courses",
                column: "ActiveReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CoverAssetId",
                table: "Courses",
                column: "CoverAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CohortId",
                table: "CourseEnrollments",
                column: "CohortId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_ReleaseId",
                table: "CourseEnrollments",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmissions_AssetId",
                table: "AssignmentSubmissions",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmissions_ReleaseAssignmentId",
                table: "AssignmentSubmissions",
                column: "ReleaseAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetDerivatives_AssetId_Kind",
                table: "AssetDerivatives",
                columns: new[] { "AssetId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetDerivatives_StorageKey",
                table: "AssetDerivatives",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetProcessingJobs_AssetId_Kind",
                table: "AssetProcessingJobs",
                columns: new[] { "AssetId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetProcessingJobs_State_AvailableAt",
                table: "AssetProcessingJobs",
                columns: new[] { "State", "AvailableAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetReferences_AssetId",
                table: "AssetReferences",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReferences_EntityType_EntityId_Purpose",
                table: "AssetReferences",
                columns: new[] { "EntityType", "EntityId", "Purpose" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CleanStorageKey",
                table: "Assets",
                column: "CleanStorageKey",
                unique: true,
                filter: "\"CleanStorageKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_OwnerAccountId",
                table: "Assets",
                column: "OwnerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_PublicStorageKey",
                table: "Assets",
                column: "PublicStorageKey",
                unique: true,
                filter: "\"PublicStorageKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_QuarantineStorageKey",
                table: "Assets",
                column: "QuarantineStorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_WorkspaceId_Status_CreatedAt",
                table: "Assets",
                columns: new[] { "WorkspaceId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthoringJobs_AuthoringProjectId_State",
                table: "AuthoringJobs",
                columns: new[] { "AuthoringProjectId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthoringJobs_LeaseExpiresAt",
                table: "AuthoringJobs",
                column: "LeaseExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuthoringJobs_ParentJobId",
                table: "AuthoringJobs",
                column: "ParentJobId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthoringJobs_State_AvailableAt",
                table: "AuthoringJobs",
                columns: new[] { "State", "AvailableAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthoringProjects_CourseId",
                table: "AuthoringProjects",
                column: "CourseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthoringProjects_LegacyEducatorProjectId",
                table: "AuthoringProjects",
                column: "LegacyEducatorProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthoringProjects_WorkspaceId_State_UpdatedAt",
                table: "AuthoringProjects",
                columns: new[] { "WorkspaceId", "State", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthoringSources_AssetId",
                table: "AuthoringSources",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthoringSources_AuthoringProjectId_Status",
                table: "AuthoringSources",
                columns: new[] { "AuthoringProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthoringSources_LegacyProjectSourceId",
                table: "AuthoringSources",
                column: "LegacyProjectSourceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRenderJobs_CertificateId",
                table: "CertificateRenderJobs",
                column: "CertificateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRenderJobs_State_AvailableAt",
                table: "CertificateRenderJobs",
                columns: new[] { "State", "AvailableAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseAnnouncements_CohortId",
                table: "CourseAnnouncements",
                column: "CohortId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseAnnouncements_CourseId_PublishedAt",
                table: "CourseAnnouncements",
                columns: new[] { "CourseId", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseAnnouncements_ReleaseId",
                table: "CourseAnnouncements",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseCertificates_CompletionRecordId",
                table: "CourseCertificates",
                column: "CompletionRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseCertificates_SerialNumber",
                table: "CourseCertificates",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseCertificates_VerificationCode",
                table: "CourseCertificates",
                column: "VerificationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseCohorts_CourseId_Status",
                table: "CourseCohorts",
                columns: new[] { "CourseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseCohorts_ReleaseId",
                table: "CourseCohorts",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseCompletionRecords_EnrollmentId",
                table: "CourseCompletionRecords",
                column: "EnrollmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseCompletionRecords_ReleaseId_LearnerId",
                table: "CourseCompletionRecords",
                columns: new[] { "ReleaseId", "LearnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseGenerationRequests_AuthoringProjectId",
                table: "CourseGenerationRequests",
                column: "AuthoringProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseOffers_CohortId",
                table: "CourseOffers",
                column: "CohortId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseOffers_CourseId_IsActive_AvailableFrom_AvailableUntil",
                table: "CourseOffers",
                columns: new[] { "CourseId", "IsActive", "AvailableFrom", "AvailableUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseOffers_CourseId_Version",
                table: "CourseOffers",
                columns: new[] { "CourseId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseOffers_ReleaseId",
                table: "CourseOffers",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseReleases_ContentHash",
                table: "CourseReleases",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_CourseReleases_CourseId_Version",
                table: "CourseReleases",
                columns: new[] { "CourseId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseReleases_SupersedesReleaseId",
                table: "CourseReleases",
                column: "SupersedesReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviewReplies_ReviewId_AuthorAccountId",
                table: "CourseReviewReplies",
                columns: new[] { "ReviewId", "AuthorAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviewReports_ReviewId_ReporterAccountId",
                table: "CourseReviewReports",
                columns: new[] { "ReviewId", "ReporterAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviewReports_Status_CreatedAt",
                table: "CourseReviewReports",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviews_CourseId_LearnerId",
                table: "CourseReviews",
                columns: new[] { "CourseId", "LearnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviews_CourseId_Status_CreatedAt",
                table: "CourseReviews",
                columns: new[] { "CourseId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviews_LearnerId",
                table: "CourseReviews",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviews_ReleaseId",
                table: "CourseReviews",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseTemplateVersions_TemplateKey_Version",
                table: "CourseTemplateVersions",
                columns: new[] { "TemplateKey", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomDomains_Host",
                table: "CustomDomains",
                column: "Host",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomDomains_Status_NextCheckAt",
                table: "CustomDomains",
                columns: new[] { "Status", "NextCheckAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomDomains_WorkspaceId",
                table: "CustomDomains",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_EducatorPublicProfiles_PhotoAssetId",
                table: "EducatorPublicProfiles",
                column: "PhotoAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_EducatorPublicProfiles_Slug",
                table: "EducatorPublicProfiles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EducatorPublicProfiles_WorkspaceId",
                table: "EducatorPublicProfiles",
                column: "WorkspaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyProjectAliases_AuthoringProjectId",
                table: "LegacyProjectAliases",
                column: "AuthoringProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationCases_ResourceType_ResourceId_Status",
                table: "ModerationCases",
                columns: new[] { "ResourceType", "ResourceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationCases_Status_Priority_CreatedAt",
                table: "ModerationCases",
                columns: new[] { "Status", "Priority", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSitePages_OrganizationSiteId_IsVisible_Navigati~",
                table: "OrganizationSitePages",
                columns: new[] { "OrganizationSiteId", "IsVisible", "NavigationOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSitePages_OrganizationSiteId_PageType",
                table: "OrganizationSitePages",
                columns: new[] { "OrganizationSiteId", "PageType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSites_LogoAssetId",
                table: "OrganizationSites",
                column: "LogoAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSites_Slug",
                table: "OrganizationSites",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSites_WorkspaceId",
                table: "OrganizationSites",
                column: "WorkspaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_Provider_EventKey",
                table: "PaymentWebhookEvents",
                columns: new[] { "Provider", "EventKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_ReceivedAt",
                table: "PaymentWebhookEvents",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductActivityEvents_ActorAccountId_OccurredAt",
                table: "ProductActivityEvents",
                columns: new[] { "ActorAccountId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductActivityEvents_CourseId_EventType_OccurredAt",
                table: "ProductActivityEvents",
                columns: new[] { "CourseId", "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductActivityEvents_WorkspaceId_EventType_OccurredAt",
                table: "ProductActivityEvents",
                columns: new[] { "WorkspaceId", "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgressMigrationRecords_EnrollmentId",
                table: "ProgressMigrationRecords",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressMigrationRecords_SourceType_SourceId",
                table: "ProgressMigrationRecords",
                columns: new[] { "SourceType", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseAssignments_ReleaseId_SourceAssignmentId",
                table: "ReleaseAssignments",
                columns: new[] { "ReleaseId", "SourceAssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseAssignments_ReleaseModuleId",
                table: "ReleaseAssignments",
                column: "ReleaseModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseItemProgress_EnrollmentId_ItemType_ItemId",
                table: "ReleaseItemProgress",
                columns: new[] { "EnrollmentId", "ItemType", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseItemProgress_ReleaseId_LearnerId",
                table: "ReleaseItemProgress",
                columns: new[] { "ReleaseId", "LearnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseLessons_ReleaseModuleId_LessonNumber",
                table: "ReleaseLessons",
                columns: new[] { "ReleaseModuleId", "LessonNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseLessons_ReleaseModuleId_SourceLessonId",
                table: "ReleaseLessons",
                columns: new[] { "ReleaseModuleId", "SourceLessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseModules_ReleaseId_ModuleNumber",
                table: "ReleaseModules",
                columns: new[] { "ReleaseId", "ModuleNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseModules_ReleaseId_SourceModuleId",
                table: "ReleaseModules",
                columns: new[] { "ReleaseId", "SourceModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseQuizAttempts_EnrollmentId_ReleaseQuizId_AttemptedAt",
                table: "ReleaseQuizAttempts",
                columns: new[] { "EnrollmentId", "ReleaseQuizId", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseQuizAttempts_ReleaseQuizId",
                table: "ReleaseQuizAttempts",
                column: "ReleaseQuizId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseQuizzes_ReleaseModuleId_OrderIndex",
                table: "ReleaseQuizzes",
                columns: new[] { "ReleaseModuleId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseQuizzes_ReleaseModuleId_SourceQuizId",
                table: "ReleaseQuizzes",
                columns: new[] { "ReleaseModuleId", "SourceQuizId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_AssetId_State",
                table: "UploadSessions",
                columns: new[] { "AssetId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_ExpiresAt",
                table: "UploadSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedExternalWebsites_Status_NextCheckAt",
                table: "VerifiedExternalWebsites",
                columns: new[] { "Status", "NextCheckAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedExternalWebsites_WorkspaceId_Host",
                table: "VerifiedExternalWebsites",
                columns: new[] { "WorkspaceId", "Host" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceFollows_LearnerId",
                table: "WorkspaceFollows",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceFollows_WorkspaceId_LearnerId",
                table: "WorkspaceFollows",
                columns: new[] { "WorkspaceId", "LearnerId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentSubmissions_Assets_AssetId",
                table: "AssignmentSubmissions",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentSubmissions_ReleaseAssignments_ReleaseAssignmentId",
                table: "AssignmentSubmissions",
                column: "ReleaseAssignmentId",
                principalTable: "ReleaseAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_CourseCohorts_CohortId",
                table: "CourseEnrollments",
                column: "CohortId",
                principalTable: "CourseCohorts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_CourseReleases_ReleaseId",
                table: "CourseEnrollments",
                column: "ReleaseId",
                principalTable: "CourseReleases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Assets_CoverAssetId",
                table: "Courses",
                column: "CoverAssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_CourseReleases_ActiveReleaseId",
                table: "Courses",
                column: "ActiveReleaseId",
                principalTable: "CourseReleases",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_GenerationJobs_AuthoringJobs_AuthoringJobId",
                table: "GenerationJobs",
                column: "AuthoringJobId",
                principalTable: "AuthoringJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LiveSessions_CourseCohorts_CohortId",
                table: "LiveSessions",
                column: "CohortId",
                principalTable: "CourseCohorts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LiveSessions_CourseReleases_ReleaseId",
                table: "LiveSessions",
                column: "ReleaseId",
                principalTable: "CourseReleases",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_CourseCohorts_CohortId",
                table: "Payments",
                column: "CohortId",
                principalTable: "CourseCohorts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_CourseOffers_OfferId",
                table: "Payments",
                column: "OfferId",
                principalTable: "CourseOffers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_CourseReleases_ReleaseId",
                table: "Payments",
                column: "ReleaseId",
                principalTable: "CourseReleases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentSubmissions_Assets_AssetId",
                table: "AssignmentSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentSubmissions_ReleaseAssignments_ReleaseAssignmentId",
                table: "AssignmentSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_CourseCohorts_CohortId",
                table: "CourseEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_CourseReleases_ReleaseId",
                table: "CourseEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Assets_CoverAssetId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_CourseReleases_ActiveReleaseId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_GenerationJobs_AuthoringJobs_AuthoringJobId",
                table: "GenerationJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_LiveSessions_CourseCohorts_CohortId",
                table: "LiveSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_LiveSessions_CourseReleases_ReleaseId",
                table: "LiveSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_CourseCohorts_CohortId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_CourseOffers_OfferId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_CourseReleases_ReleaseId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "AssetDerivatives");

            migrationBuilder.DropTable(
                name: "AssetProcessingJobs");

            migrationBuilder.DropTable(
                name: "AssetReferences");

            migrationBuilder.DropTable(
                name: "AuthoringJobs");

            migrationBuilder.DropTable(
                name: "AuthoringSources");

            migrationBuilder.DropTable(
                name: "CertificateRenderJobs");

            migrationBuilder.DropTable(
                name: "CourseAnnouncements");

            migrationBuilder.DropTable(
                name: "CourseGenerationRequests");

            migrationBuilder.DropTable(
                name: "CourseOffers");

            migrationBuilder.DropTable(
                name: "CourseReviewReplies");

            migrationBuilder.DropTable(
                name: "CourseReviewReports");

            migrationBuilder.DropTable(
                name: "CourseTemplateVersions");

            migrationBuilder.DropTable(
                name: "CustomDomains");

            migrationBuilder.DropTable(
                name: "EducatorPublicProfiles");

            migrationBuilder.DropTable(
                name: "LegacyProjectAliases");

            migrationBuilder.DropTable(
                name: "ModerationCases");

            migrationBuilder.DropTable(
                name: "OrganizationSitePages");

            migrationBuilder.DropTable(
                name: "PaymentWebhookEvents");

            migrationBuilder.DropTable(
                name: "ProductActivityEvents");

            migrationBuilder.DropTable(
                name: "ProgressMigrationRecords");

            migrationBuilder.DropTable(
                name: "ReleaseAssignments");

            migrationBuilder.DropTable(
                name: "ReleaseItemProgress");

            migrationBuilder.DropTable(
                name: "ReleaseLessons");

            migrationBuilder.DropTable(
                name: "ReleaseQuizAttempts");

            migrationBuilder.DropTable(
                name: "UploadSessions");

            migrationBuilder.DropTable(
                name: "VerifiedExternalWebsites");

            migrationBuilder.DropTable(
                name: "WorkspaceFollows");

            migrationBuilder.DropTable(
                name: "CourseCertificates");

            migrationBuilder.DropTable(
                name: "CourseCohorts");

            migrationBuilder.DropTable(
                name: "CourseReviews");

            migrationBuilder.DropTable(
                name: "AuthoringProjects");

            migrationBuilder.DropTable(
                name: "OrganizationSites");

            migrationBuilder.DropTable(
                name: "ReleaseQuizzes");

            migrationBuilder.DropTable(
                name: "CourseCompletionRecords");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "ReleaseModules");

            migrationBuilder.DropTable(
                name: "CourseReleases");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CohortId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_OfferId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ReleaseId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Status_NextVerificationAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_LiveSessions_CohortId",
                table: "LiveSessions");

            migrationBuilder.DropIndex(
                name: "IX_LiveSessions_ReleaseId",
                table: "LiveSessions");

            migrationBuilder.DropIndex(
                name: "IX_GenerationJobs_AuthoringJobId",
                table: "GenerationJobs");

            migrationBuilder.DropIndex(
                name: "IX_Courses_ActiveReleaseId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_CoverAssetId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_CohortId",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_ReleaseId",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentSubmissions_AssetId",
                table: "AssignmentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentSubmissions_ReleaseAssignmentId",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "CohortId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FailureCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "LeaseExpiresAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "LeaseOwner",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "LeaseToken",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "NextVerificationAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "OfferId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderTransactionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReturnUrl",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "VerificationAttemptCount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "VerificationPayloadHash",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CohortId",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "Announcements",
                table: "LearnerSettings");

            migrationBuilder.DropColumn(
                name: "FollowedEducatorUpdates",
                table: "LearnerSettings");

            migrationBuilder.DropColumn(
                name: "ReviewReplies",
                table: "LearnerSettings");

            migrationBuilder.DropColumn(
                name: "AuthoringJobId",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "ActiveReleaseId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CompletionRule",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CoverAssetId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CohortId",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "AssignmentSubmissions");

            migrationBuilder.DropColumn(
                name: "ReleaseAssignmentId",
                table: "AssignmentSubmissions");

            migrationBuilder.AlterColumn<string>(
                name: "TargetAudience",
                table: "Courses",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);
        }
    }
}
