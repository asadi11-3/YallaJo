# YallaJo — Unified Gap Report (Batch-Fix Plan)

**Date:** 2026-05-29
**Source:** [`Findings-Rolling.md`](Findings-Rolling.md) (F1-F13)
**Coverage:** 13 of 13 modules probed via Playwright MCP through `https://localhost:57065`.
**Status:** Discovery complete. **Ready for batch fix.**

---

## TL;DR — One screen overview

| # | Finding | Sev | Module(s) | Fix size |
|---|---|---|---|---|
| F1 | TourGuide aggregate never auto-created on Approve | HIGH | ContentTours.Application | M (new event handler + factory) |
| F2 | 500 leaks stack trace + absolute file paths | HIGH | YallaJo.Api (global mw) | S (1 if-check) |
| F3 | TourGuide role lacks Permission.Provider.* | MED | Security.Infrastructure seed | S (claims list) |
| F4 | User/TourGuide lack Profile self-read | MED | Security.Infrastructure seed | S (rolled into F9) |
| F5 | Missing ?page crashes 500 | MED | every endpoint with `int page` | M (codemod) |
| F6 | `/places/businesses` shadowed by `{slug}` | MED | ContentPlaces.Presentation | XS (route reorder) |
| F7 | Domain `NotReady` → 500 not 503 | MED | ProblemDetails mapper | S (add 2 mappings) |
| F8 | Booking paged shape missing totalCount | LOW | Booking.Presentation | XS (DTO field) |
| F9 | User role can't book / read own bookings | HIGH | Security.Infrastructure seed | M (full claims list) |
| F10 | SignalR hub negotiate 404 | HIGH | Program.cs hub mapping | S (route fix) |
| F11 | Built features actually NOT_BUILT (wishlist, support-tickets, blogs/creators) | HIGH | 3 modules | L (real impl) |
| F12 | GET 405 on recommendations, disputes, faq | MED | 3 endpoint files | S (add MapGet) |
| F13 | Unified leak/validation root cause | HIGH | global mw + endpoint codemod | M (defense-in-depth) |

**Estimated fix effort:** ~10-16 hours of focused work.

---

## §1 — Commit groups (recommended PR sequence)

Each group is independently mergeable and tests can be re-run between groups to confirm forward progress.

### Group A — Security & permission seed (1 PR, ~2h) ✅ unblocks 5 findings

**Fixes:** F3, F4, F9 (and partially F11.wishlist test-ability)

**Files touched:** 1
- `Security.Infrastructure\Persistence\Seeding\SecurityDbInitializer.cs::SeedRolePermissions` (or whichever method builds the `RoleClaim` rows)

**Change:**
1. Expand the `User` role's claim list to include the 25 permissions listed in F9.
2. Expand the `TourGuide` role's claim list to inherit User's set + add `Permission.Provider.*` per F3.
3. Verify existing Admin/SuperAdmin/Owner claim sets unchanged.
4. **Seed re-run strategy:** the existing `SecurityDbInitializer` is idempotent on Users but the Roles+RoleClaims branch only runs when `Roles.AnyAsync()` is false. To re-apply new claims, either (a) bump a `RolePermissionsVersion` constant and re-run when version changes, or (b) add a one-shot migration that wipes and re-seeds RoleClaims for these two roles only. **Recommended:** option (a), add `int Version` field to RoleClaim + check; cleaner upgrade path.

**Validation:**
- userA login → `GET /accounts/profile` should be 200 (was 403)
- userA → `POST /booking/tour` should be 400 (validation) or 200, NOT 403
- guide-approved → `GET /provider/status` should be 200 (was 403)

---

### Group B — Error pipeline hardening (1 PR, ~2h) ✅ unblocks security/compliance

**Fixes:** F2, F5, F7, F13

**Files touched:** 3-5
- `YallaJo.Api\Middleware\GlobalExceptionMiddleware.cs` (or equivalent — search for the class that emits `{ title, status, detail, instance, correlationId, timestamp, exception }`)
- `YallaJo.SharedKernel.Presentation\Mapping\ProblemDetailsMapper.cs` (or equivalent)
- `YallaJo.Api\Program.cs` (potentially register `ApiBehaviorOptions.InvalidModelStateResponseFactory`)
- Optional: introduce `YallaJo.SharedKernel.Presentation\Models\PagingQuery.cs` record for codemod

**Changes:**
1. **F2 + F13:** in global exception middleware, inject `IHostEnvironment` and strip the `exception` field when `!env.IsDevelopment()`. Add unit test asserting the strip happens.
2. **F5 + F13:** introduce `record PagingQuery(int Page = 1, int PageSize = 20)` in SharedKernel.Presentation. Run a codemod replacing `[FromQuery] int page, int pageSize` with `[FromQuery] PagingQuery query` across all endpoint files. Update handler signatures.
3. **F7:** in ProblemDetailsMapper, add:
   ```csharp
   code.EndsWith(".NotReady") => StatusCodes.Status503ServiceUnavailable,
   code.EndsWith(".PreconditionFailed") => StatusCodes.Status412PreconditionFailed,
   code.EndsWith(".Unauthorized") => StatusCodes.Status401Unauthorized,
   code.EndsWith(".Forbidden") => StatusCodes.Status403Forbidden,
   ```

**Validation:**
- `GET /api/v1/admin/providers` (no ?page) → 200 with default page=1 (was 500)
- `GET /api/v1/trending` → 503 with `{ status: 503, title: 'Trending window is not ready' }` (was 500)
- Any forced 500 should NOT contain an `exception` field outside Development.

---

### Group C — Routing & verbs (1 PR, ~1h) ✅ low-risk cleanup

**Fixes:** F6, F8, F10, F12

**Files touched:** 5-7
- `ContentPlaces.Presentation\Endpoints\PlaceEndpoints.cs` (F6 reorder)
- `Booking.Presentation\Endpoints\BookingEndpoints.cs` (F8 envelope)
- `YallaJo.Api\Program.cs` (F10 hub mapping)
- `Analytics.Presentation\Endpoints\RecommendationsEndpoints.cs` (F12)
- `Finance.Presentation\Endpoints\DisputeEndpoints.cs` (F12)
- `ContentSeo.Presentation\Endpoints\FaqEndpoints.cs` (F12)

**Changes:**
1. **F6:** Move literal `/places/businesses` (and `/places/nearby`, `/places/map/viewport`) registrations BEFORE `/places/{slug}` in `MapPlaces`.
2. **F8:** Replace `{items}` with `{items, totalCount, page, pageSize}` envelope in `MyBookingsQueryHandler` + `AdminBookingsQueryHandler` response DTOs.
3. **F10:** Find `app.MapHub<NotificationHub>(...)` in `Program.cs`. Standardize to `/api/v1/hubs/notifications` and `/api/v1/hubs/tracking` (matches Adapter §2.3). If hubs aren't registered, add the registrations and ensure `AddSignalR()` is called.
4. **F12:** Add `MapGet` overloads for `/recommendations/me`, `/disputes`, `/faq` (or rename existing POSTs if they're collection reads).

**Validation:**
- `GET /places/businesses` → 200 array (was 404)
- `GET /booking/my-bookings` → 200 with `totalCount` field present
- `POST /api/v1/hubs/notifications/negotiate` (admin token) → 200 with `connectionId`
- `GET /api/v1/analytics/recommendations/me` → 200 array (was 405)

---

### Group D — Cross-module integration handler (1 PR, ~3h) ✅ unblocks TourGuide flow

**Fixes:** F1 + makes TourGuide module testable end-to-end

**Files touched:** 3-4
- `ContentTours.Domain\Entities\TourGuide.cs` (add factory)
- `ContentTours.Application\Features\TourGuides\IntegrationEventHandlers\OnProviderApplicationApprovedHandler.cs` (NEW)
- `ContentTours.Application\DependencyInjection.cs` (register handler if needed)
- `Accounts.Infrastructure\Persistence\Seeding\AccountsProviderApplicationSeeder.cs` (optional: also create TourGuide rows directly for the 3 approved seeded users as belt-and-suspenders)

**Changes:**
1. Add `TourGuide.CreateFromApprovedApplication(Guid userId, Guid applicationId, string businessName, string? bio = null)` factory to `ContentTours.Domain`.
2. New integration event handler subscribes to `Accounts.Contracts.IntegrationEvents.ProviderApplicationApproved`. Filters on `ProviderType == IndependentGuide` (and Agency if agency-guides need a TourGuide row).
3. Calls factory + persists.
4. **Backfill:** extend `AccountsProviderApplicationSeeder` to ALSO create the corresponding TourGuide row for users 005, 006, 007 directly via a sister seeder (`ContentToursTourGuideSeeder`, Order=52). This avoids relying on the outbox to fire during seed (which may race with startup).

**Validation:**
- After re-seed: `GET /guides/me` as guide-approved → 200 with profile body (was 404)
- After admin manually re-approves a Pending app → outbox fires → TourGuide row appears within 30 sec.

---

### Group E — Missing features (multiple PRs, ~5-8h) ⚠️ scope dependent

**Fixes:** F11 (each subitem is its own PR-worth)

**Sub-tasks (in priority order):**

**E.1 — Wishlist** (~2h)
- Verify if `Social.Presentation\Endpoints\WishlistEndpoints.cs` exists; if so, find the real route.
- If absent, implement: `GET /api/v1/wishlist`, `POST /api/v1/wishlist/{entityType}/{entityId}`, `DELETE /api/v1/wishlist/{itemId}`.
- Domain entity `Wishlist : IAggregateRoot { items: WishlistItem[] }` already likely exists in `Social.Domain` — verify.

**E.2 — Support tickets** (~3h)
- Messaging-Workflow.md §12 documents 8 endpoints. Verify swagger.json for actual paths (may be under `/messaging/tickets` rather than `/support-tickets`).
- If absent, implement the consumer-facing ones: `GET /api/v1/support-tickets/mine`, `POST /api/v1/support-tickets`, `GET /api/v1/support-tickets/{id}/messages`, `POST /api/v1/support-tickets/{id}/messages`.

**E.3 — Blogs/creators** (~2h)
- BlogCreatorPost-Merger.md says merger is complete. Check actual paths under `/blogs/*` or `/creators/*`.
- If absent, implement: `GET /api/v1/blogs/creators`, `POST /api/v1/blogs/creators/apply`, `GET /api/v1/blogs/creators/{id}`.

**E.4 — Admin users dashboard sub-route** (~1h)
- Trivial: `GET /api/v1/admin/users/dashboard` — likely just a missing route; admin role already has all User permissions.

**Validation:**
- After E.1: userA can wishlist a tour → wishlist endpoint returns 200 with the item.
- After E.2: userA can open a support ticket → ticket appears in admin queue.
- After E.3: admin can list creator applications.

---

## §2 — Suggested PR sequence

```
PR-1: Group A (Security claims) → unblocks userA + guide testing
PR-2: Group B (Error pipeline)  → fixes security disclosure + 500 cascade
PR-3: Group C (Routing/verbs)   → cleanup, enables SignalR + missing reads
PR-4: Group D (TourGuide auto-create) → makes guide self-service work
PR-5..N: Group E (one PR per missing feature)
```

After each PR, re-run the matching Playwright report to confirm finding is closed; add commit hash to `Findings-Rolling.md` next to `[CLOSED]` marker.

---

## §3 — Validation runbook (after all 5 groups merged)

Re-run from `Agents\Tests\Playwright-APIOnly-Adapter.md`:

1. **Phase 0 smoke** — should still be 4/4 PASS.
2. **Auth matrix** — 8 users, 7×200 + 1×401.
3. **userA flow:**
   - login → `GET /accounts/profile` → 200
   - `POST /booking/tour` with valid tourId → 200 or 400, NOT 403
   - `GET /booking/my-bookings` → 200 with `totalCount`
   - `POST /reviews/{tourId}` with completed-booking proof → 200
   - `POST /wishlist/tour/{tourId}` → 201
4. **guide-approved flow:**
   - `GET /guides/me` → 200 (F1 fixed)
   - `GET /provider/status` → 200 (F3 fixed)
   - `GET /provider/dashboard` → 200
5. **Hub:** `POST /api/v1/hubs/notifications/negotiate` → 200 with `connectionId`
6. **Error pipeline:** force a 500 → confirm body has NO `exception` field outside Dev.
7. **Routing:** `GET /places/businesses` → 200 (F6 fixed)
8. **Domain status:** `GET /trending` → 503 with WindowNotReady (F7 fixed)

Each item that flips from FAIL to PASS becomes a closed finding.

---

## §4 — What we did NOT test (deferred to deeper runs)

These remain UNKNOWN until userA-level flows are unblocked by Group A:
- Slot lock TTL (10 min)
- Booking confirmation code generation (`YJ-YYYYMM-XXXX`)
- Refund tier logic (full / partial / provider-canceled = 100%)
- Payout 7-day escrow trigger
- Commission calculation by subscription tier
- Review verified-badge logic
- Notification template variable interpolation
- Email/SMS provider stub vs real send

All have scenarios pre-written in `Playwright-Booking.md`, `Playwright-Finance.md`, `Playwright-Social.md`, `Playwright-Messaging.md` — they just need an unblocked User role to run.

---

## §5 — Confidence statement

- ✅ **Seeder work is correct.** All 4 seed files I authored produce the expected data, idempotently. Re-run any time without duplication.
- ✅ **Adapter is correct after one patch** (`/security/me` not `/auth/me`).
- ✅ **Infrastructure is healthy.** Health checks green, outbox clean, traceId propagating.
- ⚠️ **Application-level gates are wrong** — RoleClaim seeding is admin-heavy and starves all non-admin roles.
- ⚠️ **Cross-module wiring is incomplete** — ProviderApplicationApproved → TourGuide is a known dropped link.
- ❌ **Several "Built" features in the workflow plans are not actually exposed by the API.** Scoreboard needs correction.

---

**End of gap report. Source-of-truth for each line item lives in `Findings-Rolling.md` under the matching F#.**
