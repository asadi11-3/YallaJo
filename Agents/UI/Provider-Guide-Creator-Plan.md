# Provider, Guide & Creator — Build Plan

> **Scope:** the next pages to build *after* `Accounts-Notifications-Support-Reviews-Plan.md`.
> Companion docs: `UI-UX-Design.md` §22 (template reality audit), `Accounts-Notifications-Support-Reviews-Plan.md` (current sprint).
> **Authoring rules:** `CONTROLLER_AUTHORING_GUIDE.md` (authoritative).
> **Template root:** `C:\Users\admin1\Desktop\Template\hotel-management-syste-main\booking.webestica.com`

---

## Why this plan

`YallaJo.Web` currently has **5 areas**: Accounts(61), Admin(241), Auth(82), Content(88), Public(47).
Two personas have **no area at all** yet — **Provider** (vendor self-service) and **Guide** — even though the template
ships the `agent-*` dashboard shell for them. Plus the **creator/blog-social** layer and **group-booking join** flow
remain unbuilt. This plan covers those four blocks and wires every new page to its source template file.

---

## Architecture conventions (read before building)

All areas follow `CONTROLLER_AUTHORING_GUIDE.md` (authoritative — supersedes the older `WEB_LAYER_GUIDE.md`). The **new** `Provider` and `Guide` areas use the same **layered-by-type** layout as every existing area:

- **Layout (layered by type, folder = namespace):** under `Areas/{Area}/` place `Controllers/`, `Facades/`, `ApiClients/`, `Models/{Feature}/` (DTOs + ViewModels + Mapper grouped per feature — **no** `Requests/`·`Responses/`·`ViewModels/`·`Mappers/` subfolders), and `Views/{Controller}/{View}.cshtml`. Per-feature partials → `Views/{Controller}/Partials/_X.cshtml`; area-shared chrome → `Areas/{Area}/Shared/`. View discovery already resolves `~/Areas/{area}/Views/{controller}/{view}.cshtml` — **no expander code, no `Program.cs` change.**
- **Four-tier pipeline:** `Controller → Facade → ApiClient → IApiClient`. Controllers never inject `IApiClient`/`HttpClient` (only Facades). Facades never touch `HttpContext`/`TempData`. Errors are values → `ApiResult`/`ApiResult<T>`, never exceptions.
- **DI:** `AddFeatureServices()` auto-registers every `*ApiClient` / `*Facade` as scoped **by name suffix** — keep suffixes, no manual `AddScoped`.
- **Controllers:** inherit `BaseController`; `[Area("Provider")]` / `[Area("Guide")]` + `[Authorize]` + `[RequirePermission(WebPermission.*)]`; async + trailing `CancellationToken ct`; `[HttpGet]` reads, `[HttpPost]` + `[ValidateAntiForgeryToken]` writes; `if (GuardSignOut(result) is { } signOut) return signOut;` after each facade call; PRG + `SetSuccess`/`SetError`.
- **Composite dashboards** (Dashboard, Settings) compose **many facades** in one controller — sanctioned **Pattern A**; extract a dedicated aggregate facade (Pattern B) only if reused, or a ViewComponent (Pattern C) for a cross-page widget. A controller injecting several facades is fine; injecting `IApiClient` is not.
- **Permissions:** add `WebPermission.Provider.*` / `WebPermission.Guide.*` nested groups in `Infrastructure/Authorization/WebPermission.cs`; never inline permission strings.
- **Pagination:** per-feature paged `Response` (`Items, PageNumber, PageSize, TotalCount, HasPreviousPage, HasNextPage`); pager from `HasPreviousPage`/`HasNextPage` only.
- **Views:** `@model ...Areas.{Area}.Models.{Feature}.{Vm}`; `Layout` = the area shell (`_ProviderLayout`/`_GuideLayout`); explicit `asp-area`/`asp-controller`; `@Html.AntiForgeryToken()` in POST forms; shared `_Alerts` for flash; `<partial>` / `Html.PartialAsync` only.
- **Auth/cache:** cookie BFF; `JwtAuthHandler` attaches the bearer token; per-user dashboards → **no output cache**.
- **Build check:** `dotnet build src\Hosts\YallaJo.Web\YallaJo.Web.csproj` **run alone** (parallel builds hit the `CS2012` VBCSCompiler lock).
- **Shells:** build `_ProviderLayout.cshtml` + `_ProviderSidebar.cshtml` (+ `ProviderSidebarVm.cs`) once in `Areas/Provider/Shared/`, porting the template `agent-*` dashboard chrome; Guide reuses the pattern with `_GuideLayout`/`_GuideSidebar` in `Areas/Guide/Shared/`.

> **Resolved:** Provider & Guide use the **layered-by-type** layout above (per `CONTROLLER_AUTHORING_GUIDE.md`) — the earlier "feature-first vs Admin-style" question is **closed**. Admin's extra `Modules/` nesting is Admin-only; Provider/Guide are plain layered areas like Auth/Content.

### Design & UI skills (mandatory for all views)

Every view / partial / layout below must be produced **using these skills** — load them before writing markup or CSS:

- **`ui-ux-pro-max`** + **`impeccable`** — layout, visual hierarchy, information architecture, accessibility, responsive behavior, empty/error states.
- **`design-taste-frontend`** — component architecture + performant, hardware-accelerated CSS; overrides default LLM design biases.
- **`huashu-design`** — hi-fi HTML exploration / variants before committing final markup; anti-AI-slop pass.

Constraint: stay within the existing Bootstrap 5 **Booking** template assets (`wwwroot/assets`) and the layered Razor view conventions above — the skills inform *taste and structure*, not a re-platforming.

---

## Template → page wiring (master map)

| New page | Area / route | Template source file | Reuse note |
|---|---|---|---|
| Provider Dashboard | `Provider/Dashboard` | `agent-dashboard.html` | KPI cards + booking/traffic charts + Upcoming Bookings table |
| Provider Listings | `Provider/Listings` | `agent-listings.html` | listing cards (price/day, location) + Edit/Delete |
| Tour Create/Edit | `Provider/Listings/Create`·`Edit` | `add-listing.html` (+ `add-listing-minimal.html`, `listing-added.html`) | standalone stepper, **no sidebar**; add tour tabs |
| Provider Bookings | `Provider/Bookings` | `agent-bookings.html` | table + search/sort + grid/list toggle |
| Provider Earnings | `Provider/Earnings` | `agent-earnings.html` | KPI + invoice table |
| Provider Reviews | `Provider/Reviews` | `agent-reviews.html` | tabs All/Published/Deleted + Reply |
| Provider Settings | `Provider/Settings` | `agent-settings.html` | profile/email/password/2FA/linked/sessions |
| Provider Activities | `Provider/Activities` | `agent-activities.html` | activity feed |
| Guide Console (all) | `Guide/*` | *clone* `agent-dashboard.html` + `agent-listings.html` shells | no guide template exists |
| Public Creator Profile | `Content/Creators/{slug}` | `team.html` + `blog-detail.html` author box | person card + follow |
| Blog comments | (on) `Content/Blogs/Details` | `blog-detail.html` | template has no thread — net-new component |
| Creator post composer | `Content/Creators/Compose` | *clone* `add-listing.html` stepper | net-new authoring form |
| My Join Requests | `Accounts` → `JoinRequests` | `account-bookings.html` shell | list in account sidebar |
| Request to Join (modal) | (on) `Public/Tours/Detail` | `tour-detail.html` | modal on tour page |
| Admin SEO/FAQ | `Admin` module | `admin-settings.html` + `faq.html` | Admin Form/Table pattern |

---

## Phase 0 — Grounding (do first, ~half day)

- [ ] Read the Accounts **Wishlist** feature end-to-end as the reference slice (`ApiClients/` → `Facades/` → `Controllers/` → `Views/`).
- [ ] Read Admin's dashboard (`Home`) + `Users` (table + modal partials) for KPI-card and table partials to reuse.
- [ ] Inspect `src/Modules/ContentTours` controllers → confirm exact **provider write** paths (create/update tour, schedules, pricing-tiers, waypoints, children-info, packages, guide assignment).
- [ ] Inspect `src/Modules/Booking` → confirm **provider booking read** paths + join-request paths.
- [ ] Inspect `src/Modules/Finance` → confirm earnings/payouts/dispute read paths for a vendor.
- [ ] Inspect `src/Modules/ContentBlogs` → confirm creator-profile, follower, comment, reaction paths.
- [ ] Add `WebPermission.Provider.*` / `WebPermission.Guide.*` nested groups; confirm the backend role/permission names they map to.
- [ ] Port the template `agent-*` dashboard chrome into `Areas/Provider/Shared/_ProviderLayout.cshtml` + `_ProviderSidebar.cshtml` + `ProviderSidebarVm.cs`.

**Acceptance:** a stub `Provider/Dashboard` renders inside the ported shell with the real sidebar nav and no DI errors.

---

## Phase 1 — Provider Dashboard area 🆕 `Areas/Provider`

### Endpoints

| Page | Endpoints |
|---|---|
| Dashboard | `GET /tours/mine` · `GET /booking/provider` · `GET /finance/earnings/summary` · `GET /analytics/popular/tours` · `GET /social/reviews/ratings` |
| Listings | `GET /tours/mine` · `DELETE /tours/{id}` · `PATCH /tours/{id}/status` |
| Tour Create/Edit | `POST /tours` · `PUT /tours/{id}` · `POST·PUT·DELETE /tours/{id}/schedules` · `…/pricing-tiers` · `…/waypoints` · `PUT /tours/{id}/children-info` · `POST·PUT·DELETE /tours/{id}/packages` · `POST /tours/{id}/guides` · `GET /content-core/categories`·`/tags`·`/languages` · `POST /attachments` |
| Bookings | `GET /booking/provider` · `GET /booking/{id}` · `POST /booking/{id}/confirm` · `POST /booking/{id}/reject` |
| Earnings | `GET /finance/earnings` · `GET /finance/payouts` · `GET /finance/disputes` |
| Reviews | `GET /social/reviews/{entityType}/{entityId}` · `POST /social/reviews/{id}/response` · `POST /social/reviews/{id}/report` |
| Settings | `GET·PUT /accounts/profile` · `PUT /security/account/password`·`/account/phone` · `GET /auth/sessions` · `DELETE /auth/sessions/{id}` · `POST·DELETE /auth/external-providers` |
| Activities | `GET /analytics/interactions` |

> ⚠️ `POST/PUT/DELETE /tours/*` and `GET /booking/provider`, `/finance/*` paths are **confirm-in-Phase-0** (ContentTours/Booking/Finance modules).

### Files (layered — under `Areas/Provider/`)

> Each type lives in its folder: `Controllers/`, `Facades/`, `ApiClients/`, `Models/{Feature}/` (DTOs + VMs + Mapper), `Views/{Controller}/`. Shared chrome in `Areas/Provider/Shared/`.

- [ ] **Dashboard** — `ApiClients/ProviderDashboardApiClient.cs` · `Facades/ProviderDashboardFacade.cs` · `Controllers/DashboardController.cs` (Pattern A, composes the summary facades) · `Models/Dashboard/` (responses + `DashboardVm` + mapper) · `Views/Dashboard/Index.cshtml` (KPI cards + charts + upcoming table) ← `agent-dashboard.html`
- [ ] **Listings** — `ApiClients/ListingsApiClient.cs` · `Facades/ListingsFacade.cs` · `Controllers/ListingsController.cs` · `Models/Listings/` (paged `ListingListResponse`, VMs, mapper) · `Views/Listings/Index.cshtml` (cards + Edit/Delete) ← `agent-listings.html`
- [ ] **Tour Create/Edit** — `ListingsController` Create/Edit actions · `Models/Listings/` (form VMs + schedule/pricing-tier/waypoint/children-info/package requests) · `Views/Listings/Create.cshtml`, `Edit.cshtml`, `Added.cshtml` + `Views/Listings/Partials/{_BasicTab,_SchedulesTab,_PricingTiersTab,_WaypointsTab,_ChildrenInfoTab,_PackagesTab,_MediaTab}.cshtml` ← `add-listing.html` + `listing-added.html`
- [ ] **Bookings** — `ApiClients/BookingsApiClient.cs` · `Facades/BookingsFacade.cs` · `Controllers/BookingsController.cs` · `Models/Bookings/` · `Views/Bookings/Index.cshtml` + `Detail.cshtml` ← `agent-bookings.html`
- [ ] **Earnings** — `ApiClients/EarningsApiClient.cs` · `Facades/EarningsFacade.cs` · `Controllers/EarningsController.cs` · `Models/Earnings/` · `Views/Earnings/Index.cshtml` ← `agent-earnings.html`
- [ ] **Reviews** — `ApiClients/ReviewsApiClient.cs` · `Facades/ReviewsFacade.cs` · `Controllers/ReviewsController.cs` · `Models/Reviews/` · `Views/Reviews/Index.cshtml` + `Views/Reviews/Partials/_ReplyForm.cshtml` ← `agent-reviews.html`
- [ ] **Settings** — `Controllers/SettingsController.cs` (Pattern A, composes existing Accounts/Auth/Security facades) · `Models/Settings/` · `Views/Settings/Index.cshtml` ← `agent-settings.html`
- [ ] **Activities** — `ApiClients/ActivitiesApiClient.cs` · `Facades/ActivitiesFacade.cs` · `Controllers/ActivitiesController.cs` · `Models/Activities/` · `Views/Activities/Index.cshtml` ← `agent-activities.html`
- [ ] **Shared** — `Shared/_ProviderLayout.cshtml`, `Shared/_ProviderSidebar.cshtml`, `Shared/ProviderSidebarVm.cs`

**Acceptance:** a provider can create a tour with schedules + pricing tiers + waypoints, see it under Listings, view incoming bookings, reply to a review, and read earnings.

---

## Phase 2 — Guide Console 🆕 `Areas/Guide`

No guide template — clone the Provider/`agent-*` shell into `_GuideLayout`/`_GuideSidebar`.

| Page | Template | Endpoints |
|---|---|---|
| Dashboard | clone `agent-dashboard.html` | `GET /guides/{id}/tours` · `GET /tours/{tour}/schedules` |
| My Tours | clone `agent-listings.html` | `GET /guides/{id}/tours` |
| Schedule | clone `agent-bookings.html` table | `GET /tours/{tour}/schedules` |
| Profile edit | `agent-settings.html` profile block | `GET·PUT /guides/{id}` · `GET /content-core/specializations` |

**Files** (layered — under `Areas/Guide/`)
- [ ] `Controllers/{DashboardController,ToursController,ScheduleController,ProfileController}.cs`
- [ ] `Facades/` + `ApiClients/` per concern (`GuideToursApiClient`/`Facade`, `GuideProfileApiClient`/`Facade`, …)
- [ ] `Models/{Dashboard,Tours,Schedule,Profile}/`
- [ ] `Views/{Dashboard,Tours,Schedule,Profile}/Index.cshtml`
- [ ] `Shared/_GuideLayout.cshtml` + `_GuideSidebar.cshtml` + `GuideSidebarVm.cs`

**Acceptance:** a guide sees their assigned tours + upcoming departures and can edit bio/specializations.

---

## Phase 3 — Creator identity & blog social

Extends the existing **Content** area (layered by type).

| Page | Template | Endpoints |
|---|---|---|
| Public Creator Profile (+follow) | `team.html` + `blog-detail.html` author box | `GET /blogs/profiles/{slug}` · `GET /blogs/profiles/{profileId}/followers` · `GET /blogs/profiles/{slug}/blogs` · `POST·DELETE` follow |
| Comments thread (on blog detail) | `blog-detail.html` | `GET /blogs/{id}/comments` · `POST /blogs/{id}/comments` · reply · `POST·DELETE` reaction |
| Creator post composer | clone `add-listing.html` stepper | `POST /blogs` · `PUT /blogs/{id}` · `GET /blogs/niches` · `GET /content-core/categories`·`/tags` |

**Files** (layered — under `Areas/Content/`)
- [ ] `Controllers/CreatorsController.cs` + `ApiClients/CreatorProfileApiClient.cs` + `Facades/CreatorProfileFacade.cs` + `Models/Creators/` + `Views/Creators/Profile.cshtml`
- [ ] `Views/Blogs/Partials/_CommentThread.cshtml` + `_CommentForm.cshtml` wired into `Views/Blogs/Details.cshtml` (+ `CommentsApiClient`/`Facade` + `Models/Comments/`)
- [ ] `Views/Creators/Compose.cshtml` authoring form (form VM `set` + DataAnnotations; rich-text via existing `Helpers/ContentHtmlSanitizer`)

> **Decision:** composer can live in `Content` (`CreatorsController`, chosen here) or a future Creator dashboard area. Confirm before Phase 3.

**Acceptance:** a visitor can follow a creator and comment on a post; a creator can publish a post.

---

## Phase 4 — Group booking / join requests

| Page | Template | Endpoints |
|---|---|---|
| My Join Requests | `account-bookings.html` shell | `GET /booking/join-requests` |
| Request to Join (modal) | `tour-detail.html` | `POST /booking/join-requests` |

**Files** (layered — under `Areas/Accounts/`)
- [ ] `ApiClients/JoinRequestsApiClient.cs` · `Facades/JoinRequestsFacade.cs` · `Controllers/JoinRequestsController.cs` · `Models/JoinRequests/` · `Views/JoinRequests/Index.cshtml` + sidebar nav entry
- [ ] Global shared `~/Views/Shared/_JoinRequestModal.cshtml` included on `Public/Tours/Detail`

**Acceptance:** a user requests to join a group departure and tracks status under their account.

---

## Phase 5 — Admin SEO/FAQ polish (optional)

| Page | Template | Endpoints |
|---|---|---|
| SEO / FAQ management | `admin-settings.html` + `faq.html` | `GET·POST·PUT·DELETE /seo/faqs` · `GET·PUT /seo/page-metadata` |
| Sitemap / weather | — | `GET /seo/sitemap.xml` · `GET /seo/sitemaps/{entityType}.xml` · `GET /seo/weather` |

- [ ] Add a `Seo` controller to the Admin area (Admin `Modules/` + Form/Table pattern + `Validators/`) only if not already covered by existing `Translations`/`Statistics`.

---

## Out of scope / blocked

- `account-travelers.html` — no backend traveler endpoint.
- `account-payment-details.html` — no customer saved-card API (Finance = admin-only).
- `hotel-*`, `flight-*`, `cab-*`, `room-detail.html`, `index-resort/-hotel-chain/-flight/-cab.html` — not part of the tour API.
- `compare-listing.html`, `pricing.html`, `offer-detail.html` — Phase-3+/marketing; revisit later.

---

## Sequencing & effort

1. **Phase 1 Provider** — largest surface, gates supply-side content. *(~2 sprints)*
2. **Phase 2 Guide** — small, reuses Provider shell. *(~0.5 sprint)*
3. **Phase 3 Creator/social** — extends Content. *(~1 sprint)*
4. **Phase 4 Join requests** — small. *(~0.5 sprint)*
5. **Phase 5 Admin SEO** — optional polish.

**Highest value + highest effort:** Phase 1 Tour Create/Edit (the schedules/pricing-tiers/waypoints tabs).
**Confirm-first items:** provider/guide write paths (ContentTours/Booking/Finance) in Phase 0, and creator-composer placement (Content `CreatorsController` vs a future Creator area).
