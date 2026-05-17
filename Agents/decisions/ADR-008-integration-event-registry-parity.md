# ADR-008 — Integration Event Type Registry Parity

- **Status:** Accepted
- **Date:** 2026-06-01
- **Context:** Pre-Work execution for Wave 5/6 modules

## Context

`OutboxMessage.Type` stores a stable string key (e.g. `booking.tour-booking.created.v1`) rather than a CLR full type name. Resolving the key back to a CLR `Type` requires a single source of truth: `YallaJo.SharedKernel.Infrastructure.Abstractions.Integration.IntegrationEventTypeRegistry`. The registry exposes a static `IReadOnlyDictionary<string, Type> NameToType` populated at compile time, plus `GetName(Type)` used by `OutboxMessage.Create(IIntegrationEvent)`.

Risk if the registry drifts:

- An event published by Module A but absent from the registry will throw at outbox write time.
- An event registered but with a misspelled key will silently round-trip as a different event (inbox handler mismatch).
- The cross-module event registry is the only artefact that proves Module B can actually consume what Module A emits.

## Decision

1. **Every integration event record** declared in `{Module}.Contracts/IntegrationEvents/` is registered in `IntegrationEventTypeRegistry` with a key of the form `{module}.{aggregate-kebab}.{action}.v1`.
2. The registry's parent project, `YallaJo.SharedKernel.Infrastructure.csproj`, takes a `<ProjectReference>` to every `{Module}.Contracts.csproj`. This is the **only** project that may reference all modules' Contracts.
3. Sprint kickoff parity test: each module's IntegrationTests project must include a `IntegrationEventRegistryParityTest` that asserts every `IIntegrationEvent` in its assembly is registered.

## Wave 5/6 Registry Additions (36 keys across 5 modules)

| Module | Count | Examples |
|---|---|---|
| Booking | 12 | `booking.tour-booking.created.v1`, `booking.slot-lock.released.v1`, `booking.join-request.approved.v1`, `booking.provider-document.expiring.v1` |
| Finance | 10 | `finance.payment.succeeded.v1`, `finance.payout.processed.v1`, `finance.invoice.paid.v1`, `finance.dispute.opened.v1`, `finance.subscription.cancelled.v1` |
| Social | 5 | `social.review.created.v1`, `social.review.deleted.v1`, `social.favorite.added.v1`, `social.report.created.v1`, `social.content.hidden.v1` |
| Messaging | 6 | `messaging.notification.delivered.v1`, `messaging.notification.failed.v1`, `messaging.device-token.registered.v1`, `messaging.support-ticket.opened.v1`, `messaging.support-ticket.resolved.v1`, `messaging.notification-template.updated.v1` |
| Analytics | 3 | `analytics.popularity.refreshed.v1`, `analytics.recommendation-cache.expired.v1`, `analytics.audit.exported.v1` |

Total registry size after Wave 5/6 Pre-Work: **~76 events across 11 modules**.

## Consequences

✅ Outbox writers (one per module, e.g. `BookingOutboxWriter`) can rely on `OutboxMessage.Create(integrationEvent)` to look up the canonical key — they do not invent strings.
✅ Inbox handlers in consumer modules deserialize against the registered type — version drift (e.g. `v1` → `v2`) is explicit via a new key, never silent.
✅ The `<ProjectReference>` graph keeps the registry source-of-truth — adding a Contracts project that is not referenced will fail to compile the registry update.

## Verification

- Build: `dotnet build YallaJo.SharedKernel.Infrastructure.csproj` will fail if a `using {Module}.Contracts.IntegrationEvents;` references a type that is not exposed from the referenced Contracts project.
- Runtime: `OutboxMessage.Create(integrationEvent)` throws if `GetName(eventType)` returns null — i.e. event not registered.
- Per-module test (added during sprint kickoff): reflectively scan `{Module}.Contracts.dll` for `IIntegrationEvent`-implementing records and assert each is in `IntegrationEventTypeRegistry.NameToType.Values`.
