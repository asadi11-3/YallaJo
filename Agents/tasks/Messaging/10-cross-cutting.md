# Messaging — Cross-Cutting Concerns

> Mirrors §S7 of predecessor sprints. Tech Lead enforces during PR review and at integration freeze Sun 2027-01-10 17:00.

---

## 1. DI Audit

`Messaging.Infrastructure/DependencyInjection.cs` MUST register:

| Registration | Symbol | Lifetime | Reason |
|---|---|---|---|
| DbContext factory | `IDbContextFactory<MessagingDbContext>` | Singleton | BG services + inbox/outbox processors |
| Pooled DbContext | `MessagingDbContext` via `AddDbContextPool` | Scoped | Per-request handlers |
| Unit of Work | `IMessagingUnitOfWork → MessagingUnitOfWork` | Scoped | Delegates to SharedKernel UoW (PW-1) |
| Inbox store | `IMessagingInboxStore → MessagingInboxStore` | Scoped | Idempotency for cross-module events |
| Outbox writer | `IMessagingOutboxWriter → MessagingOutboxWriter` | Scoped | Enqueue messaging.* integration events |
| Repositories (8) | INotificationRepository, INotificationPreferenceRepository, INotificationTemplateRepository, INotificationDeliveryAttemptRepository, IDeviceTokenRepository, ISupportTicketRepository, IAdminAssignmentRosterRepository, IUserSnapshotRepository | Scoped | One `EfXxxRepository` each |
| `INotificationDispatcher → NotificationDispatcher` | Scoped | Resolves channel strategy per dispatch |
| `INotificationChannelStrategy` (keyed) | Scoped × 3 keys | InApp/Email/Push strategies (PW-6) |
| `INotificationTemplateRenderer → MustacheNotificationTemplateRenderer` | Scoped | Stubble-backed |
| `IEmailSender → SmtpEmailSender` | Scoped | MailKit wrapper |
| `IAdminAssignmentService → RoundRobinAdminAssignmentService` | Scoped | T4 |
| `IProviderResolverService → ProviderResolverService` | Scoped | T2 hub group routing |
| `SignalR` | – | – | `services.AddSignalR(...)` per T2 |
| 2 BackgroundService instances | (T5 + T6) | Singleton | `AddHostedService<T>` LIFO |
| Permission catalog | `IPermissionCatalog → MessagingPermissionCatalog` | Singleton | Auto-discovered by PermissionSeeder |
| MediatR | – | per-call | `RegisterServicesFromAssembly(typeof(MessagingApplicationMarker).Assembly)` |
| FluentValidation | – | Scoped | `AddValidatorsFromAssembly(typeof(MessagingApplicationMarker).Assembly, includeInternalTypes: true)` |
| Diagnostics | `MessagingDiagnostics` static | – | ActivitySource + Meter |
| Cache keys | `IMessagingCacheKeys → MessagingCacheKeys` | Singleton | Centralizes tag formats |

**Common mistakes to fail-PR on:**
- ❌ Forgetting to register `IPermissionCatalog` → permissions absent from DB after seeder boot.
- ❌ Registering `INotificationDispatcher` as Singleton (state leaks).
- ❌ Registering Stubble's `IStubbleRenderer` as Scoped without `IStubbleRendererBuilder` building once (perf — builder reuse expected).
- ❌ Forgetting `services.AddSignalR()` call → hub mapping crashes at startup with NRE.
- ❌ NOT registering `INotificationChannelStrategy` as keyed → dispatcher can't resolve.

---

## 2. Permission Seeder Verification

Expected boot log after this sprint merges (Messaging is the 10th catalog):

```text
[INFO] PermissionSeeder discovered 10 catalogs: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, Booking, Finance, Social, Messaging
[INFO] PermissionSeeder inserted/verified 18 Messaging permissions
```

Verify: `SELECT COUNT(*) FROM security.Permissions WHERE Feature LIKE 'Messaging.%'` → **18**.

---

## 3. Outbox Type-Registry Validation

`tests/Messaging.IntegrationTests/Outbox/IntegrationEventTypeRegistryParityTests.cs`:

```csharp
[Fact]
public void All_Messaging_integration_event_records_registered_in_registry()
{
    var asm = typeof(NotificationDeliveredIntegrationEvent).Assembly;
    var declared = asm.GetTypes()
        .Where(t => t.IsAssignableTo(typeof(IIntegrationEvent)) && !t.IsAbstract)
        .ToHashSet();

    var registered = IntegrationEventTypeRegistry.All
        .Where(kvp => kvp.Key.StartsWith("messaging."))
        .Select(kvp => kvp.Value)
        .ToHashSet();

    declared.Should().BeEquivalentTo(registered);
}
```

**Logical names expected (6 emitted):**
```
messaging.notification.delivered.v1
messaging.notification.failed.v1
messaging.ticket.created.v1
messaging.ticket.assigned.v1
messaging.ticket.resolved.v1
messaging.support-sla-breached.v1   (deferred — registered but no producer yet)
```

**Inbox consumed (~29 logical names — see 03-entities-matrix.md §5)** from auth/accounts/content-places/booking/finance/social.

---

## 4. Build Lock Workaround

Same as Booking/Finance/Social — `YallaJo.Web.exe` lock blocks full-solution builds.

```powershell
dotnet build Messaging/Messaging.Domain/Messaging.Domain.csproj
dotnet build Messaging/Messaging.Contracts/Messaging.Contracts.csproj
dotnet build Messaging/Messaging.Application/Messaging.Application.csproj
dotnet build Messaging/Messaging.Infrastructure/Messaging.Infrastructure.csproj
dotnet build Messaging/Messaging.Presentation/Messaging.Presentation.csproj
dotnet build tests/Messaging.Tests.Unit/Messaging.Tests.Unit.csproj
dotnet build tests/Messaging.IntegrationTests/Messaging.IntegrationTests.csproj
```

---

## 5. Migration Sequence

Apply in this exact order (squash forbidden):

| # | Name | Owner | Task |
|---|---|---|---|
| 1 | `MessagingAddAggregateRootAndAuditMembers` | Tech Lead | PW-2 (possibly empty) |
| 2 | `MessagingAddNotificationDeliveryAttempts` | Tech Lead | PW-9 |
| 3 | `MessagingAddUserSnapshot` | Mohammad | T5 (added in PW-6 sibling) |
| 4 | `MessagingAddNotificationIndexes` | Mahmoud | T1 |
| 5 | `MessagingAddAdminAssignmentRoster` | Fadwa | T4 |
| 6 | `MessagingAddDeviceTokenUniqueIndex` | Fadwa | T3 |
| 7 | `MessagingAddSupportTicketSlaIndex` | Fadwa | T4 |
| 8 | `MessagingSeedDefaultNotificationTemplates` | Junior | T7 |

---

## 6. Inbox / Outbox Hygiene

- `CompositeOutboxProcessor` (existing in YallaJo.Api) auto-picks up MessagingDbContext.
- `OutboxCleaner` deletes processed outbox rows > 7 days. No Messaging-specific config.
- `InboxCleaner` deletes processed inbox rows > 30 days.
- **Alerting:**
  - `messaging.OutboxMessages WHERE ProcessedAt IS NULL AND CreatedAt < now - 5min > 100 rows` → page on-call.
  - `messaging.notification.failed.v1` events in last hour > 10 → email admin immediately (email service is failing; likely SMTP outage).
  - SignalR `connections_active` Grafana panel: alert if drops > 30% in 5 min (deployment/crash).

---

## 7. SignalR Scaling Notes

**v1 single-instance** assumption — in-memory backplane suffices up to ~10K concurrent connections. Document this in `Agents/decisions/ADR-XXX-signalr-scaleout.md` if not already.

**When YallaJo scales to multi-instance:**
1. Add `Microsoft.Azure.SignalR` NuGet (~$30/mo for 1K units).
2. Replace `services.AddSignalR(...)` with `services.AddSignalR(...).AddAzureSignalR(...);`.
3. Move JWT validation from JwtBearerHandler to Azure SignalR's NegotiateAsync flow (config-only change).
4. Test: deploy 2 API instances + 1 client connected to instance A → broadcast from instance B → client receives.

Alternative: **Redis backplane** (cheaper for moderate scale, free Redis). Add `Microsoft.AspNetCore.SignalR.StackExchangeRedis`.

**Out of scope this sprint** — pure single-instance acceptance.

---

## 8. Email Reliability Patterns

- **SMTP retry exponential backoff** within `IEmailSender` impl (3 attempts inside a single sync call) plus **outer BG service retry** (3 more PeriodicTimer ticks) = **up to 9 total attempts** before permanent fail. Documented to avoid surprise.
- **Bounce handling:** v1 does NOT process SMTP bounce notifications (DSN). v2 may add Amazon SES bounce-handler endpoint or VERP. **Permanent bounces → DeviceToken.Email field marks bounce → user gets in-app prompt to update.** Out of scope this sprint.
- **Rate limiting:** SMTP server caps ~10 emails/sec. EmailSender batch size 50/tick × 30sec tick = 100/min effective ceiling. Safe well under typical SMTP limits.

---

## 9. Secrets Inventory

ALL env vars (KeyVault prod), NEVER appsettings.json:

| Key | Where used | Notes |
|---|---|---|
| `Messaging__Email__Smtp__Username` | SmtpEmailSender | Gmail account / SendGrid login |
| `Messaging__Email__Smtp__Password` | SmtpEmailSender | **STRIP SPACES** from Gmail app pwd per Gotcha #18 (agent-context §9.1) |
| `Messaging__SignalR__AzureConnectionString` | (Phase 3 multi-instance) | NOT used in v1 |

`appsettings.json` has empty strings as placeholders so config binding doesn't crash on load — actual values from env.

---

## 10. Performance Budget

| Operation | Target |
|---|---|
| `POST /notifications/{id}/read` p95 | < 80ms (cached unread count rebuild ~30ms) |
| `GET /notifications?...` p95 | < 120ms (cached) |
| `GET /notifications/unread-count` p95 | < 50ms (cached 15s) |
| `POST /support/tickets` p95 | < 200ms (incl. auto-assign roundtrip) |
| `EmailNotificationSender` per-tick (50 emails) | < 30s (60% headroom for next tick) |
| `ReadNotificationCleanupService` weekly run (1M total notifications) | < 5min |
| SignalR push delivery (server → client) | < 500ms p95 |
| SignalR concurrent connections | 10K (single-instance limit) |

---

## 11. Cross-Module Coupling Risks

| Risk | Mitigation |
|---|---|
| Messaging is downstream of EVERY module — outage in upstream blocks notifications | Inbox queue absorbs; users see stale state but no data loss |
| `UserSnapshot` table can drift from Accounts.UserProfile | Periodic reconciliation BG job (Phase 3); v1 trusts `auth.user.registered.v1` + edge cases get hand-fixed |
| SignalR hub down → critical notifications missed | Falls back to email (M-R1 forces email channel for critical types) |
| Round-robin roster empty (no admins) → tickets stuck Open | Admin alert via Slack webhook; manual reassign endpoint exists |
| Stubble template parse error at render time → notification fails to dispatch | Renderer catches, logs error, falls back to last-resort inline string (M-R4) — notification still delivered with degraded text |

---

## 12. Folder Migration on Sprint Close

When acceptance gate (99-acceptance-gate.md) is signed:

```powershell
Move-Item -LiteralPath "Agents\tasks\Messaging" -Destination "Agents\decisions\closed\Messaging"
```

Update master `Phase1-Phase2-Completion-INDEX.md §1` Messaging row: 🟡 → ✅, link to closed/ folder. Update `agent-context.md §11.1` Messaging row from "🟡 Partial" → "✅ Complete (Phase 2)".
