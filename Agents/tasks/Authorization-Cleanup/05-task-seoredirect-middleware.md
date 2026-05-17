# TASK 4 — `SeoRedirectMiddleware` (BONUS)

> **Owner:** Mohammad (Intermediate) — **Hours:** 12h — **Hard deadline:** Wed **2027-03-10 17:00**
> **Earliest start:** Mon 2027-03-08 09:00 (after TASK 3 ContentCore + ContentPlaces work lands)
> **Endpoints:** 0 (middleware) — **Files:** new middleware + new ContentSeo abstraction + wiring + tests
> **Depends on:** ContentSeo module (Wave 4 sprint, already closed) — `ContentSeo.SeoRedirects` table exists.

This bonus task ships the long-deferred `SeoRedirectMiddleware` that consumes `ContentSeo.SeoRedirects` table and serves 301/302 redirects on incoming frontend slug requests. It plugs into the middleware pipeline at position 12 (after `UseAuthorization`, before module endpoints — per `YallaJo.md` middleware-order spec).

---

## 1. Interface Design

`ContentSeo.Contracts/Services/ISeoRedirectLookupService.cs`:

```csharp
namespace ContentSeo.Contracts.Services;

public interface ISeoRedirectLookupService
{
    /// <summary>
    /// Looks up an active redirect for the given canonical lowercase path.
    /// Returns null if no redirect exists.
    /// </summary>
    Task<SeoRedirectResult?> ResolveAsync(string sourcePath, CancellationToken ct);

    /// <summary>
    /// Records a non-blocking hit (fire-and-forget pattern).
    /// </summary>
    void RecordHit(Guid redirectId);
}

public sealed record SeoRedirectResult(
    Guid RedirectId,
    string SourcePath,
    string TargetPath,
    int StatusCode);  // 301 (permanent) or 302 (temporary)
```

**Implementation:** `ContentSeo.Infrastructure/Services/SeoRedirectLookupService.cs` — uses `HybridCache` with key `seo-redirect:{lowercase-path}` 5min sliding + tag `seo-redirects` (invalidation already wired from ContentSeo sprint). DB query: `SELECT TOP 1 Id, SourcePath, TargetPath, StatusCode FROM ContentSeo.SeoRedirects WHERE LOWER(SourcePath) = @path AND IsActive = 1`.

**Hit recording:** uses `Channel<RedirectHit>` Singleton (capacity 1000, DropWrite) drained by `SeoRedirectHitFlushService` BG service every 10 sec — bulk UPDATE statement increments `HitCount` for each. Same pattern as Analytics A-R1 — non-blocking, lossy under storm.

DI registration (ContentSeo.Infrastructure):
```csharp
services.AddSingleton<ISeoRedirectHitQueue, SeoRedirectHitQueue>();  // Channel wrapper
services.AddScoped<ISeoRedirectLookupService, SeoRedirectLookupService>();
services.AddHostedService<SeoRedirectHitFlushService>();
```

---

## 2. Middleware (`YallaJo.Api/Middleware/SeoRedirectMiddleware.cs`)

```csharp
namespace YallaJo.Api.Middleware;

internal sealed class SeoRedirectMiddleware(
    RequestDelegate next,
    ILogger<SeoRedirectMiddleware> logger)
{
    private static readonly PathString ApiPrefix = new("/api");
    private static readonly PathString HealthPrefix = new("/health");
    private static readonly PathString SwaggerPrefix = new("/swagger");
    private static readonly PathString HubsPrefix = new("/hubs");
    private static readonly PathString FrameworkPrefix = new("/_framework");

    public async Task InvokeAsync(
        HttpContext context,
        ISeoRedirectLookupService lookup)
    {
        // Skip infrastructure / API / hubs paths — these never have SEO redirects.
        var path = context.Request.Path;
        if (path.StartsWithSegments(ApiPrefix)
            || path.StartsWithSegments(HealthPrefix)
            || path.StartsWithSegments(SwaggerPrefix)
            || path.StartsWithSegments(HubsPrefix)
            || path.StartsWithSegments(FrameworkPrefix))
        {
            await next(context);
            return;
        }

        var normalizedPath = path.Value!.ToLowerInvariant();
        var redirect = await lookup.ResolveAsync(normalizedPath, context.RequestAborted);
        if (redirect is null)
        {
            await next(context);
            return;
        }

        lookup.RecordHit(redirect.RedirectId);  // fire-and-forget
        context.Response.Headers.Location = redirect.TargetPath
            + context.Request.QueryString.Value;  // preserve query string
        context.Response.StatusCode = redirect.StatusCode;  // 301 or 302
        logger.LogInformation(
            "SEO redirect {Source} -> {Target} ({Status})",
            normalizedPath, redirect.TargetPath, redirect.StatusCode);
    }
}
```

**Why scoped DI parameter on `InvokeAsync`:** middleware itself is a singleton (via `RequestDelegate`); injecting `ISeoRedirectLookupService` via `InvokeAsync` resolves a per-request scope, which is what we want.

**Why `lowercase` normalization:** PDF 2 §8 + ContentSeo sprint spec — slugs case-insensitive.

**Why preserve query string:** SEO landing pages often have `?utm_source=...` tracking params; losing them breaks attribution.

**Why no early return on the redirect path itself collision:** circular redirects are prevented by ContentSeo sprint's redirect-flatten rule (PDF 2 §8 — "if A→B exists and B→C added, system updates A→C directly"). So we never need to follow chains here.

---

## 3. Wire Into Program.cs

```csharp
// YallaJo.Api/Program.cs (after app.UseAuthorization(), before module endpoint mapping)
app.UseAuthorization();

// NEW middleware:
app.UseMiddleware<SeoRedirectMiddleware>();

// Then module endpoints:
app.MapAuthEndpoints();
app.MapAccountsEndpoints();
// ... etc
```

**Why after `UseAuthorization`:** redirects shouldn't bypass auth. If someone hits `/old-private-resource-url`, the redirect happens, then the destination goes through auth check.

**Why before module endpoints:** if a redirect matches, we short-circuit before any module endpoint handler runs.

---

## 4. Hit Recorder BG Service

`ContentSeo.Infrastructure/BackgroundServices/SeoRedirectHitFlushService.cs`:

```csharp
internal sealed class SeoRedirectHitFlushService(
    IServiceProvider sp,
    ISeoRedirectHitQueue queue,
    ILogger<SeoRedirectHitFlushService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var hits = queue.DrainAll();  // returns IReadOnlyList<RedirectHit>
                if (hits.Count == 0) continue;
                using var scope = sp.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ContentSeoDbContext>();
                // Group by RedirectId, count each:
                var counts = hits
                    .GroupBy(h => h.RedirectId)
                    .Select(g => new { RedirectId = g.Key, Count = g.Count() });
                foreach (var c in counts)
                {
                    await ctx.SeoRedirects
                        .Where(r => r.Id == c.RedirectId)
                        .ExecuteUpdateAsync(
                            s => s.SetProperty(r => r.HitCount, r => r.HitCount + c.Count),
                            stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "SeoRedirectHitFlushService tick failed");
        }
    }
}
```

Uses EF Core 7+ `ExecuteUpdateAsync` for bulk SQL UPDATE without entity-tracking overhead.

---

## 5. WBS

| # | Step | Hours |
|---|---|---|
| 1 | `ISeoRedirectLookupService` + `SeoRedirectResult` in ContentSeo.Contracts | 0.5 |
| 2 | `SeoRedirectLookupService` impl with HybridCache | 2 |
| 3 | `ISeoRedirectHitQueue` + `SeoRedirectHitQueue` (Channel wrapper) | 1 |
| 4 | `SeoRedirectHitFlushService` BG | 1.5 |
| 5 | `SeoRedirectMiddleware` impl + path-skip rules | 2 |
| 6 | Program.cs wiring (place at correct pipeline position) | 0.5 |
| 7 | Unit test — lookup service (cache hit + miss + invalidation) | 1.5 |
| 8 | Integration test — middleware (skip /api/*, 301 happy path, 302 happy path, query string preservation, no-match passthrough) | 2 |
| 9 | Load test — 1000 RPS for 60 sec, hit counts accurate to within 1% | 0.5 |
| 10 | PR + review | 0.5 |
| **Total** | | **12h** |

---

## 6. Acceptance

1. **Functional:**
   - GET `/old-blog-slug` (with row in `ContentSeo.SeoRedirects` mapping to `/new-blog-slug` 301) returns `301 Location: /new-blog-slug` with original query string preserved.
   - GET `/api/v1/places` (API path) does NOT trigger middleware DB lookup.
   - GET `/some-random-path` (no redirect row) returns whatever the next middleware/endpoint returns (likely 404).

2. **Performance:**
   - p95 latency overhead from middleware on cache hit: < 5ms.
   - p95 latency overhead on cache miss + DB lookup: < 30ms.
   - 1000 RPS for 60 sec → HitCount reflected in DB within 15 sec of test end (10 sec flush + 5 sec slack).

3. **Tests:**
   - 5 unit tests for `SeoRedirectLookupService` (cache miss → DB hit → cache populated; cache hit → no DB; null → null; lowercase normalization; tag invalidation).
   - 6 integration tests for middleware (skip patterns × 4, 301 happy, 302 happy, query string).
   - 1 BG service test — drain queue, verify SQL UPDATE issued.

4. **Documentation:**
   - `agent-context.md §11.2 Middleware` table row added: `SeoRedirectMiddleware` 5-min cache, 301/302 from `ContentSeo.SeoRedirects`, skip `/api/*`.
   - YallaJo.md middleware-order doc reaffirmed at position 12.
   - No new ADR needed (this is implementation of existing decision from ContentSeo sprint).
