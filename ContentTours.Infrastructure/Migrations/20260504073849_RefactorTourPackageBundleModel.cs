using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentTours.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorTourPackageBundleModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TourPackages_Tours_TourId",
                schema: "content_tours",
                table: "TourPackages");

            migrationBuilder.DropIndex(
                name: "IX_TourPackageInclusions_TourPackageId",
                schema: "content_tours",
                table: "TourPackageInclusions");

            migrationBuilder.RenameColumn(
                name: "TourId",
                schema: "content_tours",
                table: "TourPackages",
                newName: "CreatedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_TourPackages_TourId",
                schema: "content_tours",
                table: "TourPackages",
                newName: "IX_TourPackages_CreatedByUserId");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                schema: "content_tours",
                table: "TourPackageInclusions",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TourPackageTours",
                schema: "content_tours",
                columns: table => new
                {
                    TourPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackageTours", x => new { x.TourPackageId, x.TourId });
                    table.ForeignKey(
                        name: "FK_TourPackageTours_TourPackages_TourPackageId",
                        column: x => x.TourPackageId,
                        principalSchema: "content_tours",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TourPackageTours_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "content_tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TourPackages_ValidTo",
                schema: "content_tours",
                table: "TourPackages",
                column: "ValidTo");

            migrationBuilder.CreateIndex(
                name: "UX_TourPackageInclusions_PackageId_Description",
                schema: "content_tours",
                table: "TourPackageInclusions",
                columns: new[] { "TourPackageId", "Description" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageTours_TourId",
                schema: "content_tours",
                table: "TourPackageTours",
                column: "TourId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TourPackageTours",
                schema: "content_tours");

            migrationBuilder.DropIndex(
                name: "IX_TourPackages_ValidTo",
                schema: "content_tours",
                table: "TourPackages");

            migrationBuilder.DropIndex(
                name: "UX_TourPackageInclusions_PackageId_Description",
                schema: "content_tours",
                table: "TourPackageInclusions");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                schema: "content_tours",
                table: "TourPackages",
                newName: "TourId");

            migrationBuilder.RenameIndex(
                name: "IX_TourPackages_CreatedByUserId",
                schema: "content_tours",
                table: "TourPackages",
                newName: "IX_TourPackages_TourId");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                schema: "content_tours",
                table: "TourPackageInclusions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageInclusions_TourPackageId",
                schema: "content_tours",
                table: "TourPackageInclusions",
                column: "TourPackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_TourPackages_Tours_TourId",
                schema: "content_tours",
                table: "TourPackages",
                column: "TourId",
                principalSchema: "content_tours",
                principalTable: "Tours",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
