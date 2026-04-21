# Profile Creation Root Cause Analysis: GetYourProfile 404 After Registration

## Executive Summary

**Root Cause Identified:** The profile creation flow is **architecturally sound** but has **one critical timing vulnerability**: the `UserCreatedIntegrationEvent` is published asynchronously via the outbox pattern, and if the client calls `GET /profile` before the background outbox processor has executed, the profile will not exist yet, resulting in a **404 NotFound**.

---

## 1. Profile Creation Path (Complete Flow)

### 1.1 Registration Entry Point
**File:** `Auth.Application/Commands/Register/RegisterCommandHandler.cs`

```csharp
public async Task<Result<RegisterResult>> Handle(
    RegisterCommand request,
    CancellationToken cancellationToken)
{
    var result = await userRegistrationService.RegisterAsync(
        new UserRegistrationRequest(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password),
        cancellationToken);
    
    return result.Map(userId => new RegisterResult(userId));
}
```

**What happens:**
- Auth module delegates to `IUserRegistrationService` (Security module contract)
- No direct profile creation here — only Security identity is created

---

### 1.2 User Registration (Security Module)
**File:** `Security.Application/Services/UserRegistrationService.cs` (lines 30-51)

```csharp
public async Task<Result<Guid>> RegisterAsync(
    UserRegistrationRequest request,
    CancellationToken cancellationToken = default)
{
    var normalizedEmail = SecurityGuard.NormalizeEmail(request.Email);
    
    if (await EmailExistsAsync(normalizedEmail, cancellationToken))
    {
        return Result<Guid>.Conflict(...);
    }
    
    var user = User.Register(normalizedEmail, request.FirstName, request.LastName);
    user.SetPasswordHash(passwordHasher.Hash(request.Password));
    
    await userRepository.AddAsync(user, cancellationToken);
    await unitOfWork.SaveChangesAsync(cancellationToken);  // ← Persists to Security DB
    
    await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, cancellationToken);
    
    return Result<Guid>.Created(user.Id);
}
```

**What happens:**
- Creates `User` aggregate via `User.Register()` factory method
- **Raises `UserCreatedEvent` domain event** (see below)
- Persists user to Security database
- Returns UserId

---

### 1.3 Domain Event Raised
**File:** `Security.Domain/Entities/User.cs` (line 37)

```csharp
public static User Register(string email, string firstName, string lastName)
{
    var user = new User { IsActive = false };
    var primaryEmail = Email.Create(user.Id, email, true);
    user._emails.Add(primaryEmail);
    
    user.AddDomainEvent(new UserCreatedEvent(user.Id, primaryEmail.Address, firstName, lastName));
    
    return user;
}
```

**Domain Event Definition:**
**File:** `Security.Domain/Events/UserCreatedEvent.cs`

```csharp
public sealed record UserCreatedEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName) : DomainEventBase;
```

---

### 1.4 Domain Event → Integration Event (Outbox Pattern)
**File:** `Security.Infrastructure/EventHandlers/UserCreatedDomainEventHandler.cs`

```csharp
public sealed class UserCreatedDomainEventHandler(
    SecurityDbContext dbContext,
    ILogger<UserCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<UserCreatedEvent>>
{
    public Task Handle(
        DomainEventNotification<UserCreatedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.Event;
        
        logger.LogInformation(
            "Handling UserCreatedEvent for user {UserId}, writing to outbox",
            domainEvent.UserId);
        
        var integrationEvent = new UserCreatedIntegrationEvent(
            domainEvent.UserId,
            domainEvent.Email,
            domainEvent.FirstName,
            domainEvent.LastName);
        
        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
        
        return Task.CompletedTask;
    }
}
```

**What happens:**
- Converts domain event to integration event
- **Writes to `OutboxMessages` table** (Security schema)
- **NOT immediately published** — waits for background processor

**Integration Event Definition:**
**File:** `Security.Contracts/IntegrationEvents/UserCreatedIntegrationEvent.cs`

```csharp
public sealed record UserCreatedIntegrationEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName) : IntegrationEventBase;
```

---

### 1.5 Outbox Processing (Background Job)
**File:** `YallaJo.SharedKernel.Infrastructure/BackgroundJobs/OutboxProcessor.cs`

```csharp
public async Task ProcessOutboxMessagesAsync(CancellationToken ct = default)
{
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    
    var now = DateTime.UtcNow;
    
    // Select unprocessed, unlocked messages
    var messages = await dbContext.Set<OutboxMessage>()
        .Where(m => m.ProcessedOnUtc == null
                 && m.RetryCount < MaxRetryCount
                 && (m.LockedUntil == null || m.LockedUntil < now))
        .OrderBy(m => m.OccurredOnUtc)
        .Take(BatchSize)
        .ToListAsync(ct);
    
    if (messages.Count == 0) return;
    
    // Claim messages with lock
    var lockUntil = now.Add(LockDuration);  // 5 minutes
    foreach (var msg in messages)
        msg.Lock(lockUntil);
    
    await dbContext.SaveChangesAsync(ct);
    
    foreach (var message in messages)
    {
        try
        {
            var eventType = Type.GetType(message.Type);
            var integrationEvent = JsonSerializer.Deserialize(message.Content, eventType) as IIntegrationEvent;
            
            var notificationType = typeof(IntegrationEventNotification<>).MakeGenericType(eventType);
            var notification = Activator.CreateInstance(notificationType, message.Id, integrationEvent)!;
            
            await mediator.Publish((INotification)notification, ct);  // ← Publishes to Accounts handler
            
            message.MarkAsProcessed();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process outbox message {Id}", message.Id);
            message.MarkAsFailed(ex.Message);
        }
    }
    
    await dbContext.SaveChangesAsync(ct);
}
```

**Key Details:**
- Runs every **10 seconds** (see `CompositeOutboxProcessor`)
- Processes in batches of 20 messages
- Uses distributed locking (5-minute lock duration)
- Publishes via MediatR as `IntegrationEventNotification<UserCreatedIntegrationEvent>`

**Composite Processor Registration:**
**File:** `YallaJo.SharedKernel.Infrastructure/DependencyInjection.cs` (line 43)

```csharp
services.AddHostedService<CompositeOutboxProcessor>();
```

---

### 1.6 Profile Creation Handler (Accounts Module)
**File:** `Accounts.Application/EventHandlers/UserCreatedIntegrationEventHandler.cs`

```csharp
public sealed class UserCreatedIntegrationEventHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    IAccountsInboxStore inboxStore,
    ILogger<UserCreatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<UserCreatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<UserCreatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        // Inbox check — idempotency guard
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogWarning(
                "Accounts: Message {MessageId} (UserCreated for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }
        
        var evt = notification.Event;
        
        // Duplicate check
        if (await profileRepository.AnyAsync(p => p.UserId == evt.UserId, ct))
