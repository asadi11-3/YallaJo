# YallaJo Admin Dashboard — HTTP Endpoint Spec

Endpoints accessible to the **Owner**, **SuperAdmin**, and **Admin** roles, for building the admin UI dashboard.

- **Base URL:** `/api/v1`
- **Auth:** every endpoint requires `Authorization: Bearer <jwt>`
- **Authorization model:** primarily **permission-based** (`MustHavePermissionAttribute(<Feature>, <AppAction>)`). Only the 6 ContentPlaces *Business* routes use the named role policy `"Admin"` directly.

---

## Role access rule

Source of truth: `src/Modules/Security/Security.Infrastructure/Seeding/RolePermissionMapping.cs:183-263`.

| Role | Gets |
|---|---|
| **Owner** | **ALL** permissions → every endpoint below |
| **SuperAdmin** | All **except** `System.Update` (owner-only) |
| **Admin** | All except owner-only **and** `User.DeleteAny` (hard-delete user) **and** `Outbox.*` (Ops dead-letter) |

**Practical takeaway:** Owner / SuperAdmin / Admin share the **same dashboard**. Only three things are gated above Admin:

| Capability | Permission | Visible to |
|---|---|---|
| Ops / Outbox dead-letter mgmt | `Outbox.*` | SuperAdmin, Owner |
| Hard-delete a user | `User.DeleteAny` | SuperAdmin, Owner |
| System configuration | `System.Update` | Owner only |

> **UI guidance:** do **not** hardcode role names. The JWT carries permission claims — render each nav item / button based on its `Perm` column below (e.g. `hasPermission("Permission.Role.Create")`). This automatically handles role differences and any future custom roles. Branch on role tier only for the three capabilities above.

---

## Dashboard sections → endpoints

### 1. Users & Access (RBAC) — `/security`

| Group | Method | Path | Perm |
|---|---|---|---|
| Users | GET | `/security/users` | User.Read |
| Users | GET | `/security/users/{userId}` | User.Read |
| Users | PATCH | `/security/users/{userId}/activate` | User.UpdateAny |
| Users | PATCH | `/security/users/{userId}/deactivate` | User.UpdateAny |
| Users | POST | `/security/users/{userId}/roles` | UserRole.Create |
| Users | DELETE | `/security/users/{userId}/roles/{roleId}` | UserRole.Delete |
| Users | POST | `/security/users/{userId}/claims` | User.UpdateAny |
| Users | DELETE | `/security/users/{userId}/claims/{claimId}` | User.UpdateAny |
| Roles | GET | `/security/roles` | Role.Read |
| Roles | GET | `/security/roles/{roleId}` | Role.Read |
| Roles | POST | `/security/roles` | Role.Create |
| Roles | PATCH | `/security/roles/{roleId}` | Role.Update |
| Roles | PATCH | `/security/roles/{roleId}/deactivate` | Role.Update |
| Roles | POST | `/security/roles/{roleId}/claims` | RoleClaim.Create |
| Roles | DELETE | `/security/roles/{roleId}/claims/{claimId}` | RoleClaim.Delete |
| Audit | GET | `/security/audit-logs` | AuditLog.Read |

### 2. User lifecycle (Auth) — `/auth/admin/users` · Perm `User.UpdateAny`

| Method | Path | Note |
|---|---|---|
| DELETE | `/auth/admin/users/{userId}/sessions` | Force logout all sessions |
| POST | `/auth/admin/users/{userId}/reset-password` | |
| PATCH | `/auth/admin/users/{userId}/suspend` | |
| PATCH | `/auth/admin/users/{userId}/reactivate` | |
| PATCH | `/auth/admin/users/{userId}/archive` | |
| POST | `/auth/admin/users/{userId}/reassign` | |
| POST/GET | `/auth/invitations` (+ resend) | Invitation mgmt |

> ⚠️ Hard-delete user (`User.DeleteAny`) → **SuperAdmin / Owner only**.

### 3. Analytics & Dashboards — `/admin` · Perm `AdminDashboard.Read`

| Method | Path | Perm |
|---|---|---|
| GET | `/admin/dashboard` | AdminDashboard.Read |
| GET | `/admin/dashboard/revenue` | AdminDashboard.Read |
| GET | `/admin/dashboard/bookings` | AdminDashboard.Read |
| GET | `/admin/dashboard/users` | AdminDashboard.Read |
| GET | `/admin/interactions` | Interaction.Read |
| GET | `/admin/interactions/user/{userId}` | Interaction.Read |
| GET | `/admin/audit-logs` | AuditLog.Read |
| POST | `/admin/audit-logs/{id}/redact` | AuditLog.Redact |
| GET | `/admin/audit-logs/export` | AuditLog.Export |

### 4. Recommendations engine — `/analytics/admin`

| Method | Path | Perm |
|---|---|---|
| POST | `/analytics/admin/batches/refresh` | Batch.Refresh |
| GET | `/analytics/admin/batches` | Batch.Read |
| POST | `/analytics/admin/boosts` | BoostPackage.Create |
| POST | `/analytics/admin/boosts/cpc` | BoostPackage.Create |
| DELETE | `/analytics/admin/boosts/{boostId}` | BoostPackage.Delete |
| POST | `/analytics/admin/pins` | EditorialPin.Create |
| DELETE | `/analytics/admin/pins/{pinId}` | EditorialPin.Delete |
| POST | `/analytics/admin/seasonality` | SeasonalityRule.Create |
| GET | `/analytics/admin/seasonality` | Batch.Read |
| DELETE | `/analytics/admin/seasonality/{ruleId}` | SeasonalityRule.Delete |
| POST | `/analytics/admin/holidays` | HolidayCalendar.Create |
| GET | `/analytics/admin/holidays/{year}` | Batch.Read |
| PUT | `/analytics/admin/entities/{kind}/{entityId}/photogenic` | Photogenic.Update |
| POST | `/analytics/admin/experiments` | Experiment.Create |
| PUT | `/analytics/admin/experiments/{experimentId}/start` | Experiment.Update |
| PUT | `/analytics/admin/experiments/{experimentId}/complete` | Experiment.Update |
| GET | `/analytics/admin/metrics` | Batch.Read |
| GET | `/analytics/admin/segments` | Batch.Read |

### 5. Provider approvals — `/admin/providers` · Perm `AdminProviderQueue.*`

| Method | Path | Action |
|---|---|---|
| GET | `/admin/providers` | Read |
| POST | `/admin/providers/{id}/approve` | Approve |
| POST | `/admin/providers/{id}/reject` | Reject |
| POST | `/admin/providers/{id}/request-docs` | RequestDocs |
| POST | `/admin/providers/{id}/suspend` | Suspend |
| POST | `/admin/providers/{id}/reinstate` | Reinstate |

### 6. Business (Places) approvals — `/places/businesses` ⭐ role-policy `"Admin"`

Also carries `ContentPlaces.Business.*` permission.

| Method | Path | Action |
|---|---|---|
| DELETE | `/places/businesses/{id}` | Delete |
| POST | `/places/businesses/admin/{id}/approve` | Approve |
| POST | `/places/businesses/admin/{id}/reject` | Reject |
| POST | `/places/businesses/admin/{id}/request-more-docs` | RequestDocs |
| POST | `/places/businesses/admin/{id}/suspend` | Suspend |
| POST | `/places/businesses/admin/{id}/reinstate` | Reinstate |

### 7. Tour Guide management — `/guides/admin` · Perm `TourGuideProfile.*`

| Method | Path | Action |
|---|---|---|
| GET | `/guides/admin/{guideId}` | Read |
| POST | `/guides/admin/{guideId}/suspend` | Suspend |
| POST | `/guides/admin/{guideId}/reinstate` | Reinstate |
| PUT | `/guides/admin/{guideId}` | Update |
| DELETE | `/guides/admin/{guideId}` | DeleteAny |

### 8. Tour moderation — `/tours/admin`

| Method | Path | Action |
|---|---|---|
| POST | `/tours/admin/{id}/approve` | Approve |
| POST | `/tours/admin/{id}/reject` | Reject |
| POST | `/tours/admin/{id}/suspend` | Suspend |
| POST | `/tours/admin/{id}/reinstate` | Reinstate |
| PATCH | `/tours/admin/{id}/feature` | Feature |

### 9. Creator management — `/blogs/admin/creators` · Perm `AdminCreatorQueue.*`

| Method | Path | Action |
|---|---|---|
| GET | `/blogs/admin/creators/applications` | Read |
| GET | `/blogs/admin/creators/applications/{id}` | Read |
| POST | `/blogs/admin/creators/applications/{id}/approve` | Approve |
| POST | `/blogs/admin/creators/applications/{id}/reject` | Reject |
| POST | `/blogs/admin/creators/applications/{id}/request-more-info` | RequestMoreInfo |
| POST | `/blogs/admin/creators/profiles/{profileId}/suspend` | Suspend |
| POST | `/blogs/admin/creators/profiles/{profileId}/reinstate` | Reinstate |
| POST | `/blogs/admin/creators/profiles/{profileId}/promote` | PromoteTier |
| POST | `/blogs/admin/creators/profiles/{profileId}/demote` | DemoteTier |
| GET | `/blogs/admin/creators/profiles/{id}` | Read |
| PUT | `/blogs/admin/creators/profiles/{id}` | Update |
| DELETE | `/blogs/admin/creators/profiles/{id}` | Delete |
| POST | `/blogs/admin/creators/invitations` | Invite |

### 10. Blog moderation — `/blogs/admin`

| Method | Path |
|---|---|
| GET | `/blogs/admin/{id}` |
| GET | `/blogs/admin/{id}/translations` |
| GET | `/blogs/admin/{id}/translations/{languageCode}` |
| PUT | `/blogs/admin/{id}/translations/{languageCode}` |
| GET | `/blogs/admin/deleted` |
| GET | `/blogs/admin/queue` |
| POST | `/blogs/admin/{id}/approve` |
| POST | `/blogs/admin/{id}/reject` |
| POST | `/blogs/admin/{id}/remove` |

### 11. Content taxonomy — `/content-core`

| Group | Endpoints |
|---|---|
| Categories | `GET /content-core/categories/admin`, `GET /content-core/categories/admin/{id}`, plus admin create / update / delete / deactivate / restore / reorder |
| Translations | translate / batch / update / approve / backfill / approve-batch |
| Tags / Languages / Specializations | CRUD |

### 12. Bookings — `/admin/bookings`

| Method | Path | Perm |
|---|---|---|
| POST | `/admin/bookings/{id}/force-refund` | AdminBookingDashboard.Update |
| GET | `/bookings/admin/all` | AdminBookingDashboard.Read |

> Admin override (bypass provider-ownership) on confirm / cancel / reject / complete via `AdminBookingDashboard.*`.

### 13. Finance — gated by `AdminFinanceDashboard.Read`

| Method | Path |
|---|---|
| GET | `/finance/admin/dashboard` |
| GET | `/payouts/admin/pending` |
| POST | `/payouts/admin/trigger` |
| POST | `/payouts/{id}/approve` |
| GET | `/payments/admin/all` |
| GET | `/disputes/admin/open` |
| POST | `/disputes/{id}/review` |
| POST | `/disputes/{id}/resolve` |
| POST | `/disputes/{id}/escalate` |
| — | Commission rules CRUD |
| — | Invoice admin flags |

### 14. Moderation (Social) — `/social`

| Group | Method | Path | Perm |
|---|---|---|---|
| Moderation | GET | `/social/moderation/logs` | ContentModerationLog.Read |
| Moderation | POST | `/social/moderation/warn` | AdminModerationQueue.Warn |
| Moderation | POST | `/social/moderation/ban` | AdminModerationQueue.Ban |
| Moderation | DELETE | `/social/moderation/ban/{userId}` | AdminModerationQueue.Ban |
| Reports | GET | `/social/reports/admin` | AdminModerationQueue.Read |
| Reports | POST | `/social/reports/admin/{id}/resolve` | AdminModerationQueue.Resolve |
| Reviews | GET | `/social/reviews/admin/flagged` | — |
| Reviews | POST | `/social/reviews/admin/{id}/approve` | — |
| Reviews | POST | `/social/reviews/admin/{id}/remove` | — |

### 15. Support — `/support/tickets` · Perm `AdminSupportQueue.*`

| Method | Path | Action |
|---|---|---|
| POST | `/support/tickets/{id}/assign` | Assign |
| POST | `/support/tickets/{id}/resolve` | Resolve |

> Admins see all tickets via `AdminSupportQueue.Read` (handlers branch on this).

### 16. Notification templates — `/admin/notification-templates` · Perm `NotificationTemplate.*`

| Method | Path | Action |
|---|---|---|
| GET | `/admin/notification-templates` | Read |
| POST | `/admin/notification-templates` | Create |
| PUT | `/admin/notification-templates/{id}` | Update |
| DELETE | `/admin/notification-templates/{id}` | Delete |

### 17. Ops ⚠️ SuperAdmin + Owner only — `OpsEndpoints.cs`

Outbox dead-letter management (`Outbox.*` perms). **Hide from Admin.**

---

## Reference

The existing Web Admin MVC area (`src/Hosts/YallaJo.Web/Areas/Admin`) already wires these endpoints through `ApiClient` classes (`RolesApiClient`, `UsersApiClient`, etc.) using `[RequirePermission]` guards — a working reference for which endpoint maps to which screen.
