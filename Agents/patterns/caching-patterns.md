# Caching Patterns — Code Patterns

> Reference file for `Agents/agent-context.md`. Contains code examples only.
> Rules and decision logic are in the main file — this file is for copy-paste code.

---

## HybridCache Registration

```csharp
// In Program.cs or SharedKernel DI — once per application
// Package: Microsoft.Extensions.Caching.Hybrid (>= 9.3.0)
builder.Services.AddHybridCache(options =>
{
    // Default expiration for entries that don't specify their own
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(15),           // Absolute max lifetime
        LocalCacheExpiration = TimeSpan.FromMinutes(5),  // L1 in-memory expiration (re-sync from L2)
    };

    // Max size per serialized cache entry (protects against caching giant payloads)
    options.MaximumPayloadBytes = 1024 * 1024; // 1 MB
});

// Optional: Add Redis as L2 distributed cache (add when scaling to multiple servers)
// builder.Services.AddStackExchangeRedisCache(options =>
// {
//     options.Configuration = builder.Configuration.GetConnectionString("Redis");
//     options.InstanceName = "YallaJo:";
// });
```

## HybridCache GetOrCreateAsync Pattern

```csharp
// ✅ CORRECT — Cache-aside in one atomic call with stampede prevention
public sealed class ListCategoriesQueryHandler(
    ICategoryRepository repo,
    HybridCache cache)
    : IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(
        ListCategoriesQuery request, CancellationToken ct)
    {
        var cacheKey = request.CacheKey;  // From ICacheableQuery

        var dtos = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                // This factory runs ONCE even if 50 concurrent requests arrive
                var entities = await repo.GetAllAsync(
                    filter: c => request.ActiveOnly ? c.IsActive : true,
                    orderBy: q => q.OrderBy(c => c.SortOrder),
                    ct: cancel);

                return entities
                    .Select(c => CategoryDto.From(c, []))
                    .ToList() as IReadOnlyList<CategoryDto>;
            },
            options: new HybridCacheEntryOptions
            {
                Expiration = request.CacheDuration ?? TimeSpan.FromMinutes(15),
            },
            tags: request.Tags,  // From ICacheableQuery — e.g., ["categories"]
            cancellationToken: ct);

        return Result<IReadOnlyList<CategoryDto>>.Success(dtos);
    }
}
```

## HybridCache Tag-Based Eviction

```csharp
// ✅ In command handlers — invalidate by tag, not individual keys
public sealed class CreateCategoryCommandHandler(
    ICategoryRepository repo,
    IContentCoreUnitOfWork uow,
    HybridCache cache) : ICommandHandler<CreateCategoryCommand, CreateCategoryResult>
{
    public async Task<Result<CreateCategoryResult>> Handle(
        CreateCategoryCommand request, CancellationToken ct)
    {
        var category = Category.Create(request.Name, request.Slug);
        await repo.AddAsync(category, ct);
        await uow.SaveChangesAsync(ct);

        // One call invalidates ALL category-related cache entries
        // (list:active, list:all, list:with-translations, by-parent, etc.)
        await cache.RemoveByTagAsync("categories", ct);

        return Result<CreateCategoryResult>.Created(
            new CreateCategoryResult(category.Id, category.Name, category.Slug));
    }
}

// For entity-specific invalidation (e.g., update a single category):
await cache.RemoveByTagAsync($"category:{category.Id}", ct);  // Instance tag
await cache.RemoveByTagAsync("categories", ct);                 // List tag
```

## HybridCache Negative Caching

```csharp
// ✅ Cache "not found" results to prevent cache penetration attacks
public sealed class GetTagByIdQueryHandler(
    ITagRepository tagRepository,
    HybridCache cache)
    : IQueryHandler<GetTagByIdQuery, TagDto>
{
    public async Task<Result<TagDto>> Handle(
        GetTagByIdQuery request, CancellationToken ct)
    {
        // Cache key includes the ID — unique per entity
        var cacheKey = $"cc:tag:{request.Id}";

        // Use a nullable wrapper so we can cache "not found"
        var cached = await cache.GetOrCreateAsync<TagDto?>(
            cacheKey,
            async cancel =>
            {
                var tag = await tagRepository.GetByIdAsync(request.Id, cancel);
                if (tag is null)
                    return null;  // This null WILL be cached with short TTL

                return new TagDto(tag.Id, tag.Name, tag.Slug, tag.IsActive);
            },
            options: new HybridCacheEntryOptions
            {
                // Short TTL for potential not-found entries
                // (entity may be created soon after a miss)
                Expiration = TimeSpan.FromSeconds(60),
            },
            tags: ["tags", $"tag:{request.Id}"],
            cancellationToken: ct);

        if (cached is null)
            return Result<TagDto>.Failure(
                new Error("Tag.NotFound", $"Tag '{request.Id}' was not found."),
                Outcome.NotFound);

        return Result<TagDto>.Success(cached);
    }
}
```

## Updated ICacheableQuery Interface

```csharp
// ✅ Updated to support HybridCache tags
public interface ICacheableQuery
{
    /// <summary>
    /// Unique, deterministic cache key derived from the query's parameters.
    /// Use the module's static CacheKeys class to build this consistently.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// How long the cached result is valid. null uses the behavior default (15 minutes).
    /// </summary>
    TimeSpan? CacheDuration { get; }

    /// <summary>
    /// Tags for group invalidation. Command handlers call
    /// cache.RemoveByTagAsync(tag) to evict all entries with a given tag.
    /// </summary>
    IReadOnlyList<string> Tags { get; }
}

// Example implementation:
public sealed record ListCategoriesQuery(
    bool ActiveOnly = false,
    Guid? ParentCategoryId = null,
    bool WithTranslations = false)
    : IQuery<IReadOnlyList<CategoryDto>>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.CategoryList(ActiveOnly, ParentCategoryId, WithTranslations);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
    public IReadOnlyList<string> Tags => ["categories"];
}

public sealed record GetCategoryByIdQuery(Guid Id, bool WithTranslations = false)
    : IQuery<CategoryDto>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.Category(Id, WithTranslations);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
    public IReadOnlyList<string> Tags => ["categories", $"category:{Id}"];
}
```

## Updated QueryCachingBehavior (HybridCache)

```csharp
// ✅ Pipeline behavior rewritten for HybridCache
public sealed class QueryCachingBehavior<TRequest, TResponse>(
    HybridCache cache,
    ILogger<QueryCachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(15);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not ICacheableQuery cacheable)
            return await next();

        var key = cacheable.CacheKey;
        logger.LogDebug("[Cache] Key={CacheKey}", key);

        // GetOrCreateAsync handles: cache hit → return, cache miss → factory → store → return
        // Built-in stampede prevention: concurrent requests for same key coalesce
        var response = await cache.GetOrCreateAsync<TResponse>(
            key,
            async cancel => await next(),   // Factory: execute the actual handler
            options: new HybridCacheEntryOptions
            {
                Expiration = cacheable.CacheDuration ?? DefaultDuration,
            },
            tags: cacheable.Tags,
            cancellationToken: ct);

        return response!;
    }
}
```

---

## Legacy IMemoryCache Patterns (ContentCore current state — to be migrated)

> The patterns below document what ContentCore currently uses. New modules MUST NOT use these.
> They are preserved for reference during migration.

### IMemoryCache Cache-Aside Pattern (LEGACY)

```csharp
// Current ContentCore pattern — QueryCachingBehavior uses this
if (cache.TryGetValue(key, out TResponse? cached) && cached is not null)
    return cached;

var response = await next();
cache.Set(key, response, new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = duration,
    Priority = CacheItemPriority.Normal
});
return response;
// Problem: No stampede prevention. Race window between TryGetValue and Set.
```

### IMemoryCache Manual Invalidation (LEGACY)

```csharp
// Current ContentCore pattern — each handler manually removes keys
cache.Remove(ContentCoreCacheKeys.Tags(true));
cache.Remove(ContentCoreCacheKeys.Tags(false));
cache.Remove(ContentCoreCacheKeys.Tag(request.Id));
// Problem: Fragile — easy to forget a key permutation.
// HybridCache replacement: await cache.RemoveByTagAsync("tags", ct);
```

### IMemoryCache Stampede Prevention (LEGACY)

```csharp
// Manual SemaphoreSlim pattern — NOT needed with HybridCache
private static readonly SemaphoreSlim _lock = new(1, 1);

if (cache.TryGetValue(key, out cached))
    return cached;

await _lock.WaitAsync(ct);
try
{
    if (cache.TryGetValue(key, out cached)) // Double-check
        return cached;
    var data = await LoadFromDatabaseAsync(ct);
    cache.Set(key, data, TimeSpan.FromMinutes(30));
    return data;
}
finally { _lock.Release(); }
// Problem: Doesn't compose, static lock per handler, error-prone.
// HybridCache replacement: GetOrCreateAsync does this internally.
```

---

## Output Caching (Selective — Anonymous Endpoints Only)

```csharp
// In Program.cs
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.NoCache());  // Default: no caching
    options.AddPolicy("AnonymousShortCache", builder =>
        builder.Expire(TimeSpan.FromMinutes(5)).Tag("public"));
});
app.UseOutputCache();

// On specific endpoints — ONLY anonymous, read-only, identical-for-all-users
app.MapGet("/api/places/nearby", async ([AsParameters] GetNearbyPlacesRequest req, ISender sender) =>
{
    var result = await sender.Send(new GetNearbyPlacesQuery(req.Lat, req.Lng, req.RadiusKm));
    return result.ToApiResult();
})
.CacheOutput("AnonymousShortCache")
.AllowAnonymous();

// Invalidation in command handlers:
app.Services.GetRequiredService<IOutputCacheStore>()
    .EvictByTagAsync("public", ct);
```

---

## Redis L2 (Future — Add When Scaling)

```csharp
// When the app scales to multiple server instances, add Redis as L2:
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "YallaJo:";
});

// ZERO code changes to handlers — HybridCache automatically uses Redis as L2.
// L1 (in-memory) serves hot data. L2 (Redis) backs up and syncs across instances.

// Redis Failure Handling — HybridCache degrades gracefully:
// If Redis is down, L1 still works. When Redis comes back, L2 re-populates on next miss.
// No try/catch needed in handler code — HybridCache handles this internally.
```
