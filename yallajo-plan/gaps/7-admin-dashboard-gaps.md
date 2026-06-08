# Gap Report — `7-admin-dashboard.md` vs. shipped code

> **Method:** deep code-vs-plan audit against the **shipped** `Areas/Admin/Controllers/*` (43 controllers), their full route
> surfaces (every `[HttpGet]`/`[HttpPost]` template + class-level `[RequirePermission]`), the auth filter
> (`Infrastructure/Authorization/RequirePermissionAttribute.cs`), `Program.cs` authorization wiring, and the **shipped admin
> sidebar nav** (`Areas/Admin/Views/Shared/_AdminSidebar.cshtml`).
>
> **Code is canonical.** Every gap below is a **plan-documentation gap**, NOT a code violation: the shipped code respects
> Clean Arch / DDD / CQRS / MediatR, uses module-scoped schemas (no cross-module FKs), routes images through Attachment, and
> enforces granular per-permission claims. The plan has been **reconciled to match the code** — all gaps are now **RESOLVED**.
>
> **Scope:** `yallajo-plan/7-admin-dashboard.md` (§8 admin).
> **Severity:** 🔴 plan contradicts code · 🟠 undocumented surface / mis-grouping · 🟡 cosmetic.
> **Status:** ✅ RESOLVED (plan reconciled to code) for every item.

---

## 0. Summary

| Topic | Verdict |
|-------|---------|
| Permissions + Content→Admin consolidation | ✅ Accurate — re-verified granular per-action constants across 43 controllers. |
| **Authorization mechanism** | ✅ RESOLVED — plan previously claimed "policy `Admin`" everywhere; **no `[Authorize(Policy="Admin")]` exists.** `Program.cs` calls bare `AddAuthorization()`; the `AddPolicy` entries are **output-cache** policies. Real enforcement = `[Authorize]` (authenticated) + `RequirePermissionAttribute` checking **only** `ICurrentUser.HasPermission` (no role check). Plan header reworded. |
| **Nav count** | ✅ RESOLVED — prior ledger said **24** sidebar items; the actual `_AdminSidebar.cshtml` has **38** (32 top-level + 6 nested Taxonomy). Plan mapping table now 14→38. |
| **`/admin/trips`** (G1) | ✅ RESOLVED — `TripsController` (RowVersion-carrying tour-approvals screen, class perm `Tour.Approve`) documented as §8.4a. |
| **GuideApplications** (G2) | ✅ RESOLVED — `GuideApplicationsController` (class perm `GuideApplication.Read`) documented as §8.4b. |
| **Moderation split** (G3) | ✅ RESOLVED — §8.2 now enumerates 3 controllers (Moderation/Reports/FlaggedReviews). |
| **Finance split** (G4) | ✅ RESOLVED — §8.7 now enumerates 5 controllers (Payments/Payouts/Commissions/Disputes/Bookings). |
| **BlogTranslations / Statistics / Recommendations** (G5) | ✅ RESOLVED — documented as distinct surfaces (§8.6, §8.1, §8.8). |

---

## 1. Resolved gaps — undocumented / mis-described shipped surfaces

### G1 — `/admin/trips` is a second tour-moderation screen — ✅ RESOLVED
- **Code (`TripsController`):** class `[RequirePermission(WebPermission.Tour.Approve)]` (NOTE: `Tour.Approve`, **not** `Tour.ReadAny`). `GET /admin/trips` + `/admin/trips/{id}` (lookup-by-id privileged detail, SSR). POSTs **all thread `string rowVersion` (ST1)**: `/admin/trips/{id}/approve` (`Tour.Approve`), `/reject` (`Tour.Reject`), `/suspend` (`Tour.Suspend`), `/reinstate` (`Tour.Reinstate`). Sidebar item "Tour approvals" → `TripsController` (not Tours).
- **Resolution:** plan §8.4a added (TripsController, class perm `Tour.Approve`, RowVersion on all 4 POSTs → 409 [Reload]); §8.4 clarified `/admin/tours` = list-moderation (no rowVersion) vs `/admin/trips` = lookup-by-id rowVersion-guarded; mapping table notes sidebar "Tour approvals"→Trips.

### G2 — Guide Applications is a distinct admin surface — ✅ RESOLVED
- **Code (`GuideApplicationsController`):** class `[RequirePermission(WebPermission.GuideApplication.Read)]` (`.Read` constant exists). `GET /admin/guide-applications` (filters `tourId?`/status/page, SSR). POST `/admin/guide-applications/{tourId}/{id}/approve` (`GuideApplication.Approve`), `/reject` (`GuideApplication.Reject`).
- **Resolution:** plan §8.4b added.

### G3 — Moderation is three surfaces, not one (§8.2) — ✅ RESOLVED
- **Code:** `ModerationController` (class `ContentModerationLog.Read`; `/admin/moderation` + `/warn`=`AdminModerationQueue.Warn`, `/ban`=`.Ban`, `/unban`=reuses `.Ban`), `ReportsController` (class `AdminModerationQueue.Read`; `/admin/reports/{id}/resolve`=`.Resolve`), `FlaggedReviewsController` (class `AdminModerationQueue.Read`; `/admin/flagged-reviews/{id}/approve`=`.Approve`, `/remove`=`.Remove` — **both carry `rowVersion` ST1**).
- **Resolution:** §8.2 Stack rewritten to enumerate all three; rowVersion on FlaggedReviews noted in header ST1-carriers list.

### G4 — Finance is five surfaces (§8.7) — ✅ RESOLVED
- **Code:** `PaymentsController` (class `AdminFinanceDashboard.Read`; `/admin/finance/{id}/refund`=`Refund.Create`), `PayoutsController` (class `Payout.Read`; `/trigger`=`Payout.Trigger`, `/{id}/approve`=`Payout.Approve`), `CommissionsController` (class `CommissionRule.Read`; create/update/delete), `DisputesController` (class `AdminFinanceDashboard.Read`; `/review`+`/escalate`=`AdminFinanceDashboard.Update`, `/resolve`=`AdminFinanceDashboard.Approve`), `BookingsController` (class `AdminBookingDashboard.Read`; `/force-refund`=`AdminBookingDashboard.Update`, `/resolve-dispute`=`BookingDispute.Resolve` + runtime `Refund.Create` gate when `issueRefund=true`).
- **Resolution:** §8.7 Stack rewritten to 5 controllers. **Corrected:** plan previously assigned `BookingDispute.Resolve` to the Disputes surface; it actually belongs to `BookingsController.resolve-dispute` — Disputes uses `AdminFinanceDashboard.Update/.Approve`. PAY2 idempotency annotated as API-layer (not present at BFF/controller).

### G5 — BlogTranslations + Statistics + Recommendations are separate controllers — ✅ RESOLVED
- `BlogTranslationsController`: class `[RequirePermission(WebPermission.Blog.Update)]` (only; no action-level perms). `GET /admin/blogs/{id}/translations` + `/{languageCode}/edit`, `POST /{languageCode}`. → §8.6 sub-bullet.
- `StatisticsController`: class `Interaction.Read`; `GET /admin/analytics`. → §8.1 drill.
- `RecommendationsController`: class `Batch.Read`; `/admin/recommendations` + `/batches/refresh`=`Batch.Refresh`, `/boosts`(+`/deactivate`)=`BoostPackage.Create/Delete`, `/pins`(+`/deactivate`)=`EditorialPin.Create/Delete`. → §8.8 split.
- **Resolution:** all three documented.

---

## 2. Resolved gaps — newly found this pass (N-series)

### N1 — "policy Admin" wording is false (doc-only) — ✅ RESOLVED
- **Code:** `RequirePermissionAttribute.cs` (sealed `TypeFilterAttribute`, `Order=int.MinValue`) → inner `RequirePermissionFilter : IAsyncAuthorizationFilter`: not authenticated → `ChallengeResult`; lacks permission → `ForbidResult`. **No role Admin/SuperAdmin check.** `Program.cs:55` bare `AddAuthorization()`; `AddPolicy` (Program.cs:138-160) are output-cache policies. No `[Authorize(Policy="Admin")]` anywhere; controllers use plain `[Authorize]` + `[RequirePermission(...)]`, `[Area("Admin")]`, no class `[Route]`.
- **Resolution:** plan header "Authorization reality" note added; all 7 remaining "policy `Admin`" perm prefixes replaced with `[Authorize]`. Severity LOW (doc-only; enforcement was always per-permission, which the plan documents per-action).

### N2 — RowVersion (ST1) carriers broader than Trips — ✅ RESOLVED
- **Code:** rowVersion threaded by `TripsController` (all 4 POSTs), `FlaggedReviewsController` (approve+remove), `BlogsController` (restore), `NotificationTemplatesController.Delete` (`NotificationTemplatesController.cs:117-122`), and `SupportController.Close`/`Resolve` (`SupportController.cs:76-81`, `:112-117`). Other admin POSTs do not.
- **Resolution:** plan header "Optimistic concurrency ST1 carriers" note lists all five controllers. (Oracle pass-2 finding O1.)

### N3 — §8.8 mis-attributed Boost/Pin/Batch.Refresh to Growth — ✅ RESOLVED
- **Code:** `GrowthController` owns seasonality/holidays/photogenic/experiments only (perms `SeasonalityRule.*`/`HolidayCalendar.Create`/`Photogenic.Update`/`Experiment.Create|Update`; photogenic BFF route `/admin/growth/photogenic`, **not** the API verb). Boost/Pin/Batch.Refresh live in `RecommendationsController`.
- **Resolution:** §8.8 split into Growth vs Recommendations sub-sections; photogenic route corrected.

### N4 — Trips class perm = `Tour.Approve`; `GuideApplication.Read` exists — ✅ RESOLVED
- Captured in G1/G2 resolutions.

### N5 — Route casing (convention-resolved PascalCase) — ✅ RESOLVED (cosmetic)
- GET index actions without explicit templates resolve via `{area}/{controller}/{action}` to PascalCase (e.g. `/Admin/Moderation`); routes are case-insensitive so the plan's lowercase notation is functionally correct. No change required beyond noting it.

### N6 — Sidebar is 38 items, not 24 — ✅ RESOLVED
- **Code:** `_AdminSidebar.cshtml` = 38 routed destination links (32 top-level + 6 nested Taxonomy); a 39th `<a>` is the Taxonomy collapse toggle (not a destination). Mapping table updated 14→38; source file cited. (See N15.)

### N7 — Blogs: extra perms + actions — ✅ RESOLVED
- **Code (`BlogsController`, class `Blog.Read`):** publish/unpublish/archive/approve/hide/unhide=`Blog.Approve`; restore=`Blog.DeleteOwn` (+rowVersion ST1); feature=`Blog.Feature`; unfeature=`Blog.Unfeature`; reject=`Blog.Reject`; remove=`Blog.Remove`; delete=`Blog.DeleteOwn`; link-tour=`BlogTourLink.Create`; unlink-tour=`BlogTourLink.Delete`. **No `AdminBlogQueue.Read` used** (class gate = `Blog.Read`).
- **Resolution:** §8.6 Stack lists all actions/perms; new perms (`Blog.Unfeature`, `BlogTourLink.Create/Delete`) documented.

### N8 — Disputes vs Bookings perm ownership — ✅ RESOLVED
- Captured in G4: Disputes=`AdminFinanceDashboard.Update/.Approve`; `BookingDispute.Resolve`=`BookingsController.resolve-dispute` (+runtime `Refund.Create` gate).

### N9 — Invitations / Roles / EntityCategories / EntityTags are distinct controllers — ✅ RESOLVED
- **Code:** `InvitationsController` (class `User.Create`), `RolesController` (class `Role.Read`), `EntityCategoriesController` (class `EntityCategory.Read`), `EntityTagsController` (class `EntityTag.Read`).
- **Resolution:** §8.13 documents Roles + Invitations as distinct surfaces; §8.9 documents EntityCategories + EntityTags (8 controllers total, routes `/admin/entity-categories|entity-tags`).

### N10 — SEC3 sanitized-preview & PAY2 idempotency are not at controller layer — ✅ RESOLVED
- **Code:** no `@Html.Raw`/`HtmlSanitizer` in `BlogsController` (SEC3 preview is View-layer); no PAY2 idempotency key at BFF/controller (API-layer concern).
- **Resolution:** §8.6 SEC3 annotated as View-layer; §8.7 PAY2 annotated as API-layer.

---

## 2b. Resolved gaps — Oracle verification pass-2 (O-series)

### N11 — RowVersion carrier list still incomplete — ✅ RESOLVED
- **Code:** beyond N2's three, `NotificationTemplatesController.Delete(Guid id, string? rowVersion)` (`NotificationTemplatesController.cs:117-122`) and `SupportController.Close`/`Resolve` (`SupportController.cs:76-81`, `:112-117`) also thread `rowVersion`.
- **Resolution:** plan header ST1 note + N2 now list all five carriers (Trips, FlaggedReviews, Blogs-restore, NotificationTemplates-delete, Support-close/resolve).

### N12 — "every admin GET returns Razor View" is false — ✅ RESOLVED
- **Code:** `AuditLogsController.Export` is a GET that streams a **CSV file** (`AuditLogsController.cs:78-98`).
- **Resolution:** plan header "Rendering reality" reworded — admin page GETs are Razor views; `AuditLogsController.Export` returns CSV.

### N13 — "every POST is RedirectToAction (PRG)" too absolute — ✅ RESOLVED
- **Code:** all `[HttpPost]` actions carry `[ValidateAntiForgeryToken]` (confirmed), but form-validation failures re-render views: `BlogsController.Create` (`:50-69`), `BlogsController.Edit` (`:91-111`), `NotificationTemplatesController.Create` returns `View("Edit",…)` (`:66-99`), `InvitationsController.ResendSubmit` returns `View(nameof(Resend),vm)` (`:76-105`).
- **Resolution:** plan header reworded — all POSTs anti-forgery protected; most mutations PRG, validation failures may re-render views. (§3 line on BFF-POST verb principle also softened.)

### N14 — auth wording over-broad (Web vs API host) — ✅ RESOLVED
- **Code:** Web host `YallaJo.Web/Program.cs:55` bare `AddAuthorization()` (no Admin policy used by Admin controllers) — but API host `YallaJo.Api/Program.cs:191-195` *does* register role policies including `Admin`.
- **Resolution:** plan header "Authorization reality" clarified — no Admin policy used by Web Admin controllers; Web host bare `AddAuthorization()`, API host registers role policies incl. `Admin`.

### N15 — sidebar count wording (anchors vs routed links) — ✅ RESOLVED
- **Code:** `_AdminSidebar.cshtml` has 39 literal `<a>` anchors; the 39th is the Taxonomy collapse toggle (`:181-185`), leaving **38 routed destination links**.
- **Resolution:** plan header + N6 worded as "38 routed sidebar nav destination links (excluding the Taxonomy collapse toggle)".

---

## 3. ✅ Confirmed-correct (no change needed)

- **Permissions** — all granular per-action constants match shipped `[RequirePermission]` across 43 controllers.
- **Content→Admin consolidation** — `/admin/categories`, `/admin/seo/*` routes shipped as documented (diverges from 0-arch §2 which still lists `Content` as a distinct area; code wins, header already notes this).
- **Four-tier pipeline / Clean Arch / DDD / CQRS / MediatR** — intact; no cross-module DB FKs observed; entity images via Attachment; Weather only in Place/Seo-weather context.
- **BFF-POST verb principle** — all mutations page-scoped `POST` + `[ValidateAntiForgeryToken]`; most redirect (PRG), validation failures may re-render the view (see N13); PATCH/PUT/DELETE in plan are API-layer verbs (header note correct).

---

## 4. Net result

**ZERO open gaps.** Permissions and the area model were already solid. This pass corrected the plan's **authorization framing**
(no "policy Admin" — per-permission filter only), expanded the **14 logical pages → 38 sidebar nav surfaces** mapping, documented
the previously-missing surfaces (**`/admin/trips`** RowVersion-guarded tour-approvals, **GuideApplications**, the
**Moderation (3)** and **Finance (5)** splits, **BlogTranslations**, **Statistics**, **Recommendations**, **Roles**,
**Invitations**, **EntityCategories/EntityTags**), pinned the **ST1 RowVersion carriers** (Trips / FlaggedReviews / Blogs-restore),
and corrected mis-attributed perms (Disputes vs Bookings, Growth vs Recommendations). A second Oracle verification pass (O-series →
N11-N15) further corrected the **ST1 RowVersion carrier list** (added NotificationTemplates-delete + Support-close/resolve → five total),
the **GET-returns-CSV** exception (`AuditLogsController.Export`), the **POST/PRG absolutism** (validation failures re-render views),
the **Web-vs-API auth-policy** distinction (API host *does* register an `Admin` policy; Web Admin controllers do not use it), and the
**sidebar wording** (38 routed links vs 39 anchors). All findings were plan-documentation gaps;
**no shipped code was changed** because the code is canonical and rule-compliant.

---

## 5. ✅ RESOLVED — Round 2 (code-first re-audit)

A full-stack code-first re-audit (4 parallel explore agents across all 43 controllers + facades/apiclients/VMs + backend admin handlers, every flag verified against source) found **three real CODE gaps** that the prior plan-doc round missed. All three are now FIXED, built green, and covered by tests (Web 464 + 3 new eviction tests, ContentBlogs 388, Messaging 2, Finance 3 — all pass).

### GAP-P1 — Blogs Restore RowVersion not round-tripped
- **Status**: ✅ RESOLVED
- **Severity**: HIGH
- **Type**: ST1 optimistic-concurrency / data-integrity (INCOMPLETE)
- **Layer(s)**: Web BFF (ViewModel + Mapper + View)
- **Plan requirement (ref)**: §8.6 — Blogs `restore` is a RowVersion carrier (round-trip rowVersion + 409→reload).
- **Code reality (file+symbol)**: `Areas/Admin/Models/Blogs/BlogListVm.cs` `BlogRowVm` had no `RowVersion`; `Areas/Admin/Models/Blogs/BlogsMapper.cs` `ToRowVm(AdminDeletedBlogResponse)` dropped the API's `RowVersion`; `Areas/Admin/Views/Blogs/Index.cshtml` Restore form posted no hidden `rowVersion`. `BlogsController.Restore(Guid id, string rowVersion, ct)` therefore received empty → `DecodeRowVersion` → `[]` → API rejected the concurrency-guarded restore.
- **Rule impact**: ST1 violated; restore unusable / always-fails.
- **Fix**: Added `string? RowVersion` to `BlogRowVm`; mapped `RowVersion = EncodeRowVersion(r.RowVersion)` in the deleted-blog overload; added `<input type="hidden" name="rowVersion" value="@blog.RowVersion" />` to the Restore form. (`AdminDeletedBlogResponse.RowVersion` already carried the token; `EncodeRowVersion` helper already existed.)
- **Resolution**: 3 edits, mirrors the existing `EditBlogVm.RowVersion` pattern. Built + ContentBlogs 388/388 green.

### GAP-P2 — Admin entity facades miss C3 output-cache eviction
- **Status**: ✅ RESOLVED
- **Severity**: HIGH
- **Type**: Cache-correctness / C3 tag-invalidation (MISSING)
- **Layer(s)**: Web BFF (7 Admin Facades + canonical CategoriesFacade fix)
- **Plan requirement (ref)**: §8.4 evict `tour:{id}` after approve/feature; §8.4a Trips `tour:{id}`; §8.5 `place:{id}`/`business:{id}` after feature/verify/moderation; §8.6 `blog:{id}`/`homepage` after feature/hide; §8.8 `tour:{id}`/`place:{id}`/`homepage` after boost/pin/photogenic.
- **Code reality (file+symbol)**: Public detail pages are genuinely output-cached + tagged (`Public/Controllers/ToursController`+`PlacesController`+`DirectoryController` `[OutputCache(PublicMedium)]`, `BlogController` `[OutputCache(PublicLong)]`, all `PublicOutputCacheTagger.AddTag("tour:{id}"/...)`; `HomeController` `Tags=["homepage"]`). Only the 4 lookup facades (Categories/Tags/Languages/Specializations) injected `IOutputCacheStore`; the 7 entity facades (`TripsFacade`, `AdminToursFacade`, `PlacesFacade`, `BusinessesFacade`, `BlogsFacade`, `RecommendationsFacade`, `GrowthFacade`) evicted nothing. Admin writes flow API→backend, which cannot reach the Web host's in-process output cache — only the BFF facade can.
- **Rule impact**: C3 violated; admin approve/feature/edit/verify/moderation/boost/pin left stale public pages for the full 30–60 min TTL.
- **Fix**: Injected `IOutputCacheStore` into all 7 entity facades; their `Normalize` helper converted to instance + `params string[] evictTags`, evicting on `IsSuccess` with **`CancellationToken.None`** (so eviction survives a post-commit client disconnect — Oracle-confirmed correctness point; CategoriesFacade's pre-existing `ct` usage was the same latent bug, also patched = **GAP-P2a**). Per-facade tags (plan-accurate, minimal): Trips `tour:{id}`×4; Places `place:{id}` on Update/Delete/Feature/Verify/SetAccessibility (Create + RemoveAccessibilityAssignment skip — no cached entity / no id); Businesses `business:{id}`×6; Blogs `blog:{id}` on all writes + `homepage` on Feature/Unfeature/Hide/Unhide (Create skip); AdminTours `tour:{id}` on Approve/Reject/Suspend/Reinstate/Feature + offerings `tour:{tourId}` (proposals/packages skip); Recommendations `homepage`+entity (`req.EntityKind`/`EntityId`) on CreateBoost/CreatePin, `homepage` on Deactivate*/RefreshBatches; Growth `homepage`+`tour|place:{entityId}` on SetPhotogenic (seasonality/holiday/experiment skip).
- **Resolution**: 8 facades edited; new `tests/Web.Tests.Unit/AdminFacadeCacheEvictionTests.cs` (3 tests: success-evicts-tag, success-uses-CancellationToken.None, failure-evicts-nothing) all pass. Web 464 existing + 3 new green.

### GAP-P3 — DeleteNotificationTemplate missing DbUpdateConcurrencyException→Conflict
- **Status**: ✅ RESOLVED
- **Severity**: MEDIUM
- **Type**: ST1 concurrency consistency (INCOMPLETE)
- **Layer(s)**: Backend (Messaging.Application handler)
- **Plan requirement (ref)**: §8.12 — NotificationTemplate `delete` is a RowVersion carrier; canonical write-handler pattern wraps `SaveChangesAsync` and maps `DbUpdateConcurrencyException`→`Outcome.Conflict`.
- **Code reality (file+symbol)**: `Messaging.Application/Commands/DeleteNotificationTemplate/DeleteNotificationTemplateCommandHandler.cs` did the `RowVersionUtil.Equal` pre-check but called `SaveChangesAsync` unwrapped → a check-to-save race could throw an unhandled `DbUpdateConcurrencyException`→500 (sibling Finance/ContentTours handlers all wrap it).
- **Rule impact**: ST1 consistency hole; 500 instead of clean 409 under concurrent delete.
- **Fix**: Added `using Microsoft.EntityFrameworkCore;` + `try/catch (DbUpdateConcurrencyException)` → `Result.Failure(new Error("NotificationTemplate.ConcurrencyConflict", …), Outcome.Conflict)`.
- **Resolution**: Messaging 2/2 green.

### Investigated and DISMISSED (verified non-gaps — recorded so they are not re-raised)
- **Finance EscalateDispute "missing validator"** — `Dispute.Escalate` guards `IsNullOrWhiteSpace(reason)` **before** `.Trim()`; empty reason returns a clean domain failure Result (no NRE/500). Rule-10 BLOCKER condition not met. No plan/rule mandate for a validator. (`Finance.Application/Disputes/DisputeHandlers.cs`.)
- **BlogTranslations "missing [ValidateAntiForgeryToken]"** — present on `Save` (BlogTranslationsController.cs:53-54). Agent misread.
- **Lifecycle "missing class [RequirePermission]"** — present (`[RequirePermission(WebPermission.User.UpdateAny)]`, line 24); inherits `Controller` (not BaseController) by design — redirect-only per §8.13. All 6 POSTs anti-forgery-guarded.
- **4 sync GET form-renders without `ct`** (Invitations.Resend, NotificationTemplates.Create, Translations.OnDemand/Edit) — accepted repo-wide convention (13+ identical static-form siblings); no facade call / no IO.
- **Support.Reply READ-permission** — `[RequirePermission(SupportTicket.Read)]` matches plan §8.11 exactly; the plan assigns no write-perm to Reply and no `SupportTicket.Reply/Update` constant exists (WebPermission.cs SupportTicket = Read+Close only). Intentional class-Read gate.
- **CategoriesFacade not evicting `category:tree`** — `category:tree` is applied in **zero** `AddTag`/`OutputCache(Tags=)` calls anywhere in code (only a stale comment in Program.cs); categories carry the `"lookups"` tag only. Plan §8.9 line is stale vs implementation; eviction of `category:tree` would be a no-op.

### Follow-up gaps (logged, NOT in scope this pass — plan does not mandate, fixing would be invented functionality)
- **Tours Feature/Unfeature homepage staleness** — the homepage `FeaturedTours` rail won't refresh from a `tour:{id}` eviction; plan §8.4 does not mandate `homepage` eviction here. Real but separate.
- **AdminTours package:{id} eviction** — `package:{id}` is a live public tag, but plan §8.4 only mandates `tour:{id}`; ApprovePackage/RejectPackage intentionally skipped.

### Net result (Round 2)
**Three real code gaps (GAP-P1/P2/P3 + GAP-P2a) found and RESOLVED in code.** Build green (0 errors); tests green (Web 464 + 3 new eviction tests, ContentBlogs 388, Messaging 2, Finance 3). Six agent over-flags verified and dismissed with evidence; two genuine-but-out-of-scope items logged as follow-ups. The prior "zero open gaps" claim held for *plan-documentation* but missed these *implementation* defects, now closed.
