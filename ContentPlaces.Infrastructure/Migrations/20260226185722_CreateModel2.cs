using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentPlaces.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateModel2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAudioGuide",
                schema: "content_places",
                table: "Places",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasBrailleSignage",
                schema: "content_places",
                table: "Places",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWheelchairAccessible",
                schema: "content_places",
                table: "Places",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "content_places",
                table: "Businesses",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                schema: "content_places",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                schema: "content_places",
                table: "Businesses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "content_places",
                table: "Businesses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionTier",
                schema: "content_places",
                table: "Businesses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessAmenities",
                schema: "content_places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessAmenities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessAmenities_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "content_places",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessStaff",
                schema: "content_places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    JoinRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessStaff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessStaff_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "content_places",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceItems",
                schema: "content_places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    PriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    SalePrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    SalePriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    DiscountValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiscountValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceItems_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "content_places",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_Status",
                schema: "content_places",
                table: "Businesses",
                column: "Status",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAmenities_BusinessId",
                schema: "content_places",
                table: "BusinessAmenities",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessStaff_BusinessId",
                schema: "content_places",
                table: "BusinessStaff",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessStaff_BusinessId_UserId",
                schema: "content_places",
                table: "BusinessStaff",
                columns: new[] { "BusinessId", "UserId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessStaff_UserId",
                schema: "content_places",
                table: "BusinessStaff",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceItems_BusinessId",
                schema: "content_places",
                table: "ServiceItems",
                column: "BusinessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessAmenities",
                schema: "content_places");

            migrationBuilder.DropTable(
                name: "BusinessStaff",
                schema: "content_places");

            migrationBuilder.DropTable(
                name: "ServiceItems",
                schema: "content_places");

            migrationBuilder.DropIndex(
                name: "IX_Businesses_Status",
                schema: "content_places",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "HasAudioGuide",
                schema: "content_places",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "HasBrailleSignage",
                schema: "content_places",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "IsWheelchairAccessible",
                schema: "content_places",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "content_places",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                schema: "content_places",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                schema: "content_places",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "content_places",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "SubscriptionTier",
                schema: "content_places",
                table: "Businesses");
        }
    }
}
