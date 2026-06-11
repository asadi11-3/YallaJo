# Provider Area Master Plan — UI/UX Modernization, View Reduction, AJAX & Backend Support

> **Status:** PLAN — no code has been changed. Executable with zero conversation context.
> **Scope:** `src/Hosts/YallaJo.Web/Areas/Provider` + the explicitly listed `[Backend]` additions in `src/Modules/*` (exposed through `src/Hosts/YallaJo.Api`).
> **Rules contract:** `yallajo-plan/UI-UX-Design.md` (rule IDs cited per phase; respect ✅ enforced / 🎯 target legend).
> **Structural exemplar & foundations record:** `docs/public-area-master-plan.md`.
> **Intended scope doc:** `yallajo-plan/3-provider-dashboard.md`.

---

## §1 Context & Ground Rules

### 1.1 Architecture recap (verified)

- Four-tier pipeline: **Controller → Facade → ApiClient → IApiClient** (typed HTTP to `YallaJo.Api`). The browser **never** calls the API host (JS5).
- All 19 active controllers inherit `Infrastructure/Mvc/BaseController.cs` (114 L): `RedirectToLogin()` ~L32, `GuardSignOut` ~L40/L44, `SetSuccess/SetError` ~L48/L51, `SetFlash` ~L60/L69, `ApplyValidationErrors` ~L82/L92, `WantsAjax()` ~L107 (matches `X-Requested-With: fetch|XMLHttpRequest` or `Accept: application/json`; the `fetch` marker is set by `wwwroot/assets/js/api-client.js` ~L4).
- Provider pages use the **public site layout** `~/Views/Shared/_Layout.cshtml` (set explicitly in 33/33 page views — there is **no `_ViewStart.cshtml`** in the area). `_AdminLayout.cshtml` is **not** used by Provider; its Google-Fonts debt does not apply here. `_Layout` already loads self-hosted fonts, `rtl.css` (appended after `style.css` for RTL cultures), `api-client.js`, `form-ux.js`.
- `_ProviderSidebar.cshtml` lives at the non-standard `Areas/Provider/Shared/` (resolved via `Program.cs` ~L222 view-location format `~/Areas/{2}/Shared/{0}.cshtml`) and is rendered inside each view body via `<partial name="_ProviderSidebar" model="sidebar" />` (31 occurrences).
- Cultures: `en` / `ar` (default `ar`). `rtl.css` **already flips** `fa-arrow-left`, `fa-chevron-left/right`, `fa-arrow-left-long` via `scaleX(-1)` (rtl.css ~L150–159) and re-anchors navbar dropdowns (~L219). All new `[dir="rtl"]` overrides go in `rtl.css` — never inline, never per-page `<style>` (RTL1).
- Localization: `IStringLocalizer<SharedResource>`, keys in BOTH `Resources/SharedResource.en.resx` (3,846 keys) **and** `.ar.resx`, flush-left entries appended before `</root>`. **Grep before adding any key** — duplicates cause MSB3568. UTF-8 only; never re-encode resx with PowerShell (mojibake precedent: commit `9bb079bf`).

### 1.2 Foundations already built — EXTEND, never duplicate

| Foundation | Location | Use for |
|---|---|---|
| `window.YallaJo.api` (get/post/postForm/loadPartial; antiforgery header, timeout, 401→sign-in) | `wwwroot/assets/js/api-client.js` | ALL AJAX (JS5, SEC7) |
| `form[data-loading]` disable+spinner, `window.YallaJo.toast`, `data-confirm` modal | `wwwroot/assets/js/form-ux.js` | L2/F7, NF1/NF4/NF5, F8/MOD3/MOD5 |
| `BaseController.WantsAjax()` → PartialView for AJAX / PRG fallback | `Infrastructure/Mvc/BaseController.cs` ~L107 | PE1 dual-mode actions |
| `listing.js` + `_XResults` partial pattern (pushState pagination/filtering + skeletons) | `wwwroot/assets/js/listing.js` | S1/L1/L4/L5 list pages |
| Shared partials `_Pagination`, `_EmptyState`, `_StarRating`, `_TourCard` | `Areas/Public/Views/Shared/` | Reuse where model-compatible; Provider-specific variants go in `Areas/Provider/Views/Shared/` |
| `site.css` utilities (`.object-cover`, `.avatar-*`, `.text-truncate-2/3`, `.card-img-h*`, `.preserve-whitespace`) | `wwwroot/assets/css/site.css` | No new inline styles (X6) |
| Self-hosted fonts incl. Arabic stack via `html[lang="ar"]` | `wwwroot/fonts/` + `site.css` @font-face | V3/V4/A5/X10 — already wired into `_Layout` |

### 1.3 Conventions enforced in every phase

- New Razor partials need `@using Microsoft.Extensions.Localization` before `@inject` (no localization import in the area `_ViewImports` today — Phase 1 fixes this for the area, but any partial that must work standalone keeps the using).
- Dark mode via `data-bs-theme` (T1–T5). No `SetSeo` — Provider is auth-gated, not public-facing.
- Page scripts in `wwwroot/assets/js/{page}.js` via `@section Scripts` (J2); vendored libs only — **no new deps/CDNs**; ApexCharts is allowed here (X14 bans it on public pages only) and is already vendored (`_AdminLayout` references `apexcharts.min.js`).
- Auth-gated area ⇒ **NoStore** (C2). Currently **zero** cache attributes exist in the area — Phase 8 adds them.
- Every AJAX conversion keeps a no-JS path (PE1/PE2): SSR first paint, AJAX refinement (R2/S1).
- F10: **never raw entity IDs as typed input** — replace every GUID-paste field with a select/searchable combobox showing names.

### 1.4 Verified inventory (code-audited)

| Artifact | Count | Detail |
|---|---|---|
| Controller files | 20 | 19 active + `EarningsController.cs` (56 L, **entire class commented out** with dead-controller audit banner dated 2026-06-09, ~L1–18) |
| Actions | **90 active** (27 GET / 63 POST) | Per controller: Tours 8, TourWaypoints 7, TourAvailability 7, Provider 7, Bookings 6, ProviderBookings 6, Packages 6, TourPricing 6, TourSchedules 6, TourApplications 5, Reviews 5, PaymentMethods 4, Settings 4, TourGuides 3, TourImages 3, ProviderDocuments 3, Invoices 2, Dashboard 1, Finance 1, Earnings 0 |
| Antiforgery | 63/63 POSTs have `[ValidateAntiForgeryToken]` — no gaps | ✅ |
| `WantsAjax()` / `PartialView` / `Json` usage | **0** — area is 100% classic PRG | Phase 3 target |
| Facades / ApiClients | 19 / 19 | All web-called API routes verified to exist (see §1.6) — **no orphaned routes** |
| Views | **41 .cshtml** (5,474 LOC) | 33 page views + 2 shared partials (`_TourWizardSteps`, `_ProviderSidebar`) + 5 `_Form` partials + `_ViewImports` |
| Page scripts | **0** provider-specific JS files | All interactivity is Bootstrap data-attributes; 0 inline `<script>`, 0 inline `onclick`, 0 `onsubmit=confirm()` |
| Inline `style=` | 2 | `TourApplications/Index` ~L102 (`min-width:14rem`), `Bookings/Index` ~L87 (`max-width:260px`) |
| Localization | 40/41 files inject Localizer; **`Earnings/Index.cshtml` is 0% localized** (~25 hardcoded English strings) | Phase 0 deletes it |
| Auth styles | **3 coexisting**: declarative `[RequirePermission]` (Dashboard, Invoices, PaymentMethods, Provider, ProviderDocuments, Packages); imperative `ICurrentUser.HasPermission` (all 8 Tours-family); plain `[Authorize]` only (Bookings, ProviderBookings, Reviews, Settings) | Phase 1 unifies |
| Duplicated boilerplate | ~65–70% of the 1,240 lines in the 7 tour sub-resource controllers; `SetSidebar()` copy-pasted in 9 controllers; sidebar VM fallback in 29 views; `Money()/Count()/Date()` helpers in ~14 views; empty-state block ×~20; per-row delete modal ×9 views (~270 L) | Phases 1–2 |
| Dead code | `EarningsController` + orphan `Views/Earnings/Index.cshtml`; `PaymentMethodsController.Index` + view (no inbound link — Methods tab lives in Finance); `InvoicesController.Index` + view (only `Download` is linked, from Finance ~L223); dead nav key `ProviderNav = "PaymentMethods"` (no such sidebar key) | Phase 0 |

### 1.5 Per-phase verification protocol (merge gate for EVERY phase)

1. `dotnet build src/Hosts/YallaJo.Web/YallaJo.Web.csproj --no-incremental` — baseline **71 warnings / 0 errors**; **zero NEW warnings**.
2. Walk every touched surface in **EN-LTR and AR-RTL** (RTL4) — layout integrity, icon direction, number/date rendering (`<bdi>`/`dir="ltr"` intact).
3. Light **and** dark theme (`data-bs-theme`) check (T1–T5).
4. 390 px and 1280 px viewports (D1/D2 — 44 px touch targets).
5. No raw resx keys rendered; both `.en.resx` and `.ar.resx` updated; grep for duplicates first (MSB3568).
6. No new inline `style=` except X6-justified with a code comment.
7. Every AJAX-converted flow re-tested with JS disabled (PE1).
8. If `src/Modules`/`YallaJo.Api` touched (`[Backend]` lines only): `dotnet build` the solution + run the owning module's tests if present.
9. One conventional commit per phase citing rule IDs (e.g. `refactor(provider): retire Earnings page into Finance per UI-UX CON1/ERR3`).

### 1.6 Feature → chain map (verified; all routes exist in API)

| Feature | View(s) | Controller | Facade → ApiClient | API routes (module) |
|---|---|---|---|---|
| Overview | Dashboard/Index | DashboardController | DashboardFacade → DashboardApiClient | GET `tours/provider/my-tours`, `finance/guide/summary`, `booking/join-requests`, `provider/dashboard/overview`, `provider/dashboard/pending-actions`, `provider/dashboard/notifications` (Accounts/Finance/Booking/ContentTours) |
| Application | Provider/Apply, Provider/Status | ProviderController | ProviderFacade → ProviderApiClient | GET `provider/status`, POST `provider/register|apply|reapply`, POST `provider/documents/upload`, PUT `provider/documents/{id}` (Accounts) |
| Compliance docs | ProviderDocuments/Index | ProviderDocumentsController | ProviderDocumentsFacade → ProviderDocumentsApiClient | GET/POST `booking/provider/documents`, PUT `…/{id}` (Booking) |
| Tours CRUD | Tours/{Index,Create,Edit,_Form} | ToursController | ProviderToursFacade → ProviderToursApiClient | GET `tours/provider/my-tours?page&pageSize&status&sort`, GET/POST/PUT/DELETE `tours[/{id}]`, POST `…/submit|archive` (ContentTours) |
| Pricing/Schedules/Waypoints/Availability/Guides/Images/Applications | 17 views | 7 sub-resource controllers | 7 Facades → 7 ApiClients | `tours/{tourId}/pricing|schedules|waypoints|guides|applications`, `booking/availability/...` incl. POST `slots/bulk`, `content-core/attachments` (ContentTours/Booking/ContentCore) |
| Packages | Packages/{Index,Manage} | PackagesController | PackagesFacade → PackagesApiClient | `tours/packages` CRUD + inclusions + submit (ContentTours) |
| Incoming bookings | Bookings/Index | BookingsController | BookingsFacade → BookingsApiClient | `booking/join-requests` + approve/reject, `booking/{id}` confirm/reject (Booking) |
| Booking management | ProviderBookings/{Index,Details} | ProviderBookingsController | ProviderBookingsFacade → ProviderBookingsApiClient | GET `booking/provider/bookings` (API supports `status,fromDate,toDate,tourId,cursor,pageSize,countTotal` — **web only sends `status`+`pageSize=50`**), POST `booking/{id}/confirm|cancel|reject|complete` (Booking) |
| Reviews | Reviews/Index | ReviewsController | ReviewsFacade → ReviewsApiClient | GET `tours/provider/my-tours?pageSize=100` then **per-tour** `social/reviews/Tour/{tourId}` + `ratings` (N+1 — see B6), reply/edit/delete/report (Social) |
| Finance | Finance/Index | FinanceController (only controller-level `Task.WhenAll`, ~L47) | EarningsFacade + ProviderInvoicesFacade + ProviderPaymentMethodsFacade | `finance/guide/summary`, `finance/guide`, `payouts/provider`, `disputes/my`, `invoices/provider/my-invoices`, `provider-payment-methods` CRUD (Finance) |
| Invoice PDF | — | InvoicesController.Download | ProviderInvoicesFacade | GET `invoices/{id}/download` (Finance, PRINT2) |
| Settings | Settings/Index | SettingsController (sequential awaits ~L44/L53; calls `ProviderApiClient` directly bypassing facade ~L160) | Accounts-area facades (cross-area) | `provider/settings` + Accounts profile/password/phone (Accounts) |

### 1.7 Hard guardrails

- Touch **only**: `Areas/Provider/**`, `Areas/Provider`-referenced shared assets explicitly listed in a phase (`wwwroot/assets/js/provider-*.js` new files, `wwwroot/assets/css/site.css` + `rtl.css` additive sections, `Resources/SharedResource.{en,ar}.resx`, `docs/`), and the **explicitly listed `[Backend]` additions** in `src/Modules/*` / `src/Hosts/YallaJo.Api`. **Nothing else.** Never touch other Web areas (the cross-area Accounts facades used by Settings are consumed, not modified).
- Prefer **small additive endpoints** over reshaping existing ones; never break existing consumers; never remove or rename an existing API route.
- Mind the gitignore `**/[Pp]ackages/*` gotcha — `Areas/Provider/Views/Packages/**` and `Areas/Provider/Models/Packages/**` need negation entries if new files don't show in `git status` (precedent: `.gitignore` ~L205–215).
- Keep both bookings route families working during consolidation; permanent redirects only where §2 says so.
- All proposed redirects are 301 (permanent) and are listed in §2 — no other route changes.

---

## §2 View Reduction Table (first-class deliverable)

**Target: 41 → 33 .cshtml files** (3 retired pages, 5 Create/Edit pairs merged). New extracted partials (from existing duplicated markup) are listed separately and do not hide the page-count win. Public-area precedents applied: dead view deleted (Contact/Index2), page merged with anchor redirect (Help/Detail → Help#faq-{id}), retired route 301'd (Search → /tours).

| View | Verdict | Mechanism | UX gain | SEO/route impact |
|---|---|---|---|---|
| Dashboard/Index | **keep** (enhance) | Phase 4: real KPIs + ApexCharts sparklines (X14 allows on dashboards) | From doc-expiry-only KPIs to bookings/revenue overview | none (auth-gated, no SEO) |
| Provider/Apply | keep | — | Phase 5 polish only | none |
| Provider/Status | **keep, thin** | Replace manual FileUrl/FileName/FileSizeBytes "replace" form (~L160–183) with real `type="file"` upload reusing the adjacent upload form pattern | Removes placeholder-grade UX | none |
| ProviderDocuments/Index | keep | — | Already clean | none |
| Tours/Index | **keep, thin** | listing.js + `_ToursResults` partial; N×2 per-row modals (~L182–225) → single `_ConfirmActionModal` | AJAX paging/status tabs without reload; DOM bloat removed | none |
| Tours/Create + Tours/Edit | **merge → Tours/Upsert.cshtml** | One view, `Model.IsEdit` switch; Edit-only sub-resource toolbar conditional. Routes `tours/create` and `tours/{id}/edit` both stay (deep links preserved) — controller returns `View("Upsert", vm)` | Single source of truth for the form shell; wizard chrome consistent | none — both routes kept |
| Tours/_Form | keep | — | Already exemplary (dir="ltr" inputs, aria-describedby) | n/a |
| TourPricing/Create + Edit | **merge → Upsert.cshtml** | Same pattern (~85% identical 49+52 L wrappers) | consistency | none — routes kept |
| TourPricing/{Index,_Form} | keep, thin | Index: per-row delete modals → `_ConfirmActionModal` | — | none |
| TourSchedules/Create + Edit | **merge → Upsert.cshtml** | same | — | none |
| TourSchedules/{Index,_Form} | keep, thin | same modal consolidation | — | none |
| TourWaypoints/Create + Edit | **merge → Upsert.cshtml** | same | — | none |
| TourWaypoints/{Index,_Form} | keep, thin | drag-handle reorder (Phase 5) wired to existing PUT `waypoints/reorder` | reorder becomes usable | none |
| TourAvailability/Create + Edit | **merge → Upsert.cshtml** | same | — | none |
| TourAvailability/{Index,_Form} | keep (enhance) | Phase 4: month calendar view via B3 endpoint (CAL1) alongside table | visual slot management | none |
| TourGuides/Index | keep (enhance) | F10: GUID-paste input (~L36–37) → async combobox via B7 lookup | eliminates worst F10 violation | none |
| TourImages/Index | keep | Phase 5: client-side preview before upload | — | none |
| TourApplications/Index | keep, thin | inline style ~L102 → utility class | — | none |
| Packages/Index | keep (enhance) | F10: `IncludedTourIds` GUID textarea (~L85–88) → multi-select of own tours (existing `my-tours` endpoint, no backend needed); chevron pagination → shared `_Pagination` | eliminates F10 violation | none |
| Packages/Manage | keep, thin | Submit-for-review gets `data-confirm` (F8 parity with Delete) | — | none |
| Bookings/Index | keep, thin | listing.js; raw `@jr.Status`/`@b.Status` localized; Approve gets confirm | — | none |
| ProviderBookings/Index | keep (enhance) | **add pagination** (cursor — API already returns `NextCursor`) + date/tour filters (API already supports; client just doesn't send them) | unbounded table fixed | none |
| ProviderBookings/Details | keep | — | already strong | none |
| Reviews/Index | keep, thin | B6 batch ratings kills N+1; collapse triggers get `aria-expanded/controls` | faster page | none |
| Finance/Index | **keep — becomes the single finance surface** | Absorbs the three retired pages below; tabs get lazy AJAX loading (MOD8/L1); raw statuses localized | One finance home, three fewer stale copies | gains 2 inbound redirects |
| **Earnings/Index** | **retire (delete)** | Controller already dead (commented out 2026-06-09); view is orphaned, 0% localized, RTL-broken, duplicates Finance payouts/disputes tabs ~150 L | Removes the single worst file in the area (0% l10n, no `<bdi>`, h1→h4 jump, bi-* icons without aria) | Route already 404s (controller dead) — no redirect needed; delete view only |
| **Invoices/Index** | **retire** | `InvoicesController.Index` → 301 to `/provider/finance#invoices`; **`Download` action stays** (PRINT2, linked from Finance ~L223); delete view (~60 L ≈ 1:1 duplicate of Finance invoices tab) | One invoices UI | 301 `provider/invoices` → `provider/finance#invoices`; auth-gated, zero SEO impact |
| **PaymentMethods/Index** | **retire** | `PaymentMethodsController.Index` → 301 to `/provider/finance#methods`; **Create/Edit/Delete POST actions stay** (posted from Finance ~L243/L330/L376, already redirect to Finance); delete view (~150 L ≈ 1:1 duplicate of Methods tab) | One methods UI; dead nav key `"PaymentMethods"` removed | 301; auth-gated, zero SEO impact |
| Settings/Index | keep | — | Phase 5/8 polish | none |
| Shared/_TourWizardSteps | keep | verify `fa-chevron-right` separator (~L46) flips in AR (rtl.css ~L153 already covers it — verification only) | — | n/a |
| ../Shared/_ProviderSidebar | keep, thin | avatar fallback `avatar/01.jpg` (~L19) → initials avatar (`.avatar-*` utilities); collapsible (PROV5) | honest UI, no stock photo | n/a |
| _ViewImports | keep (extend) | add localization using/inject + new `_ViewStart.cshtml` | deletes ~110 boilerplate lines area-wide | n/a |

**Rejected reductions (one-line reasons):**
- Bookings/Index + ProviderBookings/Index merge — different domain objects (join-requests vs bookings lifecycle) and the scope doc (`3-provider-dashboard.md` §4.6) keeps them as distinct surfaces; merging would create one overloaded page, not better UX.
- Tours/Edit sub-resource pages → tabs inside Edit — each sub-resource has its own paged data + forms; a 7-tab mega-page hurts load, deep-linking, and the wizard model (PROV1). Wizard chrome (`_TourWizardSteps`) already provides continuity.
- Provider/Apply + Provider/Status merge — distinct lifecycle stages with different permissions (`Register` vs `Read`); deep links from emails target each separately.

**New extracted partials** (net additions, from existing duplication): `Areas/Provider/Views/Shared/_ConfirmActionModal.cshtml` (replaces ~270 L of per-row modals across 9 views), `_ProviderEmptyState.cshtml` (or reuse Public `_EmptyState` if model-compatible — decide at execution; ~20 duplicated blocks), `_KpiCard.cshtml` (10 instances), `_ToursResults.cshtml`, `_BookingsResults.cshtml`, `_ReviewsResults.cshtml`, `_FinancePayoutsTab.cshtml`/`_FinanceInvoicesTab.cshtml`/`_FinanceMethodsTab.cshtml` (for lazy tab loads). Net file count lands ≈ 41, but **page views drop 33 → 25** and ~1,000+ duplicated lines disappear.

---

## §3 Phases

Ordering: bugs/dead code → structure/AJAX/view-reduction (with `[Backend]` additions landing in the same phase as their consumer) → conversion UX → l10n/RTL → a11y → perf.

---

### Phase 0 — Dead code & bug fixes

**Rules:** REV (dead-code removal), CON1, A11Y6.

Files & steps:
1. Delete `Areas/Provider/Controllers/EarningsController.cs` (entire class already commented out; audit banner ~L1–18 authorizes deletion) and orphan `Areas/Provider/Views/Earnings/Index.cshtml` (204 L, 0% localized, superseded by Finance/Index).
2. `Areas/Provider/Controllers/InvoicesController.cs`: replace `Index` body (~L25–35) with `return RedirectPermanent("/provider/finance#invoices");` keep `Download` (~L42–55) untouched. Delete `Views/Invoices/Index.cshtml`.
3. `Areas/Provider/Controllers/PaymentMethodsController.cs`: replace `Index` body with `return RedirectPermanent("/provider/finance#methods");` keep Create/Edit/Delete POSTs. Delete `Views/PaymentMethods/Index.cshtml`. Remove dead `ViewData["ProviderNav"] = "PaymentMethods"` writes.
4. Fix duplicated `role="dialog" aria-modal="true"` misplaced on `<form class="modal-content">` instead of the `.modal` element (~25 occurrences across the area's modals — Bootstrap manages the dialog role; remove the duplicates). This is mostly absorbed by `_ConfirmActionModal` in Phase 2; fix here only the modals that survive as bespoke (booking reject/cancel reason modals).
5. `Finance/Index.cshtml`: add `id="invoices"` / `id="methods"` anchors + JS-free tab pre-selection from `location.hash` (tiny inline-free enhancement in Phase 3's `provider-finance.js`; until then the anchor scrolls to the tab strip — acceptable interim).

**Acceptance criteria:**
- `provider/invoices` and `provider/payment-methods` 301 to the Finance anchors; `Download` and all PaymentMethods POSTs still work.
- Build: zero new warnings. No view references `Earnings`.
- RTL/LTR: n/a (no markup added) — smoke-check Finance anchors in both directions anyway.

**Commit:** `refactor(provider): delete dead Earnings/Invoices/PaymentMethods pages, 301 into Finance per REV/CON1`

---

### Phase 1 — Area shell & controller structure

**Rules:** CON1, C2 (deferred attribute to Phase 8; structure here), A11Y6, REV; auth unification per `WebPermission` catalog.

Steps:
1. Create `Areas/Provider/Views/_ViewStart.cshtml` → `Layout = "~/Views/Shared/_Layout.cshtml";` then delete the 33 explicit `Layout =` lines.
2. Extend `Areas/Provider/Views/_ViewImports.cshtml` (6 L) with `@using Microsoft.Extensions.Localization` + `@inject IStringLocalizer<SharedResource> Localizer`; delete the ~80 per-view duplicate lines. (Keep the using+inject inside `Areas/Provider/Views/Shared/*` partials that are rendered cross-location — the `_ProviderSidebar` at `Areas/Provider/Shared/` is OUTSIDE `Views/` so `_ViewImports` does **not** apply to it; it keeps its own injects.)
3. Sidebar consolidation: replace the 9 copy-pasted `SetSidebar()` controller helpers and the 29-view `ViewBag.Sidebar as ProviderSidebarVm ?? new …` fallback (10 with hardcoded English `?? "Provider"`) with one mechanism: a small `ProviderSidebarViewComponent` (or an action filter on a new `ProviderAreaController : BaseController` base) that populates the VM once. Tours-family pages then also get correct active-nav highlighting (today they fall back to `"Dashboard"`, `_ProviderSidebar` ~L7).
4. Controller boilerplate dedup: introduce `Areas/Provider/Infrastructure/ProviderTourResourceController` (or shared private-helper extraction into `BaseController`-adjacent static helpers) covering the verbatim-identical `ApplyValidation` (~18 L × 5: TourPricing ~L165, TourSchedules ~L167, TourWaypoints ~L186, TourAvailability ~L209, TourGuides ~L111), `Denied` (×7), `NotFoundRedirect` (×7), `RedirectToStatus` (×8), `Finish` (×3: ProviderBookings ~L101, Packages ~L131, TourApplications ~L96). Target: remove ~210+ duplicated lines. **Do not** change routes or outcomes; pure extraction (precedent: Public ReviewsController collapse of 40 duplicated actions — here the duplication is helper-level, so extraction not action-merging is the correct analogue).
5. Auth-style unification: add the missing declarative attributes — `[RequirePermission(WebPermission.TourBooking.*)]` on Bookings/ProviderBookings actions, `Review/ReviewReply` on Reviews, profile permissions on Settings — matching the imperative checks' semantics; convert the 8 Tours-family imperative `ICurrentUser.HasPermission` calls to `[RequirePermission]` **only where the check is unconditional at action entry** (the conditional double-check in TourImages ~L28–29 stays imperative with a comment). Verify against `yallajo-plan/3-provider-dashboard.md` permission table.
6. `SettingsController` ~L160: route the direct `ProviderApiClient.GetSettingsAsync` call through `ProviderFacade` (facade method exists conceptually; add `GetSettingsAsync` pass-through) so the four-tier rule holds.

**Acceptance criteria:**
- All 33 pages render identically (visual diff EN+AR, light+dark); active-nav now correct on Tours-family pages.
- `grep -r "Layout = " Areas/Provider/Views` → 0 hits; `grep -rc "ViewBag.Sidebar as"` → 0.
- Permissions behave identically (manual matrix: provider with/without Tour.Update etc.).
- Build zero new warnings. RTL/LTR walkthrough of sidebar + 3 sample pages.

**Commit:** `refactor(provider): area _ViewStart/_ViewImports, sidebar component, dedupe controller helpers, unify [RequirePermission] per CON1/REV`

---

### Phase 2 — Shared partials & view reduction (merges)

**Rules:** L6, F8/MOD1–MOD5, CON4, RTL1, A11Y4/A11Y5; Public precedents §2.

Steps:
1. Create `Areas/Provider/Views/Shared/_ConfirmActionModal.cshtml` — parameterized (id, title, body, form action, hidden fields, danger/primary verb, optional reason textarea with minlength/maxlength). Replace per-row modal stamps in: Tours/Index (~L182–225), TourPricing/Index, TourSchedules/Index, TourWaypoints/Index, TourAvailability/Index, TourImages/Index, Packages/Index, Reviews/Index, Finance/Index (~270 L removed). One modal instance per page, populated via `data-*` attributes + tiny `provider-confirm.js` extension of the existing `data-confirm` pattern in `form-ux.js` (MOD1: one modal). No-JS path: modal markup is still a plain form POST (PE1).
2. Empty states: adopt Public `_EmptyState.cshtml` if its model fits (check `Areas/Public/Views/Shared/_EmptyState.cshtml` signature at execution); else create `_ProviderEmptyState.cshtml`. Replace the ~20 duplicated `element/17.svg` blocks. Add missing CTAs: TourAvailability/Index (inline CTA into empty state), TourGuides/Index (L6: every empty state has a CTA).
3. `_KpiCard.cshtml` partial for the 10 stat-card instances (Dashboard, Finance).
4. Pagination: adopt `Areas/Public/Views/Shared/_Pagination.cshtml` for Tours/Index (~L164–177 bespoke) and Packages/Index (~L169–190 chevron style); if the Public model doesn't fit the Provider VMs, add a thin Provider variant. Packages' unflipped `fa-chevron-left/right` issue disappears with the shared partial (and rtl.css already flips chevrons).
5. Merge the 5 Create/Edit pairs into `Upsert.cshtml` per resource (Tours, TourPricing, TourSchedules, TourWaypoints, TourAvailability): controllers' GET/POST actions keep their routes and return `View("Upsert", vm)`; VMs already carry edit-vs-create state (`IsEdit` exists on TourAvailability `_Form` — add where missing). ~380 of 470 wrapper lines removed.
6. Centralize the `Money()/Count()/Date()` helpers (~14 copies, inconsistent `N0/N2/N3`) into `Areas/Provider/Models/Shared/ProviderFormat.cs` static helpers honoring CON3 (JOD 3 decimals) + culture date patterns; replace `"d MMM yyyy"`-style literals.

**Acceptance criteria:**
- View count: 41 → 33 pages confirmed (`ls` evidence in commit body).
- Every page with a delete/destructive action still confirms via modal (F8), static backdrop (MOD3), safe-action autofocus (MOD5), works without JS (PE1).
- All currency renders JOD with 3 decimals (CON3); spot-check AR (Eastern-Arabic numerals per culture default — wrapped `<bdi dir="ltr">` where the value is a code, not a quantity).
- RTL/LTR: full walkthrough of all merged/partial-ized pages in both directions; chevrons/arrows flip; light+dark; 390/1280.

**Commit:** `refactor(provider): shared confirm-modal/empty-state/pagination partials, merge 5 Create+Edit pairs (41→33 views) per L6/F8/MOD1/CON3/RTL1`

---

### Phase 3 — AJAX modernization

**Rules:** PE1/PE2, R2/S1, JS1–JS6 (esp. JS5 typed client only), L1/L4/L5, SEC7, MOD8, NF1.

All conversions: controller action gains `if (WantsAjax()) return PartialView("_XResults", vm);` before the existing `View(...)` return — PRG fallback preserved (PE1). Pair with `[OutputCache(NoStore = true)]`-compatible behavior (this is an auth area — no output caching; C2 NoStore lands in Phase 8).

Steps:
1. **Tours/Index** → extract table+pagination into `_ToursResults.cshtml`; wire `listing.js` (pushState page/status; skeleton rows L1; aborted stale requests JS6). New script `wwwroot/assets/js/provider-tours.js` via `@section Scripts` (J2), self-init via `data-yj-component` (JS3/JS4).
2. **ProviderBookings/Index** → `_BookingsResults.cshtml`; status filter + (Phase 5's new date/tour filters) submit via AJAX with pushState; cursor "Load more" button using `NextCursor` (cursor paging is the sanctioned high-volume exception, scope doc §4.6).
3. **Bookings/Index** → join-request approve/reject + booking confirm/reject POSTs get AJAX submit via `window.YallaJo.api.postForm` + toast (NF1) + row update; full-page PRG without JS.
4. **Reviews/Index** → reply/edit/delete/report forms submit via AJAX, partial refresh of the review card; no-JS PRG kept.
5. **Finance/Index** → lazy-load non-default tabs (scope doc §4.8): split tab bodies into `_FinancePayoutsTab/_FinanceInvoicesTab/_FinanceMethodsTab` partials; controller gains three GET partial endpoints (`provider/finance/tabs/{payouts|invoices|methods}`) returning `PartialView`; first paint renders the active tab SSR (R2), others load on first activation via `api.loadPartial` (MOD8/L1 skeleton). `location.hash` selects tab (completes Phase 0 anchors). No-JS: all three tabs render SSR when JS is absent — implement as `<noscript>`-friendly: server renders all tabs when `!WantsAjax()` on full page load (current behavior preserved = the no-JS path).
6. New scripts: `provider-tours.js`, `provider-bookings.js`, `provider-reviews.js`, `provider-finance.js` — all ES modules, vanilla, through `window.YallaJo.api` only (JS5), debounced inputs (J7), AbortController (JS6).

**Acceptance criteria:**
- Every converted flow works with JS disabled (PE1) — manual check each.
- No `fetch(` outside `api-client.js` (`grep` gate); antiforgery present on all AJAX POSTs (SEC7 — comes free via api-client).
- Back/forward restores list state (pushState, S1).
- Skeletons on load (L1); toasts for action results (NF1, max 3, 5 s).
- RTL/LTR: AJAX-injected partials render correctly in AR (partials inherit page culture — verify dates/`<bdi>` inside `_XResults`); light+dark; 390/1280.

**Commit:** `feat(provider): AJAX listings + lazy finance tabs with PRG fallback per PE1/S1/JS5/L1/MOD8`

---

### Phase 4 — Backend-powered UX (`[Backend]` additions + their consumers, same phase)

**Rules:** API1, API7, F10, CAL1/CAL2, ERR3, X14, R4. Each endpoint: minimal-API endpoint class → MediatR query/handler in `{Module}.Application` → `Result<T>.ToApiResult()`, auth via `MustHavePermissionAttribute` + `RequireAuthorization()`, following each module's existing per-feature endpoint-class pattern (e.g. `ContentTours.Presentation/Endpoints/TourSearch/TourSearchEndpoints.cs`).

#### 4.A Dashboard real KPIs + charts
- `[Backend]` **B4 — Provider finance summary (Finance module).** GET `/api/v1/finance/provider/summary`. Params: none (provider from auth). Response: `{ grossTotal, netEarnings, thisMonth, pendingPayout, totalCommission, currency }` — exposes the **already-existing** richer DTO `GuideEarningsSummary(TotalGross, ThisMonth, PendingPayout, TotalCommission, NetEarnings)` (`Finance.Contracts/Services/IGuideEarningReader.cs`, today consumed only by ContentTours guide endpoints). Auth: `Payout.Read`-class permission (match existing `payouts/provider`). Wiring: new query+handler in `Finance.Application/Earnings/` (pattern: `EarningHandlers.cs` ~L10) → endpoint in `Finance.Presentation/Endpoints/Earnings/EarningsEndpoints.cs` (sibling of `/guide/summary` ~L30) → `EarningsApiClient.GetProviderSummaryAsync` → `EarningsFacade`/`DashboardFacade` → VM. Additive; `/finance/guide/summary` untouched.
- `[Backend]` **B5 — Provider booking status counts (Booking module).** GET `/api/v1/booking/provider/bookings/stats`. Params: optional `fromDate`,`toDate`. Response: `{ total, pending, confirmed, completed, cancelled, rejected }`. Auth: same permission metadata as GET `provider/bookings` (`TourBookingEndpoints.cs` ~L357). Wiring: `Booking.Application/Queries/GetProviderBookingStats/` → endpoint beside the list → `ProviderBookingsApiClient.GetStatsAsync` → `DashboardFacade` + `ProviderBookingsFacade`. (API7: one aggregate call instead of paging through lists.)
- Consumer: `DashboardFacade.GetDashboardAsync` adds the two calls to its existing `Task.WhenAll` (API1); `Dashboard/Index.cshtml` gains KPI row (bookings by status, revenue, pending payout) using `_KpiCard`, plus an ApexCharts sparkline/donut (X14 dashboards-allowed; vendor file already in `wwwroot` — reference it from the page's `@section Scripts`, do NOT add a CDN). Each dashboard section degrades independently with a Retry card (ERR3) — facade already returns per-call results.

#### 4.B Tours list status tabs
- `[Backend]` **B1 — Tour status counts (ContentTours module).** GET `/api/v1/tours/provider/my-tours/status-counts`. Params: none. Response: `{ draft, pendingReview, published, archived, rejected, total }` (align names with the `status` filter values accepted by `my-tours`, `TourSearchEndpoints.cs` ~L95–132). Auth: same as `my-tours`. Wiring: `ContentTours.Application` query+handler → endpoint in `TourSearchEndpoints.cs` → `ProviderToursApiClient.GetStatusCountsAsync` → `ProviderToursFacade.GetIndexAsync` (WhenAll with the list call, API1) → `ToursIndexVm`.
- Consumer: Tours/Index status filter becomes tabs with counts (`All (12) · Published (8) · Draft (3)…`), AJAX-driven via Phase 3's `_ToursResults`.

#### 4.C Place typeahead (tour form)
- `[Backend]` **B2 — Place lookup (ContentPlaces module).** GET `/api/v1/places/lookup?term={q}&pageSize={n}` (clamp 1–20, R4). Response: `[ { id, name, city } ]` — lightweight projection. Auth: anonymous-or-authenticated same as existing `GET /places` (`PlaceEndpoints.cs` ~L33–52); read-only. Wiring: `ContentPlaces.Application/Queries/Place/LookupPlaces/` (name-prefix search — `ListPlacesQuery` has no name search today) → endpoint → `ProviderPlacesApiClient.LookupAsync` → `ProviderPlacesFacade`.
- Consumer: Tours `_Form` place `<select>` (currently fed by fixed first-50 `GetPlaceOptionsAsync`) becomes a searchable async combobox (Choices.js is vendored — verify AR/RTL behavior per RTL contract; debounced ≥300 ms J7; AbortController JS6). **Web proxy required** (browser never calls API host, JS5): add `ToursController.PlacesLookup` GET `provider/tours/places-lookup?term=` returning `Json(...)` via facade. No-JS fallback: the existing first-50 `<select>` renders SSR (PE1).

#### 4.D Guide picker (F10)
- `[Backend]` **B7 — Guide lookup (ContentTours module).** GET `/api/v1/tours/{tourId}/guides/lookup?term={q}&pageSize={n≤10}`. Response: `[ { userId, displayName, avatarUrl? } ]` — guides eligible for assignment (mirror the validation set used by POST `tours/{tourId}/guides`). Auth: same permission as the assign endpoint. Wiring: `ContentTours.Application` query → endpoint beside guides CRUD → `ProviderTourGuidesApiClient.LookupAsync` → facade.
- Consumer: TourGuides/Index GUID input (~L36–37) → async combobox showing names+avatars (F10). Web proxy action `TourGuidesController.Lookup` returning Json. No-JS fallback: SSR `<select>` of first N eligible guides.

#### 4.E Availability calendar (CAL1)
- `[Backend]` **B3 — Availability month view (Booking module).** GET `/api/v1/booking/availability/{tourId}/calendar?year={y}&month={m}`. Response: `[ { date, slotCount, totalCapacity, bookedSeats, hasOpenSlots } ]` (one row per day with slots). Auth: same as `…/{tourId}/manage` (`AvailabilitySlotEndpoints.cs` ~L38). Wiring: `Booking.Application/Queries/GetAvailabilityCalendar/` → endpoint → `ProviderTourAvailabilityApiClient.GetCalendarAsync` → facade.
- Consumer: TourAvailability/Index gains a month-grid above the table — per-month lazy load (CAL1), day tap opens day detail (bottom sheet ≤768 px, CAL2/MOD6). Web proxy `TourAvailabilityController.Calendar` returning PartialView (month grid is server-rendered partial — simpler + localizable). Table view remains the no-JS path (PE1).

#### 4.F Reviews N+1 elimination (API7)
- `[Backend]` **B6 — Batch ratings (Social module).** GET `/api/v1/social/reviews/ratings/batch?entityType=Tour&entityIds={csv≤50}`. Response: `[ { entityId, average, count } ]`. Auth: same as existing single `ratings` (`ReviewEndpoints.cs` ~L250). Wiring: `Social.Application` query (single grouped DB query) → endpoint → `ReviewsApiClient.GetRatingsBatchAsync` → `ReviewsFacade.GetAsync` replaces its per-tour ratings loop.
- Consumer: Reviews/Index loads with 2 API calls instead of 1+2N. (The per-tour reviews *list* stays on the existing endpoint — only the selected tour's reviews are fetched, which the facade already does once a tour filter is applied; verify `ReviewsFacade.cs` ~L15–41 during execution and keep behavior.)

**Acceptance criteria (phase-wide):**
- Solution build (Web + Api + touched modules) zero new errors/warnings; existing API consumers unaffected (no signature changes — grep each touched endpoint file for route stability).
- Each endpoint returns correct shape via manual smoke (Swagger/HTTP file) and enforces its permission (401/403 matrix).
- F10: zero GUID-paste inputs remain for guides/places (Packages tours picker is Phase 5).
- Dashboard renders with any subset of KPI calls failing (ERR3 Retry cards).
- RTL/LTR: calendar grid, comboboxes (Choices.js), charts (ApexCharts `rtl` option / verify labels) checked in AR-RTL and EN-LTR; light+dark; 390/1280.

**Commit:** `feat(provider): dashboard KPIs, status tabs, place/guide lookups, availability calendar, batch ratings — [Backend] B1–B7 per API1/API7/F10/CAL1/ERR3`

---

### Phase 5 — Conversion UX

**Rules:** F1–F10, PROV1–PROV6, L2/F7, F8/F9, NF6, D3/D5, SEC4 (verify, server-side already), ST1.

Steps:
1. **Tour wizard polish (PROV1–PROV4, F4):** `_TourWizardSteps` already exists; ensure all 5 steps link correctly from merged Upsert pages; add localStorage autosave of the tour Upsert form (`yallajo:provider:tour-draft:{userId}`, F5) with draft-recovery banner (PROV3) and discard-confirm modal (PROV4); `beforeunload` guard on dirty forms (F9). New `provider-tour-form.js`.
2. **Packages tours picker (F10):** `IncludedTourIds` textarea (~L85–88) → multi-select populated from existing `my-tours` (pageSize=100) — **no backend needed**; SSR-rendered `<select multiple>` as the no-JS path, Choices.js enhancement.
3. **Provider/Status replace-document form (~L160–183):** manual FileUrl/FileName/FileSizeBytes inputs → real `<input type="file">` posting to the existing `provider/apply/documents/replace` multipart route (ProviderController ~L103–105 comment explains route scoping — keep it).
4. **Booking filters:** ProviderBookings/Index gains date-range + tour filters — API already supports `fromDate,toDate,tourId,cursor` (`TourBookingEndpoints.cs` ~L357–427); extend `ProviderBookingsApiClient.GetListAsync` (~L23–32) to pass them; flatpickr for dates (vendored; verify AR locale + RTL); "Load more" cursor button.
5. **Confirms parity (F8):** Bookings/Index `Approve` and Packages/Manage `Submit` get `data-confirm`; destructive modals keep static backdrop (MOD3) + safe-action autofocus (MOD5).
6. **Waypoints reorder:** wire drag-handle (vanilla, pointer events — no new dep) to existing PUT `waypoints/reorder`; no-JS fallback: keep current order-number form posts. Optimistic reorder with rollback toast on failure (NF6).
7. **TourImages:** client-side image preview + size/type pre-check before upload (server SEC4 magic-byte validation already authoritative); upload progress indicator.
8. **Buttons:** verify `data-loading` coverage on all POST forms area-wide (form-ux.js global; add the attribute where missing) — L2/F7.
9. **Mobile (D3/D5):** sticky bottom action bar on ProviderBookings/Details + Tours/Upsert at ≤768 px (logical-properties CSS in `site.css`, RTL1).
10. **ST1 spot-check:** Tours Upsert + ProviderDocuments Replace + Availability Edit already carry `rowVersion` fields — verify hidden inputs survive the view merges; conflict outcome arms (e.g. TourPricing ~L135) keep their messages.

**Acceptance criteria:**
- Draft autosave: type → reload → recovery banner → restore/discard both work; key matches `yallajo:provider:tour-draft:{userId}`.
- Zero GUID-paste inputs remain anywhere in the area (F10 grep: `placeholder="00000000`).
- All filters round-trip in URL (pushState) and work without JS (PE1).
- RTL/LTR: flatpickr AR calendar, drag handles, sticky bars verified both directions; light+dark; 390/1280.

**Commit:** `feat(provider): tour-draft autosave, F10 pickers, booking filters, reorder DnD, upload preview per F5/F10/PROV1-6/NF6/D5`

---

### Phase 6 — Localization & RTL

**Rules:** CON1–CON4, RTL1–RTL4. Consumes the direction-fragile audit (§1.4 + below).

Direction-fragile/l10n inventory (verified) and fixes:
1. **Raw enum/status strings** → resx via existing `Provider.Status.*` pattern: Bookings/Index `@jr.Status` ~L85 / `@b.Status` ~L165; Finance/Index `@(p.Status ?? "—")` ~L137, ~L165, ~L217; (Invoices/Index ~L72 deleted in Phase 0). Map enum → localized string in VMs/mappers, not views.
2. **Hardcoded English fallbacks:** `?? "Provider"` sidebar fallback (removed with Phase 1 sidebar component — verify); `"JOD"` currency literals (Tours/Index ~L15, TourApplications ~L14, TourPricing/Index ~L16) → currency comes from VM/`ProviderFormat` (CON3).
3. **SettingsController ~L187–196** hardcoded `ProviderTypeLabel` enum mirror → localized labels via resx keys.
4. **Directional icons:** rtl.css ~L150–159 already flips `fa-arrow-left` (17×), `fa-chevron-left/right` (3×) — **verification line-items only**: `_TourWizardSteps` ~L46 separator, all back buttons, any chevrons surviving Phase 2's pagination swap. Any `bi-*` directional icons introduced by new partials must be added to the existing `bi-*` flip section in `rtl.css` — never inline (RTL1/RTL2). Non-directional icons (stars, hearts, phones) must NOT flip (RTL2).
5. **Numbers/codes/refs:** discipline is already excellent (`<bdi dir="ltr">` everywhere except deleted Earnings) — audit new Phase 2–5 markup: booking reference tokens, cursor values, invoice numbers, coordinates (TourWaypoints), phone in Settings (`dir="ltr"` present ~placeholder `+962 7 9999 9999`) (RTL3).
6. **Date formats:** replace remaining literal `"d MMM yyyy"` (Dashboard ~L15 etc.) with culture patterns via `ProviderFormat` (CON2/CON3); ISO `yyyy-MM-dd` in Status ~L12 stays (intentional, `dir="ltr"`-wrapped).
7. **New keys:** all Phase 2–5 strings (calendar labels, filter labels, draft-banner, toasts, tab names) added to BOTH resx files; grep-before-add; flush-left before `</root>`; UTF-8.
8. **Vendored widgets in AR:** flatpickr (AR locale file vendored? verify; else month names via config), Choices.js dropdown alignment, offcanvas sidebar (end→start anchoring — Bootstrap logical), toasts placement (bottom-start in RTL via form-ux.js — verify existing behavior), ApexCharts label direction.

**Acceptance criteria:**
- `grep -rn '"[A-Z][a-z]\+ ' Areas/Provider/Views` style sweep + manual AR walkthrough of all 25 pages: zero English leaks, zero raw resx keys.
- Build green (resx duplicates would break it — MSB3568).
- Full RTL4 merge gate: every page EN-LTR + AR-RTL, light+dark, 390/1280.

**Commit:** `fix(provider): localize statuses/currency/dates, RTL verification sweep per CON1-CON4/RTL1-RTL4`

---

### Phase 7 — Accessibility

**Rules:** A11Y1–A11Y9, MOD2.

Steps (from verified audit):
1. Reviews/Index collapse triggers (~L126, ~L144, ~L146): add `aria-expanded` + `aria-controls`; prefer `<button>` over `<a role="button">`.
2. Complete the `role="dialog"` cleanup started in Phase 0 for any remaining bespoke modals; confirm focus trap + return-focus via Bootstrap defaults (MOD2).
3. Status badges: ensure icon+text everywhere (A11Y5) — Phase 6 localized the text; verify Finance/Bookings badges carry icons.
4. `aria-live="polite"` region for AJAX result updates (Phase 3 lists) + toast container (A11Y9).
5. Heading audit per page — one `h1`, no jumps (A11Y6); the known h1→h4 offender (Earnings) is deleted; sweep merged Upsert pages.
6. New empty-state images: `width/height` + `aria-hidden="true"` (the partial from Phase 2 bakes this in — verify).
7. Icon-only buttons: audit confirmed labels exist (Pricing/Schedules edit/delete) — sweep new Phase 3–5 buttons (calendar day cells, drag handles get `aria-label` + keyboard alternative: keep the order-number no-JS form as the accessible path).
8. Touch targets ≥44 px on mobile action rows (D2).

**Acceptance criteria:**
- Keyboard-only pass on: tour Upsert wizard, availability calendar, finance tabs, confirm modals (open→confirm→focus-return).
- Screen-reader smoke (NVDA): status badges announce localized text; AJAX updates announce via live region.
- RTL/LTR + light/dark spot-check of focus indicators.

**Commit:** `fix(provider): a11y sweep — collapse aria, live regions, headings, touch targets per A11Y1-A11Y9/MOD2`

---

### Phase 8 — Performance & consistency

**Rules:** C2, API1, R4, L1, JS6, X6.

Steps:
1. **C2 NoStore:** the area has zero cache headers today. Add `[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]` (or the project's standard NoStore filter — check how Accounts/Admin areas do it and match) at the area-base-controller level introduced in Phase 1.
2. **Task.WhenAll (API1):** `SettingsController.Index` sequential awaits ~L44/~L53 → `Task.WhenAll`; review `ReloadIndex`/`ReloadAsync` re-fetch helpers (Packages ~L145, ProviderDocuments ~L78, TourGuides ~L93) — parallelize where two independent fetches occur.
3. **Page-size clamps (R4):** verify every list facade clamps 1–50 (ReviewsFacade's `pageSize=100` my-tours call is replaced in Phase 4 — confirm; ProviderBookings `pageSize=50` OK).
4. Remove the 2 legacy inline styles (TourApplications ~L102, Bookings ~L87) → `site.css` utilities or justify with X6 comment.
5. Image hygiene: gallery thumbs keep `loading="lazy"` + dimensions (already present — verify post-Phase 5 preview changes).
6. Script budget: confirm the 5 new page scripts load only on their pages (`@section Scripts`), no global bloat; AbortController on every typeahead/filter (JS6).
7. Final dead-code sweep: grep for now-unreferenced VM properties/mappers from deleted views (Earnings VMs in `Models/`, PaymentMethods/Invoices index VMs) — delete with build verification.

**Acceptance criteria:**
- Response headers on any Provider page: `Cache-Control: no-store`.
- Settings page TTFB improved (two calls parallel); no functional change.
- Build zero new warnings; full-area EN/AR + light/dark + 390/1280 final regression walkthrough.

**Commit:** `perf(provider): NoStore headers, parallel fetches, page-size clamps, inline-style cleanup per C2/API1/R4/X6`

---

## §4 Execution order & sizing

| # | Phase | Size | Depends on | Parallelizable? |
|---|---|---|---|---|
| 0 | Dead code & bugs | 1 d | — | — |
| 1 | Area shell & controller structure | 2.5 d | 0 | — |
| 2 | Shared partials & view merges | 3.5 d | 1 | — |
| 3 | AJAX modernization | 4 d | 2 | ∥ with Phase 4 backend halves (different repos/layers) |
| 4 | Backend-powered UX (B1–B7 + consumers) | 6 d | 2 (consumers), backend specs independent | Backend endpoints (≈3 d) ∥ Phase 3; consumers after both |
| 5 | Conversion UX | 4 d | 3, 4 | — |
| 6 | l10n & RTL | 2 d | 5 | ∥ with Phase 7 |
| 7 | Accessibility | 2 d | 5 | ∥ with Phase 6 |
| 8 | Performance & consistency | 1.5 d | all | — |

**Total ≈ 26.5 dev-days (~5.5 weeks solo; ~4 weeks with the noted parallelization).** One conventional commit per phase; build + RTL4 gate before each merge.

## §5 Proposed `[Backend]` endpoints (summary)

| ID | Route | Module | Purpose |
|---|---|---|---|
| B1 | GET `/api/v1/tours/provider/my-tours/status-counts` | ContentTours | Tours list status tabs |
| B2 | GET `/api/v1/places/lookup?term&pageSize` | ContentPlaces | Place typeahead in tour form |
| B3 | GET `/api/v1/booking/availability/{tourId}/calendar?year&month` | Booking | Availability month grid (CAL1) |
| B4 | GET `/api/v1/finance/provider/summary` | Finance | Rich earnings KPIs (existing internal DTO exposed) |
| B5 | GET `/api/v1/booking/provider/bookings/stats` | Booking | Booking counts by status (dashboard + tabs) |
| B6 | GET `/api/v1/social/reviews/ratings/batch?entityType&entityIds` | Social | Kill ratings N+1 (API7) |
| B7 | GET `/api/v1/tours/{tourId}/guides/lookup?term&pageSize` | ContentTours | Guide picker (F10) |

All additive; no existing route modified; each consumed via new ApiClient method → Facade → VM, with a Web proxy action where the browser needs JSON/partials (JS5).
