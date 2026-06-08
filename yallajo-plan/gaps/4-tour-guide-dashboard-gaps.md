# Gap Report — `4-tour-guide-dashboard.md` vs. shipped code

> ## 🔁 BACKEND PASS (follow-up, scope extended to backend)
> A backend audit of the Guide feature (ContentTours + Booking + Accounts modules) was run after the web layer was resolved. **Fixed (high-confidence, precedent-backed NRE→500, same class as the §5 Agency fix):**
> - `CreateGuideDiscountCommandValidator` previously validated only `DiscountType`; **added `NotEmpty` for `Name` + `Currency`** (and `Description` max-length). `GuideDiscount.Create` does `name.Trim()` → a null `Name` was a NullReferenceException → 500; now a clean 400.
> - **Added `UpdateGuideDiscountCommandValidator`** (none existed) — `GuideDiscount.Update` does `name.Trim()`; same NRE→500 closed. Build: `0 Error(s)`; LSP: 0 errors.
>
> **Backend items — ALL FIXED this session (Oracle-triaged, then implemented + build-verified):**
> - 🔴→✅ **FIXED — guide-offering ownership gap (security):** the 6 schedule + pricing-tier create/update/delete handlers (`GuideTourOffering/Schedule/*`, `.../PricingTier/*`) now inject `ICurrentUser` + `ITourGuideRepository`, resolve the caller's guide via `GetByUserIdAsync`, and return `Forbidden` unless the offering/schedule/tier `TourGuideId == callerGuide.Id` (mirrors `UpdateTourGuideProfileCommandHandler`). A guide can no longer mutate another guide's offering.
> - 🟠→✅ **FIXED — data loss:** `ApplyForTourCommandHandler` now threads `request.ProposedScheduleJson` into `GuideApplication.Create(...)` (the field was already domain-mapped + EF-migrated; the handler was simply dropping it).
> - 🟡→✅ **FIXED:** `ApproveGuideApplicationCommandHandler` now captures `GuideTourOffering.Reinstate()`'s `Result` and returns failure before `SaveChanges`.
> - ❌ **FALSE POSITIVE (confirmed):** the audit agent's claim that `UpdateTourGuideProfileCommandHandler` "ignores the domain Result" is wrong — it has an ownership check + `try/catch(ArgumentException)`. No change made (correctly).
> - Build: `ContentTours.Application` `0 Error(s)`; LSP clean; security + data-loss fixes spot-verified by direct read.
>
> ## ✅ RESOLVED — all gaps closed (web + docs, prior pass)
> **Docs (Phase 1):** G1–G9 applied to `4-tour-guide-dashboard.md`:
> - **G1** §5.7 renamed "My Tours / Offerings", route → `/guide/tours` (+ `/guide/tours/{tourId}`); "no `/guide/offerings`" note added.
> - **G2** §5.7 pricing-tier/schedule **edit** buttons removed (Add + Delete only; private-tour enable/`delete`; offering/remove) with an explicit "add-then-delete-and-re-add" note.
> - **G3** §5.9 split into §5.9a Applications (`/guide/applications` + `/apply`) and §5.9b Proposals (`/guide/proposals` + `/create` + `/{id}/submit`).
> - **G4** §5.10 join-requests → flat `POST /guide/join-requests/approve|reject` (id in body).
> - **G5** §5.11 agency → `/guide/agency/apply|invitations/{id}/accept|decline|leave`.
> - **G6** §5.6 availability (`/add`, `/{blockId}/delete`), §5.2 profile (canonical `/guide/profile/*` buttons), §5.8 discounts (`/create`, `/{id}/edit`, `/{id}/delete` — has edit).
> - **G7** added **§5.12 Reviews** (`/guide/reviews`, read-only) + page-table row.
> - **G8** §5.1 notes `/guide` = dashboard landing (`DashboardController` serves both `/guide` and `/guide/dashboard`).
> - **G9** AgencyRoster cross-reference confirmed (no change).
> - Header gained a **BFF-route-reality** note listing the **14 shipped controllers** (the report's "16" was a miscount) + 14-item sidebar nav; every page now shows the BFF route with the `*(API: ...)*` path alongside.
>
> **Code (Phase 2):** a deep re-audit of all 14 Guide controllers found **no code defect** — unlike §3, there is no route collision: all 32 guide POST actions carry `[ValidateAntiForgeryToken]`, every `[HttpGet/HttpPost]` route literal is unique, no `TODO`/`NotImplementedException`, and the permission attributes (`GuideDashboard.Read`, `TourGuideProfile.DeleteOwn`, `GuideOffering.Delete`, `AgencyRoster.*`; all else `[Authorize]`) exactly match the plan. This area is a docs-only reconciliation; no code changes were required.


> **Method:** deep code-vs-plan audit against the **shipped** `Areas/Guide/Controllers/*` (16 controllers).
> The plan's permissions + the API-vs-BFF-verb principle were already corrected in the earlier §4 audit; this report pins down the
> **exact BFF routes, the page structure, and over-claimed actions** vs. the real controllers + the shipped sidebar nav.
>
> **Scope:** `yallajo-plan/4-tour-guide-dashboard.md` (§5 guide).
> **Severity:** 🔴 plan contradicts code · 🟠 route/structure wrong · 🟡 cosmetic/clarity.
> **Status:** ❌ not built · ✏️ route differs · ➕ shipped but missing from plan · ✅ matches.

---

## 0. Summary

| Topic | Verdict |
|-------|---------|
| **Permissions** | ✅ Accurate (prior §4 audit): `GuideDashboard.Read` (dashboard+tier), `TourGuideProfile.DeleteOwn`, `GuideOffering.Delete`; everything else `[Authorize]`. Confirmed against shipped attributes. |
| **§5.7 route** | 🔴 Plan says `/guide/offerings` — **no such route exists**. The offerings page is `MyToursController` at **`/guide/tours`** → `/guide/tours/{tourId}`. |
| **§5.7 editing** | 🔴 Plan lists "Save → `PUT .../pricing-tiers/{tierId}`" and schedule edit — **no edit/update action ships**; only **Add + Delete** for pricing-tiers & schedules. |
| **§5.9 split** | 🟠 Applications + Proposals are **two controllers** (`/guide/applications` + `/guide/proposals`), not one §5.9 page. |
| **Reviews page** | ➕ A shipped **`ReviewsController` (`/guide/reviews`)** + sidebar "Reviews" item — the plan has **no §5.x Reviews page**. |
| **§5.10 / §5.11 routes** | 🟠 Join-request approve/reject take id in **body** (`/guide/join-requests/approve`), not route; agency leave is `POST /guide/agency/leave` (not `DELETE /guides/me/agency`). |
| **Sidebar nav** | ➕ 14 nav items: Dashboard, Profile, Analytics, Earnings, Tier, Availability, MyTours, Discounts, Applications, Proposals, JoinRequests, **Reviews**, Agency, AgencyRoster. |

---

## 1. 🔴 Plan-contradicts-code

### G1 — §5.7 "My Offerings" route is `/guide/tours`, not `/guide/offerings`
- **Plan:** Stack Route `/guide/offerings`.
- **Code (`MyToursController`):** `GET /guide/tours` (list) → `GET /guide/tours/{tourId}` (the `Offering` action = the per-tour offering editor). **Zero `/guide/offerings` routes exist.** Sidebar nav key is `MyTours`.
- **Action:** §5.7 Route → `/guide/tours` (list) + `/guide/tours/{tourId}` (offering editor). Consider renaming the page "My Tours / Offerings" to match the shipped nav.

### G2 — §5.7 pricing-tier & schedule **edit** actions don't exist (Add + Delete only)
- **Plan:** "Add/Save/Remove Pricing Tier → `POST/PUT/DELETE .../pricing-tiers[/{tierId}]`"; same for schedules.
- **Code (`MyToursController`):** only:
  - `POST /guide/tours/{tourId}/pricing-tiers` (**AddPricingTier**) · `POST .../pricing-tiers/{tierId}/delete` (**DeletePricingTier**)
  - `POST /guide/tours/{tourId}/schedules` (**AddSchedule**) · `POST .../schedules/{scheduleId}/delete` (**DeleteSchedule**)
  - `POST /guide/tours/{tourId}/private-tour` (**Enable**) · `/private-tour/delete` (**Disable**)
  - `POST /guide/tours/{tourId}/offering/remove` (`GuideOffering.Delete`)
  - **No edit/update action** for tiers or schedules.
- **Action:** drop the "Save / edit pricing tier / edit schedule" buttons (or mark them **unbuilt**); the shipped UX is **add-then-delete-and-re-add**, not in-place edit.

---

## 2. 🟠 Route / structure gaps

### G3 — §5.9 Applications & Proposals are two separate controllers
- **Plan:** one §5.9 page at `/guide/proposals`.
- **Code:**
  - `ApplicationsController`: `GET /guide/applications` · `POST /guide/applications/apply`.
  - `ProposalsController`: `GET /guide/proposals` · `POST /guide/proposals/create` · `POST /guide/proposals/{id}/submit`.
- **Action:** split §5.9 into §5.9a Applications (`/guide/applications`) and §5.9b Proposals (`/guide/proposals`); fix button routes (`/applications/apply`, `/proposals/create`, `/proposals/{id}/submit`).

### G4 — §5.10 Join Requests: id is in the body, not the route
- **Plan:** `POST /booking/join-requests/{id}/approve` · `/reject`.
- **Code (`JoinRequestsController`):** `GET /guide/join-requests` · **`POST /guide/join-requests/approve`** · **`POST /guide/join-requests/reject`** (the request id is posted in the form body, not the URL).
- **Action:** buttons → `POST /guide/join-requests/approve|reject` (id in body).

### G5 — §5.11 Agency BFF routes differ
- **Plan:** accept/decline `POST /guides/invitations/{id}/accept|decline`; apply `POST /guides/agencies/{agencyUserId}/apply`; leave `DELETE /guides/me/agency`.
- **Code (`AgencyController`):** `GET /guide/agency` · `POST /guide/agency/apply` · `POST /guide/agency/invitations/{id}/accept` · `/decline` · **`POST /guide/agency/leave`**.
- **Action:** buttons → `/guide/agency/apply`, `/guide/agency/invitations/{id}/accept|decline`, `/guide/agency/leave`.

### G6 — §5.6 Availability & §5.2 Profile & §5.8 Discounts exact routes
- **Availability (`AvailabilityController`):** `GET /guide/availability` · `POST /guide/availability/add` · `POST /guide/availability/{blockId}/delete`.
- **Profile (`ProfileController`):** `GET /guide/profile` · `POST /guide/profile/update` · `/languages` · `/languages/delete` · `/specializations` · `/avatar` · `/cover` · `/deactivate`. *(Plan's example routes already match ✓ — promote them from "example" to the canonical button list.)*
- **Discounts (`DiscountsController`):** `GET /guide/discounts` · `POST /guide/discounts/create` · `/{id}/edit` · `/{id}/delete`. *(Discounts **does** have edit, unlike offerings.)*
- **Action:** lock §5.6/§5.2/§5.8 button routes to these.

---

## 3. ➕ Shipped-but-missing-from-plan

### G7 — Guide Reviews page (`/guide/reviews`) has no §5.x
- **Code:** `ReviewsController` → `GET /guide/reviews`; sidebar nav item **"Reviews"**. (Read surface — the guide views reviews on their tours/profile.)
- **Action:** add a **§5.12 Reviews** page (`/guide/reviews`, `[Authorize]`, read-only list of reviews about the guide/their offerings). The plan currently has no guide-side reviews surface at all.

### G8 — Dashboard route also serves `/guide`
- **Code (`DashboardController`):** `GET /guide` **and** `GET /guide/dashboard` (both `GuideDashboard.Read`). Plan only lists `/guide/dashboard`.
- **Action:** note `/guide` is the area landing (= dashboard).

### G9 — AgencyRoster is the §6 surface, correctly separated
- `AgencyRosterController` (`/guide/agency/roster*`, `AgencyRoster.*`) — already flagged in the plan's §5.11 note ✅ and is the subject of the §5/§6 agency gap. No change here beyond confirming the cross-reference.

---

## 4. ✅ Confirmed-correct

- **Permissions** — `GuideDashboard.Read` (Dashboard + Tier), `TourGuideProfile.DeleteOwn` (profile deactivate), `GuideOffering.Delete` (offering remove); all else `[Authorize]`. ✅ (matches shipped).
- **API-vs-BFF verb note** in the header ✅ — and the deep audit confirms every mutation is page-scoped `POST`.
- **`ST1` dropped everywhere** ✅ — no Guide DTO exposes RowVersion (re-confirmed).
- **§5.2 Profile example routes** (`/guide/profile/cover`, `/languages/delete`, etc.) ✅ exactly match shipped.
- **§5.3 Analytics, §5.4 Earnings, §5.5 Tier** read routes ✅ (`/guide/analytics`, `/guide/earnings`, `/guide/tier`).

---

## 5. Recommended plan edits (apply order)

1. **G1** §5.7 Route → `/guide/tours` (+ `/guide/tours/{tourId}`); rename to "My Tours / Offerings."
2. **G2** §5.7 remove pricing-tier/schedule **edit** buttons (Add + Delete only); private-tour = enable/`delete`.
3. **G3** split §5.9 → Applications (`/guide/applications`) + Proposals (`/guide/proposals`).
4. **G7** add **§5.12 Reviews** (`/guide/reviews`).
5. **G4/G5/G6** fix join-request (id-in-body), agency (`/guide/agency/*`), availability/discounts button routes.
6. **G8** note `/guide` = dashboard landing.

> **Net:** permissions are correct, but the plan's **page map is off**: §5.7 is at `/guide/tours` (not `/guide/offerings`) and **can't edit** tiers/schedules (add+delete only); Applications & Proposals are **two pages**; and a whole **Reviews page ships with no §5.x**. The join-request, agency, and availability button routes also differ from the plan. G1/G2 are the behavioral corrections; G7 is a missing page.

---

## 6. 🔁 Re-audit verification round (no-regression full re-sweep)

A complete code-first re-audit of the entire **Tour Guide Dashboard (Area `Guide`, plan §5)** was performed to prove no regression against the "ALL GAPS RESOLVED" state above. **Result: ZERO open code gaps. NO regression. NO code changes required this round.**

### Hotspots re-verified end-to-end (the 3 prior backend fixes — all intact):
- **Offering ownership (security):** all 6 `GuideTourOffering` handlers under `…/Commands/GuideTourOffering/{Schedule,PricingTier}/{Create,Update,Delete}` confirmed to inject `ITourGuideRepository` + `ICurrentUser`, resolve `callerGuide = GetByUserIdAsync(currentUser.UserId)`, and return `Result.Failure(Error("GuideTourOffering.NotOwner",…), Outcome.Forbidden)` unless `entity.TourGuideId == callerGuide.Id`. All carry a `DbUpdateConcurrencyException → Conflict` guard. ✅
- **Discount validators (NRE→500):** `CreateGuideDiscountCommandValidator` (NotEmpty `Name`+`Currency`, `Description` max-length) and `UpdateGuideDiscountCommandValidator` (NotEmpty `DiscountId`+`Name`) both present and correct. ✅
- **Data loss:** `ApplyForTourCommandHandler` threads `request.ProposedScheduleJson` into `GuideApplication.Create(...)`; `ApproveGuideApplicationCommandHandler` captures `existingOffering.Reinstate()`'s `Result` and returns failure before `SaveChanges`. ✅

### All 14 Guide BFF controllers re-audited (`src/Hosts/YallaJo.Web/Areas/Guide/Controllers/`):
Every controller: `[Area("Guide")]` + `[Authorize]`, `public sealed`, `BaseController`, **Facade-only injection** (no `IApiClient`/`HttpClient`), every action `async Task<IActionResult>` + trailing `CancellationToken`, `GuardSignOut` after every facade call, **every POST has `[ValidateAntiForgeryToken]` + PRG**, route literals unique and matching plan §5, permission attributes matching perm reality exactly (`GuideDashboard.Read` on Dashboard+Tier, `TourGuideProfile.DeleteOwn` on Profile/deactivate, `GuideOffering.Delete` on MyTours/offering-remove, `AgencyRoster.*` on AgencyRosterController; everything else `[Authorize]`-only). The only two POSTs taking raw `string? reason` (AgencyRoster reject + remove) validate non-blank at the controller before the facade — no NRE. ✅

### Backend mutation surface re-audited (NRE + ownership):
Every Guide command whose domain `.Trim()`s/derefs a string **has a `NotEmpty` validator OR the domain null-guards before `.Trim()`**; ownership enforced via `ICurrentUser` on every guide-scoped mutation. Verified: ContentTours TourProposal Create/Submit/Reject + TourGuides AddLanguage/AddSpecialization + GuideAvailabilityBlock Create (`Reason = IsNullOrWhiteSpace(reason) ? null : reason.Trim()` — `.Trim()` only after null-guard); Accounts Agency ApplyToAgency/InviteGuide/RemoveGuide/AcceptInvitation/DeclineInvitation/LeaveAgency. All have `DbUpdateConcurrencyException → Conflict` guards. ✅

### Build + tests (evidence):
- `dotnet build YallaJo.sln -c Debug` → **Build succeeded, 0 Errors** (100 pre-existing warnings, none in audited paths).
- Changed-path unit tests — **all green, 1,088 passed, 0 failed:** `Web.Tests.Unit` 464/464, `ContentTours.Tests.Unit` 284/284, `Booking.Tests.Unit` 267/267, `Accounts.Tests.Unit` 73/73.

> **Conclusion:** the Tour Guide Dashboard is fully compliant with the plan §5 + architecture rules. All previously-resolved gaps remain fixed; no new gaps found.

---

## 7. 🔁 Re-audit round 2 — TWO real gaps found + fixed (offering-handler ownership + C3 cache)

A second deep code-first re-audit (4 parallel agents over BFF controllers / facades+apiclients / backend handlers / endpoint scoping, each finding verified code-first) found that round 1's "ZERO open gaps" claim **missed two real defects**. The §6 hotspot sweep checked only the **6** schedule/pricing-tier handlers — it did **not** inspect the **3 offering-level** handlers (Remove / EnablePrivateTour / DisablePrivateTour), which lacked ownership entirely; and the C3 output-cache eviction mandated by §5.7 was absent from the facade. Both are now fixed, built green, and unit-tested.

### GAP-G18 — Guide-offering self-service handlers lacked caller-ownership enforcement (privilege escalation)
- **Status:** ✅ RESOLVED
- **Severity:** 🔴 HIGH (security — horizontal privilege escalation)
- **Type:** MISSING (ownership guard)
- **Layer(s):** Backend — `ContentTours.Application` command handlers
- **Plan requirement (ref):** §5.7 — offering writes are guide self-service (`offering/remove` `GuideOffering.Delete`; `private-tour` enable/disable). Architecture rule: ownership via `ICurrentUser` (missing = HIGH).
- **Code reality (file+symbol):** `RemoveGuideOfferingCommandHandler`, `EnablePrivateTourCommandHandler`, `DisablePrivateTourCommandHandler` (all under `src/Modules/ContentTours/ContentTours.Application/Commands/GuideTourOffering/{RemoveGuideOffering,EnablePrivateTour,DisablePrivateTour}/`) loaded the offering by **caller-supplied** `request.TourId`+`request.TourGuideId`, mutated, and saved with **no** `ICurrentUser` / `ITourGuideRepository` / `caller==owner` comparison. Endpoints (`GuideOfferingEndpoints.cs`) gate these with `AppAction.Delete`/`AppAction.Create` — the **same** `GuideOffering` permission family a guide legitimately holds — so RBAC alone does **not** restrict to *own* offering. → any authenticated guide could Remove / EnablePrivateTour / DisablePrivateTour **any** guide's offering by passing another `guideId`.
- **Rule impact:** Ownership-via-`ICurrentUser` rule violated; the backend handler is the only enforcement point for these routes.
- **Fix:** Each handler now injects `ITourGuideRepository tourGuideRepository, ICurrentUser currentUser` (sibling-order ctor) and, immediately after the `offering is null → NotFound` guard, runs: `var callerGuide = await tourGuideRepository.GetByUserIdAsync(currentUser.UserId!.Value, ct); if (callerGuide is null || offering.TourGuideId != callerGuide.Id) return Result.Failure(new Error("GuideTourOffering.NotOwner", "You can only manage your own tour offerings."), Outcome.Forbidden);` — mirrors the secure 6 schedule/pricing-tier handlers (e.g. `DeleteGuideScheduleCommandHandler`) exactly (compares the loaded entity's `TourGuideId`, no NotOwner logging, NotFound-before-ownership).
- **Resolution:** Build `0 Errors`; new tests `tests/ContentTours.Tests.Unit/Mohammad/GuideOfferingOwnershipTests.cs` (9 `[Fact]`: NotOwner→Forbidden, NullCallerGuide→Forbidden, Owner→Success ×3 handlers). `ContentTours.Tests.Unit` **293/293** (284 baseline + 9).

### GAP-G19 — `GuideMyToursFacade` did not evict the public `tour:{tourId}` output-cache tag after offering writes (stale C3 cache)
- **Status:** ✅ RESOLVED
- **Severity:** 🟠 MEDIUM (cache correctness — UI-PERF-C3)
- **Type:** MISSING (cache eviction)
- **Layer(s):** Web BFF — `Areas/Guide/Facades/GuideMyToursFacade.cs`
- **Plan requirement (ref):** §5.7 — "C3 evict `tour:{tourId}` (offerings change tour detail)."
- **Code reality (file+symbol):** Public `ToursController.Detail` is `[OutputCache(PolicyName="PublicMedium")]` (30-min TTL) + `PublicOutputCacheTagger.AddTag(HttpContext, $"tour:{id}")`, and its view renders `PricingTiers` / `Schedules` / per-guide offering data — i.e. exactly what guide offering writes mutate. `GuideMyToursFacade` injected only `(MyToursApiClient, ILogger)` and evicted nothing. Backend `HybridCache` cannot reach the Web host's in-process `IOutputCacheStore` (per the Admin GAP-P2 / Oracle ruling) → only the BFF facade can evict; guide offering edits left the public tour detail stale for up to 30 min.
- **Rule impact:** UI-PERF-C3 (writes must evict their public output-cache tags) violated for all 7 offering writes.
- **Fix:** `GuideMyToursFacade` now injects `IOutputCacheStore _cache`; the shared `WithGuideIdAsync` helper gained a leading `Guid tourId` param and, on `result.IsSuccess`, calls `await _cache.EvictByTagAsync($"tour:{tourId}", CancellationToken.None);` (CancellationToken.None survives client disconnect after the backend commit, matching Admin GAP-P2). All 7 writes (AddSchedule/DeleteSchedule/AddPricingTier/DeletePricingTier/EnablePrivateTour/DisablePrivateTour/RemoveOffering) thread `tourId`. Reads unchanged. Facade is auto-registered by reflection; `IOutputCacheStore` resolves from `AddOutputCache()` — no `Program.cs` change.
- **Resolution:** Build `0 Errors`; new tests `tests/Web.Tests.Unit/GuideMyToursFacadeCacheEvictionTests.cs` (real `HttpClient` + `StubHandler` + hand-stubbed `CapturingCacheStore : IOutputCacheStore`; `[Theory]` over all 7 writes asserting success→exactly one `tour:{tourId}` evict with `CancellationToken.None`; negative theory: backend-failure→no evict; profile-unresolvable→no evict). `Web.Tests.Unit` **482/482** (467 baseline + 15).

### Dismissed (investigated code-first, NOT gaps — recorded so they are not re-raised)
- **`SuspendGuideOfferingCommandHandler` / `ReinstateGuideOfferingCommandHandler` — NOT ownership gaps (by design).** Their endpoints are gated by `AppAction.Suspend` / `AppAction.Reinstate` (admin-only permissions a guide does **not** hold), the `GuideOfferingEndpoints.cs` block is explicitly commented `// Admin: Suspend / Reinstate / Remove`, and `SuspendGuideOfferingCommandHandler` emits `GuideTourOfferingSuspendedIntegrationEvent` carrying `SuspendedByAdminId` — i.e. an admin deliberately acting on **another** guide's offering. Adding a caller==owner check would break admin moderation.
- **3 minor BFF observations — quality/UX, not code gaps.** (1) `ProfileController.HandleMutation` helper has no `ct` param (synchronous helper, harmless). (2) `DiscountsController.Edit` redirects to Index with a generic error on invalid `ModelState` rather than re-rendering (minor UX inconsistency vs. Create). (3) `AgencyRosterController.Invite` POST returns `View(form)` on invalid input without `GuardSignOut` on the `PopulateAvailableGuidesAsync` result. None violate a binding rule.
- **`guide:{guideId}` companion eviction — out of §5 scope (noted follow-up, NOT fixed here).** Public `GuidesController.Detail` **is** `[OutputCache(PolicyName="PublicMedium")]` and its view renders `tour.OffersPrivateTour`, but the action emits **no** `PublicOutputCacheTagger.AddTag($"guide:{id}")` — so the page is cached-but-untagged and any facade eviction of `guide:{guideId}` would be a no-op. The genuine deficiency is the **missing `AddTag` in `GuidesController.Detail`**, a Public-area concern; plan §5.7 mandates only `tour:{tourId}`. Per direction-of-truth (no plan mandate for `guide:{id}`) this is recorded as an out-of-scope Public-area follow-up, not a §5 code gap.

### Build + tests (evidence, round 2):
- `dotnet build YallaJo.sln -nologo -v:q` → **Build succeeded, 0 Errors** (only pre-existing benign warnings).
- **`Web.Tests.Unit` 482/482** (467 + 15 new GAP-G19), **`ContentTours.Tests.Unit` 293/293** (284 + 9 new GAP-G18), **`Booking.Tests.Unit` 267/267**, **`Accounts.Tests.Unit` 73/73**.

> **Conclusion (round 2):** the round-1 "ZERO gaps" claim was incomplete — it audited only the 6 schedule/pricing-tier offering handlers and missed the 3 offering-level handlers (GAP-G18, security) and the §5.7-mandated C3 facade eviction (GAP-G19). Both are now fixed, built green, and unit-tested. Suspend/Reinstate confirmed correct-by-design; 3 minor BFF items and the `guide:{guideId}` Public-area tag noted as non-§5/out-of-scope. **Zero open §5 code gaps remain.**
