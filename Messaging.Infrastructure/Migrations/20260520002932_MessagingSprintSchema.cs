using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Messaging.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MessagingSprintSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationTemplates_Code",
                schema: "messaging",
                table: "NotificationTemplates");

            migrationBuilder.DropIndex(
                name: "IX_DeviceTokens_UserId_IsActive",
                schema: "messaging",
                table: "DeviceTokens");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "messaging",
                table: "NotificationTemplates");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "messaging",
                table: "NotificationTemplates");

            migrationBuilder.DropColumn(
                name: "Subject",
                schema: "messaging",
                table: "NotificationTemplates");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                schema: "messaging",
                table: "DeviceTokens");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "messaging",
                table: "DeviceTokens");

            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                schema: "messaging",
                table: "DeviceTokens");

            migrationBuilder.RenameColumn(
                name: "SenderUserId",
                schema: "messaging",
                table: "TicketMessages",
                newName: "AuthorUserId");

            migrationBuilder.RenameColumn(
                name: "Message",
                schema: "messaging",
                table: "TicketMessages",
                newName: "Body");

            migrationBuilder.RenameColumn(
                name: "IsStaffReply",
                schema: "messaging",
                table: "TicketMessages",
                newName: "IsInternal");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "messaging",
                table: "SupportTickets",
                newName: "CreatedByUserId");

            migrationBuilder.RenameColumn(
                name: "Description",
                schema: "messaging",
                table: "SupportTickets",
                newName: "InitialBody");

            migrationBuilder.RenameIndex(
                name: "IX_SupportTickets_UserId_Status",
                schema: "messaging",
                table: "SupportTickets",
                newName: "IX_SupportTickets_CreatedByUserId_Status");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "messaging",
                table: "NotificationTemplates",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "BodyTemplate",
                schema: "messaging",
                table: "NotificationTemplates",
                newName: "Body");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                schema: "messaging",
                table: "SupportTickets",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                schema: "messaging",
                table: "SupportTickets",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<byte>(
                name: "Priority",
                schema: "messaging",
                table: "SupportTickets",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<byte>(
                name: "Category",
                schema: "messaging",
                table: "SupportTickets",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                schema: "messaging",
                table: "SupportTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNotes",
                schema: "messaging",
                table: "SupportTickets",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResolvedByUserId",
                schema: "messaging",
                table: "SupportTickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaBreachAt",
                schema: "messaging",
                table: "SupportTickets",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<byte>(
                name: "Type",
                schema: "messaging",
                table: "NotificationTemplates",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<byte>(
                name: "Channel",
                schema: "messaging",
                table: "NotificationTemplates",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "HtmlBody",
                schema: "messaging",
                table: "NotificationTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                schema: "messaging",
                table: "NotificationTemplates",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExternalRef",
                schema: "messaging",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                schema: "messaging",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "Platform",
                schema: "messaging",
                table: "DeviceTokens",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                schema: "messaging",
                table: "DeviceTokens",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                schema: "messaging",
                table: "DeviceTokens",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "AdminAssignmentRosters",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LastAssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsOnLeave = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAssignmentRosters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveryAttempts",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<byte>(type: "tinyint", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExternalRef = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveryAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSnapshots",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false, defaultValue: "en"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_SlaBreachAt",
                schema: "messaging",
                table: "SupportTickets",
                column: "SlaBreachAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Type_Channel_LanguageCode",
                schema: "messaging",
                table: "NotificationTemplates",
                columns: new[] { "Type", "Channel", "LanguageCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_LastSeenAt",
                schema: "messaging",
                table: "DeviceTokens",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_UserId_DeviceId",
                schema: "messaging",
                table: "DeviceTokens",
                columns: new[] { "UserId", "DeviceId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAssignmentRosters_AdminUserId",
                schema: "messaging",
                table: "AdminAssignmentRosters",
                column: "AdminUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminAssignmentRosters_IsActive_IsOnLeave_LastAssignedAt",
                schema: "messaging",
                table: "AdminAssignmentRosters",
                columns: new[] { "IsActive", "IsOnLeave", "LastAssignedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveryAttempts_NotificationId_Channel",
                schema: "messaging",
                table: "NotificationDeliveryAttempts",
                columns: new[] { "NotificationId", "Channel" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveryAttempts_Status_AttemptedAt",
                schema: "messaging",
                table: "NotificationDeliveryAttempts",
                columns: new[] { "Status", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSnapshots_UserId",
                schema: "messaging",
                table: "UserSnapshots",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAssignmentRosters",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "NotificationDeliveryAttempts",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "UserSnapshots",
                schema: "messaging");

            migrationBuilder.DropIndex(
                name: "IX_SupportTickets_SlaBreachAt",
                schema: "messaging",
                table: "SupportTickets");

            migrationBuilder.DropIndex(
                name: "IX_NotificationTemplates_Type_Channel_LanguageCode",
                schema: "messaging",
                table: "NotificationTemplates");

            migrationBuilder.DropIndex(
                name: "IX_DeviceTokens_LastSeenAt",
                schema: "messaging",
                table: "DeviceTokens");

            migrationBuilder.DropIndex(
                name: "IX_DeviceTokens_UserId_DeviceId",
                schema: "messaging",
                table: "DeviceTokens");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                schema: "messaging",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "ResolutionNotes",
                schema: "messaging",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "ResolvedByUserId",
                schema: "messaging",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "SlaBreachAt",
                schema: "messaging",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "HtmlBody",
                schema: "messaging",
                table: "NotificationTemplates");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                schema: "messaging",
                table: "NotificationTemplates");

            migrationBuilder.DropColumn(
                name: "ExternalRef",
                schema: "messaging",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                schema: "messaging",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                schema: "messaging",
                table: "DeviceTokens");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                schema: "messaging",
                table: "DeviceTokens");

            migrationBuilder.RenameColumn(
                name: "IsInternal",
                schema: "messaging",
                table: "TicketMessages",
                newName: "IsStaffReply");

            migrationBuilder.RenameColumn(
                name: "Body",
                schema: "messaging",
                table: "TicketMessages",
                newName: "Message");

            migrationBuilder.RenameColumn(
                name: "AuthorUserId",
                schema: "messaging",
                table: "TicketMessages",
                newName: "SenderUserId");

            migrationBuilder.RenameColumn(
                name: "InitialBody",
                schema: "messaging",
                table: "SupportTickets",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                schema: "messaging",
                table: "SupportTickets",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_SupportTickets_CreatedByUserId_Status",
                schema: "messaging",
                table: "SupportTickets",
                newName: "IX_SupportTickets_UserId_Status");

            migrationBuilder.RenameColumn(
                name: "Title",
                schema: "messaging",
                table: "NotificationTemplates",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Body",
                schema: "messaging",
                table: "NotificationTemplates",
                newName: "BodyTemplate");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                schema: "messaging",
                table: "SupportTickets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "messaging",
                table: "SupportTickets",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldDefaultValue: (byte)0);

            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                schema: "messaging",
                table: "SupportTickets",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                schema: "messaging",
                table: "SupportTickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                schema: "messaging",
                table: "NotificationTemplates",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<int>(
                name: "Channel",
                schema: "messaging",
                table: "NotificationTemplates",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "messaging",
                table: "NotificationTemplates",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "messaging",
                table: "NotificationTemplates",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                schema: "messaging",
                table: "NotificationTemplates",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Platform",
                schema: "messaging",
                table: "DeviceTokens",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                schema: "messaging",
                table: "DeviceTokens",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "messaging",
                table: "DeviceTokens",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedAt",
                schema: "messaging",
                table: "DeviceTokens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Code",
                schema: "messaging",
                table: "NotificationTemplates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_UserId_IsActive",
                schema: "messaging",
                table: "DeviceTokens",
                columns: new[] { "UserId", "IsActive" });
        }
    }
}
