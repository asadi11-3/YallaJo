# Business Area Master Plan — UI/UX Modernization, View Reduction, AJAX & Backend Support

> **Audience:** an executing agent with **zero conversation context**.
> **Produced from:** code-verified inventory of `src/Hosts/YallaJo.Web/Areas/Business`, `src/Modules/ContentPlaces`, `src/Modules/Security`, plus the binding rules contract `yallajo-plan/UI-UX-Design.md`, the structural exemplar `docs/public-area-master-plan.md`, and scope docs `yallajo-plan/3-provider-dashboard.md` (§4.5), `yallajo-plan/yallajo-dashboard-ui-ux-plan.md` (§0.2–0.4), `yallajo-plan/gaps/3-provider-dashboard-gaps.md`, `yallajo-plan/controllers-by-plan.md` (L112–121).
> **All line numbers are indicative** (~L) — re-verify before editing.
> **This is a PLAN. No code was changed while producing it.**

---

## §1 Context & Ground Rules

### 1.1 What the Business area is

The Business area (`/business/*`) is the **business-owner self-service console**: register a business, manage its profile, hours, amenities, services, staff, and accessibility features. It is one of the 8 real Web areas (`yallajo-plan/0-architecture-and-rules.md`, area #6) and corresponds to §4.5 of the provider dashboard plan. It is **fully auth-gated** (`[Authorize]` + `[RequirePermission(WebPermission.Business.Read)]` at class level on every controller) — **there are no SEO concerns**: no `SetSeo`, no sitemap entries, no public deep links to preserve beyond the in-app routes themselves. This materially loosens view-reduction constraints compared to the Public area.

Layout: all 9 views use the **main public layout** `~/Views/Shared/_Layout.cshtml` (NOT `_AdminLayout`), so the Google-Fonts debt in `_AdminLayout.cshtml`/`_AuthLayout.cshtml` does **not** apply to this area — no font-migration phase needed.

### 1.2 Architecture conventions (must be preserved)

- Four-tier BFF pipeline: **Controller → Facade → ApiClient → IApiClient** (typed `ApiResult`, error-as-value). The browser **never** calls the API host; all AJAX goes through Web endpoints via `window.YallaJo.api` (JS5).
- `BaseController` helpers: `GuardSignOut`, `SetSuccess`, `SetError`, `ApplyValidationErrors`, PRG everywhere, and **`WantsAjax()`** (returns PartialView for AJAX, PRG fallback otherwise — PE1).
- Suffix-based auto-DI: classes ending `Facade`/`ApiClient` register automatically — new ones need no manual registration.
- Permissions: `WebPermission.Business.{Read,Create,Update,Submit}`, `WebPermission.BusinessAmenity.{Create,Delete}`, `WebPermission.BusinessHours.Update`, `WebPermission.ServiceItem.{Create,Update,SoftDelete}`, `WebPermission.BusinessStaff.{Create,Delete}`, `WebPermission.AccessibilityFeature.Update`. API-side: `ContentPlacesFeatures.*`.
- Cache: success mutations evict `business:{businessId}` tag (C3) — every facade already does this; keep it on any new mutation.
- Cultures `en`/`ar`, default **`ar`**; `rtl.css` is appended **after** `style.css` for RTL cultures (no `style.rtl.css` swap exists). All `[dir="rtl"]` overrides go in `rtl.css` only.
- Concurrency: `BusinessDetailResponse` exposes `UpdatedAt`, not RowVersion — staleness messaging only, **no ST1 round-trip** (per `3-provider-dashboard.md` §4.5).

### 1.3 Verified inventory (code-verified)

| Component | Count | Items |
|---|---|---|
| Controllers | **6** | `MyBusinessesController` (144L, 7 actions), `AmenitiesController` (84L, 3), `HoursController` (75L, 2), `ServicesController` (112L, 5), `StaffController` (84L, 3), `AccessibilityController` (88L, 2) |
| Actions | **22** | see §1.4 |
| Views | **9** | `MyBusinesses/{Index,Register,Manage}`, `Amenities/Index`, `Hours/Index`, `Services/{Index,Edit}`, `Staff/Index`, `Accessibility/Index` |
| Facades | **7** | `MyBusinessesFacade`, `BusinessAmenitiesFacade`, `BusinessHoursFacade`, `BusinessServicesFacade`, `BusinessStaffFacade`, `BusinessAccessibilityFacade`, `BusinessWeatherFacade` |
| ApiClients | **7** | `MyBusinessesApiClient`, `AmenitiesApiClient`, `HoursApiClient`, `ServicesApiClient`, `StaffApiClient`, `AccessibilityApiClient`, `WeatherApiClient` |
| ViewComponents | **1** | `WeatherWidgetViewComponent` (view at host-level `Views/Shared/Components/WeatherWidget/Default.cshtml`) |
| Shared | **2** | `Shared/BusinessSidebarVm.cs`, `Shared/_BusinessSidebar.cshtml` (103L) |
| Page scripts | **1** | `wwwroot/assets/js/business-register.js` (54L, CSP-safe JSON-island place→coords prefill) |
| resx keys | **201** | `Business.*` keys present in BOTH `SharedResource.en.resx` (4,477 total `<data>` entries) and `.ar.resx` — **views are already largely localized**; the l10n debt is in controllers + facades + WeatherWidget view |

### 1.4 Action inventory (22 actions)

| Controller | Route | Verb | Notes |
|---|---|---|---|
| MyBusinesses | `business` + `business/businesses` | GET | Index (list, `GetMineAsync(page=1, pageSize=50)` — **no pagination UI**) |
| MyBusinesses | `business/businesses/register` | GET | Register form |
| MyBusinesses | `business/businesses/register` | POST | `Business.Create`; reload-on-error preserves input; redirect → Manage |
| MyBusinesses | `business/businesses/{id:guid}` | GET | Manage (profile + KPIs + weather) |
| MyBusinesses | `business/businesses/{id}/update` | POST | `Business.Update`, `ReloadManageAsync` preserves input |
| MyBusinesses | `business/businesses/{id}/resubmit` | POST | `Business.Submit` |
| Amenities | `business/businesses/{id}/amenities` | GET | Index |
| Amenities | `.../amenities/add` | POST | invalid ModelState → `SetError`+redirect — **loses user input** |
| Amenities | `.../amenities/{amenityId}/remove` | POST | delete |
| Hours | `business/businesses/{id}/hours` | GET | Index (7-day table) |
| Hours | `.../hours/save` | POST | `ReloadAsync` preserves input |
| Services | `business/businesses/{id}/services` | GET | Index |
| Services | `.../services/add` | POST | invalid → `SetError`+redirect — **loses input** |
| Services | `.../services/{serviceId}/edit` | GET | **separate Edit view — merge target** |
| Services | `.../services/{serviceId}/edit` | POST | update |
| Services | `.../services/{serviceId}/remove` | POST | soft delete |
| Staff | `business/businesses/{id}/staff` | GET | Index |
| Staff | `.../staff/add` | POST | **raw UserId text input — F10 violation** |
| Staff | `.../staff/{staffId}/remove` | POST | delete |
| Accessibility | `business/businesses/{id}/accessibility` | GET | Index |
| Accessibility | `.../accessibility/save` | POST | `ReloadAsync` preserves input |

Every POST has `[ValidateAntiForgeryToken]`; every controller has a private `SetSidebar(...)` (duplicated 6×); **no controller uses `WantsAjax()` today**; the area has **zero JSON/Partial endpoints**.

### 1.5 Feature → chain map (View → Controller → Facade → ApiClient → API)

Web ApiClient base = `/api/v1/places/businesses` (ContentPlaces module, `ContentPlaces.Presentation/Endpoints/`):

| Feature | Web chain | API endpoint(s) | Status |
|---|---|---|---|
| My businesses list | Index → MyBusinessesController → MyBusinessesFacade → MyBusinessesApiClient | `GET /places/businesses/mine` (BusinessEndpoints.cs ~L388) | ✅ exists; paged, but no pagination UI |
| Register / Update / Resubmit | Register/Manage → MyBusinessesController | `POST /places/businesses`, `PUT /{id}`, `POST /{id}/resubmit` | ✅ |
| Place options for Register | `GetPlaceOptionsAsync(pageSize=200)` → `GET /api/v1/places` | ✅ exists but 200-option dump; **`GET /places/lookup` already exists** (PlaceEndpoints.cs ~L56) — should be used instead | partial |
| Hours | Hours views → BusinessHoursFacade → HoursApiClient | `GET`+`PUT /{id}/hours` | ✅ |
| Amenities | → BusinessAmenitiesFacade → AmenitiesApiClient | `GET /{businessId}/amenities` (paged), `POST /{businessId}/amenities`, `DELETE /amenities/{amenityId}` | ✅ |
| Services | → BusinessServicesFacade → ServicesApiClient | `POST /{businessId}/services`, `GET/PUT /services/{serviceId}`, `DELETE /services/{serviceId}` (soft) | ✅ |
| Staff | → BusinessStaffFacade → StaffApiClient | `GET /{id}/staff` → `BusinessStaffDto {Id, BusinessId, UserId, Role}` — **no display name/email**; `POST /{id}/staff`, `DELETE /staff/{id}` | ⚠️ partial — UI shows synthetic "Staff member N" labels |
| User lookup (staff picker) | — none — | `GET /users` exists but requires `SecurityFeatures.User Read` (admin-grade; business owners don't hold it); **no search-by-name endpoint** | ❌ missing → `[Backend]` B1 |
| Accessibility | → BusinessAccessibilityFacade → AccessibilityApiClient | `GET`+`PUT /{id}/accessibility`; `GET /accessibility/catalog` also exists (AccessibilityFeatureEndpoints.cs ~L26) | ✅ |
| Weather widget | WeatherWidgetViewComponent → BusinessWeatherFacade → WeatherApiClient | `GET /api/v1/seo/weather/{placeId}` (`X-Weather-Stale: true` → background refresh per dashboard plan §0.2) | ✅; SSR-inline today, lazy-load candidate |

**Facade pattern (verified on BusinessAmenitiesFacade, representative of all):** each sub-feature GET loads the business via `MyBusinessesApiClient.GetByIdAsync` **then** the feature data — 2 **sequential** API calls per page (API1 violation; `Task.WhenAll` candidate). `Normalize()` maps 403 → "You do not own this business.", 404, 409 — **hardcoded English fallback strings in every facade** (CON1 debt).

### 1.6 Verified defects & debt register

| # | Defect | Location | Rule |
|---|---|---|---|
| D-1 | `AdminNoStoreCacheFilter.NoStoreAreas = ["Admin", "Provider"]` — **Business missing** → auth-gated pages cacheable | `Infrastructure/Mvc/AdminNoStoreCacheFilter.cs` ~L11 | C2 |
| D-2 | Inline `style="width:48px;height:48px;"` on sidebar avatar | `Shared/_BusinessSidebar.cshtml` ~L30 | X6 |
| D-3 | Grid inconsistency: `_BusinessSidebar` renders `col-lg-4 col-xl-3` markup, but `MyBusinesses/{Index,Register,Manage}` place it **outside** a `.row` with content in `.container.vstack` (broken grid); sub-feature views use `.row.g-4` correctly | 3 MyBusinesses views | D1/CON2 |
| D-4 | Raw `Form.UserId` text input for adding staff | `Staff/Index.cshtml` ~L52–56 | **F10** |
| D-5 | Staff rows show synthetic "Staff member N" because `BusinessStaffDto` has no display info | Staff/Index + API DTO | F10/UX |
| D-6 | Add-form validation failures lose input (SetError+redirect, no reload-with-model) | AmenitiesController add, ServicesController add, StaffController add | F1–F4 |
| D-7 | One delete-confirm `<div class="modal">` rendered **per table row** (DOM bloat) | Amenities/Index, Services/Index, Staff/Index | MOD3/F8 |
| D-8 | All controller flash strings hardcoded English (e.g. "Business registered. It is now pending review.", "Amenity added.", "Opening hours updated.") | all 6 controllers | CON1 |
| D-9 | All facade fallback error strings hardcoded English ("Could not load the business.", "This amenity already exists.", "You do not own this business.", …) | all 7 facades | CON1 |
| D-10 | WeatherWidget view hardcoded English: `aria-label="Local weather"`, "Feels like", "humidity", "wind" | `Views/Shared/Components/WeatherWidget/Default.cshtml` ~L4, L19–21 | CON1 |
| D-11 | `BusinessType` enum displayed raw/unlocalized | `MyBusinesses/Index.cshtml` ~L103, Manage disabled input | CON1 |
| D-12 | PlaceId `<select>` with up to 200 options, no typeahead | `MyBusinesses/Register.cshtml` `#placeSelect` | S2/F10-adjacent |
| D-13 | Amenity icon = free-text input with `fa-wifi` placeholder | `Amenities/Index.cshtml` | F10-adjacent/UX |
| D-14 | Hours: time inputs not disabled when "Closed" checked (no JS) | `Hours/Index.cshtml` | F-forms/UX |
| D-15 | No pagination UI on MyBusinesses/Index despite paged API | `MyBusinesses/Index.cshtml` | D1/R4 |
| D-16 | Sequential business+feature API calls in every sub-feature facade | 5 facades | API1 |
| D-17 | Weather widget SSR-inline blocks Manage first paint | `Manage.cshtml` ~L66 | dashboard plan §0.3 (non-critical widget → lazy) |
| D-18 | `StatusIcon()` local helper duplicated in Index + Manage | 2 views | DRY/CON2 |
| D-19 | 6× duplicated `SetSidebar(...)` private method across controllers | all controllers | DRY |

**Direction-fragile CSS audit (feeds Phase 4):** the area is in good shape — views already use `ms-*`/`me-*`, `text-start`, `<bdi dir="ltr">` for slugs/numbers/codes. Verified residual items:
- `fa-arrow-left` back icons (Manage, Services/Edit) — **already covered** by `rtl.css` ~L150 flip section ✅ (verify visually in AR).
- `_BusinessSidebar` `offcanvas-end` — **already covered** by `rtl.css` ~L232 re-anchoring ✅ (verify visually in AR).
- New work in this plan (offcanvas service editor, Choices.js pickers, toasts) must be checked in AR-RTL — see RTL gate in §1.7.
- Hours `type=time` inputs are `dir=ltr` ✅; keep `dir="ltr"` on any new numeric/code inputs (RTL3).

### 1.7 Per-phase verification protocol (merge gate for EVERY phase)

1. `dotnet build src/Hosts/YallaJo.Web/YallaJo.Web.csproj --no-incremental` — **baseline 71 warnings / 0 errors; zero NEW warnings**. Phases touching `[Backend]` also build `src/Hosts/YallaJo.Api`.
2. **EN-LTR AND AR-RTL walkthrough of every touched surface** (RTL4). Check: logical-property layout holds, directional icons flip (arrows/chevrons only — never stars/phones, RTL2), numbers/codes/phones stay LTR via `dir="ltr"`/`<bdi>` (RTL3), offcanvas anchors to the correct edge, dropdown menus align, toasts position correctly.
3. Light **and** dark (`data-bs-theme`, T1–T5); 390px and 1280px viewports; touch targets ≥ 44px (D2).
4. No raw resx keys rendered; every new key exists in BOTH `SharedResource.en.resx` and `.ar.resx` (flush-left entries appended before `</root>`; **grep before adding — duplicates cause MSB3568; never re-encode resx with PowerShell; UTF-8 only**).
5. No new inline `style=` except X6-justified dynamic values with a code comment; no inline `<script>` except non-executable JSON islands (SEC1/J3).
6. Every AJAX conversion keeps a working no-JS path (PE1) — verify with JS disabled.
7. No new dependencies/CDNs — vendored libs only (Choices.js is already vendored; verify presence under `wwwroot/assets/vendor/` before use, else fall back to a server-rendered `<datalist>`/select pattern).
8. One conventional commit per phase citing rule IDs.

### 1.8 Hard guardrails

- Touch ONLY: `src/Hosts/YallaJo.Web/Areas/Business/**`, `wwwroot/assets/js/business-*.js`, `wwwroot/assets/css/rtl.css` + `site.css` (additive utilities only), `Resources/SharedResource.{en,ar}.resx` (additive keys only), `Views/Shared/Components/WeatherWidget/Default.cshtml`, `Infrastructure/Mvc/AdminNoStoreCacheFilter.cs` (one-array-element change), and the **explicitly listed `[Backend]` additions** in §3 (B1 only: `src/Modules/Security/**` + `src/Hosts/YallaJo.Api` wiring if endpoints are mapped there).
- **Never touch other Web areas** (Public, Admin, Provider, Guide, …). Reuse `Areas/Public/Views/Shared/` partials by reference where the framework allows; if Business needs its own variants, create them in `Areas/Business/Views/Shared/`.
- Never reshape existing API endpoints or DTOs — additive only; never break existing consumers (the public business list/detail endpoints are consumed elsewhere).
- Mind the gitignore gotcha: `**/[Pp]ackages/*` is ignored — if any new file lands under a `Packages/` path, add a negation (precedent at `.gitignore` L205–215).
- New Razor partials must declare `@using Microsoft.Extensions.Localization` before `@inject IStringLocalizer<SharedResource> L` (no area `_ViewImports` guaranteed).
- Page scripts in `wwwroot/assets/js/{page}.js` loaded via `@section Scripts` with `defer` (A6); declarative init via `data-yj-component` (JS2), idempotent (JS4), `AbortController` on fetches (JS6), state in DOM (JS10).
- ApexCharts is allowed on dashboards (X14) but **not needed** in this plan — do not add it.

---

## §2 View Reduction Table

Current: **9 views**. Target: **9 → 7 views**. Auth-gated area ⇒ no SEO/sitemap impact for any change; all existing routes are preserved (returning the consolidated views) so bookmarks and sidebar links keep working — no 301s needed, but no dead routes either.

| View | Verdict | Mechanism | UX gain | SEO/route impact |
|---|---|---|---|---|
| `MyBusinesses/Index.cshtml` | **keep / thin** | Extract `_BusinessStatusBadge.cshtml` shared partial (kills duplicated `StatusIcon()` helper, D-18); add `_Pagination`-style pager (D-15); fix grid wrapper (D-3) | Consistent status rendering; owners with >50 businesses can navigate; sidebar layout actually works | None — route unchanged |
| `MyBusinesses/Register.cshtml` | **keep / thin** | Replace 200-option `#placeSelect` with searchable combobox (Choices.js, vendored) fed by new Web JSON proxy → existing API `GET /places/lookup` (D-12); keep `<select>` SSR fallback for no-JS (PE1) | Type-to-find a place instead of scrolling 200 options; coords prefill kept | None |
| `MyBusinesses/Manage.cshtml` | **keep / thin** | Use `_BusinessStatusBadge`; lazy-load weather via `loadPartial` (D-17); `data-loading` on forms | Faster first paint; less duplicated markup | None |
| `Hours/Index.cshtml` | **merge-into Settings** | New `MyBusinesses/Settings.cshtml` parameterized tab view (`tab=hours\|accessibility`); `HoursController.Index` returns `Settings` view with hours tab active — same route, same form, same POST | One coherent "operating settings" surface; both are whole-form PUT saves with identical interaction model; fewer page hops between related settings | Route `.../hours` preserved (renders Settings, hours tab active) |
| `Accessibility/Index.cshtml` | **merge-into Settings** | `AccessibilityController.Index` returns `Settings` view with accessibility tab active; tab content rendered as partials `_HoursTab.cshtml` + `_AccessibilityTab.cshtml`; non-active tab lazy-loads via `loadPartial` (dashboard plan §0.3), SSR-rendered when JS off (PE1) | Same as above; accordion-merge precedent: Public Help/Detail → Help/Index | Route `.../accessibility` preserved (renders Settings, accessibility tab active) |
| `Amenities/Index.cshtml` | **keep / thin** | Single shared delete confirm via `form-ux.js` `data-confirm` replaces N per-row modals (D-7); icon free-text → curated icon `<select>` (D-13); AJAX add/remove via `WantsAjax()` partial `_AmenitiesList.cshtml` | Less DOM, accessible confirm flow, no more guessing FA class names, no full-page reload per amenity | None |
| `Services/Index.cshtml` | **keep / absorbs Edit** | Extract `_ServiceForm.cshtml` (shared by add + edit); edit opens in offcanvas loaded via `loadPartial` from existing GET edit route (which returns the partial when `WantsAjax()`); no-JS fallback: GET edit route renders full `Index` with the form panel pre-filled server-side (`?edit={serviceId}` semantics via the existing route) | Edit a service without leaving the list; identical form for add/edit; precedent: Public Search/Index retirement | Route `.../services/{serviceId}/edit` preserved (AJAX → partial; no-JS → full Index page with edit panel) |
| `Services/Edit.cshtml` | **retire** | Superseded by `_ServiceForm` + offcanvas above; delete the view file | One less near-duplicate 107-line view to maintain | Route kept and functional (see above) — nothing 404s |
| `Staff/Index.cshtml` | **keep / thin** | F10 fix: raw UserId input → searchable user picker fed by `[Backend]` B1 lookup; staff rows enriched with real names/emails via B1 `ids=` batch (D-4, D-5); single shared `data-confirm` (D-7) | Owners add staff by name/email instead of pasting GUIDs; the table finally shows who the staff actually are | None |

**Rejected reductions (with reason):**
- Merging Amenities/Services/Staff into Manage tabs — rejected: these are CRUD list surfaces with their own add-forms, pagination and modals; merging would make Manage a 600-line mega-view (denser, not better) and degrade no-JS usability (PE1).
- Merging Hours+Accessibility directly into Manage — rejected: Manage already carries profile form + KPIs + rejection/resubmit flow + weather; settings deserve their own surface.
- Retiring Register into a Manage modal — rejected: first-run registration is the area's entry funnel for users with zero businesses; a full page with clear sections is better UX than a modal (MOD-scope) and required for no-JS.

---

## §3 `[Backend]` additions (full specs)

**B1 — User lookup endpoint (Security module).** The only backend addition. Powers (a) the F10 staff picker typeahead and (b) batch enrichment of staff rows with display names.

- **Route/verb:** `GET /api/v1/users/lookup`
- **Params:** `q` (string, optional, min length 2 when present), `ids` (csv of GUIDs, optional, max 50), `limit` (int, default 10, clamp 1–20 per R4 spirit). At least one of `q`/`ids` is **required** → otherwise 400 (prevents user enumeration).
- **Response:** `200 OK` → `IReadOnlyList<UserLookupDto>`: `{ Guid Id, string DisplayName, string? Email, string? AvatarUrl }`. Empty list when no match. `400` on missing/short query. No paging envelope (capped list).
- **Auth/permission:** `RequireAuthorization()` — any authenticated user. Mitigations against enumeration: min query length 2, hard cap 20 results, no role/permission data in the DTO. (Deliberately NOT `SecurityFeatures.User Read` — that is admin-grade and business owners don't hold it.)
- **Owner module & wiring:** `src/Modules/Security` —
  1. CQRS query `LookupUsersQuery(string? Query, IReadOnlyList<Guid>? Ids, int Limit)` + handler in `Security.Application` (case-insensitive prefix/contains match on display name + email; `ids` filter is exact).
  2. Endpoint mapped in `Security.Presentation/Endpoints/UserEndpoints.cs` (next to existing `GET /users`).
  3. Web: new method on `Areas/Business/ApiClients/StaffApiClient.cs` → `LookupUsersAsync(string? q, IEnumerable<Guid>? ids, CancellationToken ct)` calling `/api/v1/users/lookup`.
  4. Facade: `BusinessStaffFacade.GetAsync` batch-enriches staff rows (`ids=` call, API7 — one batch call, not N) merged into a new `StaffMemberVm.DisplayName/Email`; `BusinessStaffFacade.LookupAsync(q)` for the typeahead.
  5. Controller: new `StaffController.Lookup` GET action `business/businesses/{id:guid}/staff/lookup?q=` returning `Json(...)` (browser never calls the API host — JS5); `[RequirePermission(WebPermission.BusinessStaff.Create)]`, NoStore.
  6. VM: extend `StaffMemberVm` additively.
- **Additive only:** existing `GET /users` and `BusinessStaffDto` untouched; no existing consumer breaks.

**Explicitly NOT proposed (capability already exists — wire-up only, not `[Backend]`):**
- Place typeahead → existing `GET /places/lookup` (PlaceEndpoints.cs ~L56); needs only a Web ApiClient method + JSON proxy action on `MyBusinessesController`.
- Accessibility catalog → existing `GET /accessibility/catalog`.
- Weather → existing `GET /seo/weather/{placeId}`.

---

## §4 Phases

Ordering: bugs/dead code → structure/AJAX/view-reduction (+ `[Backend]` lands with its consumer) → conversion UX → l10n/RTL → a11y → perf.

---

### Phase 0 — Bug fixes, cache correctness & quick hygiene (≈ 0.5 day)

**Files:** `Infrastructure/Mvc/AdminNoStoreCacheFilter.cs` (~L11), `Areas/Business/Shared/_BusinessSidebar.cshtml` (~L30), `Areas/Business/Views/MyBusinesses/{Index,Register,Manage}.cshtml`, `wwwroot/assets/css/site.css` (only if `.avatar-48` utility is missing — verify; the Public plan created `.avatar-*` utilities).

**Checklist:**
- [ ] Add `"Business"` to `AdminNoStoreCacheFilter.NoStoreAreas` (D-1, **C2**) — one array element; verify `Cache-Control: no-store` on `/business/businesses` afterwards.
- [ ] Replace inline `style="width:48px;height:48px;"` on the sidebar avatar with the `.avatar-48` utility (D-2, **X6**).
- [ ] Fix the grid wrapper on the 3 MyBusinesses views so `_BusinessSidebar` (`col-lg-4 col-xl-3`) and page content (`col-lg-8 col-xl-9`) sit inside one `.row.g-4`, matching the sub-feature views (D-3). Pure markup re-nesting; no visual redesign.
- [ ] Localize the `BusinessType` display (D-11): map enum → `Business.Type.{Value}` resx keys (grep first; add missing keys to BOTH resx, **CON1**).

**Acceptance criteria:**
- Build: 0 errors, zero new warnings.
- `/business/*` responses carry `no-store`.
- No inline `style=` remains in `_BusinessSidebar`.
- All 3 MyBusinesses pages show the sidebar correctly at 1280px and as offcanvas at 390px.
- **RTL/LTR:** AR-RTL walkthrough of the 3 re-gridded pages — sidebar offcanvas anchors to the start edge (rtl.css ~L232 rule applies), no horizontal overflow.

**Commit:** `fix(business): enforce NoStore (C2), remove inline avatar style (X6), repair sidebar grid (D1) and localize BusinessType (CON1)`

---

### Phase 1 — Structure: view reduction, shared partials, controller dedup (≈ 2 days)

**Files:** `Areas/Business/Views/MyBusinesses/Settings.cshtml` (new), `Areas/Business/Views/Shared/{_BusinessStatusBadge,_HoursTab,_AccessibilityTab,_ServiceForm,_DeleteConfirmForm}.cshtml` (new), `Views/Hours/Index.cshtml` (delete after merge), `Views/Accessibility/Index.cshtml` (delete after merge), `Views/Services/Edit.cshtml` (delete), `Views/Services/Index.cshtml`, `Views/MyBusinesses/{Index,Manage}.cshtml`, all 6 controllers, `Areas/Business/Controllers/BusinessControllerBase.cs` (new, optional but preferred).

**Checklist:**
- [ ] Extract `_BusinessStatusBadge.cshtml` (status → color via `MyBusinessesMapper.StatusColor` + icon switch); replace duplicated `StatusIcon()` in Index ~L? and Manage (D-18). New partial gets `@using Microsoft.Extensions.Localization` + `@inject`.
- [ ] Create `BusinessControllerBase : BaseController` (or a shared extension) hosting the 6× duplicated `SetSidebar(...)` (D-19); migrate all controllers. Behavior-identical; attribute routes unchanged.
- [ ] Build `MyBusinesses/Settings.cshtml`: tabbed surface (`nav-tabs`, `role="tablist"`) hosting `_HoursTab` + `_AccessibilityTab`; `HoursController.Index` and `AccessibilityController.Index` both return `View("~/Areas/Business/Views/MyBusinesses/Settings.cshtml", vm)` with the matching active tab. POST actions unchanged. Non-active tab: SSR both tabs when JS is off (PE1); with JS, lazy-load the inactive tab via `loadPartial` on first activation (dashboard plan §0.3) — controllers return the tab partial when `WantsAjax()`.
- [ ] Extract `_ServiceForm.cshtml` (fields: Name, Price step .001, Currency, Duration, MaxCapacity, Category select, Description, SortOrder; parameterized for add vs edit via hidden ServiceId). Use it inside Services/Index add panel AND as the edit surface.
- [ ] Services edit → offcanvas: GET `.../services/{serviceId}/edit` returns `PartialView("_ServiceForm", vm)` when `WantsAjax()`, else full `Services/Index` view with the edit panel pre-filled (no-JS path, PE1). Delete `Services/Edit.cshtml`. Pair GET with `[OutputCache(..., VaryByHeaderNames = new[] { "X-Requested-With" })]` **only if** the action is cacheable — it is auth-gated and NoStore (C2), so skip OutputCache here; just branch on `WantsAjax()`.
- [ ] Replace per-row delete modals in Amenities/Services/Staff with ONE pattern: a `_DeleteConfirmForm.cshtml` partial rendering a plain POST form with `data-confirm="@L["Business.ConfirmDelete"]"` — `form-ux.js` shows the shared confirm modal (safe action autofocus, static backdrop — F8/MOD3/MOD5); no-JS path = native form submit (PE1) (D-7).
- [ ] Verify view count lands at **7**: MyBusinesses/{Index,Register,Manage,Settings}, Amenities/Index, Services/Index, Staff/Index.

**Acceptance criteria:**
- Build clean; all 22 routes still respond (manually hit each GET; POST flows via UI).
- `/business/businesses/{id}/hours` and `/.../accessibility` render the Settings view with the correct active tab; both save flows work with JS disabled.
- `/.../services/{id}/edit` works in three modes: AJAX offcanvas, no-JS full page, direct URL hit.
- Exactly one confirm-modal mechanism in the DOM per page (not per row).
- **RTL/LTR:** AR-RTL pass on Settings tabs (tab order follows reading direction), service edit offcanvas anchors to the start edge per rtl.css, `_ServiceForm` numeric inputs keep `dir="ltr"` (RTL3); EN-LTR unchanged. Directional chevrons flip; no non-directional icon flips (RTL2).
- One `h1` per page (A11Y6) maintained on merged surfaces.

**Commit:** `refactor(business): consolidate views 9→7 — Settings tabs, _ServiceForm offcanvas, shared status badge & confirm (PE1, MOD3, F8, RTL1-RTL4, A11Y6)`

---### Phase 2 — AJAX modernization of CRUD lists (≈ 2 days)

**Files:** `AmenitiesController.cs`, `ServicesController.cs`, `StaffController.cs`, `Views/Amenities/Index.cshtml` + new `_AmenitiesList.cshtml`, `Views/Services/Index.cshtml` + new `_ServicesList.cshtml`, `Views/Staff/Index.cshtml` + new `_StaffList.cshtml`, `wwwroot/assets/js/business-crud.js` (new), facades (return-updated-list helpers as needed).

**Checklist:**
- [ ] Extract the table body of each CRUD page into a list partial (`_AmenitiesList` / `_ServicesList` / `_StaffList`) including its empty state (reuse `_EmptyState` pattern from Public shared partials or area-local variant; L6 — empty state with CTA).
- [ ] Controllers: on POST add/remove/edit success, branch `WantsAjax()` → return the refreshed list `PartialView` (inline-mutation-returns-updated-rows pattern, dashboard plan §0.3); else keep PRG (PE1). On validation failure + AJAX → `400` with `{ errors }` JSON consumed by the page script; on validation failure + no-JS → **re-render the page with the model** instead of SetError+redirect, fixing the lost-input defect (D-6, F1–F4).
- [ ] `business-crud.js` (new, `data-yj-component="business-crud"`, JS2/JS4): intercept add/remove form submits, send via `window.YallaJo.api.postForm` (antiforgery handled — SEC7, JS5), swap the list partial, fire `window.YallaJo.toast` success (NF1/NF4/NF5), show field errors inline (A11Y4: `role="alert"`, `aria-describedby`). `AbortController` per in-flight op (JS6). Wire `data-loading` so submit buttons disable+spinner (L2/F7) via existing `form-ux.js`.
- [ ] Hours UX: small enhancement in `business-crud.js` (or a dedicated `business-hours.js`) — when a day's `IsClosed` checkbox is checked, disable that row's time inputs (D-14); progressive enhancement only, server still validates.
- [ ] Skeleton/loading states on list swaps (S1/L1/L4/L5): minimal `.placeholder-glow` rows during swap.

**Acceptance criteria:**
- Add/remove amenity, service, staff member completes **without full page reload** when JS is on; toast appears bottom-right, max 3, 5s (NF1).
- With JS disabled: every flow still works via PRG, and invalid add-forms now **preserve input** (D-6 fixed).
- Validation errors render inline with `role="alert"` + `aria-describedby` (A11Y4).
- No fetch goes anywhere but same-origin Web routes (JS5); antiforgery header present on every POST (SEC7).
- **RTL/LTR:** AR-RTL pass — toasts anchored correctly, swapped partials keep logical-property layout, prices/durations wrapped LTR (`<bdi>`/`dir="ltr"`, RTL3); EN-LTR pass.
- Build clean; zero new warnings.

**Commit:** `feat(business): AJAX CRUD with WantsAjax partials, toasts and preserved-input validation (PE1, JS2-JS6, NF1, SEC7, A11Y4, L2, RTL3-RTL4)`

---

### Phase 3 — Pickers & staff identity `[Backend]` (≈ 2 days)

**Files:** `[Backend]` `src/Modules/Security/Security.Application/**` (new query+handler), `src/Modules/Security/Security.Presentation/Endpoints/UserEndpoints.cs`; Web: `Areas/Business/ApiClients/{StaffApiClient,MyBusinessesApiClient}.cs`, `Facades/{BusinessStaffFacade,MyBusinessesFacade}.cs`, `Controllers/{StaffController,MyBusinessesController}.cs`, `Models/Staff/*` (VM additive), `Views/Staff/Index.cshtml`, `Views/MyBusinesses/Register.cshtml`, `Views/Amenities/Index.cshtml`, `wwwroot/assets/js/{business-staff.js,business-register.js}`.

**Checklist:**
- [ ] **[Backend B1]** Implement `GET /api/v1/users/lookup` exactly per §3 spec: `LookupUsersQuery` + handler (Security.Application), endpoint in `UserEndpoints.cs`, `RequireAuthorization()`, min-q=2, cap 20, `400` when neither `q` nor `ids` given. Build `YallaJo.Api` clean.
- [ ] Web wiring for B1: `StaffApiClient.LookupUsersAsync` → `BusinessStaffFacade.LookupAsync` + batch enrichment of staff rows in `GetAsync` via one `ids=` call (API7) → `StaffMemberVm.DisplayName/Email` → `_StaffList` shows real names/emails instead of "Staff member N" (D-5). Graceful fallback to UserId `<bdi dir="ltr">` if lookup fails.
- [ ] `StaffController.Lookup` JSON proxy action (`.../staff/lookup?q=`), `[RequirePermission(WebPermission.BusinessStaff.Create)]`; debounce 300ms client-side (J4).
- [ ] Staff picker (F10 fix, D-4): replace raw UserId input with a searchable combobox (`business-staff.js`, Choices.js if vendored — else accessible custom listbox per MOD/A11Y patterns) querying the proxy; selection writes the hidden `Form.UserId`. No-JS fallback: keep a labeled UserId input rendered inside a `<noscript>`-gated block (PE1).
- [ ] Place picker (D-12): add `MyBusinessesApiClient.LookupPlacesAsync` → existing API `GET /places/lookup`; `MyBusinessesController.PlacesLookup` JSON proxy (`business/businesses/places/lookup?q=`); upgrade `#placeSelect` in Register to typeahead; extend `business-register.js` so a typeahead selection still prefills lat/lng (preserve the existing JSON-island behavior for no-JS, PE1). No backend change — endpoint exists.
- [ ] Amenity icon picker (D-13): replace free-text icon input with a curated `<select>` of ~20 amenity-appropriate icon options (label localized, value = icon class) defined in the Web mapper. No backend.

**Acceptance criteria:**
- `GET /api/v1/users/lookup?q=ab` returns ≤20 matches for an authenticated user; `?ids=` batch returns exact users; bare call → 400. Existing `GET /users` untouched.
- Staff page shows real display names/emails; adding staff is type-name-and-pick — no GUID pasting (F10).
- Register place field is searchable; coords prefill still works; both pickers work with keyboard only (A11Y) and degrade without JS (PE1).
- Both builds (Web + Api) clean, zero new warnings.
- **RTL/LTR:** AR-RTL — combobox dropdowns align to the start edge, emails/GUIDs render inside `<bdi dir="ltr">` (RTL3), Choices.js verified visually in AR (vendored-widget check); EN-LTR pass.

**Commit:** `feat(business): user lookup endpoint + staff/place searchable pickers, staff identity enrichment (F10, API7, J4, JS5, PE1, RTL3) [Backend]`

---

### Phase 4 — Localization & RTL closure (≈ 1 day)

**Files:** all 6 controllers, all 7 facades, `Views/Shared/Components/WeatherWidget/Default.cshtml`, `Resources/SharedResource.{en,ar}.resx`, `wwwroot/assets/css/rtl.css` (only if Phase 1–3 visual checks surfaced gaps).

**Checklist:**
- [ ] Controllers (D-8): move every flash string to resx — inject `IStringLocalizer<SharedResource>`; keys `Business.Flash.*` (e.g. `Business.Flash.Registered`, `Business.Flash.AmenityAdded`, `Business.Flash.HoursUpdated`, `Business.Flash.StaffAdded`, `Business.Flash.AccessibilityUpdated`, `Business.Flash.ServiceUpdated`, validation prompts like `Business.Flash.InvalidStaffInput`). **Grep both resx for each key before adding** (MSB3568); append flush-left before `</root>`; UTF-8; never PowerShell re-encode. (~25 strings.)
- [ ] Facades (D-9): same treatment for fallback error strings (`Business.Error.LoadBusiness`, `Business.Error.NotOwner`, `Business.Error.AmenityExists`, …). (~20 strings.)
- [ ] WeatherWidget (D-10): localize `aria-label`, "Feels like", "humidity", "wind" → `Business.Weather.*` keys; temperatures/wind already wrapped `dir="ltr"` ✅ — keep.
- [ ] Consume the §1.6 direction-fragile audit: confirm in AR that `fa-arrow-left` back links flip (rtl.css ~L150) and sidebar offcanvas re-anchors (rtl.css ~L232); fix any residue found during Phases 1–3 **in `rtl.css` only** (never inline, never per-page `<style>`) using logical properties first (RTL1).
- [ ] Full-area AR walkthrough with an Arabic UI string sweep: zero English leaks in flashes, toasts, errors, empty states.

**Acceptance criteria:**
- `grep -rn "SetSuccess(\"\|SetError(\"" Areas/Business/Controllers` → no hardcoded literals (only resx-backed calls).
- Same for facade literals.
- Both resx compile; build clean (MSB3568 would fail it).
- **RTL/LTR:** full AR-RTL + EN-LTR walkthrough of all 7 views + weather widget + every toast/flash path (RTL4 merge gate). Numbers/codes/phones stay LTR (RTL3); only directional icons flip (RTL2).

**Commit:** `feat(business): localize controller flash + facade errors + weather widget; close RTL audit (CON1, RTL1-RTL4)`

---

### Phase 5 — Accessibility hardening (≈ 1 day)

**Files:** all 7 views + new partials, `wwwroot/assets/js/business-crud.js`, `business-staff.js`.

**Checklist:**
- [ ] One `h1` per page including merged Settings view (A11Y6); heading hierarchy descends without gaps.
- [ ] Settings tabs: full ARIA tab pattern (`role=tablist/tab/tabpanel`, `aria-selected`, arrow-key navigation — Bootstrap's tab JS provides most; verify).
- [ ] Service-edit offcanvas: focus moves to first field on open, returns to trigger on close (MOD-pattern focus management); `aria-labelledby` on the offcanvas.
- [ ] AJAX list swaps: wrap lists in `aria-live="polite"` region or announce via the toast's live region so screen readers hear add/remove results.
- [ ] All form fields: `<label>` + `asp-validation-for` with `role="alert"` + `aria-describedby` (A11Y4) — verified across `_ServiceForm`, staff picker, amenity add.
- [ ] Status badges convey state by color + icon + text (A11Y5) — `_BusinessStatusBadge` must include all three.
- [ ] Touch targets ≥ 44px on all row action buttons (D2); keyboard-only pass of every flow incl. comboboxes.
- [ ] `prefers-reduced-motion` respected by any new transition (M-rules).

**Acceptance criteria:**
- Keyboard-only completion of: register, edit profile, save hours, save accessibility, add/remove amenity/service/staff, edit service via offcanvas.
- No axe-core critical violations on the 7 views (manual run acceptable).
- Build clean. **RTL/LTR:** focus order remains logical in AR (tab order follows DOM, unaffected — verify visually); EN-LTR pass.

**Commit:** `feat(business): a11y hardening — tabs ARIA, offcanvas focus, live regions, A11Y4/A11Y5/A11Y6/D2`

---

### Phase 6 — Performance & polish (≈ 1 day)

**Files:** 5 sub-feature facades, `MyBusinessesFacade.cs`, `Views/MyBusinesses/{Index,Manage}.cshtml`, `MyBusinessesController.cs`, `WeatherWidgetViewComponent` consumers, new `_BusinessesList.cshtml` (optional pager partial).

**Checklist:**
- [ ] **API1 (D-16):** in each sub-feature facade GET, run the business fetch + feature fetch concurrently via `Task.WhenAll` (both already independent; preserve GuardSignOut/Normalize semantics — evaluate both results after await).
- [ ] **Weather lazy-load (D-17):** Manage stops SSR-invoking the ViewComponent; instead renders a placeholder `div[data-yj-component="weather-widget"][data-url]` filled via `loadPartial` from a new lightweight `MyBusinessesController.Weather` GET action (`.../{id}/weather`) returning the ViewComponent/partial; no-JS fallback: `<noscript>` keeps SSR invoke (PE1). First paint of Manage no longer waits on the weather upstream.
- [ ] **Pagination (D-15):** surface pager on MyBusinesses/Index honoring the D1 paging shape (Items/PageNumber/PageSize/TotalCount/HasPrev/HasNext — no TotalPages); clamp page 1–50 (R4); pushState optional — simple PRG links are acceptable here (low-volume page).
- [ ] Verify all page scripts load `defer` via `@section Scripts` (A6); images in lists use `loading="lazy"` (A7/I-rules) where below the fold.
- [ ] Final sweep: no dead actions, no broken links, no placeholder/fake data anywhere in the area.

**Acceptance criteria:**
- Each sub-feature GET issues its 2 API calls concurrently (verify via logs/timing).
- Manage renders without blocking on weather; widget appears async; works with JS off via noscript.
- MyBusinesses/Index pages correctly past 50 businesses (seed/test data).
- Build clean. **RTL/LTR:** pager direction-correct in AR (prev/next chevrons flip — RTL2), weather numbers stay LTR (RTL3); EN-LTR pass.

**Commit:** `perf(business): parallel facade fetches, lazy weather widget, list pagination (API1, A6, A7, D1, R4, PE1, RTL2-RTL3)`

---

## §5 Execution order & sizing

| Order | Phase | Size | Depends on | Parallelizable with |
|---|---|---|---|---|
| 1 | P0 Bugs/cache/hygiene | 0.5 d | — | — (do first; tiny) |
| 2 | P1 Structure & view reduction | 2 d | P0 | — (foundation for P2/P3) |
| 3 | P2 AJAX CRUD | 2 d | P1 (partials exist) | P3 backend half |
| 4 | P3 Pickers + `[Backend]` B1 | 2 d | P1; B1 endpoint can be built **in parallel with P2** (different repo areas) | B1 ∥ P2 |
| 5 | P4 l10n/RTL closure | 1 d | P1–P3 (strings stabilized) | P5 |
| 6 | P5 A11y | 1 d | P1–P3 | P4 |
| 7 | P6 Perf & polish | 1 d | P2 (loadPartial in place) | — |

**Total: ≈ 9.5 days** sequential; **≈ 7.5–8 days** with B1∥P2 and P4∥P5 parallelization (two agents).

**Hard rules recap (apply to every phase):** touch only the files listed per phase within the §1.8 guardrail set; zero new build warnings against the 71-warning baseline; EN-LTR + AR-RTL + light/dark + 390/1280px walkthrough before merge; every AJAX path keeps a no-JS fallback (PE1); all copy via resx in BOTH languages (CON1); all RTL overrides in `rtl.css` only (RTL1–RTL4); no new dependencies or CDNs; never touch other Web areas; `[Backend]` changes limited to the B1 spec.
