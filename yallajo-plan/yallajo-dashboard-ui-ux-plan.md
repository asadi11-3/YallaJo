# YallaJo — Dashboard UI/UX Plan

> **Source of truth:** This plan is rebuilt entirely from the live route table
> (`yallajo-endpoints.txt`, 549 endpoints, code-derived). Every endpoint cited
> below exists in the actual API surface. Routes that were assumed in earlier
> drafts but do **not** exist in code have been corrected — see §11.
>
> All routes are prefixed `/api/v1` unless shown otherwise (the four system
> routes `GET /`, `GET /sitemap.xml`, `GET /sitemaps/{entityType}.xml` are
> un-versioned).

---

## Page Index

8 actor surfaces (1 public + 7 role dashboards). **~68 distinct pages** (each
list + detail counted separately; tabs and widgets are parts of a page, not
separate pages). The auth funnel (§2.10) adds ~6 standalone flow screens not
counted below.

| § | Area | Pages | Breakdown |
|---|------|-------|-----------|
| §2 | Public / Anonymous Storefront | 14 | Home, Search, Place list, Place detail, Business detail, Tour list, Tour detail, Tour Package, Guides list, Guide detail, Agencies, Blog list, Blog post, Creator profile |
| §3 | Customer Dashboard | 10 | 3.1–3.10 |
| §4 | Provider Dashboard | 10 | 4.1–4.10 |
| §5 | Tour Guide Dashboard | 11 | 5.1–5.11 |
| §6 | Agency Dashboard | 4 | 6.1–6.4 |
| §7 | Content Creator Dashboard | 5 | 7.1–7.5 |
| §8 | Admin Dashboard | 14 | 8.1–8.14 |
| §9 | SuperAdmin RBAC Console | 1 | single console |
| | **Total** | **69** | |

---

## §0 Global Conventions

### 0.1 Image / media rule

All entity imagery flows through the **Attachment** subsystem in `content-core`:

- `GET  /content-core/attachments?entityType=&entityId=` — list attachments for an entity
- `GET  /content-core/attachments/{id}` — single attachment
- `POST /content-core/attachments` — upload one file (multipart) **`AJAX↑`**
- `POST /content-core/attachments/images` — bulk upload **≤ 20** images **`AJAX↑`**
- `PUT  /content-core/attachments/primary` — set primary image for an entity
- `PUT  /content-core/attachments/reorder` — reorder gallery
- `DELETE /content-core/attachments/{id}` — delete attachment + file

**Public convenience reads** (approved / public-safe only, return 404 for missing/deleted):

- `GET /places/{id}/images`
- `GET /tours/{id}/images`

**Dedicated avatar / cover routes** (do not go through attachments):

- `POST/PUT/DELETE /accounts/profile/avatar`
- `PUT /guides/me/avatar`, `PUT /guides/me/cover-image`
- `PUT /blogs/creators/profile/mine/avatar`

### 0.2 Weather rule

Weather is **always Place-contextual** and served from cache:

- `GET  /seo/weather/{placeId}` — cached weather; sets **`X-Weather-Stale: true`** header when the snapshot has expired (client should background-refresh, not block)
- `GET  /seo/weather?lat=&lng=` — ad-hoc lookup; cache key = (lat 2dp, lng 2dp, date)
- `POST /seo/weather/refresh/{placeId}` — force refresh (admin/ops)
- `DELETE /seo/weather/cache/{id}` — evict a cache entry (admin)
- `PUT  /seo/weather/budget/reset` — reset daily weather-API budget counter (admin)

Render weather as a **widget**, never a page. On a place/business/tour detail
page it hangs off the place context.

### 0.3 Load-strategy tags

| Tag | Meaning |
|-----|---------|
| **`SSR`** | Server-rendered on first paint (SEO-critical or above-the-fold) |
| **`AJAX`** | Fetched after paint (pagination, filters, typeahead, lazy tabs) |
| **`AJAX⟳`** | Polled or pushed (SignalR) — live counters, notifications |
| **`AJAX↑`** | Multipart upload |

**Default rules**

1. First list page + KPI cards = **`SSR`**.
2. Pagination / filter / typeahead = **`AJAX`**.
3. Inline state mutations (approve/reject/toggle) = **`AJAX`** returning the **updated row/resource**, re-rendered in place.
4. Uploads = **`AJAX↑`**.
5. Notification bell / unread counts = **`AJAX⟳`**.
6. Non-default tabs load **lazily** on first activation.
7. Any `PUT` that carries **`RowVersion`** round-trips it; a `409 Conflict` shows a "reload — this changed" prompt rather than silently overwriting.

### 0.4 Shared shell

**Top bar:** global search · language switcher · notification bell · avatar menu.

- **Bell** → `GET /notifications/unread-count` **`AJAX⟳`** + `GET /notifications` (paged) + `POST /notifications/{id}/read` + `POST /notifications/read-all`. Extra: `DELETE /notifications/{id}`, `DELETE /notifications/batch`, `GET /notifications/{id}`.
- **Avatar menu** → `GET /accounts/profile`.
- **Session guard / nav driver** → `GET /security/me` **`SSR`** (returns JWT claims; nav is the union of claim-allowed dashboards).
- **Language switcher** sets `Accept-Language`, which flows into translation-aware reads (`content-core/tags`, `translations/...`).

---

## §1 Role Inventory (8 actor types)

Navigation = **union of claim-allowed dashboards**. Multi-role users get a
**workspace switcher**. Claims come from `GET /security/me`.

| # | Actor | Primary route namespaces |
|---|-------|--------------------------|
| 1 | **Anonymous / Public** | storefront reads, `auth/*` funnel, system routes |
| 2 | **Customer / Tourist** | `accounts`, `booking/my-bookings`, `booking/join-requests`, `social`, `analytics/recommendations`, `payments/my-*`, `invoices/my-*`, `disputes/my`, `support`, `notifications`, `devices` |
| 3 | **Provider** | `provider`, `provider/dashboard`, `places/businesses`, `tours` (owner), `provider/my-tours`, `tours/provider/my-tours`, `booking/provider`, `booking/availability`, `payouts/provider`, `invoices/provider`, `provider-payment-methods` |
| 4 | **Tour Guide** | `guide`, `guides/me`, `guides/{id}`, `booking/guide-discounts`, `tours/{}/guide-offerings`, `tours/{}/applications`, `tours/proposals`, `finance/guide`, `booking/join-requests` |
| 5 | **Agency** | `agency`, `agency/guides`, `agency/applications`, `agency/invitations` |
| 6 | **Content Creator** | `blogs` (own), `blogs/my-blogs`, `blogs/creators/profile/mine`, `blogs/creators/applications` |
| 7 | **Admin** | `admin/*`, `*/admin/*`, `analytics/admin`, `seo`, `content-core` (admin), `commissions`, `disputes/admin`, `payments/admin`, `payouts/admin`, `support/admin`, `ops`, `social/moderation`, `social/reports/admin`, `social/reviews/admin` |
| 8 | **SuperAdmin / RBAC** | `security/roles`, `security/users`, `security/audit-logs`, `auth/admin`, `auth/invitations` |

---

## §2 Public / Anonymous Storefront

### 2.0 Conventions
- **SEO / SSR first.** Head injects `GET /seo/metadata/{entityType}/{entityId}` and `GET /seo/faq/{entityType}/{entityId}` per detail page. The `{entityType}` is constrained by the `SeoEntityType` enum (code source of truth: `SeoEntityType.cs`), which has **exactly 6 SEO-eligible entities**: `Place` (0), `Tour` (1), `Business` (2), `Blog` (3), `TourGuide` (4), `Creator` (5). Only these 6 detail pages carry SEO metadata + FAQ; no other page injects them.
- **Public image reads** use the approved-only convenience routes (`/places/{id}/images`, `/tours/{id}/images`).
- **Reference data is cacheable:** `content-core/categories`, `content-core/languages`, `content-core/tags`, `content-core/specializations`, `places/accessibility/catalog`.
- **i18n** via `Accept-Language` + `GET /content-core/translations/{entityType}/{entityId}` (`?languageCode=&status=`).
- **Login-gated buttons** for favoriting / booking / reviewing.
- **Anonymous beacons** (fire-and-forget via `navigator.sendBeacon`): `POST /blogs/{id}/views`, `POST /interactions`.

### 2.1 Home `SSR`
**Template:** `index-tour.html` ✅ (primary) · `index-directory.html` ♻️ (category-led variant) · `index.html` ♻️ (alt hero).
**Redirects:** featured/popular card → §2.5 Tour detail · category tile → §2.2 Search (filtered) · "Explore places" → §2.3 Place list · search box submit → §2.2 Search · "List your property" CTA → `join-us.html` (§4.2).
**Buttons:** **Search** → §2.2 (nav) · **Explore Tours** → §2.5 · **Browse Places** → §2.3 · **List Your Property** → §4.2 · **Sign in / Sign up** → §2.10 (anonymous only) · card → detail (nav).
**Stack:** **Area** `Public` · **Route** `/` · **Cache** `PublicShort` (tag homepage) · **Perm** `[AllowAnonymous]` · **Rules** `R2 SSR, S1, I4 hero, A8 critical-CSS, ERR3`

`GET /tours/featured` · `GET /trending` · `GET /popular/tours` · `GET /popular/places` · `GET /popular/businesses` · `GET /content-core/categories`.

### 2.2 Search `AJAX`
**Template:** hero search fragments (`hero-multiple-search.html`, `hero-inline-form.html`) ♻️ + `noUiSlider` price filter ♻️ (no dedicated results page — render results into a `tour-grid.html`/`hotel-grid.html` shell).
**Redirects:** result card → §2.5 Tour detail / §2.3 Place detail / §2.4 Business detail (by result type) · map pin → corresponding detail.
**Buttons:** **Apply Filters** → `GET /tours/search` (AJAX, client-driven) · **Clear** (client-only) · price slider/typeahead → `GET /tours/search/suggest` (AJAX, client-only) · result card → detail (nav).
**Stack:** **Area** `Public` · **Route** `/search` · **Cache** `PublicShort` · **Perm** `[AllowAnonymous]` · **Rules** `R2+S1 SSR-then-AJAX, S2-S6, D4 offcanvas, J4 debounce`

`GET /tours/search` · `GET /tours/search/suggest` · `GET /places/businesses/search` · `GET /places/map/viewport` · `GET /places/nearby` · `GET /places/businesses/nearby`.

### 2.3 Place list + detail
**Template:** `hotel-grid.html` / `hotel-list.html` ♻️ (list) · `room-detail.html` ♻️ / `hotel-detail.html` ♻️ (detail).
**Redirects:** list card → this detail · "businesses here" → §2.4 Business detail · book CTA → §3.2 create booking (login-gated) · favorite → §3.4.
**Buttons:** **Add to Favorites / Remove** → `POST /social/favorites` (entityType=Place) / `DELETE /social/favorites/Place/{id}` (state via `GET /social/favorites/check/Place/{id}`; login-gated) · **Write Review** → `POST /social/reviews` (login-gated) · **Add Accessibility Review** → `POST /social/accessibility/reviews` (login-gated) · **View Businesses** → §2.4 (nav).
**Stack:** **Area** `Public` · **Route** `/places`, `/places/{slug}` · **Cache** `PublicShort` list / `PublicMedium` detail (tag place:{id}, C5 ETag) · **Perm** `[AllowAnonymous]` · **Rules** `R2, SEO, IMG1, MAP1, NF6/WL1`

- List `SSR`: `GET /places` (max 50/page).
- **SEO head (`Place`):** `GET /seo/metadata/Place/{id}` + `GET /seo/faq/Place/{id}`.
- Detail `SSR`: `GET /places/{slug}` or `GET /places/{id}`; then `AJAX`: `GET /places/{id}/images`, `GET /places/{id}/accessibility`, `GET /places/{id}/businesses`, weather widget (§0.2), `GET /social/reviews/place/...` via `GET /social/reviews/{entityType}/{entityId}`, `GET /social/reviews/ratings`, `GET /social/accessibility/reviews`, `GET /analytics/recommendations/for/{kind}/{entityId}`.

### 2.4 Business detail `SSR` + `AJAX`
**Template:** `directory-detail.html` ♻️ (primary) · `hotel-detail.html` ♻️ (amenities/hours layout).
**Redirects:** "part of" → §2.3 parent Place detail · review CTA → login (§2.10) → §3.5 · weather widget (§0.2, no nav).
**Buttons:** **Add to Favorites / Remove** → `POST /social/favorites` (entityType=Business) / `DELETE /social/favorites/Business/{id}` (login-gated) · **Write Review** → `POST /social/reviews` (login-gated) · **Mark Helpful** → `POST /social/reviews/{id}/helpful` · **Report Review** → `POST /social/reports` (login-gated).
**Stack:** **Area** `Public` (owner mgmt in Business §4.5) · **Route** `/businesses/{slug}` · **Cache** `PublicMedium` (tag business:{id}) · **Perm** `[AllowAnonymous]` · **Rules** `R2, SEO, IMG1, weather widget`

- **SEO head (`Business`):** `GET /seo/metadata/Business/{id}` + `GET /seo/faq/Business/{id}`.

`GET /places/businesses/{id}` · `GET /places/businesses/{id}/hours` · `GET /places/businesses/{businessId}/amenities` · `GET /places/businesses/{businessId}/services` · `GET /places/businesses/services/{id}` · `GET /places/businesses/{id}/accessibility`.

### 2.5 Tour list + detail
**Template:** `tour-grid.html` ✅ (list) · `tour-detail.html` ✅ (detail, Overview/Itinerary/Inclusions tabs).
**Redirects:** list card → this detail · guide chip → §2.7 Guide detail · "Book this tour" → §3.2 (`tour-booking.html` checkout, login-gated) · similar tours → §2.5 (other) · favorite → §3.4.
**Buttons:** **Book Now** → `POST /booking/tour` (→ AwaitingPayment, checks `GET /booking/availability/{tourId}/{date}`; login-gated) · **Add to Favorites / Remove** → `POST /social/favorites` (entityType=Tour) / `DELETE /social/favorites/Tour/{id}` · **Write Review** → `POST /social/reviews` (login-gated) · **Mark Helpful** → `POST /social/reviews/{id}/helpful` · **Report Review** → `POST /social/reports` · **Choose Guide** → §2.5 guide-offering select (nav) · view/similar beacons → `POST /analytics/recommendations/metrics`, `/sponsored-click` (fire-and-forget).
**Stack:** **Area** `Public` · **Route** `/tours`, `/tours/{slug}` · **Cache** `PublicShort` list / `PublicMedium` detail (tag tour:{id}) · **Perm** `[AllowAnonymous]` · **Rules** `R2, SEO+JSON-LD, IMG1, MAP4, CAL1-3 + S2 SignalR tour:{tourId}, D5 sticky CTA, NF6`

- List `SSR`: `GET /tours`.
- **SEO head (`Tour`):** `GET /seo/metadata/Tour/{id}` + `GET /seo/faq/Tour/{id}`.
- Detail `SSR`: **canonical** `GET /tours/by-slug/{slug}` (alias `GET /tours/slug/{slug}` exists); or `GET /tours/{id}`.
- `AJAX` tabs: `GET /tours/{id}/images` · `/waypoints` · `/children-info` · `/pricing` · `/schedules` · `/guides`.
- Guide offerings: `GET /tours/{tourId}/guide-offerings` · `/{guideId}` · `/{guideId}/pricing-tiers` · `/{guideId}/schedules`.
- Availability: `GET /booking/availability/{tourId}` · `GET /booking/availability/{tourId}/{date}`.
- Recommendations: `GET /analytics/recommendations/similar/{entityId}` · `GET /analytics/recommendations/for/{kind}/{entityId}`.

### 2.6 Tour Package `SSR`
**Template:** 🟥 USER builds (no template page; closest base = `tour-grid.html` card + `tour-detail.html` layout).
**Redirects:** package card → this detail · included-tour chip → §2.5 Tour detail · book CTA → §3.2.
**Buttons:** **Book Package** → `POST /booking/tour` (package booking, login-gated) · **Add to Favorites** → `POST /social/favorites` (entityType=Tour) · included-tour chip → §2.5 (nav).
**Stack:** **Area** `Public` · **Route** `/packages/{slug}` · **Cache** `PublicMedium` · **Perm** `[AllowAnonymous]` · **Rules** `R2, SEO, CAL5`

`GET /tours/packages` · `GET /tours/packages/{id}`.

### 2.7 Guides list + detail
**Template:** 🟥 USER builds (no template page; closest base = a directory/agent-profile card grid).
**Redirects:** guide card → this detail · "tours by this guide" → §2.5 Tour detail · agency badge → §2.8 Agency.
**Buttons:** **Add to Favorites** → `POST /social/favorites` (entityType mirrors TourGuide; login-gated) · **View Tours** → §2.5 (nav) · **View Agency** → §2.8 (nav).
**Stack:** **Area** `Public` · **Route** `/guides`, `/guides/{slug}` · **Cache** `PublicShort` list / `PublicMedium` detail · **Perm** `[AllowAnonymous]` · **Rules** `R2, SEO, NF6 TourGuide favorite, WL1`

- **SEO head (`TourGuide`):** `GET /seo/metadata/TourGuide/{id}` + `GET /seo/faq/TourGuide/{id}`.

`GET /guides` · **canonical** `GET /guides/by-slug/{slug}` · `GET /guides/{id}` · `GET /guides/{id}/tours`.

### 2.8 Agencies
**Template:** 🟥 USER builds (no template page).
**Redirects:** agency card → agency detail · "guides in this agency" → §2.7 Guide detail · "apply to join" → §5.11 (guide, login-gated).
**Buttons:** **Apply to Join** → `POST /guides/agencies/{agencyUserId}/apply` (guide role only, login-gated) · "guides in this agency" → §2.7 (nav).
**Stack:** **Area** `Public` · **Route** `/agencies`, `/agencies/{id}` · **Cache** `PublicMedium` · **Perm** `[AllowAnonymous]` (Apply → `[Authorize]`+Guide) · **Rules** `R2, SEO`

`GET /agency` (approved, accepting applications) · `GET /agency/{agencyUserId}` (public detail).

### 2.9 Blog list + post + creator profile
**Template:** `blog.html` ✅ (list) · `blog-detail.html` ✅ (post). Creator profile = 🟥 USER builds (reuse `blog.html` author header).
**Redirects:** list card → §2.9 Blog post · author/avatar → §2.9 Creator profile · related-tour chip → §2.5 Tour detail · follow CTA → login (§2.10).
**Buttons:** **Follow / Unfollow Creator** → `POST` / `DELETE /blogs/creators/profiles/{profileId}/follow` (state via `GET .../following`; login-gated) · **Post Comment** → `POST /blogs/{id}/comments` (login-gated) · **Edit Comment (≤30min)** → `PUT /blogs/comments/{commentId}` · **Delete Comment** → `DELETE /blogs/comments/{commentId}` · **React / Unreact** → `POST` / `DELETE /blogs/comments/{commentId}/reactions` · view beacon → `POST /blogs/{id}/views` (fire-and-forget) · related-tour chip → §2.5 (nav).
**Stack:** **Area** `Public` · **Route** `/blog`, `/blog/{slug}`, `/creators/{slug}` · **Cache** `PublicShort` list / `PublicLong` post+profile · **Perm** `[AllowAnonymous]` · **Rules** `R2, SEO, SEC3 sanitized HTML, API2 view beacon, NF6`

- List `SSR`: `GET /blogs` (incl. `isFeatured` filter).
- Post `SSR`: **SEO head (`Blog`):** `GET /seo/metadata/Blog/{id}` + `GET /seo/faq/Blog/{id}`; then `GET /blogs/slug/{slug}` or `GET /blogs/{id}`; then `GET /blogs/{id}/comments` `AJAX`; beacon `POST /blogs/{id}/views`.
- Creator profile: **SEO head (`Creator`):** `GET /seo/metadata/Creator/{profileId}` + `GET /seo/faq/Creator/{profileId}`; then `GET /blogs/creators/niches` · `GET /blogs/creators/profiles/{slug}` · `GET /blogs/creators/profiles/{slug}/blogs` · `GET /blogs/creators/profiles/{profileId}/followers`.
- Follow state (auth): `GET /blogs/creators/profiles/{profileId}/following`, `POST/DELETE /blogs/creators/profiles/{profileId}/follow`.
- Comment reactions (auth): `POST/DELETE /blogs/comments/{commentId}/reactions`.

### 2.10 Auth funnel
**Template:** `sign-in.html` ✅ (login) · `sign-up.html` ✅ (register) · `forgot-password.html` ✅ · `reset-password.html` / `reset-password-v2.html` ✅ · `two-factor-auth.html` ✅ (OTP / verify-email).
**Redirects:** login/register success → §3.1 Customer overview (or role dashboard via `security/me` claims) · register → `two-factor-auth.html` (OTP) → login · forgot → reset → login · invitation accept → role dashboard.
**Buttons:** **Sign In** → `POST /auth/login` · **Continue with provider** → `POST /auth/external-providers/login` · **Register** → `POST /auth/register` · **Verify Email** → `POST /auth/verify-email` · **Resend Code** → `POST /auth/resend-otp` · **Forgot Password** → `POST /auth/forgot-password` · **Reset Password** → `POST /auth/reset-password` · **Accept Invitation** → `POST /auth/invitations/accept` (all anonymous).
**Stack:** **Area** `Auth` · **Route** `/auth/sign-in|sign-up|forgot-password|reset-password|verify` · **Cache** `NoStore` · **Perm** `[AllowAnonymous]` · **Rules** `SEC5 rate-limit, F1-F3, cookie YallaJo.Auth SameSite=Lax, SEC7`

`POST /auth/register` · `/verify-email` · `/resend-otp` · `/login` · `/forgot-password` · `/reset-password` · `/refresh` · `/invitations/accept` · `/external-providers/login` · `/logout` · `/logout-all`.

### 2.11 System endpoints
**Template:** ⏭️ no UI for webhook/sitemap routes · `error.html` ✅ (404) · `coming-soon.html` ✅ (maintenance/placeholder).
**Redirects:** `error.html` → §2.1 Home · `coming-soon.html` → §2.1 Home / notify-signup.

`GET /` · `GET /sitemap.xml` · `GET /sitemaps/{entityType}.xml` · `POST /payments/webhook` (HMAC-verified) · `GET /seo/weather?lat=&lng=`.

---

## §3 Customer Dashboard

### 3.1 Overview `/me` `SSR`
**Template:** `account-bookings.html` ✅ (account-* sidebar shell; overview lands here).
**Redirects:** sidebar → §3.2–§3.10 · booking card → §3.3 · "view all favorites" → §3.4 · recommendations → §2.5/§2.3 · bell → notifications.
**Buttons:** **View Bookings** → §3.2 (nav) · **View Favorites** → §3.4 (nav) · **Discover** → §3.6 (nav) · **Mark all read** → `POST /notifications/read-all` · "not interested" on rec → `POST /analytics/recommendations/not-interested`.
**Stack:** **Area** `Accounts` · **Route** `/accounts` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `R2, API1 Task.WhenAll, NF7 bell, RT2`

`GET /accounts/profile` · `GET /booking/my-bookings` · `GET /analytics/recommendations` · `GET /social/favorites` · `GET /notifications/unread-count`.

### 3.2 My Trips `/me/bookings`
**Template:** `account-bookings.html` ✅ (list) · `tour-booking.html` ✅ (create/checkout flow).
**Redirects:** booking row → §3.3 Booking detail · "book again"/new → `tour-booking.html` → §3.7 payment step · cancel → stay (inline AJAX).
**Buttons:** **Book Again / New Booking** → `POST /booking/tour` (→ §3.7 payment) · **Cancel Booking** → `POST /booking/{id}/cancel` (inline) · **Request to Join** → `POST /booking/join-requests` · booking row → §3.3 (nav).
**Stack:** **Area** `Accounts` · **Route** `/accounts/bookings` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `R2, D1 paging, F8 cancel confirm, CAL5, NF1`

`GET /booking/my-bookings` (cursor paginated) · `POST /booking/tour` (create, AwaitingPayment) · `POST /booking/{id}/cancel` · `GET /booking/join-requests?own=true` · `POST /booking/join-requests`.

### 3.3 Booking Detail
**Template:** `booking-confirm.html` ♻️ (confirmation state) + 🟥 full detail view USER builds.
**Redirects:** "download invoice" → §3.7 · "raise dispute" → §3.8 · "view tour" → §2.5 · cancel → §3.2.
**Buttons:** **Download Invoice** → `GET /invoices/{id}/download` · **Raise Dispute** → `POST /booking/{id}/dispute` · **Cancel Booking** → `POST /booking/{id}/cancel` (inline) · **View Tour** → §2.5 (nav).
**Stack:** **Area** `Accounts` · **Route** `/accounts/bookings/{id}` · **Cache** `NoStore` · **Perm** `[Authorize]` (API owner-scoped) · **Rules** `PRINT1-3 invoice, ST1 RowVersion, REV1 ?action=review, PAY4`

`GET /booking/{id}` *(scoped: owner, provider, or admin)* · `GET /invoices/my-invoices?bookingId` · `GET /invoices/{id}/download` · `POST /booking/{id}/dispute` (Completed only) · `POST /booking/{id}/cancel` · weather widget.

### 3.4 Favorites
**Template:** `account-wishlist.html` ✅.
**Redirects:** favorite card → §2.3 / §2.4 / §2.5 detail (by entityType) · remove → stay (inline AJAX).
**Buttons:** **Remove from Favorites** → `DELETE /social/favorites/{entityType}/{entityId}` (inline) · favorite card → detail (nav).
**Stack:** **Area** `Accounts` · **Route** `/accounts/wishlist` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `NF6 optimistic, WL3 Undo, WL4 counter, WL5 cap 500`

`GET /social/favorites` · `POST /social/favorites` · `DELETE /social/favorites/{entityType}/{entityId}` · `GET /social/favorites/check/{entityType}/{entityId}`.

### 3.5 My Reviews
**Template:** 🟥 USER builds (no template page; reuse account-* list shell + review card).
**Redirects:** review row → §2.3 / §2.4 / §2.5 reviewed entity · edit (≤48h) → inline · delete → stay.
**Buttons:** **Edit Review (≤48h)** → `PUT /social/reviews/{id}` (also `PUT /social/accessibility/reviews/{id}`) · **Delete Review** → `DELETE /social/reviews/{id}` (/accessibility) · review row → reviewed entity (nav).
**Stack:** **Area** `Accounts` · **Route** `/accounts/reviews` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `REV5 48h edit badge, F8 delete`

- Standard: `GET /social/reviews/my-reviews` · `POST /social/reviews` · `PUT /social/reviews/{id}` (≤48h) · `DELETE /social/reviews/{id}`.
- Helpful votes / reporting: `POST/DELETE /social/reviews/{id}/helpful`, `POST /social/reviews/{id}/report`.
- Accessibility: `GET /social/accessibility/reviews/my` · `POST /social/accessibility/reviews` · `PUT /social/accessibility/reviews/{id}` (≤48h) · `DELETE /social/accessibility/reviews/{id}`.

### 3.6 Recommendations `/me/discover`
**Template:** 🟥 USER builds (no template page; reuse `tour-grid.html` card grid + onboarding modal).
**Redirects:** recommendation card → §2.5 / §2.3 detail · "not interested" → stay (inline) · itinerary item → §2.5.
**Buttons:** **Not Interested** → `POST /analytics/recommendations/not-interested` (inline) · **Start Onboarding** → `POST /analytics/recommendations/onboarding` · sponsored card click → `POST /analytics/recommendations/sponsored-click` (beacon) · recommendation card → detail (nav).
**Stack:** **Area** `Accounts` · **Route** `/accounts/discover` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `R2, API2 beacons fire-and-forget, NF1`

`GET /analytics/recommendations` · `/itinerary` · `/similar/{entityId}` · `/for/{kind}/{entityId}`. Events: `POST /analytics/recommendations/onboarding` · `/not-interested` · `/metrics` · `/sponsored-click`.

### 3.7 Payments & Invoices `/me/billing`
**Template:** `account-payment-details.html` ✅ (saved methods + history) · `tour-booking.html` payment step ♻️ (initiate flow).
**Redirects:** payment/invoice row → §3.3 source booking · "download" → file · "pay now" → payment gateway → §3.3.
**Buttons:** **Pay Now** → `POST /payments/initiate` (→ gateway → §3.3) · **Download Invoice** → `GET /invoices/{id}/download` · payment/invoice row → §3.3 (nav). _(DEV-ONLY `POST /payments/{bookingId}/simulate-success` — never rendered in prod UI.)_
**Stack:** **Area** `Accounts` · **Route** `/accounts/billing` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `PAY1-4 PSP iframe/idempotency/lock/webhook, PRINT2 (DEV-only simulate-success never prod)`

`GET /payments/my-payments` · `GET /payments/{id}` · `POST /payments/initiate` · `GET /invoices/my-invoices` · `GET /invoices/{id}` · `GET /invoices/{id}/download`. *(Dev-only: `POST /payments/{bookingId}/simulate-success` — never surfaced in prod UI.)*

### 3.8 Disputes
**Template:** 🟥 USER builds (no template page; reuse account-* list shell + thread view).
**Redirects:** dispute row → dispute detail/thread · "from booking" → §3.3 · resolution → §3.7 (refund).
**Buttons:** **Open Dispute** → `POST /disputes` · dispute row → thread (nav) · "from booking" → §3.3 (nav).
**Stack:** **Area** `Accounts` · **Route** `/accounts/disputes` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `R2, D1, F8`

`GET /disputes/my` · `POST /disputes` (against a payment).

### 3.9 Support
**Template:** `help-center.html` ♻️ / `help-detail.html` ♻️ (entry / KB) + 🟥 ticket thread USER builds.
**Redirects:** "open a ticket" → ticket thread · ticket row → thread · close → §3.9 list.
**Buttons:** **New Ticket** → `POST /support/tickets` · **Send Message** → `POST /support/tickets/{id}/messages` · **Close Ticket** → `POST /support/tickets/{id}/close` · ticket row → thread (nav).
**Stack:** **Area** `Accounts` · **Route** `/accounts/support` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `R2, NF1, L6 empty-state`

`GET /support/tickets` · `GET /support/tickets/{id}` · `POST /support/tickets` · `POST /support/tickets/{id}/messages` · `POST /support/tickets/{id}/close`.

### 3.10 Account Settings `/me/settings`
**Template:** `account-profile.html` ✅ (Profile tab) · `account-settings.html` ✅ (Security / Notifications / Privacy tabs) · `account-delete.html` ✅ (delete). `account-travelers.html` = 🟥 no matching endpoint (USER decides: drop or build as new feature). Devices / Linked-accounts tabs = 🟥 USER builds.
**Redirects:** delete-account confirm → logout → §2.1 Home · logout-all → §2.10 login · password change → re-auth.
**Buttons:** **Save Profile** → `PUT /accounts/profile` · **Upload/Change/Remove Avatar** → `POST/PUT/DELETE /accounts/profile/avatar` · **Save Marketing Consent** → `PUT /accounts/me/marketing-consent` · **Change Password** → `PUT /security/account/password` · **Change Phone** → `PUT /security/account/phone` · **Revoke Session** → `DELETE /auth/sessions/{sessionId}` · **Log Out All** → `POST /auth/logout-all` · **Save Notification Prefs** → `PUT /notifications/preferences` · **Register/Remove Device** → `POST` / `DELETE /devices/token[/{id}]` · **Trust Device** → `PATCH /auth/devices/{deviceId}/trust` · **Link/Unlink Account** → `POST` / `DELETE /auth/external-providers[/{providerId}]` · **Save Privacy Prefs** → `PUT /analytics/preferences` · **Export My Data** → `GET /analytics/recommendations/me/export` · **Delete My Data / Cancel** → `DELETE /analytics/recommendations/me` / `POST .../me/cancel-deletion` · **Delete Account / Restore** → `DELETE /accounts/profile` / `POST /accounts/profile/restore`.
**Stack:** **Area** `Accounts` · **Route** `/accounts/settings` · **Cache** `NoStore` · **Perm** `[Authorize]` · **Rules** `lazy tabs, F3 password meter, F8 delete, ST2 session, GEO2 currency, avatar AJAX↑/SEC4, SEC7`

Tabs — **Profile / Security / Notifications / Devices / Linked accounts / Privacy**:

- **Profile:** `GET/PUT /accounts/profile` · `POST/PUT/DELETE /accounts/profile/avatar` · `DELETE /accounts/profile` + `POST /accounts/profile/restore` · `GET/PUT /accounts/me/marketing-consent`.
- **Security:** `PUT /security/account/password` · `PUT /security/account/phone` · `GET /auth/sessions` · `DELETE /auth/sessions/{sessionId}` · `POST /auth/logout-all`.
- **Notifications:** `GET/PUT /notifications/preferences`.
- **Devices:** `GET /devices/tokens` · `POST /devices/token` · `DELETE /devices/token/{id}` · `PATCH /auth/devices/{deviceId}/trust`.
- **Linked accounts:** `POST /auth/external-providers` (link) · `DELETE /auth/external-providers/{providerId}` (unlink). *(No GET-list endpoint — render from `security/me` claims or local state; see §11.)*
- **Privacy:** `GET/PUT /analytics/preferences` · `GET /analytics/recommendations/me/export` · `DELETE /analytics/recommendations/me` (30-day window) + `POST /analytics/recommendations/me/cancel-deletion`.

---

## §4 Provider Dashboard

### 4.1 Overview `SSR`
**Template:** `agent-dashboard.html` ✅ (KPIs) · `agent-activities.html` ✅ (activity feed).
**Redirects:** KPI card → §4.3 / §4.6 / §4.8 · pending-action item → its target section · feed item → related entity.
**Buttons:** **Create Tour** → §4.3 (nav) · **Add Business** → §4.5 (nav) · **View Bookings** → §4.6 (nav) · pending-action **Resolve** → its section (nav). No write-endpoints on this page (read-only KPIs).
**Stack:** **Area** `Provider` · **Route** `/provider/dashboard` · **Cache** `NoStore` · **Perm** policy `Provider` + `WebPermission.Provider.Read` · **Rules** `R2, API1, ApexCharts dashboard-bundle (read-only)`

`GET /provider/dashboard` · `/dashboard/overview` · `/dashboard/pending-actions` · `/dashboard/notifications` · `GET /provider/analytics`.

### 4.2 Application
**Template:** `join-us.html` ✅ ("List Your Property" acquisition) + 🟥 status/document-upload steps USER builds (reuse Dropzone §0.1).
**Redirects:** apply submit → §4.1 (pending state) · approved → §4.3 first-listing · request-docs → upload step.
**Buttons:** **Register** → `POST /provider/register` · **Submit Application** → `POST /provider/apply` · **Re-apply** → `POST /provider/reapply` (after rejection) · **Upload Document** → `POST /provider/documents/upload` (AJAX↑) · **Replace Document** → `PUT /provider/documents/{id}`.
**Stack:** **Area** `Provider` · **Route** `/provider/apply` · **Cache** `NoStore` · **Perm** `[Authorize]` + `WebPermission.Provider.Apply` (pre-provider) · **Rules** `F1-F3, AJAX↑ SEC4 docs`

`GET /provider/status` · `POST /provider/register` · `POST /provider/apply` · `POST /provider/reapply` · `POST /provider/documents` · `POST /provider/documents/upload` **`AJAX↑`** · `PUT /provider/documents/{id}`.

### 4.3 Tours
**Template:** `agent-listings.html` ✅ (list) · `add-listing.html` ✅ / `add-listing-minimal.html` ✅ (editor) · `listing-added.html` ✅ (submit confirm).
**Redirects:** listing row → editor · "create" → `add-listing.html` → `listing-added.html` → §4.3 list · "preview" → §2.5 Tour detail · applications → inline / §4.6.
**Buttons:** **New Tour** → `POST /tours` · **Save** → `PUT /tours/{id}` · **Submit for Review** → `POST /tours/{id}/submit` · **Archive** → `POST /tours/{id}/archive` · **Delete** → `DELETE /tours/{id}` (confirm) · editor: **Add/Save/Remove Pricing** → `POST/PUT/DELETE /tours/{id}/pricing[/{tierId}]` · **Schedule** → `.../schedules` · **Waypoint** → `.../waypoints` + **Reorder** → `PUT .../waypoints/reorder` · **Assign Guide** → `POST /tours/{id}/guides` · **Remove Guide** → `DELETE /tours/{id}/guides/{guideUserId}` · **Open/Close Applications** → `POST /tours/{tourId}/open-applications|close-applications` · **Approve/Reject Applicant** → `POST /tours/{tourId}/applications/{applicationId}/approve|reject` · **Save Child Info** → `PUT /tours/{id}/children-info`.
**Stack:** **Area** `Provider` · **Route** `/provider/tours`, `/provider/tours/{id}/edit` · **Cache** `NoStore` · **Perm** policy `Provider` + `WebPermission.Tours.Write` · **Rules** `F4/PROV1-6 wizard, PROV2 draft, F5, ST1, C3 evict tour:{id}, SEC7`

- List: `GET /tours/provider/my-tours` (all statuses) **and/or** `GET /provider/my-tours` (with perf metrics).
- Lifecycle: `POST /tours` · `POST /tours/{id}/submit` · `POST /tours/{id}/archive` · `DELETE /tours/{id}`.
- **Editor tabs:** `PUT /tours/{id}` · `GET/PUT /tours/{id}/children-info` · pricing `GET/POST/PUT/DELETE /tours/{id}/pricing[/{tierId}]` · schedules `GET/POST/PUT/DELETE /tours/{id}/schedules[/{scheduleId}]` · waypoints `GET/POST/PUT/DELETE /tours/{id}/waypoints[/{waypointId}]` + `PUT /tours/{id}/waypoints/reorder` · guides `GET/POST/DELETE /tours/{id}/guides[/{guideUserId}]` · Media via attachments (§0.1).
- **Guide applications to my tour:** `GET /tours/{tourId}/applications` · `POST /tours/{tourId}/applications/{applicationId}/approve` · `/reject` · `POST /tours/{tourId}/open-applications` · `/close-applications`.
- `GET /tours/{tourId}/guide-offerings` (read offerings on my tour).

### 4.4 Packages
**Template:** 🟥 USER builds (reuse `add-listing.html` editor + `agent-listings.html` list).
**Redirects:** package row → package editor · submit → §4.4 list · "preview" → §2.6.
**Buttons:** **New Package** → `POST /tours/packages` · **Save** → `PUT /tours/packages/{id}` · **Delete** → `DELETE /tours/packages/{id}` (confirm) · **Add Inclusion** → `POST /tours/packages/{id}/inclusions` · **Submit for Review** → `POST /tours/packages/{id}/submit`.
**Stack:** **Area** `Provider` · **Route** `/provider/packages` · **Cache** `NoStore` · **Perm** policy `Provider` + `WebPermission.Tours.Write` · **Rules** `F4, SEC7`

`GET /tours/packages` · `POST /tours/packages` · `PUT /tours/packages/{id}` · `DELETE /tours/packages/{id}` · `POST /tours/packages/{id}/inclusions` · `POST /tours/packages/{id}/submit`.

### 4.5 Businesses
**Template:** `agent-listings.html` ♻️ (list) · `add-listing.html` ♻️ (editor, with amenities/hours/services sub-forms).
**Redirects:** business row → editor · create/resubmit → §4.5 list · "preview" → §2.4 Business detail.
**Buttons:** **New Business** → `POST /places/businesses` · **Save** → `PUT /places/businesses/{id}` · **Resubmit** → `POST /places/businesses/{id}/resubmit` (after rejection) · **Add Amenity** → `POST /places/businesses/{businessId}/amenities` · **Remove Amenity** → `DELETE /places/businesses/amenities/{amenityId}` · **Add/Save/Delete Service** → `POST /places/businesses/{businessId}/services` · `PUT/DELETE /places/businesses/services/{id}` · **Add/Remove Staff** → `POST /places/businesses/{id}/staff` · `DELETE /places/businesses/staff/{id}` · **Save Hours** → `PUT /places/businesses/{id}/hours` · **Save Accessibility** → `PUT /places/businesses/{id}/accessibility`.
**Stack:** **Area** `Business` · **Route** `/business/listings` · **Cache** `NoStore` · **Perm** policy `Provider` + `WebPermission.Business.Write` · **Rules** `C3 evict business:{id}, weather widget, SEC7`

`GET /places/businesses/mine` · `GET /places/businesses/{id}` · `POST /places/businesses` · `PUT /places/businesses/{id}` · `POST /places/businesses/{id}/resubmit`. Sub-resources:
- Amenities `GET/POST /places/businesses/{businessId}/amenities` · `DELETE /places/businesses/amenities/{amenityId}`
- Services `GET/POST /places/businesses/{businessId}/services` · `GET/PUT/DELETE /places/businesses/services/{id}`
- Staff `GET/POST /places/businesses/{id}/staff` · `DELETE /places/businesses/staff/{id}`
- Hours `GET/PUT /places/businesses/{id}/hours`
- Accessibility `GET/PUT /places/businesses/{id}/accessibility`
- Weather widget (§0.2).

### 4.6 Incoming Bookings
**Template:** `agent-bookings.html` ✅ (incoming list + availability mgmt; use Flatpickr §0.x for slots).
**Redirects:** booking row → booking detail (§3.3-style) · confirm/reject/complete → stay (inline AJAX) · "manage availability" → slots editor.
**Buttons:** **Confirm** → `POST /booking/{id}/confirm` · **Reject** → `POST /booking/{id}/reject` (always 100% refund, confirm) · **Mark Complete** → `POST /booking/{id}/complete` · **Add Slot** → `POST /booking/availability/slots` · **Bulk Add Slots** → `POST /booking/availability/slots/bulk` · **Edit/Delete Slot** → `PUT/DELETE /booking/availability/slots/{id}`.
**Stack:** **Area** `Provider` · **Route** `/provider/bookings` · **Cache** `NoStore` · **Perm** policy `Provider` + `WebPermission.Booking.Manage` · **Rules** `F8 reject-refund, Flatpickr slots, RT4 SignalR provider:{providerId}, SEC7`

`GET /booking/provider/bookings` (cursor) · `GET /booking/{id}` · `POST /booking/{id}/confirm` · `/reject` (always 100% refund) · `/complete`. Availability mgmt: `GET /booking/availability/{tourId}/manage` · `POST /booking/availability/slots` · `/slots/bulk` · `PUT/DELETE /booking/availability/slots/{id}`.

### 4.7 Reviews
**Template:** `agent-reviews.html` ✅.
**Redirects:** review row → reviewed entity (§2.3/§2.4/§2.5) · reply/report → stay (inline AJAX).
**Buttons:** **Reply** → `POST /social/reviews/{id}/reply` · **Edit/Delete Reply** → `PUT/DELETE /social/reviews/{id}/reply/{replyId}` · **Report** → `POST /social/reviews/{id}/report`.
**Stack:** **Area** `Provider` · **Route** `/provider/reviews` · **Cache** `NoStore` · **Perm** policy `Provider` + `WebPermission.Reviews.Reply` · **Rules** `SEC3, SEC7`

`GET /social/reviews/{entityType}/{entityId}` · `GET /social/reviews/ratings` · `POST /social/reviews/{id}/reply` · `PUT/DELETE /social/reviews/{id}/reply/{replyId}` · `POST /social/reviews/{id}/report`.

### 4.8 Finance
**Template:** `agent-earnings.html` ✅ (Payouts/Invoices tabs) + 🟥 payment-methods tab USER builds.
**Redirects:** payout/invoice row → detail · "download" → file · "add method" → method form.
**Buttons:** **Download Invoice** → `GET /invoices/{id}/download` (file) · **Add Payment Method** → `POST /provider-payment-methods` · **Edit Method** → `PUT /provider-payment-methods/{id}` · **Delete Method** → `DELETE /provider-payment-methods/{id}` (confirm).
**Stack:** **Area** `Provider` · **Route** `/provider/finance` · **Cache** `NoStore` · **Perm** policy `Provider` + `WebPermission.Finance.Read` · **Rules** `R6 DataTables>500, PRINT2 invoice, CON3 JOD`

Tabs — **Payouts / Invoices / Methods**:
- `GET /payouts/provider` · `GET /payouts/{id}` *(self/admin scoped)*.
- `GET /invoices/provider/my-invoices` · `GET /invoices/{id}` · `/download`.
- `GET/POST/PUT/DELETE /provider-payment-methods[/{id}]`.

### 4.9 Documents
**Template:** 🟥 USER builds (reuse Dropzone §0.1 + a simple list).
**Redirects:** document row → preview/download · upload → §4.9 list (inline AJAX↑).
**Buttons:** **Upload Document** → `POST /booking/provider/documents` (AJAX↑) · **Replace** → `PUT /booking/provider/documents/{id}`.
**Stack:** **Area** `Provider` · **Route** `/provider/documents` · **Cache** `NoStore` · **Perm** policy `Provider` + `WebPermission.Provider.Documents` · **Rules** `AJAX↑ SEC4, SEC7`

`GET /booking/provider/documents` · `GET /booking/provider/documents/{id}` · `POST /booking/provider/documents` **`AJAX↑`** · `PUT /booking/provider/documents/{id}`.

### 4.10 Settings
**Template:** `agent-settings.html` ✅.
**Redirects:** save → stay (inline AJAX) · profile/account links → §3.10 shared settings.
**Buttons:** **Save Settings** → no dedicated write-endpoint exposed (read-only `GET /provider/settings`; persisted via §3.10 account endpoints) · account/security links → §3.10 (nav).
**Stack:** **Area** `Provider` · **Route** `/provider/settings` · **Cache** `NoStore` · **Perm** policy `Provider` + `WebPermission.Provider.Read` · **Rules** `read-only GET /provider/settings; writes via §3.10`

`GET /provider/settings`.

---

## §5 Tour Guide Dashboard

### 5.1 Overview `SSR`
**Template:** `agent-dashboard.html` ♻️ (reskinned for guide KPIs).
**Redirects:** KPI card → §5.4 / §5.7 · my-tours card → §5.7 Offering · pending → §5.9 / §5.10.
**Buttons:** **View Earnings** → §5.4 (nav) · **My Offerings** → §5.7 (nav) · **Find Tours to Join** → §2.5 / §5.9 (nav). Read-only KPIs, no write-endpoints.
**Stack:** **Area** `Guide` · **Route** `/guide/dashboard` · **Cache** `NoStore` · **Perm** policy `Guide` + `WebPermission.Guide.Read` · **Rules** `R2, API1, ApexCharts (read-only)`

`GET /guide/dashboard` · `GET /guide/my-tours` · `GET /guide/analytics`.

### 5.2 Profile
**Template:** 🟥 USER builds (guide-specific; reuse `account-profile.html` form + avatar/cover Dropzone §0.1).
**Redirects:** save → stay (inline) · "view public profile" → §2.7 Guide detail.
**Buttons:** **Save Profile** → `PUT /guides/{id}` (own id) · **Change Avatar** → `PUT /guides/me/avatar` (AJAX↑) · **Change Cover** → `PUT /guides/me/cover-image` (AJAX↑) · **Add/Remove Language** → `POST/DELETE /guides/{id}/languages[/{languageId}]` · **Add Specialization** → `POST /guides/{id}/specializations` · **Deactivate Profile** → `DELETE /guides/me` (confirm).
**Stack:** **Area** `Guide` · **Route** `/guide/profile` · **Cache** `NoStore` · **Perm** policy `Guide` + `WebPermission.Guide.Write` · **Rules** `F1-F3, avatar/cover AJAX↑ SEC4, ST1, public → §2.7, SEC7`

`GET /guides/me` · `PUT /guides/{id}` (owner) · `PUT /guides/me/avatar` · `PUT /guides/me/cover-image` · languages `POST/DELETE /guides/{id}/languages[/{languageId}]` · `POST /guides/{id}/specializations` · `DELETE /guides/me` (self-deactivate).

### 5.3 Analytics
**Template:** `agent-dashboard.html` ♻️ (ApexCharts panels reused).
**Redirects:** chart drill → §5.7 Offering / §5.4 Earnings.
**Buttons:** drill-down → §5.7 / §5.4 (nav). Read-only analytics, no write-endpoints.
**Stack:** **Area** `Guide` · **Route** `/guide/analytics` · **Cache** `NoStore` · **Perm** policy `Guide` + `WebPermission.Guide.Read` · **Rules** `ApexCharts read-only`

`GET /guides/me/analytics/overview` · `/booking-trends` · `/peak-days` · `/popular-tours`.

### 5.4 Earnings
**Template:** `agent-earnings.html` ♻️.
**Redirects:** earning row → §5.7 source offering / tour · "by tour" drill → §5.7.
**Buttons:** **By Tour** → §5.7 (nav) · earning row → source offering (nav). Read-only earnings, no write-endpoints.
**Stack:** **Area** `Guide` · **Route** `/guide/earnings` · **Cache** `NoStore` · **Perm** policy `Guide` + `WebPermission.Finance.Read` · **Rules** `R6 DataTables, PRINT2, CON3 JOD`

`GET /guides/me/earnings/summary` · `/history` · `/by-tour`. *(Finance-module equivalents: `GET /finance/guide/summary`, `GET /finance/guide`.)*

### 5.5 Tier
**Template:** 🟥 USER builds (small status widget; no template page).
**Redirects:** "how tiers work" → static help · progress → §5.3 Analytics.
**Buttons:** **View Analytics** → §5.3 (nav). Read-only tier widget, no write-endpoints.
**Stack:** **Area** `Guide` · **Route** `/guide/tier` · **Cache** `NoStore` · **Perm** policy `Guide` + `WebPermission.Guide.Read` · **Rules** `read-only widget`

`GET /guides/me/tier`.

### 5.6 Availability
**Template:** 🟥 USER builds (calendar; reuse Flatpickr §0.x).
**Redirects:** block add/remove → stay (inline AJAX) · conflict → §5.7 offering schedule.
**Buttons:** **Add Availability Block** → `POST /guides/me/availability-blocks` · **Remove Block** → `DELETE /guides/me/availability-blocks/{id}`.
**Stack:** **Area** `Guide` · **Route** `/guide/availability` · **Cache** `NoStore` · **Perm** policy `Guide` + `WebPermission.Guide.Write` · **Rules** `Flatpickr, ST1, SEC7`

`GET/POST/DELETE /guides/me/availability-blocks[/{id}]`.

### 5.7 My Offerings
**Template:** 🟥 USER builds (no template page; reuse `add-listing.html` editor for pricing-tiers/schedules sub-forms).
**Redirects:** offering row → offering editor · "parent tour" → §2.5 Tour detail · save → §5.7 list.
**Buttons:** (own guideId resolved from `guides/me`) **Add/Save/Remove Pricing Tier** → `POST/PUT/DELETE /tours/{tourId}/guide-offerings/{guideId}/pricing-tiers[/{tierId}]` · **Add/Remove Schedule** → `POST/PUT/DELETE .../schedules[/{scheduleId}]` · **Enable/Disable Private Tour** → `POST/DELETE .../private-tour` · **Remove Offering** → `DELETE /tours/{tourId}/guide-offerings/{guideId}` (confirm).
**Stack:** **Area** `Guide` · **Route** `/guide/offerings` · **Cache** `NoStore` · **Perm** policy `Guide` + `WebPermission.Guide.Offerings` · **Rules** `resolve guideId via GET /guides/me, C3 evict tour:{tourId}, ST1, SEC7`

> **guideId is the real id, not `me`** (see §11). Resolve own guideId from `guides/me` first.

`GET /tours/{tourId}/guide-offerings/{guideId}` · `/pricing-tiers` · `/schedules`. Mutations: `POST/DELETE /tours/{tourId}/guide-offerings/{guideId}/pricing-tiers[/{tierId}]` · `PUT /.../pricing-tiers/{tierId}` · `POST/DELETE /.../schedules[/{scheduleId}]` · `PUT /.../schedules/{scheduleId}` · `POST/DELETE /.../private-tour` · `DELETE /tours/{tourId}/guide-offerings/{guideId}` (remove offering).

### 5.8 Discounts
**Template:** 🟥 USER builds (simple CRUD list; no template page).
**Redirects:** discount row → edit form · save/delete → §5.8 list (inline AJAX).
**Buttons:** **New Discount** → `POST /booking/guide-discounts` · **Save** → `PUT /booking/guide-discounts/{id}` · **Delete** → `DELETE /booking/guide-discounts/{id}` (confirm).
**Stack:** **Area** `Guide` · **Route** `/guide/discounts` · **Cache** `NoStore` · **Perm** policy `Guide` + `WebPermission.Booking.Discounts` · **Rules** `F8 delete, SEC7`

`GET /booking/guide-discounts/mine` · `POST /booking/guide-discounts` · `PUT/DELETE /booking/guide-discounts/{id}`.

### 5.9 Applications & Proposals
**Template:** 🟥 USER builds (list + proposal form; no template page).
**Redirects:** "apply to tour" → §2.5 Tour detail (target) · application/proposal row → its detail · submit → §5.9 list.
**Buttons:** **Apply to Tour** → `POST /tours/{tourId}/applications` · **New Proposal** → `POST /tours/proposals` · **Submit Proposal** → `POST /tours/proposals/{id}/submit`.
**Stack:** **Area** `Guide` · **Route** `/guide/proposals` · **Cache** `NoStore` · **Perm** policy `Guide` + `WebPermission.Guide.Apply` · **Rules** `D1 paging, SEC7`

`GET /guides/me/applications` · `POST /tours/{tourId}/applications` (apply) · `GET /tours/proposals?guideId=` · `GET /tours/proposals/{id}` · `POST /tours/proposals` · `POST /tours/proposals/{id}/submit`.

### 5.10 Join Requests
**Template:** 🟥 USER builds (request list; no template page).
**Redirects:** request row → §3.3-style booking context · approve/reject → stay (inline AJAX).
**Buttons:** **Approve** → `POST /booking/join-requests/{id}/approve` · **Reject** → `POST /booking/join-requests/{id}/reject`.
**Stack:** **Area** `Guide` · **Route** `/guide/join-requests` · **Cache** `NoStore` · **Perm** policy `Guide` + `WebPermission.Booking.JoinRequests` · **Rules** `NF1, SEC7`

`GET /booking/join-requests?bookingId=` · `POST /booking/join-requests/{id}/approve` · `/reject`.

### 5.11 Agency
**Template:** 🟥 USER builds (invitations + apply panel; no template page).
**Redirects:** invitation accept/decline → §5.11 (current agency) · "browse agencies" → §2.8 · leave agency → §5.11.
**Buttons:** **Accept Invitation** → `POST /guides/invitations/{id}/accept` · **Decline** → `POST /guides/invitations/{id}/decline` · **Apply to Agency** → `POST /guides/agencies/{agencyUserId}/apply` · **Leave Agency** → `DELETE /guides/me/agency` (confirm).
**Stack:** **Area** `Guide` · **Route** `/guide/agency` · **Cache** `NoStore` · **Perm** policy `Guide` + `WebPermission.Guide.Agency` · **Rules** `F8 Leave Agency, browse → §2.8, SEC7`

`GET /guides/me/invitations` · `POST /guides/invitations/{id}/accept` · `/decline` · `GET /agency` · `GET /agency/{agencyUserId}` · `POST /guides/agencies/{agencyUserId}/apply` · `DELETE /guides/me/agency`.

---

## §6 Agency Dashboard

### 6.1 Roster
**Template:** 🟥 USER builds (no template; reuse `agent-listings.html` list shell).
**Redirects:** guide row → §2.7 Guide detail · remove → stay (inline AJAX).
**Buttons:** **Remove Guide** → `DELETE /agency/guides/{guideUserId}` (confirm) · **View Guide** → §2.7 (nav) · **Recruit Guides** → §6.4 (nav).
**Stack:** **Area** `Admin` (agency-hosted) · **Route** `/agency/roster` · **Cache** `NoStore` · **Perm** `WebPermission.Agency.Read` (Agency role) · **Rules** `R2, D1, F8 Remove Guide, A11Y5, SEC7`

`GET /agency/guides` · `DELETE /agency/guides/{guideUserId}`.

### 6.2 Applications
**Template:** 🟥 USER builds (no template; review-queue list).
**Redirects:** application row → applicant §2.7 Guide detail · approve → §6.1 Roster · reject → stay.
**Buttons:** **Approve** → `POST /agency/applications/{id}/approve` · **Reject** → `POST /agency/applications/{id}/reject` · **View Applicant** → §2.7 (nav).
**Stack:** **Area** `Admin` (agency-hosted) · **Route** `/agency/applications` · **Cache** `NoStore` · **Perm** `WebPermission.Agency.Manage` · **Rules** `R2, D1, NF1, SEC7`

`GET /agency/applications` · `POST /agency/applications/{id}/approve` · `/reject`.

### 6.3 Invitations
**Template:** 🟥 USER builds (no template; sent-invites list).
**Redirects:** invite row → invited §2.7 Guide detail · "invite more" → §6.4 Recruit.
**Buttons:** **Invite More** → §6.4 (nav) · **View Guide** → §2.7 (nav). *(Read-only list; no write-endpoint for resend/revoke.)*
**Stack:** **Area** `Admin` (agency-hosted) · **Route** `/agency/invitations` · **Cache** `NoStore` · **Perm** `WebPermission.Agency.Read` · **Rules** `read-only, D1, L6`

`GET /agency/invitations/sent`.

### 6.4 Recruit
**Template:** 🟥 USER builds (no template; searchable available-guides grid).
**Redirects:** guide card → §2.7 Guide detail · invite → §6.3 Invitations (inline AJAX).
**Buttons:** **Invite Guide** → `POST /agency/guides/invite` · **View Guide** → §2.7 (nav).
**Stack:** **Area** `Admin` (agency-hosted) · **Route** `/agency/recruit` · **Cache** `NoStore` · **Perm** `WebPermission.Agency.Manage` · **Rules** `R2, S1 AJAX filter, D1, NF1, SEC7`

`GET /agency/guides/available` · `POST /agency/guides/invite`.

---

## §7 Content Creator Dashboard

### 7.1 My Blogs
**Template:** 🟥 USER builds (no template; reuse `agent-listings.html` list shell + status filter).
**Redirects:** blog row → §7.2 Blog Editor · "new post" → §7.2 · publish → §2.9 public post · status action → stay (inline).
**Buttons:** **New Post** → `POST /blogs` (Draft) → §7.2 · **Submit for Review** → `POST /blogs/{id}/submit-for-review` · **Publish** → `/publish` · **Unpublish** → `/unpublish` · **Archive** → `/archive` · **Delete** → `DELETE /blogs/{id}` (confirm) · **Restore** → `POST /blogs/{id}/restore` · **Edit** → §7.2 (nav).
**Stack:** **Area** `Creator` · **Route** `/creator/blogs` · **Cache** `NoStore` · **Perm** `WebPermission.Blogs.Write` · **Rules** `R2, D1 status filter, status badges, F8 delete, C3 evict blog/homepage, NF1, L6, SEC7`

`GET /blogs/my-blogs?status=` · `POST /blogs` (Draft) · `POST /blogs/{id}/submit-for-review` · `/publish` · `/unpublish` · `/archive` · `DELETE /blogs/{id}` + `POST /blogs/{id}/restore`.

### 7.2 Blog Editor
**Template:** 🟥 USER builds (reuse Quill editor from `add-listing.html` + `blog-detail.html` layout; cover via Dropzone §0.1).
**Redirects:** save → §7.1 My Blogs · submit-for-review → §7.1 (pending) · "preview" → §2.9 post · linked-tour chip → §2.5.
**Buttons:** **Save** → `PUT /blogs/{id}` · **Upload Cover** → attachments (§0.1, `AJAX↑`) · **Link Tour** → `POST /blogs/{id}/tours` · **Unlink Tour** → `DELETE /blogs/{id}/tours/{tourId}` · **Submit for Review** → `POST /blogs/{id}/submit-for-review` · **Preview** → §2.9 (nav).
**Stack:** **Area** `Creator` · **Route** `/creator/blogs/{id}/edit` · **Cache** `NoStore` · **Perm** `WebPermission.Blogs.Write` · **Rules** `Quill J1, F5 autosave-30s, F9, ST1, SEC3 sanitized, cover AJAX↑ SEC4, C3 evict blog:{id}, SEC7`

`GET /blogs/{id}` (or admin `GET /blogs/admin/{id}`) · `PUT /blogs/{id}` · link tours `POST /blogs/{id}/tours` · `DELETE /blogs/{id}/tours/{tourId}` · cover via attachments (§0.1).

### 7.3 Creator Profile
**Template:** 🟥 USER builds (reuse `account-profile.html` form + avatar Dropzone §0.1).
**Redirects:** save → stay (inline) · "view public profile" → §2.9 Creator profile.
**Buttons:** **Save Profile** → `PUT /blogs/creators/profile/mine` · **Change Avatar** → `PUT /blogs/creators/profile/mine/avatar` (`AJAX↑`) · **Delete Profile** → `DELETE /blogs/creators/profile/mine` (confirm) · **View Public** → §2.9 (nav).
**Stack:** **Area** `Creator` · **Route** `/creator/profile` · **Cache** `NoStore` · **Perm** `WebPermission.Creator.Write` · **Rules** `F1-F3, avatar AJAX↑ SEC4, ST1, F8 delete, SEC7`

`GET/PUT/DELETE /blogs/creators/profile/mine` · `PUT /blogs/creators/profile/mine/avatar` · `GET /blogs/creators/niches`.

### 7.4 Audience
**Template:** 🟥 USER builds (followers + comment-moderation lists).
**Redirects:** follower row → that user's public presence · comment → §2.9 source post · moderate → stay (inline).
**Buttons:** **Edit Comment** → `PUT /blogs/comments/{commentId}` (owner ≤30 min) · **Delete Comment** → `DELETE /blogs/comments/{commentId}` (confirm) · **React** → `POST /blogs/comments/{commentId}/reactions` · **Unreact** → `DELETE .../reactions`.
**Stack:** **Area** `Creator` · **Route** `/creator/audience` · **Cache** `NoStore` · **Perm** `WebPermission.Creator.Read` · **Rules** `R2, D1, comment →30min, F8 delete, NF6 react, SEC7`

`GET /blogs/creators/profiles/{profileId}/followers` · `GET /blogs/{id}/comments` · `PUT/DELETE /blogs/comments/{commentId}` (owner ≤30 min) · comment reactions `POST/DELETE /blogs/comments/{commentId}/reactions`.

### 7.5 Application
**Template:** 🟥 USER builds (application form + status; reuse `join-us.html` acquisition pattern).
**Redirects:** submit → §7.5 (pending state) · approved → §7.1 My Blogs · redeem invitation → §7.3 Creator Profile.
**Buttons:** **Start Application** → `POST /blogs/creators/applications` · **Save Draft** → `PUT /blogs/creators/applications/{applicationId}` · **Submit** → `POST /blogs/creators/applications/{applicationId}/submit` · **Redeem Invitation** → `POST /blogs/creators/invitations/redeem`.
**Stack:** **Area** `Creator` · **Route** `/creator/application` · **Cache** `NoStore` · **Perm** `[Authorize]` + `WebPermission.Creator.Apply` · **Rules** `F1-F3, F5 draft, status badge, L6, NF1, SEC7`

`GET /blogs/creators/applications/mine` · `POST /blogs/creators/applications` · `PUT /blogs/creators/applications/{applicationId}` · `POST /blogs/creators/applications/{applicationId}/submit` · `POST /blogs/creators/invitations/redeem`.

---

## §8 Admin Dashboard

### 8.1 Overview `SSR`
**Template:** `admin-dashboard.html` ✅ (KPIs $/bookings/users).
**Redirects:** KPI card → §8.7 Finance / §8.13 Users / §8.4 Tours · trending/popular → §2.5/§2.3.
**Buttons:** read-only KPIs; **drill-in** cards → §8.7 / §8.13 / §8.4 (nav). *(No write-endpoints.)*
**Stack:** **Area** `Admin` · **Route** `/admin/dashboard` · **Cache** `NoStore` · **Perm** policy `Admin` + `WebPermission.Admin.Read` · **Rules** `R2, API1 Task.WhenAll, ApexCharts (read-only)`

`GET /admin/dashboard` · `/dashboard/bookings` · `/dashboard/revenue` · `/dashboard/users` · `GET /trending` · `GET /popular/tours|places|businesses`.

### 8.2 Moderation
**Template:** `admin-reviews.html` ✅ (reviews/reports queue; reuse for moderation logs).
**Redirects:** report/review row → reported entity (§2.x) or user (§8.13) · resolve/approve/remove → stay (inline).
**Buttons:** **Resolve Report** → `POST /social/reports/admin/{id}/resolve` · **Approve Review** → `POST /social/reviews/admin/{id}/approve` · **Remove Review** → `/remove` · **Warn User** → `POST /social/moderation/warn` · **Ban User** → `/ban` · **Unban** → `DELETE /social/moderation/ban/{userId}`.
**Stack:** **Area** `Admin` · **Route** `/admin/moderation` · **Cache** `NoStore` · **Perm** policy `Admin` + `WebPermission.Moderation.Manage` · **Rules** `D1, F8 Ban/Remove, RT5 SignalR badge, A11Y5, NF1, SEC7`

`GET /social/reports/admin` · `POST /social/reports/admin/{id}/resolve` · `GET /social/reviews/admin/flagged` · `POST /social/reviews/admin/{id}/approve` · `/remove` · `GET /social/moderation/logs` · `POST /social/moderation/warn` · `/ban` · `DELETE /social/moderation/ban/{userId}`.

### 8.3 Providers
**Template:** `admin-agent-list.html` ✅ (list) · `admin-agent-detail.html` ✅ (detail).
**Redirects:** provider row → §8.3 detail · approve/reject/suspend/reinstate → stay (inline) · "their listings" → §8.4 / §8.5.
**Buttons:** **Approve** → `POST /admin/providers/{id}/approve` · **Reject** → `/reject` · **Request Docs** → `/request-docs` · **Suspend** → `/suspend` · **Reinstate** → `/reinstate`.
**Stack:** **Area** `Admin` · **Route** `/admin/providers`, `/{id}` · **Cache** `NoStore` · **Perm** policy `Admin` + `WebPermission.Providers.Manage` · **Rules** `R2, D1, F8, status badges, SEC7`

`GET /admin/providers` · `GET /admin/providers/{id}` · `POST /admin/providers/{id}/approve` · `/reject` · `/request-docs` · `/suspend` · `/reinstate`.

### 8.4 Tours Review
**Template:** 🟥 USER builds (reuse `admin-agent-list.html` queue shell + status filter).
**Redirects:** tour row → §2.5 Tour detail (admin view) · approve/reject/suspend/feature → stay (inline) · proposals/packages → their queues.
**Buttons:** **Approve** → `POST /tours/admin/{id}/approve` · **Reject** → `/reject` · **Suspend** → `/suspend` · **Reinstate** → `/reinstate` · **Feature** → `PATCH /tours/admin/{id}/feature` · **Approve/Reject Proposal** → `POST /tours/proposals/{id}/approve|reject` · **Approve/Reject Package** → `POST /tours/packages/{id}/approve|reject` · **Suspend/Reinstate Offering** → `POST /tours/{tourId}/guide-offerings/{guideId}/suspend|reinstate`.
**Stack:** **Area** `Admin` · **Route** `/admin/tours` · **Cache** `NoStore` · **Perm** policy `Admin` + `WebPermission.Tours.Moderate` · **Rules** `R2, D1 status filter, F8, C3 evict tour:{id}, row → §2.5, SEC7`

`GET /tours/admin?status=` · `POST /tours/admin/{id}/approve` · `/reject` · `/suspend` · `/reinstate` · `PATCH /tours/admin/{id}/feature`. Proposals queue: `GET /tours/proposals` (no guideId = admin pending) · `POST /tours/proposals/{id}/approve` · `/reject`. Packages: `POST /tours/packages/{id}/approve` · `/reject`. Guide offering moderation: `POST /tours/{tourId}/guide-offerings/{guideId}/suspend` · `/reinstate`.

### 8.5 Places & Businesses
**Template:** 🟥 USER builds (reuse `admin-agent-list.html` queue + `add-listing.html` editor for places CRUD).
**Redirects:** place/business row → §2.3 / §2.4 detail (admin view) · feature/verify/approve/suspend → stay (inline) · map ops → map editor.
**Buttons:** **New/Save/Delete Place** → `POST/PUT/DELETE /places[/{id}]` · **Feature** → `PATCH /places/{id}/feature` · **Verify** → `/verify` · **Set/Remove Accessibility** → `PUT /places/{id}/accessibility` / `DELETE /places/admin/accessibility/{assignmentId}` · **Approve/Reject/Suspend/Reinstate/Request-More-Docs Business** → `POST /places/businesses/admin/{id}/approve|reject|suspend|reinstate|request-more-docs` · **Delete Business** → `DELETE /places/businesses/{id}`.
**Stack:** **Area** `Admin` · **Route** `/admin/places`, `/admin/businesses` · **Cache** `NoStore` · **Perm** policy `Admin` + `WebPermission.Places.Manage` · **Rules** `R2, D1, F8 Delete, C3 evict place:{id}/business:{id}, MAP3, SEC7`

- Places: `GET/POST/PUT/DELETE /places[/{id}]` · `PATCH /places/{id}/feature` · `/verify` · `PUT /places/{id}/accessibility` · `DELETE /places/admin/accessibility/{assignmentId}` · `GET /places/accessibility/catalog`.
- Business moderation: `POST /places/businesses/admin/{id}/approve` · `/reject` · `/suspend` · `/reinstate` · `/request-more-docs` · `DELETE /places/businesses/{id}`.

### 8.6 Blogs & Creators
**Template:** 🟥 USER builds (reuse `admin-agent-list.html` queue layout + inline action menu).
**Redirects:** blog/creator row → §2.9 post / creator profile (admin view) · approve/reject/remove/feature/hide → stay (inline) · restore → deleted tab.
**Buttons:** **Approve/Reject/Remove Blog** → `POST /blogs/admin/{id}/approve|reject|remove` · **Feature/Unfeature** → `POST /blogs/{id}/feature|unfeature` · **Hide/Unhide** → `/hide|unhide` · **Restore** → `POST /blogs/{id}/restore` · **Save Translation** → `PUT /blogs/admin/{id}/translations/{languageCode}` · **Approve/Reject/Request-Info Creator App** → `POST /blogs/admin/creators/applications/{id}/approve|reject|request-more-info` · **Promote/Demote/Suspend/Reinstate Creator** → `POST /blogs/admin/creators/profiles/{profileId}/promote|demote|suspend|reinstate` · **Edit Creator Profile** → `PUT /blogs/admin/creators/profiles/{id}` · **Delete Creator Profile** → `DELETE /blogs/admin/creators/profiles/{id}` · **Send Invitation** → `POST /blogs/admin/creators/invitations`.
**Stack:** **Area** `Admin` · **Route** `/admin/blogs` · **Cache** `NoStore` · **Perm** policy `Admin` + `WebPermission.Blogs.Moderate` · **Rules** `R2, D1, F8, C3 evict blog:{id}/homepage, SEC3 sanitized, SEC7`

- Blog queue: `GET /blogs/admin/queue` · `GET /blogs/admin/{id}` · `POST /blogs/admin/{id}/approve` · `/reject` · `/remove`.
- Visibility/feature: `POST /blogs/{id}/feature` · `/unfeature` · `/hide` · `/unhide`.
- Deleted: `GET /blogs/admin/deleted` · `POST /blogs/{id}/restore`.
- Translations: `GET /blogs/admin/{id}/translations` · `/translations/{languageCode}` · `PUT /blogs/admin/{id}/translations/{languageCode}`.
- Creator apps: `GET /blogs/admin/creators/applications[/{id}]` · `POST .../{id}/approve` · `/reject` · `/request-more-info`.
- Creator profiles: `GET/PUT /blogs/admin/creators/profiles/{id}` · `POST .../{profileId}/promote` · `/demote` · `/suspend` · `/reinstate` · `DELETE /blogs/admin/creators/profiles/{id}`.
- Invitations: `POST /blogs/admin/creators/invitations`.

### 8.7 Finance Ops
**Template:** `admin-booking-list.html` ♻️ + `admin-booking-detail.html` ♻️ + `admin-earnings.html` ✅.
**Redirects:** payment/payout row → booking detail · refund/approve/trigger → stay (inline) · dispute → dispute review pane · payout → payout detail.
**Buttons:** **Refund** → `POST /payments/{id}/refund` · **Approve Payout** → `POST /payouts/{id}/approve` · **Trigger Sweep** → `POST /payouts/admin/trigger` · **Verify Method** → `POST /provider-payment-methods/{id}/verify` · **Review/Escalate/Resolve Dispute** → `POST /disputes/{id}/review|escalate|resolve` · **Resolve Booking Dispute** → `POST /booking/admin/{id}/dispute/resolve` · **Force Refund** → `POST /admin/bookings/{id}/force-refund` · **New/Save/Delete Commission** → `POST /commissions` / `PUT|DELETE /commissions/{id}`.
**Stack:** **Area** `Admin` · **Route** `/admin/finance` · **Cache** `NoStore` · **Perm** policy `Admin` + `WebPermission.Finance.Manage` · **Rules** `R6 DataTables, D1, F8 Refund/Force-Refund, PAY2 idempotency, CON3 JOD, SEC7`

- `GET /finance/admin/dashboard`.
- Commissions: `GET/POST /commissions` · `PUT/DELETE /commissions/{id}`.
- Payments: `GET /payments/admin/all` · `POST /payments/{id}/refund`.
- Payouts: `GET /payouts/admin/pending` · `POST /payouts/{id}/approve` · `POST /payouts/admin/trigger`.
- Payment methods: `POST /provider-payment-methods/{id}/verify`.
- Disputes: `GET /disputes/admin/open` · `POST /disputes/{id}/review` · `/escalate` · `/resolve`.
- Booking finance: `POST /booking/admin/{id}/dispute/resolve` · `POST /admin/bookings/{id}/force-refund` · `GET /booking/admin/all`.

### 8.8 Growth & Merchandising
**Template:** 🟥 USER builds (no template page; reuse ApexCharts dashboards + tabbed CRUD).
**Redirects:** boost/pin/photogenic target → §2.5 tour / §2.3 place (entity preview) · experiments/segments/seasonality → stay (inline).
**Buttons:** **Refresh Batch** → `POST /analytics/admin/batches/refresh` · **Add/Remove Boost** → `POST /analytics/admin/boosts` (+ `/boosts/cpc`) / `DELETE .../boosts/{boostId}` · **Add/Remove Pin** → `POST /analytics/admin/pins` / `DELETE .../pins/{pinId}` · **Add/Remove Seasonality** → `POST /analytics/admin/seasonality` / `DELETE .../{ruleId}` · **New Experiment / Start / Complete** → `POST /analytics/admin/experiments` / `PUT .../{experimentId}/start|complete` · **Add Holiday** → `POST /analytics/admin/holidays` · **Toggle Photogenic** → `PUT /analytics/admin/entities/{kind}/{entityId}/photogenic`.
**Stack:** **Area** `Admin` · **Route** `/admin/growth` · **Cache** `NoStore` · **Perm** policy `Admin` + `WebPermission.Growth.Manage` · **Rules** `R2, D1, ApexCharts, C3 evict target/homepage, NF1, SEC7`

`GET /analytics/admin/metrics` · batches `GET /analytics/admin/batches` + `POST /analytics/admin/batches/refresh` · boosts `POST /analytics/admin/boosts` + `/boosts/cpc` + `DELETE /analytics/admin/boosts/{boostId}` · pins `POST /analytics/admin/pins` + `DELETE /analytics/admin/pins/{pinId}` · seasonality `GET/POST /analytics/admin/seasonality` + `DELETE .../{ruleId}` · experiments `POST /analytics/admin/experiments` + `PUT .../{experimentId}/start` + `/complete` · holidays `GET /analytics/admin/holidays/{year}` + `POST /analytics/admin/holidays` · segments `GET /analytics/admin/segments` · `PUT /analytics/admin/entities/{kind}/{entityId}/photogenic`.

### 8.9 Content Operations
**Template:** 🟥 USER builds (no template page; tabbed CRUD tables + Choices.js multiselects).
**Redirects:** all CRUD / reorder / activate / translate → stay (inline mutations).
**Buttons:** (per tab) **New/Save/Delete** category|tag|language|specialization → `POST/PUT/DELETE /content-core/{kind}[/{id}]` · **Reorder** → `PUT /content-core/categories/reorder` · **Activate/Deactivate/Restore** → `PATCH .../{id}/activate|deactivate|restore` · **Translate / Batch / Approve / Backfill** → `POST /content-core/translations/translate|batch` · `POST .../{id}/approve` · `/approve-batch` · `/backfill/{entityKind}` · **Save Translation** → `PUT /content-core/translations/{id}` · **Assign/Unassign** → `POST/DELETE /content-core/entity-categories|entity-tags`.
**Stack:** **Area** `Content` · **Route** `/content/categories|tags|languages|specializations|translations|attachments` · **Cache** `NoStore` · **Perm** policy `Admin` + `WebPermission.Content.Manage` · **Rules** `lazy tabs, drag reorder, C3 evict category:tree/lookups (categories 1h/languages 24h), D1, RTL, SEC7`

Tabs — **Categories / Tags / Languages / Specializations / Translations / Attachments**:
- Categories: `GET /content-core/categories/admin[/{id}]` + `POST/PUT/DELETE /content-core/categories[/{id}]` + `PUT /content-core/categories/reorder` + `PATCH .../{id}/activate|deactivate|restore`.
- Tags: `GET/POST/PUT/DELETE /content-core/tags[/{id}]` + `PATCH .../{id}/activate|deactivate`.
- Languages: `GET/POST/PUT/DELETE /content-core/languages[/{id}]` + `PATCH .../{id}/activate|deactivate`.
- Specializations: `GET/POST/PUT/DELETE /content-core/specializations[/{id}]` + `PATCH .../{id}/activate|deactivate`.
- Translations: `POST /content-core/translations/translate` · `/batch` · `PUT /content-core/translations/{id}` · `POST /content-core/translations/{id}/approve` · `/approve-batch` · `/backfill/{entityKind}`.
- Entity assignments: `GET/POST/DELETE /content-core/entity-categories` · `GET/POST/DELETE /content-core/entity-tags`.
- Attachments admin (§0.1).

### 8.10 SEO Console
**Template:** 🟥 USER builds (no template page; entityType picker bound to the 6-value `SeoEntityType` {Place, Tour, Business, Blog, TourGuide, Creator}).
**Redirects:** metadata/FAQ row → §2.x detail preview for that entityType · redirects/sitemap/weather-cache → stay (inline).
**Buttons:** **New/Save/Delete Metadata** → `POST /seo/metadata` / `PUT|DELETE /seo/metadata/{id}` · **New/Save/Delete FAQ** → `POST /seo/faq` / `PUT|DELETE /seo/faq/{id}` · **Reorder FAQ** → `PUT /seo/faq/reorder` · **New/Save/Delete Redirect** → `POST /seo/redirects` / `PUT|DELETE /seo/redirects/{id}` · **Edit/Delete Sitemap Entry** → `PATCH|DELETE /seo/sitemap/entries/{id}` · **Regenerate Sitemap** → `POST /seo/sitemap/regenerate` · **Clear Weather Cache** → `DELETE /seo/weather/cache/{id}` · **Reset Budget** → `PUT /seo/weather/budget/reset` · **Refresh Weather** → `POST /seo/weather/refresh/{placeId}`. *(entityType picker limited to the 6 `SeoEntityType` values.)*
**Stack:** **Area** `Content` · **Route** `/content/seo` (tabs metadata|faq|redirects|sitemap|weather) · **Cache** `NoStore` · **Perm** policy `Admin` + `WebPermission.Seo.Manage` · **Rules** `entityType picker = 6 SeoEntityType values, FAQ reorder, drives §3 SEO/C5 ETag, Save → §2.x preview, D1, SEC7`

Tabs — **Metadata / FAQ / Redirects / Sitemap / WeatherCache**:
- **`entityType` selector** (Metadata + FAQ forms) is bound to the `SeoEntityType` enum — the only 6 valid values: `Place`, `Tour`, `Business`, `Blog`, `TourGuide`, `Creator` (see §2.0). The picker must not allow any other entity type.
- Metadata: `GET /seo/metadata` · `POST /seo/metadata` · `PUT/DELETE /seo/metadata/{id}`.
- FAQ: `GET /seo/faq` · `POST /seo/faq` · `PUT/DELETE /seo/faq/{id}` · `PUT /seo/faq/reorder`.
- Redirects: `GET/POST /seo/redirects` · `PUT/DELETE /seo/redirects/{id}`.
- Sitemap: `GET /seo/sitemap/entries` · `PATCH /seo/sitemap/entries/{id}` · `DELETE /seo/sitemap/entries/{id}` · `POST /seo/sitemap/regenerate`.
- WeatherCache: `DELETE /seo/weather/cache/{id}` · `PUT /seo/weather/budget/reset` · `POST /seo/weather/refresh/{placeId}`.

### 8.11 Support
**Template:** 🟥 USER builds (reuse `help-center.html` / `help-detail.html` shell for ticket thread).
**Redirects:** ticket row → ticket thread · assign/resolve/close → stay (inline).
**Buttons:** **Assign** → `POST /support/admin/tickets/{id}/assign` · **Resolve** → `/resolve` · **Reply** → `POST /support/tickets/{id}/messages` · **Close** → `/close`.
**Stack:** **Area** `Admin` · **Route** `/admin/support`, `/{id}` · **Cache** `NoStore` · **Perm** policy `Admin` + `WebPermission.Support.Manage` · **Rules** `R2, D1, NF1, L6, SEC7`

`GET /support/tickets[/{id}]` · `POST /support/admin/tickets/{id}/assign` · `/resolve` · `POST /support/tickets/{id}/messages` · `/close`.

### 8.12 Platform Ops
**Template:** 🟥 USER builds (no template page; ops table + replay actions).
**Redirects:** dead-letter row → dead-letter detail · replay/backfill → stay (inline) · notification-template row → template editor.
**Buttons:** **Replay** → `POST /ops/outbox/dead-letters/{module}/{id}/replay` · **Backfill Snapshots** → `POST /ops/content-tours/backfill/tour-snapshots` · **New/Save/Delete Template** → `POST /admin/notification-templates` / `PUT|DELETE /admin/notification-templates/{id}` *(edit opens from list row — no GET-by-id, §11)*.
**Stack:** **Area** `Admin` · **Route** `/admin/ops` · **Cache** `NoStore` · **Perm** policy `Admin` + `WebPermission.Ops.Manage` (SuperAdmin-tier) · **Rules** `R6 DataTables, F8 Replay/Backfill, template edit from list row (no GET-by-id), ERR4 correlation, SEC7`

`GET /ops/outbox/dead-letters` · `POST /ops/outbox/dead-letters/{module}/{id}/replay` · `POST /ops/content-tours/backfill/tour-snapshots`. Notification templates: `GET /admin/notification-templates` · `POST /admin/notification-templates` · `PUT/DELETE /admin/notification-templates/{id}` *(no GET-by-id; edit from the list row — see §11)*.

### 8.13 Users & Audit
**Template:** `admin-guest-list.html` ✅ + `admin-guest-detail.html` ✅ + `admin-settings.html` ♻️.
**Redirects:** user row → §8.13 guest detail · suspend/reactivate/archive/reset-password → stay (inline) · audit → export download · invitations → invite modal.
**Buttons:** **Suspend/Reactivate/Archive** → `PATCH /auth/admin/users/{userId}/suspend|reactivate|archive` · **Reassign** → `POST .../reassign` · **Reset Password** → `/reset-password` · **Revoke Sessions** → `DELETE /auth/admin/users/{userId}/sessions` · **Redact Audit** → `POST /admin/audit-logs/{id}/redact` · **Export Audit** → `GET /admin/audit-logs/export` (nav/download) · **Invite User** → `POST /auth/invitations` · **Resend Invite** → `/invitations/resend`.
**Stack:** **Area** `Admin` · **Route** `/admin/users`, `/admin/audit` · **Cache** `NoStore` · **Perm** policy `Admin` + `WebPermission.Users.Manage` · **Rules** `R6 DataTables, IAsyncEnumerable export>10K, D1, F8 Suspend/Archive/Reset/Revoke, ST1, SEC7`

- Audit: `GET /admin/audit-logs` · `/audit-logs/export` · `POST /admin/audit-logs/{id}/redact`.
- Interactions: `GET /admin/interactions` · `/interactions/user/{userId}`.
- User lifecycle: `PATCH /auth/admin/users/{userId}/suspend` · `/reactivate` · `/archive` · `POST .../reassign` · `/reset-password` · `DELETE /auth/admin/users/{userId}/sessions`.
- Invitations: `GET /auth/invitations/roles` · `POST /auth/invitations` · `/invitations/accept` · `/invitations/resend`.

### 8.14 Tour Guides
**Template:** `admin-agent-detail.html` ♻️ (reuse provider-detail layout for guide profile + lifecycle) · `admin-agent-list.html` ♻️ (queue/list).
**Redirects:** guide row → §2.7 Guide detail (admin view) · suspend/reinstate/edit/delete → stay (inline) · their offerings → §8.4 Tours Review.
**Buttons:** **Edit Guide** → `PUT /guides/admin/{guideId}` · **Suspend** → `POST /guides/admin/{guideId}/suspend` · **Reinstate** → `POST /guides/admin/{guideId}/reinstate` · **Delete Guide** → `DELETE /guides/admin/{guideId}` *(confirm — deactivation, 60-day hard delete)*.
**Stack:** **Area** `Admin` · **Route** `/admin/guides`, `/{id}` · **Cache** `NoStore` · **Perm** policy `Admin` + `WebPermission.Guides.Manage` · **Rules** `R2 profile+private stats, D1, F8 Delete (60-day hard)/Suspend, A11Y5, SEC7`

- Admin guide management: `GET /guides/admin/{guideId}` (full profile w/ private stats) · `PUT /guides/admin/{guideId}` · `DELETE /guides/admin/{guideId}` · `POST /guides/admin/{guideId}/suspend` · `/reinstate`.

---

## §9 SuperAdmin RBAC Console
**Template:** 🟥 USER builds (no template page; roles/users tables + claims editor).
**Redirects:** role row → role detail (role-claims) · user row → user detail (user-roles / user-claims) · activate/deactivate → stay (inline) · audit → audit-logs view.
**Buttons:** **Create Role** → `POST /security/roles` · **Update Role** → `PATCH /security/roles/{roleId}` · **Deactivate Role** → `/deactivate` · **Add/Remove Role-Claim** → `POST /security/roles/{roleId}/claims` / `DELETE .../claims/{claimId}` · **Assign/Remove User-Role** → `POST /security/users/{userId}/roles` / `DELETE .../roles/{roleId}` · **Add/Remove User-Claim** → `POST /security/users/{userId}/claims` / `DELETE .../claims/{claimId}` · **Activate/Deactivate User** → `PATCH /security/users/{userId}/activate|deactivate`.
**Stack:** **Area** `Admin` (security/*, SuperAdmin-gated) · **Route** `/admin/security` (panes /roles /users /audit) · **Cache** `NoStore` · **Perm** policy `Admin` + `WebPermission.Security.Manage` (SuperAdmin-tier) · **Rules** `R2, D1 paging, ST1 RowVersion 409, F8 Deactivate Role/User, A11Y5 badge, NF1, R6 DataTables audit, SEC7, SEC2`

- Roles: `GET /security/roles` · `GET /security/roles/{roleId}` · `POST /security/roles` · `PATCH /security/roles/{roleId}` · `PATCH /security/roles/{roleId}/deactivate`.
- Role claims: `POST /security/roles/{roleId}/claims` · `DELETE /security/roles/{roleId}/claims/{claimId}`.
- Users: `GET /security/users` (paged) · `GET /security/users/{userId}`.
- User roles: `POST /security/users/{userId}/roles` · `DELETE /security/users/{userId}/roles/{roleId}`.
- User claims: `POST /security/users/{userId}/claims` · `DELETE /security/users/{userId}/claims/{claimId}`.
- Activation: `PATCH /security/users/{userId}/activate` · `/deactivate`.
- Audit: `GET /security/audit-logs`.

---

## §10 Cross-cutting Notes

- **Workspace switcher** for multi-role identities; nav = union of claim-allowed dashboards (`security/me`).
- **Concurrency / RowVersion:** management `PUT`s round-trip `RowVersion`; `409` → "reload, this changed" UX. Endpoints exposing RowVersion explicitly include `booking/availability/{tourId}/manage`, `blogs/admin/{id}`, `blogs/admin/deleted`.
- **Status as first-class UI:** Draft / PendingReview / Approved / Suspended / Rejected / Archived / Deleted / MoreDocsNeeded → badge + context action menu wired to the matching lifecycle POST/PATCH.
- **Inline-mutation pattern:** approve/reject/toggle endpoints return the updated resource; re-render the row + SweetAlert2 toast (no full reload).
- **Lazy tabs:** non-default tabs fetch on first activation.
- **Media is always a tab** (attachments, §0.1).
- **Weather is a widget**, Place-contextual (§0.2).
- **SignalR push** for notifications; counters as `AJAX⟳`.
- **Beacons** (`/blogs/{id}/views`, `/interactions`) fire-and-forget via `navigator.sendBeacon`.
- **Public-reads vs management-reads:** public convenience routes return approved/public-safe data only; owner/admin reads (`*/mine`, `*/admin/*`, `*/manage`) return all statuses.
- **Three "my-tours" surfaces are intentional** (see §11).

---

## §11 Ambiguities — Resolved Against Code

| # | Question | Resolution (from route table) |
|---|----------|-------------------------------|
| 1 | `{id}` ownership scoping on booking detail | `GET /booking/{id}` is explicitly **owner / provider / admin** scoped. ✓ |
| 2 | Payout read scoping | `GET /payouts/{id}` is **self / admin**. ✓ |
| 3 | Invoice read scoping | `GET /invoices/{id}` is buyer-scoped; provider list is separate (`/invoices/provider/my-invoices`). ✓ |
| 4 | `guideId` = `me` or real id in guide-offerings? | **Real guideId.** Routes are `/tours/{tourId}/guide-offerings/{guideId}/...` — resolve own id via `GET /guides/me` first. |
| 5 | "my-tours" variants | **Three distinct, all real:** `GET /guide/my-tours` (guide perf), `GET /provider/my-tours` (provider perf), `GET /tours/provider/my-tours` (provider, all statuses). |
| 6 | `GET /auth/external-providers` (list linked) | **Does not exist.** Only `POST /auth/external-providers` (link), `POST /auth/external-providers/login`, `DELETE /auth/external-providers/{providerId}`. Render linked list from `security/me` claims / app state. |
| 7 | Notification-template GET-by-id | **Does not exist.** Only `GET /admin/notification-templates` (list), `POST`, `PUT/{id}`, `DELETE/{id}`. Edit from the list row. |

### New endpoints surfaced by code (were missing from earlier drafts)
- `POST /booking/tour` — the actual booking-create entry point.
- `POST /payments/{bookingId}/simulate-success` — **DEV ONLY**, must never appear in production UI.
- `GET /booking/admin/all` — admin global booking list.
- `POST/DELETE /social/reviews/{id}/helpful` — helpful votes.
- `GET /social/reviews` & `GET /social/reviews/{entityType}/{entityId}` — generic + approved-only review reads.
- `POST/DELETE /blogs/comments/{commentId}/reactions` — comment reactions.
- `POST/DELETE /blogs/creators/profiles/{profileId}/follow` + `GET /following` — follow graph.
- `POST /tours/{tourId}/guide-offerings/{guideId}/suspend` · `/reinstate` — offering moderation (admin).
- `GET /content-core/categories/admin/{id}` — admin single-category read.
