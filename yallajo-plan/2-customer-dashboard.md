# Area 2 — Customer / Tourist Dashboard

> **Actor:** Customer / Tourist (authenticated)
> **Plan section:** §3 of `yallajo-dashboard-ui-ux-plan.md`
> **Namespaces:** `accounts`, `booking/my-*`, `booking/join-requests`, `social`,
> `analytics/recommendations`, `payments/my-*`, `invoices/my-*`, `disputes/my`,
> `support`, `notifications`, `devices`.
> Status legend: ✅ Wire · ♻️ Repurpose · ⏭️ Skip · 🟥 USER builds.
> **Architecture & rules:** see [`0-architecture-and-rules.md`](0-architecture-and-rules.md). **Code area `Accounts`** (`/accounts/*`); every page is `NoStore` (`UI-PERF-C2`) and `[Authorize]` with the API enforcing per-user ownership.

## Shared shell
Top bar: global search · language switcher · notification bell (`GET /notifications/unread-count` `AJAX⟳`) · avatar menu (`GET /accounts/profile`). Nav driver: `GET /security/me`.

## Pages this area should have (10)

| # | Page | Template | Redirects to |
|---|------|----------|--------------|
| 3.1 | **Overview** `/me` | `account-bookings.html` ✅ (account-* shell) | §3.2–§3.10 · §3.3 booking · §3.4 favorites · §2.5/§2.3 recs · bell |
| 3.2 | **My Trips** `/me/bookings` | `account-bookings.html` ✅ · `tour-booking.html` ✅ (checkout) | §3.3 detail · `tour-booking.html` → §3.7 payment · cancel inline |
| 3.3 | **Booking Detail** | `booking-confirm.html` ♻️ + 🟥 full detail USER builds | §3.7 invoice · §3.8 dispute · §2.5 tour · §3.2 |
| 3.4 | **Favorites** | `account-wishlist.html` ✅ | §2.3/§2.4/§2.5 detail · remove inline |
| 3.5 | **My Reviews** | 🟥 USER builds (account-* list shell + review card) | §2.3/§2.4/§2.5 reviewed entity · edit/delete inline |
| 3.6 | **Recommendations** `/me/discover` | 🟥 USER builds (`tour-grid` grid + onboarding modal) | §2.5/§2.3 detail · "not interested" inline |
| 3.7 | **Payments & Invoices** `/me/billing` | `account-payment-details.html` ✅ · `tour-booking.html` payment step ♻️ | §3.3 source booking · download · gateway → §3.3 |
| 3.8 | **Disputes** | 🟥 USER builds (list + thread) | dispute thread · §3.3 booking · §3.7 refund |
| 3.9 | **Support** | `help-center`/`help-detail` ♻️ + 🟥 ticket thread USER builds | ticket thread · §3.9 list |
| 3.10 | **Account Settings** `/me/settings` | `account-profile` ✅ · `account-settings` ✅ · `account-delete` ✅ · `account-travelers` 🟥 (no endpoint) · Devices/Linked-accounts 🟥 | delete → logout → §2.1 · logout-all → §2.10 |

## Endpoints by page

### 3.1 Overview `SSR`
`GET /accounts/profile` · `/booking/my-bookings` · `/analytics/recommendations` · `/social/favorites` · `/notifications/unread-count`.
- **Buttons:** **View Bookings** → §3.2 (nav) · **View Favorites** → §3.4 (nav) · **Discover** → §3.6 (nav) · **Mark all read** → `POST /notifications/read-all`.
- **Stack:** **Area** `Accounts` · **Route** `/accounts` · **Cache** `NoStore` (`C2`) · **Perm** `[Authorize]` (own data) · **Rules** `R2` SSR · `API1` parallel profile/bookings/recs/favorites/unread · `NF7`+`A11Y9` bell aria-live · `D3` mobile bottom-nav.

### 3.2 My Trips
`GET /booking/my-bookings` · `POST /booking/tour` (create) · `POST /booking/{id}/cancel` · `GET /booking/join-requests?own=true` · `POST /booking/join-requests`.
- **Buttons:** **Book Again / New Booking** → `POST /booking/tour` (→ §3.7 payment) · **Cancel Booking** → `POST /booking/{id}/cancel` (inline) · **Request to Join** → `POST /booking/join-requests` · booking row → §3.3 (nav).
- **Stack:** **Area** `Accounts` · **Route** `/accounts/bookings` · **Cache** `NoStore` · **Perm** `[Authorize]` (owner-scoped) · **Rules** `R2`+`D1` paging 20/50 · `F8` cancel confirm modal (refund preview) · new booking → `CAL5`+`PAY4` · `NF1` toast · `SEC7` anti-forgery.

### 3.3 Booking Detail
`GET /booking/{id}` *(owner/provider/admin scoped)* · `GET /invoices/my-invoices?bookingId` · `/invoices/{id}/download` · `POST /booking/{id}/dispute` (Completed only) · `/booking/{id}/cancel` · weather widget.
- **Buttons:** **Download Invoice** → `GET /invoices/{id}/download` · **Raise Dispute** → `POST /booking/{id}/dispute` (Completed only) · **Cancel Booking** → `POST /booking/{id}/cancel` (inline) · **View Tour** → §2.5 (nav).
- **Stack:** **Area** `Accounts` · **Route** `/accounts/bookings/{id}` · **Cache** `NoStore` · **Perm** `[Authorize]` (owner/provider/admin scoped by API) · **Rules** `R2` · `PRINT1`/`PRINT2` invoice PDF (QuestPDF) · `PRINT3` .ics export · `F8` cancel/dispute confirm · weather widget (§0.2) · `REV1` `?action=review` modal · `ST1` RowVersion 409 → reload prompt.

### 3.4 Favorites
`GET /social/favorites` · `POST /social/favorites` · `DELETE /social/favorites/{entityType}/{entityId}` · `GET /social/favorites/check/{entityType}/{entityId}`.
- **Buttons:** **Remove from Favorites** → `DELETE /social/favorites/{entityType}/{entityId}` (inline) · favorite card → §2.3/§2.4/§2.5 detail (nav).
- **Stack:** **Area** `Accounts` · **Route** `/accounts/wishlist` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `R2` · `NF6` optimistic remove + `WL3` Undo toast · `WL4` nav counter badge · `WL5` cap 500 · `FavoriteEntityType` = {Tour,Place,Business,Blog,TourGuide}.

### 3.5 My Reviews
- Standard: `GET /social/reviews/my-reviews` · `POST /social/reviews` · `PUT /social/reviews/{id}` (≤48h) · `DELETE /social/reviews/{id}`.
- Helpful / report: `POST/DELETE /social/reviews/{id}/helpful`, `POST /social/reviews/{id}/report`.
- Accessibility: `GET /social/accessibility/reviews/my` · `POST` · `PUT/{id}` (≤48h) · `DELETE/{id}`.
- **Buttons:** **Edit Review (≤48h)** → `PUT /social/reviews/{id}` (or accessibility) · **Delete Review** → `DELETE /social/reviews/{id}` (or accessibility) · **Mark Helpful** → `POST /social/reviews/{id}/helpful` · **Report** → `POST /social/reviews/{id}/report` · review row → reviewed entity (nav).
- **Stack:** **Area** `Accounts` · **Route** `/accounts/reviews` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `R2`+`D1` paging · `REV5` 48h edit-window countdown badge ("Edit (12h left)") · `F8` delete confirm · `NF6` helpful optimistic.

### 3.6 Recommendations
`GET /analytics/recommendations` · `/itinerary` · `/similar/{entityId}` · `/for/{kind}/{entityId}`. Events: `POST /analytics/recommendations/onboarding` · `/not-interested` · `/metrics` · `/sponsored-click`.
- **Buttons:** **Not Interested** → `POST /analytics/recommendations/not-interested` (inline) · **Start Onboarding** → `POST /analytics/recommendations/onboarding` · sponsored click → `POST .../sponsored-click` (beacon) · rec card → §2.5/§2.3 detail (nav).
- **Stack:** **Area** `Accounts` · **Route** `/accounts/discover` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `R2` · onboarding `MOD` modal · `API2` not-interested/metrics/sponsored-click beacons (fire-and-forget) · `NF6` not-interested optimistic · `X14` no ApexCharts here.

### 3.7 Payments & Invoices
`GET /payments/my-payments` · `/payments/{id}` · `POST /payments/initiate` · `GET /invoices/my-invoices` · `/invoices/{id}[/download]`. *(Dev-only: `POST /payments/{bookingId}/simulate-success` — never in prod UI.)*
- **Buttons:** **Pay Now** → `POST /payments/initiate` (→ gateway → §3.3) · **Download Invoice** → `GET /invoices/{id}/download` · payment/invoice row → §3.3 (nav).
- **Stack:** **Area** `Accounts` · **Route** `/accounts/billing` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `R2` · `PAY1` PSP iframe (never store/log card) · `PAY2` idempotency key + disable Pay · `PAY3` exact total+currency+refund+lock countdown (`CAL4`) · `PAY4` webhook server-confirm ("Processing…") · `PRINT2` invoice PDF · DEV-only `simulate-success` never rendered in prod.

### 3.8 Disputes
`GET /disputes/my` · `POST /disputes`.
- **Buttons:** **Open Dispute** → `POST /disputes` · dispute row → thread (nav).
- **Stack:** **Area** `Accounts` · **Route** `/accounts/disputes` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `R2`+`D1` paging · open-dispute = ViewModel form + `SEC7` anti-forgery · `F8` confirm.

### 3.9 Support
`GET /support/tickets[/{id}]` · `POST /support/tickets` · `/{id}/messages` · `/{id}/close`.
- **Buttons:** **New Ticket** → `POST /support/tickets` · **Send Message** → `POST /support/tickets/{id}/messages` · **Close Ticket** → `POST /support/tickets/{id}/close` · ticket row → thread (nav).
- **Stack:** **Area** `Accounts` · **Route** `/accounts/support` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `R2`+`D1` paging · ticket thread = SSR + AJAX reply · `NF1` toast · `F8` close confirm.

### 3.10 Account Settings — tabs Profile / Security / Notifications / Devices / Linked accounts / Privacy
- **Profile:** `GET/PUT /accounts/profile` · `POST/PUT/DELETE /accounts/profile/avatar` · `DELETE /accounts/profile` + `POST /accounts/profile/restore` · `GET/PUT /accounts/me/marketing-consent`.
- **Security:** `PUT /security/account/password` · `/phone` · `GET /auth/sessions` · `DELETE /auth/sessions/{sessionId}` · `POST /auth/logout-all`.
- **Notifications:** `GET/PUT /notifications/preferences`.
- **Devices:** `GET /devices/tokens` · `POST /devices/token` · `DELETE /devices/token/{id}` · `PATCH /auth/devices/{deviceId}/trust`.
- **Linked accounts:** `POST /auth/external-providers` · `DELETE /auth/external-providers/{providerId}` *(no GET-list; render from `security/me`)*.
- **Privacy:** `GET/PUT /analytics/preferences` · `GET /analytics/recommendations/me/export` · `DELETE /analytics/recommendations/me` + `POST .../cancel-deletion`.
- **Buttons (all tabs):** **Save Profile** → `PUT /accounts/profile` · **Upload/Change/Remove Avatar** → `POST/PUT/DELETE /accounts/profile/avatar` · **Save Marketing Consent** → `PUT /accounts/me/marketing-consent` · **Change Password** → `PUT /security/account/password` · **Change Phone** → `PUT /security/account/phone` · **Revoke Session** → `DELETE /auth/sessions/{sessionId}` · **Log Out All** → `POST /auth/logout-all` · **Save Notification Prefs** → `PUT /notifications/preferences` · **Register/Remove Device** → `POST` / `DELETE /devices/token[/{id}]` · **Trust Device** → `PATCH /auth/devices/{deviceId}/trust` · **Link/Unlink Account** → `POST` / `DELETE /auth/external-providers[/{providerId}]` · **Save Privacy Prefs** → `PUT /analytics/preferences` · **Export My Data** → `GET /analytics/recommendations/me/export` · **Delete My Data / Cancel** → `DELETE /analytics/recommendations/me` / `POST .../cancel-deletion` · **Delete Account / Restore** → `DELETE /accounts/profile` / `POST /accounts/profile/restore`.
- **Stack:** **Area** `Accounts` (`Accounts.Shared/_AccountSidebar`) · **Route** `/accounts/settings` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `R2` + lazy tabs (non-default tab = AJAX) · `F3` password-strength meter · `F8` delete-account confirm (+ `restore`) · `ST2` session-timeout "Stay signed in" modal · `SEC7` anti-forgery (incl. AJAX) · avatar upload `AJAX↑`/`SEC4` magic-byte+EXIF-strip · `GEO2` currency toggle · `NF7` notification prefs.
