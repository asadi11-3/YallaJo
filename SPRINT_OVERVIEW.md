# YallaJo Complete Sprint Overview (Wave 5 → Phase 2 Closure)

## Executive Summary

This document consolidates **6 major sprints** covering Wave 5 (Booking/Finance) and Wave 6 (Social/Messaging) completion, plus Phase 2 closure (Analytics) and cross-cutting cleanup (Authorization-Cleanup).

**Total Scope:** 6 modules × 30-45 working days = ~200-300 person-hours. **Timeline:** June 2026 → March 2027 (9 months).

---

## Sprint Sequence & Dates

| # | Sprint | Start | End | Type | Modules | Difficulty | Endpoints | BG Services |
|---|--------|-------|-----|------|---------|------------|-----------|-------------|
| 1 | Booking | 2026-06-15 | 2026-08-13 | Core feature | Booking | ⚙️⚙️⚙️⚙️⚙️ 5/5 | 22 | 4 |
| 2 | Finance | 2026-08-17 | 2026-10-15 | Core feature | Finance | ⚙️⚙️⚙️⚙️ 4/5 | 17 | 2 |
| 3 | Social | 2026-10-19 | 2026-11-27 | Wave 6 | Social | ⚙️⚙️ 2/5 | 22 | 2 |
| 4 | Messaging | 2026-11-29 | 2027-01-14 | Wave 6 | Messaging | ⚙️⚙️⚙️⚙️ 4/5 | 22 HTTP + 1 hub | 2 |
| 5 | Analytics | 2027-01-18 | 2027-02-26 | Phase 2 | Analytics | ⚙️⚙️⚙️ 3/5 | 17 | 1 |
| 6 | Authorization-Cleanup | 2027-02-28 | 2027-03-11 | Cross-cutting | Cross 5 modules | ⚙️⚙️ 2/5 | 28 refactored | 0 |

---

## Module Dependency Graph

```
Phase 1/2 (Completed)
├── Auth
├── Security
├── Accounts
├── ContentCore
├── ContentPlaces
├── ContentTours
└── ContentBlogs-ContentSeo

Wave 5 (Overlapping)
├── Booking (220h, hardest)
│   └── Finance (200h) — consumes booking.tour-booking.* events
│       └── Messaging (175h) — consumes finance.payment.* events
│
Social (130h) ← parallel with Messaging, consumes booking.tour-booking.completed.v1
├── Messaging inbox handler: "new review" notifications

Analytics (130h) ← parallel phase 2 closure
├── Messaging inbox handler: notification delivery tracking
└── All modules emit analytics.* events

Authorization-Cleanup (60h) ← final cross-cutting refactor
└── Fixes 28 endpoints across Auth/Accounts/Security/ContentCore/ContentPlaces
```

---

## Booking Sprint (2026-06-15 → 2026-08-13)

**Owner:** Mohammad (Lead) + Mahmoud + Fadwa + Tech Lead
**40 working days × 4 devs = 190 person-hours**

### Key Features
- **22 HTTP endpoints** covering 7 tasks (TASK 1-7)
- **4 background services:** SlotLockCleanup, BookingAutoExpire, ProviderAutoAccept, DocumentExpiryCheck
- **5-step POST /tour booking engine** (highest-risk component)
- **Optimistic concurrency** on `AvailabilitySlot.RowVersion`
- **Escrow model** payment flow → Finance handoff

### Critical Rules (B-R1..B-R12)
- B-R1: Money type `decimal(19,4)` only, ISO 4217 currency enum
- B-R2: Slot capacity concurrency via RowVersion + `DbUpdateConcurrencyException` catch
- B-R3: Booking reference `YJ-YYYYMMDD-XXXXXX` (6 chars, safe alphabet)
- B-R4: 2-hour lead time minimum, no duplicate same-date bookings, max 3 concurrent AwaitingPayment
- B-R5: Refund calc via RefundPolicy tiers + 100% for provider-initiated
- B-R6: Provider confirmation window 24h (auto-confirm via BG service)
- B-R7: Outbox model for cross-module events
- B-R8: SlotLock TTL 10min, cleanup resets IsActive flag
- B-R9: Document expiry suspension for critical docs
- B-R10: Cursor pagination (opaque base64 `{Id, CreatedAt}`)
- B-R11: Authorization matrix (19 permission checks per endpoint)
- B-R12: Validation (ParticipantCount ≤ AvailableCount, trim strings)

### Integration Events (12 emitted)
- `booking.tour-booking.{created,confirmed,cancelled,completed,rejected,payment-expired}.v1`
- `booking.join-request.{approved,rejected}.v1`
- `booking.provider-document.{expiring,expired}.v1`
- `booking.provider.suspended-doc-expired.v1`
- `booking.slot-lock.expired.v1`

### Pre-Work (PW-1..PW-8, Tech Lead)
1. BookingUnitOfWork → SharedKernel IUnitOfWork delegation
2. IAggregateRoot markers + AuditableEntity on 5 entities
3. 14 domain event records
4. 12 integration event records + registry
5. 8 repository interfaces
6. ICommissionLookupService + IDiscountEvaluator stubs
7. BookingFeatures + 26 permissions
8. Test projects scaffolded

---

## Finance Sprint (2026-08-17 → 2026-10-15)

**Owner:** Mohammad (Lead Payouts) + Mahmoud (Payments) + Fadwa (Invoices) + Junior (Commission)
**45 working days × 4 devs = 200 person-hours**

### Key Features
- **17 HTTP endpoints** covering 6 tasks
- **2 background services:** PayoutBatchingService (weekly Sun midnight UTC), RefundRetryService (15min)
- **Escrow → Payout pipeline** (7-day dispute window before payout)
- **PCI compliance baseline** (webhook HMAC verification, no card storage, TLS-only)
- **Idempotency on webhook** via `GatewayTransactionId` UNIQUE constraint

### Critical Rules (F-R1..F-R12)
- F-R1: Money type `decimal(19,4)`, banker's rounding
- F-R2: Webhook HMAC-SHA256 verification BEFORE body parse
- F-R3: Idempotency via GatewayTransactionId
- F-R4: Escrow model (payments → platform escrow, NOT direct to provider)
- F-R5: Refund calc from Booking policy, gateway execution, commission retention config
- F-R6: Commission tier resolution (subscription tier override → default rule → 15% fallback)
- F-R7: Payout batching (Sunday UTC, group by Provider+Currency, hold conditions apply)
- F-R8: Invoice generation (auto on PaymentCompleted, INV-{YYYYMM}-{seq6} format, PDF on download)
- F-R9: Audit logging (state changes → analytics.AuditLogs, redacted for PCI)
- F-R10: Cursor pagination
- F-R11: Authorization matrix (17 permission checks)
- F-R12: Error registry (20+ codes)

### Integration Events (9 emitted)
- `finance.payment.{completed,failed}.v1`
- `finance.refund.{initiated,completed,failed}.v1`
- `finance.invoice.generated.v1`
- `finance.payout.{scheduled,completed}.v1`
- `finance.commission-rule.{upserted,deleted}.v1`

### Payment Gateway Abstraction
- `IPaymentGateway` interface (InitiateAsync, RefundAsync, PayoutAsync, VerifyWebhookSignatureAsync)
- `FakePaymentGateway` stub impl (dev/staging)
- Real impls (StripeGateway, HyperPayGateway) deferred to parallel ticket
- Polly retry policy (3× exponential backoff)

---

## Social Sprint (2026-10-19 → 2026-11-27)

**Owner:** Mahmoud (Reviews) + Mohammad (Reports/Moderation + RatingRecalc) + Fadwa (Favorites + OrphanedFavorites)
**30 working days × 4 devs = 130 person-hours**

### Key Features
- **22 HTTP endpoints** covering 4 tasks
- **2 background services:** OrphanedFavoritesCleanup (Sat 03:00 UTC), RatingRecalculationService (daily 03:00 UTC)
- **Bayesian rating** (verified-booking weight 1.0 / non-verified 0.5, recency decay)
- **5-report auto-hide** + admin restoration
- **Verified-booking gate** for reviews (30-day window from completion)

### Critical Rules (S-R1..S-R10)
- S-R1: Verified-booking gate (anyone can review, but badge for completed bookings within 30d)
- S-R2: One review per user per target (hard uniqueness with soft-delete skip)
- S-R3: 48-hour edit window (based on CreatedAt, not UpdatedAt)
- S-R4: Profanity filter lifecycle (BlocklistProfanityFilter 50 EN+AR words, auto AwaitingModeration status)
- S-R5: 5-report auto-hide (idempotent, admin can restore/remove)
- S-R6: Bayesian rating algorithm (AvgRating weighted by verification + recency, BayesianScore with C=10 global avg)
- S-R7: Favorites limit 500/user + toggle (POST idempotent 409 if exists, DELETE idempotent 204)
- S-R8: ICurrentUser discipline (12 handlers documented)
- S-R9: Cursor pagination + cache strategy
- S-R10: Error code registry (27 codes)

### Integration Events (5 emitted)
- `social.review.{published,deleted}.v1`
- `social.favorite.added.v1`
- `social.report.resolved.v1`
- `social.rating.recalculated.v1`

### Bayesian Formula
```
weight_i = VerificationWeight × RecencyWeight
  Verified → 1.0, Non-verified → 0.5
  <90d → 1.0, 90-180d → 0.7, >180d → 0.5

AverageRating = Σ(rating_i × weight_i) / Σ(weight_i)
BayesianScore = (ReviewCount × AvgRating + C × GlobalAverage) / (ReviewCount + C)  [C=10]
```

---

## Messaging Sprint (2026-11-29 → 2027-01-14)

**Owner:** Mohammad (NotificationHub SignalR + EmailSender BG) + Mahmoud (Notifications CRUD) + Fadwa (Devices + SupportTickets + ReadCleanup)
**30 working days × 4 devs = 175 person-hours**

### Key Features
- **22 HTTP endpoints** + 1 SignalR hub
- **2 background services:** EmailNotificationSender (30s interval, 3 retries 1+5+15min backoff), ReadNotificationCleanup (Sun 02:00 UTC, purge 30d old read)
- **Real-time notifications** via SignalR groups (user:{userId}, provider:{providerId}, admin)
- **23 inbox handlers** consuming events from Auth/Accounts/ContentPlaces/Booking/Finance/Social/Accounts
- **Critical notifications** never disabled (OTP, Payment, Refund, Security alerts)

### Critical Rules (M-R1..M-R12)
- M-R1: Critical notifications bypass preference (8 types always delivered)
- M-R2: 4 channels (InApp SignalR, Email SMTP, Push FCM/APNs stub, SMS deferred)
- M-R3: Notification creation chain (resolve template → create → dispatch fans out)
- M-R4: Mustache placeholder syntax with fallback chain
- M-R5: Notification capacity 500/user (oldest read purged first)
- M-R6: SignalR connection JWT required, 3 groups (user, provider, admin)
- M-R7: Email retry 3× exponential backoff 1+5+15min
- M-R8: Support ticket SLA (High 4h, Medium 12h, Low 24h) + round-robin assignment
- M-R9: Device token uniqueness + 30-day stale cleanup
- M-R10: Cursor pagination
- M-R11: ICurrentUser audit (12 handlers allowed)
- M-R12: Error code registry (13 codes)

### Integration Events (6 emitted)
- `messaging.notification.{delivered,failed}.v1`
- `messaging.ticket.{created,assigned,resolved}.v1`
- `messaging.support-sla-breached.v1` (deferred Phase 3)

### Support Ticket SLA Matrix
| Category | Priority | SLA |
|----------|----------|-----|
| PaymentProblem | High | 4h |
| BookingIssue | Medium | 12h |
| ProviderComplaint | Medium | 12h |
| AccountHelp | Low | 24h |
| BugReport | Low | 24h |
| Other | Low | 24h |

---

## Analytics Sprint (2027-01-18 → 2027-02-26)

**Owner:** Mahmoud (Interactions ingest) + Mohammad (Popular/Trending + PopularityScoreCalc BG) + Fadwa (Admin dashboards + Audit logs)
**30 working days × 4 devs = 130 person-hours**

### Key Features
- **17 HTTP endpoints** covering 6 tasks
- **1 background service:** PopularityScoreCalculationService (daily 03:00 UTC, Bayesian trending DELTA)
- **High-write ingest** (fire-and-forget, 202 within 50ms, Channel<InteractionEnvelope> drain pattern)
- **Popularity formula** (views×1 + clicks×2 + favorites×5 + bookings_started×8 + bookings_completed×15 + reviews×4 + rating_bonus + recency_decay)
- **Trending DELTA** (current score vs 7d-ago snapshot, capture viral/seasonal spikes)

### Critical Rules (A-R1..A-R10)
- A-R1: Fire-and-forget ingest (202 within 50ms, async Channel enqueue, drop on full)
- A-R2: Interaction dedup (5min same user+entity+type, HybridCache + DB check)
- A-R3: Popularity score formula (8 weighted components + recency decay half-life 30d)
- A-R4: Trending DELTA (current vs 7d-ago snapshot, new entities penalty 0.5×)
- A-R5: Dashboard cache strategy (30s sliding for overview, 5min for popular/trending, DashboardCache L2)
- A-R6: Audit log redaction (hard: no card/password/secret storage; soft: hash email, last-4 phone, /24 IP)
- A-R7: Audit log retention (2 years, enforcement deferred)
- A-R8: Cursor pagination (BIGINT PK, `{Id, OccurredAt}` tuple)
- A-R9: ICurrentUser audit (only 3 handlers allowed)
- A-R10: Error codes (10 codes)

### Integration Events (3 emitted)
- `analytics.popularity-scores.recalculated.v1`
- `analytics.audit-log.entry-redacted.v1`
- `analytics.trending.refreshed.v1`

### Popularity Formula
```
Score = (Views × 1.0) +
        (Clicks × 2.0) +
        (Favorites × 5.0) +
        (BookingsStarted × 8.0) +
        (BookingsCompleted × 15.0) +
        (Reviews × 4.0) +
        (RatingBonus = 0 if AvgRating < 3.5 else (AvgRating - 3.5) × 10) +
        (RecencyDecay = weight × 0.5 ^ (daysAgo / 30))
```

---

## Authorization-Cleanup Sprint (2027-02-28 → 2027-03-11)

**Owner:** Mahmoud (ICurrentUser handlers) + Fadwa (AUTH_ONLY endpoints) + Mohammad (STRING_POLICY + MISSING_METADATA + SeoRedirectMiddleware)
**10 working days × 4 devs = 60 person-hours**

### Key Features
- **0 new endpoints** — pure refactoring
- **28 endpoints fixed** across Auth/Accounts/Security/ContentCore/ContentPlaces
- **1 middleware added:** SeoRedirectMiddleware (5min cache, fire-and-forget HitCount increment)
- **No new migrations, permissions, or aggregates**

### Tasks
1. **TASK 1 (16h, Mahmoud):** Remove 8 `ICurrentUser` violations in ContentPlaces handlers
   - Pattern: remove injection, pass UserId as command property from endpoint
2. **TASK 2 (12h, Fadwa):** Fix 14 AUTH_ONLY violations (bare `.RequireAuthorization()`)
   - Pattern: replace with typed `MustHavePermissionAttribute`
3. **TASK 3 (16h, Mohammad):** Fix 14 STRING_POLICY + 4 MISSING_METADATA violations
   - Pattern: replace string policies with typed attributes, add explicit `.AllowAnonymous()` for public
4. **TASK 4 (12h, Mohammad, BONUS):** Add `SeoRedirectMiddleware`
   - Layer 12 in middleware pipeline (after `UseAuthorization`, before module endpoints)
   - Reads `ContentSeo.SeoRedirects` table, 5min cache, fire-and-forget HitCount

### Critical Rules (AC-R1..AC-R8)
- AC-R1: Every endpoint has explicit auth (either `MustHavePermissionAttribute` OR `.AllowAnonymous()`)
- AC-R2: `ICurrentUser` only for ownership/IDOR/creator-stamp (never for IsAuthenticated checks)
- AC-R3: Test coverage mandatory (metadata-presence tests + per-endpoint intent tests)
- AC-R4: `MustHavePermissionAttribute` typed (never string policies)
- AC-R5: Error code discipline (403 Forbidden for IDOR, never 401 from handler)
- AC-R6: Middleware order (SeoRedirectMiddleware layer 12)
- AC-R7: Cache discipline (HybridCache 5min sliding, fire-and-forget HitCount)
- AC-R8: Pre-PR self-check (`dotnet build`, metadata tests, module tests all green)

---

## Universal Critical Rules (Phase1-Phase2-Completion-INDEX §4)

All 6 sprints enforce these 16 rules:

| Rule | Title | Enforcement |
|------|-------|-------------|
| R1 | Money type `decimal(19,4)` | Explicit column mapping, no float/double |
| R2 | `ICurrentUser` ownership-only | No IsAuthenticated checks in handlers |
| R3 | Domain event dispatch | UnitOfWork delegates to SharedKernel IUnitOfWork |
| R4 | Idempotency via PK uniqueness | UNIQUE constraints on integration-event natural keys |
| R5 | Repository interfaces, no leaky DbContext | IReadRepository/IWriteRepository abstraction |
| R6 | Result<T> error handling | Outcome enum (Validation, Conflict, NotFound, Forbidden, ExternalServiceError) |
| R7 | Async-only persistence | No blocking `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` |
| R8 | String trimming + empty-check on input | Validator + property setter |
| R9 | Cursor pagination (not offset) | Opaque base64 `{Id, CreatedAt}` with tie-break sort |
| R10 | Cache tags (most-specific granularity) | Invalidate on state change AFTER SaveChanges |
| R11 | State guards on aggregates | Calling method on wrong state returns Error, not exception |
| R12 | Exception translation in Infrastructure | DbUpdateConcurrencyException → Result.Failure(Outcome.Conflict) |
| R13 | Error codes PascalCase `{Entity}.{Reason}` | No generic "Error" or "Failed" strings |
| R14 | Soft-delete via IsDeleted + DeletedAt | Hard-delete forbidden, audit trail preserved |
| R15 | Outbox model (per INDEX §4 R15 deep dive) | Domain event → integration event → inbox handler → MarkAsProcessed → single SaveChanges |
| R16 | Metadata-driven authorization | MustHavePermissionAttribute ONLY, never string policies or bare `.RequireAuthorization()` |

---

## Cross-Module Permission Summary

| Module | Features (count) | Actions |
|--------|-----------------|---------|
| Booking | 7 (AvailabilitySlot, RefundPolicy, Commission, ProviderDocument, TourBooking, JoinRequest, Dashboards) | Create, Read, Edit, Delete, Approve, Reject, Cancel |
| Finance | 7 (Payment, Refund, Invoice, Payout, CommissionRule, ProviderBankAccount, AdminFinanceDashboard) | Create, Read, Download, Trigger, Approve, Verify |
| Social | 6 (Review, ReviewReply, Favorite, Report, ContentModerationLog, AdminModerationQueue) | Create, Read, Update, Delete, Warn, Ban |
| Messaging | 6 (Notification, NotificationPreference, NotificationTemplate, DeviceToken, SupportTicket, AdminSupportQueue) | Create, Read, Update, Delete, Close, Assign, Resolve |
| Analytics | 6 (Interaction, PopularityScore, Trending, AdminDashboard, ProviderDashboard, AuditLog) | Create, Read, Refresh, Export, Redact |
| **Total** | **32 feature groups** | **50+ distinct actions** |

---

## Gotchas & Anti-Patterns Documented

Reference: `Agents/error-log.md` (33 gotchas from prior sprints). Key examples:
- **ERR-001:** UnitOfWork not delegating → domain events silently dropped
- **ERR-002:** IAggregateRoot marker missing → repository methods don't compile
- **ERR-018:** Gmail SMTP app password with spaces → auth fail (strip spaces)
- **ERR-027:** Method overloading with Result return → must disambiguate in LSP renames

---

## Acceptance Gate Checklist (per sprint)

Every sprint has `99-acceptance-gate.md`:
- [ ] All endpoints return correct HTTP status (via `result.ToApiResult()`)
- [ ] All error codes in error registry
- [ ] All domain events emit + integrate events register
- [ ] All repositories implement custom finders correctly
- [ ] Pre-work sign-off (tech lead)
- [ ] Mid-sprint integration freeze met
- [ ] Hard PR cutoff met
- [ ] Hard merge-to-main cutoff met
- [ ] Build green (zero warnings)
- [ ] Tests green (unit + integration)
- [ ] Sprint retro + demo completed

---

## Key Milestones

| Date | Milestone | Status |
|------|-----------|--------|
| 2026-06-12 | Booking PW cut | Pending |
| 2026-06-16 | Booking PW merge deadline | Pending |
| 2026-08-13 | Booking hard merge deadline | Pending |
| 2026-10-15 | Finance hard merge deadline | Pending |
| 2026-11-27 | Social hard merge deadline | Pending |
| 2027-01-14 | Messaging hard merge deadline | Pending |
| 2027-02-26 | Analytics hard merge deadline | Pending |
| 2027-03-11 | Authorization-Cleanup hard merge deadline | Pending |
| 2027-03-12 | Phase 2 closure complete ✅ | Target |

---

## Technology Stack (Consistent Across All Sprints)

| Layer | Technology | Version |
|-------|-----------|---------|
| **Framework** | .NET 9 | 9.0.15 |
| **API** | ASP.NET Core Minimal APIs | 9.0.15 |
| **Database** | SQL Server | 2022+ |
| **ORM** | Entity Framework Core | 9.0.15 |
| **Validation** | FluentValidation | 11.x |
| **Messaging** | MediatR | 12.x |
| **Caching** | HybridCache | 9.0.15 (in-memory L1) |
| **Cache L2** | SQL Server cache table (no Redis v1) |
| **SignalR** | ASP.NET Core SignalR | 9.0.15 |
| **Testing** | xUnit 2.9.3, NSubstitute 5.3.0, FluentAssertions 7.0.0, EF InMemory |
| **PDF** | QuestPDF | latest |
| **Email** | MailKit | latest |
| **Mustache** | Stubble.Core | latest |
| **Background Services** | Hosted services (.NET BackgroundService) | 9.0.15 |
| **Retry/Resilience** | Polly v8 | 8.x |

---

## End-to-End Feature Example: Book a Tour

**User books a tour on 2026-09-01:**

1. **Booking.POST /tour** (TASK 4, Mahmoud)
   - Step 1: Resolve tour via `BookingTourSnapshot` + validate lead time
   - Step 2: Lock slot, decrement capacity (optimistic concurrency check)
   - Step 3: Calculate commission via `ICommissionLookupService` stub
   - Step 4: Create `TourBooking` aggregate (AwaitingPayment), emit `BookingCreatedDomainEvent`
   - Outbox: `booking.tour-booking.created.v1` → Finance inbox

2. **Finance inbox** (`BookingTourBookingCreatedHandler`)
   - Pre-creates `PaymentExpectation` snapshot row
   - Outbox: none (intra-module only)

3. **User calls Finance.POST /payments/initiate**
   - Create `Payment` aggregate (Pending)
   - Call `IPaymentGateway.InitiateAsync()` (Polly retry)
   - Return redirect URL to user

4. **User redirects to payment gateway** → completes payment

5. **Gateway webhook → Finance.POST /payments/webhook**
   - HMAC signature verification (F-R2)
   - Idempotency check via `GatewayTransactionId` (F-R3)
   - Mark `Payment.Completed` + emit `PaymentCompletedDomainEvent`
   - Auto-create `Invoice` (T3, Fadwa)
   - Outbox: `finance.payment.completed.v1` → Booking inbox

6. **Booking inbox** (`FinancePaymentCompletedHandler`)
   - Transition booking AwaitingPayment → Confirmed (instant) OR PendingConfirmation (non-instant)
   - Emit `BookingConfirmedDomainEvent`
   - Outbox: `booking.tour-booking.confirmed.v1` → Messaging + Social + Analytics inboxes

7. **Messaging inbox** (23 handlers, T1 Mahmoud)
   - Create `Notification` (BookingConfirmed type)
   - Dispatcher fans out to InApp (SignalR) + Email (EmailSender BG) channels
   - SignalR pushes "Your booking is confirmed" to `user:{userId}`
   - Email queued in `NotificationDeliveryAttempt` (Pending)

8. **Social inbox** (RatingRecalculation handler, T5 Mohammad)
   - Mark `PopularityScore` stale for tour (BG will recalc)

9. **Analytics inbox** (T1 Mahmoud)
   - Insert `UserInteraction` (BookingCompleted) → ingest queue
   - Insert `AuditLog` row (redacted)

10. **EmailSender BG** (T5, Mohammad) — every 30s
    - Drains `NotificationDeliveryAttempt` queue
    - Sends via SMTP, marks Succeeded
    - Emits `messaging.notification.delivered.v1` → Analytics

11. **Tour occurs on 2026-09-15**
    - Provider calls **Booking.POST /{id}/complete** (TASK 5)
    - Mark `TourBooking.Completed`
    - Emit `BookingCompletedDomainEvent`

12. **Finance inbox** (`BookingTourBookingCompletedHandler`, T4 Mohammad)
    - Set `Payment.EscrowReleaseEligibleAt = CompletedAt + 7 days`

13. **Finance.PayoutBatchingService** (T5, Mohammad) — Sunday midnight UTC
    - Aggregate eligible payments by Provider+Currency
    - Create `Payout` rows
    - Emit `finance.payout.scheduled.v1` → Messaging

14. **Admin calls Finance.POST /payouts/{id}/approve** (T4 Mohammad)
    - Large payouts (>5000 currency) require approval
    - Call `IPaymentGateway.PayoutAsync()` (to provider bank account)
    - Emit `PayoutCompletedDomainEvent`
    - Outbox: `finance.payout.completed.v1` → Messaging + Analytics

15. **Review window opens** (30 days from completion, Social T1 Mahmoud)
    - User calls **Social.POST /reviews**
    - Check verified-booking gate (BookingEligibilitySnapshot)
    - Scan content for profanity (BlocklistProfanityFilter)
    - Create `Review` aggregate (Published or AwaitingModeration)
    - Emit `ReviewPublishedDomainEvent`

16. **RatingRecalculationService** (T5, Mohammad) — daily 03:00 UTC
    - Recalc `PopularityScore` for tour
    - Apply Bayesian formula (reviews weighted by verification + recency)
    - Emit `social.rating.recalculated.v1` → ContentTours inbox

---

## File Organization

Each sprint folder contains:
```
Agents/tasks/{Module}-team-tasks.md
├── 00-README.md           (overview, team allocation, endpoints, events)
├── 01-pre-work.md         (PW-1..PW-N for Tech Lead, hard deadline Tue before kickoff)
├── 02-critical-rules.md   (module-specific rules R1..RN on top of universal R1..R16)
├── 03-entities-matrix.md  (entity ownership, aggregates, migrations, EF configs)
├── 04-task-*.md ... 0N-task-*.md  (per-task detail: endpoints, WBS, acceptance)
├── N-cross-cutting.md     (DI, permission seeder, outbox parity, build lock)
└── 99-acceptance-gate.md  (final Tech Lead sign-off checklist)
```

On close, folder moves to `Agents/decisions/closed/{Module}/`.

---

## Handover & Continuity

**Before each sprint:**
1. Read master `Phase1-Phase2-Completion-INDEX.md` §1-4
2. Read `Agents/agent-context.md` §0-11 (33 gotchas documented)
3. Read `Agents/guide.md` (entity anatomy, CQRS templates)
4. Read sprint's `00-README.md` + `01-pre-work.md`
5. Read your assigned `0N-task-*.md` file(s)
6. Attend kickoff (90 min architecture walkthrough)
7. Run `dotnet build YallaJo.sln` clean (zero warnings)

**During sprint:**
- Daily standup 09:30 AST (15 min hard cap)
- Code review every evening (Tech Lead on rotation)
- Mid-sprint integration freeze (no more feature PRs after Sun 17:00)
- Hard PR cutoff (Wed 17:00 — no new feature PRs)
- Hard merge cutoff (Thu 17:00 — all PRs must be merged)

**After sprint:**
- Sprint retro (60 min)
- Demo (30 min)
- Folder moves to `Agents/decisions/closed/`
- Update master `Phase1-Phase2-Completion-INDEX.md` §1 status badges

---

## Success Criteria (All Sprints)

- ✅ **Green build** (zero warnings, no security vulnerabilities)
- ✅ **All tests pass** (unit + integration coverage >80%)
- ✅ **All endpoints mapped** (22-28 per sprint, every permission attribute present)
- ✅ **All migrations applied** (clean schema, no conflicts)
- ✅ **All integration events register** (zero drift, parity test green)
- ✅ **Zero critical bugs** (security, data loss, auth bypass)
- ✅ **Performance baseline met** (ingest <50ms p95, queries <1s p95)
- ✅ **Documentation complete** (per-task WBS + acceptance gates signed off)

---

## Questions & Escalation

| Question | Owner | Channel |
|----------|-------|---------|
| Architecture decision (new aggregate, new event type) | Tech Lead | Async RFC in Slack #architecture |
| Performance concern (query slow, ingest dropped) | Mohammad (lead) | Daily standup + PR review |
| Scope creep (request for new endpoint mid-sprint) | Tech Lead | Block — defer to Phase 3 |
| Merge conflict (two tasks touch same file) | Devs + Tech Lead | Resolve via standup, explicit review |
| PCI audit finding (payment data in log) | Tech Lead | Escalate to compliance officer |

---

**Document Version:** Phase 2 Closure (2027-03-12)  
**Last Updated:** Pre-sprint (2026-06-01)  
**Next Review:** After Authorization-Cleanup closed (2027-03-12)

