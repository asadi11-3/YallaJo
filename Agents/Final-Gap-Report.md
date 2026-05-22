# YallaJo — Final Gap Report

> **Generated:** end of audit + cleanup cycle covering all 6 sprints (Booking, Finance, Social, Messaging, Analytics, Auth-Cleanup) + handler coverage + 6 wave specs + UI design.
> **Status:** Backend is production-ready for **PDF1 Phase 1 + 2 minus 32 endpoints**. Frontend (MVC + Webestica template) is fully designed but not yet built.

---

## 1. Executive Summary

| Domain | Status | Score |
|---|---|---|
| **6 backend sprints** | 🟢 All passing audit | 100% |
| **PDF1 endpoint coverage** | 🟡 150/182 built | **82%** |
| **PDF2 business rules** | 🟢 Phase 1+2 enforced | ~95% |
| **Cross-module event handlers** | 🟢 All gaps closed | 100% |
| **Builds** | 🟢 All green (0 errors) | ✅ |
| **Tests** | 🟢 ~250+ unit + ~50 integration passing | ✅ |
| **Migrations** | 🟢 All applied cleanly | ✅ |
| **Frontend (MVC)** | 🔴 Designed, not built | 0% |
| **Phase 3 features** | 🔴 Not started | 0% |
| **Phase 4 features** | 🔴 Not started | 0% |

---

## 2. Backend Sprint Status

All 6 sprints have been audited and cleaned. Latest verification:

| Sprint | Audit verdict | Notes |
|---|---|---|
| **Booking** | 🟢 13/13 rules PASS | All 12 integration events registered; 3 new BG services (SlotLockCleanupService, DocumentExpiryCheckService, ProviderAutoAcceptService); unique filtered idx on SlotLock |
| **Finance** | 🟢 12/12 rules PASS | Money 4dp banker's rounding; InvoiceNumberCounter with optimistic concurrency (no raw SQL); CommissionLookupService wired; Math.Clamp moved to query handlers; PayoutBatchingService PeriodicTimer |
| **Social** | 🟢 ALL PASS | 7 new inbox handlers (ContentPlaces + ContentTours CRUD); 3 snapshot entities; both BG services PeriodicTimer; Bayesian rating recalc |
| **Messaging** | 🟢 ALL PASS | ReadNotificationCleanupService refactored to PeriodicTimer; 22/22 endpoints; SignalR Hub with [Authorize] + groups; 16 event handlers; 6 integration events registered |
| **Analytics** | 🟢 ALL PASS | A-R1 202 Accepted; A-R2 HybridCache 5min dedup; A-R3 correct score formula (V×1+C×2+F×5+BS×8+BC×15+R×4 + 30d half-life); A-R4 7d-vs-prev-7d trending delta; PopularityScoreCalculationService BG; 15 inbox handlers (Accounts skipped — no contract) |
| **Auth-Cleanup** | 🟢 CLEAN | Zero bare `.RequireAuthorization()`; 8 ContentPlaces handlers clean of ICurrentUser; SeoRedirectMiddleware wired |

### 2.1 Cross-cutting handler coverage (from `event-handler-coverage-report.md`)

| Module | Domain events | Integration events | Status |
|---|---|---|---|
| Auth | 4/4 ✅ | 5/5 ✅ | UserRegistered emitter activated (was dead code) |
| Security | 6/6 ✅ | 6/6 ✅ | Full |
| ContentBlogs | 15/15 ✅ | 5/11 ✅ | 6 Phase-2 sink events (no current consumer needed) |
| ContentCore | 12/12 ✅ | 1/8 ✅ | Language only consumed; others Phase-2 future |
| ContentPlaces | 9/9 ✅ | 8/11 ✅ | Strong; 3 minor gaps documented |
| ContentSeo | 9/9 ✅ | 0/5 (sink) | Emit-only by design |
| ContentTours | 9/9 ✅ | varies | Strong |
| Booking | 14/14 ✅ | 12/12 ✅ | All emitters present (Phase B work) |
| Finance | 18/18 ✅ | 10/13 (3 Phase-3 stubs) | All non-OOS emitters wired |
| Social | 12/12 ✅ | 5/5 ✅ | All emitters present |
| Messaging | 16/16 ✅ | 5/6 (SupportSla Phase-3) | All non-OOS emitters present |
| Analytics | 10/10 ✅ | 3/3 (emit-only sink) | All registered |

### 2.2 Background Services (11 running)

1. `SlotLockCleanupService` (Booking, every 2 min)
2. `DocumentExpiryCheckService` (Booking, daily)
3. `ProviderAutoAcceptService` (Booking, 20h timeout)
4. `PayoutBatchingService` (Finance, Sunday midnight UTC)
5. `RefundRetryService` (Finance, every 15 min)
6. `OrphanedFavoritesCleanupService` (Social, weekly Sat 03:00 UTC)
7. `RatingRecalculationService` (Social, daily 03:00 UTC)
8. `EmailNotificationSenderService` (Messaging, 30s PeriodicTimer)
9. `ReadNotificationCleanupService` (Messaging, weekly Sunday 02:00 UTC)
10. `PopularityScoreCalculationService` (Analytics, every 6h)
11. `InteractionIngestDrainService` (Analytics, continuous Channel<T> drain)

Plus 1 SignalR Hub: `NotificationHub` at `/hubs/notifications`.

---

## 3. PDF1 Endpoint Gaps (32 missing endpoints)

Reference: `Agents/agent-context.md` (PDF1 — 182 endpoints across 6 waves)

### 3.1 Wave 1 — 7 missing (~10h backend work)
See `Agents/Waves/Wave-1.md` for detail.
- 3 OAuth split endpoints: `POST /external/apple`, `/facebook`, `/google` (currently 1 generic)
- 2 role endpoints: `GET /admin/roles/{id}/claims`, `DELETE /admin/roles/{id}`
- 1 user role: `DELETE /users/{id}/roles/{roleName}` (currently uses GUID)
- 1 user status: `PUT /users/{id}/status` (currently split POST endpoints)

### 3.2 Wave 2 — 11 missing (~42h backend work) **LARGEST GAP**
See `Agents/Waves/Wave-2.md` for detail.

**ENTIRE Provider Application module is missing.** Requires building:
- New aggregate `ProviderApplication` with state machine (Pending → Approved/Rejected/MoreDocsNeeded → Suspended)
- 5 enums (ProviderType, ProviderApplicationStatus)
- 9 domain events + 6 integration events
- Repositories + EF configs + migration `AccountsAddProviderApplicationModule`
- 11 endpoints:
  - `GET /api/v1/admin/providers`
  - `GET /api/v1/provider/status`
  - `POST /api/v1/provider/register`
  - `POST /api/v1/provider/apply`
  - `POST /api/v1/provider/documents` + `PUT /provider/documents/{id}`
  - `POST /api/v1/admin/providers/{id}/{approve|reject|request-docs|suspend}`
  - `POST /api/v1/profile/avatar` (canonical) + 2 attachment image flows

### 3.3 Wave 3 — 5 missing (~12h backend work)
See `Agents/Waves/Wave-3.md` for detail.
- `GET /api/v1/guides/{id}` + `PUT /guides/{id}` (profile)
- `POST /guides/{id}/languages` + `DELETE /guides/{id}/languages/{langId}`
- `POST /guides/{id}/specializations`

### 3.4 Wave 4 — 🟢 COMPLETE
All 42+ endpoints built. Reference: `Agents/Waves/Wave-4.md`.

### 3.5 Wave 5 — 15 missing (~60h backend work) **SECOND-LARGEST GAP**
See `Agents/Waves/Wave-5.md` for detail.

**Availability Slots (6 endpoints):**
- `GET /api/v1/availability/{tourId}` + `/{date}`
- `POST /availability/slots` + `PUT/DELETE /{id}` + `POST /slots/bulk` (recurring 90d)

**Refund Policies (3 endpoints):**
- `GET /refund-policies/{tourId}` + `POST` + `PUT /{id}`

**Join Requests (3 endpoints):**
- `POST /bookings/join-request` + `/{id}/approve` + `/{id}/reject`

**Provider booking reads (3 endpoints):**
- `GET /bookings/provider/pending` + `/upcoming` + `/history`

### 3.6 Wave 6 — 🟢 COMPLETE
All 80+ endpoints + 11 BG services + SignalR Hub built. Reference: `Agents/Waves/Wave-6.md`.

### 3.7 Total backend work to PDF1 parity
**~132 hours (≈4 weeks for 2 mid-level devs)** to close all 32 PDF1 endpoint gaps.

---

## 4. PDF2 Business Rules Coverage

Reference: `Agents/guide.md` (PDF2 — 24 sections)

| Section | Coverage | Notes |
|---|---|---|
| §1 Provider Registration | 🟡 Spec'd, not built | All rules documented in Wave-2.md; backend work pending |
| §2 Tour/Place Management | 🟢 Enforced | Tour status state machine, place admin-only, critical field re-approval |
| §3 Categories | 🟢 Enforced | 3-level max, unique per parent, soft-delete |
| §4 Booking/Reservations | 🟡 Partial | SlotLock TTL, optimistic concurrency, ConfirmationCode YJ-YYYYMM-XXXX ✅; **Wave 5 availability gap** |
| §5 Payment/Refund | 🟢 Enforced | F-R1..F-R12 all enforced; escrow 7d; commission tiered; refund snapshot |
| §6 Reviews | 🟢 Enforced | S-R1..S-R7; 30d window; 48h edit; weighted Bayesian rating; profanity |
| §7 Wishlist | 🟢 Enforced | 500/user cap; toggle; 3 notif/day rate limit |
| §8 SEO | 🟢 Enforced | Sitemap regen 6h; hreflang ar+en; redirect chain ≤3 hops |
| §9 Blog | 🟢 Enforced | Admin-only; 500 char title; max 1 place + 10 tours; comments 2-level nest |
| §10 Map | 🟢 Coordinates validated | Mapbox Jordan default; clustering rules in UI doc |
| §11 Weather | 🟢 Enforced | 12h cache TTL; 1000/day budget; pre-fetch top 50 |
| §12 Notifications | 🟢 Enforced | M-R1..M-R12; SignalR + email; critical types non-disableable |
| §13 Children-Friendly | 🟢 Enforced | IsChildFriendly + MinAge/MaxAge + AgeRestriction validation |
| §14 Accessibility (data) | 🟢 Enforced | AccessibilityFeatures table; search filter |
| §15 Packaging | 🟡 Endpoints exist | TourPackage 6 endpoints built; payout proportional split needs verification |
| §16 Subscriptions | 🔴 Phase 3 | Entities are stubs only; commission tiers wired but no billing |
| §17 Referral & Loyalty | 🔴 Phase 3 | Referral/LoyaltyPoints entities are stubs |
| §18 Rating Algorithm | 🟢 Enforced | Bayesian C=10 + recency decay; daily 03:00 UTC recalc |
| §19 Dispute Center | 🔴 Phase 3 | DisputeOpened/Resolved events exist but no endpoints/flow |
| §20 Live Tracking | 🔴 Phase 4 | Entire module not built |
| §21 Recommendations | 🔴 Phase 4 | Only popularity exists; no collaborative filter / content-based |
| §22 AI Chatbot | 🔴 Phase 4 | ChatBot entities are stubs in Messaging |
| §23 Accessibility UI | 🔴 Phase 4 | Frontend-only feature; documented in UI-UX-Design.md |
| §24 Discounts | 🔴 Phase 3 | Discount entity exists; no checkout integration |

**Phase 1+2 coverage: ~95%** (gaps = Wave 2 Provider Application module + Wave 5 sub-features + minor §3 backend edge cases).

---

## 5. Frontend (UI) Status

Reference: `Agents/UI/UI-UX-Design.md`

### 5.1 Decision: **ASP.NET Core MVC with Webestica Bootstrap template**
- Template: `C:\Users\admin1\Desktop\Template\hotel-management-syste-main\booking.webestica.com`
- 74 Bootstrap 5 HTML pages, RTL-ready, 3-role architecture (User/Provider/Admin) matches YallaJo exactly
- Extends existing `YallaJo.Web` MVC project
- Server-rendered Razor with cookie auth + JWT-claim Bearer pattern
- HttpClient → YallaJo.Api (BFF pattern, separate processes)

### 5.2 Documented but not built
- **Project structure** (Areas/Account, Areas/Provider, Areas/Admin) — §3
- **~120 MVC routes** mapped to PDF1 endpoints — §4
- **API client interfaces** (~30 typed HttpClient services) — §5
- **Cookie auth + AuthTokenHandler** with silent refresh — §6
- **SignalR notification bell integration** — §7
- **i18n (AR+EN) with RTL switching** — §8
- **SEO strategy** (ViewData meta + JSON-LD + sitemap proxy) — §9
- **Forms with DataAnnotations + AntiForgery** — §10
- **Caching (OutputCache + IMemoryCache + ETag)** — §11
- **19-week sprint plan** — §12
- **90+ performance rules** (UI-PERF-A through UI-PERF-X, 14 categories) — §18
- **Performance budgets** (LCP <2s, FCP <1.2s, TTI <3s, CLS <0.05) — §18.11
- **Per-page weight targets** (9 routes with specific budgets) — §18.14

### 5.3 Critical dependency map (UI sprint → Backend wave)

| UI Sprint | Blocked on backend |
|---|---|
| UI-2 (Auth) | Wave 1 — 3 OAuth split endpoints |
| UI-5 (Booking flow) | Wave 5 — Availability + Refund Policy |
| UI-7 (Provider onboarding) | **Wave 2 — Entire Provider Application module** |
| UI-8 (Provider dashboard) | Wave 5 — 3 provider booking read endpoints |
| UI-9 (Provider advanced) | Wave 5 — Join Requests |

UI Sprints UI-1, UI-3, UI-4, UI-6, UI-10, UI-11, UI-12, UI-13 can start immediately — backend dependencies are 100% met.

---

## 6. Phase 3+4 Backlog (Out of scope for PDF1)

### 6.1 Phase 3 (Subscriptions / Loyalty / Discounts / Disputes)
- §15 TourPackages: payout proportional split logic needs verification
- §16 Subscription billing: needs full module (plans, billing cycles, prorated upgrades, grace period)
- §17 Referral & Loyalty: needs ReferralCode generation, point earning/redemption, FIFO expiry
- §19 Dispute Center: needs Dispute aggregate, DisputeEvidence/Messages, admin resolution flow
- §24 Discount Engine: needs checkout integration, stacking rules (max 2), promo code validation

### 6.2 Phase 4 (Live Tracking / Recommendations / AI Chatbot / Accessibility UI)
- §20 Live Tracking: LiveTrackingSessions, LocationSnapshots, SignalR + GPS broadcast, TourCheckpoints auto-detect
- §21 Recommendations: collaborative filter (Signal 1, weight 40%), content-based (Signal 2, weight 35%), hybrid scoring
- §22 AI Chatbot: external LLM provider integration, ChatBotConversations + Messages, rate limits, context enrichment
- §23 Accessibility UI: frontend-only — high contrast, font scaling, screen reader ARIA, color blindness filters

---

## 7. Repository State

- **Branch:** master
- **Position:** ~30+ commits ahead of origin/master
- **Pushed:** NOTHING is pushed to remote — all work is local-only
- **Builds:** All projects green (0 errors)
- **Tests:** All passing
- **Migrations:** Applied to dev DB; pending production deploy

### 7.1 Modules in solution (verified working)
Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, ContentBlogs, ContentSeo, Booking, Finance, Social, Messaging, Analytics — 13 modules, each with 5 projects (Domain, Contracts, Application, Infrastructure, Presentation). Plus `YallaJo.SharedKernel.*` (4 projects) + `YallaJo.Api` + `YallaJo.Web` + 13 test projects = **~95 projects** in solution.

---

## 8. Recommended Next Sprint Plan

### 8.1 Option A: Close backend gaps first (recommended)
**Sprint X1 (Week 1-2 = 42h):** Wave 2 Provider Application module — 2 devs parallel
**Sprint X2 (Week 3 = 32h):** Wave 5A Availability Slots + 5D Provider reads
**Sprint X3 (Week 4 = 36h):** Wave 5B Refund Policies + 5C Join Requests
**Sprint X4 (Week 5 = 24h):** Wave 1 OAuth + Wave 3 Tour Guide sub-resources
**Total: ~132h (~4 weeks for 2 backend devs to reach PDF1 parity)**

### 8.2 Option B: Start UI in parallel with backend gap closure
- Begin UI-0 (Foundation) + UI-1 (Public marketing) + UI-2 (Auth, except OAuth split) immediately
- Backend team works on Waves 1/2/3/5 gaps in parallel
- Sync points every 2 weeks
- **Estimated: ~6 weeks total** (parallel tracks)

### 8.3 Option C: Push current work to origin + plan from there
- Push 30+ commits to origin/master
- Open PRs for review
- Establish CI/CD pipeline
- Then proceed with Option B

**Recommendation: Option C → Option B.** Get current backend work into shared repo + CI/CD first; then parallel-track UI and backend gap closure.

---

## 9. Reference Files

All documentation is in `C:\Users\admin1\source\repos\YallaJo\Agents\`:

| File | Purpose |
|---|---|
| `agent-context.md` | PDF1 endpoint spec (source of truth) |
| `guide.md` | PDF2 business rules (source of truth) |
| `YallaJo.md` | Project root doc |
| `error-log.md` | Historical error tracking |
| `event-handler-coverage-report.md` | Cross-module event handler matrix |
| `Final-Gap-Report.md` | **This file** |
| `Waves/Wave-1.md` | Wave 1 spec + gaps (7 endpoints, ~10h) |
| `Waves/Wave-2.md` | Wave 2 spec + gaps (11 endpoints, ~42h — LARGEST) |
| `Waves/Wave-3.md` | Wave 3 spec + gaps (5 endpoints, ~12h) |
| `Waves/Wave-4.md` | Wave 4 verification (✅ complete) |
| `Waves/Wave-5.md` | Wave 5 spec + gaps (15 endpoints, ~60h — second-largest) |
| `Waves/Wave-6.md` | Wave 6 verification (✅ complete) |
| `UI/UI-UX-Design.md` | Full MVC frontend design + endpoint map + 90+ performance rules |

---

## 10. Bottom Line

**Backend (Phase 1+2):**
- 6 sprints done + audited + cleaned
- 150/182 PDF1 endpoints built (82%)
- 95% PDF2 business rules enforced
- All builds green, tests passing
- ~132h work to reach 100% PDF1 parity

**Frontend:**
- Fully designed (MVC + Webestica Bootstrap template)
- 0% built
- ~19 weeks (4.5 months) for 2 senior devs to build complete UI

**Phase 3+4:**
- Not started; documented as backlog
- ~6+ months additional work

**Critical path:** Close Wave 2 Provider Application module (42h) — unblocks UI-7 Provider onboarding, which is the biggest UI sprint. Everything else flows from there.

The platform is production-ready for an MVP launch covering anonymous browse + user booking + payment + reviews + admin moderation, contingent on closing the Wave 2 + Wave 5 backend gaps and building the MVC frontend.
