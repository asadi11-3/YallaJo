# TASK 2 + TASK 3 — Fix §8.2 Endpoint Authorization Violations

This file covers the two parallel sub-tasks fixing the 28 endpoint-level violations across 5 Presentation projects.

---

## TASK 2 — AUTH_ONLY violations (14 endpoints)

> **Owner:** Fadwa (Beginner) — **Hours:** 12h — **Hard deadline:** Sun **2027-03-07 17:00**
> **Earliest start:** Wed 2027-03-03 09:00
> **Scope:** Auth.Presentation (7) + Accounts.Presentation (5) + Security.Presentation (2)
> **Pattern:** Replace `.RequireAuthorization()` with `.WithMetadata(new MustHavePermissionAttribute(...))`.

### 1. Auth.Presentation (7 endpoints)

| # | Endpoint | Replace with |
|---|---|---|
| 1 | POST `/auth/change-password` | `MustHavePermission(AuthFeatures.Account, AppAction.Update)` |
| 2 | POST `/auth/logout` | `MustHavePermission(AuthFeatures.Session, AppAction.Delete)` |
| 3 | POST `/auth/logout-all` | `MustHavePermission(AuthFeatures.Session, AppAction.Delete)` |
| 4 | GET `/auth/sessions` | `MustHavePermission(AuthFeatures.Session, AppAction.Read)` |
| 5 | DELETE `/auth/sessions/{id}` | `MustHavePermission(AuthFeatures.Session, AppAction.Delete)` |
| 6 | POST `/auth/devices/{deviceId}/revoke` | `MustHavePermission(AuthFeatures.Device, AppAction.Delete)` |
| 7 | GET `/auth/profile-summary` (if exists) | `MustHavePermission(AuthFeatures.Account, AppAction.Read)` |

**If `AuthFeatures.Session` or `AuthFeatures.Device` don't exist yet, PW-2 should have flagged them. Add to `AuthFeatures` + `AuthPermissionCatalog` as part of this PR.**

### 2. Accounts.Presentation (5 endpoints)

| # | Endpoint | Replace with |
|---|---|---|
| 1 | GET `/profile` | `MustHavePermission(AccountsFeatures.Profile, AppAction.Read)` |
| 2 | PUT `/profile` | `MustHavePermission(AccountsFeatures.Profile, AppAction.Update)` |
| 3 | POST `/profile/avatar` | `MustHavePermission(AccountsFeatures.Profile, AppAction.Update)` |
| 4 | POST `/provider/apply` | `MustHavePermission(AccountsFeatures.ProviderApplication, AppAction.Create)` |
| 5 | GET `/provider/status` | `MustHavePermission(AccountsFeatures.ProviderApplication, AppAction.Read)` |

### 3. Security.Presentation (2 endpoints)

| # | Endpoint | Replace with |
|---|---|---|
| 1 | GET `/admin/roles` | `MustHavePermission(SecurityFeatures.Role, AppAction.Read)` |
| 2 | GET `/admin/permissions` | `MustHavePermission(SecurityFeatures.Permission, AppAction.Read)` |

### 4. Refactor Pattern (per endpoint)

```csharp
// Before
group.MapGet("/profile", GetProfileAsync).RequireAuthorization();

// After
group.MapGet("/profile", GetProfileAsync)
    .WithMetadata(new MustHavePermissionAttribute(
        AccountsFeatures.Profile,
        AppAction.Read));
```

If the handler also injected `ICurrentUser` for self-only access, that injection stays — it's a valid ownership/self-edit use case (AC-R2 rule).

### 5. TASK 2 WBS

| # | Step | Hours |
|---|---|---|
| 1 | Auth.Presentation — 7 endpoints + tests | 4 |
| 2 | Accounts.Presentation — 5 endpoints + tests | 3 |
| 3 | Security.Presentation — 2 endpoints + tests | 1.5 |
| 4 | Add any missing AuthFeatures/SecurityFeatures items (PW-2 outcome) | 1 |
| 5 | Per-endpoint integration tests (`MustHavePermission` attribute verification) | 2 |
| 6 | PR + review fixes | 0.5 |
| **Total** | | **12h** |

### 6. TASK 2 Acceptance

- All 14 endpoints listed above have `MustHavePermissionAttribute` attached.
- PW-3 sanity test count of `AUTH_ONLY` violations drops by 14 (this task's full scope).
- All endpoint integration tests pass.

---

## TASK 3 — STRING_POLICY + MISSING_METADATA violations (14 endpoints)

> **Owner:** Mohammad (Intermediate) — **Hours:** 16h — **Hard deadline:** Wed **2027-03-10 17:00**
> **Earliest start:** Wed 2027-03-03 09:00 (parallel with TASK 1+2)
> **Scope:** ContentCore.Presentation (9 STRING_POLICY) + ContentPlaces.Presentation (5 STRING_POLICY + 4 MISSING_METADATA)
> **Pattern:** Replace string-based policies with typed `MustHavePermissionAttribute`. Add explicit metadata to silent endpoints.

### 1. ContentCore.Presentation (9 STRING_POLICY)

Each endpoint uses `.RequireAuthorization("permission:contentcore.{feature}.{action}")` — replace with typed attribute. PW-1 CSV has exact line numbers.

| # | Endpoint (typical) | Replacement |
|---|---|---|
| 1 | POST `/categories` | `MustHavePermission(ContentCoreFeatures.Categories, AppAction.Create)` |
| 2 | PUT `/categories/{id}` | `MustHavePermission(ContentCoreFeatures.Categories, AppAction.Update)` |
| 3 | DELETE `/categories/{id}` | `MustHavePermission(ContentCoreFeatures.Categories, AppAction.Delete)` |
| 4 | PUT `/categories/reorder` | `MustHavePermission(ContentCoreFeatures.Categories, AppAction.Update)` |
| 5 | POST `/tags` | `MustHavePermission(ContentCoreFeatures.Tags, AppAction.Create)` |
| 6 | PUT `/tags/{id}` | `MustHavePermission(ContentCoreFeatures.Tags, AppAction.Update)` |
| 7 | POST `/specializations` | `MustHavePermission(ContentCoreFeatures.Specializations, AppAction.Create)` |
| 8 | PUT `/specializations/{id}` | `MustHavePermission(ContentCoreFeatures.Specializations, AppAction.Update)` |
| 9 | POST `/languages` | `MustHavePermission(ContentCoreFeatures.Languages, AppAction.Create)` |

**Exact list from PW-1 CSV is the source of truth.** Adjust if newer endpoints appeared in interim sprints.

### 2. ContentPlaces.Presentation (5 STRING_POLICY)

Same pattern. Likely candidates:

| # | Endpoint (typical) | Replacement |
|---|---|---|
| 1 | POST `/places` | `MustHavePermission(ContentPlacesFeatures.Place, AppAction.Create)` |
| 2 | PUT `/places/{id}` | `MustHavePermission(ContentPlacesFeatures.Place, AppAction.Update)` |
| 3 | DELETE `/places/{id}` | `MustHavePermission(ContentPlacesFeatures.Place, AppAction.Delete)` |
| 4 | POST `/businesses` | `MustHavePermission(ContentPlacesFeatures.Business, AppAction.Create)` |
| 5 | PUT `/businesses/{id}` | `MustHavePermission(ContentPlacesFeatures.Business, AppAction.Update)` |

### 3. ContentPlaces.Presentation (4 MISSING_METADATA — MOST DANGEROUS)

These endpoints have NO authorization metadata. Without a fix, default behavior depends on global policy — typically silently 401 or silently allow-all. Each must explicitly state intent:

| # | Endpoint (typical) | Intended | Add |
|---|---|---|---|
| 1 | GET `/places/{id}/businesses` | PUBLIC | `.AllowAnonymous()` |
| 2 | GET `/businesses/{id}/hours` | PUBLIC | `.AllowAnonymous()` |
| 3 | GET `/places/{id}/staff` | PROVIDER OWNER | `MustHavePermission(ContentPlacesFeatures.Business, AppAction.Read)` + handler IDOR via `Business.OwnerUserId` |
| 4 | GET `/businesses/{id}/amenities/admin-view` | ADMIN | `MustHavePermission(ContentPlacesFeatures.AdminBusinessManagement, AppAction.Read)` |

**Decision authority:** business intent per PDF 2. Tech Lead arbitrates ambiguous cases.

### 4. Refactor Pattern (STRING_POLICY)

```csharp
// Before
group.MapPost("/categories", CreateCategoryAsync)
    .RequireAuthorization("permission:contentcore.categories.create");

// After
group.MapPost("/categories", CreateCategoryAsync)
    .WithMetadata(new MustHavePermissionAttribute(
        ContentCoreFeatures.Categories,
        AppAction.Create));
```

### 5. Refactor Pattern (MISSING_METADATA — public)

```csharp
// Before
group.MapGet("/places/{id}/businesses", GetPlaceBusinessesAsync);
// (no metadata — silent default)

// After
group.MapGet("/places/{id}/businesses", GetPlaceBusinessesAsync)
    .AllowAnonymous();
```

### 6. TASK 3 WBS

| # | Step | Hours |
|---|---|---|
| 1 | ContentCore.Presentation — 9 endpoints + tests | 6 |
| 2 | ContentPlaces.Presentation STRING_POLICY — 5 endpoints + tests | 3 |
| 3 | ContentPlaces.Presentation MISSING_METADATA — 4 endpoints, intent decisions, + tests | 3 |
| 4 | Per-endpoint integration tests (`MustHavePermission` attribute verification) | 3 |
| 5 | PR + review fixes | 1 |
| **Total** | | **16h** |

### 7. TASK 3 Acceptance

- All 14 endpoints listed above have either `MustHavePermissionAttribute` OR `.AllowAnonymous()`.
- PW-3 sanity test STRING_POLICY count drops by 14. MISSING_METADATA count drops by 4.
- All endpoint integration tests pass.
- Tech Lead has signed off on the 4 MISSING_METADATA intent decisions in PR description.

---

## Combined Coordination Notes (TASK 2 + TASK 3)

- **Sequential, not parallel commits to same file:** if Mohammad (TASK 3) touches `ContentPlaces.Presentation` while Fadwa (TASK 2) touches Auth/Accounts/Security, no merge conflicts. If schedules slip and both end up in ContentPlaces, coordinate via daily standup.
- **TASK 1 (Mahmoud) also touches ContentPlaces endpoints** (he changes the endpoints for the 8 handlers). Mohammad and Mahmoud agree split: Mahmoud owns the 8 business-admin endpoints (he's already editing them); Mohammad owns the remaining ContentPlaces endpoints. Cross-link in standup.
- **No new migrations** in this sprint.
- **No new permissions** unless PW-2 flagged some.
