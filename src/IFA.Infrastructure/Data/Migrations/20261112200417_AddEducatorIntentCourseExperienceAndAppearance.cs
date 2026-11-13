using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IFA.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEducatorIntentCourseExperienceAndAppearance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RegistrationIntent",
                table: "Learners",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Learner");

            migrationBuilder.AddColumn<string>(
                name: "AppearanceJson",
                table: "Courses",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AppearanceUpdatedAt",
                table: "Courses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AppearanceVersion",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AwardsCertificate",
                table: "Courses",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CourseAssistantEnabled",
                table: "Courses",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Overview",
                table: "Courses",
                type: "character varying(12000)",
                maxLength: 12000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE \"Learners\" SET \"RegistrationIntent\" = 'Educator' WHERE \"Role\" = 'Educator';");
            migrationBuilder.Sql("UPDATE \"Courses\" SET \"Overview\" = \"Description\" WHERE \"Overview\" = ''; ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegistrationIntent",
                table: "Learners");

            migrationBuilder.DropColumn(
                name: "AppearanceJson",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AppearanceUpdatedAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AppearanceVersion",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AwardsCertificate",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CourseAssistantEnabled",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Overview",
                table: "Courses");
        }
    }
}
