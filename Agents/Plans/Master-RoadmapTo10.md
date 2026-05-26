# Master Roadmap: All Plans → 10/10

> **Created**: 2025-01-27
> **Goal**: Bring all 12 audited plan documents to perfect 10/10 score
> **Current Average**: 8.0/10 (range: 6.8–9.5)
> **Estimated Total Effort**: 60–80 hours
> **Strategy**: Tackle cross-cutting concerns first, then per-module gaps

---

## Current Scorecard

| # | Plan | Current | Target | Gap | Priority |
|---|------|---------|--------|-----|----------|
| 1 | ContentCore-Workflow.md | 9.2 | 10.0 | 0.8 | LOW |
| 2 | Role-System.md | 9.5 | 10.0 | 0.5 | LOW |
| 3 | Platform-Onboarding-Workflow.md | 9.0 | 10.0 | 1.0 | LOW |
| 4 | BlogCreatorPost-Merger.md | 9.3 | 10.0 | 0.7 | LOW |
| 5 | ContentPlaces-Workflow.md | 9.5 | 10.0 | 0.5 | LOW |
| 6 | TourGuide-Flow.md | 9.0 | 10.0 | 1.0 | LOW |
| 7 | Booking-Workflow.md | 8.5 | 10.0 | 1.5 | MEDIUM |
| 8 | Analytics-Workflow.md | 6.8 | 10.0 | 3.2 | **HIGH** |
| 9 | ContentSeo-Workflow.md | 8.8 | 10.0 | 1.2 | LOW |
| 10 | Finance-Workflow.md | 7.0 | 10.0 | 3.0 | **HIGH** |
| 11 | Messaging-Workflow.md | 7.5 | 10.0 | 2.5 | **HIGH** |
| 12 | Social-Workflow.md | 7.8 | 10.0 | 2.2 | MEDIUM |

---

## What "10/10" Means

| Dimension | 10/10 Criteria |
|---|---|
| **Plan Accuracy** | Doc matches reality 100% — no stale counts, no wrong names, no missing sections |
| **Implementation Completeness** | All planned features built, no placeholder/stub services in production paths |
| **Code Quality** | Every command has validator, every public-facing query has HybridCache, no CQRS bypasses, no anonymous mutations |
| **Architecture Alignment** | Consistent CQRS, proper Result pattern, clean separation, no domain-throws-exceptions |
| **Test Coverage** | Each handler has unit tests, integration tests for endpoints |
| **Documentation** | Implementation Notes section, accurate counts, cross-references |

---

## Execution Strategy: 4 Waves

```
WAVE 1 (Cross-Cutting) → WAVE 2 (HIGH priority modules) → WAVE 3 (MEDIUM) → WAVE 4 (LOW polish)
   ↓                          ↓                                 ↓                  ↓
  HybridCache                Analytics                       Booking          Doc updates
  Validators                 Finance                         Social           Permission catalogs
  Auth gates                 Messaging                                        Numerical corrections
  Doc Implementation Notes
```

---

## WAVE 1 — Cross-Cutting Concerns (10–14 hours)

These issues affect multiple modules. Tackle once, score boost everywhere.

### W1-A: Unified Validator Strategy (3-4h)

**Affected modules**: Messaging (14 commands), Social (5), Booking (some), Analytics (some)

**Approach**:
- Add `FluentValidation` to every `ICommand<T>` with user input
- Use consistent rule patterns: `NotEmpty`, `MaxLength`, valid enum
- Wire validators via existing `ValidationBehavior<TRequest, TResponse>` MediatR pipeline (already exists in SharedKernel)

**Deliverable**: ~25 new validator files

---

### W1-B: HybridCache Adoption (3-4h)

**Affected modules**: Messaging, Social, ContentCore (some queries), Analytics

**Approach**:
- Add `MessagingCacheKeys.cs`, `SocialCacheKeys.cs` (others exist)
- Wrap high-traffic query handlers with `cache.GetOrCreateAsync(key, factory, options, tags)`
- TTLs: 15s for counters, 30s for paginated lists, 5min for stable data
- Tag-based invalidation on write handlers

**Deliverable**: ~12-15 query handler modifications + 2 new cache-key files

---

### W1-C: Auth Gate Cleanup (2-3h)

**Affected modules**: Analytics (9 anonymous routes), Social (18), some Booking

**Approach**:
- Audit every `.AllowAnonymous()` — should ONLY be on truly public reads
- Convert anonymous POSTs to `.RequireAuthorization()` + permission attribute
- For genuinely public reads (e.g., blog detail, place detail), keep anonymous but rate-limit
- Add `[RateLimit]` attribute where missing

**Deliverable**: ~30 endpoint modifications

---

### W1-D: CQRS Bypass Removal (2-3h)

**Affected modules**: Analytics (15 confirmed bypasses)

**Approach**:
- Every endpoint must go through `ISender` (MediatR) — no direct `DbContext` access in Presentation
- Refactor bypassed endpoints into proper Query/Command handlers
- Audit Presentation layer for direct service injection that should be CQRS

**Deliverable**: ~15 new query handlers + endpoint refactoring

---

### W1-E: Doc Implementation Notes Standard (1h per plan = 12h, but batched)

**Approach**:
- Every plan doc gets a final "## Implementation Notes" section
- Documents: actual file counts, route paths, property name variances, deferred items, cross-module touchpoints
- Status header: "Plan" → "Implemented (audited YYYY-MM-DD, score X.X/10)"

**Deliverable**: 12 plan doc updates

---

## WAVE 2 — HIGH Priority Modules (20–25 hours)

### W2-A: Analytics 6.8 → 10.0 (8-10h)

**Apply WAVE 1 fixes** plus module-specific:

| # | Fix | Effort |
|---|-----|--------|
| 1 | Add collaborative filtering algorithm | 3h |
| 2 | Implement blended scoring (engagement + recency + popularity) | 2h |
| 3 | Build guide dashboard endpoints (currently missing) | 2h |
| 4 | GDPR export endpoint + handler | 1h |
| 5 | Audit trail spoofing fix — every entity action records `PerformedByUserId` from `ICurrentUser` not request body | 1h |
| 6 | Add DashboardCache `EntityType` + `Granularity` columns | 1h |
| 7 | Update doc with correct counts: 51 endpoints, 28 handlers, 8 bg services | 30min |

**Score target**: 10.0

---

### W2-B: Finance 7.0 → 10.0 (6-8h)

| # | Fix | Effort |
|---|-----|--------|
| 1 | Build CreditNote entity (currently doesn't exist) | 2h |
| 2 | Build GuideEarning aggregate + earnings dashboard for TourGuide role | 3h |
| 3 | Build admin dashboard endpoints (queue, metrics, suspended payouts) | 2h |
| 4 | Complete agency commission split logic | 1h |
| 5 | Replace `FakePaymentGateway` with real provider config (Stripe/JoMoPay) | 30min infrastructure |
| 6 | Add validators for 8+ missing commands | 1h |
| 7 | Doc: update entity counts (21 not plan estimate), endpoint counts (33), correct ProviderPaymentMethodType values | 30min |

**Score target**: 10.0

---

### W2-C: Messaging 7.5 → 10.0 (6-7h)

| # | Fix | Effort |
|---|-----|--------|
| 1 | Add 14 command validators | 2h |
| 2 | Implement SMS sender (ISmsSender + NoOp + real provider stub) | 1h |
| 3 | SLA monitoring background service | 1.5h |
| 4 | Notification digest service | 1.5h |
| 5 | Add HybridCache to GetMyNotifications, GetUnreadCount, GetMyPreferences | 1h |
| 6 | Add TourGuide notification types to enum | 30min |
| 7 | Doc updates | 30min |

**Score target**: 10.0

---

## WAVE 3 — MEDIUM Priority (10–14 hours)

### W3-A: Booking 8.5 → 10.0 (4-5h)

| # | Fix | Effort |
|---|-----|--------|
| 1 | Replace 5 stub services with real implementations | 3h |
| 2 | Add TourArchivedIntegrationEventHandler + GuideOfferingSuspendedIntegrationEventHandler | 1h |
| 3 | Fix duplicate TourGuide entity in Booking.Domain (remove + retarget references) | 30min |
| 4 | Add missing endpoints documented as gaps (bulk slot create, refund admin) | 1h |
| 5 | Doc updates: route corrections, payment flow re-documentation | 30min |

**Score target**: 10.0

---

### W3-B: Social 7.8 → 10.0 (4-5h)

| # | Fix | Effort |
|---|-----|--------|
| 1 | Public review listing endpoint | 30min |
| 2 | Complete Warn/Ban workflow (3 commands + handlers + 1 integration event) | 2h |
| 3 | Add HybridCache to 4 high-traffic queries | 1h |
| 4 | Add 5 command validators | 1h |
| 5 | Wire ReviewHelpfulVote to public endpoint | 30min |
| 6 | Doc updates + permission catalog comment fix | 30min |

**Score target**: 10.0

---

### W3-C: TourGuide-Flow 9.0 → 10.0 (2-3h)

| # | Fix | Effort |
|---|-----|--------|
| 1 | Delete legacy `TourTourGuide` entity + retarget AssignTourGuide/UnassignTourGuide handlers to GuideTourOffering | 2h |
| 2 | Doc: refresh with all new endpoints from out-of-scope additions | 30min |

**Score target**: 10.0

---

## WAVE 4 — LOW Polish (6–8 hours)

### W4-A: ContentCore 9.2 → 10.0 (1-2h)

| # | Fix | Effort |
|---|-----|--------|
| 1 | Generate EF migration for: `Attachment.IsMarkedForDeletion`, `TranslationCache` HasMaxLength, `Category.ParentCategoryId` index | 1h |
| 2 | Add Category cycle detection (multi-level beyond self-parent) | 30min |
| 3 | Add OpenAPI response code annotations to endpoints missing them | 30min |

**Score target**: 10.0

---

### W4-B: ContentSeo 8.8 → 10.0 (1h)

| # | Fix | Effort |
|---|-----|--------|
| 1 | Remove 4 dead permissions from catalog | 15min |
| 2 | Doc: numerical corrections (23 endpoints, 21 handlers, 4 dead permissions) | 30min |
| 3 | Add Implementation Notes section | 15min |

**Score target**: 10.0

---

### W4-C: ContentPlaces 9.5 → 10.0 (1h)

| # | Fix | Effort |
|---|-----|--------|
| 1 | Add Business cycle detection if needed (currently flat — no parent) | 15min |
| 2 | Generate EF migration for any pending schema changes | 30min |
| 3 | Doc: minor refresh | 15min |

**Score target**: 10.0

---

### W4-D: BlogCreatorPost-Merger 9.3 → 10.0 (1h)

| # | Fix | Effort |
|---|-----|--------|
| 1 | Delete empty `Commands/Creator/Posts/` and `Queries/Creator/Posts/` folders | 5min |
| 2 | Verify zero remaining references to "CreatorPost" anywhere | 30min |
| 3 | Doc: archive plan with "Merger Complete" header | 15min |

**Score target**: 10.0

---

### W4-E: Platform-Onboarding 9.0 → 10.0 (1.5h)

| # | Fix | Effort |
|---|-----|--------|
| 1 | Generate EF migration for `ProviderApplication.Reapply()` if any new persisted fields | 30min |
| 2 | Add integration tests for reapplication flow (Rejected→Draft→Submit) | 1h |
| 3 | Doc: archive with completion status | 15min |

**Score target**: 10.0

---

### W4-F: Role-System 9.5 → 10.0 (30min)

| # | Fix | Effort |
|---|-----|--------|
| 1 | Add integration tests verifying role-permission claims for all 8 roles | 30min |
| 2 | Doc: archive with completion status | 15min |

**Score target**: 10.0

---

## Execution Order (Optimal Path)

```
WEEK 1 (cross-cutting + high-impact):
  Day 1: W1-A (validators) + W1-D (CQRS bypass) — parallel
  Day 2: W1-B (HybridCache) + W1-C (auth gates) — parallel
  Day 3: W2-A (Analytics) — full day
  Day 4: W2-B (Finance) — full day
  Day 5: W2-C (Messaging) — full day

WEEK 2 (medium + polish):
  Day 1: W3-A (Booking) + W3-B (Social) — parallel
  Day 2: W3-C (TourGuide) + W4-A (ContentCore) + W4-B (ContentSeo) — parallel
  Day 3: W4-C (ContentPlaces) + W4-D (Blog) + W4-E (Onboarding) + W4-F (Role) — parallel
  Day 4: W1-E doc batch updates for all 12 plans
  Day 5: Final build verification + integration test sweep + score recalculation
```

---

## Risk Assessment

| Risk | Mitigation |
|------|-----------|
| Cross-cutting refactors break existing tests | Run integration tests after each wave |
| HybridCache invalidation bugs cause stale data | Tag-based invalidation, conservative TTLs |
| CQRS refactors in Analytics break dashboard performance | Benchmark before/after, add caching where needed |
| Removing duplicate TourGuide in Booking.Domain causes ripple changes | Find all references first, batch rename to GuideTourOffering |
| Building CreditNote/GuideEarning requires DB migration | Plan migration order: schema → seed data → cutover |
| 12 plan doc updates become tedious | Use template + script for Implementation Notes sections |

---

## Success Metrics

After execution, verify each plan scores 10/10 across:

| Dimension | How to Verify |
|---|---|
| Plan Accuracy | Re-run audit explore agents; expect 0 mismatches |
| Implementation Completeness | Compare plan checklist against codebase; expect 100% |
| Code Quality | Run `dotnet build` (0 errors), check validator coverage, cache coverage |
| Architecture Alignment | No direct DbContext in Presentation, no domain throws, no broken Result pattern |
| Test Coverage | Each module ≥80% handler coverage |
| Documentation | Every plan has Implementation Notes, accurate counts, status header |

---

## File Impact Summary

| Wave | New Files | Modified Files | Deleted Files |
|---|---|---|---|
| W1 (Cross-Cutting) | ~30 (validators, cache keys) | ~50 (handlers, endpoints) | 0 |
| W2 (HIGH) | ~40 (new features) | ~30 | 5 (stubs replaced) |
| W3 (MEDIUM) | ~15 | ~20 | 3 (legacy code) |
| W4 (LOW) | ~5 | ~25 (doc updates) | 2 (empty folders) |
| **Total** | **~90** | **~125** | **~10** |

---

## Quick-Win Checklist (Can be done in 1 hour each)

If you want fast score boosts without full waves:

1. ✅ **Doc updates only**: All 12 plans get Implementation Notes — +0.2 each = +2.4 total
2. ✅ **Permission catalog cleanups**: ContentSeo dead permissions, Social comment fix — +0.5 to those two
3. ✅ **Empty folder cleanup**: Blog Creator/Posts folders — +0.2
4. ✅ **Plain validator addition**: 5 commands at a time = 30min batches
5. ✅ **Numerical corrections in docs**: All plans get accurate file/endpoint counts

---

## Deferred / Out-of-Scope (Acceptable for 10/10)

These items are **NOT** required for 10/10 because they're documented as deferred:

- ChatBotConversation/ChatBotMessage in Messaging (`_Deferred/`)
- PackageBooking/Reservation in Booking (`_Deferred/`)
- Discount/Loyalty/Referral/Subscription in Finance (deferred by plan)
- Background sitemap generation in ContentSeo (planned for SEO-V2)
- Real SMS provider integration (deployment-time config)

---

## Recommendation

**Start with WAVE 1-A (validators)** — fastest score boost with lowest risk. Then **WAVE 2-A (Analytics)** because it has the largest gap (3.2 points).

If time-constrained: WAVE 4 alone (6-8h) brings all 7 already-fixed modules to 10/10 and the 5 new modules to ~8.5/10 average. That's the highest score-per-hour return.
