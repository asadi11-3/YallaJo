# Admin Moderation & Oversight — Build Plan

> **Scope:** Staff-side moderation + oversight surfaces in `Areas/Admin` — the counterpart to the customer/provider self-service plans.
> **Companion docs:** `UI-UX-Design.md` (§4.5 Admin routes, §22 template audit), `Provider-Guide-Creator-Plan.md` (Phase 5 Admin SEO — cross-linked, not duplicated), `Booking-Payment-Finance-Plan.md` (customer dispute half).
> **Authoring rules:** `CONTROLLER_AUTHORING_GUIDE.md` (authoritative) — supersedes `WEB_LAYER_GUIDE.md`.
> **API base:** `/api/v1` · **Status:** Planned.

## Why this plan

The Admin area is already very complete for **CRUD of lookups** (Languages, Categories, Tags, Places, Roles, Users + lifecycle, Translations, Invitations, Specializations, Statistics, Payments, Trips, AuditLogs). What it lacks is the **transactional / moderation counterpart** to everything the customer and provider plans build:

- Customers submit **reviews & reports** → no staff queue to moderate them.
- Providers/creators publish **tours & blogs** → no approval/rejection queue.
- Customers make **bookings** → no admin oversight list/detail.
- Customers open **disputes** (Tier A) → no staff resolution/escalation surface.
- Customers create **support tickets** → no staff inbox to answer them.

These map to real, unwired template pages (`admin-reviews.html`, `admin-booking-list.html`, `admin-booking-detail.html`, `admin-guest-list.html`, `admin-guest-detail.html`) and real backend endpoints with no UI home.

## Architecture conventions (layered by type)

Follows `CONTROLLER_AUTHORING_GUIDE.md`, consistent with the existing Admin features:

- **Layout:** `Areas/Admin/{Controllers/, Facades/, ApiClients/, Models/{Feature}/ (DTOs+VMs+Mapper grouped), Views/{Controller}/ + Views/{Controller}/Partials/}`. Admin-only extras: `Validators/` (FluentValidation) + `Helpers/`. Area-shared chrome already exists (`_AdminLayout`/`_AdminSidebar`). **P0 confirms** whether existing admin features resolve views through the `Areas/Admin/Modules/{module}/` nesting or flat `Views/{Controller}/` — new features MATCH the existing ones.
- **Pipeline:** four-tier `Controller → Facade → ApiClient → IApiClient`. Controllers never touch `IApiClient`; facades never touch `HttpContext`/`TempData`.
- **Errors as values:** every call returns `ApiResult`/`ApiResult<T>`; after each facade call `if (GuardSignOut(result) is { } so) return so;`.
- **DI:** `AddFeatureServices()` reflection-registers `*ApiClient`/`*Facade` by suffix — no manual `AddScoped`.
- **Controllers:** inherit `BaseController`; class attrs `[Area("Admin")] [Authorize] [RequirePermission(WebPermission.Admin.X.Read)]`; reads `[HttpGet]`, writes `[HttpPost]+[ValidateAntiForgeryToken]` + stricter `[RequirePermission(...Moderate/Resolve)]`; PRG + `SetSuccess`/`SetError`.
- **Composite dashboards** (e.g. a moderation queue mixing reviews + reports counts) use **Pattern A** (controller composes sibling facades) or **Pattern C** (ViewComponent) for cross-page KPI widgets.
- **Permissions:** add nested groups to `Infrastructure/Authorization/WebPermission.cs` — `Admin.Reviews`, `Admin.Reports`, `Admin.ContentApproval`, `Admin.Bookings`, `Admin.Disputes`, `Admin.Support` — strings `Permission.{Feature}.{Action}` matching backend `AppPermission.NameFor()`. Never inline a permission string.
- **Pagination:** every queue is paged — per-feature `Response { Items, PageNumber, PageSize, TotalCount, HasPreviousPage, HasNextPage }`; pager driven only by `HasPrevious/HasNext`; preserve status/filter across page links via `asp-route-*`.
- **No output cache** on moderation queues (staff-specific, must be fresh).
- **Build:** `dotnet build src\Hosts\YallaJo.Web\YallaJo.Web.csproj` **ALONE** (CS2012 VBCSCompiler lock).

### Design & UI skills (mandatory for all views)

- **ui-ux-pro-max + impeccable** — queue layout, scannable rows, status hierarchy, bulk-action affordances, empty/error states, a11y, responsive tables→cards.
- **design-taste-frontend** — component architecture for queue rows / moderation modals / KPI cards + hardware-accelerated CSS.
- **huashu-design** — hi-fi HTML variants for the review-moderation and dispute-resolution screens before committing markup.
- Stay within the Bootstrap 5 **Booking** template assets in `wwwroot/assets` (reuse the existing `admin-*.html` chrome).

## Template → page wiring

| Template page | New Admin route | Feature |
|---|---|---|
| `admin-reviews.html` | `/admin/reviews` | Reviews & reports moderation |
| _(no template — clone admin list)_ | `/admin/content-approvals` | Tour & blog approval queue |
| `admin-booking-list.html` | `/admin/bookings` | Bookings oversight list |
| `admin-booking-detail.html` | `/admin/bookings/{id}` | Booking detail |
| `admin-guest-list.html` / `admin-guest-detail.html` | _confirm vs existing `/admin/users`_ | Guest oversight (P0 decide reuse) |
| _(repurpose admin shell)_ | `/admin/disputes` | Dispute resolution |
| _(repurpose admin shell)_ | `/admin/support` | Support staff queue |

## Phase 0 — Grounding (do first, no UI yet)

- [ ] Read existing `Areas/Admin/Controllers/UsersController.cs` + `PlacesController.cs` (and their Facades/ApiClients/Models/Views/Partials) as the **reference slices** — copy their exact layout, view-resolution nesting, validator + modal-partial patterns.
- [ ] Inspect backend modules for **exact admin paths + permission names** (do NOT guess): `src/Modules/Social` (review moderation + reports), `src/Modules/ContentTours` + `src/Modules/ContentBlogs` (approval/publication state), `src/Modules/Booking` (admin reads), `src/Modules/Finance` (dispute review/resolve/escalate), `src/Modules/Messaging` (staff ticket queue).
- [ ] Decide: does `admin-guest-*` add anything beyond existing `/admin/users`? If not, drop it and note in §Out-of-scope.
- [ ] Add `WebPermission.Admin.{Reviews,Reports,ContentApproval,Bookings,Disputes,Support}` nested groups.
- [ ] Confirm Admin view-location nesting (`Modules/` vs flat) and add nav entries to `_AdminSidebar`.
- [ ] For any endpoint that does **not** exist, file the gap in `Agents/Gaps/{Module}-Gaps.md` and mark the phase ⛔ backend-blocked rather than inventing a route.

## Phase 1 — Reviews & reports moderation (`admin-reviews.html`)

| Endpoint | Use |
|---|---|
| `GET /social/reviews` (admin, `?status=&entityType=&page=`) | Moderation list (tabs All/Published/Hidden/Reported) |
| `GET /social/reviews/{entityType}/{entityId}` | Reviews for one entity |
| `GET /social/reviews/ratings` | Distribution bars / KPI header |
| `POST /social/reviews/{id}/hide` · `/restore` _(confirm verbs in P0)_ | Hide / restore a review |
| `GET /social/reports` (queue) · `GET /social/reports/{id}` | Reports queue + detail |
| `POST /social/reports/{id}/resolve` · `/dismiss` _(confirm)_ | Resolve / dismiss a report |

**Files** (under `Areas/Admin/`)
- [ ] `ApiClients/ReviewModerationApiClient.cs`, `ApiClients/ReportsApiClient.cs`
- [ ] `Facades/ReviewModerationFacade.cs`, `Facades/ReportsFacade.cs`
- [ ] `Controllers/ReviewsController.cs` (Pattern A — composes both facades + ratings)
- [ ] `Models/Reviews/` — paged `ReviewModerationResponse`, `ReportResponse`, `ReviewQueueVm`, `ReportQueueVm`, `ReviewModerationMapper`
- [ ] `Views/Reviews/Index.cshtml` (tabs + KPI bars) + `Partials/_ReviewRow.cshtml`, `Partials/_ReportRow.cshtml`, `Partials/_ModerateModal.cshtml`
- [ ] `_AdminSidebar` nav entry (`<permission require="@WebPermission.Admin.Reviews.Read">`)

**Acceptance:** staff can filter reviews by status, hide/restore with PRG flash, work a reports queue with paging; all writes antiforgery + permission-gated; 401→`/auth/login`.

## Phase 2 — Content approval queue (tours + blogs)

| Endpoint | Use |
|---|---|
| `GET /tours?status=pending` _(or admin variant — confirm)_ | Pending tours queue |
| `POST /tours/{id}/approve` · `/reject` _(confirm)_ | Approve / reject tour |
| `GET /blogs?status=pending` _(confirm)_ | Pending blogs queue |
| `POST /blogs/{id}/approve` · `/reject` _(confirm)_ | Approve / reject blog |
| `GET /content-core/niches` / `/categories` | Filter facets |

**Files**
- [ ] `ApiClients/ContentApprovalApiClient.cs` · `Facades/ContentApprovalFacade.cs`
- [ ] `Controllers/ContentApprovalsController.cs` (tabs Tours/Blogs)
- [ ] `Models/ContentApprovals/` — paged responses, `ApprovalQueueVm`, `RejectReasonVm` (set + DataAnnotations), `ContentApprovalMapper`
- [ ] `Views/ContentApprovals/Index.cshtml` + `Partials/_ApprovalRow.cshtml`, `Partials/_RejectModal.cshtml`
- [ ] `Validators/RejectReasonValidator.cs`

**Acceptance:** pending items list with paging; approve = one click + flash; reject requires a reason (validated); permission `Admin.ContentApproval`.

> ⛔ If the backend has no explicit pending/approve/reject surface (publication may be provider-controlled), mark this phase blocked and file `Agents/Gaps/ContentTours-Gaps.md` / `ContentBlogs-Gaps.md`.

## Phase 3 — Bookings oversight (`admin-booking-list.html` / `admin-booking-detail.html`)

| Endpoint | Use |
|---|---|
| `GET /booking` (admin all, `?status=&page=`) _(confirm admin read)_ | Bookings list + KPIs (New/Cancelled/Completed) |
| `GET /booking/{id}` | Booking detail (traveler, schedule, payment, status) |
| `POST /booking/{id}/status` _(confirm — override/refund admin actions)_ | Admin status action (if exposed) |

**Files**
- [ ] `ApiClients/BookingsOversightApiClient.cs` · `Facades/BookingsOversightFacade.cs`
- [ ] `Controllers/BookingsController.cs`
- [ ] `Models/Bookings/` — paged `AdminBookingResponse`, `AdminBookingDetailResponse`, `AdminBookingVm`, `AdminBookingDetailVm`, `Mapper`
- [ ] `Views/Bookings/Index.cshtml` (tabs All/Booked/Cancelled/Completed + KPI cards) + `Details.cshtml` + `Partials/_BookingRow.cshtml`

**Acceptance:** read-only oversight list + detail with paging/filter; money rendered verbatim from server (never recomputed); status actions only if backend exposes them, else read-only.

## Phase 4 — Dispute resolution (Finance, staff half)

| Endpoint | Use |
|---|---|
| `GET /finance/disputes` (open/all, `?status=&page=`) | Disputes queue |
| `GET /finance/disputes/{id}` | Dispute detail + evidence |
| `POST /finance/disputes/{id}/review` | Move to under-review |
| `POST /finance/disputes/{id}/resolve` | Resolve (refund/deny) |
| `POST /finance/disputes/{id}/escalate` | Escalate |

> Customer-side (`GET my disputes`, `POST open-dispute`) is owned by `Booking-Payment-Finance-Plan.md` Phase 4 — **not duplicated here**; this is the staff resolution counterpart only.

**Files**
- [ ] `ApiClients/DisputesApiClient.cs` · `Facades/DisputesFacade.cs`
- [ ] `Controllers/DisputesController.cs`
- [ ] `Models/Disputes/` — paged `DisputeResponse`, `DisputeDetailResponse`, `DisputeQueueVm`, `ResolveDisputeVm` (set + DataAnnotations), `Mapper`
- [ ] `Views/Disputes/Index.cshtml` + `Details.cshtml` + `Partials/_ResolveModal.cshtml`, `_EscalateModal.cshtml`
- [ ] `Validators/ResolveDisputeValidator.cs`

**Acceptance:** queue → detail → review/resolve/escalate state transitions, each antiforgery + `Admin.Disputes` gated, PRG flash, optimistic-free (re-fetch detail after action).

## Phase 5 — Support staff queue (Messaging)

| Endpoint | Use |
|---|---|
| `GET /support/tickets` (admin/all, `?status=&page=`) | Staff ticket inbox |
| `GET /support/tickets/{id}` | Ticket thread |
| `POST /support/tickets/{id}/messages` | Staff reply |
| `POST /support/tickets/{id}/assign` _(confirm)_ | Assign to agent |
| `POST /support/tickets/{id}/close` | Close |

> Customers only **create** tickets (Accounts plan + `Public/Contact`). This is the staff answering side.

**Files**
- [ ] `ApiClients/SupportQueueApiClient.cs` · `Facades/SupportQueueFacade.cs`
- [ ] `Controllers/SupportController.cs`
- [ ] `Models/Support/` — paged `TicketResponse`, `TicketThreadResponse`, `TicketQueueVm`, `StaffReplyVm` (set + DataAnnotations), `Mapper`
- [ ] `Views/Support/Index.cshtml` (inbox) + `Details.cshtml` (thread + reply form) + `Partials/_TicketRow.cshtml`, `_Message.cshtml`

**Acceptance:** staff can triage by status, open a thread, reply (antiforgery), close; paging; `Admin.Support` gated.

## Out of scope / blocked

- Customer-facing review submit / dispute open / ticket create — owned by other plans.
- `admin-guest-*` if it duplicates existing `/admin/users` (decide P0).
- `admin-earnings.html` admin finance dashboard — already covered by existing `Admin/Payments` + `Admin/Statistics` (confirm; extend only if a gap is found).
- Any moderation endpoint the backend does not expose → file in `Agents/Gaps/` and block the phase.

## Sequencing & effort

P0 grounding → **P1 Reviews/Reports** (highest value, real template) → **P5 Support** (high value, Messaging ready) → **P4 Disputes** (pairs with Tier A) → **P3 Bookings oversight** (read-heavy, low risk) → **P2 Content approval** (do last; most likely backend-blocked). Effort: P1/P5 medium, P3 low, P4 medium, P2 small-but-uncertain.

## Open questions

1. Admin view-location nesting — `Modules/{module}/` vs flat `Views/{Controller}/`? (match existing in P0.)
2. Exact admin endpoint paths + permission names in Social/ContentTours/ContentBlogs/Booking/Finance/Messaging — confirm before coding.
3. Does the backend expose tour/blog **approval** at all, or is publication provider-self-service? (gates P2.)
4. Does `admin-guest-*` warrant its own feature or fold into `/admin/users`?
