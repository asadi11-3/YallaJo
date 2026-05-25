using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgencyEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MarketingConsentEmailDigest",
                schema: "accounts",
                table: "Profiles",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MarketingConsentLastUpdatedUtc",
                schema: "accounts",
                table: "Profiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MarketingConsentPushNotifications",
                schema: "accounts",
                table: "Profiles",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MarketingConsentReEngagementCampaigns",
                schema: "accounts",
                table: "Profiles",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgencyAffiliations",
                schema: "accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgencyUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuideUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommissionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TerminatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TerminatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TerminationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencyAffiliations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgencyApplications",
                schema: "accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuideUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgencyUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencyApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgencyInvitations",
                schema: "accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgencyUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuideUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProposedCommissionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencyInvitations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgencyAffiliations_AgencyUserId",
                schema: "accounts",
                table: "AgencyAffiliations",
                column: "AgencyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgencyAffiliations_GuideUserId",
                schema: "accounts",
                table: "AgencyAffiliations",
                column: "GuideUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgencyAffiliations_GuideUserId_Status",
                schema: "accounts",
                table: "AgencyAffiliations",
                columns: new[] { "GuideUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AgencyApplications_AgencyUserId_Status",
                schema: "accounts",
                table: "AgencyApplications",
                columns: new[] { "AgencyUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AgencyApplications_GuideUserId",
                schema: "accounts",
                table: "AgencyApplications",
                column: "GuideUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgencyInvitations_AgencyUserId",
                schema: "accounts",
                table: "AgencyInvitations",
                column: "AgencyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgencyInvitations_ExpiresAt",
                schema: "accounts",
                table: "AgencyInvitations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgencyInvitations_GuideUserId_Status",
                schema: "accounts",
                table: "AgencyInvitations",
                columns: new[] { "GuideUserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgencyAffiliations",
                schema: "accounts");

            migrationBuilder.DropTable(
                name: "AgencyApplications",
                schema: "accounts");

            migrationBuilder.DropTable(
                name: "AgencyInvitations",
                schema: "accounts");

            migrationBuilder.DropColumn(
                name: "MarketingConsentEmailDigest",
                schema: "accounts",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "MarketingConsentLastUpdatedUtc",
                schema: "accounts",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "MarketingConsentPushNotifications",
                schema: "accounts",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "MarketingConsentReEngagementCampaigns",
                schema: "accounts",
                table: "Profiles");
        }
    }
}
