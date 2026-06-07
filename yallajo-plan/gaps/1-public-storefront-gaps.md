# Gap Report — `1-public-storefront.md` vs. shipped code

> **✅ STATUS: ALL 9 GAPS RESOLVED** (applied to `1-public-storefront.md`; re-verified against shipped `Areas/Public` + `Areas/Auth` controllers via direct grep + independent explore agent — two-source agreement). Resolution summary:
> - **G1** §2.5 SignalR `S2`/`CAL3`/`RT1` **IMPLEMENTED & SHIPPED** (no longer a gap): API `[AllowAnonymous] TourSlotsHub` at `/hubs/tour` (`tour:{tourId}` group, WS+LongPolling, keepalive 15s/timeout 30s), `BroadcastSlotCapacityToTourHandler` broadcasts `SlotCapacityChanged{slotId,remainingCapacity}` from the **integration** event (post-commit), `TourId` added to the domain+integration events, client `tour-slots.js` (self-hosted `@microsoft/signalr`, `withAutomaticReconnect`, M3, X12, PE2). Booking.Api + Web build clean.
> - **G2** Favorite buttons on §2.3/2.4/2.5/2.6/2.7/2.9 → cross-area **`POST /accounts/wishlist/toggle/{entityType}/{entityId}`** (+ `WL1` guest path); no bare `/social/favorites` button targets remain.
> - **G3** Review/report/accessibility buttons → page-scoped (`POST /{tours/{slug}|places/{slug}|businesses/{id}|guides/{slug}}/reviews|report|...`); API-vs-BFF note added to Conventions.
> - **G4** §2.9 blog comment/follow → `POST /blog/comments/{commentId}/edit|delete|react|unreact`, `POST /creators/{profileId}/follow|unfollow`.
> - **G5** §2.8 route reverted to **`/agency`, `/agency/{agencyUserId}`** (no `/agencies`); apply gate → **`[Authorize(Roles = "TourGuide")]`**.
> - **G6** §2.6/§2.7 marked **view-redesign (shipped)**; §2.6 detail route → **`/packages/{id:guid}`**.
> - **G7** §2.1 Home adds `/explore`,`/home`; error pages noted on `HomeController`; §2.2 Search lists `/search/{suggest|businesses|nearby|map}` BFF routes.
> - **G8** §2.5 clarified BFF `/tours/{slug}` → API `tours/by-slug/{slug}`.
> - **G9** §2.10 Auth notes BFF logout actions `sign-out`/`sign-out-all`, OAuth `/auth/external/*`, invite `/auth/accept-invite`, conventional GET landings for sessions/devices/providers.
>
> *Findings below retained for the historical audit trail.*

---


> **Method:** deep code-vs-plan audit. Every row below was verified against the **shipped controllers** in
> `src/Hosts/YallaJo.Web/Areas/Public/Controllers/*` (13 controllers) and `Areas/Auth/Controllers/*` (6 controllers),
> not just `yallajo-endpoints.txt`. The API route table is the *backend* contract; this report is about the **BFF (MVC) layer**
> the browser actually talks to.
>
> **Scope:** `yallajo-plan/1-public-storefront.md` (§2 storefront + §2.10 auth funnel).
> **Date basis:** current `main` working tree.
>
> **Severity:** 🔴 blocking (plan describes something the code does not / cannot do) · 🟠 wiring (route/verb shape wrong, would mislead a builder) · 🟡 cosmetic (clarity / status).
>
> **Legend for "Status":** ❌ not built · 🔁 built but elsewhere (cross-area) · ✏️ built, route/verb differs · ✅ matches.

---

## 0. Summary

| Area | Verdict |
|------|---------|
| Permissions | ✅ No defect — all public pages are `[AllowAnonymous]`; write actions are `[Authorize]` (or role `TourGuide` for agency apply). No invented `WebPermission.*` (unlike §3–§8). |
| OutputCache tiers + tags | ✅ Matches the plan & rules exactly (`PublicShort`/`PublicMedium`/`PublicLong` + `homepage`/`place:{id}`/`business:{id}`/`tour:{id}`/`blog:{id}` tags via `PublicOutputCacheTagger`). |
| BFF route shapes | 🟠 Plan lists **API** routes as button targets; shipped actions are **page-scoped** (`tours/{slug}/reviews`, not `/social/reviews`). |
| Favorites (wishlist) | 🔁 **Not in Public** — lives in `Accounts` (`POST /accounts/wishlist/toggle/...`). Plan's `POST /social/favorites` button target is wrong on the BFF layer. |
| SignalR live slots | 🔴 **Not implemented anywhere in Web** — `S2`/`CAL3 tour:{tourId}` live-slot claims on §2.5 are aspirational. |
| `§2.8` Agencies route | ✏️ Shipped is `/agency`, **not** `/agencies` (a prior plan edit introduced `/agencies` — wrong vs code). |
| `§2.6`/`§2.7` build status | ✏️ Marked 🟥 USER-builds but **shipped** (`Packages`/`Guides` controllers) → view-redesign only. Packages detail is `/packages/{id:guid}`, not `/packages/{slug}`. |

---

## 1. 🔴 Blocking gaps (code cannot do what the plan describes)

### G1 — SignalR live slot capacity — ✅ RESOLVED BY IMPLEMENTATION (§2.5)
> **Implemented.** API: `Messaging.Presentation/Hubs/TourSlotsHub.cs` (`[AllowAnonymous]`, `JoinTour`/`LeaveTour` → `tour:{tourId}` only), mapped `/hubs/tour` in `MessagingEndpoints.cs`; `AddSignalR` keepalive 15s/timeout 30s in `YallaJo.Api/Program.cs`; `Messaging.Infrastructure/EventHandlers/BroadcastSlotCapacityToTourHandler.cs` consumes `IntegrationEventNotification<AvailabilitySlotCapacityChangedIntegrationEvent>` (post-commit) → `IHubContext<TourSlotsHub>.Clients.Group("tour:{tourId}").SendAsync("SlotCapacityChanged", {slotId, remainingCapacity})`; `TourId` added to `AvailabilitySlotCapacityChangedDomainEvent` (+ 6 raise sites in `AvailabilitySlot.cs`) and `AvailabilitySlotCapacityChangedIntegrationEvent` (+ converter). Web: `wwwroot/assets/js/lib/signalr/signalr.min.js` (self-hosted 8.0.7) + `wwwroot/assets/js/tour-slots.js` + a live-availability widget in `Areas/Public/Views/Tours/Detail.cshtml`. The original analysis below is retained for history.


- **Plan says:** §2.5 Tour detail Stack cites `CAL1–3` + `S2` SignalR `tour:{tourId}` live slots; §0.3/rules cite `AJAX⟳`, `RT1 SlotCapacityChanged`.
- **Code reality:** **zero** SignalR in `YallaJo.Web` — no `*Hub.cs`, no `MapHub`, no `HubConnection`, no `SlotCapacityChanged` (grep count = 0). The public tour-detail booking widget has no live channel.
- **Impact:** The live-availability UX (count animation, flash, `withAutomaticReconnect`) cannot be built as specced. Availability is currently read-only via `GET /booking/availability/{tourId}[/{date}]`.
- **Resolution (option a, shipped):** implemented `TourSlotsHub` (group `tour:{tourId}`) per UI-PERF §S. Oracle-reviewed; refinements applied: PE2 now degrades to a **disconnected-only 60s Web-origin reload poll** (stops on reconnect — X12 preserved), the S6 comment no longer over-claims (`MaximumParallelInvocationsPerClient` ≠ per-user connection cap), and the browser→API-hub path is documented as the sanctioned **S2 public-hub exception** (no JWT, public-safe tour-capacity data, mirrors the existing `NotificationHub`). API + Web build clean; API boots past DI with zero SignalR/Hub errors (only a missing-DB seeding error, unrelated).
- ~~**Action:** Either (a) implement a `TourHub`… or (b) downgrade §2.5… Until then `S2`/`CAL3`/`RT1` are not satisfiable.~~ *(done — option a)*

---

## 2. 🔁 Cross-area gaps (built, but not where the plan implies)

### G2 — Favorites/Wishlist toggle is Accounts-only, not Public
- **Plan says:** §2.3/§2.4/§2.5/§2.7/§2.9 each expose **"Add to Favorites / Remove"** → `POST /social/favorites` · `DELETE /social/favorites/{entityType}/{entityId}` · state `GET /social/favorites/check/...`.
- **Code reality:** **No favorite route exists in any Public controller.** The toggle is shipped in **`Areas/Accounts`**:
  - `POST /accounts/wishlist/toggle/{entityType}/{entityId}` (`[Authorize]`; returns `401 "Please sign in to save favorites"` for guests)
  - `POST /accounts/wishlist/remove/{entityType}/{entityId}`
  - `GET /accounts/wishlist` · `POST /accounts/wishlist/remove-all`
  - API binding is `WishlistApiClient` → `/api/v1/social/favorites*`.
- **Impact:** The public heart button has **no public POST target**. A logged-in user on `/tours/{slug}` must POST to the **Accounts** route `/accounts/wishlist/toggle/Tour/{id}` (cross-area), or a public favorite action must be added.
- **Action:** Update §2.3–§2.9 favorite buttons to target **`POST /accounts/wishlist/toggle/{entityType}/{entityId}`** (cross-area) and note the guest path is `WL1` (sign-in modal) because the Accounts action 401s anonymous users. Do **not** describe a bare `POST /social/favorites` BFF action — it doesn't exist.

---

## 3. 🟠 Wiring gaps (BFF route/verb shape differs from the plan)

### G3 — Review / report / accessibility-review actions are page-scoped (not bare `/social/*`)
- **Plan says (buttons):** `POST /social/reviews`, `POST /social/reviews/{id}/helpful`, `POST /social/reports`, `POST /social/accessibility/reviews`, etc.
- **Code reality (shipped BFF, all `[HttpPost]` + `[Authorize]`):** every action is mounted under the entity's page route:

  | Action | Shipped BFF route (per entity) |
  |--------|-------------------------------|
  | Write review | `POST /{tours/{slug}\|places/{slug}\|businesses/{id}\|guides/{slug}}/reviews` |
  | Edit / Delete review | `…/reviews/{reviewId}/edit` · `/delete` |
  | Helpful / Unhelpful | `…/reviews/{reviewId}/helpful` · `/unhelpful` |
  | Report a review | `…/reviews/{reviewId}/report` |
  | Report entity | `…/report` |
  | Accessibility review | `…/accessibility-reviews` (+ `/{reviewId}/edit` · `/delete`) |
  | Tour join request | `POST /tours/{slug}/join` |
  | Sponsored-click beacon | `POST /places/{slug}/recommendations/sponsored-click` |

  > **Note:** businesses use `{id:guid}` (`/businesses/{id}/reviews`); tours/places/guides use `{slug}`.
- **Impact:** The plan's API routes are correct for the **ApiClient** layer but wrong as **button/form targets**. A builder wiring `<form asp-action>` to `/social/reviews` would 404.
- **Action:** Add the standard **API-verb-vs-BFF-route note** (as done in §3–§8) and list the page-scoped POST routes. The `/social/*` routes remain valid as the *API binding the ApiClient calls*.

### G4 — §2.9 blog comment / follow actions are page-scoped POST (not API `PUT`/`DELETE`)
- **Plan says:** comment edit `PUT /blogs/comments/{commentId}`, delete `DELETE …`, react/unreact `POST/DELETE …/reactions`, follow `POST/DELETE …/follow`.
- **Code reality (shipped `BlogController`):**
  - `POST /blog/{id}/comments` · `POST /blog/comments/{commentId}/edit` · `/delete` · `/react` · `/unreact`
  - `POST /creators/{profileId}/follow` · `/unfollow` (note: `/unfollow`, not `DELETE …/follow`)
  - view beacon `POST /blog/{id}/view` (`[AllowAnonymous]`)
- **Impact:** All mutations are `POST` to `/blog/*` and `/creators/*`; the plan's `PUT`/`DELETE` are API verbs only.
- **Action:** Show the real `POST /blog/*` and `POST /creators/{profileId}/follow|unfollow` BFF routes; add the API-vs-BFF note.

### G5 — §2.8 Agencies route is `/agency`, not `/agencies`
- **Plan says (current):** Route `/agencies`, `/agencies/{id}` (a prior edit changed it to `/agencies`).
- **Code reality (`AgenciesController`):** `GET /agency`, `GET /agency/{agencyUserId:guid}`, `POST /agency/{agencyUserId}/apply` — apply gate is **`[Authorize(Roles = "TourGuide")]`** (a role check, not a `Guide` policy).
- **Impact:** The `/agencies` route does not exist; the earlier "fix" diverged from code.
- **Action:** Revert §2.8 Route to **`/agency`, `/agency/{agencyUserId}`**; change "Apply = `Guide` policy" to **`[Authorize(Roles = "TourGuide")]`**.

---

## 4. 🟡 Cosmetic / status gaps

### G6 — §2.6 / §2.7 are shipped, not 🟥 greenfield
- `PackagesController` → `GET /packages`, `GET /packages/{id:guid}` (`[AllowAnonymous]`, PublicShort/PublicMedium).
- `GuidesController` → `GET /guides`, `GET /guides/{slug}` (+ review/report actions).
- **Action:** Mark §2.6/§2.7 as **view-redesign (controller shipped)**, not 🟥 USER-builds. Fix §2.6 detail route to **`/packages/{id:guid}`** (the plan says `/packages/{slug}`).

### G7 — §2.1 Home & §2.2 Search have extra/wrapped BFF routes
- **Home** (`HomeController`): `/` + **`/explore`** + **`/home`** (all PublicShort+`homepage` tag). Error pages `/error`, `/error/{code:int}` live **here**, not a separate system controller.
- **Search** (`SearchController`): BFF routes are `/search`, `/search/suggest`, **`/search/businesses`**, **`/search/nearby`**, **`/search/map`** — these *wrap* the API routes (`/places/businesses/search`, `/places/nearby`, `/places/map/viewport`) the plan lists.
- **Action:** Add `/explore`, `/home` to §2.1 routes; note §2.11 error pages are served by `HomeController`. List §2.2's real `/search/*` BFF routes (API routes are what the ApiClient calls).

### G8 — §2.5 tour-detail route clarity
- Plan §2.5 detail line says canonical `GET /tours/by-slug/{slug}` (the **API** canonical). The **BFF** route is `GET /tours/{slug}` (shipped). Both true; just clarify the BFF `/tours/{slug}` calls the API `tours/by-slug/{slug}`.

### G9 — Auth funnel route prefix confirmed correct, minor naming
- `AuthController` is `[Area("Auth")] [Route("auth")]` → routes ARE `/auth/sign-in`, `/auth/sign-up`, `/auth/forgot-password`, `/auth/reset-password`, `/auth/two-factor-auth`, `/auth/resend-otp`, `/auth/sign-out`, `/auth/sign-out-all`. ✅ Plan's `/auth/*` is correct.
- Minor: logout BFF actions are **`sign-out` / `sign-out-all`** (not `logout`/`logout-all` — those are the API names). External auth is `challenge`/`callback`/`complete` (`ExternalAuthController`); sessions revoke = `POST /auth/sessions/revoke/{sessionId}`; device trust = `POST /auth/devices/trust/{deviceId}`; provider unlink = `POST /auth/externalproviders/unlink/{providerId}`.
- **Action:** Note the BFF logout actions are `sign-out`/`sign-out-all`.

---

## 5. ✅ Confirmed-correct (no action)

- **Permissions:** all public GETs `[AllowAnonymous]`; all mutations `[Authorize]` (agency apply = role `TourGuide`). No invented `WebPermission.*`.
- **Cache:** Home PublicShort+`homepage`; list pages PublicShort; detail pages PublicMedium + tag (`place:`/`business:`/`tour:`); blog post PublicLong+`blog:`; agencies PublicMedium. Tag eviction via `PublicOutputCacheTagger`. **Matches the plan & §4 of the rules exactly.**
- **Weather widget:** wired in Public (18 refs) — `GET /seo/weather/{placeId}` Place-contextual. ✅
- **SEO head (6 entities), translations (active-culture render), beacons (`POST /blog/{id}/view`):** present and aligned.

---

## 6. Recommended plan edits (apply order)

1. **G5** revert §2.8 → `/agency`, role `TourGuide`. *(corrects a prior wrong edit)*
2. **G2** rewrite favorite buttons → `POST /accounts/wishlist/toggle/{entityType}/{entityId}` (cross-area) + `WL1` guest path.
3. **G3/G4** add API-vs-BFF note; replace bare `/social/*` and `/blogs/*` button targets with the page-scoped `POST /{entity}/{slug}/...` and `POST /blog/...` routes.
4. **G1** downgrade §2.5 SignalR to a **future** item (or open an implementation task for `TourHub`).
5. **G6/G7/G8/G9** status + route corrections (Packages `/{id:guid}`, mark §2.6/§2.7 shipped, add `/explore`,`/home`,`/search/*`, clarify by-slug, note `sign-out`).

> **Net:** the storefront plan is **structurally sound** (no permission/area defect) but its **button targets are written at the API layer**; the shipped BFF uses page-scoped POST routes, favorites live in `Accounts`, and SignalR live-slots are unbuilt. G1 (SignalR) and G2 (favorites) are the only behavioral gaps; the rest are route-shape corrections.
