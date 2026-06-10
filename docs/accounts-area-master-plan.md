# Accounts Area Master Plan — UI/UX Enhancement, View Consolidation & AJAX Modernization

> **Audience:** an execution agent with zero conversation context.
> **Scope:** `src/Hosts/YallaJo.Web/Areas/Accounts` (the authenticated Customer/Tourist dashboard, `/accounts/*`) + explicitly listed `[Backend]` additions in `src/Hosts/YallaJo.Api` / `src/Modules`.
> **Produced from:** a code-verified inventory of all 18 controllers / 56 actions / 24 views, a per-view quality audit, and a full View→Controller→Facade→ApiClient→API chain map — cross-checked against `yallajo-plan/UI-UX-Design.md` (binding rules) and `yallajo-plan/2-customer-dashboard.md` (area scope doc).
> All line numbers are **indicative** (~L123): re-verify before editing.

---

## §1 Context & Ground Rules

### 1.1 Rules contract & required reading
- **`yallajo-plan/UI-UX-Design.md` is the binding rulebook.** Cite rule IDs (PE1, JS5, D4, NF1, A11Y4, CON1, RTL1–RTL4, …) in every commit. Respect its ✅ enforced / 🎯 target legend.
- **`docs/public-area-master-plan.md`** is the structural exemplar AND the record of foundations already built (Phases 0–11 are complete and committed — do not rebuild them).
- **`yallajo-plan/2-customer-dashboard.md`** defines the intended scope of every Accounts page (10 logical pages, permission map, BFF rules). `yallajo-plan/gaps/2-customer-dashboard-gaps.md` confirms **all functional gaps are closed** — this plan is purely UI/UX + structure + AJAX; it must not regress any shipped capability.
- Also useful: `yallajo-plan/controllers-by-plan.md` (~L58, Plan 2), `yallajo-plan/yallajo-dashboard-ui-ux-plan.md` (§3 Customer Dashboard, ~L241), `yallajo-plan/yallajo-template-page-wiring.md` (§C, account-* template shells).

### 1.2 Foundations that already exist — EXTEND, never duplicate
- `wwwroot/assets/js/api-client.js` → `window.YallaJo.api` (get / post / postForm / loadPartial; antiforgery header, 10s timeout, 401→sign-in). **All AJAX goes through it (JS5). The browser never calls the API host.**
- `wwwroot/assets/js/form-ux.js` → `form[data-loading]` disable+spinner (L2/F7), `window.YallaJo.toast(message, type)` (NF1/NF4/NF5: bottom-right, max 3, 5s, dedupe, role=status), `data-confirm` / `data-confirm-title` / `data-confirm-action` / `data-confirm-cancel` modal (F8/MOD3/MOD5: static backdrop, safe action autofocused).
- `BaseController.WantsAjax()` (`Infrastructure/Mvc/BaseController.cs` ~L106) → return `PartialView` for AJAX, PRG fallback otherwise (PE1). Accounts is NoStore (global NoCache base policy) — **no OutputCache attributes needed**, so no `VaryByHeaderNames` concerns here.
- `wwwroot/assets/js/listing.js` + the `_XResults` partial pattern (Public area) → pushState pagination/filtering with skeletons (S1/L1/L4/L5). Reuse the pattern; cursor-paged Accounts lists get a "Load more" variant (see Phase 4).
- Shared partials in `Areas/Public/Views/Shared/` (`_Pagination`, `_EmptyState`, `_StarRating`, `_WishlistButton`, `_TourCard`, …) + `site.css` utilities (`.object-cover`, `.avatar-48/56/64/72/96`, `.text-truncate-2/3`, `.card-img-h*`, `.thumb-h*`, `.preserve-whitespace`). Cross-area partial reuse: reference by full path (`~/Areas/Public/Views/Shared/_EmptyState.cshtml`) or move truly global ones — prefer **new Accounts-local partials in `Areas/Accounts/Views/Shared/`** when behavior diverges.
- Self-hosted fonts (`wwwroot/fonts/` + `site.css` @font-face incl. Arabic `html[lang="ar"]` stack — V3/V4/A5/X10). Accounts views use the main `~/Views/Shared/_Layout.cshtml`, which is already Google-Fonts-free — **no font migration needed**.
- `favorites.js` (global) already powers wishlist heart toggles via `window.YallaJo.api`; `recommendations-onboarding.js` materializes onboarding picks into hidden inputs (PE-friendly, no fetch).

### 1.3 Area conventions this plan must preserve
- **BFF contract (do not break):** every controller `[Area("Accounts")] [Authorize]` (+ granular `[RequirePermission]` where shipped), sealed, : BaseController, **Facade-only injection**, async + trailing `CancellationToken`, `GuardSignOut` after every facade call, GET-reads / **POST-writes only** (no PUT/DELETE/PATCH web routes) + `[ValidateAntiForgeryToken]` + PRG. Server-side ownership checks already exist — never bypass.
- Localization: `IStringLocalizer<SharedResource>`, BOTH `Resources/SharedResource.en.resx` + `.ar.resx` (flush-left entries appended before `</root>`). **Grep before adding any key — duplicates cause MSB3568. Never re-encode resx with PowerShell; UTF-8 only.** Area `_ViewImports.cshtml` exists (5 lines) — add `@using Microsoft.Extensions.Localization` there once; each view still needs its own `@inject IStringLocalizer<YallaJo.Web.Resources.SharedResource> Localizer`.
- **RTL/LTR CSS rule — every style must survive a language switch.** `<html lang dir>` flips per culture; `rtl.css` is appended after `style.css` for RTL cultures only. Use CSS logical properties or Bootstrap direction-aware utilities (`ms-*/me-*/ps-*/pe-*/start-*/end-*/text-start/text-end`) — never physical left/right unless comment-justified (RTL1). All `[dir="rtl"]` overrides go in `rtl.css` (it already flips FA + `bi-*` directional icons and re-anchors navbar dropdowns). Flip directional icons only (RTL2). Wrap numbers, booking references, dates, phone numbers, and times in `dir="ltr"` or `<bdi>` (RTL3). Verify offcanvas sidebar (`#offcanvasSidebar`), dropdowns, toasts and modals in AR. **EN-LTR + AR-RTL visual check is a merge gate for every phase touching markup/CSS (RTL4).**
- Dark mode `data-bs-theme` (T1–T5) must keep working; no SetSeo (auth-gated area); page scripts in `wwwroot/assets/js/{page}.js` via `@section Scripts`; vendored libs only — no new deps/CDNs; every AJAX conversion keeps a no-JS path (PE1); ApexCharts permitted on dashboards (X14 bans it on public pages only) but this plan does not require it.
- Gitignore gotcha: `**/[Pp]ackages/*` swallows any folder named `Packages` — negation precedent at `.gitignore` L205-215. No Accounts path matches today; verify tracking if any new `Packages`-named folder appears.

### 1.4 Verified inventory (code-verified)
**18 controllers / 56 actions** (all `[Authorize]`, POSTs antiforgery'd, 28 actions permission-gated):

| Controller | Routes (verb · route) |
|---|---|
| OverviewController | GET `/accounts` (parallel profile+bookings+recommendations+wishlist+notifications, ERR3 flags `BookingsFailed/FavoritesFailed/RecommendationsFailed`) |
| BookingsController | GET `/accounts/bookings?tab=` · GET `/accounts/bookings/{id}` · POST `{id}/cancel` · POST `{id}/dispute` (BookingDispute.Create) · POST `{id}/pay` (Payment.Create) |
| JoinRequestsController | GET `/accounts/join-requests` · POST `/accounts/join-requests` (JoinRequest.Create) |
| WishlistController | GET `/accounts/wishlist` · POST `remove/{entityType}/{entityId}` · POST `toggle/{entityType}/{entityId}` (AJAX Json) · POST `remove-all` |
| ReviewsController | GET `/accounts/reviews` (Review.Read) · POST `/accounts/reviews/edit` (Review.Update) · POST `{id}/delete` (Review.Delete) |
| AccessibilityReviewsController | GET `/accounts/accessibility-reviews` (AccessibilityReview.Read) · POST `{id}/delete` (AccessibilityReview.Delete) — list+delete only by design |
| RecommendationsController | GET `/accounts/recommendations` (Recommendation.Read) · POST `preferences` · POST `not-interested` · POST `onboarding` (all Preference.Update) · POST `track` (AJAX, Interaction.Create) |
| NotificationsController | GET `/accounts/notifications?status&type&fromDate&toDate&cursor` (Notification.Read) · POST `{id}/delete` (Notification.Delete). Mark-read/read-all live in non-area `Features/Notifications/NotificationsController` (POST `/notifications/read`, `/notifications/read-all`) |
| PaymentsController | GET `/accounts/payments` (Payment.Read) |
| InvoicesController | GET `/accounts/invoices` (Invoice.Read) · GET `{id}/download` (Invoice.Download) |
| DisputesController | GET `/accounts/disputes?page=` (Refund.Read) · POST `/accounts/disputes` (Refund.Create) |
| SupportController | GET `/accounts/support?cursor=` (SupportTicket.Read) · GET `{id}` · POST `{id}/messages` · POST `{id}/close` (SupportTicket.Close). New-ticket = public `/contact` by design |
| ProfileController | GET `/accounts/profile` · POST `profile/update` · POST `profile/avatar` · POST `profile/avatar/delete` · POST `profile/delete` |
| SettingsController | GET `/accounts/settings` · POST `settings/notifications` · POST `settings/marketing` · POST `settings/phone` · POST `settings/sessions/revoke/{sessionId}` · POST `settings/devices/trust/{deviceId}` · POST `settings/logout-all` · POST `settings/devices` (DeviceToken.Create) · POST `settings/devices/{id}/remove` (DeviceToken.Delete) |
| ChangePasswordController | GET+POST `/accounts/changepassword` |
| UpdatePhoneController | GET+POST `/accounts/updatephone` (duplicate entry point with `settings/phone`) |
| DeleteController | GET `/accounts/delete` · POST `/accounts/delete` · POST `delete/restore` |
| PrivacyController | GET `/accounts/privacy` · GET `privacy/export` · POST `privacy/delete-data` · POST `privacy/cancel-deletion` |

**Views: 24 files** (22 routed views + 2 partials) + `Shared/_AccountSidebar.cshtml` (124L: avatar/name/email + **16 nav links** + Sign Out; rendered by every main view, offcanvas `#offcanvasSidebar` on mobile). All use `~/Views/Shared/_Layout.cshtml`. Line counts: Overview 251, Bookings/Index 109, Bookings/Detail 216, Profile/Index 153 (+ `_UpdateProfileForm` 60), ChangePassword 41, Settings 302, Wishlist 147, Recommendations 237, Notifications 176, Support/Index 109, Support/Details 110, Disputes 175, Invoices 111, Payments 101, Reviews 189, JoinRequests 102 (+ `_JoinRequestForm` 34), Delete 90, Privacy 120, UpdatePhone 29, AccessibilityReviews 94.
**Scripts:** zero external `<script src>` in Accounts views except (audit-reported) `recommendations-onboarding.js` on Recommendations — **verify at execution**. 2 inline `@section Scripts` blocks (Wishlist client sort ~L122-146; Delete checkbox-enables-button ~L79-89). 7 views use `_ValidationScriptsPartial`.
**15 Facades / 16 ApiClients**, ApiResult pattern, ~80 model classes across 16 Models folders. All 28 web features chain ✓ to existing API endpoints (zero broken chains).
**Inbound links to keep working:** `_Navbar2` profile dropdown (My Profile / My Bookings / Wishlist / Settings), NotificationBell → `/accounts/notifications`, Public Booking/Confirmation → `/accounts/bookings`, mobile bottom nav (Wishlist / Bookings / Profile).

### 1.5 Per-phase verification protocol
1. `dotnet build src/Hosts/YallaJo.Web/YallaJo.Web.csproj --no-incremental` → **baseline 71 warnings / 0 errors; zero NEW warnings** (resx duplicates surface as MSB3568 — treat as failure).
2. Walk every touched surface in **EN-LTR and AR-RTL** (merge gate, RTL4), **light + dark** (T1–T5), at **390px and 1280px**.
3. No raw resx keys rendered; no new inline `style=` except X6-justified with a code comment.
4. JS-off re-test of every converted interaction (PE1) — native POST + PRG must still work.
5. One conventional commit per phase citing rule IDs (e.g. `refactor(accounts): consolidate settings hub (Phase 2, R2/PE1/F8)`).
6. Hard guardrails in §4 apply to every phase.

---

## §2 View Reduction Table (first-class deliverable)

Target: **22 routed views → 14 routed views** (−8). Each retired page's GET 301s into its new home; **every POST action, permission gate, and the Download GET survive unchanged**. Hub tabs are deep-linkable (`#fragment` or `?tab=`) and lazy-load via `WantsAjax()` partials (R2), with full-page SSR fallback (PE1). Precedents: Public `Search/Index` → 301 into `/tours`; `Help/Detail` → accordion + `#faq-{id}` redirect.

| View | Verdict | Mechanism | UX gain | SEO/route impact |
|---|---|---|---|---|
| Overview/Index | **keep** (polish) | — | Real stats + retryable section cards | none |
| Bookings/Index | **keep** (absorbs Join Requests) | new `join-requests` tab (BookingsVm already has `ActiveTab/Tabs`) | One trips hub: upcoming/past/cancelled/join-requests | `/accounts/join-requests` GET → 301 `/accounts/bookings?tab=join-requests` |
| Bookings/Detail | **keep** (polish) | — | Pay/cancel/dispute flow polish | none |
| JoinRequests/Index | **retire** | content becomes `_JoinRequestsTab` partial inside Bookings | No orphan page; requests appear next to the bookings they join | 301 as above; POST `/accounts/join-requests` unchanged |
| JoinRequests/_JoinRequestForm | keep (partial) | reused inside the tab | — | — |
| Wishlist/Index | **keep** (AJAX polish) | — | Optimistic remove + Undo (WL2/WL3) | none |
| Recommendations/Index | **keep** (polish) | — | Optimistic not-interested (NF6) | none |
| Notifications/Index | **keep** (AJAX polish) | — | Load-more cursor paging + AJAX mark-all-read | none |
| Reviews/Index | **keep** (becomes hub) | tabs: My reviews · Accessibility reviews | One "My reviews" surface | none |
| AccessibilityReviews/Index | **retire** | content becomes `_AccessibilityReviewsTab` partial | — | `/accounts/accessibility-reviews` GET → `WantsAjax() ? PartialView : 301 /accounts/reviews#accessibility`; Delete POST unchanged |
| Payments/Index | **keep** (becomes Billing hub) | tabs: Payments · Invoices · Disputes | One billing surface instead of 3 thin pages | none |
| Invoices/Index | **retire** | `_InvoicesTab` partial | — | GET → `WantsAjax() ? PartialView : 301 /accounts/payments#invoices`; `{id}/download` GET unchanged |
| Disputes/Index | **retire** | `_DisputesTab` partial (keeps its open-dispute form) | — | GET → `WantsAjax() ? PartialView : 301 /accounts/payments#disputes`; Open POST unchanged |
| Support/Index | **keep** | — | — | none — list is deep-linked from emails |
| Support/Details | **keep** | — | thread deep-links from notifications must survive | rejected for merge: deep-linkable thread |
| Profile/Index (+ `_UpdateProfileForm`) | **keep** | — | identity editing is high-traffic, distinct from settings | none |
| Settings/Index | **keep** (becomes Settings hub) | tabs: Notifications · Security · Devices · Linked · Privacy & data · Account | 302-line wall of cards → organized lazy tabs (R2) | none |
| ChangePassword/Index | **retire** | form becomes part of `_SecurityTab` | password lives beside phone/sessions | GET → 301 `/accounts/settings#security`; POST `/accounts/changepassword` unchanged |
| UpdatePhone/Index | **retire** | `_SecurityTab` already has phone form (via existing `settings/phone` POST) | kills the duplicate phone entry point | GET → 301 `/accounts/settings#security`; keep ONE phone POST (see Phase 2) |
| Privacy/Index | **retire** | `_PrivacyTab` partial | privacy/export/delete-data beside other settings | GET → 301 `/accounts/settings#privacy`; Export GET + both POSTs unchanged |
| Delete/Index | **retire** | `_AccountDangerTab` partial ("Account" tab) | delete/restore in a guarded danger zone | GET → 301 `/accounts/settings#account`; POSTs unchanged |

Rejected reductions (one-liners): **Support/Details** — deep-linked conversation thread; merging would break email/notification links and no-JS reading. **Bookings/Detail** — payment/dispute/invoice anchor page (PAY3, REV1 deep links). **Profile vs Settings merge** — profile editing is the single highest-traffic task; burying it in a 6-tab hub harms discoverability (plan doc keeps them separate).

Sidebar effect: `_AccountSidebar` shrinks **16 nav links → 9** (Overview, My Profile, My Bookings, Wishlist, For You, Notifications, My Reviews, Billing, Support, Settings — 10 incl. both Profile and Settings) — a major scanning-cost win (D3-adjacent).

---

## §3 Phases

### Phase 0 — Recon fixes & micro-cleanup (0.5 d)
1. Verify the `recommendations-onboarding.js` reference discrepancy (inventory says zero `<script src>`, audit says Recommendations/Index references it). Whichever is true, ensure the onboarding quiz still posts its hidden inputs without JS (PE1) and the script loads via `@section Scripts` + `asp-append-version`.
2. `[Backend]` Remove the duplicate `POST /api/v1/accounts/profile/avatar` endpoint (Accounts module) **only after** confirming the web ApiClient uses the PUT variant and nothing else consumes the POST (grep API + tests). Additive-only rule: if any consumer exists, skip with a code comment.
3. Decide the duplicate phone entry point (see Phase 2 step 4) — no code yet, just confirm both POSTs share `UpdatePhoneFacade`/`SettingsFacade` semantics.
4. Acceptance: build green (71/0); no behavior change.
Commit: `chore(accounts): phase 0 recon fixes`.

### Phase 1 — Foundations: localization plumbing, shared partials, form UX (3 d)
1. **Localizer plumbing:** add `@using Microsoft.Extensions.Localization` to `Areas/Accounts/Views/_ViewImports.cshtml`; add `@inject IStringLocalizer<YallaJo.Web.Resources.SharedResource> Localizer` to every view that gets strings in Phase 6 (do the injection now, key swaps later, to keep diffs reviewable).
2. **New Accounts shared partials** in `Areas/Accounts/Views/Shared/`:
   - `_AccountMenuToggle.cshtml` — the mobile "Account menu" offcanvas trigger block currently duplicated **19×** (every main view, e.g. Overview ~L29). One partial, localized label.
   - `_StatusBadge.cshtml` — replaces the `StatusBadge()` local switch helper duplicated 5× (Overview, Bookings/Index ~L70, Bookings/Detail, JoinRequests ~L72, Reviews). Model: `(string Status)` → badge class + localized text via `Public.Status.{Status}` keys.
   - `_PrevNextPager.cshtml` — unifies the 3 hand-rolled pagers (Disputes page-based ~L108-110; Notifications + Support cursor-based). Knobs: prev/next URLs (nullable), localized labels, `aria-label`.
3. **Empty states:** replace the ~12 hand-rolled empty blocks with `<partial name="~/Areas/Public/Views/Shared/_EmptyState.cshtml" />` (ViewData: icon/title/message/CTA per L6) — e.g. Wishlist ~L50, Reviews ~L50, Recommendations ~L66, Notifications ~L108, Disputes ~L37, Invoices ~L46, Payments ~L52, JoinRequests ~L40.
4. **Inline-style sweep** (11 hits → `site.css` utilities, X6): AccessibilityReviews ~L71 → `.preserve-whitespace`; Bookings/Detail ~L51 max-height:240px → new `.img-max-h240`; Bookings/Index ~L70/74 min-height:120px → new `.min-h-120`; JoinRequests ~L65/68 max-widths → `.max-w-220` (exists) + new `.max-w-200`; Profile ~L54 progress width % → **keep inline, add X6 comment (dynamic value)**; Profile ~L92 → new `.max-w-14r`; Support/Details ~L60 max-width:80% → new `.max-w-80`; Wishlist ~L28 width:auto → `.w-auto` (Bootstrap); Wishlist ~L68/72 height:180px → new `.card-img-h180` (check: already exists from Public Phase 3 — reuse).
5. **`data-loading` rollout** (L2/F7) on the 7 feedback-less forms: `_UpdateProfileForm`, `_JoinRequestForm`, Settings notification-prefs + device-register forms, Reviews edit form, Recommendations preferences form, Privacy delete-data form.
6. **`confirm()` → `data-confirm` modal** (F8/MOD3/MOD5), killing all 9 antipatterns: Profile delete-avatar ~L100, Wishlist remove-all ~L35, Notifications delete ~L145, Reviews delete ~L125, Disputes open ~L133 (note: confirm-on-open is MOD12-questionable — opening a dispute is not destructive; drop the confirm entirely and rely on the form), Settings remove-device ~L235, Delete account ~L64, Support close-ticket ~L98, AccessibilityReviews delete ~L79. Reuse existing `Public.Confirm.*` resx keys; add new message keys per entity (both resx, grep first).
7. Acceptance: build 71/0; grep `onsubmit="return confirm` + `onclick="return confirm` in Areas/Accounts → 0; grep the duplicated menu-toggle markup → only the partial; EN/AR + light/dark + 390/1280 pass on Overview, Bookings, Wishlist, Settings.
Commit: `refactor(accounts): shared partials, form ux, confirm modals (Phase 1, F7/F8/L6/X6/MOD5)`.

### Phase 2 — Settings hub consolidation (4 d) — retires 4 views
1. **Settings/Index becomes a tabbed hub** (`nav-tabs` + `tab-content`, hash-synced like Public Tours/Detail Phase 8.5): tabs **Notifications** (notif prefs + marketing) · **Security** (password + phone + sessions + logout-all) · **Devices** (tokens + trust) · **Linked accounts** · **Privacy & data** · **Account** (delete/restore danger zone). Default tab (Notifications) SSR'd; other tabs lazy-loaded via `window.YallaJo.api.loadPartial` (R2), each with a real `<a href>` fallback to the SSR'd full hub with `?tab=` (PE1). New page script `wwwroot/assets/js/accounts-settings.js` (`data-yj-component="settings-tabs"`, idempotent JS2/JS4): tab↔hash sync + lazy partial fetch + focus management.
2. **Tab partials** in `Areas/Accounts/Views/Settings/`: `_NotificationsTab`, `_SecurityTab`, `_DevicesTab`, `_LinkedTab`, `_PrivacyTab`, `_AccountDangerTab` — extracted from today's Settings/Index ~L27-291, Privacy/Index ~L29-114, Delete/Index ~L23-67, ChangePassword/Index ~L8-35, UpdatePhone/Index ~L8-23. Each partial self-contained (own `@inject Localizer`, own forms with antiforgery + `data-loading`).
3. **Controller wiring:** `SettingsController.Index(string? tab)` renders the hub; add GET partial endpoints returning each tab when `WantsAjax()` (either `Index` + tab param, or per-tab GETs on the owning controllers — choose per-tab GETs so permissions stay where they are: `PrivacyController.Index`, `DeleteController.Index`, `ChangePasswordController.Index`, `UpdatePhoneController.Index` each become `WantsAjax() ? PartialView("_XTab", vm) : RedirectPermanent("/accounts/settings#x")`). **All POST actions keep their existing routes/controllers/permissions** — only their success PRG targets change to `/accounts/settings#<tab>`.
4. **Phone de-duplication:** keep `SettingsController.UpdatePhone` (POST `/accounts/settings/phone`) as the single write; `UpdatePhoneController` POST delegates or is deleted after grep-verifying nothing posts to `/accounts/updatephone` anymore (the retired view was its only consumer).
5. `[Backend]` **GET `/api/v1/notifications/types`** (Messaging module) — the notification-type rows are hardcoded in `SettingsFacade.cs` ~L20-31. Contract: GET, no params, `200 [{ "type": "BookingConfirmed", "nameKey": "...", "channelDefaults": {...} }]`, `[Authorize]` Notification.Read. Wiring: Messaging CQRS `GetNotificationTypesQuery` → minimal-API endpoint in Messaging Presentation → `SettingsApiClient.GetNotificationTypesAsync` → `SettingsFacade.GetNotificationRows` (replace hardcoded list, keep hardcoded list as fallback on failure) → existing VM. Additive; nothing breaks if it fails.
6. `[Backend, stretch]` **PUT `/api/v1/security/account/email`** (Security module) — users currently cannot change email. Contract: PUT, body `{ "newEmail": string, "currentPassword": string }`, 204 / 400 validation / 409 in-use, `[Authorize]` (mirror the phone-change handler `ChangePhoneCommand` pattern). Wiring: Security CQRS `ChangeEmailCommand` → endpoint → new `SettingsApiClient.UpdateEmailAsync` → `SettingsFacade.UpdateEmail` → `_SecurityTab` email form (POST `/accounts/settings/email`, antiforgery, data-loading). **Note:** production-grade email change needs a verification-mail flow — implement the simple authenticated change now and leave `// TODO(backend): email verification flow` on the handler. If the Security module owners reject this, ship the tab without the email form.
7. **Retire views:** `git rm` ChangePassword/Index.cshtml, UpdatePhone/Index.cshtml, Privacy/Index.cshtml, Delete/Index.cshtml. Update `_AccountSidebar` links (Settings absorbs Privacy & Data + Delete Profile entries).
8. Acceptance: `/accounts/changepassword`, `/accounts/updatephone`, `/accounts/privacy`, `/accounts/delete` GETs 301 to the right tab; every settings POST works JS-on (stay on tab + toast) and JS-off (PRG back to tab); permissions unchanged (28 RequirePermission gates intact); build 71/0; EN/AR + dark + mobile pass.
Commit: `refactor(accounts): tabbed settings hub, retire 4 satellite pages (Phase 2, R2/PE1/F8/C2)`.

### Phase 3 — Billing hub + Reviews hub + Trips tab (3 d) — retires 4 views
1. **Billing hub:** Payments/Index becomes tabs **Payments · Invoices · Disputes** (same hub mechanics as Phase 2; script reuse from `accounts-settings.js` → generalize the tab component into `accounts-tabs.js` if cleaner). New partials `Payments/_PaymentsTab`, `_InvoicesTab` (from Invoices/Index ~L35-111 incl. download buttons), `_DisputesTab` (from Disputes/Index ~L24-165 incl. open-dispute form + `_PrevNextPager`). `InvoicesController.Index` / `DisputesController.Index` → `WantsAjax() ? PartialView : RedirectPermanent("/accounts/payments#invoices|#disputes")`; Download GET + Open POST unchanged. Sidebar: My Invoices / My Payments / My Disputes → single **Billing** link.
2. **Reviews hub:** Reviews/Index becomes tabs **My reviews · Accessibility reviews**. New partial `Reviews/_AccessibilityReviewsTab` (from AccessibilityReviews/Index ~L25-94, keeping its delete form + REV5 edit-window badges on the reviews side). `AccessibilityReviewsController.Index` → `WantsAjax() ? PartialView : RedirectPermanent("/accounts/reviews#accessibility")`; Delete POST unchanged (AccessibilityReview.Delete).
3. **Trips tab:** Bookings/Index gains a `join-requests` tab (BookingsVm.Tabs already drives tabs — extend `BookingsFacade.GetBookings`/controller to compose the join-requests list when that tab is active, reusing `JoinRequestsFacade.GetMine`). New partial `Bookings/_JoinRequestsTab` hosting the table + `_JoinRequestForm`. `JoinRequestsController.Index` → `WantsAjax() ? PartialView : RedirectPermanent("/accounts/bookings?tab=join-requests")`; Create POST unchanged.
4. **Retire views:** `git rm` Invoices/Index.cshtml, Disputes/Index.cshtml, AccessibilityReviews/Index.cshtml, JoinRequests/Index.cshtml. Update `_AccountSidebar` (16 → 9-10 links) + any inbound links (grep `asp-controller="Invoices"|"Disputes"|"AccessibilityReviews"|"JoinRequests"` across the Web host).
5. Acceptance: all four retired GETs 301 correctly; tab deep-links work; downloads + all POSTs work JS-on/off; sidebar shows the consolidated nav; build 71/0; EN/AR + dark + mobile pass.
Commit: `refactor(accounts): billing/reviews/trips hubs, retire 4 pages (Phase 3, R2/PE1/D1)`.

### Phase 4 — AJAX modernization of the surviving lists (4 d)
1. **Notifications:** convert cursor paging to a "Load more" button (PE1: it's a real link to `?cursor=`) intercepted by a new `accounts-notifications.js` (`data-yj-component`): `loadPartial` appends the next page into the list (NotificationsController.Index returns a `_NotificationsList` partial when `WantsAjax()`); skeleton rows while loading (L1/L4/L5); `aria-live="polite"` on the list container; **mark-all-read** becomes AJAX (POST to the existing non-area `/notifications/read-all` via `window.YallaJo.api.post`, optimistic badge clear, toast on failure — NF6); delete stays PRG + `data-confirm`.
2. **Wishlist:** optimistic remove (WL2/NF6): intercept the remove form → fade card out immediately → `api.post` → on failure restore + error toast; success toast carries **[Undo]** (WL3) re-calling the existing `toggle` endpoint. Replace the inline client-sort `@section Scripts` with `accounts-wishlist.js` (same behavior, `data-yj-component`, idempotent). `aria-live` on the grid + count. Remove-all keeps PRG + `data-confirm`.
   - `[Backend]` **GET `/api/v1/social/favorites/with-details?cursor&pageSize`** (Social module) — today the facade does 1 list call + N per-entity lookups (N+1). Contract: GET, `[Authorize]`, response items `{ entityType, entityId, title, subtitle, imageUrl, price?, currency?, rating?, slug, addedAt }` + `nextCursor`. Wiring: Social CQRS `GetMyFavoritesWithDetailsQuery` (joins/denormalizes per entity type Tour/Place/Business/Blog/TourGuide) → endpoint → `WishlistApiClient.GetWithDetailsAsync` → `WishlistFacade.GetWishlist` (replace the N+1 path; keep old path as fallback) → existing `WishlistVm.Items`. Enables real cursor pagination too (today pageSize=50 hardcoded) — add a "Load more" using the same pattern as notifications.
   - `[Backend]` **GET `/api/v1/social/favorites/count`** (Social) — `{ "count": int }`, `[Authorize]`. Consumed by a small badge on the navbar Wishlist link (WL4) via the Accounts WishlistController exposing GET `/accounts/wishlist/count` (AJAX Json) — fetched once per page by `favorites.js` extension, cached in sessionStorage 60s.
3. **Recommendations:** not-interested buttons become optimistic (NF6): card fades immediately, `api.post` in background, restore+toast on failure. `track` POST already AJAX — route it through `window.YallaJo.api` if it isn't (JS5). `aria-live` on the grid.
4. **Support thread:** Reply form posts via `api.postForm` when JS-on → `SupportController.Reply` returns the refreshed thread partial (`_SupportThread`, extracted from Details ~L34-101) on `WantsAjax()`; PRG fallback unchanged; `data-loading` on the form; close-ticket stays PRG + `data-confirm`.
5. **Bookings list:** wire the **partial backend capability** — list endpoint accepts `fromDate/toDate` the web never sends. Add a small date-range filter row (flatpickr, vendored) + status tabs already present; GET form (PE1); `BookingsFacade.GetBookings` passes the new params through `BookingsApiClient`. If volume warrants, add "Load more" cursor paging per D1's infinite-scroll exception (real link fallback).
6. Acceptance: every converted interaction works JS-off (PE1); no raw `fetch(` outside api-client.js (JS5); optimistic flows revert on failure (NF6); build 71/0; EN/AR + dark + mobile pass.
Commit: `feat(accounts): ajax lists - notifications, wishlist, recommendations, support (Phase 4, NF6/WL2-WL4/JS5/L1)`.

### Phase 5 — Overview & Booking Detail conversion polish (2.5 d)
1. **Overview:** ERR3 retry cards (~L107/155/204) get real retry buttons that re-fetch just that section via `loadPartial` (new partial per rail: `_OverviewBookings`, `_OverviewFavorites`, `_OverviewPicks`; OverviewController returns them on `WantsAjax()` + `?section=`); JS-off fallback = full page reload link (PE1).
2. `[Backend]` **GET `/api/v1/accounts/profile/stats`** (Accounts module) — Overview's stat tiles currently derive counts from truncated lists. Contract: GET, `[Authorize]`, `200 { "upcomingBookings": int, "favorites": int, "reviews": int, "unreadNotifications": int }` (Accounts module aggregates via existing module queries or integration events — owner's choice; keep it one round-trip). Wiring: CQRS `GetProfileStatsQuery` → endpoint → `OverviewApiClient`/`ProfileApiClient.GetStatsAsync` → `OverviewFacade` (parallel with the rails, API1) → `OverviewVm` real counts. Fallback: keep current derived counts when the call fails (ERR3).
3. **Booking Detail:** payment-expiry countdown (reuse `booking-confirmation.js` pattern if VM exposes a timestamp — verify; else skip with `// TODO(backend)`); copy-booking-reference button + toast (reuse `.js-copy-ref` pattern); refund preview emphasized `fw-bold text-success` in the cancel confirm modal (F8: modal title = question, consequences listed, safe action autofocused); `<bdi>`/`dir="ltr"` on reference + dates (RTL3); dispute modal gets `data-loading` + inline validation (F6).
4. Acceptance: section retry works without full reload; stats real or gracefully degraded; cancel/dispute flows pass F8/F6 review; build 71/0; EN/AR + dark + mobile.
Commit: `feat(accounts): overview stats + retryable rails, booking detail polish (Phase 5, ERR3/API1/F8/RTL3)`.

### Phase 6 — Localization + RTL hardening (3 d)
1. Replace **all 87 audited hardcoded EN strings** with resx keys (BOTH `.en` + `.ar`, AR translated; grep every key before adding). Hotspot map (counts from audit): `_AccountSidebar` 18 (all 16 nav labels + Edit profile + Sign Out — new `Accounts.Nav.*` keys); Overview ~25 (welcome, card titles, retry blocks — reuse `Public.Retry`/`Public.Results.Error` where identical); Bookings/Detail ~38; Settings ~40 (now spread across tab partials); Notifications ~17; Disputes ~17 (now `_DisputesTab`); Recommendations ~19; Reviews ~14; JoinRequests ~11; Invoices ~10; Payments ~10; Privacy ~20 (`_PrivacyTab`); Delete ~11 (`_AccountDangerTab`); Wishlist 9; Support 14; Profile 7; AccessibilityReviews 6; ChangePassword/UpdatePhone copy lives on in `_SecurityTab`. Namespace new keys `Accounts.*`; reuse existing `Public.*` keys where the English text is identical (e.g. `Public.Previous/Next`, `Public.Save.changes`, `Public.Confirm.*`).
2. **RTL fixes from the direction-fragile audit:** wrap in `dir="ltr"`/`<bdi>`: Notifications timestamp ~L128 (also stop showing raw UTC — format via `CultureInfo.CurrentCulture`), Bookings/Detail reference + dates + guest counts ~L54-57, Payments dates ~L74, Invoices dates ~L77, JoinRequests dates ~L66-67, Settings phone numbers + session IP/device strings. Verify offcanvas sidebar anchoring, dropdowns, and the new tab navs in AR (RTL4). Any needed overrides go in `rtl.css` only.
3. Acceptance: AR walkthrough of every Accounts page shows zero English (except codes/refs deliberately LTR); no MSB3568; build 71/0.
Commit: `feat(accounts): localize all copy + RTL hardening (Phase 6, CON1/CON3/RTL1-RTL4)`.

### Phase 7 — Accessibility (2 d)
1. aria-labels on icon-only controls: `_AccountSidebar` edit-pencil ~L15 + sign-out ~L115, Overview bell ~L43, Recommendations not-interested ~L132, Disputes pay-link ~L92, wishlist remove buttons, tab close/copy buttons added in earlier phases.
2. Label association: Settings notification-table checkbox switches get `<label for>` or `aria-labelledby` (A11Y4); device-register + dispute-modal inputs get proper `<label asp-for>` + `aria-describedby` help text.
3. `aria-live="polite"` regions confirmed/added: wishlist grid (after sort/remove), recommendations grid (after not-interested), notifications list (after load-more/mark-all), overview rails (after retry), toasts already NF5-compliant.
4. Tab hubs: `role="tablist"/"tab"/"tabpanel"` + `aria-selected` + `aria-controls` (mirror Public Tours/Detail Phase 8.5 markup); keyboard arrow-key behavior comes free with Bootstrap nav-tabs — verify focus order after lazy loads (focus the loaded panel heading).
5. Modals from Phase 1 already follow F8/MOD3/MOD4/MOD5; verify `role="alertdialog"` on destructive ones (account delete, data delete).
6. Acceptance: axe-core scan (manual, TEST3 🎯) on Overview, Bookings Index+Detail, Settings hub, Wishlist, Notifications → zero critical/serious; full keyboard pass on the three hubs.
Commit: `feat(accounts): accessibility hardening (Phase 7, A11Y1/A11Y4/A11Y9/MOD4)`.

### Phase 8 — Performance & polish (1.5 d)
1. `loading="lazy"` on below-fold images (wishlist cards, recommendation cards, overview rails, booking thumbnails); explicit dimensions or aspect-ratio utility classes to kill CLS (I3/IMG4) — the `.card-img-h180`/`.thumb-h*` utilities from Phase 1 already constrain heights.
2. Overview stays one parallel composition (API1) — verify no sequential awaits crept in; wishlist N+1 eliminated by the Phase 4 `[Backend]` endpoint (measure: 1 API call instead of 1+N).
3. Notifications timestamps rendered server-side in user culture (no client Date parsing); avoid layout shift on tab lazy-loads (min-height placeholder per L1/L4).
4. Final sweep: grep Areas/Accounts views for `style=` (only X6-justified survivors), `confirm(`, raw `fetch(`, hardcoded EN.
5. Acceptance: build 71/0; Lighthouse (manual, M3 🎯) on `/accounts` + `/accounts/bookings` shows no CLS regressions; EN/AR + dark + mobile final pass.
Commit: `perf(accounts): lazy images, CLS, N+1 elimination (Phase 8, A7/IMG3/I3/API1)`.

---

## §4 Hard guardrails
- Touch ONLY: `Areas/Accounts/**`, `Resources/SharedResource.{en,ar}.resx`, `wwwroot/assets/css/site.css`, `wwwroot/assets/css/rtl.css`, new `wwwroot/assets/js/accounts-*.js`, `wwwroot/assets/js/favorites.js` (WL4 badge extension only), `Views/Shared/_Navbar2.cshtml` (only if the WL4 badge needs a hook), and the **explicitly listed `[Backend]` additions** below — nothing else in `YallaJo.Api`/`src/Modules`, never other Web areas.
- `[Backend]` additions (all additive; never reshape existing endpoints; each lands in the same phase as its consumer): ① DELETE duplicate avatar POST (Accounts, Phase 0, verify-first) · ② GET `/api/v1/notifications/types` (Messaging, Phase 2) · ③ PUT `/api/v1/security/account/email` (Security, Phase 2, stretch) · ④ GET `/api/v1/social/favorites/with-details` (Social, Phase 4) · ⑤ GET `/api/v1/social/favorites/count` (Social, Phase 4) · ⑥ GET `/api/v1/accounts/profile/stats` (Accounts, Phase 5). Each needs: CQRS handler + endpoint registration in the owning module, ApiClient method, Facade wiring, unit tests alongside the module's existing suites, and graceful web fallback when the endpoint fails.
- Never weaken the BFF contract (POST-only web writes, antiforgery, PRG, GuardSignOut, ownership checks, permission gates — all 28 `[RequirePermission]` attributes survive verbatim).
- Every converted interaction keeps a no-JS path (PE1) — if it can't, don't convert it.
- Every string goes to BOTH resx files; grep before adding (MSB3568); UTF-8 only, never PowerShell re-encoding.
- One conventional commit per phase citing rule IDs; build baseline 71 warnings / 0 errors with zero NEW warnings is a gate on every commit.

## §5 Execution order & sizing

| Phase | Scope | Size | Depends on | Parallelizable with |
|---|---|---|---|---|
| 0 | Recon fixes, duplicate-endpoint cleanup | 0.5 d | — | — |
| 1 | Foundations: partials, form UX, confirm modals, inline styles | 3 d | 0 | — |
| 2 | Settings hub (retire 4 views) + [Backend] ②③ | 4 d | 1 | 3 |
| 3 | Billing/Reviews/Trips hubs (retire 4 views) | 3 d | 1 | 2 |
| 4 | AJAX lists + [Backend] ④⑤ | 4 d | 1 (3 for tab partials) | 5 |
| 5 | Overview + Booking Detail polish + [Backend] ⑥ | 2.5 d | 1 | 4 |
| 6 | l10n + RTL (consumes the audit list) | 3 d | 2,3 (strings live in final partials) | 7 |
| 7 | Accessibility | 2 d | 2,3 | 6 |
| 8 | Performance | 1.5 d | all | — |

**Total: ~23.5 dev-days (~5 weeks solo; ~3 weeks with phases 2∥3 and 6∥7 split across two executors).**
**View count: 22 routed views → 14 routed views** (8 retired; their content survives as 10 lazy-loadable tab partials). Sidebar: 16 links → ~10.
