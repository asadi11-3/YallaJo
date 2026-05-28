# TourGuide-Flow.md — Fix Plan

**Created**: 2025-01-27 | **Audit Score**: 6.8/10 → Target: 8.5/10  
**Estimated Effort**: 6-8 hours | **Files**: ~30 new, ~8 modified

---

## Design Decisions

1. **Domain events follow existing pattern**: `sealed record XxxDomainEvent(...) : DomainEventBase`
2. **Integration events follow module convention**: `sealed record XxxIntegrationEvent(...) : IntegrationEventBase`
3. **Domain event handlers → outbox**: Same as all other ContentTours handlers
4. **Validators use FluentValidation**: `AbstractValidator<T>` with `.NotEmpty()` for Guid IDs
5. **TourTourGuide cleanup**: Remove entity + repo + DI + EF config (migration deferred)
6. **CommissionRate**: Add as `decimal?` with `HasPrecision(5, 4)` to TourGuide entity + EF config
7. **Property name fixes in plan doc only**: Actual code names are better; update plan to match code

---

## Fixes (Priority Order)

### Fix 1 (CRITICAL): Add 6 Domain Events for GuideApplication + TourProposal

**Problem**: Zero domain events means no cross-module notification of lifecycle transitions.

**Files to create (6)**:
- `ContentTours.Domain/Events/GuideApplicationSubmittedDomainEvent.cs`
- `ContentTours.Domain/Events/GuideApplicationApprovedDomainEvent.cs`
- `ContentTours.Domain/Events/GuideApplicationRejectedDomainEvent.cs`
- `ContentTours.Domain/Events/TourProposalSubmittedDomainEvent.cs`
- `ContentTours.Domain/Events/TourProposalApprovedDomainEvent.cs`
- `ContentTours.Domain/Events/TourProposalRejectedDomainEvent.cs`

**Files to modify (2)**:
- `ContentTours.Domain/Entities/GuideApplication.cs` — Add `AddDomainEvent()` calls in Submit(), Approve(), Reject()
- `ContentTours.Domain/Entities/TourProposal.cs` — Add `AddDomainEvent()` calls in Submit(), Approve(), Reject()

**Integration event propagation (6 new files)**:
- `ContentTours.Contracts/IntegrationEvents/GuideApplicationSubmittedIntegrationEvent.cs`
- `ContentTours.Contracts/IntegrationEvents/GuideApplicationApprovedIntegrationEvent.cs`
- `ContentTours.Contracts/IntegrationEvents/GuideApplicationRejectedIntegrationEvent.cs`
- `ContentTours.Contracts/IntegrationEvents/TourProposalSubmittedIntegrationEvent.cs`
- `ContentTours.Contracts/IntegrationEvents/TourProposalApprovedIntegrationEvent.cs`
- `ContentTours.Contracts/IntegrationEvents/TourProposalRejectedIntegrationEvent.cs`

**Domain event handlers (6 new files in Infrastructure)**:
- `ContentTours.Infrastructure/EventHandlers/GuideApplicationSubmittedDomainEventHandler.cs`
- `ContentTours.Infrastructure/EventHandlers/GuideApplicationApprovedDomainEventHandler.cs`
- `ContentTours.Infrastructure/EventHandlers/GuideApplicationRejectedDomainEventHandler.cs`
- `ContentTours.Infrastructure/EventHandlers/TourProposalSubmittedDomainEventHandler.cs`
- `ContentTours.Infrastructure/EventHandlers/TourProposalApprovedDomainEventHandler.cs`
- `ContentTours.Infrastructure/EventHandlers/TourProposalRejectedDomainEventHandler.cs`

**Registry**: Add 6 entries to `IntegrationEventTypeRegistry.cs`

**Total**: 18 new files + 2 modified + 1 registry update

---

### Fix 2 (HIGH): Remove Legacy TourTourGuide

**Problem**: Plan Phase 7 says to delete after migration. Tour entity already uses GuideOfferings, not TourTourGuide.

**Files to delete (3+)**:
- `ContentTours.Domain/Entities/TourTourGuide.cs`
- `ContentTours.Domain/Repositories/ITourTourGuideRepository.cs`
- `ContentTours.Infrastructure/Repositories/TourTourGuideRepository.cs` (if exists)
- EF config file for TourTourGuide (if exists)
- DI registration removal from `DependencyInjection.cs`

**Risk**: Must verify zero references first. If any handler/query still uses TourTourGuide, those must be migrated to GuideTourOffering.

---

### Fix 3 (HIGH): Add CommissionRate to TourGuide

**Problem**: Plan specifies `decimal? CommissionRate` — missing from entity.

**Files to modify (2)**:
- `ContentTours.Domain/Entities/TourGuide.cs` — Add `public decimal? CommissionRate { get; private set; }` + setter method
- `ContentTours.Infrastructure/Configurations/TourGuideConfiguration.cs` — Add `HasPrecision(5, 4)`

**Note**: Needs EF migration (deferred).

---

### Fix 4 (MEDIUM): Add Missing Validators (11 new files)

**Problem**: 0% validator coverage for GuideApplication, TourProposal, GuideAvailabilityBlock.

**Files to create**:
- `ApplyForTourCommandValidator.cs` (GuideApplication)
- `ApproveGuideApplicationCommandValidator.cs`
- `RejectGuideApplicationCommandValidator.cs`
- `CreateTourProposalCommandValidator.cs`
- `SubmitTourProposalCommandValidator.cs`
- `ApproveTourProposalCommandValidator.cs`
- `RejectTourProposalCommandValidator.cs`
- `CreateGuideAvailabilityBlockCommandValidator.cs`
- `UpdateGuideAvailabilityBlockCommandValidator.cs`
- `DeleteGuideAvailabilityBlockCommandValidator.cs`
- `BlockGuideAvailabilityCommandValidator.cs`

All follow standard pattern: `RuleFor(x => x.Id).NotEmpty()` for IDs, domain-specific rules for text/date fields.

---

### Fix 5 (MEDIUM): Add GuideTourOffering Timestamp Fields

**Problem**: Missing `AssignedAt` and `SuspendedAt` on GuideTourOffering entity.

**Files to modify (2)**:
- `ContentTours.Domain/Entities/GuideTourOffering.cs` — Add `DateTime? AssignedAt`, `DateTime? SuspendedAt` with setters in Create()/Suspend()/Reinstate()
- `ContentTours.Infrastructure/Configurations/GuideTourOfferingConfiguration.cs` — Map new columns

---

### Fix 6 (LOW): Update Plan Document

**Problem**: Property names, endpoint counts, status all outdated.

**Changes to `TourGuide-Flow.md`**:
1. Update header status from "Ready for execution" to reflect partial implementation
2. Fix property names: QualificationSummary→Message, Name→Title, IsExclusive→RequestExclusive, ApprovedTourId→CreatedTourId, ReviewedByUserId→ReviewedByAdminId
3. Note ApplicationId is Guid? (nullable) not Guid
4. Note CommissionRate is missing (Fix 3 adds it)
5. Note TourTourGuide still exists (Fix 2 removes it)
6. Note domain events are missing (Fix 1 adds them)
7. Add Implementation Notes section listing extras not in plan (GuideUserId denormalization, ProposedScheduleJson, MaxGroupSize on GuideSchedule, ParticipantType on GuidePricingTier)

---

### Fix 7 (MEDIUM): Build Verify

Run `dotnet build YallaJo.sln --no-restore` after all code changes.

---

## Execution Order

```
Fix 1 (domain events)  ─┐
Fix 2 (TourTourGuide)  ─┤
Fix 3 (CommissionRate)  ─┼── All independent, parallelizable
Fix 4 (validators)      ─┤
Fix 5 (timestamps)      ─┘
         │
    Fix 7 (build verify)
         │
    Fix 6 (doc update)
```

---

## File Impact Summary

| Category | New | Modified | Deleted |
|---|---|---|---|
| Domain Events | 6 | 2 | 0 |
| Integration Events | 6 | 0 | 0 |
| Event Handlers | 6 | 0 | 0 |
| Validators | 11 | 0 | 0 |
| Entities | 0 | 2 | 1-3 |
| EF Configs | 0 | 1 | 0-1 |
| DI/Registry | 0 | 2 | 0 |
| Plan Doc | 0 | 1 | 0 |
| **Total** | **29** | **8** | **1-4** |

---

## Risk Assessment

| Risk | Mitigation |
|---|---|
| TourTourGuide may have active references | Verify with `find_referencing_symbols` before delete |
| Domain event handlers need correct outbox pattern | Copy existing ContentTours handler pattern exactly |
| CommissionRate migration | Deferred — nullable column, no data loss |
| Validator rules may not match handler expectations | Use existing validators as reference patterns |

---

## Deferred Items (Not In This Fix Cycle)

1. **GuideOffering endpoints** — Schedule/pricing/private tour management (8+ endpoints, 10+ commands/queries). Warrants separate plan or Phase 2 expansion.
2. **Missing admin endpoints** — PUT/DELETE /guides/admin/{id}
3. **Missing public endpoints** — GET /guides (list), GET /guides/{slug}
4. **Missing guide-side endpoints** — GET /guides/me/applications, GET /guides/{id}/tours
5. **EF migration** for CommissionRate + GuideTourOffering timestamps
6. **GuideAvailabilityBlock Result pattern** — Change Create() from throw to Result
7. **GuidePricingTier ParticipantType** — Document in plan (code has it, plan doesn't)
