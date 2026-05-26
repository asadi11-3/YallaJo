# Messaging Module — Fix Plan

> **Created**: 2025-01-27
> **Source**: `Agents/Plans/Messaging-Audit-Report.md` (Score 7.5/10)
> **Estimated Effort**: 6-8 hours

---

## Design Decisions

1. **Validators**: Use FluentValidation with `AbstractValidator<T>` pattern — same as all other modules
2. **HybridCache**: Add to high-traffic queries (GetMyNotifications, GetUnreadCount, GetMyPreferences)
3. **SMS**: Create `ISmsSender` interface + NoOp implementation; real provider deferred to deployment config
4. **SLA monitoring**: Background service that checks overdue tickets, publishes alerts
5. **Digest**: Separate background service collects unread notifications and sends summary

---

## Fixes

### Fix 1 (HIGH): Add 14 command validators

Create validators for all commands that accept user input:

| Command | Validation Rules |
|---|---|
| CreateSupportTicketCommand | Subject NotEmpty+MaxLen(200), Body NotEmpty+MaxLen(4000), Priority valid enum |
| PostTicketMessageCommand | TicketId NotEmpty, Content NotEmpty+MaxLen(4000) |
| CloseTicketCommand | TicketId NotEmpty |
| AssignTicketCommand | TicketId NotEmpty, AgentUserId NotEmpty |
| EscalateTicketCommand | TicketId NotEmpty, Reason MaxLen(500) |
| MarkNotificationReadCommand | NotificationId NotEmpty |
| BatchMarkReadCommand | NotificationIds NotNull+NotEmpty |
| BatchDeleteNotificationsCommand | NotificationIds NotNull+NotEmpty |
| RegisterDeviceTokenCommand | Token NotEmpty+MaxLen(500), Platform valid enum |
| DeleteDeviceTokenCommand | TokenId NotEmpty |
| UpdateNotificationPreferenceCommand | PreferenceId NotEmpty, Channel valid enum |
| CreateNotificationTemplateCommand | Name NotEmpty+MaxLen(200), SubjectTemplate NotEmpty, BodyTemplate NotEmpty |
| UpdateNotificationTemplateCommand | TemplateId NotEmpty, Name NotEmpty+MaxLen(200) |
| DeleteNotificationTemplateCommand | TemplateId NotEmpty |

**Files**: 14 new validator files in `Messaging.Application/Commands/{Feature}/`

---

### Fix 2 (MEDIUM): Add HybridCache to high-traffic queries

| Query | Cache Key | TTL | Tags |
|---|---|---|---|
| GetMyNotifications | `messaging:notifications:{userId}:{page}` | 30s | `notifications:{userId}` |
| GetUnreadCount | `messaging:unread-count:{userId}` | 15s | `notifications:{userId}` |
| GetMyPreferences | `messaging:preferences:{userId}` | 5min | `preferences:{userId}` |
| GetSupportTickets | `messaging:tickets:{userId}:{status}:{page}` | 30s | `tickets:{userId}` |

**Files**: 4 query handler modifications + 1 new `MessagingCacheKeys.cs`

---

### Fix 3 (MEDIUM): Create SMS sender interface + NoOp

```
ISmsSender → NoOpSmsSender (logs "SMS not configured")
```

**Files**: 2 new files + DI registration
- `Messaging.Application/Interfaces/ISmsSender.cs`
- `Messaging.Infrastructure/Channels/NoOpSmsSender.cs`
- Modify `DependencyInjection.cs`

---

### Fix 4 (MEDIUM): SLA monitoring background service

- Periodic check (every 5 min) for tickets past SLA deadline
- Publishes `TicketSlaBreachedIntegrationEvent` to outbox
- Uses `ISupportTicketRepository.GetOverdueSlaTicketsAsync(DateTime threshold)`

**Files**: 3 new files
- `Messaging.Infrastructure/BackgroundServices/SlaMonitoringService.cs`
- `Messaging.Contracts/IntegrationEvents/TicketSlaBreachedIntegrationEvent.cs`
- Modify `ISupportTicketRepository.cs` + `SupportTicketRepository.cs`

---

### Fix 5 (LOW): Digest notification service

- Daily background service (configurable)
- Collects unread in-app notifications older than threshold
- Sends single digest email per user with summary

**Files**: 2 new files
- `Messaging.Infrastructure/BackgroundServices/NotificationDigestService.cs`
- Options class

---

### Fix 6 (LOW): Add TourGuide notification types

Extend `NotificationType` enum with:
- GuideApplicationSubmitted
- GuideApplicationApproved
- GuideApplicationRejected
- TourProposalApproved
- TourProposalRejected
- GuideOfferingSuspended

**Files**: 1 enum modification

---

### Fix 7 (LOW): Update plan document

- Update endpoint counts (23 not plan estimate)
- Update handler counts (53 event handlers)
- Mark implemented sections
- Add Implementation Notes

**Files**: 1 plan doc

---

### Fix 8 (MEDIUM): Build verify

`dotnet build YallaJo.sln --no-restore`

---

## Execution Order

```
Fix 1 (validators) ──────┐
Fix 2 (HybridCache) ─────┤
Fix 3 (SMS interface) ────┼── Fix 8 (build verify) → Fix 7 (doc update)
Fix 4 (SLA monitoring) ───┤
Fix 5 (digest service) ───┤
Fix 6 (notification types)┘
```

All fixes are independent and parallelizable except Fix 8 (gates on all code changes).

---

## Risk Assessment

| Risk | Mitigation |
|---|---|
| Cache invalidation on notification CRUD | Tag-based invalidation on write handlers |
| SLA service false positives on clock skew | Use UTC consistently + configurable grace period |
| SMS interface changes later | NoOp is zero-impact; real impl swaps via DI |
