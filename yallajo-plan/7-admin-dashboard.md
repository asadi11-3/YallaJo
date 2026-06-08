# Admin Dashboard — Area

**Actor:** Admin · **Plan section:** §8 · **Namespaces:** `/admin/*`, `*/admin/*`, `/analytics/admin/*`, `/seo/*`, `/content-core/*` (admin), `/finance/admin/*`, `/ops/*`

> **Source of truth:** YallaJo code (`yallajo-endpoints.txt`, 549 endpoints). All routes are `/api/v1`-prefixed.
> **Template family:** Webestica `admin-*` pages cover the overview, moderation, providers (agents), finance lists and users; the rest of the admin consoles have **no template page** and are built by you.
> **Architecture & rules:** see [`0-architecture-and-rules.md`](0-architecture-and-rules.md) — four-tier pipeline (Controller → Facade → ApiClient → IApiClient), code areas, caching, permissions.
> **Code area:** **everything is in the `Admin` area (`/admin/*`)** — the entire surface is **already implemented (43 shipped controllers, 38 routed sidebar nav destination links** — 32 top-level + 6 nested Taxonomy, excluding the Taxonomy collapse toggle anchor**)**. The planned separate `Content` area was **consolidated into `Admin` in code**: §8.9 Content Ops → `/admin/categories|tags|languages|specializations|translations|attachments`, §8.10 SEO → `/admin/seo/*` (this diverges from `0-architecture-and-rules.md` §2, which still lists `Content` as a distinct area — the shipped code wins). Every page is **`NoStore`** (UI-PERF-C2) and guarded by **`[Authorize]` + `[RequirePermission(WebPermission.{Feature}.{Action})]`** at class level (read) with the strictest per-action constant on each write; every write is anti-forgery protected (SEC7) and most use PRG (validation failures may re-render the view — see "Rendering reality" below).
> **Authorization reality (code-verified):** **no `[Authorize(Policy="Admin")]` policy is used by the Web Admin controllers** — the **Web host** (`YallaJo.Web/Program.cs:55`) calls bare `AddAuthorization()` and its `AddPolicy` entries are *output-cache* policies, not authz. (The separate **API host** `YallaJo.Api/Program.cs:191-195` *does* register role policies including `Admin`, but the BFF Admin controllers never reference it.) Enforcement on the Web side is **`[Authorize]` (authenticated) + `RequirePermissionAttribute`** (`Infrastructure/Authorization/RequirePermissionAttribute.cs`, a `TypeFilterAttribute`, `Order = int.MinValue`) whose filter checks **only `ICurrentUser.HasPermission(permission)`** → `Challenge` if unauthenticated, `Forbid` if missing the claim. It does **not** check role `Admin`/`SuperAdmin` directly — the granular permission claims are the gate. Every controller carries `[Area("Admin")]` but **no class-level `[Route]`** (routes are per-action attributes or area convention).
> **Permission + route reality:** the **Perm** and **Route** lines below are taken from the *shipped* `[Area("Admin")]` controllers' `[RequirePermission]` attributes. There is **no** `Admin.Read`, `*.Manage`, or `*.Moderate` umbrella constant — the real constants are granular per-action.
> **Rendering reality (code-verified):** admin **page** GETs return a **Razor View (SSR)** — the one exception is `AuditLogsController.Export` (a GET that streams a **CSV file**, `AuditLogsController.cs:78-98`). Every POST is `[ValidateAntiForgeryToken]` protected; **most mutations are `RedirectToAction` (PRG)**, but form-validation failures **re-render the view** (e.g. `BlogsController.Create`/`Edit`, `NotificationTemplatesController.Create`, `InvitationsController.ResendSubmit`). There are **no JSON/AJAX controller actions** in the Admin area. The `AJAX` / `AJAX⟳` / `AJAX↑` load tags below describe the *intended* in-page refinement and the *API-layer* endpoints the Facade→ApiClient calls — the BFF controller surface itself is SSR + POST. The `LifecycleController` is redirect-only (no views).
> **Optimistic concurrency (ST1) — code-verified carriers:** the admin POSTs that thread a `rowVersion` token are: **`TripsController`** (all four — approve/reject/suspend/reinstate), **`FlaggedReviewsController`** (approve + remove), **`BlogsController`** (restore), **`NotificationTemplatesController`** (delete), and **`SupportController`** (close + resolve). All other admin writes rely on API-layer state-precondition guards (no client-side RowVersion round-trip).
> **BFF verbs are `POST` (PRG):** the `PATCH`/`PUT`/`DELETE` shown in endpoint/button lines are **API-layer** verbs (ApiClient → API). Every BFF write action is `[HttpPost]` + anti-forgery, and most redirect (PRG) — validation failures may re-render the view (see "Rendering reality") (e.g. `POST /admin/providers/{id}/request-documents`, `POST /admin/seo/metadata/save`, `POST /admin/tours/{id}/feature`).

**Status legend:** ✅ Wire · ♻️ Repurpose · ⏭️ Skip · 🟥 USER builds (no template page — but note: most 🟥 admin pages below are **already shipped controllers**; only the view `.cshtml` needs (re)design)

**Shared shell:** top bar = global search + language switcher + notification bell (`GET /notifications/unread-count` `AJAX⟳`) + avatar menu (`GET /accounts/profile`). Nav driver = `GET /security/me`.
**Load tags:** `SSR` / `AJAX` / `AJAX⟳` / `AJAX↑`. Status is first-class UI (badge + action menu); inline mutations return the updated row + toast.

---

## Pages this area should have

| # | Page | Template | Redirects to |
|---|------|----------|--------------|
| 8.1 | Overview | `admin-dashboard.html` ✅ | KPI cards → §8.7 Finance / §8.13 Users / §8.4 Tours |
| 8.2 | Moderation | `admin-reviews.html` ✅ | resolve/approve/remove inline · reported entity |
| 8.3 | Providers | `admin-agent-list.html` ✅ + `admin-agent-detail.html` ✅ | row → provider detail · lifecycle inline |
| 8.4 | Tours Review | 🟥 view redesign (shipped `ToursController` + `TripsController` + `GuideApplicationsController`) | row → §2.5 Tour preview · approve/reject inline |
| 8.5 | Places & Businesses | 🟥 view redesign (shipped `Places`/`Businesses` controllers) | row → §2.3 / §2.4 preview · moderation inline |
| 8.6 | Blogs & Creators | 🟥 view redesign (shipped `Blogs`/`Creators` controllers) | row → §2.9 post/creator · feature/hide inline |
| 8.7 | Finance Ops | `admin-booking-list.html` ♻️ + `admin-booking-detail.html` ♻️ + `admin-earnings.html` ✅ | booking detail · refund inline · payout detail |
| 8.8 | Growth & Merchandising | 🟥 view redesign (shipped `GrowthController`) | targeted entity → §2.5 / §2.3 |
| 8.9 | Content Operations | 🟥 view redesign (shipped `Categories`/`Tags`/`Languages`/… controllers) — tabbed CRUD | inline edits |
| 8.10 | SEO Console | 🟥 view redesign (shipped `Seo*` controllers) — entityType picker | §2.x detail preview · inline |
| 8.11 | Support | 🟥 view redesign (shipped `SupportController`) | ticket thread |
| 8.12 | Platform Ops | 🟥 view redesign (shipped `Outbox`/`NotificationTemplates` controllers) | dead-letter detail inline |
| 8.13 | Users & Audit | `admin-guest-list.html` ✅ + `admin-guest-detail.html` ✅ + `admin-settings.html` ♻️ | guest detail · lifecycle inline · audit export |
| 8.14 | Tour Guides | `admin-agent-detail.html` ♻️ + `admin-agent-list.html` ♻️ | row → §2.7 Guide detail · lifecycle inline · offerings → §8.4 |

---

## Plan page → shipped nav-item map (14 logical pages compress 38 sidebar nav items)

> The shipped sidebar (`Areas/Admin/Views/Shared/_AdminSidebar.cshtml`, included by `_AdminLayout.cshtml`) renders **38 routed nav destination links** (32 top-level + 6 nested under a "Taxonomy" group; the Taxonomy collapse toggle is a 39th `<a>` but not a destination). The 14 logical pages below legitimately *group* related controllers; this table makes the grouping explicit so a builder can locate every shipped surface.

| Plan page | Shipped nav item(s) → controller |
|-----------|----------------------------------|
| 8.1 Overview | Dashboard → `Home`; Analytics → `Statistics` (`/admin/analytics`, `Interaction.Read`) |
| 8.2 Moderation | Moderation → `Moderation`; Reports → `Reports`; Flagged reviews → `FlaggedReviews` (3 controllers) |
| 8.3 Providers | Provider approvals → `Providers` |
| 8.4 Tours Review | Tour approvals → **`Trips`** (sidebar entry; RowVersion-guarded lookup/moderate); `Tours` (list-moderation, proposals/packages/offerings — no own sidebar entry, reached via links); Guide applications → `GuideApplications` |
| 8.5 Places & Businesses | Places & businesses → `Places`; Business approvals → `Businesses` |
| 8.6 Blogs & Creators | Blog moderation → `Blogs` (+ `BlogTranslations`, sub-surface under `/admin/blogs/{id}/translations`); Creator approvals → `Creators` |
| 8.7 Finance Ops | Earnings → `Payments`; Payouts → `Payouts`; Commission rules → `Commissions`; Disputes → `Disputes`; Bookings → `Bookings` (5 controllers) |
| 8.8 Growth & Merchandising | Growth tools → `Growth`; Recommendations → `Recommendations` (2 controllers — see §8.8 split) |
| 8.9 Content Operations | Categories → `Categories`; Tags → `Tags`; Languages → `Languages`; Specializations → `Specializations`; Translations → `Translations`; Attachments → `Attachments` (+ `EntityCategories`/`EntityTags` assignment controllers, no own sidebar entry) |
| 8.10 SEO Console | Metadata → `SeoMetadata`; FAQ → `SeoFaq`; Redirects → `SeoRedirects`; Sitemap → `SeoSitemap`; Weather → `SeoWeather` (5 controllers) |
| 8.11 Support | Support → `Support` |
| 8.12 Platform Ops | Outbox dead-letters → `Outbox`; Notification templates → `NotificationTemplates` |
| 8.13 Users & Audit | Users → `Users`; Roles → `Roles` (`Role.Read`); Invitations → `Invitations` (`User.Create`); Activity & audit → `AuditLogs` (`System.Read`); user lifecycle → `Lifecycle` (redirect-only, `User.UpdateAny`) |
| 8.14 Tour Guides | Tour guides → `Guides` |

---

## Endpoints by page

### 8.1 Overview
- `GET /admin/dashboard` `[/bookings | /revenue | /users]` `SSR`
- `GET /trending` · `GET /popular/*` `AJAX`
- **Buttons:** read-only KPIs; **drill-in** cards → §8.7 / §8.13 / §8.4 (nav). *(No write-endpoints.)*
- **Stack:** **Area** `Admin` · **Route** `/admin`, `/admin/dashboard` (shipped `HomeController`; analytics drill = `/admin/analytics`, shipped `StatisticsController`, `Interaction.Read`) · **Cache** `NoStore` · **Perm** `[Authorize]` + `WebPermission.AdminDashboard.Read` (shipped) · **Rules** `R2` SSR first paint, `API1` Task.WhenAll (then `GuardSignOut` each), `ERR3` each KPI section degrades to a "Couldn't load — [Retry]" card, ApexCharts via `dashboard.bundle.js` (allowed — not a public page), read-only so no SEC7.

### 8.2 Moderation
- `GET /social/reports/admin` · `POST /social/reports/admin/{id}/resolve` `AJAX`
- `GET /social/reviews/admin/flagged` · `POST .../{id}/approve` · `/remove` `AJAX`
- `GET /social/moderation/logs` `AJAX`
- `POST /social/moderation/warn` · `/ban` · `DELETE /social/moderation/ban/{userId}` `AJAX`
- **Buttons:** **Resolve Report** → `POST /social/reports/admin/{id}/resolve` · **Approve Review** → `POST /social/reviews/admin/{id}/approve` · **Remove Review** → `/remove` · **Warn User** → `POST /social/moderation/warn` · **Ban User** → `/ban` · **Unban** → `DELETE /social/moderation/ban/{userId}`.
- **Stack:** **Area** `Admin` · **Cache** `NoStore` · This logical page spans **three shipped controllers / sidebar items**, each `[Authorize]` + class `[RequirePermission]`:
  - **Moderation** (`ModerationController`, class `ContentModerationLog.Read`) — `/admin/moderation` (SSR); POST `/admin/moderation/warn` (`AdminModerationQueue.Warn`), `/ban` (`AdminModerationQueue.Ban`), `/unban` (**reuses `AdminModerationQueue.Ban`** — no separate Unban perm).
  - **Reports** (`ReportsController`, class `AdminModerationQueue.Read`) — `/admin/reports` (SSR); POST `/admin/reports/{id}/resolve` (`AdminModerationQueue.Resolve`).
  - **Flagged reviews** (`FlaggedReviewsController`, class `AdminModerationQueue.Read`) — `/admin/flagged-reviews` (SSR); POST `/admin/flagged-reviews/{id}/approve` (`AdminModerationQueue.Approve`) and `/remove` (`AdminModerationQueue.Remove`) — **both carry `rowVersion` (ST1)** → `409 [Reload]` on conflict.
  - **Rules** `D1` paged queues, `F8` confirm modal on Ban/Remove (destructive), `ST1` RowVersion on flagged-review approve/remove, `RT5` admin SignalR moderation badge, `A11Y5` status = colour+icon+text, `NF1` toast on inline action, `SEC7`.

### 8.3 Providers
- `GET /admin/providers` · `GET /admin/providers/{id}` `SSR`
- `POST /admin/providers/{id}/approve` · `/reject` · `/request-docs` · `/suspend` · `/reinstate` `AJAX`
- **Buttons:** **Approve** → `POST /admin/providers/{id}/approve` · **Reject** → `/reject` · **Request Docs** → `/request-docs` · **Suspend** → `/suspend` · **Reinstate** → `/reinstate`.
- **Stack:** **Area** `Admin` · **Route** `/admin/providers` + `/admin/providers/{id}` · **Cache** `NoStore` · **Perm** `[Authorize]` + `WebPermission.AdminProviderQueue.Read` (class); actions `.Approve`/`.Reject`/`.RequestDocs`/`.Suspend`/`.Reinstate` (shipped) · **Rules** `R2` SSR list+detail, `D1` paging, `F8` confirm Suspend/Reject, status badges (`A11Y5`), `SEC7`. *(BFF: Request Docs = `POST /admin/providers/{id}/request-documents`.)*

### 8.4 Tours Review
- `GET /tours/admin?status=` `SSR`
- `POST /tours/admin/{id}/approve` · `/reject` · `/suspend` · `/reinstate` `AJAX`
- `PATCH /tours/admin/{id}/feature` `AJAX`
- Proposals queue: `GET /tours/proposals` · `POST /tours/proposals/{id}/approve` · `/reject` `AJAX`
- Packages: `POST /tours/packages/{id}/approve` · `/reject` `AJAX`
- Offerings: `POST /tours/{tourId}/guide-offerings/{guideId}/suspend` · `/reinstate` `AJAX`
- **Buttons:** **Approve** → `POST /tours/admin/{id}/approve` · **Reject** → `/reject` · **Suspend** → `/suspend` · **Reinstate** → `/reinstate` · **Feature** → `PATCH /tours/admin/{id}/feature` · **Approve/Reject Proposal** → `POST /tours/proposals/{id}/approve|reject` · **Approve/Reject Package** → `POST /tours/packages/{id}/approve|reject` · **Suspend/Reinstate Offering** → `POST /tours/{tourId}/guide-offerings/{guideId}/suspend|reinstate`.
- **Stack:** **Area** `Admin` · **Route** `/admin/tours` + `/admin/tours/{id}` (Details) · **Cache** `NoStore` · **Perm** `[Authorize]` + `WebPermission.Tour.ReadAny` (class); actions `Tour.Approve`/`.Reject`/`.Suspend`/`.Reinstate`/`.Feature`, proposals `TourProposal.Approve`/`.Reject`, packages `Package.Approve`/`.Reject`, offerings `GuideOffering.Suspend`/`.Reinstate` (shipped `ToursController`) · **Rules** `R2` SSR, `D1` paged + status filter (preserve filter across page links), `F8` confirm Suspend/Reject, `C3` evict `tour:{id}` after approve/feature, row → §2.5 preview, `SEC7`. *(BFF routes are page-scoped POST: `/admin/tours/{id}/approve|reject|suspend|reinstate|feature`, `/admin/tours/proposals/{id}/approve|reject`, `/admin/tours/packages/{id}/approve|reject`, `/admin/tours/{tourId}/guide-offerings/{guideId}/suspend|reinstate`. No rowVersion on `ToursController`.)*

#### 8.4a Tour approvals (Trips) — RowVersion-guarded lookup/moderate
> This is the **sidebar "Tour approvals"** entry (`TripsController`) — a *second*, lookup-by-id tour-moderation screen parallel to §8.4's list-moderation. Its privileged detail read carries the **`RowVersion` concurrency token (ST1)**, so all four mutations thread `rowVersion` and a stale token returns `409` → `[Reload]`.
- **Buttons:** **Approve** → `POST /admin/trips/{id}/approve` · **Reject** → `/reject` · **Suspend** → `/suspend` · **Reinstate** → `/reinstate` (each posts the hidden `rowVersion`).
- **Stack:** **Area** `Admin` · **Route** `/admin/trips` (+ `/admin/trips/{id}` lookup) · **Cache** `NoStore` · **Perm** `[Authorize]` + **`WebPermission.Tour.Approve` (class)** — note this controller's class read-gate is `Tour.Approve`, not `Tour.ReadAny`; actions `Tour.Approve`/`.Reject`/`.Suspend`/`.Reinstate` (shipped `TripsController`) · **Rules** `R2` SSR, `ST1` RowVersion round-trip on every write (→ `409 [Reload]` on conflict), `F8` confirm Suspend/Reject, `C3` evict `tour:{id}`, `SEC7`. A supplementary approved-tours browse table is display-only.

#### 8.4b Guide applications
> Sidebar "Guide applications" entry (`GuideApplicationsController`) — the guide-application approval queue, distinct from §8.4's guide-*offering* suspend/reinstate.
- **Buttons:** **Approve** → `POST /admin/guide-applications/{tourId}/{id}/approve` · **Reject** → `/reject`.
- **Stack:** **Area** `Admin` · **Route** `/admin/guide-applications` (filters: `tourId?`, `status`, `page`) · **Cache** `NoStore` · **Perm** `[Authorize]` + `WebPermission.GuideApplication.Read` (class); actions `GuideApplication.Approve`/`.Reject` (shipped) · **Rules** `R2` SSR, `D1` paged, `F8` confirm Reject, `SEC7`.

### 8.5 Places & Businesses
- Places: `GET / POST / PUT / DELETE /places[/{id}]` `SSR`/`AJAX`
- `PATCH /places/{id}/feature` · `/verify` `AJAX`
- `PUT /places/{id}/accessibility` · `DELETE /places/admin/accessibility/{assignmentId}` `AJAX`
- `GET /places/accessibility/catalog` `AJAX`
- Business moderation: `POST /places/businesses/admin/{id}/approve` · `/reject` · `/suspend` · `/reinstate` · `/request-more-docs` `AJAX`
- `DELETE /places/businesses/{id}` `AJAX`
- **Buttons:** **New/Save/Delete Place** → `POST/PUT/DELETE /places[/{id}]` · **Feature** → `PATCH /places/{id}/feature` · **Verify** → `/verify` · **Set/Remove Accessibility** → `PUT /places/{id}/accessibility` / `DELETE /places/admin/accessibility/{assignmentId}` · **Approve/Reject/Suspend/Reinstate/Request-More-Docs Business** → `POST /places/businesses/admin/{id}/approve|reject|suspend|reinstate|request-more-docs` · **Delete Business** → `DELETE /places/businesses/{id}`.
- **Stack:** **Area** `Admin` · **Route** `/admin/places` (+ `/admin/businesses` moderation) · **Cache** `NoStore` · **Perm** `[Authorize]` + Places `WebPermission.Place.Read` (class) / `Place.Create`/`Update`/`DeleteOwn` + Businesses `Business.Read` (class) / `Business.Approve`/`Reject`/`RequestDocs`/`Suspend`/`Reinstate`/`Delete` (shipped) · **Rules** `R2`+`D1`, `F8` confirm Delete (destructive), `C3` evict `place:{id}` / `business:{id}` after feature/verify/moderation, `MAP3` single map instance for map ops, `SEC7`. *(BFF: `POST /admin/places/{id}/create|edit|delete|feature|verify`, `POST /admin/businesses/{id}/approve|…|delete`.)*

### 8.6 Blogs & Creators
- `GET /blogs/admin/queue[/{id}]` · `POST .../{id}/approve` · `/reject` · `/remove` `AJAX`
- `POST /blogs/{id}/feature` · `/unfeature` · `/hide` · `/unhide` `AJAX`
- `GET /blogs/admin/deleted` · `POST /blogs/{id}/restore` `AJAX`
- Translations (per content-core)
- Creator apps + profiles: `promote` · `demote` · `suspend` · `reinstate`
- `POST /blogs/admin/creators/invitations` `AJAX`
- **Buttons:** **Approve/Reject/Remove Blog** → `POST /blogs/admin/{id}/approve|reject|remove` · **Feature/Unfeature** → `POST /blogs/{id}/feature|unfeature` · **Hide/Unhide** → `/hide|unhide` · **Restore** → `POST /blogs/{id}/restore` · **Save Translation** → `PUT /blogs/admin/{id}/translations/{languageCode}` · **Approve/Reject/Request-Info Creator App** → `POST /blogs/admin/creators/applications/{id}/approve|reject|request-more-info` · **Promote/Demote/Suspend/Reinstate Creator** → `POST /blogs/admin/creators/profiles/{profileId}/promote|demote|suspend|reinstate` · **Delete Creator Profile** → `DELETE /blogs/admin/creators/profiles/{id}` · **Edit Creator Profile** → `PUT /blogs/admin/creators/profiles/{id}` · **Send Invitation** → `POST /blogs/admin/creators/invitations`.
- **Stack:** **Area** `Admin` · **Route** `/admin/blogs` (+ `/admin/creators`) · **Cache** `NoStore` · **Perm** `[Authorize]` + Blogs `WebPermission.Blog.Read` (class) / `Blog.Create` (create) / `Blog.Update` (edit) / `Blog.DeleteOwn` (delete + **restore**) / `Blog.Approve` (publish/unpublish/archive/approve/**hide**/**unhide**) / `Blog.Reject` / `Blog.Remove` / `Blog.Feature` / `Blog.Unfeature` / **`BlogTourLink.Create`** (link tour) / **`BlogTourLink.Delete`** (unlink tour) + Creators `AdminCreatorQueue.Read` (class) / `.Approve`/`.Reject`/`.RequestMoreInfo`/`.Suspend`/`.Reinstate`/`.PromoteTier`/`.DemoteTier`/`.Update`/`.Delete`/`.Invite` (shipped). *(No `AdminBlogQueue.Read` constant is used by `BlogsController` — the class gate is `Blog.Read`.)* · **Actions (code-verified `BlogsController`):** `/publish`,`/unpublish`,`/archive`,`/approve`,`/reject`,`/hide`,`/unhide`,`/feature`,`/unfeature`,`/remove`,`/delete`,`/restore` (**`restore` carries `rowVersion` — ST1**), `/tours` (link), `/tours/{tourId}/unlink`. · **Rules** `R2`+`D1` queues, `F8` confirm Remove/Delete, `ST1` RowVersion on blog restore, `C3` evict `blog:{id}`/`homepage` after feature/hide, `SEC3` sanitized HTML preview is **View-layer** (no `@Html.Raw`/sanitizer in the controller; enforce in the `.cshtml` with a `// SANITIZED:` comment), `SEC7`.
  - **Blog translations** (`BlogTranslationsController`, class `Blog.Update` — single class gate, no per-action perms): `/admin/blogs/{id}/translations` (Index, SSR), `/admin/blogs/{id}/translations/{languageCode}/edit` (GET), `POST /admin/blogs/{id}/translations/{languageCode}` (Save). This is the shipped BFF shape (the API-layer verb is `PUT /blogs/admin/{id}/translations/{languageCode}`).

### 8.7 Finance Ops
- `GET /finance/admin/dashboard` `SSR`
- Commissions CRUD `AJAX`
- `GET /payments/admin/all` · `POST /payments/{id}/refund` `AJAX`
- `GET /payouts/admin/pending` · `POST /payouts/{id}/approve` · `POST /payouts/admin/trigger` `AJAX`
- `POST /provider-payment-methods/{id}/verify` `AJAX`
- `GET /disputes/admin/open` · `POST /disputes/{id}/review` · `/escalate` · `/resolve` `AJAX`
- `POST /booking/admin/{id}/dispute/resolve` · `POST /admin/bookings/{id}/force-refund` · `GET /booking/admin/all` `AJAX`
- **Buttons:** **Refund** → `POST /payments/{id}/refund` · **Approve Payout** → `POST /payouts/{id}/approve` · **Trigger Sweep** → `POST /payouts/admin/trigger` · **Verify Method** → `POST /provider-payment-methods/{id}/verify` · **Review/Escalate/Resolve Dispute** → `POST /disputes/{id}/review|escalate|resolve` · **Resolve Booking Dispute** → `POST /booking/admin/{id}/dispute/resolve` · **Force Refund** → `POST /admin/bookings/{id}/force-refund` · **New/Save/Delete Commission** → `POST /commissions` / `PUT|DELETE /commissions/{id}`.
- **Stack:** **Area** `Admin` · **Cache** `NoStore` · This logical page spans **five shipped controllers / sidebar items**, each `[Authorize]` + class `[RequirePermission]` (all SSR, all POSTs anti-forgery + PRG redirect, none carry rowVersion or re-render on failure):
  - **Payments / Earnings** (`PaymentsController`, class `AdminFinanceDashboard.Read`) — `/admin/finance`; POST `/admin/finance/{id}/refund` (`Refund.Create`).
  - **Payouts** (`PayoutsController`, class `Payout.Read`) — `/admin/payouts`; POST `/admin/payouts/trigger` (`Payout.Trigger`), `/admin/payouts/{id}/approve` (`Payout.Approve`).
  - **Commissions** (`CommissionsController`, class `CommissionRule.Read`) — `/admin/commissions`; POST `/admin/commissions` (`CommissionRule.Create`), `/admin/commissions/{id}/update` (`CommissionRule.Update`), `/admin/commissions/{id}/delete` (`CommissionRule.Delete`).
  - **Disputes** (`DisputesController`, class `AdminFinanceDashboard.Read`) — `/admin/disputes`; POST `/admin/disputes/{id}/review` (**`AdminFinanceDashboard.Update`**), `/resolve` (**`AdminFinanceDashboard.Approve`**), `/escalate` (**`AdminFinanceDashboard.Update`**). *(Correction: dispute actions use `AdminFinanceDashboard.*`, not `BookingDispute.Resolve`.)*
  - **Bookings (booking finance)** (`BookingsController`, class `AdminBookingDashboard.Read`) — `/admin/bookings`, `/admin/bookings/{id}`; POST `/admin/bookings/{id}/force-refund` (`AdminBookingDashboard.Update`), `/admin/bookings/{id}/resolve-dispute` (**`BookingDispute.Resolve`** — with a runtime secondary gate: when `issueRefund=true` it also requires `Refund.Create`).
  - **Rules** `R6` DataTables server-side for >500-row payment/payout tables, `D1` paging, `F8` confirm Refund/Force-Refund (irreversible money), `PAY2` idempotency key on refund is enforced at the **API layer** (not in the BFF controller), `CON3` JOD 3-dp, `SEC7`.

### 8.8 Growth & Merchandising
- `GET /analytics/admin/metrics` `SSR`
- Batches, boosts/cpc, pins, seasonality, experiments, `holidays/{year}`, segments `AJAX`
- `PUT /analytics/admin/entities/{kind}/{entityId}/photogenic` `AJAX`
- **Buttons:** **Refresh Batch** → `POST /analytics/admin/batches/refresh` · **Add/Remove Boost** → `POST /analytics/admin/boosts` (+ `/boosts/cpc`) / `DELETE .../boosts/{boostId}` · **Add/Remove Pin** → `POST /analytics/admin/pins` / `DELETE .../pins/{pinId}` · **Add/Remove Seasonality** → `POST /analytics/admin/seasonality` / `DELETE .../{ruleId}` · **New Experiment / Start / Complete** → `POST /analytics/admin/experiments` / `PUT .../{experimentId}/start|complete` · **Add Holiday** → `POST /analytics/admin/holidays` · **Toggle Photogenic** → `PUT /analytics/admin/entities/{kind}/{entityId}/photogenic`.
- **Stack:** **Area** `Admin` · **Cache** `NoStore` · This logical page spans **two shipped controllers / sidebar items** (both `[Authorize]` + class `WebPermission.Batch.Read`, SSR, POSTs anti-forgery+PRG):
  - **Growth tools** (`GrowthController`) — `/admin/growth` (Index), `/admin/growth/segments` (`Batch.Read`); POST `/admin/growth/seasonality` (`SeasonalityRule.Create`), `/admin/growth/seasonality/{ruleId}/deactivate` (`SeasonalityRule.Delete`), `/admin/growth/holidays` (`HolidayCalendar.Create`), `/admin/growth/photogenic` (`Photogenic.Update` — BFF route, *not* the API verb `PUT /analytics/admin/entities/{kind}/{entityId}/photogenic`), `/admin/growth/experiments` (`Experiment.Create`), `/admin/growth/experiments/{experimentId}/start` (`Experiment.Update`), `/admin/growth/experiments/{experimentId}/complete` (`Experiment.Update`). *(Correction: `GrowthController` does **not** own BoostPackage/EditorialPin/Batch.Refresh — those live in `RecommendationsController` below.)*
  - **Recommendations** (`RecommendationsController`) — `/admin/recommendations` (Index); POST `/admin/recommendations/batches/refresh` (`Batch.Refresh`), `/admin/recommendations/boosts` (`BoostPackage.Create`), `/admin/recommendations/boosts/deactivate` (`BoostPackage.Delete`), `/admin/recommendations/pins` (`EditorialPin.Create`), `/admin/recommendations/pins/deactivate` (`EditorialPin.Delete`).
  - **Rules** `R2`+`D1`, ApexCharts metrics (admin-only, OK), `C3` evict target `tour:{id}`/`place:{id}`/`homepage` after boost/pin/photogenic, `NF1` toast, `SEC7`.

### 8.9 Content Operations
> Tabs — **Categories / Tags / Languages / Specializations / Translations / Attachments**
- `content-core/*` admin CRUD + `reorder` + `activate | deactivate | restore` `AJAX`
- Translations: `translate | batch | approve | backfill` `AJAX`
- `entity-categories` · `entity-tags` `AJAX`
- **Buttons:** (per tab) **New/Save/Delete** category|tag|language|specialization → `POST/PUT/DELETE /content-core/{kind}[/{id}]` · **Reorder** → `PUT /content-core/categories/reorder` · **Activate/Deactivate/Restore** → `PATCH .../{id}/activate|deactivate|restore` · **Translate / Batch / Approve / Backfill** → `POST /content-core/translations/translate|batch` · `POST .../{id}/approve` · `/approve-batch` · `/backfill/{entityKind}` · **Save Translation** → `PUT /content-core/translations/{id}` · **Assign/Unassign** → `POST/DELETE /content-core/entity-categories|entity-tags`.
- **Stack:** **Area** `Admin` *(the planned `Content` area was consolidated into `Admin` in code)* · **Cache** `NoStore` · This logical page is **eight shipped controllers** (each `[Authorize]` + own class `[RequirePermission]`, all SSR): `Categories` (`Category.Read`) · `Tags` (`Tag.Read`) · `Languages` (`Language.Read`) · `Specializations` (`Specialization.Read`) · `Translations` (`TranslationCache.Read`) · `Attachments` (`Attachment.Read`) · **`EntityCategories`** (`EntityCategory.Read` — distinct assignment controller) · **`EntityTags`** (`EntityTag.Read` — distinct assignment controller). · **Routes** `/admin/categories` · `/admin/tags` · `/admin/languages` · `/admin/specializations` · `/admin/translations` · `/admin/attachments` · `/admin/entity-categories` · `/admin/entity-tags` · **Per-action perms:** `Category`/`Tag`/`Language`/`Specialization` `.Create`/`.Update`/`.Delete`, `TranslationCache.Create`/`.Update`, `EntityCategory.*`/`EntityTag.*`, `Attachment.*` · **Rules** lazy non-default tabs, drag **Reorder** then `PUT .../reorder`, `C3` evict `category:tree`/`lookups` (IMemoryCache categories 1h / languages 24h) after any write, `D1` paging, RTL-correct translation editor, `SEC7`. *(BFF: `POST /admin/categories/create|{id}/edit|reorder`, `POST /admin/translations/on-demand|{id}/edit|{id}/approve|backfill|approve-batch`, `POST /admin/entity-categories/assign|remove`, `POST /admin/entity-tags/assign|remove`, `POST /admin/attachments/upload|{id}/delete|{id}/set-primary`.)*

### 8.10 SEO Console
> Tabs — **Metadata / FAQ / Redirects / Sitemap / WeatherCache** · entityType picker bound to the **6-value `SeoEntityType` {Place, Tour, Business, Blog, TourGuide, Creator}**
- `GET / POST / PUT / DELETE /seo/metadata` `AJAX`
- `GET / POST / PUT / DELETE /seo/faq` + `reorder` `AJAX`
- `GET / POST / PUT / DELETE /seo/redirects` `AJAX`
- `GET /seo/sitemap/entries` · `PATCH /seo/sitemap/entries/{id}` · `DELETE /seo/sitemap/entries/{id}` · `POST /seo/sitemap/regenerate` `AJAX`
- Weather cache ops `AJAX`
- **Buttons:** **New/Save/Delete Metadata** → `POST /seo/metadata` / `PUT|DELETE /seo/metadata/{id}` · **New/Save/Delete FAQ** → `POST /seo/faq` / `PUT|DELETE /seo/faq/{id}` · **Reorder FAQ** → `PUT /seo/faq/reorder` · **New/Save/Delete Redirect** → `POST /seo/redirects` / `PUT|DELETE /seo/redirects/{id}` · **Edit/Delete Sitemap Entry** → `PATCH|DELETE /seo/sitemap/entries/{id}` · **Regenerate Sitemap** → `POST /seo/sitemap/regenerate` · **Clear Weather Cache** → `DELETE /seo/weather/cache/{id}` · **Reset Budget** → `PUT /seo/weather/budget/reset` · **Refresh Weather** → `POST /seo/weather/refresh/{placeId}`. *(entityType selector limited to the 6 `SeoEntityType` values.)*
- **Stack:** **Area** `Admin` *(consolidated from the planned `Content` area)* · **Route** `/admin/seo/metadata` · `/admin/seo/faq` · `/admin/seo/redirects` · `/admin/seo/sitemap` · `/admin/seo/weather` (shipped controllers) · **Cache** `NoStore` · **Perm** `[Authorize]` + per-tab: `SeoMetadata.Read`/`Create`/`Delete`, `FaqItem.Read`/`Create`/`Update`/`Delete`, `Redirect.*`, `Sitemap.Read`/`Update`/`Delete`/`Refresh`, `Weather.Read`/`Refresh`/`Update`/`Delete` (shipped) · **Rules** entityType picker hard-bound to the 6 `SeoEntityType` values, FAQ drag-reorder → `PUT /seo/faq/reorder`, edits drive §3 SEO head/`C5` ETag on the public detail page, **Save** → §2.x preview, `D1`, `SEC7`. *(BFF: `POST /admin/seo/metadata/save|{id}/delete`, `POST /admin/seo/weather/refresh|purge|reset-budget`.)*

### 8.11 Support
- `GET /support/tickets[/{id}]` `SSR`
- `POST /support/admin/tickets/{id}/assign` · `/resolve` `AJAX`
- `POST /support/tickets/{id}/messages` · `/close` `AJAX`
- **Buttons:** **Assign** → `POST /support/admin/tickets/{id}/assign` · **Resolve** → `/resolve` · **Reply** → `POST /support/tickets/{id}/messages` · **Close** → `/close`.
- **Stack:** **Area** `Admin` · **Route** `/admin/support` + `/admin/support/{id}` · **Cache** `NoStore` · **Perm** `[Authorize]` + `WebPermission.SupportTicket.Read` (class) / `SupportTicket.Close` (close) + `AdminSupportQueue.Assign`/`Resolve` (shipped) · **Rules** `R2` SSR ticket list+thread, `D1` paging, `NF1` toast on assign/resolve, `L6` empty-state, `SEC7`.

### 8.12 Platform Ops
- `GET /ops/outbox/dead-letters` · `POST /ops/outbox/dead-letters/{module}/{id}/replay` `AJAX`
- `POST /ops/content-tours/backfill/tour-snapshots` `AJAX`
- Notification templates: `GET` (list) · `POST` · `PUT/{id}` · `DELETE/{id}` — **no GET-by-id** `AJAX`
- **Buttons:** **Replay** → `POST /ops/outbox/dead-letters/{module}/{id}/replay` · **Backfill Snapshots** → `POST /ops/content-tours/backfill/tour-snapshots` · **New/Save/Delete Template** → `POST /admin/notification-templates` / `PUT|DELETE /admin/notification-templates/{id}` *(edit opens from list row — no GET-by-id)*.
- **Stack:** **Area** `Admin` · **Route** `/admin/ops` (+ notification templates) · **Cache** `NoStore` · **Perm** `[Authorize]` + `WebPermission.Outbox.Read` (class) / `Outbox.Replay` (replay/backfill) + `NotificationTemplate.Read`/`Create`/`Update`/`Delete` (shipped) · **Rules** `R6` DataTables for dead-letter list, `F8` confirm Replay/Backfill (side-effecting), template edit hydrated from the list row (no GET-by-id), `ERR4` correlation id in detail, `SEC7`.

### 8.13 Users & Audit
- `GET /admin/audit-logs[/export]` · `POST /admin/audit-logs/{id}/redact` `SSR`/`AJAX`
- `GET /admin/interactions[/user/{userId}]` `AJAX`
- `PATCH /auth/admin/users/{userId}/suspend` · `/reactivate` · `/archive` `AJAX`
- `POST /auth/admin/users/{userId}/reassign` · `/reset-password` · `DELETE .../sessions` `AJAX`
- Invitations: `GET /auth/invitations/roles` · `POST /auth/invitations[/accept | /resend]` `AJAX`
- **Buttons:** **Suspend/Reactivate/Archive** → `PATCH /auth/admin/users/{userId}/suspend|reactivate|archive` · **Reassign** → `POST .../reassign` · **Reset Password** → `/reset-password` · **Revoke Sessions** → `DELETE /auth/admin/users/{userId}/sessions` · **Redact Audit** → `POST /admin/audit-logs/{id}/redact` · **Export Audit** → `GET /admin/audit-logs/export` (nav/download) · **Invite User** → `POST /auth/invitations` · **Resend Invite** → `/invitations/resend`.
- **Stack:** **Area** `Admin` · **Cache** `NoStore` · This logical page spans **five shipped controllers / sidebar items**, each `[Authorize]` + class `[RequirePermission]`:
  - **Users** (`UsersController`, class `User.Read`) — `/admin/users`; lifecycle writes use `User.UpdateAny`; role assignment uses `UserRole.Create`/`UserRole.Delete`.
  - **Lifecycle** (`LifecycleController`, class `User.UpdateAny`) — **redirect-only, no views** (suspend/reactivate/archive/reassign/reset-password/revoke-sessions dispatch).
  - **Roles** (`RolesController`, class `Role.Read`) — `/admin/roles`.
  - **Invitations** (`InvitationsController`, class `User.Create`) — `/admin/invitations` (distinct shipped surface — invite/resend/accept).
  - **Activity & audit** (`AuditLogsController`, class **`System.Read`**) — `/admin/audit`; actions `AuditLog.Redact` (redact) / `AuditLog.Export` (export).
  - **Rules** `R6` DataTables + `IAsyncEnumerable` export when >10K rows, `D1` paging, `F8` confirm Suspend/Archive/Reset-Password/Revoke (destructive) · staleness via API state-precondition guards (the user-lifecycle endpoints are **not** in §10's RowVersion-exposing set — no `ST1` token round-trip) · `SEC7`.

### 8.14 Tour Guides
- `GET /guides/admin/{guideId}` (full profile + private stats) `SSR`
- `PUT /guides/admin/{guideId}` · `DELETE /guides/admin/{guideId}` `AJAX`
- `POST /guides/admin/{guideId}/suspend` · `/reinstate` `AJAX`
- **Buttons:** **Edit Guide** → `PUT /guides/admin/{guideId}` · **Suspend** → `POST .../suspend` · **Reinstate** → `POST .../reinstate` · **Delete Guide** → `DELETE /guides/admin/{guideId}` *(confirm — 60-day hard delete)*.
- **Stack:** **Area** `Admin` · **Route** `/admin/guides` + `/admin/guides/{id}` · **Cache** `NoStore` · **Perm** `[Authorize]` + `WebPermission.TourGuideProfile.Read` (class) / `TourGuideProfile.Suspend`/`Reinstate`/`Update`/`DeleteAny` (shipped `GuidesController`) · **Rules** `R2` SSR profile+private stats, `D1` paged list, `F8` confirm Delete (60-day hard delete) + Suspend, status badges (`A11Y5`), `SEC7`.
