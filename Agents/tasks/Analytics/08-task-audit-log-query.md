# TASK 6 — Audit Log Query API & Redaction

> **Owner:** Fadwa (Beginner→Intermediate) — **Hours:** 14h — **Hard deadline:** Sun **2027-02-21 17:00**
> **Earliest start:** Wed 2027-01-20
> **Endpoints:** 3 + ~6 audit-log inbox handlers

---

## 1. Endpoint list

| # | Method | Path | Auth |
|---|---|---|---|
| 1 | GET | `/api/v1/admin/audit-logs?filter...&cursor=...` | `MustHavePermission(AuditLog, Read)` |
| 2 | POST | `/api/v1/admin/audit-logs/{id}/redact` | `MustHavePermission(AuditLog, Redact)` |
| 3 | GET | `/api/v1/admin/audit-logs/export?from=&to=` | `MustHavePermission(AuditLog, Export)` |

---

## 2. AuditLog entity (append-only stream)

```csharp
public sealed class AuditLog : BaseEntity   // BIGINT PK (see PW-9)
{
    public override long Id { get; protected set; }            // override Guid → long
    public Guid? UserId { get; private set; }                    // null for system actions
    public string? Username { get; private set; }                // denormalized snapshot
    public AuditLogAction Action { get; private set; }
    public string? CustomActionName { get; private set; }        // when Action = Custom
    public string EntityType { get; private set; } = string.Empty;  // "Tour", "Booking", "Payment", etc.
    public Guid EntityId { get; private set; }
    public string? OldValue { get; private set; }                // JSON or redacted
    public string? NewValue { get; private set; }                // JSON or redacted
    public string? UserAgent { get; private set; }
    public string? IpAddressHash { get; private set; }           // /24 hash per A-R6
    public string? CorrelationId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public DateTime? RedactedAt { get; private set; }            // null = not redacted; populated by Redact endpoint or auto-redact
    public string? RedactionReason { get; private set; }
    public Guid? RedactedByUserId { get; private set; }

    private AuditLog() { }

    public static AuditLog Append(
        Guid? userId,
        string? username,
        AuditLogAction action,
        string? customActionName,
        string entityType,
        Guid entityId,
        string? oldValue,
        string? newValue,
        string? userAgent,
        string? ipAddressHash,
        string? correlationId,
        DateTime now,
        IAuditLogRedactor redactor)
    {
        var (redactedOld, _) = redactor.Redact(oldValue);
        var (redactedNew, _) = redactor.Redact(newValue);
        var entry = new AuditLog
        {
            UserId = userId,
            Username = username,
            Action = action,
            CustomActionName = customActionName,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = redactedOld,
            NewValue = redactedNew,
            UserAgent = userAgent,
            IpAddressHash = ipAddressHash,
            CorrelationId = correlationId,
            OccurredAt = now,
            RedactedAt = null
        };
        entry.RaiseDomainEvent(new AuditLogEntryAppendedDomainEvent(entry.Id, entry.UserId, entry.Action, entry.EntityType, entry.EntityId, now));
        return entry;
    }

    public void RetroactivelyRedact(Guid adminUserId, string reason, DateTime now, IAuditLogRedactor redactor)
    {
        if (RedactedAt is not null) throw new InvalidOperationException("Already redacted");
        var (redactedOld, oldFields) = redactor.Redact(OldValue);
        var (redactedNew, newFields) = redactor.Redact(NewValue);
        if (oldFields.Count == 0 && newFields.Count == 0)
            throw new InvalidOperationException("No sensitive fields detected");
        OldValue = redactedOld;
        NewValue = redactedNew;
        RedactedAt = now;
        RedactedByUserId = adminUserId;
        RedactionReason = reason;
        RaiseDomainEvent(new AuditLogEntryRedactedDomainEvent(Id, Action, EntityType, EntityId, oldFields.Concat(newFields).ToList()));
    }
}
```

---

## 3. Endpoint #1 — GET /admin/audit-logs

Filters:
- `entityType?` (e.g. "Booking", "Payment")
- `entityId?` (Guid)
- `userId?` (who performed it)
- `action?` (AuditLogAction enum)
- `from?` `to?` (default last 7 days)
- `ipAddressHash?` (hash matching — admin enters known fragment to investigate)
- `cursor?`, `pageSize?` (default 20, max 50)

Returns `{items, nextCursor, totalCount?}`. Total expensive — opt-in via `?countTotal=true`.

DTO:
```csharp
public sealed record AdminAuditLogDto(
    long Id,
    Guid? UserId,
    string? Username,
    AuditLogAction Action,
    string? CustomActionName,
    string EntityType,
    Guid EntityId,
    string? OldValue,
    string? NewValue,
    string? UserAgent,
    string? IpAddressHash,
    string? CorrelationId,
    DateTime OccurredAt,
    DateTime? RedactedAt
);
```

---

## 4. Endpoint #2 — POST /admin/audit-logs/{id}/redact

```csharp
public sealed record RedactAuditLogCommand(long Id, string Reason) : ICommand<Result<Unit>>;
```

Reason ≥ 20 chars mandatory. Calls `AuditLog.RetroactivelyRedact(currentUser.UserId, reason, now, redactor)`. Emits `analytics.audit-log.entry-redacted.v1` to alert other admins.

Error codes: `AuditLog.NotFound`, `AuditLog.AlreadyRedacted`, `AuditLog.RedactionNotApplicable`.

---

## 5. Endpoint #3 — GET /admin/audit-logs/export

CSV export for legal/compliance dump. Streams response with `Content-Type: text/csv; charset=utf-8`.

- Hard limit: 100K rows. > 100K → `AuditLog.ExportTooLarge` 422 (force narrower date range).
- Always includes RedactedAt + redaction status columns.
- Header row: `Id,UserId,Username,Action,EntityType,EntityId,OccurredAt,IpAddressHash,UserAgent,CorrelationId,RedactedAt,RedactionReason`.
- OldValue / NewValue **deliberately excluded** from export to prevent CSV-injection vectors (admin can view individual entries via #1 endpoint).

---

## 6. ~6 audit-log inbox handlers

Standard pattern: each handler builds an AuditLog row via `AuditLog.Append`, persists via repo, MarkAsProcessed inbox, single SaveChanges.

| Source event | Action | OldValue / NewValue |
|---|---|---|
| `auth.user.registered.v1` | Create | null / `{ "userId": ..., "emailHash": ... }` |
| `finance.payment.completed.v1` | Create | null / `{ "paymentId": ..., "amount": ..., "currency": ... }` (no card data — already redacted upstream) |
| `finance.payout.completed.v1` | Update | `{ "status": "Pending" }` / `{ "status": "Completed" }` |
| `finance.refund.completed.v1` | Refund | `{ "amount": ... }` / `{ "amount": ..., "status": "Completed" }` |
| `accounts.provider.status-changed.v1` | Approve/Reject (depends on transition) | `{ "oldStatus": ... }` / `{ "newStatus": ... }` |
| `booking.tour-booking.cancelled.v1` | Cancel | `{ "status": "Confirmed" }` / `{ "status": "Cancelled", "reason": ... }` |

Each handler reads `IClientContextProvider` for UserAgent + IpAddressHash (likely nulls when running in BG service inbox context — acceptable).

---

## 7. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | AuditLog aggregate + EF config + migration `AnalyticsAddAuditLogColumnsAndIndexes` | 2 | 2027-02-16 |
| 2 | IAuditLogRedactor abstraction in SharedKernel + impl + unit tests | 3 | 2027-02-17 |
| 3 | GET /admin/audit-logs handler + cursor + integration test | 3 | 2027-02-19 |
| 4 | POST /admin/audit-logs/{id}/redact handler + integration test | 2 | 2027-02-20 |
| 5 | GET /admin/audit-logs/export streaming CSV | 1 | 2027-02-20 |
| 6 | 6 inbox handlers + integration tests | 2 | 2027-02-21 |
| 7 | Documentation: redaction policy in 10-cross-cutting.md | 1 | 2027-02-21 |
| **Total** | | **14h** | **Sun 2027-02-21** |

---

## 8. Acceptance tests (12+)

1. GET no filters → last 7 days.
2. GET filter entityType=Booking → only booking rows.
3. GET filter userId=X → only that user's actions.
4. POST redact valid row → 200, RedactedAt set, outbox event emitted.
5. POST redact already-redacted → 409 AlreadyRedacted.
6. POST redact row with no sensitive data → 422 RedactionNotApplicable.
7. POST redact with reason < 20 chars → 422.
8. GET export 1-day → 200 with CSV stream, header row correct.
9. GET export 2-year span → 422 ExportTooLarge (force narrow).
10. Inbox auth.user.registered → AuditLog row appears with redacted email.
11. Inbox finance.payment.completed → AuditLog row appears, no card data leaks.
12. Inbox same event twice → no duplicate AuditLog row (HasBeenProcessedAsync guard).
13. Inbox finance.payout.completed → existing PaymentSnapshot.AuditLogId stamped (cross-link for forensics).
