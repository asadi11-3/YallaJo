# TASK 3 — Device Tokens (FCM/APNs registration)

> **Owner:** Fadwa (Intermediate) — **Hours:** 14h — **Hard deadline:** Sun **2026-12-20 17:00**
> **Endpoints:** 3 HTTP
> **Depends on:** PW-1..PW-5

This task registers and manages mobile device push tokens. **v1 stores tokens only — does NOT push notifications** (PushNotificationStrategy is a stub per M-R2). v3 (Phase 3+) wires actual FCM/APNs gateway calls.

---

## Endpoint List

| # | Method | Path | Permission | Returns |
|---|---|---|---|---|
| 1 | POST | `/api/v1/devices/token` | `MustHavePermission(DeviceToken, Create)` + ICurrentUser (stamps UserId) | 201 + DeviceTokenDto |
| 2 | DELETE | `/api/v1/devices/token/{id}` | `MustHavePermission(DeviceToken, Delete)` + ICurrentUser ownership | 204 |
| 3 | GET | `/api/v1/devices/tokens` | `MustHavePermission(DeviceToken, Read)` + ICurrentUser self-filter | 200 + list (10ish typical) |

---

## POST /devices/token

**Request:**
```json
{ "deviceId": "android-uuid-12345", "platform": "Android", "token": "fcm-token-..." }
```

**UPSERT logic** (per M-R9):
1. Validate `platform` ∈ enum (Android/Ios/Web).
2. Validate `token` non-empty (≤ 500 chars; loose check — opaque value).
3. Validate `deviceId` non-empty (≤ 200 chars; e.g. UUID or vendor-specific).
4. `repo.GetByUserAndDeviceAsync(userId, deviceId, ct)`:
   - If exists → `existing.UpdateToken(token)` → updates `Token` + stamps `LastSeenAt = now`.
   - Else → `DeviceToken.Register(userId, deviceId, platform, token)` factory → raises `DeviceTokenRegisteredDomainEvent`.
5. SaveChangesAsync.
6. Invalidate cache `device-tokens:user:{userId}`.

**Idempotency:** repeat POST with same deviceId returns 200 (or 201 — pick 200 for consistency) with updated DTO. **NO 409**.

---

## DELETE /devices/token/{id}

**Ownership check:** load token, verify `Token.UserId == currentUser.UserId`. 403 if mismatch.

**Action:** `token.Delete()` → raises `DeviceTokenDeletedDomainEvent` → soft-deletes (IsDeleted=true).

**Idempotency:** if token is already deleted OR doesn't exist → return 204 (idempotent — frontend can blindly call on app uninstall).

---

## GET /devices/tokens

Lists current user's active (IsDeleted=false) device tokens. Useful for "Logged-in devices" settings UI. Returns DTO `{id, deviceId, platform, lastSeenAt, registeredAt}` — **NEVER returns the actual `token` value** (PII per OAuth/PCI principle; never expose).

Pageable but typical list is small (≤10). Cursor pagination supported but rarely needed.

---

## Domain Methods (DeviceToken aggregate)

| Method | Raises |
|---|---|
| `Register(userId, deviceId, platform, token)` factory | DeviceTokenRegisteredDomainEvent |
| `UpdateToken(newToken)` | (none — no domain event for re-registration churn) |
| `Delete()` | DeviceTokenDeletedDomainEvent |
| `MarkSeen(now)` | (none — internal stamp on any read query) |

---

## Validator

`RegisterDeviceTokenCommandValidator`:
- `DeviceId` required, 1-200 chars
- `Platform` valid enum
- `Token` required, 1-500 chars (≤500 fits FCM/APNs)

---

## Cache Strategy

| Query | Tag | TTL |
|---|---|---|
| `GET /devices/tokens` | `device-tokens:user:{userId}` | 5min |
| (no `GET by id` cached — rare) | – | – |

Invalidate on POST/DELETE.

---

## WBS (14h)

| # | Step | Hours |
|---|---|---|
| 1 | DeviceToken aggregate Register/UpdateToken/Delete + tests | 3 |
| 2 | EfDeviceTokenRepository (incl. `GetByUserAndDeviceAsync`, `DeleteStaleAsync`) | 2 |
| 3 | 3 endpoints + Commands/Queries/Validators/Handlers/DTOs | 4 |
| 4 | UNIQUE filtered index migration (`MessagingAddDeviceTokenUniqueIndex`) | 1 |
| 5 | Cache wiring | 1 |
| 6 | Integration tests (UPSERT, ownership, idempotent DELETE) | 2 |
| 7 | PR fixes | 1 |
| **Total** | | **14h** |

---

## Acceptance Tests

1. POST `/devices/token` first time → 201 + DTO.
2. POST same deviceId again → 200 + DTO with updated Token + LastSeenAt bumped.
3. POST same deviceId from DIFFERENT user → creates separate row (UNIQUE is per-user-per-deviceId).
4. POST invalid platform → 400.
5. POST too-long token (501 chars) → 400.
6. GET /devices/tokens → returns only current user's tokens (filters others).
7. GET /devices/tokens never returns the `token` value in DTO.
8. DELETE /devices/token/{id} on own → 204 + IsDeleted=true.
9. DELETE on someone else's → 403.
10. DELETE on already-deleted/non-existent → 204 (idempotent).
11. UNIQUE constraint test: two rows with same (UserId, DeviceId) where IsDeleted=0 → DB rejects.
12. Soft-deleted row allows re-insert (filtered UNIQUE excludes IsDeleted=1 rows).
