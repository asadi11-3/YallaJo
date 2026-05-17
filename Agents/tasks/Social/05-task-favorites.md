# TASK 2 — Favorites

> **Owner:** Fadwa (Beginner) — **Hours:** 16h — **Hard deadline:** Sun **2026-11-08 17:00**
> **Earliest start:** Wed 2026-10-21 (parallel with T1)
> **Endpoints:** 4 — **Depends on:** PW-1..PW-7

---

## 1. Endpoint List

| # | Method | Path | Permission | Notes |
|---|---|---|---|---|
| 1 | POST | `/api/v1/favorites` | `Favorite.Create` | Body: `{entityType, entityId}`. Returns 201 + Favorite DTO. 409 if exists. 422 if > 500. |
| 2 | DELETE | `/api/v1/favorites/{entityType}/{entityId}` | `Favorite.Delete` (self-only) | Idempotent — 204 even if not found. |
| 3 | GET | `/api/v1/favorites` | `Favorite.Read` (self-filter) | Cursor pagination, filter `?entityType=Tour\|Place\|Business`. |
| 4 | GET | `/api/v1/favorites/check/{entityType}/{entityId}` | `Favorite.Read` (self-only) | Returns `{isFavorited: bool}`. Cheap, used by frontend cards. |

---

## 2. Favorite Aggregate

```csharp
public sealed class Favorite : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public FavoriteEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public DateTime AddedAt { get; private set; }

    private Favorite() { }

    public static Result<Favorite> Add(
        Guid userId,
        FavoriteEntityType entityType,
        Guid entityId,
        int currentUserCount,
        int maxAllowed,
        DateTime now)
    {
        if (currentUserCount >= maxAllowed)
            return Result.Failure<Favorite>(new Error("Favorite.LimitExceeded", $"Maximum {maxAllowed} favorites allowed."), Outcome.UnprocessableEntity);

        var fav = new Favorite
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            AddedAt = now,
            CreatedAt = now,
            CreatedByUserId = userId,
        };
        fav.RaiseDomainEvent(new FavoriteAddedDomainEvent(fav.Id, userId, entityType, entityId, now));
        return Result.Success(fav);
    }

    public void Remove(DateTime now)
    {
        if (IsDeleted) return;   // idempotent
        IsDeleted = true;
        DeletedAt = now;
        MarkUpdated(now, UserId);
        RaiseDomainEvent(new FavoriteRemovedDomainEvent(Id, UserId, EntityType, EntityId, now));
    }
}
```

**Cap check** = current user's non-deleted favorite count loaded BEFORE calling `Add`. PDF 2 §1.7 + PDF 1 Wave 6 = max 500.

**Entity-type validation:** Use cross-module snapshots (PW-5):
- `FavoriteEntityType.Tour` → check `TourSnapshot.IsDeleted = false`
- `FavoriteEntityType.Place` → check `PlaceSnapshot.IsDeleted = false`
- `FavoriteEntityType.Business` → check `BusinessSnapshot.IsDeleted = false`

If snapshot missing or marked deleted, return `Error("Favorite.InvalidEntityType", ...) Outcome.ValidationError` — 400.

---

## 3. Handler Skeletons

```csharp
internal sealed class AddFavoriteCommandHandler(
    IFavoriteRepository repo,
    IPlaceSnapshotRepository placeSnapshots,
    ITourSnapshotRepository tourSnapshots,
    IBusinessSnapshotRepository businessSnapshots,
    ISocialUnitOfWork uow,
    ICurrentUser currentUser,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<AddFavoriteCommandHandler> logger,
    IOptions<FavoritesOptions> options)
    : IRequestHandler<AddFavoriteCommand, Result<FavoriteDto>>
{
    public async Task<Result<FavoriteDto>> Handle(AddFavoriteCommand command, CancellationToken ct)
    {
        // 1. Validate entity exists (snapshot lookup)
        var exists = command.EntityType switch
        {
            FavoriteEntityType.Tour     => await tourSnapshots.ExistsAsync(command.EntityId, ct),
            FavoriteEntityType.Place    => await placeSnapshots.ExistsAsync(command.EntityId, ct),
            FavoriteEntityType.Business => await businessSnapshots.ExistsAsync(command.EntityId, ct),
            _ => false
        };
        if (!exists) return Result.Failure<FavoriteDto>(new Error("Favorite.InvalidEntityType", "..."), Outcome.ValidationError);

        // 2. Already favorited?
        if (await repo.ExistsForUserAsync(currentUser.UserId, command.EntityType, command.EntityId, ct))
            return Result.Failure<FavoriteDto>(new Error("Favorite.AlreadyExists", "..."), Outcome.Conflict);

        // 3. Limit
        var count = await repo.CountForUserAsync(currentUser.UserId, ct);
        var add = Favorite.Add(currentUser.UserId, command.EntityType, command.EntityId, count, options.Value.MaxPerUser, timeProvider.GetUtcNow().UtcDateTime);
        if (add.IsFailure) return add.ToError<FavoriteDto>();

        repo.Add(add.Value);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync($"favorites:user:{currentUser.UserId}", ct);

        return Result.Success(FavoriteDto.FromEntity(add.Value));
    }
}
```

`FavoritesOptions` (in Social.Application/Options/) bound from `Social:Favorites` config section: `{ MaxPerUser: 500 }`.

---

## 4. Cache Strategy (S-R9 recap)

- `favorites:user:{userId}` (5 min) — list query
- `favorite-check:user:{userId}:type:{type}:id:{id}` (30 sec) — boolean check
- Both invalidated on Add / Remove

---

## 5. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Favorite aggregate + factory + Remove method | 2 | 2026-10-22 |
| 2 | EF config + migration `SocialCreateFavoritesIndexes` | 2 | 2026-10-23 |
| 3 | AddFavoriteCommand + Validator + Handler + 4 tests | 4 | 2026-10-27 |
| 4 | RemoveFavoriteCommand + Handler + 2 tests | 2 | 2026-10-28 |
| 5 | GetMyFavoritesQuery (cursor) + ICacheableQuery + 2 tests | 2 | 2026-10-30 |
| 6 | CheckFavoriteQuery + ICacheableQuery + 2 tests | 1 | 2026-10-31 |
| 7 | 4 endpoint wiring + Swagger XML docs | 1 | 2026-11-02 |
| 8 | Integration test: outbox `social.favorite.added.v1` round-trip | 1 | 2026-11-05 |
| 9 | PR review fixes | 1 | 2026-11-08 |
| **Total** | | **16h** | **Sun 2026-11-08** |

---

## 6. Acceptance Tests (8 cases)

1. POST favorite for existing tour → 201, outbox `social.favorite.added.v1` row exists.
2. POST favorite for deleted/non-existent tour → 400 `Favorite.InvalidEntityType`.
3. POST favorite already exists → 409 `Favorite.AlreadyExists`.
4. POST 501st favorite → 422 `Favorite.LimitExceeded`.
5. DELETE favorite that exists → 204, outbox `social.favorite.removed.v1` row (if we add it later — note: NOT in this sprint per S-R7, so just no row).
6. DELETE favorite that doesn't exist → 204 (idempotent).
7. GET favorites with no items → 200 + empty list.
8. GET favorites/check/Tour/{id} → returns `{isFavorited: true|false}` matching state.
