using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "booking");

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TraceContext = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackageBookings",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ParticipantCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    TotalPrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalPriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "JOD"),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SpecialRequests = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageBookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefundPolicies",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FullRefundHours = table.Column<int>(type: "int", nullable: false),
                    PartialRefundHours = table.Column<int>(type: "int", nullable: false),
                    PartialRefundPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvailabilitySlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReservationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReservationTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    PartySize = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    TotalPrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalPriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SpecialRequests = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TourBookings",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AvailabilitySlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    ParticipantCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    TotalPrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalPriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "JOD"),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfirmationCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    PointsRedeemed = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PointsDiscount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    PointsDiscountCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ReferralDiscount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    ReferralDiscountCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SpecialRequests = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourBookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TourGuides",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false, defaultValue: 0m),
                    ReviewCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CompletedTourCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    HourlyRate = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: true),
                    ResponseTimeMinutes = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourGuides", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JoinRequests",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ParticipantCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResponseMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JoinRequests_TourBookings_TourBookingId",
                        column: x => x.TourBookingId,
                        principalSchema: "booking",
                        principalTable: "TourBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AvailabilitySlots",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlotType = table.Column<int>(type: "int", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BookedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LockedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PriceOverride = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    PriceOverrideCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilitySlots", x => x.Id);
                    table.CheckConstraint("CK_AvailSlots_Capacity", "[BookedCount] + [LockedCount] <= [MaxCapacity]");
                    table.ForeignKey(
                        name: "FK_AvailabilitySlots_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalSchema: "booking",
                        principalTable: "TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProviderDocuments",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    DocumentUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderDocuments", x => x.Id);
                    table.CheckConstraint("CK_ProviderDocuments_SingleTarget", "(CASE WHEN [TourGuideId] IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN [BusinessId] IS NOT NULL THEN 1 ELSE 0 END) = 1");
                    table.ForeignKey(
                        name: "FK_ProviderDocuments_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalSchema: "booking",
                        principalTable: "TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourGuideLanguages",
                schema: "booking",
                columns: table => new
                {
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProficiencyLevel = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourGuideLanguages", x => new { x.TourGuideId, x.LanguageId });
                    table.ForeignKey(
                        name: "FK_TourGuideLanguages_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalSchema: "booking",
                        principalTable: "TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourGuideSpecializations",
                schema: "booking",
                columns: table => new
                {
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpecializationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourGuideSpecializations", x => new { x.TourGuideId, x.SpecializationId });
                    table.ForeignKey(
                        name: "FK_TourGuideSpecializations_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalSchema: "booking",
                        principalTable: "TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlotLocks",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvailabilitySlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsReleased = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlotLocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlotLocks_AvailabilitySlots_AvailabilitySlotId",
                        column: x => x.AvailabilitySlotId,
                        principalSchema: "booking",
                        principalTable: "AvailabilitySlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlots_IsActive",
                schema: "booking",
                table: "AvailabilitySlots",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlots_TourGuideId_Date_StartTime_EndTime",
                schema: "booking",
                table: "AvailabilitySlots",
                columns: new[] { "TourGuideId", "Date", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_JoinRequests_Status",
                schema: "booking",
                table: "JoinRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JoinRequests_TourBookingId_UserId",
                schema: "booking",
                table: "JoinRequests",
                columns: new[] { "TourBookingId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Unprocessed",
                schema: "booking",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "RetryCount", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageBookings_Status",
                schema: "booking",
                table: "PackageBookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PackageBookings_UserId_BookingDate",
                schema: "booking",
                table: "PackageBookings",
                columns: new[] { "UserId", "BookingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocuments_BusinessId",
                schema: "booking",
                table: "ProviderDocuments",
                column: "BusinessId",
                filter: "[BusinessId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocuments_TourGuideId_DocumentType",
                schema: "booking",
                table: "ProviderDocuments",
                columns: new[] { "TourGuideId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_RefundPolicies_IsActive",
                schema: "booking",
                table: "RefundPolicies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RefundPolicies_IsDefault",
                schema: "booking",
                table: "RefundPolicies",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BusinessId_ReservationDate_ReservationTime",
                schema: "booking",
                table: "Reservations",
                columns: new[] { "BusinessId", "ReservationDate", "ReservationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Status",
                schema: "booking",
                table: "Reservations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SlotLocks_AvailabilitySlotId_IsReleased",
                schema: "booking",
                table: "SlotLocks",
                columns: new[] { "AvailabilitySlotId", "IsReleased" });

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_ConfirmationCode",
                schema: "booking",
                table: "TourBookings",
                column: "ConfirmationCode",
                unique: true,
                filter: "[ConfirmationCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_Status",
                schema: "booking",
                table: "TourBookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_UserId_ScheduledDate",
                schema: "booking",
                table: "TourBookings",
                columns: new[] { "UserId", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TourGuides_IsActive",
                schema: "booking",
                table: "TourGuides",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TourGuides_UserId",
                schema: "booking",
                table: "TourGuides",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JoinRequests",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "PackageBookings",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "ProviderDocuments",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "RefundPolicies",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "Reservations",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "SlotLocks",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "TourGuideLanguages",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "TourGuideSpecializations",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "TourBookings",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "AvailabilitySlots",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "TourGuides",
                schema: "booking");
        }
    }
}
