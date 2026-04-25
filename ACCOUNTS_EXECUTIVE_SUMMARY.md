# YallaJo Accounts Module - Executive Summary

## Overview

The Accounts module manages user profiles linked to Security identities. Profile creation happens through **TWO distinct paths**:

1. **Synchronous Path** (Auth.Register, Auth.InviteUser): Profile created in same request
2. **Asynchronous Path** (UserCreatedIntegrationEvent): Profile created via background job

## Critical Architecture

### Synchronous Profile Creation (NORMAL PATH)

```
Auth.RegisterCommandHandler
  ↓
Security.UserRegistrationService.RegisterAsync()  [creates User]
  ↓
Accounts.ProfileCreationService.CreateForUserAsync()  [creates Profile]
  ↓
✓ Both saved atomically in same request
```

**File**: `C:\Users\User\source\repos\YallaJoJo\Auth.Application\Commands\Register\RegisterCommandHandler.cs` (Lines 21-59)

**Key Code**:
```csharp
// Line 25-31: Create Security user
var result = await userRegistrationService.RegisterAsync(...);

// Line 43-48: Create Accounts profile in SAME request
var profileResult = await profileCreationService.CreateForUserAsync(...);

// Line 50: Ignore Conflict (profile already exists)
if (profileResult.IsFailure && profileResult.Outcome != Outcome.Conflict)
```

### Asynchronous Profile Creation (FALLBACK PATH)

```
Security publishes UserCreatedIntegrationEvent
  ↓ [stored in Security.OutboxMessages]
OutboxProcessor (background job)
  ↓
Publishes IntegrationEventNotification<UserCreatedIntegrationEvent> via MediatR
  ↓
Accounts.UserCreatedIntegrationEventHandler
  ↓
Creates Profile via Profile.Create(userId, firstName, lastName)
  ↓
✓ Saved atomically with inbox message
```

**File**: `C:\Users\User\source\repos\YallaJoJo\Accounts.Application\EventHandlers\UserCreatedIntegrationEventHandler.cs` (Lines 19-56)

**Key Code**:
```csharp
// Line 24-30: Idempotency check
if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
    return;

// Line 34-43: Check if profile already exists
if (await profileRepository.AnyAsync(p => p.UserId == evt.UserId, ct))
{
    inboxStore.MarkAsProcessed(notification.MessageId);
    await unitOfWork.SaveChangesAsync(ct);
    return;
}

// Line 45-51: Create profile and mark inbox atomically
var profile = Profile.Create(evt.UserId, evt.FirstName, evt.LastName);
await profileRepository.AddAsync(profile, ct);
inboxStore.MarkAsProcessed(notification.MessageId);
await unitOfWork.SaveChangesAsync(ct);
```

## Profile Creation Service

**File**: `C:\Users\User\source\repos\YallaJoJo\Accounts.Application\Services\ProfileCreationService.cs` (Lines 47-73)

**Exposed via Contract**: `Accounts.Contracts.Abstractions.IProfileCreationService`

**Methods**:
- `CreateForUserAsync(ProfileCreationRequest)` - Normal registration
- `CreateForInvitedUserAsync(InvitedProfileCreationRequest)` - Invite flow

**Key Features**:
- ✓ Idempotency: Checks if profile already exists (returns Conflict)
- ✓ Synchronous: Calls `SaveChangesAsync()` immediately
- ✓ Atomic: Profile and inbox message saved together
- ✓ No event publishing: Does not emit domain or integration events

## GetProfile Query Handler

**File**: `C:\Users\User\source\repos\YallaJoJo\Accounts.Application\Queries\GetProfile\GetProfileQueryHandler.cs` (Lines 13-53)

**Behavior**:
1. Query Accounts database for profile by UserId
2. If profile is null: return 404 "Profile not found"
3. Query Security module for contact data (email, phone)
4. If contact is null: return 404 "User not found"
5. Combine profile + contact data and return

**Caching**:
- Cache key: `accounts:profile:{userId}`
- Duration: 10 minutes
- Failures are NOT cached (only successes)

**CRITICAL ISSUE**: Returns 404 if profile doesn't exist, but does NOT distinguish between:
- Profile never created (permanent)
- Profile not yet created (async event pending)

## Inbox/Outbox Pattern

### OutboxProcessor (Background Job)

**File**: `C:\Users\User\source\repos\YallaJoJo\YallaJo.SharedKernel.Infrastructure\BackgroundJobs\OutboxProcessor.cs` (Lines 33-96)

**Flow**:
1. Query unprocessed messages from OutboxMessages table
2. Lock messages (set LockedUntil = now + 5 minutes)
3. For each message:
   - Deserialize event from JSON
   - Create IntegrationEventNotification<TEvent>
   - Publish via MediatR (in-process)
   - Mark as processed
4. Save all changes atomically

**Retry Logic**:
- Max retries: 3
- Lock duration: 5 minutes
- If lock expires, another instance can retry

### Inbox Store (Idempotency)

**File**: `C:\Users\User\source\repos\YallaJoJo\Accounts.Infrastructure\Persistence\AccountsInboxStore.cs` (Lines 7-14)

**Methods**:
- `HasBeenProcessedAsync(messageId)` - Checks if message already processed
- `MarkAsProcessed(messageId)` - Adds to change tracker (saved atomically)

**Idempotency Key**: OutboxMessage.Id = InboxMessage.Id

## Database Schema

**Schema**: `accounts`

**Tables**:
- `Profiles` - Main aggregate (unique index on UserId)
- `InboxMessages` - Idempotency tracking
- `OutboxMessages` - Event publishing

**Profile Table**:
```sql
CREATE TABLE [accounts].[Profiles] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,  -- Logical reference, NOT FK
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [DisplayName] nvarchar(200),
    [AvatarUrl] nvarchar(2048),
    [DateOfBirth] date,
    [Gender] nvarchar(20),
    [Country] nvarchar(100),
    [City] nvarchar(100),
    [AddressLine] nvarchar(300),
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2,
    [IsDeleted] bit NOT NULL DEFAULT 0,
    [DeletedAt] datetime2,
    [RowVersion] rowversion NOT NULL,
    PRIMARY KEY ([Id]),
    UNIQUE INDEX [IX_Profiles_UserId_Unique] ([UserId])
);
```

## Critical Findings

### ✓ SAFE: Dual Profile Creation Paths

**Issue**: Both sync and async paths can create the same profile

**Mitigation**:
- ProfileCreationService checks for existing profile (returns Conflict)
- UserCreatedIntegrationEventHandler checks for existing profile (marks inbox and returns)
- Both are idempotent

### ✓ SAFE: Inbox Message Marked Before SaveChanges

**Issue**: Inbox message is marked in change tracker before SaveChangesAsync

**Mitigation**:
- SaveChangesAsync() is atomic
- If it fails, neither profile nor inbox message is persisted
- OutboxProcessor will retry

### ✓ SAFE: Duplicate Profile Creation on Event Retry

**Issue**: Event may be processed twice (network retry, etc.)

**Mitigation**:
- Inbox check prevents duplicate processing
- Primary key on InboxMessage.Id prevents duplicates

### ⚠️ PROBLEM: GetProfile Returns 404 Before Async Profile Created

**Issue**: If only async event path is used, GetProfile may return 404 before profile is created

**Impact**:
- Client cannot distinguish between "not found" and "not yet created"
- No retry guidance
- No "please wait" message

**Mitigation**:
- Synchronous path (Auth.Register) creates profile immediately
- Async path is only for direct Security registration (rare)
- Most users go through Auth.Register (synchronous)

### ⚠️ PROBLEM: No "Account Not Yet Created" Distinction

**Issue**: GetProfileQueryHandler returns 404 for both:
1. Profile doesn't exist (never created)
2. Profile not yet created (async event pending)

**Impact**:
- UX problem: no guidance on what to do
- No retry logic
- No "please wait" message

**Mitigation**:
- Synchronous path (Auth.Register) creates profile immediately
- Async path is rare (only for direct Security registration)
- Most users won't encounter this issue

## Integration Events

### UserCreatedIntegrationEvent

**File**: `C:\Users\User\source\repos\YallaJoJo\Security.Contracts\IntegrationEvents\UserCreatedIntegrationEvent.cs`

```csharp
public sealed record UserCreatedIntegrationEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName) : IntegrationEventBase;
```

**Published by**: Security module when user is created
**Consumed by**: 
- Accounts module (creates profile)
- Auth module (sends verification OTP)

### EmailVerifiedIntegrationEvent

**Consumed by**: Accounts module (marks inbox, no action)

### PhoneNumberUpdatedIntegrationEvent

**Cons
