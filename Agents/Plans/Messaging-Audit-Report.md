# Messaging Module — Audit Report

> **Audited**: 2025-01-27
> **Plan**: `Agents/Plans/Messaging-Workflow.md` (541 lines)
> **Score**: **7.5/10**

---

## Executive Summary

The Messaging module is **substantially built** with 11 entities, 23 REST endpoints + 1 SignalR hub, 53 integration event handlers, 2 background services, and 23 CQRS handlers. The template engine, ticket state machine, SLA calculation, round-robin assignment, device tokens, and preferences all work. Key gaps: **0 validators** for 14+ commands, **no SMS implementation** (enum value exists but no sender), **no HybridCache**, **no SLA monitoring service**, and **no digest/batch operations**.

---

## Module Inventory

| Layer | .cs Files |
|---|---|
| Domain | 44 |
| Application | 54 |
| Infrastructure | 87 |
| Presentation | 6 |
| Contracts | 10 |
| **Total** | **201** |

---

## Domain Entities (11)

| Entity | Type | Notes |
|---|---|---|
| Notification | Aggregate root | Core notification lifecycle |
| SupportTicket | Aggregate root | Full state machine |
| TicketMessage | Child entity | Messages within ticket |
| NotificationTemplate | Aggregate root | Template engine |
| NotificationPreference | Aggregate root | Per-user channel preferences |
| DeviceToken | Aggregate root | Push notification tokens |
| NotificationDeliveryAttempt | Entity | Retry/delivery tracking |
| UserSnapshot | Entity | Email rendering data |
| AdminAssignmentRoster | Entity | Round-robin ticket assignment |
| ChatBotConversation | DEFERRED | Stub in `_Deferred/` |
| ChatBotMessage | DEFERRED | Stub in `_Deferred/` |

---

## Endpoints (23 REST + 1 Hub)

| File | Routes | Notes |
|------|--------|-------|
| MessagingEndpoints.cs | 0 + hub | Wiring + SignalR `/hubs/notifications` |
| NotificationEndpoints.cs | 9 | CRUD + mark-read + batch ops |
| SupportTicketEndpoints.cs | 7 | Full ticket lifecycle |
| NotificationTemplateEndpoints.cs | 4 | Admin template CRUD |
| DeviceEndpoints.cs | 3 | Register/delete/list tokens |
| **Total** | **23 + 1 hub** | |

---

## Integration Event Handlers (53 classes, 42 files)

Multi-handler files:
- `TourGuideNotificationHandlers.cs` — 6 handlers
- `BookingJoinRequestHandlers.cs` — 3 handlers
- `MessagingIntegrationConverters.cs` — 4 outbound converters

The remaining 40 handlers are 1-per-file, consuming events from:
Booking, Finance, Social, ContentTours, ContentPlaces, ContentBlogs, Accounts, Auth, Security

---

## Background Services (2)

1. **EmailNotificationSenderService** — Sends queued email notifications
2. **ReadNotificationCleanupService** — Cleans old read notifications

---

## CQRS Handlers (23)

| Type | Count | Examples |
|---|---|---|
| Commands | 15 | CreateSupportTicket, PostTicketMessage, MarkNotificationRead, RegisterDeviceToken, BatchDeleteNotifications |
| Queries | 8 | GetMyNotifications, GetUnreadCount, GetSupportTickets, GetMyPreferences |

---

## Permission Catalog (18 permissions, 6 features)

| Feature | Permissions |
|---|---|
| Notification | Read, Create, Delete |
| NotificationPreference | Read, Update |
| NotificationTemplate | Read, Create, Update, Delete |
| DeviceToken | Read, Create, Delete |
| SupportTicket | Read, Create, Update, Close |
| AdminSupportQueue | Read, Assign |

---

## Notification Channels

| Channel | Enum Value | Implementation |
|---|---|---|
| InApp | ✅ | ✅ Implemented |
| Email | ✅ | ✅ Implemented |
| Push | ✅ | ✅ Implemented |
| SMS | ✅ | ❌ NOT Implemented |

---

## Key Gaps

| # | Gap | Severity | Notes |
|---|-----|----------|-------|
| 1 | 0 validators for 14+ commands | HIGH | No FluentValidation at all |
| 2 | No HybridCache usage | MEDIUM | All queries hit DB directly |
| 3 | No SMS sender/strategy | MEDIUM | Enum value exists, no implementation |
| 4 | No SLA monitoring background service | MEDIUM | SLA calc exists but not monitored |
| 5 | No digest/batch notification service | MEDIUM | Plan specifies it |
| 6 | SignalR MarkAsRead is no-op on client | LOW | State change via REST only |
| 7 | No TourGuide notification types in enum | LOW | Plan specifies adding them |

---

## Scorecard

| Dimension | Score | Notes |
|---|---|---|
| Plan Accuracy | 7/10 | Endpoint/handler counts close to plan |
| Implementation Completeness | 7/10 | Core features work; validators/cache/SMS missing |
| Code Quality | 7/10 | 0 validators is significant; otherwise clean |
| Architecture Alignment | 9/10 | Consistent patterns, proper CQRS |
| **Overall** | **7.5/10** | |
