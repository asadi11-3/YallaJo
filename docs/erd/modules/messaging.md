# Messaging module — ERD

**DbContext:** `MessagingDbContext` · **Schema:** `messaging`

Notifications, delivery attempts, templates, device tokens, support tickets/messages, the
admin assignment roster, and a user snapshot.

## Entities

| Entity | Base | Aggregate root | 🗑 | 🔒 |
|--------|------|----------------|----|----|
| `Notification` | AuditableEntity | ✅ | ❌* | ❌* |
| `NotificationPreference` | AuditableEntity | ✅ | ✅ | ✅ |
| `NotificationTemplate` | AuditableEntity | ✅ | ✅ | ✅ |
| `NotificationDeliveryAttempt` | BaseEntity | ❌ | ❌ | ❌ |
| `DeviceToken` | AuditableEntity | ✅ | ✅ | ✅ |
| `SupportTicket` | AuditableEntity | ✅ | ✅ | ✅ |
| `TicketMessage` | BaseEntity | ❌ | parent | ❌ |
| `UserSnapshot` 📸 | BaseEntity | ❌ | ❌ | ❌ |
| `AdminAssignmentRoster` | BaseEntity | ❌ | ❌ | ❌ |

\* `Notification` is `AuditableEntity` but its config **omits** the soft-delete filter and
rowversion mapping — see Limitations.

## Diagram

```mermaid
erDiagram
    Notification {
        guid Id PK
        guid UserId LREF
        string Channel
        string EntityType
        guid EntityId LREF "nullable, polymorphic"
    }
    NotificationPreference {
        guid Id PK
        guid UserId LREF
    }
    NotificationTemplate {
        guid Id PK
        string Key
    }
    NotificationDeliveryAttempt {
        guid Id PK
        guid NotificationId LREF "no FK"
        string Status
    }
    DeviceToken {
        guid Id PK
        guid UserId LREF
        string Token
    }
    SupportTicket {
        guid Id PK
        guid CreatedByUserId LREF
        guid AssignedToUserId LREF "nullable"
        string Status
    }
    TicketMessage {
        guid Id PK
        guid TicketId FK
        guid AuthorUserId LREF
        string Body
    }
    UserSnapshot {
        guid Id PK
        guid UserId LREF
        string Email
        string LanguageCode
    }
    AdminAssignmentRoster {
        guid Id PK
        guid AdminUserId LREF
    }

    SupportTicket ||--o{ TicketMessage : "TicketId (Cascade)"
```

## Relationships (real FK, intra-module)

- **`SupportTicket` 1—* `TicketMessage`** — FK `TicketId`, required, Cascade.

## Snapshots (📸)

- `UserSnapshot` — local copy of user email/name/language (from Auth/Accounts) for rendering.

## Cross-module logical references (no DB FK)

- `UserId` (and assign/resolve variants), polymorphic `Notification.EntityType` (string) + `EntityId?`.

## Notes / unclear

- **`Notification`** inherits `AuditableEntity` but its config does not map IsDeleted/DeletedAt/
  RowVersion or add a query filter → not soft-delete-filtered or concurrency-protected at DB level.
- `NotificationDeliveryAttempt.NotificationId` is intra-module but **FK-less**.
- `ChatBotConversation`/`ChatBotMessage` are deferred (not mapped).
