using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IFA.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPerLearnerStateAndProfileContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcademicEvidence_ResearchPackages_ResearchPackageId",
                table: "AcademicEvidence");

            migrationBuilder.DropForeignKey(
                name: "FK_ResearchPackages_LearnerProfiles_LearnerProfileId",
                table: "ResearchPackages");

            migrationBuilder.DropIndex(
                name: "IX_ResearchPackages_LearnerProfileId",
                table: "ResearchPackages");

            migrationBuilder.DropIndex(
                name: "IX_LearnerProfiles_LearnerId",
                table: "LearnerProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AcademicEvidence",
                table: "AcademicEvidence");

            migrationBuilder.DropColumn(
                name: "FromPreferredChannel",
                table: "VideoResources");

            migrationBuilder.DropColumn(
                name: "LearnerProfileId",
                table: "ResearchPackages");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "PracticalResources");

            migrationBuilder.DropColumn(
                name: "AvailableStudyHoursPerWeek",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "KnownStrengths",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "KnownWeaknesses",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredLearningStyle",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredYouTubeChannels",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "SubjectTopic",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "RelevanceNote",
                table: "AcademicEvidence");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "AcademicEvidence");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "AcademicEvidence");

            migrationBuilder.RenameTable(
                name: "AcademicEvidence",
                newName: "AcademicEvidences");

            migrationBuilder.RenameColumn(
                name: "Goal",
                table: "LearnerProfiles",
                newName: "Requirements");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "LearnerProfiles",
                newName: "UpdatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_AcademicEvidence_ResearchPackageId",
                table: "AcademicEvidences",
                newName: "IX_AcademicEvidences_ResearchPackageId");

            migrationBuilder.AlterColumn<string>(
                name: "YouTubeVideoId",
                table: "VideoResources",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "VideoResources",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ChannelName",
                table: "VideoResources",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "VideoResources",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "VideoResources",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "VideoResources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "VideoResources",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "VideoResources",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "VideoResources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CourseId",
                table: "ResearchPackages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyConceptsJson",
                table: "ResearchPackages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "LearnerId",
                table: "ResearchPackages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "ResearchPackages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "ResearchPackages",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "PracticalResources",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "PracticalResources",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PracticalResources",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PracticalResources",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResourceType",
                table: "PracticalResources",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "PracticalResources",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Learners",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Learners",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Learner");

            migrationBuilder.AlterColumn<string>(
                name: "TargetOutcome",
                table: "LearnerProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PreferredLanguage",
                table: "LearnerProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "en",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CurrentLevel",
                table: "LearnerProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Constraints",
                table: "LearnerProfiles",
                type: "text",
                nullable: false,
                oldClrType: typeof(List<string>),
                oldType: "text[]");

            migrationBuilder.AddColumn<string>(
                name: "KnownStrengthsJson",
                table: "LearnerProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KnownWeaknessesJson",
                table: "LearnerProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LearningGoal",
                table: "LearnerProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LearningStyle",
                table: "LearnerProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Hands-on");

            migrationBuilder.AddColumn<string>(
                name: "PreferredYouTubeChannelsJson",
                table: "LearnerProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "LearnerProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WeeklyStudyHours",
                table: "LearnerProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "AcademicEvidences",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "AcademicEvidences",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Doi",
                table: "AcademicEvidences",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Authors",
                table: "AcademicEvidences",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Abstract",
                table: "AcademicEvidences",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AcademicEvidences",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicationDate",
                table: "AcademicEvidences",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "AcademicEvidences",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AcademicEvidences",
                table: "AcademicEvidences",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScorePercentage = table.Column<int>(type: "integer", nullable: false),
                    IsPassed = table.Column<bool>(type: "boolean", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assessments_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assessments_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearnerSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DailyReminders = table.Column<bool>(type: "boolean", nullable: false),
                    WeeklyDigest = table.Column<bool>(type: "boolean", nullable: false),
                    AssessmentResults = table.Column<bool>(type: "boolean", nullable: false),
                    CourseRecommendations = table.Column<bool>(type: "boolean", nullable: false),
                    Theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "system"),
                    AccentColor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pine"),
                    FontSize = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "medium"),
                    CompactMode = table.Column<bool>(type: "boolean", nullable: false),
                    ReducedMotion = table.Column<bool>(type: "boolean", nullable: false),
                    PreferredYouTubeChannelsJson = table.Column<string>(type: "text", nullable: false),
                    ExcludedYouTubeChannelsJson = table.Column<string>(type: "text", nullable: false),
                    PreferredTopicsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnerSettings_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearnerStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentCourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentModuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveStreakDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    TotalHoursLearned = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    CompletedLessonsCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CompletedQuizzesCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnerStates_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningActivities_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    LinkUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResearchSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResearchPackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Authors = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Snippet = table.Column<string>(type: "text", nullable: false),
                    RelevanceScore = table.Column<double>(type: "double precision", nullable: false),
                    PublishedYear = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchSources_ResearchPackages_ResearchPackageId",
                        column: x => x.ResearchPackageId,
                        principalTable: "ResearchPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedOptionIndex = table.Column<int>(type: "integer", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    Feedback = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentResponses_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentResponses_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResearchPackages_CourseId",
                table: "ResearchPackages",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerProfiles_LearnerId",
                table: "LearnerProfiles",
                column: "LearnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResponses_AssessmentId",
                table: "AssessmentResponses",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResponses_QuestionId",
                table: "AssessmentResponses",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_LearnerId",
                table: "Assessments",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_QuizId",
                table: "Assessments",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerSettings_LearnerId",
                table: "LearnerSettings",
                column: "LearnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearnerStates_LearnerId",
                table: "LearnerStates",
                column: "LearnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningActivities_LearnerId",
                table: "LearningActivities",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_LearnerId",
                table: "Notifications",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchSources_ResearchPackageId",
                table: "ResearchSources",
                column: "ResearchPackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcademicEvidences_ResearchPackages_ResearchPackageId",
                table: "AcademicEvidences",
                column: "ResearchPackageId",
                principalTable: "ResearchPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ResearchPackages_Courses_CourseId",
                table: "ResearchPackages",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcademicEvidences_ResearchPackages_ResearchPackageId",
                table: "AcademicEvidences");

            migrationBuilder.DropForeignKey(
                name: "FK_ResearchPackages_Courses_CourseId",
                table: "ResearchPackages");

            migrationBuilder.DropTable(
                name: "AssessmentResponses");

            migrationBuilder.DropTable(
                name: "LearnerSettings");

            migrationBuilder.DropTable(
                name: "LearnerStates");

            migrationBuilder.DropTable(
                name: "LearningActivities");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ResearchSources");

            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropIndex(
                name: "IX_ResearchPackages_CourseId",
                table: "ResearchPackages");

            migrationBuilder.DropIndex(
                name: "IX_LearnerProfiles_LearnerId",
                table: "LearnerProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AcademicEvidences",
                table: "AcademicEvidences");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "VideoResources");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "VideoResources");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "VideoResources");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "VideoResources");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "VideoResources");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "VideoResources");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "ResearchPackages");

            migrationBuilder.DropColumn(
                name: "KeyConceptsJson",
                table: "ResearchPackages");

            migrationBuilder.DropColumn(
                name: "LearnerId",
                table: "ResearchPackages");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "ResearchPackages");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "ResearchPackages");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PracticalResources");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PracticalResources");

            migrationBuilder.DropColumn(
                name: "ResourceType",
                table: "PracticalResources");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "PracticalResources");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Learners");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Learners");

            migrationBuilder.DropColumn(
                name: "KnownStrengthsJson",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "KnownWeaknessesJson",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "LearningGoal",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "LearningStyle",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredYouTubeChannelsJson",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "WeeklyStudyHours",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "Abstract",
                table: "AcademicEvidences");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AcademicEvidences");

            migrationBuilder.DropColumn(
                name: "PublicationDate",
                table: "AcademicEvidences");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "AcademicEvidences");

            migrationBuilder.RenameTable(
                name: "AcademicEvidences",
                newName: "AcademicEvidence");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "LearnerProfiles",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Requirements",
                table: "LearnerProfiles",
                newName: "Goal");

            migrationBuilder.RenameIndex(
                name: "IX_AcademicEvidences_ResearchPackageId",
                table: "AcademicEvidence",
                newName: "IX_AcademicEvidence_ResearchPackageId");

            migrationBuilder.AlterColumn<string>(
                name: "YouTubeVideoId",
                table: "VideoResources",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "VideoResources",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "ChannelName",
                table: "VideoResources",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<bool>(
                name: "FromPreferredChannel",
                table: "VideoResources",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LearnerProfileId",
                table: "ResearchPackages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "PracticalResources",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "PracticalResources",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "PracticalResources",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "TargetOutcome",
                table: "LearnerProfiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "PreferredLanguage",
                table: "LearnerProfiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "en");

            migrationBuilder.AlterColumn<string>(
                name: "CurrentLevel",
                table: "LearnerProfiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<List<string>>(
                name: "Constraints",
                table: "LearnerProfiles",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "AvailableStudyHoursPerWeek",
                table: "LearnerProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "KnownStrengths",
                table: "LearnerProfiles",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "KnownWeaknesses",
                table: "LearnerProfiles",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLearningStyle",
                table: "LearnerProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "PreferredYouTubeChannels",
                table: "LearnerProfiles",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "SubjectTopic",
                table: "LearnerProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "AcademicEvidence",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "AcademicEvidence",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Doi",
                table: "AcademicEvidence",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Authors",
                table: "AcademicEvidence",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<string>(
                name: "RelevanceNote",
                table: "AcademicEvidence",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "AcademicEvidence",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "AcademicEvidence",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AcademicEvidence",
                table: "AcademicEvidence",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchPackages_LearnerProfileId",
                table: "ResearchPackages",
                column: "LearnerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerProfiles_LearnerId",
                table: "LearnerProfiles",
                column: "LearnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcademicEvidence_ResearchPackages_ResearchPackageId",
                table: "AcademicEvidence",
                column: "ResearchPackageId",
                principalTable: "ResearchPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ResearchPackages_LearnerProfiles_LearnerProfileId",
                table: "ResearchPackages",
                column: "LearnerProfileId",
                principalTable: "LearnerProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
