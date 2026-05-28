# Social Module — Fix Plan

> **Created**: 2025-01-27
> **Source**: `Agents/Plans/Social-Audit-Report.md` (Score 7.8/10)
> **Estimated Effort**: 4-6 hours

---

## Design Decisions

1. **Public reviews**: Add AllowAnonymous endpoint for entity reviews (already have GetPublicReviews query handler)
2. **HybridCache**: Add to GetRatingSummary, GetPublicReviews, CheckFavorite
3. **Permission comment**: Just fix the stale comment (code is correct)
4. **Warn/Ban**: Complete basic workflow — admin can warn (counter increment) and ban (status flag)
5. **Helpful vote query**: Wire existing entity to a public endpoint

---

## Fixes

### Fix 1 (MEDIUM): Public review listing endpoint

Add to `ReviewEndpoints.cs`:
- `GET /reviews/entity/{entityType}/{entityId}` — public listing with pagination, AllowAnonymous
- Uses existing `GetPublicReviewsQueryHandler`

**Files**: 1 endpoint modification

---

### Fix 2 (MEDIUM): Add HybridCache to high-traffic queries

| Query | Cache Key | TTL | Tags |
|---|---|---|---|
| GetRatingSummary | `social:rating:{entityType}:{entityId}` | 5min | `rating:{entityType}:{entityId}` |
| GetPublicReviews | `social:reviews:{entityType}:{entityId}:{page}` | 1min | `reviews:{entityType}:{entityId}` |
| CheckFavorite | `social:favorite:{userId}:{entityType}:{entityId}` | 5min | `favorites:{userId}` |
| GetMyFavorites | `social:favorites:{userId}:{page}` | 30s | `favorites:{userId}` |

**Files**: 4 query handler modifications + 1 new `SocialCacheKeys.cs`

---

### Fix 3 (MEDIUM): Complete Warn/Ban workflow

UserModerationRecord entity already exists. Need:
- `WarnUserCommand` + Handler — increments warning count, logs to ContentModerationLog
- `BanUserCommand` + Handler — sets banned flag, publishes `UserBannedIntegrationEvent`
- `UnbanUserCommand` + Handler — clears ban
- 3 validators
- Wire to ModerationEndpoints: POST /moderation/users/{userId}/warn, POST /ban, POST /unban

**Files**: 9 new files (3 commands + 3 handlers + 3 validators) + 1 endpoint modification + 1 integration event

---

### Fix 4 (LOW): Helpful vote query endpoint

Add to `ReviewEndpoints.cs`:
- `GET /reviews/{reviewId}/helpful` — returns vote count + whether current user voted

**Files**: 1 new query + handler + 1 endpoint addition

---

### Fix 5 (LOW): Fix stale permission catalog comment

Update the comment in `SocialPermissionCatalog.cs` from "19 permissions, 6 features" to "21 permissions, 7 features".

**Files**: 1 line change

---

### Fix 6 (LOW): Add 5 command validators

| Command | Rules |
|---|---|
| CreateReviewCommand | EntityType NotEmpty, EntityId NotEmpty, Rating 1-5, Content MaxLen(4000) |
| EditReviewCommand | ReviewId NotEmpty, Content MaxLen(4000), Rating 1-5 |
| SubmitReportCommand | TargetType NotEmpty, TargetId NotEmpty, Reason NotEmpty+MaxLen(2000) |
| AddFavoriteCommand | EntityType NotEmpty, EntityId NotEmpty |
| AddReviewReplyCommand | ReviewId NotEmpty, Content NotEmpty+MaxLen(2000) |

**Files**: 5 new validator files

---

### Fix 7 (LOW): Update plan document

- Correct permission count
- Document ReviewHelpfulVote flow
- Mark implemented sections
- Add Implementation Notes

**Files**: 1 plan doc

---

### Fix 8 (MEDIUM): Build verify

`dotnet build YallaJo.sln --no-restore`

---

## Execution Order

```
Fix 1 (public endpoint) ──────┐
Fix 2 (HybridCache) ──────────┤
Fix 3 (Warn/Ban) ─────────────┼── Fix 8 (build) → Fix 7 (doc)
Fix 4 (helpful vote query) ───┤
Fix 5 (permission comment) ───┤
Fix 6 (validators) ───────────┘
```

---

## Risk Assessment

| Risk | Mitigation |
|---|---|
| Cache staleness after review create/edit | Tag-based invalidation in command handlers |
| Warn/Ban integration event consumers | No consumers needed initially; Messaging will consume later |
| Public review listing performance | Cache + pagination limits; EntityRatingCache pre-computed |
