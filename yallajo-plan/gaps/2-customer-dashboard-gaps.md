# Gap Report — `2-customer-dashboard.md` vs. shipped code

> **✅ STATUS: ALL 10 GAPS RESOLVED** (applied to `2-customer-dashboard.md`; re-verified against shipped `Areas/Accounts/Controllers/*` via direct grep + independent explore agent — two-source agreement). Resolution summary:
> - **G1** Header rewritten — Accounts is `[Authorize]` **plus** granular `[RequirePermission]` (11 controllers); every page's Stack now names the real constant (`Payment.*`, `Invoice.*`, `BookingDispute.Create`, `Refund.*`, `Review.*`, `AccessibilityReview.*`, `Recommendation.Read`, `Preference.*`, `Interaction.Create`, `Notification.*`, `SupportTicket.*`, `DeviceToken.*`).
> - **G2** §3.6 route → **`/accounts/recommendations`** (not `/discover`) + real `POST /accounts/recommendations/preferences|not-interested|track`.
> - **G3** §3.7 — **no `/accounts/billing`**; split into `/accounts/payments` + `/accounts/invoices`; **Pay Now → `POST /accounts/bookings/{id}/pay`**.
> - **G4** API-vs-BFF note added to header; all mutations shown as page-scoped `POST /accounts/*` (Accounts has **no `PUT`/`DELETE`/`PATCH`** routes).
> - **G5** §3.4 Remove → `POST /accounts/wishlist/remove/{entityType}/{entityId}` (+ `toggle`, `remove-all`).
> - **G6** §3.5 split into **§3.5a Reviews** (`/accounts/reviews`) + **§3.5b Accessibility Reviews** (`/accounts/accessibility-reviews`).
> - **G7** §3.10 — `ChangePasswordController` (`/accounts/changepassword`) + `UpdatePhoneController` (`/accounts/updatephone`) are separate; phone also at `/accounts/settings/phone`.
> - **G8** §3.6 onboarding flagged **unbuilt** (no BFF action in `RecommendationsController`).
> - **G9** §3.2 join-requests flagged **read-only** (`JoinRequestsController` GET-only; no create). Also §3.9 Support new-ticket-create noted unbuilt.
> - **G10** §3.10 linked accounts = **display + OAuth deep-link** (no in-area link/unlink POST; unlink via Auth `POST /auth/externalproviders/unlink/{providerId}`). **Bonus fix:** restore is `POST /accounts/delete/restore` (DeleteController), **not** `/accounts/profile/restore`.
>
 > **Code-implementation round (the 3 "unbuilt" behavioral gaps closed):**
> - **G8 Onboarding — IMPLEMENTED.** Added the full four-tier chain: `OnboardingApiRequest`/`OnboardingEntityRefApiRequest` records (exact wire names `InterestedEntityIds`/`NotInterestedEntityIds`/`Kind`/`EntityId`), `RecommendationsApiClient.SubmitOnboardingAsync` (`POST /analytics/recommendations/onboarding`), `RecommendationsFacade.SubmitOnboardingAsync`, `RecommendationsController.Onboarding` (`POST /accounts/recommendations/onboarding`, `[ValidateAntiForgeryToken]` + `Preference.Update`, PRG), `OnboardingFormVm` (`IValidatableObject`: 1..20, no empty GUIDs), a quiz card in `Recommendations/Index.cshtml`, and `wwwroot/assets/js/recommendations-onboarding.js`. Web builds clean.
> - **G9 Customer join-request create — IMPLEMENTED.** Added `SubmitJoinRequestApiRequest` record, `JoinRequestsApiClient.SubmitAsync` (`PostAsync<Guid>` → `POST /booking/join-requests`), `JoinRequestsFacade.SubmitAsync` (returns `ApiResult`), `JoinRequestsController.Create` (`POST /accounts/join-requests`, `[ValidateAntiForgeryToken]` + `JoinRequest.Create`, PRG), `JoinRequestFormVm` (DataAnnotations: `[Required]` GUIDs, `[Range(1,50)]`), and a "Request to join a group" form in `JoinRequests/Index.cshtml`. Web builds clean.
> - **Support new-ticket create — confirmed INTENTIONAL, doc-corrected (no code).** The shipped `SupportController` comment states creation is deliberately via the public `/contact` form (`ContactController` → `POST /api/v1/support/tickets`, `CreateTicketRequest(Category, Subject, Body)`). Adding a duplicate `/accounts/support` create would violate "don't reinvent." Plan §3.9 now points ticket-create at `/contact`. **Net: G8 + G9 = code; Support = doc.**
>
 > **Oracle-verification round (2 extra defects found & fixed):** (a) §3.1 "Mark all read" was labeled an **API** route — it's a shipped **BFF route** `POST /notifications/read-all` (`NotificationsController`, `Notification.Update`; mounted at `notifications/*`, not `accounts/notifications/*`). (b) §3.10 Privacy claimed a "Save Privacy Prefs → `PUT /analytics/preferences`" action — **no such save action ships** (`PrivacyController` is read/export/delete-data/cancel-deletion only); button removed and flagged unbuilt (recommendation-prefs save is §3.6).
>
> *Findings below retained for the historical audit trail.*

---


> **Method:** deep code-vs-plan audit against the **shipped** `src/Hosts/YallaJo.Web/Areas/Accounts/Controllers/*` (18 controllers).
> The API route table is the backend contract; this report is about the **BFF (MVC) layer** the browser actually talks to.
>
> **Scope:** `yallajo-plan/2-customer-dashboard.md` (§3 customer dashboard).
> **Severity:** 🔴 plan contradicts code · 🟠 route/verb shape wrong (would mislead a builder) · 🟡 cosmetic/clarity.
> **Status:** ❌ not built · ✏️ built, route/verb/perm differs · ➗ split differently · ✅ matches.

---

## 0. Summary

| Topic | Verdict |
|-------|---------|
| **Permissions** | 🔴 **Plan header is wrong.** It says Accounts is "`[Authorize]` … API enforcing per-user ownership" with **no `WebPermission`**. The shipped controllers **do** use granular `[RequirePermission]` on most write/read actions. |
| **BFF routes** | 🟠 Many page routes differ: `/accounts/discover`→**`/accounts/recommendations`**; `/accounts/billing`→**`/accounts/payments`** (+ pay = `/accounts/bookings/{id}/pay`); reviews split into two pages. |
| **BFF verbs** | 🟠 All mutations are page-scoped **`POST`** (e.g. `POST /accounts/reviews/edit`, `/accounts/wishlist/toggle/...`), not the API `PUT`/`DELETE`/`PATCH` the plan lists. |
| **Reviews split** | ➗ `ReviewsController` (`/accounts/reviews`) and `AccessibilityReviewsController` (`/accounts/accessibility-reviews`) are **separate** pages; plan folds both into §3.5. |
| **Settings** | ✏️ One `SettingsController` GET landing + many `POST /accounts/settings/*` sub-actions; password/phone are **separate controllers** (`ChangePassword`/`UpdatePhone`). |
| Cache (`NoStore`) + `[Authorize]` baseline | ✅ Correct everywhere. |

---

## 1. 🔴 Plan-contradicts-code

### G1 — Header claim "no WebPermission, just `[Authorize]`" is false
- **Plan says (line 9):** *"every page is `NoStore` and `[Authorize]` with the API enforcing per-user ownership"* — implying no `WebPermission` constants.
- **Code reality:** the shipped Accounts controllers gate with **granular `[RequirePermission]`** (class-level read + per-action). Verified map:

  | Page / action | Shipped `[RequirePermission]` |
  |---------------|-------------------------------|
  | §3.3 Raise Dispute (booking) | `BookingDispute.Create` |
  | §3.3/§3.7 Pay Now | `Payment.Create` |
  | §3.7 Payments list | `Payment.Read` |
  | §3.7 Invoices list / download | `Invoice.Read` / `Invoice.Download` |
  | §3.8 Disputes list / open | `Refund.Read` / `Refund.Create` |
  | §3.5 Reviews list / edit / delete | `Review.Read` / `Review.Update` / `Review.Delete` |
  | §3.5 Accessibility reviews list / delete | `AccessibilityReview.Read` / `AccessibilityReview.Delete` |
  | §3.6 Recommendations read | `Recommendation.Read` |
  | §3.6 prefs / not-interested | `Preference.Update`; track = `Interaction.Create` |
  | §3.1/bell Notifications read / delete | `Notification.Read` / `Notification.Delete` |
  | §3.9 Support read / close | `SupportTicket.Read` / `SupportTicket.Close` |
  | §3.10 Privacy export/delete | `Preference.Read` / `Preference.Update` |
  | §3.10 Devices add / remove | `DeviceToken.Create` / `DeleteToken.Delete` |

  *(Pages still `[Authorize]`-only with no `[RequirePermission]`: §3.1 Overview, §3.2 My Trips list/cancel, §3.4 Wishlist, §3.10 Profile/Delete/ChangePassword/UpdatePhone/Settings notifications/marketing/sessions.)*
- **Action:** Replace the header's "no WebPermission" framing with: *"Accounts gates with `[Authorize]` **plus** granular `WebPermission.{Feature}.{Action}` on most read/write actions (see per-page Perm)."* Add the real constant to each affected page's Stack.

---

## 2. 🟠 Route / verb gaps

### G2 — §3.6 route is `/accounts/recommendations`, not `/accounts/discover`
- **Plan:** Route `/accounts/discover`.
- **Code (`RecommendationsController`):** `GET /accounts/recommendations`; actions `POST /accounts/recommendations/preferences` (`Preference.Update`), `POST .../not-interested` (`Preference.Update`), `POST .../track` (`Interaction.Create`).
- **Note:** the **onboarding** button (`POST /analytics/recommendations/onboarding`) has **no shipped BFF action** — only preferences/not-interested/track exist. Onboarding is currently a **gap** (see G7).
- **Action:** route → `/accounts/recommendations`; buttons → the real `POST /accounts/recommendations/*`; mark onboarding unbuilt.

### G3 — §3.7 Billing is split: `/accounts/payments` + pay on the booking
- **Plan:** Route `/accounts/billing`; Pay Now → `POST /payments/initiate`.
- **Code:** **no `/accounts/billing` route exists.** Instead:
  - Payments list: `GET /accounts/payments` (`PaymentsController`, `Payment.Read`).
  - Invoices list + download: `GET /accounts/invoices`, `GET /accounts/invoices/{id}/download` (`InvoicesController`, `Invoice.Read`/`Download`) — a **separate** surface.
  - **Pay Now: `POST /accounts/bookings/{id}/pay`** (`BookingsController`, `Payment.Create`) — pay is on the **booking**, not a billing page.
- **Action:** split §3.7 into the real routes; Pay Now → `POST /accounts/bookings/{id}/pay`.

### G4 — All §3 mutations are page-scoped `POST` (not API `PUT`/`DELETE`/`PATCH`)
- **Code reality (representative):**
  - Bookings: `POST /accounts/bookings/{id}/cancel` · `/dispute` · `/pay`
  - Reviews: `POST /accounts/reviews/edit` · `POST /accounts/reviews/{id}/delete`
  - Accessibility: `POST /accounts/accessibility-reviews/{id}/delete`
  - Wishlist: `POST /accounts/wishlist/toggle/{entityType}/{entityId}` · `/remove/{entityType}/{entityId}` · `/remove-all`
  - Notifications: `POST /accounts/notifications/{id}/delete`
  - Privacy: `POST /accounts/privacy/delete-data` · `/cancel-deletion`
  - Profile: `POST /accounts/profile/update` · `/avatar` · `/avatar/delete` · `/delete` · `/delete/restore`
  - Settings: `POST /accounts/settings/notifications` · `/marketing` · `/phone` · `/sessions/revoke/{sessionId}` · `/devices/trust/{deviceId}` · `/logout-all` · `/devices` · `/devices/{id}/remove`
- **Action:** add the API-verb-vs-BFF-POST note (as in §3–§8 fixes); the plan's `PUT`/`DELETE`/`PATCH` are valid only at the **ApiClient→API** layer.

### G5 — §3.4 Wishlist remove verb/route
- **Plan:** Remove → `DELETE /social/favorites/{entityType}/{entityId}`.
- **Code:** `POST /accounts/wishlist/remove/{entityType}/{entityId}` (+ `toggle`, `remove-all`). The DELETE is the API call inside `WishlistApiClient`.
- **Action:** button → `POST /accounts/wishlist/remove/...` (and the public-page heart targets `POST /accounts/wishlist/toggle/...` — cross-area; see storefront gap G2).

---

## 3. ➗ Structural-split gaps

### G6 — §3.5 My Reviews is two shipped pages, not one
- **Plan:** §3.5 folds standard reviews **and** accessibility reviews into one page.
- **Code:** two controllers/pages:
  - `ReviewsController` → `GET /accounts/reviews` (+ `edit`, `{id}/delete`), perms `Review.*`.
  - `AccessibilityReviewsController` → `GET /accounts/accessibility-reviews` (+ `{id}/delete`), perms `AccessibilityReview.*`.
- **Action:** split §3.5 into §3.5a Reviews + §3.5b Accessibility Reviews (or note the two routes explicitly). Note: the shipped accessibility page exposes **list + delete** only (no edit action found — edit may be public-side per storefront `…/accessibility-reviews/{id}/edit`).

### G7 — §3.10 password & phone are separate controllers/pages
- **Plan:** Security tab change-password / change-phone inline under `/accounts/settings`.
- **Code:** `ChangePasswordController` (`GET/POST` convention route, e.g. `accounts/changepassword`) and `UpdatePhoneController` (`accounts/updatephone`) are **dedicated controllers** with their own VMs; `SettingsController` also has `POST /accounts/settings/phone`. So phone has **two** entry points.
- **Action:** note these are separate pages; confirm which phone route is canonical (`/accounts/settings/phone` vs `UpdatePhone.Index`).

---

## 4. ❌ Behavioral gaps (plan feature with no shipped action)

### G8 — Onboarding quiz has no BFF action (§3.6)
- `RecommendationsController` exposes preferences/not-interested/track but **no `onboarding`** action. The plan's "Start Onboarding → `POST /analytics/recommendations/onboarding`" is **unbuilt** at the BFF layer.
- **Action:** mark onboarding as a future item or add a `POST /accounts/recommendations/onboarding` action.

### G9 — Join Requests is read-only in Accounts (§3.2 / §3.8-style)
- **Plan:** §3.2 "Request to Join → `POST /booking/join-requests`".
- **Code (`JoinRequestsController`):** only `GET /accounts/join-requests` ships — **no POST create** in the Accounts area. (Approve/reject live in the **Guide** area, §5.10.)
- **Action:** clarify the customer-side join page is **read-only** (list own requests); the create flow, if intended, is unbuilt here.

### G10 — Linked accounts: no link/unlink action in Accounts settings
- **Code:** `SettingsController` only **renders** the supported providers (Google/Facebook) + link state from `SettingsVm`; **linking happens through the sign-in OAuth flow** (`Auth` area `challenge`/`callback`/`complete`), and there's no `POST /accounts/.../link` action. Unlink is `Auth` area (`POST /auth/externalproviders/unlink/{providerId}`).
- **Action:** §3.10 Linked-accounts tab is **display + deep-link to OAuth**, not an in-page link/unlink POST. Matches §11 #6 (no GET-list) but also no in-area mutate.

---

## 5. ✅ Confirmed-correct

- **Cache:** every Accounts page is `NoStore` (global base policy) ✅.
- **`[Authorize]`** on every controller ✅; API enforces ownership ✅.
- **§3.3 dispute model** (booking-dispute vs payment-dispute) — confirmed: booking dispute = `POST /accounts/bookings/{id}/dispute` (`BookingDispute.Create`); payment dispute = `POST /accounts/disputes` (`Refund.Create`). The plan's two-objects note is **correct** ✅.
- **§3.4 `FavoriteEntityType` = 5 values** ✅ (matches `Social.Domain/Enums/FavoriteEntityType.cs`).
- **§3.3 `ST1`-dropped** (UpdatedAt only, no RowVersion) ✅.

---

## 6. Recommended plan edits (apply order)

1. **G1** rewrite header — Accounts DOES use granular `WebPermission`; add the real constant to each page's Stack.
2. **G2/G3** fix routes: §3.6 → `/accounts/recommendations`; §3.7 → `/accounts/payments` + `/accounts/invoices`, Pay Now → `POST /accounts/bookings/{id}/pay`.
3. **G6** split §3.5 into Reviews + Accessibility-Reviews pages.
4. **G4/G5** add API-vs-BFF note; replace bare `/social/*`,`/payments/*`,`/notifications/*` button targets with the page-scoped `POST /accounts/*` routes.
5. **G7** note password/phone are separate controllers.
6. **G8/G9/G10** flag onboarding, join-request-create, and in-area account-linking as **unbuilt** (display/deep-link only).

> **Net:** the customer plan is endpoint-accurate but its **header permission claim is wrong** (Accounts is permission-gated, not bare `[Authorize]`), several **page routes diverge** (`/discover`, `/billing` don't exist), reviews are **split into two pages**, and three features (**onboarding, customer join-request create, in-area account linking**) are **not wired** in the shipped Accounts area. G1 is the headline correction; G8–G10 are the real behavioral gaps.

---

## 7. Re-audit verification round (full code-first re-sweep) — NO REGRESSIONS, ZERO OPEN GAPS

> **Method:** independent code-first re-audit of **all 18 Accounts controllers** + their Facades / ApiClients / VMs + backend NRE-risk command paths, verified by direct `Read` (no reliance on explore agents — both hit usage limits). Each action checked against the four-tier rule set: `[Area("Accounts")]` + `[Authorize]` (+ granular `[RequirePermission]` where applicable), `sealed`, `BaseController`, **Facade-only injection** (no `IApiClient`/`HttpClient`), `async Task<IActionResult>` + trailing `CancellationToken`, `GuardSignOut` after every facade call, **GET-reads / POST-writes + `[ValidateAntiForgeryToken]` + PRG**, NoStore (global `NoCache()` base policy), and required-field validators on any POST whose backend `.Trim()`s/dereferences a string.

**Prime re-verify targets (per ledger instruction) — both PASS end-to-end:**
- **Onboarding chain (G8)** — all four tiers correct: `RecommendationsController.Onboarding` (`POST /accounts/recommendations/onboarding`, `[ValidateAntiForgeryToken]` + `Preference.Update`, ModelState guard, GuardSignOut, PRG) → `RecommendationsFacade.SubmitOnboardingAsync` (token-parse → `OnboardingEntityRefApiRequest(Kind, EntityId)` → `OnboardingApiRequest`, Normalize handles 401/422/500) → `RecommendationsApiClient.SubmitOnboardingAsync` (`POST /api/v1/analytics/recommendations/onboarding`) → wire DTO names `InterestedEntityIds`/`NotInterestedEntityIds`/`Kind`/`EntityId` exact; `RecommendationEntityType:byte` 1-based mirrors `Analytics.Domain.Enums.EntityType`; `OnboardingFormVm` `IValidatableObject` enforces 1..20 + no `Guid.Empty`. ✅
- **Join-request create (G9)** — all four tiers correct: `JoinRequestsController.Create` (`POST /accounts/join-requests`, `[ValidateAntiForgeryToken]` + `JoinRequest.Create`, ModelState guard, GuardSignOut, PRG) → `JoinRequestsFacade.SubmitAsync` (`SubmitJoinRequestApiRequest`, GUIDs guaranteed non-empty by VM) → `JoinRequestsApiClient.SubmitAsync` (`PostAsync<Guid>` → `POST /api/v1/booking/join-requests`); `JoinRequestFormVm` has `[Required]` GUIDs + `[Range(1,50)]`. ✅

**Bookings / Payments / Invoices / Disputes chain — ZERO gaps.** Controllers, Facades, ApiClients, VMs all compliant. **Backend NRE-risk paths verified safe:** `CancelTourBookingCommand` handler uses null-safe `request.Reason?.Trim()`; `OpenBookingDisputeCommand` validator has `Reason NotEmpty + MinLength(10) + MaxLength(2000)`; `OpenDisputeApiRequest` (payment-dispute) guarded by `OpenDisputeFormVm` `[Required]` (string defaults `string.Empty`). Booking-dispute (Completed-only + 48h, owner-scoped) vs payment-dispute (`POST /accounts/disputes`, `Refund.Create`) correctly kept distinct.

**Remaining 14 controllers (Wishlist, Reviews, AccessibilityReviews, Notifications, Overview, Support, Settings, Profile, Privacy, Delete, ChangePassword, UpdatePhone) — ALL compliant.** Notable confirmations: mark-all-read lives in non-area `Features/Notifications/NotificationsController` (`POST /notifications/read-all`, `Notification.Update`) per §3.1; Privacy has read/export/delete-data/cancel-deletion only (no prefs-save); Settings linked-accounts are display-only; `DeleteController.Restore` is `POST /accounts/delete/restore`; every string-dereferencing POST (Support reply, Settings RegisterDevice, Booking OpenDispute) validates non-blank at the controller before any `.Trim()`.

**Build + changed-path tests (evidence):**
- `dotnet build src\Hosts\YallaJo.Web` → **Build succeeded, 0 Warnings, 0 Errors**.
- `dotnet build YallaJo.sln` → **Build succeeded, 0 Errors** (16 pre-existing warnings, none in changed paths).
- `Web.Tests.Unit` → **464/464 passed**; `Booking.Tests.Unit` → **267/267**; `Analytics.Tests.Unit` → **26/26**; `Social.Tests.Unit` → **2/2**; `Accounts.Tests.Unit` → **73/73**. **Total 832 passed, 0 failed.**

> **Conclusion:** No regression on any RESOLVED gap; no new code gap (MISSING/INCOMPLETE/INCORRECT/NON-COMPLIANT) found across the full Customer Dashboard stack. **Customer Dashboard is at zero open code gaps and full architecture-rule compliance.**

---

## 8. Round-2 re-audit verification (independent 4-agent code-first re-sweep) — NO REGRESSIONS, ZERO OPEN GAPS

> **Method:** four independent anthropic code-first audit agents (BFF controllers · Facades/ApiClients/VMs · backend NRE-risk+ownership+concurrency+module-isolation · end-to-end onboarding+join-request chains) over the shipped `Areas/Accounts/*` + backing modules (Booking, Social, Messaging, Analytics, Finance, Accounts). **Every agent flag was re-verified against the actual source via direct `Read`/grep before being accepted or dismissed** — agents over-flag. Result: all flags are false-positives or out-of-scope; **zero open code gaps**. Prime re-verify targets (onboarding, customer join-request create) **PASS end-to-end with no regression**.

### Prime targets — both intact (file:line evidence)
- **Onboarding (G8)** — 4 tiers correct: `RecommendationsController.Onboarding` (`POST /accounts/recommendations/onboarding`, `[ValidateAntiForgeryToken]`+`[RequirePermission(WebPermission.Preference.Update)]`, ModelState guard, `GuardSignOut`, PRG, trailing `ct`) → `RecommendationsFacade.SubmitOnboardingAsync` (parses `Kind:EntityId` tokens via `OnboardingFormVm.ParseToken` → `OnboardingEntityRefApiRequest(Kind, EntityId)`, builds `OnboardingApiRequest`, `NormalizeAsync`) → `RecommendationsApiClient.SubmitOnboardingAsync` (`POST /api/v1/analytics/recommendations/onboarding`) → wire DTO `OnboardingApiRequest(InterestedEntityIds[], NotInterestedEntityIds[])` each `{Kind:RecommendationEntityType (1-based byte: Tour=1/Place=2/Business=3/Category=4), EntityId}` → backend `SubmitOnboardingResponsesCommand` + validator (`UserId NotEmpty`, `Responses NotEmpty + ≤20`, each `EntityId NotEmpty`), endpoint authenticates `currentUser` + `MustHavePermission(Preference, Update)`. `OnboardingFormVm : IValidatableObject` enforces 1..20 total + rejects `Guid.Empty` + rejects malformed tokens. ✅
- **Join-request create (G9)** — 4 tiers correct: `JoinRequestsController.Create` (`POST /accounts/join-requests`, `[ValidateAntiForgeryToken]`+`[RequirePermission(WebPermission.JoinRequest.Create)]`, ModelState guard, `GuardSignOut`, PRG, `ct`) → `JoinRequestsFacade.SubmitAsync` (`SubmitJoinRequestApiRequest`, trims Message, 401→ForceSignOut) → `JoinRequestsApiClient.SubmitAsync` (`PostAsync<Guid>` → `POST /api/v1/booking/join-requests`) → backend `SubmitJoinRequestCommandHandler` enforces ownership (`currentUser.UserId!.Value`; blocks joining own booking via `booking.UserId == userId`; requires `Confirmed` booking; no-duplicate-pending; slot-capacity) → `JoinRequest.Create(... request.Message ...)` (plain assignment, no `.Trim()`). `JoinRequestFormVm` has `[Required]` GUIDs + `[Range(1,50)]` ParticipantCount + `IValidatableObject` rejecting `Guid.Empty`. ✅ `WebPermission.Preference.Update`/`JoinRequest.Create`/`Interaction.Create` all exist in `WebPermission.cs`.

### Agent flags — all DISMISSED with code-first evidence (NOT gaps)
- **BFF `BookingsController.Cancel` has no `[RequirePermission]`** → by design (plan §3.2: My Trips list/cancel are `[Authorize]`-only). Not a gap.
- **BFF `SupportController.Reply` gated `SupportTicket.Read` on a write** → by design (plan §3.9 explicitly maps `reply = SupportTicket.Read`). Not a gap.
- **BFF `NotificationsController.Index` "missing `GuardSignOut`"** → false positive. `NotificationsFacade.GetInboxAsync` (`Features/Notifications/NotificationsFacade.cs`) is the documented ERR3/FE-1B safe-degrade **VM-returning** read (returns `NotificationsInboxVm` with `.LoadError`, never 500s) — there is no `GuardSignOut(vm)` overload; the **mutating** facade methods (`DeleteAsync` line 101, `MarkRead`/`MarkAllRead` via `Normalize`) correctly map 401→`ForceSignOut()` and the controller `GuardSignOut`s them. Not a gap.
- **Facade `SupportFacade.cs` / `PrivacyFacade.cs` "missing `using Microsoft.Extensions.Logging;`"** → false positive. `YallaJo.Web.csproj` has `<ImplicitUsings>enable</ImplicitUsings>` (Web SDK), which provides `Microsoft.Extensions.Logging` as a global using; **`dotnet build YallaJo.sln` = 0 errors** proves `ILogger<T>` resolves. Not a gap.
- **Backend Finance `OpenDisputeCommand` (Reason/Description) "missing validator → CRITICAL"** → false positive (not a RULE-10 NRE→500). `Dispute.Open()` (`Finance.Domain/Entities/Dispute.cs`) **guards `if (string.IsNullOrWhiteSpace(reason)) return Result.Failure("Dispute.ReasonRequired", Outcome.Invalid)` at line 41 BEFORE `reason.Trim()` at line 50**, and `Description = description?.Trim() ?? string.Empty` (line 51) is null-safe. A null/empty input yields a clean `Outcome.Invalid` failure, never an NRE/500. (BFF `OpenDisputeFormVm` also enforces `[Required]`.) Per the mission's hard constraint (mandatory validator only where the domain `.Trim()`s/dereferences UNGUARDED → NRE→500), this is not a code gap. Not a gap.
- **Backend `SubmitJoinRequestCommand` (Message) "missing validator → FAIL"** → false positive. `JoinRequest.Create()` stores `Message = message` (plain assignment, line 57; `Message` is `string?`) — **no `.Trim()`, no dereference** → no NRE→500. (BFF `JoinRequestFormVm.Message` has `[StringLength(500)]`; facade trims.) Not a RULE-10 gap.
- **Backend `AddReviewReplyCommand` "missing provider ownership"** → out of scope. Replying to a review is a **Provider-area** action (provider responding), not a customer-dashboard surface (§3 customer reviews = the customer's own edit/delete, both of which DO enforce `review.UserId == CallerUserId` ownership). Noted for a Provider/Social audit; not a Customer Dashboard gap.
- **`DeleteAccessibilityReviewCommand` no validator / `SetUserPreferencesCommand` `BudgetTier`+`CurrentTripStage` no `MaxLength` / various handlers no `DbUpdateConcurrencyException` catch** → not RULE-10 NRE risks (no unguarded `.Trim()`/deref on those fields; IDs are GUIDs; handlers validate ownership) and not mandated by plan/rules (plan §3.3 uses `UpdatedAt`, no RowVersion round-trip required). Consistency/quality nits, not code deficiencies. Not gaps.
- **Module isolation** — confirmed: no cross-module DB FK (Finance `Payment.BookingId` is a reference value, not an FK constraint). ✅
- **Ownership** — every customer-dashboard write command enforces per-user ownership/user-scope: `CancelTourBooking`, `OpenBookingDispute` (owner-only), `SubmitJoinRequest`, Finance `OpenDispute` (`payment.UserId == request.UserId`), `EditReview`/`DeleteReview` (`review.UserId == CallerUserId` / +admin), `DeleteAccessibilityReview`, `PostTicketMessage`/`CloseSupportTicket` (`ticket.CreatedByUserId == CallerUserId` / +admin), `DeleteNotification` (`notification.UserId == CallerUserId`), Analytics onboarding/not-interested/preferences (user-scoped). ✅

### Build + changed-path tests (evidence, this round)
- `dotnet build src\Hosts\YallaJo.Web` → **Build succeeded, 0 Errors**.
- `dotnet build YallaJo.sln` → **Build succeeded, 0 Errors** (only pre-existing benign NU1603/NU1902 warnings).
- Unit suites: **Web.Tests.Unit 482/482 · Booking.Tests.Unit 267/267 · Analytics.Tests.Unit 26/26 · Social.Tests.Unit 2/2 · Accounts.Tests.Unit 94/94 · Finance.Tests.Unit 3/3 · Messaging.Tests.Unit 2/2 = 876 passed, 0 failed.**

> **Round-2 conclusion:** Independent 4-agent code-first re-sweep found **no regression** on any RESOLVED gap and **no new code gap**. All ~10 agent flags were false-positives or out-of-scope, each dismissed against the actual source. **Customer Dashboard remains at ZERO open code gaps with full architecture-rule compliance.**
