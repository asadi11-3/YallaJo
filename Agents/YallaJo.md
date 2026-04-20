
# YallaJo — Architecture Recommendations

> Comprehensive API Endpoints, Middleware, Background Services, SignalR Hubs, and Translation Interface Design
> Covering all 4 phases of the YallaJo Business Rules

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
   - [Existing Stack](#existing-stack)
   - [Modules (14 total)](#modules-14-total)
   - [Conventions to Follow](#conventions-to-follow)
2. [Middleware Pipeline](#middleware-pipeline)
   - [Recommended Additions](#recommended-additions)
3. [Background Services](#background-services)
4. [SignalR Hubs](#signalr-hubs)
5. [Translation Interface Design](#translation-interface-design)
6. [API Endpoints (Grouped by Phase)](#api-endpoints-grouped-by-phase)
   - [Phase 1: Core Features](#phase-1-core-features)
   - [Phase 2: Experience & Discovery](#phase-2-experience--discovery)
   - [Phase 3: Growth & Monetization](#phase-3-growth--monetization)
   - [Phase 4: Advanced Features & AI](#phase-4-advanced-features--ai)
7. [Business Logic Reference](#business-logic-reference)
8. [Phase Endpoints Implementation Plan](#phase-endpoints-implementation-plan)
   - [Phase 1: Core Features (MVP)](#phase-1-core-features-mvp)
   - [Phase 2: Experience & Discovery](#phase-2-experience--discovery-1)
   - [Phase 3: Growth & Monetization](#phase-3-growth--monetization-1)
   - [Phase 4: Advanced Features & AI](#phase-4-advanced-features--ai-1)
9. [MVP Project Process Suggestion](#mvp-project-process-suggestion)
   - [MVP Scope (The "Golden Path")](#mvp-scope-the-golden-path)
   - [Step-by-Step Implementation Plan](#step-by-step-implementation-plan)
   - [MVP Release Criteria](#mvp-release-criteria)
10. [Summary Statistics](#summary-statistics)
---

## Architecture Overview

### Existing Stack

| Component | Technology |
|---|---|
| Framework | .NET 8+ Modular Monolith |
| Architecture | Clean Architecture (5 layers per module) |
| API Style | Minimal API with route groups |
| CQRS | MediatR (ICommand/IQuery with Result<T>) |
| Validation | FluentValidation (pipeline behavior) |
| ORM | EF Core with SQL Server |
| Auth | JWT Bearer (symmetric key) |
| Domain Events | Outbox/Inbox pattern via CompositeOutboxProcessor |
| Error Handling | IExceptionHandler + RFC 7807 ProblemDetails |

### Modules (14 total)

| Module | Schema | Status | Tables |
|---|---|---|---|
| Auth | `auth` | Implemented (endpoints exist) | 5 |
| Security | `security` | Implemented (entities exist) | 7 |
| Accounts | `accounts` | Implemented (entities exist) | 3 |
| ContentCore | `content_core` | Entities created, endpoints empty | ~12 |
| ContentPlaces | `content_places` | Entities created, endpoints empty | ~6 |
| ContentTours | `content_tours` | Entities created, endpoints empty | ~8 |
| ContentBlogs | `content_blogs` | Entities created, endpoints empty | ~5 |
| ContentSeo | `content_seo` | Entities created, endpoints empty | ~4 |
| Booking | `booking` | Entities created, endpoints empty | 11 |
| Finance | `finance` | Entities created, endpoints empty | 16 |
| Messaging | `messaging` | Entities created, endpoints empty | 8 |
| Social | `social` | Entities created, endpoints empty | 5 |
| Tracking | `tracking` | Entities created, endpoints empty | 3 |
| Analytics | `analytics` | Entities created, endpoints empty | 6 |

### Conventions to Follow

- **Endpoint file**: `{Module}Endpoints.cs` with `Map{Module}Endpoints(this IEndpointRouteBuilder)` extension
- **Route base**: `/api/{module-kebab}` with `.WithTags("{ModuleName}")`
- **Commands**: `Commands/{Action}/{ActionCommand.cs, ActionCommandHandler.cs, ActionCommandValidator.cs}`
- **Queries**: `Queries/{Action}/{ActionQuery.cs, ActionQueryHandler.cs}`
- **Result pattern**: All handlers return `Result<T>` or `Result`, mapped to `IResult` via `ToApiResult()`
- **Auth**: `.RequireAuthorization()` (default) or `.AllowAnonymous()` — policy-based for admin endpoints
- **DI**: `Add{Module}Application()` (MediatR + FluentValidation) + `Add{Module}Infrastructure(IConfiguration)` (DbContext, UnitOfWork, Repos, OutboxProcessor)

---

## Middleware Pipeline

### Current Pipeline (Program.cs)

```
1. UseExceptionHandler()          — Global exception → RFC 7807
2. UseStatusCodePages()            — Non-exception status codes
3. UseSwagger / UseSwaggerUI       — Dev only
4. UseHttpsRedirection()           — HTTPS enforcement
5. UseAuthentication()             — JWT Bearer validation
6. UseAuthorization()              — Policy enforcement
7. Module endpoints                — Minimal API route groups
8. MapHealthChecks("/health")      — Liveness probe
```

### Recommended Additions

Add these to the pipeline **in order** — placement matters:

#### 1. Correlation ID Middleware (Add BEFORE ExceptionHandler)

**Purpose**: Attach a unique `X-Correlation-Id` header to every request/response for distributed tracing and log correlation.

- Generate `Guid.CreateVersion7()` if no incoming header
- Store in `IRequestContext` for downstream use
- Attach to all outgoing responses and log scopes
- **Location**: `YallaJo.Api/Middleware/CorrelationIdMiddleware.cs`

```
Pipeline position: FIRST (before everything)
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
...rest of pipeline
```

#### 2. Request Localization Middleware (Add AFTER UseHttpsRedirection)

**Purpose**: Parse `Accept-Language` header and set `CultureInfo` for the request. Used by translation system and any localized responses.

- Support: `ar` (Arabic), `en` (English) — extensible via `Languages` table
- Default fallback: `en`
- Set both `CurrentCulture` and `CurrentUICulture`
- Expose via `IRequestContext.Language` (ISO 639-1 code)
- **Location**: `YallaJo.Api/Middleware/RequestLocalizationMiddleware.cs`

```
app.UseHttpsRedirection();
app.UseRequestLocalization();  // NEW
app.UseAuthentication();
```

#### 3. Rate Limiting Middleware (Add AFTER UseAuthentication)

**Purpose**: Throttle API requests per user/IP to prevent abuse. Business rules specify rate limits for:
- OTP requests: max 5 per email per hour
- Chatbot: max 50 messages per user per hour
- Search: max 100 requests per minute per IP
- General API: max 1000 requests per minute per authenticated user

- Use `System.Threading.RateLimiting` (built-in .NET 8)
- Define named policies: `"otp"`, `"chatbot"`, `"search"`, `"general"`
- Apply per-endpoint via `.RequireRateLimiting("policy")`
- **Location**: `YallaJo.Api/Configuration/RateLimitingConfiguration.cs`

```
app.UseAuthentication();
app.UseRateLimiter();  // NEW
app.UseAuthorization();
```

#### 4. SEO Redirect Middleware (Add BEFORE endpoint mapping)

**Purpose**: Handle 301/302 redirects from `content_seo.SeoMetadata` table for URL management.

- Check incoming path against `SeoRedirects` table
- Return 301 (permanent) or 302 (temporary) redirects
- Cache redirect rules (5-minute sliding expiration)
- Only apply to non-API paths (skip `/api/*`)
- **Location**: `YallaJo.Api/Middleware/SeoRedirectMiddleware.cs`

```
app.UseAuthorization();
app.UseMiddleware<SeoRedirectMiddleware>();  // NEW
// Module endpoints...
```

#### 5. CORS Configuration (Add in builder.Services + pipeline)

**Purpose**: Enable cross-origin requests for the frontend SPA and mobile apps.

```csharp
// In builder.Services:
builder.Services.AddCors(options =>
{
    options.AddPolicy("YallaJoPolicy", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>()!)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();  // Required for SignalR
    });
});

// In pipeline (AFTER UseHttpsRedirection, BEFORE UseAuthentication):
app.UseCors("YallaJoPolicy");
```

#### 6. Response Compression Middleware

**Purpose**: Compress JSON responses to reduce bandwidth. Important for map data, tour listings, and search results.

```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
});

// Pipeline: AFTER CORS, BEFORE Authentication
app.UseResponseCompression();
```

### Recommended Final Pipeline Order

```
 1. UseMiddleware<CorrelationIdMiddleware>()    — NEW: Request tracing
 2. UseExceptionHandler()                        — Existing: Global errors
 3. UseStatusCodePages()                         — Existing: Status codes
 4. UseSwagger / UseSwaggerUI                    — Existing: Dev only
 5. UseHttpsRedirection()                        — Existing: HTTPS
 6. UseRequestLocalization()                     — NEW: Accept-Language
 7. UseResponseCompression()                     — NEW: Gzip/Brotli
 8. UseCors("YallaJoPolicy")                     — NEW: Cross-origin
 9. UseAuthentication()                          — Existing: JWT Bearer
10. UseRateLimiter()                             — NEW: Throttling
11. UseAuthorization()                           — Existing: Policies
12. UseMiddleware<SeoRedirectMiddleware>()       — NEW: URL redirects
13. Module endpoints                             — Existing: Route groups
14. MapHealthChecks("/health")                   — Existing: Liveness
15. MapHub<NotificationHub>("/hubs/notifications") — NEW: SignalR
16. MapHub<LiveTrackingHub>("/hubs/tracking")    — NEW: SignalR
17. MapHub<ChatBotHub>("/hubs/chatbot")          — NEW: SignalR
```

### Additional Exception Handlers

Beyond the existing `ValidationExceptionHandler` and `DbUpdateExceptionHandler`, add:

| Handler | Exception | HTTP Status | When |
|---|---|---|---|
| `NotFoundExceptionHandler` | Custom `NotFoundException` | 404 | Entity not found by ID |
| `ForbiddenExceptionHandler` | Custom `ForbiddenException` | 403 | Provider accessing another provider's data |
| `ConflictExceptionHandler` | Custom `ConflictException` | 409 | State machine transition violations |
| `RateLimitExceptionHandler` | `RateLimitRejectedException` | 429 | Rate limit exceeded |
| `ExternalServiceExceptionHandler` | Custom `ExternalServiceException` | 502 | Translation API, Weather API, Payment gateway failures |

---

## Background Services

### Existing Background Service

- **CompositeOutboxProcessor** (SharedKernel.Infrastructure) — Processes outbox messages for all modules every 10 seconds. Already registered.

### Recommended New Background Services

> **Pattern**: Each background service should extend `BackgroundService` and be registered via `AddHostedService<T>()` in the host API project or the owning module's Infrastructure DI.
>
> For scheduled jobs, use `PeriodicTimer` or integrate Quartz.NET / Hangfire for cron-like scheduling.
>
> **Recommendation**: Use `TimeProvider` abstraction (already registered as `IDateTimeProvider`) for testability.

#### Booking Module Background Services

| # | Service | Schedule | Description | Module |
|---|---------|----------|-------------|--------|
| 1 | **SlotLockCleanupService** | Every 5 minutes | Delete expired `SlotLocks` (TTL = 10 min). Releases unpaid reservation slots back to availability. Query: `WHERE ExpiresAt < UtcNow AND Status = Locked`. | Booking |
| 2 | **BookingAutoExpireService** | Every 5 minutes | Auto-cancel bookings stuck in `AwaitingPayment` for >10 min. Transition to `Cancelled` and release slots. Publish `BookingCancelledIntegrationEvent`. | Booking |
| 3 | **ProviderAutoAcceptService** | Every 15 minutes | Auto-confirm bookings waiting provider confirmation for >24h (non-instant booking). Transition `PendingConfirmation` → `Confirmed`. Notify both parties. | Booking |
| 4 | **DocumentExpiryCheckService** | Daily at 01:00 UTC | Check `ProviderDocuments.ExpiresAt` against today. Mark expired docs, notify provider to re-upload. If critical doc expired, suspend provider. | Booking |

#### Finance Module Background Services

| # | Service | Schedule | Description | Module |
|---|---------|----------|-------------|--------|
| 5 | **PayoutBatchingService** | Weekly — Sunday midnight UTC | Aggregate completed bookings into provider payout batches. Group by provider + currency. Create `Payout` + `PayoutItem` records. Mark as `Pending`. | Finance |
| 6 | **RefundRetryService** | Every 15 minutes | Retry failed refund transactions (where `Status = Failed AND RetryCount < MaxRetries`). Call payment gateway. Exponential backoff. | Finance |
| 7 | **SubscriptionRenewalService** | Daily at 00:00 UTC | Check expiring subscriptions. Auto-renew if auto-renew enabled and payment succeeds. Downgrade to Free if payment fails after retries. | Finance |
| 8 | **LoyaltyPointsExpiryService** | Daily at 02:00 UTC | Expire loyalty points past their expiry date (FIFO). Deduct from oldest batches first. Notify users 7 days before expiry. | Finance |
| 9 | **DiscountLifecycleService** | Every 30 minutes | Activate discounts where `ValidFrom <= UtcNow AND Status = Pending`. Deactivate/expire discounts where `ValidTo < UtcNow AND Status = Active`. Recalculate `SalePrice` on affected tours. | Finance |

#### ContentSeo Module Background Services

| # | Service | Schedule | Description | Module |
|---|---------|----------|-------------|--------|
| 10 | **SitemapRegenerationService** | Every 6 hours | Regenerate XML sitemap from all published content (tours, places, blogs). Include hreflang alternates for ar/en. Write to static file or cache. | ContentSeo |
| 11 | **WeatherPreFetchService** | Daily at 05:00 UTC | Pre-fetch weather data for top 50 tour locations. Cache in `WeatherCache` table (12h TTL). Respect API budget (1000 calls/day). | ContentSeo |

#### Analytics Module Background Services

| # | Service | Schedule | Description | Module |
|---|---------|----------|-------------|--------|
| 12 | **PopularityScoreCalculationService** | Every 6 hours | Recalculate `PopularityScores` for all tours and places. Factors: booking count, review count, average rating, view count, favorite count, recency bias. | Analytics |
| 13 | **RecommendationEngineService** | Daily at 02:00 UTC | Run hybrid recommendation algorithm (collaborative 40% + content-based 35% + popularity 25%). Update `RecommendationCache` per user. Process in batches of 100 users. | Analytics |
| 14 | **RatingRecalculationService** | Daily at 03:00 UTC | Recalculate Bayesian weighted average ratings for all tours. Factor in: rating value, recency, reviewer trust score, verified booking badge. Update denormalized `AverageRating` columns. | Analytics |

#### Messaging Module Background Services

| # | Service | Schedule | Description | Module |
|---|---------|----------|-------------|--------|
| 15 | **EmailNotificationSenderService** | Continuous (queue-based) | Process pending email notifications. Use transactional email service. Retry with exponential backoff (max 3 retries). Respect per-user notification preferences. | Messaging |
| 16 | **ReadNotificationCleanupService** | Weekly — Sunday 02:00 UTC | Delete read notifications older than 30 days. Keep unread indefinitely. | Messaging |

#### Social Module Background Services

| # | Service | Schedule | Description | Module |
|---|---------|----------|-------------|--------|
| 17 | **OrphanedFavoritesCleanupService** | Weekly — Saturday 03:00 UTC | Remove favorites pointing to soft-deleted or deactivated tours/places. | Social |

#### Tracking Module Background Services

| # | Service | Schedule | Description | Module |
|---|---------|----------|-------------|--------|
| 18 | **LocationSnapshotPurgeService** | Daily at 04:00 UTC | Delete `LocationSnapshots` older than 30 days (data retention policy). Keep aggregated session data. | Tracking |

### Background Service Registration Pattern

```csharp
// Option A: Register in owning module's Infrastructure DI
public static IServiceCollection AddBookingInfrastructure(
    this IServiceCollection services, IConfiguration configuration)
{
    // ...existing registrations...
    services.AddHostedService<SlotLockCleanupService>();
    services.AddHostedService<BookingAutoExpireService>();
    return services;
}

// Option B: Register in Program.cs for cross-cutting services
builder.Services.AddHostedService<CompositeOutboxProcessor>();  // already exists
```

### Scheduling Recommendation

For the 18 background services above, there are two approaches:

| Approach | Pros | Cons |
|---|---|---|
| **PeriodicTimer (built-in)** | No extra dependency, simple, already used by CompositeOutboxProcessor | No cron syntax, no dashboard, no persistence |
| **Quartz.NET / Hangfire** | Cron expressions, dashboard UI, job persistence, retry policies, distributed locking | Extra dependency, more complexity |

**Recommendation**: Start with `PeriodicTimer` for all services (consistency with existing `CompositeOutboxProcessor` pattern). Migrate to Quartz.NET later if a dashboard or cron precision becomes needed.

---

## SignalR Hubs

### Overview

Three SignalR hubs are needed based on business rules. All use JWT Bearer auth for WebSocket connections.

```csharp
// Program.cs additions:
builder.Services.AddSignalR();

// Pipeline additions (AFTER module endpoints):
app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization();
app.MapHub<LiveTrackingHub>("/hubs/tracking").RequireAuthorization();
app.MapHub<ChatBotHub>("/hubs/chatbot").RequireAuthorization();
```

### Hub 1: NotificationHub

**Module**: Messaging
**Location**: `Messaging.Infrastructure/Hubs/NotificationHub.cs`
**Purpose**: Real-time push notifications to connected users (bell icon, toast messages).

#### Server → Client Methods (Hub sends to clients)

| Method | Payload | When Triggered |
|---|---|---|
| `ReceiveNotification` | `{ id, type, title, body, data, createdAt }` | Any notification event (booking confirmed, payment received, new review, etc.) |
| `NotificationRead` | `{ notificationId }` | Notification marked as read (sync across devices) |
| `UnreadCountUpdated` | `{ count }` | After any notification read/received — updates badge count |

#### Client → Server Methods (Clients call hub)

| Method | Parameters | Description |
|---|---|---|
| `MarkAsRead` | `notificationId: Guid` | Mark single notification as read |
| `MarkAllAsRead` | — | Mark all notifications as read |

#### Groups

- Each user joins group `user:{userId}` on connection
- Providers additionally join `provider:{providerId}` for provider-specific notifications
- Admins join `admin` group for moderation alerts

#### Connection Lifecycle

```csharp
public override async Task OnConnectedAsync()
{
    var userId = Context.User!.FindFirst("sub")!.Value;
    await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
    // Send unread count on connect
    var count = await _notificationService.GetUnreadCountAsync(Guid.Parse(userId));
    await Clients.Caller.SendAsync("UnreadCountUpdated", new { count });
}
```

---

### Hub 2: LiveTrackingHub

**Module**: Tracking
**Location**: `Tracking.Infrastructure/Hubs/LiveTrackingHub.cs`
**Purpose**: Real-time GPS broadcasting during live tours. Tour guide sends location, booked participants receive it.

#### Server → Client Methods

| Method | Payload | When Triggered |
|---|---|---|
| `LocationUpdate` | `{ sessionId, lat, lng, altitude, speed, heading, accuracy, timestamp }` | Tour guide sends GPS coordinates (every 5-15 seconds) |
| `CheckpointReached` | `{ sessionId, checkpointId, name, arrivedAt }` | Guide reaches a tour waypoint |
| `SessionStarted` | `{ sessionId, tourId, tourName, guideId }` | Tour tracking session begins |
| `SessionEnded` | `{ sessionId, endedAt, reason }` | Tour tracking session ends (completed/abandoned) |
| `GuideOffline` | `{ sessionId, lastSeenAt }` | Guide disconnected for >30 seconds |

#### Client → Server Methods

| Method | Parameters | Description |
|---|---|---|
| `SendLocation` | `lat, lng, altitude, speed, heading, accuracy` | Tour guide sends GPS update (guide-only) |
| `ReachCheckpoint` | `sessionId, checkpointId` | Tour guide marks checkpoint reached |
| `JoinSession` | `sessionId: Guid` | Participant subscribes to a live session |
| `LeaveSession` | `sessionId: Guid` | Participant unsubscribes |
| `StartSession` | `tourId, bookingId` | Guide starts tracking (creates LiveTrackingSession) |
| `EndSession` | `sessionId, reason` | Guide ends tracking |

#### Groups

- `session:{sessionId}` — All participants watching a specific live tour
- Only verified tour guide for the booking can `SendLocation`
- Participants can only join sessions for their own bookings

#### Offline Handling

- Store latest GPS in memory (Redis or in-memory cache) for late-joining participants
- Batch-persist `LocationSnapshots` every 30 seconds (not every GPS ping) to avoid DB overload
- If guide disconnects >30s, notify participants via `GuideOffline`
- 30-day retention policy for `LocationSnapshots` (handled by `LocationSnapshotPurgeService`)

---

### Hub 3: ChatBotHub

**Module**: Messaging
**Location**: `Messaging.Infrastructure/Hubs/ChatBotHub.cs`
**Purpose**: Real-time AI chatbot conversation. User sends messages, receives streaming AI responses.

#### Server → Client Methods

| Method | Payload | When Triggered |
|---|---|---|
| `ReceiveMessage` | `{ conversationId, messageId, content, role, timestamp }` | Chatbot response (complete or streamed) |
| `StreamChunk` | `{ conversationId, chunk, isLast }` | Streaming token-by-token for long responses |
| `TypingIndicator` | `{ conversationId, isTyping }` | Show/hide typing animation |
| `HandoffToHuman` | `{ conversationId, reason, ticketId }` | Bot escalates to human support agent |
| `ConversationClosed` | `{ conversationId, reason }` | Conversation ended (timeout, resolved, escalated) |

#### Client → Server Methods

| Method | Parameters | Description |
|---|---|---|
| `SendMessage` | `conversationId?, message` | User sends a message (creates conversation if null) |
| `CloseConversation` | `conversationId` | User ends conversation |
| `RateResponse` | `messageId, rating (1-5)` | User rates a chatbot response for feedback |

#### Integration with External LLM

```csharp
// Abstract interface (same pattern as ITranslationService)
public interface IChatbotProvider
{
    Task<string> GetResponseAsync(ChatContext context, CancellationToken ct);
    IAsyncEnumerable<string> StreamResponseAsync(ChatContext context, CancellationToken ct);
}

// Rate limiting: 50 messages per user per hour
// Handoff trigger: User says "talk to human" or bot confidence < threshold
// Context: Last 10 messages + user profile + current booking context
```

---

## Translation Interface Design

### Requirements (from business rules)

- Multi-language support: Arabic (ar) + English (en) minimum, extensible
- Translation tables exist in DB: `CategoryTranslations`, `PlaceTranslations`, `TourTranslations`, `BlogTranslations`, `FaqItemTranslations`, `BusinessTranslations`
- User wants: **abstract interface** → external API call → **auto-save to DB**
- Languages managed via `content_core.Languages` table

### Interface Design

**Location**: `YallaJo.SharedKernel.Application/Abstractions/Translation/`

```csharp
// ITranslationService.cs — in SharedKernel.Application
public interface ITranslationService
{
    /// <summary>
    /// Translate a single text from source language to target language.
    /// </summary>
    Task<TranslationResult> TranslateAsync(
        string text,
        string fromLanguageCode,
        string toLanguageCode,
        CancellationToken ct = default);

    /// <summary>
    /// Translate multiple texts in a single API call (batch optimization).
    /// </summary>
    Task<IReadOnlyList<TranslationResult>> BatchTranslateAsync(
        IReadOnlyList<string> texts,
        string fromLanguageCode,
        string toLanguageCode,
        CancellationToken ct = default);

    /// <summary>
    /// Detect the language of a given text.
    /// </summary>
    Task<string> DetectLanguageAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Get all supported language codes.
    /// </summary>
    Task<IReadOnlyList<SupportedLanguage>> GetSupportedLanguagesAsync(CancellationToken ct = default);
}

public sealed record TranslationResult(
    string OriginalText,
    string TranslatedText,
    string FromLanguage,
    string ToLanguage,
    double? Confidence);

public sealed record SupportedLanguage(string Code, string Name, string NativeName);
```

### Implementation Architecture

```
┌──────────────────┐     ┌─────────────────────────┐     ┌──────────────────┐
│  API Endpoint    │ →   │ ITranslationService       │ →   │  External API    │
│  (Command/Query) │     │ (SharedKernel.Application)│     │  (Google/Azure)  │
└──────────────────┘     └─────────────────────────┘     └──────────────────┘
                                    │
                                    ▼
                         ┌─────────────────────────┐
                         │  Auto-Save to DB          │
                         │  ({Entity}Translations)    │
                         └─────────────────────────┘
```

### Auto-Save Translation Service (Decorator Pattern)

**Location**: `ContentCore.Infrastructure/Services/AutoSaveTranslationService.cs`

```csharp
/// <summary>
/// Decorates any ITranslationService implementation to auto-persist
/// translations to the database after successful API translation.
/// </summary>
public sealed class AutoSaveTranslationService : ITranslationService
{
    private readonly ITranslationService _inner;  // The actual API-calling implementation
    private readonly ITranslationRepository _repository;
    private readonly IContentCoreUnitOfWork _unitOfWork;

    public async Task<TranslationResult> TranslateAsync(
        string text, string fromLang, string toLang, CancellationToken ct)
    {
        // 1. Check DB cache first
        var cached = await _repository.FindTranslationAsync(text, fromLang, toLang, ct);
        if (cached is not null)
            return cached;

        // 2. Call external API
        var result = await _inner.TranslateAsync(text, fromLang, toLang, ct);

        // 3. Auto-save to DB
        await _repository.SaveTranslationAsync(result, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return result;
    }
    // ...BatchTranslateAsync follows same pattern...
}
```

### Concrete Implementations

```csharp
// Abstract provider in SharedKernel.Infrastructure:
// GoogleTranslateService : ITranslationService
// AzureTranslateService : ITranslationService
// DeepLTranslateService : ITranslationService

// DI Registration (ContentCore.Infrastructure):
services.AddHttpClient<GoogleTranslateService>();
services.AddScoped<ITranslationService>(sp =>
    new AutoSaveTranslationService(
        inner: sp.GetRequiredService<GoogleTranslateService>(),
        repository: sp.GetRequiredService<ITranslationRepository>(),
        unitOfWork: sp.GetRequiredService<IContentCoreUnitOfWork>()));
```

### Translation Workflow for Content

When a provider creates/updates a tour, place, or blog:

1. **Content saved** in primary language (e.g., Arabic)
2. **Integration event published**: `ContentCreatedIntegrationEvent { entityType, entityId, languageCode, fields[] }`
3. **Translation handler** (in ContentCore.Infrastructure) receives event:
   - Calls `ITranslationService.BatchTranslateAsync()` for all text fields
   - Saves `{Entity}Translation` records for each target language
4. **Translation status** tracked: `Pending → AutoTranslated → HumanReviewed`
5. **Admin can override**: Manual edit of auto-translated content via admin endpoint

### Translation API Endpoints (in ContentCore)

| Method | Route | Description | Auth |
|---|---|---|---|
| POST | `/api/content-core/translations/translate` | On-demand translation (admin tool) | Admin |
| POST | `/api/content-core/translations/batch` | Batch translate multiple texts | Admin |
| GET | `/api/content-core/translations/{entityType}/{entityId}` | Get all translations for entity | Public |
| PUT | `/api/content-core/translations/{id}` | Update/override a translation | Admin |
| POST | `/api/content-core/translations/{id}/approve` | Mark human-reviewed | Admin |
| GET | `/api/content-core/languages` | List active languages | Public |
| POST | `/api/content-core/languages` | Add new language | Admin |
| PUT | `/api/content-core/languages/{id}` | Update language settings | Admin |

---

## API Endpoints (Grouped by Phase)

> **Legend**:
> - **Auth**: 🔒 = RequireAuthorization, 🌐 = AllowAnonymous, 👑 = Admin only, 🏢 = Provider only

### Phase 1: Core Features

*Authentication, Security, Provider Registration, Core Content (Places/Tours), Bookings, Payments, Reviews.*

| Module | Method | Route | Description | Auth |
|---|--------|-------|-------------|------|
| **Auth Module** | POST | `/verify-email` | Verify email with OTP code → returns tokens | 🌐 |
| **Auth Module** | POST | `/login` | Email + password login → returns tokens | 🌐 |
| **Auth Module** | POST | `/refresh` | Refresh access token | 🌐 |
| **Auth Module** | POST | `/logout` | Revoke refresh token + session | 🔒 |
| **Auth Module** | POST | `/logout-all` | Revoke all sessions for user | 🔒 |
| **Auth Module** | POST | `/register` | Register new user (email + password) → sends OTP | 🌐 |
| **Auth Module** | POST | `/forgot-password` | Initiate password reset → sends OTP to email | 🌐 |
| **Auth Module** | POST | `/reset-password` | Reset password with OTP verification | 🌐 |
| **Auth Module** | POST | `/change-password` | Change password (requires current password) | 🔒 |
| **Auth Module** | POST | `/resend-otp` | Resend OTP to email (rate limited: 5/hour) | 🌐 |
| **Auth Module** | POST | `/external/google` | OAuth login/register via Google | 🌐 |
| **Auth Module** | POST | `/external/facebook` | OAuth login/register via Facebook | 🌐 |
| **Auth Module** | POST | `/external/apple` | OAuth login/register via Apple | 🌐 |
| **Auth Module** | GET | `/sessions` | List active sessions for current user | 🔒 |
| **Auth Module** | DELETE | `/sessions/{id}` | Revoke a specific session | 🔒 |
| **Auth Module** | GET | `/devices` | List registered devices | 🔒 |
| **Security Module** | GET | `/users` | List all users (paginated, filterable) | 👑 |
| **Security Module** | GET | `/users/{id}` | Get user details by ID | 👑 |
| **Security Module** | PUT | `/users/{id}/status` | Activate/deactivate user | 👑 |
| **Security Module** | POST | `/users/{id}/roles` | Assign role to user | 👑 |
| **Security Module** | DELETE | `/users/{id}/roles/{roleName}` | Remove role from user | 👑 |
| **Security Module** | GET | `/roles` | List all roles | 👑 |
| **Security Module** | POST | `/roles` | Create a new role | 👑 |
| **Security Module** | PUT | `/roles/{id}` | Update role | 👑 |
| **Security Module** | DELETE | `/roles/{id}` | Delete role | 👑 |
| **Security Module** | GET | `/roles/{id}/claims` | Get claims for a role | 👑 |
| **Security Module** | POST | `/roles/{id}/claims` | Add claim to role | 👑 |
| **Accounts Module** | GET | `/profile` | Get current user's profile | 🔒 |
| **Accounts Module** | PUT | `/profile` | Update profile (name, bio, avatar, preferences) | 🔒 |
| **Accounts Module** | POST | `/profile/avatar` | Upload profile avatar | 🔒 |
| **Accounts Module** | GET | `/profile/{userId}` | Get public profile by user ID | 🌐 |
| **Accounts Module** | POST | `/provider/register` | Register as service provider (TourOperator/IndependentGuide/HotelResort/ActivityCenter) | 🔒 |
| **Accounts Module** | GET | `/provider/status` | Get provider registration status (state machine) | 🔒 |
| **Accounts Module** | POST | `/provider/documents` | Upload provider verification documents | 🔒 |
| **Accounts Module** | PUT | `/provider/documents/{id}` | Re-upload expired/rejected document | 🔒 |
| **Accounts Module** | GET | `/provider/dashboard` | Provider dashboard summary (bookings, earnings, rating) | 🏢 |
| **Accounts Module** | POST | `/provider/apply` | Submit provider application for review | 🔒 |
| **Accounts Module** | GET | `/admin/providers` | List all provider applications (paginated) | 👑 |
| **Accounts Module** | POST | `/admin/providers/{id}/approve` | Approve provider application | 👑 |
| **Accounts Module** | POST | `/admin/providers/{id}/reject` | Reject provider application (with reason) | 👑 |
| **Accounts Module** | POST | `/admin/providers/{id}/request-docs` | Request additional documents | 👑 |
| **Accounts Module** | POST | `/admin/providers/{id}/suspend` | Suspend active provider | 👑 |
| **ContentCore Module** | GET | `/languages` | List all active languages | 🌐 |
| **ContentCore Module** | POST | `/languages` | Add a new language | 👑 |
| **ContentCore Module** | PUT | `/languages/{id}` | Update language (name, RTL, active status) | 👑 |
| **ContentCore Module** | GET | `/categories` | List categories (tree structure, with translations) | 🌐 |
| **ContentCore Module** | GET | `/categories/{id}` | Get single category with subcategories | 🌐 |
| **ContentCore Module** | POST | `/categories` | Create category (with parent for hierarchy) | 👑 |
| **ContentCore Module** | PUT | `/categories/{id}` | Update category | 👑 |
| **ContentCore Module** | DELETE | `/categories/{id}` | Soft-delete category | 👑 |
| **ContentCore Module** | PUT | `/categories/reorder` | Reorder categories (batch sort order update) | 👑 |
| **ContentCore Module** | GET | `/tags` | List all tags (filterable, paginated) | 🌐 |
| **ContentCore Module** | POST | `/tags` | Create tag | 👑 |
| **ContentCore Module** | PUT | `/tags/{id}` | Update tag | 👑 |
| **ContentCore Module** | DELETE | `/tags/{id}` | Delete tag | 👑 |
| **ContentCore Module** | POST | `/attachments` | Upload attachment (image, video, document) | 🔒 |
| **ContentCore Module** | DELETE | `/attachments/{id}` | Delete attachment | 🔒 |
| **ContentCore Module** | GET | `/attachments/{entityType}/{entityId}` | Get all attachments for an entity | 🌐 |
| **ContentCore Module** | POST | `/attachments/{entityType}/{entityId}/images` | Upload and assign images to entity | 🔒 |
| **ContentCore Module** | PUT | `/attachments/{entityType}/{entityId}/images/reorder` | Reorder images, set primary | 🔒 |
| **ContentCore Module** | GET | `/specializations` | List all specializations | 🌐 |
| **ContentCore Module** | POST | `/specializations` | Create specialization | 👑 |
| **ContentCore Module** | PUT | `/specializations/{id}` | Update specialization | 👑 |
| **ContentPlaces Module** | GET | `/` | List places (paginated, filterable by category, location, rating) | 🌐 |
| **ContentPlaces Module** | GET | `/{id}` | Get place details (with translations, images, businesses) | 🌐 |
| **ContentPlaces Module** | GET | `/{slug}` | Get place by slug (SEO-friendly URL) | 🌐 |
| **ContentPlaces Module** | POST | `/` | Create a new place | 👑 |
| **ContentPlaces Module** | PUT | `/{id}` | Update place | 👑 |
| **ContentPlaces Module** | DELETE | `/{id}` | Soft-delete place | 👑 |
| **ContentPlaces Module** | GET | `/{id}/businesses` | List businesses at a place | 🌐 |
| **ContentPlaces Module** | POST | `/businesses` | Create business listing | 🏢 |
| **ContentPlaces Module** | PUT | `/businesses/{id}` | Update business listing | 🏢 |
| **ContentPlaces Module** | DELETE | `/businesses/{id}` | Soft-delete business | 👑 |
| **ContentPlaces Module** | GET | `/businesses/{id}/hours` | Get business hours | 🌐 |
| **ContentPlaces Module** | PUT | `/businesses/{id}/hours` | Set/update business hours (batch) | 🏢 |
| **ContentTours Module** | GET | `/` | List tours (paginated, filterable by category, place, price, date, rating) | 🌐 |
| **ContentTours Module** | GET | `/{id}` | Get tour details (with translations, schedules, pricing, images) | 🌐 |
| **ContentTours Module** | GET | `/{slug}` | Get tour by slug (SEO-friendly) | 🌐 |
| **ContentTours Module** | POST | `/` | Create tour (draft status) | 🏢 |
| **ContentTours Module** | PUT | `/{id}` | Update tour details | 🏢 |
| **ContentTours Module** | DELETE | `/{id}` | Soft-delete tour | 🏢 |
| **ContentTours Module** | POST | `/{id}/submit` | Submit tour for approval (Draft → Pending) | 🏢 |
| **ContentTours Module** | POST | `/admin/{id}/approve` | Approve tour (Pending → Approved) | 👑 |
| **ContentTours Module** | POST | `/admin/{id}/reject` | Reject tour (with reason) | 👑 |
| **ContentTours Module** | GET | `/{id}/schedules` | Get tour schedules/available dates | 🌐 |
| **ContentTours Module** | POST | `/{id}/schedules` | Create schedule (date, time, capacity) | 🏢 |
| **ContentTours Module** | PUT | `/{id}/schedules/{scheduleId}` | Update schedule | 🏢 |
| **ContentTours Module** | DELETE | `/{id}/schedules/{scheduleId}` | Delete schedule (if no bookings) | 🏢 |
| **ContentTours Module** | GET | `/{id}/pricing` | Get pricing tiers for a tour | 🌐 |
| **ContentTours Module** | POST | `/{id}/pricing` | Create pricing tier (adult, child, group, etc.) | 🏢 |
| **ContentTours Module** | PUT | `/{id}/pricing/{tierId}` | Update pricing tier | 🏢 |
| **ContentTours Module** | GET | `/{id}/guides` | List tour guides assigned to tour | 🌐 |
| **ContentTours Module** | POST | `/{id}/guides` | Assign tour guide | 🏢 |
| **ContentTours Module** | DELETE | `/{id}/guides/{guideId}` | Unassign tour guide | 🏢 |
| **ContentTours Module** | GET | `/search` | Full-text search tours (with filters, sorting, pagination) | 🌐 |
| **ContentTours Module** | GET | `/featured` | Get featured/promoted tours | 🌐 |
| **ContentTours Module** | GET | `/provider/my-tours` | List current provider's tours | 🏢 |
| **Booking Module** | POST | `/tour` | Create tour booking (lock slot → AwaitingPayment) | 🔒 |
| **Booking Module** | GET | `/{id}` | Get booking details (with payment status, provider info) | 🔒 |
| **Booking Module** | GET | `/my-bookings` | List current user's bookings (paginated, filterable by status) | 🔒 |
| **Booking Module** | POST | `/{id}/cancel` | Cancel booking (trigger refund if applicable) | 🔒 |
| **Booking Module** | POST | `/{id}/confirm` | Provider confirms non-instant booking | 🏢 |
| **Booking Module** | POST | `/{id}/reject` | Provider rejects non-instant booking (with reason) | 🏢 |
| **Booking Module** | POST | `/{id}/complete` | Mark booking as completed (after tour) | 🏢 |
| **Booking Module** | GET | `/provider/pending` | List pending bookings for provider | 🏢 |
| **Booking Module** | GET | `/provider/upcoming` | List upcoming confirmed bookings for provider | 🏢 |
| **Booking Module** | GET | `/provider/history` | Provider booking history | 🏢 |
| **Booking Module** | GET | `/admin/all` | List all bookings (admin view, filterable) | 👑 |
| **Booking Module** | POST | `/join-request` | Request to join an existing group booking | 🔒 |
| **Booking Module** | POST | `/join-request/{id}/approve` | Approve join request | 🏢 |
| **Booking Module** | POST | `/join-request/{id}/reject` | Reject join request | 🏢 |
| **Booking Module** (Availability & Slots Sub-routes) | GET | `/availability/{tourId}` | Get available dates/slots for a tour | 🌐 |
| **Booking Module** (Availability & Slots Sub-routes) | GET | `/availability/{tourId}/{date}` | Get slots for specific date (with remaining capacity) | 🌐 |
| **Booking Module** (Availability & Slots Sub-routes) | POST | `/availability/slots` | Create availability slot | 🏢 |
| **Booking Module** (Availability & Slots Sub-routes) | PUT | `/availability/slots/{id}` | Update slot (capacity, time) | 🏢 |
| **Booking Module** (Availability & Slots Sub-routes) | DELETE | `/availability/slots/{id}` | Delete slot (if no bookings) | 🏢 |
| **Booking Module** (Availability & Slots Sub-routes) | POST | `/availability/slots/bulk` | Bulk create recurring availability slots | 🏢 |
| **Booking Module** (Tour Guides Sub-routes) | GET | `/guides` | List tour guides (filterable by language, specialization, rating) | 🌐 |
| **Booking Module** (Tour Guides Sub-routes) | GET | `/guides/{id}` | Get tour guide profile | 🌐 |
| **Booking Module** (Tour Guides Sub-routes) | POST | `/guides` | Register as tour guide (provider creates guide profile) | 🏢 |
| **Booking Module** (Tour Guides Sub-routes) | PUT | `/guides/{id}` | Update tour guide profile | 🏢 |
| **Booking Module** (Tour Guides Sub-routes) | POST | `/guides/{id}/languages` | Add language to guide | 🏢 |
| **Booking Module** (Tour Guides Sub-routes) | DELETE | `/guides/{id}/languages/{langId}` | Remove language from guide | 🏢 |
| **Booking Module** (Tour Guides Sub-routes) | POST | `/guides/{id}/specializations` | Add specialization to guide | 🏢 |
| **Booking Module** (Tour Guides Sub-routes) | GET | `/refund-policies/{tourId}` | Get refund policy for a tour | 🌐 |
| **Booking Module** (Tour Guides Sub-routes) | POST | `/refund-policies` | Create refund policy | 🏢 |
| **Booking Module** (Tour Guides Sub-routes) | PUT | `/refund-policies/{id}` | Update refund policy | 🏢 |
| **Finance Module** | POST | `/payments/initiate` | Initiate payment for booking (returns payment URL/token) | 🔒 |
| **Finance Module** | POST | `/payments/webhook` | Payment gateway webhook (confirm/fail) | 🌐 |
| **Finance Module** | GET | `/payments/{id}` | Get payment details | 🔒 |
| **Finance Module** | GET | `/payments/my-payments` | List user's payment history | 🔒 |
| **Finance Module** | POST | `/payments/{id}/refund` | Initiate refund (full or partial) | 🔒 |
| **Finance Module** | GET | `/payments/admin/all` | List all payments (admin) | 👑 |
| **Finance Module** | GET | `/payouts/provider` | List provider's payouts | 🏢 |
| **Finance Module** | GET | `/payouts/{id}` | Get payout details with items | 🏢 |
| **Finance Module** | POST | `/payouts/admin/trigger` | Manually trigger payout batch | 👑 |
| **Finance Module** | POST | `/payouts/{id}/approve` | Approve payout for processing | 👑 |
| **Finance Module** | GET | `/payouts/admin/pending` | List pending payouts | 👑 |
| **Finance Module** | GET | `/commissions` | List commission rules | 👑 |
| **Finance Module** | POST | `/commissions` | Create commission rule (tiered by revenue) | 👑 |
| **Finance Module** | PUT | `/commissions/{id}` | Update commission rule | 👑 |
| **Finance Module** | DELETE | `/commissions/{id}` | Delete commission rule | 👑 |
| **Finance Module** | GET | `/invoices/my-invoices` | List user's invoices | 🔒 |
| **Finance Module** | GET | `/invoices/{id}` | Get invoice details with line items | 🔒 |
| **Finance Module** | GET | `/invoices/{id}/download` | Download invoice as PDF | 🔒 |
| **Finance Module** | GET | `/invoices/provider/my-invoices` | List provider's invoices | 🏢 |
| **Social Module** | GET | `/reviews/{entityType}/{entityId}` | List reviews for tour/place (paginated, sortable) | 🌐 |
| **Social Module** | GET | `/reviews/{id}` | Get single review details | 🌐 |
| **Social Module** | POST | `/reviews` | Create review (rating 1-5, text, optional photos) — requires verified booking | 🔒 |
| **Social Module** | PUT | `/reviews/{id}` | Edit own review (within 48h window) | 🔒 |
| **Social Module** | DELETE | `/reviews/{id}` | Delete own review | 🔒 |
| **Social Module** | POST | `/reviews/{id}/reply` | Provider replies to review | 🏢 |
| **Social Module** | POST | `/reviews/{id}/report` | Report inappropriate review | 🔒 |
| **Social Module** | POST | `/reviews/admin/{id}/approve` | Approve flagged review | 👑 |
| **Social Module** | POST | `/reviews/admin/{id}/remove` | Remove review (moderation) | 👑 |
| **Social Module** | GET | `/reviews/admin/flagged` | List flagged reviews for moderation | 👑 |
| **Social Module** | GET | `/reviews/my-reviews` | List current user's reviews | 🔒 |
| **Social Module** | GET | `/favorites` | List user's favorites (tours + places) | 🔒 |
| **Social Module** | POST | `/favorites` | Add to favorites (entityType + entityId) | 🔒 |
| **Social Module** | DELETE | `/favorites/{entityType}/{entityId}` | Remove from favorites | 🔒 |
| **Social Module** | GET | `/favorites/check/{entityType}/{entityId}` | Check if entity is in user's favorites | 🔒 |
| **Social Module** | POST | `/reports` | Report content (review, blog comment, tour) with reason | 🔒 |
| **Social Module** | GET | `/reports/admin` | List all reports (filterable by status, type) | 👑 |
| **Social Module** | POST | `/reports/admin/{id}/resolve` | Resolve report (dismiss, warn, remove content) | 👑 |
| **Social Module** | GET | `/moderation/logs` | Content moderation audit log | 👑 |
| **Analytics Module** | GET | `/admin/audit-logs` | List audit logs (paginated, filterable) | 👑 |

---

### Phase 2: Experience & Discovery

*SEO, Blogs, Map view, Weather, Notifications, Interactions.*

| Module | Method | Route | Description | Auth |
|---|--------|-------|-------------|------|
| **ContentCore Module** | POST | `/translations/translate` | On-demand translation (admin tool) | 👑 |
| **ContentCore Module** | POST | `/translations/batch` | Batch translate multiple texts | 👑 |
| **ContentCore Module** | GET | `/translations/{entityType}/{entityId}` | Get all translations for entity | 🌐 |
| **ContentCore Module** | PUT | `/translations/{id}` | Update/override a translation | 👑 |
| **ContentCore Module** | POST | `/translations/{id}/approve` | Mark translation as human-reviewed | 👑 |
| **ContentPlaces Module** | GET | `/nearby` | Find places near coordinates (lat, lng, radius) | 🌐 |
| **ContentPlaces Module** | GET | `/map/viewport` | Places within map viewport (bounding box) | 🌐 |
| **ContentTours Module** | GET | `/{id}/waypoints` | Get tour route waypoints | 🌐 |
| **ContentTours Module** | POST | `/{id}/waypoints` | Add waypoint to tour route | 🏢 |
| **ContentTours Module** | PUT | `/{id}/waypoints/reorder` | Reorder waypoints | 🏢 |
| **ContentTours Module** | DELETE | `/{id}/waypoints/{waypointId}` | Remove waypoint | 🏢 |
| **ContentTours Module** | GET | `/{id}/children-info` | Get children-friendly details (age ranges, facilities) | 🌐 |
| **ContentTours Module** | PUT | `/{id}/children-info` | Update children-friendly metadata | 🏢 |
| **ContentBlogs Module** | GET | `/` | List published blog posts (paginated, filterable by tag, category, author) | 🌐 |
| **ContentBlogs Module** | GET | `/{id}` | Get blog post details (with translations, comments, related tours) | 🌐 |
| **ContentBlogs Module** | GET | `/{slug}` | Get blog by slug (SEO-friendly) | 🌐 |
| **ContentBlogs Module** | POST | `/` | Create blog post (draft status) | 👑 |
| **ContentBlogs Module** | PUT | `/{id}` | Update blog post | 👑 |
| **ContentBlogs Module** | DELETE | `/{id}` | Soft-delete blog post | 👑 |
| **ContentBlogs Module** | POST | `/{id}/publish` | Publish draft blog post | 👑 |
| **ContentBlogs Module** | POST | `/{id}/unpublish` | Unpublish blog post (back to draft) | 👑 |
| **ContentBlogs Module** | GET | `/{id}/comments` | List comments on blog post (paginated) | 🌐 |
| **ContentBlogs Module** | POST | `/{id}/comments` | Add comment to blog post | 🔒 |
| **ContentBlogs Module** | PUT | `/comments/{commentId}` | Edit own comment | 🔒 |
| **ContentBlogs Module** | DELETE | `/comments/{commentId}` | Delete comment (own or admin) | 🔒 |
| **ContentBlogs Module** | POST | `/comments/{commentId}/reactions` | React to comment (like, helpful, etc.) | 🔒 |
| **ContentBlogs Module** | DELETE | `/comments/{commentId}/reactions` | Remove reaction | 🔒 |
| **ContentBlogs Module** | POST | `/{id}/tours` | Associate tour with blog post | 👑 |
| **ContentBlogs Module** | DELETE | `/{id}/tours/{tourId}` | Remove tour association | 👑 |
| **ContentSeo Module** | GET | `/metadata/{entityType}/{entityId}` | Get SEO metadata for entity | 🌐 |
| **ContentSeo Module** | POST | `/metadata` | Create/update SEO metadata (title, description, keywords, og:tags) | 👑 |
| **ContentSeo Module** | PUT | `/metadata/{id}` | Update SEO metadata | 👑 |
| **ContentSeo Module** | POST | `/redirects` | Create URL redirect (301/302) | 👑 |
| **ContentSeo Module** | GET | `/redirects` | List all redirects | 👑 |
| **ContentSeo Module** | DELETE | `/redirects/{id}` | Delete redirect | 👑 |
| **ContentSeo Module** | GET | `/sitemap.xml` | Get generated XML sitemap | 🌐 |
| **ContentSeo Module** | POST | `/sitemap/regenerate` | Force sitemap regeneration (triggers background job) | 👑 |
| **ContentSeo Module** | GET | `/faq/{entityType}/{entityId}` | Get FAQ items for entity (with translations) | 🌐 |
| **ContentSeo Module** | POST | `/faq` | Create FAQ item | 👑 |
| **ContentSeo Module** | PUT | `/faq/{id}` | Update FAQ item | 👑 |
| **ContentSeo Module** | DELETE | `/faq/{id}` | Delete FAQ item | 👑 |
| **ContentSeo Module** | PUT | `/faq/reorder` | Reorder FAQ items | 👑 |
| **ContentSeo Module** | GET | `/weather/{placeId}` | Get cached weather for a place (12h TTL) | 🌐 |
| **ContentSeo Module** | POST | `/weather/refresh/{placeId}` | Force weather refresh for a place | 👑 |
| **Messaging Module** | GET | `/notifications` | List user's notifications (paginated, filterable by type, read/unread) | 🔒 |
| **Messaging Module** | GET | `/notifications/unread-count` | Get unread notification count | 🔒 |
| **Messaging Module** | POST | `/notifications/{id}/read` | Mark notification as read | 🔒 |
| **Messaging Module** | POST | `/notifications/read-all` | Mark all notifications as read | 🔒 |
| **Messaging Module** | DELETE | `/notifications/{id}` | Delete a notification | 🔒 |
| **Messaging Module** | GET | `/notifications/preferences` | Get notification preferences | 🔒 |
| **Messaging Module** | PUT | `/notifications/preferences` | Update notification preferences (per type: in-app, email, push) | 🔒 |
| **Messaging Module** | POST | `/devices/token` | Register device push token (FCM/APNs) | 🔒 |
| **Messaging Module** | DELETE | `/devices/token/{id}` | Unregister device token | 🔒 |
| **Messaging Module** | GET | `/templates` | List notification templates | 👑 |
| **Messaging Module** | POST | `/templates` | Create notification template | 👑 |
| **Messaging Module** | PUT | `/templates/{id}` | Update template | 👑 |
| **Messaging Module** | POST | `/support/tickets` | Create support ticket | 🔒 |
| **Messaging Module** | GET | `/support/tickets` | List user's support tickets | 🔒 |
| **Messaging Module** | GET | `/support/tickets/{id}` | Get ticket details with messages | 🔒 |
| **Messaging Module** | POST | `/support/tickets/{id}/messages` | Add message to ticket | 🔒 |
| **Messaging Module** | POST | `/support/tickets/{id}/close` | Close ticket | 🔒 |
| **Messaging Module** | GET | `/support/admin/tickets` | List all tickets (admin) | 👑 |
| **Messaging Module** | POST | `/support/admin/tickets/{id}/assign` | Assign ticket to agent | 👑 |
| **Messaging Module** | POST | `/support/admin/tickets/{id}/resolve` | Mark ticket as resolved | 👑 |
| **Analytics Module** | POST | `/interactions` | Track user interaction (view, click, search, share) | 🔒 |
| **Analytics Module** | GET | `/popular/tours` | Get most popular tours (by score) | 🌐 |
| **Analytics Module** | GET | `/popular/places` | Get most popular places | 🌐 |
| **Analytics Module** | GET | `/trending` | Get trending tours (recent popularity spike) | 🌐 |
| **Analytics Module** | GET | `/admin/dashboard` | Admin analytics dashboard (booking stats, revenue, user growth) | 👑 |
| **Analytics Module** | GET | `/admin/dashboard/revenue` | Revenue analytics (by period, provider, category) | 👑 |
| **Analytics Module** | GET | `/admin/dashboard/bookings` | Booking analytics (conversion, cancellation rates) | 👑 |
| **Analytics Module** | GET | `/provider/analytics` | Provider's analytics dashboard | 🏢 |

---

### Phase 3: Growth & Monetization

*Tour Packages, Subscriptions, Loyalty, Referrals, Disputes, Accessibility.*

| Module | Method | Route | Description | Auth |
|---|--------|-------|-------------|------|
| **ContentPlaces Module** | GET | `/{id}/accessibility` | Get accessibility features for a place | 🌐 |
| **ContentPlaces Module** | PUT | `/{id}/accessibility` | Update accessibility features | 👑 |
| **ContentTours Module** | GET | `/packages` | List tour packages | 🌐 |
| **ContentTours Module** | GET | `/packages/{id}` | Get package details with included tours | 🌐 |
| **ContentTours Module** | POST | `/packages` | Create tour package (multi-tour bundle) | 🏢 |
| **ContentTours Module** | PUT | `/packages/{id}` | Update package | 🏢 |
| **ContentTours Module** | DELETE | `/packages/{id}` | Delete package | 🏢 |
| **ContentTours Module** | POST | `/packages/{id}/inclusions` | Add tour to package | 🏢 |
| **Booking Module** | POST | `/package` | Create package booking (atomic multi-tour) | 🔒 |
| **Booking Module** | POST | `/{id}/dispute` | Open dispute for booking | 🔒 |
| **Finance Module** | GET | `/subscriptions/plans` | List all subscription plans (provider + user) | 🌐 |
| **Finance Module** | GET | `/subscriptions/plans/{id}` | Get plan details with features | 🌐 |
| **Finance Module** | POST | `/subscriptions/subscribe` | Subscribe to a plan | 🔒 |
| **Finance Module** | POST | `/subscriptions/cancel` | Cancel subscription | 🔒 |
| **Finance Module** | POST | `/subscriptions/change-plan` | Upgrade/downgrade subscription | 🔒 |
| **Finance Module** | GET | `/subscriptions/my-subscription` | Get current user's active subscription | 🔒 |
| **Finance Module** | GET | `/subscriptions/provider/features` | Get feature gates for current provider tier | 🏢 |
| **Finance Module** | POST | `/subscriptions/admin/plans` | Create subscription plan | 👑 |
| **Finance Module** | PUT | `/subscriptions/admin/plans/{id}` | Update plan | 👑 |
| **Finance Module** | POST | `/subscriptions/admin/plans/{id}/features` | Add feature to plan | 👑 |
| **Finance Module** | GET | `/loyalty/balance` | Get current user's loyalty points balance | 🔒 |
| **Finance Module** | GET | `/loyalty/transactions` | List loyalty point transactions (earned, redeemed, expired) | 🔒 |
| **Finance Module** | POST | `/loyalty/redeem` | Redeem points for discount on booking | 🔒 |
| **Finance Module** | GET | `/loyalty/rules` | Get earning rules (points per JOD spent) | 🌐 |
| **Finance Module** | GET | `/referrals/my-code` | Get user's referral code and stats | 🔒 |
| **Finance Module** | POST | `/referrals/apply` | Apply referral code (during registration) | 🌐 |
| **Finance Module** | GET | `/referrals/history` | List referral history (who used your code) | 🔒 |
| **Finance Module** | POST | `/disputes` | Open dispute for a booking | 🔒 |
| **Finance Module** | GET | `/disputes/{id}` | Get dispute details with evidence | 🔒 |
| **Finance Module** | POST | `/disputes/{id}/evidence` | Upload evidence for dispute | 🔒 |
| **Finance Module** | POST | `/disputes/{id}/respond` | Provider responds to dispute | 🏢 |
| **Finance Module** | POST | `/disputes/admin/{id}/resolve` | Admin resolves dispute (full refund, partial refund, reject) | 👑 |
| **Finance Module** | GET | `/disputes/admin/pending` | List pending disputes | 👑 |
| **Social Module** | GET | `/accessibility/reviews/{placeId}` | Get accessibility reviews for a place | 🌐 |
| **Social Module** | POST | `/accessibility/reviews` | Submit accessibility review | 🔒 |
| **Social Module** | PUT | `/accessibility/reviews/{id}` | Update accessibility review | 🔒 |

---

### Phase 4: Advanced Features & AI

*Live Tracking, Recommendation Engine, Chatbot, Discounts.*

| Module | Method | Route | Description | Auth |
|---|--------|-------|-------------|------|
| **Finance Module** | GET | `/discounts/active` | List active discounts for tours | 🌐 |
| **Finance Module** | GET | `/discounts/{id}` | Get discount details | 🌐 |
| **Finance Module** | POST | `/discounts` | Create discount (5 types: percentage, fixed, early bird, group, flash sale) | 🏢 |
| **Finance Module** | PUT | `/discounts/{id}` | Update discount | 🏢 |
| **Finance Module** | POST | `/discounts/{id}/submit` | Submit for approval (if requires approval) | 🏢 |
| **Finance Module** | POST | `/discounts/admin/{id}/approve` | Approve discount | 👑 |
| **Finance Module** | POST | `/discounts/admin/{id}/reject` | Reject discount | 👑 |
| **Finance Module** | POST | `/discounts/admin/{id}/revoke` | Revoke active discount | 👑 |
| **Finance Module** | POST | `/discounts/validate` | Validate discount code at checkout | 🔒 |
| **Finance Module** | GET | `/discounts/provider/my-discounts` | List provider's discounts | 🏢 |
| **Messaging Module** | POST | `/chatbot/conversations` | Start new chatbot conversation | 🔒 |
| **Messaging Module** | GET | `/chatbot/conversations` | List user's conversations | 🔒 |
| **Messaging Module** | GET | `/chatbot/conversations/{id}` | Get conversation with messages | 🔒 |
| **Messaging Module** | POST | `/chatbot/conversations/{id}/messages` | Send message (REST fallback if not using SignalR) | 🔒 |
| **Messaging Module** | POST | `/chatbot/conversations/{id}/close` | End conversation | 🔒 |
| **Messaging Module** | POST | `/chatbot/conversations/{id}/handoff` | Escalate to human support | 🔒 |
| **Tracking Module** | POST | `/sessions` | Start live tracking session (guide only, for a booking) | 🏢 |
| **Tracking Module** | POST | `/sessions/{id}/end` | End tracking session | 🏢 |
| **Tracking Module** | GET | `/sessions/{id}` | Get session details (status, guide, participants) | 🔒 |
| **Tracking Module** | GET | `/sessions/active` | List active tracking sessions for user's bookings | 🔒 |
| **Tracking Module** | GET | `/sessions/{id}/snapshots` | Get location snapshot history for a session | 🔒 |
| **Tracking Module** | GET | `/sessions/{id}/latest` | Get latest location for a session | 🔒 |
| **Tracking Module** | GET | `/checkpoints/{tourId}` | Get checkpoint definitions for a tour | 🌐 |
| **Tracking Module** | POST | `/checkpoints` | Create tour checkpoint (waypoint marker) | 🏢 |
| **Tracking Module** | PUT | `/checkpoints/{id}` | Update checkpoint | 🏢 |
| **Tracking Module** | DELETE | `/checkpoints/{id}` | Delete checkpoint | 🏢 |
| **Tracking Module** | GET | `/sessions/provider/history` | Provider's tracking session history | 🏢 |
| **Analytics Module** | GET | `/recommendations` | Get personalized tour recommendations for current user | 🔒 |
| **Analytics Module** | GET | `/recommendations/similar/{tourId}` | Get similar tours (content-based) | 🌐 |
| **Analytics Module** | GET | `/preferences` | Get current user's preferences | 🔒 |
| **Analytics Module** | PUT | `/preferences` | Update user preferences (categories, price range, etc.) | 🔒 |
| **Analytics Module** | GET | `/preferences/categories` | Get user's preferred categories | 🔒 |
| **Analytics Module** | PUT | `/preferences/categories` | Update preferred categories | 🔒 |

---

## Business Logic Reference

#### Auth Module Logic

**#6 POST `/register`**:
- Validate: email format, password minimum 8 chars with uppercase + lowercase + digit + special character
- Normalize email: `Trim().ToLowerInvariant()`
- Check uniqueness: call `ISecurityService.GetUserIdByEmailAsync()` — return 409 Conflict if exists
- Create user in Security module via `ISecurityService` (hashed password, default role `User`)
- Generate 6-digit OTP, store in `auth.Otps` with 10-minute TTL
- Send OTP via email using `IEmailService`
- Publish `UserCreatedIntegrationEvent` → Accounts module creates Profile
- Rate limit: max 3 registration attempts per IP per hour

**#1 POST `/verify-email`** (existing):
- Lookup OTP by email + code in `auth.Otps`
- Check: not expired (10-min TTL), not already used, max 5 attempts
- On success: call `ISecurityService.MarkEmailVerifiedAsync()`, generate JWT + refresh token
- On failure: increment attempt count, return 404 if OTP not found, 429 if max attempts exceeded
- Delete OTP after successful verification

**#2 POST `/login`** (existing):
- Normalize email, call `ISecurityService.VerifyCredentialsAsync()`
- Return 401 if credentials invalid or email not verified
- Create Device record (user agent, device name from headers)
- Create Session (30-day expiry, IP address from `IRequestContext`)
- Generate refresh token, hash it, store in `auth.RefreshTokens` (30-day expiry)
- Generate JWT access token with claims: sub (userId), email, roles, custom claims
- JWT expiry: 15 minutes (short-lived)
- Track: last login timestamp, login count

**#3 POST `/refresh`** (existing):
- Hash incoming refresh token, look up in `auth.RefreshTokens`
- Validate: not revoked, not expired, session still active
- Rotate: revoke old refresh token, generate new one (refresh token rotation for security)
- Generate new JWT access token with fresh claims from `ISecurityService.GetUserDataByIdAsync()`
- If refresh token reuse detected (already revoked token presented): revoke ALL user sessions (security breach)

**#7 POST `/forgot-password`**:
- Lookup user by email via `ISecurityService`
- If user exists: generate 6-digit OTP, store with 10-min TTL, send via email
- If user doesn't exist: return 200 anyway (prevent email enumeration)
- Rate limit: max 5 requests per email per hour

**#8 POST `/reset-password`**:
- Validate OTP (same rules as verify-email)
- Validate new password (same strength rules as register)
- Update password via `ISecurityService`, revoke ALL sessions and refresh tokens
- Send confirmation email

**#9 POST `/change-password`**:
- Require current password verification via `ISecurityService.VerifyCredentialsAsync()`
- Validate new password strength, must differ from current password
- Update password, optionally revoke other sessions (user choice)

**#10 POST `/resend-otp`**:
- Rate limit: max 5 per email per hour (enforced via rate limiting middleware)
- Invalidate previous OTP for this email
- Generate new 6-digit OTP with 10-min TTL, send via email

**#11-13 POST `/external/{provider}`**:
- Validate OAuth token with provider (Google/Facebook/Apple)
- Extract: email, name, provider user ID
- If email exists in system: link external provider, login (create session + tokens)
- If email doesn't exist: auto-register, mark email as verified (OAuth-verified), create profile
- Store in `auth.ExternalProviders`: provider name, provider user ID, linked at timestamp
- Handle: token refresh, provider token revocation

**#14 GET `/sessions`**:
- Return all active (non-expired, non-revoked) sessions for `ICurrentUser.UserId`
- Include: device info, IP address, created at, last activity, is current session

**#15 DELETE `/sessions/{id}`**:
- Validate session belongs to `ICurrentUser.UserId`
- Revoke session: mark as revoked, revoke associated refresh token
- Cannot revoke own current session (use /logout instead)


---


#### Security Module Logic


**#1 GET `/users`**:
- Admin only — requires `Admin` role claim
- Support filters: email (partial match), role, status (active/inactive/suspended), registration date range
- Pagination: page + pageSize (max 50), sortable by name, email, createdAt
- Return: userId, email, roles, status, lastLoginAt, createdAt (no passwords or sensitive data)

**#3 PUT `/users/{id}/status`**:
- Toggle user active/inactive. Deactivated users cannot login
- If deactivating: revoke all sessions and refresh tokens for this user
- Cannot deactivate yourself (prevent admin lockout)
- If reactivating: user must re-verify email if previously suspended for policy violations
- Publish `UserStatusChangedIntegrationEvent` → other modules react (cancel pending bookings, etc.)

**#4-5 Role Assignment**:
- Validate role exists before assignment
- System roles (`Admin`, `User`, `Provider`) cannot be deleted
- Provider role assignment triggers provider profile setup flow
- Removing Provider role: check no active bookings or pending payouts before removal

**#7 POST `/roles`**:
- Role name must be unique (case-insensitive)
- Reserved names: Admin, User, Provider, SuperAdmin — cannot create

**#11 POST `/roles/{id}/claims`**:
- Claims structure: Type + Value pairs
- Standard claim types: `permission`, `feature`, `tier`
- Used for feature gating (subscription tier enforcement)


---


#### Accounts Module Logic


**#2 PUT `/profile`**:
- Updatable fields: firstName, lastName, bio (max 500 chars), dateOfBirth, gender, phoneNumber, preferredLanguage, preferredCurrency (JOD/USD/EUR)
- Validate phone format (E.164), language code exists in `Languages` table
- Cannot change email (handled by Auth module)
- `UpdatedAt` timestamp auto-set

**#3 POST `/profile/avatar`**:
- Accept: JPEG, PNG, WebP — max 5MB
- Resize to standard sizes: 64x64 (thumbnail), 200x200 (small), 400x400 (medium)
- Store via attachment service, update profile AvatarUrl
- Delete previous avatar files on replacement

**#4 GET `/profile/{userId}` (public)**:
- Return only public info: firstName, lastName initial, avatar, bio, memberSince
- If user is a provider: include provider type, rating, verified badge, tour count
- Hide: email, phone, date of birth, private preferences

**#5 POST `/provider/register`**:
- **Provider Types**: TourOperator, IndependentGuide, HotelResort, ActivityCenter
- Required fields: businessName, providerType, contactEmail, contactPhone, address, description
- Additional by type:
  - TourOperator: licenseNumber, registrationNumber
  - IndependentGuide: personalIdNumber, languages spoken
  - HotelResort: starRating, facilityType
  - ActivityCenter: activityTypes, certifications
- Initial state: `Pending` (state machine begins)
- Publish `ProviderRegisteredIntegrationEvent`

**Provider State Machine**:
```
Pending → MoreDocsNeeded (admin requests more docs)
Pending → Approved (admin approves)
Pending → Rejected (admin rejects)
MoreDocsNeeded → Pending (provider re-uploads)
Approved → Suspended (admin suspends OR document expired)
Suspended → Approved (admin reinstates)
Rejected → Pending (provider re-applies with fixes)
```

**#7 POST `/provider/documents`**:
- **Required Documents** (by provider type):
  - All: Business license, tax registration
  - TourOperator: Tourism authority license, insurance certificate
  - IndependentGuide: Personal ID, first aid certification
  - HotelResort: Health & safety certificate, fire safety
  - ActivityCenter: Activity-specific certifications, liability insurance
- File constraints: PDF, JPEG, PNG — max 10MB per document
- Each document has: type, file, expiresAt (mandatory for licenses/certifications)
- Documents stored encrypted (IV + HMAC in Attachment record)
- **Document Expiry**: `DocumentExpiryCheckService` runs daily, flags expiring docs 30 days before, suspends provider if critical doc expires

**#10 POST `/provider/apply`**:
- Validates all required documents are uploaded for the provider type
- All documents must be non-expired
- Transitions state to `Pending` (if was `MoreDocsNeeded` or `Rejected`)
- Sends notification to admin queue

**#12 POST `/admin/providers/{id}/approve`**:
- Transition: `Pending` → `Approved`
- Assign `Provider` role to user via `ISecurityService`
- Publish `ProviderApprovedIntegrationEvent` → enable provider features, send welcome email
- Provider can now create tours, manage availability, receive bookings

**#13 POST `/admin/providers/{id}/reject`**:
- Transition: `Pending` → `Rejected`
- Requires: rejectionReason (string, mandatory)
- Notify provider via email + in-app notification with reason
- Provider can fix issues and re-apply (#10)

**#14 POST `/admin/providers/{id}/request-docs`**:
- Transition: `Pending` → `MoreDocsNeeded`
- Requires: list of missing/insufficient document types with notes
- Notify provider with specific document requests

**#15 POST `/admin/providers/{id}/suspend`**:
- Transition: `Approved` → `Suspended`
- Requires: suspensionReason
- Effects: provider cannot receive new bookings, existing confirmed bookings proceed
- Publish `ProviderSuspendedIntegrationEvent` → hide provider's tours from search, notify booked users


---


#### ContentCore Module Logic


**#1 GET `/languages`**:
- Return only active languages (`IsActive = true`)
- Include: code (ISO 639-1), name, nativeName, isRtl
- Used by frontend for language picker and translation UI
- Cache: 1 hour (languages rarely change)

**#4-5 Categories (GET list / GET single)**:
- **3-level hierarchy**: Root → SubCategory → Sub-SubCategory (max depth enforced)
- Return as tree structure: parent with nested children
- Include translations for request's `Accept-Language`
- Filter: isActive, parentId (to get children of a specific category)
- Slugs must be unique per level, auto-generated from name if not provided

**#6 POST `/categories`**:
- Validate: name (2-200 chars), slug (unique, URL-safe), parentCategoryId exists if provided
- Enforce max 3 levels: if parent is a level-2 category, reject (would create level-4)
- Auto-generate slug from name using transliteration (Arabic → Latin)
- Set SortOrder: next available in parent's children
- Auto-trigger translation: publish `ContentCreatedIntegrationEvent` for auto-translation

**#14 POST `/attachments`**:
- File upload with validation:
  - Images: JPEG, PNG, WebP, AVIF — max 10MB, auto-resize to standard sizes
  - Videos: MP4, MOV — max 100MB, extract thumbnail
  - Documents: PDF — max 20MB
- Store encrypted: generate IV + HMAC for each file
- Track: originalFileName, mimeType, fileSize, width, height, durationSeconds
- EntityType + EntityId: polymorphic association (Place, Tour, Business, Review, Blog, TourGuide)

**#17 POST `/{entityType}/{entityId}/images`**:
- Upload multiple images at once (max 20 per entity)
- Auto-create `EntityImage` records for each size (Thumbnail, Small, Medium, Large, Original)
- First image is auto-set as primary (`IsPrimary = true`)
- Validate: caller owns the entity (provider owns tour, admin for places)

**#22-26 Translations**:
- #22 Translate: call `ITranslationService.TranslateAsync()`, auto-save via decorator
- #23 Batch: translate multiple fields (name, description, etc.) in one call
- #24 Get: return all translations for entity, grouped by language
- #25 Update: manual override of auto-translated text (sets status to `HumanReviewed`)
- #26 Approve: mark as reviewed, locks translation from auto-override
- Translation status flow: `Pending` → `AutoTranslated` → `HumanReviewed`


---


#### ContentPlaces Module Logic


**#1 GET `/` (List places)**:
- Public endpoint, supports:
  - Filter by: categoryId, coordinates (lat/lng/radius), rating range, hasActiveTours
  - Sort by: name, rating, popularity, distance (if coordinates provided)
  - Pagination: page + pageSize (max 50)
- Return with: primary image, average rating, tour count, translations (per Accept-Language)
- Apply soft-delete filter (`IsDeleted = false`)

**#4 POST `/` (Create place)**:
- Admin only — places are curated content
- Required: name, slug (unique), location (lat, lng, address), description, categoryId
- Optional: placeType, elevation, timezone, website, phone, email
- Auto-trigger: translation for all active languages, sitemap regeneration
- Publish `PlaceCreatedIntegrationEvent`

**#8 POST `/businesses` (Create business)**:
- Provider creates a business listing associated with a place
- Required: name, placeId, businessType, contactInfo, description
- Must verify: place exists and is active
- Business requires admin approval before appearing publicly (optional workflow)
- Auto-create default business hours (Mon-Fri 9-5) that provider customizes

**#12 PUT `/businesses/{id}/hours`**:
- Batch update: send all 7 days at once (replace strategy, not merge)
- Each day: dayOfWeek, openTime (TimeOnly), closeTime (TimeOnly), isClosed (bool)
- Validation: openTime < closeTime (unless 24h: both 00:00), no overlapping shifts per day
- Support: split shifts (two entries per day — e.g., 9:00-13:00, 17:00-22:00)

**#15 GET `/nearby`**:
- Requires: lat, lng, radiusKm (max 100km)
- Uses Haversine formula for distance calculation (SQL Server geography or computed)
- Returns places sorted by distance, with distance field in response
- Limit: max 50 results, applies active + soft-delete filters

**#16 GET `/map/viewport`**:
- Requires: northLat, southLat, eastLng, westLng (bounding box)
- Mapbox integration: return places with coordinates for map pin rendering
- Cluster pins when zoom level is low (>50 results in viewport)
- Include: id, name, lat, lng, primaryImage, rating, tourCount (minimal payload for map)


---


#### ContentTours Module Logic


**#1 GET `/` (List tours)**:
- Public — only returns tours with `Status = Approved` and `IsActive = true`
- Filters: categoryId, placeId, priceRange (min/max), dateRange, durationRange, languageCode, childrenFriendly, accessibilityLevel, guideId
- Sort: price (asc/desc), rating, popularity, newest, distance (if coordinates)
- Pagination: page + pageSize (max 50)
- Include: primary image, starting price, average rating, review count, next available date

**#4 POST `/` (Create tour)**:
- Provider only — must have `Status = Approved` provider account
- Initial tour status: `Draft`
- Required: title, description, placeId, categoryId, durationMinutes, basePrice, currency (JOD/USD/EUR), maxGroupSize, languageCode
- Optional: minGroupSize, difficultyLevel, ageRestriction, meetingPoint (lat, lng, address), whatToExpect, whatToBring, cancellationPolicy
- Validate: placeId exists, categoryId exists, price > 0, duration > 0
- Auto-trigger translation upon publishing

**Tour Approval State Machine**:
```
Draft → Pending (provider submits)
Pending → Approved (admin approves)
Pending → Rejected (admin rejects with reason)
Rejected → Draft (provider edits and can resubmit)
Approved → Suspended (admin suspends or provider deactivates)
Suspended → Approved (admin reinstates)
```

**#7 POST `/{id}/submit`**:
- Transition: `Draft` → `Pending`
- Pre-submit validation:
  - At least 1 image uploaded
  - At least 1 pricing tier defined
  - At least 1 schedule defined
  - Description > 100 characters
  - Meeting point coordinates set
- Fail with 422 and list of missing requirements if validation fails

**#11 POST `/{id}/schedules`**:
- Create availability schedule: date, startTime, endTime, capacity
- Recurrence support: `once`, `daily`, `weekly`, `custom` (specific days of week)
- For recurring: generate slots up to 90 days ahead
- Validation: no overlapping schedules for same tour, capacity > 0
- Blackout dates: can mark specific dates as unavailable

**#15 POST `/{id}/pricing`**:
- Pricing tier types: `Adult`, `Child`, `Infant`, `Senior`, `Group`, `Private`
- Each tier: type, price, currency, minAge, maxAge, minQuantity, maxQuantity
- At least one `Adult` tier required
- Child tier: must have age range (e.g., 3-12), price can be 0 (free)
- Group tier: special pricing when minQuantity reached
- Sale price: separate field, calculated by `DiscountLifecycleService` when discounts apply

**#24-29 Tour Packages (Phase 3)**:
- Package: bundle of 2+ tours sold as one unit with package discount
- Atomic booking: all tours in package succeed or none (transaction)
- Package pricing: sum of individual tour prices minus package discount
- Inclusions: each inclusion references a tourId, specific schedule, and pricing tier
- Provider can only package their own tours
- Capacity check: each included tour must have availability

**#32 GET `/search`**:
- Full-text search across: title, description (all languages), place name, category name, tag names
- Ranking factors: text relevance, popularity score, review count, recency
- Faceted search: return counts per category, price range distribution, available dates
- Search suggestions: autocomplete endpoint (debounced, returns top 5)


---


#### ContentBlogs Module Logic


**#4 POST `/` (Create blog post)**:
- Admin/editor only — content team creates blog posts
- Initial status: `Draft`
- Required: title, slug (unique), body (rich text/markdown), authorId, categoryId
- Optional: featuredImageId, tags[], excerpt (auto-generated from first 200 chars if empty)
- SEO: auto-generate meta description from excerpt, og:image from featured image

**#7 POST `/{id}/publish`**:
- Transition: `Draft` → `Published`
- Sets publishedAt timestamp (for chronological listing)
- Auto-trigger: translation, sitemap regeneration, SEO metadata creation
- Publish `BlogPublishedIntegrationEvent` (for notification to subscribers)

**#10 POST `/{id}/comments`**:
- Authenticated users only
- Validation: body (5-2000 chars), profanity filter (same as reviews)
- Nested replies: optional parentCommentId for threaded conversations (max 3 levels deep)
- Auto-moderation: flag comments with links or suspicious patterns
- Rate limit: max 10 comments per user per hour

**#13 POST `/comments/{commentId}/reactions`**:
- Reaction types: `Like`, `Helpful`, `Insightful`, `Funny`
- One reaction per user per comment (toggle — re-sending same type removes it)
- Reaction counts denormalized on comment record for fast display


---


#### ContentSeo Module Logic


**#2 POST `/metadata`**:
- One SEO record per entityType + entityId (upsert)
- Fields: metaTitle (max 60 chars), metaDescription (max 160 chars), keywords[], ogTitle, ogDescription, ogImageUrl, canonicalUrl, noIndex (bool), hreflang links
- Auto-populated defaults from entity data if not manually set
- Language variants: separate SEO metadata per language (hreflang)

**#4 POST `/redirects`**:
- Create 301 (permanent) or 302 (temporary) redirect
- Validate: oldPath is unique (no duplicate redirect sources), newPath exists or is valid URL
- Circular redirect detection: follow chain, reject if loop
- Redirect chain limit: max 3 hops

**#7 GET `/sitemap.xml`**:
- Generated by `SitemapRegenerationService` every 6 hours
- Includes: all published tours, places, blogs with lastModified dates
- Hreflang alternates for each language (ar, en)
- Sitemap index if >50,000 URLs (split into sub-sitemaps)
- Served from cache (regenerated in background, not on-request)

**#14 GET `/weather/{placeId}`**:
- Cache-first: check `WeatherCache` table, return if not expired (12h TTL)
- Cache miss: call `IWeatherProvider.GetWeatherAsync(lat, lng)`
- Weather data: current temp, condition, humidity, wind, 5-day forecast
- API budget: max 1000 external calls per day, tracked in-memory counter
- If budget exceeded: return stale cache data with `X-Weather-Stale: true` header
- Pre-fetched for top 50 locations daily by `WeatherPreFetchService`


---


#### Booking Module Logic


**Booking State Machine (CRITICAL — core business flow)**:
```
AwaitingPayment → Confirmed (instant booking: payment succeeds)
AwaitingPayment → PendingConfirmation (non-instant: payment succeeds, awaits provider)
AwaitingPayment → Cancelled (payment timeout 10 min OR user cancels)
PendingConfirmation → Confirmed (provider confirms)
PendingConfirmation → Rejected (provider rejects)
PendingConfirmation → Confirmed (auto-accept after 24h via ProviderAutoAcceptService)
Confirmed → Completed (tour date passed + provider marks complete)
Confirmed → Cancelled (user cancels — refund per policy)
Confirmed → Disputed (user opens dispute)
Completed → Disputed (user opens dispute within 48h of completion)
Disputed → Resolved (admin resolves)
```

**#1 POST `/tour` (Create tour booking)**:
- **Step 1 — Validate availability**:
  - Check tour exists, is Approved, is Active
  - Check schedule/slot exists for requested date, has remaining capacity
  - Verify requested participant count ≤ remaining capacity
  - Check: user has no duplicate booking for same tour + date
- **Step 2 — Lock slot** (optimistic concurrency):
  - Create `SlotLock` record: bookingId, slotId, userId, expiresAt = Now + 10 min
  - Decrement available capacity atomically (use RowVersion for concurrency)
  - If capacity insufficient after lock: return 409 Conflict (race condition)
- **Step 3 — Calculate pricing**:
  - Sum pricing tiers × quantities (adults, children, etc.)
  - Apply active discounts (check `Discounts` table: valid date range, not exhausted, applicable to this tour)
  - Apply discount stacking rules: max 1 percentage + 1 fixed, or 1 early bird, highest value wins if conflict
  - Apply loyalty points if user redeems (deduct from balance)
  - Currency: use tour's base currency (JOD/USD/EUR)
  - Calculate commission: apply `CommissionRules` (tiered by provider's total revenue)
- **Step 4 — Create booking**:
  - Status: `AwaitingPayment`
  - Generate booking reference number (format: `YJ-{YYYYMMDD}-{random6}`)
  - Store: totalPrice, commissionAmount, netProviderAmount, currency, breakdown
  - Publish `BookingCreatedIntegrationEvent`
- **Step 5 — Return payment initiation URL/token** to client
- **Timeout**: `BookingAutoExpireService` cancels after 10 min if payment not received

**#2 POST `/package` (Package booking — Phase 3)**:
- Same as tour booking but for multiple tours atomically
- All-or-nothing: if any tour slot unavailable, entire booking fails
- Lock ALL slots in a transaction
- Single payment for entire package
- Package discount applied on top of individual tour prices

**#5 POST `/{id}/cancel`**:
- User cancels own booking — refund calculated by refund policy:
  - ≥48h before tour: full refund
  - 24-48h before: 50% refund (configurable per tour)
  - <24h before: no refund (configurable per tour)
  - Provider cancels: always full refund to user
- Release slot lock, increment available capacity
- Publish `BookingCancelledIntegrationEvent` → trigger refund processing, notify parties

**#6 POST `/{id}/confirm` (Provider confirms non-instant)**:
- Only for non-instant bookings in `PendingConfirmation` status
- Provider must confirm within 24h (else `ProviderAutoAcceptService` auto-confirms)
- Transition: `PendingConfirmation` → `Confirmed`
- Notify user via NotificationHub + email

**#7 POST `/{id}/reject` (Provider rejects)**:
- Only for `PendingConfirmation` status
- Requires: rejectionReason
- Auto-trigger full refund (user paid but provider rejected)
- Release slot, notify user, log reason

**#14 POST `/join-request`**:
- Request to join existing confirmed group booking (if tour allows)
- Validate: booking is Confirmed, tour allows join, capacity remaining
- Join request state: `Pending` → `Approved`/`Rejected`
- If approved: add participant, charge separately, update booking count

**#19 POST `/availability/slots`**:
- Provider creates time slots for their tours
- Validation: no overlap with existing slots for same tour + date
- Capacity: must be ≥ 1, cannot exceed tour's maxGroupSize
- Supports: single slot creation or recurring via #22 bulk endpoint

**#22 POST `/availability/slots/bulk`**:
- Generate recurring slots: specify pattern (daily/weekly/custom), start date, end date, time, capacity
- Max: generate up to 90 days of slots at once
- Skip existing dates (no duplicate slots)
- Return: count of created slots + any skipped dates

**#31 POST `/refund-policies`**:
- Per-tour refund policy (provider sets when creating tour)
- Structure: list of tiers with `hoursBeforeTour` threshold and `refundPercentage`
- Example: [{hours: 48, percent: 100}, {hours: 24, percent: 50}, {hours: 0, percent: 0}]
- Default policy if none set: full refund up to 24h, 0% after


---


#### Finance Module Logic


**#1 POST `/payments/initiate`**:
- Input: bookingId, paymentMethod (card, wallet, etc.)
- Verify booking exists, is in `AwaitingPayment` status, belongs to user, not expired
- Calculate: totalAmount, currency, commission breakdown
- **Escrow model**: payment goes to YallaJo escrow, NOT directly to provider
  - Provider payout happens after tour completion (weekly batch)
- Call payment gateway (abstracted behind `IPaymentGateway`):
  - Returns: paymentUrl (redirect) or clientSecret (Stripe Elements)
- Create `Payment` record: amount, currency, gatewayRef, status=`Pending`
- Return payment token/URL to frontend

**#2 POST `/payments/webhook`**:
- Payment gateway callback (Stripe/PayMob webhook)
- Verify webhook signature (gateway-specific)
- On success:
  - Update Payment status: `Pending` → `Completed`
  - Transition booking: `AwaitingPayment` → `Confirmed` (instant) or `PendingConfirmation` (non-instant)
  - Release slot lock, permanently reserve capacity
  - Create invoice with line items
  - Publish `PaymentCompletedIntegrationEvent` → notify user + provider, award loyalty points
- On failure:
  - Update Payment status: `Pending` → `Failed`
  - Release slot lock, restore capacity
  - Notify user of payment failure

**#5 POST `/payments/{id}/refund`**:
- Initiate refund (full or partial based on refund policy)
- Call `IPaymentGateway.RefundAsync(gatewayRef, amount)`
- If gateway call fails: mark as `RefundPending`, `RefundRetryService` retries every 15 min (max 3 attempts)
- On success: create `Payment` record with negative amount (refund), update booking
- Commission handling: YallaJo may retain commission on refunds (configurable per policy)

**#9 POST `/payouts/admin/trigger`**:
- Admin manually triggers payout batch (normally weekly via `PayoutBatchingService`)
- Aggregation logic:
  - Group completed bookings by provider + currency
  - Calculate: totalBookingRevenue - commission = netProviderAmount
  - Commission tiers (from `CommissionRules`):
    - Revenue ≤10K JOD/month: 15% commission
    - Revenue 10K-50K: 12% commission
    - Revenue >50K: 10% commission
  - Deduct: any pending refunds, dispute holds
- Create `Payout` + `PayoutItem` records
- Status: `Pending` (requires admin approval for large payouts >5K)

**#18 POST `/subscriptions/subscribe`**:
- **Provider Tiers**: Free, Basic (29 JOD/mo), Premium (79 JOD/mo), Enterprise (199 JOD/mo)
- **User Tiers**: Free, YallaJo+ (9.99 JOD/mo)
- Feature gating: each plan has `PlanFeatures` linking to `SubscriptionFeatures`
  - Free: 3 tours, basic analytics, standard support
  - Basic: 10 tours, advanced analytics, priority support, featured listing
  - Premium: unlimited tours, full analytics, premium support, top placement, discounts tool
  - Enterprise: everything + API access, white-label, dedicated account manager
- On subscribe: charge via payment gateway, create `Subscription` record with startDate, endDate (+1 month), autoRenew=true
- Publish `SubscriptionCreatedIntegrationEvent` → update provider feature flags

**#26 GET `/loyalty/balance`**:
- Return: totalEarned, totalRedeemed, totalExpired, currentBalance, expiringIn30Days
- Points earned: 1 point per JOD spent on bookings
- Points expire: 12 months after earning (FIFO consumption)
- Minimum redemption: 100 points = 1 JOD discount

**#28 POST `/loyalty/redeem`**:
- Input: bookingId, pointsToRedeem
- Validate: pointsToRedeem ≤ currentBalance, ≥ 100 (minimum), ≤ totalBookingPrice (can't earn money)
- FIFO: consume oldest non-expired points first
- Create `LoyaltyTransaction` with type `Redeemed`
- Apply as discount to booking total

**#31 POST `/referrals/apply`**:
- Apply referral code during registration
- Validate: code exists, referrer is active, not self-referral, first-time use
- Reward: 50 points to referrer + 50 points to new user (configurable)
- Create `Referral` record linking referrer → referee
- Points awarded after referee completes first booking (anti-gaming)

**#35 POST `/discounts` (Create discount)**:
- **5 Discount Types**:
  - `Percentage`: X% off (max 50%)
  - `FixedAmount`: X JOD off
  - `EarlyBird`: discount for booking N+ days in advance
  - `GroupDiscount`: discount when group size ≥ N
  - `FlashSale`: time-limited deep discount (max 72h)
- Fields: type, value, code (optional — auto-generated if empty), validFrom, validTo, maxUsages, maxUsagesPerUser, minOrderAmount, applicableTourIds[]
- Approval required for: discounts >30%, flash sales, site-wide discounts
- **Stacking rules**: max 1 percentage + 1 fixed OR 1 early bird. Best value wins if conflict
- SalePrice: when discount active, `DiscountLifecycleService` updates SalePrice on tour's pricing tiers

**#41 POST `/discounts/validate`**:
- Input: discountCode, tourId, participants, bookingDate
- Check: code exists, within validFrom/validTo, not exhausted (usages < maxUsages), user hasn't exceeded maxUsagesPerUser, tour is eligible
- Return: discountAmount, discountedTotal, discountType (for frontend display)
- Does NOT apply — just validates (applied during booking creation)

**#43-48 Disputes**:
- Open within 48h of tour completion (or during confirmed status)
- Dispute reasons: tour not as described, guide no-show, safety concern, partial delivery, other
- Evidence upload: images, documents, max 10 attachments
- Provider has 72h to respond
- Admin resolution options: full refund, partial refund (% or fixed amount), reject (side with provider), split (compromise)
- Resolution publishes `DisputeResolvedIntegrationEvent` → trigger refund if applicable, update provider rating


---


#### Messaging Module Logic


**#1 GET `/notifications`**:
- Return user's notifications filtered by their preferences
- Types: BookingConfirmed, BookingCancelled, PaymentReceived, NewReview, ReviewReply, ProviderApproved, DiscountOnWishlist, PointsEarned, PointsExpiring, DocumentExpiring, SubscriptionRenewed, TourApproved, DisputeUpdate
- Filter: by type[], read/unread, date range
- Sort: newest first (default), includes relative time ("2 hours ago")
- Pagination: cursor-based (not offset) for real-time consistency

**#7 PUT `/notifications/preferences`**:
- Per notification type: enable/disable for each channel (inApp, email, push)
- Default: all enabled for inApp, major events for email, none for push
- Validation: cannot disable all channels for critical notifications (payment, booking)

**#8 POST `/devices/token`**:
- Register FCM (Android) or APNs (iOS) token for push notifications
- Deduplicate: update if deviceId already has a token
- Cleanup: remove stale tokens (not updated in 30 days)

**#13 POST `/support/tickets`**:
- Categories: Booking Issue, Payment Problem, Provider Complaint, Account Help, Bug Report, Other
- Priority auto-assignment: Payment = High, Booking = Medium, Other = Low
- Required: category, subject (10-200 chars), message (20-5000 chars)
- Auto-assign to available support agent (round-robin)
- SLA: High = 4h response, Medium = 12h, Low = 24h

**#21-26 Chatbot**:
- Rate limit: 50 messages per user per hour
- Context: last 10 messages + user profile + current bookings
- Handoff triggers: user says "talk to human", 3 consecutive "I don't understand" from bot, confidence < 0.3
- On handoff: create SupportTicket with conversation history attached
- Provider: `IChatbotProvider` abstraction (OpenAI/Claude/custom LLM)
- Content guardrails: no price quoting, no booking modification, no payment processing via chat


---


#### Social Module Logic


**#3 POST `/reviews` (Create review)**:
- **Verified booking requirement**: user must have a `Completed` booking for this tour/place
- Rating: 1-5 integer, required
- Text: 20-2000 characters, optional but encouraged (10 bonus loyalty points for text review)
- Photos: max 5, must be JPEG/PNG/WebP, max 5MB each
- **Profanity filter**: scan text against blocklist + ML-based detection, auto-flag if suspicious
- One review per user per booking (prevent spam)
- Review appears immediately but flagged ones go to moderation queue
- Publish `ReviewCreatedIntegrationEvent` → recalculate tour rating, notify provider

**#4 PUT `/reviews/{id}` (Edit review)**:
- Allowed only within 48 hours of creation
- Re-run profanity filter on updated text
- Track edit history (keep original for moderation)

**#6 POST `/reviews/{id}/reply` (Provider reply)**:
- One reply per review (can edit, not add multiple)
- Provider must own the tour being reviewed
- Profanity filter on reply text
- Notify reviewer that provider replied

**Rating Algorithm (Bayesian Weighted Average)**:
```
weightedRating = (v / (v + m)) * R + (m / (v + m)) * C

Where:
  R = average rating of this tour
  v = number of reviews for this tour
  m = minimum reviews for confidence (default: 10)
  C = global average across all tours
  
Additional factors:
  - Recency: reviews in last 90 days weighted 2x
  - Verified badge: reviews from users with 5+ completed bookings weighted 1.5x
  - Text presence: reviews with text weighted 1.2x
```
- Recalculated daily by `RatingRecalculationService`

**#13 POST `/favorites` (Add to favorites)**:
- EntityType: Tour, Place
- Toggle: if already favorited, return 409 Conflict
- Publish `FavoriteAddedIntegrationEvent` → used by:
  - Discount notifications: when a favorited tour gets a discount, notify user
  - Recommendation engine: favorites indicate strong preference signal
- Max favorites: 500 per user (prevent abuse)

**#16 POST `/reports`**:
- Report reasons: Spam, Inappropriate, Misleading, Harassment, FakeReview, Other
- Required: entityType, entityId, reason, description (20-500 chars)
- Rate limit: max 10 reports per user per day (prevent report abuse)
- Auto-action: if 3+ reports on same content → auto-hide, escalate to moderation
- Moderation log: every action creates `ContentModerationLog` entry for audit trail


---


#### Tracking Module Logic


**#1 POST `/sessions` (Start live tracking)**:
- Only the assigned tour guide for a confirmed booking can start
- Validate: booking exists, is Confirmed, tour date is today, session not already active
- Create `LiveTrackingSession`: status=`Active`, startedAt=now
- Auto-notify booked participants via NotificationHub: "Live tracking started for {tourName}"
- Session auto-ends if no GPS update received for 1 hour (abandoned)

**Tracking Session State Machine**:
```
Scheduled → Active (guide starts)
Active → Completed (guide ends normally)
Active → Abandoned (no GPS for 1 hour)
```

**GPS Data Flow (via SignalR — NOT REST)**:
- Guide sends GPS every 5-15 seconds via `LiveTrackingHub.SendLocation()`
- Hub broadcasts to `session:{sessionId}` group (all watching participants)
- Batch persist: accumulate snapshots in memory, flush to `LocationSnapshots` table every 30 seconds
- Each snapshot: sessionId, lat, lng, altitude, speed, heading, accuracy, timestamp
- Data retention: 30 days (purged by `LocationSnapshotPurgeService`)

**#8 POST `/checkpoints`**:
- Tour checkpoints are waypoints along the tour route
- Fields: tourId, name, description, lat, lng, sortOrder, estimatedArrivalMinutes
- When guide reaches checkpoint (within 50m radius): auto-trigger `CheckpointReached` event
- Alternatively: guide manually marks checkpoint via `LiveTrackingHub.ReachCheckpoint()`

**Offline Mode**:
- If guide loses connectivity: client-side app buffers GPS data locally
- On reconnect: bulk-send buffered snapshots with original timestamps
- Participants see "Guide offline" indicator, last known position shown


---


#### Analytics Module Logic


**#1 POST `/interactions` (Track user interaction)**:
- Fire-and-forget: async processing, non-blocking response
- Interaction types: `View`, `Click`, `Search`, `Share`, `AddToFavorite`, `BookingStarted`, `BookingCompleted`
- Payload: entityType, entityId, interactionType, metadata (searchQuery, referrer, duration)
- Used by: recommendation engine (collaborative filtering), popularity scoring, analytics dashboards
- Storage: `analytics.UserInteractions` table (BIGINT auto-increment PK for high volume)
- Rate limit: deduplicate same user + same entity + same type within 5 minutes (prevent view inflation)

**#2 GET `/recommendations` (Personalized)**:
- **Hybrid Algorithm** (recalculated daily by `RecommendationEngineService`):
  - **Collaborative Filtering (40%)**: "Users who booked X also booked Y" — based on booking co-occurrence matrix
  - **Content-Based (35%)**: Match user's preferred categories, price range, location proximity, language
  - **Popularity (25%)**: Trending tours, high-rated tours, seasonal popularity
- Cache: `analytics.RecommendationCaches` per user (refreshed daily)
- Cold start (new user, <3 interactions): fall back to popularity-only recommendations
- Return: top 20 tours, sorted by composite recommendation score
- Exclude: already-booked tours, tours in disliked categories

**#4 GET `/popular/tours`**:
- Read from `analytics.PopularityScores` (recalculated every 6 hours)
- **Popularity Score Formula**:
```
score = (bookingCount * 3) + (reviewCount * 2) + (avgRating * 10) + (viewCount * 0.1) + (favoriteCount * 1.5) + recencyBonus

recencyBonus = max(0, 50 - daysSinceLastBooking)  // Recent activity boosts score
```
- Filterable by: category, place, date range, price range
- Return: top 50, with score breakdown for admin dashboard

**#6 GET `/trending`**:
- Trending = tours with highest score DELTA in last 7 days vs previous 7 days
- Captures viral / seasonal spikes (e.g., new tour launch, holiday season)
- Separate from popular (which is absolute, not relative)

**#8 PUT `/preferences`**:
- User explicitly sets: preferredCategories[], priceRange (min/max), preferredLanguages[], preferredDuration (min/max hours), childrenFriendly (bool), accessibilityNeeds[]
- Preferences feed into recommendation engine's content-based component
- Also used for: personalized search ranking, notification filtering

**#11 GET `/admin/audit-logs`**:
- Every state change in the system is logged: entity, action, oldValue, newValue, userId, timestamp, userAgent, IP
- Filterable: by entity type, action, user, date range, IP
- Retention: 2 years (regulatory compliance)
- Sensitive data: password changes logged as event (no values), payment data redacted

**#12-14 Admin Dashboard**:
- Real-time aggregations:
  - Revenue: total, by period (day/week/month), by provider, by category, by currency
  - Bookings: total, conversion rate (started → completed), cancellation rate, average booking value
  - Users: new registrations, active users (7d/30d), provider signups
- Provider dashboard (#15): filtered to current provider's data only
- Charts: time series data with configurable granularity (hourly/daily/weekly/monthly)


---


## Phase Endpoints Implementation Plan

To effectively execute the MVP and subsequent phases, the API endpoints defined above should be implemented in the following strict order. This ensures foundational modules are built before dependent features.

### Phase 1: Core Features (MVP)
**Goal:** Enable users to register, providers to list tours, and users to book and pay for those tours.

**Must-Implement Endpoints:**
1. **Auth & Security**: All `/api/auth/*` endpoints (Login, Register, OTP). Role assignments `/api/security/roles`.
2. **Accounts**: Provider onboarding `/api/accounts/provider/*` and Profile management `/api/accounts/profile`.
3. **Content Core & Places**: Category tree `/api/content-core/categories`, Attachments upload `/api/content-core/attachments`, and Place CRUD `/api/places/*`.
4. **Content Tours**: Tour CRUD `/api/tours/*`, Tour Schedules `/api/tours/{id}/schedules`, Pricing `/api/tours/{id}/pricing`, and search `/api/tours/search`.
5. **Booking**: The core booking state machine `/api/bookings/tour`, Provider confirmations `/api/bookings/{id}/confirm`, and Availability slots `/api/bookings/availability/*`.
6. **Finance**: Payment initiation and webhook `/api/finance/payments/*`, Provider Payouts `/api/finance/payouts/*`.
7. **Social**: Basic review submission `/api/social/reviews` and favorites `/api/social/favorites`.

### Phase 2: Experience & Discovery
**Goal:** Improve search visibility, user engagement, and real-time communication.

**Must-Implement Endpoints:**
1. **Content SEO**: Metadata overrides `/api/seo/metadata`, Sitemap generation `/api/seo/sitemap.xml`, and Weather integration `/api/seo/weather/*`.
2. **Content Blogs**: Full CMS for articles `/api/blogs/*` and comments `/api/blogs/{id}/comments`.
3. **Content Places (Map)**: Map viewport loading `/api/places/map/viewport` and Nearby search `/api/places/nearby`.
4. **Messaging**: Notifications center `/api/messaging/notifications`, push device tokens `/api/messaging/devices/token`, and Support Tickets `/api/messaging/support/tickets`.
5. **Analytics**: Interaction tracking `/api/analytics/interactions` and Admin Dashboards `/api/analytics/admin/dashboard`.

### Phase 3: Growth & Monetization
**Goal:** Increase revenue through bundles, subscriptions, and retention programs.

**Must-Implement Endpoints:**
1. **Content Tours**: Multi-tour Packages `/api/tours/packages/*`.
2. **Booking**: Package booking `/api/bookings/package`.
3. **Finance (Monetization)**: Subscriptions `/api/finance/subscriptions/*`, Loyalty Points `/api/finance/loyalty/*`, and Referrals `/api/finance/referrals/*`.
4. **Finance (Disputes)**: The Dispute Center `/api/finance/disputes/*` for handling conflicts.
5. **Social**: Accessibility specific reviews `/api/social/accessibility/reviews`.

### Phase 4: Advanced Features & AI
**Goal:** Differentiate the platform with real-time tracking, AI assistance, and dynamic pricing.

**Must-Implement Endpoints:**
1. **Tracking**: Live GPS tracking sessions `/api/tracking/sessions/*` and Waypoint Checkpoints `/api/tracking/checkpoints`.
2. **Analytics**: Personalized AI Recommendations `/api/analytics/recommendations` and User Preferences `/api/analytics/preferences`.
3. **Messaging**: Real-time AI Chatbot `/api/messaging/chatbot/*`.
4. **Finance**: Advanced Discount Engine (Flash sales, Early Bird) `/api/finance/discounts/*`.

---

## MVP Project Process Suggestion

To successfully launch the Minimum Viable Product (MVP) for YallaJo, we must avoid "Big Bang" integration. By breaking the work into vertical, functional slices, we can test and verify value immediately. 

I recommend focusing **strictly on Phase 1** for the MVP. Phases 2, 3, and 4 should be deferred until real users are successfully completing bookings.

### MVP Scope (The "Golden Path")
1. User registers/logs in.
2. Provider registers and is approved.
3. Provider creates a Tour (which gets auto-translated).
4. User searches/finds the Tour.
5. User books and pays for the Tour.
6. User leaves a review.

### Step-by-Step Implementation Plan

#### Step 1: Infrastructure & Scaffolding (1 Week)
- Implement `ITranslationService` and the `AutoSaveTranslationService` decorator.
- Wire up the 6 new Middlewares (CorrelationId, Localization, ExceptionHandlers).
- Ensure the Outbox/Inbox background processor is running smoothly.
- **Verification**: Can translate a dummy string and see it hit the DB. Pipeline handles errors gracefully.

#### Step 2: Identity & Access (1 Week)
- **Modules**: `Auth`, `Security`, `Accounts` (User Profiles).
- Implement JWT Bearer token generation, Refresh Token rotation, and basic Email OTP.
- **Verification**: User can register, verify email, login, and access a locked `[Authorize]` endpoint.

#### Step 3: Provider Onboarding (1 Week)
- **Modules**: `Accounts` (Provider flow), `Booking` (Provider Documents).
- Build the Provider State Machine (`Pending -> Approved`).
- Implement Document upload with secure attachment storage.
- **Verification**: Admin can approve a Provider application, granting them the `Provider` role.

#### Step 4: Core Content & Catalog (2 Weeks)
- **Modules**: `ContentCore`, `ContentPlaces`, `ContentTours`.
- Build Category hierarchy, Place CRUD, and Tour CRUD.
- Integrate the auto-translation event hook (When Tour created -> translate to EN/AR).
- **Verification**: Provider can create a Tour with pricing and schedule. It appears in the public search listing in both Arabic and English.

#### Step 5: Booking Engine & State Machine (2 Weeks)
- **Modules**: `Booking`.
- Implement the complex Booking State Machine (`AwaitingPayment -> Confirmed -> Completed`).
- Implement the 10-minute pessimistic/optimistic `SlotLock` to prevent double-booking.
- Build the Background Services (`SlotLockCleanupService`, `BookingAutoExpireService`).
- **Verification**: User can lock a slot, wait 10 minutes, and watch it automatically release.

#### Step 6: Payments & Escrow (1 Week)
- **Modules**: `Finance`.
- Abstract the `IPaymentGateway` (Stripe/PayMob mock for dev).
- Implement the payment Webhook listener to transition Booking from `AwaitingPayment` to `Confirmed`.
- **Verification**: Successful payment webhook triggers `PaymentCompletedIntegrationEvent` and confirms the booking.

#### Step 7: Post-Booking (1 Week)
- **Modules**: `Social`.
- Implement Reviews (restricted to verified `Completed` bookings).
- Basic Admin dashboards for monitoring.
- **Verification**: User can leave a 1-5 star review, and the `Tour.AverageRating` recalculates.

### MVP Release Criteria
- [ ] No fake data needed to complete the Golden Path.
- [ ] Database strictly enforces referential integrity without cross-module FKs.
- [ ] All Background Services for Phase 1 are running via `PeriodicTimer`.
- [ ] 100% of Phase 1 API Endpoints return expected `Result<T>` and correct HTTP Status Codes.

---

## Summary Statistics

| Category | Count |
|---|---|
| **Total API Endpoints** | **~196** |
| Auth Module | 16 |
| Security Module | 11 |
| Accounts Module | 15 |
| ContentCore Module | 26 |
| ContentPlaces Module | 16 |
| ContentTours Module | 34 |
| ContentBlogs Module | 16 |
| ContentSeo Module | 15 |
| Booking Module | 32 |
| Finance Module | 52 |
| Messaging Module | 26 |
| Social Module | 22 |
| Tracking Module | 11 |
| Analytics Module | 15 |
| **New Middleware** | **6** |
| **New Background Services** | **18** |
| **SignalR Hubs** | **3** |
| **New Exception Handlers** | **5** |

### Phase Breakdown

| Phase | Endpoints | Background Services | SignalR | Other |
|---|---|---|---|---|
| Phase 1 (Core) | ~80 | 6 | — | 3 middleware, 3 exception handlers |
| Phase 2 (Discovery) | ~55 | 4 | NotificationHub | 3 middleware, 2 exception handlers |
| Phase 3 (Monetization) | ~35 | 5 | — | — |
| Phase 4 (AI/Advanced) | ~26 | 3 | LiveTrackingHub, ChatBotHub | — |

