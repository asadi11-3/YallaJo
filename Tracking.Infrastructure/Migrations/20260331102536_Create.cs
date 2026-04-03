using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tracking");

            migrationBuilder.CreateTable(
                name: "LiveTrackingSessions",
                schema: "tracking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLocationUpdate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveTrackingSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "tracking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocationSnapshots",
                schema: "tracking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: false),
                    Accuracy = table.Column<double>(type: "float", nullable: false),
                    Speed = table.Column<double>(type: "float", nullable: true),
                    Heading = table.Column<double>(type: "float", nullable: true),
                    Altitude = table.Column<double>(type: "float", nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationSnapshots_LiveTrackingSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "tracking",
                        principalTable: "LiveTrackingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourCheckpoints",
                schema: "tracking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WaypointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReachedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourCheckpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourCheckpoints_LiveTrackingSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "tracking",
                        principalTable: "LiveTrackingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LiveTrackingSessions_TourBookingId",
                schema: "tracking",
                table: "LiveTrackingSessions",
                column: "TourBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveTrackingSessions_TourGuideId_Status",
                schema: "tracking",
                table: "LiveTrackingSessions",
                columns: new[] { "TourGuideId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LocationSnapshots_SessionId_CapturedAt",
                schema: "tracking",
                table: "LocationSnapshots",
                columns: new[] { "SessionId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Unprocessed",
                schema: "tracking",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "RetryCount", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TourCheckpoints_SessionId_WaypointId",
                schema: "tracking",
                table: "TourCheckpoints",
                columns: new[] { "SessionId", "WaypointId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationSnapshots",
                schema: "tracking");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "tracking");

            migrationBuilder.DropTable(
                name: "TourCheckpoints",
                schema: "tracking");

            migrationBuilder.DropTable(
                name: "LiveTrackingSessions",
                schema: "tracking");
        }
    }
}
