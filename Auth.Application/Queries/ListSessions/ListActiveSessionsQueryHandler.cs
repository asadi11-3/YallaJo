using Auth.Application.Caching;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Queries.ListSessions;

public sealed class ListActiveSessionsQueryHandler(
    ISessionRepository sessionRepository,
    IDeviceRepository deviceRepository,
    ICurrentUser currentUser,
    HybridCache cache)
    : IQueryHandler<ListActiveSessionsQuery, IReadOnlyList<ActiveSessionDto>>
{
    private static readonly HybridCacheEntryOptions _cacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };

    public async Task<Result<IReadOnlyList<ActiveSessionDto>>> Handle(
        ListActiveSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId.Value;

        Guid? currentSessionId = null;
        var sidClaim = currentUser.GetClaim("sid");
        if (sidClaim is not null && Guid.TryParse(sidClaim, out var parsedSid))
            currentSessionId = parsedSid;

        // Cache is keyed per user. Write commands (Logout, RevokeSession, etc.)
        // invalidate via RemoveByTagAsync(AuthCacheKeys.UserSessionsTag(userId)).
        var dtos = await cache.GetOrCreateAsync(
            key: AuthCacheKeys.UserSessions(userId),
            state: (sessionRepository, deviceRepository, userId, currentSessionId),
            factory: static async (state, ct) =>
                await BuildDtosAsync(
                    state.sessionRepository,
                    state.deviceRepository,
                    state.userId,
                    state.currentSessionId,
                    ct),
            options: _cacheOptions,
            tags: [AuthCacheKeys.UserSessionsTag(userId)],
            cancellationToken: cancellationToken);

        return Result<IReadOnlyList<ActiveSessionDto>>.Success(dtos);
    }

    private static async Task<IReadOnlyList<ActiveSessionDto>> BuildDtosAsync(
        ISessionRepository sessionRepository,
        IDeviceRepository deviceRepository,
        Guid userId,
        Guid? currentSessionId,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // استخدام GetAllAsync بدلاً من الدالة المخصصة التي تم حذفها
        var sessions = await sessionRepository.GetAllAsync(
            filter: s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now,
            orderBy: q => q.OrderByDescending(s => s.CreatedAt),
            asNoTracking: true,
            ct: ct);

        if (sessions.Count == 0) // أو !sessions.Any() بناءً على نوع الـ Collection الراجع
            return Array.Empty<ActiveSessionDto>();

        var deviceIds = sessions.Select(s => s.DeviceId).Distinct().ToHashSet();
        var devices = await deviceRepository.GetAllAsync(
            filter: d => deviceIds.Contains(d.Id),
            asNoTracking: true,
            ct: ct);

        var deviceMap = devices.ToDictionary(d => d.Id);

        return sessions
            .Select(s =>
            {
                deviceMap.TryGetValue(s.DeviceId, out var device);
                return new ActiveSessionDto(
                    SessionId: s.Id,
                    DeviceId: s.DeviceId,
                    DeviceName: device?.DeviceName,
                    UserAgent: device?.UserAgent,
                    IpAddress: s.IpAddress,
                    CreatedAt: s.CreatedAt,
                    ExpiresAt: s.ExpiresAt,
                    IsCurrent: currentSessionId.HasValue && s.Id == currentSessionId.Value);
            })
            .ToList();
    }
}
