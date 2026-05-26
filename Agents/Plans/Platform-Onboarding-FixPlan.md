# Platform-Onboarding-Workflow — Fix Plan

> **Date**: 2025-07-18
> **Source**: `Platform-Onboarding-Audit-Report.md`
> **Total Fixes**: 4 code fixes + 4 documentation fixes
> **Estimated Effort**: 4-6 hours
> **Risk Level**: MEDIUM — Fix 1 modifies domain entity state machine

---

## Design Decisions

1. **Reapplication method**: Add a dedicated `Reapply()` method to ProviderApplication rather than extending `Submit()`. Clearer intent, guards cooling period + max attempts, transitions Rejected → Draft.
2. **Route naming**: Fix documentation to match actual routes (code is correct, plan is wrong).
3. **"Tourist" → "Guest"**: Replace all "Tourist" references in plan with "Guest" (the actual role).
4. **Missing endpoints**: Create the 3 missing endpoints. Public agency list/detail are useful for guide discovery. Dashboard notifications is useful for provider UX.
5. **Integration events**: Document the generic `ProviderStatusChangedIntegrationEvent` in the plan rather than creating dedicated events for Submitted/MoreDocsRequested — the generic approach is a valid design choice.
6. **ProviderDocumentExpiryService**: Complete the TODO — add notification publishing on document expiry.

---

## Fixes

### Fix 1 (🔴 CRITICAL): Implement Rejected → Draft Reapplication Path

**Problem**: `ProviderApplication.Submit()` only accepts Draft or MoreDocsNeeded states. Rejected providers cannot reapply despite the entity having `CoolingPeriodEndsAt`, `ReapplicationCount`, and `MaxReapplications` infrastructure already in place.

**Action**: Add `Reapply()` method to ProviderApplication entity:
```csharp
public void Reapply()
{
    if (Status != ProviderApplicationStatus.Rejected)
        throw new BusinessRuleViolationException("Only rejected applications can be reapplied.");
    
    if (ReapplicationCount >= MaxReapplications)
        throw new BusinessRuleViolationException($"Maximum re-applications ({MaxReapplications}) reached.");
    
    if (CoolingPeriodEndsAt.HasValue && DateTime.UtcNow < CoolingPeriodEndsAt.Value)
        throw new BusinessRuleViolationException("Cooling period has not ended yet.");
    
    Status = ProviderApplicationStatus.Draft;
    // Optionally raise a domain event: ProviderApplicationReappliedDomainEvent
}
```

**Files**:
- `Accounts.Domain/ProviderApplication/ProviderApplication.cs` (add method)
- `Accounts.Application/ProviderApplication/Commands/Reapply/` (new command + handler + validator)
- `Accounts.Presentation/Endpoints/ProviderEndpoints.cs` (new POST /api/v1/provider/reapply endpoint)

**Risk**: MEDIUM — Modifies domain entity behavior. Must verify tests pass.

---

### Fix 2 (❌ HIGH): Replace "Tourist" with "Guest" Throughout Plan

**Problem**: Plan uses "Tourist" as the default role name (14+ occurrences across Sections 2, 3, 4, 6). No "Tourist" role exists in `AppRoles.cs`. The actual default is "Guest".

**Action**: Find-and-replace "Tourist" → "Guest" throughout `Platform-Onboarding-Workflow.md`, adjusting surrounding text for clarity (e.g., "Tourist onboarding" → "Guest onboarding", "Tourist → Provider upgrade" → "Guest → Provider upgrade").

**Files**: `Agents/Plans/Platform-Onboarding-Workflow.md` (1 file, documentation only)

---

### Fix 3 (❌ MEDIUM): Add 3 Missing Endpoints

**Problem**: 3 endpoints described in the plan do not exist in code.

#### 3a: Public Agency Endpoints (2 endpoints)
```
GET /api/v1/agencies         → List agencies (public, paginated)
GET /api/v1/agencies/{id}    → Get agency detail (public)
```

**Files**:
- `Accounts.Application/Agency/Queries/GetAgencies/` (new query + handler)
- `Accounts.Application/Agency/Queries/GetAgencyById/` (new query + handler)
- `Accounts.Presentation/Endpoints/AgencyPublicEndpoints.cs` (new endpoint group)

#### 3b: Dashboard Notifications Endpoint (1 endpoint)
```
GET /api/v1/provider/dashboard/notifications → Provider notifications
```

**Files**:
- `Accounts.Application/ProviderDashboard/Queries/GetProviderNotifications/` (new query + handler)
- `Accounts.Presentation/Endpoints/ProviderDashboardEndpoints.cs` (add endpoint)

**Risk**: LOW — New endpoints, no existing code modified.

---

### Fix 4 (❌ MEDIUM): Update All Route Paths in Plan

**Problem**: 13 endpoint routes in the plan use wrong prefixes. Plan says `/api/v1/provider/agency/...` but actual code uses `/api/v1/agency/...`. Plan says `/api/v1/guides/agency-invitations/...` but actual code uses `/api/v1/guides/me/invitations/...`.

**Action**: Update all endpoint tables in Sections 7 and 11 of `Platform-Onboarding-Workflow.md` to match actual routes.

**Key corrections**:
| Plan Route Prefix | Actual Route Prefix |
|------------------|-------------------|
| `/api/v1/provider/agency/` | `/api/v1/agency/` |
| `/api/v1/guides/agency-invitations/` | `/api/v1/guides/me/invitations/` or `/api/v1/guides/invitations/` |
| `POST /api/v1/guides/apply-to-agency` | `POST /api/v1/guides/agency-applications` |
| `POST /api/v1/guides/leave-agency` | `DELETE /api/v1/guides/me/agency` |

**Files**: `Agents/Plans/Platform-Onboarding-Workflow.md` (1 file, documentation only)

---

### Fix 5 (⚠️ LOW): Update Integration Events Section in Plan

**Problem**: Plan claims `ProviderApplicationSubmittedIntegrationEvent` and `ProviderMoreDocsRequestedIntegrationEvent` exist as separate contracts. They don't — these transitions use the generic `ProviderStatusChangedIntegrationEvent`.

**Action**: Update Section 12 of the plan:
1. Remove the 2 non-existent dedicated events
2. Add `ProviderStatusChangedIntegrationEvent` (generic, published for ALL transitions)
3. Add note that `AccountsIntegrationConverters` publishes BOTH generic + dedicated events for Approved/Suspended/Rejected/Reinstated
4. Add `ProviderRegisteredDomainEvent` (exists but not mentioned)

**Files**: `Agents/Plans/Platform-Onboarding-Workflow.md` (1 file, documentation only)

---

### Fix 6 (⚠️ LOW): Fix Property Names and Module References

**Problem**: Several property/module names in the plan don't match actual code.

| Location in Plan | Plan Says | Actual |
|-----------------|-----------|--------|
| AgencyAffiliation entity | `TerminatedBy` | `TerminatedByUserId` |
| CreatorApplicationApproved event | `UserId` | `ApplicantUserId` |
| EmailVerifiedIntegrationEvent | "Auth.Contracts" | Security.Contracts |

**Files**: `Agents/Plans/Platform-Onboarding-Workflow.md` (1 file, documentation only)

---

### Fix 7 (⚠️ LOW): Complete ProviderDocumentExpiryService

**Problem**: Service exists and is registered but has TODOs in the implementation — notification publishing on document expiry is incomplete.

**Action**: Complete the notification publishing logic in the background service.

**Files**: `Accounts.Infrastructure/BackgroundServices/ProviderDocumentExpiryService.cs` (1 file)

**Risk**: LOW — Additive, no existing behavior changed.

---

### Fix 8 (⚠️ LOW): Document Additional Findings in Plan

**Problem**: Plan omits some features that exist in code.

**Items to add**:
1. `AgencyInvitation.IsExpired` property (computed, useful for queries)
2. `RegisterExternalAsync()` bypasses Guest → User lifecycle (assigns User directly)
3. `ProviderApplicationStatus` has no "Reinstated" value — `Reinstate()` returns to Approved
4. `AccountsEndpoints.cs` mounts 5 route groups (not described in plan)

**Files**: `Agents/Plans/Platform-Onboarding-Workflow.md` (1 file, documentation only)

---

## Execution Order

```
Fix 1 (CRITICAL) ──┐
Fix 3 (MEDIUM)  ───┤──→ Build Verify ──→ Fix 7 (LOW)
                    │
Fix 2 (HIGH)    ───┤
Fix 4 (MEDIUM)  ───┤──→ (documentation, no build needed)
Fix 5 (LOW)     ───┤
Fix 6 (LOW)     ───┤
Fix 8 (LOW)     ───┘
```

- Fixes 2, 4, 5, 6, 8 are all documentation-only (modify the plan document)
- Fix 1 (code) + Fix 3 (code) + Fix 7 (code) require build verification
- Fix 1 should be done first (highest risk, blocks confidence in state machine)

---

## File Impact Summary

| Type | Files |
|------|-------|
| Domain entity modified | 1 (`ProviderApplication.cs`) |
| New command/handler/validator | 3-4 sets (Reapply + 2-3 query sets) |
| New endpoint groups | 1-2 |
| Existing endpoints modified | 1-2 |
| Background service modified | 1 |
| Plan document updated | 1 (extensive) |
| **Total** | **~12-15 files** |

---

## Risk Assessment

| Risk | Mitigation |
|------|-----------|
| Reapply() may conflict with existing test expectations | Run full ProviderApplication test suite after change |
| New public agency endpoints may expose sensitive data | Use DTOs with public-safe fields only, AllowAnonymous or read-only |
| Route changes in docs may confuse developers using old plan | Add "Updated on" header to plan + changelog note |
| Generic StatusChanged event design may not be sufficient | Document that consumers can filter by Status field in the event payload |

---

## Follow-Up Items (Not in Scope)

1. **Category cycle detection** in ProviderApplication document requirements (if any category-based requirements emerge)
2. **OpenAPI response annotations** on all new endpoints
3. **ProviderStatusChangedIntegrationEvent consumers** — verify all modules that need status awareness are subscribed
4. **External OAuth registration** — plan doesn't cover OAuth provider flow (exists in code as RegisterExternalAsync)
5. **Agency roster capacity limits** — no max-members-per-agency constraint visible
