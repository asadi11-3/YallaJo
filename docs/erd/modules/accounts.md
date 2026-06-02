# Accounts module — ERD

**DbContext:** `AccountsDbContext` · **Schema:** `accounts`

Manages user profiles, provider applications/documents, and agency affiliations.

## Entities

| Entity | Base | Aggregate root | 🗑 soft-delete | 🔒 rowversion |
|--------|------|----------------|---------------|--------------|
| `Profile` | AuditableEntity | ✅ | ✅ | ✅ |
| `ProviderApplication` | AuditableEntity | ✅ | ✅ | ✅ |
| `ProviderDocument` | BaseEntity | ❌ (child) | ❌ | ❌ |
| `AgencyApplication` | AuditableEntity | ✅ | ✅ | ✅ |
| `AgencyAffiliation` | AuditableEntity | ✅ | ✅ | ✅ |
| `AgencyInvitation` | AuditableEntity | ✅ | ✅ | ✅ |
| `OutboxMessage` / `InboxMessage` | infra | — | — | — |

## Diagram

```mermaid
erDiagram
    Profile {
        guid Id PK
        guid UserId LREF "→ Security.User (unique)"
        string DisplayName
        json MarketingConsent "◆ owned"
    }
    ProviderApplication {
        guid Id PK
        guid UserId LREF
        guid ReviewedByUserId LREF "nullable"
        string Status
    }
    ProviderDocument {
        guid Id PK
        guid ApplicationId FK
        string DocumentType
    }
    AgencyApplication {
        guid Id PK
        guid GuideUserId LREF
        guid AgencyUserId LREF
        string Status
    }
    AgencyAffiliation {
        guid Id PK
        guid AgencyUserId LREF
        guid GuideUserId LREF
        guid TerminatedByUserId LREF "nullable"
    }
    AgencyInvitation {
        guid Id PK
        guid AgencyUserId LREF
        guid GuideUserId LREF
    }

    ProviderApplication ||--o{ ProviderDocument : "Documents (Cascade)"
```

## Relationships

- **`ProviderApplication` 1—* `ProviderDocument`** — real FK `ApplicationId`, required, **Cascade**.

## Owned types

- **`Profile.MarketingConsent`** (`OwnsOne`, optional) → record `MarketingConsent(EmailDigest, PushNotifications, ReEngagementCampaigns, LastUpdatedUtc?)` → columns `MarketingConsent*`.

## Cross-module logical references (no DB FK)

- `Profile.UserId`, `ProviderApplication.UserId / ReviewedByUserId?`, all `Agency*.{GuideUserId, AgencyUserId, TerminatedByUserId?}` → **Security.User**.

## Notes / unclear

- `ProviderDocument` is the only non-auditable entity (no soft-delete, no rowversion); relies on Cascade from its parent.
- A filtered unique index `UX_ProviderApplications_UserId_Approved` exists in migration SQL (not in Fluent config).
