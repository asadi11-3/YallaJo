# YallaJo Cross-Document Analysis Report

> **Date**: 2025-07-14
> **Scope**: Full cross-reference audit of all 4 source-of-truth documents + 9 module gap analyses
> **Documents Audited**:
> 1. `agent-context.md` — Architecture rules, gotchas, non-negotiable constraints
> 2. `YallaJo.md` — 196 endpoints, middleware pipeline, 18 background services, 3 SignalR hubs
> 3. `YallaJo Business Rules & Edge Cases.pdf` — 63 pages, 24 sections of domain rules
> 4. `Endpoints.pdf` — 39 pages, Wave 1-6 sequenced development plan
> 5. 9 individual module gap analyses (ContentSeo through Finance)

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Module Health Scoreboard](#2-module-health-scoreboard)
3. [Critical Contradictions Between Documents](#3-critical-contradictions-between-documents)
4. [Redundant Processes](#4-redundant-processes)
5. [Crossing Logic Failures](#5-crossing-logic-failures)
6. [Ambiguities Requiring Product Decision](#6-ambiguities-requiring-product-decision)
7. [Systemic Cross-Module Issues](#7-systemic-cross-module-issues)
8. [Per-Module Gap Summary](#8-per-module-gap-summary)
9. [Universally Clean Areas](#9-universally-clean-areas)
10. [Recommended Resolution Order](#10-recommended-resolution-order)

---

## 1. Executive Summary

This report consolidates findings from auditing all 4 YallaJo specification documents against each other and against 9 individual module gap analyses. The audit uncovered:

| Category | Count | Severity |
|----------|-------|----------|
| Critical Contradictions | 6 | CRITICAL — must resolve before coding |
| Redundant Processes | 4 | HIGH — architectural debt if ignored |
| Crossing Logic Failures | 4 | HIGH — production bugs guaranteed |
| Ambiguities | 1 | MEDIUM — needs product decision |
| Systemic Cross-Module Issues | 3 | HIGH — affect 7+ modules each |

**Bottom line**: The 6 contradictions are blocking — two developers reading different documents will build incompatible systems. The 4 crossing logic failures will produce production bugs where module boundaries create gaps in business rule enforcement. The 4 redundant processes represent missed DRY opportunities that will compound maintenance cost.

---

## 2. Module Health Scoreboard

| Rank | Module | Score | Files | Endpoints | Critical Gaps |
|------|--------|-------|-------|-----------|---------------|
| 1 | ContentSeo | 9.0/10 | 153 | 17 | 7 dead permissions, MaxHops mismatch |
| 2 | ContentCore | 8.5/10 | 303 | 48 | 4 infrastructure `throw new` |
| 3 | ContentBlogs | 7.5/10 | 444 | 62 | 3 dead permissions, reaction divergence |
| 4 | ContentTours | 7.0/10 | 338 | 45 | No provider guard, validator mismatches |
| 5 | Messaging | 7.0/10 | 202 | 22+hub | Zero validators, SignalR bug, zero cache |
| 6 | Social | 7.0/10 | 166 | 19 | No public reviews, zero cache |
| 7 | Analytics | 6.5/10 | 240 | 47 | 15 non-CQRS endpoints, 3 anonymous POSTs |
| 8 | ContentPlaces | 6.0/10 | 258 | 36 | Dual state machine gap, 9 missing rules |
| 9 | Finance | 5.5/10 | 190 | 19 | 6 bare entities, commission model divergence |

---

## 3. Critical Contradictions Between Documents

These are cases where two authoritative documents specify **different behavior** for the same feature. Each must be resolved with a product decision before implementation.

---

### CONTRADICTION-1: Commission Model

| Aspect | Business Rules PDF (§5.3) | Endpoints.pdf / YallaJo.md |
|--------|---------------------------|----------------------------|
| Model | **Subscription-tier based** | **Revenue-tier based** |
| Free tier | 15% commission | ≤10K JOD → 15% |
| Mid tier | Basic=10%, Premium=7% | 10K–50K JOD → 12% |
| Top tier | Enterprise=Custom | >50K JOD → 10% |

**Impact**: These are fundamentally different business models. Subscription-tier means providers pay a subscription fee to get lower commission. Revenue-tier means commission drops automatically as revenue grows. They create different incentive structures and different Finance module implementations.

**Resolution Required**: Product owner must choose ONE model. The Finance module's `CommissionCalculationService` currently implements revenue-tiers.

**Affected Modules**: Finance (primary), Analytics (reporting), ContentPlaces (provider dashboard)

---

### CONTRADICTION-2: Review Eligibility

| Aspect | Business Rules PDF (§6.2) | Endpoints.pdf |
|--------|---------------------------|---------------|
| Who can review | **Anyone** (no booking required) | **Only users with Completed booking** |
| Verification | "Verified" badge for booked users | Verified booking is a hard requirement |

**Impact**: The PDF model allows broader review coverage but risks fake reviews. The Endpoints model ensures authenticity but limits review volume. These require completely different authorization logic in the Social module.

**Resolution Required**: Product decision needed. Most travel platforms use the Endpoints.pdf approach (booking-verified reviews only).

**Affected Modules**: Social (primary), ContentTours (tour detail page), Analytics (review metrics)

---

### CONTRADICTION-3: Rating Scale

| Aspect | Business Rules PDF | Endpoints.pdf |
|--------|-------------------|---------------|
| Scale | **1.0 – 5.0** | **1 – 5** |
| Granularity | **0.5 increments** (1.0, 1.5, 2.0 ... 5.0) | **Integer only** (1, 2, 3, 4, 5) |

**Impact**: 0.5 increments give 9 possible values; integers give 5. This affects database column type (`decimal` vs `int`), validation rules, UI star components, and average calculations.

**Resolution Required**: Choose one. Half-star ratings are more common in travel/hospitality (TripAdvisor, Google Maps).

**Affected Modules**: Social (rating storage), ContentTours (display), Analytics (aggregation), Recommendations (scoring)

---

### CONTRADICTION-4: Report Auto-Hide Threshold

| Aspect | Business Rules PDF | Endpoints.pdf |
|--------|-------------------|---------------|
| Auto-hide after | **5 reports** | **3 reports** |

**Impact**: Lower threshold (3) means more aggressive content moderation. Higher threshold (5) means more tolerance but slower response to abusive content.

**Resolution Required**: Product/legal decision. Consider starting with 3 (Endpoints.pdf) for safety, with admin override capability.

**Affected Modules**: Social (review moderation), ContentBlogs (comment moderation)

---

### CONTRADICTION-5: Provider Type Taxonomy

| Business Rules PDF | Endpoints.pdf |
|-------------------|---------------|
| Independent Tour Guide | IndependentGuide |
| Business Owner | *(no equivalent)* |
| Agency | TourOperator |
| Freelance Activity Instructor | *(no equivalent)* |
| *(no equivalent)* | HotelResort |
| *(no equivalent)* | ActivityCenter |

**Impact**: This is not just a naming difference — the **concepts** are different. The PDF envisions individual-focused provider types (freelance instructors, business owners). The Endpoints envision facility-focused types (hotels, activity centers). These create different onboarding flows, document requirements, and business validation rules.

**Resolution Required**: Unified taxonomy needed. The codebase currently has an enum — it must match the chosen taxonomy.

**Affected Modules**: Accounts (registration), ContentPlaces (business creation), ContentTours (tour creation), Finance (tier rules)

---

### CONTRADICTION-6: Default Refund Policy

| Aspect | Business Rules PDF | Endpoints.pdf |
|--------|-------------------|---------------|
| Policy | **72h before → full refund, 24h before → 50%** | **Up to 24h before → full refund, after → 0%** |
| Tiers | Two-tier (72h/24h) | One-tier (24h cutoff) |

**Impact**: The PDF policy is more consumer-friendly (partial refunds). The Endpoints policy is simpler but harsher. The refund calculation service must implement one or the other.

**Resolution Required**: Product/legal decision. The Finance module's `RefundCalculationService` needs a definitive specification.

**Affected Modules**: Finance (primary), Messaging (refund notifications), Analytics (refund reporting)

---

## 4. Redundant Processes

These are patterns reimplemented independently across modules that should share a common abstraction.

---

### REDUNDANCY-1: Three Parallel Admin-Review State Machines

| State Machine | Module | States |
|--------------|--------|--------|
| Provider Application | Accounts / ContentPlaces | Submitted → UnderReview → Approved/Rejected → Resubmitted |
| Tour Approval | ContentTours | Submitted → UnderReview → Approved/Rejected → Resubmitted |
| Discount Approval | Finance | Submitted → UnderReview → Approved/Rejected |

**Problem**: All three follow the identical pattern: entity submitted → admin reviews → approve/reject with reason → optional resubmission. Each reimplements:
- State transition validation
- Admin assignment logic
- Rejection reason tracking
- Resubmission flow
- Notification triggers on state change

**Recommendation**: Extract a generic `AdminReviewStateMachine<TEntity>` into SharedKernel with:
- Configurable states and transitions
- Built-in audit trail
- Standard integration events (`EntityApproved`, `EntityRejected`)
- Resubmission limit configuration

**Estimated deduplication**: ~40-60 lines of domain logic per module × 3 modules.

---

### REDUNDANCY-2: Profanity Filter — No SharedKernel Service

**Used in**:
- Social module (review content filtering)
- ContentBlogs module (comment content filtering)
- Messaging module (chatbot input sanitization)

**Problem**: Each module either implements its own text sanitization or will need to. There is no `IProfanityFilter` interface in SharedKernel.

**Recommendation**: Create `SharedKernel.Services.IProfanityFilter` with:
- Multi-language support (Arabic + English minimum)
- Configurable severity levels (block vs. flag for review)
- Integration with content moderation domain events

---

### REDUNDANCY-3: Blog Comments ≈ Review Threads

| Feature | Blog Comments (ContentBlogs) | Reviews (Social) |
|---------|------------------------------|-------------------|
| Content submission | ✓ | ✓ |
| Moderation (approve/reject) | ✓ | ✓ |
| Report/flag | ✓ | ✓ |
| Profanity filter | ✓ | ✓ |
| Reply threading | ✓ | ✓ |
| Reactions/likes | ✓ | ✓ |
| Author notification | ✓ | ✓ |

**Problem**: These are functionally near-identical UGC (User-Generated Content) systems that share zero code. Adding a feature to one (e.g., edit window, spam detection) must be duplicated in the other.

**Recommendation**: Extract a generic `UserContent` domain primitive with moderation, threading, and reaction behaviors. Blog comments and reviews become thin wrappers adding context-specific rules (reviews add rating; comments add blog-post context).

---

### REDUNDANCY-4: EntityImage Handling Across 5 Modules

**Modules with image handling**:
1. ContentPlaces — business photos
2. ContentTours — tour photos
3. ContentBlogs — blog post images
4. Social — review photos
5. Accounts — profile/document images

**Problem**: Each module manages image upload, storage, ordering, alt-text, and deletion independently. Image validation rules (size limits, format, dimensions) are duplicated or inconsistent.

**Recommendation**: A shared `EntityImage` value object exists in some modules but lacks unified:
- Upload validation pipeline (max size, allowed formats, dimension constraints)
- Ordering/reordering logic
- CDN URL generation
- Thumbnail generation triggers

---

## 5. Crossing Logic Failures

These are business processes that span multiple modules where the handoff between modules is broken or missing.

---

### CROSSING-1: Document Expiry → Business Suspension (UNLINKED)

**Pipeline**: Accounts (document management) → ContentPlaces (business status)

**Expected flow**:
1. Provider uploads documents during registration (Accounts module)
2. Documents have expiry dates (business license, insurance, etc.)
3. When documents expire → Business should be suspended in ContentPlaces
4. Provider uploads new documents → Business reactivated

**Current state**: The Accounts module tracks document expiry, but there is **zero integration event** connecting expired documents to ContentPlaces business suspension. A provider with expired insurance can continue operating.

**Fix**: `Accounts` should publish `ProviderDocumentExpired` integration event. `ContentPlaces` should subscribe and trigger business suspension with reason "Expired documents".

**Severity**: HIGH — legal/liability risk.

---

### CROSSING-2: Provider→Tour→Booking Pipeline — Missing Guards

**Pipeline**: Accounts (provider application) → ContentPlaces (create business) → ContentTours (create tour) → Booking

**Expected flow**:
1. Provider submits application → gets Approved
2. Provider creates Business → links to approved application
3. Provider creates Tour → links to active Business
4. Tourist books Tour → only if Tour is Published

**Current state**:
- `CreateBusiness` has **no guard** checking that the provider's application is Approved
- `CreateTour` has **no guard** checking that the provider's application is Approved
- A provider with a Rejected or Pending application could potentially create businesses and tours

**Fix**: Both `CreateBusinessCommandHandler` and `CreateTourCommandHandler` must query the Accounts module (via integration event or read model) to verify `ProviderApplication.Status == Approved`.

**Severity**: HIGH — unauthorized providers could list tours.

---

### CROSSING-3: Discount→Wishlist→Notification Pipeline (UNIMPLEMENTED)

**Pipeline**: Finance (discount created) → Social (wishlisted tours) → Messaging (push notification)

**Specified behavior** (from Business Rules PDF):
1. Provider creates a discount on a tour (Finance module)
2. System checks which tourists have wishlisted that tour (Social module)
3. System sends push notification to those tourists (Messaging module)

**Current state**: This pipeline is fully specified in the Business Rules PDF but has **zero implementation**. No integration events connect these three modules for this flow.

**Fix**:
1. Finance publishes `DiscountCreatedForTour { TourId, DiscountPercentage, ValidUntil }`
2. Social subscribes, queries wishlists by TourId, publishes `NotifyWishlistUsers { UserIds[], TourId, DiscountInfo }`
3. Messaging subscribes and sends push notifications

**Severity**: HIGH — specified feature completely missing.

---

### CROSSING-4: Provider Suspension Cascade

**Pipeline**: Accounts/ContentPlaces (suspension) → ContentTours, Finance, Social, Messaging (cascade)

**Expected behavior when a provider is suspended**:
1. All active tours should be unpublished/hidden
2. Pending bookings should be flagged for review/cancellation
3. Payouts should be held
4. Provider's responses to reviews should be hidden
5. Active chat sessions should show "provider unavailable"

**Current state**: Suspension in one module does **not cascade** to dependent modules. There are no integration events for `ProviderSuspended` that other modules subscribe to.

**Fix**: Publish `ProviderSuspended { ProviderId, Reason, SuspendedUntil }` integration event. Each downstream module subscribes:
- ContentTours: unpublish all tours
- Finance: hold payouts
- Social: hide provider responses
- Messaging: mark conversations inactive

**Severity**: HIGH — suspended providers remain fully operational in other modules.

---

## 6. Ambiguities Requiring Product Decision

### AMBIGUITY-1: Two Rating Formulas — Same or Different?

**Business Rules PDF §18 (Rating Algorithm)**:
> Bayesian Rating = (C × m + Σ ratings) / (C + n)
> Where C = minimum votes threshold, m = global average

**Business Rules PDF §6.2 (Reviews)**:
> Weighted Average considering recency, verified status, and review length

**Question**: Are these the **same calculation** used in different contexts (Bayesian for search ranking, Weighted for display)? Or are they **two independent systems**?

**Impact**: If they're different, the system needs two rating columns per tour — one for display, one for search ranking. If they're the same, one formula must be chosen.

**Recommendation**: Use Bayesian for search ranking (Analytics/Recommendations) and simple weighted average for display (Social/ContentTours). Document this explicitly.

---

## 7. Systemic Cross-Module Issues

These are architectural patterns that violate the project's own rules (`agent-context.md`) across multiple modules.

---

### SYSTEMIC-1: ICurrentUser Redundant Auth Checks (7/9 modules)

**Rule violated**: `agent-context.md` states that `ICurrentUser` is populated by middleware after authentication — handlers should not re-check `IsAuthenticated` or null-check `UserId`.

**Violation counts by module**:

| Module | Redundant Checks | Admin Bypass Violations |
|--------|-----------------|------------------------|
| ContentTours | 22 | — |
| Messaging | 21 | — |
| Social | 18 | — |
| ContentBlogs | 18 | 2 |
| ContentPlaces | 12 | 9 |
| Finance | 11 | — |
| ContentCore | 8 | — |
| **ContentSeo** | **0** | **0** |
| **Analytics** | **0** | **0** |

**Total**: ~110 redundant checks + 11 admin bypass violations.

**Admin bypass pattern**: `if (AppRoles.HighestPrivilegeLevel >= Admin)` — this circumvents proper policy-based authorization.

**Fix**: Remove all `IsAuthenticated` / `UserId is null` checks from handlers. Replace admin bypass patterns with proper `[Authorize(Policy = "...")]` attributes.

---

### SYSTEMIC-2: Zero HybridCache in 3 Modules

| Module | HybridCache Usage | Expected Cache Targets |
|--------|--------------------|----------------------|
| Finance | Zero | Subscription plans, commission tiers, exchange rates |
| Messaging | Zero | Notification templates, unread counts |
| Social | Zero | Popular reviews, rating aggregates |

**Impact**: These modules will hit the database on every request for data that changes infrequently.

---

### SYSTEMIC-3: Missing FluentValidation Coverage

| Module | Validators Present | Commands Without Validation |
|--------|-------------------|----------------------------|
| Messaging | 0 | 14 commands — **zero validators for entire module** |
| Finance | 3 | 5 commands unvalidated |
| Analytics | 3 | ~21 paths unvalidated |

**Fix**: Every command must have a corresponding FluentValidation validator per `agent-context.md` rules.

---

## 8. Per-Module Gap Summary

### ContentSeo (9.0/10)
- 7 dead permissions registered but unused
- `MaxHops=10` in crawler vs specification says `MaxHops=3`
- Otherwise exemplary — zero ICurrentUser violations, clean CQRS

### ContentCore (8.5/10)
- 4 infrastructure services use `throw new` instead of Result pattern: `AzureTranslateService`, `LocalFileStorageService`, and 2 others
- This violates `agent-context.md` non-negotiable rule: "Zero `throw new` in Application layer"

### ContentBlogs (7.5/10)
- 3 dead permissions: `HidePost`, `UnhidePost`, `Manage`
- Reaction types in code diverge from specification
- 18 redundant ICurrentUser checks + 2 admin guard bypasses

### ContentTours (7.0/10)
- **No ProviderApplication guard** on CreateTour (see CROSSING-2)
- Validator spec mismatches:
  - `MaxGroupSize`: code allows 500, spec says 100
  - `Duration`: code allows 43200 min (30 days), spec says 2880 min (48h)
  - `BasePrice`: code allows `≥ 0` (free tours), spec says `> 0`
- Missing: unique tour name per provider, max 50 active tours cap, archive endpoint
- Search uses `LIKE` instead of full-text search

### Messaging (7.0/10)
- **Zero FluentValidation validators** for 14 commands
- `IHubContext<Hub>` instead of `IHubContext<NotificationHub>` — SignalR routing bug
- 6 `DateTime.UtcNow` violations (should use `TimeProvider`)
- No SLA breach monitoring
- Bare `ChatBot` entities (schema only, zero domain logic)
- Zero HybridCache

### Social (7.0/10)
- **No public review listing endpoint** — core product feature blocked
- Zero HybridCache
- Dead `AccessibilityReview` entity (schema only)
- Dead `Warn`/`Ban` permissions
- Missing discount-to-wishlist notification pipeline

### Analytics (6.5/10)
- **15 endpoints bypass CQRS/MediatR** — direct repository injection in Presentation layer
- **3 anonymous POST endpoints** — spam/click fraud risk
- 15 admin endpoints reuse generic permissions (no granular admin authorization)
- Audit log `AdminUserId` can be spoofed (self-reported, not derived from token)
- Missing collaborative filtering (spec says 40% weight in recommendations)
- Diversity rule mismatch: code uses `MaxPerPlaceId`, spec says `MaxPerProviderId`

### ContentPlaces (6.0/10)
- **Dual state machine gap**: Business status vs ProviderApplication with zero cross-module linkage
- 9 missing business rules:
  1. `MoreDocsNeeded` transition
  2. Resubmission limit
  3. SLA timer for admin review
  4. Document expiry tracking
  5. `LicenseNumber` uniqueness
  6. Business search endpoint
  7. Max active businesses cap
  8. Place name + country uniqueness
  9. Business hours validation
- 12 redundant ICurrentUser checks + 9 admin bypass violations

### Finance (5.5/10)
- **6 bare entities** with zero domain logic:
  - `Discount`, `Subscription`, `Loyalty`, `Referral`, `Dispute`, `SubscriptionPlan`
- Commission model divergence (see CONTRADICTION-1)
- `SaveChanges` called inside `DownloadInvoice` query handler (violates CQRS)
- Zero HybridCache
- 7 dead permissions
- Only 3/8 commands have validators

---

## 9. Universally Clean Areas

Across all 9 modules (315+ endpoints), the following are **consistently correct**:

| Area | Status |
|------|--------|
| Endpoint authorization | 100% — every endpoint has `[Authorize]` |
| Application layer exceptions | Zero `throw new` (except ContentCore infrastructure) |
| Domain event handlers | Zero `SaveChanges` calls |
| DateTime usage | Zero `DateTime.Now` / `.Today` in App+Domain (except Messaging's 6) |
| Guid generation | Near-zero `Guid.NewGuid()` violations |
| Outbox/Inbox pattern | Compliant across all modules |
| Cross-module contracts | Clean integration event design |
| MediatR pipeline | Correct behavior chain in all modules |

---

## 10. Recommended Resolution Order

### Phase 1: Resolve Contradictions (BLOCKING — before any new coding)
| # | Item | Owner | Effort |
|---|------|-------|--------|
| 1 | CONTRADICTION-1: Commission model | Product + Finance dev | Decision only |
| 2 | CONTRADICTION-2: Review eligibility | Product + Social dev | Decision only |
| 3 | CONTRADICTION-3: Rating scale | Product + Social dev | Decision only |
| 4 | CONTRADICTION-4: Report threshold | Product + Social dev | Decision only |
| 5 | CONTRADICTION-5: Provider taxonomy | Product + Accounts dev | Decision + enum update |
| 6 | CONTRADICTION-6: Refund policy | Product/Legal + Finance dev | Decision only |

### Phase 2: Fix Crossing Logic (HIGH — production bug prevention)
| # | Item | Modules | Effort |
|---|------|---------|--------|
| 7 | CROSSING-2: Provider guards | ContentPlaces, ContentTours | 1-2 days |
| 8 | CROSSING-1: Doc expiry → suspension | Accounts, ContentPlaces | 2-3 days |
| 9 | CROSSING-4: Suspension cascade | 5 modules | 3-4 days |
| 10 | CROSSING-3: Discount→Wishlist→Notify | Finance, Social, Messaging | 2-3 days |

### Phase 3: Fix Systemic Issues (HIGH — code quality)
| # | Item | Scope | Effort |
|---|------|-------|--------|
| 11 | Remove 110 ICurrentUser checks | 7 modules | 2-3 days |
| 12 | Add missing validators | Messaging (14), Finance (5), Analytics (21) | 3-4 days |
| 13 | Add HybridCache | Finance, Messaging, Social | 2-3 days |
| 14 | Fix Analytics CQRS bypass | Analytics (15 endpoints) | 3-4 days |
| 15 | Fix Analytics anonymous POSTs | Analytics (3 endpoints) | 1 day |

### Phase 4: Extract Shared Abstractions (MEDIUM — reduce debt)
| # | Item | Effort |
|---|------|--------|
| 16 | REDUNDANCY-1: Generic AdminReviewStateMachine | 3-4 days |
| 17 | REDUNDANCY-2: SharedKernel profanity filter | 1-2 days |
| 18 | REDUNDANCY-4: Unified EntityImage pipeline | 2-3 days |
| 19 | REDUNDANCY-3: Generic UGC base (long-term) | 4-5 days |

### Phase 5: Module-Specific Fixes (MEDIUM — per module backlogs)
| # | Item | Module | Effort |
|---|------|--------|--------|
| 20 | Fix SignalR hub type | Messaging | 30 min |
| 21 | Fix 6 DateTime.UtcNow → TimeProvider | Messaging | 1 hour |
| 22 | Fix SaveChanges in DownloadInvoice | Finance | 30 min |
| 23 | Implement 6 bare Finance entities | Finance | 5-7 days |
| 24 | Add public review listing endpoint | Social | 1 day |
| 25 | Fix validator spec mismatches | ContentTours | 1 day |
| 26 | Remove dead permissions | ContentSeo (7), ContentBlogs (3), Finance (7) | 1 day |
| 27 | Fix MaxHops 10→3 | ContentSeo | 30 min |
| 28 | Fix ContentCore infrastructure throws | ContentCore | 1 day |
| 29 | Add 9 missing business rules | ContentPlaces | 4-5 days |
| 30 | Fix AdminUserId spoofing | Analytics | 1 day |

---

## Appendix: Finding Index

| ID | Category | Severity | Section |
|----|----------|----------|---------|
| CONTRADICTION-1 | Contradiction | CRITICAL | §3 |
| CONTRADICTION-2 | Contradiction | CRITICAL | §3 |
| CONTRADICTION-3 | Contradiction | CRITICAL | §3 |
| CONTRADICTION-4 | Contradiction | CRITICAL | §3 |
| CONTRADICTION-5 | Contradiction | CRITICAL | §3 |
| CONTRADICTION-6 | Contradiction | CRITICAL | §3 |
| REDUNDANCY-1 | Redundancy | HIGH | §4 |
| REDUNDANCY-2 | Redundancy | HIGH | §4 |
| REDUNDANCY-3 | Redundancy | MEDIUM | §4 |
| REDUNDANCY-4 | Redundancy | MEDIUM | §4 |
| CROSSING-1 | Crossing Logic | HIGH | §5 |
| CROSSING-2 | Crossing Logic | HIGH | §5 |
| CROSSING-3 | Crossing Logic | HIGH | §5 |
| CROSSING-4 | Crossing Logic | HIGH | §5 |
| AMBIGUITY-1 | Ambiguity | MEDIUM | §6 |
| SYSTEMIC-1 | Systemic | HIGH | §7 |
| SYSTEMIC-2 | Systemic | HIGH | §7 |
| SYSTEMIC-3 | Systemic | HIGH | §7 |

---

*Report generated from cross-referencing all 4 source-of-truth documents against 9 module gap analyses. Total coverage: 315+ endpoints, 2,294 source files, 9 bounded contexts.*
