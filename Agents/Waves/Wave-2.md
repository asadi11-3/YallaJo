# Wave 2 — First-Level Content & Onboarding

> **Sources:** `Agents/agent-context.md` §Wave 2 (Endpoints) + `Agents/guide.md` §1 (Provider Registration), §2 (Places)
> **Dependencies:** Wave 1 (Auth, Roles)
> **Focus:** Places, Attachments, Provider Application module (NEW)
> **Largest gap in PDF1 — 11 missing P0 endpoints, ~40 hours**

---

## 1. Current Status

| Area | Built | Missing |
|---|---|---|
| Places | 10/10 ✅ | — |
| Attachments (generic) | ✅ | — |
| Attachments image flows | ⚠️ | `POST /attachments/{entityType}/{entityId}/images`, `PUT .../reorder` |
| Profile avatar | ✅ | `POST /profile/avatar` canonical verb added; existing PUT retained for compatibility |
| **Provider Application module** | **0/11** 🔴 | **ENTIRE MODULE MISSING** |

### 1.1 Provider Application — Missing endpoints (11)

| # | Method | Path | Auth |
|---|---|---|---|
| 1 | GET | `/api/v1/admin/providers` | `AdminProviderQueue.Read` |
| 2 | GET | `/api/v1/provider/status` | `Profile.Read` (self) |
| 3 | POST | `/api/v1/provider/register` | self |
| 4 | POST | `/api/v1/provider/apply` | `ProviderApplication.Submit` (self) |
| 5 | POST | `/api/v1/provider/documents` | self |
| 6 | PUT | `/api/v1/provider/documents/{id}` | self |
| 7 | POST | `/api/v1/admin/providers/{id}/approve` | `AdminProviderQueue.Approve` |
| 8 | POST | `/api/v1/admin/providers/{id}/reject` | `AdminProviderQueue.Reject` |
| 9 | POST | `/api/v1/admin/providers/{id}/request-docs` | `AdminProviderQueue.RequestDocs` |
| 10 | POST | `/api/v1/admin/providers/{id}/suspend` | `AdminProviderQueue.Suspend` |
| 11 | POST | `/api/v1/profile/avatar` (canonical) | ✅ implemented under existing Accounts route group as `/api/v1/accounts/profile/avatar` with `Profile.Update` permission |

---

## 2. New Module: `Accounts.ProviderApplication`

This is the largest deliverable of this sprint — a complete state-machine-driven application module.

### 2.1 Aggregate: `ProviderApplication`

`AuditableEntity, IAggregateRoot` in `Accounts.Domain/Entities/`:

```csharp
public sealed class ProviderApplication : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public ProviderType Type { get; private set; }
    public string BusinessName { get; private set; } = "";
    public string ContactEmail { get; private set; } = "";
    public string ContactPhone { get; private set; } = "";
    public string Address { get; private set; } = "";
    public string Description { get; private set; } = "";
    public ProviderApplicationStatus Status { get; private set; }  // Draft, Pending, MoreDocsNeeded, Approved, Rejected, Suspended
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? SuspensionReason { get; private set; }
    public int ReapplicationCount { get; private set; }            // max 3 per guide.md §1.3
    public DateTime? CoolingPeriodEndsAt { get; private set; }     // 7d cooling after reject

    // Type-specific JSON metadata
    public string? TypeSpecificDataJson { get; private set; }

    private readonly List<ProviderDocument> _documents = [];
    public IReadOnlyCollection<ProviderDocument> Documents => _documents.AsReadOnly();

    // Factory + state methods
    public static ProviderApplication Register(...) // raises ProviderRegisteredDomainEvent
    public void Submit(...)                         // Draft|MoreDocsNeeded|Rejected → Pending
    public void Approve(Guid adminId, TimeProvider) // Pending → Approved + raises event
    public void Reject(Guid adminId, string reason, TimeProvider)
    public void RequestMoreDocs(Guid adminId, IReadOnlyList<string> missingTypes, TimeProvider)
    public void Suspend(Guid adminId, string reason, TimeProvider)
    public void Reinstate(Guid adminId, TimeProvider)
    public void AddDocument(ProviderDocument doc)
    public void ReplaceDocument(Guid docId, ProviderDocument newDoc)
}
```

### 2.2 Enums

```csharp
public enum ProviderType : byte
{
    TourOperator = 0,
    IndependentGuide = 1,   // PDF2 §1.1: "Independent Tour Guide"
    HotelResort = 2,        // PDF2 §1.1: "Business Owner" maps here
    ActivityCenter = 3,     // PDF2 §1.1: "Freelance Activity Instructor"
    Agency = 4              // PDF2 §1.1 also lists Agency
}

public enum ProviderApplicationStatus : byte
{
    Draft = 0,
    Pending = 1,
    MoreDocsNeeded = 2,
    Approved = 3,
    Rejected = 4,
    Suspended = 5
}
```

### 2.3 Domain Events (Accounts.Domain/Events/)

- `ProviderRegisteredDomainEvent(ApplicationId, UserId, Type, RegisteredAt)`
- `ProviderApplicationSubmittedDomainEvent(ApplicationId, SubmittedAt)`
- `ProviderApprovedDomainEvent(ApplicationId, UserId, Type, ApprovedAt, ApprovedByAdminId)`
- `ProviderRejectedDomainEvent(ApplicationId, UserId, Reason, RejectedAt)`
- `ProviderMoreDocsRequestedDomainEvent(ApplicationId, MissingDocumentTypes, RequestedAt)`
- `ProviderSuspendedDomainEvent(ApplicationId, UserId, Reason, SuspendedAt)`
- `ProviderReinstatedDomainEvent(ApplicationId, UserId, ReinstatedAt)`
- `ProviderDocumentExpiringDomainEvent(ApplicationId, DocumentId, ExpiresAt)` — already exists in Booking
- `ProviderDocumentExpiredDomainEvent(ApplicationId, DocumentId, ExpiredAt)` — already exists in Booking

### 2.4 Integration Events (Accounts.Contracts/IntegrationEvents/)

- `accounts.provider.registered.v1` — `ProviderRegisteredIntegrationEvent`
- `accounts.provider.approved.v1` — `ProviderApprovedIntegrationEvent` (consumers: Booking unlocks provider features, Messaging sends welcome email)
- `accounts.provider.rejected.v1` — `ProviderRejectedIntegrationEvent`
- `accounts.provider.suspended.v1` — `ProviderSuspendedIntegrationEvent` (consumers: Booking hides tours, ContentTours hides listings)
- `accounts.provider.reinstated.v1` — `ProviderReinstatedIntegrationEvent`
- `accounts.provider.status-changed.v1` — generic status-change event (Finance already consumes for commission tier re-eval)

### 2.5 Required Documents by Type (PDF2 §1.1)

| Type | Required Documents |
|---|---|
| TourOperator | Business license, tax registration, Tourism authority license, insurance certificate |
| IndependentGuide | Government ID, MoTA license, Insurance, TIN |
| HotelResort (Business Owner) | Business license, tax certificate, proof of ownership/lease, health & safety, fire safety |
| ActivityCenter (Activity Instructor) | Government ID, insurance, TIN + relevant certification, liability insurance |
| Agency | Business license, tax certificate, ownership/lease + list of affiliated guides |

Server-side validation map (`ProviderDocumentRequirements.cs`):
```csharp
public static IReadOnlyList<DocumentType> Required(ProviderType type) => type switch
{
    ProviderType.TourOperator => new[] { DocumentType.BusinessLicense, DocumentType.TaxRegistration, ... },
    ...
};
```

---

## 3. Business Rules (guide.md §1)

### 3.1 Registration
- ✅ Must have OTP-verified user account before applying
- ✅ All required documents must be uploaded (non-draft) before submit
- ✅ Max file size 5MB (avatar), 10MB (documents); PDF/JPG/PNG only
- ✅ Max 10 documents per application (including re-submissions)
- ✅ A user holds ONE provider type at a time; switching requires deactivating previous
- ✅ Agencies must have ≥1 affiliated guide listed at registration

### 3.2 Admin Review
- ✅ 7-day SLA; system reminder to admin at day 5
- ✅ Status change → email + in-app notification to provider
- ✅ Rejection reason mandatory, visible on provider's status page
- ✅ Escalation at day 7 to super-admin

### 3.3 Re-application
- ✅ 7-day cooling period from rejection date
- ✅ Re-application resets review SLA to fresh 7 days
- ✅ Previous rejection reason visible to provider
- ✅ **Max 3 re-applications** — beyond that, contact-support manual flow

### 3.4 Document Expiry (already partially built — BG service `DocumentExpiryCheckService` exists)
- ✅ Notification at 30 days before expiry
- ✅ 14-day grace period after expiry, then auto-suspend
- ✅ Auto-suspend = listings hidden, no new bookings; confirmed bookings honored
- ✅ Lift suspension on renewed document upload + admin re-approval

### 3.5 State Machine (guide §1.5)

```
(none) ──Submit──▶ Pending
Pending ──Approve──▶ Approved
Pending ──Reject──▶ Rejected ──(7d cooling)──▶ Pending (re-apply, max 3)
Pending ──RequestDocs──▶ MoreDocsNeeded ──Resubmit──▶ Pending
Approved ──DocExpires+14d──▶ Suspended
Suspended ──RenewDoc+AdminApprove──▶ Approved
```

### 3.6 Edge Cases (guide §1.4)
- Corrupt file → validate file headers; reject "File could not be read"
- Submit without required → button disabled, missing docs highlighted
- 7-day SLA breach → escalate to super-admin + applicant email
- Duplicate license number for same provider type → enforce unique constraint, second app rejected
- Agency removes all guides → status → Suspended until ≥1 added

---

## 4. Authorization

### 4.1 New `AccountsFeatures` constants
Add to `Accounts.Contracts/Authorization/AccountsFeatures.cs`:
```csharp
public const string ProviderApplication = nameof(ProviderApplication);
public const string AdminProviderQueue = nameof(AdminProviderQueue);
```

### 4.2 New `AccountsPermissionCatalog` entries
```csharp
new(ProviderApplication, AppAction.Register, ...),  // create app
new(ProviderApplication, AppAction.Submit, ...),    // submit for review
new(ProviderApplication, AppAction.Read, ...),      // self-status
new(AdminProviderQueue, AppAction.Read, ...),       // admin list
new(AdminProviderQueue, AppAction.Approve, ...),
new(AdminProviderQueue, AppAction.Reject, ...),
new(AdminProviderQueue, AppAction.RequestDocs, ...),
new(AdminProviderQueue, AppAction.Suspend, ...),
new(AdminProviderQueue, AppAction.Reinstate, ...),
```

Add `AppAction.Register`, `AppAction.Submit`, `AppAction.RequestDocs`, `AppAction.Reinstate` to SharedKernel `AppAction.cs` if missing.

---

## 5. Migration

`AccountsAddProviderApplicationModule` covers:
- `accounts.ProviderApplications` table
- `accounts.ProviderDocuments` table (FK to ProviderApplications)
- Indexes: `(UserId)`, `(Status, ReviewedAt)`, `(Type, Status)`, unique filtered `(UserId) WHERE Status = Approved` (one active type per user)

Generate via:
```powershell
dotnet ef migrations add AccountsAddProviderApplicationModule `
  --project 'Accounts.Infrastructure\Accounts.Infrastructure.csproj' `
  --startup-project 'YallaJo.Api\YallaJo.Api.csproj' `
  --context AccountsDbContext --output-dir Migrations
```

---

## 6. Other Missing Wave 2 Endpoints (3, ~3h)

### 6.1 `POST /api/v1/profile/avatar` (1h)
Currently exists as `PUT /api/v1/profile/avatar` in `Accounts.Presentation/Endpoints/Profile/ProfileEndpoints.cs`. Per PDF1 it's POST. Add POST alias route — both can call the same command handler.

### 6.2 `POST /api/v1/attachments/{entityType}/{entityId}/images` (1.5h)
Bulk upload, max 20 per entity, auto-create EntityImage records (Thumbnail/Small/Medium/Large/Original). First image = primary. Caller must own the entity (validated server-side per entityType: Tour → check Tour.ProviderUserId, Place → admin-only, etc.).

Add to `ContentCore.Presentation/Endpoints/AttachmentEndpoints.cs`.

### 6.3 `PUT /api/v1/attachments/{entityType}/{entityId}/images/reorder` (0.5h)
Body: `IReadOnlyList<Guid> orderedImageIds`. Updates SortOrder; first becomes IsPrimary.

---

## 7. WBS

| # | Step | Hours | Owner |
|---|---|---|---|
| 1 | Aggregate `ProviderApplication` + enums + domain events | 6 | Backend Dev A |
| 2 | Domain rules: state machine + reapplication cap + cooling | 4 | Backend Dev A |
| 3 | `ProviderDocument` entity + encryption integration (IV + HMAC via SharedKernel) | 4 | Backend Dev B |
| 4 | Repos: `IProviderApplicationRepository`, `IProviderDocumentRepository` + EF configs | 3 | Backend Dev B |
| 5 | Migration `AccountsAddProviderApplicationModule` | 1 | Backend Dev A |
| 6 | Commands: Register, Submit, AddDocument, ReplaceDocument | 4 | Backend Dev A |
| 7 | Admin commands: Approve, Reject, RequestDocs, Suspend, Reinstate | 4 | Backend Dev B |
| 8 | Queries: GetMyApplicationStatus, GetAdminQueue (paginated) | 3 | Backend Dev B |
| 9 | Integration events emitter (`Accounts.Infrastructure/EventHandlers/AccountsIntegrationConverters.cs`) | 2 | Backend Dev A |
| 10 | Register events in `IntegrationEventTypeRegistry` | 0.5 | Either |
| 11 | Endpoints: `Accounts.Presentation/Endpoints/Provider/ProviderEndpoints.cs` + `AdminProviderEndpoints.cs` | 4 | Backend Dev A |
| 12 | Permissions catalog updates (AccountsFeatures + AccountsPermissionCatalog) | 1 | Backend Dev B |
| 13 | `POST /profile/avatar` alias + 2 attachment image endpoints | 3 | Backend Dev B |
| 14 | Unit tests (8+) + Integration tests (5+) | 4 | Both |
| **Total** | | **~42h** | |

---

## 8. Acceptance Criteria

- [ ] All 11 missing endpoints respond per PDF1 spec
- [ ] State machine transitions verified end-to-end (Pending→Approved, Pending→Rejected→Pending after 7d, etc.)
- [ ] Max 3 re-applications enforced — 4th attempt returns 422 `ProviderApplication.TooManyReapplications`
- [ ] Document file constraints enforced (5MB avatar, 10MB docs, PDF/JPG/PNG only)
- [ ] Required-by-type validation rejects partial document sets
- [ ] `ProviderApprovedIntegrationEvent` triggers role assignment (consumed by Security module — verify cross-module integration)
- [ ] `DocumentExpiryCheckService` (already exists in Booking — verify cross-module wiring) marks expiring/expired documents
- [ ] Admin queue paginated, filterable by status + type
- [ ] Integration tests: 5+ tests covering state machine, document upload, admin actions
- [ ] All endpoints use `MustHavePermissionAttribute`
- [ ] `dotnet build` green for Accounts.* + YallaJo.Api
