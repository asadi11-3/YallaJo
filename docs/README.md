# YallaJo Engineering Docs

> Onboarding-first documentation for **new backend engineers** joining YallaJo.
> These docs **describe** the system as it exists today. They do not propose changes.

---

## What is YallaJo?

A **modular-monolith tourism & booking platform for Jordan**. Tourists discover and book tours/places; providers (tour operators, guides, hotels, activity centers, businesses) sell experiences; admins moderate; the system handles payments, payouts, notifications, SEO, analytics, and (planned) live tracking.

Stack at a glance: **.NET 8 · Minimal API · EF Core (SQL Server) · MediatR (CQRS) · FluentValidation · JWT Bearer · Serilog · OpenTelemetry · SignalR · Outbox/Inbox**.

---

## Reading order

### 1-hour track (just enough)
1. [`01-system-overview.md`](./01-system-overview.md) — what it is, who uses it, how it's shaped.
2. [`03-module-map.md`](./03-module-map.md) — the 14 modules at a glance.
3. [`workflows/06-booking-lifecycle.md`](./workflows/06-booking-lifecycle.md) — the core business flow.

### 1-day track (productive)
4. [`workflows/01-user-onboarding.md`](./workflows/01-user-onboarding.md) — registration + cross-module fan-out.
5. [`workflows/17-outbox-inbox-eventing.md`](./workflows/17-outbox-inbox-eventing.md) — how modules talk (mechanism).
6. [`eventing/integration-event-catalog.md`](./eventing/integration-event-catalog.md) — **who publishes / who consumes what** (Phase A: Booking, Finance, Identity).
7. [`risks/risk-register.md`](./risks/risk-register.md) — what's stubbed, what's risky.

### 1-week track (deep)
8. Existing references (do not duplicate here):
   - [`../AGENTS.md`](../AGENTS.md) — agent operating manual + reading order.
   - [`../Agents/agent-context.md`](../Agents/agent-context.md) — conventions, gotchas, build state.
   - [`../Agents/guide.md`](../Agents/guide.md) — code patterns bible.
   - [`../Agents/error-log.md`](../Agents/error-log.md) — prior mistakes (do not repeat).
   - [`../Agents/YallaJo.md`](../Agents/YallaJo.md) — full endpoint catalog + business rules.
   - [`../Agents/decisions/`](../Agents/decisions/) — ADR-001..008.

---

## Document index

| Doc | Purpose |
|---|---|
| [`01-system-overview.md`](./01-system-overview.md) | System purpose, stack, actors, integrations, high-level diagrams |
| [`03-module-map.md`](./03-module-map.md) | One-page card per module |
| [`workflows/README.md`](./workflows/README.md) | Workflow index + diagram conventions |
| [`workflows/01-user-onboarding.md`](./workflows/01-user-onboarding.md) | Registration → OTP → Profile creation |
| [`workflows/06-booking-lifecycle.md`](./workflows/06-booking-lifecycle.md) | SlotLock → Payment → Confirmation → Completion |
| [`workflows/17-outbox-inbox-eventing.md`](./workflows/17-outbox-inbox-eventing.md) | Outbox/Inbox mechanism (the *how*) |
| [`eventing/integration-event-catalog.md`](./eventing/integration-event-catalog.md) | Cross-module event catalog — Phase A: Booking · Finance · Identity (the *who & what*) |
| [`risks/risk-register.md`](./risks/risk-register.md) | Known stubs, gaps, and follow-ups |

> Workflows 02–05, 07–16 are planned but not in this pass. See `workflows/README.md` for the full inventory.

---

## Glossary (quick)

| Term | Meaning |
|---|---|
| **Module** | A bounded domain with its own DB schema and 5 layers (Domain / Application / Infrastructure / Presentation / Contracts) |
| **Aggregate root** | The only entity per cluster that may emit domain events (ADR-007) |
| **Domain event** | In-module event dispatched via MediatR after `SaveChanges` |
| **Integration event** | Cross-module event delivered via Outbox → Inbox (ADR-008) |
| **Outbox** | Per-module table that stages integration events for reliable delivery |
| **Inbox** | Per-module table that deduplicates consumed integration events |
| **SlotLock** | Time-limited (~10 min) reservation of seats on a tour while a user pays |
| **JoinRequest** | Request to be added to an existing group booking |
| **Payout** | Aggregated provider earnings paid out in a batch |
| **Provider** | A user that sells tours/services (Tour Operator, Independent Guide, Hotel/Resort, Activity Center) |
