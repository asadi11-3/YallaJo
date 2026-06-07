# Area 3 — Provider / Host Dashboard

> **Actor:** Provider (authenticated business/tour owner)
> **Plan section:** §4 of `yallajo-dashboard-ui-ux-plan.md`
> **Namespaces (underlying API):** `provider`, `provider/dashboard`, `places/businesses`, `tours` (owner),
> `provider/my-tours`, `tours/provider/my-tours`, `booking/provider`, `booking/availability`,
> `payouts/provider`, `invoices/provider`, `provider-payment-methods`.
> Template family: `agent-*`. Status legend: ✅ Wire · ♻️ Repurpose · ⏭️ Skip · 🟥 USER builds.
> **BFF route reality:** the shipped **BFF** surface is page-scoped `POST` across **26 controllers** (Provider area 20 + Business area 6); every URL below is the **BFF** route the button actually targets (e.g. `POST /provider/tours/{id}/delete`), distinct from the API `/api/v1/...` `PUT`/`DELETE` paths in the namespace list above. Every POST carries `[ValidateAntiForgeryToken]` and uses PRG.
> **Architecture & rules:** see [`0-architecture-and-rules.md`](0-architecture-and-rules.md) — four-tier pipeline + per-page `Stack` legend.
> **Code areas:** `Provider` (`/provider/*`) for all pages **except §4.5 Businesses**, which lives in the `Business` area (`/business/*`). All pages are authenticated (`NoStore`, UI-PERF-C2). **Authorization shape varies by controller — the controller attributes are the source of truth.** Verified patterns:
> - **`[Authorize(Policy = "Provider")]`** — `FinanceController` (policy only, no `[RequirePermission]`), `ProviderDocumentsController` (+ class-level `[RequirePermission(ProviderDocument.Read)]` + per-action), `PackagesController` (per-action `[RequirePermission]`).
> - **plain `[Authorize]` + class-level `[RequirePermission]`** — `DashboardController` (`ProviderDashboard.Read`), `InvoicesController` (`Invoice.Read` + per-action `Download`), `PaymentMethodsController` (`ProviderPaymentMethod.Read` + per-action), and **all 6 Business controllers** (`Business.Read` + per-action sub-resource constants).
> - **plain `[Authorize]` + per-action `[RequirePermission]`** — `ProviderController` (application flow: `ProviderApplication.Read/Register/Submit/Update/Create`).
> - **plain `[Authorize]` + inline `ICurrentUser.HasPermission(...)`** — the **8 tour-editor controllers** (`Tours`→`Tour.*`, `TourPricing`→`TourPricingTier.*`, `TourSchedules`→`TourSchedule.*`, `TourWaypoints`→`TourWaypoint.*`, `TourGuides`→`TourGuide.*`, `TourImages`→`Tour.ReadOwn`+`Attachment.*`, `TourApplications`→`GuideApplication.*`+`Tour.Update`, `TourAvailability`→`AvailabilitySlot.*`).
> - **plain `[Authorize]` only (no explicit permission attribute/inline check at the controller)** — `BookingsController`, `ProviderBookingsController`, `EarningsController`, `ReviewsController`, `SettingsController` (authorization is `[Authorize]`-gated; finer-grained checks, if any, are enforced downstream in the API). The **Perm** lines below for these pages name the *representative API-level* constant, not a BFF `[RequirePermission]`.
>
> Each page's **Perm** line names the *representative* constant; treat the controller attributes above as authoritative.
>
> **Permission constants (verified against `Infrastructure/Authorization/WebPermission.cs`):** there is **no** `Provider`, `Tours`, `Finance`, `Reviews`, or `Booking.Manage` namespace. The real classes are `ProviderDashboard`, `ProviderApplication`, `ProviderDocument`, `Tour`, `TourPricingTier`, `TourSchedule`, `TourWaypoint`, `TourChildrenInfo`, `TourGuide`, `GuideApplication`, `Package`, `Business`(+`BusinessHours`/`BusinessAmenity`/`BusinessStaff`/`ServiceItem`/`AccessibilityFeature`), `TourBooking`, `AvailabilitySlot`, `Payout`, `Invoice`, `ProviderPaymentMethod`, `Review`/`ReviewReply`, `Attachment` (tour images), `JoinRequest` (booking join-request moderation). Each page's **Perm** below names the *representative* (class-level read) constant; write actions layer the stricter per-action constant noted inline.

## Pages this area should have (10)

| # | Page | Template | Redirects to |
|---|------|----------|--------------|
| 4.1 | **Overview** | `agent-dashboard.html` ✅ · `agent-activities.html` ✅ | §4.3/§4.6/§4.8 · feed item → entity |
| 4.2 | **Application** | `join-us.html` ✅ + 🟥 docs steps USER builds (Dropzone) | §4.1 pending · approved → §4.3 · request-docs → upload |
| 4.3 | **Tours** | `agent-listings.html` ✅ · `add-listing(-minimal).html` ✅ · `listing-added.html` ✅ | editor · §2.5 preview · §4.6 applications |
| 4.4 | **Packages** | 🟥 USER builds (reuse `add-listing` + `agent-listings`) | package editor · §4.4 list · §2.6 preview |
| 4.5 | **Businesses** | `agent-listings.html` ♻️ · `add-listing.html` ♻️ | editor · §4.5 list · §2.4 preview |
| 4.6 | **Incoming Bookings** | `agent-bookings.html` ✅ (Flatpickr slots) | `/manage` detail (§3.3-style) · confirm/reject/cancel/complete inline · join-request approve/reject · slots editor (under tour) |
| 4.7 | **Reviews** | `agent-reviews.html` ✅ | reviewed entity (§2.3/§2.4/§2.5) · reply/report inline |
| 4.8 | **Finance** | `agent-earnings.html` ✅ + 🟥 methods tab USER builds | payout/invoice detail · download · method form |
| 4.9 | **Documents** | 🟥 USER builds (Dropzone + list) | preview/download · upload inline |
| 4.10 | **Settings** | `agent-settings.html` ✅ | save inline · §3.10 shared settings |

## Endpoints by page

### 4.1 Overview `SSR`
**BFF routes (`DashboardController`):** `GET /provider` · `GET /provider/dashboard`. *(The KPI/feed data — overview, pending-actions, notifications, analytics — is fetched server-side from the underlying API `/api/v1/provider/dashboard/*` + `/provider/analytics` and rendered into this one page; those are **not** separate BFF routes.)*
- **Buttons:** Create Tour → §4.3 (nav) · Add Business → §4.5 (nav) · View Bookings → §4.6 (nav) · pending-action Resolve → its section (nav). Read-only KPIs, no write-endpoints.
- **Stack:** **Area** `Provider` · **Route** `/provider/dashboard` · **Cache** `NoStore` · **Perm** `[Authorize]` + class-level `[RequirePermission(WebPermission.ProviderDashboard.Read)]` · **Rules** `R2 SSR · API1 Task.WhenAll KPIs (then GuardSignOut each) · ERR3 each KPI/feed section degrades to a "Couldn't load — [Retry]" card · L1 skeleton · NF7 bell · ApexCharts (dashboard bundle, not public X14)`

### 4.2 Application
`GET /provider/status` · `GET/POST /provider/apply` · `POST /provider/submit` · `POST /provider/reapply` · `POST /provider/apply/documents/upload` `AJAX↑` · `POST /provider/apply/documents/replace`.
- **Buttons:** Register (start application) → `POST /provider/apply` (→ `RegisterAsync`) · Submit Application → `POST /provider/submit` · Re-apply (after rejection) → `POST /provider/reapply` · Upload Document → `POST /provider/apply/documents/upload` (AJAX↑) · Replace Document → `POST /provider/apply/documents/replace` (PUT semantics via page-scoped POST).
- **Stack:** **Area** `Provider` · **Route** `/provider/apply` · **Cache** `NoStore` · **Perm** `[Authorize]` (pre-provider applicant) + `WebPermission.ProviderApplication.Read` (representative); writes: `.Register` (apply/start), `.Submit` (submit), `.Update` (reapply + document replace), `.Create` (document upload) · **Rules** `F1–F3 validate · F6 scroll-to-error · F9 dirty-guard · AJAX↑ SEC4 magic-byte · PE1 native POST · PRG redirects to GET /provider/status · ValidateAntiForgeryToken on every POST`
> **There is no `/provider/register`.** The application is *started* by `POST /provider/apply` (calls `RegisterAsync`, perm `ProviderApplication.Register`) and *submitted* by `POST /provider/submit` (`.Submit`).
> **Application docs ≠ ongoing docs.** This page's application-document actions are page-scoped under **`/provider/apply/documents/*`** (`ProviderApplication.Create`/`Update`, `ProviderController`) — deliberately distinct from the §4.9 standalone Documents page at **`/provider/documents`** (`ProviderDocument.*`, `ProviderDocumentsController`). The two route families are kept separate so the BFF URLs do not collide; do not conflate them.

### 4.3 Tours
> **API-vs-BFF:** the editor is split across **8 Provider controllers**; every mutation is a **page-scoped `POST`** (e.g. `/provider/tours/{id}/delete`), not the API's `PUT`/`DELETE`. GET forms back the create/edit pages.
- List (`ToursController`): `GET /provider/tours` · `GET/POST /provider/tours/create` · `GET/POST /provider/tours/{id}/edit` · `POST /provider/tours/{id}/submit` · `POST /provider/tours/{id}/archive` · `POST /provider/tours/{id}/delete`.
- Pricing (`TourPricingController`): `GET /provider/tours/{id}/pricing` · `GET/POST .../pricing/create` · `GET/POST .../pricing/{tierId}/edit` · `POST .../pricing/{tierId}/delete`.
- Schedules (`TourSchedulesController`): `GET .../schedules` · `GET/POST .../schedules/create` · `GET/POST .../schedules/{scheduleId}/edit` · `POST .../schedules/{scheduleId}/delete`.
- Waypoints (`TourWaypointsController`): `GET .../waypoints` · `GET/POST .../waypoints/create` · `GET/POST .../waypoints/{waypointId}/edit` · `POST .../waypoints/{waypointId}/delete` · `POST .../waypoints/reorder`.
- Guides (`TourGuidesController`): `GET .../guides` · `POST .../guides/assign` · `POST .../guides/{guideUserId}/remove`.
- Images (`TourImagesController`): `GET .../images` · `POST .../images/upload` · `POST .../images/{attachmentId}/delete`. *(Gated inline by `WebPermission.Attachment.Read`/`Create`/`Delete` via `ICurrentUser.HasPermission`, not `[RequirePermission]`.)*
- Applications (`TourApplicationsController`): `GET .../applications` · `POST .../applications/{applicationId}/approve|reject` · `POST .../applications/open` · `POST .../applications/close`.
- **Buttons:** New Tour → `POST /provider/tours/create` · Save → `POST /provider/tours/{id}/edit` · Submit → `POST /provider/tours/{id}/submit` · Archive → `POST /provider/tours/{id}/archive` · Delete → `POST /provider/tours/{id}/delete` · pricing/schedules/waypoints Add/Save/Remove → `POST .../{pricing|schedules|waypoints}/create|{…Id}/edit|{…Id}/delete` + Reorder → `POST .../waypoints/reorder` · Assign Guide → `POST .../guides/assign` · Remove Guide → `POST .../guides/{guideUserId}/remove` · Open/Close Applications → `POST .../applications/open|close` · Approve/Reject Applicant → `POST .../applications/{applicationId}/approve|reject`.
- **Stack:** **Area** `Provider` · **Route** `/provider/tours` (list) · `/provider/tours/{id}/edit` (wizard) · **Cache** `NoStore` · **Perm** plain `[Authorize]` on all 8 editor controllers, with permissions enforced **inline** via `ICurrentUser.HasPermission(...)` (not `[RequirePermission]`): read `Tour.ReadOwn`; writes `Tour.Create`/`Update`/`Submit`/`Archive`/`DeleteOwn`, `TourPricingTier.*`, `TourSchedule.*`, `TourWaypoint.*`, `TourGuide.Update`, `GuideApplication.Approve`/`Reject` (+`Tour.Update`), and `Attachment.Read`/`Create`/`Delete` (images) · **Rules** `F4/PROV1–6 five-step wizard · PROV2 localStorage draft · F5 autosave 30s · ST1 RowVersion concurrency (TourDetailResponse exposes RowVersion) · AJAX↑ media SEC4 · C3 evict tour:{id} on save · D1 list paging · F8 destructive delete-confirm modal (per MOD12)`

### 4.4 Packages
`GET /provider/packages` · `POST /provider/packages/create` · `GET /provider/packages/{id}` · `POST /provider/packages/{id}/inclusions` · `POST /provider/packages/{id}/submit` · `POST /provider/packages/{id}/delete`. *(`PackagesController`; page-scoped POST — not API `PUT`/`DELETE`.)*
- **Buttons:** New Package → `POST /provider/packages/create` · Add Inclusion → `POST /provider/packages/{id}/inclusions` · Submit → `POST /provider/packages/{id}/submit` · Delete → `POST /provider/packages/{id}/delete`.
- **Stack:** **Area** `Provider` · **Route** `/provider/packages` · **Cache** `NoStore` · **Perm** policy `Provider` + `WebPermission.Package.Create`/`Update`/`Delete` (no class-level read constant; `Package` has only Create/Update/Delete/Approve/Reject) · **Rules** `F4 editor · F5 autosave · C3 evict tour-package · F8 destructive delete-confirm (per MOD12)` *(no `ST1`: `PackageDetailResponse` exposes neither `RowVersion` nor `UpdatedAt` — the API guards state preconditions server-side)*

### 4.5 Businesses
> **API-vs-BFF:** lives in the `Business` area across **6 controllers**, all page-scoped `POST` (`/add`,`/remove`,`/save`,`/edit`,`/update`,`/resubmit`), not API `POST/PUT/DELETE`.
- List (`MyBusinessesController`): `GET /business` · `GET /business/businesses` · `GET/POST /business/businesses/register` · `GET /business/businesses/{id}` · `POST /business/businesses/{id}/update` · `POST /business/businesses/{id}/resubmit`.
- Amenities (`AmenitiesController`): `GET .../{id}/amenities` · `POST .../{id}/amenities/add` · `POST .../{id}/amenities/{amenityId}/remove`.
- Hours (`HoursController`): `GET .../{id}/hours` · `POST .../{id}/hours/save`.
- Services (`ServicesController`): `GET .../{id}/services` · `POST .../{id}/services/add` · `GET/POST .../{id}/services/{serviceId}/edit` · `POST .../{id}/services/{serviceId}/remove`.
- Staff (`StaffController`): `GET .../{id}/staff` · `POST .../{id}/staff/add` · `POST .../{id}/staff/{staffId}/remove`.
- Accessibility (`AccessibilityController`): `GET .../{id}/accessibility` · `POST .../{id}/accessibility/save`.
- **Buttons:** New Business → `POST /business/businesses/register` · Save → `POST /business/businesses/{id}/update` · Resubmit → `POST /business/businesses/{id}/resubmit` · Amenity Add/Remove → `POST .../amenities/add|{amenityId}/remove` · Service Add/Edit/Remove → `POST .../services/add` · `POST .../services/{serviceId}/edit` · `POST .../services/{serviceId}/remove` · Staff Add/Remove → `POST .../staff/add|{staffId}/remove` · Save Hours → `POST .../hours/save` · Save Accessibility → `POST .../accessibility/save`.
- **Stack:** **Area** `Business` · **Route** `/business/businesses` (list) · `/business/businesses/{id}` (editor) · **Cache** `NoStore` · **Perm** plain `[Authorize]` + class-level `[RequirePermission(WebPermission.Business.Read)]` (all 6 controllers); per-action writes layer `Business.Create` (register), `Business.Update` (update), `Business.Submit` (resubmit), `BusinessHours.Update`, `BusinessAmenity.Create`/`Delete`, `BusinessStaff.Create`/`Delete`, `ServiceItem.Create`/`Update`/`SoftDelete`, `AccessibilityFeature.Update` · **Rules** `F-forms validate · staleness via UpdatedAt (BusinessDetailResponse exposes UpdatedAt, not a RowVersion token — no ST1 round-trip) · AJAX↑ media SEC4 · C3 evict business:{id} · weather widget (Place-contextual) · D1 list paging`

### 4.6 Incoming Bookings
> **Two controllers + page-scoped POST.** `BookingsController` (`/provider/bookings`, summary + lookup + **join-request moderation** + quick confirm/reject) and `ProviderBookingsController` (`/provider/bookings/manage/*`, full management incl. provider **Cancel**). Availability slots are page-scoped **under the tour** (`TourAvailabilityController`), not a bare `/booking/availability/slots`.
- Bookings (`BookingsController`): `GET /provider/bookings` · `POST /provider/bookings/lookup` · `POST /provider/bookings/join-requests/{id}/approve` · `POST /provider/bookings/join-requests/{id}/reject` · `POST /provider/bookings/{id}/confirm` · `POST /provider/bookings/{id}/reject`.
- Manage (`ProviderBookingsController`): `GET /provider/bookings/manage` · `GET /provider/bookings/manage/{id}` · `POST /provider/bookings/manage/{id}/confirm` · `POST .../{id}/reject` · `POST .../{id}/cancel` · `POST .../{id}/complete`.
- Availability (`TourAvailabilityController`): `GET /provider/tours/{id}/availability` · `POST .../availability/bulk` · `GET/POST .../availability/create` · `GET/POST .../availability/{slotId}/edit` · `POST .../availability/{slotId}/delete`.
- **Buttons:** Confirm → `POST /provider/bookings/{id}/confirm` (or `/manage/{id}/confirm`) · Reject → `POST /provider/bookings/{id}/reject` (100% refund) · Cancel → `POST /provider/bookings/manage/{id}/cancel` · Complete → `POST /provider/bookings/manage/{id}/complete` · Lookup → `POST /provider/bookings/lookup` · Join-Request Approve/Reject → `POST /provider/bookings/join-requests/{id}/approve|reject` · Add/Bulk Slot → `POST /provider/tours/{id}/availability/create|bulk` · Edit/Delete Slot → `POST .../availability/{slotId}/edit|delete`.
- **Stack:** **Area** `Provider` · **Route** `/provider/bookings` · `/provider/bookings/manage` (management) · **Cache** `NoStore` · **Perm** `[Authorize]` at the controller (no BFF `[RequirePermission]`); representative API-level constants: read `TourBooking.ReadOwn`, writes `TourBooking.Confirm`/`Reject`/`Cancel`/`Complete`, `JoinRequest.Approve`/`Reject`; slots `AvailabilitySlot.Create`/`Update`/`Delete` (enforced inline on `TourAvailabilityController`) · **Rules** `D1 **cursor paging (infinite scroll — high-volume exception, not page-number 20/50)** for the bookings list · F8 reject/cancel-confirm (refund preview) · CAL slots Flatpickr · RT4 provider:{providerId} SignalR booking alerts · NF1 toast · ST1 RowVersion on slot edits (the API `/manage` read exposes RowVersion) · ValidateAntiForgeryToken + PRG on every POST`

### 4.7 Reviews
`GET /provider/reviews` · `POST /provider/reviews/{id}/reply` · `POST .../reply/{replyId}/edit` · `POST .../reply/{replyId}/delete` · `POST /provider/reviews/{id}/report`. *(All page-scoped POST on `ReviewsController` — not API `PUT`/`DELETE`.)*
- **Buttons:** Reply → `POST /provider/reviews/{id}/reply` · Edit Reply → `POST /provider/reviews/{id}/reply/{replyId}/edit` · Delete Reply → `POST /provider/reviews/{id}/reply/{replyId}/delete` · Report → `POST /provider/reviews/{id}/report`.
- **Stack:** **Area** `Provider` · **Route** `/provider/reviews` · **Cache** `NoStore` · **Perm** `[Authorize]` at the controller (no BFF `[RequirePermission]`); representative API-level constants: read `Review.Read`, reply `ReviewReply.Create`, report `Review.*` · **Rules** `D1 paging · F1 reply validate · F8 report-confirm (per MOD12) · A11Y5 rating color+icon+text · ValidateAntiForgeryToken + PRG on every POST`

### 4.8 Finance — tabs Payouts / Invoices / Methods
> **One unified page (`FinanceController` `GET /provider/finance`) aggregating standalone write controllers.** Each tab's GET + write actions live on dedicated controllers; all writes are page-scoped `POST`.
- Finance shell (`FinanceController`): `GET /provider/finance`.
- Earnings/Payouts (`EarningsController`): `GET /provider/earnings`.
- Invoices (`InvoicesController`): `GET /provider/invoices` · `GET /provider/invoices/{id}/download`.
- Methods (`PaymentMethodsController`): `GET /provider/payment-methods` · `POST /provider/payment-methods/create` · `POST /provider/payment-methods/{id}/edit` · `POST /provider/payment-methods/{id}/delete`.
- **Buttons:** Download Invoice → `GET /provider/invoices/{id}/download` · Add Method → `POST /provider/payment-methods/create` · Edit Method → `POST /provider/payment-methods/{id}/edit` · Delete Method → `POST /provider/payment-methods/{id}/delete`.
- **Stack:** **Area** `Provider` · **Route** `/provider/finance` · **Cache** `NoStore` · **Perm** shell `FinanceController` = `[Authorize(Policy = "Provider")]` (policy only); `InvoicesController` = class `[RequirePermission(Invoice.Read)]` + per-action `Invoice.Download`; `PaymentMethodsController` = class `[RequirePermission(ProviderPaymentMethod.Read)]` + per-action `Create`/`Update`/`Delete`; `EarningsController` = `[Authorize]`-only (Payouts read enforced downstream) · **Rules** `D1 paging · R6 DataTables server-side >500 rows · PRINT2 invoice PDF (QuestPDF) · lazy non-default tabs · ValidateAntiForgeryToken + PRG on every POST`

### 4.9 Documents
`GET /provider/documents` · `POST /provider/documents/upload` `AJAX↑` · `POST /provider/documents/{id}/replace`. *(`ProviderDocumentsController`; page-scoped POST — `/replace` is PUT semantics via POST, not API `PUT`.)*
> **BFF vs API:** the shipped BFF surface is `/provider/documents/*`; the underlying API path is `/api/v1/booking/provider/documents/*` (Booking module, `ProviderDocument.*` permission, scoped per-caller). Do **not** put the API path on a button.
> **Distinct from §4.2 application docs:** this standalone Documents page owns `/provider/documents/*` (`ProviderDocument.*`); the application-flow upload/replace live under `/provider/apply/documents/*` (`ProviderApplication.*`). Separate route families, separate permission families — no URL collision.
- **Buttons:** Upload Document → `POST /provider/documents/upload` (AJAX↑) · Replace → `POST /provider/documents/{id}/replace`.
- **Stack:** **Area** `Provider` · **Route** `/provider/documents` · **Cache** `NoStore` · **Perm** policy `Provider` + `WebPermission.ProviderDocument.Read` (class-level); writes layer `ProviderDocument.Create` (upload)/`Update` (replace) · **Rules** `AJAX↑ SEC4 magic-byte + EXIF-strip + authenticated proxy · D1 list paging · L2 upload spinner · ValidateAntiForgeryToken + PRG on every POST`

### 4.10 Settings
`GET /provider/settings` · `POST /provider/settings/profile` · `POST /provider/settings/password` · `POST /provider/settings/phone`. *(`SettingsController` — provider settings has its **own** page-scoped write actions; it is **not** read-only and does not defer to Accounts for these.)*
- **Buttons:** Save Profile → `POST /provider/settings/profile` · Change Password → `POST /provider/settings/password` · Change Phone → `POST /provider/settings/phone` · account/security links → §3.10 (nav).
- **Stack:** **Area** `Provider` · **Route** `/provider/settings` · **Cache** `NoStore` · **Perm** `[Authorize]` at the controller (no BFF `[RequirePermission]`; finer checks enforced downstream in the API) · **Rules** `F-forms validate · F7 disabled-when-invalid · ValidateAntiForgeryToken + PRG on every POST · provider-local profile/password/phone writes (overlaps §3.10 Account Settings, but the provider has its own copies — not a redirect)`
