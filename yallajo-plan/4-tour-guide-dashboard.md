# Tour Guide Dashboard — Area

**Actor:** Tour Guide · **Plan section:** §5 · **Namespaces:** `/guide/*`, `/guides/me/*`, `/guides/{id}/*`, `/tours/{tourId}/guide-offerings/*`, `/tours/proposals/*`, `/booking/guide-discounts/*`, `/finance/guide/*`

> **Source of truth:** YallaJo code (`yallajo-endpoints.txt`, 549 endpoints). All routes are `/api/v1`-prefixed.
> **Template family:** the Webestica `agent-*` pages are **reskinned** for the guide where they fit; everything guide-specific has **no template page** and is built by you.
> **Architecture & rules:** every page follows the four-tier pipeline (Controller → Facade → ApiClient → IApiClient) and the UI-UX rules — see [`0-architecture-and-rules.md`](0-architecture-and-rules.md).
> **Code area:** all pages live in the **`Guide`** area (`/guide/*`), `NoStore` cache (UI-PERF-C2, authenticated). The `Areas/Guide` controllers are **already implemented**; the **Perm** lines below are taken from the *shipped* `[RequirePermission]` attributes.
>
> **Permission reality (verified against `Areas/Guide/Controllers/*` + `WebPermission.cs`):** there is **no** `Guide`, `Booking.Discounts`, `Booking.JoinRequests`, or `Finance.Read` namespace. The shipped controllers use only `GuideDashboard.Read` (dashboard + tier), `TourGuideProfile.DeleteOwn` (profile deactivate), and `GuideOffering.Delete` (remove offering); **every other guide action is `[Authorize]` only**, with ownership enforced by the API. (A separate `AgencyRosterController` uses `AgencyRoster.*` — that is the agency-owner roster surface, not §5.11.)
>
> **BFF verbs are `POST` (PRG):** the `PUT`/`DELETE` shown in the endpoint/button lines are the **API-layer** verbs (ApiClient → API). Every BFF action the browser posts to is `[HttpPost]` + `@Html.AntiForgeryToken()` and follows PRG: a **successful** mutation redirects, while **errors** either re-render the form with errors or redirect with a flash message, depending on the action — e.g. the shipped routes are `POST guide/profile/update`, `POST guide/profile/languages/delete`, `POST guide/tours/{tourId}/schedules/{scheduleId}/delete`, `POST guide/tours/{tourId}/offering/remove`.
>
> **BFF route reality (verified against shipped `Areas/Guide/Controllers/*` — 14 controllers):** Dashboard, Profile, Analytics, Earnings, Tier, Availability, **MyTours** (= My Offerings, at `/guide/tours`), Discounts, **Applications** (`/guide/applications`), **Proposals** (`/guide/proposals`), JoinRequests, **Reviews** (`/guide/reviews`), Agency, AgencyRoster. The shipped **sidebar nav has up to 14 route links** matching these controllers — **13 always render**, plus **AgencyRoster** which is conditional on `Has(WebPermission.AgencyRoster.Read)` (`_GuideSidebar.cshtml`). Each **§5.x Route** below is the page-scoped **BFF** route the browser actually targets — distinct from the `/api/v1/...` API paths in the namespace list above. All 32 guide POST actions carry `[ValidateAntiForgeryToken]` and follow PRG: success redirects, errors either re-render the form or redirect with a flash, depending on the action.

**Status legend:** ✅ Wire (use template page as-is) · ♻️ Repurpose (adapt an existing template page) · ⏭️ Skip (no UI) · 🟥 USER builds (no template page — build with a design skill)

**Shared shell:** top bar = global search + language switcher + notification bell (`GET /notifications/unread-count` `AJAX⟳`) + avatar menu (`GET /accounts/profile`). Nav driver = `GET /security/me`.
**Load tags:** `SSR` / `AJAX` / `AJAX⟳` (poll/SignalR) / `AJAX↑` (upload).

---

## Pages this area should have

| # | Page | Template | Redirects to |
|---|------|----------|--------------|
| 5.1 | Overview `/guide` | `agent-dashboard.html` ♻️ | KPI cards → §5.4 Earnings, §5.7 My Tours / Offerings |
| 5.2 | Profile | 🟥 Build Using Design skills | save inline · view public → §2.7 Guide detail |
| 5.3 | Analytics | `agent-dashboard.html` ♻️ (charts) | drill-down → §5.4 / §5.7 |
| 5.4 | Earnings | `agent-earnings.html` ♻️ | by-tour → §5.7 · invoice download inline |
| 5.5 | Tier | 🟥 Build Using Design skills | — |
| 5.6 | Availability | 🟥 Build Using Design skills (Flatpickr) | save inline |
| 5.7 | My Tours / Offerings `/guide/tours` | 🟥 Build Using Design skills | offering editor → `/guide/tours/{tourId}` · parent tour → §2.5 |
| 5.8 | Discounts | 🟥 Build Using Design skills | create/edit inline |
| 5.9a | Applications `/guide/applications` | 🟥 Build Using Design skills | open tour → §2.5 · apply inline |
| 5.9b | Proposals `/guide/proposals` | 🟥 Build Using Design skills | proposal create/submit inline |
| 5.10 | Join Requests | 🟥 Build Using Design skills | approve/reject inline |
| 5.11 | Agency | 🟥 Build Using Design skills | invitation → §5.11 · browse agencies → §2.8 |
| 5.12 | Reviews `/guide/reviews` | 🟥 Build Using Design skills | reviewed entity → §2.5/§2.7 (read-only) |

---

## Endpoints by page

### 5.1 Overview `/guide`
> **`/guide` is the area landing = the dashboard.** `DashboardController` serves **both** `GET /guide` and `GET /guide/dashboard` (both gated by `GuideDashboard.Read`).
- `GET /guide` · `GET /guide/dashboard` `SSR` *(KPI/feed data is fetched server-side from the underlying API and rendered into this one page; the dashboard does not expose a separate `/guide/my-tours` BFF route — the My Tours surface is §5.7 at `/guide/tours`.)*
- **Buttons:** View Earnings → §5.4 (nav) · My Tours / Offerings → §5.7 (nav) · Find Tours → §5.9a (nav). Read-only KPIs.
- **Stack:** **Area** `Guide` · **Route** `/guide` · `/guide/dashboard` · **Cache** `NoStore` (UI-PERF-C2) · **Perm** `[Authorize]` + class-level `[RequirePermission(WebPermission.GuideDashboard.Read)]` (shipped `DashboardController`; there is no named `Guide` policy) · **Rules** `R2` SSR first paint · `API1` parallel calls (then `GuardSignOut` each) · `ERR3` each KPI section degrades to a "Couldn't load — [Retry]" card · dashboard-bundle ApexCharts (allowed off public, `X14`) · `A11Y1`

### 5.2 Profile
- `GET /guides/me` `SSR`
- `PUT /guides/{id}` (own id) `AJAX`
- `PUT /guides/me/avatar` · `PUT /guides/me/cover-image` `AJAX↑`
- `POST /guides/{id}/languages` · `DELETE /guides/{id}/languages/{languageId}` `AJAX`
- `POST /guides/{id}/specializations` `AJAX`
- `DELETE /guides/me` (deactivate) `AJAX`
- **Buttons (canonical BFF routes):** Save Profile → `POST /guide/profile/update` · Change Avatar → `POST /guide/profile/avatar` (AJAX↑) · Change Cover → `POST /guide/profile/cover` (AJAX↑) · Add Language → `POST /guide/profile/languages` · Remove Language → `POST /guide/profile/languages/delete` · Add Specialization → `POST /guide/profile/specializations` · Deactivate → `POST /guide/profile/deactivate`.
- **Stack:** **Area** `Guide` · **Route** `/guide/profile` · **Cache** `NoStore` · **Perm** `[Authorize]` (API-enforced ownership) for update/languages/specializations/avatar/cover; **`WebPermission.TourGuideProfile.DeleteOwn`** only on **Deactivate** (shipped `ProfileController`) · **Rules** `F1`–`F3` forms · BFF actions are `[HttpPost]`+`[ValidateAntiForgeryToken]`, PRG (success redirects; errors re-render or redirect-with-flash per action) (`POST guide/profile/update`, `…/languages`, `…/languages/delete`, `…/avatar`, `…/cover`, `…/deactivate`) · avatar/cover `AJAX↑` `SEC4` magic-byte+EXIF-strip · `RTL1`/i18n · public view → §2.7 *(no `ST1`: no Guide DTO exposes a `RowVersion` token)*

### 5.3 Analytics
- `GET /guides/me/analytics/overview` `SSR`
- `GET /guides/me/analytics/booking-trends` · `/peak-days` · `/popular-tours` `AJAX`
- **Buttons:** drill-down → §5.4 / §5.7 (nav). Read-only.
- **Stack:** **Area** `Guide` · **Route** `/guide/analytics` · **Cache** `NoStore` · **Perm** `[Authorize]` (API-enforced ownership; shipped `AnalyticsController` has no `[RequirePermission]`) · **Rules** `R2` SSR · ApexCharts charts-bundle · `API1` parallel · read-only

### 5.4 Earnings
- `GET /guides/me/earnings/summary` `SSR` *(canonical; `/finance/guide/summary` is the finance-module equivalent)*
- `GET /guides/me/earnings/history` `AJAX` *(canonical; `/finance/guide` equivalent)*
- `GET /guides/me/earnings/by-tour` `AJAX`
- **Buttons:** By Tour → §5.7 (nav). Read-only earnings.
- **Stack:** **Area** `Guide` · **Route** `/guide/earnings` · **Cache** `NoStore` · **Perm** `[Authorize]` (API-enforced ownership; shipped `EarningsController` has no `[RequirePermission]`) · **Rules** `R6` DataTables server-side >500 · `PRINT2` invoice PDF · `CON3` JOD 3-decimals · read-only

### 5.5 Tier
- `GET /guides/me/tier` `SSR`
- **Buttons:** View Analytics → §5.3 (nav). Read-only widget.
- **Stack:** **Area** `Guide` · **Route** `/guide/tier` · **Cache** `NoStore` · **Perm** `[Authorize]` + class-level `[RequirePermission(WebPermission.GuideDashboard.Read)]` (shipped `TierController`; no named `Guide` policy) · **Rules** `R2` SSR · read-only widget

### 5.6 Availability `/guide/availability`
- `GET /guide/availability` `SSR` *(API: `GET /guides/me/availability-blocks`)*
- `POST /guide/availability/add` · `POST /guide/availability/{blockId}/delete`
- **Buttons:** Add Block → `POST /guide/availability/add` · Remove Block → `POST /guide/availability/{blockId}/delete`.
- **Stack:** **Area** `Guide` · **Route** `/guide/availability` · **Cache** `NoStore` · **Perm** `[Authorize]` (API-enforced ownership; shipped `AvailabilityController` has no `[RequirePermission]`) · **Rules** Flatpickr date picker · `F`-forms · BFF `[HttpPost]`+`[ValidateAntiForgeryToken]`, PRG (success redirects; errors re-render or redirect-with-flash per action) · `SEC7` anti-forgery *(no `ST1`: availability-block DTO exposes no `RowVersion`)*

### 5.7 My Tours / Offerings `/guide/tours`
> **Shipped as `MyToursController`** — list at `GET /guide/tours`, per-tour offering editor at `GET /guide/tours/{tourId}`. **There is no `/guide/offerings` route.** `guideId` is resolved server-side (the BFF owns the `GET /guides/me` → real-id resolution); browser routes are keyed by `{tourId}` only.
> **No in-place edit for tiers/schedules.** The shipped UX is **add-then-delete-and-re-add**, not edit. Only Add + Delete actions ship for pricing-tiers and schedules; private-tour is enable/disable.
- `GET /guide/tours` `SSR` (list) · `GET /guide/tours/{tourId}` `SSR` (offering editor)
- `POST /guide/tours/{tourId}/pricing-tiers` (**add**) · `POST /guide/tours/{tourId}/pricing-tiers/{tierId}/delete` (**delete**)
- `POST /guide/tours/{tourId}/schedules` (**add**) · `POST /guide/tours/{tourId}/schedules/{scheduleId}/delete` (**delete**)
- `POST /guide/tours/{tourId}/private-tour` (**enable**) · `POST /guide/tours/{tourId}/private-tour/delete` (**disable**)
- `POST /guide/tours/{tourId}/offering/remove` (**remove offering**, `GuideOffering.Delete`)
- **Buttons:** Add Pricing Tier → `POST /guide/tours/{tourId}/pricing-tiers` · Delete Pricing Tier → `POST /guide/tours/{tourId}/pricing-tiers/{tierId}/delete` · Add Schedule → `POST /guide/tours/{tourId}/schedules` · Delete Schedule → `POST /guide/tours/{tourId}/schedules/{scheduleId}/delete` · Enable Private Tour → `POST /guide/tours/{tourId}/private-tour` · Disable Private Tour → `POST /guide/tours/{tourId}/private-tour/delete` · Remove Offering → `POST /guide/tours/{tourId}/offering/remove`. *(No "Save / edit tier / edit schedule" buttons — those actions are **not built**; to change a tier or schedule, delete and re-add.)*
- **Stack:** **Area** `Guide` · **Route** `/guide/tours` (list) · `/guide/tours/{tourId}` (offering editor) · **Cache** `NoStore` · **Perm** `[Authorize]` (API-enforced ownership) for pricing/schedules/private-tour; **`WebPermission.GuideOffering.Delete`** only on **Remove Offering** (shipped `MyToursController`) · **Rules** BFF actions are `[HttpPost]`+`[ValidateAntiForgeryToken]`, PRG (success redirects; errors re-render or redirect-with-flash per action) · `C3` evict `tour:{tourId}` (offerings change tour detail) · `SEC7` *(no `ST1`: no offering DTO exposes a `RowVersion`)*

### 5.8 Discounts
- `GET /guide/discounts` `SSR` *(API: `GET /booking/guide-discounts/mine`)*
- `POST /guide/discounts/create` · `POST /guide/discounts/{id}/edit` · `POST /guide/discounts/{id}/delete` *(Discounts **does** support edit, unlike offerings.)*
- **Buttons:** New Discount → `POST /guide/discounts/create` · Edit → `POST /guide/discounts/{id}/edit` · Delete → `POST /guide/discounts/{id}/delete`.
- **Stack:** **Area** `Guide` · **Route** `/guide/discounts` · **Cache** `NoStore` · **Perm** `[Authorize]` (API-enforced ownership; shipped `DiscountsController` has no `[RequirePermission]`) · **Rules** `F`-forms · `F8` delete confirm · BFF actions `[HttpPost]`+`[ValidateAntiForgeryToken]`, PRG (success redirects; errors re-render or redirect-with-flash per action) · `SEC7` *(no `ST1`: discount DTO exposes no `RowVersion`)*

### 5.9a Applications `/guide/applications`
> **Two separate controllers.** Applications and Proposals ship as `ApplicationsController` and `ProposalsController` — two pages, not one.
- `GET /guide/applications` `SSR` *(API: `GET /guides/me/applications`)*
- `POST /guide/applications/apply` *(API: `POST /tours/{tourId}/applications`)*
- **Buttons:** Apply to Tour → `POST /guide/applications/apply`.
- **Stack:** **Area** `Guide` · **Route** `/guide/applications` · **Cache** `NoStore` · **Perm** `[Authorize]` (API-enforced ownership; shipped `ApplicationsController` has no `[RequirePermission]`) · **Rules** `D1` page-number paging on the list · `F`-forms · BFF `[HttpPost]`+`[ValidateAntiForgeryToken]`, PRG (success redirects; errors re-render or redirect-with-flash per action) · `SEC7`

### 5.9b Proposals `/guide/proposals`
- `GET /guide/proposals` `SSR` *(API: `GET /tours/proposals?guideId=` · `GET /tours/proposals/{id}`)*
- `POST /guide/proposals/create` · `POST /guide/proposals/{id}/submit`
- **Buttons:** New Proposal → `POST /guide/proposals/create` · Submit Proposal → `POST /guide/proposals/{id}/submit`.
- **Stack:** **Area** `Guide` · **Route** `/guide/proposals` · **Cache** `NoStore` · **Perm** `[Authorize]` (API-enforced ownership; shipped `ProposalsController` has no `[RequirePermission]`) · **Rules** `D1` page-number paging on the list · `F`-forms · BFF `[HttpPost]`+`[ValidateAntiForgeryToken]`, PRG (success redirects; errors re-render or redirect-with-flash per action) · `SEC7`

### 5.10 Join Requests `/guide/join-requests`
> **Request id is posted in the form body, not the URL.** The shipped routes are flat: `POST /guide/join-requests/approve` and `/reject` (each with a hidden `id` field), not `/{id}/approve`.
- `GET /guide/join-requests` `SSR` *(API: `GET /booking/join-requests?bookingId=`)*
- `POST /guide/join-requests/approve` · `POST /guide/join-requests/reject` *(id in body)*
- **Buttons:** Approve → `POST /guide/join-requests/approve` (id in body) · Reject → `POST /guide/join-requests/reject` (id in body).
- **Stack:** **Area** `Guide` · **Route** `/guide/join-requests` · **Cache** `NoStore` · **Perm** `[Authorize]` (API-enforced ownership; shipped `JoinRequestsController` has no `[RequirePermission]`) · **Rules** approve/reject inline · BFF `[HttpPost]`+`[ValidateAntiForgeryToken]`, PRG (success redirects; errors re-render or redirect-with-flash per action) · `NF1` toast · `SEC7`

### 5.11 Agency `/guide/agency`
> All BFF routes are page-scoped `POST` under `/guide/agency/*` (`AgencyController`); leave is `POST /guide/agency/leave`, not API `DELETE /guides/me/agency`.
- `GET /guide/agency` `SSR` *(API: `GET /guides/me/invitations` + agency browse)*
- `POST /guide/agency/apply` *(API: `POST /guides/agencies/{agencyUserId}/apply`)*
- `POST /guide/agency/invitations/{id}/accept` · `POST /guide/agency/invitations/{id}/decline`
- `POST /guide/agency/leave` *(API: `DELETE /guides/me/agency`)*
- **Buttons:** Accept Invitation → `POST /guide/agency/invitations/{id}/accept` · Decline → `POST /guide/agency/invitations/{id}/decline` · Apply to Agency → `POST /guide/agency/apply` · Leave Agency → `POST /guide/agency/leave`.
- **Stack:** **Area** `Guide` · **Route** `/guide/agency` · **Cache** `NoStore` · **Perm** `[Authorize]` (API-enforced ownership; shipped `AgencyController` has no `[RequirePermission]`) · **Rules** `F8` confirm Leave Agency (destructive) · BFF `[HttpPost]`+`[ValidateAntiForgeryToken]`, PRG (success redirects; errors re-render or redirect-with-flash per action) · `SEC7` · browse agencies → §2.8
> The agency-**owner** roster surface (managing affiliated guides) is a **separate** shipped `AgencyRosterController` gated by `WebPermission.AgencyRoster.*` — that is §6 Agency Dashboard, not this guide-side §5.11 (which is the guide joining/leaving an agency).

### 5.12 Reviews `/guide/reviews`
> **Shipped page with a sidebar nav item** (`ReviewsController` + "Reviews" in `_GuideSidebar.cshtml`). Read-only surface where the guide views reviews about their offerings/profile.
- `GET /guide/reviews` `SSR` (read-only list)
- **Buttons:** open reviewed entity → §2.5 Tour detail / §2.7 Guide detail (nav). No write actions on this page.
- **Stack:** **Area** `Guide` · **Route** `/guide/reviews` · **Cache** `NoStore` · **Perm** `[Authorize]` (API-enforced ownership; shipped `ReviewsController` has no `[RequirePermission]`) · **Rules** `D1` page-number paging · `A11Y5` rating color+icon+text · read-only
