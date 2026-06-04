# Auth & Security Hardening — Build Plan

> **Scope:** Polish + close gaps on the already-built `Areas/Auth` (+ `Areas/Accounts/Settings`, `Infrastructure/Authentication`). Not net-new flows.
> **Companion docs:** `UI-UX-Design.md` (§4.7 Wave-1 Auth, §22.1 Auth audit), `Accounts-Notifications-Support-Reviews-Plan.md` (Settings already owns notif prefs + 2FA phone).
> **Authoring rules:** `CONTROLLER_AUTHORING_GUIDE.md` (authoritative) — supersedes `WEB_LAYER_GUIDE.md`.
> **API base:** `/api/v1` · **Status:** Planned.

## Why this plan

`Areas/Auth` is built (82 files: SignIn, SignUp, ForgotPassword, ResetPassword, TwoFactor, AcceptInvite, Devices, ExternalProviders, Sessions). The §22.1 template-reality audit flagged a handful of **correctness + UX gaps** that don't belong to any feature plan:

- Template ships **Google/Facebook** social buttons but backend supports more providers — **no Apple**, and provider parity is unverified.
- `two-factor-auth.html` has a **4-box OTP** input but the backend issues a **6-digit** OTP — a real correctness bug.
- `sign-up.html` has no **password-strength meter** (weak-password UX).
- **External-provider link/unlink** isn't surfaced in account settings.
- **Session list + device-trust** management is thin vs the backend's `sessions` + `devices/trust` capability.

Mostly polish on built screens; one item (OTP length) is a genuine bug.

## Architecture conventions (layered by type)

Per `CONTROLLER_AUTHORING_GUIDE.md`. Note: **`Areas/Auth` is the layer-first reference area** — keep its existing layout: `Areas/Auth/{Controllers/, Facades/, ApiClients/, Models/{Feature}/, Views/{Controller}/}` + `Shared/_AuthLayout.cshtml`, `Shared/_RecaptchaField.cshtml`.

- **Pipeline:** four-tier `Controller → Facade → ApiClient → IApiClient`; errors as `ApiResult` values; `GuardSignOut` after each facade call.
- **DI:** suffix-based via `AddFeatureServices()` — keep `*ApiClient`/`*Facade` suffixes. The SignalR-style/OAuth handlers that are **not** HTTP `ApiClient`s stay registered explicitly in `Program.cs` (e.g. `WebSignInService`, external auth handlers) — don't rename them into the suffix convention.
- **Controllers:** `BaseController`; Auth self-service uses `[AllowAnonymous]`/`[Authorize]` (no `[RequirePermission]`); writes `[HttpPost]+[ValidateAntiForgeryToken]`; PRG + `SetSuccess`/`SetError`.
- **No output cache** on any auth/session screen.
- **Build:** `dotnet build src\Hosts\YallaJo.Web\YallaJo.Web.csproj` **ALONE** (CS2012).

### Design & UI skills (mandatory for all views)

- **ui-ux-pro-max + impeccable** — auth form hierarchy, inline validation, OTP entry ergonomics, error/empty states, a11y (labelled inputs, focus order, aria-live for errors), responsive.
- **design-taste-frontend** — the password-strength meter component + provider-button component architecture + performant CSS (no layout thrash on keystroke).
- **huashu-design** — hi-fi variant exploration for the OTP + strength-meter interactions before committing markup.
- Stay within Bootstrap 5 **Booking** template assets in `wwwroot/assets` (reuse `sign-in.html`, `sign-up.html`, `two-factor-auth.html` chrome).

## Template → page wiring

| Template page | Route | Change |
|---|---|---|
| `sign-in.html` | `/auth/login` | Add Apple button; ensure 3-provider parity |
| `sign-up.html` | `/auth/register` | Password-strength meter |
| `two-factor-auth.html` | `/auth/two-factor` (+ verify-email) | **Fix 4-box → 6-digit** OTP + resend |
| `account-settings.html` | `/account/settings` | Linked-accounts link/unlink panel |
| _(Sessions/Index built)_ | `/auth/sessions` | Device-trust + sign-out-all polish |

## Phase 0 — Grounding (do first)

- [ ] Read the existing `Areas/Auth` slices: `Controllers/{Auth,ExternalProviders,Sessions,Devices}Controller.cs` + their Facades/ApiClients/Models/Views to learn the exact built patterns.
- [ ] Inspect `Infrastructure/Authentication/` (Google + Facebook + `WebSignInService`) and `Program.cs` OAuth registration — determine whether **Apple** is wired server-side at all.
- [ ] Confirm backend endpoints/shapes in `src/Modules/Auth` + `src/Modules/Security`: `external-providers/login`, `external-providers` (link), `external-providers/{providerId}` (unlink), `sessions`, `sessions/{id}`, `logout-all`, `devices/{deviceId}/trust`, and the **OTP length** for `verify-email` / 2FA.
- [ ] For each gap (esp. Apple OAuth, device-trust) confirm existence before building; otherwise file `Agents/Gaps/Auth-Gaps.md` and mark ⛔.

## Phase 1 — OAuth provider parity + link/unlink

| Endpoint | Use |
|---|---|
| `POST /auth/external-providers/login` | Social sign-in (google/facebook/apple) |
| `POST /auth/external-providers` | Link provider to signed-in account |
| `DELETE /auth/external-providers/{providerId}` | Unlink provider |
| `GET /auth/external-providers` _(or via profile — confirm)_ | List linked providers |

**Files**
- [ ] Extend `Areas/Auth/ApiClients/ExternalProvidersApiClient.cs` + `Facades/ExternalProvidersFacade.cs` (link/unlink/list if missing).
- [ ] `Infrastructure/Authentication/` — add Apple handler **only if** backend supports it (else skip + gap note).
- [ ] `Areas/Auth/Views/ExternalProviders/Index.cshtml` — linked-accounts panel (provider rows + Unlink), surfaced via `/account/settings` link.
- [ ] Add the **Apple** button + ensure Google/Facebook on `Views/Auth/SignIn.cshtml` + `SignUp.cshtml` (component-consistent).

**Acceptance:** all configured providers sign in; signed-in user can link/unlink with PRG flash + antiforgery; unlink of the last credential is guarded (friendly error from `ApiResult.IsConflict`).

## Phase 2 — Fix OTP to 6 digits (correctness)

| Endpoint | Use |
|---|---|
| `POST /auth/verify-email` | Email OTP verify (6-digit) |
| `POST /auth/resend-otp` | Resend |
| 2FA verify (`two-factor`) | 6-digit code entry |

**Files**
- [ ] `Areas/Auth/Views/Auth/TwoFactor.cshtml` — replace the 4-box input with a **6-digit** entry (6 boxes or single `inputmode="numeric" maxlength="6"` with masking), auto-advance + paste-fill, `aria-label`s.
- [ ] Any verify-email view sharing the OTP partial → extract `Views/Auth/Partials/_OtpInput.cshtml` (length parameter) and reuse.
- [ ] `Models/TwoFactor/` (+ verify-email VM) — `[StringLength(6, MinimumLength = 6)]`, `[RegularExpression("\\d{6}")]`.
- [ ] Wire **resend** (`resend-otp`) with a cooldown timer (UX) + `SetSuccess`.

**Acceptance:** 6-digit entry submits successfully; client validation rejects ≠6 digits; resend works with cooldown; server `ApplyValidationErrors` authoritative; keyboard + paste friendly.

## Phase 3 — Password-strength meter (sign-up UX)

**Files**
- [ ] `Areas/Auth/Views/Auth/SignUp.cshtml` — add a strength meter bound to the password field (zxcvbn-style scoring, no backend call), with rule hints (length/case/number/symbol matching backend policy) + aria-live status.
- [ ] `wwwroot/assets/js/yallajo-auth.js` (or extend existing auth JS) — progressive enhancement; **form must work without JS** (server policy is authoritative on submit via `ApplyValidationErrors`).
- [ ] Mirror hints on `ResetPassword.cshtml`.

**Acceptance:** real-time strength + rule feedback; no submit blocked purely client-side; backend remains the source of truth; meets contrast/keyboard a11y.

## Phase 4 — Session & device-trust management

| Endpoint | Use |
|---|---|
| `GET /auth/sessions` | Active sessions list (device, IP, last-seen) |
| `DELETE /auth/sessions/{sessionId}` | Revoke one session |
| `POST /auth/logout-all` | Sign out everywhere |
| `PATCH /auth/devices/{deviceId}/trust` | Mark/unmark device trusted |

**Files**
- [ ] Extend `Areas/Auth/ApiClients/SessionsApiClient.cs` + `DevicesApiClient.cs` + their Facades (revoke / logout-all / trust if missing).
- [ ] `Areas/Auth/Views/Sessions/Index.cshtml` — enrich rows with current-session badge, **Revoke** per row, **Sign out all** button, **Trust/Untrust** toggle; confirm modals.
- [ ] `Partials/_SessionRow.cshtml`, `Partials/_RevokeAllModal.cshtml`.
- [ ] Surface entry from `/account/settings` "Active sessions" section.

**Acceptance:** list shows sessions; revoke/logout-all/trust each antiforgery + PRG; revoking the current session forces re-login via `GuardSignOut`; trust toggle reflects state after re-fetch.

## Out of scope / blocked

- Net-new auth flows (register/login/forgot/reset/accept-invite) — already built.
- 2FA **phone enrollment** + notification prefs — owned by `Accounts/Settings`.
- Apple OAuth UI if backend has no Apple provider → file `Agents/Gaps/Auth-Gaps.md`, ship Google/Facebook parity only.
- `reset-password-v2.html` template variant (duplicate styling) — not needed.

## Sequencing & effort

**P2 (OTP fix) first — it's a correctness bug.** Then P1 (provider parity/link-unlink), P4 (sessions/device-trust), P3 (strength meter, pure UX) last. Effort: P2 small, P1 small-medium (Apple uncertain), P4 medium, P3 small.

## Open questions

1. Is **Apple OAuth** supported server-side? (gates the Apple button + handler.)
2. Confirmed **OTP length** for verify-email vs 2FA — both 6?
3. Does `devices/{deviceId}/trust` exist and is a `GET /auth/external-providers` list endpoint available, or are linked providers read via the profile payload?
4. Password policy rules to mirror client-side (length/charset) — read from `src/Modules/Auth`/`Security`.
