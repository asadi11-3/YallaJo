# Playwright MCP Test Scenarios — Platform Onboarding Module

> **API-ONLY MODE.** This checkout has no `YallaJo.Web`. Every `browser_navigate("https://localhost:57065/swagger…")` step below is a docking step; the actual request runs through `window.__yj.apiFetch(...)` defined in [`Playwright-APIOnly-Adapter.md`](./Playwright-APIOnly-Adapter.md). Read that adapter once at the start of every Playwright session — it also lists the 8 seeded test users and their credentials.

## Source Plans / Score

- Primary inputs read in full: `Agents/Plans/Platform-Onboarding-Workflow.md`, `Platform-Onboarding-Audit-Report.md`, `Platform-Onboarding-FixPlan.md`.
- Reference inputs read: `Agents/Plans/Master-RoadmapTo10.md`, `Agents/Plans/CrossDocumentAnalysisReport.md` (Provider Type Taxonomy resolution).
- Code sampled against current repository root: `Accounts.Application`, `Accounts.Domain`, `Accounts.Infrastructure`, `Accounts.Presentation`, plus `YallaJo.Web/Areas/**` inventory. User-provided `src\...` paths do not exist in this checkout.
- Current score baseline: Workflow header says **implemented/audited score 9.0/10**; older audit report says **7.8/10**. Current code is newer than the audit: `ProviderApplication.Reapply()`, `POST /api/v1/provider/reapply`, public agency endpoints, and dashboard notifications now exist.
- Runtime context: Web URL `https://localhost:57065/swagger`, API URL `https://localhost:57065`; app currently not running, SQL bug noted, Recaptcha disabled. No `YallaJo.Web/Areas/PlatformOnboarding` or `YallaJo.Web/Areas/Onboarding` area exists in this checkout, so active Playwright tests should use Swagger/API `fetch` fallback until Razor UI ships.
- MCP mode: use `browser_navigate`, `browser_snapshot`, `browser_click`, `browser_fill_form`, `browser_select_option`, `browser_file_upload`, `browser_evaluate`, `browser_network_requests`, `browser_wait_for`, `browser_console_messages`, `browser_take_screenshot`, `browser_tabs`.

## 0. Prerequisites (file storage configured, OTP service stub, ProviderTypes seeded)

1. Fix the local SQL startup issue before running scenarios. Start API at `https://localhost:57065`; verify `/swagger` loads and no fatal console/network errors occur.
2. Start Web at `https://localhost:57065/swagger`; expected UI gap: onboarding Razor area currently absent. Web route tests may assert 404/redirect and then continue with API/Swagger.
3. Recaptcha disabled; do not fill/assert Recaptcha.
4. Seed/login users, all password `TestPass!23`:
   - `admin@yallajo.test` — Administrator with onboarding-review permission.
   - `userA@yallajo.test` — logged-in user, OTP/email-verified, no provider role; primary onboarding candidate.
   - `userB@yallajo.test` — already applied 3 times; exceeds reapply limit.
   - `guide-pending@yallajo.test` — existing Pending application.
   - `guide-approved@yallajo.test` — already Approved; cannot re-apply as different type.
   - `suspended@yallajo.test` — auto-suspended due to expired docs.
5. Provider types seeded/displayable. Product-facing Phase 1 taxonomy uses 4 business labels: Independent Tour Guide, Business Owner, Agency, Freelance Activity Instructor. Current code enum has 6 values: `TourOperator=0`, `IndependentGuide=1`, `HotelResort=2`, `ActivityCenter=3`, `Agency=4`, `BusinessOwner=5`. Tests must record this taxonomy divergence, not silently accept wrong UI labels.
6. Prepare mock upload directory and test files before browser run:
   - `valid-id.pdf` — real PDF, < 1 MB.
   - `valid-license.jpg` — real JPG, < 1 MB.
   - `valid-registration.png` — real PNG, < 1 MB.
   - `invalid-notes.txt` — plain text; must be rejected.
   - `oversized-5mb-plus.pdf` — real or padded PDF > 5 MB; business rule says reject.
   - `oversized-10mb-plus.pdf` — > 10 MB; current code validator rejects.
7. File-upload caveat: current API endpoint accepts document metadata (`FileUrl`, `FileName`, `FileSizeBytes`, `ExpiresAt`) rather than multipart upload. If the Web UI introduces multipart, use `browser_file_upload`; otherwise simulate upload by using the configured mock storage/admin test helper to create `FileUrl`, then call `POST /api/v1/provider/documents` via `browser_evaluate`.
8. Time-travel tests require one of:
   - test/admin clock-skew endpoint callable via `browser_evaluate`; or
   - direct DB update of document `ExpiresAt`, `SubmittedAt`, `CoolingPeriodEndsAt`, and background-service last-run markers.
9. ProviderDocumentExpiryService runs daily with 30-day warning. There is no visible separate `SuspensionService` in sampled Accounts code; suspension at grace-end should be treated as NOT_BUILT unless another module adds it.

## 1. Built — Active Scenarios

> Base API routes from current code: `/api/v1/provider`, `/api/v1/admin/providers`, `/api/v1/agency`, `/api/v1/guides`. Because `AccountsEndpoints.cs` passes prefixed route groups into endpoint classes that also call `MapGroup("/api/v1/...")`, first smoke-test actual route mounting; if routes are double-prefixed, capture as blocker before deeper tests.

### Provider Registration Flow

| ID | Endpoint/UI | Role/User | MCP steps | Assertions / edge cases |
|---|---|---|---|---|
| TC-PO-001 | Web `/onboarding`, `/PlatformOnboarding`, `/provider/register` | Anonymous | `browser_navigate` each likely UI route; inspect `browser_snapshot` and network. | Current checkout likely 404/redirect because Razor area is absent. Record UI NOT_BUILT; continue API route scenarios. |
| TC-PO-002 | `POST /api/v1/auth/login` then API auth storage | `userA` | Navigate Swagger or login UI; use `browser_evaluate` login; store bearer/cookie in page context. | 200 login; no Recaptcha; JWT/cookie usable for provider calls. |
| TC-PO-003 | `GET /api/v1/provider/status` | `userA` | Authenticated GET before any app. | 404/no application or status `None`; anonymous returns 401; user with no permission returns 403 if permission seed is stricter than baseline. |
| TC-PO-004 | `POST /api/v1/provider/register` | `userA` | Select `IndependentGuide`; POST business/contact/address/description/type JSON. | 201/created result; status `Draft`; `ProviderRegisteredIntegrationEvent` and generic `ProviderStatusChanged(None→Draft)` eventually in outbox if DB inspected. |
| TC-PO-005 | Provider type UI/options | `userA` | If UI exists: select provider type. Else call API with all enum values 0..5. | Current API accepts 6 enum values. Product-facing 4-label taxonomy mismatch logged: Freelance Activity Instructor has no enum; HotelResort/ActivityCenter/TourOperator exist beyond Phase 1 business labels. |
| TC-PO-006 | Register duplicate while Draft | `userA` | Repeat register with different provider type. | Conflict/invalid: one active application per user. Must not create second application or allow second type. |
| TC-PO-007 | Register while Pending | `guide-pending` | Login pending user; POST register Agency/BusinessOwner. | 409 `Provider.ApplicationPending`/equivalent; cannot select second provider type while existing application Pending. |
| TC-PO-008 | Register while Approved | `guide-approved` | Login approved provider; POST register another type. | 409 `Provider.AlreadyApproved`; cannot re-apply for different provider type. |
| TC-PO-009 | Register when suspended | `suspended` | Login suspended provider; GET status; attempt register. | Status `Suspended`; register blocked unless reinstatement flow returns to Approved first. |
| TC-PO-010 | Register invalid payload | `userA` disposable/reset | POST missing business name, invalid email, empty phone/address/description, invalid enum. | 400/422 validation; max lengths: business 200, email 256, phone 30, address 500, description 2000. |
| TC-PO-011 | Submit without required docs | Draft application | `POST /api/v1/provider/apply`. | 422 `ProviderApplication.MissingRequiredDocuments`; remains `Draft`; missing doc list visible via status/detail if exposed. |
| TC-PO-012 | Submit complete IndependentGuide application | `userA` | Add GovernmentId, MotaLicense, TaxIdentificationNumber, InsuranceCertificate; POST apply. | 200; status `Pending`; `SubmittedAt` set; generic status event `Draft→Pending`; user sees pending status. |

### Document Management

| ID | Endpoint/UI | Role/User | MCP steps | Assertions / edge cases |
|---|---|---|---|---|
| TC-PO-013 | Web document upload control | Draft app | If UI exists, call `browser_file_upload` with `valid-id.pdf`, `valid-license.jpg`, `valid-registration.png`. | Upload succeeds for PDF/JPG/PNG; UI shows filename, type, expiry date. If no UI, mark UI NOT_BUILT and run API metadata tests. |
| TC-PO-014 | `POST /api/v1/provider/documents` PDF | Draft app | API metadata POST: `DocumentType=GovernmentId`, `FileName=valid-id.pdf`, `<5MB`, `ExpiresAt=+1y`. | 201; document ID returned; status still Draft; expiry saved. |
| TC-PO-015 | JPG/PNG accepted | Draft app | Add JPG/PNG document metadata or upload through UI. | 201 for JPG and PNG; file names preserved. |
| TC-PO-016 | Invalid file type rejected | Draft app | Use `browser_file_upload` `invalid-notes.txt` if UI exists; API metadata `FileName=invalid-notes.txt`. | Business expectation: reject TXT. Current sampled API validator does **not** validate extension/content type, only metadata and size; if accepted, log NOT_BUILT/divergence. |
| TC-PO-017 | Oversized >5MB rejected | Draft app | Upload/API metadata with `FileSizeBytes=5MB+1`. | User requirement says reject >5MB. Current validator allows up to 10MB; if 5-10MB accepted, log divergence. |
| TC-PO-018 | Oversized >10MB rejected | Draft app | API metadata `FileSizeBytes=10MB+1`. | 400/422 validation; current code message says max 10 MB. |
| TC-PO-019 | Max 10 docs per application | Draft app | Add 10 distinct document types/fixtures, then attempt 11th. | First 10 succeed; 11th returns `ProviderApplication.TooManyDocuments`; count remains 10. |
| TC-PO-020 | Duplicate document type rejected | Draft app | Add GovernmentId twice. | 409/422 `DuplicateDocumentType`; existing doc unchanged. |
| TC-PO-021 | Replace document | Draft/MoreDocsNeeded app | `PUT /api/v1/provider/documents/{id}` with new URL/name/expiry. | 200; same `DocumentType`; `UploadedAt` changes; expiry updated; old URL no longer active in detail/status. |
| TC-PO-022 | Replace missing document | Draft app | PUT random GUID. | 404/422 `DocumentNotFound`; no new doc created. |
| TC-PO-023 | Document type enum coverage | Any draft | Iterate all enum values: BusinessLicense, TaxRegistration, TourismAuthorityLicense, InsuranceCertificate, GovernmentId, MotaLicense, TaxIdentificationNumber, ProofOfOwnership, HealthAndSafety, FireSafety, RelevantCertification, LiabilityInsurance, AffiliatedGuidesList. | Valid enum values accepted if app not at max and not duplicate; invalid enum rejected. Note user examples `IdCard`, `BusinessRegistration`, `GuideLicense` are product aliases, not current code enum names. |
| TC-PO-024 | Required docs per provider type | New/reset users or DB reset | For each enum type, submit with one required doc missing. | Each type enforces hardcoded required set. IndependentGuide requires GovernmentId/MotaLicense/TaxIdentificationNumber/InsuranceCertificate; Agency requires AffiliatedGuidesList too; BusinessOwner requires BusinessLicense/TaxRegistration/HealthAndSafety. |

### Admin Review Workflow

| ID | Endpoint/UI | Role/User | MCP steps | Assertions / edge cases |
|---|---|---|---|---|
| TC-PO-025 | Admin login + queue | `admin` | Login; GET `/api/v1/admin/providers?status=Pending&type=IndependentGuide&page=1&pageSize=20`. | 200 queue; includes pending app; filters by status/type; pageSize clamped max 100. |
| TC-PO-026 | Admin opens application sees docs | `admin` | Use queue result application ID; if detail endpoint absent, inspect queue payload or DB/admin UI. | Admin can see applicant business/contact/type/status/docs. If no dedicated detail endpoint exists, mark detail UI/API gap. |
| TC-PO-027 | Non-admin queue denied | `userA` | GET admin queue. | 403 for authenticated non-admin; anonymous 401. |
| TC-PO-028 | Approve pending IndependentGuide | `admin` | POST `/api/v1/admin/providers/{id}/approve`; poll status as user. | 200; status Approved; `ReviewedAt`/`ReviewedByUserId`; integration events: `ProviderApproved`, `ProviderStatusChanged(Pending→Approved)`; Security role assignment handler grants `TourGuide` for IndependentGuide; ContentTours creates TourGuide profile. |
| TC-PO-029 | Approve non-pending invalid | `admin` | Approve Draft/Rejected/Approved/Suspended app. | 422/invalid status; no duplicate event; idempotent second approve must not publish duplicate outbox. |
| TC-PO-030 | Reject with reason | `admin` | Submit app; POST reject with reason. | 200; status Rejected; `RejectionReason`; `CoolingPeriodEndsAt≈now+7d`; `ReapplicationCount++`; `ProviderRejected` + generic status event; user notified via Messaging handler. |
| TC-PO-031 | Reject validation | `admin` | POST reject with empty/short reason if validator enforces. | 400/422; app remains Pending. If short reason accepted, log validation gap. |
| TC-PO-032 | Request more docs | `admin` | POST request-docs with missing document list and notes. | 200; status `MoreDocsNeeded`; user notified via generic `ProviderStatusChanged`; sampled code has no dedicated `ProviderMoreDocsRequestedIntegrationEvent`, only generic. |
| TC-PO-033 | MoreDocsNeeded user resubmits | Applicant | Replace/add requested docs; `POST /api/v1/provider/apply`. | 200; status Pending; MoreDocsNeeded→Pending accepted by `Submit()`. |
| TC-PO-034 | 7-day SLA warning | Pending app | Set `SubmittedAt=now-6d` via clock/DB; GET admin queue/dashboard. | SLA warning/escalation should be visible if implemented. If no SLA fields/service/UI exists, mark NOT_BUILT. |
| TC-PO-035 | 7-day SLA escalation | Pending app | Set `SubmittedAt=now-8d`; run/poll background/admin queue. | Escalation flag/notification expected by business rule; likely NOT_BUILT unless queue exposes overdue status. |

### Re-application

| ID | Endpoint/UI | Role/User | MCP steps | Assertions / edge cases |
|---|---|---|---|---|
| TC-PO-036 | Reapply blocked during cooling period | Rejected user | Immediately after rejection, POST `/api/v1/provider/reapply`. | 422 `CoolingPeriodActive`; status remains Rejected; `CoolingPeriodEndsAt` surfaced. |
| TC-PO-037 | Reapply after cooling period | Rejected user | Fast-forward/DB set `CoolingPeriodEndsAt<now`; POST reapply. | 200; status Draft; `RejectionReason` cleared; `ProviderReappliedDomainEvent` raised; user can update/replace docs and resubmit. |
| TC-PO-038 | Reapply max 3 times | `userB` | Login userB with `ReapplicationCount=3`; POST reapply. | 422 `MaxReapplicationsReached`; no Draft transition. |
| TC-PO-039 | 4th application attempt blocked | `userB` | Try register/apply via UI/API after three rejections. | Blocked; no new app row and no status reset. |
| TC-PO-040 | Reapply invalid state | Approved/Pending/Draft user | POST reapply from non-Rejected states. | 422 invalid status; no event. |

### Document Expiry Lifecycle

| ID | Endpoint/UI | Role/User | MCP steps | Assertions / edge cases |
|---|---|---|---|---|
| TC-PO-041 | 30-day expiring warning | Approved provider | Set doc `ExpiresAt=now+29d`; run `ProviderDocumentExpiryService` tick or wait; inspect notifications/outbox. | Publishes `ProviderDocumentExpiringIntegrationEvent`; dashboard notifications endpoint returns expiring doc. |
| TC-PO-042 | Not warning before threshold | Approved provider | Set doc `ExpiresAt=now+31d`; run service. | No expiring event/notification yet. |
| TC-PO-043 | Expired document detection | Approved provider | Set doc `ExpiresAt=now-1d`; run service. | Sampled service logs expired docs but does not publish `DocumentExpired`; if no event/status change, mark NOT_BUILT. |
| TC-PO-044 | 14-day grace period after expiry | Approved provider | Set doc expired by 1-13 days; poll status. | Business expectation: grace state visible, provider warned but active. Current code has no grace status on document/application; likely NOT_BUILT. |
| TC-PO-045 | Auto-suspend after grace | Approved provider | Set doc expired by 15+ days; run expiry/suspension background flow. | Business expectation: provider status Suspended and `ProviderSuspended` event. Current sampled code lacks grace/suspension service; likely NOT_BUILT except seeded `suspended` fixture. |
| TC-PO-046 | Re-upload during grace reactivates | Grace provider | Replace expired document before grace end. | Business expectation: document Active, warning cleared, no suspension. Current code can replace docs but lacks grace/reactivation state; record partial. |
| TC-PO-047 | Suspended provider reinstatement | `admin` + `suspended` | POST `/api/v1/admin/providers/{id}/reinstate` after replacing valid docs. | 200; Suspended→Approved; `ProviderReinstated` + generic status event; downstream modules re-enable where handlers exist. |

### Provider Type Restrictions

| ID | Endpoint/UI | Role/User | MCP steps | Assertions / edge cases |
|---|---|---|---|---|
| TC-PO-048 | One provider type at a time | Any active app user | Try second register in Draft/Pending/Approved/Suspended states. | Conflict/invalid; current user has at most one active provider application. |
| TC-PO-049 | Cannot IndependentGuide + Agency simultaneously | `guide-approved` | Attempt Agency register/application under same account. | 409 AlreadyApproved/one-type guard; roles not both granted on same account. |
| TC-PO-050 | Agency approval requires ≥1 guide attached | Agency applicant | Submit Agency with required docs but no affiliated guides; admin approve. | Business expectation: blocked until at least one guide attached. Sampled `ApproveProviderCommand`/domain only checks Pending; if approval succeeds, log NOT_BUILT/divergence. |
| TC-PO-051 | Agency with affiliated guide approval | Agency applicant + guide | Create/seed agency guide affiliation, then admin approve. | Approval allowed; Agency/provider role granted; guide roster endpoints usable. |
| TC-PO-052 | BusinessOwner post-approval business creation access | Approved BusinessOwner | After approval, attempt ContentPlaces business create path if available. | Provider role grants business creation; business still starts Pending. If provider guard absent in ContentPlaces, cross-module test should catch separately. |
| TC-PO-053 | IndependentGuide post-approval TourGuide profile | Approved IndependentGuide | Poll ContentTours guide profile/API after approval. | TourGuide profile auto-created; role granted as TourGuide (per sampled Security handler), not generic Provider only. |
| TC-PO-054 | Suspended provider cannot operate | `suspended` | Try provider dashboard/settings and downstream create tour/business. | Status Suspended visible; downstream operations should block via provider status service/guards. If ContentTours/ContentPlaces allow create, record crossing divergence. |

## 2. NOT_BUILT

| ID | Planned item | Source | Current status / skip reason | Skeleton MCP assertion |
|---|---|---|---|---|
| SKIP-PO-001 | Razor UI area `YallaJo.Web/Areas/PlatformOnboarding` or `Areas/Onboarding` | User context | No such area in current checkout. | Navigate likely UI routes; expect 404/redirect; use API fallback. |
| SKIP-PO-002 | True multipart document upload endpoint | User requirement | Current sampled API accepts metadata (`FileUrl`, name, size) only. | UI can use `browser_file_upload` if built; API multipart to `/documents` should fail/415 until implemented. |
| SKIP-PO-003 | MIME/extension validation for TXT rejection | User requirement | Sampled `AddProviderDocumentCommandValidator` defines allowed content types but does not use them; no extension check. | TXT upload/API metadata may be accepted; expected NOT_BUILT bug. |
| SKIP-PO-004 | 5MB max file-size rule | User requirement | Current code enforces 10MB from plan, not 5MB. | 5MB+ file accepted up to 10MB; log divergence. |
| SKIP-PO-005 | `ProviderApplicationSubmittedIntegrationEvent` dedicated contract | Older plan/audit | Current code uses generic `ProviderStatusChangedIntegrationEvent` for Submitted. | Search/outbox should not require dedicated event; assert generic event instead. |
| SKIP-PO-006 | `ProviderMoreDocsRequestedIntegrationEvent` dedicated contract | Older plan/audit | Current code uses generic status event only. | Assert generic `NewStatus=MoreDocsNeeded`. |
| SKIP-PO-007 | `DocumentExpired` / `ProviderDocumentExpired` event | CrossDocumentAnalysisReport CROSSING-1 | Sampled service only logs expired docs; no expiry event found in sampled code. | Force expired doc; expect no event; mark crossing gap. |
| SKIP-PO-008 | 14-day grace state | Business rules | No document state enum/Grace status sampled. | Force expired doc; no Grace state visible. |
| SKIP-PO-009 | Automatic suspension after grace | User/business rules | No separate `SuspensionService` sampled; manual admin suspend exists. | Expired >14d does not auto-suspend unless another background service exists. |
| SKIP-PO-010 | SLA warning/escalation service | User/business rules | No dedicated SLA background service sampled. | Pending >7d likely only filterable by date, no escalation event. |
| SKIP-PO-011 | Agency approval requires attached guide | User/business rule | Sampled domain approval does not check guide count. | Approve Agency with zero guides may succeed. |
| SKIP-PO-012 | Phase-1 4-provider taxonomy exactly as labels | Cross-document resolution/user context | Current enum has 6 values; Freelance Activity Instructor missing. | UI/API option mismatch should be logged. |
| SKIP-PO-013 | Dedicated admin application detail endpoint | Test need | Sampled admin endpoints expose queue/actions but no `GET /admin/providers/{id}`. | Detail route likely 404; queue may contain enough data. |
| SKIP-PO-014 | Admin clock-skew endpoint | User note | Not sampled. | If route absent, use DB direct updates for time travel. |

## 3. DEFERRED

| ID | Deferred item | Source | Test treatment |
|---|---|---|---|
| DEF-PO-001 | Automated document verification by AI/third-party | Workflow Future Extensions | No active Playwright scenario except asserting manual review remains required. |
| DEF-PO-002 | Provider analytics dashboard beyond overview/pending-actions/notifications/settings | Workflow Future Extensions | Covered in analytics/provider-dashboard future suite. |
| DEF-PO-003 | Provider subscription tiers / premium features | Workflow Future Extensions | Finance/product suite, not onboarding MVP. |
| DEF-PO-004 | Provider referral program | Workflow Future Extensions | No route expected. |
| DEF-PO-005 | Provider mobile app / push UX | Workflow Future Extensions | Messaging/mobile future suite. |
| DEF-PO-006 | Configurable document requirements | Workflow decision says hardcoded for MVP | Test current hardcoded requirements only. |

## 4. Integration Events

### Publishes

- `ProviderRegisteredIntegrationEvent` — TC-PO-004.
- `ProviderStatusChangedIntegrationEvent` for all transitions: None→Draft, Draft/MoreDocsNeeded→Pending, Pending→Approved/Rejected/MoreDocsNeeded, Approved→Suspended, Suspended→Approved — TC-PO-004, 012, 028, 030, 032, 033, 047.
- `ProviderApprovedIntegrationEvent` — TC-PO-028, TC-PO-053.
- `ProviderRejectedIntegrationEvent` — TC-PO-030, TC-PO-036.
- `ProviderSuspendedIntegrationEvent` — admin/manual suspension and expected doc-expiry suspension once built; TC-PO-054.
- `ProviderReinstatedIntegrationEvent` — TC-PO-047.
- `ProviderDocumentExpiringIntegrationEvent` — TC-PO-041.
- `AgencyAffiliationCreatedIntegrationEvent` / `AgencyAffiliationTerminatedIntegrationEvent` — used by agency roster tests (only dependency for TC-PO-051).

### User-requested / business-event names to track

- `ProviderApplied` maps to current generic `ProviderStatusChanged(NewStatus=Pending)`; no dedicated submitted event.
- `ProviderApproved` maps to `ProviderApprovedIntegrationEvent`.
- `ProviderRejected` maps to `ProviderRejectedIntegrationEvent`.
- `ProviderSuspended` maps to `ProviderSuspendedIntegrationEvent` for manual/admin suspension; doc-expiry auto path appears NOT_BUILT.
- `DocumentExpiringSoon` maps to `ProviderDocumentExpiringIntegrationEvent`.
- `DocumentExpired` appears NOT_BUILT in sampled code.

### Consumes

- `UserRegistered` / email verification lifecycle: eligibility tracking is indirect; actual role path is Guest → User via `EmailVerifiedIntegrationEvent` from Security.Contracts per plan implementation notes.
- `ProviderApprovedIntegrationEvent` consumed by Security (role grant), ContentTours (TourGuide profile for IndependentGuide), Messaging (approval notification), ContentBlogs (link CreatorProfile).
- `ProviderSuspendedIntegrationEvent` should be consumed downstream by ContentTours/Booking/Finance/Social/Messaging for cascade; CrossDocumentAnalysisReport says cascade is partially resolved, so each module suite must verify its own consumer.

## 5. Validation Matrix

| Area | Rules to assert | Expected status/code |
|---|---|---|
| Provider register shape | BusinessName required <=200; ContactEmail valid <=256; ContactPhone required <=30; Address required <=500; Description required <=2000; Type enum valid | 400/422 validation |
| Eligibility | OTP/email-verified User role required; one active provider application; not already Approved/Suspended unless admin flow | 401/403/409/422 |
| Provider type | Product 4 labels vs code 6 enum values; invalid enum rejected | 400/422; taxonomy divergence logged |
| Required documents | Type-specific hardcoded required document set must be complete before submit | 422 MissingRequiredDocuments |
| File type | PDF/JPG/PNG only; TXT rejected | Expected 400/422; current API likely NOT_BUILT for metadata path |
| File size | User requirement max 5MB; code max 10MB | >5MB should reject by business rule; current 5-10MB divergence; >10MB rejects |
| Document count | Max 10 docs/application | 422 TooManyDocuments |
| Duplicate document type | One document per type unless replacing existing by ID | 409/422 DuplicateDocumentType |
| Expiry field | Accept valid nullable/future date; preserve on replace; warning at 30d | 201/200; warning event at threshold |
| Submit | Draft or MoreDocsNeeded only, all docs present, cooling period inactive | 200 Pending or 422 invalid |
| Admin approve | Pending only; Agency needs guide if rule implemented | 200 or 422 invalid |
| Admin reject | Pending only, reason required, sets cooling + reapply count | 200 or validation |
| Reapply | Rejected only, after cooling, count <3 | 200 Draft or 422 |
| Suspension | Approved only; reinstatement Suspended only | 200 or 422 invalid |

## 6. Auth Matrix (Anonymous=401, User can apply/view own, Admin can review/decide)

| Route group | Anonymous | Verified User (`userA`) | Pending/Approved Provider | Admin |
|---|---:|---:|---:|---:|
| `GET /api/v1/provider/status` | 401 | ✅ own only | ✅ own only | ⚠️ own/admin only depending handler |
| `POST /api/v1/provider/register` | 401 | ✅ if no active app | ❌ 409 active/approved/suspended | ⚠️ only if applying as self; admin review separate |
| `POST /api/v1/provider/documents` | 401 | ✅ own Draft/MoreDocsNeeded | ⚠️ replace/update own docs if allowed | ⚠️ not admin route |
| `PUT /api/v1/provider/documents/{id}` | 401 | ✅ own only | ✅ own only if state allows | ⚠️ not admin route |
| `POST /api/v1/provider/apply` | 401 | ✅ own Draft/MoreDocsNeeded | ❌ invalid if Approved/Suspended | ⚠️ not admin route |
| `POST /api/v1/provider/reapply` | 401 | ✅ own Rejected after cooling | ❌ invalid if not Rejected | ⚠️ not admin route |
| `GET /api/v1/provider/dashboard/*` | 401 | ❌/404 unless approved | ✅ approved/suspended status visible | ✅ only if permission assigned |
| `GET /api/v1/admin/providers` | 401 | 403 | 403 | ✅ |
| Admin approve/reject/request-docs/suspend/reinstate | 401 | 403 | 403 | ✅ |
| Public agency list/detail `/api/v1/agency` | ✅ | ✅ | ✅ | ✅ |
| Agency roster mutations | 401 | 403 | ✅ Agency only | ✅ if permission assigned |

## 7. State Machine — Provider Application (None → Pending → Approved/Rejected/MoreDocsNeeded → Suspended)

| From | Trigger | To | Scenario |
|---|---|---|---|
| None | `RegisterProviderCommand` | Draft | TC-PO-004 |
| Draft | `SubmitApplicationCommand` with missing docs | Draft | TC-PO-011 |
| Draft | `SubmitApplicationCommand` with required docs | Pending | TC-PO-012 |
| Pending | `ApproveProviderCommand` | Approved | TC-PO-028 |
| Pending | `RejectProviderCommand` | Rejected | TC-PO-030 |
| Pending | `RequestMoreDocsCommand` | MoreDocsNeeded | TC-PO-032 |
| MoreDocsNeeded | add/replace docs + submit | Pending | TC-PO-033 |
| Rejected | `ReapplyProviderCommand` during cooling | Rejected | TC-PO-036 |
| Rejected | `ReapplyProviderCommand` after cooling and count <3 | Draft | TC-PO-037 |
| Rejected | reapply when count >=3 | Rejected | TC-PO-038 |
| Approved | `SuspendProviderCommand` / future doc-expiry auto suspend | Suspended | TC-PO-054 / SKIP-PO-009 |
| Suspended | `ReinstateProviderCommand` | Approved | TC-PO-047 |

## 8. State Machine — Document (Active → ExpiringSoon → Expired → Grace → Replaced/AutoRemoved)

| From | Trigger | To | Scenario | Current implementation status |
|---|---|---|---|---|
| Uploaded/Active | Add document with valid future expiry | Active | TC-PO-014..015 | Built as metadata fields; no explicit status enum. |
| Active | `ExpiresAt <= now+30d` daily watcher | ExpiringSoon | TC-PO-041 | Built as `ProviderDocumentExpiringIntegrationEvent`; no persisted document state. |
| Active | `ExpiresAt > now+30d` | Active | TC-PO-042 | Built/no event. |
| ExpiringSoon | Provider replaces doc | Active/Replaced | TC-PO-021, TC-PO-046 | Replace built; no explicit state clearing beyond new expiry. |
| Active/ExpiringSoon | `ExpiresAt < now` | Expired | TC-PO-043 | Detection/logging only in sampled service; event/status NOT_BUILT. |
| Expired | within 14 days | Grace | TC-PO-044 | NOT_BUILT as explicit state. |
| Grace | replace valid doc | Replaced/Active | TC-PO-046 | Replacement built; grace semantics NOT_BUILT. |
| Grace | expired >14 days | AutoRemoved/ProviderSuspended | TC-PO-045 | Auto suspension NOT_BUILT in sampled code. |

## 9. Background Services Test

| ID | Service | MCP/time-travel steps | Assertions |
|---|---|---|---|
| BG-PO-001 | `ProviderDocumentExpiryService` daily — expiring soon | Set approved provider doc `ExpiresAt=now+29d`; run service tick or wait; inspect outbox/notifications. | One `ProviderDocumentExpiringIntegrationEvent`; dashboard notifications include doc; duplicate tick should not spam if idempotency implemented (verify; current service may duplicate without sent marker). |
| BG-PO-002 | `ProviderDocumentExpiryService` daily — outside threshold | Set `ExpiresAt=now+31d`; run tick. | No event. |
| BG-PO-003 | `ProviderDocumentExpiryService` daily — expired | Set `ExpiresAt=now-1d`; run tick. | Log warning; no `DocumentExpired` event in sampled code — NOT_BUILT/divergence. |
| BG-PO-004 | SuspensionService daily — grace exceeded | Set approved provider doc expired by 15+ days; run expected suspension service. | Expected business behavior: provider Suspended, `ProviderSuspended` event. Current sampled code lacks service; mark NOT_BUILT unless discovered at runtime. |
| BG-PO-005 | Agency invitation expiry service dependency | Seed agency invite expired >7d; wait/poll. | Agency invitation expiry is adjacent onboarding flow; expired invites should not count as attached guide for Agency approval. |
| BG-PO-006 | 7-day admin review SLA | Set Pending `SubmittedAt=now-8d`; run any SLA watcher/admin queue. | Expected warning/escalation. If no watcher/field, mark NOT_BUILT. |

## 10. Known Divergence (per CrossDocumentAnalysisReport.md provider taxonomy resolution)

- Provider type taxonomy was resolved to a unified enum with `BusinessOwner=5`, but user-facing Phase 1 says 4 provider types: Independent Tour Guide, Business Owner, Agency, Freelance Activity Instructor. Current code has 6 enum values and lacks `FreelanceActivityInstructor`; tests must flag UI/API label mismatches.
- Workflow plan originally says max document size 10MB, while user requirement says 5MB. Current code enforces 10MB; `>5MB` rejection is a business-rule divergence to catch.
- User document type examples (`IdCard`, `BusinessRegistration`, `GuideLicense`) differ from current enum (`GovernmentId`, `BusinessLicense`, `MotaLicense`). UI may alias them, but API tests must use actual enum names.
- Current code uses generic `ProviderStatusChangedIntegrationEvent` for Submitted and MoreDocsNeeded, not dedicated events.
- Document expiry → ContentPlaces business suspension is a documented crossing gap: expiring warnings exist, but expired/grace/suspension cascade appears incomplete in sampled code.
- Provider suspension cascade is only partially resolved across downstream modules. Onboarding tests should verify Accounts status/events; ContentTours/ContentPlaces/Booking/Finance/Social suites must verify their own reactions.
- Route mounting should be smoke-tested first. `AccountsEndpoints.cs` and endpoint classes both define absolute route groups; if runtime routes are double-prefixed, all API scenarios are blocked until route mapping is fixed.
