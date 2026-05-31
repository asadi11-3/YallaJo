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

Module → module hops cross the **Outbox / Inbox boundary** (see [`17-outbox-inbox-eventing.md`](./17-outbox-inbox-eventing.md) for the *mechanism* and [`../eventing/integration-event-catalog.md`](../eventing/integration-event-catalog.md) for the *who publishes/consumes what*).

---

## Workflow inventory

> Only the **bold** workflows exist in this first documentation pass.

| # | Workflow | File | Status |
|---|---|---|---|
| 1 | **User onboarding (register → OTP → profile)** | [`01-user-onboarding.md`](./01-user-onboarding.md) | implemented / documented |
| 2 | **Authentication & session lifecycle** | [`02-authentication-session.md`](./02-authentication-session.md) | implemented / documented |
| 3 | **Password reset** | [`03-password-reset.md`](./03-password-reset.md) | implemented / documented |
| 4 | **Provider onboarding & document lifecycle** | [`04-provider-onboarding.md`](./04-provider-onboarding.md) | implemented / documented |
| 5 | **Tour authoring & approval** | [`05-tour-authoring-approval.md`](./05-tour-authoring-approval.md) | implemented / documented |
| 6 | **Booking lifecycle** | [`06-booking-lifecycle.md`](./06-booking-lifecycle.md) | implemented / documented |
| 7 | **Payment & refund** | [`07-payment-refund.md`](./07-payment-refund.md) | implemented / documented |
| 8 | **Payout & commission** | [`08-payout-commission.md`](./08-payout-commission.md) | implemented / documented |
| 9 | **Invoicing** | [`09-invoicing.md`](./09-invoicing.md) | implemented / documented |
| 10 | **Notifications fan-out** | [`10-notifications-fanout.md`](./10-notifications-fanout.md) | implemented / documented |
| 11 | **Support tickets** | [`11-support-tickets.md`](./11-support-tickets.md) | implemented / documented |
| 12 | **Content translation pipeline** | [`12-content-translation-pipeline.md`](./12-content-translation-pipeline.md) | implemented / documented |
| 13 | **SEO sitemap & redirects** | [`13-seo-sitemap-redirects.md`](./13-seo-sitemap-redirects.md) | implemented / documented |
| 14 | **Reviews & moderation** | [`14-reviews-moderation.md`](./14-reviews-moderation.md) | implemented / documented |
| 15 | **Blog publishing & comments** | [`15-blog-publishing-comments.md`](./15-blog-publishing-comments.md) | implemented / documented |
| 16 | **Analytics & popularity** | [`16-analytics-popularity.md`](./16-analytics-popularity.md) | implemented / documented |
| 17 | **Outbox/Inbox eventing (mechanism)** | [`17-outbox-inbox-eventing.md`](./17-outbox-inbox-eventing.md) | implemented / documented |
| 18 | **Agency roster & affiliations** | [`18-agency-roster.md`](./18-agency-roster.md) | implemented / documented |
