# Gap Report — `3-provider-dashboard.md` vs. shipped code

> ## ✅ RESOLVED — all gaps closed
> **Docs (Phase 1):** G1–G10 applied to `3-provider-dashboard.md` (register→`/provider/apply`+`/submit`; Settings writes; 8-controller tour editor; `/business/businesses`; two booking controllers + Cancel + join-requests; availability under tour; reviews reply POST; finance standalone controllers; documents `/replace` POST). **Bonus:** §4.4 Packages was *also* corrected to page-scoped POST (`/provider/packages/*`) — not in the original report but caught by the deep re-audit.
> **Code (Phase 2):** the deep re-audit found a **real runtime defect** the report missed — `ProviderController` and `ProviderDocumentsController` both declared the identical `[HttpPost("provider/documents/upload")]`, causing `AmbiguousMatchException` (HTTP 500) on document upload. **Fixed** by moving the application-flow route to `/provider/apply/documents/upload`. The parallel `ProviderController` `provider/documents/replace` did **not** collide (the standalone owner uses `provider/documents/{id}/replace`) but was moved to `/provider/apply/documents/replace` for namespace hygiene / consistency. Oracle-confirmed minimal fix; permission families unchanged (`ProviderApplication.*` vs `ProviderDocument.*`); tag-helper views auto-reroute.


> **Method:** deep code-vs-plan audit against the **shipped** `Areas/Provider/Controllers/*` (20 controllers) and
> `Areas/Business/Controllers/*` (6 controllers). The plan's permission constants were already corrected in the earlier §3 audit
> and are **accurate**; this report is about **BFF routes, verbs, and page structure** vs. the real controllers.
>
> **Scope:** `yallajo-plan/3-provider-dashboard.md` (§4 provider).
> **Severity:** 🔴 plan contradicts code · 🟠 route/verb shape wrong · 🟡 cosmetic/clarity.
> **Status:** ❌ not built · ✏️ built, route/verb differs · ➕ shipped but undocumented in plan · ✅ matches.

---

## 0. Summary

| Topic | Verdict |
|-------|---------|
| **Permissions** | ✅ Accurate (corrected in the prior §3 audit) — granular per-action constants match the shipped `[RequirePermission]`. |
| **§4.2 Application** | 🔴 "Register → `POST /provider/register`" is **wrong** — there is **no `/provider/register`**; register is `POST /provider/apply` (→ `RegisterAsync`), then `POST /provider/submit`. |
| **§4.3 Tours editor** | 🟠 Split across **8 controllers** with page-scoped sub-routes `/provider/tours/{id}/{pricing\|schedules\|waypoints\|guides\|images\|availability}/...`; all RESTful `POST` (create/edit/delete + GET forms), not the API `/tours/{id}/...` `PUT`/`DELETE`. |
| **§4.5 Businesses** | 🟠 Route is **`/business/businesses`** (not `/business/listings`); register, sub-resources split into 6 controllers with `/add`,`/remove`,`/save`,`/edit` POST actions. |
| **§4.6 Bookings** | 🟠➕ **Two controllers**: `BookingsController` (`/provider/bookings` + **join-requests**) and `ProviderBookingsController` (`/provider/bookings/manage/*` + a **`/cancel`** action the plan omits). |
| **§4.8 Finance** | ✅(mostly) Unified `/provider/finance` (Payouts/Invoices/Methods tabs) ✓; tab write actions are page-scoped POST on the standalone controllers. |
| **§4.10 Settings** | 🔴 Plan says "no dedicated write-endpoint (read-only)" — **false**; ships `POST /provider/settings/profile\|password\|phone`. |
| **Verbs** | 🟠 Every mutation is page-scoped **`POST`** (e.g. `/provider/tours/{id}/delete`), not API `PUT`/`DELETE`/`PATCH`. |

---

## 1. 🔴 Plan-contradicts-code

### G1 — §4.2 "Register → `POST /provider/register`" route does not exist
- **Plan:** Register → `POST /provider/register`; Submit Application → `POST /provider/apply`.
- **Code (`ProviderController`):** `GET /provider/status` · `GET/POST /provider/apply` (POST calls `RegisterAsync`, perm `ProviderApplication.Register`) · **`POST /provider/submit`** (`ProviderApplication.Submit`) · `POST /provider/reapply` (`.Update`) · `POST /provider/apply/documents/upload` (`.Create`) · `POST /provider/apply/documents/replace` (`.Update`). **There is no `/provider/register`.** *(Application-doc routes were `provider/documents/{upload,replace}` at audit time; relocated under `/provider/apply/*` during Phase 2 to resolve a route collision — see RESOLVED banner.)*
- **Action:** Register → `POST /provider/apply`; Submit → `POST /provider/submit`. Fix the button list.

### G2 — §4.10 Settings is NOT read-only
- **Plan:** "Save Settings → no dedicated write-endpoint (read-only; persisted via §3.10)."
- **Code (`SettingsController`):** `GET /provider/settings` **plus** `POST /provider/settings/profile`, `POST /provider/settings/password`, `POST /provider/settings/phone`. Provider settings has its **own** write actions (does not defer to Accounts).
- **Action:** §4.10 → list the three POST actions; drop the "read-only / persisted via §3.10" claim.

### G3 — §4.9 Documents replace is `POST .../replace`, not `PUT`
- **Plan:** Replace → `PUT /booking/provider/documents/{id}`.
- **Code (`ProviderDocumentsController`):** `GET /provider/documents` · `POST /provider/documents/upload` (`ProviderDocument.Create`) · **`POST /provider/documents/{id}/replace`** (`ProviderDocument.Update`). BFF route is `/provider/documents/*`, not `/booking/provider/documents/*` (that's the API path).
- **Action:** Replace → `POST /provider/documents/{id}/replace`; BFF list is `/provider/documents`.

---

## 2. 🟠 Route / verb / structure gaps

### G4 — §4.3 Tours editor is 8 controllers with page-scoped sub-routes
- **Code reality** (all `[HttpPost]`, area `Provider`, perms as the plan lists):

  | Sub-feature | Shipped routes |
  |-------------|----------------|
  | Tours | `GET /provider/tours` · `GET/POST /provider/tours/create` · `GET/POST /provider/tours/{id}/edit` · `POST /provider/tours/{id}/submit\|archive\|delete` |
  | Pricing (`TourPricingController`) | `GET /provider/tours/{id}/pricing` · `GET/POST .../pricing/create` · `GET/POST .../pricing/{tierId}/edit` · `POST .../pricing/{tierId}/delete` |
  | Schedules | `.../schedules` + `create`/`{scheduleId}/edit`/`{scheduleId}/delete` |
  | Waypoints | `.../waypoints` + `create`/`{waypointId}/edit`/`{waypointId}/delete` + **`POST .../waypoints/reorder`** |
  | Guides | `GET .../guides` · `POST .../guides/assign` · `POST .../guides/{guideUserId}/remove` |
  | Images (`TourImagesController`) | `GET .../images` · `POST .../images/upload` · `POST .../images/{attachmentId}/delete` |
  | Applications (`TourApplicationsController`) | `GET .../applications` · `POST .../applications/{applicationId}/approve\|reject` · `POST .../applications/open\|close` |

- **Notes vs plan:** open/close applications are **`/applications/open`** and **`/applications/close`** (not `/open-applications`); guide assign is **`/guides/assign`** (not `POST /tours/{id}/guides`); images are a dedicated controller.
- **Action:** add the API-vs-BFF note; list the page-scoped `/provider/tours/{id}/...` routes; fix open/close + guide-assign + images.

### G5 — §4.5 Businesses route + sub-resource shape
- **Code (`Areas/Business`, 6 controllers):**
  - `MyBusinessesController`: `GET /business` · `GET /business/businesses` · `GET/POST /business/businesses/register` (`Business.Create`) · `GET /business/businesses/{id}` · `POST /business/businesses/{id}/update` (`Business.Update`) · `POST /business/businesses/{id}/resubmit`.
  - Amenities: `GET .../amenities` · `POST .../amenities/add` · `POST .../amenities/{amenityId}/remove`.
  - Hours: `POST .../hours/save`. Accessibility: `POST .../accessibility/save`.
  - Services: `POST .../services/add` · `GET/POST .../services/{serviceId}/edit` · `POST .../services/{serviceId}/remove` (`ServiceItem.SoftDelete`).
  - Staff: `POST .../staff/add` · `POST .../staff/{staffId}/remove`.
- **vs plan:** Route is **`/business/businesses`** (not `/business/listings`); create is **`register`**; save is **`update`**; sub-actions are `/add`,`/remove`,`/save`,`/edit` POST.
- **Action:** fix §4.5 route to `/business/businesses`; list the real sub-resource POST routes.

### G6 — §4.6 Bookings: two controllers + an undocumented Cancel + join-requests
- **Code:**
  - `BookingsController`: `GET /provider/bookings` · `POST /provider/bookings/lookup` · **`POST /provider/bookings/join-requests/{id}/approve\|reject`** (join-request moderation lives here) · `POST /provider/bookings/{id}/confirm\|reject`.
  - `ProviderBookingsController`: `GET /provider/bookings/manage[/{id}]` · `POST /provider/bookings/manage/{id}/confirm\|reject\|`**`cancel`**`\|complete`.
- **vs plan:** plan §4.6 describes one bookings page (confirm/reject/complete + slots) and omits: (a) the **`/manage`** sub-surface, (b) **provider Cancel** (`/manage/{id}/cancel`), (c) **join-request approve/reject** (plan puts join-requests only in §5.10 Guide).
- **Action:** document both controllers; add provider Cancel + provider-side join-request actions; note slots are under **`/provider/tours/{id}/availability/*`** (TourAvailabilityController), not a bare `/booking/availability/slots`.

### G7 — §4.6 Availability slots are page-scoped under the tour
- **Plan:** `POST /booking/availability/slots[/bulk]` · `PUT/DELETE .../slots/{id}`.
- **Code (`TourAvailabilityController`):** `GET /provider/tours/{id}/availability` · `POST .../availability/bulk` · `GET/POST .../availability/create` · `GET/POST .../availability/{slotId}/edit` · `POST .../availability/{slotId}/delete`. (The RowVersion `/manage` read is the API binding; BFF is `/provider/tours/{id}/availability`.)
- **Action:** slot CRUD → the page-scoped `/provider/tours/{id}/availability/*` routes.

### G8 — §4.7 Reviews reply edit/delete are page-scoped POST
- **Code (`ReviewsController`):** `GET /provider/reviews` · `POST /provider/reviews/{id}/reply` · `POST .../reply/{replyId}/edit` · `POST .../reply/{replyId}/delete` · `POST /provider/reviews/{id}/report`.
- **Action:** replace `PUT/DELETE .../reply/{replyId}` with `POST .../reply/{replyId}/edit\|delete`.

---

## 3. ➕ Shipped-but-undocumented

### G9 — §4.8 Finance is a unified page + standalone write controllers (plan tab-structure is right)
- **Code:** `FinanceController` = "one page with Payouts / Invoices / Methods tabs" (aggregates `EarningsFacade` + `ProviderInvoicesFacade`); standalone controllers own the writes:
  - `EarningsController` `GET /provider/earnings`, `InvoicesController` `GET /provider/invoices` + `/{id}/download`, `PaymentMethodsController` `GET /provider/payment-methods` + `POST .../create` · `/{id}/edit` · `/{id}/delete`.
- **vs plan:** §4.8 tab model is **correct** ✓; only the **method write routes** differ (`POST /provider/payment-methods/create|{id}/edit|{id}/delete`, not `POST/PUT/DELETE /provider-payment-methods`).
- **Action:** keep the tab model; fix the method write routes to the page-scoped POST.

### G10 — Provider settings has password+phone (overlaps §3.10)
- `SettingsController` ships `POST /provider/settings/password` and `/phone` — provider can change password/phone **without** going to the Accounts area. The plan's "shared with §3.10" is only partly true (provider has its own copies).
- **Action:** note the provider-local password/phone actions.

---

## 4. ✅ Confirmed-correct

- **Permissions** — all per-action constants match the shipped `[RequirePermission]` (e.g. `ProviderApplication.Register/Submit/Update/Create`, `Tour.*`, `Package.*`, `Business.*` + sub-resources, `TourBooking.*`, `AvailabilitySlot.*`, `Invoice.*`, `ProviderPaymentMethod.*`, `ProviderDocument.*`, `ProviderDashboard.Read`). ✅
- **§4.1 Overview** — `GET /provider`, `/provider/dashboard` (`ProviderDashboard.Read`) ✅; ERR3/API1 correct.
- **§4.8 tab structure** ✅. **Cache `NoStore`** everywhere ✅.
- **ST1 claims** — §4.3 Tours (RowVersion ✓), §4.5 Business (UpdatedAt only ✓), §4.4 Packages (none ✓) all match the DTO reality from the prior audit.

---

## 5. Recommended plan edits (apply order)

1. **G1** §4.2 Register → `POST /provider/apply`; Submit → `POST /provider/submit` (no `/provider/register`).
2. **G2** §4.10 Settings is NOT read-only — add `POST /provider/settings/profile|password|phone`.
3. **G5** §4.5 route → `/business/businesses`; fix register/update/sub-resource routes.
4. **G6** §4.6 document the two booking controllers + provider Cancel + join-request approve/reject; slots under `/provider/tours/{id}/availability/*`.
5. **G3/G4/G7/G8/G9** add API-vs-BFF note; replace API `PUT`/`DELETE` button targets with the page-scoped `POST /provider/...` routes (tours editor, availability, reviews reply, payment methods, documents replace).
6. **G10** note provider-local password/phone.

> **Net:** permissions are solid, but the provider plan describes **API routes/verbs** where the shipped BFF uses **page-scoped POST** across 26 controllers. Two outright errors: **§4.2 `/provider/register` doesn't exist** (it's `/provider/apply`) and **§4.10 Settings is not read-only** (ships profile/password/phone writes). The bookings surface is **richer than documented** (a `/manage` controller, provider Cancel, and provider-side join-request moderation).
