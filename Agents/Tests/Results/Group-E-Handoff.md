# Group E — Handoff (next session)

**Scope:** build 6 missing features identified during 2026-05-29 gap sweep.
**Total estimate:** 5–8 hours.
**Recommended order:** smallest → largest, so each merge unblocks a Playwright test.

---

## §1 Backlog (smallest first)

| # | Finding | Feature | File(s) to touch | Est. | Notes |
|---|---|---|---|---|---|
| 1 | F12.3 | `MapGet("/faq", ...)` global list | `ContentSeo.Presentation\FaqItemEndpoints.cs` | 30 min | ✅ **DONE 2026-05-29.** Added `GetAllFaqItemsQuery` + handler + `MapGet("/faq")`. Paginated (`PaginatedResult<FaqItemDto>`), optional `entityType` filter, `activeOnly=true` default, Accept-Language pass-through, `AllowAnonymous`. LSP clean. **Verify post-restart:** `GET /api/v1/seo/faq?page=1&pageSize=10` expect 200 with `{items, totalCount, page, pageSize}`. |
| 2 | F6 | `MapGet("/places/businesses", ...)` list | `ContentPlaces.Presentation\Endpoints\Business\BusinessEndpoints.cs` | 1 h | ✅ **DONE 2026-05-29.** Added `MapGet("/places/businesses")` that delegates to existing `SearchBusinessesQuery` with `Query=null`. Reuses cache (`ICacheableQuery` via the search query, tag `TagBusinesses`), `ILogger`, public `Status=Approved` filter. `AllowAnonymous`. LSP clean. **Verify post-restart:** (a) `GET /api/v1/places/businesses?page=1&pageSize=10` → 200 paginated; (b) `GET /api/v1/places/businesses?country=Jordan` → 200 filtered; (c) `GET /api/v1/places/businesses?pageSize=999` → 200 capped to 50. |
| 3 | F11 | Wishlist endpoints | `Social.Presentation\Endpoints\FavoriteEndpoints.cs` | 1–2 h | ✅ **NOT_A_BUG 2026-05-29.** Already built as "Favorites". Real path: `/api/v1/social/favorites` (POST add, GET list cursor-paginated, DELETE `/{entityType}/{entityId}` idempotent, GET `/check/{entityType}/{entityId}`). All have `MustHavePermissionAttribute` + `ICacheableQuery` + max-500-per-user rule. Test sweep used `/api/v1/wishlist` (doesn't exist) — same pattern as F12.1/F12.2 path correction. |
| 4 | F11 | Support tickets endpoints | `Messaging.Presentation\Endpoints\SupportTicketEndpoints.cs` | 1–2 h | ✅ **NOT_A_BUG 2026-05-29.** Already built. Real prefix: `/api/v1/support` (registered at `MessagingEndpoints.cs:15`). Test sweep used `/api/v1/support-tickets` — same path correction pattern. |
| 5 | F11 | Blogs/creators endpoints | `ContentBlogs.Presentation\Endpoints\Creator\` | 1–2 h | ✅ **NOT_A_BUG 2026-05-29.** Already built — 15 public endpoints at `/api/v1/blogs/creators/*` (`/niches`, `/profiles/{slug}`, `/profiles/{id}/followers`, `/applications`, `/applications/mine`, `/applications/{id}/submit`, `/profile/mine`, `/profiles/{id}/follow`, `/profiles/{id}/following`, `/invitations/redeem`, `/profile/mine/avatar`) + 14 admin endpoints at `/api/v1/blogs/admin/creators/*`. Test sweep hit `/blogs/creators` with no sub-segment → no MapGet("/") so 404. Real entry points are sub-routes. |
| 6a | F10 | SignalR notification hub | `Messaging.Presentation\Hubs\NotificationHub.cs` | — | ✅ **NOT_A_BUG 2026-05-29.** Already built. Registered at `MessagingEndpoints.cs:19` `endpoints.MapHub<NotificationHub>("/hubs/notifications")`. Test sweep used `/api/v1/hubs/notifications/negotiate` — wrong prefix; real path is `/hubs/notifications/negotiate` (no `/api/v1`). |
| 6b | F10 | SignalR tracking hub | `Tracking.Presentation\Hubs\TrackingHub.cs` (new) + endpoint registration | 2–3 h | ⏳ **GENUINELY NOT_BUILT.** `Tracking.Presentation` has 0 `MapHub` calls and no `Hubs/` folder. Need: TrackingHub.cs class extending `Hub` with `JoinTour(Guid bookingId)` / `PushLocation(GeoSnapshot)` / `LeaveTour` methods; `[Authorize]` attribute; per-booking group membership; map at `/hubs/tracking`. **Heaviest remaining Group E item.** |

---

## §2 First 10 minutes of next session (do this)

1. Re-read this file + `Findings-Rolling.md` F6/F10/F11/F12.3 + `Gap-Report-2026-05-29.md` §1 Group E.
2. Verify what's already on disk before building new:
   ```
   ast_grep_search lang=csharp pattern='MapGet("/faq", $$$)' path=ContentSeo.Presentation
   ast_grep_search lang=csharp pattern='MapGet("/places/businesses", $$$)' path=ContentPlaces.Presentation
   list_dir Social.Presentation, Messaging.Presentation, ContentBlogs.Presentation, Tracking.Presentation
   ```
3. Pick item #1 (FAQ list) as warmup. Build → LSP → restart API → curl. **Establish the per-feature loop.**
4. Iterate items #2 → #6.

---

## §3 What NOT to redo

- Group A/B/D code patches are on disk and LSP-clean. **Do not re-edit:**
  - `Security.Infrastructure\Seeding\RolePermissionMapping.cs`
  - `YallaJo.Api\ExceptionHandlers\GlobalExceptionHandler.cs`
  - `Analytics.Application\Queries\GetTrending\GetTrendingQueryHandler.cs`
  - `ContentTours.Application\Features\TourGuides\IntegrationEventHandlers\ProviderApprovedIntegrationEventHandler.cs`
- All 13 findings F1–F13 + F14 already triaged. **Do not re-investigate** — read the ledger.
- 8 seed users already in DB. Don't reseed.

---

## §4 Verification still owed (independent of Group E)

User has not yet restarted API to verify Groups A/B/D. After restart, the next session should run these 5 calls **before** starting Group E:

| Call | Expected | Was |
|---|---|---|
| userA `GET /api/v1/accounts/profile` | 200 | 403 (F4) |
| userA `GET /api/v1/booking/my-bookings` | 200 | 403 (F9) |
| guide-approved `GET /api/v1/provider/status` | 200 | 403 (F3) |
| admin `GET /api/v1/admin/providers` (no `?page`) | 400 | 500 leak (F5/F13) |
| admin `GET /api/v1/trending` | 200 `[]` | 500 (F7) |

For F1, the seeded guide-approved row will still `/guides/me` 404 (outbox fired before handler existed). To prove F1: as admin, approve a NEW provider application → fires new outbox → handler runs → that user's `/guides/me` returns 200.

---

## §5 Where the canonical state lives

- **Code state:** working tree at `C:\Users\admin1\source\repos\YallaJo\` (no commits made — review `git status` first).
- **Findings ledger:** `Agents\Tests\Results\Findings-Rolling.md` (F1–F14).
- **Fix plan:** `Agents\Tests\Results\Gap-Report-2026-05-29.md`.
- **Per-module reports:** `Agents\Tests\Results\{Module}-2026-05-29.md`.
- **Playwright master index:** `Agents\Playwright-Test-Scenarios.md` + `Agents\Tests\Playwright-*.md` + `Agents\Tests\Playwright-APIOnly-Adapter.md`.
- **Seeders:** `YallaJo.SharedKernel.Infrastructure\Data\SeedIdentityProfiles.cs` (24 users), `Security.Infrastructure\Persistence\Seeding\SecurityDbInitializer.cs`, `Accounts.Infrastructure\Persistence\Seeding\AccountsDbInitializer.cs`, `Auth.Infrastructure\Persistence\Seeding\AuthDbInitializer.cs`, `Accounts.Infrastructure\Persistence\Seeding\AccountsProviderApplicationSeeder.cs` (4 provider apps).

---

**End of handoff. Resume Group E item #1 in the next session.**
