# Role-System — Fix Plan

> **Date**: 2025-07-18
> **Source**: `Role-System-Audit-Report.md`
> **Total Fixes**: 3 code fixes + 1 documentation update
> **Estimated Effort**: 1-2 hours
> **Risk Level**: LOW — All fixes are additive or documentation-only

---

## Design Decisions

1. **Broad vs Granular Permissions**: The current broad group-level filtering is likely intentional for MVP simplicity. Fix 1 is a **documentation fix only** — aligning the plan with reality. If granular filtering is desired, that's a separate feature request requiring refactoring RolePermissionMapping.
2. **TourGuide UpdateSelf**: Adding UpdateSelf is LOW risk — it's additive and follows the pattern already used for Provider/Creator.
3. **Event property names**: These are documentation-only fixes. The code is correct; the plan just has wrong names.

---

## Fixes

### Fix 1 (MEDIUM): Update Permission Mapping Documentation

**Problem**: Plan Section 5 describes per-feature permission mapping (e.g., "ContentPlaces.Business.Create → Provider only") but actual implementation uses broad group+action filtering, giving all ContentManagement permissions to Provider/Creator/TourGuide.

**Action**: Update `Role-System.md` Section 5 to accurately reflect the actual broad filtering behavior.

**Files**: `Agents/Plans/Role-System.md` (1 file, documentation only)

---

### Fix 2 (LOW): Add TourGuide UpdateSelf Permission

**Problem**: Provider and Creator both have `Security.User.UpdateSelf` permission, but TourGuide doesn't. TourGuides should be able to update their own profile.

**Action**: Add `AppAction.UpdateSelf` for TourGuide in `RolePermissionMapping.cs`.

**Files**: `Security.Infrastructure/Seeding/RolePermissionMapping.cs` (1 file)

**Code Change**:
```csharp
// In TourGuide mapping, add:
|| p.Action == AppAction.UpdateSelf
```

---

### Fix 3 (LOW): Fix Plan Documentation Inaccuracies

**Problem**: 4 minor inaccuracies in the plan document.

**Action**: Correct the following in `Role-System.md`:
1. EmailVerifiedIntegrationEvent module: "Auth.Contracts (or Identity)" → "Security.Contracts"
2. CreatorApplicationApproved property: "UserId" → "ApplicantUserId"  
3. EmailVerified property: "Email" → "EmailAddress"
4. Add note about RegisterExternalAsync assigning User directly

**Files**: `Agents/Plans/Role-System.md` (1 file, documentation only)

---

### Fix 4 (LOW): Document Legacy SecurityDbInitializer

**Problem**: Plan doesn't clearly distinguish between SecurityDataSeeder (runtime, current) and SecurityDbInitializer (legacy bootstrap, only 5 roles).

**Action**: Add a note in `Role-System.md` clarifying that SecurityDbInitializer is legacy and should not be relied upon for role/permission seeding.

**Files**: `Agents/Plans/Role-System.md` (1 file, documentation only)

---

## Execution Order

```
Fix 2 (TourGuide UpdateSelf) ──┐
                                ├── Build Verify ──→ Done
Fix 1 + 3 + 4 (Doc updates) ───┘
```

All fixes are independent and parallelizable. Build verification only needed for Fix 2 (code change).

---

## File Impact Summary

| File | Fix # | Change Type |
|------|-------|-------------|
| `Security.Infrastructure/Seeding/RolePermissionMapping.cs` | 2 | Modified (1 line) |
| `Agents/Plans/Role-System.md` | 1, 3, 4 | Modified (documentation) |

**Total**: 2 files modified, 0 new files

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| TourGuide UpdateSelf breaks existing auth flow | Very Low | Low | Additive permission, no existing behavior changed |
| SecurityDataSeeder re-runs at startup | None | None | Seeder is idempotent — new claims added automatically |

---

## Follow-Up Items (Not In Scope)

1. **Per-feature permission granularity**: If the broad ContentManagement approach is too permissive, refactor RolePermissionMapping to filter by specific feature names
2. **SecurityDbInitializer cleanup**: Consider removing the legacy initializer entirely to avoid confusion
3. **Role management admin UI**: Plan mentions but doesn't detail admin endpoints for role assignment/revocation
