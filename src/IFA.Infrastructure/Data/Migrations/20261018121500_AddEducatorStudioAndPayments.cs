using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IFA.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEducatorStudioAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quizzes_ModuleId",
                table: "Quizzes");

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Quizzes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "Quizzes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "ThumbnailUrl",
                table: "Courses",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AddColumn<string>(
                name: "ChapaReference",
                table: "Courses",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageSource",
                table: "Courses",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "Courses",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Courses",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExternalMaterials",
                table: "Courses",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Courses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceAmount",
                table: "Courses",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CourseEnrollments",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateTable(
                name: "EducatorProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EducatorLearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducatorProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducatorProjects_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EducatorProjects_Learners_EducatorLearnerId",
                        column: x => x.EducatorLearnerId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderTxRef = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CheckoutUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Payments_Learners_LearnerId",
                        column: x => x.LearnerId,
                        principalTable: "Learners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseBlueprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Json = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseBlueprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseBlueprints_EducatorProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "EducatorProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GenerationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleIndex = table.Column<int>(type: "integer", nullable: false),
                    ModuleTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GenerationJobs_EducatorProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "EducatorProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExtractedContent = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AddedBy = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Included = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectSources_EducatorProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "EducatorProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SourceConflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SourceIds = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Explanation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SuggestedResolutions = table.Column<string>(type: "text", nullable: true),
                    Resolution = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    ManualNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResolvedBy = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceConflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceConflicts_EducatorProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "EducatorProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_ModuleId",
                table: "Quizzes",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseBlueprints_ProjectId",
                table: "CourseBlueprints",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EducatorProjects_CourseId",
                table: "EducatorProjects",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_EducatorProjects_EducatorLearnerId",
                table: "EducatorProjects",
                column: "EducatorLearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_EducatorProjects_Status",
                table: "EducatorProjects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationJobs_CourseId_ModuleIndex",
                table: "GenerationJobs",
                columns: new[] { "CourseId", "ModuleIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_GenerationJobs_ProjectId",
                table: "GenerationJobs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CourseId_LearnerId",
                table: "Payments",
                columns: new[] { "CourseId", "LearnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_LearnerId",
                table: "Payments",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderTxRef",
                table: "Payments",
                column: "ProviderTxRef",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSources_ProjectId",
                table: "ProjectSources",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceConflicts_ProjectId",
                table: "SourceConflicts",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseBlueprints");

            migrationBuilder.DropTable(
                name: "GenerationJobs");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "ProjectSources");

            migrationBuilder.DropTable(
                name: "SourceConflicts");

            migrationBuilder.DropTable(
                name: "EducatorProjects");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_ModuleId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "ChapaReference",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CoverImageSource",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "ExternalMaterials",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "PriceAmount",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CourseEnrollments");

            migrationBuilder.AlterColumn<string>(
                name: "ThumbnailUrl",
                table: "Courses",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4096)",
                oldMaxLength: 4096);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_ModuleId",
                table: "Quizzes",
                column: "ModuleId",
                unique: true);
        }
    }
}
