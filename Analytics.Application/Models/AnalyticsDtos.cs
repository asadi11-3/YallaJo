namespace Analytics.Application.Models;

public sealed record UserInteractionDto(long Id, Guid? UserId, string EntityType, Guid EntityId, string InteractionType, DateTime OccurredAt, string? UserAgent);
public sealed record CursorPageDto<T>(IReadOnlyList<T> Items, long? NextId);
public sealed record PopularEntityDto(Guid EntityId, string EntityType, decimal Score, int? TrendingRank, int ReviewCount);
public sealed record AdminDashboardOverviewDto(decimal Revenue, int Bookings, int Users, int Alerts);
public sealed record AdminRevenueDashboardDto(IReadOnlyList<RevenueTimePointDto> Series, decimal TotalRevenue);
public sealed record RevenueTimePointDto(DateTime Date, decimal Revenue);
public sealed record AdminBookingsDashboardDto(int TotalBookings, int CompletedBookings, int CancelledBookings);
public sealed record AdminUsersDashboardDto(int TotalUsers, int NewUsers);
public sealed record ProviderDashboardDto(Guid ProviderId, decimal Revenue, int Bookings, decimal AverageRating);
public sealed record ProviderAnalyticsDto(Guid ProviderId, IReadOnlyList<RevenueTimePointDto> Series);
public sealed record ProviderTourListItemDto(Guid TourId, string Title, decimal Score);
public sealed record AdminAuditLogDto(long Id, Guid? UserId, string Action, string EntityType, Guid EntityId, DateTime OccurredAt, DateTime? RedactedAt);
