# Analytics Module — Fix Plan

> **Created**: 2025-01-27
> **Based on**: `Analytics-Audit-Report.md`
> **Effort**: ~8-12 hours (code fixes) + ~1 hour (doc fixes)
> **Build gate**: Fix 14

---

## Design Decisions

1. Fix plan document inaccuracies FIRST (non-breaking, high-value)
2. CQRS bypass fix creates proper handlers but reuses existing inline logic
3. Auth fixes: add RequireAuthorization where missing; keep AllowAnonymous on public GETs
4. Anonymous POST decision: per plan Decision #1, sponsored-click and metrics should require auth
5. Collaborative filtering is a MAJOR feature — scope as separate work item, not this fix plan
6. DashboardCache schema unchanged for now (simple key/value works with key-encoding)

---

## Fixes

### Fix 1 (HIGH) — Update plan doc numerical claims
- **Files**: `Analytics-Workflow.md`
- **Changes**: 
  - Line 45: "47 endpoints" → "51 endpoints"
  - Line 36: "20 integration event handlers" → "28"
  - Line 37: "10 background services" → "8"
  - Add note: 9 anonymous endpoints (7 GET + 2 POST), not "3 anonymous POSTs"
  - Guide dashboard: 3 endpoints exist at /guide/* (not /dashboard/guide)
  - GDPR export: exists at /me/export (not /gdpr/export)

### Fix 2 (HIGH) — Fix 2 admin endpoints missing RequireAuthorization
- **Files**: `RecommendationsEndpoints.cs` (2 lines)
- **Changes**: Add `.RequireAuthorization()` to the 2 admin interaction endpoints that only have MustHavePermission metadata

### Fix 3 (HIGH) — Remove AllowAnonymous from 2 POSTs (Decision #1)
- **Files**: `RecommendationsEndpoints.cs`
- **Changes**: Replace `.AllowAnonymous()` with `.RequireAuthorization()` on:
  - POST /sponsored-click
  - POST /metrics

### Fix 4 (HIGH) — CQRS bypass: Extract 15 inline endpoints to Command/Query handlers
- **Files**: 15 new command/query + handler pairs + validators (~45 new files)
- **Scope**: Each of the 15 direct-repo endpoints in RecommendationsEndpoints.cs gets a proper handler
- **Pattern**: Follow existing module conventions (ICommand/IQuery, Result pattern, HybridCache)
- **Note**: This is the largest fix — do in batches

### Fix 5 (MEDIUM) — Wire UserExcludedEntity into scorers (Decision #11)
- **Files**: V1ContentSimilarityScorer.cs, any scorer that queries recommendations
- **Changes**: Add exclusion filter: if entity in UserExcludedEntity for user, skip

### Fix 6 (MEDIUM) — Fix audit trail AdminUserId derivation (Decision #14)
- **Files**: Any admin command handlers that accept AdminUserId in request
- **Changes**: Remove from request DTO, derive from ICurrentUser.UserId

### Fix 7 (LOW) — Collaborative filtering placeholder interfaces
- **Files**: 2-3 new interface files
- **Changes**: Create `ICollaborativeScoringEngine` and `IBlendedScoringEngine` interfaces + NoOp implementations for DI. Actual implementation deferred.

### Fix 8 (LOW) — DashboardCache note in plan
- **Files**: `Analytics-Workflow.md`
- **Changes**: Document that DashboardCache is key/value (not entity-typed), guide dashboard uses key encoding

---

## Execution Order

```
Fix 1 (doc) ──┐
Fix 2 (auth) ─┤
Fix 3 (auth) ─┼──→ Fix 14 (Build Verify)
Fix 4 (CQRS) ─┤
Fix 5 (scorer)┤
Fix 6 (audit) ┤
Fix 7 (iface) ┤
Fix 8 (doc) ──┘
```

Fixes 1-8 are parallelizable except Fix 4 which has internal dependencies.

---

## Risk Assessment

| Risk | Mitigation |
|---|---|
| Fix 4: 15 new handlers may break existing endpoint behavior | Each handler replicates existing inline logic verbatim |
| Fix 3: Removing anonymous POST may break external integrations | Sponsored-click likely from internal UI only; metrics endpoint verify |
| Fix 5: Exclusion filter may slow scoring | Use in-memory HashSet lookup, O(1) |

---

## NOT In Scope (Deferred)

- Full collaborative filtering engine (Decision #2) — major feature, separate work
- Nightly matrix recomputation service (Decision #3) — depends on collaborative
- Blended scoring assembly — depends on collaborative
- DashboardCache schema redesign for granularity
- New background service for guide analytics
