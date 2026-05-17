# TASK 3 — Provider Documents (upload, list, expiry tracking foundation)

**Owner:** Fadwa (Beginner)
**Endpoints:** 4
**Estimated hours:** 20
**Earliest start:** Tue 2026-06-30 (after TASK 2 merges)
**Hard PR deadline:** Mon 2026-07-13 17:00
**Dependencies:** PW-1, PW-2, PW-7; ContentCore attachment service already exists (uses encrypted IV+HMAC storage per PDF 1 Wave 2).

---

## Endpoint list

| # | Method + Path | Permission | Returns |
|---|---|---|---|
| 1 | `POST /api/v1/booking/provider/documents` | `ProviderDocument + Create` (provider self) | 201 + `{id, type, fileName, expiresAt, status}` |
| 2 | `PUT /api/v1/booking/provider/documents/{id}` | `ProviderDocument + Update` (provider self) | 200 + same DTO |
| 3 | `GET /api/v1/booking/provider/documents` | `ProviderDocument + Read` (provider self) | 200 + list |
| 4 | `GET /api/v1/booking/provider/documents/{id}` | `ProviderDocument + Read` (provider self OR admin) | 200 + DTO with attachment URL |

> **NOTE:** Provider-document upload happens via Accounts-flow provider-onboarding originally (`POST /accounts/provider/documents` per PDF 1 Wave 2). This sprint OWNS the Booking-module copy because Booking is the one that runs `DocumentExpiryCheckService`. **The Accounts endpoint stays as a thin facade** that internally forwards to the Booking module via an in-process MediatR call (eventually replace with integration event in Wave 5 sprint #2). For now: provider uploads through EITHER endpoint; both write to the same `Booking.ProviderDocuments` table. Fadwa's job is to:
> 1. Make `Booking.ProviderDocuments` the authoritative table.
> 2. Refactor the Accounts endpoint to forward (1-line MediatR Send).
> 3. Wire DocumentExpiryCheckService (TASK 7 will wire the BG service itself; this task ensures the columns/indexes exist).

---

## Schema additions (migration `BookingAddProviderDocumentExpiryColumns`)

| Column | Type | Default | Notes |
|---|---|---|---|
| ExpiryWarningSent | bit | 0 | True after 30d-warning event raised |
| ExpiryProcessed | bit | 0 | True after expiry event raised |
| ApprovedAt | datetime2 NULL | NULL | Admin approval stamp |
| ApprovedByUserId | uniqueidentifier NULL | NULL | |
| RejectedAt | datetime2 NULL | NULL | |
| RejectionReason | nvarchar(500) NULL | NULL | |

Indexes:
- `IX_ProviderDocuments_ExpiresAt_Status` on `(ExpiresAt, Status)` filtered `WHERE Status='Approved' AND ExpiryProcessed=0` — supports BG service query.
- `UNIQUE IX_ProviderDocuments_Provider_Type` on `(ProviderId, DocumentType)` filtered `WHERE Status <> 'Rejected'` — enforces "one active doc per type per provider" (per PDF 2 §1.1 "duplicate license unique constraint per provider type rejects second").

---

## Domain methods

```csharp
public static ProviderDocument CreateForProvider(
    Guid providerId,
    DocumentType type,
    Guid attachmentId,
    string originalFileName,
    DateOnly? expiresAt)
{
    var doc = new ProviderDocument
    {
        Id = Guid.CreateVersion7(),
        ProviderId = providerId,
        DocumentType = type,
        AttachmentId = attachmentId,
        OriginalFileName = originalFileName,
        ExpiresAt = expiresAt,
        Status = DocumentStatus.Pending,
        ExpiryWarningSent = false,
        ExpiryProcessed = false
    };
    return doc;
}

public Result Approve(Guid adminUserId)
{
    if (Status == DocumentStatus.Approved) return Result.Failure(new Error("ProviderDocument.AlreadyApproved", "Document already approved."));
    Status = DocumentStatus.Approved;
    ApprovedAt = DateTime.UtcNow;
    ApprovedByUserId = adminUserId;
    MarkUpdated();
    return Result.Success();
}

public Result Reject(string reason)
{
    if (Status == DocumentStatus.Rejected) return Result.Failure(new Error("ProviderDocument.AlreadyRejected", "Document already rejected."));
    Status = DocumentStatus.Rejected;
    RejectedAt = DateTime.UtcNow;
    RejectionReason = reason;
    MarkUpdated();
    return Result.Success();
}

public Result MarkExpiringSoon(int daysRemaining)
{
    if (ExpiryWarningSent) return Result.Failure(new Error("ProviderDocument.WarningAlreadySent", "Expiry warning already raised."));
    ExpiryWarningSent = true;
    RaiseDomainEvent(new ProviderDocumentExpiringDomainEvent(Id, ProviderId, DocumentType, ExpiresAt!.Value, daysRemaining));
    MarkUpdated();
    return Result.Success();
}

public Result MarkExpired()
{
    if (ExpiryProcessed) return Result.Failure(new Error("ProviderDocument.AlreadyProcessed", "Expiry already processed."));
    Status = DocumentStatus.Expired;
    ExpiryProcessed = true;
    var isCritical = DocumentTypeExtensions.IsCritical(DocumentType);
    RaiseDomainEvent(new ProviderDocumentExpiredDomainEvent(Id, ProviderId, DocumentType, isCritical));
    MarkUpdated();
    return Result.Success();
}
```

`DocumentTypeExtensions.cs`:

```csharp
public static class DocumentTypeExtensions
{
    private static readonly HashSet<DocumentType> CriticalDocs = new()
    {
        DocumentType.MoTALicense,
        DocumentType.InsuranceCertificate,
        DocumentType.LiabilityInsurance,
        DocumentType.HealthSafetyCertificate,
        DocumentType.FireSafetyCertificate,
        DocumentType.TourismAuthorityLicense
    };

    public static bool IsCritical(DocumentType type) => CriticalDocs.Contains(type);

    public static int MaxFileSizeBytes(DocumentType type) => 10 * 1024 * 1024; // 10MB across the board per PDF 1 Wave 2
}
```

---

## Endpoint contracts

### POST /provider/documents

Multipart form:
- `Type`: string enum DocumentType
- `File`: IFormFile (PDF/JPEG/PNG, ≤10MB)
- `ExpiresAt`: date (optional; required for license types — validator branches on DocumentType)

Handler:
1. Validate file size (`Result.Failure(new Error("ProviderDocument.FileTooLarge", "..."), Outcome.Validation)`).
2. Validate file MIME against `{application/pdf, image/jpeg, image/png}`.
3. Call `IAttachmentService.UploadEncryptedAsync(file, entityType: "ProviderDocument", ct)` → returns `attachmentId`.
4. Create domain entity via factory.
5. Repository.Add + UoW.SaveChanges.
6. Invalidate cache tag `"provider-documents:{providerId}"`.
7. Return 201.

> If step 3 succeeds but step 5 fails, the attachment is orphaned. Orphan cleanup BG already exists in ContentCore (per agent-context.md). NOT this task's concern.

### Validators per DocumentType

Some doc types require ExpiresAt, others optional. Validator branches:

```csharp
RuleFor(x => x.ExpiresAt).NotNull().GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
    .When(x => DocumentTypeRequiresExpiry(x.Type))
    .WithMessage("Expiry date required for this document type and must be in the future.");
```

Doc types requiring expiry: MoTALicense, BusinessLicense, TaxRegistration, InsuranceCertificate, LiabilityInsurance, HealthSafetyCertificate, FireSafetyCertificate, ActivityCertification, TourismAuthorityLicense, FirstAidCertification.

---

## Cache

- `GET /provider/documents` query → `ICacheableQuery`, tag `"provider-documents:{providerId}"`, TTL 5 min (frequent admin lookups).
- `GET /provider/documents/{id}` → tag `"provider-document:{id}"`, TTL 10 min.
- POST/PUT handlers invalidate both tags.

---

## WBS

| Step | Sub-deliverable | Hours | Finish-by |
|---|---|---|---|
| 1 | Refactor `ProviderDocument.cs` + `DocumentTypeExtensions.cs` + 4 domain events (PW-3 verify) | 3 | Tue 2026-06-30 EOD |
| 2 | Migration `BookingAddProviderDocumentExpiryColumns` + indexes | 2 | Wed 2026-07-01 EOD |
| 3 | UploadProviderDocument CQRS (multipart, calls `IAttachmentService`) + 4 unit tests | 4 | Fri 2026-07-03 EOD |
| 4 | UpdateProviderDocument CQRS (replace file or expiry) + 2 unit tests | 2 | Sun 2026-07-05 EOD |
| 5 | GetProviderDocuments (list) + GetProviderDocument (single) queries + cache | 2 | Mon 2026-07-06 EOD |
| 6 | Refactor `Accounts.Presentation/POST /provider/documents` endpoint to forward via MediatR Send | 1 | Mon 2026-07-06 EOD |
| 7 | Integration test: upload PDF → DB row exists with attachment + correct status; admin GET sees it | 2 | Tue 2026-07-07 EOD |
| 8 | Integration test: upload duplicate type (same provider, Pending state) → 409 | 1 | Tue 2026-07-07 EOD |
| 9 | Hand-off doc for TASK 7: outline `DocumentExpiryCheckService` queries (uses `IProviderDocumentRepository.GetExpiringWithinAsync(30, ct)` + `GetExpiredAsync(ct)`) | 1 | Wed 2026-07-08 EOD |
| 10 | Code review cycle | 2 | Mon 2026-07-13 EOD |
| **Sum** | | **20** | |

---

## Edge cases

| # | Case | Expected |
|---|---|---|
| 1 | Upload PDF 9.5MB | OK |
| 2 | Upload PDF 11MB | 400 FileTooLarge |
| 3 | Upload .docx | 400 unsupported MIME |
| 4 | Upload MoTALicense without ExpiresAt | 400 validation |
| 5 | Upload duplicate (Approved exists same type) | 409 (unique constraint) |
| 6 | Upload duplicate after first one Rejected | OK (filtered unique allows it) |
| 7 | PUT to replace file: old attachment is orphaned (background cleanup handles); new attachment ID stamped | OK |
| 8 | PUT to extend ExpiresAt past today on already-Expired doc | 400 InvalidState (cannot edit Expired; must re-upload) |
| 9 | Admin views list — sees all providers' docs paginated | OK |
| 10 | Provider views list — sees only own | OK (handler filters by `ICurrentUser.UserId`) |

---

## Files Fadwa touches

```
Booking.Domain/Entities/ProviderDocument.cs                       (refactor)
Booking.Domain/Extensions/DocumentTypeExtensions.cs               (new)
Booking.Domain/Events/ProviderDocumentExpiringDomainEvent.cs       (PW-3 verify)
Booking.Domain/Events/ProviderDocumentExpiredDomainEvent.cs         (PW-3 verify)
Booking.Application/Commands/UploadProviderDocument/...
Booking.Application/Commands/UpdateProviderDocument/...
Booking.Application/Queries/GetProviderDocuments/...
Booking.Application/Queries/GetProviderDocument/...
Booking.Application/Interfaces/IProviderDocumentRepository.cs       (extend GetExpiringWithinAsync, GetExpiredAsync)
Booking.Infrastructure/Repositories/ProviderDocumentRepository.cs
Booking.Infrastructure/Persistence/Configurations/ProviderDocumentConfiguration.cs
Booking.Infrastructure/Migrations/{timestamp}_BookingAddProviderDocumentExpiryColumns.cs
Booking.Presentation/Endpoints/ProviderDocumentEndpoints.cs        (new sub-file)
Booking.Presentation/BookingEndpoints.cs                          (wire-up)
Accounts.Presentation/AccountsEndpoints.cs                          (refactor forward, ~5 lines)
tests/Booking.Tests.Unit/Commands/UploadProviderDocumentHandlerTests.cs
tests/Booking.IntegrationTests/ProviderDocumentRoundTripTests.cs
```
