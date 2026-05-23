# Workflows

Each workflow doc follows the same template:

1. **Trigger** — what kicks it off (endpoint, event, schedule).
2. **Actors** — who/what participates.
3. **Sequence diagram** — Mermaid `sequenceDiagram`.
4. **State diagram** — Mermaid `stateDiagram-v2` where a state machine applies.
5. **Side effects** — emitted events + consumers.
6. **Failure / timeout paths** — what can go wrong and how the system reacts.
7. **Code references** — file paths (no copy-paste).
8. **Related risks** — links into [`../risks/risk-register.md`](../risks/risk-register.md).

---

## Diagram conventions

| Element | Meaning |
|---|---|
| `participant X as ModuleX` | A module (logical, all run in-process) |
| `participant DB as SQL` | Persistence (per-module schema) |
| `participant OB as Outbox` | Per-module outbox table |
| `participant IB as Inbox` | Per-module inbox table |
| Solid arrow `->>` | Synchronous in-process call |
| Dashed arrow `-->>` | Response / return |
| Note over X | Side effect or state transition |
| Dashed self-arrow | Async dispatch via MediatR |

Module → module hops cross the **Outbox / Inbox boundary** (see [`17-outbox-inbox-eventing.md`](./17-outbox-inbox-eventing.md)).

---

## Workflow inventory

> Only the **bold** workflows exist in this first documentation pass.

| # | Workflow | File | Status |
|---|---|---|---|
| 1 | **User onboarding (register → OTP → profile)** | [`01-user-onboarding.md`](./01-user-onboarding.md) | ✅ in this pass |
| 2 | Authentication & session lifecycle | `02-authentication-session.md` | planned |
| 3 | Password reset | `03-password-reset.md` | planned |
| 4 | Provider onboarding & document lifecycle | `04-provider-onboarding.md` | planned |
| 5 | Tour authoring & approval | `05-tour-authoring-approval.md` | planned |
| 6 | **Booking lifecycle** | [`06-booking-lifecycle.md`](./06-booking-lifecycle.md) | ✅ in this pass |
| 7 | Payment & refund | `07-payment-refund.md` | planned |
| 8 | Payout & commission | `08-payout-commission.md` | planned |
| 9 | Invoicing | `09-invoicing.md` | planned |
| 10 | Notifications fan-out | `10-notifications-fanout.md` | planned |
| 11 | Support tickets | `11-support-tickets.md` | planned |
| 12 | Content translation pipeline | `12-content-translation-pipeline.md` | planned |
| 13 | SEO sitemap & redirects | `13-seo-sitemap-redirects.md` | planned |
| 14 | Reviews & moderation | `14-reviews-moderation.md` | planned |
| 15 | Blog publishing & comments | `15-blog-publishing-comments.md` | planned |
| 16 | Analytics & popularity | `16-analytics-popularity.md` | planned |
| 17 | **Outbox/Inbox eventing (mechanism)** | [`17-outbox-inbox-eventing.md`](./17-outbox-inbox-eventing.md) | ✅ in this pass |
