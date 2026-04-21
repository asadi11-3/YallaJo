# GetYourProfile Endpoint Trace Analysis

## Executive Summary
The `GetYourProfile` endpoint (named "GetProfile" in code) retrieves the authenticated user's profile from the **Accounts module**. It expects:
1. An authenticated user with a valid `UserId` claim
2. A corresponding Profile record in the Accounts database
3. A corresponding User record in the Security database with a primary email

**Profile Not Found** occurs when the Profile row is missing from the Accounts.Profiles table, even though the user is authenticated.

---

## Endpoint Details

### HTTP Route
```
GET /api/v1/accounts/profile/
```

### Endpoint Name
```
GetProfile
```

### Location
```
Accounts.Presentation/Endpoints/Profile/ProfileEndpoints.cs (lines 31-44)
```

### Handler Code
```csharp
profile.MapGet("/", async (ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        return Results.Unauthorized();

    var result = await sender.Send(new GetProfileQuery(currentUser.UserId.Value), ct);
    return result.ToApiResult();
})
.WithName("GetProfile")
.Produces<GetProfileResult>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status404NotFound)
.WithSummary("Get the current user's profile")
.RequireAuthorization();
```

### Authentication Requirement
- **Required**: User must be authenticated (`currentUser.IsAuthenticated == true`)
- **Required**: User must have a valid `UserId` claim in JWT token
- **UserId Source**: JWT claim "sub" (subject claim)
- **Authorization**: `RequireAuthorization()` enforced

---

## Query Handler

### Location
```
Accounts.Application/Queries/GetProfile/GetProfileQueryHandler.cs
```

### Query Definition
```csharp
public sealed record GetProfileQuery(Guid UserId) : IQuery<GetProfileResult>, ICacheableQuery
{
    public string CacheKey => AccountsCacheKeys.UserProfile(UserId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IEnumerable<string> Tags => [AccountsCacheKeys.UserProfileTag(UserId)];
}
```

### Handler Logic - Critical Path
```csharp
public async Task<Result<GetProfileResult>> Handle(
    GetProfileQuery request,
    CancellationToken cancellationToken)
{
    var userId = request.UserId;

    // STEP 1: Query Profile from Accounts database
    var profile = await profileRepository.FirstOrDefaultAsync(
        filter: p => p.UserId == userId,
        ct: cancellationToken);

    // CONDITION 1: Profile Not Found
    if (profile is null)
    {
        return Result<GetProfileResult>.Failure(
            Error.NotFound("Profile", "Profile not found."),
            Outcome.NotFound);  // ← Returns 404 NotFound
    }

    // STEP 2: Query User contact data from Security database
    var contact = await securityService.GetPrimaryContactDataAsync(userId, cancellationToken);
    
    // CONDITION 2: User Not Found in Security
    if (contact is null)
    {
        return Result<GetProfileResult>.Failure(
            Error.NotFound("User", "User not found."),
            Outcome.NotFound);  // ← Returns 404 NotFound
    }

    // STEP 3: Combine and return
    var result = new GetProfileResult(
        UserId: profile.UserId,
        FirstName: profile.FirstName,
        LastName: profile.LastName,
        DisplayName: profile.DisplayName,
        AvatarUrl: profile.AvatarUrl,
        PhoneNumber: contact.PhoneNumber,
        DateOfBirth: profile.DateOfBirth,
        Gender: profile.Gender?.ToString(),
        Country: profile.Country,
        City: profile.City,
        AddressLine: profile.AddressLine,
        Email: contact.Email);

    return Result<GetProfileResult>.Success(result);
}
```

---

## Repository Method

### Interface
```
Accounts.Domain/Repositories/IProfileRepository.cs
```

### Method Used
```csharp
profileRepository.FirstOrDefaultAsync(
    filter: p => p.UserId == userId,
    ct: cancellationToken)
```

**Note**: `IProfileRepository` inherits from `IRepository<Profile, Guid>` which provides the `FirstOrDefaultAsync` method.

### Database Query
- **Table**: `accounts.Profiles`
- **Filter**: `WHERE UserId = @userId AND IsDeleted = 0`
- **Returns**: Single Profile record or null

---

## Profile Entity & Database Schema

### Location
```
Accounts.Domain/Entities/Profile.cs
Accounts.Infrastructure/Persistence/Configurations/ProfileConfiguration.cs
```

### Profile Entity Structure
```csharp
public sealed class Profile : AuditableEntity, IAggregateRoot
{
    public Guid Id { get; private set; }                    // Primary Key
    public Guid UserId { get; private set; }                // Logical reference to Security.User.Id (NOT a FK)
    public string FirstName { get; private set; }           // Required, max 100
    public string LastName { get; private set; }            // Required, max 100
    public string? DisplayName { get; private set; }        // Optional, max 200
    public string? AvatarUrl { get; private set; }          // Optional, max 2048
    public DateOnly? DateOfBirth { get; private set; }      // Optional
    public Gender? Gender { get; private set; }             // Optional enum
    public string? Country { get; private set; }            // Optional, max 100
    public string? City { get; private set; }               // Optional, max 100
    public string? AddressLine { get; private set; }        // Optional, max 300
    
    // Auditable fields
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public byte[] RowVersion { get; private set; }
}
```

### Database Table
```
Table: accounts.Profiles
├── Id (GUID, PK, ValueGeneratedNever)
├── UserId (GUID, Required, Unique Index)
├── FirstName (VARCHAR(100), Required)
├── LastName (VARCHAR(100), Required)
├── DisplayName (VARCHAR(200), Nullable)
├── AvatarUrl (VARCHAR(2048), Nullable)
├── DateOfBirth (DATE, Nullable)
├── Gender (VARCHAR(20), Nullable)
├── Country (VARCHAR(100), Nullable)
├── City (VARCHAR(100), Nullable)
├── AddressLine (VARCHAR(300), Nullable)
├── CreatedAt (DATETIME, Required)
├── UpdatedAt (DATETIME, Nullable)
├── IsDeleted (BIT, Required, Default=0)
├── DeletedAt (DATETIME, Nullable)
└── RowVersion (ROWVERSION)

Indexes:
├── PK_Profiles (Id)
└── IX_Profiles_UserId_Unique (UserId) ← ONE profile per user
```

**Key Constraint**: `UserId` has a **UNIQUE** index — only ONE profile per user allowed.

---

## Profile Creation Flow

### Trigger
When a new user is created in the Security module, a `UserCreatedIntegrationEvent` is published.

### Handler
```
Accounts.Application/EventHandlers/UserCreatedIntegrationEventHandler.cs
```

### Creation Logic
```csharp
public async Task Handle(
    IntegrationEventNotification<UserCreatedIntegrationEvent> notification,
    CancellationToken ct)
{
    // Idempotency check
    if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
    {
        logger.LogWarning("Message {MessageId} already processed — skipping.", notification.MessageId);
        return;
    }

    var evt = notification.Event;

    // Duplicate check
    if (await profileRepository.AnyAsync(p => p.UserId == evt.UserId, ct))
    {
        logger.LogWarning("Profile already exists for user {UserId} — marking inbox and skipping.", evt.UserId);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        return;
    }

    // CREATE PROFILE
    var profile = Profile.Create(evt.UserId, evt.FirstName, evt.LastName);
    await profileRepository.AddAsync(profile, ct);

    // Mark as processed and persist atomically
    inboxStore.MarkAsProcessed(notification.MessageId);
    await unitOfWork.SaveChangesAsync(ct);

    logger.LogInformation(
        "Profile {ProfileId} created for user {UserId} ({FirstName} {LastName}).",
        profile.Id, evt.UserId, evt.FirstName, evt.LastName);
}
```

### Event Data
```csharp
public sealed record UserCreatedIntegrationEvent(
    Guid UserId,
    string Em
