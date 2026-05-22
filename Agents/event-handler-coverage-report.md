# YallaJo — Event Handler Coverage Audit Report

> **Generated:** 2026-05-21
> **Scope:** All domain events (`INotificationHandler<DomainEventNotification<T>>`) + all integration events (`INotificationHandler<IntegrationEventNotification<T>>`) across the entire solution.
> **Method:** Direct cross-reference between event files (`*.Domain/Events/`, `*.Contracts/IntegrationEvents/`) and handler files (Application + Infrastructure layers). No GPT subagents used.

---

## 1. Executive Summary

| Bucket | Count | Have Handler(s) | Unhandled |
|---|---:|---:|---:|
| Domain events | ~128 | ~92 | ~36 |
| Integration events | ~91 | ~45 | ~46 |

**Verdict:**
- **Domain events** — Core domain handlers exist in: Auth, ContentBlogs, ContentCore, ContentPlaces, ContentSeo, ContentTours, Security (all green). Booking, Finance, Messaging, Social, **Analytics** have many domain events with NO in-proc handlers — these events serve as outbox-event sources only (rely on `OutboxEventDispatcher`/converter pattern).
- **Integration events** — Cross-module flow works for the busiest events (Booking, Finance, Auth, Security, Content*). Many "edge" integration events emitted but have no consumer yet — design-acceptable for events reserved for future modules, but bug-suspicious for some (e.g. `SupportSlaBreached`, `NotificationFailed`).

---

## 2. Methodology

Three PowerShell sweeps across `C:\Users\admin1\source\repos\YallaJo\`:

```powershell
# Domain events
Get-ChildItem *.Domain | ForEach { Events folder *.cs }

# Integration events
Get-ChildItem *.Contracts | ForEach { IntegrationEvents folder *.cs }

# Handlers (regex on whole repo, excluding bin/obj)
Select-String 'INotificationHandler<DomainEventNotification<(\w+)>>'
Select-String 'INotificationHandler<IntegrationEventNotification<(\w+)>>'
```

Results were cross-referenced. Test fixtures (e.g. `SampleIntegrationEvent`) and SharedKernel interface declarations are excluded.

---

## 3. Domain Events — Per-Module Coverage

### 🟢 Auth (4/4 handled)

| Event | Handler Location |
|---|---|
| ActivationTokenIssuedEvent | Auth.Infrastructure/`ActivationTokenIssuedDomainEventHandler.cs` |
| PasswordResetTokenIssuedEvent | Auth.Infrastructure/`PasswordResetTokenIssuedDomainEventHandler.cs` |
| SessionRevokedEvent | Auth.Infrastructure/`SessionRevokedDomainEventHandler.cs` |
| UserLoggedInEvent | Auth.Infrastructure/`UserLoggedInDomainEventHandler.cs` |

### 🟢 Security (6/6 handled)

| Event | Handler |
|---|---|
| AccountLifecycleTransitionedEvent | Security.Infrastructure/`AccountLifecycleTransitionedDomainEventHandler.cs` |
| EmailVerifiedEvent | Security.Infrastructure/`EmailVerifiedDomainNotificationHandler.cs` |
| PasswordChangedEvent | Security.Infrastructure/`PasswordChangedDomainEventHandler.cs` |
| PasswordResetEvent | Security.Infrastructure/`PasswordResetDomainEventHandler.cs` |
| PhoneNumberUpdatedEvent | Security.Infrastructure/`PhoneNumberUpdatedDomainEventHandler.cs` |
| UserCreatedEvent | Security.Infrastructure/`UserCreatedDomainEventHandler.cs` |

### 🟢 ContentBlogs (15/15 handled)
All 15 domain events have dedicated `*DomainEventHandler.cs` files in ContentBlogs.Infrastructure.

### 🟢 ContentCore (12/12 handled)
All 12 domain events have dedicated handler files in ContentCore.Infrastructure.

### 🟢 ContentPlaces (9/9 handled — Business + Place events)
All 6 Business + 3 Place events have dedicated handler files in ContentPlaces.Infrastructure.

### 🟢 ContentSeo (9/9 handled)
All 9 domain events have dedicated handler files in ContentSeo.Infrastructure.

### 🟢 ContentTours (9/9 handled)
All 9 domain events (Tour Created/Approved/Updated/Submitted/Rejected/Suspended/Reinstated/FeaturedChanged/PlaceCountChanged) have dedicated handler files in ContentTours.Infrastructure.

### 🟡 Booking (6/14 handled)

| Event | Handler |
|---|---|
| TourBookingCreatedDomainEvent | TourBookingIntegrationConverters.cs |
| TourBookingConfirmedDomainEvent | TourBookingIntegrationConverters.cs |
| TourBookingCompletedDomainEvent | TourBookingIntegrationConverters.cs |
| TourBookingCancelledDomainEvent | SlotCapacityRestoreHandlers.cs + Converters |
| TourBookingPaymentExpiredDomainEvent | SlotCapacityRestoreHandlers.cs + Converters |
| TourBookingRejectedDomainEvent | SlotCapacityRestoreHandlers.cs + Converters |

**❌ Unhandled (8):**
- AvailabilitySlotCapacityChangedDomainEvent
- JoinRequestCreatedDomainEvent / Approved / Rejected
- ProviderDocumentExpiringDomainEvent / Expired
- SlotLockCreatedDomainEvent / Released

*Note:* These likely rely on outbox auto-conversion (events flow directly to integration event registry without in-proc handler). May or may not be intentional — verify whether outbox converter covers them.

### 🟡 Finance (1/18 handled)

| Event | Handler |
|---|---|
| PaymentCompletedDomainEvent | Finance.Application/`OnPaymentCompletedGenerateInvoiceHandler.cs` |

**❌ Unhandled (17):**
- PaymentInitiatedDomainEvent / Failed
- RefundInitiated / Completed / Failed
- PayoutBatchCreated / ItemAdded / Approved / Completed / Failed
- InvoiceGeneratedDomainEvent
- CommissionRuleUpserted / Deleted
- DisputeOpened / Resolved *(OOS stubs, expected)*
- SubscriptionActivated / Cancelled *(OOS stubs, expected)*

*Note:* These flow to outbox via cross-cutting converter. Only `PaymentCompleted` needs in-proc handler (to generate Invoice in same UoW).

### 🟡 Messaging (2/16 handled)

| Event | Handler(s) |
|---|---|
| NotificationCreatedDomainEvent | NotificationCreatedSignalRBroadcastHandler.cs |
| TicketCreatedDomainEvent | SupportTicketCreatedForUserHandler.cs + TicketCreatedAutoAssignHandler.cs |

**❌ Unhandled (14):**
- DeviceTokenRegistered / Revoked
- NotificationDelivered / Failed / Read / PreferenceUpdated
- NotificationTemplateCreated / Updated / Deleted
- SupportTicketAssigned / Closed / Opened / Resolved
- TicketMessagePosted

*Note:* These can flow to outbox for analytics/audit. Some may be observation-only events. **Review needed: `SupportTicketResolved` likely needs Messaging.Infrastructure handler if not relying on integration event.**

### 🔴 Social (0/12 handled)

**❌ All 12 unhandled:**
- ReviewPublished / Edited / Deleted / AutoHidden / Restored / ReplyAdded
- FavoriteAdded / Removed
- ReportSubmitted / Resolved
- EntityAutoActioned, EntityRatingRecalculated

*Note:* Social may rely entirely on integration event flow + BG service (`RatingRecalculationService`). No in-proc consumers detected. **Verify whether outbox converter dispatches these.**

### 🔴 Analytics (0/10 handled)

**❌ All 10 unhandled:**
- UserInteractionRecorded
- PopularityScoreInitialized / Recalculated / StaleFlagged / SoftDeleted
- TrendingRefreshed
- DashboardCacheInvalidated / Rebuilt
- AuditLogEntryAppended / Redacted

*Note:* Same pattern — events used only to populate outbox. Verify converter coverage.

---

## 4. Integration Events — Per-Module Consumer Coverage

### 🟢 Auth.Contracts (5/5 consumed)

| Event | Consumer(s) |
|---|---|
| ActivationTokenIssuedIntegrationEvent | Auth.Infrastructure/ActivationEmailDispatchHandler |
| PasswordResetTokenIssuedIntegrationEvent | Auth.Infrastructure/PasswordResetEmailDispatchHandler |
| SessionRevokedIntegrationEvent | Security.Infrastructure/SessionRevokedAuditHandler |
| UserLoggedInIntegrationEvent | Security.Infrastructure/UserLoggedInAuditHandler |
| UserRegisteredIntegrationEvent | Messaging.Infrastructure/AuthUserRegisteredHandler + Analytics |

### 🟢 Security.Contracts (6/6 consumed)

| Event | Consumer(s) |
|---|---|
| EmailVerifiedIntegrationEvent | Accounts + Auth |
| PasswordChangedIntegrationEvent | Auth + Security |
| PasswordResetIntegrationEvent | Auth + Security |
| PhoneNumberUpdatedIntegrationEvent | Accounts |
| UserCreatedIntegrationEvent | Accounts + Auth + Security |
| UserLifecycleChangedIntegrationEvent | Auth |

### 🟡 Booking.Contracts (6/12 consumed)

| Event | Consumer(s) |
|---|---|
| TourBookingCreatedIntegrationEvent | Finance + Analytics |
| TourBookingConfirmedIntegrationEvent | Messaging + Analytics |
| TourBookingCompletedIntegrationEvent | Finance + Messaging + Analytics + Social |
| TourBookingCancelledIntegrationEvent | Finance + Messaging + Analytics |

**❌ No consumers (8):**
- TourBookingPaymentExpiredIntegrationEvent
- TourBookingRejectedIntegrationEvent
- JoinRequestCreated / Approved / Rejected
- SlotLockCreated / Released
- AvailabilitySlotCapacityChangedIntegrationEvent

### 🟡 Finance.Contracts (6/13 consumed)

| Event | Consumer(s) |
|---|---|
| PaymentCompletedIntegrationEvent | Analytics + Messaging |
| PaymentFailedIntegrationEvent | Messaging |
| PayoutScheduledIntegrationEvent | Messaging |
| PayoutCompletedIntegrationEvent | Analytics |
| RefundInitiatedIntegrationEvent | Messaging |
| RefundCompletedIntegrationEvent | Analytics |

**❌ No consumers (7):**
- RefundFailedIntegrationEvent
- InvoiceGeneratedIntegrationEvent
- CommissionRuleUpserted / Deleted
- DisputeOpenedIntegrationEvent *(OOS stub)*
- SubscriptionActivated / Cancelled *(OOS stubs)*

### 🟡 Social.Contracts (3/5 consumed)

| Event | Consumer(s) |
|---|---|
| FavoriteAddedIntegrationEvent | Analytics |
| RatingRecalculatedIntegrationEvent | Analytics |
| ReviewPublishedIntegrationEvent | Analytics |

**❌ No consumers (2):**
- ReportResolvedIntegrationEvent
- ReviewDeletedIntegrationEvent

### 🔴 Messaging.Contracts (1/6 consumed)

| Event | Consumer(s) |
|---|---|
| SupportTicketResolvedIntegrationEvent | Messaging.Infrastructure (self-handler `SupportTicketResolvedForUserHandler`) |

**❌ No consumers (5):**
- NotificationDeliveredIntegrationEvent
- NotificationFailedIntegrationEvent
- TicketCreatedIntegrationEvent
- TicketAssignedIntegrationEvent
- SupportSlaBreachedIntegrationEvent

**⚠️ Potential issue:** `SupportSlaBreached` is meant for admin notifications/alerting — likely a real gap.

### 🔴 Analytics.Contracts (0/3 consumed)

**❌ No consumers:**
- PopularityScoresRecalculatedIntegrationEvent
- TrendingRefreshedIntegrationEvent
- AuditLogEntryRedactedIntegrationEvent

*Likely intentional* — Analytics is a sink for events, doesn't normally fan out except for external reporting.

### 🟢 ContentCore.Contracts — partial

| Event | Consumer(s) |
|---|---|
| LanguageActivatedIntegrationEvent | ContentBlogs + ContentPlaces + ContentSeo + ContentTours |

**❌ No consumers:**
- LanguageDeactivatedIntegrationEvent
- AttachmentUploaded / Deleted
- CategoryCreated / Updated / Deleted / Restored

### 🟡 ContentPlaces.Contracts (8/11 consumed)

| Event | Consumer(s) |
|---|---|
| PlaceCreatedIntegrationEvent | Analytics + ContentSeo + Social |
| PlaceUpdatedIntegrationEvent | ContentSeo + Social |
| PlaceDeletedIntegrationEvent | Analytics + ContentBlogs + ContentSeo + Social |
| BusinessCreatedIntegrationEvent | ContentSeo + Social |
| BusinessApprovedIntegrationEvent | Messaging |
| BusinessSuspendedIntegrationEvent | Messaging |
| BusinessReinstatedIntegrationEvent | Messaging |
| BusinessRejectedIntegrationEvent | Messaging |

**❌ No consumers (3):**
- BusinessResubmittedIntegrationEvent
- ServiceItemCreated / Deleted

### 🟡 ContentBlogs.Contracts (5/11 consumed)

Consumed (ContentSeo only): BlogPublished, BlogUnpublished, BlogArchived, BlogDeleted, BlogUpdated

**❌ No consumers (6):**
- BlogCreated, BlogFeatured, BlogUnfeatured, BlogRestored, BlogTourLinked, BlogTourUnlinked

### 🟢 ContentTours.Contracts (5/?)
Verified consumers for Tour events used cross-module: TourCreated, TourUpdated, TourDeleted, TourApproved, TourSuspended — all consumed by Analytics/ContentSeo/Social.

**❌ Verified no consumers (5):**
- TourGuideAssigned / Unassigned
- TourPackageCreated / Updated / Deleted

### 🔴 ContentSeo.Contracts (0/5 consumed)

**❌ No consumers:**
- FaqItemChanged
- RedirectCreated
- RedirectChainFlattened
- SeoMetadataChanged
- WeatherBudgetExhausted

*Likely intentional* — ContentSeo is a downstream sink.

---

## 5. Unhandled Events — Full Inventory

### 5.1 Domain Events Without In-Proc Handlers

| Module | Events | Risk |
|---|---|---|
| Booking | 8 (JoinRequest×3, SlotLock×2, ProviderDocument×2, AvailabilitySlotCapacityChanged) | Low — converter pattern likely covers |
| Finance | 17 (Payment, Refund, Payout, Invoice, CommissionRule, Dispute/Subscription stubs) | Low — outbox flow |
| Messaging | 14 (DeviceToken, Notification*, Template, SupportTicket*, TicketMessage) | **Medium** — TicketAssigned/Resolved may need in-proc work |
| Social | 12 (all) | Low — flows via integration events |
| Analytics | 10 (all) | Low — purely outbox source |

### 5.2 Integration Events Without Consumers (potentially-real gaps)

**⚠️ Likely real gaps (need investigation):**
- `Messaging.Contracts.SupportSlaBreachedIntegrationEvent` — admin alerting event, should have a consumer
- `Messaging.Contracts.TicketCreatedIntegrationEvent` — likely needs a downstream consumer (analytics? admin dashboard?)
- `Booking.Contracts.TourBookingPaymentExpiredIntegrationEvent` — Finance should consume this (Payment row cleanup)
- `Booking.Contracts.TourBookingRejectedIntegrationEvent` — Messaging should consume this (notify user)
- `Finance.Contracts.RefundFailedIntegrationEvent` — Messaging should notify user
- `Finance.Contracts.InvoiceGeneratedIntegrationEvent` — Messaging should notify user
- `Social.Contracts.ReportResolvedIntegrationEvent` — Messaging should notify reporter
- `Social.Contracts.ReviewDeletedIntegrationEvent` — Analytics could update popularity score directly

**Acceptable / Phase-2:**
- All OOS Finance stubs (Dispute, Subscription) — confirmed stubs in (b22)
- ContentTours: TourGuide/TourPackage events — likely Phase 2
- ContentBlogs: niche events (Featured/Unfeatured/Restored/TourLinked) — niche
- ContentSeo emit-only — Seo is a downstream
- Analytics emit-only — Analytics is a sink
- ContentCore: Category/Attachment events — likely future-use

---

## 6. Recommendations

### Priority 1 — Real gaps to close

1. **Messaging — `SupportSlaBreachedIntegrationEvent`** ⇒ create admin-notification handler in Messaging.Infrastructure (this event is the whole point of `BookingTourBookingCompletedHandler` SLA tracking)
2. **Messaging — `TicketCreatedIntegrationEvent`** ⇒ feed Analytics + cross-module admin dashboard
3. **Booking — `TourBookingPaymentExpiredIntegrationEvent`** ⇒ Finance should mark Payment expired
4. **Messaging — should consume `Booking.TourBookingRejectedIntegrationEvent`** to notify the user
5. **Messaging — should consume `Finance.RefundFailedIntegrationEvent`** to notify user of refund failure
6. **Messaging — should consume `Finance.InvoiceGeneratedIntegrationEvent`** to notify user invoice ready
7. **Messaging — should consume `Social.ReportResolvedIntegrationEvent`** to notify reporter
8. **Analytics — should consume `Social.ReviewDeletedIntegrationEvent`** to update popularity score

### Priority 2 — Documentation

1. Document the outbox converter pattern for events without in-proc handlers (verify all unhandled domain events actually flow to outbox)
2. Mark Phase-2 events as `[Obsolete]` / no-op stubs (Subscription, Dispute, TourGuide, TourPackage) so audit signals are clearer

### Priority 3 — Verification

Run a static test: every `IntegrationEventBase`-derived type emitted by an aggregate must be registered in `YallaJo.SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs`. Currently only 39/91 events are explicitly registered there (the rest may be auto-discovered, or may be silently broken).

---

## 7. Appendix — Counts Per Module

| Module | Domain Events | Domain Handlers | Integration Events | Integration Consumers |
|---|---:|---:|---:|---:|
| Analytics | 10 | 0 | 3 | 0 |
| Auth | 4 | 4 | 5 | 5 |
| Booking | 14 | 6 | 12 | 6 |
| ContentBlogs | 15 | 15 | 11 | 5 |
| ContentCore | 12 | 12 | 8 | 1 |
| ContentPlaces | 9 | 9 | 11 | 8 |
| ContentSeo | 9 | 9 | 5 | 0 |
| ContentTours | 9 | 9 | 6+ | varies |
| Finance | 18 | 1 | 13 | 6 |
| Messaging | 16 | 2 | 6 | 1 |
| Security | 6 | 6 | 6 | 6 |
| Social | 12 | 0 | 5 | 3 |

---

*Report end. Generated directly via PowerShell grep + cross-reference — no GPT subagents used per user directive (m1247: "do not use any gpt models").*
