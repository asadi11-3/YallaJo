# Auth module — ERD

**DbContext:** `AuthDbContext` · **Schema:** `auth`

Authentication: devices, sessions, refresh tokens, OTPs, activation / password-reset
tokens, and external login providers.

## Entities

| Entity | Base | Aggregate root | 🗑 soft-delete | 🔒 rowversion |
|--------|------|----------------|---------------|--------------|
| `Device` | AuditableEntity | ✅ | ✅ | ✅ |
| `Session` | AuditableEntity | ✅ | ✅ | ✅ |
| `RefreshToken` | AuditableEntity | ✅ | ✅ | ✅ |
| `Otp` | AuditableEntity | ❌ | ✅ | ✅ |
| `ActivationToken` | AuditableEntity | ✅ | ✅ | ✅ |
| `PasswordResetToken` | AuditableEntity | ✅ | ✅ | ✅ |
| `ExternalProvider` | AuditableEntity | ✅ | ✅ | ✅ |
| `OutboxMessage` / `InboxMessage` | infra | — | — | — |

## Diagram

```mermaid
erDiagram
    Device {
        guid Id PK
        guid UserId LREF
        string DeviceIdentifier
    }
    Session {
        guid Id PK
        guid UserId LREF
        guid DeviceId FK
    }
    RefreshToken {
        guid Id PK
        guid UserId LREF
        guid SessionId FK
        guid ReplacedByTokenId "self LREF (no FK)"
    }
    Otp {
        guid Id PK
        guid UserId LREF
        string Code
    }
    ActivationToken {
        guid Id PK
        guid UserId LREF
    }
    PasswordResetToken {
        guid Id PK
        guid UserId LREF
    }
    ExternalProvider {
        guid Id PK
        guid UserId LREF
        string Provider
    }

    Device ||--o{ Session : "DeviceId (Cascade, navigationless)"
    Session ||--o{ RefreshToken : "SessionId (Cascade, navigationless)"
```

## Relationships

- **`Device` 1—* `Session`** — FK `DeviceId`, required, **Cascade**, navigationless (`HasOne<Device>().WithMany()`).
- **`Session` 1—* `RefreshToken`** — FK `SessionId`, required, **Cascade**, navigationless.

## Cross-module logical references (no DB FK)

- All entities carry `UserId` → **Security.User**.

## Notes / unclear

- `Otp` is **not** an aggregate root while the other token entities are (intentional asymmetry).
- `RefreshToken.ReplacedByTokenId?` is a logical self-reference (token rotation) — **not mapped** as a relationship.
- No owned types, no join tables, no many-to-many.
