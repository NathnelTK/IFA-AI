using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IFA.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ResolvePostMergeModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PracticalResource_ResearchPackages_ResearchPackageId",
                table: "PracticalResource");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoResource_ResearchPackages_ResearchPackageId",
                table: "VideoResource");

            migrationBuilder.DropIndex(
                name: "IX_QuizAttempts_QuizId",
                table: "QuizAttempts");

            migrationBuilder.DropIndex(
                name: "IX_QuizAnswers_QuizAttemptId",
                table: "QuizAnswers");

            migrationBuilder.DropIndex(
                name: "IX_LessonProgress_LessonId",
                table: "LessonProgress");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VideoResource",
                table: "VideoResource");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PracticalResource",
                table: "PracticalResource");

            migrationBuilder.RenameTable(
                name: "VideoResource",
                newName: "VideoResources");

            migrationBuilder.RenameTable(
                name: "PracticalResource",
                newName: "PracticalResources");

            migrationBuilder.RenameIndex(
                name: "IX_VideoResource_ResearchPackageId",
                table: "VideoResources",
                newName: "IX_VideoResources_ResearchPackageId");

            migrationBuilder.RenameIndex(
                name: "IX_PracticalResource_ResearchPackageId",
                table: "PracticalResources",
                newName: "IX_PracticalResources_ResearchPackageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VideoResources",
                table: "VideoResources",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PracticalResources",
                table: "PracticalResources",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_QuizId_LearnerId",
                table: "QuizAttempts",
                columns: new[] { "QuizId", "LearnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizAnswers_QuizAttemptId_QuestionId",
                table: "QuizAnswers",
                columns: new[] { "QuizAttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_LessonId_LearnerId",
                table: "LessonProgress",
                columns: new[] { "LessonId", "LearnerId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PracticalResources_ResearchPackages_ResearchPackageId",
                table: "PracticalResources",
                column: "ResearchPackageId",
                principalTable: "ResearchPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoResources_ResearchPackages_ResearchPackageId",
                table: "VideoResources",
                column: "ResearchPackageId",
                principalTable: "ResearchPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PracticalResources_ResearchPackages_ResearchPackageId",
                table: "PracticalResources");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoResources_ResearchPackages_ResearchPackageId",
                table: "VideoResources");

            migrationBuilder.DropIndex(
                name: "IX_QuizAttempts_QuizId_LearnerId",
                table: "QuizAttempts");

            migrationBuilder.DropIndex(
                name: "IX_QuizAnswers_QuizAttemptId_QuestionId",
                table: "QuizAnswers");

            migrationBuilder.DropIndex(
                name: "IX_LessonProgress_LessonId_LearnerId",
                table: "LessonProgress");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VideoResources",
                table: "VideoResources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PracticalResources",
                table: "PracticalResources");

            migrationBuilder.RenameTable(
                name: "VideoResources",
                newName: "VideoResource");

            migrationBuilder.RenameTable(
                name: "PracticalResources",
                newName: "PracticalResource");

            migrationBuilder.RenameIndex(
                name: "IX_VideoResources_ResearchPackageId",
                table: "VideoResource",
                newName: "IX_VideoResource_ResearchPackageId");

            migrationBuilder.RenameIndex(
                name: "IX_PracticalResources_ResearchPackageId",
                table: "PracticalResource",
                newName: "IX_PracticalResource_ResearchPackageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VideoResource",
                table: "VideoResource",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PracticalResource",
                table: "PracticalResource",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_QuizId",
                table: "QuizAttempts",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAnswers_QuizAttemptId",
                table: "QuizAnswers",
                column: "QuizAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_LessonId",
                table: "LessonProgress",
                column: "LessonId");

            migrationBuilder.AddForeignKey(
                name: "FK_PracticalResource_ResearchPackages_ResearchPackageId",
                table: "PracticalResource",
                column: "ResearchPackageId",
                principalTable: "ResearchPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoResource_ResearchPackages_ResearchPackageId",
                table: "VideoResource",
                column: "ResearchPackageId",
                principalTable: "ResearchPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
