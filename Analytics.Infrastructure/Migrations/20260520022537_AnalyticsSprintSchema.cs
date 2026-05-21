using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AnalyticsSprintSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserInteractions_OccurredAt",
                schema: "analytics",
                table: "UserInteractions");

            migrationBuilder.DropIndex(
                name: "IX_PopularityScores_EntityType_EntityId",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                schema: "analytics",
                table: "UserInteractions");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                schema: "analytics",
                table: "UserInteractions");

            migrationBuilder.DropColumn(
                name: "Latitude",
                schema: "analytics",
                table: "UserInteractions");

            migrationBuilder.DropColumn(
                name: "Longitude",
                schema: "analytics",
                table: "UserInteractions");

            migrationBuilder.DropColumn(
                name: "BookingCount",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropColumn(
                name: "BookmarkCount",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropColumn(
                name: "LastCalculatedAt",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropColumn(
                name: "ReviewScore",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropColumn(
                name: "ShareCount",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropColumn(
                name: "TrendingScore",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                schema: "analytics",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "OldValues",
                schema: "analytics",
                table: "AuditLogs",
                newName: "RedactionReason");

            migrationBuilder.RenameColumn(
                name: "NewValues",
                schema: "analytics",
                table: "AuditLogs",
                newName: "OldValue");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "analytics",
                table: "UserInteractions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "SessionId",
                schema: "analytics",
                table: "UserInteractions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "InteractionType",
                schema: "analytics",
                table: "UserInteractions",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<byte>(
                name: "EntityType",
                schema: "analytics",
                table: "UserInteractions",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "ClientIpHash",
                schema: "analytics",
                table: "UserInteractions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                schema: "analytics",
                table: "UserInteractions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "analytics",
                table: "PopularityScores",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<byte>(
                name: "EntityType",
                schema: "analytics",
                table: "PopularityScores",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<decimal>(
                name: "CategoryRankPercentile",
                schema: "analytics",
                table: "PopularityScores",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InteractionCountSnapshot",
                schema: "analytics",
                table: "PopularityScores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsStale",
                schema: "analytics",
                table: "PopularityScores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRecalculatedAt",
                schema: "analytics",
                table: "PopularityScores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                schema: "analytics",
                table: "PopularityScores",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TrendingRank",
                schema: "analytics",
                table: "PopularityScores",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                schema: "analytics",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<Guid>(
                name: "EntityId",
                schema: "analytics",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "Action",
                schema: "analytics",
                table: "AuditLogs",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                schema: "analytics",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomActionName",
                schema: "analytics",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddressHash",
                schema: "analytics",
                table: "AuditLogs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewValue",
                schema: "analytics",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RedactedAt",
                schema: "analytics",
                table: "AuditLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RedactedByUserId",
                schema: "analytics",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                schema: "analytics",
                table: "AuditLogs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookingSnapshots",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CreatedAtSnapshot = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DashboardCaches",
                schema: "analytics",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RebuiltAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardCaches", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "EntityPopularitySnapshots",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<byte>(type: "tinyint", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TakenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityPopularitySnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IngestDebounceMarkers",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<byte>(type: "tinyint", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastFlagged = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestDebounceMarkers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentSnapshots",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserInteractions_InteractionType",
                schema: "analytics",
                table: "UserInteractions",
                column: "InteractionType");

            migrationBuilder.CreateIndex(
                name: "IX_UserInteractions_OccurredAt_Id",
                schema: "analytics",
                table: "UserInteractions",
                columns: new[] { "OccurredAt", "Id" },
                descending: new[] { true, false });

            migrationBuilder.CreateIndex(
                name: "IX_PopularityScores_EntityType_EntityId",
                schema: "analytics",
                table: "PopularityScores",
                columns: new[] { "EntityType", "EntityId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PopularityScores_EntityType_TrendingRank",
                schema: "analytics",
                table: "PopularityScores",
                columns: new[] { "EntityType", "TrendingRank" });

            migrationBuilder.CreateIndex(
                name: "IX_PopularityScores_IsStale",
                schema: "analytics",
                table: "PopularityScores",
                column: "IsStale");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_RedactedAt",
                schema: "analytics",
                table: "AuditLogs",
                column: "RedactedAt",
                filter: "[RedactedAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSnapshots_BookingId",
                schema: "analytics",
                table: "BookingSnapshots",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingSnapshots_ProviderId",
                schema: "analytics",
                table: "BookingSnapshots",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityPopularitySnapshots_EntityType_EntityId_TakenAt",
                schema: "analytics",
                table: "EntityPopularitySnapshots",
                columns: new[] { "EntityType", "EntityId", "TakenAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestDebounceMarkers_EntityType_EntityId",
                schema: "analytics",
                table: "IngestDebounceMarkers",
                columns: new[] { "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSnapshots_PaymentId",
                schema: "analytics",
                table: "PaymentSnapshots",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSnapshots_ProviderId",
                schema: "analytics",
                table: "PaymentSnapshots",
                column: "ProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingSnapshots",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "DashboardCaches",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "EntityPopularitySnapshots",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "IngestDebounceMarkers",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "PaymentSnapshots",
                schema: "analytics");

            migrationBuilder.DropIndex(
                name: "IX_UserInteractions_InteractionType",
                schema: "analytics",
                table: "UserInteractions");

            migrationBuilder.DropIndex(
                name: "IX_UserInteractions_OccurredAt_Id",
                schema: "analytics",
                table: "UserInteractions");

            migrationBuilder.DropIndex(
                name: "IX_PopularityScores_EntityType_EntityId",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropIndex(
                name: "IX_PopularityScores_EntityType_TrendingRank",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropIndex(
                name: "IX_PopularityScores_IsStale",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_RedactedAt",
                schema: "analytics",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ClientIpHash",
                schema: "analytics",
                table: "UserInteractions");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                schema: "analytics",
                table: "UserInteractions");

            migrationBuilder.DropColumn(
                name: "CategoryRankPercentile",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropColumn(
                name: "InteractionCountSnapshot",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropColumn(
                name: "IsStale",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropColumn(
                name: "LastRecalculatedAt",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropColumn(
                name: "Score",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropColumn(
                name: "TrendingRank",
                schema: "analytics",
                table: "PopularityScores");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                schema: "analytics",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "CustomActionName",
                schema: "analytics",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IpAddressHash",
                schema: "analytics",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "NewValue",
                schema: "analytics",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "RedactedAt",
                schema: "analytics",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "RedactedByUserId",
                schema: "analytics",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Username",
                schema: "analytics",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "RedactionReason",
                schema: "analytics",
                table: "AuditLogs",
                newName: "OldValues");

            migrationBuilder.RenameColumn(
                name: "OldValue",
                schema: "analytics",
                table: "AuditLogs",
                newName: "NewValues");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "analytics",
                table: "UserInteractions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SessionId",
                schema: "analytics",
                table: "UserInteractions",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "InteractionType",
                schema: "analytics",
                table: "UserInteractions",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                schema: "analytics",
                table: "UserInteractions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                schema: "analytics",
                table: "UserInteractions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                schema: "analytics",
                table: "UserInteractions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                schema: "analytics",
                table: "UserInteractions",
                type: "decimal(10,8)",
                precision: 10,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                schema: "analytics",
                table: "UserInteractions",
                type: "decimal(11,8)",
                precision: 11,
                scale: 8,
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "analytics",
                table: "PopularityScores",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                schema: "analytics",
                table: "PopularityScores",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AddColumn<int>(
                name: "BookingCount",
                schema: "analytics",
                table: "PopularityScores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BookmarkCount",
                schema: "analytics",
                table: "PopularityScores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCalculatedAt",
                schema: "analytics",
                table: "PopularityScores",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "ReviewScore",
                schema: "analytics",
                table: "PopularityScores",
                type: "decimal(3,2)",
                precision: 3,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ShareCount",
                schema: "analytics",
                table: "PopularityScores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TrendingScore",
                schema: "analytics",
                table: "PopularityScores",
                type: "decimal(10,4)",
                precision: 10,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                schema: "analytics",
                table: "PopularityScores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                schema: "analytics",
                table: "AuditLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                schema: "analytics",
                table: "AuditLogs",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                schema: "analytics",
                table: "AuditLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                schema: "analytics",
                table: "AuditLogs",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserInteractions_OccurredAt",
                schema: "analytics",
                table: "UserInteractions",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_PopularityScores_EntityType_EntityId",
                schema: "analytics",
                table: "PopularityScores",
                columns: new[] { "EntityType", "EntityId" },
                unique: true);
        }
    }
}
