# Admin Area Master Plan — Modernization, View Reduction & AJAX

> **Audience:** an autonomous coding agent with zero conversation context.
> **Produced from:** a code-verified audit of `src/Hosts/YallaJo.Web/Areas/Admin` (controllers, facades, ApiClients, views, layout, API chains) performed against the shipped code on `master`.
> **Binding contracts:** `yallajo-plan/UI-UX-Design.md` (platform rules — cite rule IDs) **and** `docs/admin-dashboard-ui-ux.md` (admin design contract — four states, modal CRUD §4.7, `.font-data`, canonical badges, banned patterns, permission-driven rendering). Both must be honored in every phase.
> **Scope truth:** `yallajo-plan/7-admin-dashboard.md` (14 logical pages, 43 controllers). `docs/admin-dashboard-endpoints.md` (API contract per section).
> All line numbers are **indicative** (~L123) — re-locate before editing.
> Three parts: **I. Foundations & hygiene** (P0–P3), **II. Structure, view reduction & AJAX** (P4–P8), **III. Experience polish** (P9–P12).

---

## §1 Context & Ground Rules

### 1.1 Project layout

- Area root: `src/Hosts/YallaJo.Web/Areas/Admin/{Controllers, Facades, ApiClients, Models, Views}`.
- Layout: `Areas/Admin/Views/Shared/_AdminLayout.cshtml` (288L) — **not** the public `_Layout`. Verified facts:
  - Google Fonts **already removed** (~L94-98 is a comment documenting removal per V3) — no font migration needed.
  - Inline pre-paint theme script ~L26-82 (justified, CSP-safe — leave).
  - Conditional `rtl.css` ~L114-117; EN/AR language toggle (`language-toggle.js` ~L84); `@Html.AntiForgeryToken()` ~L151/~L281.
  - Scripts ~L316-328: `bootstrap.bundle.min.js`, `overlayscrollbars.min.js`, **`apexcharts.min.js` + `flatpickr.min.js` loaded globally on every admin page** (used by only ~6 views — perf debt, see P12), `functions.js` (theme).
  - **`api-client.js` and `form-ux.js` are NOT loaded by `_AdminLayout`** — see P1 (foundation gap).
- Sidebar: `Areas/Admin/Views/Shared/_AdminSidebar.cshtml` (315L) — 8 sections / 40+ permission-gated entries; offcanvas < xl.
- Shared partials today: `Shared/Partials/_ConfirmModal.cshtml` (81L), `Users/Partials/{_SuspendModal,_ReactivateModal,_ArchiveModal,_ReassignModal,_AdminResetPasswordModal,_LifecycleBadge}`, `AuditLogs/Partials/_MetadataDetails`, `Places/{_Form,_Table}`, `Roles/_CreateRoleForm`.
- Page scripts: `wwwroot/assets/js/admin-dashboard.js`, `admin-statistics.js`, `admin-users.js` (28 views declare `@section Scripts`).

### 1.2 Conventions (cite rule IDs in every commit)

- Four-tier pipeline Controller → Facade → ApiClient → `IApiClient`; `ApiResult` values not exceptions; `BaseController` helpers `GuardSignOut` (`if (GuardSignOut(result) is { } signOut) return signOut;`), `SetSuccess`/`SetError`/`SetFlash`, `ApplyValidationErrors` + PRG.
- **C2:** auth area = NoStore. There is **zero `[OutputCache]`** in Admin today — keep it that way; AJAX conversions must NOT add output caching.
- **PE1:** every AJAX conversion keeps the no-JS path (native form POST + PRG; SSR first paint). The admin shell already requires Bootstrap JS (offcanvas sidebar), but forms must remain native `<form>` POSTs.
- **JS5/JS1/JS2/JS4:** all AJAX via `window.YallaJo.api` (browser never calls the API host); vanilla JS; declarative `data-yj-component` init; idempotent; `var` declarations at function root (repo ESLint).
- **SEC7:** antiforgery on every POST (already true for all ~136 POST actions) incl. AJAX header (api-client.js adds `RequestVerificationToken` automatically).
- **CON1:** all user-facing copy in **both** `Resources/SharedResource.en.resx` (2-space-indented entries) and `.ar.resx` (flush-left entries); append before `</root>`; **grep before adding any key** (duplicates ⇒ MSB3568); UTF-8 only, never re-encode with PowerShell.
- **RTL1–RTL4:** logical properties / `ms-*`,`me-*`,`ps-*`,`pe-*`,`text-start/end` only; `[dir="rtl"]` overrides live in `rtl.css`; flip directional icons only; numbers/ids/codes/timestamps in `dir="ltr"` or `<bdi>` (pair with `.font-data` per admin contract §1); **EN-LTR + AR-RTL visual check is a merge gate for every phase that touches markup/CSS**.
- **X14:** ApexCharts is **allowed** on admin dashboards (vendored, already in use on 6 views).
- **R6:** >500-row tables get server-side paging/filtering — satisfied here by the AJAX listing pattern (P5/P6), not DataTables.
- **Admin design contract** (docs/admin-dashboard-ui-ux.md): permission-driven rendering (absent, not disabled); read-before-write; **four states** per data view (loaded / skeleton / composed empty with CTA / inline retryable error — §4.9); KPI tile variants §4.1; canonical status badge §4.2 (`badge bg-{color} bg-opacity-10 text-{color}`, never color-only); data-table pattern §4.4; **modal CRUD is the standard for simple create/edit** §4.7 (full pages reserved for complex records); typed confirmation for irreversible actions §4.6; banned patterns §1 (no hero-metric walls, no 3-identical-card rows, no gradient text, no em dashes).
- Backend additions: **additive only**, never reshape existing contracts; minimal-API endpoint + CQRS handler + `MustHavePermissionAttribute`; `Result<T>` + `.ToApiResult()`; `OperationCanceledException` guard in handlers; register literal routes safely relative to `/{id:guid}` catch-alls (constraint prevents collision, but place literal segments first and add a comment).

### 1.3 Controllers (43) by domain

| Domain | Controllers |
|---|---|
| Overview | Home (`/admin`, `/admin/dashboard`), Statistics (`/admin/analytics`) |
| People & access | Users, Lifecycle (redirect-only), Roles, Invitations, AuditLogs |
| Marketplace ops | Providers, Businesses, Guides, GuideApplications, Tours, Trips, Places |
| Blogs & creators | Blogs, BlogTranslations, Creators |
| Finance & commerce | Payments, Payouts, Commissions, Disputes, Bookings |
| Trust & moderation | Moderation, Reports, FlaggedReviews, Support |
| Growth & recs | Growth, Recommendations |
| Content ops (taxonomy) | Categories, Tags, Languages, Specializations, Translations, Attachments, EntityCategories, EntityTags |
| SEO console | SeoMetadata, SeoFaq, SeoRedirects, SeoSitemap, SeoWeather |
| Platform ops | Outbox, NotificationTemplates |

Authorization: `[Authorize]` + `[RequirePermission(WebPermission.{Feature}.{Action})]` class-level read + strictest per-action write. No role-name checks in Web. Role visibility: Owner = all; SuperAdmin = all − System.Update; Admin = all − System.Update − User.DeleteAny − Outbox.*.

### 1.4 Verified inventory (code-verified on master)

| Metric | Value |
|---|---|
| Controllers / actions | **43 / 231** (~136 POST, all `[ValidateAntiForgeryToken]`) |
| Facades / ApiClients | **44 / 44** (~200 ApiClient methods → 150+ distinct endpoints, **0 dead calls**) |
| Views (.cshtml) | **77 total = 62 full views + 15 partials** (12,172 lines) |
| `WantsAjax` / `[OutputCache]` / `listing.js` refs | **0 / 0 / 0** — the whole area is SSR + full-page reload |
| `data-loading` | **0 occurrences** (no submit spinners anywhere — L2/F7 gap) |
| `data-confirm` | **171 occurrences in 35 files** — BUT `_AdminLayout` loads neither `api-client.js` nor `form-ux.js`; wiring must be verified/fixed in P1 (attributes may be inert unless loaded per-page) |
| Inline `style=` / external CDNs / jQuery / `onclick=` | **0 / 0 / 0 / 0** |
| Localization | Views fully localized (`IStringLocalizer` throughout); only gap: `Users/Partials/_ArchiveModal.cshtml` ~L36 `placeholder="ARCHIVE"` typed-confirm token. **Controllers: 87 hardcoded English flash strings in 35 controllers; 0 controllers inject `IStringLocalizer`** — the dominant CON1 gap (P10) |
| RTL | No physical left/right/float anywhere; `<bdi>`/`dir="ltr"` widely used; ~15 culture-formatted numbers lack `<bdi>` (e.g. `Places/_Table.cshtml` ~L61) |
| Modals | **48 total**: ~8 simple confirms + ~40 bespoke rich-form/action modals (Users/Details 5, Businesses 4, Creators 4, Recommendations 4, Blogs/Edit 3, Commissions 3, Guides 3, …) |
| Duplicated markup | empty-state ×~25 views, pagination footer ×~12, modal header ×~23, filter card ×~8, status badge ×~15 (`_LifecycleBadge` exists for users only) |
| Pagination | offset-based full-reload in most lists; **Bookings is cursor-based (`NextCursor`)**; Places has page-size selector; Users has a client-side JS filter (`data-yj-users-filter`) |
| Facade parallelism | only **12 / 44** facades use `Task.WhenAll` (API1 audit target — P12) |
| Charts | ApexCharts in ~6 views (Home, Statistics, Growth, Finance…), vendored; **no skeletons, no aria-live** |
| Raw entity-ID text inputs (**F10 violations**) | `Moderation/Index` ~L121/L131/L157/L167/L197 (warn/ban/unban `UserId`+`EntityId`); `Recommendations/Index` ~L146/L160/L207/L254/L280 (`ProviderId`, `EntityId`, `boostId`, `pinId`); `Growth/Index` ~L332 (`entityId`); `SeoFaq/Index` ~L197 (`EntityId`); `SeoMetadata/Index` ~L40 (`EntityId`); plus Trips lookup-by-id and Businesses lookup-by-place-id flows |
| ST1 RowVersion carriers | Trips (×4), FlaggedReviews (approve/remove), Blogs (restore), NotificationTemplates (delete), Support (close/resolve) — preserve round-trips in any view rework |
| Dashboard data | REAL (`/api/v1/admin/dashboard[/revenue|/bookings|/users]`) — no stubs |

Unconsumed API capabilities exist (~52: admin booking overrides confirm/cancel/reject/complete, tours proposals/packages extras, accessibility/hours/amenities, translations bulk ops, analytics metrics/segments…) — this plan only adopts the ones a phase explicitly needs; do not gold-plate.

### 1.5 Per-phase verification protocol

1. `dotnet build src/Hosts/YallaJo.Web/YallaJo.Web.csproj --no-incremental` → **71 warnings / 0 errors** baseline; **zero NEW warnings** is a hard gate. `[Backend]` phases also `dotnet build src/Hosts/YallaJo.Api/YallaJo.Api.csproj --no-incremental` → **88 warnings / 0 errors**.
2. Walk every touched surface **EN-LTR and AR-RTL**, light **and** dark (`#bd-theme`), at 375px and 1280px (admin contract checks 375/768/1024/1440 — use 375 + 1280 minimum).
3. No raw resx keys rendered; no new inline `style=` (X6 exception requires a code comment); no-JS PRG path intact for every converted form; permission-gated rendering unchanged (absent, not disabled).
4. resx: grep-first; en 2-space-indented, ar flush-left; both files end (as of writing) with `Creator.Status.*` keys before `</root>` — append after them.
5. Pre-existing OUT-OF-SCOPE blocker: `tests/Web.Tests.Unit` fails to compile (`AdminBookingsResolveDisputeTests.cs` ~L57 ctor drift; `SearchFacadeTests.cs` ~L55+) — do **not** fix unless a phase touches those exact facades, in which case repair the affected test file only.
6. One conventional commit per phase citing rule IDs; never stage `src/Hosts/YallaJo.Api/Program.cs` if it carries unrelated local modifications (stage explicit file lists).

### 1.6 Hard guardrails

- Touch only `Areas/Admin/**`, `wwwroot/assets/js/admin-*.js` (+ new admin JS files), `wwwroot/assets/css/site.css`/`rtl.css` (additive utilities), `Resources/SharedResource.{en,ar}.resx`, and the **explicitly listed `[Backend]` additions** in `src/Modules/*`/`YallaJo.Api`. Never other Web areas; never the public `_Layout.cshtml`; `_AdminLayout.cshtml` edits limited to what phases list.
- Browser never calls the API host directly (JS5) — new JSON needs MVC proxy actions.
- Never invent backend capabilities: verify chain or tag `[Backend]` with full spec.
- Mind `.gitignore` `**/[Pp]ackages/*` (negation precedent at `.gitignore` ~L205-215).

---

## §2 View Reduction Table

62 full views audited. Reduction target: **62 → 55** (−7) plus markup-mass reduction via shared partials. Mechanism for "merge-into-Index (modal CRUD)": per admin contract §4.7 — list page hosts a create/edit modal (same modal, mode by id); edit prefill from row `data-*` attributes (lists already render every editable field); deep links `GET /admin/{kind}/{id}/edit` keep working by returning Index with the modal **server-rendered open** (`modal fade show d-block` + backdrop div) so the flow works pre-JS (PE1); POST actions unchanged.

| View (lines) | Verdict | Mechanism | UX gain | Route impact |
|---|---|---|---|---|
| Categories/Edit (77L) | **retire** | modal CRUD on Categories/Index | edit without context loss; matches §4.7 | GET edit → Index w/ open modal |
| Tags/Edit (49L) | **retire** | modal CRUD on Tags/Index | same | same |
| Languages/Edit (53L) | **retire** | modal CRUD on Languages/Index | same | same |
| Specializations/Edit (55L) | **retire** | modal CRUD on Specializations/Index | same | same |
| Translations/Edit (52L) | **retire** | modal CRUD on Translations/Index (queue keeps per-item edit + batch approve per contract §5.12) | review-in-place | same |
| Invitations/Resend (49L) | **retire** | `data-confirm` form on Invitations/Index row | one-click resend with confirm | GET resend → 301 to Index |
| Blogs/Create (88L) | **retire** | "New draft" modal on Blogs/Index (title + language) → POST creates draft → redirect to Blogs/Edit | removes a one-field stepping-stone page | GET create → Index w/ open modal |
| Blogs/Edit (426L) | keep | full editor page (contract §4.7 reserves full pages for complex records) | — | — |
| NotificationTemplates/Edit (109L) | keep | template body editor = full page w/ live preview (contract §5.14); metadata stays in list modal | — | — |
| BlogTranslations/Edit (75L) | keep | long-form translation content justifies a page | — | — |
| Places/Create (30L) + Places/Edit (35L) | **thin/keep** | already thin compositions over `Places/_Form` (90L); complex form justifies pages | — | — |
| Translations/OnDemand (75L) | keep | distinct workflow surface | — | — |
| Users/Details, Providers/Details, Roles/Details, Tours/Details, Places/Details, Bookings/Details, Support/Details | keep | detail pages per contract §4.5 (2-column, tabs) | — | — |
| All 36 remaining Index views + Home + Statistics | keep (thinned by shared partials P2 + AJAX P5/P6) | — | — | — |

Rejected reductions: merging SEO consoles (SeoMetadata/SeoFaq/SeoRedirects/SeoSitemap/SeoWeather) into one tabbed page — each has distinct permissions and heavy modal sets; tab-merging would couple 5 permission sets into one view and bloat a single 900+ line file (worse, not better). Merging EntityCategories + EntityTags — superficially similar but different taxonomies/permissions; keep.

New shared partials added (net new files are fine): `Areas/Admin/Views/Shared/Partials/{_EmptyState, _PaginationFooter, _ModalHeader, _FilterCard, _StatusBadge, _ActionModal}` (P2) + per-list `_XResults` partials (P5/P6).

---

## §3 Phases

> Every phase: exact files, indicative line refs, checklist, acceptance criteria (incl. an RTL line when markup/CSS is touched), ONE conventional commit citing rule IDs.

### Phase 0 — Hygiene & micro-fixes (0.5d)

Files: `Users/Partials/_ArchiveModal.cshtml` (~L36), `Places/_Table.cshtml` (~L61) + ~14 sibling numeric spots, `_AdminLayout.cshtml` (~L252/~L266 PENDING comments), both resx.

- [ ] Localize the typed-confirm token: add `Admin.Users.Archive.ConfirmToken` (EN `ARCHIVE` / AR equivalent) to both resx; use it for the placeholder, the instruction text, and the server/client comparison value so the typed word matches the UI language (CON1, contract §4.6).
- [ ] `<bdi>` sweep: wrap the ~15 culture-formatted numbers lacking it (grep `ToString("N0"` / `ToString("C"` across Admin views) — RTL3, pair with `.font-data`.
- [ ] Resolve the two `PENDING` avatar-wiring comments in `_AdminLayout` ~L252/~L266: wire avatar/display-name from the existing nav user context if available; otherwise replace comment with a dated `// TODO(backend):` note — do not invent endpoints.
- Acceptance: build 71/0; AR-RTL spot-check of archive modal and numeric columns; zero raw keys.
- Commit: `fix(admin): localize archive confirm token, bdi numeric sweep, layout notes [CON1 RTL3 A11Y5]`

### Phase 1 — AJAX & form-UX foundation in the admin shell (1d)

Files: `Areas/Admin/Views/Shared/_AdminLayout.cshtml` (~L316-328), `Shared/Partials/_ConfirmModal.cshtml`, possibly per-view `@section Scripts` cleanups.

- [ ] Load `~/assets/js/api-client.js` and `~/assets/js/form-ux.js` from `_AdminLayout` (defer, `asp-append-version`) before page scripts — the area currently loads neither, so `window.YallaJo.api`, toasts (NF1), `data-loading` (L2/F7) and the shared `data-confirm` modal (F8) are unavailable globally.
- [ ] **Audit the existing 171 `data-confirm` usages (35 files):** determine how they are wired today (per-page script vs `_ConfirmModal` markup vs inert). Reconcile to ONE mechanism: form-ux.js's shared `#yj-confirm-modal` with localized `data-confirm`/`-title`/`-action`/`-cancel` attributes. If `_ConfirmModal.cshtml` duplicates that behavior, keep whichever is canonical and delete the duplicate path; if usages are inert (no JS loaded), this phase actually FIXES silent missing confirmations on destructive actions.
- [ ] Verify no double-binding with theme `functions.js`.
- Acceptance: a destructive action on 3 sample pages (Users archive, Blogs delete, Payouts approve) shows a localized confirm, Cancel autofocused, ESC closes (MOD3/MOD5); JS-off still submits natively (PE1); build 71/0; EN+AR confirm copy.
- Commit: `feat(admin): load shared api-client and form-ux in admin shell, unify confirms [JS5 F8 NF1 MOD3 MOD5 PE1]`

### Phase 2 — Shared partial consolidation (2d)

Files: NEW `Areas/Admin/Views/Shared/Partials/{_EmptyState.cshtml,_PaginationFooter.cshtml,_ModalHeader.cshtml,_FilterCard.cshtml,_StatusBadge.cshtml,_ActionModal.cshtml}` + ~30 views refactored. New partials need `@using Microsoft.Extensions.Localization` before `@inject`.

- [ ] `_EmptyState` (model: image, message, optional CTA text+url) → replace ~25 duplicated empty-state blocks. Keep composed-empty-with-CTA semantics (contract §4.9, L6).
- [ ] `_PaginationFooter` (model: HasPrevious/HasNext/Page/route values; render `<bdi dir="ltr">` page numbers; `.tap-target` ≥44px links — D1/D2/RTL3) → ~12 views.
- [ ] `_ModalHeader` (title + localized close) → ~23 modals.
- [ ] `_FilterCard` wrapper → ~8 filter forms (Bookings, Places, Growth, Payments…).
- [ ] `_StatusBadge` (model: status string + domain hint): generalize `Users/Partials/_LifecycleBadge` into the canonical contract §4.2 mapping (success/warning/danger/info/dark + icon + localized text, never color-only — A11Y5) → ~15 views. Keep `_LifecycleBadge` delegating to it or replace usages.
- [ ] `_ActionModal` (model: id, title, body fragment, form action/route, submit label+class, optional reason textarea) → adopt for the ~15 simplest status/action modals first (e.g. Businesses, Creators, Guides approve/suspend variants); the rich ones (refund, reassign, claim matrix) stay bespoke.
- Acceptance: pixel-equivalent before/after EN+AR light+dark on 5 sampled views; each pattern defined exactly once; build 71/0.
- Commit: `refactor(admin): shared empty-state, pagination, modal, filter and status-badge partials [L6 D1 D2 A11Y5 RTL3 CON1]`

### Phase 3 — View reduction: modal CRUD for simple entities (2d)

Files: `Categories/{Index,Edit}`, `Tags/{Index,Edit}`, `Languages/{Index,Edit}`, `Specializations/{Index,Edit}`, `Translations/{Index,Edit}`, `Invitations/{Index,Resend}`, `Blogs/{Index,Create}` + their controllers.

- [ ] For each retired Edit view (per §2 table): add a create/edit modal to the Index (use `_ActionModal`/`_ModalHeader`; same modal for create vs edit, mode by id — contract §4.7); row "Edit" button carries `data-*` prefill attributes; small admin JS (`admin-modal-crud.js`, NEW, `data-yj-component="modal-crud"`, idempotent JS2/JS4) copies `data-*` → form fields on open, focuses first field, returns focus to trigger on close (MOD).
- [ ] Controller GET Edit actions: return Index VM with `EditId` set → view renders the modal server-side open (`modal fade show d-block` + static backdrop markup) so deep links and no-JS work (PE1). POST Create/Update/Delete actions unchanged (PRG + flash).
- [ ] `Invitations/Resend` → row form with `data-confirm` (F8); GET Resend → `RedirectToActionPermanent(Index)` (use 4-arg overload if a fragment is needed).
- [ ] `Blogs/Create` → "New draft" modal on Blogs/Index (Title + SourceLanguageCode); POST unchanged → redirect to `Blogs/Edit/{id}`.
- [ ] Delete the 7 retired .cshtml files (`git rm`).
- Acceptance: 62 → 55 full views; every retired route still resolves (modal-open Index or 301); create/edit/delete flows work JS-on AND JS-off; focus management per MOD3/MOD5; EN+AR + dark on all touched lists (RTL4); build 71/0.
- Commit: `refactor(admin): modal CRUD for taxonomy and simple entities, retire 7 edit views [F8 PE1 MOD3 MOD5 CON1 RTL4]`

### Phase 4 — AJAX list refinement, wave 1 (2.5d)

Files: `Users`, `Places`, `Providers`, `Guides`, `Tours`, `Translations` (+`GuideApplications`) controllers + Index views + NEW `_UsersResults.cshtml` etc. per list; reuse `wwwroot/assets/js/listing.js` (generic — single root `[data-yj-component="listing"]`, skeleton, `api.loadPartial` swap + pushState, same-pathname link + `form[data-yj-listing]` delegation, popstate — do NOT modify it).

- [ ] Per list: extract results table + `_PaginationFooter` + empty state into `_XResults` partial; wrap in `<div id="listingResults" data-yj-component="listing" aria-live="polite" aria-busy="false" data-error-text="@Localizer["Public.Results.Error"]" data-retry-text="@Localizer["Public.Retry"]" data-loading-text="@Localizer["Public.Loading"]">` (reuse existing Public.* keys — present in both resx); filter form gets `data-yj-listing`; add `@section Scripts` `listing.js`.
- [ ] Controllers: `return WantsAjax() ? PartialView("_XResults", vm) : View(vm);` on success AND error-fallback paths. NO `[OutputCache]` (C2).
- [ ] Users: remove/absorb the bespoke client-side filter (`data-yj-users-filter`) into the server-backed listing flow (S1 — SSR first, AJAX refinement).
- Acceptance: filter + pagination refine without full reload on all 6-7 lists; back/forward restores (pushState); JS-off identical results; skeleton ≤100ms (L1/L4); aria-live announces (A11Y9); EN+AR/dark (RTL4); build 71/0.
- Commit: `feat(admin): ajax pagination and filtering for people and marketplace lists [S1 L1 L4 PE1 JS5 D1 A11Y9 C2 R6]`

### Phase 5 — AJAX list refinement, wave 2: high-volume finance/audit tables (2d)

Files: `Payments`, `Payouts`, `AuditLogs`, `Bookings` (cursor-based), `Outbox`, `Moderation` log table — same `_XResults` + `WantsAjax` + listing.js pattern.

- [ ] Bookings: keep the cursor (`NextCursor`) semantics — pager links carry `cursor` route value; listing.js link delegation handles it unchanged (same pathname).
- [ ] AuditLogs: list AJAX-ified; CSV `Export` action untouched (streams; R6/IAsyncEnumerable intent already satisfied API-side).
- [ ] These are the >500-row R6 tables — server-side paging via the listing pattern is the R6 mechanism of record (no DataTables dependency added).
- [ ] Preserve ST1 RowVersion hidden fields inside swapped fragments (FlaggedReviews if included).
- Acceptance: as wave 1 + cursor paging works through AJAX and JS-off; `.font-data` numeric columns render `<bdi dir="ltr">` inside RTL (RTL3); build 71/0.
- Commit: `feat(admin): ajax refinement for finance, audit and ops tables [S1 R6 PE1 JS5 D1 A11Y9 C2 RTL3]`

### Phase 6 — [Backend] queue status-counts + counted tabs (2d)

Queues today show flat lists with no per-status counts (contract §5.6 wants tabbed queues with count tiles). Mirror the proven ContentBlogs pattern (`GetMyBlogStatusCountsQuery` + `MapGet(".../status-counts")`): grouped count over the queue entity, one tiny additive endpoint each.

- [ ] **[Backend B1]** `GET /api/v1/admin/providers/status-counts` · perm `AdminProviderQueue.Read` · response `{ pending, awaitingDocuments, approved, suspended, rejected }` · Providers module: `GetProviderQueueStatusCountsQuery : IQuery<ProviderQueueStatusCountsDto>` + handler (repository `GroupBy(Status)`), route registered in the provider admin endpoints file next to the existing queue list (literal segment before any `/{id:guid}` route + comment).
- [ ] **[Backend B2]** `GET /api/v1/blogs/admin/creators/applications/status-counts` · perm `AdminCreatorQueue.Read` · response `{ draft, pending, approved, rejected, moreInfoNeeded }` · ContentBlogs module (direct sibling of the shipped `GetMyBlogStatusCountsQuery` — copy its shape: `IQuery<T>`, `ICurrentUser` not needed here, `OperationCanceledException` guard, `Result.Success(dto)`).
- [ ] **[Backend B3]** `GET /api/v1/support/admin/tickets/status-counts` · perm `SupportTicket.Read` · response `{ open, assigned, resolved, closed }` · Support module.
- [ ] **[Backend B4]** `GET /api/v1/payments/disputes/admin/status-counts` · perm `AdminFinanceDashboard.Read` · response `{ open, underReview, escalated, resolved }` · Finance/Payments module (route under the existing `/disputes/admin` group).
- [ ] Web wiring per queue: ApiClient `GetStatusCountsAsync` → Facade fetches counts **in parallel** with the list (`Task.WhenAll`, best-effort try/catch → null, ERR3) → VM `StatusCounts` + `CountFor(...)` → Index renders counted tab pills (plain GET links `asp-route-status` participating in the P4/P5 listing AJAX; badges `<bdi dir="ltr">` `.font-data`; active pill `aria-current="page"`).
- [ ] Apply to: Providers, Creators, Support, Disputes (and reuse counts as the soft-tinted queue KPI tiles of contract §4.1 atop each queue page — only when count > 0 rows exist, no empty chrome).
- Acceptance: API build 88/0 + Web 71/0; ONE extra API call per queue render; counts match list contents per filter; tabs work JS-on/off; EN+AR (RTL3); zero new warnings.
- Commit: `feat(admin): queue status counts and counted tabs for providers, creators, support, disputes [Backend][API1 API7 L6 A11Y5 RTL3 CON1]`

### Phase 7 — F10: replace raw entity-ID inputs with lookups (2d)

Files: `Moderation/Index` (~L121-197), `Recommendations/Index` (~L146-280), `Growth/Index` (~L332), `SeoFaq/Index` (~L197), `SeoMetadata/Index` (~L40), `Trips/Index` (lookup-by-id), `Businesses/Index` (place-id lookup) + NEW `wwwroot/assets/js/admin-lookup.js`.

- [ ] Generalize the shipped creator tour-combobox pattern (`creator-tour-combobox.js`: 300ms debounce S2, AbortController via `api.get(url,{signal})` JS6, ARIA combobox/listbox keyboard nav) into a reusable `admin-lookup.js` driven by `data-yj-component="lookup"` + `data-lookup-url` + hidden id input + visible query input (no-JS fallback: server resolves typed name → top exact match else validation error — PE1).
- [ ] MVC JSON proxy actions (JS5 — browser never calls API host), each `[RequirePermission]`-gated with the consuming page's read permission:
  - `GET /admin/lookups/users?q=` → **verify first**: `GET /api/v1/security/users` already supports a search/filter param (check its query handler). If yes, proxy it. If not, **[Backend B5]** `GET /api/v1/security/users/suggest?q=` · perm `User.Read` · response `[{ id, displayName, email }]` ≤10 · Security module.
  - `GET /admin/lookups/tours?q=` → proxy existing anonymous `GET /api/v1/tours/search/suggest` (exists; `{Id,Name,Slug}`).
  - `GET /admin/lookups/places?q=` → proxy the existing public places search/suggest endpoint (verify exact route in `yallajo-endpoints.txt`; if no suggest variant exists, **[Backend B6]** `GET /api/v1/places/search/suggest?q=` mirroring the tours one).
  - `GET /admin/lookups/providers?q=` → **verify** the provider queue list endpoint's search param; else **[Backend B7]** `GET /api/v1/admin/providers/suggest?q=` · perm `AdminProviderQueue.Read`.
- [ ] Recommendations `boostId`/`pinId` deactivation inputs: replace with per-row deactivate buttons on the already-rendered boosts/pins tables (`data-confirm`) — the IDs are on screen; typing them is pure F10 debt.
- [ ] Keep `dir="ltr"` + `.font-data` styling on any remaining technical id displays (RTL3).
- Acceptance: zero visible raw-GUID text inputs in the area (grep); lookups keyboard-operable (combobox APG); debounce ≥300ms + stale-cancel; JS-off name resolution works; API+Web builds clean.
- Commit: `feat(admin): typeahead lookups replace raw id inputs across moderation, growth, seo, recommendations [Backend][F10 S2 JS5 JS6 PE1 A11Y]`

### Phase 8 — data-loading & submit-feedback sweep (1d)

Files: all Admin views with forms (~136 POST forms).

- [ ] Add `data-loading` to EVERY submit form (modal action forms, filter forms excluded — GET filters are handled by listing.js skeletons) — L2/F7 double-submit guard (form-ux.js loaded since P1).
- [ ] `aria-busy="true"` is handled by form-ux/listing wiring; verify on 3 samples.
- [ ] Toasts: AJAX paths from P4-P7 surface success/failure via `window.YallaJo.toast` (NF1); PRG paths keep `_Alerts`-style flash banners — no double-announce.
- Acceptance: every POST shows spinner + disabled submit; double-submit impossible; build 71/0.
- Commit: `feat(admin): data-loading and submit feedback across all admin forms [L2 F7 NF1 A11Y9]`

### Phase 9 — Dashboard & chart four-states polish (1.5d)

Files: `Home/Index.cshtml` (229L), `Statistics/Index.cshtml` (171L), `Growth/Index.cshtml` (329L), finance dashboard section of `Payments/Index.cshtml`; `admin-dashboard.js`, `admin-statistics.js`.

- [ ] Per-chart shimmer skeleton reserved at final height (contract §4.8; `placeholder-glow`, transform/opacity only X13, `prefers-reduced-motion` gated M3) shown until ApexCharts render completes; composed empty state when a series is empty; inline retryable error per section (ERR3 — page never blocks on slowest query).
- [ ] `aria-live="polite"` on the 3 chart containers lacking it (A11Y9); keep `role="img"` + `aria-label`.
- [ ] KPI tiles: verify they follow contract §4.1 variants (trend tile on dashboards) and `.font-data` + `<bdi dir="ltr">` numerals (RTL3); no 3-identical-card rows (banned).
- [ ] Shared flatpickr range behavior across revenue/bookings charts (contract §5.1) — verify, fix if drifted; flatpickr RTL rendering checked in AR.
- Acceptance: zero CLS on dashboard load (skeleton reserves height); charts announce updates; reduced-motion kills shimmer; EN+AR light+dark walkthrough of all 4 surfaces (RTL4); build 71/0.
- Commit: `feat(admin): chart skeletons, per-section states and dashboard polish [L1 L4 ERR3 A11Y9 X13 M3 RTL3 CON3]`

### Phase 10 — l10n closure: controller flash strings (2.5d)

Files: 35 controllers with hardcoded flash strings (87 occurrences — grep `Set(Success|Error|Flash)\(\s*"` under `Areas/Admin/Controllers`), both resx.

- [ ] Each affected controller: add `using Microsoft.Extensions.Localization;` + `using YallaJo.Web.Resources;`; ctor-inject `IStringLocalizer<SharedResource> localizer` → `_localizer`. Convention (established by Creator-area precedent): `SetSuccess/SetError(_localizer["Key"])` (LocalizedString implicit); `SetFlash` 2-arg and `?? fallback` contexts use `.Value`.
- [ ] Key naming: `Admin.{Controller}.Flash.{Name}` (e.g. `Admin.Blogs.Flash.Published`, `Admin.Users.Flash.Suspended`); dedupe repeated strings (e.g. "Missing version token…" style guards) into shared keys; expect ~80-90 distinct keys.
- [ ] Add ALL keys to BOTH resx (en 2-space-indented, ar flush-left, before `</root>`, grep-first — MSB3568 on dupes) with Arabic translations.
- [ ] Facade `.Error` strings stay English (codebase convention: internal-grade defaults; user-facing flash localized at controller layer satisfies CON1).
- [ ] Final l10n re-grep of the area (views + controllers) → zero raw user-facing English.
- Acceptance: AR walkthrough shows Arabic flash messages on 5 sampled write flows; build 71/0 zero new warnings; resx key parity EN=AR.
- Commit: `chore(admin): localize all controller flash messages [CON1]`

### Phase 11 — a11y hardening (1.5d)

Files: touched views, `site.css` (additive only).

- [ ] axe-core: zero serious+critical on the 14 logical pages, both themes.
- [ ] One `<h1>` per page (A11Y6 — verified clean, re-check post-refactor); labels + `asp-validation-for` `role="alert"` on new modal forms (A11Y4); `_StatusBadge` everywhere status is shown (A11Y5).
- [ ] Touch targets ≥44px (D2): pager links + icon-only row buttons get `.tap-target` (utility already in site.css from Creator work).
- [ ] Modal focus discipline (MOD3/MOD5): open → first field; close → trigger; ESC closes; verify on the new modal-CRUD and `_ActionModal` instances; typed-confirm modals keep Cancel as safe default.
- [ ] Keyboard pass on lookups (P7 comboboxes) and counted tabs (P6).
- Acceptance: axe clean; keyboard-only operation of one full moderation flow; EN+AR; build 71/0.
- Commit: `fix(admin): accessibility hardening across admin console [A11Y4 A11Y5 A11Y6 A11Y9 D2 MOD3 MOD5]`

### Phase 12 — Performance & exit gate (1d)

Files: `_AdminLayout.cshtml` (~L316-328), chart views, facades.

- [ ] Move `apexcharts.min.js` + `flatpickr.min.js` out of the global layout into `@section Scripts`/`@section Styles` of the ~6 views that use them (A6/J2 — every other admin page stops paying ~150KB+); `defer` all page scripts; keep `bootstrap.bundle` global (shell dependency).
- [ ] Facade `Task.WhenAll` audit (API1): 12/44 use it today — parallelize independent fetches in multi-call facades (Details pages fetching record + related lists; queue pages fetching list + counts from P6). Keep best-effort semantics.
- [ ] Images: `loading="lazy"` + explicit dimensions on any below-the-fold imgs (empty-state SVGs already sized).
- [ ] C2 spot-check: no `[OutputCache]` anywhere; NoStore headers verified on dashboard/users/payments.
- [ ] Animation audit: transform/opacity only (X13), reduced-motion (M3) incl. notification `.animation-blink`.
- [ ] Final sweep: grep area for `style=` (zero), `console.log` in admin JS, raw keys; run §1.5 protocol over the whole area as exit gate.
- Acceptance: non-chart admin pages no longer load ApexCharts/flatpickr; zero CLS; §1.5 passes area-wide; build 71/0 — final.
- Commit: `perf(admin): per-page chart bundles, parallel facades, cache and animation hygiene [A6 J2 API1 C2 X13 M3 I-family]`

---

## §4 [Backend] additions summary

| ID | Phase | Endpoint | Perm | Module | Notes |
|---|---|---|---|---|---|
| B1 | P6 | `GET /api/v1/admin/providers/status-counts` | AdminProviderQueue.Read | Providers | grouped queue counts |
| B2 | P6 | `GET /api/v1/blogs/admin/creators/applications/status-counts` | AdminCreatorQueue.Read | ContentBlogs | sibling of shipped `GetMyBlogStatusCountsQuery` |
| B3 | P6 | `GET /api/v1/support/admin/tickets/status-counts` | SupportTicket.Read | Support | |
| B4 | P6 | `GET /api/v1/payments/disputes/admin/status-counts` | AdminFinanceDashboard.Read | Finance | |
| B5 | P7 | `GET /api/v1/security/users/suggest?q=` | User.Read | Security | **conditional** — only if existing users list lacks a search param |
| B6 | P7 | `GET /api/v1/places/search/suggest?q=` | anonymous (mirror tours) | ContentPlaces | **conditional** — only if no places suggest exists |
| B7 | P7 | `GET /api/v1/admin/providers/suggest?q=` | AdminProviderQueue.Read | Providers | **conditional** |

All additive: new query + handler + `MapGet` + `MustHavePermissionAttribute`; `Result<T>.ToApiResult()`; literal segments registered before `/{id:guid}` patterns with a comment; never reshape existing contracts. API build gate 88w/0e.

## §5 Execution order

| Phase | Est. | Depends on | Parallelization |
|---|---|---|---|
| P0 hygiene | 0.5d | — | — |
| P1 shell foundation | 1d | — | ∥ P0 |
| P2 shared partials | 2d | P0 | ∥ P1 |
| P3 modal-CRUD view reduction | 2d | P1, P2 | |
| P4 AJAX wave 1 | 2.5d | P1, P2 | ∥ P3 |
| P5 AJAX wave 2 | 2d | P4 pattern | ∥ P6 backend |
| P6 [Backend] queue counts | 2d | P4 (tabs ride listing) | backend ∥ P5 frontend |
| P7 [Backend] F10 lookups | 2d | P1 | ∥ P6 |
| P8 data-loading sweep | 1d | P1 | ∥ P9 |
| P9 dashboard/charts | 1.5d | P1 | ∥ P8 |
| P10 flash l10n | 2.5d | — | ∥ anything |
| P11 a11y | 1.5d | P3-P9 | ∥ P12 prep |
| P12 perf + exit gate | 1d | all | last |

**Total ≈ 21.5 days solo (~4.5 weeks); two agents ≈ 2.5–3 weeks** (backend phases P6/P7 parallel frontend; P10 fully independent).

## §6 Hard rules recap

1. Touch only `Areas/Admin/**` + listed shell/asset files + the §4 `[Backend]` additions. Never other Web areas, never public `_Layout`.
2. Browser never calls the API host (JS5) — MVC proxies only.
3. Every AJAX conversion keeps the no-JS PRG path (PE1); modal-CRUD deep links server-render the modal open.
4. Every string in BOTH resx (CON1); grep-first; en 2-space / ar flush-left.
5. EN-LTR + AR-RTL light+dark verification is a merge gate per phase (RTL4).
6. Build gates: Web 71w/0e, API 88w/0e, zero new warnings.
7. One conventional commit per phase citing rule IDs; stage explicit file lists.
8. Forbidden: `[OutputCache]` in the area (C2), DataTables/new JS deps, jQuery, inline styles without X6 comment, role-name checks in Web, reshaping existing API contracts, server-side draft autosave inventions, em dashes in UI copy.
