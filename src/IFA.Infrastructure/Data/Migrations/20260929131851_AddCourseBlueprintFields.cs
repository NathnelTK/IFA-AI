using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IFA.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseBlueprintFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "KeyTopics",
                table: "Modules",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceLearnerProfileId",
                table: "Courses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceResearchPackageId",
                table: "Courses",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KeyTopics",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "SourceLearnerProfileId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "SourceResearchPackageId",
                table: "Courses");
        }
    }
}
