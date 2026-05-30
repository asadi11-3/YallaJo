# 00 — Introduction, Purpose & Scope

> **What this document is.** This is the entry point to the YallaJo engineering
> documentation. It defines **what YallaJo is**, **why it exists**, and **what is and is not in
> scope** for the system as it stands today.
>
> **Basis.** Every statement here is derived from the **current implementation** and the
> **post-P0/P1 authorization model** — not from earlier drafts. It frames the system at a high
> level and links forward to the detailed docs; it does not duplicate their content.

---

## 1. Introduction

YallaJo is a **tourism and experiences marketplace for Jordan**. It connects **Travelers** with
the people and organizations that supply local experiences — **Providers**, **TourGuides**,
**Agencies**, **Businesses**, and **Content Creators** — and supports the full marketplace
journey:

> **Discover → Book → Pay → Fulfill → Review → Payout**

Travelers discover tours, places, guides, and editorial content; book and pay for experiences;
have them fulfilled by providers; leave reviews; and providers are paid out for completed
bookings. Administrators moderate content, manage identities and finance, and operate the
platform.

### Architectural shape (summary)

YallaJo is a single-deployable **Modular Monolith** built on **.NET 9**. Its defining
architectural traits are:

- **Modular boundaries** — the system is decomposed into independent functional modules, each
  owning its own data and exposing cross-module capability only through contracts and events.
- **CQRS** — commands and queries are handled through a mediator pipeline, keeping write and read
  concerns separated.
- **Event-driven communication** — modules collaborate **asynchronously** via a transactional
  **Outbox/Inbox** mechanism (at-least-once delivery with consumer-side idempotency), rather than
  by reaching into each other's data.
- **SharedKernel** — a small set of cross-cutting building blocks (result types, CQRS contracts,
  the outbox/inbox infrastructure, domain-event dispatch, and authorization primitives) reused by
  every module.
- **JWT permission-claim authorization** — callers are authenticated with JWTs that carry
  fine-grained permission claims; endpoints are gated by those claims, complemented by runtime
  ownership guards.

> For the full technology stack, container/context diagrams, and per-module detail, see
> [`01-system-overview.md`](./01-system-overview.md). This document does not restate those.

### Audience & how to read

This documentation set is written for **backend engineers and technical stakeholders**. After
this document, read [`02-actors-and-roles.md`](./02-actors-and-roles.md) for the actor and
authorization model, then [`03-use-case-model.md`](./03-use-case-model.md) for the business
use-case model.

---

## 2. Purpose

### 2.1 The problem being solved

Discovering, booking, paying for, and fulfilling local tourism experiences in Jordan is
fragmented across disconnected channels. YallaJo provides a **single, moderated, multi-role
marketplace** that unifies discovery, booking, payment, fulfillment, reviews, and provider
payouts — with the governance (moderation, finance controls, auditing) needed to operate it
safely.

### 2.2 Value for Travelers

A single place to discover and search tours, places, guides, and editorial content; book and pay
for experiences; manage their own bookings, favorites, reviews, and notifications; and raise
refunds or disputes on their own bookings.

### 2.3 Value for Providers & TourGuides

Self-service onboarding and operation: apply and submit documents, publish and manage their own
tours/offerings and availability, run the lifecycle of bookings they own (confirm, complete,
reject) and handle join requests, manage agency rosters they own, and receive payouts for
completed work.

### 2.4 Value for Administrators

Platform governance: moderate providers, businesses, tours, blogs, and creators; moderate users
and content; manage roles, users, finance (payouts, commissions, KYC), analytics, and audit; with
the highest tiers (SuperAdmin, Owner) controlling user hard-deletion, operational outbox recovery,
and system settings.

### 2.5 Purpose of this documentation set

This set documents the system **as it is implemented today**. It is descriptive, not
aspirational: it records current capabilities, the actor/authorization model, business use cases,
core workflows, and known limitations, so engineers can understand and safely extend the system.
It intentionally avoids roadmap promises.

### 2.6 Relationship to the other foundation documents

| Document | Role |
|---|---|
| [`01-system-overview.md`](./01-system-overview.md) | Technology stack, architecture diagrams, and per-module detail. |
| [`02-actors-and-roles.md`](./02-actors-and-roles.md) | The actor catalog, role hierarchy, and the post-P0/P1 authorization model. |
| [`03-use-case-model.md`](./03-use-case-model.md) | Business use cases grouped into packages and mapped to actors. |

This document (`00`) frames all three: it states the *what*, *why*, and *scope*; they provide the
*how* and the *detail*.

---

## 3. Scope

### 3.1 In Scope

The system's capabilities are organized into the same business domains used by the use-case model
(see [`03-use-case-model.md`](./03-use-case-model.md)):

| Domain | In-scope capability (summary) |
|---|---|
| **Identity & Access** | Registration, OTP verification, login/refresh/logout, password reset, invitations, external login (Google/Facebook), session & device management. |
| **Tourism Discovery** | Browsing and searching tours, places, guides, and blogs; recommendations; SEO content; favorites/wishlist. |
| **Booking & Payments** | Booking a tour, managing own bookings, paying, refunds/disputes, and payment confirmation intake. |
| **Provider Operations** | Provider/guide onboarding, tour/slot/offering management, owner-scoped booking lifecycle, join-request handling, payouts, and agency-roster management. |
| **Content Management** | Blog authoring/publishing, tour proposals, reviews and replies, content reporting, and owner-scoped content deletion. |
| **Administration** | Moderation queues and approvals, user/content moderation, platform-wide deletion, finance administration, role/user management, and analytics/audit. |
| **System Operations** | Background processing (outbox dispatch, schedulers), operational outbox dead-letter recovery, and system settings. |

**Structural scope.** The system comprises **14 functional modules** — Auth, Security, Accounts,
ContentCore, ContentPlaces, ContentTours, ContentBlogs, ContentSeo, Booking, Finance, Messaging,
Social, Tracking, and Analytics — built on a shared **SharedKernel** (cross-cutting building
blocks) and exposed through a single API **host** that also provides an operational **Ops**
surface. Per-module responsibilities are detailed in
[`01-system-overview.md`](./01-system-overview.md) and the module map.

**Supported workflows (summary).** The following flows operate end-to-end today: user onboarding
(register → OTP → profile); the booking lifecycle (including owner-scoped provider
confirm/complete/reject with administrative override); payment and refund handling (against the
current payment gateway — see §3.3); provider onboarding, approval, and suspension fan-out;
content authoring and moderation; notification fan-out; SEO/sitemap maintenance; analytics and
popularity scoring; and the underlying Outbox/Inbox eventing mechanism. Documented workflows live
under [`workflows/`](./workflows/); cross-module event ownership is catalogued under
[`eventing/`](./eventing/).

**Authorization scope.** Access is governed by the permission-claim model with the post-P0/P1
outcomes — owner-scoped vs. administrative deletion (`DeleteOwn` / `DeleteAny`), Provider as a
real self-service role (provider self-service plus consumer capabilities), agency-roster
ownership, and operational outbox permissions restricted to the top tier. The authoritative
description is [`02-actors-and-roles.md`](./02-actors-and-roles.md).

### 3.2 Out of Scope

The following are intentionally **not** delivered as working capabilities in the current system:

- **AI assistant features** — no AI/chatbot assistant capability.
- **Real-time chat platform** — there is no person-to-person chat; only system notifications
  exist.
- **Loyalty and subscription programs** — present only as scaffolding; not operational.
- **Production payment provider integration** — no real payment service provider is integrated
  (see §3.3).
- **Push notifications** — mobile push (e.g. FCM/APNs) is not delivered.
- **Other deferred roadmap items** — capabilities named in the codebase or risk register but not
  implemented are out of scope until built.

This document makes **no commitments to future delivery or timelines** for the items above.

### 3.3 Known Technical Limitations

These are verified limitations of the current implementation. They are summarized here for scoping
purposes; full detail and recommended follow-ups live in the
[risk register](./risks/risk-register.md).

| Limitation | Meaning for scope | Reference |
|---|---|---|
| **FakePaymentGateway** | Payments are simulated; no real money moves. The webhook path exists but the gateway is a stub. | risk register (payment gateway) |
| **NoOp integrations** | Certain integrations are inert placeholders (e.g. weather provider, Search Console pinger, discount evaluation, SEO redirect lookup) and produce no real effect. | risk register (NoOp services) |
| **Tracking module limitations** | The Tracking module is scaffolded only; it has no working operational surface. | risk register (Tracking) |
| **Local file storage** | Attachments and invoice PDFs are stored on the host filesystem, not in object storage/CDN. | risk register (storage) |
| **Single-instance assumptions** | Background jobs and outbox processing assume a single running instance; horizontal scale-out is not in scope without additional coordination. | risk register (background jobs) |
| **Audit-only claim reconciliation** | The security seeder **reports** stale/missing role claims but **deletes nothing**; pre-P0/P1 databases may retain stale claims. | risk register / [`02-actors-and-roles.md`](./02-actors-and-roles.md) §13 |

For the complete and current list of risks and observations, see the
[risk register](./risks/risk-register.md).

### 3.4 Production Readiness Note

The current implementation is feature-rich and suitable for development and validation
environments; however, several production-hardening items remain tracked in the
[risk register](./risks/risk-register.md).

---

## Cross References

- [`01-system-overview.md`](./01-system-overview.md) — technology stack, architecture diagrams, modules.
- [`02-actors-and-roles.md`](./02-actors-and-roles.md) — actors, role hierarchy, authorization model.
- [`03-use-case-model.md`](./03-use-case-model.md) — business use-case model and actor mapping.
- [`workflows/`](./workflows/) — end-to-end workflow walkthroughs.
- [`eventing/`](./eventing/) — cross-module integration-event catalog.
- [`risks/`](./risks/) — risk register and known limitations.
