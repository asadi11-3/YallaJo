using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromoBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PromoBlocks",
                schema: "content_core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlacementKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ImageUrl = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: true),
                    AttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ButtonText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ButtonUrl = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: true),
                    BadgeText = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    IconName = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoBlocks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromoBlocks_PlacementKey",
                schema: "content_core",
                table: "PromoBlocks",
                column: "PlacementKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromoBlocks",
                schema: "content_core");
        }
    }
}
