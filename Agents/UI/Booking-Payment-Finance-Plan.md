# Booking · Payment · Finance — Build Plan (Tier A)

> **Scope:** the transactional core — complete the **availability → slot → participants → payment → confirmation** flow, plus bookings management (cancel-with-refund) and customer **disputes**. This is the highest-priority block after `Accounts-Notifications-Support-Reviews-Plan.md` and `Provider-Guide-Creator-Plan.md`.
> **Companion docs:** `UI-UX-Design.md` §22 (template reality audit) + Wave-5 (Booking & Payment), `Agents/Plans/Booking-*`, `Agents/Plans/Finance-*`.
> **Authoring rules:** `CONTROLLER_AUTHORING_GUIDE.md` (authoritative — supersedes `WEB_LAYER_GUIDE.md`).
> **Template root:** `C:\Users\admin1\Desktop\Template\hotel-management-syste-main\booking.webestica.com`
> **API base path:** `/api/v1`
> **Status:** 📋 Planned — not started

---

## Why this plan

`Public/Booking/Book` (from `tour-booking.html`) and `Public/Booking/Confirmation` (`booking-confirm.html`) exist only as **scaffolds**, and `Accounts/Bookings` lists bookings but has **no cancel-with-refund** path. The single biggest functional hole in the product is the **checkout → payment → confirmation → refund** journey — without it the catalog cannot transact. Wave-5 flagged **~15 missing Booking/Payment endpoints** (availability slots, refund policies, provider reads, join-requests), so this plan is **part UI build, part backend-dependency confirmation**.

`account-payment-details.html` (saved cards) stays **out of scope** — there is no customer saved-card API (Finance card vaulting is admin/gateway-side). Payment is collected **in-flow** at checkout, not stored per-account.

---

## Architecture conventions (read before building)

Identical to the Accounts & Provider plans — all areas are **layered by type** per `CONTROLLER_AUTHORING_GUIDE.md`:

- **Layout (folder = namespace):** under `Areas/{Area}/` place `Controllers/`, `Facades/`, `ApiClients/`, `Models/{Feature}/` (DTOs + ViewModels + Mapper grouped — **no** `Requests/`·`Responses/`·`ViewModels/`·`Mappers/` subfolders), `Views/{Controller}/{View}.cshtml`; per-feature partials → `Views/{Controller}/Partials/_X.cshtml`. No expander code, no `Program.cs` change.
- **Four-tier pipeline:** `Controller → Facade → ApiClient → IApiClient`. Controllers never inject `IApiClient`/`HttpClient` (only Facades); Facades never touch `HttpContext`/`TempData`. **Errors are values** → `ApiResult`/`ApiResult<T>` (flags `IsUnauthorized`/`IsConflict`/`IsNotFound`/`IsValidationError`, `RequireSignOut`), never exceptions.
- **DI:** `AddFeatureServices()` auto-registers `*ApiClient` / `*Facade` scoped **by name suffix** — keep suffixes, no manual `AddScoped`.
- **Controllers:** inherit `BaseController`; `[Area("…")]` + `[Authorize]` (checkout is per-user); async + trailing `CancellationToken ct`; `[HttpGet]` reads, `[HttpPost]` + `[ValidateAntiForgeryToken]` writes; `if (GuardSignOut(result) is { } signOut) return signOut;` after each facade call; **PRG** + `SetSuccess`/`SetError`.
- **Money discipline:** never compute price totals in the view or controller — render **server-returned** amounts (base / discount / taxes / payable) verbatim from the price-summary DTO. Re-fetch the authoritative quote before payment; never trust a posted total.
- **Pagination:** per-feature paged `Response` (`Items, PageNumber, PageSize, TotalCount, HasPreviousPage, HasNextPage`); pager from flags only.
- **Views:** `@model ...Areas.{Area}.Models.{Feature}.{Vm}`; explicit `asp-area`/`asp-controller`; `@Html.AntiForgeryToken()` in POST forms; shared `_Alerts` for flash; `<partial>` / `Html.PartialAsync` only; `<partial name="_ValidationScriptsPartial"/>` in `@section Scripts`.
- **Caching:** booking/payment pages are **per-user and price-sensitive → no output cache**. Public availability reads may use a very short (≤30s) cache only if the backend marks them cacheable.
- **Build check:** `dotnet build src\Hosts\YallaJo.Web\YallaJo.Web.csproj` **run alone** (`CS2012` VBCSCompiler lock).

### Design & UI skills (mandatory for all views)

- **`ui-ux-pro-max`** + **`impeccable`** — stepper layout, trust/security cues at the pay step, error/empty/declined states, mobile checkout.
- **`design-taste-frontend`** — component architecture + performant, hardware-accelerated CSS; overrides default LLM design biases.
- **`huashu-design`** — hi-fi HTML exploration / variants of the checkout stepper before committing markup; anti-AI-slop pass.

Constraint: stay within the Bootstrap 5 **Booking** template assets (`wwwroot/assets`, the `tour-booking.html` stepper + `booking-confirm.html` success card) and the layered Razor conventions above.

---

## Template → page wiring (master map)

| Page | Area / route | Template source | Reuse note |
|---|---|---|---|
| Checkout stepper | `Public/Booking/Book` | `tour-booking.html` | 3-step: Tour Review → Traveler Info → Make Payment; add availability/slot step |
| Booking confirmation | `Public/Booking/Confirmation` | `booking-confirm.html` | success card (id, method, total, date, guests) + Download PDF/Share |
| My Bookings (+cancel) | `Accounts/Bookings` | `account-bookings.html` | already built — add cancel-with-refund modal + refund-quote |
| My Disputes | `Accounts/Disputes` | `account-payment-details.html` shell (repurposed) | open dispute against a payment + track status |

---

## Phase 0 — Grounding & backend confirmation (do first)

- [ ] Read the existing `Public/Booking` slice (`ApiClients/` → `Facades/` → `Controllers/BookingController` → `Views/Booking/{Book,Confirmation}.cshtml`) and `Models/Booking/{BookingApiContracts,BookingVm}` — know what already round-trips.
- [ ] Inspect `src/Modules/Booking` controllers → confirm **exact paths** for: availability (`GET /booking/availability/{tourId}`, `GET /booking/availability/{tourId}/{date}`), create (`POST /booking/tour`), `GET /booking/my-bookings`, `GET /booking/{id}`, `POST /booking/{id}/cancel`, and whether **per-date slot capacity / refund-policy** endpoints exist (Wave-5 gaps).
- [ ] Inspect `src/Modules/Finance` → confirm the **customer payment** surface: is there a `POST /finance/payments` / checkout-intent, a gateway redirect/confirm callback, or does `POST /booking/tour` itself return a payment reference? Confirm customer dispute paths (`GET` my disputes, `POST` open-dispute-against-a-payment).
- [ ] **Decide the payment integration mode** (see Open questions) — hosted-redirect vs in-page gateway element vs pay-on-confirm. This gates Phase 2.
- [ ] Add `WebPermission.Booking.*` / `WebPermission.Finance.*` nested groups if these are permission-gated (most are `[Authorize]` self-service).
- [ ] Record any missing backend endpoints as ⛔ blockers and surface them in `Agents/Plans/Booking-*` / `Finance-*` for the API team.

**Acceptance:** a written confirmation of which endpoints exist vs are gaps; a chosen payment mode.

---

## Phase 1 — Availability & checkout stepper (`Public/Booking/Book`)

| Endpoint | Use |
|---|---|
| `GET /tours/slug/{slug}` · `/tours/{tour}/pricing-tiers` | tour summary + price tiers for the review/participants step |
| `GET /booking/availability/{tourId}` | calendar of available departures |
| `GET /booking/availability/{tourId}/{date}` | slots/capacity for a chosen date (public) |
| `POST /booking/tour` | create the booking (participants + selected slot + tier) → returns booking id / payment ref |

**Files** (layered — under `Areas/Public/`)
- [ ] `ApiClients/AvailabilityApiClient.cs` (or extend existing `BookingApiClient`)
- [ ] `Facades/BookingFacade.cs` — extend: availability calendar, slot lookup, create-booking; map price summary verbatim
- [ ] `Controllers/BookingController.cs` — `Book` (GET stepper), `Availability` (GET partial/JSON for the date picker), `Create` (POST → PRG to Confirmation)
- [ ] `Models/Booking/` — `AvailabilityResponse`, `SlotResponse`, `CreateBookingRequest` (participants + slotId + tierId), `PriceSummaryResponse`, `BookingStepperVm`, `ParticipantFormVm` (form VM: `set` + DataAnnotations), `BookingMapper`
- [ ] `Views/Booking/Book.cshtml` (3-step stepper) + `Views/Booking/Partials/{_ReviewStep,_TravelerStep,_AvailabilityPicker,_PriceSummary}.cshtml`

**Acceptance:** pick a date → see slots → enter travelers → a booking is created and the user lands on Confirmation; price summary matches the server quote exactly.

---

## Phase 2 — Payment / checkout step + confirmation

> Shape depends on Phase 0's payment-mode decision. Below assumes a **checkout-intent + confirm** model; adapt to hosted-redirect if that's what the backend exposes.

| Endpoint | Use |
|---|---|
| `POST /finance/payments` *(confirm path in P0)* | create payment intent for a booking |
| gateway confirm / callback *(confirm in P0)* | finalize payment → mark booking paid |
| `GET /booking/{id}` | render the confirmation card from authoritative booking state |

**Files** (layered — under `Areas/Public/`)
- [ ] `ApiClients/PaymentApiClient.cs` · `Facades/PaymentFacade.cs`
- [ ] `Controllers/BookingController.cs` — `Pay` (POST → intent), `PaymentReturn`/`Confirmation` (GET, idempotent render)
- [ ] `Models/Booking/` — `PaymentIntentResponse`, `PayRequest`, `ConfirmationVm`
- [ ] `Views/Booking/Partials/_PaymentStep.cshtml` (gateway element or redirect button; **no raw PAN handling in our app** unless PCI-scoped) + `Views/Booking/Confirmation.cshtml` ← `booking-confirm.html`

**Acceptance:** completing payment marks the booking paid and shows the confirmation card (id, method, total, tour date, guests) with Download-PDF/Share; refreshing Confirmation is idempotent.

---

## Phase 3 — Bookings management: cancel-with-refund (`Accounts/Bookings`)

| Endpoint | Use |
|---|---|
| `GET /booking/my-bookings` · `GET /booking/{id}` | list / detail (already wired — verify) |
| refund-policy / quote *(confirm in P0)* | show refund amount before confirming cancel |
| `POST /booking/{id}/cancel` | cancel (+ trigger refund server-side) |

**Files** (layered — under `Areas/Accounts/`)
- [ ] `Facades/BookingsFacade.cs` — add refund-quote + cancel
- [ ] `Controllers/BookingsController.cs` — add `Cancel` (POST + antiforgery) and a `RefundQuote` GET partial
- [ ] `Models/Bookings/` — `RefundQuoteResponse`, `CancelBookingRequest`
- [ ] `Views/Bookings/Partials/_CancelRefundModal.cshtml` wired into the existing `Views/Bookings/Index.cshtml` / `Detail.cshtml`

**Acceptance:** a user opens a booking, sees the refund amount per policy, confirms cancel, and the booking moves to Canceled.

---

## Phase 4 — Customer disputes (`Accounts/Disputes`)

| Endpoint | Use |
|---|---|
| `GET /finance/disputes` (mine) *(confirm in P0)* | my disputes list |
| `POST /finance/disputes` (against a payment) *(confirm in P0)* | open a dispute |
| `GET /finance/disputes/{id}` | dispute detail / status |

**Files** (layered — under `Areas/Accounts/`)
- [ ] `ApiClients/DisputesApiClient.cs` · `Facades/DisputesFacade.cs` · `Controllers/DisputesController.cs` (Index, Detail, Open)
- [ ] `Models/Disputes/` — `DisputeResponse`, `DisputeListResponse` (paged), `OpenDisputeRequest`, `DisputeVm`, `DisputeMapper`
- [ ] `Views/Disputes/Index.cshtml` + `Detail.cshtml`; add a **Disputes** entry to `AccountSidebarVm` + `_AccountSidebar.cshtml`

**Acceptance:** a user opens a dispute against a paid booking and tracks its status; admin-side resolution stays in the Admin area.

---

## Out of scope / blocked

| Item | Reason |
|---|---|
| `account-payment-details.html` (saved cards) | ⛔ no customer saved-card API; payment is collected in-flow |
| admin dispute resolve/escalate/review | ✅ already Admin-area scope (Admin/Payments) — not customer-facing |
| provider booking confirm/reject | covered by **Provider** plan Phase 1 (Bookings) |
| availability-slot / refund-policy endpoints | ⚠️ may be **backend gaps** (Wave-5) — confirm in Phase 0 before building dependent UI |

---

## Sequencing & effort

1. **Phase 0** — grounding + payment-mode decision *(blocking; ~0.5 day)*.
2. **Phase 1** — availability + stepper *(~1 sprint; largest)*.
3. **Phase 2** — payment + confirmation *(~1 sprint; depends on gateway)*.
4. **Phase 3** — cancel-with-refund *(~0.5 sprint)*.
5. **Phase 4** — disputes *(~0.5 sprint)*.

**Highest risk:** Phase 2 (external payment gateway + any missing backend endpoints). De-risk it in Phase 0.

## Open questions

- **Payment integration mode:** hosted-redirect (Stripe Checkout/HostedPage) vs in-page element vs pay-on-confirm — which does `src/Modules/Finance` expose? Determines whether we ever touch card data (and PCI scope).
- Do per-date **slot capacity** and **refund-policy** endpoints exist, or are they Wave-5 gaps to build backend-first?
- Does `POST /booking/tour` create an unpaid booking (then pay) or require payment atomically? Drives the Phase 1↔2 boundary.
