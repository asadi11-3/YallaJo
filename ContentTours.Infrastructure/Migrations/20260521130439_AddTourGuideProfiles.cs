using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentTours.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTourGuideProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TourGuides",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    HasFirstAid = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MoTALicenseNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false, defaultValue: 0m),
                    ReviewCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourGuides", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TourGuideLanguages",
                schema: "content_tours",
                columns: table => new
                {
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Proficiency = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourGuideLanguages", x => new { x.TourGuideId, x.LanguageId });
                    table.ForeignKey(
                        name: "FK_TourGuideLanguages_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalSchema: "content_tours",
                        principalTable: "TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourGuideSpecializations",
                schema: "content_tours",
                columns: table => new
                {
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpecializationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourGuideSpecializations", x => new { x.TourGuideId, x.SpecializationId });
                    table.ForeignKey(
                        name: "FK_TourGuideSpecializations_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalSchema: "content_tours",
                        principalTable: "TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TourGuides_IsActive",
                schema: "content_tours",
                table: "TourGuides",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TourGuides_UserId",
                schema: "content_tours",
                table: "TourGuides",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TourGuideLanguages",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourGuideSpecializations",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourGuides",
                schema: "content_tours");
        }
    }
}
