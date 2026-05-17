# Permission Coverage Gaps — Auth-Cleanup PW-2

**Date:** 2027-02-28
**Scope:** All `RequireAuthorization()` violations from `endpoint-violations-2027-02-28.csv` mapped to existing or proposed permission catalog entries.

## Summary

| Category | Files | Count |
|---|---|---|
| STRING_POLICY (`.RequireAuthorization("Admin")`) | 1 | 5 |
| AUTH_AND_PERM_MIX (paired with MustHavePermission — redundant) | ~18 | ~90 |
| AUTH_ONLY (no permission attached) | ~5 | ~7 |
| **TOTAL** | **24** | **102** |

## Resolution Strategy

### STRING_POLICY — BusinessEndpoints.cs (5 occurrences)
| Line | Endpoint | Replace with |
|---|---|---|
| 151 | PATCH /business/{id} | `MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.UpdateAny)` |
| 187 | POST /business/{id}/approve | `MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Approve)` |
| 205 | POST /business/{id}/reject | `MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Reject)` |
| 223 | POST /business/{id}/suspend | `MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Suspend)` |
| 239 | POST /business/{id}/reinstate | `MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Reinstate)` |

**Required AppAction verbs:** Approve, Reject, Suspend, Reinstate, UpdateAny — ALL present in current AppAction.cs (33 constants).
**Required feature constants:** `ContentPlacesFeatures.Business` — verify exists in ContentPlaces.Contracts/Authorization/.

### AUTH_AND_PERM_MIX — 18 endpoint files (~90 occurrences)
Pattern: endpoint has BOTH `.WithMetadata(new MustHavePermissionAttribute(...))` AND `.RequireAuthorization()`.

**Resolution:** Drop `.RequireAuthorization()`. The `MustHavePermission` policy handler already requires an authenticated principal; explicit `RequireAuthorization()` is redundant. No new permissions needed.

**Affected files:**
- Auth: SessionEndpoints, InvitationEndpoints
- Security: UserEndpoints
- ContentCore: AttachmentEndpoints, CategoryEndpoints, TagEndpoints, SpecializationEndpoints, TranslationEndpoints, LanguageEndpoints, EntityTagEndpoints, EntityCategoryEndpoints
- ContentPlaces: PlaceEndpoints, ServiceItemEndpoints, BusinessStaffEndpoints, BusinessAmenityEndpoints, AccessibilityFeatureEndpoints

### AUTH_ONLY — endpoints missing permission attachment
| File | Likely permission gap |
|---|---|
| Auth/DeviceEndpoints.cs | Needs `SecurityFeatures.User` + `AppAction.ReadOwn`/`UpdateSelf` for device management |
| Auth/ExternalProviderEndpoints.cs | Needs `AccountsFeatures.Profile` + `AppAction.UpdateSelf` for linking, or AllowAnonymous for OAuth callbacks |
| Accounts/ProfileEndpoints.cs | Needs `AccountsFeatures.Profile` + `AppAction.ReadOwn` / `UpdateSelf` |
| Security/AccountEndpoints.cs | Needs `SecurityFeatures.User` + `AppAction.UpdateSelf` |
| Security/AuditLogEndpoints.cs | Needs `SecurityFeatures.AuditLog` + `AppAction.ReadAny` |
| Security/RoleEndpoints.cs | Needs `SecurityFeatures.Role` + `AppAction.ReadAny`/`Update` |
| YallaJo.Api/OpsEndpoints.cs | `AllowAnonymous` for health probes, else `SystemAccess.Ops` |

### Catalog Gaps to Verify Pre-Sprint
- `AccountsFeatures.Profile` — verify in Accounts.Contracts/Authorization/
- `SecurityFeatures.User`, `SecurityFeatures.Role`, `SecurityFeatures.AuditLog` — verify in Security.Contracts/Authorization/
- `ContentPlacesFeatures.Business` — verify in ContentPlaces.Contracts/Authorization/

If any feature is missing, add `public const string X = nameof(X);` to the relevant `{Module}Features.cs` and a matching `PermissionDescriptor` entry to `{Module}PermissionCatalog.cs`.

## Sprint Action Plan
1. Drop redundant `.RequireAuthorization()` for AUTH_AND_PERM_MIX (~90 simple removals)
2. Replace 5 STRING_POLICY usages with `MustHavePermissionAttribute` metadata
3. Add `MustHavePermissionAttribute` or `.AllowAnonymous()` to 7 AUTH_ONLY endpoints
4. Run Phase 3 PW-3 sanity tests — must turn GREEN

**Estimated effort:** 4-6h
