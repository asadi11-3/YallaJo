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
- i18n via `Accept-Language` + `GET /content-core/translations/{entityType}/{entityId}`. Facades render **only the active culture's value** (request `ar` → Arabic only, `en` → English only — never both side-by-side); default-language fallback is used **only** when the active-culture value is missing.
- **Security/theme baseline (every page):** `SEC1` CSP on every response (no `unsafe-inline`; Mapbox/asset-CDN/App-Insights allowlisted; report → `/api/v1/csp-report`) · `SEC2` hardening headers · `T5/A6` the one allowed inline theme-bootstrap script carries a per-request CSP **nonce**.
- Login-gated buttons for favorite / book / review.
- Anonymous beacons (sendBeacon): `POST /blogs/{id}/views`, `POST /interactions`.
- **Code area:** `Public` (anonymous, OutputCache + SSR) except the auth funnel (`Auth` area). All pages are `[AllowAnonymous]`; write buttons are login-gated → sign-in modal with return URL (`WL1`).
- **BFF routes vs API routes (read this).** Every `/social/*`, `/blogs/*`, `/seo/*`, `/booking/*` route shown below is the **API-layer** path the `ApiClient` calls. The **button/form the browser POSTs to is the page-scoped BFF route** (`[HttpPost]` + anti-forgery + PRG), verified against the shipped `Areas/Public/Controllers/*`:
  - Reviews/reports live under the entity page: `POST /{tours/{slug}|places/{slug}|businesses/{id}|guides/{slug}}/reviews` (+ `/{reviewId}/edit|delete|helpful|unhelpful|report`), accessibility reviews `…/accessibility-reviews` (+ `/{reviewId}/edit|delete`), entity report `…/report`. (Businesses use `{id:guid}`; tours/places/guides use `{slug}`.)
  - Blog/creator actions: `POST /blog/{id}/comments`, `POST /blog/comments/{commentId}/edit|delete|react|unreact`, `POST /creators/{profileId}/follow|unfollow`, beacon `POST /blog/{id}/view`.
  - Detail BFF reads are slug-routed (`GET /tours/{slug}`, `/places/{slug}`, `/guides/{slug}`, `/blog/{slug}`) and call the API's canonical by-slug read internally (e.g. `/tours/{slug}` → API `tours/by-slug/{slug}`).
- **Favorites are a cross-area `Accounts` action — there is NO favorite route in the Public area.** The heart button POSTs to **`POST /accounts/wishlist/toggle/{entityType}/{entityId}`** (`[Area("Accounts")]`, `[Authorize]`; returns `401 "Please sign in to save favorites"` for guests → `WL1` sign-in modal). State is read via the Accounts wishlist surface; the `/social/favorites*` paths are the API binding `WishlistApiClient` calls, never a Public BFF route.
- **SignalR live-slot capacity is SHIPPED (UI-PERF S2 — the one public-page hub).** The API hosts an `[AllowAnonymous]` `TourSlotsHub` at **`/hubs/tour`** (`Messaging.Presentation/Hubs/TourSlotsHub.cs`, mapped in `MessagingEndpoints.cs`, WebSockets+LongPolling per S1, keepalive 15s/timeout 30s per S7). The public tour-detail page connects with `?tourId=` and joins the **`tour:{tourId}` group only** (S5 — it exposes no method that can reach `user:`/`provider:`/`admin`). Capacity changes broadcast `SlotCapacityChanged { slotId, remainingCapacity }` (S3 IDs-only) from `BroadcastSlotCapacityToTourHandler` (an **integration-event** handler → post-commit, never pre-commit rollbacks). Client `wwwroot/assets/js/tour-slots.js` uses `withAutomaticReconnect([0,2000,10000,30000,60000])` (S4), suppresses the flash under `prefers-reduced-motion` (M3), runs no `setInterval` while connected (X12), and on hub-down degrades to a manual **Reconnect** affordance **plus** a gentle 60s Web-origin page-reload poll **only while disconnected** (PE2 — the poll stops the instant the hub reconnects, preserving X12). The `@microsoft/signalr` client is self-hosted (`wwwroot/assets/js/lib/signalr/`); CSP `connect-src` must allowlist the API origin + `wss:`.
  - **S2 public-hub exception (documented):** the anonymous browser connects directly to the **API**-hosted `/hubs/tour` — the same browser→API-hub pattern the existing authenticated `NotificationHub` (`/hubs/notifications`) already uses. This is the one sanctioned deviation from "browser ↔ Web only" (§1): the connection carries **no JWT**, only a public `tourId`, and the hub is read-only/broadcast. Tour-slot capacity is already **public-safe data** shown on the SSR page, so subscribing by tour GUID exposes nothing not already public. (A future hardening could move the hub behind a Web-origin reverse proxy or a Web-issued signed subscription ticket; not required while the data is public.)

## Pages this area should have (14 + auth funnel)

| # | Page | Template | Redirects to |
|---|------|----------|--------------|
| 2.1 | **Home** | `index-tour.html` ✅ · `index-directory.html` ♻️ · `index.html` ♻️ | §2.5 Tour detail · §2.2 Search · §2.3 Place list · `join-us.html` (§4.2) |
| 2.2 | **Search** | hero search fragments ♻️ + `noUiSlider` ♻️ (render into `tour-grid`/`hotel-grid` shell) | §2.5 / §2.3 / §2.4 detail by result type |
| 2.3 | **Place list + detail** | `hotel-grid`/`hotel-list` ♻️ (list) · `room-detail`/`hotel-detail` ♻️ (detail) | this detail · §2.4 Business detail · §3.2 book · §3.4 favorite |
| 2.4 | **Business detail** | `directory-detail.html` ♻️ · `hotel-detail.html` ♻️ | §2.3 parent Place · §2.10 login → §3.5 · weather widget |
| 2.5 | **Tour list + detail** | `tour-grid.html` ✅ (list) · `tour-detail.html` ✅ (detail) | this detail · §2.7 Guide · §3.2 `tour-booking.html` checkout · similar tours |
| 2.6 | **Tour Package** | ✏️ view-redesign (shipped `PackagesController`; base = `tour-grid`/`tour-detail`) | this detail · §2.5 included tour · §3.2 book |
| 2.7 | **Guides list + detail** | ✏️ view-redesign (shipped `GuidesController`; directory/profile card grid) | this detail · §2.5 tours by guide · §2.8 Agency |
| 2.8 | **Agencies** | ✏️ view-redesign (shipped `AgenciesController`) | agency detail · §2.7 Guide detail · §5.11 apply (login) |
| 2.9 | **Blog list + post + creator profile** | `blog.html` ✅ (list) · `blog-detail.html` ✅ (post) · Creator profile 🟥 USER builds | §2.9 post · §2.9 Creator profile · §2.5 related tour · §2.10 login |
| 2.10 | **Auth funnel** | `sign-in` ✅ · `sign-up` ✅ · `forgot-password` ✅ · `reset-password`/`-v2` ✅ · `two-factor-auth` ✅ | success → §3.1 (or role dashboard via `security/me`) |
| 2.11 | **System** | ⏭️ no UI (webhook/sitemap) · `error.html` ✅ (404) · `coming-soon.html` ✅ | §2.1 Home |

## Endpoints by page

### 2.1 Home `SSR`
`GET /tours/featured` · `GET /trending` · `GET /popular/tours` · `GET /popular/places` · `GET /popular/businesses` · `GET /content-core/categories`.
- **Buttons:** **Search** → §2.2 (nav) · **Explore Tours** → §2.5 (nav) · **Browse Places** → §2.3 (nav) · **List Your Property** → `join-us.html` §4.2 (nav) · **Sign in / Sign up** → §2.10 (nav, only when anonymous) · category/featured cards → detail (nav).
- **Stack:** **Area** `Public` · **Route** `/` (shipped `HomeController` also serves `/explore` and `/home`; the `/error` + `/error/{code}` branded error pages live in this controller too) · **Cache** `PublicShort` (5 min, tag `homepage`) · **Perm** `[AllowAnonymous]` · **Rules** `R2` SSR first paint · `API1` parallel featured/trending/popular · `A8` inline critical CSS · `I4` preload hero LCP · `ERR3` per-section degrade · `CC1` consent · `SEC1`/`SEC2` CSP + hardening headers · `T1–T5` theme (T5 nonce'd bootstrap) · `A11Y1`.

### 2.2 Search `AJAX`
- **BFF routes (shipped `SearchController`):** `GET /search` (results) · `GET /search/suggest` (autocomplete) · `GET /search/businesses` · `GET /search/nearby` · `GET /search/map`. These wrap the API reads `GET /tours/search` · `/tours/search/suggest` · `/places/businesses/search` · `/places/map/viewport` · `/places/nearby` · `/places/businesses/nearby` (the `ApiClient` layer).
- **Buttons:** **Search** → `GET /search` (submit) · **Apply Filters** (facets/price `noUiSlider`) → re-query `AJAX` · **Clear Filters** → reset (client) · **Map view / List view** toggle → `GET /search/map` · **Use my location** → `GET /search/nearby` (client geolocation) · result card → §2.3/§2.4/§2.5 detail (nav).
- **Stack:** **Area** `Public` · **Route** `/search` (+ `/search/suggest|businesses|nearby|map`) · **Cache** `PublicShort` · **Perm** `[AllowAnonymous]` · **Rules** `S1` SSR results then AJAX filter + `history.pushState` · `S2` autocomplete 300 ms · `S3/D4` filter sidebar → mobile offcanvas + sticky Apply · `R4` paging 20/50 · `SEC5` 30/min/IP · `J6` AbortController (last-write-wins on stale typeahead/filter calls) + `S4` back/forward restores filter state · `MAP1` lazy Mapbox.

### 2.3 Place list + detail
- List `SSR`: `GET /places`.
- SEO head (`Place`): `GET /seo/metadata/Place/{id}` + `GET /seo/faq/Place/{id}`.
- Localized text (`AJAX`): `GET /content-core/translations/Place/{id}` (`?languageCode=&status=`) — honors `Accept-Language` with default-language fallback; Facade renders localized fields.
- Detail `SSR`: `GET /places/{slug}` or `/places/{id}`; then `AJAX`: `/places/{id}/images`, `/accessibility`, `/businesses`, weather widget `GET /seo/weather/{placeId}` (sets `X-Weather-Stale`; ad-hoc fallback `GET /seo/weather?lat=&lng=`), `GET /social/reviews/{entityType}/{entityId}`, `/ratings`, `/social/accessibility/reviews`, `GET /analytics/recommendations/for/{kind}/{entityId}`.
- **Buttons (detail)** *(BFF routes; `/social/*` shown are the API binding — see header note)*:
  - **Add to Favorites / Remove** (login-gated) → **`POST /accounts/wishlist/toggle/Place/{id}`** (cross-area Accounts; guest → `WL1` sign-in modal). API binding: `POST/DELETE /social/favorites/Place/{id}`, state `GET /social/favorites/check/Place/{id}`.
  - **Write a Review** (login-gated) → `POST /places/{slug}/reviews`; edit `POST /places/{slug}/reviews/{reviewId}/edit` (≤48h) · delete `…/{reviewId}/delete`. (API: `POST/PUT/DELETE /social/reviews[/{id}]`.)
  - **Write Accessibility Review** (login-gated) → `POST /places/{slug}/accessibility-reviews`; edit `…/{reviewId}/edit` (≤48h) · delete `…/{reviewId}/delete`.
  - **Helpful / Unhelpful** on a review → `POST /places/{slug}/reviews/{reviewId}/helpful` · `…/unhelpful`.
  - **Report a review** → `POST /places/{slug}/reviews/{reviewId}/report` · **Report this place** → `POST /places/{slug}/report` (login-gated).
  - **View Businesses here** → §2.4 (nav) · **Share** → client-only (no endpoint).
- **Stack:** **Area** `Public` · **Route** `/places`, `/places/{slug}` · **Cache** list `PublicShort` · detail `PublicMedium` (30 min, tag `place:{id}`, `C5` ETag) · **Perm** `[AllowAnonymous]` · **Rules** `R2` SSR · `I1–I4` images · `MAP1` lazy map · `NF6/WL1` optimistic + guest-gated favorite · `REV3–5` reviews · `A11Y1`.

### 2.4 Business detail
- SEO head (`Business`): `GET /seo/metadata/Business/{id}` + `/seo/faq/Business/{id}`.
- Localized text (`AJAX`): `GET /content-core/translations/Business/{id}` (`?languageCode=&status=`) — honors `Accept-Language` with default-language fallback; Facade renders localized fields.
- `GET /places/businesses/{id}` · `/hours` · `/{businessId}/amenities` · `/{businessId}/services` · `/services/{id}` · `/{id}/accessibility`.
- Weather widget (Place-contextual, §0.2): `GET /seo/weather/{placeId}` of the parent place (sets `X-Weather-Stale`; ad-hoc fallback `GET /seo/weather?lat=&lng=`).
- **Buttons** *(BFF routes; businesses are `{id:guid}`-routed)*:
  - **Add to Favorites / Remove** (login-gated) → **`POST /accounts/wishlist/toggle/Business/{id}`** (cross-area Accounts; guest → `WL1`). API binding: `POST/DELETE /social/favorites/Business/{id}`, state `GET /social/favorites/check/Business/{id}`.
  - **Write a Review** (login-gated) → `POST /businesses/{id}/reviews`; edit `…/reviews/{reviewId}/edit` (≤48h) · delete `…/reviews/{reviewId}/delete`.
  - **Helpful / Unhelpful** → `POST /businesses/{id}/reviews/{reviewId}/helpful` · `…/unhelpful`.
  - **Report a review** → `POST /businesses/{id}/reviews/{reviewId}/report` · **Report this business** → `POST /businesses/{id}/report`.
  - **Write Accessibility Review** (login-gated) → `POST /businesses/{id}/accessibility-reviews` (+ `…/{reviewId}/edit|delete`).
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
- **Buttons (detail)** *(BFF routes; tours are `{slug}`-routed)*:
  - **Book Now / Check Availability** → opens checkout (`GET /tours/{tourId}/book` → `POST /tours/{tourId}/book`, login-gated; API `GET /booking/availability/{tourId}/{date}` + `POST /booking/tour` → AwaitingPayment) → §3.2/§3.7.
  - **Add to Favorites / Remove** (login-gated) → **`POST /accounts/wishlist/toggle/Tour/{id}`** (cross-area Accounts; guest → `WL1`). API binding: `POST/DELETE /social/favorites/Tour/{id}`.
  - **Write a Review** (login-gated; **only for users with a `Completed` booking on this tour** per `REV2`) → `POST /tours/{slug}/reviews`; edit `…/reviews/{reviewId}/edit` (≤48h) · delete `…/reviews/{reviewId}/delete`.
  - **Helpful / Unhelpful** → `POST /tours/{slug}/reviews/{reviewId}/helpful` · `…/unhelpful` · **Report a review** → `…/reviews/{reviewId}/report` · **Report this tour** → `POST /tours/{slug}/report`.
  - **Request to Join** (group booking, login-gated) → `POST /tours/{slug}/join`.
  - **Choose a Guide** (guide-offerings) → §2.7 guide / select offering (nav) · **Similar tours** card → this detail (nav).
  - **Sponsored/recommendation click** beacon → `POST /analytics/recommendations/sponsored-click` / `POST /analytics/recommendations/metrics` (fire-and-forget) · **Share** → client-only.
- **Stack:** **Area** `Public` · **Route** `/tours`, `/tours/{slug}` (BFF `/tours/{slug}` → API canonical `tours/by-slug/{slug}`) · **Cache** list `PublicShort` · detail `PublicMedium` (tag `tour:{id}`, `C5` ETag) · **Perm** `[AllowAnonymous]` · **Rules** `R2` SSR · JSON-LD `TouristAttraction` · `IMG1` GLightbox gallery · `MAP4` numbered waypoints · `CAL1–2` availability heatmap (read via `GET /booking/availability/{tourId}[/{date}]`) · **`S2`/`CAL3`/`RT1` live-slot SignalR = SHIPPED** (`[AllowAnonymous] TourSlotsHub` at `/hubs/tour`, `tour:{tourId}` group, `SlotCapacityChanged{slotId,remainingCapacity}`; client `tour-slots.js` with `S4` auto-reconnect, `M3` reduced-motion, `X12` no-poll, `PE2` reconnect fallback) · `NF6/WL1` favorite · `REV2` review-when-Completed · `D5` sticky Book CTA · checkout → `CAL4` 10-min lock + `PAY`.

### 2.6 Tour Package `SSR` *(shipped `PackagesController`)*
- BFF reads: `GET /packages` (list) · `GET /packages/{id:guid}` (detail). API binding: `GET /tours/packages` · `/tours/packages/{id}`.
- **Buttons:** **Book Package** → `POST /tours/{tourId}/book` per included tour → §3.2/§3.7 checkout (login-gated) · **Add to Favorites** → `POST /accounts/wishlist/toggle/Tour/{id}` (cross-area; per included entity) · **View included tour** → §2.5 (nav).
- **Stack:** **Area** `Public` · **Route** `/packages`, `/packages/{id:guid}` · **Cache** list `PublicShort` · detail `PublicMedium` · **Perm** `[AllowAnonymous]` · **Rules** `R2` SSR · `NF6/WL1` favorite (cross-area Accounts) · book → `CAL5/PAY` checkout · `D5` sticky CTA.

### 2.7 Guides list + detail
- SEO head (`TourGuide`): `GET /seo/metadata/TourGuide/{id}` + `/seo/faq/TourGuide/{id}`.
- Localized text (`AJAX`): `GET /content-core/translations/TourGuide/{id}` (`?languageCode=&status=`) — honors `Accept-Language` with default-language fallback; Facade renders localized fields.
- `GET /guides` · `/guides/by-slug/{slug}` (canonical) · `/guides/{id}` · `/guides/{id}/tours`; reviews `GET /social/reviews/TourGuide/{id}` · `/social/reviews/ratings` `AJAX`.
- **Buttons** *(BFF routes; guides are `{slug}`-routed)*: **View Guide's Tours** → §2.5 list filtered by `/guides/{id}/tours` (nav) · **Book a tour with this guide** → §2.5 offering → `POST /tours/{tourId}/book` (login-gated) · **Add to Favorites / Remove** (login-gated) → **`POST /accounts/wishlist/toggle/TourGuide/{id}`** (cross-area; guest → `WL1`; API binding `POST/DELETE /social/favorites/TourGuide/{id}`) · **Write a Review** (login-gated) → `POST /guides/{slug}/reviews`; edit `…/reviews/{reviewId}/edit` (≤48h) · delete `…/reviews/{reviewId}/delete` · **Helpful / Unhelpful** → `POST /guides/{slug}/reviews/{reviewId}/helpful` · `…/unhelpful` · **Report a review** → `…/reviews/{reviewId}/report` · **Report this guide** → `POST /guides/{slug}/report` · **Write Accessibility Review** → `POST /guides/{slug}/accessibility-reviews` (+ `…/{reviewId}/edit|delete`) · **Share** → client-only.
- **Stack:** **Area** `Public` · **Route** `/guides`, `/guides/{slug}` · **Cache** list `PublicShort` · detail `PublicMedium` · **Perm** `[AllowAnonymous]` · **Rules** `R2` SSR · SEO `TourGuide` · `NF6/WL1` favorite (`TourGuide` is a valid `FavoriteEntityType`) · `REV3–5` reviews · `A11Y1`.

### 2.8 Agencies *(shipped `AgenciesController`)*
- BFF reads: `GET /agency` (list) · `GET /agency/{agencyUserId:guid}` (detail). API binding: `GET /agency` · `/agency/{agencyUserId}`.
- **Buttons:** **View Agency** → agency detail (nav) · **View Guides** → §2.7 (nav) · **Apply to this Agency** (guide-only, login-gated) → **`POST /agency/{agencyUserId}/apply`** → §5.11.
- **Stack:** **Area** `Public` · **Route** `/agency`, `/agency/{agencyUserId}` (the shipped BFF routes — there is **no** `/agencies` route) · **Cache** `PublicMedium` · **Perm** `[AllowAnonymous]` (Apply = **`[Authorize(Roles = "TourGuide")]`** — a role check, not a `Guide` policy) · **Rules** `R2` SSR · `A11Y1`.

### 2.9 Blog list + post + creator profile
- List `SSR`: `GET /blogs`.
- Post: SEO head (`Blog`) `GET /seo/metadata/Blog/{id}` + `/seo/faq/Blog/{id}`; `GET /blogs/slug/{slug}` or `/blogs/{id}`; `/blogs/{id}/comments`; beacon `POST /blogs/{id}/views`.
- Localized text (`AJAX`): `GET /content-core/translations/Blog/{id}` (`?languageCode=&status=`) — honors `Accept-Language` with default-language fallback; Facade renders localized fields.
- Creator profile: `GET /blogs/creators/niches` · `/profiles/{slug}[/blogs]`; the Facade resolves `profileId` from the `{slug}` read first, then SEO head (`Creator`) `GET /seo/metadata/Creator/{profileId}` + `/seo/faq/Creator/{profileId}` and `/profiles/{profileId}/followers`.
- Follow (auth): `GET /blogs/creators/profiles/{profileId}/following`, `POST/DELETE .../follow`.
- Comment reactions (auth): `POST/DELETE /blogs/comments/{commentId}/reactions`.
- **Buttons (post)** *(BFF routes; blog detail is `/blog/{slug}`, creator profile `/creators/{slug}`)*:
  - **Post Comment / Reply** (login-gated) → `POST /blog/{id}/comments`; edit ≤30min `POST /blog/comments/{commentId}/edit` · delete `POST /blog/comments/{commentId}/delete`. (API: `POST /blogs/{id}/comments`, `PUT/DELETE /blogs/comments/{commentId}`.)
  - **React / Unreact** on a comment → `POST /blog/comments/{commentId}/react` · `POST /blog/comments/{commentId}/unreact`.
  - **Follow / Unfollow Creator** (login-gated) → `POST /creators/{profileId}/follow` · `POST /creators/{profileId}/unfollow`; state `GET .../following` (API).
  - **Add to Favorites / Remove** (login-gated) → **`POST /accounts/wishlist/toggle/Blog/{id}`** (cross-area; guest → `WL1`; API binding `POST/DELETE /social/favorites/Blog/{id}`).
  - **Report this post** → `POST /blog/{id}/report`-style entity report (login-gated; API `POST /social/reports` entityType=Blog).
  - **Related tour** card → §2.5 (nav) · **View Creator Profile** → §2.9 creator (nav) · **Share** → client-only · view beacon `POST /blog/{id}/view` (auto, fire-and-forget; `[AllowAnonymous]`).
- **Stack:** **Area** `Public` · **Route** `/blog`, `/blog/{slug}`, `/creators/{slug}` · **Cache** list `PublicShort` · post/profile `PublicLong` (1h, tag `blog:{id}`) · **Perm** `[AllowAnonymous]` · **Rules** `R2` SSR · `SEC3` sanitized blog HTML (`Html.Raw` only with `// SANITIZED:`) · comments/follow/react/favorite = `AJAX` + `NF6` optimistic · report → `POST /social/reports` · view beacon `API2` fire-and-forget · comment-edit window ≤30 min (endpoint-derived, `PUT /blogs/comments/{commentId}`; not a `REV` rule).

### 2.10 Auth funnel *(shipped `AuthController` `[Route("auth")]` + `ExternalAuth`/`Sessions`/`Devices`/`ExternalProviders`/`AcceptInvite` controllers)*
- **BFF routes (verified):** `AuthController` `[Route("auth")]` → `GET/POST /auth/sign-in` · `/auth/sign-up` · `/auth/forgot-password` · `/auth/reset-password` · `/auth/two-factor-auth` · `POST /auth/resend-otp` · **`POST /auth/sign-out`** · **`POST /auth/sign-out-all`** (BFF logout actions are `sign-out`/`sign-out-all`, **not** `logout`). Invite accept = `GET/POST /auth/accept-invite` (`AcceptInviteController`). External OAuth = `POST /auth/external/challenge` · `GET /auth/external/callback` · `POST /auth/external/complete` (`ExternalAuthController`). Session/device/provider mgmt (`[Authorize]`, used by §3.10): GET landings route conventionally (`/Auth/Sessions`, `/Auth/Devices`, `/Auth/ExternalProviders`); POSTs are `POST /auth/sessions/revoke/{sessionId}` · `POST /auth/devices/trust/{deviceId}` · `POST /auth/externalproviders/unlink/{providerId}`.
- **API binding:** `POST /auth/register` · `/verify-email` · `/resend-otp` · `/login` · `/forgot-password` · `/reset-password` · `/refresh` · `/invitations/accept` · `/external-providers/login` · `/logout` · `/logout-all`.
- **Buttons:**
  - sign-up: **Create Account** → `POST /auth/register` → OTP screen.
  - two-factor/verify: **Verify** → `POST /auth/verify-email` (success → §3.1 / role dashboard) · **Resend code** → `POST /auth/resend-otp`.
  - sign-in: **Sign In** → `POST /auth/login` → role dashboard via `GET /security/me` · **Continue with Google/etc.** → `POST /auth/external-providers/login`.
  - forgot-password: **Send reset code** → `POST /auth/forgot-password`. reset-password: **Reset Password** → `POST /auth/reset-password`.
  - invite accept page: **Accept Invitation** → `POST /auth/invitations/accept`.
- **Stack:** **Area** `Auth` · **Route** `/auth/sign-in` · `/auth/sign-up` · `/auth/forgot-password` · `/auth/reset-password` · `/auth/verify` · **Cache** `NoStore` · **Perm** `[AllowAnonymous]` · **Rules** `SEC5` rate-limit (sign-in 5/min/IP, OTP 5/min/user) → 429+Retry-After · `SEC7` anti-forgery · `PE1` native POST · `F1–F3` blur-validate + password-strength meter · cookie `YallaJo.Auth` HttpOnly+Secure+SameSite=Lax 30-day sliding · `Auth.Shared/_RecaptchaField` · success → `GET /security/me` routes to role dashboard.

### 2.11 System
`GET /` · `/sitemap.xml` · `/sitemaps/{entityType}.xml` · `POST /payments/webhook` · `GET /seo/weather?lat=&lng=`.
- **Stack:** **Area** `Public` · **Route** `/sitemap.xml`, `/sitemaps/{entityType}.xml`, `/error` + `/error/{code:int}` (served by the shipped `HomeController`, not a separate system controller), `/coming-soon` · **Cache** sitemap `PublicDay` · error `NoStore` · **Perm** `[AllowAnonymous]` · **Rules** `ERR1` branded 400/403/404/429/500/503 + search + home CTA · `ERR2` correlation ID · `PAY4` `POST /payments/webhook` is API-side server-confirm (no MVC UI) · robots.txt disallows `/accounts/`, `/provider/`, `/admin/`, `/business/`, `/creator/`, `/guide/`.
