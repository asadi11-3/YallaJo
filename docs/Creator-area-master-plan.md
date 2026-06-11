# Creator Area Master Plan — UI/UX Modernization, View Consolidation, AJAX & Backend Support

> **Audience:** an autonomous coding agent with **zero conversation context**. Everything needed to execute is in this document.
> **Produced from:** a full code audit of `src/Hosts/YallaJo.Web/Areas/Creator`, the ContentBlogs/ContentCore modules, `yallajo-endpoints.txt`, and the binding rules contract `yallajo-plan/UI-UX-Design.md`. All line numbers are **indicative** (~L123) — re-locate before editing.
> **Relationship to other docs:** `docs/content-creator-dashboard-plan.md` is the historical *build* plan for this area (now shipped; some claims stale — notably the cover-image endpoint, which was **removed** by migration `20260605193113_RemoveCreatorProfileCoverImageUrl`). `yallajo-plan/6-content-creator-dashboard.md` is the area scope contract. `docs/public-area-master-plan.md` built the AJAX/UX foundations this plan **extends — never duplicates**.
> **Three parts:** I. Hygiene & Structure (Phases 0–2) · II. AJAX & Capability Upgrades (Phases 3–7) · III. Experience Polish (Phases 8–11).

---

## §1 Context & Ground Rules

### 1.1 Project layout

```
src/Hosts/YallaJo.Web/Areas/Creator/
  Controllers/   ApplicationController, ArticlesController, AudienceController,
                 DashboardController, PreviewController, ProfileController
  Facades/       CreatorApplicationFacade, CreatorArticlesFacade, CreatorArticleImagesFacade,
                 CreatorAudienceFacade, CreatorDashboardFacade, CreatorPreviewFacade, CreatorProfileFacade
  ApiClients/    CreatorApiClient, CreatorArticlesApiClient, CreatorArticleImagesApiClient
  Models/        Application(6) Articles(3 + Images 3) Audience(3) Dashboard(4) Preview(2) Profile(4)
  Shared/        _CreatorSidebar.cshtml, _ViewImports.cshtml, CreatorSidebarVm.cs
  Views/         Application/{Index, Invite, _ApplicationFormFields}  Articles/{Index, Editor, _ArticleImages}
                 Audience/Index  Dashboard/Index  Preview/Index  Profile/Index
                 (NO Views/Shared folder exists yet — Phase 1 creates it)
src/Hosts/YallaJo.Web/wwwroot/assets/js/creator-article-editor.js   (only Creator page script)
src/Hosts/YallaJo.Api                                               (minimal-API host)
src/Modules/ContentBlogs                                            (Blog, CreatorProfile, CreatorApplication,
                                                                     CreatorNiche, CreatorFollow, CreatorInvitation)
src/Modules/ContentCore                                             (Attachment subsystem, EntityType enum)
```

All Creator views use the main `~/Views/Shared/_Layout.cshtml` (via `_ViewStart`) — **not** `_AdminLayout`/`_AuthLayout`, so the known Google-Fonts debt in those two layouts is **out of scope** here. `_Layout` already loads `theme-bootstrap.js` early, conditional `rtl.css` (~L85-88), antiforgery meta (~L253), `api-client.js` + `form-ux.js` (~L256-257).

### 1.2 Conventions (binding — cite rule IDs from `yallajo-plan/UI-UX-Design.md` in every commit)

- **Pipeline:** Controller → Facade → ApiClient → API. `ApiResult` values, never exceptions. `BaseController` helpers: `GuardSignOut`, `SetSuccess`/`SetError`, `ApplyValidationErrors`, PRG. Suffix-based DI auto-registration (new `*Facade`/`*ApiClient` classes register automatically).
- **AJAX (JS5, PE1, SEC7):** all browser calls go through `window.YallaJo.api` (`get/post/postForm/loadPartial` — antiforgery header, 10s timeout, 401→sign-in). The browser **never** calls the API host; MVC endpoints only. Every AJAX conversion keeps a working no-JS path (PE1: native form POST + PRG fallback via `BaseController.WantsAjax()` → `PartialView`).
- **Caching (C2):** the Creator area is auth-gated → `NoStore` everywhere. Do **not** add `[OutputCache]` to Creator actions.
- **Localization (CON1):** `IStringLocalizer<SharedResource>`; every new key in BOTH `Resources/SharedResource.en.resx` and `.ar.resx`, entries flush-left, appended before `</root>`. **Grep before adding** any key — duplicates cause MSB3568. UTF-8 only; never re-encode resx with PowerShell. New Razor partials need `@using Microsoft.Extensions.Localization` before `@inject` (the Creator `_ViewImports.cshtml` exists but verify it covers any new Shared folder).
- **RTL/LTR (RTL1–RTL4):** site flips `<html lang dir>` per culture (EN→ltr, default AR→rtl); `rtl.css` is **appended after** `style.css` for RTL only. Use CSS logical properties / Bootstrap direction-aware utilities (`ms-*`/`me-*`/`ps-*`/`pe-*`/`start-*`/`end-*`/`text-start`/`text-end`) — never physical `left/right` unless justified with a code comment (RTL1). All `[dir="rtl"]` overrides live in `rtl.css` (RTL1). Flip directional icons only (RTL2). Numbers, codes, GUIDs, slugs, dates wrapped `dir="ltr"` or `<bdi>` (RTL3 — the area already does this well; preserve it). **Both-direction visual check is a merge gate for every phase touching markup/CSS (RTL4).**
- **Theming (T1–T5):** `data-bs-theme`; semantic Bootstrap colors only (area is already dark-mode-safe — keep it that way).
- **JS (J1–J3, JS2, JS4, JS6, X8):** vanilla only, no jQuery `$.ajax`; page scripts in `wwwroot/assets/js/{page}.js` via `@section Scripts`; declarative `data-yj-component` init; idempotent; `AbortController` on superseded requests. Vendored libs only — **no new deps/CDNs** (V3: Google Fonts forbidden; fonts already self-hosted incl. Arabic stack).
- **SEO:** `SetSeo` only on public pages — Creator is `robots`-disallowed (`/creator/`), so **no SetSeo work** here.
- **Charts (X14):** ApexCharts is allowed on dashboards — but this plan does **not** add charts (no time-series data is exposed for creators; do not invent it).
- **Backend additions:** allowed ONLY for the explicitly tagged `[Backend]` items in this plan (B1, B3, B4, optional B2). Additive only; never reshape existing endpoints or break existing consumers.

### 1.3 Verified inventory (counts re-verified against code)

| Asset | Count | Detail |
|---|---|---|
| Controllers | 6 | Application (6 actions), Articles (15), Audience (1), Dashboard (1), Preview (1), Profile (4) |
| Actions | **27** | all `[Authorize]`, all permission-gated (`WebPermission.*`) except none |
| Facades | 7 | see §1.1 |
| ApiClients | 3 | CreatorApiClient (10 methods), CreatorArticlesApiClient (6), CreatorArticleImagesApiClient (5) |
| Views | **12 .cshtml** | 9 page views + 2 partials (`_ApplicationFormFields`, `_ArticleImages`) + `_CreatorSidebar` |
| Page scripts | 1 | `creator-article-editor.js` (250L: Quill init, F5 localStorage autosave 30s, F9 beforeunload) |
| API endpoints consumed | 21 | ContentBlogs (CreatorEndpoints.cs, BlogEndpoints.cs) + ContentCore (AttachmentEndpoints.cs) |

**Feature → chain map (every chain verified end-to-end):**

| Page | Route | Controller action | Facade | API endpoints |
|---|---|---|---|---|
| Dashboard | `/creator`, `/creator/dashboard` | Dashboard.Index `[Creator.Read]` (class-level) | CreatorDashboardFacade (parallel profile + application + recent articles) | GET `/blogs/creators/profile/mine`, GET `.../applications/mine`, GET `/blogs/my-blogs` |
| Articles list | `/creator/articles` | Articles.Index `[Blog.ReadOwn]` | CreatorArticlesFacade.GetMyArticlesAsync(page, status) | GET `/blogs/my-blogs?page&pageSize&status` |
| New article | `/creator/articles/new` | Articles.New GET+POST `[Blog.Create]` | CreatorArticlesFacade.CreateAsync | POST `/blogs` |
| Editor | `/creator/articles/{id}/edit` | Articles.Edit GET `[Blog.Read]` / POST `[Blog.Update]` | GetEditorAsync via **admin-get** (only source of RowVersion+Status) / UpdateAsync | GET `/blogs/admin/{id}`, PUT `/blogs/{id}` |
| Submit / Delete / Restore | POSTs under `/creator/articles/{id}/…` | `[Blog.Submit]`/`[Blog.DeleteOwn]` | RowVersion-guarded (ST1) | POST `/blogs/{id}/submit-for-review`, DELETE `/blogs/{id}`, POST `/blogs/{id}/restore` |
| Tour link/unlink | POSTs `…/tours`, `…/tours/{tourId}/unlink` | `[BlogTourLink.Create/Delete]` | Link/UnlinkTourAsync | POST `/blogs/{id}/tours`, DELETE `/blogs/{id}/tours/{tourId}` |
| Article images | POSTs under editor | `[Attachment.Create/Delete/Update]`, `[EntityImage.Update]` | CreatorArticleImagesFacade (single-file loop — see Phase 4) | GET/POST/DELETE `/content-core/attachments…`, PUT `…/primary`, PUT `…/reorder` |
| Profile | `/creator/profile` | Profile.Index/Update/Avatar/Deactivate | CreatorProfileFacade (avatar = **URL-only** `UpdateAvatarVm.AvatarUrl`) | GET/PUT `/blogs/creators/profile/mine`, PUT `…/mine/avatar`, DELETE `…/mine` |
| Audience | `/creator/audience` | Audience.Index `[Creator.Read]` | CreatorAudienceFacade (anonymous ordinals "Follower #N") | GET `…/profiles/{profileId}/followers` |
| Application | `/creator/application` | Application.Index/Create/Update/Submit/Invite | CreatorApplicationFacade (parallel mine + niches) | GET/POST/PUT `…/applications…`, POST `…/invitations/redeem`, GET `…/niches` |
| Preview | `/creator/preview` | Preview.Index `[Creator.Read]` | CreatorPreviewFacade (own slug → public profile + published blogs, parallel) | GET `…/profiles/{slug}`, GET `…/profiles/{slug}/blogs` |

**Capabilities that exist in the API but are unconsumed by Creator web** (inputs to phases): bulk image upload `POST /content-core/attachments/images` (≤20 files); blog comments family (`GET /blogs/{id}/comments` etc.); tour autocomplete `GET /tours/search/suggest` → `TourSuggestResponse {Id, Name, Slug}`.

**Verified backend facts the plan depends on:**
- `ContentCore.Domain/Enums/EntityType.cs`: `Place=0, Tour=1, Business=2, Review=3, Blog=4, TourGuide=5` — **no Creator member** (B1 adds it).
- `EntityOwnershipResolver` (`ContentCore.Application/Authorization/`, 41L): switch fan-out to per-module ownership services from `*.Contracts` (pattern to mirror: `IBlogOwnershipService` in `ContentBlogs.Contracts.Authorization`). Unknown types → `IsSupported:false`.
- `AttachmentLimits` (`ContentCore.Application/Limits/`): `MaxCountByEntity = {Review:5, Blog:20, Tour:30, Place:30, Business:20, TourGuide:10}`; **missing type → int.MaxValue** (so B1 MUST add a `Creator` cap). `MaxImageBytes` 10MB.
- `UploadAttachmentCommandHandler`: IDOR guard + ownership guard + magic-byte signature validation (SEC4 already enforced server-side) + cache evict + media-processing enqueue.
- `AdminBlogDetailDto` (`ContentBlogs.Application/Queries/Blog/Dtos/`, 18L): has RowVersion + Status but **no LinkedTours**. The public `BlogDetailDto` (same folder) already has `int TourCount` + `IReadOnlyCollection<BlogTourSummaryDto> LinkedTours` where `BlogTourSummaryDto(Guid TourId, int SortOrder)` — B4 mirrors this additively.
- `BlogTour` entity stores only `BlogId, TourId, SortOrder` — no denormalized tour name; names are resolved web-side (Phase 5).
- Cover image: **does not exist** (removed). Never propose cover-image features.

### 1.4 Verification protocol (every phase)

1. `dotnet build src/Hosts/YallaJo.Web/YallaJo.Web.csproj --no-incremental` — baseline **71 warnings / 0 errors**; zero NEW warnings. Phases touching `[Backend]` files additionally build `src/Hosts/YallaJo.Api/YallaJo.Api.csproj` (same zero-new-warnings bar).
2. Walk through **every touched surface** in EN-LTR **and** AR-RTL (RTL4 merge gate), in light **and** dark theme, at 390px and 1280px.
3. No raw resx keys rendered; no new inline `style=` except X6-justified with a comment; no new inline `<script>`.
4. Every AJAX conversion demonstrably degrades: disable JS, repeat the flow, confirm PRG works (PE1).
5. Keyboard pass on touched modals/forms: focus trap, ESC close, safe-action autofocus (F8, MOD3/MOD5).
6. `.gitignore` gotcha: `**/[Pp]ackages/*` is ignored — if any new path contains `packages`, add a negation (precedent at `.gitignore` ~L205-215).

### 1.5 Hard guardrails

- Touch ONLY: `src/Hosts/YallaJo.Web/Areas/Creator/**`, `wwwroot/assets/js/creator-*.js`, `wwwroot/assets/css/site.css` + `rtl.css` (additive utilities/overrides only), `Resources/SharedResource.{en,ar}.resx` (additive keys only), and the **explicitly listed `[Backend]` files** in Phases 5–7. Never touch other Web areas, `_Layout.cshtml`, Admin creator console, or Public blog pages.
- Never invent backend capabilities. Each `[Backend]` item below was verified missing; anything else discovered mid-execution gets a `// TODO(backend)` comment, not an improvised endpoint.
- One conventional commit per phase, message citing rule IDs (e.g. `feat(creator): convert articles list to AJAX pagination [S1 L1 PE1 JS5 D1]`).

---

## §2 View Reduction Table

Creator is a lean 12-view area (vs Public's 29) that was purpose-built recently — most views earn their keep. Verdicts for **every** view:

| View (lines) | Verdict | Mechanism | UX gain | SEO/route impact |
|---|---|---|---|---|
| `Dashboard/Index` (249L) | **keep, thin** | Extract 4 duplicated stat-card blocks (~L68-119) into `_StatCard` partial; restructure to lead with needs-attention (Phase 7) | Less scannning noise; actionable first screen | none (robots-disallowed area) |
| `Articles/Index` (200L) | **keep, thin** | Extract results+pager into `_ArticlesResults` partial for AJAX swap (Phase 3) | pushState filter/paging, skeletons, no full reloads | route unchanged |
| `Articles/Editor` (234L) | **keep** | Core surface; F10 tour-link fix + image section upgrades happen inside it | — | route unchanged |
| `Articles/_ArticleImages` (136L) | **keep (partial)** | Becomes the AJAX-swappable fragment for upload/delete/reorder/primary (Phase 4) | No page reload per image action | n/a |
| `Application/Index` (112L) | **keep** | Absorbs Invite (below) | Single application hub | route unchanged |
| `Application/Invite` (47L) | **MERGE → Application/Index** | Invite token form becomes a card/collapse on Application/Index (`#invite` anchor). `GET /creator/application/invite` remains and 301-redirects to `/creator/application#invite`; `POST …/invite` unchanged | One page for the whole "become a creator" journey; the tiny orphan page disappears | Internal authed route; redirect preserves deep links. No SEO impact (robots-disallowed) |
| `Application/_ApplicationFormFields` (72L) | **keep (partial)** | already a clean extraction | — | n/a |
| `Audience/Index` (104L) | **keep, thin** | Pager → AJAX (Phase 3); optionally enriched with comments feed (Phase 7, optional B2) | — | route unchanged |
| `Preview/Index` (162L) | **keep** | Distinct purpose (public-eye view) — merging into Dashboard would conflate "my console" with "what visitors see"; **rejected**: would hurt clarity, not density | — | route unchanged |
| `Profile/Index` (141L) | **keep** | Avatar section upgraded from URL-input to file upload (Phase 6) | — | route unchanged |
| `Shared/_CreatorSidebar` (98L) | **keep (partial)** | alt-text fix only | — | n/a |
| `Application/Invite` covered above | | | | |

**Net: 12 → 11 views** (Invite merged). New partials added under a new `Areas/Creator/Views/Shared/`: `_StatCard.cshtml`, `_ArticlesResults.cshtml`, `_LinkedTourChips.cshtml` (net new partials are explicitly fine — reduction targets *pages*, not composition units). No retirement hurts deep-linking: the only removed route 301s, and the area has no SEO surface.

---

## Part I — Hygiene & Structure

### Phase 0 — Bug fixes, dead weight, doc sync

**Files:** `Areas/Creator/Views/Articles/Editor.cshtml`, `Articles/Index.cshtml`, `Articles/_ArticleImages.cshtml`, `Shared/_CreatorSidebar.cshtml`, `wwwroot/assets/css/site.css`, `yallajo-plan/6-content-creator-dashboard.md`.

- [ ] Remove inline style `style="min-height: 16rem;"` on `#contentEditor` (Editor.cshtml ~L74) → `.quill-editor-host { min-block-size: 16rem; }` in `site.css` (X6, RTL1 logical property).
- [ ] Remove inline style `style="z-index: 1090;"` on toast container (Articles/Index.cshtml ~L31) → reuse/add a `.toast-container-elevated` utility (X6).
- [ ] Remove inline style `style="aspect-ratio:4/3;object-fit:cover;"` (_ArticleImages.cshtml ~L46) → `.img-thumb-4x3` utility in `site.css` (X6).
- [ ] `_CreatorSidebar.cshtml` ~L17: `alt="avatar"` → `alt="@Model.DisplayName"` (A11Y7).
- [ ] Doc sync: `yallajo-plan/6-content-creator-dashboard.md` §7.0 says Dashboard is `[Authorize]` only, but `DashboardController` has class-level `[RequirePermission(WebPermission.Creator.Read)]`. Verify which is intended (code is authoritative — a brand-new user with no Creator.Read grant must still be able to see the "Become a creator" dashboard state; if the permission blocks that funnel, relax the controller to `[Authorize]` and keep state-based rendering; otherwise fix the doc). Record the decision in the doc.
- [ ] `wwwroot/assets/js/creator-article-editor.js` ~L84 and ~L196: linter flags `var` declarations not at the root of their enclosing function — hoist them (or convert to `let`/`const` consistently with the file's style). Behavior-neutral fix.
- [ ] Confirm no other dead code: audit found zero dead views/actions/TODOs in the area — assert and move on.

**Acceptance:** build clean (zero new warnings); zero inline `style=` left in the area (grep `style="` under `Areas/Creator/Views` returns nothing un-commented); EN+AR visual check of editor, articles list, sidebar (RTL4).
**Commit:** `fix(creator): remove inline styles, fix avatar alt, sync dashboard permission doc [X6 A11Y7 RTL1]`

### Phase 1 — Creator shared partials & design-system alignment

**Files:** NEW `Areas/Creator/Views/Shared/_StatCard.cshtml`, `Areas/Creator/Views/Shared/_ViewImports.cshtml` (if needed for `@inject IStringLocalizer`), `Views/Dashboard/Index.cshtml`, `Views/Preview/Index.cshtml`, `site.css`.

- [ ] Create `Areas/Creator/Views/Shared/` and `_StatCard.cshtml` — model: `(string Icon, string ColorClass, string Value, string Label)`; markup mirrors the existing block (icon-xl `bg-* bg-opacity-10`, count in `<bdi dir="ltr">` with `.font-data`, label). Include `@using Microsoft.Extensions.Localization` if the partial localizes anything.
- [ ] Dashboard/Index.cshtml ~L68-119: replace the 4 duplicated stat-card blocks with 4 `<partial>` renders (D1 area-design consistency; CON3 culture formatting preserved via existing `<bdi>`).
- [ ] Preview/Index.cshtml: replace its 5 metric blocks with the same partial.
- [ ] Audit whether any Public shared partials apply: `_EmptyState` lives in `Areas/Public/Views/Shared/` — Razor resolves partials per-area + global only, so **copy-adapt** (do not cross-reference Public's folder) any wanted partial into `Areas/Creator/Views/Shared/`, or skip if the area's bespoke empty states (already present and good) suffice. Default: skip — don't churn working markup.

**Acceptance:** Dashboard + Preview render pixel-equivalent before/after in EN+AR, light+dark (RTL4); stat-card markup defined exactly once; build clean.
**Commit:** `refactor(creator): extract _StatCard shared partial for dashboard and preview metrics [CON3 RTL3 T3]`

### Phase 2 — View reduction: merge Application/Invite

**Files:** `Views/Application/Index.cshtml`, DELETE `Views/Application/Invite.cshtml`, `Controllers/ApplicationController.cs` (~L100-157 Invite actions), resx (only if new heading copy is needed).

- [ ] Move the invite-token form (Invite.cshtml, 47L: single token input `dir="ltr"` + submit) into `Application/Index.cshtml` as a bordered card with `id="invite"`, collapsed by default under a "Have an invitation code?" disclosure (`<details>` or Bootstrap collapse — keyboard-accessible either way, A11Y).
- [ ] `ApplicationController.Invite` GET: replace `View()` with `RedirectToActionPermanent(nameof(Index), fragment: "invite")` — preserves old deep links/bookmarks (precedent: Public Help/Detail → Help/Index `#faq-{id}`).
- [ ] `ApplicationController.Invite` POST: unchanged route/handler; on validation error, return `Index` view with the invite section expanded and error attached (`ApplyValidationErrors`), not the deleted view.
- [ ] Delete `Invite.cshtml`. Update the sidebar/application-page links that pointed at the Invite route, if any (grep `asp-action="Invite"`).
- [ ] PE1: the merged form still POSTs natively; no JS required for the disclosure's no-JS state (`<details>` is open-able without JS; if Bootstrap collapse is used, render expanded when ModelState has invite errors).

**Acceptance:** `/creator/application/invite` 301s to `/creator/application#invite`; token redemption works (happy + invalid-token paths); view count 12 → 11; EN+AR + dark check of the application page (RTL4 — token input keeps `dir="ltr"`, RTL3).
**Commit:** `refactor(creator): merge invite page into application hub with permanent redirect [PE1 F-family RTL3]`

---

## Part II — AJAX & Capability Upgrades

### Phase 3 — AJAX list conversions (Articles, Audience, Preview)

**Files:** `Controllers/ArticlesController.cs` (Index ~top), `AudienceController.cs`, `PreviewController.cs`, NEW `Views/Articles/_ArticlesResults.cshtml`, `Views/Articles/Index.cshtml` (~L51 filter form, ~L100-200 table+pager), `Views/Audience/Index.cshtml` (follower list+pager), `Views/Preview/Index.cshtml` (article list+pager), reuse `wwwroot/assets/js/listing.js`.

Pattern (identical to Public's listing conversions): extract results+pager into a partial; controller returns `PartialView` when `WantsAjax()`; wrap the container in `data-yj-component="listing"` so the existing `listing.js` handles filter-form submit + pager clicks via `YallaJo.api.loadPartial` → swap → `history.pushState` → skeletons (L1/L4/L5) → `aria-live="polite"` announce (A11Y9). **No `[OutputCache]`** — this area is NoStore (C2); the Public plan's VaryByHeader note does not apply here.

- [ ] `_ArticlesResults.cshtml`: table/cards + status badges + Prev/Next pager (existing ~L178-193 markup, D1 HasPrevious/HasNext only) + the post-delete Undo strip (keep Undo inside the partial so a swapped list retains it).
- [ ] `Articles.Index`: `if (WantsAjax()) return PartialView("_ArticlesResults", vm);` — GET filter form (~L51) and pager links get `data-yj-listing-*` hooks; URLs stay canonical (`?page=&status=`) so no-JS works untouched (PE1, S1).
- [ ] Same treatment for `Audience/Index` (follower page pager) and `Preview/Index` (published-articles pager) — small `_AudienceResults` / inline-fragment partials only if `listing.js` requires; if a page's pager is trivial, an area-local minimal partial is acceptable, but reuse `listing.js`, never write a new pager script (JS2/JS4).
- [ ] Verify `listing.js` is generic (it was built for Public `_XResults`); if it hardcodes Public selectors, parameterize via `data-` attributes in `listing.js` itself (shared shell file — allowed, additive, keep Public behavior intact and re-test one Public listing page).
- [ ] Skeletons: add a lightweight row-skeleton inside each partial container shown ≤100ms after request start (L1); respect `prefers-reduced-motion` (M3).

**Acceptance:** filter + pagination on all three pages work without reload, back/forward restores state (pushState), JS-disabled flow identical to today (PE1); `aria-live` announces result counts (A11Y9); EN+AR both directions: pager chevrons flip per `rtl.css` rules, counts stay `<bdi>` (RTL2/RTL3/RTL4); build clean.
**Commit:** `feat(creator): ajax pagination and filtering for articles, audience, preview [S1 L1 L4 PE1 JS5 D1 A11Y9 C2]`

### Phase 4 — Article images: bulk upload + AJAX section

**Files:** `ApiClients/CreatorArticleImagesApiClient.cs` (~L20-40 UploadAsync), `Facades/CreatorArticleImagesFacade.cs` (~L40-70 UploadAsync loop), `Controllers/ArticlesController.cs` (UploadImages/DeleteImage/SetPrimaryImage/ReorderImages actions), `Views/Articles/_ArticleImages.cshtml`, `Views/Articles/Editor.cshtml` (image section host), resx.

- [ ] **Bulk upload (API7, web-only):** replace the single-file loop with one call to the existing `POST /api/v1/content-core/attachments/images?entityType=Blog&entityId={blogId}` (accepts ≤20 files, multipart; antiforgery disabled server-side on upload endpoints; magic-byte validation server-side SEC4). New `CreatorArticleImagesApiClient.UploadManyAsync(blogId, files)`; facade keeps its client-side count/size pre-validation (≤5 images per article is the current web rule — clamp before calling).
- [ ] **AJAX-ify the section (PE1 kept):** image POST actions (`UploadImages`, `DeleteImage`, `SetPrimaryImage`, `ReorderImages`) gain `WantsAjax()` → `PartialView("_ArticleImages", vm)`; the editor wraps the section in a `data-yj-component` container; a small addition to `creator-article-editor.js` (or new `creator-article-images.js` via `@section Scripts`) intercepts the four forms, posts via `YallaJo.api.postForm`, swaps the fragment, toasts result (NF1). Native POST + PRG remains for no-JS.
- [ ] Upload affordance: `<input type="file" multiple accept="image/*">` with selected-file count + per-file size feedback before submit (F-family); submit button uses `data-loading` (L2).
- [ ] Reorder: keep the existing up/down buttons (keyboard-accessible, A11Y) as the baseline mechanism; optional drag-and-drop only if achievable with vanilla pointer events in <80 lines (no new deps, J1) — otherwise skip.
- [ ] Per-image delete keeps a confirm (F8) — migrate the bespoke per-image modals to the shared `data-confirm` mechanism from `form-ux.js` to delete duplicated modal markup.

**Acceptance:** uploading 3 files = 1 HTTP call (verify network tab); all four image actions update the gallery without page reload; JS-off flow still works; focus returns to a sensible element after swap (A11Y); EN+AR + dark sweep of the gallery (RTL4 — grid uses logical gaps); build clean.
**Commit:** `feat(creator): bulk image upload and ajax gallery section [API7 PE1 JS5 L2 F8 NF1 SEC4]`

### Phase 5 — Editor: fix F10 tour linking `[Backend B4]`

The editor's tour link form has a **raw GUID text input** (`Editor.cshtml` ~L133 `linkTourId`) and the unlink modal another (~L162) — direct F10 violations. Fix = autocomplete combobox + linked-tour chips. Two gaps verified: (a) the admin-get DTO doesn't return linked tours; (b) the web has no tour-lookup JSON endpoint in this area.

**`[Backend]` B4 — expose linked tours on admin-get (additive):**
- Contract: `GET /api/v1/blogs/admin/{id}` (existing route, verb, auth `Blog.Read` unchanged) — response gains `int TourCount` and `IReadOnlyCollection<BlogTourSummaryDto> LinkedTours` (`TourId: guid`, `SortOrder: int`), exactly mirroring the public `BlogDetailDto` (~L16-17). Additive fields = no consumer breakage (Admin web area simply ignores them).
- Wiring: `ContentBlogs.Application/Queries/Blog/Dtos/AdminBlogDetailDto.cs` (+2 fields) → the admin-get query handler (populate from `BlogTour` junction, same join the public detail query already does) → `YallaJo.Api` route untouched → Web: extend the admin-get response record in `Areas/Creator/Models/Articles/` + `CreatorArticlesMapper` → `ArticleEditorVm.LinkedTours`.
- [ ] `[Backend]` Implement B4 (module + DTO + handler); build API host clean.

**Web work:**
- [ ] NEW MVC JSON lookup: `ArticlesController.TourLookup(string q)` → `GET /creator/articles/tour-lookup?q=` `[RequirePermission(WebPermission.BlogTourLink.Create)]`, returns `Json(new { items = [...] })` of `{id, name, slug}`. Facade method calls a new `CreatorArticlesApiClient.SuggestToursAsync(q)` → existing anonymous `GET /api/v1/tours/search/suggest` (response shape `TourSuggestResponse {Id, Name, Slug}` — mirror the record locally; do not reference Public-area models). Debounce 300ms client-side (S2), `AbortController` on supersede (JS6).
- [ ] Combobox UI in `Editor.cshtml` replacing the raw input: text input + listbox of suggestions (ARIA combobox pattern: `role="combobox"`, `aria-expanded`, `aria-activedescendant`, arrow-key navigation — A11Y); selecting fills a hidden `tourId` + shows the chosen name. No-JS fallback (PE1): keep a plain form path — render the suggest flow progressive (without JS the text input accepts a tour *name*; server-side `LinkTour` action resolves via suggest API taking the top exact match, else returns a validation error "pick from suggestions"). Document this fallback in the action.
- [ ] Linked-tour chips: NEW `Views/Shared/_LinkedTourChips.cshtml` rendering `ArticleEditorVm.LinkedTours` as chips, each with an inline unlink button posting `UnlinkTour(id, tourId, rowVersion)` with `data-confirm` (F8). **Delete the unlink modal + raw GUID input entirely.** Chip names: resolve via the suggest/lookup data when available; for initial render the facade resolves names with bounded parallel calls to existing public `GET /api/v1/tours/{id}` via `Task.WhenAll` (API1; bounded — articles link ≤ a handful of tours; add a comment noting API7 is satisfied by the small fixed bound).
- [ ] Quill loading skeleton: show a `.placeholder-glow` block over the editor host until Quill init completes (L1), respecting M3.
- [ ] resx: new keys for combobox placeholder/labels/empty-suggestions in BOTH en+ar (CON1; grep first).

**Acceptance:** zero raw entity-ID inputs remain in the area (grep `name="tourId"` on text inputs returns nothing) — F10 satisfied; linking and unlinking work with JS on and off; RowVersion still round-trips (ST1); combobox is fully keyboard-operable (A11Y); suggestions debounce ≥300ms and cancel stale requests (S2, JS6); EN+AR check — tour names render correctly, GUID-free UI, chips wrap in both directions (RTL4); Web + API builds clean.
**Commit:** `feat(creator): tour link combobox and chips, expose linked tours on admin blog get [Backend][F10 S2 JS5 JS6 ST1 F8 API1 PE1]`

### Phase 6 — Profile: avatar file upload `[Backend B1]`

Today the avatar is a **URL text input** (`Profile/Index.cshtml` quick-update form; `UpdateAvatarVm.AvatarUrl [Url]`) — poor UX and an odd trust model. The attachments subsystem supports uploads but `EntityType` has no `Creator` member, so ownership cannot resolve. B1 makes creator profiles attachment-capable; the web then uploads a file and feeds the resulting URL into the existing `PUT /profile/mine/avatar`. **No new API endpoint is created.**

**`[Backend]` B1 — make CreatorProfile an attachable entity:**
- `src/Modules/ContentCore/ContentCore.Domain/Enums/EntityType.cs`: append `Creator = 6` (never renumber existing members).
- `src/Modules/ContentBlogs/ContentBlogs.Contracts/Authorization/ICreatorOwnershipService.cs` (NEW): `Task<EntityOwnershipResolution> GetCreatorProfileOwnershipAsync(Guid profileId, CancellationToken ct)` — mirror `IBlogOwnershipService` exactly.
- `ContentBlogs.Infrastructure`: implementation querying CreatorProfile (owner = `UserId`; respect soft-delete → `IsDeleted`); DI-register alongside the existing blog ownership service registration.
- `ContentCore.Application/Authorization/EntityOwnershipResolver.cs` (~L20-35): add `EntityType.Creator => creatorOwnershipService.GetCreatorProfileOwnershipAsync(...)` switch arm + constructor injection.
- `ContentCore.Application/Limits/AttachmentLimits.cs`: add `Creator: 3` to `MaxCountByEntity` (avatar history cap; missing entry would mean int.MaxValue — unacceptable).
- Auth: existing `POST /content-core/attachments` route + `[Attachment.Create]` permission unchanged; Creator role must hold `Attachment.Create` (it already does — the editor image flow uses it).
- [ ] `[Backend]` Implement B1; build API host clean; verify upload with `EntityType=Creator, EntityId={profileId}` succeeds for the owner and 403s for another user (IDOR guard + ownership guard).

**Web work:**
- [ ] `Profile/Index.cshtml`: replace the avatar-URL quick form with a file-upload form (`accept="image/*"`, single file, shows current avatar 96px + live preview via `URL.createObjectURL` before submit; `data-loading` on submit — L2). Keep the URL input as a collapsed "use an image URL instead" fallback (PE1-friendly and preserves the existing capability — zero-loss change).
- [ ] `ProfileController.AvatarUpload` POST `[RequirePermission(WebPermission.Creator.Update)]`: facade orchestrates (1) upload via a new `CreatorProfileFacade` → attachments client call (reuse `CreatorArticleImagesApiClient` generalized with an `entityType` parameter, or a small dedicated method — prefer generalizing the existing client method signature additively), (2) take returned attachment URL, (3) existing `PUT /profile/mine/avatar`. PRG with success flash (NF — `_Alerts`).
- [ ] Client-side pre-validation: type + ≤10MB (matches `MaxImageBytes`) with localized error (CON1); server remains the authority (SEC4 magic-byte already enforced in handler).
- [ ] resx keys (both files, grep first): upload label, preview alt, size error, success flash.

**Acceptance:** owner can upload an avatar file end-to-end; another account gets 403 (verified via two seeded accounts — see `docs/seeded-accounts.md`); URL fallback still works; preview shows pre-submit; EN+AR + dark sweep of profile page (RTL4); Web + API builds clean; existing Blog attachment flows regress-tested (one upload via editor).
**Commit:** `feat(creator): avatar file upload via attachments, creator entity ownership [Backend][F10-adjacent L2 SEC4 CON1 PE1]`

### Phase 7 — Dashboard "needs attention" + Audience enrichment `[Backend B3, optional B2]`

The dashboard currently leads with 4 vanity metric cards; the design intent (recorded in `docs/content-creator-dashboard-plan.md`) is to lead with **actionable state** — drafts to finish, articles in review. That needs per-status counts, which today would require N filtered list calls (API7 violation). B3 adds one tiny aggregate endpoint.

**`[Backend]` B3 — own-blog status counts:**
- Contract: `GET /api/v1/blogs/my-blogs/status-counts` · auth `[Blog.ReadOwn]` (same as `/my-blogs`) · no params · response `{ "draft": n, "pendingReview": n, "published": n, "archived": n, "deleted": n }` · owner-scoped (caller's blogs only) · module: ContentBlogs.
- Wiring: NEW query `GetMyBlogStatusCountsQuery` + handler (single grouped count over the caller's blogs incl. soft-deleted bucket) in `ContentBlogs.Application/Queries/Blog/` → route in `BlogEndpoints.cs` registered **before** the `/{id}` catch-all pattern (route-order gotcha: `my-blogs/status-counts` must not bind as `{id}`) → Web `CreatorArticlesApiClient.GetMyStatusCountsAsync()` → `CreatorDashboardFacade` (add to the existing `Task.WhenAll` fan-out — API1) → `CreatorDashboardVm`.
- [ ] `[Backend]` Implement B3; API build clean.

**Web work:**
- [ ] `Dashboard/Index.cshtml` (Approved state, ~L60-130): insert a "Needs attention" strip ABOVE the stat cards: `{draft count} drafts to finish → /creator/articles?status=Draft`, `{pendingReview} in review`, `{deleted} recently deleted (restorable)` — each a localized link-card, rendered only when count > 0; all-zero → render nothing (no empty chrome). Stat cards (Phase 1 partial) remain below. Counts in `<bdi dir="ltr">` (RTL3); badge colors always paired with icon+text (A11Y5).
- [ ] `Articles/Index.cshtml` filter (~L51): upgrade the status filter to tabs/pills with per-status counts from B3 (passed through `MyArticlesVm`), replacing count-less options. Works as plain GET links (PE1) and participates in the Phase 3 AJAX listing.
- [ ] **Optional B2 (only if dashboard/audience enrichment is prioritized after B3 ships) `[Backend]`:** `GET /api/v1/blogs/my-blogs/comments?page=&pageSize=` · `[Blog.ReadOwn]` · paginated `{items: [{commentId, blogId, blogTitle, excerpt, authorDisplay, createdAt}], hasNext}` · ContentBlogs query handler joining caller's blogs → comments (anonymous-safe author display, no emails) → `BlogEndpoints.cs` route → `CreatorArticlesApiClient` → `CreatorAudienceFacade` → "Recent comments on your articles" read-only card on `Audience/Index` (links target the public blog post's comment anchor). Avoids the API7 per-article loop that any web-only implementation would need. If descoped, ship Audience unchanged — do NOT loop `GET /blogs/{id}/comments` per article.
- [ ] resx for all new strings, both files (CON1).

**Acceptance:** dashboard shows actionable items first and hides the strip at zero; tab counts match list contents; one extra API call total per dashboard render (network tab); EN+AR/dark sweep — count badges legible both themes, numerals stable in RTL (RTL3/RTL4); Web + API builds clean.
**Commit:** `feat(creator): needs-attention dashboard and counted status tabs [Backend][API1 API7 L6 A11Y5 RTL3 CON1]`

---

## Part III — Experience Polish

### Phase 8 — Conversion UX sweep (loading, confirms, toasts)

**Files:** every Creator view with a form; `form-ux.js` is consumed, not modified.

The area predates the Public foundations: it uses **zero** `data-loading`/`data-confirm` attributes and bespoke modals. Standardize:

- [ ] Add `data-loading` to every submit form in the area (Application create/update/submit + invite, Articles new/submit/delete/restore, Editor save, image forms if not already from Phase 4, Profile update/avatar/deactivate) — disables + spinner (L2/F7).
- [ ] Migrate destructive confirms to `data-confirm` (F8/MOD3/MOD5 — safe action autofocus): article delete, image delete (Phase 4 may have done these), profile deactivate (keep its richer 60-day warning copy — `data-confirm` supports custom message; if the deactivate modal's content is too rich for the generic mechanism, keep the bespoke modal but align focus/ESC behavior with MOD3/MOD5 and note why).
- [ ] Post-action feedback: PRG flash messages already flow via `_Alerts`; AJAX paths (Phases 3-5) toast via `window.YallaJo.toast` (NF1: bottom-right, max 3, 5s). Verify the Articles Undo strip and toast coexist sanely (don't double-announce).
- [ ] Editor autosave polish: `creator-article-editor.js` already does F5 (localStorage 30s) + F9 (beforeunload). Add a subtle "Draft saved locally · {time}" indicator near the save button (aria-live polite, A11Y9); **no server autosync** — no drafts-autosave endpoint exists and F5 explicitly conditions server sync on one (do not build it).

**Acceptance:** every form in the area shows a spinner on submit; every destructive action confirms with safe-default focus; double-submit impossible (button disabled); keyboard-only pass of all confirms (ESC closes, Tab cycles); EN+AR sweep (RTL4); build clean.
**Commit:** `feat(creator): data-loading, data-confirm and toast standardization across all forms [L2 F7 F8 NF1 MOD3 MOD5 A11Y9]`

### Phase 9 — Localization & RTL audit closure

**Files:** `Resources/SharedResource.{en,ar}.resx`, `rtl.css` (only if needed), touched views.

The audit found the area **already clean**: zero hardcoded English, counts/dates/slugs/GUIDs already in `<bdi dir="ltr">`, no direction-fragile CSS, no directional icons needing flips. This phase is therefore a **closure gate**, not a fix-list:

- [ ] Re-grep the area post-Phases 0-8 for hardcoded strings (`Select-String -Pattern '>[A-Za-z]{3,}' Areas/Creator/Views` heuristics + manual review of new partials/JS-rendered strings — JS toast/combobox messages must come from `data-*` attributes filled by Localizer, never literals in JS).
- [ ] Verify every key added in Phases 2-8 exists in BOTH resx files; build catches MSB3568 duplicates; spot-check AR renders (no raw keys).
- [ ] New surfaces direction check: combobox dropdown alignment (anchor with `start-0` not `left-0`), chips wrap direction, needs-attention cards, file-upload preview — all logical-properties only (RTL1); any unavoidable physical override goes in `rtl.css` with a comment (never inline).
- [ ] Numbers/codes added by new features (status counts, file sizes, timestamps) wrapped `dir="ltr"`/`<bdi>` (RTL3).
- [ ] Full both-direction walkthrough of all 11 views + every modal/dropdown/toast in AR-RTL: flatpickr/Choices are NOT used in this area (Quill is direction-agnostic for LTR/RTL text entry — verify typing Arabic article content renders RTL inside the editor), offcanvas sidebar anchors to the correct side, toasts position correctly.

**Acceptance:** RTL4 merge gate passes for the whole area; zero raw keys; zero new physical-direction CSS; AR article authoring in Quill confirmed usable.
**Commit:** `chore(creator): l10n completeness and rtl closure audit [CON1 RTL1 RTL2 RTL3 RTL4]`

### Phase 10 — Accessibility hardening

**Files:** touched views; no new tooling deps (axe via browser devtools/Playwright run, not a package).

- [ ] Run axe on all 11 views (both themes): zero serious/critical. Known checks: one `h1` per page (A11Y6), label coverage `asp-for` + `asp-validation-for role="alert"` (A11Y4 — already good, re-verify new forms), badge color+icon+text (A11Y5 — status badges in Articles list and Dashboard), alt text (A11Y7 — Phase 0 fixed the sidebar; follower avatars correctly decorative).
- [ ] AJAX swap announcements: every Phase 3-5 fragment swap fires `aria-live="polite"` updates with localized result summaries (A11Y9).
- [ ] Focus management: after AJAX gallery actions focus returns to the actioned control's successor; after modal close focus returns to the trigger (MOD-family); combobox per ARIA APG pattern (Phase 5 built it — verify with keyboard + NVDA-class reader smoke test).
- [ ] Touch targets ≥44px on pager links, chips' unlink buttons, image reorder buttons (D2).
- [ ] Skip link works on Creator pages (A11Y2 — provided by `_Layout`; verify it lands on the area's main content, not the sidebar).

**Acceptance:** axe clean (serious+) on all views in light+dark; full keyboard journey: apply → write article → upload images → link tour → submit for review, mouse-free; EN+AR (RTL4).
**Commit:** `fix(creator): accessibility hardening across creator console [A11Y2 A11Y4 A11Y5 A11Y6 A11Y7 A11Y9 D2 MOD3]`

### Phase 11 — Performance & polish

**Files:** touched views, `site.css`.

- [ ] Images: `loading="lazy"` + explicit `width`/`height` (or aspect-ratio utility) on gallery thumbs, avatars, preview-page article images (A7, I-family, CLS).
- [ ] Confirm NoStore on all Creator responses (C2) — spot-check response headers on dashboard/articles/profile.
- [ ] Facade fan-outs use `Task.WhenAll` where independent (API1): Dashboard (already parallel — re-verify with B3 added), Application (parallel mine+niches — already), Preview (already), Editor (admin-get + images + tour-name resolution from Phase 5 — parallelize).
- [ ] JS budget: `creator-article-editor.js` and any new script load only on their pages via `@section Scripts` (A6 defer); Quill assets load only on the editor (already vendored — verify no other page pulls them).
- [ ] Animations: only transform/opacity (X13); all respect `prefers-reduced-motion` (M3) — audit skeletons/toasts/chips added in earlier phases.
- [ ] Final sweep: grep area for `style=` (X6), `http://` CDN refs (V-family), `console.log` leftovers; run the §1.4 protocol once over the whole area as the exit gate.

**Acceptance:** zero layout shift on gallery/dashboard load (devtools CLS check); no render-blocking additions; protocol §1.4 passes area-wide; build clean — final.
**Commit:** `perf(creator): lazy media, parallel facades, animation and cache hygiene [A6 A7 API1 C2 M3 X13]`

---

## §4 Proposed `[Backend]` additions — summary

| ID | What | Where | Status |
|---|---|---|---|
| **B4** | Add `TourCount` + `LinkedTours` to `AdminBlogDetailDto` (mirror public `BlogDetailDto`) | ContentBlogs query DTO + handler; no route change | Phase 5 — required for F10 fix |
| **B1** | `EntityType.Creator = 6` + `ICreatorOwnershipService` (Contracts + Infrastructure impl + resolver arm + DI) + `AttachmentLimits` `Creator: 3` | ContentCore enum/resolver/limits + ContentBlogs.Contracts/Infrastructure; no new endpoint | Phase 6 — required for avatar upload |
| **B3** | `GET /api/v1/blogs/my-blogs/status-counts` → `{draft, pendingReview, published, archived, deleted}` · `[Blog.ReadOwn]` | ContentBlogs query + handler + `BlogEndpoints.cs` route (register before `{id}` patterns) | Phase 7 — required for needs-attention |
| **B2** *(optional)* | `GET /api/v1/blogs/my-blogs/comments?page&pageSize` · `[Blog.ReadOwn]` · paginated own-blog comments | ContentBlogs query + handler + route | Phase 7 — optional Audience enrichment; if descoped, do nothing (never loop per-article) |

All additive; zero changes to existing contracts' shapes beyond appended fields; existing consumers unaffected.

## §5 Execution order

| Phase | Scope | Est. | Depends on |
|---|---|---|---|
| 0 | Inline styles, alt fix, doc sync | 0.5 d | — |
| 1 | `_StatCard` + Creator Views/Shared | 0.5 d | — |
| 2 | Merge Invite → Application | 0.5 d | — |
| 3 | AJAX lists (Articles/Audience/Preview) | 2 d | 1 (partial conventions) |
| 4 | Bulk upload + AJAX gallery | 2 d | 3 (WantsAjax pattern in area) |
| 5 | Tour combobox + chips `[B4]` | 2.5 d | 3 |
| 6 | Avatar upload `[B1]` | 2 d | — (backend-heavy, independent) |
| 7 | Needs-attention + tabs `[B3]` (+opt `[B2]` +1 d) | 1.5 d | 1, 3 |
| 8 | data-loading/confirm/toast sweep | 1.5 d | 3, 4, 5, 6 |
| 9 | l10n/RTL closure | 1 d | 0–8 |
| 10 | A11y hardening | 1.5 d | 0–9 |
| 11 | Perf & final sweep | 1 d | all |

**Total: ~16.5 days solo (+1 optional B2) ≈ 3.5 weeks.**
**Parallelization:** {0, 1, 2} are independent → 1 day combined for two agents. Phase 6 (backend-heavy) parallels Phases 3–5 (frontend-heavy). Phases 9 and 10 can interleave. Two agents ≈ 2 weeks.

## §6 Hard rules recap (verbatim gates)

1. Plan scope only: `Areas/Creator/**`, `creator-*.js`, additive `site.css`/`rtl.css`/resx, shared `listing.js` (parameterization only, Public regression-tested), and the four `[Backend]` items' listed files. Nothing else. Never other Web areas, never `_Layout`, never Admin/Public blog surfaces.
2. Browser never calls the API host; all AJAX via `window.YallaJo.api` against Creator MVC endpoints (JS5).
3. Every AJAX conversion keeps the native-POST/PRG no-JS path (PE1).
4. Every string in BOTH resx files, flush-left, grep-before-add (CON1, MSB3568).
5. EN-LTR + AR-RTL + light + dark + 390/1280 walkthrough is a merge gate per phase (RTL4).
6. Build gate: `dotnet build src/Hosts/YallaJo.Web/YallaJo.Web.csproj --no-incremental` — 71 warnings / 0 errors baseline, zero new. API host build added for `[Backend]` phases.
7. Conventional commits, one per phase, citing rule IDs.
8. Cover-image features are forbidden (capability was removed from the API). Server-side autosave is forbidden unless a drafts endpoint exists (it does not). Charts are not in scope (no creator time-series data exists — do not invent it).
