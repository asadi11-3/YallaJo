using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Action = table.Column<byte>(type: "tinyint", nullable: false),
                    CustomActionName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddressHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RedactedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RedactionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RedactedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

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
                name: "BoostPackages",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityKind = table.Column<byte>(type: "tinyint", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoostMultiplier = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    BillingMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "FlatFee"),
                    BidPerClick = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DailyBudgetCap = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SpentToday = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoostPackages", x => x.Id);
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
                name: "EditorialPins",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityKind = table.Column<byte>(type: "tinyint", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Context = table.Column<int>(type: "int", nullable: false),
                    BadgeText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditorialPins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntityAttributeSnapshots",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityKind = table.Column<byte>(type: "tinyint", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BasePriceAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    BasePriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    SalePrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    BookingCount = table.Column<int>(type: "int", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    LocationLatitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    LocationLongitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BusinessType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Difficulty = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    IsChildFriendly = table.Column<bool>(type: "bit", nullable: false),
                    IsAccessible = table.Column<bool>(type: "bit", nullable: false),
                    IsInstantBooking = table.Column<bool>(type: "bit", nullable: false),
                    IsHalal = table.Column<bool>(type: "bit", nullable: true),
                    HasVegetarianOptions = table.Column<bool>(type: "bit", nullable: true),
                    HasAlcoholFreeArea = table.Column<bool>(type: "bit", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CategoryIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpcomingCapacity = table.Column<int>(type: "int", nullable: true),
                    UpcomingBookings = table.Column<int>(type: "int", nullable: true),
                    IsPhotogenicHotspot = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LastSnapshotAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityAttributeSnapshots", x => x.Id);
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
                name: "ExperimentAssignments",
                schema: "analytics",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExperimentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentAssignments", x => new { x.UserId, x.ExperimentId });
                });

            migrationBuilder.CreateTable(
                name: "Experiments",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrafficPercent = table.Column<int>(type: "int", nullable: false),
                    VariantsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experiments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GdprDeletionRequests",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledHardDeleteAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    IsExecuted = table.Column<bool>(type: "bit", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GdprDeletionRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HolidayCalendar",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HolidayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    BoostRulesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayCalendar", x => x.Id);
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
                name: "OutboxMessages",
                schema: "analytics",
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

            migrationBuilder.CreateTable(
                name: "PopularityScores",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<byte>(type: "tinyint", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TrendingRank = table.Column<int>(type: "int", nullable: true),
                    LastRecalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsStale = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    InteractionCountSnapshot = table.Column<int>(type: "int", nullable: false),
                    AverageRatingSnapshot = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true),
                    ReviewCountSnapshot = table.Column<int>(type: "int", nullable: false),
                    CategoryRankPercentile = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PopularityScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationCaches",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityKind = table.Column<byte>(type: "tinyint", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    SignalsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationCaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeasonalityRules",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonthStart = table.Column<int>(type: "int", nullable: false),
                    MonthEnd = table.Column<int>(type: "int", nullable: false),
                    Multiplier = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonalityRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SponsoredClickEvents",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BidId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SourceKind = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    ChargedAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ClickedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsFraudulent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsoredClickEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuggestionBatches",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceKind = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Context = table.Column<byte>(type: "tinyint", nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsStale = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestionBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuggestionMetrics",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecommendationCacheId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExperimentVariant = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestionMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TripArcs",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DayClustersJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    MinDays = table.Column<int>(type: "int", nullable: false),
                    MaxDays = table.Column<int>(type: "int", nullable: false),
                    InterestTags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripArcs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserExcludedEntities",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityKind = table.Column<byte>(type: "tinyint", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExcludedEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserInteractions",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EntityType = table.Column<byte>(type: "tinyint", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InteractionType = table.Column<byte>(type: "tinyint", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClientIpHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInteractions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BudgetTier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsFamilyTraveler = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CurrentTripStage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastComputedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferredCategories",
                schema: "analytics",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreferenceScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 0m),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferredCategories", x => new { x.UserId, x.CategoryId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                schema: "analytics",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OccurredAt",
                schema: "analytics",
                table: "AuditLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_RedactedAt",
                schema: "analytics",
                table: "AuditLogs",
                column: "RedactedAt",
                filter: "[RedactedAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                schema: "analytics",
                table: "AuditLogs",
                column: "UserId");

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
                name: "IX_BoostPackages_BillingMode_IsActive",
                schema: "analytics",
                table: "BoostPackages",
                columns: new[] { "BillingMode", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BoostPackages_EntityKind_EntityId_IsActive",
                schema: "analytics",
                table: "BoostPackages",
                columns: new[] { "EntityKind", "EntityId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BoostPackages_ExpiresAt",
                schema: "analytics",
                table: "BoostPackages",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_BoostPackages_ProviderId",
                schema: "analytics",
                table: "BoostPackages",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_EditorialPins_Context_IsActive",
                schema: "analytics",
                table: "EditorialPins",
                columns: new[] { "Context", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EditorialPins_EntityKind_EntityId",
                schema: "analytics",
                table: "EditorialPins",
                columns: new[] { "EntityKind", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityAttributeSnapshots_EntityKind_EntityId",
                schema: "analytics",
                table: "EntityAttributeSnapshots",
                columns: new[] { "EntityKind", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntityAttributeSnapshots_IsDeleted",
                schema: "analytics",
                table: "EntityAttributeSnapshots",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EntityAttributeSnapshots_LocationLatitude_LocationLongitude",
                schema: "analytics",
                table: "EntityAttributeSnapshots",
                columns: new[] { "LocationLatitude", "LocationLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityAttributeSnapshots_PlaceId",
                schema: "analytics",
                table: "EntityAttributeSnapshots",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityPopularitySnapshots_EntityType_EntityId_TakenAt",
                schema: "analytics",
                table: "EntityPopularitySnapshots",
                columns: new[] { "EntityType", "EntityId", "TakenAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentAssignments_ExperimentId",
                schema: "analytics",
                table: "ExperimentAssignments",
                column: "ExperimentId");

            migrationBuilder.CreateIndex(
                name: "IX_Experiments_Name",
                schema: "analytics",
                table: "Experiments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Experiments_Status",
                schema: "analytics",
                table: "Experiments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GdprDeletionRequests_ScheduledHardDeleteAt",
                schema: "analytics",
                table: "GdprDeletionRequests",
                column: "ScheduledHardDeleteAt");

            migrationBuilder.CreateIndex(
                name: "IX_GdprDeletionRequests_UserId_IsCancelled_IsExecuted",
                schema: "analytics",
                table: "GdprDeletionRequests",
                columns: new[] { "UserId", "IsCancelled", "IsExecuted" });

            migrationBuilder.CreateIndex(
                name: "IX_HolidayCalendar_StartDate_EndDate",
                schema: "analytics",
                table: "HolidayCalendar",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_HolidayCalendar_Year_IsActive",
                schema: "analytics",
                table: "HolidayCalendar",
                columns: new[] { "Year", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Unprocessed",
                schema: "analytics",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "RetryCount", "OccurredOnUtc" });

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
                name: "IX_RecommendationCaches_BatchId",
                schema: "analytics",
                table: "RecommendationCaches",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationCaches_BatchId_Position",
                schema: "analytics",
                table: "RecommendationCaches",
                columns: new[] { "BatchId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationCaches_UserId_EntityKind_Score",
                schema: "analytics",
                table: "RecommendationCaches",
                columns: new[] { "UserId", "EntityKind", "Score" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_SeasonalityRules_PlaceId_IsActive",
                schema: "analytics",
                table: "SeasonalityRules",
                columns: new[] { "PlaceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SponsoredClickEvents_BidId_ClickedAt",
                schema: "analytics",
                table: "SponsoredClickEvents",
                columns: new[] { "BidId", "ClickedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SponsoredClickEvents_UserId_BidId_ClickedAt",
                schema: "analytics",
                table: "SponsoredClickEvents",
                columns: new[] { "UserId", "BidId", "ClickedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionBatches_ComputedAt",
                schema: "analytics",
                table: "SuggestionBatches",
                column: "ComputedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionBatches_IsStale",
                schema: "analytics",
                table: "SuggestionBatches",
                column: "IsStale");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionBatches_SourceKind_SourceId_Context",
                schema: "analytics",
                table: "SuggestionBatches",
                columns: new[] { "SourceKind", "SourceId", "Context" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionMetrics_BatchId_Stage_OccurredAt",
                schema: "analytics",
                table: "SuggestionMetrics",
                columns: new[] { "BatchId", "Stage", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionMetrics_OccurredAt",
                schema: "analytics",
                table: "SuggestionMetrics",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionMetrics_UserId_OccurredAt",
                schema: "analytics",
                table: "SuggestionMetrics",
                columns: new[] { "UserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TripArcs_IsActive",
                schema: "analytics",
                table: "TripArcs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserExcludedEntities_ExpiresAt",
                schema: "analytics",
                table: "UserExcludedEntities",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserExcludedEntities_UserId_EntityKind_EntityId",
                schema: "analytics",
                table: "UserExcludedEntities",
                columns: new[] { "UserId", "EntityKind", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserInteractions_EntityType_EntityId",
                schema: "analytics",
                table: "UserInteractions",
                columns: new[] { "EntityType", "EntityId" });

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
                name: "IX_UserInteractions_UserId",
                schema: "analytics",
                table: "UserInteractions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                schema: "analytics",
                table: "UserPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "BookingSnapshots",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "BoostPackages",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "DashboardCaches",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "EditorialPins",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "EntityAttributeSnapshots",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "EntityPopularitySnapshots",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "ExperimentAssignments",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "Experiments",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "GdprDeletionRequests",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "HolidayCalendar",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "PaymentSnapshots",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "PopularityScores",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "RecommendationCaches",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "SeasonalityRules",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "SponsoredClickEvents",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "SuggestionBatches",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "SuggestionMetrics",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "TripArcs",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "UserExcludedEntities",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "UserInteractions",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "UserPreferences",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "UserPreferredCategories",
                schema: "analytics");
        }
    }
}
