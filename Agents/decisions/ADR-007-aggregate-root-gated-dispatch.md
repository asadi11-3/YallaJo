# ADR-007 — IAggregateRoot-Gated Domain Event Dispatch

- **Status:** Accepted (re-confirmed during Wave 5/6 Pre-Work)
- **Date:** 2026-06-01
- **Context:** Pre-Work execution for Wave 5/6 modules

## Context

`YallaJo.SharedKernel.Infrastructure/Data/UnitOfWork<TContext>` enumerates `ChangeTracker.Entries<BaseEntity>().Where(e => e.Entity is IAggregateRoot)` to find entities with pending domain events. Child entities (e.g. `TicketMessage`, `InvoiceLineItem`, `DisputeMessage`, `UserPreferredCategory`) deliberately do **not** implement `IAggregateRoot`. Without this marker:

- Children could raise events that fire outside their owning aggregate's transaction boundary.
- Cascade-deletes would dispatch orphan events.
- Consumers of integration events would see inconsistent partial state.

## Decision

Mark only true aggregate roots with `IAggregateRoot`. The Wave 5/6 audit produced the following inventory (33 aggregate roots across 5 new modules):

| Module | Aggregate Roots | Children (not marked) |
|---|---|---|
| Booking | 6 (TourBooking, AvailabilitySlot, RefundPolicy, JoinRequest, ProviderDocument, SlotLock) | — |
| Finance | 11 (Payment, Payout, InvoiceItem, Subscription, SubscriptionPlan, Discount, CommissionRule, Dispute, Referral, LoyaltyPoints, ProviderBankAccount) | DiscountUsage, DisputeEvidence, DisputeMessage, InvoiceLineItem, LoyaltyTransaction, PayoutItem, PlanFeature, SubscriptionFeature |
| Social | 3 (Review, Favorite, Report) | AccessibilityReview*, ContentModerationLog* |
| Messaging | 6 (Notification, NotificationPreference, NotificationTemplate, DeviceToken, SupportTicket, ChatBotConversation) | TicketMessage, ChatBotMessage |
| Analytics | 5 (PopularityScore, UserInteraction, AuditLog, RecommendationCache, UserPreference) | UserPreferredCategory |

\* AccessibilityReview and ContentModerationLog are flat append-only logs — they store moderation history but do not own an event-bearing lifecycle, so they intentionally remain bare `AuditableEntity` / `BaseEntity`.

## Consequences

✅ Domain event dispatch is structurally bounded: only an aggregate root can fire events that propagate.
✅ Repository interfaces match exactly: `I{Aggregate}Repository : IRepository<{Aggregate}, Guid>` requires `where TEntity : class, IAggregateRoot`, so the compiler enforces the boundary.
✅ Child entity modifications still participate in the transactional save — they are written by the aggregate root's repository.

## Verification

- Unit test `UoW_Dispatches_Events_Only_For_AggregateRoots` (in `tests/SharedKernel.Tests.Unit/Data/UnitOfWorkTests.cs`) asserts that an entity implementing `IDomainEvent`-bearing but NOT `IAggregateRoot` has its events silently ignored by dispatch.
- `IRepository<TEntity, TKey>` generic constraint `where TEntity : class, IAggregateRoot` compiles only when the entity is marked.
- AnalyticsDbContext-style BIGINT roots (`UserInteraction`, `AuditLog`) use `BaseEntity<long>, IAggregateRoot` — the marker is base-class agnostic.
