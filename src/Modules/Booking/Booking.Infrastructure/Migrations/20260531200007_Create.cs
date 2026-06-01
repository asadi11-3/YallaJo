using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "booking");

            migrationBuilder.CreateTable(
                name: "GuideDiscounts",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuideUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxUsageCount = table.Column<int>(type: "int", nullable: true),
                    CurrentUsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuideDiscounts", x => x.Id);
                });

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
                name: "PricingTierSnapshots",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TierType = table.Column<byte>(type: "tinyint", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingTierSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProviderSnapshots",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderSnapshots", x => x.Id);
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
                name: "TourBookings",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvailabilitySlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParticipantCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Reference = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false, defaultValue: 0m),
                    LoyaltyAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false, defaultValue: 0m),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    LineItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefundPolicySnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrivate = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    JoinedFromBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsInstantBooking = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PaymentExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmationSource = table.Column<int>(type: "int", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationSource = table.Column<int>(type: "int", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefundAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SpecialRequests = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                name: "TourSnapshots",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsInstantBooking = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RefundPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RefundPolicySnapshotJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JoinRequests",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvailabilitySlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ParticipantCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResponseMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResultingBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    ExpiringNotificationSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiredNotificationSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspensionDispatchedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParticipantCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
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
                name: "IX_AvailabilitySlots_TourId_Date_Active",
                schema: "booking",
                table: "AvailabilitySlots",
                columns: new[] { "TourId", "Date" },
                filter: "[IsActive] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_GuideDiscounts_GuideUserId",
                schema: "booking",
                table: "GuideDiscounts",
                column: "GuideUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GuideDiscounts_GuideUserId_TourId",
                schema: "booking",
                table: "GuideDiscounts",
                columns: new[] { "GuideUserId", "TourId" });

            migrationBuilder.CreateIndex(
                name: "IX_GuideDiscounts_IsActive_ValidFrom_ValidUntil",
                schema: "booking",
                table: "GuideDiscounts",
                columns: new[] { "IsActive", "ValidFrom", "ValidUntil" });

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
                name: "IX_PricingTierSnapshots_TourId_TierType",
                schema: "booking",
                table: "PricingTierSnapshots",
                columns: new[] { "TourId", "TierType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocuments_BusinessId",
                schema: "booking",
                table: "ProviderDocuments",
                column: "BusinessId",
                filter: "[BusinessId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocuments_Status_ExpiresAt_ExpiredNotification",
                schema: "booking",
                table: "ProviderDocuments",
                columns: new[] { "Status", "ExpiresAt", "ExpiredNotificationSentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocuments_Status_ExpiresAt_ExpiringNotification",
                schema: "booking",
                table: "ProviderDocuments",
                columns: new[] { "Status", "ExpiresAt", "ExpiringNotificationSentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocuments_Suspension_Pending",
                schema: "booking",
                table: "ProviderDocuments",
                columns: new[] { "Status", "DocumentType", "SuspensionDispatchedAt" },
                filter: "[Status] = 3 AND [SuspensionDispatchedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ProviderDocuments_Business_Type_Active",
                schema: "booking",
                table: "ProviderDocuments",
                columns: new[] { "BusinessId", "DocumentType" },
                unique: true,
                filter: "[BusinessId] IS NOT NULL AND [Status] <> 2 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_ProviderDocuments_TourGuide_Type_Active",
                schema: "booking",
                table: "ProviderDocuments",
                columns: new[] { "TourGuideId", "DocumentType" },
                unique: true,
                filter: "[TourGuideId] IS NOT NULL AND [Status] <> 2 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSnapshots_OwnerUserId",
                schema: "booking",
                table: "ProviderSnapshots",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSnapshots_ProviderId",
                schema: "booking",
                table: "ProviderSnapshots",
                column: "ProviderId",
                unique: true);

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
                name: "IX_SlotLocks_AvailabilitySlotId_IsReleased",
                schema: "booking",
                table: "SlotLocks",
                columns: new[] { "AvailabilitySlotId", "IsReleased" });

            migrationBuilder.CreateIndex(
                name: "IX_SlotLocks_BookingId",
                schema: "booking",
                table: "SlotLocks",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_SlotLocks_ExpiresAt_Active",
                schema: "booking",
                table: "SlotLocks",
                column: "ExpiresAt",
                filter: "[IsReleased] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_SlotLock_User_Slot_Active",
                schema: "booking",
                table: "SlotLocks",
                columns: new[] { "UserId", "AvailabilitySlotId" },
                unique: true,
                filter: "[IsReleased] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_AvailabilitySlotId",
                schema: "booking",
                table: "TourBookings",
                column: "AvailabilitySlotId");

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_ProviderId_Status",
                schema: "booking",
                table: "TourBookings",
                columns: new[] { "ProviderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_Reference",
                schema: "booking",
                table: "TourBookings",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_Status",
                schema: "booking",
                table: "TourBookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_Status_UpdatedAt",
                schema: "booking",
                table: "TourBookings",
                columns: new[] { "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_TourId_Status",
                schema: "booking",
                table: "TourBookings",
                columns: new[] { "TourId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_UserId_Status",
                schema: "booking",
                table: "TourBookings",
                columns: new[] { "UserId", "Status" });

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

            migrationBuilder.CreateIndex(
                name: "IX_TourSnapshots_ProviderId",
                schema: "booking",
                table: "TourSnapshots",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_TourSnapshots_TourId",
                schema: "booking",
                table: "TourSnapshots",
                column: "TourId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuideDiscounts",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "JoinRequests",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "PricingTierSnapshots",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "ProviderDocuments",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "ProviderSnapshots",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "RefundPolicies",
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
                name: "TourSnapshots",
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
