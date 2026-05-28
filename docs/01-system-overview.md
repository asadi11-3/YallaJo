# 01 — System Overview

## What YallaJo is

A **tourism marketplace for Jordan** that connects tourists with local providers (tour operators, independent guides, hotels, activity centers, businesses). It covers the full lifecycle: discovery → booking → payment → fulfillment → review → payout.

**Shape:** single deployable **.NET 8 Modular Monolith** (`YallaJo.Api`) with 14 modules, each with its own SQL Server schema.

---

## Tech stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 |
| API style | Minimal API + route groups per module |
| Persistence | EF Core + SQL Server (one DbContext per module) |
| CQRS | MediatR (`ICommand<T>` / `IQuery<T>` returning `Result<T>`) |
| Validation | FluentValidation (pipeline behavior) |
| Auth | JWT Bearer (symmetric key) + policy + permission catalog |
| Bot protection | Google reCAPTCHA (pipeline behavior) |
| Eventing | Outbox dispatch via `CompositeOutboxProcessor`; Inbox dedupe via `EfInboxStore` |
| Realtime | SignalR (`NotificationHub` live; tracking + chatbot planned) |
| Background jobs | `BackgroundService` + `PeriodicTimer` (ADR-003: no Hangfire) |
| Logging | Serilog (structured, request logging) |
| Telemetry | OpenTelemetry (tracing + metrics) |
| Errors | `IExceptionHandler` chain → RFC 7807 ProblemDetails |
| Email | Gmail SMTP (Auth) + generic SMTP (Messaging) |
| Translation | Azure Translator + auto-save decorator |
| PDF | QuestPDF (invoices) |
| Storage | Local filesystem (attachments, invoices) |
| Payments | `FakePaymentGateway` (stub) — see [risks](./risks/risk-register.md) |

---

## C4 — System context

```mermaid
C4Context
    title YallaJo — System Context
    Person(tourist, "Tourist", "Books tours, leaves reviews")
    Person(provider, "Provider", "Sells tours, manages availability")
    Person(admin, "Admin / SuperAdmin / Owner", "Moderates content, manages roles, approves payouts")
    System(yj, "YallaJo API", ".NET 8 Modular Monolith")
    System_Ext(sql, "SQL Server", "Per-module schemas")
    System_Ext(smtp, "Gmail / SMTP", "Transactional email")
    System_Ext(oauth, "Google / Facebook / Apple", "External OAuth login")
    System_Ext(recaptcha, "Google reCAPTCHA", "Bot protection")
    System_Ext(translate, "Azure Translator", "Auto translation")
    System_Ext(gateway, "Payment Gateway (stub)", "Charges / refunds")
    System_Ext(fs, "Local filesystem", "Attachments + invoice PDFs")

    Rel(tourist, yj, "HTTPS / SignalR")
    Rel(provider, yj, "HTTPS / SignalR")
    Rel(admin, yj, "HTTPS")
    Rel(yj, sql, "EF Core")
    Rel(yj, smtp, "SMTP")
    Rel(yj, oauth, "OIDC / OAuth")
    Rel(yj, recaptcha, "HTTPS")
    Rel(yj, translate, "HTTPS")
    Rel(yj, gateway, "HTTPS webhook")
    Rel(yj, fs, "Read / Write")
```

---

## C4 — Container view

```mermaid
C4Container
    title YallaJo — Containers
    Person(user, "User")

    Container_Boundary(api, "YallaJo.Api (single process)") {
        Container(presentation, "Module Presentation layers", ".NET Minimal API", "14 route groups")
        Container(application, "Module Application layers", "MediatR", "Commands, Queries, Validators, EventHandlers")
        Container(infrastructure, "Module Infrastructure layers", "EF Core, BackgroundServices", "DbContexts, Repositories, Outbox writers, BG jobs")
        Container(shared, "SharedKernel", ".NET libs", "Result, CQRS contracts, Outbox/Inbox, DomainEvent dispatcher, Permission auth")
        Container(hubs, "SignalR Hubs", "SignalR", "NotificationHub (live), LiveTracking + ChatBot (planned)")
    }

    ContainerDb(sql, "SQL Server", "DB", "auth, security, accounts, content_*, booking, finance, messaging, social, analytics, tracking schemas")
    Container_Ext(externals, "External systems", "", "SMTP, OAuth, reCAPTCHA, Azure Translator, Payment Gateway")

    Rel(user, presentation, "HTTPS / WSS")
    Rel(presentation, application, "MediatR send")
    Rel(application, infrastructure, "DI: repos, services, UoW")
    Rel(infrastructure, sql, "EF Core")
    Rel(application, shared, "Result, behaviors")
    Rel(infrastructure, shared, "Outbox, Inbox, DomainEvents")
    Rel(infrastructure, externals, "HTTPS / SMTP")
    Rel(hubs, user, "Push (notifications)")
```

---

## 14 modules at a glance

| Module | Schema | Core responsibility |
|---|---|---|
| **Auth** | `auth` | Login, register, OTP, JWT, sessions, refresh tokens, external OAuth, invitations, password reset |
| **Security** | `security` | Users, roles, claims, permission catalog, role hierarchy, admin audit |
| **Accounts** | `accounts` | User profiles, avatars, profile reassignment on auth events |
| **ContentCore** | `content_core` | Languages, Categories, Tags, Specializations, Attachments, Translations |
| **ContentPlaces** | `content_places` | Places, Businesses, BusinessHours/Staff/Amenities, ServiceItems |
| **ContentTours** | `content_tours` | Tours, Schedules, PricingTiers, Packages, Waypoints, TourGuides |
| **ContentBlogs** | `content_blogs` | Blogs, Comments + Reactions, BlogViews, Blog↔Tour links |
| **ContentSeo** | `content_seo` | SeoMetadata, Redirects, Sitemap, FAQ, Weather cache |
| **Booking** | `booking` | TourBookings, JoinRequests, AvailabilitySlots, SlotLocks, RefundPolicies, ProviderDocuments |
| **Finance** | `finance` | Payments, Invoices, Payouts, Commissions, Subscriptions, Discounts, Loyalty, Disputes |
| **Messaging** | `messaging` | Notifications, templates, device tokens, support tickets, chatbot |
| **Social** | `social` | Reviews, Favorites, Reports, moderation |
| **Tracking** | `tracking` | Live GPS sessions + location snapshots (scaffolded) |
| **Analytics** | `analytics` | Interactions, popularity scores, recommendation cache, dashboards, audit log |

Detailed cards: [`03-module-map.md`](./03-module-map.md).

---

## Actors & roles

```mermaid
flowchart LR
    Anon([Anonymous]) -->|browse, register, login| API
    User([Tourist / User]) -->|book, review, favorite, support| API
    Provider([Provider]) -->|publish tours, confirm bookings| API
    Guide([Tour Guide]) -->|run sessions| API
    Admin([Admin]) -->|moderate, approve| API
    SuperAdmin([SuperAdmin]) -->|roles, payouts, commissions| API
    Owner([Owner]) -->|everything| API
    System([Background workers]) -->|outbox, expiry, payouts, scoring| API
    API[(YallaJo.Api)]
```

Role hierarchy: `Owner > SuperAdmin > Admin > Provider > User`. Policies live in `Program.cs`; fine-grained checks live in `{Module}PermissionCatalog`.

---

## High-level architecture

```mermaid
flowchart TD
    subgraph Host["YallaJo.Api (single process)"]
        MW["Middleware pipeline<br/>Serilog · Swagger · HTTPS · Localization · RateLimiter · Auth · SeoRedirect"]
        EP["Minimal API endpoints<br/>(14 × Map{Module}Endpoints)"]
        MED["MediatR<br/>Validation · reCAPTCHA · Logging behaviors"]
        DOMAIN["Domain layer<br/>Aggregates · DomainEvents"]
        INFRA["Infrastructure<br/>DbContext · Repos · UoW · OutboxWriter"]
        BG["BackgroundServices<br/>Outbox processor · cleanups · payout batching · scoring"]
        HUB["SignalR Hubs"]
    end

    DB[(SQL Server<br/>per-module schemas + Outbox + Inbox)]
    EXT["External services<br/>SMTP · OAuth · reCAPTCHA · Azure Translator · Payment gateway"]

    MW --> EP --> MED --> DOMAIN
    MED --> INFRA --> DB
    DOMAIN -. "raise events" .-> INFRA
    INFRA -. "write outbox" .-> DB
    BG --> DB
    BG -. "dispatch integration events" .-> MED
    INFRA --> EXT
    HUB --> DB
```

See [`workflows/17-outbox-inbox-eventing.md`](./workflows/17-outbox-inbox-eventing.md) for the eventing detail.

---

## Authentication & authorization (summary)

| Concern | Mechanism |
|---|---|
| Identity | JWT Bearer (symmetric key, `Jwt:Key/Issuer/Audience`) |
| Session | `Session` + `RefreshToken` per device (rotated) |
| Email verify / password reset | `Otp`, `ActivationToken`, `PasswordResetToken` (state-machine entities) |
| External login | Google / Facebook / Apple (`ExternalProvider` + nonce store on HybridCache) |
| Bot protection | Google reCAPTCHA pipeline behavior on sensitive commands |
| Role policies | `Owner`, `SuperAdmin`, `Admin` (defined in `Program.cs`) |
| Permissions | Module-scoped catalogs (`AuthPermissionCatalog`, `BookingPermissionCatalog`, …) |
| Ownership | Domain-level guards (`ITourOwnershipService`, `IBlogOwnershipService`, …) |
| Audit | `IAdminAuditWriter` → Analytics `AuditLog` |

---

## External integrations

| Integration | Status | Code reference |
|---|---|---|
| SQL Server | Live | All `*.Infrastructure/Persistence` |
| Gmail SMTP (Auth emails) | Live | `Auth.Infrastructure/Services/GmailEmailService.cs` |
| SMTP (Messaging) | Live | `Messaging.Infrastructure/Services/SmtpEmailSender.cs` |
| Google reCAPTCHA | Live | `Auth.Infrastructure/Recaptcha/GoogleRecaptchaVerifier.cs` |
| Google / Facebook / Apple OAuth | Live (scaffolded) | `Auth.Infrastructure/ExternalAuth/*` |
| Azure Translator | Live | `ContentCore.Infrastructure/Services/AzureTranslateService.cs` |
| Local file storage | Live | `ContentCore.Infrastructure/Services/LocalFileStorageService.cs`, `Finance.Infrastructure/Storage/LocalFileInvoiceStorage.cs` |
| QuestPDF | Live | `Finance.Infrastructure/Pdf/QuestPdfInvoiceRenderer.cs` |
| SignalR | Live (NotificationHub only) | `Messaging.Presentation/Hubs/NotificationHub.cs` |
| OpenTelemetry | Live | `YallaJo.Api/Extensions/OpenTelemetryExtensions.cs` |
| Payment gateway | **Stub** | `Finance.Infrastructure/Gateways/FakePaymentGateway.cs` |
| Weather provider | **Stub** | `ContentSeo.Infrastructure/Weather/NoOpWeatherProvider.cs` |
| Search Console pinger | **Stub** | `ContentSeo.Infrastructure/Sitemap/NoOpSearchConsolePinger.cs` |
| Push (FCM/APNs) | Planned | — |
| AI chatbot LLM | Planned | — |

See [risk register](./risks/risk-register.md) for the implications of each stub.

---

## Cross-references

- Architecture decisions: [`../Agents/decisions/`](../Agents/decisions/) (ADR-001 .. ADR-008)
- Endpoint catalog & business rules: [`../Agents/YallaJo.md`](../Agents/YallaJo.md)
- Code conventions: [`../Agents/guide.md`](../Agents/guide.md)
- Build state & gotchas: [`../Agents/agent-context.md`](../Agents/agent-context.md)
- Patterns: [`../Agents/patterns/`](../Agents/patterns/) (caching, error-handling, polly)
