# Security module — ERD

**DbContext:** `SecurityDbContext` · **Schema:** `security`

The identity authority: users, roles, claims, emails, phones, and the audit log.
`Security.User.Id` is the most-referenced identifier across the platform.

## Entities

| Entity | Base | Aggregate root | 🗑 soft-delete | 🔒 rowversion |
|--------|------|----------------|---------------|--------------|
| `User` | AuditableEntity | ✅ | ✅ | ✅ |
| `Role` | AuditableEntity | ❌ *(see notes)* | ✅ | ✅ |
| `UserRole` ⊕ | AuditableEntity | ❌ | ✅ | ✅ |
| `UserClaim` | AuditableEntity | ❌ | ✅ | ✅ |
| `RoleClaim` | AuditableEntity | ❌ | ✅ | ✅ |
| `Email` | AuditableEntity | ❌ | ✅ | ✅ |
| `Phone` | AuditableEntity | ❌ | ✅ | ✅ |
| `AuditLog` | BaseEntity | ❌ | ❌ | ❌ |
| `OutboxMessage` / `InboxMessage` | infra | — | — | — |

## Diagram

```mermaid
erDiagram
    User {
        guid Id PK
        string LifecycleState
        bool IsActive
    }
    Role {
        guid Id PK
        string Name
    }
    UserRole {
        guid Id PK
        guid UserId FK
        guid RoleId FK
    }
    UserClaim {
        guid Id PK
        guid UserId FK
        string ClaimType
    }
    RoleClaim {
        guid Id PK
        guid RoleId FK
        string ClaimType
    }
    Email {
        guid Id PK
        guid UserId FK
        string Address
    }
    Phone {
        guid Id PK
        guid UserId FK
        string Number
    }
    AuditLog {
        guid Id PK
        guid UserId LREF "nullable"
        guid ActorUserId LREF "nullable"
        string ResourceType
        guid ResourceId LREF "nullable"
    }

    User ||--o{ Email : "Emails (Cascade)"
    User ||--o{ Phone : "Phones (Cascade)"
    User ||--o{ UserRole : "UserRoles (Cascade)"
    User ||--o{ UserClaim : "UserClaims (Cascade)"
    Role ||--o{ UserRole : "UserRoles (Cascade)"
    Role ||--o{ RoleClaim : "RoleClaims (Cascade)"
```

## Relationships (all real FKs, intra-module)

| Principal → Dependent | FK | Delete |
|-----------------------|----|--------|
| User → Email | `UserId` | Cascade |
| User → Phone | `UserId` | Cascade |
| User → UserRole | `UserId` | Cascade |
| User → UserClaim | `UserId` | Cascade |
| Role → UserRole | `RoleId` | Cascade |
| Role → RoleClaim | `RoleId` | Cascade |

## Join table (⊕)

- **`UserRole`** — junction of `User` ↔ `Role`. Modeled as a **surrogate-key join entity**
  (own `Id` PK) with two 1:N relationships + a **unique composite index** on
  `(UserId, RoleId)`. Functionally many-to-many; structurally two one-to-many. No
  `UsingEntity` is used anywhere in the system.

## Cross-module logical references (no DB FK)

- `AuditLog.UserId?`, `AuditLog.ActorUserId?`, `AuditLog.ResourceId?` (+ `ResourceType`) —
  module-agnostic; may reference any module's resource.

## Notes / unclear

- **`Role` does NOT implement `IAggregateRoot`** (confirmed: `Role : AuditableEntity`),
  although it owns `UserRole`/`RoleClaim` collections. Reported as-is (possible inconsistency).
- `AuditLog` is an append-only log on `BaseEntity` (no soft-delete, no rowversion).
- `User.IsActive` is a derived/shadow column kept in sync with `LifecycleState`.
