# Playwright MCP Test Scenarios — TourGuide Module

> **API-ONLY MODE.** This checkout has no `YallaJo.Web`. Every `browser_navigate("https://localhost:57065/swagger…")` step below is a docking step; the actual request runs through `window.__yj.apiFetch(...)` defined in [`Playwright-APIOnly-Adapter.md`](./Playwright-APIOnly-Adapter.md). Read that adapter once at the start of every Playwright session — it also lists the 8 seeded test users and their credentials.

## Source Plans
- Workflow: Plans/TourGuide-Flow.md
- Audit: Plans/TourGuide-Flow-Audit-Report.md
- Fix Plan: Plans/TourGuide-Flow-FixPlan.md
- Module score (per Master-RoadmapTo10.md): 9.0/10 current → 10.0 target; TourGuide-Flow W3-C is HIGH-priority polish despite roadmap table risk LOW.
- Actual code sampled: `ContentTours.Application`, `ContentTours.Domain`, `ContentTours.Infrastructure`, `ContentTours.Presentation/Endpoints/TourGuide`. Requested `src/TourGuide.*` and `YallaJo.Web/Areas/TourGuide` paths are not present; TourGuide lives under ContentTours and no Razor Area exists yet.
- Audit divergence: original audit said GuideOffering endpoints/events were missing, but current code contains GuideOffering endpoints, GuideApplication/TourProposal domain events, integration events, validators, and handlers. Legacy `TourTourGuide` still exists.

## 0. Prerequisites
- App running at https://localhost:57065/swagger.
- API running at https://localhost:57065; Swagger at `/swagger`.
- DB migrated (`Update-Database` from `YallaJo.Api/`). Fix local SQL connection first: use a single `MOHAMMAD\SQLEXPRESS`, not `MOHAMMAD\SQLEXPRESS\\SQLEXPRESS`.
- Seed users present: `admin@yallajo.test`, `userA@yallajo.test`, `userB@yallajo.test`, `guide-pending@yallajo.test`, `guide-approved@yallajo.test`, `business@yallajo.test`, `agency@yallajo.test`, `suspended@yallajo.test`; password `TestPass!23`.
- Recaptcha disabled globally; skip recaptcha assertions.
- SignalR/notifications background processors enabled for outbox/inbox validation; CompositeOutboxProcessor running.
- ContentPlaces seed has at least one active Place. ContentTours seed has at least one Approved tour open for guide applications, one Closed tour, one Active `TourGuide`, one Suspended `TourGuide`, one `GuideTourOffering`, language and specialization IDs.
- File storage configured if avatar/cover URL endpoints later move to uploads; current endpoints accept URL strings.
- Use browser pages as the user-facing entry point when available. Because `YallaJo.Web/Areas/TourGuide` is absent, API-backed scenarios use Playwright network/evaluate calls against `https://localhost:57065/api/v1/...` while still starting at `https://localhost:57065/swagger`.

## 1. Built — Active Test Scenarios

### TC-TG-001: Public list of tour guides
- **Endpoint:** GET `/api/v1/guides?page=1&pageSize=20`
- **Command/Query/Handler:** `ListTourGuidesQuery` / `ListTourGuidesQueryHandler` in `ContentTours.Application/Queries/TourGuides/ListPublic/`
- **Workflow Ref:** §Part 2 lines 623-631; §Part 3 Overview KPI guide discovery; audit §2.2 (now built)
- **Audit Status:** Built
- **Role(s):** Anonymous, Customer, Guide, Admin
- **Preconditions:** At least one active approved guide and one suspended/deactivated guide.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_snapshot()`
  3. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides?page=1&pageSize=20').then(r => ({status:r.status, body:r.json()}))" })`
  4. `mcp__playwright__browser_network_requests()` — capture `GET /api/v1/guides`.
- **Assertions:**
  - HTTP: 200 with paginated result: `items`, `page`, `pageSize`, `totalCount` or equivalent result shape.
  - DOM/network: no redirect to login; no 401.
  - DB/event side-effects: none; query is read-only and cacheable.
- **Edge Cases:** `pageSize=0` returns 400; `pageSize>100` returns 400/clamped; inactive/suspended guides excluded; anonymous still allowed.

### TC-TG-002: Public guide profile by slug
- **Endpoint:** GET `/api/v1/guides/by-slug/{slug}`
- **Command/Query/Handler:** `GetTourGuideBySlugQuery` / `GetTourGuideBySlugQueryHandler` in `ContentTours.Application/Queries/TourGuides/GetBySlug/`
- **Workflow Ref:** §Part 2 lines 625-631; audit §2.2
- **Audit Status:** Built
- **Role(s):** Anonymous
- **Preconditions:** `guide-approved` has unique slug.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/by-slug/guide-approved').then(async r => ({status:r.status, body:await r.json()}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:**
  - HTTP: 200 with `id`, `slug`, `displayName`, `bio`, `averageRating`, `languages`, `specializations`.
  - DB/event side-effects: none.
- **Edge Cases:** unknown slug 404; uppercase slug normalizes or 404 consistently; suspended guide not public; slug with invalid chars returns 400/404 without 500.

### TC-TG-003: Public guide profile by GUID
- **Endpoint:** GET `/api/v1/guides/{id}`
- **Command/Query/Handler:** `GetTourGuideByIdQuery` / `GetTourGuideByIdQueryHandler` in `ContentTours.Application/Queries/TourGuides/GetById/`
- **Workflow Ref:** §Part 2 lines 652-660
- **Audit Status:** Built
- **Role(s):** Anonymous
- **Preconditions:** Known approved guide ID.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/{guideId}').then(async r => ({status:r.status, body:await r.json()}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 200 profile DTO; no private admin-only fields leaked (`reportCount`, `suspensionReason` unless intentionally public); 404 for missing GUID.
- **Edge Cases:** empty GUID 400; soft-deleted guide 404; suspended guide hidden/403 per policy; cache invalidates after profile update.

### TC-TG-004: Guide gets own profile
- **Endpoint:** GET `/api/v1/guides/me`
- **Command/Query/Handler:** `GetTourGuideByIdQuery` or self wrapper endpoint in `TourGuideProfileEndpoints.cs`
- **Workflow Ref:** §Part 2 lines 632-640
- **Audit Status:** Built
- **Role(s):** Approved guide only
- **Preconditions:** Login as `guide-approved@yallajo.test`.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/Auth/Login" })`
  2. `mcp__playwright__browser_fill_form({ fields: [{ name: "Email", type: "textbox", value: "guide-approved@yallajo.test" }, { name: "Password", type: "textbox", value: "TestPass!23" }] })`
  3. `mcp__playwright__browser_click({ element: "Sign in", ref: "button" })`
  4. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/me', { credentials:'include' }).then(async r => ({status:r.status, body:await r.json()}))" })`
  5. `mcp__playwright__browser_network_requests()`
- **Assertions:** HTTP 200; DTO includes private guide-owned fields; customer receives 403; anonymous 401.
- **Edge Cases:** pending guide 403/404; suspended provider 403; JWT expiry redirects or 401; profile not found returns 404 not 500.

### TC-TG-005: Guide updates own public profile by GUID
- **Endpoint:** PUT `/api/v1/guides/{id}`
- **Command/Query/Handler:** `UpdateTourGuideProfileCommand` / `UpdateTourGuideProfileCommandHandler` in `ContentTours.Application/Commands/TourGuides/UpdateProfile/`
- **Workflow Ref:** §Part 2 lines 632-660; handler changes lines 664-676
- **Audit Status:** Built
- **Role(s):** OwnGuide; Admin if permissioned
- **Preconditions:** Login as `guide-approved`; know own guide ID.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/{ownGuideId}', { method:'PUT', credentials:'include', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ displayName:'Approved Guide QA', bio:'Licensed QA guide in Jordan', yearsOfExperience:7, hasFirstAid:true, moTALicenseNumber:'MOTA-QA-7' }) }).then(async r => ({status:r.status, body: r.status===204 ? null : await r.text()}))" })`
  3. `mcp__playwright__browser_wait_for({ text: "" })`
  4. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/200 success; subsequent GET shows updated values; `TourGuideUpdatedDomainEvent` stages `TourGuideUpdatedIntegrationEvent` outbox row.
- **Edge Cases:** other guide ID returns 403; displayName empty 400; bio too long 400; years negative 400; suspended guide cannot update.

### TC-TG-006: Guide updates avatar URL
- **Endpoint:** PUT `/api/v1/guides/me/avatar`
- **Command/Query/Handler:** `UpdateGuideAvatarCommand` / `UpdateGuideAvatarCommandHandler`
- **Workflow Ref:** §Part 2 lines 632-640
- **Audit Status:** Built
- **Role(s):** OwnGuide
- **Preconditions:** Login as `guide-approved`.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/me/avatar', {method:'PUT', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ avatarUrl:'https://cdn.yallajo.test/guides/avatar.jpg' })}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/200; profile returns new `avatarUrl`; cache tag for guide profile invalidated.
- **Edge Cases:** invalid URL 400; empty URL policy consistent; anonymous 401; customer 403; huge URL >500 chars 400.

### TC-TG-007: Guide updates cover image URL
- **Endpoint:** PUT `/api/v1/guides/me/cover-image`
- **Command/Query/Handler:** `UpdateGuideCoverImageCommand` / `UpdateGuideCoverImageCommandHandler`
- **Workflow Ref:** §Part 2 lines 632-640
- **Audit Status:** Built
- **Role(s):** OwnGuide
- **Preconditions:** Login as approved guide.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/me/cover-image', {method:'PUT', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ coverImageUrl:'https://cdn.yallajo.test/guides/cover.jpg' })}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/200; updated public profile; no file upload side effect expected.
- **Edge Cases:** malformed URL 400; overlong URL 400; suspended guide 403; unauthorized 401.

### TC-TG-008: Guide self-deactivates profile
- **Endpoint:** DELETE `/api/v1/guides/me`
- **Command/Query/Handler:** `DeactivateTourGuideCommand` / `DeactivateTourGuideCommandHandler`
- **Workflow Ref:** §Part 2 lines 632-640; status enum lines 600-608
- **Audit Status:** Built
- **Role(s):** OwnGuide
- **Preconditions:** Use disposable approved guide seed; no active bookings if domain blocks deletion.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/me', {method:'DELETE', credentials:'include'}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/200; status becomes `Deactivated`; `DeletedAt` set; public GET no longer shows guide; outbox publishes `TourGuideDeactivatedIntegrationEvent` if registered.
- **Edge Cases:** pending guide 403; active future bookings conflict 409 if implemented; double delete idempotent 204 or 409; admin can still audit.

### TC-TG-009: Add guide language
- **Endpoint:** POST `/api/v1/guides/{id}/languages`
- **Command/Query/Handler:** `AddTourGuideLanguageCommand` / `AddTourGuideLanguageCommandHandler`
- **Workflow Ref:** §Part 2 existing endpoints lines 652-660
- **Audit Status:** Built
- **Role(s):** OwnGuide
- **Preconditions:** Login as approved guide; seed language ID exists.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/{ownGuideId}/languages', {method:'POST', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ languageId:'{languageId}', proficiency:'Fluent' })}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 201/204; language appears on profile; `TourGuideLanguageAddedIntegrationEvent` outbox row.
- **Edge Cases:** duplicate language 409; invalid proficiency 400; last-language removal guarded in TC-TG-010; other guide 403; unknown language 404.

### TC-TG-010: Remove guide language
- **Endpoint:** DELETE `/api/v1/guides/{id}/languages/{languageId}`
- **Command/Query/Handler:** `RemoveTourGuideLanguageCommand` / `RemoveTourGuideLanguageCommandHandler`
- **Workflow Ref:** §Part 2 lines 652-660
- **Audit Status:** Built
- **Role(s):** OwnGuide
- **Preconditions:** Approved guide has at least two languages.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/{ownGuideId}/languages/{languageId}', {method:'DELETE', credentials:'include'}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204; language removed; `TourGuideLanguageRemovedIntegrationEvent` outbox row.
- **Edge Cases:** removing non-existing language 404; removing last language 400/409; other guide 403; anonymous 401.

### TC-TG-011: Add guide specialization
- **Endpoint:** POST `/api/v1/guides/{id}/specializations`
- **Command/Query/Handler:** `AddTourGuideSpecializationCommand` / `AddTourGuideSpecializationCommandHandler`
- **Workflow Ref:** §Part 2 lines 652-660
- **Audit Status:** Built
- **Role(s):** OwnGuide
- **Preconditions:** Seed specialization ID exists.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/{ownGuideId}/specializations', {method:'POST', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ specializationId:'{specializationId}' })}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 201/204; profile includes specialization; outbox `TourGuideSpecializationAddedIntegrationEvent`.
- **Edge Cases:** duplicate 409; unknown specialization 404; other guide 403; suspended guide 403.

### TC-TG-012: Guide views own applications
- **Endpoint:** GET `/api/v1/guides/me/applications?page=1&pageSize=20`
- **Command/Query/Handler:** `GetMyGuideApplicationsQuery` / `GetMyGuideApplicationsQueryHandler`
- **Workflow Ref:** §Part 2 lines 652-660; §Part 3 My Applications lines 925-938
- **Audit Status:** Built
- **Role(s):** OwnGuide
- **Preconditions:** Guide has at least one application.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/me/applications?page=1&pageSize=20', {credentials:'include'}).then(async r => ({status:r.status, body:await r.json()}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 200 paginated list; statuses include Draft/Submitted/Approved/Rejected; no other guide applications leaked.
- **Edge Cases:** no applications returns empty list; customer 403; page too large 400/clamped; suspended guide 403.

### TC-TG-013: Public list guide tours
- **Endpoint:** GET `/api/v1/guides/{id}/tours?page=1&pageSize=20`
- **Command/Query/Handler:** `GetGuideToursQuery` / `GetGuideToursQueryHandler`
- **Workflow Ref:** §Part 2 lines 652-660; §Part 3 My Tours lines 806-831
- **Audit Status:** Built
- **Role(s):** Anonymous
- **Preconditions:** Guide has active offering(s).
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/{guideId}/tours?page=1&pageSize=20').then(async r => ({status:r.status, body:await r.json()}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 200; only active approved tours/offerings shown; suspended offering omitted.
- **Edge Cases:** unknown guide 404; guide with no tours empty; suspended guide hidden; pagination validation.

### TC-TG-014: Admin reads guide private profile
- **Endpoint:** GET `/api/v1/guides/admin/{id}`
- **Command/Query/Handler:** `GetTourGuideByIdQuery` admin endpoint path in `AdminTourGuideEndpoints.cs`
- **Workflow Ref:** §Part 2 lines 642-650
- **Audit Status:** Built
- **Role(s):** Admin
- **Preconditions:** Login as `admin@yallajo.test`.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/Auth/Login" })`
  2. `mcp__playwright__browser_fill_form({ fields: [{ name:"Email", type:"textbox", value:"admin@yallajo.test" }, { name:"Password", type:"textbox", value:"TestPass!23" }] })`
  3. `mcp__playwright__browser_click({ element:"Sign in", ref:"button" })`
  4. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/admin/{guideId}', {credentials:'include'}).then(async r => ({status:r.status, body:await r.json()}))" })`
  5. `mcp__playwright__browser_network_requests()`
- **Assertions:** 200; includes admin-visible status/suspension/report data; customer gets 403.
- **Edge Cases:** anonymous 401; non-admin guide 403; missing guide 404; deleted guide visible only if intended.

### TC-TG-015: Admin suspends guide profile
- **Endpoint:** POST `/api/v1/guides/admin/{id}/suspend`
- **Command/Query/Handler:** `SuspendTourGuideCommand` / `SuspendTourGuideCommandHandler`
- **Workflow Ref:** §Part 2 lines 578-582 and 642-650
- **Audit Status:** Built
- **Role(s):** Admin
- **Preconditions:** Active guide with no irreversible state.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/admin/{guideId}/suspend', {method:'POST', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ reason:'QA suspension' })}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_wait_for({ text: "" })`
  4. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/200; status `Suspended`; `SuspendedAt`, `SuspendedByAdminId`, `SuspensionReason` set; outbox `TourGuideSuspendedIntegrationEvent` published.
- **Edge Cases:** blank reason 400; already suspended idempotent or 409; self-suspend admin hierarchy if applicable; guide cannot suspend self via admin route.

### TC-TG-016: Admin reinstates suspended guide
- **Endpoint:** POST `/api/v1/guides/admin/{id}/reinstate`
- **Command/Query/Handler:** `ReinstateTourGuideCommand` / `ReinstateTourGuideCommandHandler`
- **Workflow Ref:** §Part 2 lines 578-582 and 642-650
- **Audit Status:** Built
- **Role(s):** Admin
- **Preconditions:** Guide is Suspended.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/admin/{guideId}/reinstate', {method:'POST', credentials:'include'}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/200; status Active; suspension fields cleared; `TourGuideActivatedIntegrationEvent` or reinstated equivalent published.
- **Edge Cases:** active guide 409/idempotent; deactivated guide cannot reinstate; non-admin 403; missing guide 404.

### TC-TG-017: Admin updates guide fields
- **Endpoint:** PUT `/api/v1/guides/admin/{id}`
- **Command/Query/Handler:** `AdminUpdateTourGuideCommand` / `AdminUpdateTourGuideCommandHandler`
- **Workflow Ref:** §Part 2 lines 642-650; audit said missing, current code built
- **Audit Status:** Built
- **Role(s):** Admin
- **Preconditions:** Login as admin.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/admin/{guideId}', {method:'PUT', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ displayName:'Admin Edited Guide', bio:'Admin QA update', yearsOfExperience:8, hasFirstAid:true, moTALicenseNumber:'MOTA-ADMIN' })}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/200; GET admin returns updated fields; audit/event emitted if configured.
- **Edge Cases:** invalid field lengths 400; non-admin 403; stale row version if used returns 409; deactivated guide update blocked or allowed per policy.

### TC-TG-018: Admin deactivates guide
- **Endpoint:** DELETE `/api/v1/guides/admin/{id}`
- **Command/Query/Handler:** `AdminDeactivateTourGuideCommand` / `AdminDeactivateTourGuideCommandHandler`
- **Workflow Ref:** §Part 2 lines 642-650; audit said missing, current code built
- **Audit Status:** Built
- **Role(s):** Admin
- **Preconditions:** Disposable active guide.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/admin/{guideId}', {method:'DELETE', credentials:'include'}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/200; guide no longer public; status Deactivated; cleanup eligibility after 60 days.
- **Edge Cases:** non-admin 403; active bookings conflict; double delete idempotent/409; missing guide 404.

### TC-TG-019: Tour owner/admin opens tour for applications
- **Endpoint:** POST `/api/v1/tours/{tourId}/open-applications`
- **Command/Query/Handler:** `OpenTourForApplicationsCommand` / `OpenTourForApplicationsCommandHandler`
- **Workflow Ref:** §Tour Modifications lines 345-350
- **Audit Status:** Built
- **Role(s):** Tour owner, Admin
- **Preconditions:** Approved tour with `IsOpenForApplications=false`.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/open-applications', {method:'POST', credentials:'include'}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/200; tour `IsOpenForApplications=true`; guide application POST becomes allowed.
- **Edge Cases:** non-owner business 403; draft/rejected tour invalid state 400; missing tour 404; already open idempotent or 409.

### TC-TG-020: Tour owner/admin closes tour applications
- **Endpoint:** POST `/api/v1/tours/{tourId}/close-applications`
- **Command/Query/Handler:** `CloseTourForApplicationsCommand` / `CloseTourForApplicationsCommandHandler`
- **Workflow Ref:** §Tour Modifications lines 345-350
- **Audit Status:** Built
- **Role(s):** Tour owner, Admin
- **Preconditions:** Approved tour open for applications.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/close-applications', {method:'POST', credentials:'include'}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/200; `IsOpenForApplications=false`; new guide application blocked.
- **Edge Cases:** pending applications remain reviewable; already closed idempotent/409; non-owner 403; missing tour 404.

### TC-TG-021: Guide applies to run existing tour
- **Endpoint:** POST `/api/v1/tours/{tourId}/applications`
- **Command/Query/Handler:** `ApplyForTourCommand` / `ApplyForTourCommandHandler`
- **Workflow Ref:** §Guide Application endpoints lines 307-318; entities lines 65-93
- **Audit Status:** Built
- **Role(s):** Approved guide
- **Preconditions:** Tour open for applications; guide not already assigned and has no pending duplicate.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/applications', {method:'POST', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ message:'I have Petra route experience', proposedScheduleJson:'[]', relevantExperience:'7 years', proposedPricingNotes:'Adult tier 40 JOD' })}).then(async r => ({status:r.status, body:await r.text()}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 201 with application ID; status Submitted or Draft per implementation; `GuideApplicationSubmittedDomainEvent` and `NewGuideApplicationIntegrationEvent`/submitted event staged.
- **Edge Cases:** duplicate pending application 409; closed tour 400/409; guide already offering tour 409; suspended guide 403; message >2000 chars 400.

### TC-TG-022: Owner/admin lists guide applications for a tour
- **Endpoint:** GET `/api/v1/tours/{tourId}/applications`
- **Command/Query/Handler:** `ListGuideApplicationsQuery` / handler in `ContentTours.Application/Queries/GuideApplication`
- **Workflow Ref:** §Guide Application endpoints lines 307-318
- **Audit Status:** Built
- **Role(s):** Tour owner, Admin
- **Preconditions:** Tour has applications from multiple guides.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/applications', {credentials:'include'}).then(async r => ({status:r.status, body:await r.json()}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 200 list; owner sees own tour only; admin sees all; applicant cannot list all unless owner/admin.
- **Edge Cases:** customer 403; non-owner guide 403; no applications empty; pagination filters if present.

### TC-TG-023: Owner/admin approves guide application
- **Endpoint:** POST `/api/v1/tours/{tourId}/applications/{applicationId}/approve`
- **Command/Query/Handler:** `ApproveGuideApplicationCommand` / `ApproveGuideApplicationCommandHandler`
- **Workflow Ref:** §GuideApplication state machine lines 85-92
- **Audit Status:** Built
- **Role(s):** Tour owner, Admin
- **Preconditions:** Application is Submitted; tour open/approved.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/applications/{applicationId}/approve', {method:'POST', credentials:'include'}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/200; application `Approved`; `GuideTourOffering` row created Active with ApplicationId; `GuideApplicationApprovedIntegrationEvent` published.
- **Edge Cases:** approving draft/rejected 400; duplicate offering conflict 409; non-owner 403; application tour mismatch 404/400.

### TC-TG-024: Owner/admin rejects guide application
- **Endpoint:** POST `/api/v1/tours/{tourId}/applications/{applicationId}/reject`
- **Command/Query/Handler:** `RejectGuideApplicationCommand` / `RejectGuideApplicationCommandHandler`
- **Workflow Ref:** §GuideApplication state machine lines 85-92
- **Audit Status:** Built
- **Role(s):** Tour owner, Admin
- **Preconditions:** Application is Submitted.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/applications/{applicationId}/reject', {method:'POST', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ reason:'Schedule does not match tour needs' })}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/200; status Rejected; `RejectionReason` set; `GuideApplicationRejectedIntegrationEvent` outbox row.
- **Edge Cases:** blank reason 400; already approved cannot reject 400/409; applicant cannot reject own unless owner; rejected resubmission limit max 2.

### TC-TG-025: Guide creates tour proposal
- **Endpoint:** POST `/api/v1/tours/proposals`
- **Command/Query/Handler:** `CreateTourProposalCommand` / `CreateTourProposalCommandHandler`
- **Workflow Ref:** §Tour Proposal endpoints lines 319-331; proposal entity lines 103-137
- **Audit Status:** Built
- **Role(s):** Approved guide
- **Preconditions:** Active Place ID exists.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/proposals', {method:'POST', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ title:'QA Petra Sunrise Walk', description:'Detailed guided sunrise experience', shortDescription:'Sunrise walk', placeId:'{placeId}', difficulty:1, durationMinutes:180, maxGroupSize:10, basePrice:35, currency:'JOD', requestExclusive:true, proposalNotes:'Unique early route' })}).then(async r => ({status:r.status, body:await r.text()}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 201 with proposal ID; status Draft/Submitted per implementation; Place existence validated.
- **Edge Cases:** missing PlaceId 400; deleted/nonexistent place 422/404; invalid currency 400; duration <=0 400; suspended guide 403.

### TC-TG-026: Admin/guide lists tour proposals
- **Endpoint:** GET `/api/v1/tours/proposals`
- **Command/Query/Handler:** `ListTourProposalsQuery` / handler in TourProposal queries
- **Workflow Ref:** §Tour Proposal endpoints lines 319-331
- **Audit Status:** Built
- **Role(s):** Admin; guide sees own if handler scopes
- **Preconditions:** Several proposals in Draft/Submitted/Approved/Rejected.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/proposals', {credentials:'include'}).then(async r => ({status:r.status, body:await r.json()}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 200 list; guide cannot see other guides' proposals unless admin; filter/status if supported works.
- **Edge Cases:** anonymous 401; customer 403; empty list OK; invalid page 400.

### TC-TG-027: Guide submits tour proposal
- **Endpoint:** POST `/api/v1/tours/proposals/{id}/submit`
- **Command/Query/Handler:** `SubmitTourProposalCommand` / `SubmitTourProposalCommandHandler`
- **Workflow Ref:** §TourProposal state machine lines 129-136
- **Audit Status:** Built
- **Role(s):** Proposal owner guide
- **Preconditions:** Proposal Draft; Place still active.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/proposals/{proposalId}/submit', {method:'POST', credentials:'include'}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/200; status Submitted; `SubmittedAt` set; `TourProposalSubmittedIntegrationEvent` outbox row.
- **Edge Cases:** submitting already submitted 409; incomplete proposal 400; place deleted since create 422; non-owner 403.

### TC-TG-028: Admin approves tour proposal and creates tour/offering
- **Endpoint:** POST `/api/v1/tours/proposals/{id}/approve`
- **Command/Query/Handler:** `ApproveTourProposalCommand` / `ApproveTourProposalCommandHandler`
- **Workflow Ref:** §TourProposal lines 103-137; ownership decisions lines 28-42
- **Audit Status:** Built
- **Role(s):** Admin
- **Preconditions:** Proposal Submitted; Place active.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/proposals/{proposalId}/approve', {method:'POST', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ isExclusive:true })}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/200; proposal Approved; `CreatedTourId` set; new Tour has `OwnershipType=GuideProposed`, `ProposedByGuideId`, `IsExclusive` as chosen; proposer offering Active; `TourProposalApprovedIntegrationEvent` published.
- **Edge Cases:** non-admin 403; already approved 409; deleted place blocks approval; race/double approval yields one tour only.

### TC-TG-029: Admin rejects tour proposal
- **Endpoint:** POST `/api/v1/tours/proposals/{id}/reject`
- **Command/Query/Handler:** `RejectTourProposalCommand` / `RejectTourProposalCommandHandler`
- **Workflow Ref:** §TourProposal state machine lines 129-136
- **Audit Status:** Built
- **Role(s):** Admin
- **Preconditions:** Proposal Submitted.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/proposals/{proposalId}/reject', {method:'POST', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ reason:'Needs clearer safety plan' })}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/200; status Rejected; reason stored; `TourProposalRejectedIntegrationEvent` published.
- **Edge Cases:** blank reason 400; already approved cannot reject; proposer cannot self-approve/reject; missing proposal 404.

### TC-TG-030: Public list guides offering a tour
- **Endpoint:** GET `/api/v1/tours/{tourId}/guides`
- **Command/Query/Handler:** `GetTourGuidesQuery` / `GetTourGuidesQueryHandler`
- **Workflow Ref:** §Guide Offering endpoints lines 332-344
- **Audit Status:** Built
- **Role(s):** Anonymous
- **Preconditions:** Tour has multiple active guide offerings.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guides').then(async r => ({status:r.status, body:await r.json()}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 200 list with guide IDs, display names, rating; suspended/removed offerings excluded.
- **Edge Cases:** unknown tour 404/empty; exclusive tour returns only proposer; inactive guides hidden; cache invalidated after assign/suspend.

### TC-TG-031: Admin/owner assigns guide to tour
- **Endpoint:** POST `/api/v1/tours/{tourId}/guides`
- **Command/Query/Handler:** `AssignTourGuideCommand` / `AssignTourGuideCommandHandler`
- **Workflow Ref:** §Decision #10 and §GuideOffering lines 147-177
- **Audit Status:** Built
- **Role(s):** Agency/tour owner, Admin
- **Preconditions:** Active guide not already assigned; tour not exclusive or guide is proposer.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guides', {method:'POST', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ guideUserId:'{guideUserId}', isPrimary:false })}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204/201; `GuideTourOffering` created Active; `TourGuideAssignedIntegrationEvent` published.
- **Edge Cases:** duplicate assignment 409; suspended guide 400/403; exclusive tour rejects non-proposer; non-owner 403; legacy `TourTourGuide` not used for new rows.

### TC-TG-032: Admin/owner unassigns guide from tour
- **Endpoint:** DELETE `/api/v1/tours/{tourId}/guides/{guideUserId}`
- **Command/Query/Handler:** `UnassignTourGuideCommand` / `UnassignTourGuideCommandHandler`
- **Workflow Ref:** §GuideOffering lifecycle lines 175-184; migration notes lines 427-433
- **Audit Status:** Built / legacy divergence
- **Role(s):** Agency/tour owner, Admin
- **Preconditions:** Guide assigned to tour and no future bookings if guarded.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guides/{guideUserId}', {method:'DELETE', credentials:'include'}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 204; offering removed/marked Removed; `TourGuideUnassignedIntegrationEvent` if present; list no longer includes guide.
- **Edge Cases:** non-existing assignment 404; future bookings conflict; non-owner 403; double unassign idempotent/404.

### TC-TG-033: GuideOffering schedule CRUD
- **Endpoint:** GET/POST/PUT/DELETE `/api/v1/tours/{tourId}/guide-offerings/{guideId}/schedules[/{scheduleId}]`
- **Command/Query/Handler:** `GetGuideSchedulesQueryHandler`, `CreateGuideScheduleCommandHandler`, `UpdateGuideScheduleCommandHandler`, `DeleteGuideScheduleCommandHandler`
- **Workflow Ref:** §GuideSchedule lines 186-202; §GuideOffering endpoints lines 332-344; Part 3 My Tours lines 818-825
- **Audit Status:** Built
- **Role(s):** Anonymous read if permission allows, OwnGuide writes, Admin/owner as applicable
- **Preconditions:** Active GuideTourOffering.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guide-offerings/{guideId}/schedules', {credentials:'include'}).then(async r => ({status:r.status, body:await r.json()}))" })`
  3. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guide-offerings/{guideId}/schedules', {method:'POST', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ dayOfWeek:1, startTime:'09:00', endTime:'12:00', maxGroupSize:8 })}).then(async r => ({status:r.status, body:await r.text()}))" })`
  4. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guide-offerings/{guideId}/schedules/{scheduleId}', {method:'PUT', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ dayOfWeek:1, startTime:'10:00', endTime:'13:00', maxGroupSize:10 })}).then(r => ({status:r.status}))" })`
  5. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guide-offerings/{guideId}/schedules/{scheduleId}', {method:'DELETE', credentials:'include'}).then(r => ({status:r.status}))" })`
  6. `mcp__playwright__browser_network_requests()`
- **Assertions:** create returns 201 ID; update/delete 204; schedules reflect changes; no overlapping invalid ranges accepted.
- **Edge Cases:** DayOfWeek outside 0-6 400; end before start 400; overlap conflict 409; other guide 403; suspended offering blocks write.

### TC-TG-034: GuideOffering pricing tier CRUD
- **Endpoint:** GET/POST/PUT/DELETE `/api/v1/tours/{tourId}/guide-offerings/{guideId}/pricing-tiers[/{tierId}]`
- **Command/Query/Handler:** `GetGuidePricingTiersQueryHandler`, `CreateGuidePricingTierCommandHandler`, `UpdateGuidePricingTierCommandHandler`, `DeleteGuidePricingTierCommandHandler`
- **Workflow Ref:** §GuidePricingTier lines 204-222; §GuideOffering endpoints lines 332-344
- **Audit Status:** Built
- **Role(s):** Anonymous read if permission allows, OwnGuide writes
- **Preconditions:** Active offering.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guide-offerings/{guideId}/pricing-tiers', {credentials:'include'}).then(async r => ({status:r.status, body:await r.json()}))" })`
  3. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guide-offerings/{guideId}/pricing-tiers', {method:'POST', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ name:'Adult', price:40, currency:'JOD', participantType:1, minParticipants:1, maxParticipants:10, description:'QA adult tier' })}).then(async r => ({status:r.status, body:await r.text()}))" })`
  4. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guide-offerings/{guideId}/pricing-tiers/{tierId}', {method:'PUT', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ name:'Adult+', price:45, currency:'JOD', participantType:1, minParticipants:1, maxParticipants:12, description:'Updated' })}).then(r => ({status:r.status}))" })`
  5. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guide-offerings/{guideId}/pricing-tiers/{tierId}', {method:'DELETE', credentials:'include'}).then(r => ({status:r.status}))" })`
  6. `mcp__playwright__browser_network_requests()`
- **Assertions:** create/update/delete reflected; Money stored with precision; `ParticipantType` persisted.
- **Edge Cases:** negative price 400; invalid currency 400; min > max 400; deleting last adult tier blocked if booking requires adult tier; other guide 403.

### TC-TG-035: Configure private tour pricing
- **Endpoint:** POST/DELETE `/api/v1/tours/{tourId}/guide-offerings/{guideId}/private-tour`
- **Command/Query/Handler:** `EnablePrivateTourCommandHandler`, `DisablePrivateTourCommandHandler`
- **Workflow Ref:** §Decision #5 and §GuideTourOffering lines 157-173
- **Audit Status:** Built
- **Role(s):** OwnGuide, Admin
- **Preconditions:** Active offering.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guide-offerings/{guideId}/private-tour', {method:'POST', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ multiplier:2.0, flatPrice:null })}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guide-offerings/{guideId}/private-tour', {method:'DELETE', credentials:'include'}).then(r => ({status:r.status}))" })`
  4. `mcp__playwright__browser_network_requests()`
- **Assertions:** enabling sets `OffersPrivateTour=true` and multiplier or flat price; disabling clears private pricing; public pricing endpoint reflects private option.
- **Edge Cases:** both multiplier and flatPrice null 400; both set 400 if mutually exclusive; multiplier <=1 400; suspended offering blocks update.

### TC-TG-036: Suspend/reinstate/remove guide offering
- **Endpoint:** POST `/api/v1/tours/{tourId}/guide-offerings/{guideId}/suspend`, POST `/reinstate`, DELETE offering
- **Command/Query/Handler:** `SuspendGuideOfferingCommandHandler`, `ReinstateGuideOfferingCommandHandler`, `RemoveGuideOfferingCommandHandler`
- **Workflow Ref:** §GuideTourOffering lines 147-184
- **Audit Status:** Built
- **Role(s):** Tour owner, Admin
- **Preconditions:** Active offering.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guide-offerings/{guideId}/suspend', {method:'POST', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ reason:'QA temporary suspension' })}).then(r => ({status:r.status}))" })`
  3. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guide-offerings/{guideId}/reinstate', {method:'POST', credentials:'include'}).then(r => ({status:r.status}))" })`
  4. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/tours/{tourId}/guide-offerings/{guideId}', {method:'DELETE', credentials:'include'}).then(r => ({status:r.status}))" })`
  5. `mcp__playwright__browser_network_requests()`
- **Assertions:** suspend sets Status Suspended, reason, SuspendedAt and publishes `GuideTourOfferingSuspendedIntegrationEvent`; reinstate sets Active; remove marks Removed/deletes and listing excludes it.
- **Edge Cases:** blank reason 400; already suspended/reinstated idempotence/409; future bookings block remove; non-owner 403.

### TC-TG-037: Guide availability block CRUD
- **Endpoint:** GET/POST/DELETE `/api/v1/guides/me/availability-blocks[/{id}]`
- **Command/Query/Handler:** `GetMyAvailabilityBlocksQueryHandler`, `CreateGuideAvailabilityBlockCommandHandler`, `DeleteGuideAvailabilityBlockCommandHandler`
- **Workflow Ref:** §Part 3 GuideAvailabilityBlock lines 1040-1061; Dashboard endpoints lines 1075-1082
- **Audit Status:** Built
- **Role(s):** OwnGuide
- **Preconditions:** Login as approved guide.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/me/availability-blocks', {credentials:'include'}).then(async r => ({status:r.status, body:await r.json()}))" })`
  3. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/me/availability-blocks', {method:'POST', credentials:'include', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ startDate:'2026-07-01', endDate:'2026-07-03', reason:'Family leave' })}).then(async r => ({status:r.status, body:await r.text()}))" })`
  4. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/me/availability-blocks/{blockId}', {method:'DELETE', credentials:'include'}).then(r => ({status:r.status}))" })`
  5. `mcp__playwright__browser_network_requests()`
- **Assertions:** create returns 201 ID; list includes date range; delete removes; bookings/calendar availability excludes blocked dates.
- **Edge Cases:** endDate < startDate 400; overlapping block conflict or allowed documented; reason >200 400; other guide cannot delete; date in past 400 if implemented.

### TC-TG-038: Guide tier progress dashboard
- **Endpoint:** GET `/api/v1/guides/me/tier`
- **Command/Query/Handler:** `GetGuideTierProgressQuery` / `GetGuideTierProgressQueryHandler`
- **Workflow Ref:** §Part 3 Trust Tier lines 978-998
- **Audit Status:** Built
- **Role(s):** OwnGuide
- **Preconditions:** Guide has stats for completed tours/rating/report count.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => await fetch('https://localhost:57065/api/v1/guides/me/tier', {credentials:'include'}).then(async r => ({status:r.status, body:await r.json()}))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 200; includes current tier, next tier, progress bars/criteria, commission rate if configured.
- **Edge Cases:** Gold guide has no next tier; new guide zero progress; high report rate blocks progress; customer 403.

### TC-TG-039: Guide earnings dashboard reads
- **Endpoint:** GET `/api/v1/guides/me/earnings/summary`, `/by-tour`, `/history`
- **Command/Query/Handler:** `GetGuideEarningsSummaryQueryHandler`, `GetGuideEarningsByTourQueryHandler`, `GetGuideEarningsHistoryQueryHandler`
- **Workflow Ref:** §Part 3 Earnings lines 868-901 and dashboard endpoints lines 1083-1097
- **Audit Status:** Built / Partial (read endpoints only; Finance mutations deferred)
- **Role(s):** OwnGuide
- **Preconditions:** Seed completed booking/earning data or empty state.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => Promise.all(['/summary','/by-tour','/history?page=1&pageSize=20'].map(p => fetch('https://localhost:57065/api/v1/guides/me/earnings'+p, {credentials:'include'}).then(async r => ({url:p,status:r.status,body:await r.text()}))))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 200 for all; summary has gross/net/commission/pending; by-tour aggregates by offering; history paginates.
- **Edge Cases:** no earnings returns zeros; negative/invalid page 400; guide cannot request another guide's earnings; Finance unavailable returns graceful 503/empty per contract.

### TC-TG-040: Guide analytics dashboard reads
- **Endpoint:** GET `/api/v1/guides/me/analytics/overview`, `/booking-trends`, `/popular-tours`, `/peak-days`
- **Command/Query/Handler:** `GetGuideBookingOverviewQueryHandler`, `GetGuideBookingTrendsQueryHandler`, `GetGuidePopularToursQueryHandler`, `GetGuidePeakDaysQueryHandler`
- **Workflow Ref:** §Part 3 Analytics lines 904-922 and endpoints lines 1108-1117
- **Audit Status:** Built / Partial (read models may be empty)
- **Role(s):** OwnGuide
- **Preconditions:** Guide has bookings/analytics seed or empty state.
- **Playwright MCP Steps:**
  1. `mcp__playwright__browser_navigate({ url: "https://localhost:57065/swagger/" })`
  2. `mcp__playwright__browser_evaluate({ function: "async () => Promise.all(['/overview','/booking-trends?granularity=monthly&months=6','/popular-tours?limit=10','/peak-days'].map(p => fetch('https://localhost:57065/api/v1/guides/me/analytics'+p, {credentials:'include'}).then(async r => ({url:p,status:r.status,body:await r.text()}))))" })`
  3. `mcp__playwright__browser_network_requests()`
- **Assertions:** 200 for all; no raw cross-guide data leak; trend arrays sorted by period; popular tours limited.
- **Edge Cases:** invalid granularity 400; months >24 clamped/400; no data returns zeros; customer 403.

## 2. NOT_BUILT — Skip-Until-Implemented Scenarios

### NOT_BUILT-TC-TG-041: Razor TourGuide dashboard area navigation
- **Why skipped:** Requested `YallaJo.Web/Areas/TourGuide/` does not exist. Workflow Part 3 defines dashboard UX, but no Razor Area/pages/controllers were found.
- **Workflow Ref:** §Part 3 Dashboard Sections lines 786-1037
- **Scenario skeleton:** Navigate to `https://localhost:57065/swagger/TourGuide/Dashboard`; assert KPI cards, My Tours, Calendar, Bookings, Earnings, Analytics, Applications, Proposals, Reviews, Notifications tabs; use `mcp__playwright__browser_snapshot`, `browser_click`, `browser_take_screenshot`.
- **Expected when implemented:** dashboard renders for approved guide; customer 403; suspended provider sees suspension state.

### NOT_BUILT-TC-TG-042: In-app notification endpoints under guides/me
- **Why skipped:** Workflow defines `/guides/me/notifications`; sampled TourGuide endpoints have no notification group.
- **Workflow Ref:** §Part 3 lines 1001-1037 and 1066-1074
- **Scenario skeleton:** GET list, unread count, mark read, mark all read using `mcp__playwright__browser_evaluate`; assert Messaging notifications for application/proposal/tier/payout events.

### NOT_BUILT-TC-TG-043: Manual payout request and payment method management
- **Why skipped:** TourGuide endpoints include earnings reads only; no `/guides/me/payouts`, `/payment-methods` mutation routes in sampled code. Finance owns money.
- **Workflow Ref:** §Part 3 lines 884-901 and 1083-1097
- **Scenario skeleton:** Add JoMoPay method, set default, request payout above 20 JOD, assert Finance `Payout` row and status Pending.

### NOT_BUILT-TC-TG-044: Review response/report from guide dashboard
- **Why skipped:** No `/guides/me/reviews` endpoints in TourGuide sampled code; Social module owns review replies.
- **Workflow Ref:** §Part 3 lines 958-975 and 1098-1107
- **Scenario skeleton:** GET reviews, POST response, PUT response within 48h, report inappropriate review; assert one reply per review and 48h edit window.

### NOT_BUILT-TC-TG-045: Booking management check-in/no-show/cancellation actions
- **Why skipped:** Workflow booking management is planned; Booking module not sampled as built here and no TourGuide booking endpoints exist.
- **Workflow Ref:** §Part 3 lines 851-865
- **Scenario skeleton:** Guide views upcoming booking, opens detail, marks checked-in/no-show, processes cancellation according to tour policy; assert Booking state transitions.

### NOT_BUILT-TC-TG-046: Document expiry warning and auto-suspend flow
- **Why skipped:** TourGuide has `MoTALicenseNumber` but no sampled document expiry entity/service. User explicitly requires coverage; mark skip until provider document expiry service exists.
- **Workflow Ref:** Cross-cutting provider lifecycle; not explicit in TourGuide-Flow, required by business rules
- **Scenario skeleton:** Seed guide document expiring in 30 days → background warning event/email; at 14-day grace → urgent warning; after expiry+grace → auto-suspend guide/provider; assert status Suspended, public profile hidden, notification emitted.

### NOT_BUILT-TC-TG-047: Provider registration pending-to-approved creation path UI
- **Why skipped:** TourGuide registration comes from provider approval via `ProviderApprovedCreateTourGuideHandler`; no TourGuide self-registration UI/endpoint sampled here.
- **Workflow Ref:** §Part 2 cross-link lines 516-519; §TourGuide Entity lines 522-588
- **Scenario skeleton:** Pending provider uploads docs, admin approves, ContentTours consumes provider-approved event, creates TourGuide with ApplicationId/LinkedProviderId.

### NOT_BUILT-TC-TG-048: Guide change-slug command
- **Why skipped:** Audit §2.1 listed ChangeSlug missing; current endpoint set uses by-slug read but no change-slug route found.
- **Workflow Ref:** §Part 2 methods lines 568-576 and new handlers lines 680-694
- **Scenario skeleton:** PUT `/guides/me/slug`; assert unique slug validation, old slug 404, new slug 200.

### NOT_BUILT-TC-TG-049: Tour proposal update draft/detail/admin queue endpoints
- **Why skipped:** Current `TourProposalEndpoints.cs` has list/create/submit/approve/reject only; no GET detail, PUT update, admin queue route.
- **Workflow Ref:** §Tour Proposal endpoints lines 319-331; audit §1.5
- **Scenario skeleton:** Create draft, GET detail, PUT fields, list admin queue for Submitted; assert owner/admin scoping.

### NOT_BUILT-TC-TG-050: Guide application detail/update draft/submit endpoints
- **Why skipped:** Current `GuideApplicationEndpoints.cs` has list/apply/approve/reject/open/close; no GET detail, PUT draft, submit route.
- **Workflow Ref:** §Guide Application endpoints lines 307-318; audit §1.5
- **Scenario skeleton:** Create Draft application, update message/pricing notes, submit, assert SubmittedAt and outbox.

### NOT_BUILT-TC-TG-051: Guide cleanup hard-delete service after 60 days
- **Why skipped:** Workflow lists `GuideCleanupService`; not confirmed in sampled endpoints/code.
- **Workflow Ref:** §Part 2 Background Services lines 727-730
- **Scenario skeleton:** Seed Deactivated guide with `DeletedAt` older than 60 days, trigger service, assert profile hard deleted/anonymized tour history preserved.

### NOT_BUILT-TC-TG-052: Live calendar UI with booking overlay
- **Why skipped:** API availability blocks/schedules exist, but no Razor calendar UI or Booking overlay endpoint found.
- **Workflow Ref:** §Part 3 Calendar lines 834-849
- **Scenario skeleton:** Navigate dashboard calendar, switch week/month, click empty slot to block, click booked slot to show tourist detail, unblock.

## 3. DEFERRED — Post-MVP Scenarios

### DEFERRED-TC-TG-053: Live GPS tracking during tour session
- **Why deferred:** Workflow Decision #33 explicitly defers live tracking.
- **Workflow Ref:** §Part 3 lines 781-783
- **Scenario skeleton:** Start tour session, stream GPS, customer follows live guide; verify SignalR map updates.

### DEFERRED-TC-TG-054: Trust-tier auto-approval for Gold guide proposals
- **Why deferred:** Workflow Phase 4 notes start with all-review; auto-approval behind tier check later.
- **Workflow Ref:** §TourProposal lines 129-132; Risks line 502
- **Scenario skeleton:** Gold guide submits proposal; assert auto-approved, tour created, notifications sent without admin click.

### DEFERRED-TC-TG-055: Tier promotion/demotion background evaluator
- **Why deferred:** Workflow says criteria configurable and service evaluates periodically; endpoint reads progress only.
- **Workflow Ref:** §Part 3 lines 989-998
- **Scenario skeleton:** Seed completed tours/rating/report rate; run evaluator; assert tier promoted/demoted and notifications.

### DEFERRED-TC-TG-056: Full Finance payout execution
- **Why deferred:** Finance owns weekly/on-demand payout execution; TourGuide currently has read-side earnings.
- **Workflow Ref:** §Part 3 lines 884-901
- **Scenario skeleton:** Weekly batch creates payout, transfer succeeds/fails, guide notification emitted.

### DEFERRED-TC-TG-057: Cross-module analytics conversion from profile views
- **Why deferred:** Requires page-view tracking and analytics rollups beyond current read endpoints.
- **Workflow Ref:** §Part 3 lines 904-922
- **Scenario skeleton:** Generate profile views/bookings; rollup conversion; verify dashboard chart.

### DEFERRED-TC-TG-058: Legacy TourTourGuide removal migration
- **Why deferred:** `TourTourGuide` entity/repository still present; roadmap W3-C asks deletion/retarget.
- **Workflow Ref:** §Migration lines 427-433; FixPlan Fix 2; Master W3-C
- **Scenario skeleton:** Migration moves legacy records to GuideTourOffering and removes legacy table without breaking Assign/Unassign.

### DEFERRED-TC-TG-059: Document expiry automated scheduler hardening
- **Why deferred:** Required by user/business rule but no implementation sampled.
- **Workflow Ref:** Business rules provider document lifecycle
- **Scenario skeleton:** Time-travel tests for 30-day warning, 14-day grace, and auto-suspend with idempotent notifications.

## 4. Integration Events

### Events PUBLISHED by TourGuide / ContentTours → consumer tests
- `TourGuideRegisteredIntegrationEvent` — after guide registration/provider approval; consumers: Messaging welcome, Analytics provider count.
- `TourGuideUpdatedIntegrationEvent` / `TourGuideProfileUpdatedIntegrationEvent` — after profile/admin update; consumers: Search/Analytics read models.
- `TourGuideLanguageAddedIntegrationEvent`, `TourGuideLanguageRemovedIntegrationEvent`, `TourGuideSpecializationAddedIntegrationEvent` — consumers: Search facets, recommendations.
- `TourGuideAssignedIntegrationEvent`, `TourGuideUnassignedIntegrationEvent` — consumers: Booking availability, Analytics guide-tour stats.
- `GuideTourOfferingSuspendedIntegrationEvent` — consumers: Booking availability and Messaging notification.
- `GuideApplicationApprovedIntegrationEvent`, `GuideApplicationRejectedIntegrationEvent`, `NewGuideApplicationIntegrationEvent`/submitted — consumers: Messaging in-app/email, Analytics.
- `TourProposalSubmittedIntegrationEvent`, `TourProposalApprovedIntegrationEvent`, `TourProposalRejectedIntegrationEvent` — consumers: Messaging, Analytics; approval also implies new Tour consumer events.
- Test pattern: after command returns success, poll module outbox table or API diagnostics until `ProcessedOnUtc` non-null; assert consumer inbox row exists exactly once and duplicate delivery is idempotent.

### Events CONSUMED by TourGuide → producer-side tests
- `ProviderApprovedIntegrationEvent` from provider/accounts/content places pipeline → `ProviderApprovedCreateTourGuideHandler` creates/links `TourGuide`.
- Provider suspended/reinstated events → ContentTours handler suspends/reinstates tours/offerings as applicable.
- Future document-expiry events → should suspend guide/provider after grace.
- Producer-side Playwright pattern: perform producer action in Accounts/ContentPlaces, then use `mcp__playwright__browser_wait_for({ time: 12 })`, query ContentTours API/profile, assert idempotent single guide/profile transition.

## 5. Validation Matrix

| Field | Rule | Test Input | Expected Error |
|---|---|---|---|
| displayName | Required, max 100 | `""` | 400 validation |
| bio | Required/limited profile text | 5001 chars | 400 validation |
| yearsOfExperience | Non-negative realistic range | `-1` | 400 validation |
| avatarUrl/coverImageUrl | Valid URL/rooted URL, max 500 | `javascript:alert(1)` | 400 validation |
| languageId/specializationId | Non-empty existing GUID | `00000000-0000-0000-0000-000000000000` | 400/404 |
| proficiency | Allowed values/length | `""`, `"unknown-level"` | 400 validation |
| application message | Required, max 2000 | empty or 2001 chars | 400 validation |
| rejection reason | Required, max policy length | whitespace | 400 validation |
| proposal title | Required | empty | 400 validation |
| proposal placeId | Required active Place | deleted/nonexistent GUID | 404/422 |
| proposal durationMinutes | Positive | `0` | 400 validation |
| proposal maxGroupSize | Positive | `0` | 400 validation |
| proposal basePrice | Positive | `-1` | 400 validation |
| currency | ISO/supported | `"XXX"`, `"JO"` | 400 validation |
| guide schedule dayOfWeek | 0-6 | `7` | 400 validation |
| guide schedule time range | start < end | `13:00` → `10:00` | 400 validation |
| guide pricing tier participants | min <= max | min 10 max 1 | 400 validation |
| guide pricing tier price | > 0 | `0` | 400 validation |
| private multiplier | > 1 if used | `0.5` | 400 validation |
| availability block date range | start <= end, not invalid | start after end | 400 validation |
| pagination | page>=1, pageSize bounded | page 0, pageSize 1000 | 400/clamped |

## 6. Auth Matrix

| Endpoint | Anonymous | Customer | OtherGuide | OwnGuide | Admin |
|---|---:|---:|---:|---:|---:|
| GET `/api/v1/guides` | 200 | 200 | 200 | 200 | 200 |
| GET `/api/v1/guides/by-slug/{slug}` | 200 | 200 | 200 | 200 | 200 |
| GET `/api/v1/guides/{id}` | 200 | 200 | 200 | 200 | 200 |
| GET `/api/v1/guides/me` | 401 | 403 | 200 own only | 200 | 200/403 per policy |
| PUT `/api/v1/guides/{id}` | 401 | 403 | 403 | 204 | 204 if permission |
| POST `/api/v1/guides/{id}/languages` | 401 | 403 | 403 | 201/204 | 204 if manage |
| DELETE `/api/v1/guides/{id}/languages/{languageId}` | 401 | 403 | 403 | 204 | 204 if manage |
| POST `/api/v1/guides/{id}/specializations` | 401 | 403 | 403 | 201/204 | 204 if manage |
| GET `/api/v1/guides/me/applications` | 401 | 403 | 200 own only | 200 | 200/403 per policy |
| GET `/api/v1/guides/{id}/tours` | 200 | 200 | 200 | 200 | 200 |
| PUT `/api/v1/guides/me/avatar` | 401 | 403 | 200 own only | 200 | 200/403 |
| DELETE `/api/v1/guides/me` | 401 | 403 | 200 own only | 200 | 200/403 |
| GET `/api/v1/guides/admin/{id}` | 401 | 403 | 403 | 403 | 200 |
| POST `/api/v1/guides/admin/{id}/suspend` | 401 | 403 | 403 | 403 | 204 |
| POST `/api/v1/tours/{tourId}/applications` | 401 | 403 | 201 if eligible | 201 if eligible | 403/201 per permission |
| GET `/api/v1/tours/{tourId}/applications` | 401 | 403 | 403 unless owner/applicant scope | 403 unless owner | 200 |
| POST application approve/reject | 401 | 403 | 403 unless tour owner | 403 unless tour owner | 204 |
| POST `/api/v1/tours/proposals` | 401 | 403 | 201 | 201 | 201/403 per permission |
| POST proposal submit | 401 | 403 | 403 if not owner | 204 if owner | 204/403 |
| POST proposal approve/reject | 401 | 403 | 403 | 403 | 204 |
| GET guide offering schedules/pricing | 401/200 per endpoint permission | 403/200 per policy | 200 if permission | 200 | 200 |
| POST/PUT/DELETE own schedules/pricing/private-tour | 401 | 403 | 403 | 201/204 | 204 |
| POST guide offering suspend/reinstate/remove | 401 | 403 | 403 unless tour owner | 403 unless tour owner | 204 |
| GET `/api/v1/guides/me/earnings/*` | 401 | 403 | 200 own only | 200 | 200/403 |
| GET `/api/v1/guides/me/analytics/*` | 401 | 403 | 200 own only | 200 | 200/403 |

## 7. State Machine Transitions

### TourGuide profile lifecycle
| From | Action | To | Scenario |
|---|---|---|---|
| Provider Approved / Registered | `ProviderApprovedCreateTourGuideHandler` or register | Active | NOT_BUILT-TC-TG-047 + integration producer test |
| Active | `SuspendTourGuideCommand` | Suspended | TC-TG-015 |
| Suspended | `ReinstateTourGuideCommand` | Active | TC-TG-016 |
| Active | `DeactivateTourGuideCommand` / `AdminDeactivateTourGuideCommand` | Deactivated | TC-TG-008, TC-TG-018 |
| Deactivated older than 60 days | `GuideCleanupService` | Hard-deleted/anonymized | NOT_BUILT-TC-TG-051 |
| Active with expired required document | expiry scheduler | Suspended | NOT_BUILT-TC-TG-046 / DEFERRED-TC-TG-059 |

### GuideApplication lifecycle
| From | Action | To | Scenario |
|---|---|---|---|
| None | `ApplyForTourCommand` | Draft or Submitted (implementation-dependent) | TC-TG-021 |
| Draft | Update draft | Draft | NOT_BUILT-TC-TG-050 |
| Draft | Submit | Submitted | NOT_BUILT-TC-TG-050 if separate submit implemented |
| Submitted | `ApproveGuideApplicationCommand` | Approved + Active GuideTourOffering | TC-TG-023 |
| Submitted | `RejectGuideApplicationCommand` | Rejected | TC-TG-024 |
| Rejected | Resubmit (max 2) | Submitted | NOT_BUILT-TC-TG-050 |

### TourProposal lifecycle
| From | Action | To | Scenario |
|---|---|---|---|
| None | `CreateTourProposalCommand` | Draft | TC-TG-025 |
| Draft | Update draft | Draft | NOT_BUILT-TC-TG-049 |
| Draft | `SubmitTourProposalCommand` | Submitted | TC-TG-027 |
| Submitted | `ApproveTourProposalCommand` | Approved + Tour + GuideTourOffering | TC-TG-028 |
| Submitted | `RejectTourProposalCommand` | Rejected | TC-TG-029 |
| Submitted by Gold guide | Auto-approval | Approved | DEFERRED-TC-TG-054 |

### GuideTourOffering lifecycle
| From | Action | To | Scenario |
|---|---|---|---|
| None | `AssignTourGuideCommand` or application/proposal approval | Active | TC-TG-031 / TC-TG-023 / TC-TG-028 |
| Active | `SuspendGuideOfferingCommand` | Suspended | TC-TG-036 |
| Suspended | `ReinstateGuideOfferingCommand` | Active | TC-TG-036 |
| Active/Suspended | `RemoveGuideOfferingCommand` / unassign | Removed | TC-TG-032 / TC-TG-036 |

### Document expiry lifecycle
| State | Trigger | Expected Transition | Scenario |
|---|---|---|---|
| Valid document | Expiry in 30 days | Warn guide/admin, no suspension | NOT_BUILT-TC-TG-046 |
| Warning | Expiry in 14 days/grace | Urgent warning, restricted reminders | NOT_BUILT-TC-TG-046 |
| Grace elapsed | Expired + grace | Auto-suspend provider/guide, hide offerings | NOT_BUILT-TC-TG-046 / DEFERRED-TC-TG-059 |
| Suspended for expiry | Valid replacement approved | Reinstate eligible | future doc-renewal test |

## 8. Known Divergence (per CrossDocumentAnalysisReport.md)
- **Requested paths diverge from actual code:** no `src/TourGuide.*` projects; TourGuide implementation is in `ContentTours.*`. no `YallaJo.Web/Areas/TourGuide` Razor UI.
- **Audit report stale in current workspace:** audit said zero GuideApplication/TourProposal events and no GuideOffering endpoints; sampled code now contains those events/endpoints/handlers. Keep scenarios marked Built based on code, not old audit.
- **Legacy cleanup still open:** `ContentTours.Domain/Entities/TourTourGuide.cs` and `ITourTourGuideRepository.cs` still exist. Master roadmap W3-C requires deletion/retarget to `GuideTourOffering`.
- **Provider suspension cascade partially resolved:** CrossDocumentAnalysisReport flags ContentTours/Social pending. Include integration tests for provider suspension → tours/offerings hidden.
- **IndependentGuide role mapping:** CrossDocumentAnalysisReport maps Independent Tour Guide to `IndependentGuide`; tests should verify role/permission mapping grants `TourGuideProfile`, `GuideApplication`, `TourProposal`, and `GuideOffering` permissions to seeded guide accounts.
- **Tour approval state machine mismatch risk:** Cross-document state uses Submitted/UnderReview/Approved/Rejected/Resubmitted; ContentTours uses Draft/Pending/Approved/Rejected/Suspended/Archived elsewhere. Tests should assert actual API states, not prose names.
- **Document expiry business rule not represented in TourGuide-Flow:** user requested 30-day warning → 14-day grace → auto-suspend coverage; mark NOT_BUILT until document expiry service exists.
