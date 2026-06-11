# Auth Area Master Plan — UI/UX Modernization, View Reduction, AJAX & Backend Additions

**Audience:** an autonomous agent with zero conversation context.
**Produced from:** a full code audit of `src/Hosts/YallaJo.Web/Areas/Auth`, `src/Modules/Auth`, `wwwroot/assets/{js,css}`, the binding rules contract `yallajo-plan/UI-UX-Design.md`, and the structural exemplar `docs/public-area-master-plan.md`.
**Scope:** `Areas/Auth` (Web) + the explicitly listed `[Backend]` additions in `src/Modules/Auth` + `src/Hosts/YallaJo.Api` wiring. Nothing else.
**All line numbers are indicative (~L123) — re-verify before editing. All claims below were code-verified at plan time.**

---

## §1 Context & Ground Rules

### 1.1 Layouts and page split

The Auth area serves **two distinct surfaces**:

| Surface | Layout | Pages |
|---|---|---|
| **Anonymous funnel** (chrome-less) | `Areas/Auth/Views/Shared/_AuthLayout.cshtml` (default via `_ViewStart.cshtml`) | SignIn, SignUp, ForgotPassword, ResetPassword, TwoFactor, AcceptInvite/* |
| **Authenticated account pages** (full site chrome) | public `Views/Shared/_Layout.cshtml` (opted in per-view) | Sessions/Index, ExternalProviders/Index, ExternalProviders/Complete |

**Critical layout facts (verified):**
- `_AuthLayout.cshtml` (76L) loads: `theme-bootstrap.js` pre-paint (T1/T5), font-awesome + bootstrap-icons vendor CSS, `style.css`, `auth.css`, conditional `rtl.css` (~L46–49), ImportMap, `_Alerts`, bootstrap.bundle, `functions.js`.
- `_AuthLayout` does **NOT** load Google Fonts — the historic debt note ("Google Fonts at ~L30–32") is **already resolved**. The *actual* remaining debt: it does **not** load `site.css` (→ no self-hosted DM Sans / Poppins / IBM Plex Sans Arabic on the funnel; Arabic funnel pages fall back to system `sans-serif`), and it does **not** load `api-client.js` or `form-ux.js` (→ no `window.YallaJo.api`, no toast, no `data-loading`, no `data-confirm` on funnel pages). Its comment ~L29–33 falsely claims no local font files exist — `wwwroot/fonts/` has all 8 woff2 files and `site.css` ~L13–85 has the `@font-face` blocks with `font-display: swap` + unicode-range, plus `html[lang="ar"]` overrides ~L89–98 (V3/V4/A5/X10).
- Public `_Layout.cshtml` loads `site.css` ~L82, `api-client.js` ~L256, `form-ux.js` ~L257 — so Sessions/ExternalProviders pages already have the foundations.

### 1.2 Conventions (binding — cite rule IDs from `yallajo-plan/UI-UX-Design.md` in every commit)

1. **Pipeline:** View → Controller → Facade → ApiClient → API (`/api/v1/auth/*`). Browser never calls the API host; all AJAX goes through `window.YallaJo.api` (JS5).
2. **AJAX = post-SSR refinement only** (R2). Every AJAX conversion keeps a working no-JS path (PE1). Use `BaseController.WantsAjax()` → `PartialView` / PRG fallback.
3. **Localization (CON1):** `IStringLocalizer<SharedResource>`; every new key added to **BOTH** `Resources/SharedResource.en.resx` and `.ar.resx`, flush-left, appended immediately before `</root>`. **Grep for the key first** — a duplicate key fails the build with MSB3568. Never re-encode resx via PowerShell; UTF-8 only. New Razor partials need `@using Microsoft.Extensions.Localization` before `@inject` (the area `_ViewImports.cshtml` covers Auth views, but partials placed elsewhere are not guaranteed coverage).
4. **RTL/LTR (RTL1–RTL4):** the site flips `<html lang dir>` per culture (EN→`ltr`, AR→`rtl`); `rtl.css` is appended **after** `style.css` for RTL cultures (no `style.rtl.css` swap exists). Every style this plan adds must use **CSS logical properties** (`margin-inline-start`, `padding-inline-end`, `inset-inline-*`, `border-start-*`) or Bootstrap direction-aware utilities (`ms-*`/`me-*`/`ps-*`/`pe-*`/`start-*`/`end-*`/`text-start`/`text-end`) — never physical `left/right` unless justified with a code comment (RTL1). All `[dir="rtl"]` overrides live in `rtl.css` — never inline, never per-page `<style>`. Flip **directional icons only** (arrows/chevrons/back) — never stars/hearts/phones (RTL2). Numbers, OTP codes, IPs, emails, dates, tokens wrapped in `dir="ltr"` / `<bdi>` (RTL3). **Both-direction visual check (EN-LTR + AR-RTL) is a merge gate for every phase that touches markup or CSS (RTL4).**
5. **Caching:** auth-gated and credential pages are `NoStore` (C2). `SetSeo` is **NOT** used in this area — none of these pages are public-facing marketing surfaces (sign-in/sign-up may keep their existing minimal titles).
6. **Dark mode:** `data-bs-theme` tokens only (T1–T5); no hardcoded colors.
7. **Scripts:** page scripts live in `wwwroot/assets/js/{page}.js`, loaded via `@section Scripts`; idempotent init (JS4); vendored libs only — **no new dependencies, no CDNs** (the existing Google reCAPTCHA `api.js` CDN load in `_RecaptchaField.cshtml` is a pre-existing, security-mandated exception — do not add others).
8. **Inline styles:** only for dynamic computed values with a justifying comment (X6) — e.g. the password-meter `style="width: 0%"`.
9. **Security posture:** every POST keeps `ValidateAntiForgeryToken` (SEC7; `api-client.js` sends the header automatically). The auth funnel is deliberately free of inline `<script>` (CSP) — keep it that way. Rate limits already exist API-side (`Auth.Presentation/RateLimiting/RateLimitPolicies.cs`: Login 10/min/IP, OTP 3/min sliding per-email, Register 5/min/IP, Refresh 20/min — cite these *actual* numbers, not the SEC5 target draft numbers).
10. **Anti-enumeration invariants (do not regress):** forgot-password/resend-otp always return the same uniform success message; the two-factor page reads the pending email **only** from the encrypted `YallaJo.PendingVerify` cookie (DataProtection, 15-min, `Path=/auth`) — never reintroduce `?email=` query params.

### 1.3 Controllers (6 controllers, 24 actions — verified)

| Controller | Actions | Routes | Auth |
|---|---|---|---|
| `AuthController.cs` (352L) | 13 | GET/POST `/auth/sign-in`, GET/POST `/auth/sign-up`, GET/POST `/auth/forgot-password`, GET/POST `/auth/reset-password`, GET/POST `/auth/two-factor-auth`, POST `/auth/resend-otp`, POST `/auth/sign-out`, POST `/auth/sign-out-all` | `[AllowAnonymous]` |
| `AcceptInviteController.cs` (81L) | 2 | GET/POST `/auth/accept-invite` | `[AllowAnonymous]` |
| `DevicesController.cs` (32L) | 2 | GET `/Auth/Devices` (**redirects to Sessions — by design, keep**), POST `/auth/devices/trust/{deviceId:guid}` | `[Authorize]` |
| `SessionsController.cs` (44L) | 2 | GET `/Auth/Sessions`, POST `/auth/sessions/revoke/{sessionId:guid}` | `[Authorize]` |
| `ExternalAuthController.cs` (253L) | 3 | POST `/auth/external/challenge`, GET `/auth/external/callback`, POST `/auth/external/complete` | `[AllowAnonymous]` |
| `ExternalProvidersController.cs` (82L) | 2 | GET `/Auth/ExternalProviders`, POST `/auth/externalproviders/unlink/{providerId:guid}` | `[Authorize]` |

No duplicated-action collapse opportunity exists here (unlike Public's ReviewsController precedent): each controller has a distinct concern and ≤3 actions. **Do not merge controllers.**

### 1.4 Verified inventory

- **Views: 13 content views** + `_AuthLayout.cshtml` + `_ViewImports.cshtml` (6L, has Localizer inject) + `_ViewStart.cshtml` (7L, defaults `_AuthLayout`) + `Shared/_RecaptchaField.cshtml` (24L, CSP-safe).
  `Auth/{SignIn(137L), SignUp(138L), ForgotPassword(70L), ResetPassword(100L), TwoFactor(95L)}`, `AcceptInvite/{Index(52L), Expired(28L), InvalidLink(24L)}`, `Devices/Index(20L — dead orphan)`, `Sessions/Index(142L)`, `ExternalProviders/{Index(158L), Complete(39L)}`.
- **Facades: 11** — AcceptInvite, Devices, ExternalProviders, ForgotPassword, Login, LogoutAll, Logout, Register, ResetPassword, Sessions, VerifyEmail.
- **ApiClients: 11** — same names, 1:1 with facades.
- **Models: 41 files** across 10 folders (bespoke per-feature Result types with manual ModelState copy loops — see Phase 1.6).
- **Page scripts:** `auth-signup.js` (18L), `auth-resetpassword.js` (18L — **byte-for-byte duplicate of auth-signup.js**), `auth-twofactor.js` (79L), `external-complete.js` (8L), `recaptcha-field.js` (67L). Shared: `theme-bootstrap.js`, `api-client.js`, `form-ux.js`.
- **Area CSS:** `wwwroot/assets/css/auth.css` (19L — `.otp-input{letter-spacing:.5em}` ~L11–13, `.progress-thin{height:4px}` ~L17–19; clean, no physical props). `rtl.css` has **no** auth-specific sections.
- **Backend module:** `src/Modules/Auth` mounted at `/api/v1/auth` (`Auth.Presentation/AuthEndpoints.cs:17–24`). All current Web chains verified end-to-end (Facade → ApiClient → endpoint → CQRS handler) — see Phase 1 for the two missing capabilities this plan adds.
- **Web auth infra (do not break):** `PendingVerificationStore` (`Infrastructure/Authentication/SignIn/PendingVerificationStore.cs` ~L23/~L38, registered `Program.cs` ~L112); `WebSignInService.cs` ~L9 — cookie with exactly 4 claims (UserId/AccessToken/RefreshToken/RefreshTokenExpiresAt), persistent **8-hour** expiry, roles extracted per-request from the embedded JWT (`ExtractUserClaimsFromJwt` ~L81). Note: plan docs claim "30-day sliding" — the code says 8h persistent; this plan does **not** change cookie lifetime.

### 1.5 Per-phase verification protocol (every phase, no exceptions)

1. `dotnet build src/Hosts/YallaJo.Web/YallaJo.Web.csproj --no-incremental` — **baseline: 71 warnings / 0 errors. Zero NEW warnings.** Phases touching `src/Modules/Auth`/`YallaJo.Api` additionally build `src/Hosts/YallaJo.Api/YallaJo.Api.csproj` with zero new warnings.
2. Manual walkthrough of **every touched surface** in **EN-LTR and AR-RTL** (RTL4 merge gate), **light + dark** theme, at **390px and 1280px**.
3. No raw resx key text visible on any page (e.g. `Auth.Sessions.Revoked` rendered literally).
4. `grep` confirms no new inline `style=` (except X6-justified with comment) and no new inline `<script>` in the auth funnel (CSP posture).
5. Every AJAX-converted flow re-tested with JavaScript disabled — full PRG path must still work (PE1).
6. Anti-enumeration spot-check after Phases 1–2: forgot-password and resend-otp responses identical for existing vs. non-existing emails; two-factor page unreachable without the pending cookie.

---

## §2 View Reduction Table

Current: **13 content views**. Target: **13 → 11** (plus 3 new shared partials that delete ~120 lines of duplicated markup).

| View | Verdict | Mechanism | UX gain | SEO / route impact |
|---|---|---|---|---|
| `Auth/SignIn.cshtml` | **keep** (thin) | extract `_SocialAuthButtons` partial; add `data-loading` | faster perceived submit; one source of truth for social buttons | none — `/auth/sign-in` is `LoginPath`, must stay |
| `Auth/SignUp.cshtml` | **keep** (thin) | extract `_SocialAuthButtons` + `_PasswordMeterField` partials | consistent meter, less drift | none |
| `Auth/ForgotPassword.cshtml` | **keep** | — | data-loading + toast only | none |
| `Auth/ResetPassword.cshtml` | **keep** (thin) | extract `_PasswordMeterField` partial (currently duplicates SignUp's meter markup incl. duplicate `id="psw-input"`) | identical meter behavior everywhere | none |
| `Auth/TwoFactor.cshtml` | **keep** | resend → toast via `form-ux.js`; localized countdown via JSON island | clearer resend feedback | none — reachable only with pending cookie |
| `AcceptInvite/Index.cshtml` | **keep** (enhance) | add `_PasswordMeterField` (F3 parity with SignUp/Reset) | strength guidance during invite redemption | none |
| `AcceptInvite/Expired.cshtml` | **merge into `AcceptInvite/Status.cshtml`** | parameterized status view (`InviteStatusVm { Kind, Email? }`); views are 94% identical | one consistent, better-designed status card (icon + reason + CTA) | none — these are POST-result / fallback renders, not routable URLs; no redirects needed |
| `AcceptInvite/InvalidLink.cshtml` | **merge into `AcceptInvite/Status.cshtml`** | same | same | none |
| `Devices/Index.cshtml` | **retire (delete)** | dead orphan — `DevicesController.Index` already 301-redirects to Sessions (`controllers-by-plan.md` L14: controller stays, view is a documented orphan); precedent: Public Contact/Index2 dead-code deletion | removes a stale, never-rendered page that could drift | none — view is unreachable today |
| `Sessions/Index.cshtml` | **keep** (modernize) | AJAX revoke/trust/untrust/revoke-others via `WantsAjax()` + new `_SessionsTable` partial; mobile card layout at <768px | no full-page reload per row action; usable on 390px | none — future Accounts §3.10 will embed this surface; keep route stable |
| `ExternalProviders/Index.cshtml` | **keep** (modernize) | AJAX unlink via `WantsAjax()` + new `_ProvidersList` partial | instant unlink feedback | none — unlink deliberately lives in Auth area per `2-customer-dashboard.md` §3.10 |
| `ExternalProviders/Complete.cshtml` | **keep, re-layout** | switch from public `_Layout` to `_AuthLayout` (it is a sub-second auto-submit interstitial — full site chrome is wasted weight and visual noise); add spinner (L1) | faster paint mid-OAuth; coherent funnel look | none — `GET /auth/external/callback` renders it directly |

**Rejected reductions (with reasons):**
- *SignIn + SignUp into one tabbed view:* rejected — breaks browser autofill heuristics, breaks `LoginPath`/`AccessDeniedPath` deep links, and conflicts with the future MOD9 deep-linkable sign-in modal.
- *Sessions + ExternalProviders into one tabbed "Security" page:* rejected — `2-customer-dashboard.md` §3.10 already plans the consolidated tabs **in the Accounts area**; merging here would create a throwaway page and churn routes other plans cite.
- *TwoFactor absorbed into SignUp as a step:* rejected — the page is also the post-sign-in unverified-email gate; it must remain independently reachable via the pending-cookie flow (no-JS PE1 path).

---

## Phase 0 — Bugs & dead code (no behavior redesign)

**Files:** `Areas/Auth/Views/Devices/Index.cshtml` (delete), `Areas/Auth/Views/Sessions/Index.cshtml`, `Areas/Auth/Views/ExternalProviders/Index.cshtml`, `Areas/Auth/Controllers/{DevicesController,SessionsController,ExternalAuthController}.cs`, `wwwroot/assets/js/theme-bootstrap.js`, `Areas/Auth/Views/Shared/_AuthLayout.cshtml`, `Resources/SharedResource.{en,ar}.resx`.

- [ ] **Delete dead view** `Areas/Auth/Views/Devices/Index.cshtml` (20L orphan; `DevicesController.Index` ~L16 redirects to Sessions and stays). (Public precedent: Contact/Index2.)
- [ ] **Fix live double-confirm bug:** `Sessions/Index.cshtml` and `ExternalProviders/Index.cshtml` render the cross-area `Areas/Admin/Views/Shared/Partials/_ConfirmModal.cshtml` in `@section Scripts` while their layout (public `_Layout`) **also** loads `form-ux.js` — both register capture-phase `submit` handlers for `form[data-confirm]` with different re-entry flags (`data-confirmed` vs `data-yj-confirmed`) → **double modal interception**. Remove the `_ConfirmModal` partial render from both views; rely solely on the `form-ux.js` `data-confirm` modal (F8/MOD3/MOD5). Keep the existing `data-confirm`/`data-confirm-*` attributes (already localized).
- [ ] **Fix `theme-bootstrap.js` ~L38 logic bug:** `storedTheme !== 'light' || storedTheme !== 'dark'` is always true → change `||` to `&&`. Also wrap the file in an IIFE so a double include cannot throw a global redeclaration `SyntaxError` (JS4). This file is shared shell — change is allowed because it is listed here.
- [ ] **CON1: localize hardcoded controller flashes:** `DevicesController.cs` ~L26 `"Device marked as trusted."` and `SessionsController.cs` ~L38 `"Session revoked."` → `Localizer["Auth.Devices.Trusted"]` / `Localizer["Auth.Sessions.Revoked"]`; inject `IStringLocalizer<SharedResource>` into both controllers; add the two keys to **both** resx files (grep first — MSB3568).
- [ ] **CON1 (dev-facing, low priority but in-scope):** `ExternalAuthController.cs` ~L48 and ~L94 `BadRequest("Provider is not configured.")` → localized key `Auth.External.ProviderNotConfigured`.
- [ ] **Fix stale comment** in `_AuthLayout.cshtml` ~L29–33 (claims no local font files exist; they do). Comment-only edit here — the actual `site.css` wiring lands in Phase 2.

**Acceptance criteria:** build clean (0 new warnings); Sessions revoke and ExternalProviders unlink each show exactly **one** confirm modal; flashes render localized in EN **and** AR; deleted view causes no build/runtime reference errors; AR-RTL + EN-LTR walkthrough of Sessions/ExternalProviders unchanged visually (RTL4).
**Commit:** `fix(auth): remove dead Devices view, double-confirm modal, theme bug, hardcoded flashes [CON1, F8, JS4, T1]`

---

## Phase 1 — Structure, view reduction, AJAX & `[Backend]` additions

### 1.1 `[Backend]` Untrust device endpoint

There is currently no way to revoke device trust (`Device` entity has `Trust()` only — `src/Modules/Auth/Auth.Domain/Entities/Device.cs` ~L31–36; no command, no endpoint).

- **Contract:** `DELETE /api/v1/auth/devices/{deviceId:guid}/trust` → `204 No Content`; `404` if device not found / not owned by caller; auth required + permission `Device/Update` (same as trust — symmetric); owner-only check in handler. Owned by the **Auth module**.
- **Wiring:** `Device.Untrust()` domain method (mirror of `Trust()`) → `UntrustDeviceCommand` + handler in `Auth.Application/Commands/UntrustDevice/` (invalidate the sessions cache tag, mirroring `TrustDeviceCommandHandler`) → `MapDelete` in `Auth.Presentation/Endpoints/Device/DeviceEndpoints.cs` (~L17 has the trust `MapPatch` to mirror) → Web `DevicesApiClient.UntrustDeviceAsync(deviceId)` → `DevicesFacade.UntrustAsync` → new MVC action `POST /auth/devices/untrust/{deviceId:guid}` on `DevicesController` (localized flash `Auth.Devices.Untrusted`).
- [ ] `[Backend]` Add `Device.Untrust()` + `UntrustDeviceCommand`/handler + endpoint (`src/Modules/Auth`).
- [ ] Web: `DevicesApiClient.UntrustDeviceAsync` + `DevicesFacade.UntrustAsync` + `DevicesController` untrust action + "Remove trust" button in `_SessionsTable` (only on trusted devices), `data-confirm`.

### 1.2 `[Backend]` Revoke-other-sessions endpoint

"Sign out everywhere" today is only `POST /auth/logout-all`, which kills the **current** session too (`LogoutAllCommandHandler.cs` ~L26–40, no `sid` exclusion). The common UX need is "sign out other devices" without ending the current session.

- **Contract:** `POST /api/v1/auth/sessions/revoke-others` → `200 { "revokedCount": int }`; auth required + permission `Session/Delete`; handler revokes all of the caller's non-revoked, non-expired sessions **except** the one matching the JWT `sid` claim, plus their refresh tokens; invalidates the sessions cache tag. Owned by the **Auth module**. Additive — does not touch `logout-all` (existing consumers unbroken).
- **Wiring:** `RevokeOtherSessionsCommand` + handler in `Auth.Application/Commands/RevokeOtherSessions/` (model on `RevokeSessionCommandHandler` + `LogoutAllCommandHandler`) → `MapPost("/sessions/revoke-others")` in `Auth.Presentation/Endpoints/Session/SessionEndpoints.cs` (near ~L41 logout-all) → Web `SessionsApiClient.RevokeOthersAsync()` → `SessionsFacade.RevokeOthersAsync()` → new MVC action `POST /auth/sessions/revoke-others` on `SessionsController` → "Sign out other devices" button on Sessions page (`data-confirm`; flash/toast `Auth.Sessions.OthersRevoked` with count). Keep the existing "Sign out everywhere" (logout-all) button as the secondary, destructive option.
- [ ] `[Backend]` `RevokeOtherSessionsCommand` + handler + endpoint (`src/Modules/Auth`).
- [ ] Web: ApiClient + Facade methods, controller action, button + localized strings (both resx).

### 1.3 Sessions page AJAX modernization

**Files:** `SessionsController.cs`, `Sessions/Index.cshtml`, new `Areas/Auth/Views/Sessions/_SessionsTable.cshtml`, new `wwwroot/assets/js/auth-sessions.js`, `auth.css`.

- [ ] Extract the sessions table + empty state into `_SessionsTable.cshtml` (keep existing `<bdi dir="ltr">`/`.font-data` treatment of IPs and dates — RTL3).
- [ ] `SessionsController`: revoke / untrust / trust / revoke-others actions return `PartialView("_SessionsTable", vm)` when `WantsAjax()`, PRG otherwise (PE1). GET Index stays full view. Auth pages are NoStore (C2) — no `OutputCache` here.
- [ ] `auth-sessions.js` (loaded via `@section Scripts`, idempotent init JS4): intercept the row forms' submit **after** the `form-ux.js` confirm resolves (listen on the delegated re-submit), call `YallaJo.api.postForm`, swap the table container's innerHTML with the returned partial, fire `YallaJo.toast` with the localized message from a `data-success-msg` attribute (NF1). On error: toast danger + leave DOM unchanged. AbortController via api-client default (JS6).
- [ ] **390px responsiveness:** add a stacked card presentation for `<768px` (CSS-only, logical properties; e.g. `.sessions-table` rows become definition cards). New CSS goes in `auth.css` with logical properties only — no `rtl.css` entry should be needed (RTL1).
- [ ] Loading state: skeleton shimmer or `data-loading` on the row buttons (L1/L2).

### 1.4 ExternalProviders page AJAX modernization

**Files:** `ExternalProvidersController.cs`, `ExternalProviders/Index.cshtml`, new `_ProvidersList.cshtml` partial, new `wwwroot/assets/js/auth-providers.js`.

- [ ] Extract linked-providers list + available-providers cards into `_ProvidersList.cshtml`; move the `ProviderIcon`/`ProviderDisplay` view-code helpers into the partial.
- [ ] Unlink action: `WantsAjax()` → `PartialView("_ProvidersList", refreshed vm)` / PRG fallback. The 409 last-login-method conflict returns the existing localized error — surface it as a danger toast in AJAX mode, flash in PRG mode.
- [ ] `auth-providers.js`: same pattern as `auth-sessions.js`. Link flow (OAuth challenge/callback) stays full-navigation — **never AJAX an OAuth redirect**.

### 1.5 AcceptInvite consolidation (13 → 12 with Phase 0's deletion = net 11)

**Files:** `AcceptInviteController.cs`, delete `AcceptInvite/{Expired,InvalidLink}.cshtml`, new `AcceptInvite/Status.cshtml`, new `Areas/Auth/Models/AcceptInvite/InviteStatusVm.cs`.

- [ ] `InviteStatusVm { InviteStatusKind Kind /* InvalidLink | Expired */, string? Email }`.
- [ ] `Status.cshtml`: one well-designed status card — kind-specific icon, localized headline/body (`Auth.Invite.InvalidLink.*`, `Auth.Invite.Expired.*` — reuse existing keys where they exist, grep first), "contact your administrator" guidance for Expired (the API resend endpoint `POST /auth/invitations/resend` is admin-only — `InvitationEndpoints.cs` ~L165–182 — so **no self-service resend button**; do not invent one), and a primary "Back to sign-in" CTA (L6).
- [ ] `AcceptInviteController` ~L26/~L41: `View("InvalidLink")` → `View("Status", new InviteStatusVm(InvalidLink))`; `View("Expired", …)` → `View("Status", new InviteStatusVm(Expired, email))`.
- [ ] Add `_PasswordMeterField` (from 1.6) to `AcceptInvite/Index.cshtml` (F3 parity).

### 1.6 Funnel partial extraction (duplication kill)

**Files:** new `Areas/Auth/Views/Shared/_SocialAuthButtons.cshtml`, new `Areas/Auth/Views/Shared/_PasswordMeterField.cshtml`, `SignIn.cshtml`, `SignUp.cshtml`, `ResetPassword.cshtml`, `AcceptInvite/Index.cshtml`, new `wwwroot/assets/js/password-meter.js`, delete `auth-signup.js` + `auth-resetpassword.js`.

- [ ] `_SocialAuthButtons.cshtml` (model: returnUrl + mode): the ~30 duplicated lines of Google/Facebook challenge POST forms in SignIn/SignUp. Brand names "Google"/"Facebook" stay literal (brand names, not CON1 violations).
- [ ] `_PasswordMeterField.cshtml`: password input + eye toggle + meter (`.progress-thin`, `aria-valuenow`, `style="width: 0%" <!-- X6: dynamic computed value -->`). Parameterize the input id/name via the VM expression so duplicate `id="psw-input"` collisions cannot recur. Include `@using Microsoft.Extensions.Localization` before `@inject` (no guaranteed `_ViewImports` for Shared partials).
- [ ] `password-meter.js`: single replacement for the byte-identical `auth-signup.js`/`auth-resetpassword.js`; idempotent init via `data-wired` flag (JS4); wire by class/data-attribute, not hardcoded id, so multiple meters per page are safe. Update `@section Scripts` references in SignUp/ResetPassword/AcceptInvite Index. Delete the two old files.
- [ ] (Optional, only if zero-risk) `_AuthSplitCard.cshtml` layout partial for the shared split-card shell — **skip if it forces awkward section plumbing**; the two partials above are the mandatory wins.

**Acceptance criteria (whole phase):** Web **and** Api builds clean (0 new warnings each); view count = **11**; sessions revoke/trust/untrust/revoke-others work with JS (partial swap + toast, no full reload) **and** without JS (PRG + flash); unlink same; untrust + revoke-others verified against the running API (manual or integration test mirroring `TrustDeviceCommandHandler` tests); `logout-all` behavior unchanged; AcceptInvite invalid/expired/completed paths all render `Status.cshtml` correctly; password meter works on SignUp, ResetPassword, AcceptInvite; EN-LTR + AR-RTL, light+dark, 390/1280 on every touched page — table→card stack verified in **both** directions (RTL4); IPs/dates still render LTR inside RTL (RTL3).
**Commits (one per sub-block, e.g.):** `feat(auth): add untrust-device endpoint and wire Sessions UI [Backend, PE1, JS5, NF1, C2, RTL1]` · `feat(auth): revoke-other-sessions endpoint + sign-out-other-devices UX [Backend, F8, PE1]` · `refactor(auth): consolidate AcceptInvite status views and funnel partials [CON1, F3, X6, JS4]`

---

## Phase 2 — Conversion UX (funnel polish)

**Files:** `_AuthLayout.cshtml`, `SignIn.cshtml`, `SignUp.cshtml`, `ForgotPassword.cshtml`, `ResetPassword.cshtml`, `TwoFactor.cshtml`, `AcceptInvite/Index.cshtml`, `ExternalProviders/Complete.cshtml`, `external-complete.js`, `auth-twofactor.js`.

- [ ] **`_AuthLayout` foundation upgrade:** add `site.css` after `style.css` (~L43 region; brand fonts incl. IBM Plex Sans Arabic on the funnel — V3/V4/A5/X10; **rtl.css link must remain last** among the three), and load `api-client.js` + `form-ux.js` before `functions.js` (~L60 region) so the funnel gains `YallaJo.api`, toast, `data-loading`, `data-confirm`. Verify funnel page weight stays ≤ 300KB (per-page target for `/auth/sign-in`); these scripts are small and cached site-wide.
- [ ] **`data-loading` on every funnel submit** (L2/F7): SignIn, SignUp, ForgotPassword, ResetPassword, TwoFactor, AcceptInvite Index forms get `data-loading`. **Verify compatibility with `recaptcha-field.js`** (it intercepts submit, injects token, re-submits with `data-submitted` latch) — the spinner must engage on the *final* submit, not strand on the intercepted one; if double-engagement occurs, gate `form-ux.js` handling on the latch.
- [ ] **TwoFactor resend → toast:** replace the `#resendMsg` raw text div with `YallaJo.toast` success/danger (NF1/NF4); migrate `auth-twofactor.js` raw `fetch` (~L51, manual antiforgery ~L55) to `YallaJo.api.post` (JS5 — gains 10s timeout, X-Requested-With, 401 redirect); keep the 60s countdown on the button. (String localization for this file lands in Phase 3 — keep English fallbacks one more phase or do both together if convenient.)
- [ ] **`Complete.cshtml` re-layout:** switch to `_AuthLayout` (remove the public `_Layout` opt-in), add a centered spinner + localized "Completing sign-in…" line (L1) shown while `external-complete.js` auto-submits; keep the `noscript` submit button (PE1). Wrap `external-complete.js` in an IIFE with an idempotency guard (JS4).
- [ ] Confirm `Cache-Control: no-store` on all funnel + account responses (C2) — verify, add where missing.

**Acceptance criteria:** funnel pages render DM Sans/Poppins (EN) and IBM Plex Sans Arabic (AR) — visually verified both cultures; every funnel submit shows spinner + disabled button incl. with recaptcha enabled; resend shows toast in both cultures; OAuth completion shows branded funnel interstitial with spinner and still works with JS off; build clean; EN-LTR + AR-RTL + light/dark + 390/1280 pass (RTL4); page weight check on `/auth/sign-in` ≤ 300KB.
**Commit:** `feat(auth): funnel UX — brand fonts, data-loading, toast resend, funnel interstitial [V3, L1, L2, NF1, JS5, PE1, C2, RTL4]`

---

## Phase 3 — l10n & RTL hardening

Consumes the direction-fragile audit list. **Files:** `auth-twofactor.js`, `TwoFactor.cshtml`, `form-ux.js`, `auth.css`, `rtl.css`, `Resources/SharedResource.{en,ar}.resx`.

- [ ] **`auth-twofactor.js` CON1 strings** (~L39 `'Click to resend'`, ~L41 `'Resend in '+remaining+'s'`, ~L67 `'A new code has been sent.'`, ~L71/~L75 `'Could not resend code…'`): pass localized strings through the existing `#auth-twofactor-data` JSON island (extend the VM/view; keep the `// SANITIZED` comment discipline). The countdown template must be a localized format string (e.g. `"Resend in {0}s"` / `"إعادة الإرسال خلال {0} ث"`) and the rendered counter wrapped so digits don't reorder: set the button text via a `<bdi>`-wrapped or `dir="auto"` span (RTL3).
- [ ] **`form-ux.js` fallback strings** (~L169 `'Are you sure?'`, ~L172 `'Cancel'`, ~L174 `'Confirm'`, ~L61/~L137 `aria-label="Close"`): these only render when `data-confirm-*` attrs are omitted — audit all Auth-area `data-confirm` usages and ensure every one supplies localized `data-confirm-title/-message/-yes/-no` attributes from resx. (Changing `form-ux.js` defaults is shell-wide; permitted only as additive `data-*` lookups on `<html>` — otherwise leave the file alone and fix at call sites.)
- [ ] **Direction-fragile fix #1:** `auth.css` ~L11–13 — add `direction: ltr; unicode-bidi: isolate;` to `.otp-input` (defense-in-depth alongside markup `dir="ltr"`); optionally compensate trailing letter-spacing with `padding-inline-start: .5em; text-align: center;` (logical — RTL1).
- [ ] **Direction-fragile fix #2:** `rtl.css` §3 ≡ §7 exact duplicate blocks (~L90–107 ≡ ~L186–206, navbar dropdown re-anchor + notif-badge) — delete one copy.
- [ ] **Direction-fragile fix #3:** `rtl.css` ~L168–174 `[dir="rtl"] input[type="email"], …url…, …tel… { text-align: right; }` contradicts the Latin-LTR intent of its own section — change to `direction: ltr; text-align: left; /* RTL1-exception: Latin-only data, intentional physical */`.
- [ ] **Direction-fragile fix #4 (guard, not edit):** confirm no new auth CSS introduced by Phases 1–2 required an `rtl.css` entry (all logical). If any did, refactor to logical properties now.
- [ ] Resx hygiene sweep: every key added by Phases 0–2 exists in **both** files, flush-left, before `</root>`; grep for duplicates (MSB3568); UTF-8 intact (no PowerShell re-encode).

**Acceptance criteria:** zero hardcoded user-facing English in any Auth-area JS/view/controller (grep `'…'`/`"…"` literals in the five auth JS files); two-factor countdown renders correctly in AR (digits LTR, template localized); OTP input visually correct in AR; `rtl.css` smaller than before (dedupe) with no visual regression on Admin navbar (spot-check one Admin page since §3/§7 touch the shared navbar); full EN-LTR + AR-RTL pass of the entire funnel + Sessions + ExternalProviders (RTL4).
**Commit:** `fix(auth): localize remaining JS strings, harden OTP/RTL CSS, dedupe rtl.css [CON1, RTL1, RTL2, RTL3, RTL4]`

---

## Phase 4 — Accessibility

**Files:** all Auth views, `_SessionsTable.cshtml`, `_ProvidersList.cshtml`, `auth-sessions.js`, `auth-providers.js`, `password-meter.js`.

- [ ] Every form input: `label asp-for` + validation `role="alert"` + `aria-describedby` linkage (A11Y4) — already largely present; audit the new partials (`_PasswordMeterField`, `_SocialAuthButtons`, Status.cshtml) to the same bar.
- [ ] Password meter: `role="progressbar"` with `aria-valuemin/max/now` (existing) + a visually-hidden live region announcing strength tier changes (`aria-live="polite"`), localized tier names (weak/fair/strong).
- [ ] AJAX swap regions (`_SessionsTable`, `_ProvidersList` containers): `aria-live="polite"` + `aria-busy` during fetch so screen readers announce row changes; restore focus to a sensible element after swap (the table container or the action's nearest surviving sibling) — never let focus drop to `<body>`.
- [ ] Confirm modal flows: Esc closes (A11Y8 — `form-ux.js` modal is Bootstrap-native, verify), focus returns to invoking button on dismiss.
- [ ] Touch targets ≥ 44px for row action buttons on mobile card layout (D2).
- [ ] Social buttons: discernible names (`aria-label` localized if icon-only).
- [ ] Keyboard-only walkthrough of the full funnel: sign-up → OTP → sessions, plus revoke/unlink with confirm modals.

**Acceptance criteria:** keyboard-only run completes every flow; NVDA/VoiceOver spot-check announces validation errors, strength changes, and table refreshes; no focus loss after AJAX swaps; build clean; EN+AR / light+dark / 390+1280 pass (RTL4 — verify focus-visible outlines don't clip in RTL).
**Commit:** `feat(auth): a11y — live regions, focus management, labeled controls [A11Y4, A11Y8, D2, L1]`

---

## Phase 5 — Performance & final hardening

**Files:** `_AuthLayout.cshtml`, funnel views, (verification-only elsewhere).

- [ ] Font preload: `<link rel="preload" as="font" type="font/woff2" crossorigin>` in `_AuthLayout` for the 1–2 fonts above the fold per culture (DM Sans latin for EN; IBM Plex Sans Arabic 400 for AR — culture-conditional like the rtl.css link). Don't preload all 8 files (A-series weight discipline).
- [ ] Verify no preconnects beyond the allowed set (A5); the recaptcha origin gets `preconnect` only when recaptcha is enabled on that page.
- [ ] Defer/async audit of funnel scripts (A6): everything except `theme-bootstrap.js` (pre-paint by design, T5/X1 exception) should be `defer`.
- [ ] Page-weight measurement: `/auth/sign-in` ≤ 300KB, LCP ≤ 1.2s / TTI ≤ 2.0s on local Lighthouse run (M3 target — record numbers in the PR description; not a hard gate if infra-bound, but regressions vs. pre-plan baseline are).
- [ ] `NoStore` (C2) re-verified on all Auth responses incl. the AJAX partials.
- [ ] Final full-protocol regression: §1.5 steps 1–6 across every page in the area, both cultures, both themes, both widths.

**Acceptance criteria:** Lighthouse/page-weight numbers recorded; no regression vs. baseline; build clean (Web + Api); complete EN-LTR + AR-RTL matrix pass (RTL4).
**Commit:** `perf(auth): font preload, defer audit, cache verification [A5, A6, C2, M3, V4, T5]`

---

## Execution order, sizing & parallelization

| Order | Phase | Est. | Dependencies | Parallelizable? |
|---|---|---|---|---|
| 1 | Phase 0 — bugs/dead code | 0.5–1 d | none | — |
| 2 | Phase 1 — structure/AJAX/[Backend] | 3.5–4.5 d | Phase 0 (confirm-modal fix) | 1.1+1.2 (backend pair) parallel with 1.5+1.6 (funnel refactor); 1.3/1.4 depend on 1.1/1.2 endpoints |
| 3 | Phase 2 — conversion UX | 1.5–2 d | Phase 1.6 partials; `_AuthLayout` upgrade can start anytime after Phase 0 | layout upgrade ∥ per-view data-loading |
| 4 | Phase 3 — l10n/RTL | 1–1.5 d | Phases 1–2 (audits their output) | — |
| 5 | Phase 4 — a11y | 1 d | Phases 1–3 | — |
| 6 | Phase 5 — perf | 0.5–1 d | all prior | — |

**Total: ~8–11 days solo (≈ 2 working weeks).** With two agents: backend pair (1.1/1.2) and funnel refactor (1.5/1.6) concurrently → ~7 days.

---

## Hard rules for the executing agent

1. **Touch only:** `src/Hosts/YallaJo.Web/Areas/Auth/**`, the listed shell files (`wwwroot/assets/js/{theme-bootstrap,form-ux,external-complete,recaptcha-field,auth-*,password-meter,auth-sessions,auth-providers}.js`, `wwwroot/assets/css/{auth,rtl}.css`, `Areas/Auth/Views/Shared/_AuthLayout.cshtml`, `Resources/SharedResource.{en,ar}.resx`), and the **explicitly listed `[Backend]` additions** in `src/Modules/Auth` + `src/Hosts/YallaJo.Api` (untrust device, revoke-others). **Never touch other Web areas** (the Phase 0 removal of the Admin `_ConfirmModal` *render* happens in Auth views — do not edit the Admin partial itself).
2. Backend additions are **additive only** — never reshape `logout-all`, `trust`, or any existing endpoint; existing consumers must be unbroken.
3. Every AJAX conversion keeps the no-JS PRG path (PE1). Browser → MVC endpoints only, via `window.YallaJo.api` (JS5).
4. Both resx files for every key, grep-first (MSB3568), UTF-8, flush-left before `</root>`, never PowerShell re-encode.
5. **RTL4 merge gate:** EN-LTR + AR-RTL visual pass for every phase touching markup/CSS; logical properties only (RTL1); `[dir="rtl"]` overrides only in `rtl.css`; directional icons only flipped (RTL2); numbers/codes/IPs/dates LTR-isolated (RTL3).
6. No new dependencies, no CDNs, no inline `<script>` in the funnel (CSP), no inline `style=` without an X6 comment, no `SetSeo` on auth pages, `NoStore` everywhere (C2).
7. Never regress the security invariants in §1.2.10 (anti-enumeration, pending-cookie-only email, antiforgery on every POST, rate limits untouched).
8. One conventional commit per phase/sub-block, citing rule IDs.
9. `.gitignore` gotcha: `**/[Pp]ackages/*` is ignored — if any new path contains `Packages/`, add a negation (precedent at `.gitignore` ~L205–215).
10. Build baseline: **71 warnings / 0 errors** on `YallaJo.Web.csproj` — zero new warnings in any phase; same zero-new-warning bar for `YallaJo.Api.csproj` in Phase 1.
