# Admin Dashboard — Area

**Actor:** Admin · **Plan section:** §8 · **Namespaces:** `/admin/*`, `*/admin/*`, `/analytics/admin/*`, `/seo/*`, `/content-core/*` (admin), `/finance/admin/*`, `/ops/*`

> **Source of truth:** YallaJo code (`yallajo-endpoints.txt`, 549 endpoints). All routes are `/api/v1`-prefixed.
> **Template family:** Webestica `admin-*` pages cover the overview, moderation, providers (agents), finance lists and users; the rest of the admin consoles have **no template page** and are built by you.
> **Architecture & rules:** see [`0-architecture-and-rules.md`](0-architecture-and-rules.md) — four-tier pipeline (Controller → Facade → ApiClient → IApiClient), code areas, caching, permissions.
> **Code area:** mostly **`Admin`** (`/admin/*`); **§8.9 Content Operations** + **§8.10 SEO Console** live in the **`Content`** area (`/content/*`). Every page is **`NoStore`** (UI-PERF-C2), policy **`Admin`** + a `WebPermission.{Feature}.{Action}` constant; every write is anti-forgery protected (SEC7) and uses PRG.

**Status legend:** ✅ Wire · ♻️ Repurpose · ⏭️ Skip · 🟥 USER builds (no template page)

**Shared shell:** top bar = global search + language switcher + notification bell (`GET /notifications/unread-count` `AJAX⟳`) + avatar menu (`GET /accounts/profile`). Nav driver = `GET /security/me`.
**Load tags:** `SSR` / `AJAX` / `AJAX⟳` / `AJAX↑`. Status is first-class UI (badge + action menu); inline mutations return the updated row + toast.

---

## Pages this area should have

| # | Page | Template | Redirects to |
|---|------|----------|--------------|
| 8.1 | Overview | `admin-dashboard.html` ✅ | KPI cards → §8.7 Finance / §8.13 Users / §8.4 Tours |
| 8.2 | Moderation | `admin-reviews.html` ✅ | resolve/approve/remove inline · reported entity |
| 8.3 | Providers | `admin-agent-list.html` ✅ + `admin-agent-detail.html` ✅ | row → provider detail · lifecycle inline |
| 8.4 | Tours Review | 🟥 USER builds | row → §2.5 Tour preview · approve/reject inline |
| 8.5 | Places & Businesses | 🟥 USER builds | row → §2.3 / §2.4 preview · moderation inline |
| 8.6 | Blogs & Creators | 🟥 USER builds | row → §2.9 post/creator · feature/hide inline |
| 8.7 | Finance Ops | `admin-booking-list.html` ♻️ + `admin-booking-detail.html` ♻️ + `admin-earnings.html` ✅ | booking detail · refund inline · payout detail |
| 8.8 | Growth & Merchandising | 🟥 USER builds | targeted entity → §2.5 / §2.3 |
| 8.9 | Content Operations | 🟥 USER builds (tabbed CRUD) | inline edits |
| 8.10 | SEO Console | 🟥 USER builds (entityType picker) | §2.x detail preview · inline |
| 8.11 | Support | 🟥 USER builds (reuse help-center) | ticket thread |
| 8.12 | Platform Ops | 🟥 USER builds | dead-letter detail inline |
| 8.13 | Users & Audit | `admin-guest-list.html` ✅ + `admin-guest-detail.html` ✅ + `admin-settings.html` ♻️ | guest detail · lifecycle inline · audit export |
| 8.14 | Tour Guides | `admin-agent-detail.html` ♻️ + `admin-agent-list.html` ♻️ | row → §2.7 Guide detail · lifecycle inline · offerings → §8.4 |

---

## Endpoints by page

### 8.1 Overview
- `GET /admin/dashboard` `[/bookings | /revenue | /users]` `SSR`
- `GET /trending` · `GET /popular/*` `AJAX`
- **Buttons:** read-only KPIs; **drill-in** cards → §8.7 / §8.13 / §8.4 (nav). *(No write-endpoints.)*
- **Stack:** **Area** `Admin` · **Route** `/admin/dashboard` · **Cache** `NoStore` · **Perm** `Admin` + `WebPermission.Admin.Read` · **Rules** `R2` SSR first paint, `API1` Task.WhenAll for the independent KPI calls, ApexCharts via `dashboard.bundle.js` (allowed — not a public page), read-only so no SEC7.

### 8.2 Moderation
- `GET /social/reports/admin` · `POST /social/reports/admin/{id}/resolve` `AJAX`
- `GET /social/reviews/admin/flagged` · `POST .../{id}/approve` · `/remove` `AJAX`
- `GET /social/moderation/logs` `AJAX`
- `POST /social/moderation/warn` · `/ban` · `DELETE /social/moderation/ban/{userId}` `AJAX`
- **Buttons:** **Resolve Report** → `POST /social/reports/admin/{id}/resolve` · **Approve Review** → `POST /social/reviews/admin/{id}/approve` · **Remove Review** → `/remove` · **Warn User** → `POST /social/moderation/warn` · **Ban User** → `/ban` · **Unban** → `DELETE /social/moderation/ban/{userId}`.
- **Stack:** **Area** `Admin` · **Route** `/admin/moderation` · **Cache** `NoStore` · **Perm** `Admin` + `WebPermission.Moderation.Manage` · **Rules** `D1` paged queues, `F8` confirm modal on Ban/Remove (destructive), `RT5` admin SignalR moderation badge, `A11Y5` status = colour+icon+text, `NF1` toast on inline action, `SEC7`.

### 8.3 Providers
- `GET /admin/providers` · `GET /admin/providers/{id}` `SSR`
- `POST /admin/providers/{id}/approve` · `/reject` · `/request-docs` · `/suspend` · `/reinstate` `AJAX`
- **Buttons:** **Approve** → `POST /admin/providers/{id}/approve` · **Reject** → `/reject` · **Request Docs** → `/request-docs` · **Suspend** → `/suspend` · **Reinstate** → `/reinstate`.
- **Stack:** **Area** `Admin` · **Route** `/admin/providers` + `/admin/providers/{id}` · **Cache** `NoStore` · **Perm** `Admin` + `WebPermission.Providers.Manage` · **Rules** `R2` SSR list+detail, `D1` paging, `F8` confirm Suspend/Reject, status badges (`A11Y5`), `SEC7`.

### 8.4 Tours Review
- `GET /tours/admin?status=` `SSR`
- `POST /tours/admin/{id}/approve` · `/reject` · `/suspend` · `/reinstate` `AJAX`
- `PATCH /tours/admin/{id}/feature` `AJAX`
- Proposals queue: `GET /tours/proposals` · `POST /tours/proposals/{id}/approve` · `/reject` `AJAX`
- Packages: `POST /tours/packages/{id}/approve` · `/reject` `AJAX`
- Offerings: `POST /tours/{tourId}/guide-offerings/{guideId}/suspend` · `/reinstate` `AJAX`
- **Buttons:** **Approve** → `POST /tours/admin/{id}/approve` · **Reject** → `/reject` · **Suspend** → `/suspend` · **Reinstate** → `/reinstate` · **Feature** → `PATCH /tours/admin/{id}/feature` · **Approve/Reject Proposal** → `POST /tours/proposals/{id}/approve|reject` · **Approve/Reject Package** → `POST /tours/packages/{id}/approve|reject` · **Suspend/Reinstate Offering** → `POST /tours/{tourId}/guide-offerings/{guideId}/suspend|reinstate`.
- **Stack:** **Area** `Admin` · **Route** `/admin/tours` · **Cache** `NoStore` · **Perm** `Admin` + `WebPermission.Tours.Moderate` · **Rules** `R2` SSR, `D1` paged + status filter (preserve filter across page links), `F8` confirm Suspend/Reject, `C3` evict `tour:{id}` after approve/feature, row → §2.5 preview, `SEC7`.

### 8.5 Places & Businesses
- Places: `GET / POST / PUT / DELETE /places[/{id}]` `SSR`/`AJAX`
- `PATCH /places/{id}/feature` · `/verify` `AJAX`
- `PUT /places/{id}/accessibility` · `DELETE /places/admin/accessibility/{assignmentId}` `AJAX`
- `GET /places/accessibility/catalog` `AJAX`
- Business moderation: `POST /places/businesses/admin/{id}/approve` · `/reject` · `/suspend` · `/reinstate` · `/request-more-docs` `AJAX`
- `DELETE /places/businesses/{id}` `AJAX`
- **Buttons:** **New/Save/Delete Place** → `POST/PUT/DELETE /places[/{id}]` · **Feature** → `PATCH /places/{id}/feature` · **Verify** → `/verify` · **Set/Remove Accessibility** → `PUT /places/{id}/accessibility` / `DELETE /places/admin/accessibility/{assignmentId}` · **Approve/Reject/Suspend/Reinstate/Request-More-Docs Business** → `POST /places/businesses/admin/{id}/approve|reject|suspend|reinstate|request-more-docs` · **Delete Business** → `DELETE /places/businesses/{id}`.
- **Stack:** **Area** `Admin` · **Route** `/admin/places` (+ `/admin/businesses` moderation) · **Cache** `NoStore` · **Perm** `Admin` + `WebPermission.Places.Manage` · **Rules** `R2`+`D1`, `F8` confirm Delete (destructive), `C3` evict `place:{id}` / `business:{id}` after feature/verify/moderation, `MAP3` single map instance for map ops, `SEC7`.

### 8.6 Blogs & Creators
- `GET /blogs/admin/queue[/{id}]` · `POST .../{id}/approve` · `/reject` · `/remove` `AJAX`
- `POST /blogs/{id}/feature` · `/unfeature` · `/hide` · `/unhide` `AJAX`
- `GET /blogs/admin/deleted` · `POST /blogs/{id}/restore` `AJAX`
- Translations (per content-core)
- Creator apps + profiles: `promote` · `demote` · `suspend` · `reinstate`
- `POST /blogs/admin/creators/invitations` `AJAX`
- **Buttons:** **Approve/Reject/Remove Blog** → `POST /blogs/admin/{id}/approve|reject|remove` · **Feature/Unfeature** → `POST /blogs/{id}/feature|unfeature` · **Hide/Unhide** → `/hide|unhide` · **Restore** → `POST /blogs/{id}/restore` · **Save Translation** → `PUT /blogs/admin/{id}/translations/{languageCode}` · **Approve/Reject/Request-Info Creator App** → `POST /blogs/admin/creators/applications/{id}/approve|reject|request-more-info` · **Promote/Demote/Suspend/Reinstate Creator** → `POST /blogs/admin/creators/profiles/{profileId}/promote|demote|suspend|reinstate` · **Delete Creator Profile** → `DELETE /blogs/admin/creators/profiles/{id}` · **Edit Creator Profile** → `PUT /blogs/admin/creators/profiles/{id}` · **Send Invitation** → `POST /blogs/admin/creators/invitations`.
- **Stack:** **Area** `Admin` · **Route** `/admin/blogs` · **Cache** `NoStore` · **Perm** `Admin` + `WebPermission.Blogs.Moderate` · **Rules** `R2`+`D1` queues, `F8` confirm Remove/Delete, `C3` evict `blog:{id}`/`homepage` after feature/hide, `SEC3` sanitized HTML preview, `SEC7`.

### 8.7 Finance Ops
- `GET /finance/admin/dashboard` `SSR`
- Commissions CRUD `AJAX`
- `GET /payments/admin/all` · `POST /payments/{id}/refund` `AJAX`
- `GET /payouts/admin/pending` · `POST /payouts/{id}/approve` · `POST /payouts/admin/trigger` `AJAX`
- `POST /provider-payment-methods/{id}/verify` `AJAX`
- `GET /disputes/admin/open` · `POST /disputes/{id}/review` · `/escalate` · `/resolve` `AJAX`
- `POST /booking/admin/{id}/dispute/resolve` · `POST /admin/bookings/{id}/force-refund` · `GET /booking/admin/all` `AJAX`
- **Buttons:** **Refund** → `POST /payments/{id}/refund` · **Approve Payout** → `POST /payouts/{id}/approve` · **Trigger Sweep** → `POST /payouts/admin/trigger` · **Verify Method** → `POST /provider-payment-methods/{id}/verify` · **Review/Escalate/Resolve Dispute** → `POST /disputes/{id}/review|escalate|resolve` · **Resolve Booking Dispute** → `POST /booking/admin/{id}/dispute/resolve` · **Force Refund** → `POST /admin/bookings/{id}/force-refund` · **New/Save/Delete Commission** → `POST /commissions` / `PUT|DELETE /commissions/{id}`.
- **Stack:** **Area** `Admin` · **Route** `/admin/finance` · **Cache** `NoStore` · **Perm** `Admin` + `WebPermission.Finance.Manage` · **Rules** `R6` DataTables server-side for >500-row payment/payout tables, `D1` paging, `F8` confirm Refund/Force-Refund (irreversible money), `PAY2` idempotency key on refund, `CON3` JOD 3-dp, `SEC7`.

### 8.8 Growth & Merchandising
- `GET /analytics/admin/metrics` `SSR`
- Batches, boosts/cpc, pins, seasonality, experiments, `holidays/{year}`, segments `AJAX`
- `PUT /analytics/admin/entities/{kind}/{entityId}/photogenic` `AJAX`
- **Buttons:** **Refresh Batch** → `POST /analytics/admin/batches/refresh` · **Add/Remove Boost** → `POST /analytics/admin/boosts` (+ `/boosts/cpc`) / `DELETE .../boosts/{boostId}` · **Add/Remove Pin** → `POST /analytics/admin/pins` / `DELETE .../pins/{pinId}` · **Add/Remove Seasonality** → `POST /analytics/admin/seasonality` / `DELETE .../{ruleId}` · **New Experiment / Start / Complete** → `POST /analytics/admin/experiments` / `PUT .../{experimentId}/start|complete` · **Add Holiday** → `POST /analytics/admin/holidays` · **Toggle Photogenic** → `PUT /analytics/admin/entities/{kind}/{entityId}/photogenic`.
- **Stack:** **Area** `Admin` · **Route** `/admin/growth` · **Cache** `NoStore` · **Perm** `Admin` + `WebPermission.Growth.Manage` · **Rules** `R2`+`D1`, ApexCharts metrics (admin-only, OK), `C3` evict target `tour:{id}`/`place:{id}`/`homepage` after boost/pin/photogenic, `NF1` toast, `SEC7`.

### 8.9 Content Operations
> Tabs — **Categories / Tags / Languages / Specializations / Translations / Attachments**
- `content-core/*` admin CRUD + `reorder` + `activate | deactivate | restore` `AJAX`
- Translations: `translate | batch | approve | backfill` `AJAX`
- `entity-categories` · `entity-tags` `AJAX`
- **Buttons:** (per tab) **New/Save/Delete** category|tag|language|specialization → `POST/PUT/DELETE /content-core/{kind}[/{id}]` · **Reorder** → `PUT /content-core/categories/reorder` · **Activate/Deactivate/Restore** → `PATCH .../{id}/activate|deactivate|restore` · **Translate / Batch / Approve / Backfill** → `POST /content-core/translations/translate|batch` · `POST .../{id}/approve` · `/approve-batch` · `/backfill/{entityKind}` · **Save Translation** → `PUT /content-core/translations/{id}` · **Assign/Unassign** → `POST/DELETE /content-core/entity-categories|entity-tags`.
- **Stack:** **Area** `Content` · **Route** `/content/categories` · `/content/tags` · `/content/languages` · `/content/specializations` · `/content/translations` · `/content/attachments` · **Cache** `NoStore` · **Perm** `Admin` + `WebPermission.Content.Manage` · **Rules** lazy non-default tabs, drag **Reorder** then `PUT .../reorder`, `C3` evict `category:tree`/`lookups` (IMemoryCache categories 1h / languages 24h) after any write, `D1` paging, RTL-correct translation editor, `SEC7`.

### 8.10 SEO Console
> Tabs — **Metadata / FAQ / Redirects / Sitemap / WeatherCache** · entityType picker bound to the **6-value `SeoEntityType` {Place, Tour, Business, Blog, TourGuide, Creator}**
- `GET / POST / PUT / DELETE /seo/metadata` `AJAX`
- `GET / POST / PUT / DELETE /seo/faq` + `reorder` `AJAX`
- `GET / POST / PUT / DELETE /seo/redirects` `AJAX`
- `GET /seo/sitemap/entries` · `PATCH /seo/sitemap/entries/{id}` · `DELETE /seo/sitemap/entries/{id}` · `POST /seo/sitemap/regenerate` `AJAX`
- Weather cache ops `AJAX`
- **Buttons:** **New/Save/Delete Metadata** → `POST /seo/metadata` / `PUT|DELETE /seo/metadata/{id}` · **New/Save/Delete FAQ** → `POST /seo/faq` / `PUT|DELETE /seo/faq/{id}` · **Reorder FAQ** → `PUT /seo/faq/reorder` · **New/Save/Delete Redirect** → `POST /seo/redirects` / `PUT|DELETE /seo/redirects/{id}` · **Edit/Delete Sitemap Entry** → `PATCH|DELETE /seo/sitemap/entries/{id}` · **Regenerate Sitemap** → `POST /seo/sitemap/regenerate` · **Clear Weather Cache** → `DELETE /seo/weather/cache/{id}` · **Reset Budget** → `PUT /seo/weather/budget/reset` · **Refresh Weather** → `POST /seo/weather/refresh/{placeId}`. *(entityType selector limited to the 6 `SeoEntityType` values.)*
- **Stack:** **Area** `Content` · **Route** `/content/seo` (tabs `/content/seo/metadata|faq|redirects|sitemap|weather`) · **Cache** `NoStore` · **Perm** `Admin` + `WebPermission.Seo.Manage` · **Rules** entityType picker hard-bound to the 6 `SeoEntityType` values, FAQ drag-reorder → `PUT /seo/faq/reorder`, edits drive §3 SEO head/`C5` ETag on the public detail page, **Save** → §2.x preview, `D1`, `SEC7`.

### 8.11 Support
- `GET /support/tickets[/{id}]` `SSR`
- `POST /support/admin/tickets/{id}/assign` · `/resolve` `AJAX`
- `POST /support/tickets/{id}/messages` · `/close` `AJAX`
- **Buttons:** **Assign** → `POST /support/admin/tickets/{id}/assign` · **Resolve** → `/resolve` · **Reply** → `POST /support/tickets/{id}/messages` · **Close** → `/close`.
- **Stack:** **Area** `Admin` · **Route** `/admin/support` + `/admin/support/{id}` · **Cache** `NoStore` · **Perm** `Admin` + `WebPermission.Support.Manage` · **Rules** `R2` SSR ticket list+thread, `D1` paging, `NF1` toast on assign/resolve, `L6` empty-state, `SEC7`.

### 8.12 Platform Ops
- `GET /ops/outbox/dead-letters` · `POST /ops/outbox/dead-letters/{module}/{id}/replay` `AJAX`
- `POST /ops/content-tours/backfill/tour-snapshots` `AJAX`
- Notification templates: `GET` (list) · `POST` · `PUT/{id}` · `DELETE/{id}` — **no GET-by-id** `AJAX`
- **Buttons:** **Replay** → `POST /ops/outbox/dead-letters/{module}/{id}/replay` · **Backfill Snapshots** → `POST /ops/content-tours/backfill/tour-snapshots` · **New/Save/Delete Template** → `POST /admin/notification-templates` / `PUT|DELETE /admin/notification-templates/{id}` *(edit opens from list row — no GET-by-id)*.
- **Stack:** **Area** `Admin` · **Route** `/admin/ops` · **Cache** `NoStore` · **Perm** `Admin` + `WebPermission.Ops.Manage` (SuperAdmin-tier) · **Rules** `R6` DataTables for dead-letter list, `F8` confirm Replay/Backfill (side-effecting), template edit hydrated from the list row (no GET-by-id), `ERR4` correlation id in detail, `SEC7`.

### 8.13 Users & Audit
- `GET /admin/audit-logs[/export]` · `POST /admin/audit-logs/{id}/redact` `SSR`/`AJAX`
- `GET /admin/interactions[/user/{userId}]` `AJAX`
- `PATCH /auth/admin/users/{userId}/suspend` · `/reactivate` · `/archive` `AJAX`
- `POST /auth/admin/users/{userId}/reassign` · `/reset-password` · `DELETE .../sessions` `AJAX`
- Invitations: `GET /auth/invitations/roles` · `POST /auth/invitations[/accept | /resend]` `AJAX`
- **Buttons:** **Suspend/Reactivate/Archive** → `PATCH /auth/admin/users/{userId}/suspend|reactivate|archive` · **Reassign** → `POST .../reassign` · **Reset Password** → `/reset-password` · **Revoke Sessions** → `DELETE /auth/admin/users/{userId}/sessions` · **Redact Audit** → `POST /admin/audit-logs/{id}/redact` · **Export Audit** → `GET /admin/audit-logs/export` (nav/download) · **Invite User** → `POST /auth/invitations` · **Resend Invite** → `/invitations/resend`.
- **Stack:** **Area** `Admin` · **Route** `/admin/users` (+ `/admin/audit`) · **Cache** `NoStore` · **Perm** `Admin` + `WebPermission.Users.Manage` · **Rules** `R6` DataTables + `IAsyncEnumerable` export when >10K rows, `D1` paging, `F8` confirm Suspend/Archive/Reset-Password/Revoke (destructive), `ST1` concurrency, `SEC7`.

### 8.14 Tour Guides
- `GET /guides/admin/{guideId}` (full profile + private stats) `SSR`
- `PUT /guides/admin/{guideId}` · `DELETE /guides/admin/{guideId}` `AJAX`
- `POST /guides/admin/{guideId}/suspend` · `/reinstate` `AJAX`
- **Buttons:** **Edit Guide** → `PUT /guides/admin/{guideId}` · **Suspend** → `POST .../suspend` · **Reinstate** → `POST .../reinstate` · **Delete Guide** → `DELETE /guides/admin/{guideId}` *(confirm — 60-day hard delete)*.
- **Stack:** **Area** `Admin` · **Route** `/admin/guides` + `/admin/guides/{id}` · **Cache** `NoStore` · **Perm** `Admin` + `WebPermission.Guides.Manage` · **Rules** `R2` SSR profile+private stats, `D1` paged list, `F8` confirm Delete (60-day hard delete) + Suspend, status badges (`A11Y5`), `SEC7`.
