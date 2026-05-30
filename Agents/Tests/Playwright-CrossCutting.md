# Playwright Test Scenarios — Cross-Cutting Concerns

> **API-ONLY MODE.** This checkout has no `YallaJo.Web`. Every `browser_navigate("https://localhost:57065/swagger…")` step below is a docking step; the actual request runs through `window.__yj.apiFetch(...)` defined in [`Playwright-APIOnly-Adapter.md`](./Playwright-APIOnly-Adapter.md). Read that adapter once at the start of every Playwright session — it also lists the 8 seeded test users and their credentials.

**Scope:** Concerns that span every module: global auth matrix, performance, i18n (Arabic/English + RTL), accessibility (WCAG 2.1 AA), SignalR realtime, rate-limiting, error handling, security headers, observability.

**Audience:** Run AFTER per-module suites pass. These are integration-level checks that detect cross-module regressions.

**Source plans consulted:**
- `Agents/Plans/Master-RoadmapTo10.md`
- `Agents/Plans/CrossDocumentAnalysisReport.md`
- `Agents/Plans/UI-UX-Pattern-Report.md`
- `Agents/Plans/Plans/Role-System.md`
- `Agents/Plans/endpoint-authorization-audit.md`
- `Agents/Plans/Plans/Module-Workflow-Template.md`
- `Agents/Plans/patterns/{caching,error-handling,polly}-patterns.md`
- `Agents/Plans/decisions/ADR-00*.md`

---

## §0 Prerequisites

### Hard blockers (must be resolved before ANY test runs)
- [ ] **SQL connection string fixed** — change `Server=MOHAMMAD\SQLEXPRESS\\SQLEXPRESS` → `Server=MOHAMMAD\SQLEXPRESS;` in `src/YallaJo.Api/appsettings.Development.json`
- [ ] **App running** — `dotnet run --project src/YallaJo.Api` (https://localhost:57065) + `dotnet run --project src/YallaJo.Web` (https://localhost:57065/swagger)
- [ ] **Migrations applied** — `dotnet ef database update` on the API project
- [ ] **Recaptcha disabled** (already done — globally commented out)
- [ ] **Seed data loaded** — see seed users below

### Seed users (used across every scenario)

| Email | Password | Role | Notes |
|---|---|---|---|
| admin@yallajo.test | TestPass!23 | Admin | global super-user |
| userA@yallajo.test | TestPass!23 | User | regular consumer |
| userB@yallajo.test | TestPass!23 | User | second consumer (for two-party tests) |
| guide-pending@yallajo.test | TestPass!23 | Pending Guide | onboarding incomplete |
| guide-approved@yallajo.test | TestPass!23 | Approved Guide | active provider |
| business@yallajo.test | TestPass!23 | Business Owner | provider |
| agency@yallajo.test | TestPass!23 | Agency | provider |
| suspended@yallajo.test | TestPass!23 | Suspended | banned/locked |

### Tooling
- Playwright MCP server (browser_navigate / browser_snapshot / browser_click / browser_fill_form / browser_select_option / browser_press_key / browser_wait_for / browser_network_requests / browser_network_request / browser_evaluate / browser_take_screenshot / browser_console_messages / browser_tabs).
- `@axe-core/playwright` injection via `browser_evaluate` for a11y scans.
- `browser_network_requests(static=false, filter="api/v1")` for API trace assertions.

---

## §1 Built — Global Auth Matrix

### TC-CC-1.1 — Anonymous user can reach public pages
- **Endpoints:** GET `/`, GET `/explore`, GET `/tours`, GET `/places`, GET `/blog`, GET `/auth/login`, GET `/auth/register`, GET `/auth/forgot-password`, GET `/sitemap.xml`, GET `/robots.txt`
- **Playwright MCP:**
  1. `browser_navigate(url="https://localhost:57065/swagger/")` → expect 200, no redirect to /auth/login
  2. For each public path, `browser_navigate` and assert `browser_console_messages(level="error")` is empty
- **Assertions:**
  - Status 200 on all
  - No "Unauthorized" banner
  - No JS console errors

### TC-CC-1.2 — Anonymous redirected from gated pages
- **Endpoints (gated):** `/account/*`, `/bookings/*`, `/wishlist`, `/notifications`, `/provider/*`, `/admin/*`
- **Playwright MCP:**
  1. Ensure no cookie (`browser_evaluate(function="() => document.cookie = ''; localStorage.clear(); sessionStorage.clear();")`)
  2. For each gated path: `browser_navigate(url=...)` → assert URL ends with `/auth/login?returnUrl=...`
- **Assertions:** redirect to `/auth/login`, `returnUrl` query param preserved

### TC-CC-1.3 — Role-gated endpoints reject wrong role
- **Matrix:**
  - User → Admin pages → 403
  - User → Provider pages → 403
  - Pending Guide → Provider operational pages → 403 (must be Approved)
  - Suspended → ANY auth-required page → 403/redirect with "Account suspended" banner
  - Guide → Admin Sitemap controls → 403
  - Business → Agency-payout endpoints → 403
- **Playwright MCP:**
  1. Login as each role (`browser_fill_form` on /auth/login)
  2. `browser_navigate` to a forbidden URL
  3. Assert page contains "Access denied" or HTTP 403 via `browser_network_requests` last response
- **Assertions:** correct 403 / redirect with cause shown

### TC-CC-1.4 — Token/cookie tampering
- Modify auth cookie to invalid signature → next API call returns 401 → user redirected to /auth/login.
- Replay expired token → 401 + `WWW-Authenticate: Bearer error="invalid_token"`.
- **Playwright MCP:** `browser_evaluate` to corrupt cookie, then trigger a navigation.

### TC-CC-1.5 — Logout invalidates session everywhere
- Login in tab1 (`browser_tabs(action="new")`), tab2 same user. Logout from tab1. Reload tab2 → must be /auth/login.
- Assert SignalR connection in tab2 disconnects (check via `browser_console_messages`).

---

## §2 Built — Global Validation, Error Surfaces

### TC-CC-2.1 — RFC 7807 Problem Details on every 4xx
- Pick a known 400-yielding endpoint per module (e.g., POST /api/v1/auth/register with empty email).
- **Playwright MCP:** `browser_network_request(index=N, part="response-body")` after triggering the error.
- **Assertions:** `Content-Type: application/problem+json`, body has `type`, `title`, `status`, `detail`, `traceId`, `errors{}`.

### TC-CC-2.2 — Validation errors are field-attributed
- Submit invalid form in each module's primary create flow.
- **Assertions:** Each invalid field has a visible error message near the input (not just a top banner) and `aria-describedby` set.

### TC-CC-2.3 — 500 responses are anonymised in production
- Force a 500 (e.g. with a known broken endpoint, or by deliberately bad payload that bypasses validation).
- **Assertions:** Response body in `ASPNETCORE_ENVIRONMENT=Production` mode contains NO stack trace; only `traceId`. In Development, stack is shown.

### TC-CC-2.4 — No raw exception leakage in UI
- For any 5xx, the rendered page must show a friendly "Something went wrong" panel with the traceId — never a yellow .NET stack.

### TC-CC-2.5 — Polly retry headers are honoured
- Hit an endpoint that talks to a flaky external (weather/payments). Inspect `browser_network_requests` for `Retry-After` honouring and Polly circuit-break behaviour (consecutive failures → eventual `503 Service Unavailable` from the gateway with a clear message).

---

## §3 Built — Performance Budgets

### TC-CC-3.1 — Page TTFB / Largest Contentful Paint
- For each top-level page (`/`, `/explore`, `/tours/{id}`, `/account`):
  - **Playwright MCP:** `browser_evaluate(function="() => JSON.stringify(performance.getEntriesByType('navigation'))")`
  - **Assertions:** TTFB < 600 ms, LCP < 2.5 s, CLS < 0.1 (steady-state, not first cold).

### TC-CC-3.2 — Image weight and lazy loading
- `browser_network_requests(filter=".(png|jpg|jpeg|webp|avif)", static=true)` on a tour-detail page.
- **Assertions:** No single image > 500 KB; off-screen images have `loading="lazy"`; `srcset`/`sizes` present on responsive images.

### TC-CC-3.3 — Query response time (read paths)
- For each module's main list endpoint (e.g., GET `/api/v1/tours`, GET `/api/v1/places`), do warm cache + cold cache runs.
- **Assertions:** warm < 200 ms p95, cold < 800 ms p95.
- Use `browser_network_request(index=N, part="response-headers")` to read `Server-Timing` if present.

### TC-CC-3.4 — Bundle size guard
- `browser_network_requests(filter=".(js|css)")` on homepage.
- **Assertions:** Total JS (gzip) < 350 KB on first paint; no single chunk > 250 KB.

### TC-CC-3.5 — HybridCache warm path
- Make two identical reads on a cached endpoint (anything with `[HybridCache]`).
- **Assertions:** second call has lower latency; if `X-Cache: HIT` header set by Polly + HybridCache, verify it.

---

## §4 Built — i18n (English ⇄ Arabic, RTL)

### TC-CC-4.1 — Language toggle persists
1. `browser_navigate("/")` → switch language to Arabic via UI selector.
2. `browser_navigate("/explore")` → still Arabic.
3. Reload → still Arabic (cookie/header persists).
- **Assertions:** `<html lang="ar" dir="rtl">`, cookie `.AspNetCore.Culture` set.

### TC-CC-4.2 — RTL layout correctness on critical pages
- For `/`, `/explore`, `/tours/{id}`, `/booking/{id}`, `/auth/login`:
  - `browser_take_screenshot(fullPage=true, filename="rtl-{page}.png")`.
  - `browser_evaluate("() => getComputedStyle(document.body).direction")` → "rtl"
  - Visually verify (manual check on screenshot): nav reversed, icons mirrored where directional, no overlapping text, no clipped buttons.

### TC-CC-4.3 — No missing translation keys
- For every page above, scan rendered text for `[[Missing: …]]`, `??`, or raw resource keys.
- **Playwright MCP:** `browser_evaluate("() => document.body.innerText.match(/\\[\\[Missing:|^\\$res\\./gm)")` → must be `null`.

### TC-CC-4.4 — Number, currency, date formatting per culture
- Tour price page in Arabic must show Arabic-Indic digits if culture demands it; date formatted Hijri/Gregorian per policy; currency in JOD with locale-correct symbol placement.

### TC-CC-4.5 — Form input direction
- Arabic locale: text inputs default to RTL alignment; numeric fields stay LTR. Mixed content (English email inside Arabic page) uses `dir="ltr"`/`bdi`.

---

## §5 Built — Accessibility (WCAG 2.1 AA)

### TC-CC-5.1 — axe-core automated scan on every public page
- `browser_evaluate` to inject axe (`fetch('https://cdn.jsdelivr.net/npm/axe-core@4/axe.min.js').then(r=>r.text()).then(eval); await axe.run()`).
- For each of `/`, `/explore`, `/tours/{id}`, `/auth/login`, `/auth/register`, `/bookings`, `/account`:
- **Assertions:** zero "critical" or "serious" violations.

### TC-CC-5.2 — Keyboard-only navigation
- `browser_press_key("Tab")` repeatedly through `/auth/login`.
- **Assertions:** focus visible on every focusable element; logical tab order; no keyboard trap.

### TC-CC-5.3 — Skip-to-content link present and works
- On `/`, first tab → "Skip to main content" link visible; pressing Enter focuses `<main>`.

### TC-CC-5.4 — Forms have associated labels
- Every `<input>` must have `<label for>` or `aria-label`/`aria-labelledby`.
- **Playwright MCP:** `browser_evaluate("() => [...document.querySelectorAll('input,select,textarea')].filter(e => !e.labels?.length && !e.getAttribute('aria-label') && !e.getAttribute('aria-labelledby')).map(e=>e.outerHTML)")` → must be `[]`.

### TC-CC-5.5 — Colour contrast
- axe scan covers it; spot-check brand buttons (orange on white) ≥ 4.5:1 for text, ≥ 3:1 for large text.

### TC-CC-5.6 — Reduced motion respected
- `browser_evaluate("() => matchMedia('(prefers-reduced-motion: reduce)').matches")` toggled true → confirm no infinite animations, no parallax, no auto-rotating carousels.

### TC-CC-5.7 — Screen-reader landmarks
- Each page has exactly one `<main>`, one `<header>`, one `<footer>`, sensible `<nav aria-label>` on each nav region.

### TC-CC-5.8 — Smart Accessibility UI controls (PDF §23)
- Toggle High Contrast, Font Scaling, Reduced Motion, Color-Blindness filter from accessibility panel.
- Settings persist in `localStorage` for guests, in profile for logged-in users.
- **NOT_BUILT** stub here if panel absent.

---

## §6 Built — SignalR / Realtime

### TC-CC-6.1 — Notification hub connects on login
1. Login as userA.
2. `browser_console_messages(level="info")` → expect WebSocket connect logs.
3. Trigger a notification (e.g., book a tour → expect "Booking confirmed" toast in real time).

### TC-CC-6.2 — Reconnect after network blip
- Use `browser_evaluate("() => navigator.serviceWorker?.controller?.postMessage({offline:true})")` or DevTools network throttling proxy. Wait > 30 s. Restore. Expect reconnect within 10 s without page reload.

### TC-CC-6.3 — Targeted user broadcast (current bug from b7)
- Send admin → userA targeted notification. Verify userB does NOT see it.
- Verifies the planned fix: `IHubContext<NotificationHub>` (typed) vs the broken `IHubContext<Hub>` (untyped).

### TC-CC-6.4 — Live Tour Tracking GPS push (PDF §20)
- **NOT_BUILT** in current code per Messaging audit. Add skeleton: assert `/livetours/{id}/hub` endpoint exists or returns 404 for now.

### TC-CC-6.5 — Hub auth gates
- Anonymous tries to connect to `/hubs/notifications` → reject 401.
- Wrong role tries `/hubs/admin` → reject 403.

---

## §7 Built — Rate Limiting

### TC-CC-7.1 — Login brute-force protection
- 5 wrong password attempts in 60 s → 429 Too Many Requests or progressive delay; lockout banner shown.

### TC-CC-7.2 — OTP resend rate limit
- Spam POST `/api/v1/auth/verify-email/resend` 6 times → 429 with `Retry-After` header.

### TC-CC-7.3 — Recommendation/search endpoint flood
- Fire 100 GET `/api/v1/recommendations` in 10 s from one IP → expect 429 after threshold.

### TC-CC-7.4 — Per-user limits beat per-IP limits when authenticated
- Two users behind same IP each make moderate traffic — neither hits the IP-only cap.

### TC-CC-7.5 — 429 body is RFC 7807
- See TC-CC-2.1 — same shape, plus `Retry-After` seconds value.

---

## §8 Built — Security Headers, Cookies, CSP

### TC-CC-8.1 — Required security headers on every response
- `Strict-Transport-Security`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`, `X-Frame-Options: DENY` (or `frame-ancestors 'none'` in CSP), `Permissions-Policy` set.
- **Playwright MCP:** `browser_network_request(index=1, part="response-headers")` on root page.

### TC-CC-8.2 — CSP allows required CDNs only
- CSP should list Mapbox, Google Recaptcha (currently disabled, expected absent until re-enabled), payment gateway, SignalR backplane, fonts.
- No `unsafe-inline` for scripts (style may be allowed if hashed).

### TC-CC-8.3 — Cookies are `Secure`, `HttpOnly`, `SameSite=Lax` (auth) / `Strict` (CSRF)
- `browser_evaluate("() => document.cookie")` shows ONLY non-HttpOnly cookies; auth cookie absent here → confirms HttpOnly.
- Capture via `browser_network_request` on a login response.

### TC-CC-8.4 — Antiforgery on all state-changing forms
- Submit POST form with missing `__RequestVerificationToken` → 400 antiforgery failure.

### TC-CC-8.5 — Mixed content prevention
- `browser_console_messages(level="warning")` after navigating each page → zero "Mixed Content" warnings.

---

## §9 Built — Observability & Tracing

### TC-CC-9.1 — `traceId` propagated end-to-end
- Trigger any API call; capture `X-Trace-Id` (or `traceparent`) response header.
- Confirm same id appears in a deliberate 5xx response body.

### TC-CC-9.2 — Health endpoints public, minimal
- GET `/health` (liveness) → 200, JSON body `{ "status": "Healthy" }`.
- GET `/health/ready` (readiness) → 200 when DB + cache + outbox processor ready.

### TC-CC-9.3 — Outbox dispatcher visible in metrics
- Trigger a booking. Within 10 s, an integration event row in Outbox table transitions Processed=true. (DB check via API admin endpoint or query tool — fallback OK; for Playwright, navigate to admin outbox monitor if present.)

---

## §10 Cross-cutting integration scenarios (workflow-level)

These exercise multiple modules in a single thread of execution. Each maps to a workflow plan.

### TC-CC-10.1 — Full happy path: discover → book → pay → review (PDF §1-6 + plans)
1. Anonymous user lands on `/`.
2. Searches "Petra" → sees tour list.
3. Filters: Children-Friendly + Accessible + ≤ 100 JOD.
4. Opens tour detail → adds to wishlist (login wall).
5. Registers (Recaptcha disabled, OTP via email).
6. Verifies email (use admin override or DB peek for the OTP — flag as DEFERRED if no test mailbox).
7. Picks slot, confirms booking → AwaitingPayment.
8. Pays via gateway sandbox → Confirmed.
9. Receives confirmation notification (SignalR + Email logged).
10. Mark booking Completed via admin time-travel or scheduled job.
11. Submits review (1-5 in 0.5).
12. Wishlist gets a discount notification later (Social pipeline).
- **Assertions per step** + screenshots between steps.

### TC-CC-10.2 — Provider lifecycle: onboard → publish → earn → payout (PDF §1, §5; Platform-Onboarding + Finance plans)
1. New user registers and applies as Guide.
2. Uploads documents (size/MIME checked).
3. Admin reviews within 7-day SLA.
4. Approved → publishes a tour (Pending → Approved by admin).
5. A user books → Confirmed → Completed.
6. Earning lands in escrow 7 days.
7. Weekly Sunday payout batch runs → guide gets payout if ≥ minimum.
8. Guide downloads MonthlyStatement (NOT_BUILT — persisted, but DTO returned).

### TC-CC-10.3 — Dispute escalation across modules (PDF §19)
1. Customer files dispute within 7 days of booking.
2. Provider has 48 h to respond.
3. Admin resolves within 7 days.
4. Resolution: Partial Refund → CreditNote (NOT_BUILT — assert API stub returns 501/404 cleanly).
5. Notification emitted to all parties.

### TC-CC-10.4 — Suspension cascade (Role-System + Social + Security)
1. Admin suspends provider.
2. Their active tours hide from listings.
3. Their open bookings get auto-refunded.
4. Their payouts pause.
5. Logging in as them → "Account suspended" banner; outbound notifications cease.

### TC-CC-10.5 — Subscription tier change (PDF §16 — DEFERRED per plans)
- Mark as DEFERRED unless tier UI is built. Verify endpoints return 501/404 with clean Problem Details.

---

## §11 NOT_BUILT / DEFERRED skeletons

These exist as test stubs so they fail loudly once features land.

| ID | Feature | Status | Stub assertion |
|---|---|---|---|
| TC-CC-NB-1 | Live Tour Tracking SignalR | NOT_BUILT | `/hubs/livetracking` returns 404 or 501; document expected URL |
| TC-CC-NB-2 | AI Chatbot | DEFERRED | `/chatbot` route 404; no public widget |
| TC-CC-NB-3 | Subscription billing | DEFERRED | `/subscriptions` 404 |
| TC-CC-NB-4 | Referral/Loyalty | DEFERRED | `/referrals` 404; no UI link |
| TC-CC-NB-5 | Persisted MonthlyStatement | NOT_BUILT | API returns DTO only; no PDF download |
| TC-CC-NB-6 | CreditNote refund | NOT_BUILT | Dispute resolution "Partial Refund" returns 501 |
| TC-CC-NB-7 | Real collaborative-filter Recs | NOT_BUILT | `/recommendations` returns popularity-only ordering |
| TC-CC-NB-8 | Smart Accessibility UI panel | NOT_BUILT | accessibility panel button absent |

---

## §12 Known PDF ↔ Code Divergences (apply globally)

| # | PDF says | Code says | Action |
|---|---|---|---|
| 1 | Provider types = 4 | Enum has 6 values | Tests assert against CODE (6) |
| 2 | Doc max size 5 MB | Code enforces 10 MB | Tests assert against CODE (10 MB) |
| 3 | Min guide payout 10 JOD | Code 20 JOD | Tests assert against CODE (20 JOD) |
| 4 | Commission tiered by subscription | Code uses revenue-tier rule (resolved) | Tests assert revenue-tier behavior |
| 5 | Review eligibility "anyone" | Code allows only booking-verified | Tests assert booking-verified |
| 6 | Auto-hide after 5 reports | Code threshold 3 | Tests assert code value; flag plan delta |
| 7 | Refund cutoff 72 h / 24 h | Plan resolved to 24 h | Tests assert 24 h |
| 8 | Rating recency window | Two formulas in docs | Tests assert code value |
| 9 | Weather lives in ContentPlaces | Code: in ContentSeo | Test against `src/ContentSeo` |
| 10 | TourGuide module path | Code: `src/ContentTours.*` | Test paths point to ContentTours |
| 11 | Razor `Areas/TourGuide`, `Areas/Booking`, `Areas/PlatformOnboarding` | DO NOT EXIST | Use API-only Playwright (`browser_evaluate` POST) |
| 12 | SignalR broadcast hub | `IHubContext<Hub>` (bug) → must be `IHubContext<NotificationHub>` | Add regression TC-CC-6.3 |

---

## §13 Execution Order

1. **Phase 0 — environment** (TC-CC-0.x prereqs).
2. **Phase A — per-module suites** (run each `Playwright-{Module}.md` in dependency order: ContentCore → ContentPlaces → ContentSeo → RoleSystem → PlatformOnboarding → TourGuide → Booking → Finance → Social → Messaging → Analytics).
3. **Phase B — Cross-cutting auth (§1)**.
4. **Phase C — Cross-cutting validation/errors (§2)**.
5. **Phase D — Performance (§3)**.
6. **Phase E — i18n (§4)** and Accessibility (§5).
7. **Phase F — SignalR (§6)** and Rate-limiting (§7).
8. **Phase G — Security headers (§8)** and Observability (§9).
9. **Phase H — Full integration flows (§10)**.
10. **Phase I — NOT_BUILT/DEFERRED stubs (§11)** to keep them visible.

A run is **green** only if all Built phases pass AND all NOT_BUILT/DEFERRED stubs still fail in the expected way (so they will alarm once shipped).

---

## §14 Playwright MCP cheat-sheet (cross-cutting patterns)

### Inject axe-core
```js
browser_evaluate({function: `async () => {
  const s = document.createElement('script');
  s.src = 'https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.9.1/axe.min.js';
  await new Promise(r => { s.onload = r; document.head.appendChild(s); });
  const res = await axe.run();
  return res.violations.filter(v => v.impact === 'critical' || v.impact === 'serious');
}`})
```

### Capture every API call on a page
```js
browser_network_requests({static: false, filter: "api/v1"})
```

### Force RTL
```js
browser_evaluate({function: `() => {
  document.documentElement.setAttribute('dir', 'rtl');
  document.documentElement.setAttribute('lang', 'ar');
}`})
```

### Read auth cookie expiry from headers
```js
browser_network_request({index: 1, part: "response-headers"})  // look for Set-Cookie
```

### Tab-order trace
```js
browser_evaluate({function: `() => {
  const f = [...document.querySelectorAll('*')].filter(e => e.tabIndex >= 0);
  return f.map(e => ({tag: e.tagName, id: e.id, label: e.ariaLabel || e.innerText?.slice(0, 30)}));
}`})
```

---

**End of cross-cutting scenarios. Combine with the 11 per-module files in `Agents/Tests/`.**
