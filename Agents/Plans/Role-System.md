# Role System — Architecture Plan

> **Status:** All design decisions LOCKED. Ready for execution.
> **Scope:** Define platform roles, auto-assignment on approval events, role stacking, and privilege hierarchy.

---

## Decision Summary

| # | Decision | Detail |
|---|----------|--------|
| 1 | **Automatic role assignment** | Roles assigned automatically on approval via integration event handlers in Security module |
| 2 | **Single Provider role** | One Provider role for TourOperator, HotelResort, ActivityCenter, Agency, BusinessOwner |
| 3 | **IndependentGuide → TourGuide only** | IndependentGuide gets TourGuide role (NOT Provider). Cannot create business listings. |
| 4 | **Roles stack additively** | User can hold multiple roles simultaneously (User + Provider + Creator). Same account, more capabilities. |
| 5 | **Guest = pre-verified** | Registered but email not verified. Upgrades to User on email verification. |
| 6 | **Security module owns role assignment** | Security.Infrastructure consumes all approval integration events and assigns roles. Single responsibility. |
| 7 | **TourGuide cannot create businesses** | Guides are found via tour search only, not business listings in ContentPlaces. |
| 8 | **Agency auto-assigns TourGuide** | When agency affiliates a user as guide, system auto-assigns TourGuide role if not present. |
| 9 | **Suspension blocks via status, not role removal** | Suspended provider keeps Provider role. ProviderApplication.Status=Suspended blocks actions. Reinstate restores access without re-assigning. |

---

## Roles

### Final Role Table

| Role | Privilege Level | Auto-Assigned When | Permissions Scope |
|------|----------------|-------------------|-------------------|
| **Owner** | 100 | Platform bootstrap (singleton) | Everything — unrestricted |
| **SuperAdmin** | 80 | Manual by Owner | Admin management, system configuration, audit |
| **Admin** | 60 | Manual by SuperAdmin | All approval queues, content moderation, dashboards, force actions |
| **User** | 10 (Standard) | Email verified (upgrade from Guest) | Browse, book tours, leave reviews, manage favorites, earn loyalty |
| **Provider** | 10 (Standard) | ProviderApplication approved (all types EXCEPT IndependentGuide) | Create businesses, manage service items, provider booking dashboard, receive payouts |
| **TourGuide** | 10 (Standard) | IndependentGuide approved OR Agency affiliates a user | Create/run tours, set schedule/pricing, apply to tours, propose tours, guide dashboard |
| **Creator** | 10 (Standard) | CreatorApplication approved | Create blog posts, submit for review, creator dashboard |
| **Guest** | 10 (Standard) | Registration (before email verification) | Limited read access, cannot book or write |

### Privilege Hierarchy

```
Owner (100) > SuperAdmin (80) > Admin (60) > Standard (10)
                                               ├── User
                                               ├── Provider
                                               ├── TourGuide
                                               ├── Creator
                                               └── Guest
```

All Standard-tier roles (User, Provider, TourGuide, Creator, Guest) are differentiated by **permissions**, not privilege level. Higher privilege levels can manage lower ones.

---

## Role Assignment Triggers

### Automatic (via Integration Event Handlers)

| Trigger Event | Handler | Role Assigned | Condition |
|---|---|---|---|
| `ProviderApprovedIntegrationEvent` | `ProviderApprovedAssignRoleHandler` | **Provider** | `Type != IndependentGuide` |
| `ProviderApprovedIntegrationEvent` | `ProviderApprovedAssignRoleHandler` | **TourGuide** | `Type == IndependentGuide` |
| `CreatorApplicationApprovedIntegrationEvent` | `CreatorApprovedAssignRoleHandler` | **Creator** | Always |
| `AgencyGuideAffiliatedIntegrationEvent` | `AgencyGuideAffiliatedAssignRoleHandler` | **TourGuide** | If user doesn't already have TourGuide role |
| `EmailVerifiedIntegrationEvent` | `EmailVerifiedUpgradeRoleHandler` | **User** (replaces Guest) | If current role is Guest |

### Manual (Admin Actions)

| Action | Who Can Do It | Result |
|---|---|---|
| Assign Admin role | SuperAdmin or Owner | User gets Admin role |
| Assign SuperAdmin role | Owner only | User gets SuperAdmin role |
| Revoke any Standard role | Admin+ | Remove Provider/TourGuide/Creator from user |

---

## Role Stacking Examples

| User Journey | Final Roles |
|---|---|
| Tourist signs up, verifies email | User |
| Tourist applies as TourOperator, approved | User + Provider |
| Tourist applies as IndependentGuide, approved | User + TourGuide |
| Provider (HotelResort) also applies as Creator, approved | User + Provider + Creator |
| Agency affiliates an existing User as guide | User + TourGuide |
| IndependentGuide also gets Creator approved | User + TourGuide + Creator |
| Agency owner (Provider) also guides tours personally | User + Provider + TourGuide |

**Note:** A user ALWAYS keeps the User role. Business roles (Provider, TourGuide, Creator) are additive on top.

---

## ProviderType → Role Mapping

| ProviderType | Role Assigned | Can Create Business? | Can Run Tours? |
|---|---|---|---|
| TourOperator (0) | Provider | Yes (Agency type) | No (assigns guides) |
| IndependentGuide (1) | TourGuide | No | Yes |
| HotelResort (2) | Provider | Yes (Hotel type) | No |
| ActivityCenter (3) | Provider | Yes (Activity type) | No |
| Agency (4) | Provider | Yes (Agency type) | No (assigns guides) |
| BusinessOwner (5) | Provider | Yes (Restaurant/Shop/Transport/Other) | No |

---

## Suspension Behavior

Suspension does NOT revoke roles. It blocks actions via entity status:

| Entity | Status Field | Effect When Suspended |
|---|---|---|
| ProviderApplication | `Status = Suspended` | All Provider-scoped endpoints return 403 via `IProviderStatusService` check |
| CreatorProfile | `Status = Suspended` | All Creator-scoped endpoints return 403 via profile status check |
| TourGuide | `Status = Suspended` | All TourGuide-scoped endpoints return 403 via guide status check |

**Reinstate** restores access immediately — no role re-assignment needed.

---

## Code Changes

### 1. AppRoles.cs (MODIFY)

```csharp
public static class AppRoles
{
    public const string Admin = nameof(Admin);
    public const string SuperAdmin = nameof(SuperAdmin);
    public const string Owner = nameof(Owner);
    public const string User = nameof(User);
    public const string Provider = nameof(Provider);      // NEW
    public const string TourGuide = nameof(TourGuide);
    public const string Creator = nameof(Creator);        // NEW
    public const string Guest = nameof(Guest);

    public static IReadOnlyList<string> AllRoles { get; } = new[]
        { SuperAdmin, Admin, Owner, Provider, TourGuide, Creator, User, Guest };

    public static IReadOnlyList<string> DefaultRoles { get; } = new[] { Guest };  // CHANGED: Guest on signup

    public static IReadOnlyList<string> ProtectedRoles { get; } = new[] { SuperAdmin, Owner };

    public static IReadOnlyList<string> OwnerOnlyRoles { get; } = new[] { Owner, SuperAdmin };
}
```

**GetPrivilegeLevel update:**
```csharp
if (roleName.Equals(User, ...) || roleName.Equals(TourGuide, ...)
    || roleName.Equals(Provider, ...) || roleName.Equals(Creator, ...)
    || roleName.Equals(Guest, ...))
    return RolePrivilegeLevel.Standard;
```

### 2. New Event Handlers (Security.Infrastructure)

#### `ProviderApprovedAssignRoleHandler.cs`
```csharp
public sealed class ProviderApprovedAssignRoleHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ISecurityUnitOfWork unitOfWork,
    ISecurityInboxStore inboxStore,
    ILogger<ProviderApprovedAssignRoleHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ProviderApprovedIntegrationEvent>>
{
    // If Type == IndependentGuide → assign TourGuide role
    // Else → assign Provider role
    // Idempotent: skip if user already has role
}
```

#### `CreatorApprovedAssignRoleHandler.cs`
```csharp
public sealed class CreatorApprovedAssignRoleHandler(...)
    : INotificationHandler<IntegrationEventNotification<CreatorApplicationApprovedIntegrationEvent>>
{
    // Assign Creator role
    // Idempotent: skip if user already has role
}
```

#### `AgencyGuideAffiliatedAssignRoleHandler.cs`
```csharp
public sealed class AgencyGuideAffiliatedAssignRoleHandler(...)
    : INotificationHandler<IntegrationEventNotification<AgencyGuideAffiliatedIntegrationEvent>>
{
    // Assign TourGuide role if not present
}
```

#### `EmailVerifiedUpgradeRoleHandler.cs`
```csharp
public sealed class EmailVerifiedUpgradeRoleHandler(...)
    : INotificationHandler<IntegrationEventNotification<EmailVerifiedIntegrationEvent>>
{
    // Remove Guest role, add User role
}
```

### 3. New Integration Events (if not existing)

| Event | Module | Properties |
|---|---|---|
| `AgencyGuideAffiliatedIntegrationEvent` | Accounts.Contracts | UserId, AgencyId, AffiliationId |
| `EmailVerifiedIntegrationEvent` | Auth.Contracts (or Identity) | UserId, Email |

**Already existing:** `ProviderApprovedIntegrationEvent` (Accounts.Contracts), `CreatorApplicationApprovedIntegrationEvent` (ContentBlogs.Contracts)

### 4. Role Seed Data

Roles must exist in the database. Add to role seed:
```
{ Name: "Provider", Description: "Approved service provider" }
{ Name: "Creator", Description: "Approved content creator" }
```

Existing roles already seeded: Owner, SuperAdmin, Admin, User, TourGuide, Guest.

### 5. Permission Catalog Updates

Each module's PermissionCatalog assigns permissions to roles:

| Permission | Roles That Get It |
|---|---|
| ContentPlaces.Business.Create | Provider |
| ContentPlaces.Business.Update | Provider |
| ContentPlaces.ServiceItem.* | Provider |
| ContentTours.Tour.Create | Provider, TourGuide |
| ContentTours.TourGuide.* | TourGuide |
| ContentTours.GuideApplication.* | TourGuide |
| ContentTours.TourProposal.* | TourGuide |
| ContentBlogs.Blog.Create | Admin, Creator |
| ContentBlogs.Blog.SubmitForReview | Creator |
| Booking.TourBooking.Create | User, Provider, TourGuide, Creator |

---

## DefaultRoles Change

**Current:** `DefaultRoles = { User }` — assigned on registration.
**New:** `DefaultRoles = { Guest }` — assigned on registration. Upgraded to User on email verification.

**Impact:** Need to verify what `DefaultRoles` is used for in the registration flow and update accordingly.

---

## Execution Phases

### Phase 1: Domain Changes (Low Risk)
1. Add `Provider` and `Creator` constants to `AppRoles`
2. Update `AllRoles` list
3. Update `DefaultRoles` to `{ Guest }`
4. Update `GetPrivilegeLevel` to include Provider and Creator
5. Add role seed data for Provider and Creator

### Phase 2: Integration Events (Low Risk)
1. Verify `ProviderApprovedIntegrationEvent` includes `ProviderType` field
2. Verify `CreatorApplicationApprovedIntegrationEvent` includes `UserId`
3. Create `AgencyGuideAffiliatedIntegrationEvent` in Accounts.Contracts (if not exists)
4. Verify `EmailVerifiedIntegrationEvent` exists (or create in Auth.Contracts)

### Phase 3: Event Handlers (Medium Risk)
1. Create `ProviderApprovedAssignRoleHandler` in Security.Infrastructure
2. Create `CreatorApprovedAssignRoleHandler` in Security.Infrastructure
3. Create `AgencyGuideAffiliatedAssignRoleHandler` in Security.Infrastructure
4. Create `EmailVerifiedUpgradeRoleHandler` in Security.Infrastructure
5. Register inbox processing for each handler

### Phase 4: Permission Mapping (Medium Risk)
1. Update each module's PermissionCatalog to map permissions to new roles
2. Verify endpoint `MustHavePermission` attributes align with role-permission grants
3. Update any role-checking logic that references hardcoded role names

### Phase 5: Registration Flow (Low Risk)
1. Update registration to assign Guest (not User) as default role
2. Add email verification → User upgrade handler
3. Test: new user gets Guest → verify email → gets User

### Phase 6: Testing & Build (Low Risk)
1. Update existing tests referencing old DefaultRoles
2. Verify solution builds with 0 errors
3. Verify existing endpoint auth still works

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Changing DefaultRoles from User→Guest breaks existing users | Only affects NEW registrations. Existing users keep User role. |
| Existing `IProviderStatusService` checks redundant with role | Keep as belt-and-suspenders. Role = can access endpoint. Service = is currently active (not suspended). |
| Role seeding order matters | Seed roles before handlers run. Use migration or startup seeder. |
| AgencyGuideAffiliatedIntegrationEvent doesn't exist yet | Create as part of Platform-Onboarding plan. This plan depends on it. |
| TourGuide role already exists but never auto-assigned | First handler run assigns to existing approved IndependentGuides (one-time migration). |

---

## Dependencies on Other Plans

| This Plan Needs | From Plan |
|---|---|
| `AgencyGuideAffiliatedIntegrationEvent` | Platform-Onboarding-Workflow.md (Phase: Agency Roster) |
| `EmailVerifiedIntegrationEvent` | Auth module (may already exist) |
| Permission-to-role mapping details | Each module's PermissionCatalog (already exists) |

---

## File Count Estimate

| Type | Count |
|------|-------|
| Modified files | 3-5 (AppRoles.cs, registration flow, role seed, GetPrivilegeLevel) |
| New files | 4-6 (event handlers + integration event DTOs) |
| Test updates | 2-4 |
| **Total** | ~10-15 files |
