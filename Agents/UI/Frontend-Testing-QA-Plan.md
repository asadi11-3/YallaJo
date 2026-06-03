# Frontend Testing & QA — Build Plan

> **Scope:** Automated test coverage for `YallaJo.Web` — facade/mapper unit tests, controller integration tests, and Playwright E2E for critical user journeys.
> **Companion docs:** `CONTROLLER_AUTHORING_GUIDE.md` (the patterns under test), `Agents/Tests/Playwright-*.md` (existing E2E notes + Results/), `CrossCutting-i18n-Performance-A11y-SEO-Plan.md` (Tier D — a11y/perf budgets, shared with E2E axe checks).
> **Authoring rules:** mirror `CONTROLLER_AUTHORING_GUIDE.md` conventions so tests assert the real contracts (ApiResult, GuardSignOut, PRG, permissions, paging).
> **API base:** `/api/v1` · **Status:** Planned.

## Why this plan

The 7 feature plans build a lot of new surface (booking→payment, messaging, moderation, provider/guide consoles) but **none of them own test coverage**. Existing tests are thin (`Web.Tests.Unit` ≈ 17, `Auth.Tests.Unit` ≈ 38, all backend-leaning) and there are `Agents/Tests/Playwright-*.md` notes with no wired harness for the new flows. This plan makes the four-tier architecture **verifiable** and guards the money/auth paths against regressions.

## Architecture conventions (test-side)

- **Unit (xUnit)** under `tests/Web.Tests.Unit` — extend the existing project. Pure, fast, no network:
  - **Facades:** assert `ApiResult → VM` mapping + failure translation (`IsUnauthorized→ForceSignOut`, `IsConflict/IsNotFound/IsValidationError→friendly text`) + cache-eviction call on successful writes. Mock the `*ApiClient` (sealed → wrap behind its constructor `IApiClient`, or extract a thin seam; prefer mocking `IApiClient` and exercising the real ApiClient+Facade pair).
  - **Mappers:** `Response→VM` and `VM→Request`, trimming, null-safety, paged-`Response` flag math is **absent** (assert pager reads `HasPrevious/HasNext` only).
  - **ViewComponents:** invoke + assert the VM (e.g. notification bell unread count, popular-tours rail).
- **Integration (WebApplicationFactory)** under a new `tests/Web.Tests.Integration` — boot the MVC app with a **stubbed API** (mock `IApiClient` / a test `HttpMessageHandler`). Assert controller behavior, not the backend:
  - PRG redirects on successful writes; `GuardSignOut` → 302 `/auth/login` on 401; `[ValidateAntiForgeryToken]` enforced; `[RequirePermission]` → 403/AccessDenied; paging query round-trips filters.
- **E2E (Playwright)** under `tests/Web.E2E` — real browser against a running app + seeded test API. Tie into the existing `Agents/Tests/Playwright-*.md` conventions and write results to `Agents/Tests/Results/`.
  - **a11y:** run `axe` in E2E on each covered page (shared budget with Tier D §P4).
- **Build/run discipline:** build the web csproj **ALONE** (`dotnet build src\Hosts\YallaJo.Web\YallaJo.Web.csproj`) before E2E; CS2012 lock means no parallel build with test projects.

### Design & UI skills (mandatory where tests assert UX)

- **impeccable + ui-ux-pro-max** — define the **acceptance heuristics** the E2E/axe checks encode (focus management in checkout/cancel-refund/start-conversation modals, aria-live toasts, empty/error states, keyboard-operable carousels/typeahead/map).
- **design-taste-frontend** — performance assertions (no CLS from missing width/height, deferred non-critical JS) become measurable E2E checks.
- **huashu-design** — not used at runtime; reference only when a visual-regression baseline is needed.

## Phase 0 — Grounding (do first)

- [ ] Read existing `tests/Web.Tests.Unit` + `tests/Auth.Tests.Unit` to match project style, fixtures, naming.
- [ ] Read `Agents/Tests/Playwright-*.md` + `Agents/Tests/Results/` to reuse the existing E2E conventions/selectors.
- [ ] Decide the **E2E harness**: Playwright for .NET vs Node Playwright (recommend matching whatever `Agents/Tests/` already assumes).
- [ ] Decide **API strategy**: integration = mocked `IApiClient`/`HttpMessageHandler`; E2E = seeded test API instance or recorded fixtures.
- [ ] Decide **test-data/seeding** + auth (a test sign-in helper that mints the BFF cookie) for E2E.
- [ ] Add CI note: web build alone, then unit → integration → E2E gates.

## Phase 1 — Facade + mapper unit tests

| Target | Assertions |
|---|---|
| Every built Facade (Auth, Accounts/*, Public/*, Content/*) | success→VM, each failure flag→friendly/ForceSignOut, cache-evict on write |
| Every Mapper | Response→VM, VM→Request, trim/null-safety |
| Paged Responses | pager uses `HasPrevious/HasNext`; no `TotalPages` |

**Files**
- [ ] `tests/Web.Tests.Unit/Facades/*FacadeTests.cs` (one per facade)
- [ ] `tests/Web.Tests.Unit/Mappers/*MapperTests.cs`
- [ ] `tests/Web.Tests.Unit/TestKit/` — `FakeApiClient` (returns crafted `ApiResult<T>`), VM/DTO builders

**Acceptance:** every facade's failure-translation table covered; mapper round-trips green; runs in seconds; no network.

## Phase 2 — Controller integration tests

| Journey | Assertion |
|---|---|
| Any write action | success → PRG 302 + `_Alerts` success flag |
| 401 from API | `GuardSignOut` → 302 `/auth/login` |
| Missing/!valid antiforgery | request rejected |
| `[RequirePermission]` without claim | 403 / AccessDenied view |
| Paged list | `?page=2&status=x` preserved in pager links |

**Files**
- [ ] `tests/Web.Tests.Integration/` (new xUnit project; `WebApplicationFactory<Program>`)
- [ ] `TestKit/StubApiHandler.cs` (maps routes→canned JSON/status), `TestKit/AuthHelper.cs` (mint cookie + permission claims)
- [ ] `Controllers/*ControllerTests.cs` — Auth, Accounts (Bookings/Settings/Wishlist), Public (Tours/Booking), Admin (Users/Languages as permission examples)

**Acceptance:** PRG, sign-out guard, antiforgery, permission gating, and paging behaviors verified without a real backend.

## Phase 3 — E2E critical paths (auth + booking/payment)

| Flow | Steps |
|---|---|
| Auth | register → verify-email (6-digit) → login → logout; forgot/reset |
| Booking → payment | tour detail → availability → slot → participants → pay → confirmation |
| Cancel-with-refund | my-bookings → cancel modal → refunded state |

**Files**
- [ ] `tests/Web.E2E/auth.spec.*`, `booking.spec.*`
- [ ] `tests/Web.E2E/fixtures/` (seeded tour, schedule, slot), `support/` (login helper, money-assertion helper that reads server-rendered totals verbatim)
- [ ] axe pass per page; results → `Agents/Tests/Results/`

**Acceptance:** happy-path booking→pay→confirm green; money values asserted as server-rendered (never recomputed in test); auth flow green incl. 6-digit OTP; no critical axe violations on these pages.

## Phase 4 — E2E secondary + a11y sweep

| Flow | Coverage |
|---|---|
| Reviews | submit via shared `_ReviewForm` on Tours/Detail; My Reviews edit/delete |
| Notifications | bell unread badge + mark-read (SignalR or polling fallback) |
| Support | create ticket → (staff) reply → close |
| Provider/Guide | dashboard loads; create-listing stepper smoke |
| Moderation | reviews queue hide/restore; dispute resolve (Admin plan) |
| a11y sweep | axe across all covered pages; keyboard-only nav of modals/typeahead/map |

**Files**
- [ ] `tests/Web.E2E/{reviews,notifications,support,provider,moderation}.spec.*`
- [ ] `tests/Web.E2E/a11y.spec.*` (axe budget shared with Tier D §P4)

**Acceptance:** each secondary journey has a smoke + key-action test; a11y budget enforced; flaky-test policy (retry + quarantine) documented.

## Out of scope

- Backend module unit/integration tests (live with each `src/Modules/*` + their `tests/`).
- Load/performance benchmarking beyond CLS/Lighthouse checks (Tier D owns Web-Vitals).
- Visual-regression snapshots (optional future; note harness if added).

## Sequencing & effort

P0 → **P1 (unit, fast ROI)** → **P3 (E2E booking/auth — protect the money + login paths)** → **P2 (controller integration)** → **P4 (secondary + a11y)**. P1/P2 medium, P3 medium-high (harness + seeding), P4 ongoing as features land. **Write tests phase-by-phase alongside each feature plan**, not all at the end.

## Open questions

1. E2E harness: Playwright for .NET vs Node? (match `Agents/Tests/`.)
2. E2E backend: seeded live test API vs recorded fixtures?
3. Test sign-in: mint BFF cookie directly vs drive the real OAuth/login UI?
4. CI ordering + flaky-test quarantine policy.
