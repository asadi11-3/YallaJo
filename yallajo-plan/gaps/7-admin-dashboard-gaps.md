# Gap Report — `7-admin-dashboard.md` vs. shipped code

> **Method:** deep code-vs-plan audit against the **shipped** `Areas/Admin/Controllers/*` (43 controllers), their full route surfaces,
> and the **shipped admin sidebar nav (24 items)**. The plan's permissions + the `Content`→`Admin` consolidation were already corrected
> in the earlier §7 audit and are **accurate**; this pass maps the plan's **14 logical pages** onto the **24 shipped nav surfaces** and
> finds undocumented controllers + route-shape divergences.
>
> **Scope:** `yallajo-plan/7-admin-dashboard.md` (§8 admin).
> **Severity:** 🔴 plan contradicts code · 🟠 undocumented surface / mis-grouping · 🟡 cosmetic.
> **Status:** ❌ not in plan · ✏️ differs · ➕ shipped but undocumented · ✅ matches.

---

## 0. Summary

| Topic | Verdict |
|-------|---------|
| Permissions + Content→Admin consolidation | ✅ Accurate (prior §7 audit) — re-verified granular per-action constants across 43 controllers. |
| **Page count** | 🟠 Plan = **14 pages**; shipped = **24 sidebar nav items**. The plan legitimately *groups* some (Finance, SEO), but several surfaces are **undocumented**. |
| **`/admin/trips`** | ➕🔴 A **second tour-moderation controller** (`TripsController`, "Admin Tour-approvals screen", `Tour.Approve/Reject/Suspend/Reinstate`) parallel to `ToursController` — **no plan mention at all.** |
| **GuideApplications** | ➕ Separate nav + controller (`/admin/guide-applications/{tourId}/{id}/approve\|reject`) — folded silently into §8.4. |
| **Moderation split** | 🟠 Three nav items — `Moderation`, `Reports`, `FlaggedReviews` — the plan folds into one §8.2. |
| **Finance split** | 🟠 Five nav items — `Finance`(payments), `Payouts`, `Commissions`, `Disputes`, + booking finance — plan folds into one §8.7. |
| **BlogTranslations** | ➕ Separate controller (`/admin/blogs/{id}/translations/*`) — plan mentions translations only inline in §8.6. |
| **Statistics** | ➕ Separate nav (`/admin/analytics`) — folded into §8.1. |
| BFF route shapes | ✏️ All page-scoped `POST` (e.g. `/admin/blogs/{id}/approve`, `/admin/seo/metadata/save`) — header note already covers this ✓. |

---

## 1. ➕🔴 Undocumented shipped surfaces

### G1 — `/admin/trips` is a second tour-moderation screen (no plan mention)
- **Code (`TripsController`):** *"Admin Tour-approvals screen: look up a tour by id to load its privileged detail (which carries the **RowVersion** concurrency token) and moderate it (approve/reject/suspend/reinstate). A supplementary approved-tours browse table is display-only."*
  - `GET /admin/trips` (lookup/index) · `POST /admin/trips/{id}/approve` (`Tour.Approve`) · `/reject` (`Tour.Reject`) · `/suspend` (`Tour.Suspend`) · `/reinstate` (`Tour.Reinstate`).
- **vs plan:** §8.4 Tours Review describes `/admin/tours/*` only. **There is a parallel `/admin/trips/*` surface** doing the same approve/reject/suspend/reinstate — and notably **it's the one that carries RowVersion** (the privileged detail read). The plan never mentions "Trips."
- **Action:** document `/admin/trips` — either as part of §8.4 (note the two surfaces: `/admin/tours` list-moderation vs `/admin/trips` lookup-by-id RowVersion-guarded moderation) or as its own page. Clarify which is canonical. **This is the headline gap.**

### G2 — Guide Applications is a distinct admin surface
- **Code (`GuideApplicationsController`):** `GET /admin/guide-applications` · `POST /admin/guide-applications/{tourId}/{id}/approve` (`GuideApplication.Approve`) · `/reject` (`GuideApplication.Reject`). Separate sidebar nav item "GuideApplications".
- **vs plan:** §8.4 mentions guide-offering suspend/reinstate but not the **guide-application approval queue** as its own surface.
- **Action:** add Guide Applications (own page or a §8.4 sub-section) at `/admin/guide-applications`.

### G3 — Moderation is three surfaces, not one (§8.2)
- **Code:** `ModerationController` (`/admin/moderation` + `/warn`,`/ban`,`/unban`, perms `AdminModerationQueue.Warn/Ban`), `ReportsController` (`/admin/reports` + `/{id}/resolve`, `AdminModerationQueue.Read/Resolve`), `FlaggedReviewsController` (`/admin/flagged-reviews` + `/{id}/approve`,`/remove`, `AdminModerationQueue.Approve/Remove`). **Three nav items.**
- **vs plan:** §8.2 folds all into one Moderation page.
- **Action:** note §8.2 spans **three shipped pages/controllers** (Moderation logs+warn/ban, Reports queue, Flagged-reviews queue).

### G4 — Finance is five surfaces (§8.7)
- **Code nav items:** `Finance` (`PaymentsController`, `/admin/finance` + `/{id}/refund`, `Refund.Create`), `Payouts` (`/admin/payouts/trigger`,`/{id}/approve`), `Commissions` (`/admin/commissions` CRUD), `Disputes` (`/admin/disputes/{id}/review\|resolve\|escalate`), + booking finance (`BookingsController`: `/admin/bookings`, `/{id}/force-refund`, `/{id}/resolve-dispute`).
- **vs plan:** §8.7 Finance Ops folds all into one page.
- **Action:** note §8.7 spans **five shipped controllers/nav items**; list their routes.

### G5 — Blog Translations + Statistics are separate controllers
- `BlogTranslationsController`: `GET/POST /admin/blogs/{id}/translations`, `/translations/{languageCode}/edit`, `/translations/{languageCode}` — plan mentions translations inline in §8.6 but it's a dedicated controller.
- `StatisticsController`: `GET /admin/analytics` (`Interaction.Read`) — separate nav "Statistics"; plan folds into §8.1.
- **Action:** note both as distinct surfaces.

---

## 2. 🟠 Mapping: plan's 14 pages → shipped 24 nav items

| Plan page | Shipped nav item(s) / controller(s) |
|-----------|-------------------------------------|
| 8.1 Overview | `Dashboard` (`HomeController`) + **`Statistics`** (`/admin/analytics`) |
| 8.2 Moderation | **`Moderation` + `Reports` + `FlaggedReviews`** (3) |
| 8.3 Providers | `Providers` (incl. `/payment-methods/{methodId}/verify`) |
| 8.4 Tours Review | `Tours` + **`Trips`** + **`GuideApplications`** (+ proposals/packages/offerings under `/admin/tours/*`) |
| 8.5 Places & Businesses | `Places` (create/edit/feature/verify/accessibility) + `Businesses` (approve/…/delete) |
| 8.6 Blogs & Creators | `Blogs` (+ **`BlogTranslations`**) + `Creators` |
| 8.7 Finance Ops | **`Finance` + `Payouts` + `Commissions` + `Disputes`** + booking finance (`/admin/bookings`) |
| 8.8 Growth | `Growth` + **`Recommendations`** (`/admin/recommendations/batches\|boosts\|pins`) |
| 8.9 Content Ops | `Categories` `Tags` `Languages` `Specializations` `Translations` `Attachments` `EntityCategories` `EntityTags` (8 controllers) |
| 8.10 SEO Console | `SeoMetadata` `SeoFaq` `SeoRedirects` `SeoSitemap` `SeoWeather` (5 nav items) |
| 8.11 Support | `Support` |
| 8.12 Platform Ops | `Outbox` + `NotificationTemplates` |
| 8.13 Users & Audit | `Users` (roles/claims) + `Lifecycle` (suspend/…/revoke-sessions) + audit (`AuditLogsController`) |
| 8.14 Tour Guides | `Guides` (suspend/reinstate/edit/delete) |

- **Note:** §8.8 Growth also includes a **`Recommendations`** nav item (`RecommendationsController`: batches/boosts/pins) the plan partly covers under analytics-admin.
- **Action:** add this mapping table to the plan so the 14→24 grouping is explicit, and surface the bolded undocumented items (Trips, GuideApplications, Statistics, BlogTranslations, the Moderation/Finance splits, Recommendations).

---

## 3. ✏️ Route-shape confirmations (header note already covers verbs)

- All mutations are page-scoped `POST` ✅ (header API-vs-BFF note correct).
- Notable exact routes: Providers request-docs = `/admin/providers/{id}/request-documents`; SEO metadata = `/admin/seo/metadata/save`; weather = `/admin/seo/weather/refresh\|purge\|reset-budget`; translations = `/admin/translations/on-demand\|{id}/edit\|{id}/approve\|backfill\|approve-batch`; entity assign = `/admin/entity-categories/assign\|remove`, `/admin/entity-tags/assign\|remove`; categories reorder = `/admin/categories/reorder`; commissions = `/admin/commissions` + `/{id}/update\|delete`; outbox = `/admin/outbox/{module}/{id}/replay` + `/admin/outbox/backfill/tour-snapshots`.
- **Action:** these are all consistent with the plan's intent; only the undocumented surfaces (§1) need adding.

---

## 4. ✅ Confirmed-correct

- **Permissions** — all granular per-action constants match shipped `[RequirePermission]` across 43 controllers ✅.
- **Content→Admin consolidation** — §8.9/§8.10 routes `/admin/categories|seo/*` ✅.
- **§8.1 Statistics noted**, **§8.13 Lifecycle + revoke-sessions**, **§8.12 Outbox + templates**, **§8.6 blog feature/hide/approve/reject/remove + tours-link** — all shipped as the plan describes ✅.
- **BFF-POST verb principle** ✅.

---

## 5. Recommended plan edits (apply order)

1. **G1** document **`/admin/trips`** (the RowVersion-carrying tour-approvals screen) — the single biggest omission; clarify vs `/admin/tours`.
2. **G2** add **Guide Applications** (`/admin/guide-applications`).
3. **G3/G4** note §8.2 = 3 controllers (Moderation/Reports/FlaggedReviews) and §8.7 = 5 (Finance/Payouts/Commissions/Disputes/Bookings).
4. **G5** note BlogTranslations + Statistics + Recommendations as distinct surfaces.
5. **Mapping table** (§2) — add the 14→24 plan-page → nav-item map to make the grouping explicit.

> **Net:** permissions and the area model are solid. The deep route+nav read shows the plan's **14 pages compress 24 shipped nav surfaces**, and three surfaces are **entirely undocumented**: **`/admin/trips`** (a second, RowVersion-guarded tour-moderation screen — G1, the headline), **GuideApplications**, and the **Moderation/Finance splits**. None are *wrong*, but a builder using the plan would miss `/admin/trips` and the guide-application queue.
