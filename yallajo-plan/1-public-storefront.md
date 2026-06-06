# Area 1 — Anonymous / Public Storefront

> **Actor:** Anonymous / Public (no auth required)
> **Plan section:** §2 of `yallajo-dashboard-ui-ux-plan.md`
> **Source of truth:** `yallajo-endpoints.txt` (549 endpoints). All routes `/api/v1`-prefixed
> except the 4 system routes. Status legend: ✅ Wire · ♻️ Repurpose · ⏭️ Skip · 🟥 USER builds.
> **Architecture & rules:** see [`0-architecture-and-rules.md`](0-architecture-and-rules.md). Every page below carries a **Stack:** line (Area · Route · Cache · Perm · Rules).

## Conventions (apply to every page here)
- **SEO/SSR first.** Only 6 detail pages inject `GET /seo/metadata/{entityType}/{entityId}` +
  `GET /seo/faq/{entityType}/{entityId}`, constrained by the `SeoEntityType` enum:
  `Place`, `Tour`, `Business`, `Blog`, `TourGuide`, `Creator`.
- Public image reads via approved-only routes (`/places/{id}/images`, `/tours/{id}/images`).
- Reference data cacheable (`content-core/categories|languages|tags|specializations`, `places/accessibility/catalog`).
- i18n via `Accept-Language` + `GET /content-core/translations/{entityType}/{entityId}`.
- Login-gated buttons for favorite / book / review.
- Anonymous beacons (sendBeacon): `POST /blogs/{id}/views`, `POST /interactions`.
- **Code area:** `Public` (anonymous, OutputCache + SSR) except the auth funnel (`Auth` area). All pages are `[AllowAnonymous]`; write buttons are login-gated → sign-in modal with return URL (`WL1`).

## Pages this area should have (14 + auth funnel)

| # | Page | Template | Redirects to |
|---|------|----------|--------------|
| 2.1 | **Home** | `index-tour.html` ✅ · `index-directory.html` ♻️ · `index.html` ♻️ | §2.5 Tour detail · §2.2 Search · §2.3 Place list · `join-us.html` (§4.2) |
| 2.2 | **Search** | hero search fragments ♻️ + `noUiSlider` ♻️ (render into `tour-grid`/`hotel-grid` shell) | §2.5 / §2.3 / §2.4 detail by result type |
| 2.3 | **Place list + detail** | `hotel-grid`/`hotel-list` ♻️ (list) · `room-detail`/`hotel-detail` ♻️ (detail) | this detail · §2.4 Business detail · §3.2 book · §3.4 favorite |
| 2.4 | **Business detail** | `directory-detail.html` ♻️ · `hotel-detail.html` ♻️ | §2.3 parent Place · §2.10 login → §3.5 · weather widget |
| 2.5 | **Tour list + detail** | `tour-grid.html` ✅ (list) · `tour-detail.html` ✅ (detail) | this detail · §2.7 Guide · §3.2 `tour-booking.html` checkout · similar tours |
| 2.6 | **Tour Package** | 🟥 USER builds (base = `tour-grid`/`tour-detail`) | this detail · §2.5 included tour · §3.2 book |
| 2.7 | **Guides list + detail** | 🟥 USER builds (directory/profile card grid) | this detail · §2.5 tours by guide · §2.8 Agency |
| 2.8 | **Agencies** | 🟥 USER builds | agency detail · §2.7 Guide detail · §5.11 apply (login) |
| 2.9 | **Blog list + post + creator profile** | `blog.html` ✅ (list) · `blog-detail.html` ✅ (post) · Creator profile 🟥 USER builds | §2.9 post · §2.9 Creator profile · §2.5 related tour · §2.10 login |
| 2.10 | **Auth funnel** | `sign-in` ✅ · `sign-up` ✅ · `forgot-password` ✅ · `reset-password`/`-v2` ✅ · `two-factor-auth` ✅ | success → §3.1 (or role dashboard via `security/me`) |
| 2.11 | **System** | ⏭️ no UI (webhook/sitemap) · `error.html` ✅ (404) · `coming-soon.html` ✅ | §2.1 Home |

## Endpoints by page

### 2.1 Home `SSR`
`GET /tours/featured` · `GET /trending` · `GET /popular/tours` · `GET /popular/places` · `GET /popular/businesses` · `GET /content-core/categories`.
- **Buttons:** **Search** → §2.2 (nav) · **Explore Tours** → §2.5 (nav) · **Browse Places** → §2.3 (nav) · **List Your Property** → `join-us.html` §4.2 (nav) · **Sign in / Sign up** → §2.10 (nav, only when anonymous) · category/featured cards → detail (nav).
- **Stack:** **Area** `Public` · **Route** `/` · **Cache** `PublicShort` (5 min, tag `homepage`) · **Perm** `[AllowAnonymous]` · **Rules** `R2` SSR first paint · `API1` parallel featured/trending/popular · `A8` inline critical CSS · `I4` preload hero LCP · `ERR3` per-section degrade · `CC1` consent · `A11Y1`.

### 2.2 Search `AJAX`
`GET /tours/search` · `/tours/search/suggest` · `/places/businesses/search` · `/places/map/viewport` · `/places/nearby` · `/places/businesses/nearby`.
- **Buttons:** **Search** → `GET /tours/search` (submit) · **Apply Filters** (facets/price `noUiSlider`) → re-query `AJAX` · **Clear Filters** → reset (client) · **Map view / List view** toggle → `/places/map/viewport` (client) · **Use my location** → `/places/nearby` (client geolocation) · result card → §2.3/§2.4/§2.5 detail (nav).
- **Stack:** **Area** `Public` · **Route** `/search` · **Cache** `PublicShort` · **Perm** `[AllowAnonymous]` · **Rules** `S1` SSR results then AJAX filter + `history.pushState` · `S2` autocomplete 300 ms · `S3/D4` filter sidebar → mobile offcanvas + sticky Apply · `R4` paging 20/50 · `SEC5` 30/min/IP · `MAP1` lazy Mapbox.

### 2.3 Place list + detail
- List `SSR`: `GET /places`.
- SEO head (`Place`): `GET /seo/metadata/Place/{id}` + `GET /seo/faq/Place/{id}`.
- Localized text (`AJAX`): `GET /content-core/translations/Place/{id}` (`?languageCode=&status=`) — honors `Accept-Language` with default-language fallback; Facade renders localized fields.
- Detail `SSR`: `GET /places/{slug}` or `/places/{id}`; then `AJAX`: `/places/{id}/images`, `/accessibility`, `/businesses`, weather, `GET /social/reviews/{entityType}/{entityId}`, `/ratings`, `/social/accessibility/reviews`, `GET /analytics/recommendations/for/{kind}/{entityId}`.
- **Buttons (detail):**
  - **Add to Favorites / Remove** (login-gated) → `POST /social/favorites` · `DELETE /social/favorites/Place/{id}`; initial state `GET /social/favorites/check/Place/{id}` `AJAX`.
  - **Write a Review** (login-gated) → `POST /social/reviews`; edit ≤48h `PUT /social/reviews/{id}`, `DELETE /social/reviews/{id}`.
  - **Write Accessibility Review** (login-gated) → `POST /social/accessibility/reviews`; edit ≤48h `PUT .../{id}`.
  - **Helpful** on a review → `POST /social/reviews/{id}/helpful` · undo `DELETE .../helpful`.
  - **Report a review** → `POST /social/reviews/{id}/report` · **Report this place** → `POST /social/reports` (entityType=Place; login-gated).
  - **View Businesses here** → §2.4 (nav) · **Share** → client-only (no endpoint).
- **Stack:** **Area** `Public` · **Route** `/places`, `/places/{slug}` · **Cache** list `PublicShort` · detail `PublicMedium` (30 min, tag `place:{id}`, `C5` ETag) · **Perm** `[AllowAnonymous]` · **Rules** `R2` SSR · `I1–I4` images · `MAP1` lazy map · `NF6/WL1` optimistic + guest-gated favorite · `REV3–5` reviews · `A11Y1`.

### 2.4 Business detail
- SEO head (`Business`): `GET /seo/metadata/Business/{id}` + `/seo/faq/Business/{id}`.
- Localized text (`AJAX`): `GET /content-core/translations/Business/{id}` (`?languageCode=&status=`) — honors `Accept-Language` with default-language fallback; Facade renders localized fields.
- `GET /places/businesses/{id}` · `/hours` · `/{businessId}/amenities` · `/{businessId}/services` · `/services/{id}` · `/{id}/accessibility`.
- **Buttons:**
  - **Add to Favorites / Remove** (login-gated) → `POST /social/favorites` · `DELETE /social/favorites/Business/{id}`; state `GET /social/favorites/check/Business/{id}`.
  - **Write a Review** (login-gated) → `POST /social/reviews`; edit ≤48h `PUT /social/reviews/{id}`.
  - **Helpful** → `POST /social/reviews/{id}/helpful` · undo `DELETE .../helpful`.
  - **Report a review** → `POST /social/reviews/{id}/report` · **Report this business** → `POST /social/reports` (entityType=Business).
  - **View parent Place** → §2.3 (nav) · **Directions / Call** → client-only (no endpoint).
- **Stack:** **Area** `Public` (owner management lives in the `Business` area §4.5) · **Route** `/businesses/{slug}` · **Cache** `PublicMedium` (tag `business:{id}`, `C5` ETag) · **Perm** `[AllowAnonymous]` · **Rules** `R2` SSR · Place-contextual weather widget (§0.2) · `NF6/WL1` favorite · `REV3–5` reviews · `MAP1` lazy map.

### 2.5 Tour list + detail
- List `SSR`: `GET /tours`. SEO head (`Tour`): `/seo/metadata/Tour/{id}` + `/seo/faq/Tour/{id}`.
- Localized text (`AJAX`): `GET /content-core/translations/Tour/{id}` (`?languageCode=&status=`) — honors `Accept-Language` with default-language fallback; Facade renders localized fields.
- Detail: `GET /tours/by-slug/{slug}` (canonical) or `/tours/{id}`; tabs `/images`·`/waypoints`·`/children-info`·`/pricing`·`/schedules`·`/guides`.
- Offerings: `GET /tours/{tourId}/guide-offerings[/{guideId}][/pricing-tiers][/schedules]`.
- Availability: `GET /booking/availability/{tourId}[/{date}]`.
- Recommendations: `GET /analytics/recommendations/similar/{entityId}` · `/for/{kind}/{entityId}`.
- **Buttons (detail):**
  - **Book Now / Check Availability** → opens checkout; `GET /booking/availability/{tourId}/{date}` then `POST /booking/tour` (AwaitingPayment) → §3.2/§3.7 checkout (login-gated).
  - **Add to Favorites / Remove** (login-gated) → `POST /social/favorites` · `DELETE /social/favorites/Tour/{id}`; state `GET /social/favorites/check/Tour/{id}`.
  - **Write a Review** (login-gated) → `POST /social/reviews`; edit ≤48h `PUT /social/reviews/{id}`.
  - **Helpful** → `POST /social/reviews/{id}/helpful` · undo `DELETE .../helpful` · **Report a review** → `POST /social/reviews/{id}/report` · **Report this tour** → `POST /social/reports` (entityType=Tour).
  - **Choose a Guide** (guide-offerings) → §2.7 guide / select offering (nav) · **Similar tours** card → this detail (nav).
  - **Sponsored/recommendation click** beacon → `POST /analytics/recommendations/sponsored-click` / `POST /analytics/recommendations/metrics` (fire-and-forget) · **Share** → client-only.
- **Stack:** **Area** `Public` · **Route** `/tours`, `/tours/{slug}` · **Cache** list `PublicShort` · detail `PublicMedium` (tag `tour:{id}`, `C5` ETag) · **Perm** `[AllowAnonymous]` · **Rules** `R2` SSR · JSON-LD `TouristAttraction` · `IMG1` GLightbox gallery · `MAP4` numbered waypoints · `CAL1–3` availability heatmap + SignalR `S2` `tour:{tourId}` live slots · `NF6/WL1` favorite · `REV2` review-when-Completed · `D5` sticky Book CTA · checkout → `CAL4` 10-min lock + `PAY`.

### 2.6 Tour Package `SSR`
`GET /tours/packages` · `/tours/packages/{id}`.
- **Buttons:** **Book Package** → `POST /booking/tour` per included tour → §3.2/§3.7 checkout (login-gated) · **Add to Favorites** → `POST /social/favorites` (entityType per included entity) · **View included tour** → §2.5 (nav).
- **Stack:** **Area** `Public` · **Route** `/packages/{slug}` · **Cache** `PublicMedium` · **Perm** `[AllowAnonymous]` · **Rules** `R2` SSR · `NF6` favorite · book → `CAL5/PAY` checkout · `D5` sticky CTA.

### 2.7 Guides list + detail
- SEO head (`TourGuide`): `GET /seo/metadata/TourGuide/{id}` + `/seo/faq/TourGuide/{id}`.
- Localized text (`AJAX`): `GET /content-core/translations/TourGuide/{id}` (`?languageCode=&status=`) — honors `Accept-Language` with default-language fallback; Facade renders localized fields.
- `GET /guides` · `/guides/by-slug/{slug}` (canonical) · `/guides/{id}` · `/guides/{id}/tours`; reviews `GET /social/reviews/TourGuide/{id}` · `/social/reviews/ratings` `AJAX`.
- **Buttons:** **View Guide's Tours** → §2.5 list filtered by `/guides/{id}/tours` (nav) · **Book a tour with this guide** → §2.5 offering → `POST /booking/tour` (login-gated) · **Add to Favorites / Remove** (login-gated) → `POST /social/favorites` · `DELETE /social/favorites/TourGuide/{id}`; state `GET /social/favorites/check/TourGuide/{id}` · **Write a Review** (login-gated) → `POST /social/reviews` (entityType=TourGuide); edit ≤48h `PUT /social/reviews/{id}`, `DELETE /social/reviews/{id}` · **Helpful** → `POST /social/reviews/{id}/helpful` · undo `DELETE .../helpful` · **Report a review** → `POST /social/reviews/{id}/report` · **Report this guide** → `POST /social/reports` (entityType=TourGuide) · **Share** → client-only.
- **Stack:** **Area** `Public` · **Route** `/guides`, `/guides/{slug}` · **Cache** list `PublicShort` · detail `PublicMedium` · **Perm** `[AllowAnonymous]` · **Rules** `R2` SSR · SEO `TourGuide` · `NF6/WL1` favorite (`TourGuide` is a valid `FavoriteEntityType`) · `REV3–5` reviews · `A11Y1`.

### 2.8 Agencies
`GET /agency` · `/agency/{agencyUserId}`.
- **Buttons:** **View Agency** → agency detail (nav) · **View Guides** → §2.7 (nav) · **Apply to this Agency** (guide-only, login-gated) → `POST /guides/agencies/{agencyUserId}/apply` → §5.11.
- **Stack:** **Area** `Public` · **Route** `/agency`, `/agency/{agencyUserId}` · **Cache** `PublicMedium` · **Perm** `[AllowAnonymous]` (Apply = `[Authorize]` + `Guide` policy) · **Rules** `R2` SSR · `A11Y1`.

### 2.9 Blog list + post + creator profile
- List `SSR`: `GET /blogs`.
- Post: SEO head (`Blog`) `GET /seo/metadata/Blog/{id}` + `/seo/faq/Blog/{id}`; `GET /blogs/slug/{slug}` or `/blogs/{id}`; `/blogs/{id}/comments`; beacon `POST /blogs/{id}/views`.
- Localized text (`AJAX`): `GET /content-core/translations/Blog/{id}` (`?languageCode=&status=`) — honors `Accept-Language` with default-language fallback; Facade renders localized fields.
- Creator profile: SEO head (`Creator`); `GET /blogs/creators/niches` · `/profiles/{slug}[/blogs]` · `/profiles/{profileId}/followers`.
- Follow (auth): `GET /blogs/creators/profiles/{profileId}/following`, `POST/DELETE .../follow`.
- Comment reactions (auth): `POST/DELETE /blogs/comments/{commentId}/reactions`.
- **Buttons (post):**
  - **Post Comment / Reply** (login-gated) → `POST /blogs/{id}/comments`; edit ≤30min `PUT /blogs/comments/{commentId}`, `DELETE /blogs/comments/{commentId}`.
  - **React** (like) on a comment → `POST /blogs/comments/{commentId}/reactions` · remove `DELETE .../reactions`.
  - **Follow / Unfollow Creator** (login-gated) → `POST /blogs/creators/profiles/{profileId}/follow` · `DELETE .../follow`; state `GET .../following`.
  - **Add to Favorites / Remove** (login-gated) → `POST /social/favorites` · `DELETE /social/favorites/Blog/{id}`; state `GET /social/favorites/check/Blog/{id}`.
  - **Report this post** → `POST /social/reports` (entityType=Blog; login-gated).
  - **Related tour** card → §2.5 (nav) · **View Creator Profile** → §2.9 creator (nav) · **Share** → client-only · view beacon `POST /blogs/{id}/views` (auto, fire-and-forget).
- **Stack:** **Area** `Public` · **Route** `/blog`, `/blog/{slug}`, `/creators/{slug}` · **Cache** list `PublicShort` · post/profile `PublicLong` (1h, tag `blog:{id}`) · **Perm** `[AllowAnonymous]` · **Rules** `R2` SSR · `SEC3` sanitized blog HTML (`Html.Raw` only with `// SANITIZED:`) · comments/follow/react/favorite = `AJAX` + `NF6` optimistic · report → `POST /social/reports` · view beacon `API2` fire-and-forget · `REV5`-style ≤30min comment-edit window.

### 2.10 Auth funnel
`POST /auth/register` · `/verify-email` · `/resend-otp` · `/login` · `/forgot-password` · `/reset-password` · `/refresh` · `/invitations/accept` · `/external-providers/login` · `/logout` · `/logout-all`.
- **Buttons:**
  - sign-up: **Create Account** → `POST /auth/register` → OTP screen.
  - two-factor/verify: **Verify** → `POST /auth/verify-email` (success → §3.1 / role dashboard) · **Resend code** → `POST /auth/resend-otp`.
  - sign-in: **Sign In** → `POST /auth/login` → role dashboard via `GET /security/me` · **Continue with Google/etc.** → `POST /auth/external-providers/login`.
  - forgot-password: **Send reset code** → `POST /auth/forgot-password`. reset-password: **Reset Password** → `POST /auth/reset-password`.
  - invite accept page: **Accept Invitation** → `POST /auth/invitations/accept`.
- **Stack:** **Area** `Auth` · **Route** `/auth/sign-in` · `/auth/sign-up` · `/auth/forgot-password` · `/auth/reset-password` · `/auth/verify` · **Cache** `NoStore` · **Perm** `[AllowAnonymous]` · **Rules** `SEC5` rate-limit (sign-in 5/min/IP, OTP 5/min/user) → 429+Retry-After · `SEC7` anti-forgery · `PE1` native POST · `F1–F3` blur-validate + password-strength meter · cookie `YallaJo.Auth` HttpOnly+Secure+SameSite=Lax 30-day sliding · `Auth.Shared/_RecaptchaField` · success → `GET /security/me` routes to role dashboard.

### 2.11 System
`GET /` · `/sitemap.xml` · `/sitemaps/{entityType}.xml` · `POST /payments/webhook` · `GET /seo/weather?lat=&lng=`.
- **Stack:** **Area** `Public` · **Route** `/sitemap.xml`, `/sitemaps/{entityType}.xml`, `/error`, `/coming-soon` · **Cache** sitemap `PublicDay` · error `NoStore` · **Perm** `[AllowAnonymous]` · **Rules** `ERR1` branded 400/403/404/429/500/503 + search + home CTA · `ERR2` correlation ID · `PAY4` `POST /payments/webhook` is API-side server-confirm (no MVC UI) · robots.txt disallows `/accounts//provider//admin//business//creator//guide/`.
