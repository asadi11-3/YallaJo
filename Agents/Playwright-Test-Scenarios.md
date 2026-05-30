# YallaJo — Playwright MCP Test Suite (Master Index)

**This is the master index for the Playwright MCP test corpus.** The actual scenarios live in 12 per-module files under `Agents/Tests/`. This document orchestrates them.

**Source basis:** Every scenario is grounded in the canonical workflow plans in `Agents/Plans/`, cross-checked against the actual source code, and reconciled with the `YallaJo Business Rules & Edge Cases.pdf` (guidance, not ground-truth — divergences are recorded explicitly, see §6).

> **⚠ API-ONLY MODE.** This checkout has no `YallaJo.Web`. **Read [`Tests/Playwright-APIOnly-Adapter.md`](Tests/Playwright-APIOnly-Adapter.md) FIRST.** It defines the universal preflight, the `window.__yj.apiFetch(...)` helper, login token capture, multipart upload, SignalR connect, and the cheat-sheet that maps every old `browser_navigate(<Web URL>)` step to its `apiFetch` equivalent. Every per-module file below assumes the adapter is loaded.

---

## §1 Index — Per-module suites

> **Adapter (read first):** [`Tests/Playwright-APIOnly-Adapter.md`](Tests/Playwright-APIOnly-Adapter.md) — universal preflight + `apiFetch` helper. Not a scenario file; it's loaded once per Playwright session.

| # | Module | File | Built | NOT_BUILT | DEFERRED | Total | Workflow plans used |
|---|---|---|---:|---:|---:|---:|---|
| 1 | TourGuide | [`Tests/Playwright-TourGuide.md`](Tests/Playwright-TourGuide.md) | 40 | 12 | 7 | **59** | TourGuide-Flow.md, TourGuide-Flow-Audit-Report.md, TourGuide-Flow-FixPlan.md |
| 2 | Booking | [`Tests/Playwright-Booking.md`](Tests/Playwright-Booking.md) | 42 | 16 | 5 | **63** | Booking-Workflow.md, Booking-Audit-Report.md, Booking-FixPlan.md |
| 3 | Finance | [`Tests/Playwright-Finance.md`](Tests/Playwright-Finance.md) | mixed | mixed | mixed | **87** | Finance-Workflow.md, Finance-Audit-Report.md, Finance-FixPlan.md, YallaJo_Payment_Integration_Plan.md |
| 4 | Messaging | [`Tests/Playwright-Messaging.md`](Tests/Playwright-Messaging.md) | mixed | mixed | mixed | **67** | Messaging-Workflow.md, Messaging-Audit-Report.md, Messaging-FixPlan.md |
| 5 | Social | [`Tests/Playwright-Social.md`](Tests/Playwright-Social.md) | 52 | 6 | 4 | **62** | Social-Workflow.md, Social-Audit-Report.md, Social-FixPlan.md, BlogCreatorPost-Merger.md |
| 6 | Platform-Onboarding | [`Tests/Playwright-PlatformOnboarding.md`](Tests/Playwright-PlatformOnboarding.md) | 60 | 14 | — | **74** | Platform-Onboarding-Workflow.md, Platform-Onboarding-Audit-Report.md, Platform-Onboarding-FixPlan.md |
| 7a | ContentCore | [`Tests/Playwright-ContentCore.md`](Tests/Playwright-ContentCore.md) | 52 | 8 | 5 | **65** | ContentCore-Workflow.md, ContentCore-Audit-Report.md, ContentCore-FixPlan.md |
| 7b | ContentPlaces | [`Tests/Playwright-ContentPlaces.md`](Tests/Playwright-ContentPlaces.md) | 43 | 10 | 7 | **60** | ContentPlaces-Workflow.md, ContentPlaces-Audit-Report.md, ContentPlaces-FixPlan.md |
| 7c | ContentSeo | [`Tests/Playwright-ContentSeo.md`](Tests/Playwright-ContentSeo.md) | 36 | 6 | 4 | **46** | ContentSeo-Workflow.md, ContentSeo-Audit-Report.md, ContentSeo-FixPlan.md |
| 8a | Analytics | [`Tests/Playwright-Analytics.md`](Tests/Playwright-Analytics.md) | mixed | mixed | mixed | **40** | Analytics-Workflow.md, Analytics-Module-Workflow.md, Analytics-Audit-Report.md, Analytics-FixPlan.md, Recomendation Engine/* |
| 8b | RoleSystem | [`Tests/Playwright-RoleSystem.md`](Tests/Playwright-RoleSystem.md) | mixed | mixed | mixed | **33** | Role-System.md, Role-System-Audit-Report.md, Role-System-FixPlan.md, authorization-refactor-plan.md, endpoint-authorization-audit.md |
| 9 | **Cross-cutting** | [`Tests/Playwright-CrossCutting.md`](Tests/Playwright-CrossCutting.md) | — | — | — | **~70** | Master-RoadmapTo10.md, CrossDocumentAnalysisReport.md, UI-UX-Pattern-Report.md, patterns/*, decisions/ADR-* |

**Grand total: ~726 scenarios (656 per-module + ~70 cross-cutting).**

---

## §2 Universal prerequisites (MUST be done before any run)

### Hard blockers
- [x] **SQL connection string fixed.** Verified `YallaJo.Api/appsettings.Development.json` uses `Server=MOHAMMAD\SQLEXPRESS;` (single suffix) — user confirmed.
- [ ] **Apply migrations.** `dotnet ef database update --project YallaJo.Api` (or just start the API in Development — migrations run automatically via `UseDataSeedingAsync`).
- [ ] **Start the API.** `dotnet run --project YallaJo.Api` → expect https://localhost:57065 (Swagger at `/swagger`).
- [x] **Web app: N/A.** This checkout has no `YallaJo.Web`. All scenarios run API-only via the adapter (`Tests/Playwright-APIOnly-Adapter.md`).
- [x] **Recaptcha disabled globally.** `RecaptchaValidationBehavior`, all 8 protected commands, and DI registrations are commented out. Re-enable with find/replace: `// RECAPTCHA DISABLED - TODO: uncomment when re-enabling`.
- [x] **Test users seeded.** `SeedIdentityProfiles.cs` + `SecurityDbInitializer` + `AccountsDbInitializer` + `AuthDbInitializer` + `AccountsProviderApplicationSeeder` (Order=51) are idempotent and seed all 8 users below on first start. `guide-pending` ends in `ProviderApplication.Status=Pending`; `guide-approved` / `business` / `agency` end in `Approved`.

### Seed users (used by every per-module file)

| Email | Password | Role |
|---|---|---|
| admin@yallajo.test | TestPass!23 | Admin |
| userA@yallajo.test | TestPass!23 | User (consumer) |
| userB@yallajo.test | TestPass!23 | User (second consumer) |
| guide-pending@yallajo.test | TestPass!23 | Pending Guide |
| guide-approved@yallajo.test | TestPass!23 | Approved Guide |
| business@yallajo.test | TestPass!23 | Business Owner |
| agency@yallajo.test | TestPass!23 | Agency |
| suspended@yallajo.test | TestPass!23 | Suspended |

### Playwright MCP tools available

- Page lifecycle: `mcp__playwright__browser_navigate`, `browser_navigate_back`, `browser_close`, `browser_resize`, `browser_tabs`
- Interaction: `browser_click`, `browser_hover`, `browser_drag`, `browser_drop`, `browser_press_key`, `browser_type`, `browser_select_option`, `browser_fill_form`, `browser_file_upload`, `browser_handle_dialog`
- Inspection: `browser_snapshot`, `browser_take_screenshot`, `browser_evaluate`, `browser_console_messages`
- Network: `browser_network_requests`, `browser_network_request`
- Waits: `browser_wait_for`

---

## §3 Recommended execution order

> Each phase only starts when its predecessor is green. Per-module files (§A) run in dependency order so that data from lower modules feeds upper modules.

### Phase 0 — Environment & smoke
- Universal prereqs above.
- **Load the API-Only adapter** ([`Tests/Playwright-APIOnly-Adapter.md`](Tests/Playwright-APIOnly-Adapter.md) §0): `browser_navigate('https://localhost:57065/swagger')` → `browser_evaluate` installs `window.__yj`.
- 4-step API smoke test via `window.__yj.apiFetch`:
  1. `GET /health` → 200 with `{ status: "Healthy" }`.
  2. `POST /api/v1/auth/login` for `admin@yallajo.test / TestPass!23` → 200 with `accessToken`, `refreshToken`.
  3. `GET /api/v1/security/me` with the bearer token → 200 with roles incl `Admin`. (Note: `/auth/me` does NOT exist; current-user lives on the Security module.)
  4. `POST /api/v1/auth/login` for `suspended@yallajo.test / TestPass!23` → expect non-200 (suspended account is blocked).
- Confirm no console errors after each step.

### Phase A — Per-module suites (dependency order)
1. **ContentCore** — base entities, EntityImage, EntityTag, categories. Bedrock for all content.
2. **ContentPlaces** — countries/cities/places. Depends on ContentCore.
3. **ContentSeo** — sitemaps, redirects, hreflang. Depends on ContentCore/Places.
4. **RoleSystem** — roles, permissions, authorization gates. Needed before any role-gated flow.
5. **Platform-Onboarding** — provider registration, doc upload, admin review. Produces approved providers used downstream.
6. **TourGuide** (lives in `src/ContentTours.*`) — guide profiles, tours, slots. Depends on onboarded providers.
7. **Booking** — slot locks, confirmations, completion. Depends on tours.
8. **Finance** — payments, payouts, escrow, commissions. Depends on bookings.
9. **Social** — reviews, wishlist, reports, blog. Depends on completed bookings.
10. **Messaging** — notifications, support tickets, SignalR. Touches everything.
11. **Analytics** — interactions, recommendations, dashboards. Consumes events from all above.

### Phase B — Cross-cutting
1. **Auth matrix** (§1 of CrossCutting) — anonymous routing, role gates, suspension, token tampering.
2. **Validation & error surfaces** (§2 CC) — RFC 7807, field attribution, 500 anonymization.
3. **Performance** (§3 CC) — TTFB/LCP, image weight, bundle size, HybridCache warm/cold.
4. **i18n** (§4 CC) — language toggle, RTL on critical pages, missing key scan, locale formatting.
5. **Accessibility** (§5 CC) — axe scan, keyboard nav, landmarks, contrast, reduced motion.
6. **SignalR** (§6 CC) — connect-on-login, reconnect, targeted broadcast (typed-hub regression), hub auth.
7. **Rate limiting** (§7 CC) — login brute-force, OTP resend, search flood.
8. **Security headers / CSP / cookies** (§8 CC).
9. **Observability** (§9 CC) — traceId propagation, `/health` endpoints, outbox liveness.

### Phase C — Full integration flows
- TC-CC-10.1 — Full happy path: discover → book → pay → review.
- TC-CC-10.2 — Provider lifecycle: onboard → publish → earn → payout.
- TC-CC-10.3 — Dispute escalation.
- TC-CC-10.4 — Suspension cascade.
- TC-CC-10.5 — Subscription tier change (DEFERRED stub).

### Phase D — NOT_BUILT / DEFERRED tripwires
- Run §11 of CrossCutting to confirm stub endpoints still 404/501 as expected. These are designed to **fail loudly once shipped**, so the next person knows tests need fleshing out.

---

## §4 Global pass criteria

A test run is **PASS** when:

1. Every **Built** scenario across all 12 files passes.
2. Every **NOT_BUILT** stub still fails in the expected stub way (e.g., 404/501). If a stub passes unexpectedly, that means a feature shipped — convert the stub into a real Built scenario.
3. Every **DEFERRED** scenario remains skipped (or stub-failing) per ADR-005 / Master-RoadmapTo10 deferrals.
4. Zero "critical" or "serious" axe violations on any public-facing page.
5. No JS console errors on any page (warnings allowed only if explicitly catalogued).
6. p95 page TTFB < 600 ms on a warm cache; p95 list-endpoint latency < 200 ms warm.

---

## §5 Project structure (Agents/Tests/)

```
Agents/
├── Plans/                                    # 41 source workflow plans (unchanged)
│   ├── Master-RoadmapTo10.md
│   ├── CrossDocumentAnalysisReport.md
│   ├── Module-Workflow-Template.md
│   ├── {Module}-Workflow.md                  # 13 modules
│   ├── {Module}-Audit-Report.md              # 13 audits
│   ├── {Module}-FixPlan.md                   # 13 fix plans
│   └── ...
├── Tests/                                    # ← created by this work
│   ├── Playwright-APIOnly-Adapter.md         # ← READ FIRST · universal preflight + apiFetch helper
│   ├── Playwright-TourGuide.md               # 59 scenarios
│   ├── Playwright-Booking.md                 # 63
│   ├── Playwright-Finance.md                 # 87
│   ├── Playwright-Messaging.md               # 67
│   ├── Playwright-Social.md                  # 62
│   ├── Playwright-PlatformOnboarding.md      # 74
│   ├── Playwright-ContentCore.md             # 65
│   ├── Playwright-ContentPlaces.md           # 60
│   ├── Playwright-ContentSeo.md              # 46
│   ├── Playwright-Analytics.md               # 40
│   ├── Playwright-RoleSystem.md              # 33
│   └── Playwright-CrossCutting.md            # ~70 scenarios across §1-§14
└── Playwright-Test-Scenarios.md              # this file (index)
```

---

## §6 Global divergences (workflow ↔ code) — applies to every file

These divergences were discovered while writing the per-module suites. **Tests always assert the CODE behaviour**; plan deltas are logged for follow-up.

| # | Source plan / PDF | Actual code | Test stance |
|---|---|---|---|
| 1 | TourGuide module under `TourGuide.*` | Code lives in `ContentTours.*` | Tests target `ContentTours.*` |
| 2 | `YallaJo.Web` with `Areas/*` | **`YallaJo.Web` does not exist in this checkout — API-only.** | Every scenario uses the [APIOnly adapter](Tests/Playwright-APIOnly-Adapter.md) (`window.__yj.apiFetch`). The boxed callout at the top of each per-module file points to it. |
| 3 | Provider types = 4 (Independent Guide, Business Owner, Agency, Freelance Instructor) | Enum has 6 values | Tests assert 6 values; plan needs reconciliation |
| 4 | Document max size 5 MB | Code enforces 10 MB | Tests assert 10 MB |
| 5 | Min guide payout 10 JOD | Code enforces 20 JOD | Tests assert 20 JOD |
| 6 | Commission tiered by subscription | Code uses revenue-tier rule (resolved) | Tests assert revenue-tier |
| 7 | Review eligibility "anyone with account" | Code requires verified booking | Tests assert booking-verified |
| 8 | Review auto-hide after 5 reports | Code threshold 3 | Tests assert 3 |
| 9 | Refund cutoff 72h/24h ambiguity | Plan resolved to 24h | Tests assert 24h |
| 10 | Weather feature under ContentPlaces | Code under ContentSeo | Tests target ContentSeo |
| 11 | SignalR broadcast uses `IHubContext<Hub>` | Bug — must be `IHubContext<NotificationHub>` | Tests include regression TC-CC-6.3 |
| 12 | `ProviderEndpoints` route mapping | Possible double `/api/v1/provider` nesting | Test verifies single resolution |
| 13 | Analytics diversity rule `MaxPerPlaceId` | Should be `MaxPerProviderId` | Tests assert `ProviderId` once fixed; current stub asserts current behavior |

---

## §7 Top NOT_BUILT areas (run the corresponding stubs to keep them visible)

| Area | Owning module | Stub location |
|---|---|---|
| Persisted MonthlyStatement (PDF) | Finance | Playwright-Finance.md §2 |
| Unified `PaymentMethod` (bank + wallets) | Finance | Playwright-Finance.md §2 |
| `CreditNote` for refunds | Finance | Playwright-Finance.md §2 |
| `GuideEarning` table | Finance | Playwright-Finance.md §2 |
| Real collaborative-filter Rec engine | Analytics | Playwright-Analytics.md §2 |
| Blended 40/35/25 scoring | Analytics | Playwright-Analytics.md §2 |
| Nightly matrix recompute | Analytics | Playwright-Analytics.md §2 |
| SLA monitoring service | Messaging | Playwright-Messaging.md §2 |
| Digest / batching of notifications | Messaging | Playwright-Messaging.md §2 |
| SMS / Push real providers | Messaging | Playwright-Messaging.md §2 |
| ChatBot shell | Messaging | Playwright-Messaging.md §2 |
| FluentValidation coverage (14 Messaging, 5 Finance, ~21 Analytics) | All | Playwright-CrossCutting.md §2 |
| HybridCache rollout (Finance, Messaging, Social) | All | Playwright-CrossCutting.md §3.5 |
| Razor UI for TourGuide / Booking / PlatformOnboarding | Web | each Playwright-{Module}.md §2 |
| Doc expiry warning→grace→auto-suspend lifecycle | Platform-Onboarding | Playwright-PlatformOnboarding.md §2 |
| Discount → wishlist notification pipeline | Social | Playwright-Social.md §2 |
| Country/City CRUD endpoints | ContentPlaces | Playwright-ContentPlaces.md §2 |
| EntityTag consumers in Analytics/ContentSeo | ContentCore | Playwright-ContentCore.md §2 |
| Manual sitemap create | ContentSeo | Playwright-ContentSeo.md §2 |
| Per-feature granular roles / role-admin UI | RoleSystem | Playwright-RoleSystem.md §2 |
| Live Tour Tracking SignalR | (new) | Playwright-CrossCutting.md §11 |
| AI Chatbot | (DEFERRED) | Playwright-CrossCutting.md §11 |
| Subscription billing | (DEFERRED) | Playwright-CrossCutting.md §11 |
| Referral / Loyalty | (DEFERRED) | Playwright-CrossCutting.md §11 |

---

## §8 What changed from the previous version of this file

This file used to be a single 600-line PDF-derived test plan. It has been **replaced by an index** because:

1. The PDF is guidance, not ground truth — workflow plans in `Agents/Plans/` are authoritative.
2. The corpus exceeded what one file can usefully hold (~726 scenarios).
3. Per-module files allow parallel execution and per-module review.

The PDF mapping is preserved inside each per-module file (every scenario links to its `PDF §` + `Workflow §` source).

---

## §9 How to run

Once prerequisites pass, point any Playwright MCP client at:

- **Per-module suite**: open the relevant `Tests/Playwright-{Module}.md` and execute each `TC-{module}-{seq}` in order. Each block lists exact MCP tool calls.
- **Cross-cutting**: run `Tests/Playwright-CrossCutting.md` after all per-module suites are green.
- **Integration flows**: §10 of CrossCutting (TC-CC-10.1 through 10.5).

Run order summary: **Phase 0 → Phase A (×11 modules in dep order) → Phase B (×9 cc concerns) → Phase C (5 integration flows) → Phase D (NOT_BUILT tripwires)**.

---

## §10 Maintenance

- When a `NOT_BUILT` feature ships, **convert its stub to a Built scenario** (don't add a parallel one).
- When a divergence in §6 gets resolved (code matches plan), **delete the row** and update the affected per-module file.
- When a new module ships, add `Tests/Playwright-{NewModule}.md` and link it in §1 above.
- Keep this index lean — detailed scenario text always lives in the per-module file.

---

**End of master index. See `Tests/` for the 12 per-module files + the API-Only adapter.**

---

## §11 Change log

- **2026-05-29 — API-only conversion.** Every per-module file now opens with a callout pointing to `Tests/Playwright-APIOnly-Adapter.md`. All ~60 `https://localhost:57070` Web URLs were replaced with `https://localhost:57065/swagger` (Swagger as the docking page; actual calls go through `window.__yj.apiFetch`). Master index updated to: drop the "start Web app" prereq, mark SQL fix and Recaptcha disable as done, add the test-user seed status, and rewrite Phase 0 smoke test to use the adapter.
- **2026-05-29 — Test seeders.** `SeedIdentityProfiles` extended with `Status` field + 8 `@yallajo.test` users. `SecurityDbInitializer`, `AccountsDbInitializer`, `AuthDbInitializer` rewritten to be idempotent. New `AccountsProviderApplicationSeeder` (Order=51) creates 4 provider applications (1 Pending guide, 1 Approved guide, 1 Approved BusinessOwner, 1 Approved Agency) with all required documents. All 4 projects build clean (0 errors).
