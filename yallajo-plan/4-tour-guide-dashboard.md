# Tour Guide Dashboard — Area

**Actor:** Tour Guide · **Plan section:** §5 · **Namespaces:** `/guide/*`, `/guides/me/*`, `/guides/{id}/*`, `/tours/{tourId}/guide-offerings/*`, `/tours/proposals/*`, `/booking/guide-discounts/*`, `/finance/guide/*`

> **Source of truth:** YallaJo code (`yallajo-endpoints.txt`, 549 endpoints). All routes are `/api/v1`-prefixed.
> **Template family:** the Webestica `agent-*` pages are **reskinned** for the guide where they fit; everything guide-specific has **no template page** and is built by you.
> **Architecture & rules:** every page follows the four-tier pipeline (Controller → Facade → ApiClient → IApiClient) and the UI-UX rules — see [`0-architecture-and-rules.md`](0-architecture-and-rules.md).
> **Code area:** all pages live in the **`Guide`** area (`/guide/*`), `NoStore` cache (UI-PERF-C2, authenticated), gated by policy `Guide` + `WebPermission.Guide.*`.

**Status legend:** ✅ Wire (use template page as-is) · ♻️ Repurpose (adapt an existing template page) · ⏭️ Skip (no UI) · 🟥 Build Using Design skills (match the template theme)

**Shared shell:** top bar = global search + language switcher + notification bell (`GET /notifications/unread-count` `AJAX⟳`) + avatar menu (`GET /accounts/profile`). Nav driver = `GET /security/me`.
**Load tags:** `SSR` / `AJAX` / `AJAX⟳` (poll/SignalR) / `AJAX↑` (upload).

---

## Pages this area should have

| # | Page | Template | Redirects to |
|---|------|----------|--------------|
| 5.1 | Overview `/guide` | `agent-dashboard.html` ♻️ | KPI cards → §5.4 Earnings, §5.7 My Offerings |
| 5.2 | Profile | 🟥 Build Using Design skills | save inline · view public → §2.7 Guide detail |
| 5.3 | Analytics | `agent-dashboard.html` ♻️ (charts) | drill-down → §5.4 / §5.7 |
| 5.4 | Earnings | `agent-earnings.html` ♻️ | by-tour → §5.7 · invoice download inline |
| 5.5 | Tier | 🟥 Build Using Design skills | — |
| 5.6 | Availability | 🟥 Build Using Design skills (Flatpickr) | save inline |
| 5.7 | My Offerings | 🟥 Build Using Design skills | offering editor inline · parent tour → §2.5 |
| 5.8 | Discounts | 🟥 Build Using Design skills | create/edit inline |
| 5.9 | Applications & Proposals | 🟥 Build Using Design skills | open tour → §2.5 · proposal detail inline |
| 5.10 | Join Requests | 🟥 Build Using Design skills | approve/reject inline |
| 5.11 | Agency | 🟥 Build Using Design skills | invitation → §5.11 · browse agencies → §2.8 |

---

## Endpoints by page

### 5.1 Overview `/guide`
- `GET /guide/dashboard` `SSR`
- `GET /guide/my-tours` `AJAX` — guide performance surface (distinct from provider surfaces)
- `GET /guide/analytics` `AJAX`
- **Buttons:** View Earnings → §5.4 (nav) · My Offerings → §5.7 (nav) · Find Tours → §5.9 (nav). Read-only KPIs.
- **Stack:** **Area** `Guide` · **Route** `/guide/dashboard` · **Cache** `NoStore` (UI-PERF-C2) · **Perm** `Guide` + `WebPermission.Guide.Read` · **Rules** `R2` SSR first paint · `API1` parallel calls · dashboard-bundle ApexCharts (allowed off public, `X14`) · `A11Y1`

### 5.2 Profile
- `GET /guides/me` `SSR`
- `PUT /guides/{id}` (own id) `AJAX`
- `PUT /guides/me/avatar` · `PUT /guides/me/cover-image` `AJAX↑`
- `POST /guides/{id}/languages` · `DELETE /guides/{id}/languages/{languageId}` `AJAX`
- `POST /guides/{id}/specializations` `AJAX`
- `DELETE /guides/me` (deactivate) `AJAX`
- **Buttons:** Save Profile → `PUT /guides/{id}` · Change Avatar → `PUT /guides/me/avatar` (AJAX↑) · Change Cover → `PUT /guides/me/cover-image` (AJAX↑) · Add/Remove Language → `POST/DELETE /guides/{id}/languages[/{languageId}]` · Add Specialization → `POST /guides/{id}/specializations` · Deactivate → `DELETE /guides/me`.
- **Stack:** **Area** `Guide` · **Route** `/guide/profile` · **Cache** `NoStore` · **Perm** `Guide` + `WebPermission.Guide.Write` · **Rules** `F1`–`F3` forms · avatar/cover `AJAX↑` `SEC4` magic-byte+EXIF-strip · `ST1` concurrency · `RTL1`/i18n · public view → §2.7

### 5.3 Analytics
- `GET /guides/me/analytics/overview` `SSR`
- `GET /guides/me/analytics/booking-trends` · `/peak-days` · `/popular-tours` `AJAX`
- **Buttons:** drill-down → §5.4 / §5.7 (nav). Read-only.
- **Stack:** **Area** `Guide` · **Route** `/guide/analytics` · **Cache** `NoStore` · **Perm** `Guide` + `WebPermission.Guide.Read` · **Rules** `R2` SSR · ApexCharts charts-bundle · `API1` parallel · read-only

### 5.4 Earnings
- `GET /guides/me/earnings/summary` (or `/finance/guide/summary`) `SSR`
- `GET /guides/me/earnings/history` (or `/finance/guide`) `AJAX`
- `GET /guides/me/earnings/by-tour` `AJAX`
- **Buttons:** By Tour → §5.7 (nav). Read-only earnings.
- **Stack:** **Area** `Guide` · **Route** `/guide/earnings` · **Cache** `NoStore` · **Perm** `Guide` + `WebPermission.Finance.Read` · **Rules** `R6` DataTables server-side >500 · `PRINT2` invoice PDF · `CON3` JOD 3-decimals · read-only

### 5.5 Tier
- `GET /guides/me/tier` `SSR`
- **Buttons:** View Analytics → §5.3 (nav). Read-only widget.
- **Stack:** **Area** `Guide` · **Route** `/guide/tier` · **Cache** `NoStore` · **Perm** `Guide` + `WebPermission.Guide.Read` · **Rules** `R2` SSR · read-only widget

### 5.6 Availability
- `GET /guides/me/availability-blocks` `SSR`
- `POST /guides/me/availability-blocks` · `DELETE /guides/me/availability-blocks/{id}` `AJAX`
- **Buttons:** Add Block → `POST /guides/me/availability-blocks` · Remove Block → `DELETE /guides/me/availability-blocks/{id}`.
- **Stack:** **Area** `Guide` · **Route** `/guide/availability` · **Cache** `NoStore` · **Perm** `Guide` + `WebPermission.Guide.Write` · **Rules** Flatpickr date picker · `F`-forms · `SEC7` anti-forgery · `ST1`

### 5.7 My Offerings
> **`guideId` is the REAL id, not `me`** — resolve once via `GET /guides/me`, then use that id.
- `GET /tours/{tourId}/guide-offerings/{guideId}` `SSR`
- `GET /tours/{tourId}/guide-offerings/{guideId}/pricing-tiers` · `/schedules` `AJAX`
- `POST` / `PUT` / `DELETE` pricing-tiers `[/{tierId}]` `AJAX`
- `POST` / `DELETE` schedules `[/{scheduleId}]` `AJAX`
- `POST` / `DELETE` `.../private-tour` `AJAX`
- `DELETE` offering `AJAX`
- **Buttons:** (guideId from `guides/me`) Add/Save/Remove Pricing Tier → `POST/PUT/DELETE .../pricing-tiers[/{tierId}]` · Add/Remove Schedule → `POST/PUT/DELETE .../schedules[/{scheduleId}]` · Enable/Disable Private Tour → `POST/DELETE .../private-tour` · Remove Offering → `DELETE /tours/{tourId}/guide-offerings/{guideId}`.
- **Stack:** **Area** `Guide` · **Route** `/guide/offerings` · **Cache** `NoStore` · **Perm** `Guide` + `WebPermission.Guide.Offerings` · **Rules** resolve real `guideId` via `GET /guides/me` first · `C3` evict `tour:{tourId}` (offerings change tour detail) · `ST1` · `SEC7`

### 5.8 Discounts
- `GET /booking/guide-discounts/mine` `SSR`
- `POST /booking/guide-discounts` · `PUT /booking/guide-discounts/{id}` · `DELETE /booking/guide-discounts/{id}` `AJAX`
- **Buttons:** New Discount → `POST /booking/guide-discounts` · Save → `PUT .../{id}` · Delete → `DELETE .../{id}`.
- **Stack:** **Area** `Guide` · **Route** `/guide/discounts` · **Cache** `NoStore` · **Perm** `Guide` + `WebPermission.Booking.Discounts` · **Rules** `F`-forms · `SEC7` · `ST1`

### 5.9 Applications & Proposals
- `GET /guides/me/applications` `SSR`
- `POST /tours/{tourId}/applications` `AJAX`
- `GET /tours/proposals?guideId=` · `GET /tours/proposals/{id}` `AJAX`
- `POST /tours/proposals` · `POST /tours/proposals/{id}/submit` `AJAX`
- **Buttons:** Apply to Tour → `POST /tours/{tourId}/applications` · New Proposal → `POST /tours/proposals` · Submit Proposal → `POST /tours/proposals/{id}/submit`.
- **Stack:** **Area** `Guide` · **Route** `/guide/proposals` · **Cache** `NoStore` · **Perm** `Guide` + `WebPermission.Guide.Apply` · **Rules** `D1` paging on lists · `F`-forms · `SEC7`

### 5.10 Join Requests
- `GET /booking/join-requests?bookingId=` `SSR`
- `POST /booking/join-requests/{id}/approve` · `/reject` `AJAX`
- **Buttons:** Approve → `POST /booking/join-requests/{id}/approve` · Reject → `POST .../{id}/reject`.
- **Stack:** **Area** `Guide` · **Route** `/guide/join-requests` · **Cache** `NoStore` · **Perm** `Guide` + `WebPermission.Booking.JoinRequests` · **Rules** approve/reject inline · `NF1` toast · `SEC7`

### 5.11 Agency
- `GET /guides/me/invitations` `SSR`
- `POST /guides/invitations/{id}/accept` · `/decline` `AJAX`
- `GET /agency` · `GET /agency/{agencyUserId}` `AJAX`
- `POST /guides/agencies/{agencyUserId}/apply` `AJAX`
- `DELETE /guides/me/agency` `AJAX`
- **Buttons:** Accept Invitation → `POST /guides/invitations/{id}/accept` · Decline → `POST .../{id}/decline` · Apply to Agency → `POST /guides/agencies/{agencyUserId}/apply` · Leave Agency → `DELETE /guides/me/agency`.
- **Stack:** **Area** `Guide` · **Route** `/guide/agency` · **Cache** `NoStore` · **Perm** `Guide` + `WebPermission.Guide.Agency` · **Rules** `F8` confirm Leave Agency (destructive) · `SEC7` · browse agencies → §2.8
