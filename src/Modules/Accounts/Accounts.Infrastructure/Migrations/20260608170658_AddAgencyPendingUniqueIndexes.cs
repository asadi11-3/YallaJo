using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgencyPendingUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_AgencyInvitations_Pending_Agency_Guide",
                schema: "accounts",
                table: "AgencyInvitations",
                columns: new[] { "AgencyUserId", "GuideUserId" },
                unique: true,
                filter: "[Status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "UX_AgencyApplications_Pending_Guide_Agency",
                schema: "accounts",
                table: "AgencyApplications",
                columns: new[] { "GuideUserId", "AgencyUserId" },
                unique: true,
                filter: "[Status] = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_AgencyInvitations_Pending_Agency_Guide",
                schema: "accounts",
                table: "AgencyInvitations");

            migrationBuilder.DropIndex(
                name: "UX_AgencyApplications_Pending_Guide_Agency",
                schema: "accounts",
                table: "AgencyApplications");
        }
    }
}
