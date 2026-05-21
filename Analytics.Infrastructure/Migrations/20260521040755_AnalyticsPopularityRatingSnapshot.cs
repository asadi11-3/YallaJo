using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AnalyticsPopularityRatingSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AverageRatingSnapshot",
                schema: "analytics",
                table: "PopularityScores",
                type: "decimal(3,2)",
                precision: 3,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCountSnapshot",
                schema: "analytics",
                table: "PopularityScores",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageRatingSnapshot",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropColumn(
                name: "ReviewCountSnapshot",
                schema: "analytics",
                table: "PopularityScores");
        }
    }
}
