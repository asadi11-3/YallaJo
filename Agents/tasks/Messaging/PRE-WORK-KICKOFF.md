# Messaging — Pre-Work Kickoff Briefing

> Sprint window: 2026-11-29 → 2027-01-14 (Wave 6). Owner: Mohammad.

## What's already wired

- `IMessagingUnitOfWork` + `MessagingUnitOfWork` delegate. **🐛 CRITICAL FIX applied:** the previous implementation called `context.SaveChangesAsync()` directly, bypassing domain-event dispatch. The new delegate routes through `IUnitOfWork<MessagingDbContext>`. Verified by 0-error build + ADR-006.
- 6 aggregate roots: Notification (base class upgraded from `BaseEntity` → `AuditableEntity`), NotificationPreference, NotificationTemplate, DeviceToken, SupportTicket (already had), ChatBotConversation (already had).
- 14 domain events covering notifications×4, preferences×1, templates×2, device tokens×2, support tickets×4, ticket messages×1.
- 6 integration events registered as `messaging.{aggregate}.{action}.v1`.
- 6 repositories: INotificationRepository (+GetUnreadCountAsync, GetUnsentAsync), INotificationPreferenceRepository, INotificationTemplateRepository (+GetByCodeAsync), IDeviceTokenRepository, ISupportTicketRepository (+GetByIdWithMessagesAsync), IMessagingOutboxWriter.
- Service abstractions in `Messaging.Contracts/Services/`:
  - `INotificationDispatcher` (orchestrator) — `NoopNotificationDispatcher` stub.
  - `INotificationChannelStrategy` (keyed by channel — **no stub registered**, sprint adds per-channel strategies).
  - `INotificationTemplateRenderer` — `NoopNotificationTemplateRenderer` stub.
  - `IEmailSender` — `NoopEmailSender` stub.
- `MessagingFeatures` (7) + `MessagingPermissionCatalog` (18 perms — SupportOperations + ModerationTools groups).
- Test projects scaffolded.

## Day-0 sprint tasks (PW-9/PW-10 land here)

1. **PW-9 migration:** Add `NotificationDeliveryAttempts` table (audit trail for EmailSender retries). Columns: `Id, NotificationId, Channel, AttemptNumber, AttemptedAt, Success, ProviderMessageId?, Error?`. FK to Notification.
2. **PW-10 appsettings:** Add `Messaging` section to `appsettings.json` for SMTP host/port, BG service intervals, SignalR config. **Secrets via env vars only**.
3. Replace `NoopNotificationDispatcher` with the real fan-out implementation that resolves keyed `INotificationChannelStrategy` per `NotificationChannel` enum value.
4. Implement per-channel strategies (Email, Push, SignalR, SMS) and register with `AddKeyedScoped`.
5. Implement `NotificationHub` SignalR hub.
6. Replace `NoopNotificationTemplateRenderer` with Scriban/Handlebars-style template engine using `NotificationTemplate.BodyTemplate`.
7. Replace `NoopEmailSender` with real SMTP/SendGrid implementation that writes one row per attempt into `NotificationDeliveryAttempts`.

## Watchpoints

- `Notification.SentAt` is the canonical "delivered" timestamp — set inside the channel strategy AFTER successful send, BEFORE raising `NotificationSentDomainEvent`.
- `IMessagingInboxStore` + `InboxMessages` table already exist (from Wave 4 partial work) — keep using them; do not re-introduce a different inbox abstraction.
- SignalR groups: scope by `userId` so a single user with multiple tabs gets one broadcast.
