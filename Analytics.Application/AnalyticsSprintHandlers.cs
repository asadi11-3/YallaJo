using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Application.Models;
using Analytics.Contracts.IntegrationEvents;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Analytics.Domain.Repositories;
using FluentValidation;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application;

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

public sealed record RecordInteractionCommand(Guid? UserId, string? SessionId, string EntityType, Guid EntityId, string InteractionType, string? ClientIp, string? UserAgent) : ICommand;
public sealed class RecordInteractionCommandValidator : AbstractValidator<RecordInteractionCommand>
{
    public RecordInteractionCommandValidator()
    {
        RuleFor(x => x.EntityType).Must(x => Enum.TryParse<EntityType>(x, true, out _));
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.InteractionType).Must(x => Enum.TryParse<InteractionType>(x, true, out _));
    }
}
public sealed class RecordInteractionCommandHandler(IInteractionIngestQueue queue, HybridCache cache, ILogger<RecordInteractionCommandHandler> logger) : ICommandHandler<RecordInteractionCommand>
{
    public async Task<Result> Handle(RecordInteractionCommand request, CancellationToken ct)
    {
        var entityType = Enum.Parse<EntityType>(request.EntityType, true);
        var interactionType = Enum.Parse<InteractionType>(request.InteractionType, true);

        if (request.UserId is not null)
        {
            var dedupeKey = $"interaction-dedupe:{request.UserId.Value:N}:{entityType}:{request.EntityId:N}:{interactionType}";
            var sentinel = Guid.NewGuid().ToString("N");
            var observed = await cache.GetOrCreateAsync(
                dedupeKey,
                factory: _ => ValueTask.FromResult(sentinel),
                options: new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(5),
                    LocalCacheExpiration = TimeSpan.FromMinutes(5),
                },
                tags: null,
                cancellationToken: ct).ConfigureAwait(false);

            if (!string.Equals(observed, sentinel, StringComparison.Ordinal))
            {
                logger.LogDebug("Skipped duplicate analytics interaction {EntityType}/{EntityId}", entityType, request.EntityId);
                return Result.Success();
            }
        }

        _ = queue.TryEnqueue(new InteractionEnvelope(request.UserId, request.SessionId, entityType, request.EntityId, interactionType, DateTime.UtcNow, request.ClientIp, request.UserAgent));
        logger.LogDebug("Queued analytics interaction {EntityType}/{EntityId}", entityType, request.EntityId);
        return Result.Success();
    }
}

public sealed record GetAdminInteractionsQuery(Guid? UserId, string? EntityType, Guid? EntityId, string? InteractionType, DateTime? From, DateTime? To, long? AfterId, int PageSize) : IQuery<CursorPageDto<UserInteractionDto>>, ICacheableQuery
{
    public string CacheKey => $"analytics:interactions:{UserId}:{EntityType}:{EntityId}:{InteractionType}:{From:o}:{To:o}:{AfterId}:{PageSize}";
    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30);
    public IReadOnlyList<string> Tags => ["analytics:interactions"];
}
public sealed class GetAdminInteractionsQueryHandler(IUserInteractionRepository repo, ILogger<GetAdminInteractionsQueryHandler> logger) : IQueryHandler<GetAdminInteractionsQuery, CursorPageDto<UserInteractionDto>>
{
    public async Task<Result<CursorPageDto<UserInteractionDto>>> Handle(GetAdminInteractionsQuery request, CancellationToken ct)
    {
        EntityType? et = Enum.TryParse<EntityType>(request.EntityType, true, out var e) ? e : null;
        InteractionType? it = Enum.TryParse<InteractionType>(request.InteractionType, true, out var i) ? i : null;
        var page = await repo.GetPageAsync(request.UserId, et, request.EntityId, it, request.From, request.To, request.AfterId, request.PageSize, ct);
        logger.LogDebug("Read {Count} analytics interactions", page.Items.Count);
        return Result.Success(new CursorPageDto<UserInteractionDto>(page.Items.Select(Map).ToList(), page.NextId));
    }
    private static UserInteractionDto Map(UserInteraction x) => new(x.Id, x.UserId, x.EntityType.ToString(), x.EntityId, x.InteractionType.ToString(), x.OccurredAt, x.UserAgent);
}
public sealed record GetUserInteractionsQuery(Guid UserId, long? AfterId, int PageSize) : IQuery<CursorPageDto<UserInteractionDto>>, ICacheableQuery
{
    public string CacheKey => $"analytics:interactions:user:{UserId}:{AfterId}:{PageSize}";
    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30);
    public IReadOnlyList<string> Tags => [$"analytics:interactions:user:{UserId}"];
}
public sealed class GetUserInteractionsQueryHandler(IUserInteractionRepository repo, ILogger<GetUserInteractionsQueryHandler> logger) : IQueryHandler<GetUserInteractionsQuery, CursorPageDto<UserInteractionDto>>
{
    public async Task<Result<CursorPageDto<UserInteractionDto>>> Handle(GetUserInteractionsQuery request, CancellationToken ct)
    {
        var page = await repo.GetPageAsync(request.UserId, null, null, null, null, null, request.AfterId, request.PageSize, ct);
        logger.LogDebug("Read {Count} user interactions", page.Items.Count);
        return Result.Success(new CursorPageDto<UserInteractionDto>(page.Items.Select(x => new UserInteractionDto(x.Id, x.UserId, x.EntityType.ToString(), x.EntityId, x.InteractionType.ToString(), x.OccurredAt, x.UserAgent)).ToList(), page.NextId));
    }
}

public sealed record GetPopularEntitiesQuery(string EntityType, int Count = 20) : IQuery<IReadOnlyList<PopularEntityDto>>, ICacheableQuery
{
    public string CacheKey => $"popular:{EntityType}:{Count}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [$"popular:{EntityType}"];
}

public sealed class GetPopularEntitiesQueryHandler(IPopularityScoreRepository repo, ILogger<GetPopularEntitiesQueryHandler> logger) : IQueryHandler<GetPopularEntitiesQuery, IReadOnlyList<PopularEntityDto>>
{
    public async Task<Result<IReadOnlyList<PopularEntityDto>>> Handle(GetPopularEntitiesQuery request, CancellationToken ct)
    {
        if (!Enum.TryParse<EntityType>(request.EntityType, true, out var type)) return Result.Failure<IReadOnlyList<PopularEntityDto>>(new Error("Analytics.InvalidEntityType", "Unsupported entity type."));
        var rows = await repo.GetTopByTypeAsync(type, request.Count, ct);
        logger.LogDebug("Read {Count} popular {Type}", rows.Count, type);
        return Result.Success((IReadOnlyList<PopularEntityDto>)rows.Select(x => new PopularEntityDto(x.EntityId, x.EntityType.ToString(), x.Score, x.TrendingRank, 0)).ToList());
    }
}
public sealed record GetTrendingQuery(int Count = 20) : IQuery<IReadOnlyList<PopularEntityDto>>, ICacheableQuery
{
    public string CacheKey => $"trending:{Count}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["trending"];
}
public sealed class GetTrendingQueryHandler(IPopularityScoreRepository repo, ILogger<GetTrendingQueryHandler> logger) : IQueryHandler<GetTrendingQuery, IReadOnlyList<PopularEntityDto>>
{
    public async Task<Result<IReadOnlyList<PopularEntityDto>>> Handle(GetTrendingQuery request, CancellationToken ct)
    {
        var all = new List<PopularEntityDto>();
        foreach (var type in Enum.GetValues<EntityType>()) all.AddRange((await repo.GetTrendingAsync(type, request.Count, ct)).Select(x => new PopularEntityDto(x.EntityId, x.EntityType.ToString(), x.Score, x.TrendingRank, 0)));
        logger.LogDebug("Read {Count} trending entities", all.Count);
        return all.Count == 0 ? Result.Failure<IReadOnlyList<PopularEntityDto>>(new Error("Trending.WindowNotReady", "Trending window is not ready."), Outcome.ServerError) : Result.Success((IReadOnlyList<PopularEntityDto>)all.OrderBy(x => x.TrendingRank).Take(request.Count).ToList());
    }
}

public sealed record GetAdminDashboardOverviewQuery() : IQuery<AdminDashboardOverviewDto>, ICacheableQuery { public string CacheKey => "admin:dashboard:overview"; public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30); public IReadOnlyList<string> Tags => ["admin:dashboard:overview"]; }
public sealed class GetAdminDashboardOverviewQueryHandler(IAnalyticsDashboardReader reader, ILogger<GetAdminDashboardOverviewQueryHandler> logger) : IQueryHandler<GetAdminDashboardOverviewQuery, AdminDashboardOverviewDto> { public async Task<Result<AdminDashboardOverviewDto>> Handle(GetAdminDashboardOverviewQuery request, CancellationToken ct) { logger.LogDebug("Read admin dashboard overview"); return Result.Success(await reader.GetAdminOverviewAsync(ct)); } }
public sealed record GetAdminRevenueDashboardQuery(DateTime? From, DateTime? To) : IQuery<AdminRevenueDashboardDto>, ICacheableQuery { public string CacheKey => $"admin:dashboard:revenue:{From:o}:{To:o}"; public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30); public IReadOnlyList<string> Tags => ["admin:dashboard:revenue"]; }
public sealed class GetAdminRevenueDashboardQueryHandler(IAnalyticsDashboardReader reader, ILogger<GetAdminRevenueDashboardQueryHandler> logger) : IQueryHandler<GetAdminRevenueDashboardQuery, AdminRevenueDashboardDto> { public async Task<Result<AdminRevenueDashboardDto>> Handle(GetAdminRevenueDashboardQuery request, CancellationToken ct) { logger.LogDebug("Read admin revenue dashboard"); return Result.Success(await reader.GetAdminRevenueAsync(request.From, request.To, ct)); } }
public sealed record GetAdminBookingsDashboardQuery(DateTime? From, DateTime? To) : IQuery<AdminBookingsDashboardDto>, ICacheableQuery { public string CacheKey => $"admin:dashboard:bookings:{From:o}:{To:o}"; public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30); public IReadOnlyList<string> Tags => ["admin:dashboard:bookings"]; }
public sealed class GetAdminBookingsDashboardQueryHandler(IAnalyticsDashboardReader reader, ILogger<GetAdminBookingsDashboardQueryHandler> logger) : IQueryHandler<GetAdminBookingsDashboardQuery, AdminBookingsDashboardDto> { public async Task<Result<AdminBookingsDashboardDto>> Handle(GetAdminBookingsDashboardQuery request, CancellationToken ct) { logger.LogDebug("Read admin bookings dashboard"); return Result.Success(await reader.GetAdminBookingsAsync(request.From, request.To, ct)); } }
public sealed record GetAdminUsersDashboardQuery(DateTime? From, DateTime? To) : IQuery<AdminUsersDashboardDto>, ICacheableQuery { public string CacheKey => $"admin:dashboard:users:{From:o}:{To:o}"; public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30); public IReadOnlyList<string> Tags => ["admin:dashboard:users"]; }
public sealed class GetAdminUsersDashboardQueryHandler(IAnalyticsDashboardReader reader, ILogger<GetAdminUsersDashboardQueryHandler> logger) : IQueryHandler<GetAdminUsersDashboardQuery, AdminUsersDashboardDto> { public async Task<Result<AdminUsersDashboardDto>> Handle(GetAdminUsersDashboardQuery request, CancellationToken ct) { logger.LogDebug("Read admin users dashboard"); return Result.Success(await reader.GetAdminUsersAsync(request.From, request.To, ct)); } }

public sealed record GetProviderDashboardQuery(Guid ProviderId) : IQuery<ProviderDashboardDto>, ICacheableQuery { public string CacheKey => $"provider:dashboard:{ProviderId}"; public TimeSpan? CacheDuration => TimeSpan.FromSeconds(60); public IReadOnlyList<string> Tags => [$"provider:dashboard:{ProviderId}"]; }
public sealed class GetProviderDashboardQueryHandler(IAnalyticsDashboardReader reader, ILogger<GetProviderDashboardQueryHandler> logger) : IQueryHandler<GetProviderDashboardQuery, ProviderDashboardDto> { public async Task<Result<ProviderDashboardDto>> Handle(GetProviderDashboardQuery request, CancellationToken ct) { logger.LogDebug("Read provider dashboard {ProviderId}", request.ProviderId); return Result.Success(await reader.GetProviderDashboardAsync(request.ProviderId, ct)); } }
public sealed record GetProviderAnalyticsQuery(Guid ProviderId, DateTime? From, DateTime? To) : IQuery<ProviderAnalyticsDto>, ICacheableQuery { public string CacheKey => $"provider:analytics:{ProviderId}:{From:o}:{To:o}"; public TimeSpan? CacheDuration => TimeSpan.FromSeconds(60); public IReadOnlyList<string> Tags => [$"provider:analytics:{ProviderId}"]; }
public sealed class GetProviderAnalyticsQueryHandler(IAnalyticsDashboardReader reader, ILogger<GetProviderAnalyticsQueryHandler> logger) : IQueryHandler<GetProviderAnalyticsQuery, ProviderAnalyticsDto> { public async Task<Result<ProviderAnalyticsDto>> Handle(GetProviderAnalyticsQuery request, CancellationToken ct) { logger.LogDebug("Read provider analytics {ProviderId}", request.ProviderId); return Result.Success(await reader.GetProviderAnalyticsAsync(request.ProviderId, request.From, request.To, ct)); } }
public sealed record GetProviderMyToursQuery(Guid ProviderId, long? AfterId, int PageSize) : IQuery<CursorPageDto<ProviderTourListItemDto>>, ICacheableQuery { public string CacheKey => $"provider:tours:{ProviderId}:{AfterId}:{PageSize}"; public TimeSpan? CacheDuration => TimeSpan.FromSeconds(60); public IReadOnlyList<string> Tags => [$"provider:tours:{ProviderId}"]; }
public sealed class GetProviderMyToursQueryHandler(IAnalyticsDashboardReader reader, ILogger<GetProviderMyToursQueryHandler> logger) : IQueryHandler<GetProviderMyToursQuery, CursorPageDto<ProviderTourListItemDto>> { public async Task<Result<CursorPageDto<ProviderTourListItemDto>>> Handle(GetProviderMyToursQuery request, CancellationToken ct) { logger.LogDebug("Read provider tours {ProviderId}", request.ProviderId); return Result.Success(await reader.GetProviderToursAsync(request.ProviderId, request.AfterId, request.PageSize, ct)); } }

public sealed record GetAuditLogsQuery(string? EntityType, Guid? EntityId, Guid? UserId, string? Action, DateTime? From, DateTime? To, long? AfterId, int PageSize) : IQuery<CursorPageDto<AdminAuditLogDto>>;
public sealed class GetAuditLogsQueryHandler(IAuditLogRepository repo, ILogger<GetAuditLogsQueryHandler> logger) : IQueryHandler<GetAuditLogsQuery, CursorPageDto<AdminAuditLogDto>>
{
    public async Task<Result<CursorPageDto<AdminAuditLogDto>>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        AuditLogAction? action = Enum.TryParse<AuditLogAction>(request.Action, true, out var a) ? a : null;
        var page = await repo.GetPageAsync(request.EntityType, request.EntityId, request.UserId, action, request.From, request.To, request.AfterId, request.PageSize, ct);
        logger.LogDebug("Read {Count} audit logs", page.Items.Count);
        return Result.Success(new CursorPageDto<AdminAuditLogDto>(page.Items.Select(x => new AdminAuditLogDto(x.Id, x.UserId, x.Action.ToString(), x.EntityType, x.EntityId, x.OccurredAt, x.RedactedAt)).ToList(), page.NextId));
    }
}
public sealed record ExportAuditLogsQuery(DateTime From, DateTime To) : IQuery<string>;
public sealed class ExportAuditLogsQueryHandler(IAuditLogRepository repo, ILogger<ExportAuditLogsQueryHandler> logger) : IQueryHandler<ExportAuditLogsQuery, string>
{
    public async Task<Result<string>> Handle(ExportAuditLogsQuery request, CancellationToken ct)
    {
        var count = await repo.CountAsync(request.From, request.To, ct);
        if (count > 100_000) return Result.Failure<string>(new Error("AuditLog.ExportTooLarge", "Audit export exceeds 100,000 rows."), Outcome.UnprocessableEntity);
        logger.LogDebug("Exporting {Count} audit logs", count);
        return Result<string>.Success("Id,UserId,Action,EntityType,EntityId,OccurredAt\n");
    }
}
public sealed record RedactAuditLogCommand(long Id, Guid AdminUserId, string Reason) : ICommand;
public sealed class RedactAuditLogCommandHandler(IAuditLogRepository repo, IAuditLogRedactor redactor, IAnalyticsUnitOfWork uow, IAnalyticsOutboxWriter outbox, ILogger<RedactAuditLogCommandHandler> logger) : ICommandHandler<RedactAuditLogCommand>
{
    public async Task<Result> Handle(RedactAuditLogCommand request, CancellationToken ct)
    {
        var entry = await repo.GetByIdAsync(request.Id, ct);
        if (entry is null) return Result.Failure(new Error("AuditLog.NotFound", "Audit log entry was not found."), Outcome.NotFound);
        var old = redactor.Redact(entry.OldValue); var @new = redactor.Redact(entry.NewValue);
        var fields = old.RedactedFields.Concat(@new.RedactedFields).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        entry.RetroactivelyRedact(request.AdminUserId, request.Reason, fields, old.RedactedValue, @new.RedactedValue, DateTime.UtcNow);
        await outbox.WriteAsync(new AuditLogEntryRedactedIntegrationEvent(entry.Id, entry.EntityType, entry.EntityId, request.AdminUserId.ToString(), DateTime.UtcNow), ct);
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Redacted audit log {Id}", request.Id);
        return Result.Success();
    }
}
