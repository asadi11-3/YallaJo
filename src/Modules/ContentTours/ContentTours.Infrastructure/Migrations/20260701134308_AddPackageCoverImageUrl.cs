using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentTours.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageCoverImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                schema: "content_tours",
                table: "TourPackages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                schema: "content_tours",
                table: "TourPackages");
        }
    }
}
