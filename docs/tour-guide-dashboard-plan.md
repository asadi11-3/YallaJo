# Tour Guide Dashboard - Build Plan

> Status: PROPOSED (awaiting approval). Planning only, nothing built yet.
> Scope: a tour-guide-FACING self-service area (the guide's own dashboard and pages).
> This is NOT the admin moderation of guides, which already exists under `Areas/Admin` (Guides / Guide-applications).

## 1. Goal

Give an authenticated tour guide their own area in the existing web app (`src/Hosts/YallaJo.Web`) to manage their profile, offerings, schedule, bookings, earnings, analytics, applications, discounts, join requests, tour proposals, and agency relationship, reusing the backend endpoints that already exist and the existing webestica Bootstrap theme. No new backend work is required for the core pages (every page below maps to a verified existing endpoint). The guide area spans four backend modules: ContentTours (`/api/v1/guides`, `/api/v1/tours`), Booking (`/api/v1/booking`), and Accounts (`/api/v1/guides` agency routes).

## 1a. Pages and endpoints at a glance

How many pages: 13 confirmed, plus 1 conditional (Reviews) that ships only if a guide-facing reviews endpoint exists. So 13 or 14 total.

This count grew from the original 8-9 after a solution-wide audit found five guide self-service capabilities that live in other modules (Booking and Accounts) or as write-actions in ContentTours that the first draft missed. Pages 1-7 use the ContentTours profile group `/api/v1/guides`; pages 8-12 reach into other backend groups (ContentTours `/api/v1/tours`, Booking `/api/v1/booking`, Accounts `/api/v1/guides`), noted per row.

| # | Page | Endpoint(s) it calls | Permission gate |
| --- | --- | --- | --- |
| 1 | Dashboard | GET `/me`, GET `/me/tier`, GET `/me/earnings/summary`, GET `/me/analytics/overview` | TourGuideProfile.Read |
| 2 | My profile | GET `/me`; PUT `/{id}`; PUT `/me/avatar`; PUT `/me/cover-image`; POST `/{id}/languages`; DELETE `/{id}/languages/{languageId}`; POST `/{id}/specializations`; DELETE `/me` | TourGuideProfile.Read, TourGuide.Update, TourGuideProfile.Update, TourGuideProfile.DeleteOwn |
| 3 | My tours | GET `/{id}/tours?page&pageSize` | TourGuideProfile.Read |
| 4 | My offerings | GET `/{tourId}/guide-offerings` (+ `/{guideId}` detail); schedules GET/POST/PUT/DELETE; pricing-tiers GET/POST/PUT/DELETE; private-tour POST/DELETE | GuideOffering.Read / Create / Update / Delete |
| 5 | Availability | GET `/me/availability-blocks`; POST `/me/availability-blocks`; DELETE `/me/availability-blocks/{id}` | TourGuideProfile.Read / Update |
| 6 | Earnings | GET `/me/earnings/summary`, GET `/me/earnings/by-tour`, GET `/me/earnings/history?page&pageSize` | TourGuideProfile.Read |
| 7 | Analytics | GET `/me/analytics/overview`, GET `/me/analytics/booking-trends`, GET `/me/analytics/popular-tours`, GET `/me/analytics/peak-days` | TourGuideProfile.Read |
| 8 | Applications | GET `/me/applications?page&pageSize` (read own); POST `/api/v1/tours/{tourId}/applications` (apply to run a tour) | TourGuideProfile.Read, GuideApplication.Create |
| 9 | Discounts | GET `/api/v1/booking/guide-discounts/mine`; POST `/api/v1/booking/guide-discounts`; PUT `/api/v1/booking/guide-discounts/{id}`; DELETE `/api/v1/booking/guide-discounts/{id}` | Booking.TourBooking (ReadOwn / Create / Update / Delete) |
| 10 | Join requests | GET `/api/v1/booking/join-requests?tourBookingId&myRequestsOnly`; POST `/api/v1/booking/join-requests/{id}/approve`; POST `/api/v1/booking/join-requests/{id}/reject` | Booking.JoinRequest (ReadOwn / Approve / Reject) |
| 11 | My proposals | GET `/api/v1/tours/proposals`; POST `/api/v1/tours/proposals`; POST `/api/v1/tours/proposals/{id}/submit` | TourProposal.Read / Create / Submit |
| 12 | My agency | GET `/api/v1/guides/me/invitations`; POST `/api/v1/guides/agencies/{agencyUserId}/apply`; POST `/api/v1/guides/invitations/{id}/accept`; POST `/api/v1/guides/invitations/{id}/decline`; DELETE `/api/v1/guides/me/agency` | GuideAgency.Read / Create / Update / Delete |
| 13 | Reviews (conditional) | A guide-reviews endpoint, to be confirmed. If none exists, this page is dropped and rating/review-count are shown on the Dashboard instead. | TourGuideProfile.Read |

No new backend endpoints are required. Backend-side, the only change is adding the web permission groups that the reused endpoints already enforce server-side: `WebPermission.GuideOffering`, `WebPermission.TourProposal`, `WebPermission.GuideAgency`, plus confirming Booking groups (`TourBooking`, `JoinRequest`) and `GuideApplication.Create` exist (extend, do not duplicate).

Caveat on page 11 (My proposals): the backend `GET /api/v1/tours/proposals` list handler is currently a stub that returns an empty array, so create and submit work but the list is not yet truly implemented server-side. This page is "build the create/submit flow now, surface the list once the backend query lands."

Note on images: avatar, cover image, and any other image are NOT uploaded through the page endpoints above. Every image is handled as an attachment through the shared ContentCore attachment endpoints (see section 2.4). The page endpoints that take an image URL (for example PUT `/me/avatar`) receive the URL that the attachment upload returns; the upload itself is always a separate multipart call to ContentCore first.

## 2. What already exists (verified)

### 2.1 Backend endpoints (ContentTours module, parent group `/api/v1/guides`)

All `/me/*` routes require authentication. Permissions are `ContentToursFeatures.TourGuideProfile` (Read / Update / DeleteOwn), `ContentToursFeatures.TourGuide` (Update), and `ContentToursFeatures.GuideOffering` (Read / Create / Update / Delete / Suspend / Reinstate).

| Capability | Method + route | Permission |
| --- | --- | --- |
| Own profile | GET `/api/v1/guides/me` -> `TourGuideProfileDto` | TourGuideProfile.Read |
| Update profile | PUT `/api/v1/guides/{id:guid}` (Bio, YearsOfExperience, HasFirstAid, MoTALicenseNumber) | TourGuide.Update |
| Avatar / cover | PUT `/api/v1/guides/me/avatar` ; PUT `/api/v1/guides/me/cover-image` | TourGuideProfile.Update |
| Languages | POST `/api/v1/guides/{id:guid}/languages` ; DELETE `/api/v1/guides/{id:guid}/languages/{languageId:guid}` | TourGuide.Update |
| Specializations | POST `/api/v1/guides/{id:guid}/specializations` | TourGuide.Update |
| Self-deactivate | DELETE `/api/v1/guides/me` | TourGuideProfile.DeleteOwn |
| Tier progress | GET `/api/v1/guides/me/tier` | TourGuideProfile.Read |
| Earnings summary | GET `/api/v1/guides/me/earnings/summary` | TourGuideProfile.Read |
| Earnings by tour | GET `/api/v1/guides/me/earnings/by-tour` | TourGuideProfile.Read |
| Earnings history | GET `/api/v1/guides/me/earnings/history?page&pageSize` | TourGuideProfile.Read |
| Analytics overview | GET `/api/v1/guides/me/analytics/overview` | TourGuideProfile.Read |
| Booking trends | GET `/api/v1/guides/me/analytics/booking-trends?granularity&months` | TourGuideProfile.Read |
| Popular tours | GET `/api/v1/guides/me/analytics/popular-tours?limit` | TourGuideProfile.Read |
| Peak days | GET `/api/v1/guides/me/analytics/peak-days` | TourGuideProfile.Read |
| Availability blocks | GET / POST `/api/v1/guides/me/availability-blocks` ; DELETE `/{id:guid}` | TourGuideProfile.Read / Update |
| My applications | GET `/api/v1/guides/me/applications?page&pageSize` -> `GetMyGuideApplicationsResult` | TourGuideProfile.Read |
| Assigned tours | GET `/api/v1/guides/{id:guid}/tours?page&pageSize` | (public) |
| Offerings (per tour) | GET `/api/v1/guides/{tourId:guid}/guide-offerings` (+ `/{guideId}` detail) | GuideOffering.Read |
| Schedules | GET / POST / PUT / DELETE `.../guide-offerings/{guideId}/schedules` | GuideOffering.Read / Create / Update / Delete |
| Pricing tiers | GET / POST / PUT / DELETE `.../guide-offerings/{guideId}/pricing-tiers` | GuideOffering.Read / Create / Update / Delete |
| Private tour pricing | POST / DELETE `.../guide-offerings/{guideId}/private-tour` | GuideOffering.Create / Delete |

`TourGuideProfileDto`: `Id, UserId, DisplayName?, AvatarUrl?, Bio, YearsOfExperience, HasFirstAid, MoTALicenseNumber?, AverageRating, ReviewCount, TourCount, Languages[], Specializations[]`.

Exact field lists for the earnings, analytics, tier, offering, schedule, pricing-tier, applications-list, and assigned-tours DTOs are to be confirmed at build time (routes and permissions are confirmed). See Open Questions.

### 2.2 Web shell to mirror

`Areas/Provider` is the closest existing self-service area and the template to copy. It has per-feature `ApiClients` / `Controllers` / `Facades`, per-feature `Models` folders, a `Shared/_ProviderSidebar.cshtml` + `Shared/ProviderSidebarVm.cs`, and Views per feature. The new Guide area mirrors this structure exactly.

`Areas/Guide` does not exist yet.

### 2.3 Permissions

`WebPermission.cs` already has `TourGuide`, `TourGuideProfile`, `Payout`, `Tour`, and `GuideApplication` groups. It does NOT have a `GuideOffering` group, so one must be added (Read, Create, Update, Delete, Suspend, Reinstate), following the extend-never-duplicate rule.

### 2.4 Image and file handling (ContentCore attachments)

Every image in this area (guide avatar and cover image) is handled as an attachment through the shared ContentCore attachment endpoints, not through bespoke per-page upload routes. These endpoints already exist and are reused as-is.

| Capability | Method + route | Notes |
| --- | --- | --- |
| Upload one file | POST `/attachments` (multipart/form-data: `IFormFile file` + `EntityType`, `EntityId`, `AttachmentType`, optional `Width`, `Height`, `DurationSeconds`, `SortOrder`) -> `UploadAttachmentResult` | Attachment.Create |
| Bulk upload images | POST `/attachments/images?entityType&entityId` (multipart/form-data: `IFormFileCollection files`, up to 20) -> `BulkUploadImagesResult` | Attachment.Create |
| List for an entity | GET `/attachments?entityType&entityId` -> `IReadOnlyList<AttachmentDto>` | Attachment.Read |
| Get one | GET `/attachments/{id}` -> `AttachmentDto` | Attachment.Read |
| Delete | DELETE `/attachments/{id}` | Attachment.Delete |
| Reorder | PUT `/attachments/reorder` (EntityType, EntityId, OrderedAttachmentIds) | Attachment.Update |
| Set primary image | PUT `/attachments/primary` (EntityType, EntityId, AttachmentId) | EntityImage.Update |

How this maps to guide pages:

- `EntityType` for a guide image is `TourGuide` (the `EntityType` enum is shared across modules: Place, Business, Tour, Blog, Review, TourGuide).
- Flow for an avatar or cover: upload the file to POST `/attachments` (or `/attachments/images`) with `EntityType=TourGuide` and the guide id, take the returned attachment URL, then call the profile route that stores it (PUT `/me/avatar` or `/me/cover-image`). Those routes only persist a URL; they never receive the file bytes.
- Permissions are `ContentCoreFeatures.Attachment` (Create, Read, Update, Delete) and `ContentCoreFeatures.EntityImage` (Update, for set-primary). The web shell needs matching `WebPermission.Attachment` and `WebPermission.EntityImage` groups; verify whether they already exist before adding (extend, do not duplicate).
- The upload endpoints use `multipart/form-data` and disable antiforgery, so the web ApiClient must post a real multipart body (file stream), not JSON, for these calls.

### 2.5 Cross-module guide endpoints (added after the solution-wide audit)

These guide self-service capabilities live outside the ContentTours `/api/v1/guides` profile group and were missed by the first draft. All are verified existing endpoints.

Booking module (parent group `/api/v1/booking`):

| Capability | Method + route | Permission |
| --- | --- | --- |
| List own discounts | GET `/api/v1/booking/guide-discounts/mine` | TourBooking.ReadOwn |
| Create discount | POST `/api/v1/booking/guide-discounts` | TourBooking.Create |
| Update discount | PUT `/api/v1/booking/guide-discounts/{id}` | TourBooking.Update |
| Deactivate discount | DELETE `/api/v1/booking/guide-discounts/{id}` | TourBooking.Delete |
| Incoming join requests | GET `/api/v1/booking/join-requests?tourBookingId&myRequestsOnly` | JoinRequest.ReadOwn |
| Approve join request | POST `/api/v1/booking/join-requests/{id}/approve` | JoinRequest.Approve |
| Reject join request | POST `/api/v1/booking/join-requests/{id}/reject` | JoinRequest.Reject |

`CreateGuideDiscountRequest`: `TourId?, Name, Description?, GuideDiscountType DiscountType, decimal DiscountValue, string Currency, ValidFrom, ValidUntil?, MaxUsageCount?`.
`UpdateGuideDiscountRequest`: `Name, Description?, DiscountValue, ValidFrom, ValidUntil?, MaxUsageCount?`.

ContentTours module, apply-to-run-a-tour and proposals (parent group `/api/v1/tours`):

| Capability | Method + route | Permission |
| --- | --- | --- |
| Apply to run a tour | POST `/api/v1/tours/{tourId}/applications` | GuideApplication.Create |
| List my proposals | GET `/api/v1/tours/proposals` (backend list is a stub today) | TourProposal.Read |
| Create proposal draft | POST `/api/v1/tours/proposals` | TourProposal.Create |
| Submit proposal | POST `/api/v1/tours/proposals/{id}/submit` | TourProposal.Submit |

`ApplyForTourRequest`: `Message, RelevantExperience, ProposedBasePrice, ProposedScheduleJson`.
`CreateTourProposalRequest`: `Title, Description, ShortDescription, PlaceId, DurationMinutes, MaxGroupSize, BasePrice, Currency, RequestExclusive`.
The proposal `approve` and `reject` routes are admin-only (TourProposal.Approve / Reject) and are deliberately excluded from the guide area.

Accounts module, guide-side agency relationship (parent group `/api/v1/guides`, mounted by Accounts not ContentTours):

| Capability | Method + route | Permission |
| --- | --- | --- |
| List agency invitations received | GET `/api/v1/guides/me/invitations` -> `IReadOnlyList<InvitationDto>` | GuideAgency.Read |
| Apply to join an agency | POST `/api/v1/guides/agencies/{agencyUserId}/apply` (body `ApplyToAgencyRequest(string? Message)`) | GuideAgency.Create |
| Accept invitation | POST `/api/v1/guides/invitations/{id}/accept` | GuideAgency.Update |
| Decline invitation | POST `/api/v1/guides/invitations/{id}/decline` | GuideAgency.Update |
| Leave current agency | DELETE `/api/v1/guides/me/agency` | GuideAgency.Delete |

`AccountsFeatures.GuideAgency` permission actions are verified in `AccountsPermissionCatalog.cs`: Read (view invitations and applications), Create (apply to join), Update (accept or decline invitation), Delete (leave current agency).

## 3. Design decisions

### 3.1 Register and theme (impeccable)

Register: product. The design serves the task, it is not the product.

Theme scene sentence: "a Jordanian tour guide, between tours in daylight, glancing at their phone to confirm tomorrow's bookings and check this month's payout." That forces a light, calm, glanceable interface, not a dark ops console.

Color strategy: Restrained. Reuse the existing webestica tinted-neutral surfaces plus a single accent, the project primary `#5143d9`, kept under ~10% of any screen. Status colors (`success #0cbc87`, `warning #f7c32e`, `danger #d6293e`, `info #4f9ef8`) are used only to encode state, never for decoration, and always paired with a text label.

### 3.2 Quality bar (all four skills, applied within Razor + Bootstrap)

Hard rules for every page:

- No hero-metric template (one giant number over a small label with a gradient). The dashboard leads with "what needs your attention" and a calm activity summary, not a wall of vanity stats.
- No identical card grids and no row of three equal feature cards. Group with whitespace, dividers (`border-top`, `divide`), and a real visual hierarchy. Use a card only where elevation communicates a real boundary.
- No side-stripe accent borders, no gradient text, no default glassmorphism.
- Modals only for short confirmations or tiny edits. Anything longer is an inline page or panel.
- No em dashes in copy. No emoji as icons, use Bootstrap Icons (`bi-*`). No `Inter` as a display font (keep the theme's existing font stack).
- No placeholder filler such as "John Doe", "Acme", or "99.99%". Real fields only, with honest empty states.
- `.font-data` on every number, id, amount, and timestamp.
- Accessibility: body contrast at least 4.5:1, 4/8px spacing rhythm (tiers 16/24/32/48), touch targets at least 44px, color never the only signal, form labels above inputs with inline validation and visible focus.
- Every page ships its empty state, loading state, and error state, plus a first-run onboarding state where relevant (for example, a guide with no offerings yet).

## 4. Information architecture (pages)

Each page is one vertical slice: `Models/{X}/{XResponse, XRequest, XVm, XMapper}.cs` + `ApiClients/{X}ApiClient.cs` + `Facades/{X}Facade.cs` + `Controllers/{X}Controller.cs` + `Views/{X}/*.cshtml`, gated by a permission, with a sidebar entry.

1. Dashboard (overview). Endpoints: GET `/me`, `/me/tier`, `/me/earnings/summary`, `/me/analytics/overview`. Gate: TourGuideProfile.Read. Layout: a greeting and profile-completion nudge, a short "needs attention" list (pending applications, suspended offerings, upcoming bookings), a calm earnings-this-month line, and tier progress. Deliberately not a metric wall.
2. My profile. Endpoints: GET `/me`, PUT `/{id}`, PUT `/me/avatar`, PUT `/me/cover-image`, languages and specializations add/remove. Gate: TourGuideProfile.Read for view, TourGuide.Update / TourGuideProfile.Update for edits. Inline edit form, not a modal.
3. My tours. Endpoint: GET `/{id}/tours`. Gate: TourGuideProfile.Read. The tours the guide is assigned to, each linking into its offering.
4. My offerings (per tour: schedules, pricing tiers, private tour). Endpoints: the `/{tourId}/guide-offerings` tree. Gate: GuideOffering.Read / Create / Update / Delete. Schedules and pricing tiers are short CRUD, so small modals are acceptable here.
5. Availability. Endpoints: GET / POST / DELETE `/me/availability-blocks`. Gate: TourGuideProfile.Read / Update. A simple list of day-off blocks with add and remove.
6. Earnings. Endpoints: `/me/earnings/summary`, `/by-tour`, `/history`. Gate: TourGuideProfile.Read. A summary band, a by-tour breakdown table, and a paginated history.
7. Analytics. Endpoints: `/me/analytics/overview`, `/booking-trends`, `/popular-tours`, `/peak-days`. Gate: TourGuideProfile.Read. ApexCharts (the theme already loads it): a trends line, a popular-tours bar, a peak-days view, each guarded for empty data.
8. Applications. Endpoints: GET `/me/applications` (read own), POST `/api/v1/tours/{tourId}/applications` (apply to run a tour). Gate: TourGuideProfile.Read, GuideApplication.Create. Two parts: the status list of the guide's own applications (clear status badges, rejection reasons) and an apply flow that lets a guide submit `Message, RelevantExperience, ProposedBasePrice, ProposedScheduleJson` against an open tour. A browse-open-tours surface feeds the apply action (source list to be confirmed at build time).
9. Discounts. Endpoints: GET `/booking/guide-discounts/mine`, POST / PUT / DELETE `/booking/guide-discounts`. Gate: Booking.TourBooking (ReadOwn / Create / Update / Delete). A list of the guide's own discounts with add, edit, and deactivate. Create and edit are short CRUD, so small modals are acceptable.
10. Join requests. Endpoints: GET `/booking/join-requests?tourBookingId&myRequestsOnly`, POST `/booking/join-requests/{id}/approve`, POST `/booking/join-requests/{id}/reject`. Gate: Booking.JoinRequest (ReadOwn / Approve / Reject). Incoming requests from travelers wanting to join a confirmed booking the guide runs, with approve and reject actions and a clear empty state when there are none.
11. My proposals. Endpoints: GET `/tours/proposals`, POST `/tours/proposals`, POST `/tours/proposals/{id}/submit`. Gate: TourProposal.Read / Create / Submit. A guide proposes a brand-new tour (draft then submit for admin review). Note the backend list query is a stub returning an empty array today, so v1 builds the create and submit flow and shows a "list pending backend" empty state until the query lands.
12. My agency. Endpoints: GET `/guides/me/invitations`, POST `/guides/agencies/{agencyUserId}/apply`, POST `/guides/invitations/{id}/accept`, POST `/guides/invitations/{id}/decline`, DELETE `/guides/me/agency`. Gate: GuideAgency.Read / Create / Update / Delete. Manage the guide-to-agency relationship: see received invitations (accept or decline), apply to join an agency, and leave the current agency. The current agency state and any pending invitations drive the layout.
13. Reviews (conditional). Verify whether a guide-facing reviews endpoint exists (Social or ContentTours). If yes, a reviews list; if not, the profile's `AverageRating` and `ReviewCount` are surfaced on the Dashboard and this page is dropped from v1.

## 5. Shell (new `Areas/Guide`)

Mirror `Areas/Provider`:

- `Areas/Guide/Views/_ViewImports.cshtml` and `_ViewStart.cshtml` (match the Provider convention; confirm whether Provider uses a dedicated `_GuideLayout` or the shared `~/Views/Shared/_Layout.cshtml`).
- `Areas/Guide/Shared/_GuideSidebar.cshtml` + `GuideSidebarVm.cs`, with one nav entry per page, each wrapped in `<permission require="@WebPermission.X.Y">`.
- An active-key helper so each controller sets its current nav item (the same pattern as `_AdminSidebar` / `_ProviderSidebar`).

## 6. Permission additions

- Add `WebPermission.GuideOffering { Read, Create, Update, Delete, Suspend, Reinstate }` (search first to avoid a duplicate class).
- Add `WebPermission.TourProposal { Read, Create, Submit }` for the proposals page (the backend enforces `ContentToursFeatures.TourProposal`).
- Add `WebPermission.GuideAgency { Read, Create, Update, Delete }` for the agency page (the backend enforces `AccountsFeatures.GuideAgency`).
- Confirm Booking groups exist for the new pages: `WebPermission.TourBooking` (ReadOwn / Create / Update / Delete) for Discounts and `WebPermission.JoinRequest` (ReadOwn / Approve / Reject) for Join requests; add only the missing members.
- Confirm `WebPermission.GuideApplication` exposes Create (the apply-to-run-a-tour action on the Applications page); the group already exists per section 2.3.
- Confirm `WebPermission.TourGuideProfile` exposes Read, Update, DeleteOwn and `WebPermission.TourGuide` exposes Update; extend rather than duplicate if a member is missing.

## 7. Build order

Build clean after each slice with `dotnet build src/Hosts/YallaJo.Web/YallaJo.Web.csproj`.

1. Shell: `Areas/Guide` skeleton, `_GuideSidebar` + VM, view imports and start, WebPermission groups (`GuideOffering`, `TourProposal`, `GuideAgency`, and any missing Booking / GuideApplication members).
2. Dashboard.
3. My profile.
4. My tours, then offerings (schedules, pricing, private tour).
5. Availability.
6. Earnings.
7. Analytics.
8. Applications (status list + apply-to-run-a-tour).
9. Discounts.
10. Join requests.
11. My proposals (create and submit; list once backend query lands).
12. My agency.
13. Reviews (only if an endpoint exists).

## 8. How each skill shapes the work

| Skill | What it governs here |
| --- | --- |
| impeccable | Register (product), information architecture, the scene-driven light theme, the Restrained single-accent color strategy, the absolute bans, and the required empty / error / onboarding states. The primary lens. |
| design-taste-frontend | Anti-slop discipline: icon and color restraint, no three-equal-card rows, dividers over card overuse, honest data, label-above-input forms, tactile pressed states. |
| ui-ux-pro-max | Concrete checks: WCAG contrast, the 4/8px spacing rhythm, 44px touch targets, form labels with inline validation and focus management, loading and empty states. |
| huashu-design | Optional first step: a standalone HTML hi-fi prototype of the Dashboard (and 2 to 3 layout variations) to confirm direction before writing Razor, plus its anti-AI-slop checklist. |

## 9. Open questions / verify at build time

- Exact field lists for the earnings, analytics, tier, offering, schedule, pricing-tier, applications-list, assigned-tours, guide-discount, join-request, tour-proposal, and agency-invitation DTOs.
- Whether `Areas/Provider` uses a dedicated layout or the shared `_Layout`, so the Guide shell matches.
- Whether a guide-facing reviews endpoint exists (decides page 13).
- Whether `WebPermission.Attachment` / `WebPermission.EntityImage` groups already exist (needed for the image upload flow), and confirmation that `EntityType=TourGuide` is the value the attachment resolver expects for guide images.
- Whether `WebPermission.TourBooking`, `WebPermission.JoinRequest`, and `WebPermission.GuideApplication` already exist in the web project, and which members are missing (for pages 8, 9, 10).
- The source list that feeds the "browse open tours to apply" surface on page 8 (which endpoint returns tours currently open for guide applications).
- The backend `GET /api/v1/tours/proposals` list query is a stub today (returns an empty array); confirm when the real query lands so page 11's list can be wired.
- Whether to produce the optional huashu HTML hi-fi prototype before coding.

## 10. Decision needed

Approve this plan to start with step 1 (the shell), or request changes first. Optionally, ask for the huashu HTML hi-fi Dashboard prototype before any Razor is written.
