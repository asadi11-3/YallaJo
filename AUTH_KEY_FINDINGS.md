# YallaJoJo Auth Database - Key Findings Summary

## 🎯 Overview

The YallaJoJo authentication system is well-architected with two distinct modules:

1. **Security Module** - User identity, roles, claims, emails, phones
2. **Auth Module** - Sessions, devices, refresh tokens, OTPs, external providers

---

## ✅ Strengths

### 1. **Proper Cascade Delete Configuration**
- All foreign key relationships configured with `OnDelete(DeleteBehavior.Cascade)`
- Deleting a user cascades to: Emails, Phones, UserRoles, UserClaims
- Deleting a device cascades to: Sessions
- Deleting a session cascades to: RefreshTokens

### 2. **Email Uniqueness Enforcement**
- **Unique constraint** on `Emails.Address` column
- Prevents duplicate email registrations globally
- Index: `IX_Emails_Address_Unique`

### 3. **Token Expiry Fields**
- ✅ **Sessions.ExpiresAt** - Session expiration timestamp
- ✅ **RefreshTokens.ExpiresAt** - Token expiration timestamp  
- ✅ **Otps.ExpiresAt** - OTP expiration timestamp
- All have dedicated indexes for cleanup queries

### 4. **Comprehensive Soft Delete**
- All entities support soft delete with `IsDeleted` and `DeletedAt` fields
- Query filters automatically exclude deleted records
- Allows data recovery and audit trails

### 5. **Concurrency Control**
- All entities have `RowVersion` field (SQL Server rowversion type)
- Prevents lost updates in concurrent scenarios
- Optimistic locking implementation

### 6. **Advanced Index Optimization**
- **Filtered indexes** for performance:
  - `Phones.PhoneNumber` - Only indexes verified phones
  - `Otps` - Only indexes unused OTPs (WHERE IsUsed = 0)
  - `ExternalProviders` - Only indexes active links (WHERE IsActive = 1)
- **Composite indexes** for common query patterns
- **Covering indexes** for efficient lookups

### 7. **Audit Trail**
- `CreatedAt`, `UpdatedAt`, `DeletedAt` on all entities
- Dedicated `AuditLogs` table with before/after values
- IP address and user tracking

---

## ⚠️ Important Considerations

### 1. **Logical Foreign Keys (No DB Constraints)**
The Auth module references Users via `UserId` fields **without database constraints**:
- `Devices.UserId` → `Users.Id`
- `Sessions.UserId` → `Users.Id`
- `RefreshTokens.UserId` → `Users.Id`
- `Otps.UserId` → `Users.Id`
- `ExternalProviders.UserId` → `Users.Id`

**Why:** Allows module independence and separate database deployments

**Implication:** Application code must enforce referential integrity when deleting users

### 2. **Brute-Force Protection on OTPs**
- `Otps.AttemptCount` tracks failed verification attempts
- Domain invariant: `IsExhausted => AttemptCount >= 5`
- **Max 5 attempts** before OTP is locked

### 3. **Device Trust Tracking**
- `Devices.IsTrusted` flag with `TrustedAt` timestamp
- `Devices.LastSeenAt` tracks last activity
- Enables device management and security features

### 4. **Session Revocation**
- `Sessions.IsRevoked` flag with `RevokedAt` timestamp
- Allows immediate session termination
- Supports "logout all devices" functionality

### 5. **Token Replacement Chain**
- `RefreshTokens.ReplacedByTokenId` tracks token rotation
- Enables token refresh without creating orphaned tokens
- Supports token revocation chains

### 6. **External Provider Deactivation**
- `ExternalProviders.IsActive` flag allows unlinking
- **Filtered unique index** prevents re-linking issues:
  - Without filter: Would permanently block re-linking
  - With filter: Allows re-linking after deactivation

---

## 📊 Table Summary

### Security Schema (7 tables)
| Table | Purpose | Key Constraint |
|-------|---------|-----------------|
| Users | User identity | PK: Id |
| Emails | Email addresses | UNIQUE: Address |
| Phones | Phone numbers | Filtered UNIQUE: PhoneNumber (IsVerified=1) |
| Roles | Role definitions | UNIQUE: Name |
| UserRoles | User-Role assignments | UNIQUE: (UserId, RoleId) |
| UserClaims | User-specific claims | UNIQUE: (UserId, ClaimType, ClaimValue) |
| RoleClaims | Role-specific claims | UNIQUE: (RoleId, ClaimType, ClaimValue) |
| AuditLogs | Audit trail | - |

### Auth Schema (7 tables)
| Table | Purpose | Key Constraint |
|-------|---------|-----------------|
| Devices | Device registration | UNIQUE: DeviceToken |
| Sessions | User sessions | FK: DeviceId (Cascade) |
| RefreshTokens | Refresh tokens | UNIQUE: TokenHash, FK: SessionId (Cascade) |
| Otps | One-time passwords | Filtered Index: (UserId, Purpose, IsUsed) |
| ExternalProviders | OAuth providers | Filtered UNIQUE: (Provider, ProviderUserId) |
| OutboxMessages | Event publishing | - |
| InboxMessages | Event idempotency | - |

---

## 🔐 Security Features

### Password Management
- `Users.PasswordHash` - Hashed password (max 512 chars)
- No plaintext passwords stored
- Supports password reset and change operations

### Email Verification
- `Emails.IsVerified` flag with `VerifiedAt` timestamp
- `Emails.IsPrimary` designates primary email
- Prevents unverified email usage

### Phone Verification
- `Phones.IsVerified` flag with `VerifiedAt` timestamp
- `Phones.IsPrimary` designates primary phone
- Filtered index optimizes verified phone queries

### OTP Security
- `Otps.CodeHash` - Hashed OTP code (never stored plaintext)
- `Otps.AttemptCount` - Brute-force protection (max 5)
- `Otps.ExpiresAt` - Time-limited validity
- `Otps.IsUsed` - One-time use enforcement

### Session Security
- `Sessions.ExpiresAt` - Session timeout
- `Sessions.IsRevoked` - Immediate termination
- `Sessions.IpAddress` - IP tracking
- Cascade delete prevents orphaned tokens

### Token Security
- `RefreshTokens.TokenHash` - Hashed token (never stored plaintext)
- `RefreshTokens.ExpiresAt` - Token expiration
- `RefreshTokens.IsRevoked` - Token revocation
- `RefreshTokens.ReplacedByTokenId` - Token rotation tracking

---

## 🚀 Performance Optimizations

### Filtered Indexes
```sql
-- Only index active OTPs (unused)
CREATE INDEX IX_Otps_UserId_Purpose_IsUsed_Active 
ON Otps(UserId, Purpose, IsUsed)
WHERE IsUsed = 0;

-- Only index verified phones
CREATE INDEX IX_Phones_PhoneNumber 
ON Phones(PhoneNumber)
WHERE IsVerified = 1;

-- Only index active external provider links
CREATE UNIQUE INDEX IX_ExternalProviders_Provider_ProviderUserId_Active
ON ExternalProviders(Provider, ProviderUserId)
WHERE IsActive = 1;
```

### Cleanup Query Optimization
```sql
-- Sessions cleanup
SELECT * FROM Sessions WHERE ExpiresAt < GETUTCDATE()

-- RefreshTokens cleanup
SELECT * FROM RefreshTokens WHERE ExpiresAt < GETUTCDATE()

-- OTPs cleanup
SELECT * FROM Otps WHERE ExpiresAt < GETUTCDATE()
```

All have dedicated indexes on `ExpiresAt` column.

---

## 📋 Checklist for Developers

### When Deleting a User
- [ ] Verify all Auth module data is cleaned up (Sessions, Devices, RefreshTokens, Otps, ExternalProviders)
- [ ] Consider cascade delete behavior
- [ ] Check audit logs for user activity
- [ ] Notify user of account deletion

### When Implementing Token Refresh
- [ ] Use `RefreshTokens.ExpiresAt` for validation
- [ ] Set `ReplacedByTokenId` when issuing new token
- [ ] Revoke old token with `IsRevoked = true`
- [ ] Check `Sessions.ExpiresAt` as well

### When Implementing Session Management
- [ ] Check `Sessions.ExpiresAt` for validity
- [ ] Check `Sessions.IsRevoked` status
- [ ] Verify `Devices.LastSeenAt` for activity tracking
- [ ] Use `Sessions.IpAddress` for security checks

### When Implementing OTP Verification
- [ ] Check `Otps.IsExpired()` domain invariant
- [ ] Check `Otps.IsExhausted` (max 5 attempts)
- [ ] Increment `AttemptCount` before checking exhaustion
- [ ] Set `IsUsed = true` and `UsedAt` on successful verification

### When Implementing External Provider Linking
- [ ] Use filtered unique index for active links only
- [ ] Allow re-linking after deactivation (IsActive = false)
- [ ] Track `VerifiedAt` timestamp
- [ ] Store `ProviderEmail` for reference

---

## 🔧 Maintenance Tasks

### Required Background Jobs
1. **Session Cleanup** - Delete expired sessions (ExpiresAt < NOW)
2. **Token Cleanup** - Delete expired refresh tokens (ExpiresAt < NOW)
3. **OTP Cleanup** - Delete expired OTPs (ExpiresAt < NOW)
