using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IFA.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableOperationsTenancyAndSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GenerationJobs_CourseId_ModuleIndex",
                table: "GenerationJobs");

            migrationBuilder.AddColumn<string>(
                name: "DedupeKey",
                table: "Notifications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AuthorizationVersion",
                table: "Learners",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisabledAt",
                table: "Learners",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedAt",
                table: "Learners",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordChangedAt",
                table: "Learners",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SecurityStamp",
                table: "Learners",
                type: "uuid",
                nullable: false,
                defaultValueSql: "md5(random()::text || clock_timestamp()::text)::uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ActorLearnerId",
                table: "GenerationJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "GenerationJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableAt",
                table: "GenerationJobs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "CausationId",
                table: "GenerationJobs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "GenerationJobs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorCode",
                table: "GenerationJobs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "GenerationJobs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "BlueprintModule");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastHeartbeatAt",
                table: "GenerationJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeaseExpiresAt",
                table: "GenerationJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeaseOwner",
                table: "GenerationJobs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LeaseToken",
                table: "GenerationJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttempts",
                table: "GenerationJobs",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "RequestJson",
                table: "GenerationJobs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "EducatorProjects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAccountId",
                table: "Courses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Courses",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Courses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByAccountId",
                table: "Courses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "Courses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountMfaCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProtectedSecret = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ProtectionKeyVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisabledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountMfaCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountMfaCredentials_Learners_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountRecoveryCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRecoveryCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountRecoveryCodes_Learners_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountRoleGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrantedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRoleGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountRoleGrants_Learners_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountSecurityTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TokenHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSecurityTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountSecurityTokens_Learners_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorLearnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CausationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    CsrfTokenHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    HostScope = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    SiteScope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MfaLevel = table.Column<int>(type: "integer", nullable: false),
                    AuthorizationVersionSnapshot = table.Column<long>(type: "bigint", nullable: false),
                    ParentSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserAgentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IpAddressHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthSessions_AuthSessions_ParentSessionId",
                        column: x => x.ParentSessionId,
                        principalTable: "AuthSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AuthSessions_Learners_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EducatorWorkspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SuspendedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuspensionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AuthorizationVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducatorWorkspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InternalRequestReplays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NonceHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalRequestReplays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MfaLoginChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    HostScope = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    SiteScope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaLoginChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MfaLoginChallenges_Learners_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeduplicationKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvailableAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    LeaseOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LeaseToken = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastHeartbeatAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeadLetteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CausationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ActorLearnerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicSlugAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicSlugAliases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegacyTokenExchanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyTokenExchanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegacyTokenExchanges_AuthSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AuthSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LegacyTokenExchanges_Learners_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OAuthAuthorizationCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RedirectUri = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CodeChallenge = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CodeChallengeMethod = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StateHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TargetSiteScope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MfaLevel = table.Column<int>(type: "integer", nullable: false),
                    TargetHost = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthAuthorizationCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OAuthAuthorizationCodes_AuthSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AuthSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OAuthAuthorizationCodes_Learners_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EducatorApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EvidenceJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewerAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducatorApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducatorApplications_EducatorWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "EducatorWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EducatorApplications_Learners_ApplicantAccountId",
                        column: x => x.ApplicantAccountId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VerificationClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClaimValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EvidenceHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewerAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationClaims_EducatorWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "EducatorWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceEntitlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GrantedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceEntitlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceEntitlements_EducatorWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "EducatorWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TokenHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    InvitedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceInvitations_EducatorWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "EducatorWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    State = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    InvitedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuspendedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceMemberships_EducatorWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "EducatorWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceMemberships_Learners_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseInstructorAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentRole = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseInstructorAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseInstructorAssignments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseInstructorAssignments_WorkspaceMemberships_Membership~",
                        column: x => x.MembershipId,
                        principalTable: "WorkspaceMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceInstructorProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Bio = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PhotoUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ExpertiseJson = table.Column<string>(type: "jsonb", nullable: false),
                    LinksJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceInstructorProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceInstructorProfiles_WorkspaceMemberships_Membership~",
                        column: x => x.MembershipId,
                        principalTable: "WorkspaceMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Normalize legacy generation rows before the active-job uniqueness
            // and status constraints are added. Duplicate rows are preserved as
            // failed review records rather than deleted.
            migrationBuilder.Sql("""
                UPDATE "GenerationJobs"
                SET "AvailableAt" = COALESCE("CreatedAt", CURRENT_TIMESTAMP),
                    "MaxAttempts" = CASE WHEN "MaxAttempts" < 1 THEN 3 ELSE "MaxAttempts" END,
                    "Kind" = 'BlueprintModule',
                    "Status" = CASE WHEN "Status" = 'Pending' THEN 'Queued' ELSE "Status" END;

                UPDATE "GenerationJobs"
                SET "Status" = 'Failed',
                    "ErrorCode" = 'invalid_legacy_status',
                    "Error" = 'The legacy job had an unsupported status and was preserved for review.',
                    "CompletedAt" = COALESCE("CompletedAt", CURRENT_TIMESTAMP)
                WHERE "Status" NOT IN ('Queued', 'Running', 'Done', 'Failed');

                WITH ranked AS (
                    SELECT "Id",
                           row_number() OVER (
                               PARTITION BY "CourseId", "ModuleIndex"
                               ORDER BY CASE "Status" WHEN 'Running' THEN 0 ELSE 1 END,
                                        "CreatedAt" DESC,
                                        "Id") AS position
                    FROM "GenerationJobs"
                    WHERE "Status" IN ('Queued', 'Running')
                )
                UPDATE "GenerationJobs" AS job
                SET "Status" = 'Failed',
                    "ErrorCode" = 'duplicate_legacy_job',
                    "Error" = 'A newer active job for this course module was retained during migration.',
                    "CompletedAt" = CURRENT_TIMESTAMP
                FROM ranked
                WHERE job."Id" = ranked."Id" AND ranked.position > 1;
                """);

            // Existing accounts are grandfathered as verified. Future accounts
            // are created with EmailVerifiedAt unset and must complete verification.
            migrationBuilder.Sql("""
                UPDATE "Learners"
                SET "EmailVerifiedAt" = COALESCE("EmailVerifiedAt", "CreatedAt", CURRENT_TIMESTAMP),
                    "PasswordChangedAt" = COALESCE("PasswordChangedAt", "CreatedAt", CURRENT_TIMESTAMP),
                    "SecurityStamp" = md5('security-stamp:' || "Id"::text)::uuid
                WHERE "EmailVerifiedAt" IS NULL
                   OR "PasswordChangedAt" IS NULL
                   OR "SecurityStamp" = '00000000-0000-0000-0000-000000000000'::uuid;

                INSERT INTO "AccountRoleGrants"
                    ("Id", "AccountId", "Role", "IsActive", "GrantedAt", "Reason")
                SELECT md5('role:learner:' || learner."Id"::text)::uuid,
                       learner."Id", 'Learner', true,
                       COALESCE(learner."CreatedAt", CURRENT_TIMESTAMP),
                       'Backfilled from legacy account'
                FROM "Learners" learner
                ON CONFLICT DO NOTHING;

                INSERT INTO "AccountRoleGrants"
                    ("Id", "AccountId", "Role", "IsActive", "GrantedAt", "Reason")
                SELECT md5('role:educator:' || learner."Id"::text)::uuid,
                       learner."Id", 'Educator', true,
                       COALESCE(learner."CreatedAt", CURRENT_TIMESTAMP),
                       'Backfilled from legacy role or owned content'
                FROM "Learners" learner
                WHERE lower(learner."Role") = 'educator'
                   OR EXISTS (SELECT 1 FROM "Courses" course WHERE course."CreatorLearnerId" = learner."Id")
                   OR EXISTS (SELECT 1 FROM "EducatorProjects" project WHERE project."EducatorLearnerId" = learner."Id")
                ON CONFLICT DO NOTHING;

                INSERT INTO "EducatorWorkspaces"
                    ("Id", "Type", "Status", "Name", "Slug", "CreatedByAccountId", "CreatedAt", "UpdatedAt", "AuthorizationVersion")
                VALUES
                    ('00000000-0000-0000-0000-000000000001'::uuid,
                     'System', 'Verified', 'Witforge Library', 'witforge-library', NULL,
                     CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 0)
                ON CONFLICT DO NOTHING;

                INSERT INTO "EducatorWorkspaces"
                    ("Id", "Type", "Status", "Name", "Slug", "CreatedByAccountId", "CreatedAt", "UpdatedAt", "AuthorizationVersion")
                SELECT md5('workspace:' || learner."Id"::text)::uuid,
                       'Individual', 'Verified',
                       left(COALESCE(NULLIF(trim(learner."Name"), ''), split_part(learner."Email", '@', 1)), 160),
                       left(COALESCE(NULLIF(trim(both '-' from regexp_replace(lower(learner."Name"), '[^a-z0-9]+', '-', 'g')), ''), 'educator'), 80)
                           || '-' || substr(replace(learner."Id"::text, '-', ''), 1, 8),
                       learner."Id",
                       COALESCE(learner."CreatedAt", CURRENT_TIMESTAMP),
                       CURRENT_TIMESTAMP,
                       0
                FROM "Learners" learner
                WHERE EXISTS (
                    SELECT 1 FROM "AccountRoleGrants" role
                    WHERE role."AccountId" = learner."Id" AND role."Role" = 'Educator' AND role."IsActive" = true)
                ON CONFLICT DO NOTHING;

                INSERT INTO "WorkspaceMemberships"
                    ("Id", "WorkspaceId", "AccountId", "Role", "State", "CreatedAt", "AcceptedAt")
                SELECT md5('membership:' || learner."Id"::text)::uuid,
                       md5('workspace:' || learner."Id"::text)::uuid,
                       learner."Id", 'Owner', 'Active',
                       COALESCE(learner."CreatedAt", CURRENT_TIMESTAMP),
                       COALESCE(learner."CreatedAt", CURRENT_TIMESTAMP)
                FROM "Learners" learner
                WHERE EXISTS (
                    SELECT 1 FROM "AccountRoleGrants" role
                    WHERE role."AccountId" = learner."Id" AND role."Role" = 'Educator' AND role."IsActive" = true)
                ON CONFLICT DO NOTHING;

                INSERT INTO "WorkspaceInstructorProfiles"
                    ("Id", "MembershipId", "DisplayName", "ExpertiseJson", "LinksJson", "IsVisible", "UpdatedAt")
                SELECT md5('instructor-profile:' || learner."Id"::text)::uuid,
                       md5('membership:' || learner."Id"::text)::uuid,
                       left(COALESCE(NULLIF(trim(learner."Name"), ''), split_part(learner."Email", '@', 1)), 160),
                       '[]'::jsonb, '[]'::jsonb, true, CURRENT_TIMESTAMP
                FROM "Learners" learner
                WHERE EXISTS (
                    SELECT 1 FROM "AccountRoleGrants" role
                    WHERE role."AccountId" = learner."Id" AND role."Role" = 'Educator' AND role."IsActive" = true)
                ON CONFLICT DO NOTHING;

                UPDATE "Courses" course
                SET "WorkspaceId" = CASE
                        WHEN course."CreatorLearnerId" IS NULL
                            THEN '00000000-0000-0000-0000-000000000001'::uuid
                        ELSE md5('workspace:' || course."CreatorLearnerId"::text)::uuid
                    END,
                    "Slug" = left(COALESCE(NULLIF(trim(both '-' from regexp_replace(lower(course."Title"), '[^a-z0-9]+', '-', 'g')), ''), 'course'), 100)
                        || '-' || substr(replace(course."Id"::text, '-', ''), 1, 8),
                    "CreatedByAccountId" = course."CreatorLearnerId",
                    "UpdatedByAccountId" = course."CreatorLearnerId",
                    "UpdatedAt" = COALESCE(course."CreatedAt", CURRENT_TIMESTAMP)
                WHERE course."WorkspaceId" IS NULL;

                UPDATE "EducatorProjects" project
                SET "WorkspaceId" = md5('workspace:' || project."EducatorLearnerId"::text)::uuid
                WHERE project."WorkspaceId" IS NULL;

                INSERT INTO "CourseInstructorAssignments"
                    ("Id", "CourseId", "MembershipId", "AssignmentRole", "OrderIndex", "IsActive", "AssignedAt", "AssignedByAccountId")
                SELECT md5('course-instructor:' || course."Id"::text || ':' || membership."Id"::text)::uuid,
                       course."Id", membership."Id", 'Owner', 0, true,
                       COALESCE(course."CreatedAt", CURRENT_TIMESTAMP),
                       course."CreatorLearnerId"
                FROM "Courses" course
                JOIN "WorkspaceMemberships" membership
                  ON membership."WorkspaceId" = course."WorkspaceId"
                 AND membership."AccountId" = course."CreatorLearnerId"
                WHERE course."CreatorLearnerId" IS NOT NULL
                ON CONFLICT DO NOTHING;

                INSERT INTO "WorkspaceEntitlements"
                    ("Id", "WorkspaceId", "Key", "IsEnabled", "GrantedAt")
                SELECT md5('entitlement:courses.create:' || workspace."Id"::text)::uuid,
                       workspace."Id", 'courses.create', true, CURRENT_TIMESTAMP
                FROM "EducatorWorkspaces" workspace
                WHERE workspace."Type" <> 'System'
                ON CONFLICT DO NOTHING;

                INSERT INTO "AuditEvents"
                    ("Id", "Action", "ResourceType", "ResourceId", "Outcome", "MetadataJson", "OccurredAt")
                SELECT md5('duplicate-projects:' || project."CourseId"::text)::uuid,
                       'migration.duplicate_course_projects',
                       'EducatorProject',
                       project."CourseId"::text,
                       'ReviewRequired',
                       jsonb_build_object(
                           'count', count(*),
                           'projectIds', jsonb_agg(project."Id" ORDER BY project."CreatedAt")),
                       CURRENT_TIMESTAMP
                FROM "EducatorProjects" project
                WHERE project."CourseId" IS NOT NULL
                GROUP BY project."CourseId"
                HAVING count(*) > 1
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DedupeKey",
                table: "Notifications",
                column: "DedupeKey",
                unique: true,
                filter: "\"DedupeKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationJobs_ActiveCourseModule",
                table: "GenerationJobs",
                columns: new[] { "CourseId", "ModuleIndex" },
                unique: true,
                filter: "\"Status\" IN ('Queued', 'Running')");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationJobs_Status_AvailableAt",
                table: "GenerationJobs",
                columns: new[] { "Status", "AvailableAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GenerationJobs_Status_LeaseExpiresAt",
                table: "GenerationJobs",
                columns: new[] { "Status", "LeaseExpiresAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_GenerationJobs_Status",
                table: "GenerationJobs",
                sql: "\"Status\" IN ('Queued', 'Running', 'Done', 'Failed')");

            migrationBuilder.CreateIndex(
                name: "IX_EducatorProjects_WorkspaceId",
                table: "EducatorProjects",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_WorkspaceId_Slug",
                table: "Courses",
                columns: new[] { "WorkspaceId", "Slug" },
                unique: true,
                filter: "\"WorkspaceId\" IS NOT NULL AND \"Slug\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountMfaCredentials_AccountId",
                table: "AccountMfaCredentials",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountRecoveryCodes_AccountId_UsedAt",
                table: "AccountRecoveryCodes",
                columns: new[] { "AccountId", "UsedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountRecoveryCodes_CodeHash",
                table: "AccountRecoveryCodes",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoleGrants_AccountId_Role",
                table: "AccountRoleGrants",
                columns: new[] { "AccountId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoleGrants_Role_IsActive",
                table: "AccountRoleGrants",
                columns: new[] { "Role", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountSecurityTokens_AccountId_Purpose_ConsumedAt",
                table: "AccountSecurityTokens",
                columns: new[] { "AccountId", "Purpose", "ConsumedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountSecurityTokens_TokenHash",
                table: "AccountSecurityTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_CorrelationId",
                table: "AuditEvents",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OccurredAt",
                table: "AuditEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_ResourceType_ResourceId",
                table: "AuditEvents",
                columns: new[] { "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_AccountId_RevokedAt_ExpiresAt",
                table: "AuthSessions",
                columns: new[] { "AccountId", "RevokedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_ParentSessionId",
                table: "AuthSessions",
                column: "ParentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_TokenHash",
                table: "AuthSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseInstructorAssignments_CourseId_IsActive_OrderIndex",
                table: "CourseInstructorAssignments",
                columns: new[] { "CourseId", "IsActive", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseInstructorAssignments_CourseId_MembershipId",
                table: "CourseInstructorAssignments",
                columns: new[] { "CourseId", "MembershipId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseInstructorAssignments_MembershipId",
                table: "CourseInstructorAssignments",
                column: "MembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_EducatorApplications_ApplicantAccountId_Status",
                table: "EducatorApplications",
                columns: new[] { "ApplicantAccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EducatorApplications_WorkspaceId_Status",
                table: "EducatorApplications",
                columns: new[] { "WorkspaceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EducatorWorkspaces_Slug",
                table: "EducatorWorkspaces",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EducatorWorkspaces_Type_Status",
                table: "EducatorWorkspaces",
                columns: new[] { "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InternalRequestReplays_ExpiresAt",
                table: "InternalRequestReplays",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_InternalRequestReplays_NonceHash",
                table: "InternalRequestReplays",
                column: "NonceHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyTokenExchanges_AccountId",
                table: "LegacyTokenExchanges",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyTokenExchanges_SessionId",
                table: "LegacyTokenExchanges",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyTokenExchanges_TokenHash",
                table: "LegacyTokenExchanges",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MfaLoginChallenges_AccountId_ConsumedAt_ExpiresAt",
                table: "MfaLoginChallenges",
                columns: new[] { "AccountId", "ConsumedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MfaLoginChallenges_TokenHash",
                table: "MfaLoginChallenges",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthAuthorizationCodes_AccountId_ConsumedAt_ExpiresAt",
                table: "OAuthAuthorizationCodes",
                columns: new[] { "AccountId", "ConsumedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthAuthorizationCodes_CodeHash",
                table: "OAuthAuthorizationCodes",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthAuthorizationCodes_SessionId",
                table: "OAuthAuthorizationCodes",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_DeduplicationKey",
                table: "OutboxMessages",
                column: "DeduplicationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_LeaseExpiresAt",
                table: "OutboxMessages",
                column: "LeaseExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt_DeadLetteredAt_AvailableAt",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "DeadLetteredAt", "AvailableAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicSlugAliases_TargetType_Slug",
                table: "PublicSlugAliases",
                columns: new[] { "TargetType", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicSlugAliases_TargetType_TargetId",
                table: "PublicSlugAliases",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_VerificationClaims_State_ExpiresAt",
                table: "VerificationClaims",
                columns: new[] { "State", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VerificationClaims_WorkspaceId_ClaimType_ClaimValue",
                table: "VerificationClaims",
                columns: new[] { "WorkspaceId", "ClaimType", "ClaimValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceEntitlements_WorkspaceId_Key",
                table: "WorkspaceEntitlements",
                columns: new[] { "WorkspaceId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceInstructorProfiles_MembershipId",
                table: "WorkspaceInstructorProfiles",
                column: "MembershipId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceInvitations_TokenHash",
                table: "WorkspaceInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceInvitations_WorkspaceId_Email_AcceptedAt_RevokedAt",
                table: "WorkspaceInvitations",
                columns: new[] { "WorkspaceId", "Email", "AcceptedAt", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMemberships_AccountId_State",
                table: "WorkspaceMemberships",
                columns: new[] { "AccountId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMemberships_WorkspaceId_AccountId",
                table: "WorkspaceMemberships",
                columns: new[] { "WorkspaceId", "AccountId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_EducatorWorkspaces_WorkspaceId",
                table: "Courses",
                column: "WorkspaceId",
                principalTable: "EducatorWorkspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EducatorProjects_EducatorWorkspaces_WorkspaceId",
                table: "EducatorProjects",
                column: "WorkspaceId",
                principalTable: "EducatorWorkspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_EducatorWorkspaces_WorkspaceId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_EducatorProjects_EducatorWorkspaces_WorkspaceId",
                table: "EducatorProjects");

            migrationBuilder.DropTable(
                name: "AccountMfaCredentials");

            migrationBuilder.DropTable(
                name: "AccountRecoveryCodes");

            migrationBuilder.DropTable(
                name: "AccountRoleGrants");

            migrationBuilder.DropTable(
                name: "AccountSecurityTokens");

            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "CourseInstructorAssignments");

            migrationBuilder.DropTable(
                name: "EducatorApplications");

            migrationBuilder.DropTable(
                name: "InternalRequestReplays");

            migrationBuilder.DropTable(
                name: "LegacyTokenExchanges");

            migrationBuilder.DropTable(
                name: "MfaLoginChallenges");

            migrationBuilder.DropTable(
                name: "OAuthAuthorizationCodes");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "PublicSlugAliases");

            migrationBuilder.DropTable(
                name: "VerificationClaims");

            migrationBuilder.DropTable(
                name: "WorkspaceEntitlements");

            migrationBuilder.DropTable(
                name: "WorkspaceInstructorProfiles");

            migrationBuilder.DropTable(
                name: "WorkspaceInvitations");

            migrationBuilder.DropTable(
                name: "AuthSessions");

            migrationBuilder.DropTable(
                name: "WorkspaceMemberships");

            migrationBuilder.DropTable(
                name: "EducatorWorkspaces");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_DedupeKey",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_GenerationJobs_ActiveCourseModule",
                table: "GenerationJobs");

            migrationBuilder.DropIndex(
                name: "IX_GenerationJobs_Status_AvailableAt",
                table: "GenerationJobs");

            migrationBuilder.DropIndex(
                name: "IX_GenerationJobs_Status_LeaseExpiresAt",
                table: "GenerationJobs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GenerationJobs_Status",
                table: "GenerationJobs");

            migrationBuilder.DropIndex(
                name: "IX_EducatorProjects_WorkspaceId",
                table: "EducatorProjects");

            migrationBuilder.DropIndex(
                name: "IX_Courses_WorkspaceId_Slug",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DedupeKey",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "AuthorizationVersion",
                table: "Learners");

            migrationBuilder.DropColumn(
                name: "DisabledAt",
                table: "Learners");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAt",
                table: "Learners");

            migrationBuilder.DropColumn(
                name: "PasswordChangedAt",
                table: "Learners");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Learners");

            migrationBuilder.DropColumn(
                name: "ActorLearnerId",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "AvailableAt",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "CausationId",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "ErrorCode",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "LastHeartbeatAt",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "LeaseExpiresAt",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "LeaseOwner",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "LeaseToken",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "MaxAttempts",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "RequestJson",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "EducatorProjects");

            migrationBuilder.DropColumn(
                name: "CreatedByAccountId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "UpdatedByAccountId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Courses");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationJobs_CourseId_ModuleIndex",
                table: "GenerationJobs",
                columns: new[] { "CourseId", "ModuleIndex" });
        }
    }
}
