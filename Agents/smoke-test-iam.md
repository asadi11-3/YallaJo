# YallaJo — IAM End-to-End Smoke Test Checklist

> **Scope:** manual validation of the completed Auth / Security / Accounts
> lifecycle flows (Phases 2A → 3D). Run through **Swagger UI**
> (`https://<api-host>/swagger`) or any HTTP client (curl, Postman,
> Bruno, HTTPie, REST Client).
>
> **Non-goals:** does NOT exercise UI, does NOT create or refactor code.
> If a smoke test reveals a blocker, stop and raise — do not patch in
> place.
>
> **Status when written:** `dotnet test YallaJo.sln` green — Security
> 108/108, Auth 252/252, Accounts 16/16, Web 31/31, SharedKernel 5/5,
> ContentCore 11/11.

---

## 0. Preconditions

### 0.1 Environment
- API running locally from `YallaJo.Api` (or staging).
- SQL Server reachable via `DefaultConnection`
  (`YallaJo.Api/appsettings.json`).
- Gmail SMTP section configured and reachable (or an allow-listed dev
  relay).
- `Recaptcha:BypassForTesting` set to `true` in `appsettings.Development.json`
  **for local smoke only**. If kept `false`, use a valid reCAPTCHA v3
  token in every request that carries `RecaptchaToken`.
- For any cross-module command that wraps Auth + Security + Accounts in
  one `TransactionScope` (reassignment path), **MSDTC** must be running
  on the SQL host. Pre-check with:
  - `sc query MSDTC` — State must be `RUNNING`.
  - If distributed transactions are disabled, reassignment will fail
    with a `System.Transactions.TransactionException`.

### 0.2 Seed data (already applied on startup)
From `YallaJo.SharedKernel.Infrastructure/Data/SeedIdentityProfiles.cs`
— every seeded user's password is `P@ssw0rd!`.

| Role         | Email                            | UserId                                 |
|--------------|----------------------------------|----------------------------------------|
| Owner        | `owner@yallajo.local`            | `11111111-1111-1111-1111-111111111111` |
| SuperAdmin-1 | `superadmin.1@yallajo.local`     | `22222222-2222-2222-2222-222222222222` |
| Admin-1      | `admin.1@yallajo.local`          | `33333333-3333-3333-3333-333333333333` |
| TourGuide-1  | `guide.petra@yallajo.local`      | `44444444-4444-4444-4444-444444444444` |
| User-1       | `traveler.1@yallajo.local`       | `66666666-6666-6666-6666-666666666666` |

Role hierarchy (top → bottom): **Owner > SuperAdmin > Admin > User**.
An actor can only manage strictly-lower-privileged users and cannot
manage themselves.

### 0.3 Disposable test identities
Use deterministic emails so cleanup / requerying is easy:

- `smoke.invitee@example.test` — Phase 3 provisioned/invited target.
- `smoke.reassign.new@example.test` — reassignment target email.
- Invite role id for "User": `a4444444-4444-4444-4444-444444444444`.

### 0.4 DB quick-inspect queries (reused across every step)

```sql
-- Security — user + primary email + lifecycle
SELECT u.Id, u.LifecycleState, u.IsActive, u.PasswordHash,
       e.Address, e.IsVerified, e.VerifiedAt
FROM security.Users u
LEFT JOIN security.Emails e ON e.UserId = u.Id AND e.IsPrimary = 1
WHERE u.Id = @UserId;

-- Auth — sessions + refresh tokens
SELECT Id, UserId, IsRevoked, RevokedReason, CreatedAt
FROM auth.Sessions WHERE UserId = @UserId;

SELECT Id, SessionId, UserId, IsRevoked, ExpiresAt
FROM auth.RefreshTokens WHERE UserId = @UserId;

-- Auth — activation + reset tokens
SELECT Id, UserId, State, RevokedReason, DeliveryStatus, ExpiresAt,
       DeliveryAddress
FROM auth.ActivationTokens WHERE UserId = @UserId
ORDER BY CreatedAt DESC;

SELECT Id, UserId, State, RevokedReason, ResetOrigin, DeliveryStatus,
       ExpiresAt, DeliveryAddress
FROM auth.PasswordResetTokens WHERE UserId = @UserId
ORDER BY CreatedAt DESC;

-- Auth — external providers
SELECT Id, UserId, Provider, ProviderUserId, IsActive
FROM auth.ExternalProviders WHERE UserId = @UserId;

-- Auth + Security — outbox drain
SELECT TOP 20 Id, Type, ProcessedOnUtc, Error FROM auth.OutboxMessages
  ORDER BY OccurredOnUtc DESC;
SELECT TOP 20 Id, Type, ProcessedOnUtc, Error FROM security.OutboxMessages
  ORDER BY OccurredOnUtc DESC;
SELECT TOP 20 Id, Type, ProcessedOnUtc, Error FROM accounts.OutboxMessages
  ORDER BY OccurredOnUtc DESC;

-- Accounts — profile
SELECT Id, UserId, FirstName, LastName, DisplayName, AvatarUrl,
       DateOfBirth, Gender, Country, City, AddressLine,
       UpdatedAt, IsDeleted
FROM accounts.Profiles WHERE UserId = @UserId;
```

---

## 1. Smoke test order (authoritative)

Execute in this exact order — each step depends on DB state left by
prior steps.

| # | Step | Flow under test |
|---|------|-----------------|
|  1 | Login as Admin actor | authenticate the admin bearer for all admin calls |
|  2 | Admin provision account (invite) | Phase 2B provisioning |
|  3 | Send/resend activation email | Phase 2C-1 activation token + outbox |
|  4 | Accept invite (activation) | Phase 2C-5 activation + login in one call |
|  5 | Login as new user | Phase 2A lifecycle gate + JWT issuance |
|  6 | Forgot password (self-service) | Phase 2C-2/3 reset pipeline |
|  7 | Reset password | Phase 2C-5 credential replacement + session revoke |
|  8 | Admin reset password | Phase 3A admin-initiated reset |
|  9 | Admin suspend user | Phase 3B — `Active → Suspended`, sessions revoked |
| 10 | Admin reactivate user | Phase 3B — `Suspended → Active` |
| 11 | Admin archive user | Phase 3B — terminal state, sessions revoked |
| 12 | Admin reassign account (new provisioned user) | Phase 3C — email retarget + full credential teardown |
| 13 | Confirm old owner lockout | Phase 3C invariants (password/session/token/provider) |
| 14 | Confirm new assignee activation works | Phase 3C new activation token path |
| 15 | Confirm profile scrub | Phase 3D — placeholder values + PII nulled |
| 16 | Confirm external provider links deactivated | Phase 3C — `ExternalProvider.IsActive=false` |
| 17 | Confirm outbox dispatch | activation/reset emails actually leave the outbox |
| 18 | Regression: lifecycle guard negatives | admin refuses ineligible-lifecycle transitions |

---

## 2. Step-by-step checklist

### Step 1 — Login as Admin actor

**Endpoint:** `POST /api/v1/auth/login`
**Body:**
```json
{ "email": "admin.1@yallajo.local", "password": "P@ssw0rd!",
  "recaptchaToken": "test-recaptcha-token" }
```
**Expect:**
- HTTP `200 OK`.
- Response contains `accessToken` (JWT) + `refreshToken`.
- DB: new row in `auth.Sessions` (IsRevoked=false), new row in
  `auth.RefreshTokens` (IsRevoked=false).
- JWT payload: `sub=33333333-...`, role `Admin`, claim `sid`.

**Save:** `ADMIN_ACCESS_TOKEN`, `ADMIN_REFRESH_TOKEN`. Every subsequent
admin call uses `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`.

**Failure cases to verify:**
- Wrong password → `401 Unauthorized`, no session created.
- Unknown email → `401 Unauthorized`, no session created.

---

### Step 2 — Admin provision account (invite)

**Endpoint:** `POST /api/v1/auth/invitations`
**Body:**
```json
{
  "email": "smoke.invitee@example.test",
  "firstName": "Smoke",
  "lastName": "Invitee",
  "displayName": null,
  "avatarUrl": null,
  "roleIds": ["a4444444-4444-4444-4444-444444444444"]
}
```
**Expect:**
- HTTP `201 Created`. Body has `userId`, `profileId`, confirmation
  message.
- `security.Users` row created: `LifecycleState=Provisioned`,
  `IsActive=0`, `PasswordHash` set to a placeholder.
- `security.Emails`: primary email matches input, `IsVerified=0`.
- `accounts.Profiles`: row created with `FirstName=Smoke`,
  `LastName=Invitee`, `UserId` matches.
- `auth.ActivationTokens`: one new row, `State=Issued`,
  `DeliveryAddress=smoke.invitee@example.test`.
- `auth.OutboxMessages`: a new `ActivationTokenIssuedIntegrationEvent`
  row (check `ProcessedOnUtc`).

**Save:** `INVITEE_USER_ID`.

**Failure cases:**
- Duplicate email → `409 Conflict`.
- Missing `roleIds` → `400` validation.
- Token from non-admin → `403 Forbidden`.

---

### Step 3 — Resend activation email

**Endpoint:** `POST /api/v1/auth/invitations/resend`
**Body:** `{ "email": "smoke.invitee@example.test" }`

**Expect:**
- HTTP `200 OK`.
- `auth.ActivationTokens` for `INVITEE_USER_ID` — prior row now has
  `State=Revoked`, `RevokedReason=Superseded`; a new row exists with
  `State=Issued`.
- `auth.OutboxMessages` has an additional
  `ActivationTokenIssuedIntegrationEvent` row.
- Within a few seconds, `ProcessedOnUtc` on the outbox row moves to
  non-null (the `OutboxProcessor` background job has drained it) and
  the inbox on the email-dispatch side marks the message processed.

**Grab the plain token:** in dev, sniff it from:
- the outbox payload `Content` column of the newest
  `ActivationTokenIssuedIntegrationEvent` row (JSON field
  `PlainToken`), **or**
- the actual email delivered by the Gmail SMTP relay.

**Save:** `INVITEE_ACTIVATION_TOKEN`.

---

### Step 4 — Accept invite (activation)

**Endpoint:** `POST /api/v1/auth/invitations/accept`
**Body:**
```json
{
  "email": "smoke.invitee@example.test",
  "token": "<INVITEE_ACTIVATION_TOKEN>",
  "password": "SmokeP@ss1",
  "confirmPassword": "SmokeP@ss1"
}
```
**Expect:**
- HTTP `200 OK`. Body confirms "account is now active".
- `security.Users` for `INVITEE_USER_ID`: `LifecycleState=Active`,
  `IsActive=1`, `PasswordHash` updated (no longer placeholder).
- `security.Emails` primary: `IsVerified=1`, `VerifiedAt` non-null.
- `auth.ActivationTokens` newest row: `State=Consumed` (or equivalent
  used state), `ConsumedAt` non-null.
- Any stale activation rows: `State=Revoked`,
  `RevokedReason=Superseded`.
- `security.OutboxMessages`: `EmailVerifiedIntegrationEvent` present.

**Failure cases:**
- Wrong token → `400`/`404`.
- Token after successful accept → `409 Conflict` (already consumed).
- Token after 7 days (invite lifetime) → `400`/`409` (expired).

---

### Step 5 — Login as new user

**Endpoint:** `POST /api/v1/auth/login`
**Body:**
```json
{ "email": "smoke.invitee@example.test", "password": "SmokeP@ss1",
  "recaptchaToken": "test-recaptcha-token" }
```
**Expect:**
- HTTP `200 OK`. `accessToken` + `refreshToken` returned.
- `auth.Sessions` has a new row for `INVITEE_USER_ID`, `IsRevoked=0`.
- `auth.RefreshTokens` has a new row, `IsRevoked=0`.

**Save:** `INVITEE_ACCESS_TOKEN`, `INVITEE_REFRESH_TOKEN`,
`INVITEE_SESSION_ID` (from JWT `sid` claim).

**Failure case (lifecycle gate):** after a later step moves the user
to a non-Active state, this same call MUST return `401 Unauthorized`.

---

### Step 6 — Forgot password (self-service)

**Endpoint:** `POST /api/v1/auth/forgot-password`
**Body:** `{ "email": "smoke.invitee@example.test",
             "recaptchaToken": "test-recaptcha-token" }`

**Expect:**
- HTTP `200 OK` (generic message — enumeration-safe).
- `auth.PasswordResetTokens` new row for `INVITEE_USER_ID`:
  `State=Issued`, `ResetOrigin=SelfService`,
  `DeliveryAddress=smoke.invitee@example.test`.
- `auth.OutboxMessages`: `PasswordResetTokenIssuedIntegrationEvent`
  row. After outbox drain: `ProcessedOnUtc` non-null.
- **Throttle check:** call again within 60 seconds. HTTP `200 OK` but
  NO new `PasswordResetTokens` row and NO new outbox row.

**Grab the plain OTP** from the outbox row's `Content` JSON field
`PlainCode` (or the delivered email).

**Save:** `INVITEE_RESET_OTP`.

**Failure cases (silent):**
- Unknown email → `200 OK` generic (no DB changes, enumeration-safe).
- Non-Active account → `200 OK` generic (no DB changes).

---

### Step 7 — Reset password

**Endpoint:** `POST /api/v1/auth/reset-password`
**Body:**
```json
{
  "email": "smoke.invitee@example.test",
  "otpCode": "<INVITEE_RESET_OTP>",
  "newPassword": "SmokeP@ss2",
  "confirmNewPassword": "SmokeP@ss2"
}
```
**Expect:**
- HTTP `200 OK`.
- `security.Users.PasswordHash` changed from Step 4 value.
- `auth.PasswordResetTokens` newest row: `State=Consumed`.
- `auth.Sessions` / `auth.RefreshTokens` for this user: every
  previously-active row is now `IsRevoked=1` with reason
  `PasswordReplacedBySelf`.
- `INVITEE_ACCESS_TOKEN` from Step 5 — any authenticated call that
  validates the session (e.g. `GET /api/v1/auth/sessions`) must
  fail with `401 Unauthorized`.
- Re-login with new password succeeds.

**Failure cases:**
- Wrong OTP → `400 Bad Request`, attempt counter incremented on token.
- Exhausted attempts → `429 Too Many Requests`.
- Expired token → `400 Bad Request`.

Refresh the `INVITEE_ACCESS_TOKEN` / refresh token by logging in again
before continuing.

---

### Step 8 — Admin reset password

**Endpoint:**
`POST /api/v1/auth/admin/users/{INVITEE_USER_ID}/reset-password`
**Auth:** `Bearer <ADMIN_ACCESS_TOKEN>`
**Body:** `{ "reason": "smoke test admin reset" }`

**Expect:**
- HTTP `200 OK`. Response message admin-safe (no plain OTP).
- `security.Users.LifecycleState=PendingPasswordReset`,
  `IsActive=0`.
- `auth.PasswordResetTokens` new row:
  `ResetOrigin=AdminInitiated`, `State=Issued`.
- Any prior active reset token: `State=Revoked`,
  `RevokedReason=Superseded`.
- Sessions + refresh tokens revoked with reason `PasswordResetByAdmin`.
- Outbox row with `PasswordResetTokenIssuedIntegrationEvent`,
  `Origin=AdminInitiated`.
- Login as the target user now fails with `401 Unauthorized`
  (lifecycle gate blocks).

**Recovery to continue smoke:** have the user complete
`/reset-password` with the new OTP, which flips them back to `Active`.

**Failure cases:**
- Admin tries same-or-higher role target → `403`.
- Admin tries themselves → `403`.
- Target in `Provisioned` / `PendingActivation` / `Archived` → `409`.

---

### Step 9 — Admin suspend user

**Endpoint:**
`PATCH /api/v1/auth/admin/users/{INVITEE_USER_ID}/suspend`
**Auth:** admin bearer.

**Expect:**
- HTTP `200 OK`.
- `security.Users.LifecycleState=Suspended`, `IsActive=0`.
- All active sessions + refresh tokens revoked with reason
  `AccountSuspended`.
- Login for that user → `401 Unauthorized`.

**Failure cases:**
- User in `Provisioned`/`PendingActivation`/`Archived` → `409`.
- Idempotent re-suspend on already-Suspended → `200 OK`, no event
  emitted (no new `AccountLifecycleTransitionedEvent`).

---

### Step 10 — Admin reactivate user

**Endpoint:**
`PATCH /api/v1/auth/admin/users/{INVITEE_USER_ID}/reactivate`
**Auth:** admin bearer.

**Expect:**
- HTTP `200 OK`.
- `security.Users.LifecycleState=Active`, `IsActive=1`.
- Sessions/tokens remain revoked from Step 9 (reactivation does not
  re-create them — the user must re-login).
- Login succeeds again.

**Failure cases:**
- User not in `Suspended` → `409`.
- Archived user → `409` (terminal state).

---

### Step 11 — Admin archive user

⚠ Archive is **terminal**. After this step `INVITEE_USER_ID` cannot be
reused for suspend/reactivate/reassign. Use a fresh provisioned user
for Step 12.

**Pre-req:** provision a second user (repeat Step 2 +
Steps 3–5) with email `smoke.archive@example.test`. Save as
`ARCHIVE_USER_ID`.

Then archive the original `INVITEE_USER_ID`:

**Endpoint:**
`PATCH /api/v1/auth/admin/users/{INVITEE_USER_ID}/archive`
**Auth:** admin bearer.

**Expect:**
- HTTP `200 OK`.
- `security.Users.LifecycleState=Archived`, `IsActive=0`.
- Sessions + refresh tokens revoked with reason `AccountArchived`.
- Further admin lifecycle verbs on this user → `409` (terminal).
- Login → `401`.

---

### Step 12 — Admin reassign account

⚠ Reassignment requires a target **not** in
`Provisioned|PendingActivation|Archived`. Use a fresh **activated**
user. Create one via Steps 2 + 3 + 4 + 5 with email
`smoke.reassign.old@example.test` and save as `REASSIGN_USER_ID`.
Make sure to log in as that user (Step 5) so there's an active
session + refresh token to verify teardown on.

Optionally also link a provider row directly in DB to prove external
provider deactivation (Step 16):

```sql
INSERT INTO auth.ExternalProviders
  (Id, UserId, Provider, ProviderUserId, ProviderEmail, IsActive,
   CreatedAt, IsDeleted)
VALUES
  (NEWID(), '<REASSIGN_USER_ID>', 'google', 'g-smoke-reassign-1',
   'smoke.reassign.old@example.test', 1, SYSUTCDATETIME(), 0);
```

**Endpoint:**
`POST /api/v1/auth/admin/users/{REASSIGN_USER_ID}/reassign`
**Auth:** admin bearer.
**Body:**
```json
{ "newEmail": "smoke.reassign.new@example.test",
  "reason": "smoke test reassign" }
```

**Expect:**
- HTTP `200 OK`. Body message confirms
  "Activation email queued for delivery."
- **Security:**
  - `security.Users.LifecycleState=PendingActivation`, `IsActive=0`.
  - `security.Users.PasswordHash` starts with `REASSIGNED:`.
  - `security.Emails` primary: `Address=smoke.reassign.new@example.test`
    (normalized lowercase), `IsVerified=0`, `VerifiedAt=NULL`.
  - `AccountLifecycleTransitionedEvent` from `Active` →
    `PendingActivation` present (visible in `security.OutboxMessages`
    if audit subscriber is wired).
- **Auth:**
  - All `Sessions` for `REASSIGN_USER_ID`: `IsRevoked=1`,
    `RevokedReason=AccountReassigned`.
  - All `RefreshTokens` for `REASSIGN_USER_ID`: `IsRevoked=1`.
  - Every prior `ActivationTokens` row: `State=Revoked`,
    `RevokedReason=Superseded`.
  - A NEW `ActivationTokens` row: `State=Issued`,
    `DeliveryAddress=smoke.reassign.new@example.test`.
  - Every prior `PasswordResetTokens` row: `State=Revoked`,
    `RevokedReason=Superseded`.
  - Every `ExternalProviders` row for the user: `IsActive=0`.
  - `OutboxMessages` has a new
    `ActivationTokenIssuedIntegrationEvent` for the new email.
- **Accounts (Phase 3D):** see Step 15.

**Failure cases:**
- Target in `Provisioned`/`PendingActivation`/`Archived` → `409`.
- Target = actor → `403`.
- Actor not strictly above target role → `403`.
- `newEmail` already in use → `409`.
- Unauthenticated actor → `401`.

---

### Step 13 — Confirm old owner lockout

Each of the following must FAIL; together they prove the old owner
has no path back in.

1. **Password login with OLD email:**
   `POST /login` body `{email:"smoke.reassign.old@example.test",
   password:"SmokeP@ss1", ...}` → `401`.
2. **Password login with OLD email + a new guess:** → `401`.
3. **Password login with NEW email + OLD password:** → `401`
   (lifecycle gate blocks — user is `PendingActivation`).
4. **Refresh with OLD refresh token:**
   `POST /api/v1/auth/refresh` body `{refreshToken:"<OLD>"}` → `401`
   (token revoked).
5. **Authenticated call with OLD access token:**
   `GET /api/v1/auth/sessions` with old bearer → `401`
   (session revoked; if access token is not yet expired the sid-bound
   session is gone).
6. **External-provider login** using the previously-linked Google
   identity → `401`/`Unauthorized` (link deactivated).
7. **Replay OLD activation / reset tokens** via `/invitations/accept`
   or `/reset-password` → `400`/`404` (superseded).

---

### Step 14 — Confirm new assignee activation works

Treat the new owner exactly like a fresh invitee.

1. Grab the plain activation token for the new row (outbox
   `Content.PlainToken` or delivered email).
2. `POST /api/v1/auth/invitations/accept` with:
   ```json
   { "email": "smoke.reassign.new@example.test",
     "token": "<NEW_ACTIVATION_TOKEN>",
     "password": "NewOwnerP@ss1",
     "confirmPassword": "NewOwnerP@ss1" }
   ```

**Expect:**
- HTTP `200 OK`.
- `security.Users.LifecycleState=Active`, `IsActive=1`.
- `security.Emails` primary: `IsVerified=1`, `VerifiedAt` non-null.
- `security.Users.PasswordHash` no longer starts with `REASSIGNED:`.
- `auth.ActivationTokens` newest row: `Consumed`.

Then:
- `POST /login` with NEW email + `NewOwnerP@ss1` → `200 OK`.
- New access/refresh tokens issued; fresh `Sessions` /
  `RefreshTokens` rows appear.

---

### Step 15 — Confirm profile scrub (Phase 3D)

**After Step 12** (before Step 14), inspect:

```sql
SELECT FirstName, LastName, DisplayName, AvatarUrl, DateOfBirth,
       Gender, Country, City, AddressLine, UpdatedAt, IsDeleted
FROM accounts.Profiles WHERE UserId = '<REASSIGN_USER_ID>';
```

**Expect:**
- `FirstName = 'Pending'`.
- `LastName = 'Activation'`.
- `DisplayName = 'smoke.reassign.new'` (local part of new email).
- `AvatarUrl = NULL`.
- `DateOfBirth = NULL`, `Gender = NULL`.
- `Country = NULL`, `City = NULL`, `AddressLine = NULL`.
- `IsDeleted = 0` (row preserved).
- `UpdatedAt` > `CreatedAt`.

**Regression guard:** `accounts.Profiles.Id` and `UserId` are
unchanged from before reassignment (same row, scrubbed in place).

**After Step 14**, profile is free to be updated by the new owner
via `PUT /api/v1/accounts/profile` (normal UpdateProfile flow). That
flow must still succeed — proves Phase 3D did not break existing
profile mutators.

---

### Step 16 — Confirm external provider deactivation

From the link inserted before Step 12:

```sql
SELECT Id, UserId, Provider, ProviderUserId, IsActive, UpdatedAt
FROM auth.ExternalProviders WHERE UserId = '<REASSIGN_USER_ID>';
```

**Expect:**
- Every row has `IsActive = 0`.
- `UpdatedAt` changed (domain `Deactivate()` calls `MarkUpdated()`).
- Filtered unique index on `(Provider, ProviderUserId)` still allows
  the same external identity to link to a fresh user later (since the
  filter is `[IsActive] = 1`).

**Live test:** attempt `POST /api/v1/auth/external-login` with the
ticket tied to `g-smoke-reassign-1` — must return `401 Unauthorized`.

---

### Step 17 — Confirm outbox email dispatch

For each email-producing step above (2, 3, 6, 8, 12):

1. Inspect the originating outbox table
   (`auth.OutboxMessages` for activation + reset,
   `security.OutboxMessages` for lifecycle/credential audit,
   `accounts.OutboxMessages` for profile events).
2. Within ~10 s the `OutboxProcessor` background service should flip
   `ProcessedOnUtc` to a non-null value and `Error` to `NULL`.
3. The matching inbox table in the consuming module marks the message
   as processed (prevents duplicate send).
4. The Gmail relay (or dev SMTP capture) delivers one email per
   outbox row.

**Failure signals:**
- `ProcessedOnUtc` NULL after 30 s + non-null `Error` → outbox stuck;
  check logs for SMTP exceptions.
- Duplicate delivery → inbox not being flagged.
- Activation token row shows `DeliveryStatus=Failed`,
  `State=Revoked`, `RevokedReason=EmailFailed` → SMTP refused; token
  auto-revoked as designed.

---

### Step 18 — Regression: lifecycle guard negatives

Verify the admin command contract rejects illegal combinations.
Expect `409 Conflict` for each of the following (target id = a user
currently in the listed lifecycle state):

| Target state         | Suspend | Reactivate | Archive | Reassign | AdminResetPw |
|----------------------|:-------:|:----------:|:-------:|:--------:|:------------:|
| Provisioned          | 409     | 409        | 200     | 409      | 409          |
| PendingActivation    | 409     | 409        | 200     | 409      | 409          |
| Active               | 200     | 409        | 200     | 200      | 200          |
| Suspended            | 200*    | 200        | 200     | 200      | 409          |
| PendingPasswordReset | 200     | 409        | 200     | 200      | 409          |
| Archived             | 409     | 409        | 409     | 409      | 409          |

`200*` = idempotent (no transition event raised).

Authz negatives that must return `403 Forbidden` regardless of state:
- Actor targeting themselves.
- Actor targeting a peer or higher role.
- Actor without `User.UpdateAny` permission.

Authentication negative that must return `401 Unauthorized`:
- Missing or expired bearer.

---

## 3. Expected lifecycle state matrix (quick reference)

| Step                          | Target lifecycle before | Target lifecycle after |
|-------------------------------|-------------------------|------------------------|
| 2 Invite                      | –                       | `Provisioned` (→ `PendingActivation` once activation email sent) |
| 4 Accept invite               | `PendingActivation`     | `Active`               |
| 7 Self reset password         | `Active`                | `Active` (PasswordHash rotated) |
| 8 Admin reset password        | `Active`                | `PendingPasswordReset` |
| 9 Admin suspend               | `Active`                | `Suspended`            |
| 10 Admin reactivate           | `Suspended`             | `Active`               |
| 11 Admin archive              | any non-Archived        | `Archived` (terminal)  |
| 12 Admin reassign             | `Active` / `Suspended` / `PendingPasswordReset` | `PendingActivation` |
| 14 Accept new activation      | `PendingActivation`     | `Active`               |

## 4. Expected token / session behavior

| Step | Sessions / refresh tokens for target | Activation tokens | Password reset tokens | External providers |
|------|--------------------------------------|-------------------|-----------------------|--------------------|
|  2   | – (no session yet)                   | 1 × Issued         | –                     | –                  |
|  3   | no change                            | prior × Superseded + 1 × Issued | –         | no change          |
|  4   | –                                    | newest × Consumed | –                     | no change          |
|  5   | +1 Session, +1 RefreshToken          | no change         | –                     | no change          |
|  6   | no change                            | no change         | 1 × Issued            | no change          |
|  7   | all revoked (`PasswordReplacedBySelf`) | no change       | newest × Consumed     | no change          |
|  8   | all revoked (`PasswordResetByAdmin`) | no change         | prior × Superseded + 1 × Issued (`AdminInitiated`) | no change |
|  9   | all revoked (`AccountSuspended`)     | no change         | no change             | no change          |
| 10   | no change (require re-login)         | no change         | no change             | no change          |
| 11   | all revoked (`AccountArchived`)      | no change         | no change             | no change          |
| 12   | all revoked (`AccountReassigned`)    | prior × Superseded + 1 × Issued for new email | prior × Superseded | all → IsActive=0 |
| 14   | +1 Session after login               | newest × Consumed | no change             | no change          |

## 5. Failure cases to verify (consolidated)

Already inlined per step. Must-hit cross-cutting cases:

- **401** from any admin verb without a bearer.
- **403** from every admin verb when actor ≤ target in role hierarchy.
- **403** from every admin verb when actor == target.
- **409** from suspend/reactivate/archive/reassign/admin-reset when
  lifecycle is ineligible.
- **400** validation when request body missing required fields
  (e.g. reassign without `newEmail`).
- **429** from `reset-password` after exhausting OTP attempts.
- Enumeration-safe `200 OK` (no DB change) from `forgot-password`
  for non-existent email / non-Active / throttled retry.
- Idempotency: repeated outbox dispatch does NOT send duplicate
  emails (inbox flag holds).
- Reuse detection: re-presenting an already-rotated refresh token
  revokes the entire session and returns `401`.

## 6. Final pass/fail checklist

Tick each item only after DB + HTTP verification.

- [ ] Step  1 — Admin login returns JWT + refresh, sessions row exists.
- [ ] Step  2 — Invitee provisioned, profile created, activation token issued.
- [ ] Step  3 — Resend supersedes prior activation token, new outbox row.
- [ ] Step  4 — Accept-invite activates user, consumes token, verifies email.
- [ ] Step  5 — Invitee login returns JWT + refresh.
- [ ] Step  6 — Forgot-password issues SelfService reset token, throttles 2nd call.
- [ ] Step  7 — Reset-password rotates hash and revokes all sessions (`PasswordReplacedBySelf`).
- [ ] Step  8 — Admin reset moves user to `PendingPasswordReset`, revokes sessions (`PasswordResetByAdmin`), issues AdminInitiated reset token.
- [ ] Step  9 — Suspend moves user to `Suspended`, revokes sessions (`AccountSuspended`).
- [ ] Step 10 — Reactivate moves user back to `Active`; re-login required.
- [ ] Step 11 — Archive moves user to `Archived` terminal, revokes sessions (`AccountArchived`).
- [ ] Step 12 — Reassign retargets email, invalidates password (`REASSIGNED:` prefix), moves to `PendingActivation`, revokes sessions (`AccountReassigned`), supersedes tokens, deactivates provider links, issues new activation for new email.
- [ ] Step 13 — All 7 old-owner attack paths return `401`/`404`/`400`.
- [ ] Step 14 — Accept-invite with new activation token flips user to `Active` on the new email; login with new credentials works.
- [ ] Step 15 — Accounts profile shows `Pending`/`Activation`, `DisplayName=smoke.reassign.new`, all optional PII NULL, `IsDeleted=0`, `UpdatedAt` bumped.
- [ ] Step 16 — Every `auth.ExternalProviders` row for target has `IsActive=0`; external-login with old link returns `401`.
- [ ] Step 17 — Every outbox row across the smoke run reaches `ProcessedOnUtc` with `Error=NULL`; emails delivered.
- [ ] Step 18 — Lifecycle/authz negative matrix matches §2.18 table exactly.

If every box is ticked, the Phase 2–3D IAM backend is validated
end-to-end. Any red boxes are smoke-test blockers and must be triaged
before declaring the flow production-ready.
