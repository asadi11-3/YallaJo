# TASK 6 — Join Request (group booking participants)

**Owner:** Fadwa (Beginner)
**Endpoints:** 3
**Estimated hours:** 14
**Earliest start:** Tue 2026-07-14 (after TASK 3 merges)
**Hard PR deadline:** Mon 2026-07-27 17:00
**Dependencies:** TASK 4 (TourBooking aggregate exists in Confirmed state), TASK 1 (AvailabilitySlot.Lock/Cancel for capacity adjustment).

> **Purpose:** Allows a user to request to JOIN someone else's already-Confirmed group booking (e.g., friend has booked a tour for 4 people, you want to slot into 1 of those seats but separately so you pay your own share). Booking owner approves or rejects.

---

## Endpoint list

| # | Method + Path | Permission | Returns | Errors |
|---|---|---|---|---|
| 1 | `POST /api/v1/booking/join-request` | `JoinRequest + Create` | 201 + DTO | 400, 404 (booking not found), 409 (booking not Confirmed / capacity full / tour does not allow joins / already requested) |
| 2 | `POST /api/v1/booking/join-request/{id}/approve` | `JoinRequest + Approve` (booking owner) | 200 | 403, 404, 409 (already resolved or capacity-full) |
| 3 | `POST /api/v1/booking/join-request/{id}/reject` | `JoinRequest + Reject` (booking owner) | 200 | 403, 404, 409 |

---

## Domain

`JoinRequest` aggregate fields:
- `Id` (Guid)
- `BookingId` (FK to TourBooking) — booking being joined
- `RequesterUserId` (Guid)
- `ParticipantCount` (int, default 1 — typically 1 person; allow up to 4 for "join me with my family")
- `Status` (JoinRequestStatus: Pending / Approved / Rejected / Expired)
- `Message` (nvarchar(500) NULL) — optional intro from requester
- `RespondedAt` (datetime2 NULL)
- `RejectionReason` (nvarchar(500) NULL)
- `ExpiresAt` (datetime2) — Pending requests auto-expire 48h after creation

Unique constraint: `(BookingId, RequesterUserId)` filtered `WHERE Status = 'Pending'` — user cannot have two simultaneous pending requests for the same booking.

```csharp
public static JoinRequest Create(Guid bookingId, Guid requesterUserId, int participantCount, string? message)
{
    if (participantCount < 1 || participantCount > 4) throw new DomainInvariantException("ParticipantCount must be 1..4.");

    var req = new JoinRequest
    {
        Id = Guid.CreateVersion7(),
        BookingId = bookingId,
        RequesterUserId = requesterUserId,
        ParticipantCount = participantCount,
        Message = message?.Trim(),
        Status = JoinRequestStatus.Pending,
        ExpiresAt = DateTime.UtcNow.AddHours(48)
    };
    req.RaiseDomainEvent(new JoinRequestCreatedDomainEvent(req.Id, req.BookingId, req.RequesterUserId, req.ParticipantCount));
    return req;
}

public Result Approve()
{
    if (Status != JoinRequestStatus.Pending)
        return Result.Failure(new Error("JoinRequest.AlreadyResolved", $"Request already in state {Status}."), Outcome.Conflict);
    if (DateTime.UtcNow > ExpiresAt)
        return Result.Failure(new Error("JoinRequest.Expired", "Request has expired."), Outcome.Conflict);

    Status = JoinRequestStatus.Approved;
    RespondedAt = DateTime.UtcNow;
    MarkUpdated();
    RaiseDomainEvent(new JoinRequestApprovedDomainEvent(Id, BookingId, RequesterUserId, ParticipantCount));
    return Result.Success();
}

public Result Reject(string reason)
{
    if (Status != JoinRequestStatus.Pending)
        return Result.Failure(new Error("JoinRequest.AlreadyResolved", $"Request already in state {Status}."), Outcome.Conflict);
    if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
        return Result.Failure(new Error("JoinRequest.RejectionReasonRequired", "Reason must be at least 10 chars."), Outcome.Validation);

    Status = JoinRequestStatus.Rejected;
    RespondedAt = DateTime.UtcNow;
    RejectionReason = reason.Trim();
    MarkUpdated();
    RaiseDomainEvent(new JoinRequestRejectedDomainEvent(Id, BookingId, RequesterUserId, RejectionReason));
    return Result.Success();
}
```

---

## Capacity model on approval

When a join request is approved:
1. Look up the booking + its AvailabilitySlot.
2. Verify `slot.AvailableCount >= request.ParticipantCount` (else 409 CapacityFull).
3. Call `slot.Lock(request.ParticipantCount)` (uses TASK 1's domain method).
4. The lock is **permanent for join requests** (no SlotLock row — straight to LockedCount that the integration event handler will move to BookedCount once Finance settles the new participant's payment).

> **Open question:** how does the new participant pay? Per PDF 1 Wave 5, "on approve add participant + charge separately + update count". The join request triggers an integration event `booking.join-request.approved.v1` which Finance consumes to **create a new Payment record** for the requester. Once Finance webhook fires `finance.payment.completed.v1` for THAT payment, the BookedCount is incremented. **In THIS sprint Finance is not yet shipped** — the join-request approval just stamps LockedCount and waits. Will be properly wired in Wave 5 Sprint #2 (Finance second sprint, after Discount engine).

---

## Tour-level gate: "this tour allows joins"

`BookingTourSnapshot` needs a new column: `AllowsJoinRequests: bit` (default false). Default to `true` if not present in upstream snapshot (we DON'T have a way to opt-out from ContentTours yet — defer the opt-out switch to a future ContentTours sprint).

For this sprint:
- Field defaults to `true` (every tour allows joins).
- POST /join-request silently passes the check.
- Future ContentTours sprint adds `Tour.AllowsJoinRequests` field + integration event, snapshot picks it up.

---

## Endpoint contracts

### POST /join-request

```json
{
  "bookingId": "<guid>",
  "participantCount": 1,
  "message": "Hey, can I join you on this tour? I'm a solo traveler."
}
```

Handler:
1. Load booking. 404 if missing.
2. Verify `booking.Status == Confirmed`. Else 409 `JoinRequest.BookingNotConfirmed`.
3. Verify `currentUser.UserId != booking.UserId` (cannot join your own booking). Else 409 `JoinRequest.SelfJoin`.
4. Verify uniqueness (no existing Pending request from this user for this booking). Else 409 `JoinRequest.Duplicate`.
5. Verify slot capacity (`slot.AvailableCount >= cmd.ParticipantCount`). Else 409 `JoinRequest.CapacityFull`.
6. Verify tour allows joins. Else 409 `JoinRequest.NotAllowed`.
7. Create entity via factory.
8. SaveChanges.
9. Notification handler emits in-app + email to booking owner (Messaging sprint wires this; for now just outbox row).

### POST /join-request/{id}/approve

Handler:
1. Load join request. 404.
2. Load booking via `BookingId`.
3. Authorize: `currentUser.UserId == booking.UserId` (owner only). Else 403.
4. Verify slot still has capacity.
5. Call `request.Approve()`.
6. Call `slot.Lock(request.ParticipantCount)`.
7. SaveChanges.

### POST /join-request/{id}/reject

```json
{ "reason": "Sorry, prefer to keep this private trip." }
```

Handler:
1. Load join request. 404.
2. Load booking.
3. Authorize: owner.
4. Call `request.Reject(reason)`.
5. SaveChanges.

---

## Cache

- No explicit cache for join-request endpoints in this sprint (low traffic).
- Approve invalidates `availability:tour:{tourId}` + `availability:tour:{tourId}:date:{date}`.

---

## WBS

| Step | Sub-deliverable | Hours | Finish-by |
|---|---|---|---|
| 1 | Refactor `JoinRequest.cs` aggregate + 3 domain events (PW-3 verify) | 2 | Tue 2026-07-14 EOD |
| 2 | Migration `BookingAddJoinRequestExpiresAt` + unique filtered index | 1 | Wed 2026-07-15 EOD |
| 3 | CreateJoinRequest CQRS + validator + handler + 4 unit tests | 4 | Fri 2026-07-17 EOD |
| 4 | ApproveJoinRequest + RejectJoinRequest CQRS + handlers + 5 unit tests | 4 | Mon 2026-07-20 EOD |
| 5 | Endpoint wiring + auth + DTO | 1 | Mon 2026-07-20 EOD |
| 6 | Integration test: end-to-end (create request → owner approves → slot LockedCount incremented) | 2 | Tue 2026-07-21 EOD |
| 7 | Code review | 2 | Mon 2026-07-27 EOD |
| **Sum** | | **16** | |

---

## Edge cases

| # | Case | Expected |
|---|---|---|
| 1 | Request to join AwaitingPayment booking | 409 BookingNotConfirmed |
| 2 | Request to join own booking | 409 SelfJoin |
| 3 | Two requests by same user for same booking, both Pending | second one 409 Duplicate |
| 4 | Request when slot has 0 available | 409 CapacityFull |
| 5 | Owner approves but capacity since dropped to 0 | 409 CapacityFull (re-checked at approve time) |
| 6 | Random user tries to approve | 403 |
| 7 | Approve already-Approved request | 409 AlreadyResolved |
| 8 | Reject without reason | 400 RejectionReasonRequired |
| 9 | Approve a request created 49h ago | 409 Expired |
| 10 | Tour does not allow joins (future field) | 409 NotAllowed |
| 11 | participantCount=5 | 400 (max 4) |
| 12 | participantCount=0 | 400 |

---

## Files Fadwa touches

```
Booking.Domain/Entities/JoinRequest.cs                       (refactor)
Booking.Domain/Events/JoinRequestCreatedDomainEvent.cs        (PW-3 verify)
Booking.Domain/Events/JoinRequestApprovedDomainEvent.cs       (PW-3 verify)
Booking.Domain/Events/JoinRequestRejectedDomainEvent.cs       (PW-3 verify)
Booking.Application/Commands/CreateJoinRequest/...
Booking.Application/Commands/ApproveJoinRequest/...
Booking.Application/Commands/RejectJoinRequest/...
Booking.Application/Interfaces/IJoinRequestRepository.cs       (PW-5 verify; extend with HasPendingByUserForBookingAsync)
Booking.Infrastructure/Repositories/JoinRequestRepository.cs
Booking.Infrastructure/Persistence/Configurations/JoinRequestConfiguration.cs
Booking.Infrastructure/Migrations/{timestamp}_BookingAddJoinRequestExpiresAt.cs
Booking.Presentation/Endpoints/JoinRequestEndpoints.cs        (new sub-file)
Booking.Presentation/BookingEndpoints.cs                       (wire-up)
tests/Booking.Tests.Unit/Commands/CreateJoinRequestHandlerTests.cs
tests/Booking.Tests.Unit/Commands/ApproveJoinRequestHandlerTests.cs
tests/Booking.IntegrationTests/JoinRequestRoundTripTests.cs
```
