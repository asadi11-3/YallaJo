# Risk Register

> Read-only observations from system discovery. Each entry includes a **Recommended follow-up** (not "fix" — nothing here is being implemented or scheduled).
>
> **Legend:** Severity = Critical / High / Medium / Low · Status = Open / Acknowledged / Mitigated / Accepted

| ID | Title | Area | Severity | Status |
|---|---|---|---|---|
| RISK-001 | JWT uses symmetric key | Auth | Medium | Open |
| RISK-002 | Payment gateway is a stub (`FakePaymentGateway`) | Finance | **Critical** | Acknowledged |
| RISK-003 | Booking depends on stub snapshot readers | Booking | High | Acknowledged |
| RISK-004 | Weather provider + Search Console pinger are NoOps | ContentSeo | Low | Acknowledged |
| RISK-005 | Integration-event handler coverage gaps | Cross-module | Medium | Acknowledged |
| RISK-006 | Module status table in `Agents/YallaJo.md` drifted from reality | Docs | Low | Open |
| RISK-007 | Background jobs use `PeriodicTimer` with no distributed lock | Cross-cutting | High | Acknowledged |
| RISK-008 | `NoOpDiscountEvaluator` means discounts do not apply | Booking / Finance | Medium | Acknowledged |
| RISK-009 | Local filesystem storage for attachments and invoice PDFs | ContentCore / Finance | Medium | Open |
| RISK-010 | Tracking module + LiveTracking / ChatBot hubs are planned, not delivered | Tracking / Messaging | Low | Acknowledged |
| RISK-011 | Outbox hardening still in progress (dead-letter exists) | SharedKernel | Medium | Acknowledged |
| RISK-012 | Documented endpoint authorization violations not yet closed | Cross-module | High | Acknowledged |
| RISK-013 | Recommended middleware (CORS, Correlation-Id, Response Compression) not in pipeline | API host | Low | Open |
| RISK-014 | Large set of historical analysis MD files at repo root may drift | Docs / repo hygiene | Low | Open |
| RISK-015 | Push notifications (FCM/APNs) and AI chatbot LLM planned but not implemented | Messaging | Low | Acknowledged |

---

## RISK-001 — JWT uses symmetric key

| | |
|---|---|
| **Area** | Auth |
| **Severity** | Medium |
| **Status** | Open |
| **Evidence** | `YallaJo.Api/Program.cs` configures `JwtBearer` with `SymmetricSecurityKey` from `Jwt:Key` |
| **Impact** | Same secret signs and validates tokens; leakage of `Jwt:Key` allows token forgery from any environment that holds it. Hampers rotation and multi-environment isolation. |
| **Recommended follow-up** | Evaluate asymmetric signing (RSA / EC) with key rotation. Document secret-management story for all environments. |
| **Linked workflow(s)** | [`01-user-onboarding.md`](../workflows/01-user-onboarding.md) |

---

## RISK-002 — Payment gateway is a stub

| | |
|---|---|
| **Area** | Finance |
| **Severity** | Critical (for production) |
| **Status** | Acknowledged |
| **Evidence** | `Finance.Infrastructure/Gateways/FakePaymentGateway.cs` implements `IPaymentGateway`. `Finance.Application/Commands/ProcessWebhook/` does not enforce a real signature scheme. |
| **Impact** | The booking → payment → confirmation flow is end-to-end exercisable but **not actually charging money**. Webhook endpoint accepts gateway-shaped payloads without provider-specific signature verification. |
| **Recommended follow-up** | Select a payment provider (Stripe / HyperPay / etc.). Implement real `IPaymentGateway`. Implement signed-webhook verification on `POST /finance/payments/webhook`. Ensure PCI scope is understood. |
| **Linked workflow(s)** | [`06-booking-lifecycle.md`](../workflows/06-booking-lifecycle.md) |

---

## RISK-003 — Booking depends on stub snapshot readers

| | |
|---|---|
| **Area** | Booking |
| **Severity** | High |
| **Status** | Acknowledged |
| **Evidence** | `Booking.Infrastructure/Services/StubBookingPricingSnapshotReader.cs`, `StubBookingProviderSnapshotReader.cs`, `StubBookingTourSnapshotReader.cs`, `StubBookingCommissionLookup.cs`, `ThrowingBookingSnapshotReaders.cs` |
| **Impact** | Booking creation reads pricing/provider/tour info from stubs rather than from ContentTours / Accounts / Finance. Real cross-module pricing, capacity, and commission resolution is not wired. |
| **Recommended follow-up** | Replace stubs with real implementations that read snapshots produced via integration events (snapshot-on-event pattern) or via cross-module read services exposed in `*.Contracts`. |
| **Linked workflow(s)** | [`06-booking-lifecycle.md`](../workflows/06-booking-lifecycle.md) |

---

## RISK-004 — Weather + Search Console are NoOps

| | |
|---|---|
| **Area** | ContentSeo |
| **Severity** | Low |
| **Status** | Acknowledged |
| **Evidence** | `ContentSeo.Infrastructure/Weather/NoOpWeatherProvider.cs`, `Sitemap/NoOpSearchConsolePinger.cs` |
| **Impact** | Weather endpoints return empty/cached data; sitemap regeneration does not ping Search Console. Feature gaps but no data integrity risk. |
| **Recommended follow-up** | Pick a weather API; implement `IWeatherProvider` with budget gating already in place (`IWeatherBudgetGate`, `WeatherDailyBudget`). Implement `ISearchConsolePinger`. |
| **Linked workflow(s)** | — |

---

## RISK-005 — Integration-event handler coverage gaps

| | |
|---|---|
| **Area** | Cross-module |
| **Severity** | Medium |
| **Status** | Acknowledged |
| **Evidence** | [`../../Agents/event-handler-coverage-report.md`](../../Agents/event-handler-coverage-report.md); ADR-008 parity rules |
| **Impact** | Some published integration events may have no consumer or vice versa, breaking the ADR-008 parity assumption and silently dropping cross-module effects. |
| **Recommended follow-up** | Run the parity report regularly. Add a build-time check that fails CI on missing consumers/producers. |
| **Linked workflow(s)** | [`17-outbox-inbox-eventing.md`](../workflows/17-outbox-inbox-eventing.md) |

---

## RISK-006 — Module status table drifted

| | |
|---|---|
| **Area** | Docs |
| **Severity** | Low |
| **Status** | Open |
| **Evidence** | `Agents/YallaJo.md` lists many content modules as "Entities created, endpoints empty"; many endpoints clearly exist now in `*.Presentation`. |
| **Impact** | New engineers can be misled about delivered vs planned scope. |
| **Recommended follow-up** | Periodically reconcile the module status table with actual `*Endpoints.cs`. Consider auto-generating it. |
| **Linked workflow(s)** | — |

---

## RISK-007 — Background jobs use `PeriodicTimer` with no distributed lock

| | |
|---|---|
| **Area** | Cross-cutting / Infrastructure |
| **Severity** | High (on horizontal scale) |
| **Status** | Acknowledged (ADR-003 explicitly chose not to use Hangfire) |
| **Evidence** | `YallaJo.SharedKernel.Infrastructure/BackgroundJobs/CompositeOutboxProcessor.cs`; `Booking.Infrastructure/BackgroundServices/*`; `Finance.Infrastructure/BackgroundServices/*` |
| **Impact** | Multiple API instances would double-poll the outbox and double-execute scheduled jobs (slot-lock cleanup, payout batching, popularity scoring, etc.). At single-instance scale this is fine. |
| **Recommended follow-up** | Either keep single-instance and document it explicitly, or introduce a leader-election / advisory-lock approach before scaling out. Revisit ADR-003. |
| **Linked workflow(s)** | [`06-booking-lifecycle.md`](../workflows/06-booking-lifecycle.md), [`17-outbox-inbox-eventing.md`](../workflows/17-outbox-inbox-eventing.md) |

---

## RISK-008 — `NoOpDiscountEvaluator` means discounts do not apply

| | |
|---|---|
| **Area** | Booking / Finance |
| **Severity** | Medium |
| **Status** | Acknowledged |
| **Evidence** | `Booking.Infrastructure/Services/NoOpDiscountEvaluator.cs` |
| **Impact** | Bookings ignore Finance `Discount` / `DiscountUsage` rules. Marketing campaigns and coupon codes are non-functional end-to-end. |
| **Recommended follow-up** | Implement a real `IDiscountEvaluator` that resolves Finance discount rules against the booking context. |
| **Linked workflow(s)** | [`06-booking-lifecycle.md`](../workflows/06-booking-lifecycle.md) |

---

## RISK-009 — Local filesystem storage

| | |
|---|---|
| **Area** | ContentCore / Finance |
| **Severity** | Medium |
| **Status** | Open |
| **Evidence** | `ContentCore.Infrastructure/Services/LocalFileStorageService.cs`; `Finance.Infrastructure/Storage/LocalFileInvoiceStorage.cs` |
| **Impact** | Attachments and invoice PDFs are written to the API host's filesystem. Not safe for multi-instance deployments and no CDN delivery. |
| **Recommended follow-up** | Introduce object storage (Azure Blob / S3) behind the existing `IInvoiceStorage` and storage interfaces. Front user-facing media with a CDN. |
| **Linked workflow(s)** | (planned `09-invoicing.md`) |

---

## RISK-010 — Tracking, LiveTracking / ChatBot hubs are planned

| | |
|---|---|
| **Area** | Tracking / Messaging |
| **Severity** | Low |
| **Status** | Acknowledged |
| **Evidence** | `Agents/YallaJo.md` describes `LiveTrackingHub` and `ChatBotHub`; only `NotificationHub` is present (`Messaging.Presentation/Hubs/NotificationHub.cs`). |
| **Impact** | Real-time tour tracking and chatbot are not yet usable. |
| **Recommended follow-up** | Either scope them out of MVP explicitly, or schedule. |
| **Linked workflow(s)** | — |

---

## RISK-011 — Outbox hardening in progress

| | |
|---|---|
| **Area** | SharedKernel |
| **Severity** | Medium |
| **Status** | Acknowledged |
| **Evidence** | [`../../Agents/outbox-hardening-implementation-plan.md`](../../Agents/outbox-hardening-implementation-plan.md); `OutboxDeadLetterHealthCheck.cs`; `OutboxCleaner.cs` |
| **Impact** | Production-grade reliability (poison-message handling, replay tooling, monitoring) is partially in place. |
| **Recommended follow-up** | Complete the items in the hardening plan; expose dead-letter replay tooling. |
| **Linked workflow(s)** | [`17-outbox-inbox-eventing.md`](../workflows/17-outbox-inbox-eventing.md) |

---

## RISK-012 — Endpoint authorization gaps tracked but open

| | |
|---|---|
| **Area** | Cross-module |
| **Severity** | High |
| **Status** | Acknowledged |
| **Evidence** | `Agents/endpoint-authorization-audit.md`, `Agents/endpoint-violations.csv`, `Agents/endpoint-violations-2027-02-28.csv`, `Agents/permission-coverage-gaps-2027-02-28.md` |
| **Impact** | Specific endpoints are known to be missing or have inconsistent authorization. Listed but not all closed. |
| **Recommended follow-up** | Treat the CSV as a backlog; gate merges with an authorization-coverage check; re-audit periodically. |
| **Linked workflow(s)** | — |

---

## RISK-013 — Recommended middleware not in pipeline

| | |
|---|---|
| **Area** | API host |
| **Severity** | Low |
| **Status** | Open |
| **Evidence** | `Agents/YallaJo.md` "Middleware Pipeline" lists CORS, Correlation-Id, Response Compression as recommended; `YallaJo.Api/Program.cs` does not register them. |
| **Impact** | No request correlation header for distributed tracing; no negotiated compression; cross-origin policy not configured in host. |
| **Recommended follow-up** | Decide whether each is required, and either implement or close the recommendation in `Agents/YallaJo.md`. |
| **Linked workflow(s)** | — |

---

## RISK-014 — Historical analysis MD files at repo root

| | |
|---|---|
| **Area** | Docs / repo hygiene |
| **Severity** | Low |
| **Status** | Open |
| **Evidence** | Files like `AUTH_*`, `ACCOUNTS_*`, `SECURITY_*`, `PROFILE_CREATION_*`, `SHARED_*`, `YALLAJO_*` at repo root |
| **Impact** | These appear to be one-off exploration artifacts. Risk of being mistaken for source-of-truth as the codebase evolves. |
| **Recommended follow-up** | Move to `docs/archive/` or delete what is superseded by [`docs/`](../). Keep only living docs at root. |
| **Linked workflow(s)** | — |

---

## RISK-015 — Push (FCM/APNs) + AI chatbot LLM not implemented

| | |
|---|---|
| **Area** | Messaging |
| **Severity** | Low |
| **Status** | Acknowledged |
| **Evidence** | `Messaging.Infrastructure/Services/PushNotificationStrategy.cs` exists but external provider integration and `IChatbotProvider` are not in the tree. |
| **Impact** | Mobile push and AI chat are not delivered. |
| **Recommended follow-up** | Pick providers and implement, or scope out for MVP. |
| **Linked workflow(s)** | (planned `10-notifications-fanout.md`) |
