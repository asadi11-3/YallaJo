# Web-Side Security UI Inventory

**Generated:** $(date)
**Scope:** YallaJo.Web Security Module + Profile Area
**Status:** Complete enumeration (no files modified)

---

## 1. USERS FEATURE
**Path:** `YallaJo.Web/Areas/Admin/Modules/Security/Features/Users`

### 1.1 Controller
| File | Class | Actions |
|------|-------|---------|
| `UsersController.cs` (143 lines) | `UsersController` | **[Area("Admin")]** **[Authorize]** **[RequirePermission(WebPermission.User.Read)]** |

#### Controller Actions
| Action | Route | HTTP Verb | Permission | ViewModel |
|--------|-------|-----------|-----------|-----------|
| `Index` | `/admin/users` | GET | `WebPermission.User.Read` | `UserListVm` |
| `Details` | `/admin/users/details/{userId:guid}` | GET | `WebPermission.User.Read` | `UserDetailsVm` |
| `Activate` | `/admin/users/{userId:guid}/activate` | POST | `WebPermission.User.UpdateAny` | — |
| `Deactivate` | `/admin/users/{userId:guid}/deactivate` | POST | `WebPermission.User.UpdateAny` | — |
| `AssignRole` | `/admin/users/{userId:guid}/roles` | POST | `WebPermission.UserRole.Create` | `AssignRoleVm` |
| `RemoveRole` | `/admin/users/{userId:guid}/roles/{roleId:guid}/remove` | POST | `WebPermission.UserRole.Delete` | — |
| `AddClaim` | `/admin/users/{userId:guid}/claims` | POST | `WebPermission.User.UpdateAny` | `AddClaimVm` |
| `RemoveClaim` | `/admin/users/{userId:guid}/claims/{claimId:guid}/remove` | POST | `WebPermission.User.UpdateAny` | — |

### 1.2 API Client
| File | Class | Methods |
|------|-------|---------|
| `UsersApiClient.cs` (44 lines) | `UsersApiClient` | 8 methods |

#### API Client Methods
| Method | HTTP Verb | URL | Request DTO | Response DTO |
|--------|-----------|-----|-------------|--------------|
| `GetUsersAsync(page, pageSize, ct)` | GET | `/api/v1/security/users?page={page}&pageSize={pageSize}` | — | `UserListResponse` |
| `GetUserAsync(userId, ct)` | GET | `/api/v1/security/users/{userId}` | — | `UserItemResponse` |
| `ActivateUserAsync(userId, ct)` | PATCH | `/api/v1/security/users/{userId}/activate` | `null` | — |
| `DeactivateUserAsync(userId, ct)` | PATCH | `/api/v1/security/users/{userId}/deactivate` | `null` | — |
| `AssignRoleAsync(userId, request, ct)` | POST | `/api/v1/security/users/{userId}/roles` | `AssignRoleRequest` | — |
| `RemoveRoleAsync(userId, roleId, ct)` | DELETE | `/api/v1/security/users/{userId}/roles/{roleId}` | — | — |
| `AddClaimAsync(userId, request, ct)` | POST | `/api/v1/security/users/{userId}/claims` | `AddUserClaimRequest` | — |
| `RemoveClaimAsync(userId, claimId, ct)` | DELETE | `/api/v1/security/users/{userId}/claims/{claimId}` | — | — |

### 1.3 Facade
| File | Class | Methods |
|------|-------|---------|
| `UsersFacade.cs` (153 lines) | `UsersFacade` | 8 methods |

#### Facade Methods
| Method | Returns | Logic |
|--------|---------|-------|
| `GetUsersAsync(page, pageSize, ct)` | `ApiResult<UserListVm>` | Maps `UserListResponse` → `UserListVm` with pagination |
| `GetDetailsAsync(userId, ct)` | `ApiResult<UserDetailsVm>` | Parallel fetch user + roles; maps to `UserDetailsVm` |
| `ActivateAsync(userId, ct)` | `ApiResult` | Calls API; handles auth/errors |
| `DeactivateAsync(userId, ct)` | `ApiResult` | Calls API; handles auth/errors |
| `AssignRoleAsync(userId, vm, ct)` | `ApiResult` | Maps `AssignRoleVm` → `AssignRoleRequest`; handles conflict/validation |
| `RemoveRoleAsync(userId, roleId, ct)` | `ApiResult` | Calls API; treats 404 as success |
| `AddClaimAsync(userId, vm, ct)` | `ApiResult` | Maps `AddClaimVm` → `AddUserClaimRequest`; handles conflict/validation |
| `RemoveClaimAsync(userId, claimId, ct)` | `ApiResult` | Calls API; treats 404 as success |

### 1.4 Request DTOs
| File | Class | Properties |
|------|-------|-----------|
| `Requests/AssignRoleRequest.cs` (6 lines) | `AssignRoleRequest` | `Guid RoleId` |
| `Requests/AddUserClaimRequest.cs` (8 lines) | `AddUserClaimRequest` | `string ClaimType`, `string ClaimValue` |

### 1.5 Response DTOs
| File | Class | Properties |
|------|-------|-----------|
| `Responses/UserListResponse.cs` (11 lines) | `UserListResponse` | `IReadOnlyList<UserItemResponse> Items`, `int PageNumber`, `int PageSize`, `int TotalCount`, `bool HasPreviousPage`, `bool HasNextPage` |
| `Responses/UserItemResponse.cs` (10 lines) | `UserItemResponse` | `Guid Id`, `string Email`, `bool IsActive`, `IReadOnlyList<string> Roles`, `IReadOnlyList<UserClaimItemResponse> Claims` |
| `Responses/UserClaimItemResponse.cs` (9 lines) | `UserClaimItemResponse` | `Guid Id`, `string ClaimType`, `string ClaimValue` |

### 1.6 ViewModels
| File | Class | Properties |
|------|-------|-----------|
| `ViewModels/UserListVm.cs` (13 lines) | `UserListVm` | `IReadOnlyList<UserRowVm> Users`, `int Page`, `int PageSize`, `int TotalCount`, `bool HasPrevious`, `bool HasNext` |
| `ViewModels/UserRowVm.cs` (10 lines) | `UserRowVm` | `Guid Id`, `string Email`, `bool IsActive`, `IReadOnlyList<string> Roles` |
| `ViewModels/UserDetailsVm.cs` (11 lines) | `UserDetailsVm` | `Guid UserId`, `string Email`, `bool IsActive`, `IReadOnlyList<string> CurrentRoles`, `IReadOnlyList<RoleOptionVm> AvailableRoles`, `IReadOnlyList<UserClaimVm> Claims` |
| `ViewModels/AssignRoleVm.cs` (10 lines) | `AssignRoleVm` | `[Required] Guid? RoleId` |
| `ViewModels/AddClaimVm.cs` (13 lines) | `AddClaimVm` | `[Required] string ClaimType`, `[Required] string ClaimValue` |
| `ViewModels/UserClaimVm.cs` (8 lines) | `UserClaimVm` | `Guid Id`, `string ClaimType`, `string ClaimValue` |
| `ViewModels/RoleOptionVm.cs` (8 lines) | `RoleOptionVm` | `Guid Id`, `string Name` |

### 1.7 Mapper
| File | Class | Methods |
|------|-------|---------|
| `Mappers/UsersMapper.cs` (29 lines) | `UsersMapper` | `ToRowVm()`, `ToAssignRoleRequest()`, `ToAddClaimRequest()` |

### 1.8 Views
| File | Purpose | Lines |
|------|---------|-------|
| `Views/Index.cshtml` (56 lines) | List users with pagination; "Manage" button per user | Table + pagination controls |
| `Views/Details.cshtml` (161 lines) | User details: status, roles, claims; activate/deactivate; assign/remove roles; add/remove claims | Form-heavy; permission-gated sections |

---

## 2. ROLES FEATURE
**Path:** `YallaJo.Web/Areas/Admin/Modules/Security/Features/Roles`

### 2.1 Controller
| File | Class | Actions |
|------|-------|---------|
| `RolesController.cs` (168 lines) | `RolesController` | **[Area("Admin")]** **[Authorize]** **[RequirePermission(WebPermission.Role.Read)]** |

#### Controller Actions
| Action | Route | HTTP Verb | Permission | ViewModel |
|--------|-------|-----------|-----------|-----------|
| `Index` | `/admin/roles` | GET | `WebPermission.Role.Read` | `RoleListVm` |
| `Details` | `/admin/roles/details/{roleId:guid}` | GET | `WebPermission.Role.Read` | `RoleDetailsVm` |
| `Create` | `/admin/roles/create` | POST | `WebPermission.Role.Create` | `CreateRoleVm` |
| `Update` | `/admin/roles/{roleId:guid}/update` | POST | `WebPermission.Role.Update` | `UpdateRoleVm` |
| `Deactivate` | `/admin/roles/{roleId:guid}/deactivate` | POST | `WebPermission.Role.Update` | — |
| `AddClaim` | `/admin/roles/{roleId:guid}/claims` | POST | `WebPermission.RoleClaim.Create` | `AddRoleClaimVm` |
| `RemoveClaim` | `/admin/roles/{roleId:guid}/claims/{claimId:guid}/remove` | POST | `WebPermission.RoleClaim.Delete` | — |

### 2.2 API Client
| File | Class | Methods |
|------|-------|---------|
| `RolesApiClient.cs` (37 lines) | `RolesApiClient` | 7 methods |

#### API Client Methods
| Method | HTTP Verb | URL | Request DTO | Response DTO |
|--------|-----------|-----|-------------|--------------|
| `GetRolesAsync(ct)` | GET | `/api/v1/security/roles` | — | `List<RoleItemResponse>` |
| `GetRoleAsync(roleId, ct)` | GET | `/api/v1/security/roles/{roleId}` | — | `RoleDetailsResponse` |
| `CreateRoleAsync(request, ct)` | POST | `/api/v1/security/roles` | `CreateRoleRequest` | `CreateRoleResponse` |
| `UpdateRoleAsync(roleId, request, ct)` | PATCH | `/api/v1/security/roles/{roleId}` | `UpdateRoleRequest` | — |
| `DeactivateRoleAsync(roleId, ct)` | PATCH | `/api/v1/security/roles/{roleId}/deactivate` | `nu
