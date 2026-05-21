using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Social.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SocialAddAggregateRootAndAuditMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_BusinessId",
                schema: "social",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_PlaceId",
                schema: "social",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_TourGuideId",
                schema: "social",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_TourId",
                schema: "social",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_UserId_EntityType_EntityId",
                schema: "social",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                schema: "social",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "PlaceId",
                schema: "social",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "TourGuideId",
                schema: "social",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "TourId",
                schema: "social",
                table: "Reviews");

            migrationBuilder.RenameColumn(
                name: "IsVerified",
                schema: "social",
                table: "Reviews",
                newName: "ProfanityFlagged");

            migrationBuilder.RenameColumn(
                name: "IsReported",
                schema: "social",
                table: "Reviews",
                newName: "IsVerifiedBooking");

            migrationBuilder.RenameColumn(
                name: "HelpfulCount",
                schema: "social",
                table: "Reviews",
                newName: "CurrentReportCount");

            migrationBuilder.RenameColumn(
                name: "Reason",
                schema: "social",
                table: "ContentModerationLogs",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "OccurredAt",
                schema: "social",
                table: "ContentModerationLogs",
                newName: "ActionedAt");

            migrationBuilder.RenameColumn(
                name: "ModeratorUserId",
                schema: "social",
                table: "ContentModerationLogs",
                newName: "AdminUserId");

            migrationBuilder.AddColumn<DateTime>(
                name: "AutoHiddenAt",
                schema: "social",
                table: "Reviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEditedAt",
                schema: "social",
                table: "Reviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                schema: "social",
                table: "Reviews",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<Guid>(
                name: "TargetId",
                schema: "social",
                table: "Reviews",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte>(
                name: "TargetType",
                schema: "social",
                table: "Reviews",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                schema: "social",
                table: "Reports",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<byte>(
                name: "Reason",
                schema: "social",
                table: "Reports",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<byte>(
                name: "EntityType",
                schema: "social",
                table: "Reports",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "social",
                table: "Reports",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<byte>(
                name: "ResolutionAction",
                schema: "social",
                table: "Reports",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                schema: "social",
                table: "Reports",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<byte>(
                name: "EntityType",
                schema: "social",
                table: "Favorites",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<DateTime>(
                name: "AddedAt",
                schema: "social",
                table: "Favorites",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "social",
                table: "Favorites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "social",
                table: "Favorites",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "social",
                table: "Favorites",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<byte>(
                name: "EntityType",
                schema: "social",
                table: "ContentModerationLogs",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<byte>(
                name: "Action",
                schema: "social",
                table: "ContentModerationLogs",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceReportId",
                schema: "social",
                table: "ContentModerationLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookingEligibilitySnapshots",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetType = table.Column<byte>(type: "tinyint", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedBookingCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingEligibilitySnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntityRatingCaches",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetType = table.Column<byte>(type: "tinyint", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    BayesianScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    LastRecalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityRatingCaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "social",
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
                name: "ProfanityBlocklistEntries",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Word = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfanityBlocklistEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReviewReplies",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastEditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewReplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewReplies_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalSchema: "social",
                        principalTable: "Reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Status_CreatedAt",
                schema: "social",
                table: "Reviews",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TargetType_TargetId",
                schema: "social",
                table: "Reviews",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_User_Target_Unique",
                schema: "social",
                table: "Reviews",
                columns: new[] { "UserId", "TargetType", "TargetId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterUserId",
                schema: "social",
                table: "Reports",
                column: "ReporterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Status_CreatedAt",
                schema: "social",
                table: "Reports",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_EntityType_EntityId",
                schema: "social",
                table: "Favorites",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_User_Entity_Unique",
                schema: "social",
                table: "Favorites",
                columns: new[] { "UserId", "EntityType", "EntityId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ContentModerationLogs_ActionedAt",
                schema: "social",
                table: "ContentModerationLogs",
                column: "ActionedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentModerationLogs_AdminUserId",
                schema: "social",
                table: "ContentModerationLogs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingEligibilitySnapshots_User_Target_Unique",
                schema: "social",
                table: "BookingEligibilitySnapshots",
                columns: new[] { "UserId", "TargetType", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingEligibilitySnapshots_UserId",
                schema: "social",
                table: "BookingEligibilitySnapshots",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityRatingCaches_LastRecalculatedAt",
                schema: "social",
                table: "EntityRatingCaches",
                column: "LastRecalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EntityRatingCaches_TargetType_TargetId_Unique",
                schema: "social",
                table: "EntityRatingCaches",
                columns: new[] { "TargetType", "TargetId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProfanityBlocklistEntries_LanguageCode",
                schema: "social",
                table: "ProfanityBlocklistEntries",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_ProfanityBlocklistEntries_Word_Language_Unique",
                schema: "social",
                table: "ProfanityBlocklistEntries",
                columns: new[] { "Word", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewReplies_ReviewId",
                schema: "social",
                table: "ReviewReplies",
                column: "ReviewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingEligibilitySnapshots",
                schema: "social");

            migrationBuilder.DropTable(
                name: "EntityRatingCaches",
                schema: "social");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "social");

            migrationBuilder.DropTable(
                name: "ProfanityBlocklistEntries",
                schema: "social");

            migrationBuilder.DropTable(
                name: "ReviewReplies",
                schema: "social");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_Status_CreatedAt",
                schema: "social",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_TargetType_TargetId",
                schema: "social",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_User_Target_Unique",
                schema: "social",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterUserId",
                schema: "social",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_Status_CreatedAt",
                schema: "social",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_EntityType_EntityId",
                schema: "social",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_User_Entity_Unique",
                schema: "social",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_ContentModerationLogs_ActionedAt",
                schema: "social",
                table: "ContentModerationLogs");

            migrationBuilder.DropIndex(
                name: "IX_ContentModerationLogs_AdminUserId",
                schema: "social",
                table: "ContentModerationLogs");

            migrationBuilder.DropColumn(
                name: "AutoHiddenAt",
                schema: "social",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "LastEditedAt",
                schema: "social",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "social",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "TargetId",
                schema: "social",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "TargetType",
                schema: "social",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ResolutionAction",
                schema: "social",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                schema: "social",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "AddedAt",
                schema: "social",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "social",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "social",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "social",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "SourceReportId",
                schema: "social",
                table: "ContentModerationLogs");

            migrationBuilder.RenameColumn(
                name: "ProfanityFlagged",
                schema: "social",
                table: "Reviews",
                newName: "IsVerified");

            migrationBuilder.RenameColumn(
                name: "IsVerifiedBooking",
                schema: "social",
                table: "Reviews",
                newName: "IsReported");

            migrationBuilder.RenameColumn(
                name: "CurrentReportCount",
                schema: "social",
                table: "Reviews",
                newName: "HelpfulCount");

            migrationBuilder.RenameColumn(
                name: "Notes",
                schema: "social",
                table: "ContentModerationLogs",
                newName: "Reason");

            migrationBuilder.RenameColumn(
                name: "AdminUserId",
                schema: "social",
                table: "ContentModerationLogs",
                newName: "ModeratorUserId");

            migrationBuilder.RenameColumn(
                name: "ActionedAt",
                schema: "social",
                table: "ContentModerationLogs",
                newName: "OccurredAt");

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId",
                schema: "social",
                table: "Reviews",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlaceId",
                schema: "social",
                table: "Reviews",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TourGuideId",
                schema: "social",
                table: "Reviews",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TourId",
                schema: "social",
                table: "Reviews",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "social",
                table: "Reports",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldDefaultValue: (byte)0);

            migrationBuilder.AlterColumn<int>(
                name: "Reason",
                schema: "social",
                table: "Reports",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                schema: "social",
                table: "Reports",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "social",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                schema: "social",
                table: "Favorites",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                schema: "social",
                table: "ContentModerationLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<int>(
                name: "Action",
                schema: "social",
                table: "ContentModerationLogs",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BusinessId",
                schema: "social",
                table: "Reviews",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_PlaceId",
                schema: "social",
                table: "Reviews",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TourGuideId",
                schema: "social",
                table: "Reviews",
                column: "TourGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TourId",
                schema: "social",
                table: "Reviews",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_EntityType_EntityId",
                schema: "social",
                table: "Favorites",
                columns: new[] { "UserId", "EntityType", "EntityId" },
                unique: true);
        }
    }
}
