# Endpoint Coverage and Gaps — Whole-Solution Audit

Status: advisory audit only. No code has been changed. Nothing here is a build instruction; it is a verified map of what the backend exposes, what the web layer actually consumes, where built pages are missing their endpoints, and a suggested plan for the unbuilt gaps.

Scope: all 14 backend modules under `src/Modules/*` and the web host `src/Hosts/YallaJo.Web` (86 `*ApiClient.cs` files, 81 built view folders across Areas Public, Auth, Accounts, Guide, Provider, Content, Admin).

Verification: every "consumed / not consumed" claim below was checked against real `*ApiClient.cs` source and backend `*Endpoints.cs` route groups, not inferred from naming.

---

## 0. Executive summary

- Two **broken paths** ship on built admin pages today (wrong base URL). These are live bugs, not gaps. Fix first.
- Two built admin pages (**EntityTags**, **EntityCategories**) have **empty ApiClients** — the UI exists but calls nothing.
- The **Guide** area is **scaffolded UI with almost no API wiring** (4 view folders, 1 thin client).
- Several whole backend feature groups have **no web consumer at all** (invoices, provider payment methods, notification inbox, device tokens, SEO writes, business-owner self-service, review/favorite user-writes, analytics personalization).
- A handful of built pages **assemble data the long way** because they never call the purpose-built summary endpoints (provider dashboard, provider documents list).

---

## 0.1 FE-0B audit corrections (verified during route-cleanup pass)

These three rows in the original audit were re-verified against current source and corrected. They do **not** reflect new work — only fixes to inaccurate or now-outdated audit claims.

- **Profile avatar DELETE — CONNECTED (was reported missing/unwired).**
  `DELETE /api/v1/accounts/profile/avatar` is fully wired end-to-end:
  `Accounts/Views/Profile/Index.cshtml` (remove-avatar `<form>`) → `Accounts.Controllers.ProfileController.DeleteAvatar` (`POST /accounts/profile/avatar/delete`) → `ProfileFacade.DeleteAvatarAsync` → `Accounts.ProfileApiClient.DeleteAvatarAsync`. No frontend work needed. (The §3 Accounts row already marks `Profile …/avatar/delete/…` ✅; this note records the explicit confirmation.)

- **Booking `/booking/provider/documents` — separate compliance-docs feature, NOT a duplicate/dead route.**
  The Booking-module `ProviderDocument` aggregate (`Booking.Domain.Entities.ProviderDocument`, `CreateForTourGuide` / `CreateForBusiness`, with expiry + suspension domain events) is a distinct bounded-context concern from the Accounts-module provider-application onboarding paperwork at `/api/v1/provider/documents/*`. They share a path substring by coincidence only. Reclassified from "duplicate/dead" to **MISSING FRONTEND** for an unbuilt operational compliance-docs UI (future feature batch, e.g. `FE-PROVIDER-COMPLIANCE-DOCS`). No backend cleanup; no FE-0B action.

- **Tour by-slug route — CONNECTED after FE-0B.**
  `Public.ToursApiClient.GetTourBySlugAsync` was migrated from the legacy alias `/api/v1/tours/slug/{slug}` to the canonical `/api/v1/tours/by-slug/{slug}`. Both backend routes remain alive (no backend change). The §3 ContentTours "Tour public/…" row (✅) now uses the canonical route.

---

## 1. Bugs on already-built pages (fix immediately)

These pages are built and shipping, but call the wrong backend path. Every request 404s.

| # | Web client | Current base (wrong) | Backend actual | Fix |
|---|---|---|---|---|
| B1 | `Areas/Admin/ApiClients/ReportsApiClient.cs:11` | `/api/v1/reports` | `/api/v1/social/reports` | set `Base = "/api/v1/social/reports"` |
| B2 | `Areas/Admin/ApiClients/FlaggedReviewsApiClient.cs:11` | `/api/v1/reviews` | `/api/v1/social/reviews` | set `Base = "/api/v1/social/reviews"` |

Routes affected:
- B1: `GET {Base}/admin`, `POST {Base}/admin/{id}/resolve` (Social `ReportEndpoints.cs`).
- B2: `GET {Base}/admin/flagged`, `POST {Base}/admin/{id}/approve`, `POST {Base}/admin/{id}/remove` (Social `ReviewEndpoints.cs`). Approve/remove bodies send `{ notes, rowVersion }`.

Confirmed in `SocialEndpoints.cs`: reviews group is `/api/v1/social/reviews`, reports group is `/api/v1/social/reports`.

### Empty-client pages (built UI, no calls)
| Page | Client | State |
|---|---|---|
| `Areas/Admin/Views/EntityTags` | `EntityTagsApiClient.cs` | empty — page calls nothing |
| `Areas/Admin/Views/EntityCategories` | `EntityCategoriesApiClient.cs` | empty — page calls nothing |

Backend exists for both: `GET /api/v1/content-core/entity-tags` (anon), `POST`, `DELETE`; same shape for `entity-categories`. Either wire the clients or remove the pages.

Also: `BusinesssApiClient.cs` (triple-s typo) is an empty duplicate of `BusinessesApiClient.cs` — dead file, delete.

---

## 2. Built pages missing endpoints they should call

These pages exist and work, but never call purpose-built backend endpoints — they reconstruct the data from other calls or simply omit the feature.

| Page | Should call (exists, unused) | Today |
|---|---|---|
| `Areas/Provider/Views/Dashboard` | `GET /provider/dashboard/overview`, `/pending-actions`, `/notifications` | Assembles dashboard from `tours/provider/my-tours` + `finance/guide/summary` + `booking/join-requests` instead of the single summary endpoints. |
| `Areas/Provider/Views/Settings` | `GET /provider/settings` | Page exists, never calls the settings endpoint. |
| `Areas/Provider` documents flow | `GET /provider/documents` (list), `PUT /provider/documents/{id}` | Only `POST /provider/documents/upload` is wired (`ProviderApiClient.cs:49`); cannot list or update submitted docs. |
| Any logged-in page (global) | `GET /notifications`, `/unread-count`, `POST /{id}/read`, `/read-all` | No notification inbox anywhere; only `GET/PUT /notifications/preferences` is wired (Accounts/Settings). |

Recommendation: wire the provider dashboard/settings/documents endpoints into the existing pages (low effort, endpoints already exist), and add a notification-bell partial to the shared layout that calls the inbox endpoints.

---

## 3. Consumed vs not consumed — full matrix

✅ consumed by at least one web client · ❌ no web consumer · ⚠️ consumed but broken path

### Accounts
| Endpoint group | Status |
|---|---|
| Profile get/put/avatar/delete/restore | ✅ |
| `me/marketing-consent` | ✅ |
| Provider register/apply/reapply/status/documents-upload | ✅ |
| Provider `dashboard/*`, `settings`, documents list/update | ❌ (see §2) |
| AdminProvider approve/reject/etc. | ✅ |
| Agency owner (guides/invite/applications) | ❌ |
| AgencyPublic | ❌ |
| GuideAgency (me/invitations, apply, accept, decline, leave) | ❌ |

### Analytics
| Group | Status |
|---|---|
| `popular/*`, `trending` | ✅ |
| `admin/dashboard`, `admin/interactions` | ✅ |
| Recommendations admin (batches/boosts/pins) | ✅ |
| Recommendations consumer feed (similar/for/onboarding/itinerary/GDPR) | ❌ |
| `provider/*`, `guide/*` analytics | ❌ |
| audit-logs redact/export | ❌ |
| `POST /interactions`, `analytics/preferences` | ❌ |

### Auth
| Group | Status |
|---|---|
| register/login/logout/verify/reset/sessions/external/devices/invitations-accept | ✅ |
| admin invitations + user lifecycle | ✅ |
| `refresh`, `admin/users/{id}/sessions` delete | ❌ |

### Booking
| Group | Status |
|---|---|
| TourBooking create/get/my-bookings/cancel/provider/confirm/reject/admin-all/force-refund | ✅ |
| TourBooking `complete` | ❌ |
| AvailabilitySlot manage/slots/public | ✅ |
| AvailabilitySlot `slots/bulk` | ❌ |
| ProviderDocument group (`/booking/provider/documents`) | ❌ — missing frontend for a **separate compliance-docs feature** (NOT a duplicate of Accounts onboarding docs; see FE-0B note §0.1) |
| JoinRequest get/approve/reject/submit | ✅ |
| GuideDiscount | ❌ |

### ContentBlogs
| Group | Status |
|---|---|
| Blog admin CRUD/publish/feature/approve/reject/translations/queue/deleted | ✅ |
| Blog `/{id}/tours` link/unlink | ✅ |
| BlogComment comments + reactions | ✅ (`Content/BlogsApiClient.cs:31,50,54,57`) |
| Blog `/my-blogs`, submit-for-review, restore, views | ❌ |
| Creator self-service (profile/applications/follow/redeem/avatar) | ❌ |
| Creator public (niches/profiles/followers) | ❌ |
| AdminCreator (applications, suspend/promote, invitations) | ✅ |

### ContentCore
| Group | Status |
|---|---|
| categories / languages / specializations / tags / translations | ✅ |
| attachments (upload/list/delete/reorder/primary/bulk) | ✅ |
| entity-tags / entity-categories | ❌ (empty clients, §1) |

### ContentPlaces
| Group | Status |
|---|---|
| Place public + admin CRUD/feature/verify | ✅ |
| Business public search/get/hours/amenities/services/accessibility (read) | ✅ |
| Business admin approve/reject/suspend/delete | ✅ |
| Business owner self-service (`/mine`, resubmit, PUT hours/accessibility, POST amenities/services/staff) | ❌ |

### ContentSeo
| Group | Status |
|---|---|
| faq read | ✅ |
| faq write/reorder | ❌ |
| redirects / metadata / sitemap / weather | ❌ |

### ContentTours
| Group | Status |
|---|---|
| Tour public/CRUD/submit/archive/admin moderation | ✅ |
| Schedule / Pricing | ✅ |
| Waypoints write + children-info PUT | ✅ (`Provider/TourApiClient.cs:41,44,47,50`) |
| TourGuide-on-tour write, TourPackage | ❌ |
| TourSearch search/suggest/featured/provider | ✅ |
| GuideApplication get + approve/reject | ✅ |
| GuideApplication `POST /` apply-to-run | ❌ |
| GuideOffering (schedules/pricing/private/suspend) | ❌ |
| TourGuideProfile `GET /me`, PUT `/{id}`, availability, earnings/summary | ✅ (partial, Guide area) |
| TourGuideProfile cover/tier/earnings-detail/analytics/applications/languages-write/delete | ❌ |
| TourGuideProfile public list/by-slug/{id}/tours | ✅ |
| AdminTourGuide suspend/reinstate | ✅ (edit/delete ❌) |
| TourProposal | ❌ |

### Finance
| Group | Status |
|---|---|
| Earnings `/finance/guide`, `/guide/summary`, `/admin/dashboard` | ✅ |
| Payment initiate/simulate-success/admin-all | ✅ |
| Payment my-payments, refund | ❌ |
| Invoice (my/provider/get/download) | ❌ |
| Payout provider + admin pending/trigger/approve | ✅ |
| Commissions | ✅ |
| ProviderPaymentMethod | ❌ |
| Dispute my + admin/review/resolve/escalate | ✅ |

### Messaging
| Group | Status |
|---|---|
| Notification preferences | ✅ |
| Notification inbox (list/unread/read/read-all/delete) | ❌ |
| Device tokens | ❌ |
| SupportTicket create + admin | ✅ |
| NotificationTemplate CRUD | ✅ |

### Security
| Group | Status |
|---|---|
| Account password/phone | ✅ |
| Users list/get/activate/roles/claims | ✅ |
| Roles CRUD + claims | ✅ |
| AuditLog | ✅ |
| `security/me` | ❌ |

### Social
| Group | Status |
|---|---|
| Review public list/by-entity/ratings | ✅ |
| Review reply + report (provider) | ✅ |
| Review user-write (create/update/delete/my-reviews/helpful) | ❌ |
| Review admin flagged/approve/remove | ⚠️ broken path (B2) |
| Favorite list/delete | ✅ |
| Favorite add/check | ❌ |
| Report create + admin resolve | ⚠️ broken path (B1) |
| Moderation (logs/warn/ban) | ❌ |

### Tracking
No HTTP routes exist (event/background only).

### Ops
Outbox dead-letters/replay ✅.

---

## 4. Unbuilt gaps — suggested plans

Grouped by user role, each with the backend that already exists and a thin build outline. All reuse the existing webestica theme and the established vertical-slice pattern (`Models / ApiClients / Facades / Controllers / Views` + permission gate + sidebar nav). No build implied here — these are proposals.

### 4.1 Customer self-service (highest user-facing value)
- **My reviews**: backend ready (`POST/PUT/DELETE /social/reviews`, `/my-reviews`, `/helpful`). Add an Accounts/Reviews page so customers can write/edit reviews and mark helpful. Today they can only read.
- **Wishlist add/check**: `POST /social/favorites`, `GET /favorites/check/{entityType}/{entityId}`. Wire add + heart-state into tour/place cards; only list+remove exist today.
- **Notification inbox**: `GET /notifications`, `/unread-count`, `POST /{id}/read`, `/read-all`. Shared layout bell + Accounts/Notifications page.
- **My invoices**: `GET /invoices/my-invoices`, `/{id}/download`. Add to Accounts/Bookings or a new Invoices page.
- **My payments**: `GET /payments/my-payments`. Surface payment history alongside bookings.

### 4.2 Provider self-service (wire existing pages first)
- Wire `provider/dashboard/*`, `provider/settings`, `provider/documents` list/update into the **existing** Provider Dashboard/Settings pages (§2) — endpoints already exist, lowest effort.
- **Provider payment methods**: `/provider-payment-methods` CRUD + verify — new Provider/PaymentMethods page (no consumer today).
- **Bulk availability**: `POST /availability/slots/bulk` — add a bulk-create action to the existing TourAvailability page.

### 4.3 Business owner self-service (whole area missing)
Backend supports a full owner flow (`/places/businesses/mine`, resubmit, PUT hours/accessibility, POST amenities/services/staff) but no owner area exists. Suggest a new Areas/Business self-service shell mirroring Areas/Provider: My business, Hours, Amenities, Services, Staff, Accessibility, Resubmit-for-review.

### 4.4 Guide self-service (scaffolded, mostly unwired)
Guide area has 4 view folders (Dashboard, MyTours, Profile, Schedule) but one thin client. The full plan already lives in `docs/tour-guide-dashboard-plan.md` (13 pages, covers GuideOffering, GuideDiscount, JoinRequest guide-side, GuideApplication apply, TourProposal, GuideAgency, full `/me/*` profile/earnings/analytics). Treat that doc as the build spec for closing these gaps.

### 4.5 Creator self-service (not built)
Backend Creator + Blog `/my-blogs` flow has no consumer. The full plan lives in `docs/content-creator-dashboard-plan.md` (5–7 pages). Treat as the build spec.

### 4.6 SEO / content-ops admin (no consumer)
`/seo` redirects, metadata, sitemap entries, weather, and faq write/reorder have no admin UI. Suggest an Admin/Seo section (Redirects, Metadata, Sitemap, Weather, FAQ-editor) — lower priority, ops-facing.

### 4.7 Moderation & misc
- **Moderation console**: `/social/moderation` logs/warn/ban — fold into existing Admin moderation surface.
- **Analytics personalization**: consumer recommendation feed + `analytics/preferences` + `POST /interactions` — only worth building when a personalized home/feed is on the roadmap.

---

## 5. Priority order (suggested)

1. **§1 bugs** — Reports + FlaggedReviews base paths, empty EntityTags/EntityCategories clients, delete typo file. (Live defects.)
2. **§2 wire-ups** — provider dashboard/settings/documents + notification inbox. (Endpoints exist, low effort, high value.)
3. **§4.1 customer self-service** — reviews, wishlist add, invoices, payments.
4. **§4.4 / §4.5** — guide + creator areas (specs already written).
5. **§4.2 / §4.3** — provider payment methods + business-owner area.
6. **§4.6 / §4.7** — SEO admin, moderation, analytics personalization.

---

## 6. Open items to confirm before building any §4 item
- Web permission groups exist for each target (e.g. GuideOffering, GuideAgency, TourProposal were missing from `WebPermission.cs` per the guide/creator plans — verify before gating new pages).
- Exact request/response DTOs for each new client (read the corresponding `*Endpoints.cs` + request records at build time).
- Whether `tours/{id}/guides` write and `tours/packages` should be provider-facing or admin-facing before adding clients.

---

## 7. Page roadmap (how many pages this all adds up to)

### 7.1 Coverage counts (logical endpoint groups, §3 matrix)
- ✅ consumed: **45**
- ❌ not consumed: **33**
- ⚠️ consumed but broken path: **2**
- Total tracked groups: **80** (≈56% wired, ≈44% not)

Raw HTTP-route count for reference: **~418 routes** across 14 modules + the Ops host (Tracking exposes 0 HTTP routes). The 80 groups are the planning unit; the 418 is the implementation surface.

### 7.2 Built vs needed (by area)

| Area | Built today | New pages to close gaps | Wire-up only (no new page) |
|---|---|---|---|
| Public | 6 | 0 | — |
| Auth | 6 | 0 | — |
| Accounts | 8 | 4 (My reviews, Notifications, Invoices, Payments) | wishlist add/check |
| Provider | 13 | 1 (Payment methods) | dashboard, settings, documents, bulk-availability |
| Admin | 36 | 6 (5 SEO + 1 Moderation) | fix Reports/FlaggedReviews, wire EntityTags/EntityCategories |
| Guide | 4 (scaffold) | 13 (full shell per `tour-guide-dashboard-plan.md`) | — |
| Creator | 0 | 6 (full shell per `content-creator-dashboard-plan.md`) | — |
| Business owner | 0 | 6 (new area: My business, Hours, Amenities, Services, Staff, Accessibility) | — |
| Analytics feed | 0 | 0–1 (optional, only if personalized feed is on roadmap) | — |

- **Already built:** ~77 page/view folders.
- **New pages to close every gap: ~36** (Accounts 4 · Provider 1 · Admin 6 · Guide 13 · Creator 6 · Business owner 6 · Analytics 0–1).

### 7.3 Recommended tiering (do NOT build all 36 at once)

| Tier | New pages | Scope |
|---|---|---|
| Tier 0 — fixes | 0 | §1 broken paths + §2 wire-ups. Edits to existing pages. Highest value, lowest cost. Do first. |
| Tier 1 — near-term, high value | ~5 | Customer reviews, notifications, invoices, payments + provider payment methods. User-facing holes in already-shipping areas. |
| Tier 2 — already speced | ~19 | Guide (13) + Creator (6). Specs exist; build when those roles launch. |
| Tier 3 — defer | ~12 | Business owner (6), SEO admin (5), Moderation (1). Build on concrete demand. |

### 7.4 Bottom line
- **To finish everything: ~36 new pages.**
- **Realistic near-term: ~5 new pages** + ~half a day of wire-up/bug-fix on existing pages.
- **~19 more** are already designed (guide/creator plans), awaiting those features going live.
- **~12** stay on the backlog until there is a real user for them.
