using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Social.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SocialAddContentSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessSnapshots",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlaceSnapshots",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TourSnapshots",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OwnerProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSnapshots_BusinessId",
                schema: "social",
                table: "BusinessSnapshots",
                column: "BusinessId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSnapshots_IsDeleted",
                schema: "social",
                table: "BusinessSnapshots",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSnapshots_OwnerId",
                schema: "social",
                table: "BusinessSnapshots",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSnapshots_PlaceId",
                schema: "social",
                table: "BusinessSnapshots",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceSnapshots_IsDeleted",
                schema: "social",
                table: "PlaceSnapshots",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceSnapshots_PlaceId",
                schema: "social",
                table: "PlaceSnapshots",
                column: "PlaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourSnapshots_BusinessId",
                schema: "social",
                table: "TourSnapshots",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_TourSnapshots_IsDeleted",
                schema: "social",
                table: "TourSnapshots",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TourSnapshots_OwnerProviderId",
                schema: "social",
                table: "TourSnapshots",
                column: "OwnerProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_TourSnapshots_PlaceId",
                schema: "social",
                table: "TourSnapshots",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_TourSnapshots_TourId",
                schema: "social",
                table: "TourSnapshots",
                column: "TourId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessSnapshots",
                schema: "social");

            migrationBuilder.DropTable(
                name: "PlaceSnapshots",
                schema: "social");

            migrationBuilder.DropTable(
                name: "TourSnapshots",
                schema: "social");
        }
    }
}
