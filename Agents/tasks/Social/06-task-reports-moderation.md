# TASK 4 — Reports + Content Moderation

> **Owner:** Mohammad (Intermediate) — **Hours:** 28h — **Hard deadline:** Sun **2026-11-22 17:00**
> **Earliest start:** Wed 2026-10-21 (parallel with T1, T2)
> **Endpoints:** 6 — **Depends on:** PW-1..PW-7, T1 (Review.AutoHide method must exist for the 5-report threshold)

---

## 1. Endpoint List

| # | Method | Path | Permission | Notes |
|---|---|---|---|---|
| 1 | POST | `/api/v1/reports` | `Report.Create` | Body: `{entityType, entityId, reason, description}`. Returns 201 + Report DTO. Triggers auto-action chain if threshold hit. |
| 2 | POST | `/api/v1/reviews/{id}/report` | `Report.Create` | Convenience alias — populates entityType=Review, entityId={id}. |
| 3 | POST | `/api/v1/reports/admin/{id}/resolve` | `AdminModerationQueue.Resolve` | Body: `{action, notes?}` — action ∈ {Dismiss, RemoveContent, WarnUser, BanUser}. |
| 4 | POST | `/api/v1/reviews/admin/{id}/approve` | `AdminModerationQueue.Approve` | Restore an AutoHidden review. |
| 5 | POST | `/api/v1/reviews/admin/{id}/remove` | `AdminModerationQueue.Remove` | Permanently remove a flagged review. |
| 6 | GET | `/api/v1/reports/admin` | `Report.Read` (admin filter) | Cursor pagination, filters by reason, status, entityType, dateRange. |
| 7 | GET | `/api/v1/moderation/logs` | `ContentModerationLog.Read` | Cursor pagination, filters by adminUserId, action, dateRange. |

(Endpoint count = 7 not 6 — fix in INDEX manifest before sprint kickoff.)

---

## 2. Report Aggregate

```csharp
public sealed class Report : AuditableEntity, IAggregateRoot
{
    public Guid ReporterUserId { get; private set; }
    public ReportableEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public ReportReason Reason { get; private set; }
    public string Description { get; private set; }     // 20-500 chars
    public ReportStatus Status { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public ModerationAction? ResolutionAction { get; private set; }
    public string? ResolutionNotes { get; private set; }

    public static Result<Report> Submit(...)  // raises ReportSubmittedDomainEvent
    public Result Resolve(Guid adminUserId, ModerationAction action, string? notes, DateTime now)
    {
        if (Status != ReportStatus.Open && Status != ReportStatus.UnderReview)
            return Result.Failure(new Error("Report.AlreadyResolved", ...), Outcome.Conflict);
        Status = action == ModerationAction.Dismiss ? ReportStatus.Dismissed : ReportStatus.Resolved;
        ResolvedByUserId = adminUserId;
        ResolvedAt = now;
        ResolutionAction = action;
        ResolutionNotes = notes;
        MarkUpdated(now, adminUserId);
        RaiseDomainEvent(new ReportResolvedDomainEvent(Id, adminUserId, action, now));
        return Result.Success();
    }
}
```

---

## 3. ContentModerationLog (BaseEntity, append-only)

```csharp
public sealed class ContentModerationLog : BaseEntity
{
    public Guid AdminUserId { get; private set; }
    public ReportableEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public ModerationAction Action { get; private set; }
    public string? Notes { get; private set; }
    public DateTime ActionedAt { get; private set; }
    public Guid? SourceReportId { get; private set; }   // null for direct admin actions (approve/remove)

    public static ContentModerationLog Record(...)
}
```

Every moderation action (resolve report, approve auto-hidden review, remove review) writes a log row in the SAME SaveChanges as the action itself.

---

## 4. 5-Report Auto-Hide Chain (S-R5)

`SubmitReportCommandHandler` flow:
1. Validate (entity exists snapshot lookup, user hasn't already reported, reason valid, description 20-500).
2. `Report.Submit(...)` → returns Report aggregate.
3. SaveChanges (commits Report row + raises ReportSubmittedDomainEvent + outbox row).
4. Re-query `IReportRepository.CountActiveReportsAsync(EntityType, EntityId, ct)`.
5. If `entityType == Review` AND count >= 5:
   - Load Review aggregate via `IReviewRepository`.
   - Call `review.AutoHide(count)` (idempotent — checks Status != AutoHidden).
   - SaveChanges (commits Review.Status = AutoHidden + raises ReviewAutoHiddenDomainEvent + outbox row).
   - Log via ContentModerationLog with `Action = RemoveContent` (auto-actioned, AdminUserId = `Guid.Empty` or system user ID, source report ID null because triggered by aggregate threshold, NOT a specific report resolution).

Step 4 needs to happen AFTER step 3's SaveChanges so the new report is included in the count. Acceptable double-trip cost.

**Auto-action threshold for non-Review entities** (Tour, Place, Business, Blog) — PDF 1 Wave 6 says "3+ reports auto-hide+escalate moderation". This sprint only auto-hides Reviews; auto-hide of Tours/Places/Businesses requires those modules to expose a `MarkAutoHidden` integration event, which is deferred. We just write the ContentModerationLog row + raise `EntityAutoActionedDomainEvent` for visibility, but no actual hide happens.

---

## 5. Admin Endpoints

`ResolveReportCommandHandler` flow:
1. Load Report aggregate.
2. `report.Resolve(adminUserId, action, notes, now)`.
3. Apply secondary effect based on action:
   - `Dismiss` → no further action.
   - `RemoveContent` → load target entity (Review only this sprint), call `review.AdminDelete(adminUserId)`.
   - `WarnUser` → publish `social.user.warned.v1` integration event (Messaging consumes → email).
   - `BanUser` → publish `social.user.banned.v1` (deferred — for now log warning).
4. Write ContentModerationLog row.
5. SaveChanges (commits all of: Report.Resolve, optional Review delete, log row, both outbox events atomically).

---

## 6. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Report aggregate + ReportStatus + ModerationAction enums + factories | 3 | 2026-10-26 |
| 2 | ContentModerationLog entity + EF configs + migration `SocialAddReportIndexesAndStatus` + `SocialAddContentModerationLog` | 3 | 2026-10-28 |
| 3 | SubmitReportCommand + Validator + 6 tests | 4 | 2026-11-02 |
| 4 | 5-report auto-hide chain integration + 4 tests | 3 | 2026-11-05 |
| 5 | ResolveReportCommand + Validator + 4 tests | 3 | 2026-11-08 |
| 6 | ApproveAutoHiddenReviewCommand + RemoveReviewCommand + Handlers + 4 tests | 3 | 2026-11-12 |
| 7 | GetAdminReportsQuery + Handler + ICacheableQuery + tests | 3 | 2026-11-15 |
| 8 | GetModerationLogsQuery + Handler + ICacheableQuery + tests | 2 | 2026-11-17 |
| 9 | 7 endpoint wiring + Swagger XML docs | 2 | 2026-11-19 |
| 10 | Integration test: full report → auto-hide → admin resolve chain | 1 | 2026-11-20 |
| 11 | PR review fixes | 1 | 2026-11-22 |
| **Total** | | **28h** | **Sun 2026-11-22** |

---

## 7. Acceptance Tests (10 cases)

1. POST report on Review → 201, outbox `social.report.submitted.v1` not emitted (it's a domain-only event, not integration this sprint — verify in PW-4).
2. POST 5th report on same review → review.Status = AutoHidden, ReviewAutoHiddenDomainEvent raised, ContentModerationLog row written.
3. POST report by same user twice → 409 `Report.AlreadyReported`.
4. POST report with reason = Spam, description 5 chars → 400 `Report.DescriptionTooLong` (named badly — actually too short).
5. POST report with invalid entityType → 400 `Report.TargetNotReportable`.
6. Admin Resolve report with Dismiss → Report.Status = Dismissed, no content removed, log row written.
7. Admin Resolve with RemoveContent → Review soft-deleted, outbox `social.review.deleted.v1` row.
8. Admin Resolve already-resolved report → 409 `Report.AlreadyResolved`.
9. Admin Approve auto-hidden review → Status = Published, ReviewRestoredDomainEvent raised.
10. Admin Approve never-hidden review → 422 `Moderation.ReviewNotAutoHidden`.
