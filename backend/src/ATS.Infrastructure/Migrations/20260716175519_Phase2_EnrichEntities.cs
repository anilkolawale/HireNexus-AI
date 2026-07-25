using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_EnrichEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DraftLetterText",
                table: "Offers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiGeneratedDescription",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosingDate",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RemoteOption",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TotalPositions",
                table: "Jobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AiRecommendation",
                table: "Feedbacks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "Feedbacks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AiProfileScore",
                table: "Candidates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiProfileSummary",
                table: "Candidates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableFrom",
                table: "Candidates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationPreference",
                table: "Candidates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NoticePeriodDays",
                table: "Candidates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "YearsOfTotalExperience",
                table: "Candidates",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DraftLetterText",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "AiGeneratedDescription",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ClosingDate",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "RemoteOption",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "TotalPositions",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "AiRecommendation",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "AiProfileScore",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "AiProfileSummary",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "AvailableFrom",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "LocationPreference",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "NoticePeriodDays",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "YearsOfTotalExperience",
                table: "Candidates");
        }
    }
}
