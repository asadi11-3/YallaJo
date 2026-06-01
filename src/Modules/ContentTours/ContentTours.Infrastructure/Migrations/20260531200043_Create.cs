using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentTours.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "content_tours");

            migrationBuilder.CreateTable(
                name: "GuideApplications",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuideUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ProposedScheduleJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProposedBasePrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    RelevantExperience = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResubmissionCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReviewedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuideApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuidePricingTiers",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuideTourOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PriceAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    PriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    MinParticipants = table.Column<int>(type: "int", nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuidePricingTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuideSchedules",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuideTourOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<byte>(type: "tinyint", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuideSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "content_tours",
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
                name: "OutboxMessages",
                schema: "content_tours",
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
                name: "TourGuides",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LinkedProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    HasFirstAid = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MoTALicenseNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TrustTier = table.Column<int>(type: "int", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SuspensionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SuspendedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuspendedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false, defaultValue: 0m),
                    ReviewCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CompletedTourCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReportCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                name: "TourPackages",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    PriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "JOD"),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TourProposals",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuideUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ShortDescription = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxGroupSize = table.Column<int>(type: "int", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    RequestExclusive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReviewedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedTourId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tours",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxGroupSize = table.Column<int>(type: "int", nullable: false),
                    MinAge = table.Column<int>(type: "int", nullable: true),
                    BasePrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    BasePriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "JOD"),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: false),
                    MeetingPointLatitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: true),
                    MeetingPointLongitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false, defaultValue: 0m),
                    ReviewCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    BookingCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsInstantBooking = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CancellationPolicyHours = table.Column<int>(type: "int", nullable: false, defaultValue: 24),
                    MetaTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposedByGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsExclusive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    OwnershipType = table.Column<int>(type: "int", nullable: false),
                    IsOpenForApplications = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsChildFriendly = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsAccessible = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AgeRestriction = table.Column<int>(type: "int", nullable: true),
                    AllowsChildren = table.Column<bool>(type: "bit", nullable: false),
                    MinChildAge = table.Column<int>(type: "int", nullable: true),
                    MaxChildAge = table.Column<int>(type: "int", nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    SalePrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    SalePriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    DiscountValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiscountValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SuspendedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspensionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReinstatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuideAvailabilityBlocks",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuideAvailabilityBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuideAvailabilityBlocks_TourGuides_GuideId",
                        column: x => x.GuideId,
                        principalSchema: "content_tours",
                        principalTable: "TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourGuideLanguages",
                schema: "content_tours",
                columns: table => new
                {
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Proficiency = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourGuideLanguages", x => new { x.TourGuideId, x.LanguageId });
                    table.ForeignKey(
                        name: "FK_TourGuideLanguages_TourGuides_TourGuideId",
                        column: x => x.TourGuideId,
                        principalSchema: "content_tours",
                        principalTable: "TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourGuideSpecializations",
                schema: "content_tours",
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
                        principalSchema: "content_tours",
                        principalTable: "TourGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourPackageInclusions",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackageInclusions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPackageInclusions_TourPackages_TourPackageId",
                        column: x => x.TourPackageId,
                        principalSchema: "content_tours",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuideTourOfferings",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OffersPrivateTour = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PrivateTourPriceMultiplier = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    PrivateTourFlatPrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    IsProposer = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspendedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspensionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SuspendedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuideTourOfferings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuideTourOfferings_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "content_tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourChildFacilities",
                schema: "content_tours",
                columns: table => new
                {
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Facility = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourChildFacilities", x => new { x.TourId, x.Facility });
                    table.ForeignKey(
                        name: "FK_TourChildFacilities_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "content_tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "TourPricingTiers",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    PriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "JOD"),
                    MinParticipants = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    MaxParticipants = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ParticipantType = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPricingTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPricingTiers_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "content_tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourSchedules",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<byte>(type: "tinyint", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time(7)", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time(7)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourSchedules_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "content_tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourTourGuides",
                schema: "content_tours",
                columns: table => new
                {
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourTourGuides", x => new { x.TourId, x.TourGuideId });
                    table.ForeignKey(
                        name: "FK_TourTourGuides_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "content_tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourTranslations",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MeetingPoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourTranslations_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "content_tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourWaypoints",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    WaypointType = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourWaypoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourWaypoints_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "content_tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourPricingTierTranslations",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourPricingTierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPricingTierTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPricingTierTranslations_TourPricingTiers_TourPricingTierId",
                        column: x => x.TourPricingTierId,
                        principalSchema: "content_tours",
                        principalTable: "TourPricingTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuideApplications_GuideUserId",
                schema: "content_tours",
                table: "GuideApplications",
                column: "GuideUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GuideApplications_TourId_TourGuideId",
                schema: "content_tours",
                table: "GuideApplications",
                columns: new[] { "TourId", "TourGuideId" });

            migrationBuilder.CreateIndex(
                name: "IX_GuideAvailabilityBlocks_GuideId",
                schema: "content_tours",
                table: "GuideAvailabilityBlocks",
                column: "GuideId");

            migrationBuilder.CreateIndex(
                name: "IX_GuideAvailabilityBlocks_GuideId_StartDate_EndDate",
                schema: "content_tours",
                table: "GuideAvailabilityBlocks",
                columns: new[] { "GuideId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_GuidePricingTiers_GuideTourOfferingId",
                schema: "content_tours",
                table: "GuidePricingTiers",
                column: "GuideTourOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_GuidePricingTiers_TourGuideId_TourId",
                schema: "content_tours",
                table: "GuidePricingTiers",
                columns: new[] { "TourGuideId", "TourId" });

            migrationBuilder.CreateIndex(
                name: "IX_GuideSchedules_GuideTourOfferingId",
                schema: "content_tours",
                table: "GuideSchedules",
                column: "GuideTourOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_GuideSchedules_TourGuideId_TourId",
                schema: "content_tours",
                table: "GuideSchedules",
                columns: new[] { "TourGuideId", "TourId" });

            migrationBuilder.CreateIndex(
                name: "IX_GuideTourOfferings_TourGuideId",
                schema: "content_tours",
                table: "GuideTourOfferings",
                column: "TourGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_GuideTourOfferings_TourId_TourGuideId",
                schema: "content_tours",
                table: "GuideTourOfferings",
                columns: new[] { "TourId", "TourGuideId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_ProcessedAt",
                schema: "content_tours",
                table: "InboxMessages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Unprocessed",
                schema: "content_tours",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "RetryCount", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TourGuides_Slug",
                schema: "content_tours",
                table: "TourGuides",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TourGuides_Status",
                schema: "content_tours",
                table: "TourGuides",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TourGuides_UserId",
                schema: "content_tours",
                table: "TourGuides",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_TourPackageInclusions_PackageId_Description",
                schema: "content_tours",
                table: "TourPackageInclusions",
                columns: new[] { "TourPackageId", "Description" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackages_CreatedByUserId",
                schema: "content_tours",
                table: "TourPackages",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackages_ValidTo",
                schema: "content_tours",
                table: "TourPackages",
                column: "ValidTo");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackageTours_TourId",
                schema: "content_tours",
                table: "TourPackageTours",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPricingTiers_TourId",
                schema: "content_tours",
                table: "TourPricingTiers",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPricingTierTranslations_TourPricingTierId_LanguageCode",
                schema: "content_tours",
                table: "TourPricingTierTranslations",
                columns: new[] { "TourPricingTierId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourProposals_GuideUserId",
                schema: "content_tours",
                table: "TourProposals",
                column: "GuideUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TourProposals_TourGuideId",
                schema: "content_tours",
                table: "TourProposals",
                column: "TourGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_Tours_Name",
                schema: "content_tours",
                table: "Tours",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Tours_PlaceId",
                schema: "content_tours",
                table: "Tours",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Tours_Slug",
                schema: "content_tours",
                table: "Tours",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TourSchedules_TourId_DayOfWeek_StartTime",
                schema: "content_tours",
                table: "TourSchedules",
                columns: new[] { "TourId", "DayOfWeek", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TourTranslations_TourId_LanguageId",
                schema: "content_tours",
                table: "TourTranslations",
                columns: new[] { "TourId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourWaypoints_TourId",
                schema: "content_tours",
                table: "TourWaypoints",
                column: "TourId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuideApplications",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "GuideAvailabilityBlocks",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "GuidePricingTiers",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "GuideSchedules",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "GuideTourOfferings",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourChildFacilities",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourGuideLanguages",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourGuideSpecializations",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourPackageInclusions",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourPackageTours",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourPricingTierTranslations",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourProposals",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourSchedules",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourTourGuides",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourTranslations",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourWaypoints",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourGuides",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourPackages",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourPricingTiers",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "Tours",
                schema: "content_tours");
        }
    }
}
