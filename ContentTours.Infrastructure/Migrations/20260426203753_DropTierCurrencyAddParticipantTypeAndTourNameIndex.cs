using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentTours.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropTierCurrencyAddParticipantTypeAndTourNameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                schema: "content_tours",
                table: "TourPricingTiers");

            migrationBuilder.AddColumn<byte>(
                name: "ParticipantType",
                schema: "content_tours",
                table: "TourPricingTiers",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)255);

            migrationBuilder.CreateTable(
                name: "TourPricingTierTranslations",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPricingTierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPricingTierTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPricingTierTranslations_TourPricingTiers_TourPricingTierId",
                        column: x => x.TourPricingTierId,
                        principalSchema: "content_tours",
                        principalTable: "TourPricingTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TourPricingTierTranslations_TourPricingTierId_LanguageCode",
                schema: "content_tours",
                table: "TourPricingTierTranslations",
                columns: new[] { "TourPricingTierId", "LanguageCode" },
                unique: true);

            // Q9: Index on Tour.Name to support SuggestTours LIKE 'prefix%' with index seek
            migrationBuilder.CreateIndex(
                name: "IX_Tours_Name",
                schema: "content_tours",
                table: "Tours",
                column: "Name");

            // BUG-24: Backfill ParticipantType for existing tiers whose Name was "Adult" (case-insensitive).
            // Without this, all pre-migration tiers get ParticipantType=255 (Other), breaking submit gate.
            // Value 1 = ParticipantType.Adult
            migrationBuilder.Sql("""
                UPDATE [content_tours].[TourPricingTiers]
                SET    [ParticipantType] = 1
                WHERE  [ParticipantType] = 255
                  AND  LOWER([Name]) IN (N'adult', N'adults');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tours_Name",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropTable(
                name: "TourPricingTierTranslations",
                schema: "content_tours");

            migrationBuilder.DropColumn(
                name: "ParticipantType",
                schema: "content_tours",
                table: "TourPricingTiers");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                schema: "content_tours",
                table: "TourPricingTiers",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }
    }
}
