# YallaJo Public Area — Master Plan: Structure, AJAX & UI/UX (Agent Execution Spec)

> **Audience:** an autonomous coding agent executing this plan without prior conversation context.
> **Produced from:** a full audit of all 29 Public-area views, shared shell views, 13 controllers (68 actions), JS assets, existing AJAX mechanisms, and the backend API catalog (branch `master`), cross-checked against the binding rules contract `yallajo-plan/UI-UX-Design.md`.
> **Supersedes:** `public-area-ui-ux-enhancement-plan.md` and `public-area-view-consolidation-ajax-plan.md` (merged into this document).
> **Line numbers are indicative** (accurate at audit time) — always re-locate by content, never edit blindly by line number.
>
> The plan is organized in three parts:
> - **Part I — Foundations** (Phases 0–3): bug fixes, AJAX plumbing, shared shell, design system.
> - **Part II — Structure & AJAX** (Phases 4–7): retire the Search page, consolidate duplicated controllers, reduce views, AJAX-ify listings.
> - **Part III — Experience Polish** (Phases 8–11): conversion flows, localization/RTL, accessibility, performance & trust.

---

## 1. Context & Ground Rules

### 1.1 Project layout
- ASP.NET Core MVC modular monolith. Web frontend: `src/Hosts/YallaJo.Web` (calls the API via typed ApiClients/Facades — **never** touch `src/Hosts/YallaJo.Api` or `src/Modules` for this plan).
- Public storefront area: `src/Hosts/YallaJo.Web/Areas/Public/` with `Controllers/`, `Facades/`, `ApiClients/`, `Models/{Feature}/`, `Views/`.
- Shared shell: `src/Hosts/YallaJo.Web/Views/Shared/` (`_Layout.cshtml`, `_Navbar2.cshtml`, `_Alerts.cshtml`, …).
- Static assets: `src/Hosts/YallaJo.Web/wwwroot/assets/` (`css/style.css` = 543KB Webestica "Booking" Bootstrap 5.3 theme; `css/rtl.css` = 6.6KB; `js/` page scripts; `vendor/` libraries).

### 1.2 Existing conventions you MUST follow
- **Rules contract:** `yallajo-plan/UI-UX-Design.md` is the binding UI/UX rulebook (stable rule IDs like `UI-UX-L2`, `UI-PERF-V3`, `UI-UX-D3`). This plan cites those IDs; when in doubt, the rules doc wins — **after** the Phase 0.8 corrections below land (the doc has known factual drift).
- **Non-negotiable AJAX rules:** **R2/S1** (first paint SSR; AJAX = post-SSR refinement w/ pushState; back restores), **PE1** (everything works without JS — native form POST fallbacks), **JS5** (one shared apiClient; components never raw-fetch; browser talks only to MVC endpoints, never the API host), **SEC7** (antiforgery on all AJAX POSTs), **NF1/NF5** (toasts), **L1/L4/L5** (skeletons), **S2** (navbar autocomplete spec).
- **Localization:** `@inject IStringLocalizer<SharedResource> Localizer` (alias `L` in some views). Resource files: `src/Hosts/YallaJo.Web/Resources/SharedResource.en.resx` (3,846 keys) + `SharedResource.ar.resx`. Every new user-facing string gets a key in **both** files. Key style follows existing patterns like `Home.Featured.Title`.
- **SEO:** views call `ViewData.SetSeo(new SeoModel { ... })` — do not break this.
- **Dark mode:** Bootstrap 5.3 `data-bs-theme`, pre-paint via `assets/js/theme-bootstrap.js`. Use theme-aware utility classes, never hardcoded colors.
- **RTL:** `<html lang dir>` set from `CurrentUICulture`; `rtl.css` loaded only for RTL cultures. Arabic = first-class.
- **JS:** page scripts live in `wwwroot/assets/js/{page-name}.js`, loaded via `@section Scripts`. ES modules, `data-yj-component` self-init, idempotent (JS1–JS4). No new inline `<script>` blocks.
- **Vendored libs available:** bootstrap, tiny-slider, glightbox, flatpickr, choices, nouislider, bs-stepper, aos, splide, font-awesome, bootstrap-icons. **Do not add new dependencies.**
- Wishlist toggling: delegated handler in `assets/js/favorites.js` on `.js-favorite` buttons (auth-only).

### 1.3 Public controllers (the complete list — 13)
`Agencies, Blog, Booking, Contact, Culture, Directory, Guides, Help, Home, Packages, Places, Search, Tours`
There is **no** `TourGuides` and **no** `Businesses` controller in Public (they exist only in Provider/Admin areas respectively). This causes the broken links in Phase 0.

### 1.4 Current state (verified inventory — why Part II exists)
- **13 controllers, 68 actions, 29 views.** ~38 of those actions are near-identical review/accessibility-review/report POST→Redirect handlers duplicated across `ToursController`, `PlacesController`, `DirectoryController`, `GuidesController` — all delegating to the same `ReviewsFacade` / `AccessibilityReviewsFacade`.
- **7 JSON endpoints exist** in the whole Web host (Search×4: `suggest`/`businesses`/`nearby`/`map`; Wishlist toggle; Recommendations track; ResendOtp). No PartialView-returning AJAX endpoints exist.
- **No shared JS api client** — every script hand-rolls fetch + antiforgery header (`RequestVerificationToken`), violating JS5.
- **Backend search is underused:** `GET /api/v1/tours/search` accepts 16 params (priceMin/Max, difficulty, durationMinutesMin/Max, isChildFriendly, isAccessible, isInstantBooking, hasDiscount, minRating, sort, q, placeId, lang, page, pageSize) but the web only sends `q, placeId, page, pageSize`. `SearchFacade` has a TODO for from/to/participants.
- Slug routing: Blog/Guides/Places/Tours; ID routing: Agencies/Directory/Packages. All POSTs use `ValidateAntiForgeryToken` (except the blog view-beacon). `PublicOutputCacheTagger` tags detail pages for cache invalidation.

### 1.5 Verification protocol (run after EVERY phase)
1. `dotnet build` the solution — zero new warnings/errors.
2. Manual/Playwright pass of affected pages in: EN-LTR + AR-RTL, light + dark theme, mobile (390px) + desktop (1280px) viewports.
3. Check no `@Localizer` key renders as raw key text (missing resource).
4. Grep affected views: no new inline `style=` attributes, no new inline `<script>`.
5. For converted AJAX interactions: verify the no-JS path still works (disable JS, repeat the flow).

---

# PART I — FOUNDATIONS

## 2. Phase 0 — Bugs, Dead Code & Rules-Doc Sync (P0)

### 0.1 Fix broken navbar logo link
`Views/Shared/_Navbar2.cshtml` (~L14): `href="index.html"` is a theme leftover.
→ Replace with tag-helper link to Public Home (`asp-area="Public" asp-controller="Home" asp-action="Index"`).

### 0.2 Fix "List your experience" nav link
`_Navbar2.cshtml` (~L33-64): `asp-controller="TourGuides"` — controller doesn't exist in Public; it's in the **Provider** area.
→ Point to the correct area (`asp-area="Provider"`) or to an appropriate public landing/registration entry point. Verify the destination actually resolves for anonymous users; if Provider requires auth, link to the join/apply flow instead.

### 0.3 Fix Home businesses section link
`Areas/Public/Views/Home/Index.cshtml` (businesses section, `_HomeBusinessCard` block): `asp-controller="Businesses"` — Admin-only controller.
→ Change to `asp-controller="Directory"`.

### 0.4 Fix "FAQ" nav mislabel
`_Navbar2.cshtml`: item labeled "FAQ" links to `Agencies`.
→ Either relabel to a localized "Agencies" or repoint to `Help`. Decide by what the menu lacks (Help is missing entirely — prefer pointing FAQ → Help and adding Agencies under an Explore dropdown, see Phase 2.3).

### 0.5 Fix hardcoded avatar
`_Navbar2.cshtml` (~L91 and ~L100): `src="assets/images/avatar/01.jpg"` (also missing `~/` prefix).
→ Bind the authenticated user's real avatar URL; fallback to an initials avatar or default asset via `Url.Content("~/...")`. Check how other areas (Accounts) resolve the avatar and reuse that mechanism.

### 0.6 Delete dead code
- `Areas/Public/Views/Contact/Index2.cshtml` + `ContactController.Index2()` action + `wwwroot/assets/js/contact-index2.js` (nothing routes to Index2 — verified).
- `Views/Shared/_Navbar.cshtml` (only `_Navbar2` is rendered by `_Layout`).
- `Views/Shared/Components/NotificationBell/Default1.cshtml` (duplicate of `Default.cshtml`).
- Fix stale comment in `_Layout.cshtml` (~L14) that claims the navbar lives in `_Navbar.cshtml`.
- `ContactController.Index()` has an unnecessary `Task` wrapper (~L25) — simplify while you're there.

### 0.7 Fix wrong script reference
`Areas/Public/Views/Places/Index.cshtml` (~L206) loads `directory-index.js`. Verify intent; if Places needs its own behavior create/load the correct script, otherwise remove the reference.

### 0.8 Sync the rules doc with reality (`yallajo-plan/UI-UX-Design.md`)
The binding rulebook has factual drift — fix it first so later phases can cite it safely. **Docs-only change; this is the one allowed exception to the "Public area only" guardrail.**
- **Areas list:** doc says 9 real areas incl. `Content` — reality is 8 (`Accounts, Admin, Auth, Business, Creator, Guide, Provider, Public`). Remove `Content`, fix the count.
- **Shared partials list:** references `_Navbar.cshtml` — the active partial is `_Navbar2.cshtml` (`_Navbar` is deleted in 0.6). Update the name.
- **§2 RTL stylesheet rule:** says "Load `style.rtl.css` (swap, not append)" — actual assets are `style.css` + appended `rtl.css`; no `style.rtl.css` exists. Reword to match reality, or keep the swap approach explicitly marked 🎯 target with a migration note.
- **Status legend:** add near the top — `✅ enforced today / 🎯 target (no CI gate yet)` — and mark currently-aspirational CI gates (CON1 resx grep gate, UI-PERF-M3 Lighthouse CI, M5 bundle-size check, TEST3 axe-core CI) as 🎯 until the gates exist.
- **Conditional language** ("when the backend ships X") for REV3–REV5, CAL7, and SEC1's `/api/v1/csp-report` — same treatment rule F5 already has.
- **Change log:** append real-dated rows (not `init`) for each correction above.

**Acceptance:** all nav/footer/home links resolve with HTTP 200 for anonymous users; deleted files leave no dangling references (`grep` for `Index2`, `_Navbar"`, `Default1`); rules doc contains no references to nonexistent files/areas.

---

## 3. Phase 1 — AJAX Foundation: apiClient, form-ux, Partial-Response Convention (P0)

**Goal:** one blessed way to do AJAX and one blessed way to give form feedback, before anything else is converted. Everything in Part II depends on this phase.

### 1.1 `wwwroot/assets/js/api-client.js` (ES module, rules JS1/JS5/JS6)
- Exports `apiGet(url, {signal})`, `apiPost(url, body|FormData, {signal})`, `apiPostForm(form)` and `loadPartial(url, {signal}) → html string`.
- Behavior: attaches `RequestVerificationToken` header from `input[name="__RequestVerificationToken"]` (SEC7); `Accept: application/json` or `text/html` per call; default timeout via `AbortSignal.timeout(10000)`; on `401` → redirect to sign-in with `returnUrl`; on non-OK → throw typed error `{status, message}` consumed by the caller's toast (NF1/NF2).
- **Antiforgery for anonymous users:** `_Layout.cshtml` currently injects the antiforgery hidden input only when authenticated (~L276). Make the injection unconditional — it is cheap and removes a whole class of bugs for anonymous AJAX.
- Migrate `favorites.js` and `search-index.js` to import from `api-client.js` as the proof. Do NOT rewrite their logic.

### 1.2 `wwwroot/assets/js/form-ux.js` (loaded from `_Layout` site-wide)
- On any `form[data-loading]` submit: disable submit button, swap label to spinner (`spinner-border spinner-border-sm`) + localized "Please wait…" from `data-loading-text`, prevent double submit (rules UI-UX-L2/F7).
- Toast helper for AJAX success/error (used by favorites, helpful votes, comments) — per NF1/NF4/NF5: bottom-right, max 3 visible, 5s auto-dismiss, queued + deduped (2s window), container has `role="status" aria-live="polite"`. PRG flash messages keep using `_Alerts`.
- Shared confirm-modal trigger via `data-confirm` attributes (full spec in Phase 10.5 — implement the mechanism here, roll out usage there).
- Apply `data-loading` to: Booking/Book submit (~L219), Contact form, review/report/comment forms, apply-as-guide form (Agencies/Detail ~L80-104), sort form submits.

### 1.3 Partial-response convention (server side)
- New helper on the Public base controller (or extension): `bool WantsAjax()` — true when `X-Requested-With: fetch` header or `Accept: text/html-partial` is present. `BlogController` already has a `WantsNoContent()` precedent; generalize, don't duplicate.
- Convention: every converted POST/GET action returns
  - AJAX → `PartialView("_X", vm)` (for HTML regions) or `Json(...)` (for counts/toggles),
  - non-AJAX → existing `Redirect` (PRG) or full `View` unchanged. **PE1 satisfied by keeping the native path.**
- Localized HTML always rendered server-side (CON1) — never build HTML strings in JS.

**Acceptance:** `dotnet build` clean; favorites + search suggest still work through the new client; no script except `api-client.js` contains a raw `fetch(` (CI-greppable); double-submit impossible on forms carrying `data-loading`.

---

## 4. Phase 2 — Shared Shell: Footer, Mobile Nav, Navbar + Search Overlay, Arabic Font (P0)

### 2.1 Rebuild footer (`_Layout.cshtml` ~L169-221)
Current footer is **account-centric** (logo → Accounts/Profile; columns "Company" = My Profile/Sessions/Devices, "Account" = Change Password/Update Phone; hardcoded English) — wrong for a public travel storefront.
Replace with:
- Logo → Public Home.
- Column **Explore**: Tours, Places (Destinations), Packages, Guides, Directory (Businesses).
- Column **Company**: Blog (Stories), Agencies, Contact.
- Column **Support**: Help Center, FAQ (Help), Privacy/Terms placeholders (link `#` with TODO comment if pages don't exist yet).
- Tagline + copyright localized; social icon row (bootstrap-icons); keep it theme-consistent (`bg-dark`/theme classes already in use).
- All strings via `Localizer` with new keys (e.g., `Footer.Explore`, `Footer.Support`, …) added to **both** resx files.

### 2.2 Rebuild mobile bottom nav (`_Layout.cshtml` ~L228-258)
Currently rendered only when authenticated, with account items and a mislabeled "Home" → Accounts/Profile.
→ Make it public for all users per rule **UI-UX-D3** (≤768px, `fixed-bottom d-md-none`, hidden on auth screens, touch targets ≥44×44px per D2), with D3's five slots: **Browse** (Tours), **Search** (opens the 2.4 search overlay; JS-off fallback = link to `/tours`), **Wishlist** (auth-gated — current `.js-favorite` behavior preserved; guests → sign-in per WL1), **Bookings** (My Bookings when authed / sign-in redirect when not), **Profile** (Profile when authed / Sign-in when not). Localized labels, active-state via current controller, bootstrap-icons.
Do NOT link to `Search/Index` — that page is retired in Phase 4.

### 2.3 Navbar (`_Navbar2.cshtml`)
- Localize **every** label via the already-injected localizer (`L`): Explore, Destinations, Stories, Menu, Sign In, My Profile, My Bookings, Wishlist, Settings, Sign Out, Mode, tooltips Light/Dark/Auto, etc. (Currently 0% localized despite injector being present.)
- Add a **Register/Sign-up CTA** next to Sign In for anonymous users (primary button = Register, secondary = Sign In).
- Add missing destinations without crowding: convert "Explore" into a dropdown (Tours, Packages, Guides, Directory) or add a secondary "More" dropdown (Help, Contact, Agencies). Keep top-level ≤ 5 items.
- Keep existing role-gated dashboard links, dark-mode switcher, language toggle exactly as they work today.

### 2.4 Navbar search overlay (replaces the standalone Search page)
- Add an icon button (`bi-search`, ≥44px touch target, localized `aria-label`, visible at all breakpoints) that opens a **full-width search overlay** (Bootstrap collapse/offcanvas-top; on ≤768px it becomes a full-screen sheet per MOD6 spirit).
- Overlay contents: search input (autofocus on open, `Esc` closes — A11Y8), 300ms-debounced autocomplete via existing `GET /search/suggest` (S2: top-5, categorized Tours/Places/Businesses once Phase 4.2 lands, "View all results" link), recent-searches chips from `localStorage` (cap 5), popular categories fallback when empty (S5).
- New module `wwwroot/assets/js/search-overlay.js` (`data-yj-component="search-overlay"`, idempotent init JS4, uses apiClient). Loaded site-wide from `_Layout` with `defer` — keep it < 5KB.
- **PE1 fallback:** the overlay is a real `<form method="get" action="/tours">` with `name="q"` — JS-off users submit and land on Tours with the query applied.

### 2.5 Move inline styles out of `_Layout`
`_Layout.cshtml` ~L94-152 contains an inline `<style>` block (`.lang-toggle` styles + `[dir=rtl]` dropdown/notification-badge fixes).
→ Move lang-toggle styles into the site CSS (create `wwwroot/assets/css/site.css` if none exists and reference it after `style.css`), and RTL-specific rules into `rtl.css`.

### 2.6 Web fonts — self-host, add Arabic (rules UI-PERF-V3/V4/A5/X10)
`_Layout` (~L70-72) currently preconnects to and loads DM Sans + Poppins from the **Google Fonts CDN — forbidden by rule V3** (privacy/perf/PDPL); Arabic falls back to system fonts.
→ Self-host everything in `wwwroot/fonts/`:
- Download woff2 subsets (V4: Latin subsets of DM Sans + Poppins; Arabic subset of **IBM Plex Sans Arabic** or Noto Kufi Arabic — pick one, ~30KB target).
- `@font-face` rules in site CSS with `font-display: swap` (X10 forbids `block`).
- **Remove** the `fonts.googleapis.com`/`gstatic` preconnect + stylesheet `<link>`s from `_Layout` (A5 allows preconnect only to the API + Mapbox).
- In `rtl.css`, set the AR `font-family` stack to prefer the Arabic family. Verify headings + body render with it in AR.

**Acceptance:** footer/mobile-nav/navbar fully localized (switch to AR and verify no English leaks); all links resolve; mobile nav visible logged-out; search overlay opens/closes on every page (EN/AR, RTL, dark, 390px) and degrades to a plain GET form; Arabic font visibly applied in AR; **zero requests to `fonts.googleapis.com`/`fonts.gstatic.com`** in the network tab.

---

## 5. Phase 3 — Design System Consolidation (P1)

> Do this phase **before** Parts II–III — the partials reduce every later change's surface area.

### 3.1 Extract shared partials → `Areas/Public/Views/Shared/`
Create with `@model` viewmodels (add small dedicated VMs under `Areas/Public/Models/Shared/` if needed):

| Partial | Replaces duplication in | Notes |
|---|---|---|
| `_TourCard.cshtml` | Home (`_HomeTourCard`), Tours/Index ~L116-206 | unify image height, badges, rating, wishlist btn |
| `_PlaceCard.cshtml` | Home (`_HomePlaceCard`), Places/Index ~L99-153 | give Places cards the same badge treatment as Tours |
| `_BusinessCard.cshtml` | Home (`_HomeBusinessCard`), Directory/Index ~L112-183, Places/Details businesses section | |
| `_Pagination.cshtml` | Tours/Index ~L216-248, Places/Index ~L156-197, Directory/Index ~L185-200, Blog, Agencies, Packages | one full-numbers style; localized Previous/Next; `aria-label` on nav + links |
| `_EmptyState.cshtml` | Tours ~L103-113, Places ~L77-96, Directory ~L102-109, detail-page subsections | params: icon/svg, title, message, CTA (rule L6: empty states always offer a CTA); standardize on the `element/17.svg` style |
| `_ReviewForm.cshtml` + `_ReportForm.cshtml` | Tours/Detail ~L644-695, Places/Details ~L418-483, Guides/Detail ~L176-239, Blog/Post report ~L267-299 | keep antiforgery + collapse behavior; design the form fields now, point `action` at the unified routes when Phase 5 lands (take `targetType`/`targetId`/`returnUrl` as parameters from day one) |
| `_StarRating.cshtml` | `_Reviews.cshtml` `Html.Raw()` inline SVGs (~L19-30, 38, 54) and every card rating row | params: value, count, size; `aria-label` "X out of 5" |
| `_WishlistButton.cshtml` | 5 different stylings across Tours/Places/Directory/Home cards + detail headers | one style: `btn btn-sm btn-light rounded-circle js-favorite` + `aria-label` + `aria-pressed`; behavior per WL1–WL3/NF6: guest → disabled heart + tooltip "Sign in to save" (link to sign-in with return URL); optimistic toggle with revert + toast on failure; remove-toast offers [Undo] — implement what `favorites.js` supports today, TODO the rest |
| `_CommentThread.cshtml` | Blog/Post ~L116-254 nested comments | recursive or two-level partial; will also be the AJAX refresh payload in Phase 5.4 |

### 3.2 CSS utilities (add to site CSS, then **replace all inline styles**)
```css
.text-truncate-2 / .text-truncate-3   /* -webkit-line-clamp */
.preserve-whitespace                  /* white-space: pre-line */
.object-cover                         /* object-fit: cover */
.avatar-48 .avatar-56 .avatar-72 .avatar-96
.card-img-h220 .hero-img-h360 .hero-img-h420 .thumb-h90 .thumb-h185 .thumb-h204
```
Known inline-style hotspots to convert: Tours/Index ~L126; Tours/Detail ~L127/137/206/465; Places/Details hero/thumbs; Directory/Index hero `background-image` (keep as inline background only if URL is dynamic — otherwise class); Directory/Detail ~L91-123; Booking/Book ~L51/55; Booking/Confirmation ~L17/24/38; Packages ~L46/71; Blog ~L33/37/52/81/131/203; Help ~L23/50; Contact ~L68; Agencies ~L28/41/50/51; Guides ~L35/42/72/97; Home feature icons 48px circles.

### 3.3 Consistency sweep
- Cards: `card card-hover-shadow h-100` everywhere (Directory uses `card shadow`; sponsored recommendations use `card border`).
- Grid density: Directory 4-col `col-xl-3` → match Tours/Places `col-md-6 col-xl-4`.
- One date format across pages (pick the `d MMM yyyy` style used in Tours/Detail; respect culture).
- Review action buttons: one hierarchy (`btn-outline-secondary` for secondary actions).
- Directory "Page X of Y" pagination → `_Pagination`.

**Acceptance:** `grep -r "style=" Areas/Public/Views` returns only justified dynamic cases (rule UI-PERF-X6: inline `style=` only for dynamic computed values, each documented with a code comment); all listing pages render identically to before except standardized details; build green.

---

# PART II — STRUCTURE & AJAX

## 6. Phase 4 — Retire the Search Page; Tours Becomes the Search Surface (P1)

**Goal (explicit product decision):** no standalone Search page; search lives in the navbar overlay (Phase 2.4), results live on Tours.

### 4.1 Retire `Search/Index.cshtml`
- `/search` becomes a **301 redirect** to `/tours?q={q}&placeId={placeId}` (preserve page). Keep the route alive for old links/SEO; delete the view + its bespoke results markup. Delete `search-index.js` after 4.3 absorbs its useful parts.
- Keep `SearchController` as the **JSON-only search gateway**: `Suggest`, `Businesses`, `Nearby`, `Map` stay (they already match the consolidation goal — one controller fronting several API endpoints). Do not move them.

### 4.2 Wire the unused backend filter params
- Wire the **12 unused backend filter params** through `SearchFacade`/`ToursFacade` → `ToursController.Index` (price range, difficulty, duration, child-friendly, accessible, instant-booking, has-discount, minRating, sort). Backend (`GET /api/v1/tours/search`) already supports them — facade work only; if a param turns out unsupported end-to-end, mark TODO, don't fake it.
- Date-range/participants from the home hero stay as URL intent until the `SearchFacade` TODO is resolved; either keep the params with no fake filtering or drop them from the hero form (default: keep params, add code comment).

### 4.3 Filter UI on Tours index
- **Filter offcanvas** per rules D4/S3: price range (nouislider — already vendored), category select, min-rating, difficulty, duration, toggles (child-friendly / accessible / instant-booking / has-discount), date range (flatpickr, only once supported per 4.2). On mobile render as bottom-sheet offcanvas with a sticky Apply button and an active-filter count badge on the trigger; active-filter chips above results (reuse/extend the chip markup that existed on the old Search page).
- Submit as GET params (PE1) — the offcanvas wraps a real form.
- Sort dropdown maps to backend sorts: Relevance | Price | Rating | Popularity | Newest (S6).

### 4.4 AJAX refinement on Tours index (S1)
- Extract the results grid + pagination into `_TourResults.cshtml`; `ToursController.Index` returns it when `WantsAjax()` (Phase 1.3 convention).
- Filter/sort/page changes fetch via apiClient, swap the grid, update URL via `history.pushState`, restore on `popstate`. Skeleton cards while loading (L1/L4/L5: 4-6 Bootstrap `placeholder` cards mimicking real card layout, shown within 100ms), `aria-busy` + `aria-live="polite"` results count.
- Every fetch has error handling → localized inline alert + retry (NF2). No raw `fetch` outside apiClient.
- "Nearby" and map view: port `search/nearby` + `search/map` consumption from the old search page into an optional map toggle on Tours index (PE2: text list fallback; `aria-pressed` on the toggle). If the old map code path never actually worked, **hide the toggle** behind a feature-flag comment instead — do not block the phase.

**Acceptance:** navbar search works on every page; `/search?q=petra` 301s to `/tours?q=petra`; Tours index filters actually filter (against real backend params) and round-trip without full reload but ALSO work with JS disabled (plain GET form); back button restores previous results state; view count −1.

---

## 7. Phase 5 — Consolidate ~38 Duplicated Review Actions into One Controller (P1)

**Goal:** one `ReviewsController` (Public area) replaces the review/accessibility/report action copies in Tours/Places/Directory/Guides. Action count drops ~68 → ~35.

### 5.1 New `Areas/Public/Controllers/ReviewsController.cs`
Routes keyed by target type (matches the Social API shape `reviews/{targetType}/{targetId}`):
```
POST reviews/{targetType}/{targetId}                  → CreateReview
POST reviews/{targetType}/{targetId}/edit             → EditReview
POST reviews/{targetType}/{targetId}/delete           → DeleteReview
POST reviews/{reviewId:guid}/helpful                  → MarkHelpful
POST reviews/{reviewId:guid}/helpful/remove           → UnmarkHelpful
POST reviews/{reviewId:guid}/report                   → ReportReview
POST reviews/{targetType}/{targetId}/accessibility            → CreateAccessibilityReview
POST reviews/{targetType}/{targetId}/accessibility/edit       → EditAccessibilityReview
POST reviews/{targetType}/{targetId}/accessibility/delete     → DeleteAccessibilityReview
GET  reviews/{targetType}/{targetId}/list?page=               → List (returns _Reviews partial; powers AJAX refresh + pagination)
```
- `targetType` = constrained route value (`tour|place|business|guide`) mapped to the existing facade target-type values. Validate strictly; 404 on unknown.
- Each POST takes a `returnUrl` (validated `Url.IsLocalUrl`) for the PRG fallback; AJAX requests get the refreshed `_Reviews`/`_AccessibilityReviews` partial back (Phase 1.3 convention) so the page updates in place.
- Copy the *most complete* existing implementation (ToursController's set) as the base; the four controllers' bodies are interchangeable apart from redirect targets — verify with a diff before deleting.

### 5.2 Migration
- Update `_Reviews.cshtml` / `_AccessibilityReviews.cshtml` and the Phase 3 `_ReviewForm`/`_ReportForm` partials to post to the new unified routes with `targetType`/`targetId`/`returnUrl` hidden fields.
- Delete the ~38 superseded actions from Tours/Places/Directory/Guides controllers **only after** all four detail pages are verified against the new routes (create/edit/delete/helpful/report, EN+AR, signed-in/out). Tours keeps `RequestJoin`; Places keeps `SponsoredClick`.
- Old POST routes can vanish (they were form targets, not link destinations — no SEO concern).

### 5.3 AJAX-ify review interactions (uses 5.1's partial responses)
- Helpful vote: optimistic count bump, revert + toast on failure (NF6/MOD12 — reversible, no confirm).
- Create/edit: submit via `apiPostForm`, swap the reviews region with returned partial, success toast (NF1), button spinner (F7/L2).
- Delete: confirm modal (F8, mechanism from Phase 1.2) → AJAX → region swap.

### 5.4 Blog interactions (same pattern, stays in BlogController)
Blog comments/reactions/follow are blog-specific, not duplicated — don't move them; convert them:
- `ReactComment`/`UnreactComment`/`Follow`/`UnfollowCreator` → optimistic AJAX with apiClient + revert-on-fail toast.
- Comment create/edit/delete → AJAX returning the `_CommentThread` partial; PRG fallback intact; delete via confirm modal.

**Acceptance:** all four detail pages' review flows work JS-on and JS-off; `git grep "CreateReview" -- "*Tours*" "*Places*" "*Directory*" "*Guides*"` returns nothing under Controllers; blog comments/reactions update without reload.

---

## 8. Phase 6 — View Reduction (P1)

| View | Action | Net |
|---|---|---|
| `Contact/Index2.cshtml` | Deleted in Phase 0.6 (dead) | −1 |
| `Search/Index.cshtml` | Deleted in Phase 4.1 | −1 |
| `Help/Detail.cshtml` | Merge into `Help/Index`: FAQ accordion items get `id="faq-{id}"`; `help/{id:guid}` route remains but redirects to `help#faq-{id}`. Delete view; keep the facade method. | −1 |
| `Blog/Creator.cshtml` | **Keep** — distinct SEO entity (`creators/{slug}`, own canonical/JSON-LD). Reduce its markup via Phase 3 partials instead. | 0 |
| Tours/Places/Directory/Guides/Packages/Agencies `Index` views | **Keep routes & views** (SEO needs distinct URLs/canonicals) but converge them on shared structure: `_FilterBar`-style header, card partials, `_Pagination`, `_EmptyState` (Phase 3). Each Index view shrinks to ~40 lines of composition. | 0 (lines −60%) |
| `Booking/Confirmation`, `Home/Error` | Keep | 0 |

Result: **29 → 26 views**, with the six listing views reduced to thin compositions. Do not merge listing pages into one mega-route — slug-distinct URLs are an SEO requirement (UI-UX-Design.md §3).

**Acceptance:** build clean; `help/{guid}` redirects and the accordion deep-link opens + scrolls; no references to deleted files.

---

## 9. Phase 7 — AJAX Pagination & Filters on All Listing Pages (P1)

After Phase 4's `_TourResults` pattern proves out, replicate on Places, Directory, Guides, Blog, Agencies, Packages indexes:
- Extract each grid + pagination into a `_XResults.cshtml` partial; the Index action returns it when `WantsAjax()`.
- One generic module `wwwroot/assets/js/listing.js` (`data-yj-component="listing"`) handles: intercept pagination/filter clicks → `loadPartial` → swap → `pushState` → skeletons → `aria-live` count. Per-page JS deleted where it only did this.
- Native links remain real `<a href>`/GET forms (PE1); OutputCache policies unchanged (partial responses share the same cache tags via `PublicOutputCacheTagger`).

**Acceptance:** paging on all six listing pages without full reload; back button restores previous page state (S1); JS-off paging still works; CLS unchanged or better.

---

# PART III — EXPERIENCE POLISH

## 10. Phase 8 — Conversion Flow UX (P1)

### 8.1 Booking page (`Areas/Public/Views/Booking/Book.cshtml` + `assets/js/booking-book.js`)
- Add progress indicator at top: 3 steps (Choose slot → Your details → Confirmation), bs-stepper visual or simple custom — current page = steps 1-2, confirmation page = step 3.
- Client-side validation before submit: slot selected (show message near slot group, `aria-live`), participants ≥ 1 and ≤ available seats (read seats from existing data attrs `booking-book.js` already uses).
- Bootstrap `is-invalid` + `invalid-feedback` pattern; keep server validation as source of truth.
- Required indicator (`*`) on slot section + travelers; `aria-label` on the form (~L34).
- Double-submit protection via Phase 1.2 helper (`data-loading`).

### 8.2 Confirmation page (`Areas/Public/Views/Booking/Confirmation.cshtml`)
- Copy-to-clipboard button next to booking reference (~L29) with toast feedback.
- "A confirmation email has been sent to {email}" notice (only if the backend actually sends one — check `BookingController`/Facade; if not, omit and leave TODO).
- "What's next" list (find your booking under My Bookings, contact support link).
- Payment-pending alert (~L113): add expiry countdown if expiry timestamp is available in the VM; highlight refund amount (~L106) with `fw-bold text-success`.
- Localize everything (title "Booking confirmation" ~L3 and all labels are hardcoded EN).
- `role="status"`/`aria-live` on the status alerts; `aria-label` on the status icon (~L14-30).

### 8.3 Packages (`Packages/Index.cshtml`, `Packages/Detail.cshtml`)
- Index cards: add primary CTA button (localized "View package" is fine, but card needs a visible action; keep stretched-link with `aria-label`).
- Detail: add a **booking path**. Check `PackagesController`/Facade for any booking capability; if none exists, add a prominent "Inquire / Contact us" CTA linking to Contact with `?subject=Package: {name}` prefill, plus per-included-tour "Book this tour" links. Do NOT invent backend booking.
- Detail: add `_Reviews` section only if the facade exposes package reviews; otherwise skip (no fake sections).
- Index: server-side sort dropdown matching the Tours pattern if the facade supports ordering; otherwise skip.

### 8.4 Listing filters beyond Tours
- `Places/Index.cshtml` (~L30-66): city/country are free-text **exact-match** inputs — worst filter UX. Replace with selects or Choices.js autocomplete populated from the VM (check `PlacesFacade` for available city/country lists; if absent, populate from current page data and add TODO).
- Add active-filter chips after submit (reuse the Tours chips markup from Phase 4.3).
- Fix checkbox vertical alignment (`mt-4` hack ~L51) with proper `form-check` alignment.

### 8.5 Detail page improvements
- `Tours/Detail.cshtml`: sync active tab to URL hash (`#itinerary` etc.) on click + restore on load (small addition to `tours-detail.js`); add `role="tablist"`/`role="tab"`/`aria-controls`; loading + error fallback UI for the SignalR slots widget (`data-yj-component="tour-slots"`, ~L542-564) — show skeleton while connecting, localized "Live availability unavailable" on failure (PE2).
- `Directory/Detail.cshtml` opening hours (~L294-319): highlight current day + "Open now / Closed" badge computed client-side from rendered hours (new small block in `directory-detail.js`); localize day names via culture-aware rendering from the VM.
- Weather cards (Places/Details ~L142-177, Directory/Detail ~L229-266): staleness note exists — keep; do NOT add refresh button unless an endpoint exists (check facades; otherwise skip).

**Acceptance:** booking flow has visible step indicator, client validation, and no double-submit; packages have a clear next action; AR + EN verified.

---

## 11. Phase 9 — Localization & RTL (P1)

### 9.1 Hardcoded English elimination (~50 strings)
Add keys to **both** `SharedResource.en.resx` and `SharedResource.ar.resx` (provide real Arabic translations, not machine-garbage; follow existing key naming). Worst offenders, in priority order:

| File | Count | Examples (line refs indicative) |
|---|---|---|
| `Views/Help/Index.cshtml` | 13 | "How can we help you?" L12, "Frequently asked questions" L53, "Still need help?" L82, "Submit a ticket" L96/102, "Read full article" L68 |
| `Views/Shared/_AccessibilityReviews.cshtml` | 6+ | "Accessibility reviews" L36, "Edit your review" L84, "Delete your review" L130, "Write an accessibility review" L147, L168, L171 — **plus feature-type names** (L107): localize via key-per-enum-value pattern |
| `Views/Help/Detail.cshtml` | 5 | merged into Help/Index in Phase 6 — localize the merged accordion instead |
| `Views/Guides/Detail.cshtml` | 4 | "reviews" L46, "yrs experience" L70, "specialties" L82, "yrs" L138 |
| `Booking/Confirmation.cshtml` | many | whole page (see 8.2) |
| `Home/Index.cshtml` | many | hero copy, features section, trust card (see also Phase 11.3) |
| `_Navbar2.cshtml` + footer + mobile nav | all | covered by Phase 2 |
| `Home/Error.cshtml` | 2 | "Back to home" L23, "Contact support" L24 |
| `Blog/Post.cshtml` | 2 | "Sign in to comment" L107, "to contact our support team." L121 |
| `Agencies/Detail.cshtml` | 2 | "active guide"/"guides" L33 (use plural-aware keys) |
| Pagination partial | 2 | Previous/Next (covered by 3.1) |

### 9.2 RTL hardening
- Directional icons: add CSS in `rtl.css` — `[dir="rtl"] .fa-angle-left/right`, `.bi-arrow-left/right` → `transform: scaleX(-1)` (or swap classes conditionally in the new partials).
- Audit `me-*`/`ms-*` spacing in Public views — Bootstrap 5 logical properties handle RTL automatically, so only fix cases where a *physical* direction was intended.
- Verify vendored widgets in AR: flatpickr (use its `locale` + RTL support), Choices.js dropdown alignment, tiny-slider/splide direction, the Phase 2.4 search overlay, the Phase 4.3 filter offcanvas.
- Coordinates display (Places/Details ~L136): wrap in `dir="ltr"` span so lat/long don't reorder in RTL (RTL3).
- Grow `rtl.css` as needed; keep all RTL fixes there (not inline).

**Acceptance:** full AR walkthrough of every Public page shows zero English (except brand names), correct icon directions, correctly aligned dropdowns/datepickers/overlays.

---

## 12. Phase 10 — Accessibility (P1-P2)

1. **Icon-only buttons** → `aria-label` (localized): wishlist buttons, navbar search button, search submit (Home hero ~L101-180), gallery controls, status icons (Confirmation), copy button.
2. **Live regions**: `aria-live="polite"` on results counters (Tours ~L50-52, Places ~L69-75, Directory ~L87-100), booking price summary (~Book L188-257), SignalR availability widget, `_Alerts.cshtml` (`role="alert"`), Error page message (`role="alert"`), all Phase 4/7 AJAX-swapped regions.
3. **Tabs**: Tours/Detail nav-pills → proper `role="tablist"/"tab"/"tabpanel"` + `aria-controls` + `aria-selected` (Bootstrap supports this; just add attributes).
4. **Forms**: `fieldset`+`legend` for accessibility-feature checkbox groups (`_AccessibilityReviews` ~L143-189) and slot radio group (Book ~L99-106); `aria-label` on bare textareas (Blog/Post ~L96/292, Guides/Detail ~L204/231); `aria-describedby` linking inputs to help text; client-side "select at least one feature" validation message with `aria-live`.
5. **Replace `onsubmit="return confirm(...)"`** deletes (`_Reviews`, `_AccessibilityReviews`, Blog comments) with the shared localized Bootstrap confirm modal (one modal in `_Layout`, triggered via `data-confirm` attributes — mechanism built in Phase 1.2). Modal per F8/MOD3/MOD5: title is a question, body states consequences, the SAFE action is autofocused (never the destructive one), `data-bs-backdrop="static"` for destructive confirms (Esc still allowed).
6. **Toggles**: `aria-pressed` on list/map toggle; `aria-disabled` + tooltip on disabled contact submit (Contact ~L118); `aria-expanded` already OK on collapses — verify (incl. search overlay trigger).
7. **Images**: meaningful `alt` on content images (tour/place names); `alt=""` + `aria-hidden="true"` on decorative ones.
8. Keyboard pass: glightbox galleries, custom participants counter dropdown (Home hero), navbar dropdowns, search overlay (focus trap while open, focus returns to trigger on close).

**Acceptance:** axe-core scan (Playwright) on Home, Tours Index/Detail, Booking, Help shows no critical/serious violations.

---

## 13. Phase 11 — Performance & Trust Polish (P2)

1. **Lazy loading** (rules A7/IMG3): `loading="lazy"` on all below-the-fold imgs (cards, galleries, thumbnails). `Guides/Index.cshtml` ~L42 is the existing model. Hero/LCP images must NOT be lazy — use `loading="eager" fetchpriority="high"`.
2. **CLS prevention** (rules I3/IMG4): explicit `width`/`height` attrs or aspect-ratio CSS classes on card/gallery images (pairs with 3.2 height utilities).
3. **Honest trust signals** on `Home/Index.cshtml`:
   - Remove/replace "Trusted by 1M+ travelers" + fake avatar pile (avatar/01-04.jpg) and static "4.9/5.0 From 12,500+ reviews" — bind real aggregate stats from `HomeFacade` if available; otherwise use honest qualitative copy ("Hand-picked local guides").
   - Replace stock hotel-theme imagery (`bg/06.jpg`, `category/hotel/4by3/11.jpg`, `12.jpg`) with Jordan-relevant images from existing assets if present (Petra/Wadi Rum/Dead Sea); otherwise leave a TODO with asset list for the team.
   - Verify "Watch our story" YouTube embed (`tXHviS-4ygo`) is real; if placeholder, remove the button.
4. **Hero search honesty**: date-range + participants currently travel as URL params but don't filter (comment in code admits it). Resolved by Phase 4.2 if the facade TODO lands; otherwise visually de-emphasize/remove the non-functional fields until supported.
5. (Stretch) Theme CSS trim: 543KB `style.css` — investigate PurgeCSS against Razor views as a separate task; do not do this casually.

---

## 14. Execution Order, Sizing & Hard Rules

| Order | Phase | Est. | Dependencies |
|---|---|---|---|
| 1 | Phase 0 — bugs/dead code/rules-doc sync | 2 d | none |
| 2 | Phase 1 — AJAX foundation | 2 d | none |
| 3 | Phase 2 — shell + search overlay + fonts | 4 d | 1 (overlay uses apiClient) |
| 4 | Phase 3 — design system | 4-5 d | none (parallel w/ 2) |
| 5 | Phase 4 — retire Search, Tours-as-search | 4-5 d | 1, 3 |
| 6 | Phase 5 — ReviewsController + AJAX reviews | 4 d | 1, 3 (_ReviewForm) |
| 7 | Phase 6 — view reduction | 1-2 d | 4 (Search gone) |
| 8 | Phase 7 — listing AJAX rollout | 3 d | 4 (pattern proven) |
| 9 | Phase 8 — conversion UX | 4-5 d | 1, 3 |
| 10 | Phase 9 — l10n/RTL | 3-4 d | parallel w/ 8 |
| 11 | Phase 10 — a11y | 3 d | after 3 |
| 12 | Phase 11 — perf/trust | 2-3 d | last |

Roughly 6–7 working weeks solo; Phases 2∥3, 8∥9 parallelize for a pair.

**Hard rules for the executing agent:**
- Comply with `yallajo-plan/UI-UX-Design.md` rule IDs and cite them in commit messages where applicable (e.g. `feat(public): self-host fonts per UI-PERF-V3`). On any conflict between this plan and the rules doc, the rules doc wins — after the 0.8 corrections land.
- Never modify `YallaJo.Api`, `src/Modules`, or non-Public areas (except the shared shell files explicitly listed and the docs-only 0.8 fix).
- Never call the API host from the browser; all AJAX targets are Web MVC endpoints (JS5).
- Every converted interaction keeps a working no-JS path (PE1) — if you can't keep it, don't convert it.
- Never invent backend capabilities — check the relevant Facade → ApiClient → API endpoint chain first; if missing, add a visible `// TODO(backend)` comment and skip the UI affordance.
- Every new string → both resx files. Every change verified in AR-RTL + dark mode (per §1.5 protocol).
- Prefer editing existing files; new files only as specified (partials, `site.css`, `api-client.js`, `form-ux.js`, `search-overlay.js`, `listing.js`, `ReviewsController`).
- Commit per phase (or per logical task within a phase) with conventional messages, e.g. `fix(public): repair broken navbar links`, `refactor(public): unify review actions into ReviewsController`.
