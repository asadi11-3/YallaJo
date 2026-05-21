# Wave 3 — Core Catalog (Places, Tours, Tour Guides, Businesses)

> **Sources:** `Agents/agent-context.md` §Wave 3 (Endpoints) + `Agents/guide.md` §2 (Tour Management)
> **Dependencies:** Wave 2 (Provider must be Approved to create tours)
> **Focus:** Tour CRUD, Business listings, Tour Guide profiles
> **Missing:** ~5 endpoints (~12h)

---

## 1. Current Status

| Area | Built | Missing |
|---|---|---|
| Tours CRUD | 11/11 ✅ | — |
| Tour search/listing | 5/5 ✅ | — |
| Businesses | 13/13 ✅ | — |
| Business hours | All ✅ | — |
| Tour Guides | 3/8 ⚠️ | 5 sub-resources missing |

### 1.1 Tour Guides — Missing endpoints (5)

| # | Method | Path | Description |
|---|---|---|---|
| 1 | GET | `/api/v1/guides/{id}` | Get tour guide profile |
| 2 | PUT | `/api/v1/guides/{id}` | Update guide profile |
| 3 | POST | `/api/v1/guides/{id}/languages` | Add language to guide |
| 4 | DELETE | `/api/v1/guides/{id}/languages/{langId}` | Remove language |
| 5 | POST | `/api/v1/guides/{id}/specializations` | Add specialization to guide |

Currently `POST /api/v1/guides` (register) + `GET /api/v1/guides` (list) exist. Sub-resources missing.

---

## 2. Tour Guide Domain Updates

### 2.1 Aggregate (already partially exists in `ContentTours.Domain/Entities/TourGuide.cs`)

Verify these methods exist:
```csharp
public sealed class TourGuide : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }  // FK to authentic user with Provider role
    public string Bio { get; private set; } = "";
    public int YearsOfExperience { get; private set; }
    public bool HasFirstAid { get; private set; }
    public string? MoTALicenseNumber { get; private set; }

    private readonly List<TourGuideLanguage> _languages = [];
    private readonly List<TourGuideSpecialization> _specializations = [];

    // Methods (add if missing):
    public void UpdateProfile(string bio, int yearsExp, bool hasFirstAid, string? license)
    public void AddLanguage(Guid languageId, string proficiency)  // raises TourGuideLanguageAddedDomainEvent
    public void RemoveLanguage(Guid languageId)
    public void AddSpecialization(Guid specializationId)
    public void RemoveSpecialization(Guid specializationId)
}
```

### 2.2 Many-to-Many Junction Tables (already exist)
- `TourGuideLanguages(TourGuideId, LanguageId, Proficiency)` — Native/Fluent/Conversational/Basic
- `TourGuideSpecializations(TourGuideId, SpecializationId)`

---

## 3. Business Rules (guide.md §2.2 + PDF1 Wave 3)

### 3.1 Tour Creation (already enforced ✅)
- Provider must have Status=Approved
- Initial status: Draft
- Required: title, description, placeId, categoryId, durationMinutes 15-2880, basePrice > 0 in JOD/USD/EUR, maxGroupSize 1-100, languageCode
- Unique name per provider; global unique slug (auto-generated with random suffix on collision)
- Place must exist and be active
- ≥1 image required before submit
- ≥1 schedule defined before submit
- ≥1 pricing tier defined before submit
- Description ≥100 chars at submit
- Meeting point lat/lng required
- Auto-trigger translation upon publishing
- Max 50 active tours per provider

### 3.2 Tour Approval (already enforced ✅)
- Admin 7-day SLA
- Critical field edits (BasePrice, DurationMinutes, MaxGroupSize, Latitude/Longitude, MeetingPoint) → revert to Pending
- Non-critical edits (description, photos) → go live immediately

### 3.3 Business Listings (already enforced ✅)
- Provider creates listing linked to Place
- Place must exist + active
- Default business hours Mon-Fri 9-5 auto-created
- Batch update business hours: send all 7 days at once (replace strategy)
- Validation: openTime < closeTime; 24h = both 00:00; no overlapping shifts; split shifts supported (multiple entries per day)

### 3.4 Tour Guides (PDF1 Wave 3 + guide.md)

- **GET /guides/{id}:** public, returns Bio, YearsOfExperience, HasFirstAid, Languages (with proficiency), Specializations, AverageRating, ReviewCount, Tour count
- **PUT /guides/{id}:** owner only (guide.UserId == currentUser.UserId). Updatable: Bio (max 2000 chars), YearsOfExperience (0-80), HasFirstAid bool, MoTALicenseNumber.
- **POST /languages:** owner only. Body: `{ languageId, proficiency }`. Proficiency: Native | Fluent | Conversational | Basic. Cannot add duplicate (UNIQUE on TourGuideId+LanguageId).
- **DELETE /languages/{langId}:** owner only. Cannot remove last language (must have ≥1).
- **POST /specializations:** owner only. Body: `{ specializationId }`. UNIQUE on TourGuideId+SpecializationId.

### 3.5 Edge Cases (guide.md §2.4)
- Provider creates tour for non-existent/deleted place → 404; frontend prevents
- Two providers same tour name same place → allowed (uniqueness per-provider); slug auto-differentiated
- Admin deletes place with active tours → blocked; admin must archive/reassign first
- Provider edits price with confirmed future bookings → price applies to NEW bookings only; existing keep booked price
- Provider tries to reduce MaxGroupSize below BookedCount → blocked

---

## 4. Authorization

Existing `ContentToursFeatures.TourGuide` should already have `Read`, `Create`, `Update`, `Delete`. Add if missing:
- `ContentToursFeatures.TourGuideLanguage` — `Create`, `Delete`
- `ContentToursFeatures.TourGuideSpecialization` — `Create`, `Delete`

Or piggyback on `TourGuide.Update` permission for all sub-resources (simpler — recommended).

---

## 5. Implementation

### 5.1 New endpoints in `ContentTours.Presentation/Endpoints/TourGuideEndpoints.cs`

```csharp
group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => ...)
    .WithName("GetTourGuide")
    .AllowAnonymous();

group.MapPut("/{id:guid}", async (Guid id, UpdateTourGuideRequest req, ICurrentUser user, ISender sender, CancellationToken ct) => ...)
    .RequireAuthorization()
    .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuide, AppAction.Update));

group.MapPost("/{id:guid}/languages", ...)
    .RequireAuthorization()
    .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuide, AppAction.Update));

group.MapDelete("/{id:guid}/languages/{langId:guid}", ...)
    .RequireAuthorization()
    .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuide, AppAction.Update));

group.MapPost("/{id:guid}/specializations", ...)
    .RequireAuthorization()
    .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuide, AppAction.Update));
```

### 5.2 Commands

- `UpdateTourGuideProfileCommand(GuideId, CallerUserId, Bio, YearsOfExperience, HasFirstAid, MoTALicenseNumber)` — verify guide.UserId == CallerUserId
- `AddTourGuideLanguageCommand(GuideId, CallerUserId, LanguageId, Proficiency)`
- `RemoveTourGuideLanguageCommand(GuideId, CallerUserId, LanguageId)`
- `AddTourGuideSpecializationCommand(GuideId, CallerUserId, SpecializationId)`

### 5.3 Query

- `GetTourGuideByIdQuery(GuideId)` — returns `TourGuideDto` with full profile

---

## 6. WBS

| # | Step | Hours |
|---|---|---|
| 1 | TourGuide aggregate method additions (if missing) | 1 |
| 2 | Domain events: TourGuideUpdated, LanguageAdded/Removed, SpecializationAdded | 1 |
| 3 | `GetTourGuideByIdQuery` + handler + DTO | 1.5 |
| 4 | `UpdateTourGuideProfileCommand` + validator + handler | 2 |
| 5 | 2 language commands + handlers + validators | 2 |
| 6 | 1 specialization command + handler | 1 |
| 7 | 5 endpoint routes in `TourGuideEndpoints.cs` | 1.5 |
| 8 | Migration (if schema changes) | 0.5 |
| 9 | Unit tests (6+) + integration tests (3+) | 1.5 |
| **Total** | | **~12h** |

---

## 7. Acceptance Criteria

- [ ] All 5 missing endpoints respond per PDF1 spec
- [ ] Ownership enforced — only guide.UserId can edit
- [ ] Cannot add duplicate language/specialization
- [ ] Cannot remove last language
- [ ] Proficiency enum validated
- [ ] Public GET works without auth
- [ ] Integration tests cover: get, update, add/remove language, add specialization
- [ ] `dotnet build` green for ContentTours.*
- [ ] No regressions in existing Wave 3 tests
