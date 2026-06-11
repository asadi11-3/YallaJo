# Guide Area Master Plan — UI/UX Modernization, View Consolidation & AJAX Enhancement

> **Scope:** `src/Hosts/YallaJo.Web/Areas/Guide` (the tour-guide dashboard), plus the explicitly listed `[Backend]` additions in `src/Hosts/YallaJo.Api` / `src/Modules`.
> **Contract:** every change cites rule IDs from `yallajo-plan/UI-UX-Design.md`. Structural exemplar: `docs/public-area-master-plan.md`.
> **All line numbers are indicative (~L123) — re-verify before editing.**

---

## §1 Context & Ground Rules

### 1.1 Project layout (relevant slice)

```
src/Hosts/YallaJo.Web/
  Areas/Guide/
    Controllers/        14 controllers, 48 actions (15 GET, 33 POST — all POSTs AFT-protected)
    Facades/            14 Guide*Facade classes
    ApiClients/         13 *ApiClient classes (thin IApiClient wrappers)
    Models/             VM + form-VM + API-contract records per feature
    Views/              16 page views + _ViewImports
    Shared/             _GuideSidebar.cshtml + GuideSidebarVm.cs (non-standard location)
  Infrastructure/Mvc/BaseController.cs        (GuardSignOut, SetFlash, ApplyValidationErrors, L, WantsAjax ~L132)
  Infrastructure/Middleware/SecurityHeadersMiddleware.cs (CSP ~L107-110 — stale Google-Fonts entries)
  Views/Shared/_Layout.cshtml                 (public Webestica layout — currently used by ALL Guide views)
  wwwroot/assets/js/    api-client.js, form-ux.js, listing.js, guide-agency-roster.js (only Guide script)
  wwwroot/assets/css/   style.css, rtl.css (appended after style.css for RTL), site.css
  Resources/SharedResource.en.resx (3,846 keys) + SharedResource.ar.resx
src/Hosts/YallaJo.Api  + src/Modules/*        ([Backend] additions only where listed in §1.6)
```

### 1.2 Conventions enforced throughout this plan

- Four-tier pipeline: View → Controller → Facade → ApiClient → `IApiClient`. Browser never calls the API host directly (JS5) — all AJAX hits Web endpoints via `window.YallaJo.api`.
- PRG everywhere; every AJAX conversion keeps a working no-JS path (PE1/PE2). `WantsAjax()` (BaseController ~L132) gates PartialView vs PRG.
- Localization: `IStringLocalizer<SharedResource>`, BOTH `SharedResource.en.resx` + `.ar.resx`, flush-left entries appended before `</root>`. **Grep before adding any key** (duplicates → MSB3568). Never re-encode resx with PowerShell; UTF-8 only.
- New Razor partials need `@using Microsoft.Extensions.Localization` before `@inject` until Phase 1 lands the `_ViewImports` upgrade.
- RTL/LTR: logical properties / Bootstrap direction-aware utilities only (RTL1); all `[dir="rtl"]` overrides in `rtl.css` (never inline / per-page `<style>`); flip directional icons only (RTL2); numbers/codes/dates/phones in `dir="ltr"` or `<bdi>` (RTL3); EN-LTR + AR-RTL visual check is a **merge gate** for every phase touching markup/CSS (RTL4).
- Dark mode `data-bs-theme` (T1–T5); auth-gated pages NoStore (C2); page scripts in `wwwroot/assets/js/{page}.js` via `@section Scripts` (J2); vendored libs only — no new deps/CDNs; ApexCharts allowed on dashboards (X14 bans it on public pages only); no `SetSeo` rich SEO on these auth pages — but `noindex` robots meta IS added (robots.txt already disallows `/guide/`).
- Inline `style=` only for dynamic values with an `X6` justification comment.
- gitignore gotcha: `**/[Pp]ackages/*` — negation precedent at `.gitignore` L205-215 if anything lands under a `packages/` folder.

### 1.3 Foundations that already exist — EXTEND, never duplicate

| Foundation | Use in this plan |
|---|---|
| `api-client.js` → `window.YallaJo.api` (get/post/postForm/loadPartial; antiforgery SEC7, timeout, 401→sign-in) | All Guide AJAX (Phases 4–5) |
| `form-ux.js` → `form[data-loading]` (L2/F7), `window.YallaJo.toast` (NF1/NF4/NF5), `data-confirm` modal (F8/MOD3/MOD5) | Phase 4 destroys the per-row modal explosion with `data-confirm` |
| `BaseController.WantsAjax()` + `[OutputCache(..., VaryByHeaderNames = new[] { "X-Requested-With" })]` | Phase 4 fragment endpoints (note: Guide pages are NoStore C2, so OutputCache applies only if a future anonymous fragment appears — Guide fragments stay NoStore) |
| `listing.js` + `_XResults` partial pattern (pushState, skeletons S1/L1/L4/L5) | Phase 4 listing pages (Applications, Earnings, MyTours, Reviews) |
| Shared partials in `Areas/Public/Views/Shared/` + `site.css` utilities (.object-cover, .avatar-*, .text-truncate-2/3) | Reuse where layout-compatible; Guide-specific variants go in `Areas/Guide/Views/Shared/` (Phase 2) |
| Self-hosted fonts (`wwwroot/fonts/` + site.css @font-face incl. Arabic stack) | Already used — Guide views ride `_Layout` which loads site.css. **No Google-Fonts migration needed** (Guide never uses `_AdminLayout`/`_AuthLayout`); only the stale CSP whitelist is cleaned (Phase 6) |

### 1.4 Verified inventory (code-verified)

**Controllers — 14, actions — 48** (all `sealed`, inherit `Infrastructure/Mvc/BaseController`, `[Area("Guide")]+[Authorize]`, literal `guide/...` attribute routes):

| Controller | Actions | Permission | Notes |
|---|---|---|---|
| Agency | 5 (Index; Apply; Accept/Decline `invitations/{id:guid}`; Leave) | GuideAgency.Read/Create/Update/Delete per action | ReloadAsync ~L88 misses GuardSignOut |
| AgencyRoster | 6 (Index; Invite GET+POST; Approve/Reject `applications/{id:guid}`; Remove `guides/{guideUserId:guid}/remove`) | class `AgencyRoster.Read` + per-action | reason-required guard duplicated ~L102/~L120 |
| Analytics | 1 (Index) | bare [Authorize] | facade uses `.Result` after WhenAll ~L51-57 |
| Applications | 2 (Index paged; Apply) | bare | only ReloadAsync that DOES GuardSignOut ~L79 |
| Availability | 3 (Index; Add; Delete `{blockId:guid}/delete`) | bare | wasted SetSidebar before redirect ~L77 |
| Dashboard | 1 (Index, dual route `guide` + `guide/dashboard`) | class GuideDashboard.Read | sidebar inline ~L24 |
| Discounts | 4 (Index; Create; Edit `{id:guid}/edit`; Deactivate `{id:guid}/delete`) | bare | **Edit ~L73 loses user input on validation error ~L83-87** |
| Earnings | 1 (Index paged) | bare | facade `.Result` after WhenAll ~L37 |
| JoinRequests | 3 (Index; Approve; Reject — id in body) | bare | twin handlers |
| MyTours | 9 (Index paged; Offering `{tourId:guid}`; AddSchedule/DeleteSchedule; AddPricingTier/DeletePricingTier; Enable/DisablePrivateTour; RemoveOffering) | RemoveOffering = GuideOffering.Delete; rest bare | 6 mutation POSTs never check ModelState; HandleMutation ~L138 |
| Profile | 8 (Index; Update; AddLanguage/RemoveLanguage; AddSpecialization — **no Remove**; UploadAvatar/UploadCover; Deactivate) | Deactivate = TourGuideProfile.DeleteOwn; rest bare | TryValidateImage ~L172; ReloadAsync ~L159 misses GuardSignOut |
| Proposals | 3 (Index; Create; Submit `{id:guid}/submit`) | bare | ApiClient stub comment ~L21 is STALE — backend is real |
| Reviews | 1 (Index paged) | bare | facade `.Result` after WhenAll ~L38 |
| Tier | 1 (Index) | class GuideDashboard.Read | double-sets ViewData["GuideNav"] (view L8 + controller ~L23) |

**Views — 16 page views** (+ `_GuideSidebar.cshtml`, `_ViewImports.cshtml`): Dashboard/Index 234L, Agency/Index 193L, AgencyRoster/Index 251L, AgencyRoster/Invite 78L, Analytics/Index 193L, Applications/Index 156L, Availability/Index 151L, Discounts/Index 254L, Earnings/Index 176L, JoinRequests/Index 172L, MyTours/Index 149L, MyTours/Offering 386L, Profile/Index 249L, Proposals/Index 203L, Reviews/Index 141L, Tier/Index 131L.

**Area-wide facts (all code-verified):**
- All 16 views use the PUBLIC `~/Views/Shared/_Layout.cshtml` → public footer (~L109-175), back-to-top, public mobile bottom navbar (~L183-236) and unused vendor JS (tiny-slider, glightbox) render on every dashboard page.
- **Zero** `WantsAjax()` usage, **zero** `fetch`/`window.YallaJo.api`, **zero** `data-loading`, **zero** skeletons in the entire area. The only page script is `guide-agency-roster.js` (40L, `window.prompt` with hardcoded English ~L32).
- **Zero** `[ResponseCache]`/NoStore anywhere despite all pages being per-user authenticated data (C2 violation).
- All 33 POSTs carry `[ValidateAntiForgeryToken]` ✅. All 48 actions reachable (no dead actions). No dead links.
- ~30+ hardcoded English flash strings in controllers (e.g. "Application submitted." Agency ~L51) despite `BaseController.L` localizer; `Reviews/Index.cshtml` is fully unlocalized (~14 strings).
- Dead code: `MyToursApiClient.UpdateScheduleAsync` ~L30 + `UpdatePricingTierAsync` ~L40 (never called — add/delete-only UX is by design).
- N+1: `ResolveGuideIdAsync` in `GuideProfileFacade` ~L187 & `GuideMyToursFacade` ~L168 re-calls `GET /guides/me` before every mutation (15 actions affected). `GET /guides/me` is bound by 4 separate ApiClients.
- Sidebar: `GuideSidebarVm.AvatarUrl/Email` never populated → stock avatar `avatar/01.jpg` always shows; `SetSidebar()` copy-pasted 12× (+2 inline); 16 views repeat sidebar ViewBag boilerplate.
- Duplication: stat-card markup 14× across 4 views; empty-state 17.svg block ~14×; per-row confirm-delete modals in 6 views (worst: Offering renders 2N+1 modals); pagination 4 views × 3 variants; `StatusStyle` lambda 6×; Money/Date helpers re-declared in 10 views with inconsistent formats; ~24 inline `style=` attributes reducible to ~4 utilities.

### 1.5 Per-phase verification protocol (every phase, no exceptions)

1. `dotnet build src/Hosts/YallaJo.Web/YallaJo.Web.csproj --no-incremental` — baseline **71 warnings / 0 errors**; **zero NEW warnings**. Phases touching `[Backend]` also build `src/Hosts/YallaJo.Api`.
2. **EN-LTR and AR-RTL walkthrough of every touched surface** (RTL4 merge gate): switch culture, verify mirroring, icon flips (directional only, RTL2), `<bdi>`/`dir="ltr"` number isolation (RTL3), vendored-widget anchoring (offcanvas end→start, `dropdown-menu-end`, toasts/tooltips).
3. Light **and** dark (`data-bs-theme`) on every touched page (T1–T5).
4. 390px and 1280px viewports (D1); touch targets ≥44px (D2).
5. No raw resx keys rendered (grep the page for `Guide.` literals in output).
6. No new inline `style=` except X6-justified with a code comment.
7. Every AJAX-converted flow re-tested with JS disabled — full PRG path must still work (PE1).
8. One conventional commit per phase citing rule IDs.

### 1.6 `[Backend]` additions — complete list (nothing else outside Areas/Guide may be touched in `YallaJo.Api`/`src/Modules`)

| ID | Endpoint | Phase |
|---|---|---|
| **B1** | `DELETE /api/v1/guides/{guideId:guid}/specializations/{specializationId:guid}` (ContentTours) | 5 |
| **B2** | `GET /api/v1/tours/open-for-applications?q=&page=&pageSize=` (ContentTours) | 5 |
| **B3** | Guide authorization for review replies on `POST /api/v1/social/reviews/{id}/reply` (Social — verify-then-extend, additive only) | 5 |

Full contracts in Phase 5. Prefer small additive endpoints; never reshape existing ones; never break existing consumers.

### 1.7 Hard guardrails

- Touch ONLY: `Areas/Guide/**`, the shell files explicitly listed per phase (`Infrastructure/Middleware/SecurityHeadersMiddleware.cs`, `Resources/SharedResource.*.resx`, `wwwroot/assets/css/site.css`, `wwwroot/assets/css/rtl.css`, `wwwroot/assets/js/guide-*.js`, new `Areas/Guide/Views/Shared/**`), and the §1.6 `[Backend]` files in `YallaJo.Api`/`src/Modules`.
- **Never** touch other Web areas (Public/Provider/Admin/Auth/Account/...) or `Views/Shared/_Layout.cshtml` itself — the Guide layout is a NEW file that references it conceptually but does not modify it.
- Never delete a route without a permanent redirect (view-reduction rows specify each).
- Never add a resx key without grepping both resx files first.
- Baseline build numbers in §1.5 are the gate; if baseline drifted, record the new baseline in the phase commit body.

---

## §2 View Reduction Table (16 → 14 page views)

Precedents from the Public plan: Search/Index retired via 301 into Tours; Help/Detail merged into Help/Index accordion with `#faq-{id}` redirect; Contact/Index2 deleted dead (29 → 26). Same bar applies here: a reduction must make the surviving page **better designed**, not just denser. All Guide pages are auth-gated and `robots.txt`-disallowed, so SEO impact is nil for every row; deep-linking and no-JS usability are the binding constraints.

| View | Verdict | Mechanism | UX gain | SEO/route impact |
|---|---|---|---|---|
| Dashboard/Index | **keep** (absorbs Tier) | Hub page; tier block replaced by shared `_TierProgress` partial rendering the FULL tier detail (anchor `#tier`) | One place for "how am I doing" — kills the Dashboard↔Tier ping-pong; removes the near-duplicate markup (Dashboard ~L79-142 vs Tier ~L58-121) | none (auth page); dual route `guide`+`guide/dashboard` unchanged |
| Tier/Index | **retire** | `TierController.Index` → `RedirectPermanent("/guide/dashboard#tier")`; view deleted; sidebar "Tier" link becomes anchor to `/guide/dashboard#tier`; `GuideTierFacade` data merged into `GuideDashboardFacade` (already fetches `GetTierProgressAsync`) | Tier page was 131L of near-duplicate Dashboard markup with zero unique actions | 301; deep links survive via anchor; noindex page anyway |
| AgencyRoster/Invite | **merge-into-Roster/Index** | Invite form becomes a server-rendered section on Roster/Index with anchor `#invite-guide` (collapsed card, progressively enhanced to offcanvas in Phase 4); GET `guide/agency/roster/invite` → `RedirectPermanent("/guide/agency/roster#invite-guide")`; POST route unchanged (PRG re-renders Index with section open + validation errors) | Invite-in-context: owner sees roster, applications, sent invitations AND the invite form on one page — no round-trip; no-JS path intact (form is plain HTML in DOM, PE1) | 301 on GET; POST unchanged; noindex |
| Agency/Index | **thin** | Leave modal → shared `data-confirm`; StatusStyle lambda → `_GuideStatusBadge`; raw-GUID Apply input → agency select (Phase 5) | Cleaner page, real picker | none |
| AgencyRoster/Index | **thin** | Per-row remove modals → `data-confirm`; reject `window.prompt` → reason modal (JoinRequests pattern ~L152-166); absorbs Invite section | One consistent destructive-action pattern (F8/MOD3) | none |
| Analytics/Index | **keep** | CSS progress bars optionally upgraded to ApexCharts (X14 allows on dashboards); duplicated label+progress cards (~L89-106 / ~L125-142) → one parameterized partial | Real trends visualization | none |
| Applications/Index | **keep** | Raw-GUID TourId input → async tour combobox fed by **B2** (F10); table → `_ApplicationsResults` fragment + listing.js | Guides can actually FIND applicable tours instead of pasting GUIDs | none |
| Availability/Index | **thin** | Per-block delete modals (N modals) → single `data-confirm` | DOM weight ↓, consistent UX | none |
| Discounts/Index | **thin** | Collapse-row Edit kept (good in-context pattern) but controller data-loss bug fixed (Phase 0) + client validation; per-row deactivate modals → `data-confirm`; raw-GUID TourId → own-tours `<select>` (existing endpoint, no backend) | Edit no longer eats user input; picker | none |
| Earnings/Index | **keep** | Gold standard — donor of patterns (`Guide.Status.*` keys ~L21, bdi pager ~L165). Stat cards → `_GuideStatCard`; invoice download link wired when invoice id present (PRINT2) | — | none |
| JoinRequests/Index | **keep** | Donor of the reject-reason modal pattern; reductions limited to shared partials | — | none |
| MyTours/Index | **keep** | Table → `_MyToursResults` fragment + listing.js pagination | pushState paging | none |
| MyTours/Offering | **thin** | 386L split into `_OfferingSchedules` + `_OfferingPricingTiers` + `_OfferingPrivateTour` partials (each an AJAX-refreshable fragment); 2N+1 modals → `data-confirm`; validation spans added | Largest page becomes maintainable; add/delete without full reload | none |
| Profile/Index | **thin** | Specialization remove button added (**B1**); avatar/cover upload gets preview + data-loading; inline styles → utilities | Symmetric language/spec management | none |
| Proposals/Index | **keep** | PlaceId raw GUID → async place combobox (`/places/lookup` exists, Web proxy only); stale stub comments deleted (Phase 0) | Picker; trust in the page (it actually lists proposals — backend is real) | none |
| Reviews/Index | **keep — full rewrite** | The area outlier: localization, culture formatting, bdi, fa-* icons, A11Y5 badge, heading hierarchy, standard empty state, `_GuidePagination`, plus reply form (**B3**) | From worst page in the area to standard-compliant, and guides can finally reply | none |

**Rejected reductions (with reason):**
- Applications + JoinRequests merge — rejected: different domains and actors (tour-guide applications vs booking join requests); merged page would mix two mental models and two unrelated form sets.
- Analytics → Dashboard merge — rejected: Dashboard already absorbs Tier; analytics' three data sections deserve their own page (D-pattern: hub links out, detail pages own depth).
- Earnings → Dashboard merge — rejected: paged history table + future invoice downloads need a dedicated page; Dashboard only shows the summary cards.

**Target: 16 → 14 page views**, plus ~9 new shared partials replacing ~600 lines of duplicated markup.

---

## Phase 0 — Bugs, dead code, correctness (no visual change)

**Goal:** fix every behavioral defect found in the audit before building on top.

**Files:** `Areas/Guide/Controllers/{Discounts,Agency,Availability,Profile,Proposals,MyTours,Availability,Tier}Controller.cs`, `Areas/Guide/Facades/{GuideAgencyRoster,GuideEarnings,GuideReviews,GuideAnalytics}Facade.cs`, `Areas/Guide/ApiClients/{MyTours,Proposals}ApiClient.cs`, `Areas/Guide/Models/Proposals/ProposalsApiContracts.cs`, `Areas/Guide/Views/Tier/Index.cshtml`.

**Checklist:**
- [ ] **Discounts.Edit data-loss fix** (`DiscountsController.cs` ~L73-87): on validation/facade failure, re-render Index with the edit form values + `ApplyValidationErrors` (mirror `Create`'s ReloadAsync pattern) instead of flash+redirect. The collapse row for the failed id renders open (`show` class keyed by a `ViewData["OpenEditId"]`). (F-series form integrity)
- [ ] **GuardSignOut on every reload path**: add to `ReloadAsync` in Agency ~L88, Availability ~L88, Discounts ~L122, Profile ~L159, Proposals ~L69, and to `GuideAgencyRosterFacade.PopulateAvailableGuidesAsync` ~L101 (copy Applications ~L79, the one correct instance).
- [ ] **R1 sweep**: replace `.Result` after `Task.WhenAll` with `await` in `GuideEarningsFacade` ~L37, `GuideReviewsFacade` ~L38, `GuideAnalyticsFacade` ~L51-57 (match GuideDashboardFacade ~L33 style incl. the `// UI-PERF-R1` comment).
- [ ] **API1**: parallelize `GuideAgencyRosterFacade.GetRosterAsync` ~L27-48 — three independent reads (guides, applications, sent invitations) → `Task.WhenAll`.
- [ ] **Dead code**: delete `MyToursApiClient.UpdateScheduleAsync` ~L30 + `UpdatePricingTierAsync` ~L40 and their `IApiClient` route bindings/contract records if unused elsewhere (grep first).
- [ ] **Stale stub comments**: delete/replace "backend is currently a stub returning an empty array" in `ProposalsApiClient.cs` ~L21 and `ProposalsApiContracts.cs` ~L5 — the backend list handler is real (`ContentTours…TourProposalEndpoints.cs` ~L29).
- [ ] **ModelState guard** on MyTours mutation POSTs (`AddSchedule` ~L65, `AddPricingTier` ~L81, `EnablePrivateTour` ~L97): `if (!ModelState.IsValid) { SetError(...); return RedirectToAction(nameof(Offering), new { tourId }); }` — interim until Phase 4 fragments; prevents garbage hitting the facade.
- [ ] **Wasted work**: remove `SetSidebar()` calls before unconditional redirects (`AvailabilityController.Delete` ~L77, `DiscountsController.Deactivate` ~L111).
- [ ] Remove redundant `ViewData["GuideNav"]` double-set in `Views/Tier/Index.cshtml` L8 (controller ~L23 already sets it).
- [ ] **Duplicate reason-guard**: extract the reason-required check duplicated at `AgencyRosterController` ~L102/~L120 into one private helper.

**Acceptance criteria:**
- Build: zero new warnings. All PRG flows manually re-tested (Discounts edit failure keeps input; reload paths kick to sign-in when session expires).
- No view markup changed except the Tier L8 deletion → no RTL/visual delta expected; still run the §1.5 EN/AR spot-check on Discounts + Tier.

**Commit:** `fix(guide): repair Discounts edit data-loss, GuardSignOut on reloads, R1/API1 facade fixes, prune dead client methods (UI-PERF-R1, UI-PERF-API1, PE2)`

---

## Phase 1 — Guide shell: base controller, sidebar ViewComponent, layout, NoStore

**Goal:** one place for the cross-cutting plumbing currently copy-pasted 12–16×, plus correct caching and dashboard-appropriate chrome.

**Files (new):** `Areas/Guide/Controllers/GuideBaseController.cs`, `Areas/Guide/ViewComponents/GuideSidebarViewComponent.cs`, `Areas/Guide/Views/Shared/Components/GuideSidebar/Default.cshtml`, `Areas/Guide/Views/Shared/_GuideLayout.cshtml`, `Areas/Guide/Services/GuideIdAccessor.cs`.
**Files (modified):** all 14 controllers, all 16 views, `Areas/Guide/Views/_ViewImports.cshtml`, `Areas/Guide/Shared/_GuideSidebar.cshtml` (deleted at end), `Areas/Guide/Shared/GuideSidebarVm.cs` (deleted).

**Checklist:**
- [ ] `GuideBaseController : BaseController`, `[Area("Guide")] [Authorize] [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]` (C2). Protected `SetNav(string key)` setting `ViewData["GuideNav"]`. All 14 controllers inherit it; delete the 12 copy-pasted `SetSidebar()` privates + 2 inline blocks; each `Index` calls `SetNav("dashboard")` etc.
- [ ] `GuideSidebarViewComponent`: resolves DisplayName (claims), Email (claims — `TourGuideProfileDto` has no email field), AvatarUrl via `DashboardApiClient.GetMyProfileAsync` behind `GuideIdAccessor` (below). View `Components/GuideSidebar/Default.cshtml` = current `_GuideSidebar.cshtml` content (133L, already localized) with the avatar fallback kept (`avatar/01.jpg` ~L19) but now actually receiving real `AvatarUrl`. Permission gate on AgencyRoster link (`ICurrentUser.Has(WebPermission.AgencyRoster.Read)` ~L106) preserved. Views replace the 16× `var sidebar = ViewBag.Sidebar as GuideSidebarVm ?? …` + `<partial>` boilerplate with `<vc:guide-sidebar />`. Delete `Areas/Guide/Shared/_GuideSidebar.cshtml` + `GuideSidebarVm.cs` after migration; delete the `new GuideSidebarVm{…}` lines from all controllers.
- [ ] `GuideIdAccessor` (scoped service, registered by the existing suffix-DI convention or explicitly): memoizes `GET /guides/me` per request + caches `guideId`/`AvatarUrl` in `ISession` (key `guide:me:{userId}`, invalidated by Profile avatar/update POSTs). `GuideProfileFacade.ResolveGuideIdAsync` ~L187 and `GuideMyToursFacade.ResolveGuideIdAsync` ~L168 delegate to it — kills the N+1 `GET /guides/me` before all 15 mutations (API-perf; no backend change).
- [ ] `_GuideLayout.cshtml` in `Areas/Guide/Views/Shared/`: derived from `~/Views/Shared/_Layout.cshtml` structure but **dashboard chrome**: keeps `<head>` pipeline (site.css, style.css + conditional rtl.css append, dark-mode bootstrap, antiforgery token block per `_Layout` ~L253, api-client.js, form-ux.js), **drops** public footer (~L109-175), public mobile bottom navbar (~L183-236), back-to-top, tiny-slider/glightbox/favorites.js/search-overlay.js vendor payload. Adds `<meta name="robots" content="noindex,nofollow">` (auth pages; robots.txt already disallows `/guide/`). Slim authenticated top bar (brand link home, language switcher, theme toggle, account menu) — reuse the markup blocks from `_Layout`'s navbar, trimmed. All 16 views switch `Layout = "~/Areas/Guide/Views/Shared/_GuideLayout.cshtml"`.
- [ ] `_ViewImports.cshtml`: add `@using Microsoft.Extensions.Localization`, `@using System.Globalization`, `@inject IStringLocalizer<SharedResource> Localizer` — then strip the duplicate inject/using lines from all 16 views.

**Acceptance criteria:**
- Every Guide page renders with the new layout: no public footer/mobile navbar; sidebar shows the real avatar after upload; AgencyRoster link still permission-gated; logout works.
- Response headers show `Cache-Control: no-store` on all Guide GETs (C2).
- Network tab: exactly ONE `GET /guides/me` per request pipeline (verified on a Profile mutation).
- Build zero new warnings; **EN-LTR + AR-RTL** walkthrough of all 16 pages (layout swap = full-surface RTL regression risk: offcanvas sidebar anchors at the correct logical edge, top bar mirrors, dropdowns use `dropdown-menu-end`); light+dark; 390/1280.
- JS-disabled smoke test: navigation, logout, one form POST per page still work (PE1).

**Commit:** `refactor(guide): GuideBaseController + sidebar ViewComponent + dedicated dashboard layout with NoStore (UI-PERF-C2, CON1, RTL4, D1)`

---

## Phase 2 — Guide design system: shared partials & CSS utilities

**Goal:** collapse the duplication table (§1.4) into parameterized partials, removing ~24 inline styles and 3 inconsistent pattern variants.

**Files (new, all under `Areas/Guide/Views/Shared/`):** `_GuideStatCard.cshtml`, `_GuideEmptyState.cshtml`, `_GuidePagination.cshtml`, `_GuideStatusBadge.cshtml`, `_TierProgress.cshtml`, `_GuidePageHeader.cshtml`; (new models) `Areas/Guide/Models/Shared/{StatCardVm,EmptyStateVm,PaginationVm,StatusBadgeVm}.cs`; (modified) `wwwroot/assets/css/site.css`, `wwwroot/assets/css/rtl.css` (only if an override is needed), all 16 views progressively (full adoption finishes alongside Phases 3–5 view work).

**Checklist:**
- [ ] `_GuideStatCard` (icon, label key, `<bdi dir="ltr">` value, optional trend) — replaces 14 card blocks (Dashboard ~L25-76, Analytics ~L28-79, Earnings ~L30-73, Reviews ~L34-59). Value always bdi-wrapped (RTL3).
- [ ] `_GuideEmptyState` (message key, optional CTA link/key, 17.svg illustration) — replaces ~14 instances; Offering's bare `<p>` empty states (~L51/145/272) and Reviews' bi-icon deviation (~L69-72) converge on it (L6).
- [ ] `_GuidePagination` (PaginationVm: Page, TotalPages, HasPrevious/HasNext from VM — **never compute page counts in the view**, D1-paging): localized Prev/Next, `aria-label` on `<nav>`, pager text `<bdi>`-wrapped (Earnings ~L165 is the donor; fixes Applications ~L143 + MyTours ~L138 + Reviews ~L125-135).
- [ ] `_GuideStatusBadge` (status string → icon + color + localized `Guide.Status.{Value}` text — A11Y5 color+icon+text): replaces the 6 `StatusStyle` lambdas and fixes the 7 views rendering raw enum text (Agency ~L124, Roster ~L133/205, Applications ~L116, JoinRequests ~L90, Proposals ~L150, MyTours ~L87, Offering ~L31, Reviews ~L88). Add the missing `Guide.Status.*` keys to BOTH resx files (grep first — Earnings already added some ~L21).
- [ ] `_TierProgress` (full tier detail: current tier, progress metrics with `role="progressbar"`) — single source for Dashboard ~L79-142 and (until Phase 3 retires it) Tier ~L58-121.
- [ ] `_GuidePageHeader` (h1 + optional action button slot) — normalizes the one-h1 rule (A11Y6).
- [ ] **site.css utilities** (with X6 note that data-driven `width:%` stays inline + comment): `.yj-progress-thin { height: .375rem; }` (kills the 6px/8px/.5rem trio — Dashboard ~L109+, Analytics ~L99/135, Tier ~L69+), `.yj-cell-truncate { max-width: 16rem; }` (kills 220/240/260/280px magic widths — Discounts ~L142, JoinRequests ~L73, Proposals ~L142, Applications ~L109; CON4: add `title`/tooltip with full value), `.yj-avatar-96 { width:96px; height:96px; object-fit:cover; }` (Profile ~L31/35). All logical-property safe (RTL1); confirm no rtl.css override needed.
- [ ] Normalize `text-secondary` → `text-body-secondary` in Availability/MyTours/Applications ~L84/Discounts ~L116 (dark-mode correctness, T-series).
- [ ] Standardize icon-button a11y on ONE technique: `aria-label` (Availability ~L109 donor); convert Offering's visually-hidden+title (~L100-101/191-192).
- [ ] Money/Date helpers: add `Areas/Guide/Infrastructure/GuideFormat.cs` static class (Money with culture + JOD 3-decimals CON3, Date `d` culture-aware) replacing the 10 per-view helper variants; the hardcoded `"JOD"` fallback (Dashboard L11 etc.) centralizes there.

**Acceptance criteria:**
- Grep `Areas/Guide/Views` for `style="height:` and `style="max-width:` → 0 hits; remaining inline styles are data-driven `width:@(...)%` with `X6` comments only.
- Each partial renders correctly EN-LTR + AR-RTL (badge icon spacing via `me-1`, pagination chevrons flip per RTL2, bdi pager per RTL3); light+dark; both viewports.
- Build zero new warnings; no raw resx keys; no duplicate resx entries (grep before add).

**Commit:** `feat(guide): shared design-system partials + css utilities, kill stat-card/empty-state/pagination/status duplication (A11Y5, CON3, CON4, L6, RTL1-RTL3, X6, T1)`

---

## Phase 3 — View reduction: Tier → Dashboard, Invite → Roster

**Goal:** execute the two retire/merge verdicts from §2. 16 → 14 page views.

**Files:** `Areas/Guide/Controllers/TierController.cs`, `Areas/Guide/Controllers/AgencyRosterController.cs` (~L41 GET Invite), `Areas/Guide/Views/Tier/Index.cshtml` (delete), `Areas/Guide/Views/AgencyRoster/Invite.cshtml` (delete), `Areas/Guide/Views/AgencyRoster/Index.cshtml`, `Areas/Guide/Views/Dashboard/Index.cshtml`, `Areas/Guide/Facades/{GuideTier,GuideDashboard,GuideAgencyRoster}Facade.cs`, sidebar component view.

**Checklist:**
- [ ] Dashboard absorbs Tier: `GuideDashboardFacade` already calls `GetTierProgressAsync` — extend `DashboardVm` with the full `TierProgressVm` payload; Dashboard view renders `_TierProgress` at anchor `id="tier"` replacing its summary block ~L79-142.
- [ ] `TierController.Index` ~L20 → `return RedirectPermanent("/guide/dashboard#tier");` (route `guide/tier` kept alive forever); delete `Views/Tier/Index.cshtml`; delete `GuideTierFacade` + its DI registration if nothing else consumes it (grep); keep `DashboardApiClient.GetTierProgressAsync`.
- [ ] Sidebar: "Tier" nav item → href `/guide/dashboard#tier` (or removed and Tier surfaces as a Dashboard tab-anchor — keep the link, users know it).
- [ ] Roster absorbs Invite: move Invite form markup (`Invite.cshtml` ~L34-77 incl. validation summary, commission `dir="ltr"` input ~L53) into a card section with `id="invite-guide"` at the bottom of `AgencyRoster/Index.cshtml`, gated by `<permission>` for AgencyRoster.Create (as the old GET was). `GuideAgencyRosterFacade.GetRosterAsync` additionally populates the available-guides list (now 4-way `Task.WhenAll`, API1). The `fa-arrow-left` back icon (Invite ~L19) dies with the page (one direction-fragile item resolved).
- [ ] `AgencyRosterController`: GET `Invite` ~L41 → `RedirectPermanent` to `/guide/agency/roster#invite-guide`; POST `Invite` ~L58 stays on the same route but failure path re-renders **Index** (full VM + form values + `ApplyValidationErrors`, section visibly open) instead of the deleted Invite view.
- [ ] `_ValidationScriptsPartial` added to Roster Index (the merged form has numeric/percent inputs).

**Acceptance criteria:**
- `GET /guide/tier` → 301 → `/guide/dashboard#tier`; `GET /guide/agency/roster/invite` → 301 → `/guide/agency/roster#invite-guide`. Old bookmarks land on the right anchor.
- Invite POST with invalid input re-renders Roster with errors and **preserved input**; with JS disabled the whole flow works (PE1).
- View count on disk: 14 page views.
- EN-LTR + AR-RTL on Dashboard + Roster (tier progressbars mirror, invite section mirrors, commission input stays `dir="ltr"` per RTL3); light+dark; 390/1280; build clean.

**Commit:** `feat(guide): merge Tier into Dashboard and Invite into Roster with 301s — 16→14 views (UI-PERF-API1, PE1, RTL3, F-series)`

---

## Phase 4 — AJAX modernization (PE1-preserving)

**Goal:** bring the Guide area to Provider/Public parity: `WantsAjax()` fragments, `listing.js` pagination, `data-loading` on every form, one shared confirm modal, toasts — while every flow keeps its PRG fallback.

**Files:** all 14 controllers (POST actions gain WantsAjax branches), new fragment partials `Areas/Guide/Views/{Applications/_ApplicationsResults, Earnings/_EarningsResults, MyTours/_MyToursResults, Reviews/_ReviewsResults, MyTours/_OfferingSchedules, MyTours/_OfferingPricingTiers, MyTours/_OfferingPrivateTour, AgencyRoster/_RosterTables}.cshtml`, new `wwwroot/assets/js/guide-offering.js`, `wwwroot/assets/js/guide-roster.js` (replaces `guide-agency-roster.js` — delete it), all form-bearing views.

**Checklist:**
- [ ] **Listing pages → listing.js pattern** (S1/L1/L4/L5, R2): Applications, Earnings, MyTours, Reviews extract their table+pagination into `_XResults` partials; GET actions return `PartialView` when `WantsAjax()`; pagination links get the `listing.js` data attributes for pushState + skeleton swap. No-JS: links remain real hrefs (PE1). Pages are NoStore (C2) — do NOT add OutputCache here.
- [ ] **`data-loading` on all 30+ forms** (L2/F7): every POST form in the area gets `data-loading` (form-ux.js is already loaded). Zero-JS cost, kills double-submits.
- [ ] **Per-row modal explosion → `data-confirm`** (F8/MOD3/MOD5): replace per-row confirm modals in Agency (~L172-193), AgencyRoster (~L220-245), Availability (~L126-150), Discounts (~L226-254), Proposals (~L179-203), Offering (~L317-381, 2N+1 → 0) with `data-confirm="<localized message>"` + `data-confirm-title`/`data-confirm-action` attributes on the POST forms, using form-ux.js's shared modal. No-JS fallback: form submits directly (destructive without confirm is acceptable degradation per PE2; document it). Profile Deactivate keeps its bespoke modal (it carries explanatory content) but gains `data-loading`.
- [ ] **Reject-reason modal** (MOD8): AgencyRoster reject + remove get ONE proper modal with labeled `<textarea>` (copy JoinRequests ~L152-166 donor); delete `guide-agency-roster.js` and its `window.prompt` (~L32, hardcoded English). New `guide-roster.js`: opens the modal, posts via `window.YallaJo.api.postForm`, refreshes `_RosterTables` fragment, `window.YallaJo.toast` on success (NF1). No-JS: modal markup is server-rendered per page (single instance, target id in hidden input), plain POST works.
- [ ] **Offering page AJAX** (`guide-offering.js`, JS1/JS2/JS9 data-yj-component self-init, idempotent): AddSchedule/DeleteSchedule/AddPricingTier/DeletePricingTier/Enable/DisablePrivateTour POSTs go through `window.YallaJo.api.postForm`; controller `HandleMutation` ~L138 gains a `WantsAjax()` branch returning the relevant fragment partial (`_OfferingSchedules` / `_OfferingPricingTiers` / `_OfferingPrivateTour`); JS swaps the fragment + toast. AbortController on rapid re-submits (JS6). No-JS: existing PRG redirect to Offering (PE1).
- [ ] **Invite offcanvas enhancement**: the Phase 3 `#invite-guide` card section gains a progressive offcanvas trigger (JS moves the section into an offcanvas on wide screens; without JS it stays an in-page card). Offcanvas uses `offcanvas-end` (logical anchor; verify rtl.css re-anchoring per the §1.5 protocol).
- [ ] **Validation parity** (F1/F2): `_ValidationScriptsPartial` via `@section Scripts` on every form-bearing view (currently only Profile ~L247-249); add missing `asp-validation-for` spans + a validation summary on Offering (~none today).
- [ ] **AJAX POST responses**: success → fragment + `X-YallaJo-Toast` convention or JSON `{ok, message}` consumed by toast (follow whichever pattern Provider established in form-ux.js — extend, don't fork); failure → 422 with validation map consumed by field-level errors.

**Acceptance criteria:**
- With JS: paginating Applications/Earnings/MyTours/Reviews updates URL via pushState, shows skeleton, no full reload; adding/deleting a schedule on Offering swaps only the schedules card + toast; every destructive action shows the shared confirm modal (static backdrop, autofocus on Cancel per MOD5).
- With JS disabled: every single flow above still completes via PRG (PE1 — tested page by page).
- `guide-agency-roster.js` deleted; no `window.prompt` anywhere; no hardcoded English in JS (CON1 — strings come from `data-*` attributes rendered from resx).
- EN-LTR + AR-RTL: toasts position correctly, offcanvas anchors at logical end, modals mirror (MOD10), skeletons mirror; light+dark; 390/1280; build clean; zero new warnings.

**Commit:** `feat(guide): AJAX modernization — listing fragments, data-loading, shared confirm modal, offering fragment swaps with PRG fallbacks (PE1, JS5, JS6, S1, L1-L4, F7-F8, NF1, MOD3-MOD10, RTL4)`

---

## Phase 5 — Conversion UX: pickers, reply, specialization remove (includes ALL `[Backend]` work)

**Goal:** kill every raw-GUID input (F10), close the two real capability gaps, and let guides reply to reviews.

### 5a `[Backend]` B1 — Remove guide specialization

- **Contract:** `DELETE /api/v1/guides/{guideId:guid}/specializations/{specializationId:guid}` → 204 on success, 404 unknown, 403 not-owner. Auth: `MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Update)` + handler-level ownership check — mirror the sibling `MapDelete("/{id:guid}/languages/{languageId:guid}")` at `ContentTours.Presentation/Endpoints/TourGuide/TourGuideProfileEndpoints.cs` ~L168 exactly.
- **Wiring:** new `ContentTours.Application/Commands/TourGuides/RemoveSpecialization/RemoveTourGuideSpecializationCommand.cs` (+Handler calling the READY domain method `TourGuide.RemoveSpecialization(Guid)` at `ContentTours.Domain/Entities/TourGuide.cs` ~L303, mirroring RemoveLanguage's handler) → endpoint in `TourGuideProfileEndpoints.cs` after ~L168 → `ProfileApiClient.RemoveSpecializationAsync(guideId, specializationId)` → `GuideProfileFacade.RemoveSpecializationAsync` → `ProfileController.RemoveSpecialization` POST `guide/profile/specializations/delete` (mirror RemoveLanguage ~L78) → per-badge remove button in `Profile/Index.cshtml` ~L171 region (mirror language badges ~L123-129, aria-label per Phase 2 convention).

### 5b `[Backend]` B2 — Browse tours open for guide applications

- **Contract:** `GET /api/v1/tours/open-for-applications?q=&page=&pageSize=` (anonymous — tour data is public, matches `/tours/search`; pageSize clamped 1–50 per R4). Response: `{ items: [{ tourId, title, slug, city?, basePrice, currency }], totalCount }`. Owner: ContentTours.
- **Wiring:** new `ContentTours.Application/Queries/Tours/ListOpenForApplications/ListOpenForApplicationToursQuery.cs` (+Handler filtering `Tour.IsOpenForApplications` — flag at `Tour.cs` ~L50 — AND published status; `ICacheableQuery` with tag `tours`) → endpoint registered alongside `GuideApplicationEndpoints.cs` (~L51 region) house-style (`.AllowAnonymous()`, `.WithName/.WithSummary/.Produces<>`), per the TourProposalEndpoints exemplar → `ApplicationsApiClient.GetOpenToursAsync(q, page, pageSize)` → `GuideApplicationsFacade.GetOpenToursAsync` → `ApplicationsController.OpenTours` GET `guide/applications/open-tours` returning `Json` lookup items (Web proxy — browser never calls the API host, JS5).

### 5c `[Backend]` B3 — Guide review replies (verify-then-extend, additive)

- **Existing:** `POST /api/v1/social/reviews/{id}/reply` (`Social.Presentation/Endpoints/Review/ReviewEndpoints.cs` ~L121, `AddReviewReplyCommand`, perm `SocialFeatures.ReviewReply/Create`, body `AddReplyRequest(Content)`); `ReviewDto.Replies` already populated (~L24, `ReviewReplyDto(Id, ReviewId, ProviderUserId, Content, CreatedAt, LastEditedAt)`).
- **Verify first:** (1) does the guide role hold `ReviewReply/Create` in the permission catalog? (2) does the `AddReviewReplyCommand` handler authorize the caller as the review TARGET when `entityType=TourGuide` (it may be provider-centric given `ProviderUserId` naming)? If either fails: **additively** grant the permission to the guide role in `Social`'s permission catalog and/or extend the handler's ownership check to accept the TourGuide entity owner. Never change provider behavior.
- **Wiring:** `ReviewsApiClient.AddReplyAsync(reviewId, content)` → `GuideReviewsFacade.ReplyAsync` (guard sign-out) → `ReviewsController.Reply` POST `guide/reviews/{id:guid}/reply` `[ValidateAntiForgeryToken]` → reply form (textarea + `data-loading`) under each unanswered review in the rewritten Reviews view; `WantsAjax()` branch returns the single review-card fragment; PRG fallback redirects to the same page (PE1).

### 5d Web-only pickers (no backend — endpoints exist)

- [ ] **Applications tour picker** (F10): replace raw GUID input (`Applications/Index.cshtml` ~L45) with an async combobox (Choices.js — vendored; verify AR/RTL rendering per §1.5) querying `guide/applications/open-tours` (5b), debounced 300ms (J7), AbortController (JS6); option label `Title — City`, submits `tourId`. No-JS fallback: server renders the first page of open tours as a plain `<select>` (PE1).
- [ ] **Discounts tour picker** (F10): raw GUID input (`Discounts/Index.cshtml` ~L95) → plain `<select>` of the guide's OWN tours (facade already can reach `GET /guides/{guideId}/tours` via existing clients; add `DiscountsApiClient` or reuse via facade composition — small lists, server-rendered, zero JS).
- [ ] **Agency picker** (F10): raw GUID input (`Agency/Index.cshtml` ~L51) → server-rendered `<select>` fed by existing `GET /api/v1/agency?page&pageSize` (`AgencyListItemDto.UserId` = the apply route's `agencyUserId`). New `AgencyApiClient.GetAgenciesAsync` method.
- [ ] **Proposals place picker** (F10): raw GUID input (`Proposals/Index.cshtml` ~L64) → async combobox via new Web proxy `ProposalsController.PlaceLookup` GET `guide/proposals/places?term=` wrapping existing `GET /api/v1/places/lookup?term=&pageSize=` (PlaceLookupDto: Id, Name, City). New `ProposalsApiClient.LookupPlacesAsync`. No-JS fallback: text input accepting a place name with server-side resolve, or keep the GUID input hidden behind `<noscript>` with helper text — choose resolve-by-name (better PE2).
- [ ] **Reviews/Index full rewrite** (carries 5c): localization of all ~14 strings (new `Guide.Reviews.*` keys, both resx), culture-aware dates (CON3), `<bdi>` on ratings/counts/dates (RTL3), fa-* icons with `aria-hidden` (consistency + A11Y7), `_GuideStatusBadge` for the verified badge (A11Y5), heading hierarchy h1→h2.h6 (A11Y6), `_GuideEmptyState`, `_GuidePagination`, star rating with `aria-label="4.0 out of 5"` (A11Y4) — reuse Public `_StarRating` if layout-compatible, else Guide variant.
- [ ] **Earnings invoice download** (PRINT2): where the earnings history row carries an invoice id, render a download link to a Web proxy `EarningsController.DownloadInvoice` GET `guide/earnings/invoices/{id:guid}/download` streaming `GET /api/v1/invoices/{id}/download` (exists — Finance `InvoiceEndpoints.cs` ~L143; **verify** guide claims satisfy its userId/provider_id authorization before shipping; if not satisfiable without backend change, drop this line item and log it as follow-up — do NOT modify Finance).

**Acceptance criteria:**
- Zero raw-GUID typed inputs remain in the area (grep views for `TourIdPlaceholder|AgencyIdPlaceholder|PlaceIdPlaceholder` → only `<noscript>`-free picker markup).
- `[Backend]` B1/B2: `dotnet build` on the Api host clean; manual API smoke (B1 removes a specialization and 403s for non-owner; B2 returns only `IsOpenForApplications && Published` tours, respects R4 clamp). B3 verified path documented in the commit body.
- Reviews page: AR-RTL renders numbers/dates isolated, reply posts with JS (fragment swap + toast) and without JS (PRG).
- EN-LTR + AR-RTL on every touched page incl. Choices.js comboboxes (dropdown alignment, typed Arabic terms); light+dark; 390/1280; build clean both hosts; no duplicate resx keys.

**Commit:** `feat(guide): entity pickers replace raw GUID inputs, review replies, specialization removal [Backend: ContentTours remove-specialization + open-for-applications endpoints, Social guide-reply auth] (F10, JS5, JS6, J7, PE1, PRINT2, A11Y4-A11Y7, CON3, RTL3)`

---

## Phase 6 — Localization & RTL hardening

**Goal:** zero hardcoded user-facing English; every direction-fragile spot from the audit fixed.

**Files:** all 14 controllers (flash strings), `AgencyRoster/Index.cshtml`, remaining views with raw enum text not yet converted by `_GuideStatusBadge` adoption, `wwwroot/assets/css/rtl.css`, `Infrastructure/Middleware/SecurityHeadersMiddleware.cs` ~L107-110, `Resources/SharedResource.{en,ar}.resx`.

**Checklist:**
- [ ] **Controller flash strings → `L[...]`** (CON1): replace all ~30+ hardcoded strings ("Application submitted." Agency ~L51, etc.) with `L["Guide.Flash.*"]` keys; add EN+AR pairs (grep both resx first; flush-left before `</root>`; UTF-8; never PowerShell re-encode).
- [ ] Hidden input `value="Removed by agency"` (`AgencyRoster/Index.cshtml` ~L236) → localized via resx-rendered `data-`/value attribute (or better: free-text reason via the Phase 4 modal — verify it landed; if so the hidden default dies entirely).
- [ ] Raw VM strings: `@day.DayOfWeek` (Analytics ~L132) → culture-aware via `CultureInfo.CurrentUICulture.DateTimeFormat.GetDayName(...)`; `@trend.Period` (~L96) → verify upstream format, localize formatting in the facade if needed.
- [ ] **Direction-fragile fixes (the §1.4 audit list, each item):**
  1. ~~`fa-arrow-left` Invite ~L19~~ — already deleted in Phase 3.
  2. `fa-arrow-left` back link `MyTours/Offering` ~L25 → keep the class but ensure `rtl.css`'s existing FA directional-icon `scaleX(-1)` flip section covers `fa-arrow-left`; if not, add it there (never inline, RTL2).
  3. Reviews bdi gaps — fixed in Phase 5 rewrite; verify.
  4. Pager bdi (Applications ~L143, MyTours ~L138) — fixed by `_GuidePagination` (Phase 2); verify.
  5. `offcanvas-end` sidebar — verify rtl.css re-anchors it in AR (the file already has navbar dropdown re-anchoring sections); add `[dir="rtl"]` override in rtl.css if missing.
  6. `.commission-input` (now in Roster's merged invite form) — confirm rtl.css coverage; input keeps `dir="ltr"` (RTL3).
  7. `fa-arrow-trend-up` (Earnings ~L28, sidebar) — semantic "growth", intentionally NOT flipped (RTL2); add a one-line comment in rtl.css's flip list excluding it if the flip section is class-wildcard based.
- [ ] **CSP cleanup** (shell file): remove `https://fonts.googleapis.com` (style-src) + `https://fonts.gstatic.com` (font-src) from `SecurityHeadersMiddleware.cs` ~L107-110 — fonts are self-hosted (V3/A5); **grep the whole solution for googleapis first** to confirm no consumer remains (this is a global header — if any other area still loads Google Fonts, defer this line and log it).
- [ ] Final CON1 sweep: grep `Areas/Guide` views + controllers + `guide-*.js` for quoted sentence-case English; resolve every hit.

**Acceptance criteria:**
- Grep `Areas/Guide/Controllers` for `SetSuccess("`/`SetError("`/`SetFlash("` with string literals → 0 hits.
- Full AR walkthrough of all 14 pages: no English leaks, no reordered numbers/dates/codes, back-arrow flips, growth icon doesn't, offcanvas opens from the logical end, all overrides live in rtl.css only (no inline, no per-page `<style>`).
- EN regression pass (rtl.css changes must not leak into LTR — it's only appended for RTL cultures, but verify selectors are `[dir="rtl"]`-scoped).
- Build clean (MSB3568 would surface duplicate resx keys); light+dark; 390/1280.

**Commit:** `fix(guide): localize all flash/UI strings, RTL hardening via rtl.css, drop stale Google-Fonts CSP (CON1, RTL1-RTL4, V3/A5, SEC headers)`

---

## Phase 7 — Accessibility pass

**Goal:** WCAG 2.1 AA across the area (A11Y1).

**Files:** all 14 page views + shared partials + `_GuideLayout.cshtml`.

**Checklist:**
- [ ] Heading hierarchy audit per page: exactly one `h1` (A11Y6), descending levels — Reviews fixed in Phase 5; verify Dashboard (h1→h2.h6 pattern), Offering's section headings.
- [ ] `aria-hidden="true"` on every decorative icon (Reviews' bi-* set converted in Phase 5; sweep remaining fa-*).
- [ ] Modal semantics: remove the misplaced `role="dialog" aria-modal="true"` on `.modal-content` (Agency ~L174, Roster ~L224, Proposals ~L183, Profile ~L226) — Bootstrap manages the `.modal` element; most bespoke modals died in Phase 4, fix survivors (Profile Deactivate).
- [ ] Forms: every input has a `<label>` or `aria-label` (A11Y4); file inputs keep visually-hidden labels (Profile ~L52/60 donor); helper text wired via `aria-describedby` (Invite-merged form donor).
- [ ] AJAX live regions: fragment-swap containers get `aria-live="polite"` / toasts already aria-live per form-ux.js (NF-series, A11Y9); skeletons get `aria-busy="true"`.
- [ ] `_GuideLayout`: skip link to `#main-content` (A11Y2), `<main>` landmark, focus management after fragment swaps (focus the swapped region's heading).
- [ ] Keyboard pass: confirm-modal focus trap + Escape (MOD-series), offcanvas focus return, combobox arrow-key operation (Choices.js defaults — verify).
- [ ] Color-contrast spot-check of badge palette in light AND dark (`-soft` backgrounds).

**Acceptance criteria:**
- Keyboard-only walkthrough of all 14 pages completes every primary task.
- Axe (or equivalent) scan on Dashboard, Offering, Profile, Reviews: zero critical/serious issues.
- EN+AR (screen-reader text must come from resx — CON1), light+dark, 390/1280, build clean.

**Commit:** `fix(guide): a11y pass — landmarks, labels, live regions, modal semantics, focus management (A11Y1-A11Y9, MOD2, NF5)`

---

## Phase 8 — Performance & polish

**Goal:** finish line: payload, perceived speed, consistency.

**Files:** `_GuideLayout.cshtml`, `Areas/Guide` page scripts, `Analytics` view/facade (optional chart upgrade), `Dashboard`/`Earnings` views.

**Checklist:**
- [ ] Verify `_GuideLayout` ships ZERO unused vendor JS (tiny-slider/glightbox/etc. gone since Phase 1); page scripts loaded only via `@section Scripts` per page (J2).
- [ ] **Optional — Analytics charts**: upgrade booking-trends + peak-days CSS bars to ApexCharts (X14 permits on dashboards): vendored bundle only, loaded only on Analytics via `@section Scripts`, `prefers-reduced-motion` respected (M3), RTL: verify Apex `rtl` option / axis label rendering in AR. Keep the CSS-bar markup as the no-JS fallback inside `<noscript>` or render-then-enhance (PE2).
- [ ] Skeleton coverage check: every listing.js surface has skeleton placeholders (L1); button spinners on all `data-loading` forms (L2).
- [ ] Image hygiene: avatar/cover use `.object-cover`/`.yj-avatar-96` utilities, explicit width/height attributes to avoid CLS.
- [ ] Confirm `GuideIdAccessor` session cache hit-rate (Phase 1) — no page issues >1 `/guides/me` call; `GuideAgencyRosterFacade` 4-way WhenAll verified.
- [ ] Dead-weight grep: no leftover `GuideSidebarVm`, `guide-agency-roster.js`, Tier/Invite view references anywhere.
- [ ] Final full-protocol run of §1.5 across all 14 pages, both cultures, both themes, both viewports.

**Acceptance criteria:**
- Network audit on Dashboard: no unused vendor JS, fonts self-hosted only, single `/guides/me`.
- Lighthouse (or equivalent) on Dashboard + MyTours: no regressions vs pre-plan baseline; CLS ≈ 0 on image-bearing pages.
- Build clean; RTL4 gate passed for the Apex upgrade if taken.

**Commit:** `perf(guide): trim dashboard payload, skeleton/spinner coverage, optional ApexCharts analytics (J2, L1-L2, M3, X14, PE2, RTL4)`

---

## Execution-order table

| Phase | Scope | Size | Dependencies / parallelization |
|---|---|---|---|
| 0 | Bugs & dead code | 1.5 d | None. Start immediately. |
| 1 | Shell: base controller, sidebar VC, layout, NoStore | 2 d | After 0. Blocks everything (all views change layout). |
| 2 | Design-system partials + CSS utilities | 2 d | After 1. Partial creation can start parallel to late Phase 1; adoption sweeps run with 3–5. |
| 3 | View reduction (Tier→Dashboard, Invite→Roster) | 1.5 d | After 2 (`_TierProgress` needed). |
| 4 | AJAX modernization | 3 d | After 2; per-page work parallelizable across two agents (listings vs Offering/Roster). |
| 5 | Conversion UX + ALL `[Backend]` (B1/B2/B3) | 3 d | **B1/B2 backend work can start any time after Phase 0 in parallel** (separate host); Web wiring needs Phases 2+4. Reviews rewrite needs Phase 2 partials. |
| 6 | l10n/RTL hardening | 2 d | After 4+5 (strings stabilize after AJAX/picker changes). |
| 7 | A11y pass | 1 d | After 6. |
| 8 | Perf & polish | 1 d | Last. |
| **Total** | | **~17 days** | Critical path 0→1→2→4→5(web)→6→7→8; backend B1/B2 and Phase 3 run off-path. |

---

## Appendix A — New resx key namespaces (grep both files before adding ANY key)

- `Guide.Flash.*` — controller flash messages (Phase 6)
- `Guide.Status.*` — status badge texts (Phase 2; some already exist via Earnings ~L21 — extend, don't duplicate)
- `Guide.Reviews.*` — Reviews rewrite (Phase 5)
- `Guide.Common.*` — pagination/empty-state/confirm strings (Phases 2/4; `Guide.Common.Previous` etc. already exist — grep)
- `Guide.Roster.RejectReason*` — reason modal (Phase 4)

## Appendix B — Deleted artifacts ledger

| Artifact | Deleted in | Replacement |
|---|---|---|
| `Views/Tier/Index.cshtml` + `GuideTierFacade` (if unreferenced) | Phase 3 | Dashboard `#tier` + `_TierProgress` |
| `Views/AgencyRoster/Invite.cshtml` | Phase 3 | Roster `#invite-guide` section |
| `Areas/Guide/Shared/_GuideSidebar.cshtml` + `GuideSidebarVm.cs` | Phase 1 | `GuideSidebarViewComponent` |
| `wwwroot/assets/js/guide-agency-roster.js` | Phase 4 | `guide-roster.js` + reason modal |
| `MyToursApiClient.UpdateScheduleAsync` / `UpdatePricingTierAsync` | Phase 0 | — (dead) |
| 12× `SetSidebar()` privates, 6× `ReloadAsync` divergences, 6× `StatusStyle` lambdas, 10× Money/Date helpers | Phases 1–2 | `GuideBaseController`, partials, `GuideFormat` |
