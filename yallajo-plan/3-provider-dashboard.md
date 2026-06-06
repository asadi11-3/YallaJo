# Area 3 — Provider / Host Dashboard

> **Actor:** Provider (authenticated business/tour owner)
> **Plan section:** §4 of `yallajo-dashboard-ui-ux-plan.md`
> **Namespaces:** `provider`, `provider/dashboard`, `places/businesses`, `tours` (owner),
> `provider/my-tours`, `tours/provider/my-tours`, `booking/provider`, `booking/availability`,
> `payouts/provider`, `invoices/provider`, `provider-payment-methods`.
> Template family: `agent-*`. Status legend: ✅ Wire · ♻️ Repurpose · ⏭️ Skip · 🟥 USER builds.
> **Architecture & rules:** see [`0-architecture-and-rules.md`](0-architecture-and-rules.md) — four-tier pipeline + per-page `Stack` legend.
> **Code areas:** `Provider` (`/provider/*`) for all pages **except §4.5 Businesses**, which lives in the `Business` area (`/business/*`). All pages are authenticated (`NoStore`, UI-PERF-C2) and gated by policy `Provider` + a `WebPermission` constant.

## Pages this area should have (10)

| # | Page | Template | Redirects to |
|---|------|----------|--------------|
| 4.1 | **Overview** | `agent-dashboard.html` ✅ · `agent-activities.html` ✅ | §4.3/§4.6/§4.8 · feed item → entity |
| 4.2 | **Application** | `join-us.html` ✅ + 🟥 docs steps USER builds (Dropzone) | §4.1 pending · approved → §4.3 · request-docs → upload |
| 4.3 | **Tours** | `agent-listings.html` ✅ · `add-listing(-minimal).html` ✅ · `listing-added.html` ✅ | editor · §2.5 preview · §4.6 applications |
| 4.4 | **Packages** | 🟥 USER builds (reuse `add-listing` + `agent-listings`) | package editor · §4.4 list · §2.6 preview |
| 4.5 | **Businesses** | `agent-listings.html` ♻️ · `add-listing.html` ♻️ | editor · §4.5 list · §2.4 preview |
| 4.6 | **Incoming Bookings** | `agent-bookings.html` ✅ (Flatpickr slots) | booking detail (§3.3-style) · confirm/reject/complete inline · slots editor |
| 4.7 | **Reviews** | `agent-reviews.html` ✅ | reviewed entity (§2.3/§2.4/§2.5) · reply/report inline |
| 4.8 | **Finance** | `agent-earnings.html` ✅ + 🟥 methods tab USER builds | payout/invoice detail · download · method form |
| 4.9 | **Documents** | 🟥 USER builds (Dropzone + list) | preview/download · upload inline |
| 4.10 | **Settings** | `agent-settings.html` ✅ | save inline · §3.10 shared settings |

## Endpoints by page

### 4.1 Overview `SSR`
`GET /provider/dashboard` · `/dashboard/overview` · `/dashboard/pending-actions` · `/dashboard/notifications` · `GET /provider/analytics`.
- **Buttons:** Create Tour → §4.3 (nav) · Add Business → §4.5 (nav) · View Bookings → §4.6 (nav) · pending-action Resolve → its section (nav). Read-only KPIs, no write-endpoints.
- **Stack:** **Area** `Provider` · **Route** `/provider/dashboard` · **Cache** `NoStore` · **Perm** `Provider · WebPermission.Provider.Read` · **Rules** `R2 SSR · API1 Task.WhenAll KPIs · L1 skeleton · NF7 bell · ApexCharts (dashboard bundle, not public X14)`

### 4.2 Application
`GET /provider/status` · `POST /provider/register` · `/apply` · `/reapply` · `/documents` · `/documents/upload` `AJAX↑` · `PUT /provider/documents/{id}`.
- **Buttons:** Register → `POST /provider/register` · Submit Application → `POST /provider/apply` · Re-apply → `POST /provider/reapply` · Upload Document → `POST /provider/documents/upload` (AJAX↑) · Replace Document → `PUT /provider/documents/{id}`.
- **Stack:** **Area** `Provider` · **Route** `/provider/apply` · **Cache** `NoStore` · **Perm** `[Authorize]` (pre-provider applicant) · `WebPermission.Provider.Apply` · **Rules** `F1–F3 validate · F6 scroll-to-error · F9 dirty-guard · AJAX↑ SEC4 magic-byte · PE1 native POST`

### 4.3 Tours
- List: `GET /tours/provider/my-tours` (all statuses) and/or `GET /provider/my-tours` (perf metrics).
- Lifecycle: `POST /tours` · `/tours/{id}/submit` · `/archive` · `DELETE /tours/{id}`.
- Editor tabs: `PUT /tours/{id}` · `GET/PUT /tours/{id}/children-info` · pricing/schedules/waypoints CRUD + `PUT .../waypoints/reorder` · `GET/POST/DELETE /tours/{id}/guides[/{guideUserId}]` · Media via attachments.
- Guide applications: `GET /tours/{tourId}/applications` · `POST .../{applicationId}/approve|reject` · `POST /tours/{tourId}/open-applications|close-applications` · `GET /tours/{tourId}/guide-offerings`.
- **Buttons:** New Tour → `POST /tours` · Save → `PUT /tours/{id}` · Submit → `POST /tours/{id}/submit` · Archive → `POST /tours/{id}/archive` · Delete → `DELETE /tours/{id}` · pricing/schedules/waypoints Add/Save/Remove → `POST/PUT/DELETE /tours/{id}/{pricing|schedules|waypoints}` + Reorder → `PUT .../waypoints/reorder` · Assign/Remove Guide → `POST/DELETE /tours/{id}/guides[/{guideUserId}]` · Open/Close Applications → `POST /tours/{tourId}/open-applications|close-applications` · Approve/Reject Applicant → `POST /tours/{tourId}/applications/{applicationId}/approve|reject` · Save Child Info → `PUT /tours/{id}/children-info`.
- **Stack:** **Area** `Provider` · **Route** `/provider/tours` (list) · `/provider/tours/{id}/edit` (wizard) · **Cache** `NoStore` · **Perm** `Provider · WebPermission.Tours.Write` · **Rules** `F4/PROV1–6 five-step wizard · PROV2 localStorage draft · F5 autosave 30s · ST1 RowVersion concurrency · AJAX↑ media SEC4 · C3 evict tour:{id} on save · D1 list paging · MOD12 delete confirm`

### 4.4 Packages
`GET /tours/packages` · `POST` · `PUT/{id}` · `DELETE/{id}` · `POST .../{id}/inclusions` · `/submit`.
- **Buttons:** New Package → `POST /tours/packages` · Save → `PUT /tours/packages/{id}` · Delete → `DELETE /tours/packages/{id}` · Add Inclusion → `POST .../{id}/inclusions` · Submit → `POST .../{id}/submit`.
- **Stack:** **Area** `Provider` · **Route** `/provider/packages` · **Cache** `NoStore` · **Perm** `Provider · WebPermission.Tours.Write` · **Rules** `F4 editor · F5 autosave · ST1 concurrency · C3 evict tour-package · MOD12 delete confirm`

### 4.5 Businesses
`GET /places/businesses/mine` · `/{id}` · `POST` · `PUT/{id}` · `POST .../{id}/resubmit`. Sub-resources: amenities, services, staff, hours, accessibility (CRUD) + weather widget.
- **Buttons:** New Business → `POST /places/businesses` · Save → `PUT /places/businesses/{id}` · Resubmit → `POST .../{id}/resubmit` · Amenity/Service/Staff Add/Remove → `POST/PUT/DELETE` sub-resources · Save Hours → `PUT .../{id}/hours` · Save Accessibility → `PUT .../{id}/accessibility`.
- **Stack:** **Area** `Business` · **Route** `/business/listings` · `/business/{id}/edit` · **Cache** `NoStore` · **Perm** `Provider · WebPermission.Business.Write` · **Rules** `F-forms validate · ST1 concurrency · AJAX↑ media SEC4 · C3 evict business:{id} · weather widget (Place-contextual) · D1 list paging`

### 4.6 Incoming Bookings
`GET /booking/provider/bookings` · `GET /booking/{id}` · `POST /booking/{id}/confirm|reject|complete`. Availability: `GET /booking/availability/{tourId}/manage` · `POST .../slots[/bulk]` · `PUT/DELETE .../slots/{id}`.
- **Buttons:** Confirm → `POST /booking/{id}/confirm` · Reject → `POST /booking/{id}/reject` (100% refund) · Complete → `POST /booking/{id}/complete` · Add/Bulk Slot → `POST .../slots[/bulk]` · Edit/Delete Slot → `PUT/DELETE .../slots/{id}`.
- **Stack:** **Area** `Provider` · **Route** `/provider/bookings` · **Cache** `NoStore` · **Perm** `Provider · WebPermission.Booking.Manage` · **Rules** `D1 paging · F8 reject-confirm (refund preview) · CAL slots Flatpickr · RT4 provider:{providerId} SignalR booking alerts · NF1 toast · ST1 on slot edits`

### 4.7 Reviews
`GET /social/reviews/{entityType}/{entityId}` · `/ratings` · `POST /social/reviews/{id}/reply` · `PUT/DELETE .../reply/{replyId}` · `POST .../report`.
- **Buttons:** Reply → `POST /social/reviews/{id}/reply` · Edit/Delete Reply → `PUT/DELETE .../reply/{replyId}` · Report → `POST /social/reviews/{id}/report`.
- **Stack:** **Area** `Provider` · **Route** `/provider/reviews` · **Cache** `NoStore` · **Perm** `Provider · WebPermission.Reviews.Reply` · **Rules** `D1 paging · F1 reply validate · MOD12 report confirm · A11Y5 rating color+icon+text`

### 4.8 Finance — tabs Payouts / Invoices / Methods
`GET /payouts/provider[/{id}]` · `GET /invoices/provider/my-invoices` · `/invoices/{id}[/download]` · `GET/POST/PUT/DELETE /provider-payment-methods[/{id}]`.
- **Buttons:** Download Invoice → `GET /invoices/{id}/download` · Add Method → `POST /provider-payment-methods` · Edit/Delete Method → `PUT/DELETE /provider-payment-methods/{id}`.
- **Stack:** **Area** `Provider` · **Route** `/provider/finance` · **Cache** `NoStore` · **Perm** `Provider · WebPermission.Finance.Read` (methods write → `WebPermission.Finance.Methods`) · **Rules** `D1 paging · R6 DataTables server-side >500 rows · PRINT2 invoice PDF (QuestPDF) · lazy non-default tabs · ST1 on method edits`

### 4.9 Documents
`GET /booking/provider/documents[/{id}]` · `POST` `AJAX↑` · `PUT/{id}`.
- **Buttons:** Upload Document → `POST /booking/provider/documents` (AJAX↑) · Replace → `PUT /booking/provider/documents/{id}`.
- **Stack:** **Area** `Provider` · **Route** `/provider/documents` · **Cache** `NoStore` · **Perm** `Provider · WebPermission.Provider.Documents` · **Rules** `AJAX↑ SEC4 magic-byte + EXIF-strip + authenticated proxy · D1 list paging · L2 upload spinner`

### 4.10 Settings
`GET /provider/settings`.
- **Buttons:** Save Settings → no dedicated write-endpoint (read-only; persisted via §3.10 account endpoints) · account/security links → §3.10 (nav).
- **Stack:** **Area** `Provider` · **Route** `/provider/settings` · **Cache** `NoStore` · **Perm** `Provider · WebPermission.Provider.Read` · **Rules** `F-forms · F7 disabled-when-invalid · shared with §3.10 Account Settings (Accounts area)`
