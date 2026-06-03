# YallaJo Web — Accounts Feature Build Plan
## Notifications · Support · My Reviews (+ Tours/Detail enrichment)

**Status:** 📋 Planned — not started
**Date:** 2025-01-27
**Primary area:** `src/Hosts/YallaJo.Web/Areas/Accounts` (layered by type); Phase 4 touches `Areas/Public`
**Authoring rules:** `CONTROLLER_AUTHORING_GUIDE.md` (authoritative) — supersedes the older `WEB_LAYER_GUIDE.md`
**Companion docs:** `Agents/UI/UI-UX-Design.md` (§22 Template Page Reality Audit) · `Agents/Plans/Messaging-*` · `Agents/Plans/Social-*`
**API base path:** `/api/v1`

---

## 1. Why this plan

A reality check on `YallaJo.Web` shows **~95% of the Webestica template pages already have a controller + view** across the 5 areas (all **layered by type**):

| Area | Status |
|---|---|
| **Auth** (82 files) | ✅ done — sign-in, sign-up, forgot/reset, two-factor, accept-invite, external-providers, sessions, devices |
| **Accounts** (61 files) | ✅ done — profile, settings, delete, bookings, wishlist, change-password, update-phone |
| **Public** (47 files) | ✅ scaffolded — home(index-tour), tours(grid/detail), booking(book/confirm), directory(index/detail), help(center/detail), contact |
| **Content** (88 files) | ✅ scaffolded — blogs, guides, faq, places, search, categories, tags |
| **Admin** (241 files) | ✅ very complete — 18 controllers incl. Users, Roles, Places CRUD, Payments, Statistics, Translations |

The genuine remaining work is **3 backend-ready Account features** plus enrichment wiring on existing pages. This plan builds them.

---

## 2. Architectural constraints (from `CONTROLLER_AUTHORING_GUIDE.md`)

> **This supersedes the earlier `WEB_LAYER_GUIDE.md` feature-first guidance.** Every YallaJo.Web area is **layered by type**, not feature folders.

- **Layout (layered by type, folder = namespace):** within `Areas/Accounts/` place `Controllers/`, `Facades/`, `ApiClients/`, `Models/{Feature}/` (DTOs + ViewModels + Mapper grouped per feature — **no** separate `Requests/`·`Responses/`·`ViewModels/`·`Mappers/` folders), and `Views/{Controller}/{View}.cshtml`. Per-feature partials → `Views/{Controller}/Partials/_X.cshtml`; area-shared partials (e.g. `_AccountSidebar`) → `Areas/Accounts/Shared/`. View discovery already resolves `~/Areas/Accounts/Views/{controller}/{view}.cshtml` — **no expander code, no `Program.cs` change.**
- **Four-tier pipeline:** `Controller → Facade → ApiClient → IApiClient`. Controllers never call `IApiClient` directly and never inject `HttpClient` (only via a Facade). Facades never touch `HttpContext`/`TempData`.
- **Errors are values, not exceptions:** every client/facade method returns `ApiResult` / `ApiResult<T>` (flags `IsUnauthorized`/`IsConflict`/`IsNotFound`/`IsValidationError`, `RequireSignOut`).
- **DI:** `AddFeatureServices()` reflection-registers every class whose name ends in `ApiClient` or `Facade` as scoped **by name suffix** — keep the suffixes, never `AddScoped` manually.
- **DTOs / VMs (in `Models/{Feature}/`):** `Response` = inbound (`init`, camelCase JSON); `Request` = outbound positional record; list/display VMs use `init`, **form VMs use `set` + DataAnnotations**; `Mapper` is a static dumb Response→VM / VM→Request (trims input).
- **Controller rules:** inherit `BaseController`; class `[Area("Accounts")]` + `[Authorize]`; every action `async Task<IActionResult>` with a trailing `CancellationToken ct`; reads `[HttpGet]`, writes `[HttpPost]` + `[ValidateAntiForgeryToken]`; after every facade call `if (GuardSignOut(result) is { } signOut) return signOut;`; **PRG** after a successful write with `SetSuccess`/`SetError`; never re-implement `RedirectToLogin`/`RequireSignOut`.
- **Permissions:** use `WebPermission.*` constants via `[RequirePermission]` / `<permission>` — never inline a permission string.
- **Pagination:** no unbounded lists; each feature declares its **own** paged `Response` (`Items, PageNumber, PageSize, TotalCount, HasPreviousPage, HasNextPage`); the pager is driven only by `HasPreviousPage`/`HasNextPage` (no math in the view).
- **Views:** `@model ...Areas.Accounts.Models.{Feature}.{Vm}`; `Layout = "~/Views/Shared/_Layout.cshtml"`; set `asp-area`/`asp-controller` explicitly; `@Html.AntiForgeryToken()` in every POST form; use shared `_Alerts` for flash (never hand-write alert divs); `<partial name="_ValidationScriptsPartial"/>` in `@section Scripts`; use `<partial>` / `Html.PartialAsync` (never `Html.Partial`). Every page renders the shared `_AccountSidebar` bound to `AccountSidebarVm`.
- **Auth/transport:** JWT attached automatically by `JwtAuthHandler`. **Caching:** all three features are per-user/private → **no output cache**.
- **Build check:** `dotnet build src\Hosts\YallaJo.Web\YallaJo.Web.csproj` **run alone** (parallel builds hit the VBCSCompiler `CS2012` lock).

### 2.1 Design & UI skills (mandatory for all views)

Every view / partial / layout in this plan must be produced **using these skills** — load them before writing markup or CSS:

- **`ui-ux-pro-max`** + **`impeccable`** — layout, visual hierarchy, information architecture, accessibility, responsive behavior, empty/error states.
- **`design-taste-frontend`** — component architecture + performant, hardware-accelerated CSS; overrides default LLM design biases.
- **`huashu-design`** — hi-fi HTML exploration / variants before committing final markup; anti-AI-slop pass.

Constraint: stay within the existing Bootstrap 5 **Booking** template assets (`wwwroot/assets`) and the layered Razor view conventions in §2 — the skills inform *taste and structure*, not a re-platforming.

---

## 3. Already built — do NOT rebuild

The Accounts area already contains these features (each as `Controllers/` + `Facades/` + `ApiClients/` + `Models/{Feature}/` + `Views/{Controller}/`): **Bookings** (Index+Detail), **ChangePassword**, **Delete**, **Profile** (Index + `_UpdateProfileForm`), **Settings** (notification prefs + marketing consent), **UpdatePhone**, **Wishlist**. Area-shared: `Shared/AccountSidebarVm.cs`, `Shared/_AccountSidebar.cshtml`.

---

## 4. Scope decisions (avoid duplication)

- **Notifications = inbox only.** Notification *preferences* are already handled in the Accounts **Settings** feature → this feature is the message inbox + unread badge only.
- **Support = management only.** `Public/Contact` already **creates** tickets → this adds the account-side list / thread / reply / close.
- **Reviews spans two areas.** "My Reviews" management lives in Accounts; the submit form is a **global shared partial** included by `Public/Tours/Detail` and `Public/Directory/Detail`.

---

## 5. Phases

### Phase 0 — Grounding (do once)

- [ ] Confirm the Accounts area uses the **layered-by-type** layout from `CONTROLLER_AUTHORING_GUIDE.md` (`Controllers/` + `Facades/` + `ApiClients/` + `Models/{Feature}/` + `Views/{Controller}/`); if any legacy `Features/` folders remain, follow the layered convention for all new code.
- [ ] Read the Accounts **Wishlist** feature end-to-end as the reference slice (`ApiClients/WishlistApiClient` → `Facades/WishlistFacade` → `Controllers/WishlistController` → `Views/Wishlist/`) — mirror how the Facade maps `ApiResult<T>`→VM and how views bind the sidebar.
- [ ] Inspect backend DTO shapes: `src/Modules/Messaging` (notifications + support) and `src/Modules/Social` (reviews), or the API host endpoint signatures.
- [ ] Confirm the Accounts route prefix (`/Account/...` vs `/Accounts/...`).
- [ ] Add 3 nav entries to `Shared/AccountSidebarVm.cs` + `Shared/_AccountSidebar.cshtml` (Notifications w/ unread badge, Support, My Reviews).
- [ ] Add `WebPermission` nested groups if these features are permission-gated.

### Phase 1 — Notifications inbox (highest value, self-contained)

| Endpoint | Use |
|---|---|
| `GET /notifications` | inbox list (tabs All / Unread, paginated) |
| `GET /notifications/unread-count` | sidebar badge |
| `POST /notifications/{id}/read`, `POST /notifications/read-all` | mark read |
| `DELETE /notifications/{id}`, `DELETE /notifications/batch` | dismiss |

**Files** (layered — under `Areas/Accounts/`)
- [ ] `ApiClients/NotificationsApiClient.cs`
- [ ] `Facades/NotificationsFacade.cs`
- [ ] `Controllers/NotificationsController.cs` (Index, MarkRead, MarkAllRead, Delete, BatchDelete, UnreadCount)
- [ ] `Models/Notifications/` — `NotificationResponse`, `NotificationListResponse` (paged: `Items/PageNumber/PageSize/TotalCount/HasPreviousPage/HasNextPage`), `UnreadCountResponse`, `NotificationVm`, `NotificationListVm`, `NotificationMapper`
- [ ] `Views/Notifications/Index.cshtml` + `Views/Notifications/Partials/_NotificationRow.cshtml`

**Acceptance:** inbox lists items; unread badge renders; read / read-all / delete work; empty state shown.

### Phase 2 — Support tickets (list + thread)

| Endpoint | Use |
|---|---|
| `GET /support/tickets` | my tickets list |
| `GET /support/tickets/{id}` | ticket thread |
| `POST /support/tickets` | new ticket (reuse contact form styling) |
| `POST /support/tickets/{id}/messages` | reply |
| `POST /support/tickets/{id}/close` | close |

**Files** (layered — under `Areas/Accounts/`)
- [ ] `ApiClients/SupportApiClient.cs`
- [ ] `Facades/SupportFacade.cs`
- [ ] `Controllers/SupportController.cs` (Index, Detail, Create, Reply, Close)
- [ ] `Models/Support/` — `CreateTicketRequest`, `ReplyRequest`, `TicketResponse`, `TicketListResponse` (paged), `TicketMessageResponse`, `TicketListVm`, `TicketDetailVm`, `TicketMessageVm`, `SupportMapper`
- [ ] `Views/Support/Index.cshtml` + `Views/Support/Detail.cshtml` + `Views/Support/Partials/_Message.cshtml`

**Acceptance:** list → open thread → reply appends → close changes status; create round-trips.

### Phase 3 — My Reviews + shared submit form (spans Accounts + Public)

| Endpoint | Use |
|---|---|
| `GET /social/reviews/my-reviews` | my reviews list (Accounts) |
| `POST /social/reviews` | submit (form on Public detail pages) |
| `PUT /social/reviews/{id}`, `DELETE /social/reviews/{id}` | edit / remove |
| `POST /social/reviews/{id}/helpful`, `DELETE /social/reviews/{id}/helpful` | helpful toggle |
| `POST /social/reviews/{id}/report` | report |

**Files** (layered — under `Areas/Accounts/`)
- [ ] `ApiClients/ReviewsApiClient.cs`
- [ ] `Facades/ReviewsFacade.cs`
- [ ] `Controllers/ReviewsController.cs` (Index, Create, Edit, Delete)
- [ ] `Models/Reviews/` — `CreateReviewRequest`, `UpdateReviewRequest`, `ReviewResponse`, `ReviewListResponse` (paged), `MyReviewVm`, `ReviewListVm`, `ReviewFormVm` (form VM: `set` + DataAnnotations), `ReviewMapper`
- [ ] `Views/Reviews/Index.cshtml`
- [ ] **Global shared `~/Views/Shared/_ReviewForm.cshtml`** (cross-area partial) included by `Public/Tours/Detail` and `Public/Directory/Detail` via `<partial name="_ReviewForm" model="..."/>`

**Acceptance:** submit from a tour detail → appears in My Reviews → edit / delete work.

### Phase 4 — (optional follow-on) wire `Public/Tours/Detail` enrichment panels

Connect the existing-but-thin tour detail page to its data:
`GET /tours/slug/{slug}` · `/tours/{tour}/schedules` · `/pricing-tiers` · `/waypoints` · `/children-info` · `/tours/{tour}/guides` · `/packages(/{id})` · reviews `GET /social/reviews/{entityType}/{entityId}` + `/ratings` · `GET /booking/availability/{tourId}` · favorite `POST /social/favorites` + `GET /social/favorites/check/...` · `GET /seo/page-metadata` · `POST /interactions`.
> Depends on Phase 3's `_ReviewForm` for the review-submit panel.

---

## 6. Sequencing & effort

- **Order:** Phase 0 → 1 → 2 → 3 → (4). Phases 1–3 are independent after Phase 0 and can be parallelized.
- **Effort:** ~1 feature-slice each for Phases 1–3; Phase 4 is integration-only on an existing page.

---

## 7. Out of scope / backend-blocked

| Page | Reason |
|---|---|
| `account-travelers` | ⛔ no traveler endpoint in the API spec |
| `account-payment-details` | ⛔ customer saved-card CRUD not in spec (Finance = admin-only) |
| hotel / flight / cab / room-detail pages | ⛔ not part of the tour-centric API |

---

## 8. Open questions

- Exact request/response DTO shapes for Messaging (notifications, support) and Social (reviews) — confirm in Phase 0 against the backend modules.
- Accounts route prefix (`/Account` vs `/Accounts`) — confirm in Phase 0.
- Should "create ticket" live in the account Support feature, or keep it solely on `Public/Contact` with the account area linking out?
