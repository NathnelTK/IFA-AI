using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IFA.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Learners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    OverallProgress = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Learners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TargetAudience = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ProviderLogo = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Badge = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Rating = table.Column<double>(type: "double precision", nullable: false),
                    ReviewCount = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EstimatedDuration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    ShareCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatorLearnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_Learners_CreatorLearnerId",
                        column: x => x.CreatorLearnerId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SkillMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CompetencyPercentage = table.Column<int>(type: "integer", nullable: false),
                    HexColor = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IconName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsWeakArea = table.Column<bool>(type: "boolean", nullable: false),
                    LastAssessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillMetrics_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgressPercentage = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CompletedModulesCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    EnrolledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseEnrollments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseEnrollments_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseShareInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderLearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShareCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseShareInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseShareInvites_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseShareInvites_Learners_SenderLearnerId",
                        column: x => x.SenderLearnerId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EstimatedHours = table.Column<int>(type: "integer", nullable: false),
                    IsGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Modules_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ContentMarkdown = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    ReadingTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    YouTubeVideoId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    YouTubeVideoTitle = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    ScholarxivCitationDoi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ScholarxivPaperTitle = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lessons_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Quizzes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PassingScorePercentage = table.Column<int>(type: "integer", nullable: false, defaultValue: 70),
                    LastScorePercentage = table.Column<int>(type: "integer", nullable: true),
                    IsPassed = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quizzes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quizzes_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Options = table.Column<List<string>>(type: "text[]", nullable: false),
                    CorrectOptionIndex = table.Column<int>(type: "integer", nullable: false),
                    Explanation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    TargetSkillName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    BloomTaxonomyLevel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CourseId_LearnerId",
                table: "CourseEnrollments",
                columns: new[] { "CourseId", "LearnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_LearnerId",
                table: "CourseEnrollments",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CreatorLearnerId",
                table: "Courses",
                column: "CreatorLearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_IsPublic",
                table: "Courses",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_ShareCode",
                table: "Courses",
                column: "ShareCode");

            migrationBuilder.CreateIndex(
                name: "IX_CourseShareInvites_CourseId",
                table: "CourseShareInvites",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseShareInvites_SenderLearnerId",
                table: "CourseShareInvites",
                column: "SenderLearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseShareInvites_ShareCode",
                table: "CourseShareInvites",
                column: "ShareCode");

            migrationBuilder.CreateIndex(
                name: "IX_Learners_Email",
                table: "Learners",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_ModuleId_LessonNumber",
                table: "Lessons",
                columns: new[] { "ModuleId", "LessonNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_CourseId_ModuleNumber",
                table: "Modules",
                columns: new[] { "CourseId", "ModuleNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuizId",
                table: "Questions",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_ModuleId",
                table: "Quizzes",
                column: "ModuleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillMetrics_LearnerId_SkillName",
                table: "SkillMetrics",
                columns: new[] { "LearnerId", "SkillName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseEnrollments");

            migrationBuilder.DropTable(
                name: "CourseShareInvites");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "SkillMetrics");

            migrationBuilder.DropTable(
                name: "Quizzes");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Learners");
        }
    }
}
