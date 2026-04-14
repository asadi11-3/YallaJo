# Auth Module - Quick Reference Guide

## Token Management

### Access Token (JWT)
- **Algorithm:** HMAC-SHA256
- **Lifetime:** 15 minutes
- **Claims:** sub, email, jti, iat, sid, role, custom
- **Validation:** Signature, expiration, issuer, audience

### Refresh Token
- **Generation:** 64 random bytes (512 bits)
- **Storage:** SHA256 hash only
- **Lifetime:** 30 days
- **Rotation:** Old token revoked, new token issued
- **Reuse Attack Detection:** If revoked token presented, entire session terminated

## Authentication Flows

### Login
```
POST /api/v1/auth/login
{ email, password }
→ { userId, accessToken, refreshToken, refreshTokenExpiresAt }
```

### Verify Email
```
POST /api/v1/auth/verify-email
{ email, otpCode }
→ { userId, accessToken, refreshToken, refreshTokenExpiresAt }
```

### Refresh Token
```
POST /api/v1/auth/refresh
{ refreshToken }
→ { accessToken, refreshToken, refreshTokenExpiresAt }
```

### Forgot Password
```
POST /api/v1/auth/forgot-password
{ email }
→ { message }
```

### Reset Password
```
POST /api/v1/auth/reset-password
{ email, otpCode, newPassword, confirmNewPassword }
→ { success }
```

### Resend OTP
```
POST /api/v1/auth/resend-otp
{ email, purpose }
→ { message }
```

## Session Management

### Logout
```
POST /api/v1/auth/logout
{ refreshToken }
→ 200 OK
```

### Logout All
```
POST /api/v1/auth/logout-all
→ 200 OK
```

### List Sessions
```
GET /api/v1/auth/sessions
→ [{ sessionId, deviceId, deviceName, userAgent, ipAddress, createdAt, expiresAt, isCurrent }]
Cached: 5 minutes per user
```

### Revoke Session
```
DELETE /api/v1/auth/sessions/{sessionId}
→ 200 OK
```

## Device Management

### Trust Device
```
PATCH /api/v1/auth/devices/{deviceId}/trust
→ 200 OK
```

## External Providers

### Link Provider
```
POST /api/v1/auth/external-providers
{ provider, providerUserId, providerEmail }
→ providerId
```

### Unlink Provider
```
DELETE /api/v1/auth/external-providers/{providerId}
→ 200 OK
```

## Rate Limiting

| Policy | Limit | Partition | Window |
|--------|-------|-----------|--------|
| LoginPolicy | 10/min | IP | Fixed |
| OtpPolicy | 3/min | Email | Sliding |
| RefreshPolicy | 20/min | IP | Fixed |
| RegisterPolicy | 5/min | IP | Fixed |

## Database Schema

### Sessions
- UserId, DeviceId, ExpiresAt, IsRevoked, RevokedAt, IpAddress
- Indexes: UserId, DeviceId, IsRevoked, ExpiresAt

### RefreshTokens
- UserId, SessionId, TokenHash (unique), ExpiresAt, IsRevoked, ReplacedByTokenId
- Indexes: TokenHash (unique), SessionId, UserId, IsRevoked, ExpiresAt

### Devices
- UserId, DeviceToken (unique), UserAgent, DeviceName, IsTrusted, TrustedAt, LastSeenAt
- Indexes: UserId, DeviceToken (unique), IsTrusted

### Otps
- UserId, Purpose, CodeHash, DeliveryChannel, DeliveryAddress, ExpiresAt, AttemptCount, IsUsed
- Indexes: (UserId, Purpose, IsUsed) with filter [IsUsed] = 0, ExpiresAt

### ExternalProviders
- UserId, Provider, ProviderUserId, ProviderEmail, IsActive, VerifiedAt
- Indexes: UserId, (Provider, ProviderUserId) unique with filter [IsActive] = 1, IsActive

## Key Security Features

✅ **Token Security**
- Refresh tokens hashed before storage
- Token rotation with reuse-attack detection
- Session ID included in JWT

✅ **OTP Security**
- 6-digit OTPs with PBKDF2 hashing
- Max 5 verification attempts
- 10-minute expiry
- One-time use enforcement

✅ **Session Security**
- Device tracking with trust status
- IP address capture
- 30-day expiration
- Explicit revocation support

✅ **Password Security**
- OTP-based reset
- Automatic session revocation on change/reset
- Password complexity delegated to Security module

✅ **External Provider Security**
- Duplicate link prevention
- Provider account hijacking prevention
- Soft deactivation allows re-linking

✅ **Rate Limiting**
- Per-IP login limiting
- Per-email OTP limiting
- Per-IP refresh limiting

✅ **Event-Driven**
- Domain events for audit trail
- Integration events for cross-module communication
- Outbox pattern for reliability
- Inbox pattern for idempotency

## Commands & Handlers

| Command | Handler | Dependencies |
|---------|---------|--------------|
| LoginCommand | LoginCommandHandler | ISecurityService, IDeviceRepository, ISessionRepository, IRefreshTokenRepository, ITokenService, IRequestContext |
| RefreshTokenCommand | RefreshTokenCommandHandler | IRefreshTokenRepository, ISessionRepository, IDeviceRepository, ISecurityService, ITokenService |
| LogoutCommand | LogoutCommandHandler | IRefreshTokenRepository, ISessionRepository, ITokenService, HybridCache |
| LogoutAllCommand | LogoutAllCommandHandler | ISessionRepository, IRefreshTokenRepository, ICurrentUser, HybridCache |
| VerifyEmailCommand | VerifyEmailCommandHandler | ISecurityService, IOtpRepository, IDeviceRepository, ISessionRepository, IRefreshTokenRepository, IOtpService, ITokenService, IRequestContext |
| ForgotPasswordCommand | ForgotPasswordCommandHandler | ISecurityService, IOtpRepository, IOtpService, IEmailService |
| ResetPasswordCommand | ResetPasswordCommandHandler | ISecurityService, IOtpRepository, IOtpService |
| ResendOtpCommand | ResendOtpCommandHandler | ISecurityService, IOtpRepository, IOtpService, IEmailService |
| RevokeSessionCommand | RevokeSessionCommandHandler | ISessionRepository, IRefreshTokenRepository, ICurrentUser, HybridCache |
| TrustDeviceCommand | TrustDeviceCommandHandler | IDeviceRepository, ICurrentUser |
| LinkExternalProviderCommand | LinkExternalProviderCommandHandler | IExternalProviderRepository, ICurrentUser |
| UnlinkExternalProviderCommand | UnlinkExternalProviderCommandHandler | IExternalProviderRepository, ICurrentUser |
| ForceRevokeUserSessionsCommand | ForceRevokeUserSessionsCommandHandler | ISessionRepository, IRefreshTokenRepository, HybridCache |

## Queries & Handlers

| Query | Handler | Caching |
|-------|---------|---------|
| ListActiveSessionsQuery | ListActiveSessionsQueryHandler | 5 min per user, tag-based invalidation |

## Integration Events

| Event | Trigger | Handler |
|-------|---------|---------|
| UserLoggedInIntegrationEvent | Session created | Published to outbox |
| SessionRevokedIntegrationEvent | Session revoked | Published to outbox |
| PasswordResetIntegrationEvent | Password reset in Security | PasswordResetIntegrationEventHandler |
| PasswordChangedIntegrationEvent | Password changed in Security | PasswordChangedIntegrationEventHandler |
| UserCreatedIntegrationEvent | User created in Accounts | UserCreatedIntegrationEventHandler |
| EmailVerifiedIntegrationEvent | Email verified in Accounts | EmailVerifiedIntegrationEventHandler |

## Background Jobs

| Job | Schedule | Action |
|-----|----------|--------|
| AuthCleanupService | Every 24 hours | Delete expired/revoked tokens, sessions, OTPs |

## Configuration

### appsettings.json
```json
{
  "Jwt": {
    "Issuer": "...",
    "Audience": "...",
    "Key": "...",
    "AccessTokenMinutes": 15
  },
  "Gmail": {
    "SenderEmail": "...",
    "AppPassword": "..."
  }
}
```

## Dependency Injection

### AddAuthApplication()
- Registers MediatR handlers
- Registers FluentValidation validators

### AddAuthInfrastructure(configuration)
- Registers DbContext
- Registers repositories
- Registers services (JWT, OTP, Email)
- Registers event handlers
- Registers background jobs
- Registers HybridCache

## Error Codes

| Status | Meaning |
|--------|---------|
| 200 | Success |
| 400 | Validation error |
| 401 | Unauthorized (invalid credentials, expired token, etc.) |
| 403 | Forbidden (insufficient permissions) |
| 404 | Not found (user, session, device, etc.) |
| 409 | Conflict (duplicate link, provider already linked, etc.) |
| 429 | Too many requests (rate limit exceeded) |

