# YallaJo — Webestica "Booking" Template → Plan Wiring

> **Purpose.** Maps every real page in the Webestica *Booking* travel theme
> (`booking.webestica.com`, root `*.html` only) onto a section of
> `yallajo-dashboard-ui-ux-plan.md`, with the YallaJo endpoints each page binds to.
>
> **Sources of truth.** Template = the actual HTML pages (headings extracted from
> markup). YallaJo API = `yallajo-endpoints.txt` (code-derived). `docs/` was not read.
>
> **How to read the status column:**
> - ✅ **Wire** — template page maps cleanly; reskin + bind endpoints.
> - ♻️ **Repurpose** — template page is for a vertical YallaJo lacks (hotel/flight/cab); reuse its *layout* for a YallaJo entity.
> - ⏭️ **Skip** — vertical/demo page with no YallaJo use.
> - 🟥 **USER builds** — YallaJo plan page that has **no** template equivalent; left for you.
>
> Ignored by design: `rtl/` mirror, `docs/` component showcase, hero-* fragments.

---

## A. Public / Storefront

| Template page | Real heading(s) | → Plan § | YallaJo endpoints | Status |
|---|---|---|---|---|
| `index-tour.html` | Life Is Adventure / Our Best Packages | **§2.1 Home** | `GET /tours/featured` · `/trending` · `/popular/tours\|places\|businesses` · `/content-core/categories` | ✅ Wire (primary home) |
| `index-directory.html` | Browse by Category / Great places to Explore | **§2.1 / §2.3** | `GET /content-core/categories` · `GET /places` | ✅ Wire (places landing) |
| `index.html` | The Best Holidays Start Here | §2.1 | — | ♻️ Repurpose (hotel home → alt home hero) |
| `index-resort.html`, `index-hotel-chain.html` | — | — | — | ⏭️ Skip (hotel verticals) |
| `index-flight.html`, `index-cab.html` | — | — | — | ⏭️ Skip (no vertical) |
| `tour-grid.html` | tour list grid | **§2.5 Tour list** | `GET /tours` · `GET /tours/search` · `/search/suggest` | ✅ Wire |
| `tour-detail.html` | Overview / Itinerary / Inclusions & Exclusions | **§2.5 Tour detail** | `GET /tours/by-slug/{slug}` · `/{id}/images\|waypoints\|children-info\|pricing\|schedules\|guides` · `/tours/{tourId}/guide-offerings` · `GET /booking/availability/{tourId}` · SEO head `Tour` | ✅ Wire |
| `tour-booking.html` | Review your Booking / Traveler Detail / Payment options | **§3.2 create + §3.7** | `POST /booking/tour` · `POST /payments/initiate` | ✅ Wire (checkout) |
| `hotel-detail.html` | About / Amenities / Room Options | **§2.4 Business detail** | `GET /places/businesses/{id}` · `/hours\|amenities\|services\|accessibility` · SEO head `Business` | ♻️ Repurpose |
| `directory-detail.html` | Emperor Salon & Spa / Description / Gallery / Details | **§2.4 Business detail** (alt) | same as §2.4 | ♻️ Repurpose |
| `hotel-grid.html`, `hotel-list.html` | 150 Hotels in New York | **§2.3 Place list** | `GET /places` | ♻️ Repurpose |
| `room-detail.html` | Luxury Room / Select Rooms / Price Summary | **§2.3 Place detail** | `GET /places/{slug}` · `/{id}/images\|accessibility\|businesses` · weather widget · SEO head `Place` | ♻️ Repurpose |
| `hotel-booking.html` | Review / Guest Details / Payment Options | §3.2/§3.7 | (covered by tour-booking) | ⏭️ Skip (dup of tour-booking) |
| `flight-*`, `cab-*` (list/detail/booking) | — | — | — | ⏭️ Skip (no vertical) |
| `compare-listing.html` | compare | — | client-side compare of `GET /tours` / `/places` | ♻️ Optional |
| `offer-detail.html` | promo/offer | §2.x | `GET /analytics/recommendations` (sponsored) | ♻️ Optional |
| `booking-confirm.html` | Congratulations | **§3.3 confirm** | post-`POST /booking/tour` success | ✅ Wire |
| `listing-added.html` | listing submitted successfully | **§4.3/§4.5 confirm** | post-`POST /tours` or `/places/businesses` | ✅ Wire |
| `blog.html` | The Blog / Latest Article | **§2.9 Blog list** | `GET /blogs` (`isFeatured`) | ✅ Wire |
| `blog-detail.html` | — | **§2.9 Blog post** | `GET /blogs/slug/{slug}` · `/{id}/comments` · `POST /blogs/{id}/views` · SEO head `Blog` | ✅ Wire |

---

## B. Auth

| Template page | → Plan § | YallaJo endpoints | Status |
|---|---|---|---|
| `sign-in.html` | §2.10 | `POST /auth/login` · `/external-providers/login` | ✅ Wire |
| `sign-up.html` | §2.10 | `POST /auth/register` · `/verify-email` · `/resend-otp` | ✅ Wire |
| `forgot-password.html` | §2.10 | `POST /auth/forgot-password` | ✅ Wire |
| `reset-password.html` / `reset-password-v2.html` | §2.10 | `POST /auth/reset-password` | ✅ Wire (pick one) |
| `two-factor-auth.html` | §2.10 | `POST /auth/verify-email` / OTP step | ✅ Wire (OTP) |

---

## C. Customer Dashboard (`account-*`)

| Template page | → Plan § | YallaJo endpoints | Status |
|---|---|---|---|
| `account-bookings.html` | **§3.2 My Trips** | `GET /booking/my-bookings` · `POST /booking/{id}/cancel` | ✅ Wire |
| `account-profile.html` | **§3.10 Profile tab** | `GET/PUT /accounts/profile` · `POST/PUT/DELETE /accounts/profile/avatar` | ✅ Wire |
| `account-wishlist.html` | **§3.4 Favorites** | `GET /social/favorites` · `POST/DELETE /social/favorites/...` | ✅ Wire |
| `account-payment-details.html` | **§3.7 Billing** | `GET /payments/my-payments` · `GET /invoices/my-invoices` | ✅ Wire |
| `account-settings.html` | **§3.10 Security/Notifications/Privacy tabs** | `PUT /security/account/password\|phone` · `GET/PUT /notifications/preferences` · `GET/PUT /analytics/preferences` | ✅ Wire |
| `account-delete.html` | **§3.10 (delete)** | `DELETE /accounts/profile` + `POST /accounts/profile/restore` | ✅ Wire |
| `account-travelers.html` | — | *(no YallaJo "saved travelers" endpoint)* | 🟥 USER decides (drop or new feature) |

**Customer plan pages with NO template page → 🟥 USER builds:**
§3.3 Booking Detail (full) · §3.5 My Reviews · §3.6 Recommendations/Discover · §3.8 Disputes · §3.9 Support tickets · §3.10 Devices + Linked-accounts tabs.

---

## D. Provider / Host Dashboard (`agent-*`)

| Template page | → Plan § | YallaJo endpoints | Status |
|---|---|---|---|
| `agent-dashboard.html` | **§4.1 Overview** | `GET /provider/dashboard[/overview\|/pending-actions\|/notifications]` · `/provider/analytics` | ✅ Wire |
| `agent-listings.html` | **§4.3 Tours / §4.5 Businesses** | `GET /tours/provider/my-tours` · `GET /places/businesses/mine` | ✅ Wire |
| `agent-bookings.html` | **§4.6 Incoming Bookings** | `GET /booking/provider/bookings` · `POST /booking/{id}/confirm\|reject\|complete` | ✅ Wire |
| `agent-earnings.html` | **§4.8 Finance** | `GET /payouts/provider` · `GET /invoices/provider/my-invoices` | ✅ Wire |
| `agent-reviews.html` | **§4.7 Reviews** | `GET /social/reviews/{entityType}/{entityId}` · `POST /social/reviews/{id}/reply` | ✅ Wire |
| `agent-activities.html` | **§4.1 (activity feed)** | `GET /provider/dashboard/notifications` | ♻️ Repurpose |
| `agent-settings.html` | **§4.10 Settings** | `GET /provider/settings` | ✅ Wire |
| `add-listing.html` | **§4.3 Tour editor / §4.5 Business editor** | `POST /tours` + editor tabs (pricing/schedules/waypoints/guides) · `POST /places/businesses` · attachments §0.1 (Quill→blog, Dropzone→attachments) | ✅ Wire |
| `add-listing-minimal.html` | §4.3 (quick create) | `POST /tours` | ♻️ Optional |

**Provider plan pages with NO template page → 🟥 USER builds:**
§4.2 Provider Application/onboarding · §4.4 Packages · §4.9 Provider Documents · availability-slot manager (§4.6 sub-tool).

**Reuse note:** `agent-*` family also seeds the **Tour Guide dashboard (§5)** — reskin agent-dashboard→§5.1, agent-earnings→§5.4, agent-reviews→(guide). But §5 specifics (§5.2 Profile w/ languages/specializations, §5.5 Tier, §5.6 Availability blocks, §5.7 Offerings, §5.8 Discounts, §5.9 Applications/Proposals, §5.11 Agency) → 🟥 USER builds.

---

## E. Platform Admin Dashboard (`admin-*`)

| Template page | → Plan § | YallaJo endpoints | Status |
|---|---|---|---|
| `admin-dashboard.html` | **§8.1 Overview** | `GET /admin/dashboard[/bookings\|/revenue\|/users]` | ✅ Wire |
| `admin-agent-list.html` | **§8.3 Providers** | `GET /admin/providers` | ✅ Wire |
| `admin-agent-detail.html` | **§8.3 Provider detail** | `GET /admin/providers/{id}` · `POST .../approve\|reject\|suspend\|reinstate` | ✅ Wire |
| `admin-booking-list.html` | **§8.7 (booking finance)** | `GET /booking/admin/all` | ✅ Wire |
| `admin-booking-detail.html` | **§8.7 booking detail** | `GET /booking/{id}` · `POST /admin/bookings/{id}/force-refund` | ✅ Wire |
| `admin-guest-list.html` | **§8.13 Users** | `GET /security/users` | ✅ Wire |
| `admin-guest-detail.html` | **§8.13 user detail** | `GET /security/users/{userId}` · lifecycle `PATCH /auth/admin/users/{userId}/...` | ✅ Wire |
| `admin-earnings.html` | **§8.7 Finance Ops** | `GET /finance/admin/dashboard` · `/payouts/admin/pending` | ✅ Wire |
| `admin-reviews.html` | **§8.2 Moderation** | `GET /social/reviews/admin/flagged` · `POST .../approve\|remove` | ✅ Wire |
| `admin-settings.html` | **§8.x platform settings** | (admin config) | ✅ Wire |

**Admin plan pages with NO template page → 🟥 USER builds:**
§8.4 Tours Review queue · §8.5 Places & Businesses moderation · §8.6 Blogs & Creators · §8.8 Growth & Merchandising · §8.9 Content Operations · §8.10 SEO Console · §8.11 Support admin · §8.12 Platform Ops.

---

## F. Marketing / Utility (static, low-binding)

| Template page | → Plan / use | Status |
|---|---|---|
| `join-us.html` "List Your Property" | §4.2 Provider acquisition CTA | ✅ Wire |
| `help-center.html`, `help-detail.html` | §3.9 Support entry (FAQ) | ♻️ Partial |
| `faq.html` | public FAQ (`GET /seo/faq/...`) | ✅ Wire |
| `about.html`, `team.html`, `contact.html`, `contact-2.html`, `pricing.html` | static marketing | ✅ Wire (static) |
| `privacy-policy.html`, `terms-of-service.html` | static legal | ✅ Wire (static) |
| `coming-soon.html`, `error.html` | system states (404) | ✅ Wire |

---

## G. 🟥 YallaJo Pages With NO Template — USER Builds

These plan surfaces have **no** Webestica page to reskin. Build from scratch:

1. **§2.7 Guides list + detail** — no template guide page (repurpose tour-grid/agent-detail).
2. **§2.8 Agencies list + detail** (public).
3. **§5 Tour Guide dashboard specifics** — §5.2, §5.5, §5.6, §5.7, §5.8, §5.9, §5.10, §5.11.
4. **§6 Agency dashboard** (entirely) — §6.1–§6.4.
5. **§7 Content Creator dashboard** (entirely) — §7.1–§7.5 (Quill from add-listing helps the editor).
6. **§9 SuperAdmin RBAC console** (entirely).
7. **Customer:** §3.3 Booking Detail · §3.5 Reviews · §3.6 Discover · §3.8 Disputes · §3.9 Support.
8. **Provider:** §4.2 Application · §4.4 Packages · §4.9 Documents.
9. **Admin:** §8.4, §8.5, §8.6, §8.8, §8.9, §8.10, §8.11, §8.12.

---

## H. Vendor-lib → YallaJo feature cheatsheet

| Template vendor | Use in YallaJo |
|---|---|
| **Quill** (add-listing) | Blog editor §7.2, tour rich descriptions |
| **Dropzone** | Attachment uploads §0.1 (`POST /content-core/attachments/images` ≤20) |
| **Flatpickr** | Booking availability dates, schedule pickers |
| **ApexCharts** | Dashboards §4.1/§5.3/§8.1, analytics |
| **Glightbox / tiny-slider / Splide** | Image galleries (places/tours/businesses) |
| **Choices.js** | Categories/tags/languages/specializations multiselects |
| **noUiSlider** | Price-range filters in search §2.2 |
| **Stepper** | Multi-step booking checkout (tour-booking) & onboarding |
