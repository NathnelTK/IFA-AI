using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IFA.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWitforgeCourseWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccentColor",
                table: "Courses",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "#1B3D2F");

            migrationBuilder.AddColumn<DateTime>(
                name: "EnrollmentClosesAt",
                table: "Courses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EnrollmentLimit",
                table: "Courses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnrollmentMode",
                table: "Courses",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Open");

            migrationBuilder.AddColumn<DateTime>(
                name: "EnrollmentOpensAt",
                table: "Courses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstructorBio",
                table: "Courses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstructorTitle",
                table: "Courses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Courses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Courses",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "English");

            migrationBuilder.AddColumn<List<string>>(
                name: "LearningObjectives",
                table: "Courses",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "Courses",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Beginner");

            migrationBuilder.AddColumn<int>(
                name: "MaxQuizAttempts",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "PassingScorePercentage",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 70);

            migrationBuilder.AddColumn<string>(
                name: "Prerequisites",
                table: "Courses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequirePassingQuizzes",
                table: "Courses",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemeMode",
                table: "Courses",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "pine");

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "Courses",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Public");

            // IsPublic historically meant both "published" and "listed". Preserve
            // those courses, while linked Studio records remain unpublished drafts.
            migrationBuilder.Sql("""
                UPDATE "Courses" SET "IsPublished" = true, "Visibility" = 'Public' WHERE "IsPublic" = true;
                UPDATE "Courses" SET "ShareCode" = upper(md5("Id"::text || clock_timestamp()::text))
                WHERE length("ShareCode") < 20;
                """);

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InstructionsMarkdown = table.Column<string>(type: "text", nullable: false),
                    DueDaysAfterEnrollment = table.Column<int>(type: "integer", nullable: false),
                    RubricCriteriaJson = table.Column<string>(type: "text", nullable: false),
                    MaxScore = table.Column<int>(type: "integer", nullable: false),
                    AllowedFileTypes = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MaxFileSizeMb = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assignments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assignments_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Assignments_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CourseDiscussions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: true),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsInstructorReply = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseDiscussions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseDiscussions_CourseDiscussions_ParentId",
                        column: x => x.ParentId,
                        principalTable: "CourseDiscussions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseDiscussions_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseDiscussions_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseDiscussions_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LiveSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MeetingUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiveSessions_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionText = table.Column<string>(type: "text", nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    Feedback = table.Column<string>(type: "text", nullable: true),
                    GradedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GradedByEducatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentSubmissions_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentSubmissions_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_IsPublished_Visibility",
                table: "Courses",
                columns: new[] { "IsPublished", "Visibility" });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_CourseId",
                table: "Assignments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_LessonId",
                table: "Assignments",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_ModuleId",
                table: "Assignments",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmissions_AssignmentId_LearnerId",
                table: "AssignmentSubmissions",
                columns: new[] { "AssignmentId", "LearnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmissions_LearnerId",
                table: "AssignmentSubmissions",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseDiscussions_CourseId",
                table: "CourseDiscussions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseDiscussions_LearnerId",
                table: "CourseDiscussions",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseDiscussions_LessonId",
                table: "CourseDiscussions",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseDiscussions_ParentId",
                table: "CourseDiscussions",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveSessions_CourseId",
                table: "LiveSessions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OneOpenCheckout",
                table: "Payments",
                columns: new[] { "CourseId", "LearnerId", "Status" },
                unique: true,
                filter: "\"Status\" IN ('Pending', 'Initializing')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_OneOpenCheckout",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "AssignmentSubmissions");

            migrationBuilder.DropTable(
                name: "CourseDiscussions");

            migrationBuilder.DropTable(
                name: "LiveSessions");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Courses_IsPublished_Visibility",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AccentColor",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "EnrollmentClosesAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "EnrollmentLimit",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "EnrollmentMode",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "EnrollmentOpensAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "InstructorBio",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "InstructorTitle",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "LearningObjectives",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "MaxQuizAttempts",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "PassingScorePercentage",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Prerequisites",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "RequirePassingQuizzes",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "ThemeMode",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Courses");
        }
    }
}
