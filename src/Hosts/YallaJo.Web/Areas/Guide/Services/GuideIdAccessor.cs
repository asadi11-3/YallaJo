using Microsoft.Extensions.Caching.Memory;
using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Infrastructure.Identity;

namespace YallaJo.Web.Areas.Guide.Services;

/// <summary>
/// Resolves and caches the current user's tour-guide identity (guide id, display name, avatar).
/// Kills the N+1 "GET /guides/me before every mutation" pattern (UI-PERF-API7):
///  - per-request memoization via a scoped task field (one backend call max per request),
///  - cross-request caching via IMemoryCache keyed <c>guide:me:{userId}</c> (short TTL).
/// Call <see cref="Invalidate"/> after profile mutations that change the avatar or display name.
/// </summary>
public sealed class GuideIdAccessor
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly DashboardApiClient _api;
    private readonly IMemoryCache _cache;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GuideIdAccessor> _logger;

    // Scoped service => this field memoizes for the lifetime of one HTTP request.
    private Task<GuideIdentity?>? _identity;

    public GuideIdAccessor(
        DashboardApiClient api,
        IMemoryCache cache,
        ICurrentUser currentUser,
        ILogger<GuideIdAccessor> logger)
    {
        _api = api;
        _cache = cache;
        _currentUser = currentUser;
        _logger = logger;
    }

    public sealed record GuideIdentity(Guid GuideId, string? DisplayName, string? AvatarUrl);

    /// <summary>Returns the cached guide identity, or null when the user has no guide profile.</summary>
    public Task<GuideIdentity?> GetIdentityAsync(CancellationToken ct = default)
        => _identity ??= LoadAsync(ct);

    /// <summary>Returns the guide id only (the common case for mutation facades).</summary>
    public async Task<Guid?> GetGuideIdAsync(CancellationToken ct = default)
        => (await GetIdentityAsync(ct))?.GuideId;

    /// <summary>Drops both the per-request memo and the cross-request cache entry.</summary>
    public void Invalidate()
    {
        _identity = null;
        if (_currentUser.UserId is { } userId)
        {
            _cache.Remove(CacheKey(userId));
        }
    }

    private async Task<GuideIdentity?> LoadAsync(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return null;
        }

        if (_cache.TryGetValue(CacheKey(userId), out GuideIdentity? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            var result = await _api.GetMyProfileAsync(ct);
            if (result is not { IsSuccess: true, Data: not null })
            {
                return null;
            }

            var identity = new GuideIdentity(result.Data.Id, result.Data.DisplayName, result.Data.AvatarUrl);
            _cache.Set(CacheKey(userId), identity, CacheTtl);
            return identity;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to resolve guide identity for user {UserId}.", userId);
            return null;
        }
    }

    private static string CacheKey(Guid userId) => $"guide:me:{userId}";
}
