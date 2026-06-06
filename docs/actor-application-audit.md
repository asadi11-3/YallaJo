# Actor application & onboarding audit

Whole-solution audit of every "apply to become an actor" / onboarding flow, mapping each
backend endpoint to whether a user-facing web page exists to initiate it.

Status: planning / advisory only. Nothing has been built from this document.

---

## 0. Executive summary

| Actor | Backend endpoint | Web "apply / become" page | Verdict |
|---|---|---|---|
| Account / user (register) | Yes | Yes (Auth/Register, VerifyEmail) | Complete |
| Invited user (accept invite) | Yes | Yes (Auth/AcceptInvite) | Complete |
| Provider (apply to sell tours) | Yes | Yes (Provider/Apply) | Complete |
| Guide -> run a specific tour | Yes | Yes (Guide/Applications) | Complete |
| Guide -> join an agency | Yes | Yes (Guide/Agency) | Complete |
| Guide -> propose a new tour | Yes | Yes (Guide/Proposals) | Likely complete (re-verify wiring) |
| Become a tour guide (the role) | Yes (via Accounts provider apply, ProviderType = IndependentGuide) | No | GAP: frontend only |
| Content creator (apply for role) | Yes | No | GAP: frontend only |
| Business owner (register a business) | Yes | No (manage/resubmit only) | GAP: frontend only |
| Preference onboarding (cold-start) | Yes | Accounts/Recommendations exists | Not a role enrollment |

Three real gaps, all **frontend only**: **become-a-guide** (backend already works through the
Accounts provider application with ProviderType = IndependentGuide, which auto-creates the
TourGuide profile on admin approval; only the user-facing entry page is missing),
**content-creator application** (all five backend endpoints ready, no page),
**business-owner initial application** (backend create endpoint ready, no start page).

See `actor-application-plan.md` for the build plan that closes these.

---

## 1. Backend onboarding/application endpoints per actor (verified file:line)

### 1.1 Account / user
- `POST /api/v1/auth/register` (anon, RegisterRequest) - RegistrationEndpoints.cs:16
- `POST /api/v1/auth/verify-email` (anon, VerifyEmailRequest) - CredentialEndpoints.cs:20
- `POST /api/v1/auth/resend-otp` (anon, ResendOtpRequest) - CredentialEndpoints.cs:94
- `GET /api/v1/auth/invitations/roles` (SecurityFeatures.User/Create) - InvitationEndpoints.cs:30
- `POST /api/v1/auth/invitations` (User/Create, InviteUserRequest) - InvitationEndpoints.cs:56
- `POST /api/v1/auth/invitations/accept` (anon, AcceptInviteRequest) - InvitationEndpoints.cs:92
- `POST /api/v1/auth/invitations/resend` (User/Create, ResendInviteRequest) - InvitationEndpoints.cs:124

### 1.2 Provider
- `GET /api/v1/provider/status` (ProviderApplication/Read) - ProviderEndpoints.cs:31
- `POST /api/v1/provider/register` (ProviderApplication/Register, RegisterProviderRequest) - :48
- `POST /api/v1/provider/apply` (ProviderApplication/Submit) - :70
- `POST /api/v1/provider/documents` (Create, AddProviderDocumentRequest) - :85
- `POST /api/v1/provider/documents/upload` (Create, UploadProviderDocumentRequest [FromForm]) - :105
- `PUT /api/v1/provider/documents/{id}` (Update, ReplaceProviderDocumentRequest) - :138
- `POST /api/v1/provider/reapply` (Update) - :186
- Admin: GET /admin/providers (AdminProviderQueue/Read, AdminProviderEndpoints.cs:29), GET /{id} (:51),
  POST /{id}/approve (:66), /reject (RejectProviderRequest, :82), /request-docs (RequestMoreDocsRequest, :98),
  /suspend (SuspendProviderRequest, :114), /reinstate (:130)

### 1.3 Tour guide
- No dedicated "create guide application / become-a-guide" endpoint exists (confirmed by exhaustive search).
- Closest read: `GET /api/v1/guides/me/applications` (TourGuideProfile/Read) - TourGuideProfileEndpoints.cs:69
- Apply to RUN a specific tour:
  - `GET /api/v1/tours/{tourId}/applications` (GuideApplication/Read) - GuideApplicationEndpoints.cs:27
  - `POST /api/v1/tours/{tourId}/applications` (GuideApplication/Create, ApplyForTourRequest) - :51
  - `POST .../{applicationId}/approve` (:78), `/reject` (RejectGuideApplicationRequest, :98)
  - `POST /tours/{tourId}/open-applications` (TourGuide/Update, :118), `/close-applications` (:136)
- Adjacent (propose a new tour):
  - `POST /api/v1/tours/proposals` (TourProposal/Create, CreateTourProposalRequest) - TourProposalEndpoints.cs:39
  - `POST /api/v1/tours/proposals/{id}/submit` (TourProposal/Submit) - :67

### 1.4 Guide <-> agency
- Guide-side: `GET /api/v1/guides/me/invitations` (GuideAgency/Read, GuideAgencyEndpoints.cs:24);
  `POST /guides/agencies/{agencyUserId}/apply` (GuideAgency/Create, ApplyToAgencyRequest, :37);
  `POST /guides/invitations/{id}/accept` (Update, :53); `/decline` (:68);
  `DELETE /guides/me/agency` (Delete, :82)
- Agency-side: `GET /api/v1/agency/applications` (AgencyRoster/Read, AgencyEndpoints.cs:38);
  `POST /agency/guides/invite` (Create, InviteGuideRequest, :51);
  `POST /agency/applications/{id}/approve` (:70); `/reject` (RejectGuideApplicationRequest, :85);
  `GET /agency/guides` (:25); `GET /agency/invitations/sent` (AgencyPublicEndpoints.cs:57);
  `GET /agency/guides/available` (:45); `GET /api/v1/agency` (anon discovery, :22)
- No separate agency-registration endpoint exists.

### 1.5 Content creator
- `POST /api/v1/blogs/creators/applications` (Creator/Submit, CreateCreatorApplicationRequest) - CreatorEndpoints.cs:86
- `GET /api/v1/blogs/creators/applications/mine` (Creator/Read) - :114
- `PUT /api/v1/blogs/creators/applications/{id}` (Creator/Update, UpdateCreatorApplicationRequest) - :127
- `POST /api/v1/blogs/creators/applications/{id}/submit` (Creator/Submit) - :158
- `POST /api/v1/blogs/creators/invitations/redeem` (Creator/RedeemInvitation, RedeemCreatorInvitationRequest) - :262
- Admin: GET /blogs/admin/creators/applications (AdminCreatorQueue/Read, AdminCreatorEndpoints.cs:36),
  GET /{id} (:56), POST /{id}/approve (ApproveApplicationRequest, :73), /reject (RejectApplicationRequest, :94),
  /request-more-info (RequestMoreInfoRequest, :115), POST /admin/creators/invitations (Invite, SendInvitationRequest, :276)
- DTOs in CreatorRequests.cs lines 3/13/29/35/39/41/45.

### 1.6 Business owner
- `POST /api/v1/places/businesses` (Business/Create, CreateBusinessRequest) - BusinessEndpoints.cs:147
- `POST /api/v1/places/businesses/{id}/resubmit` (Business/Submit) - :233
- `GET /api/v1/places/businesses/mine` (Business/Read) - :389
- Admin review: POST /places/businesses/admin/{id}/approve (Business/Approve + RequireAuthorization 'Admin', :253),
  /reject (RejectBusinessRequest, :270), /request-more-docs (RequestMoreDocsRequest, :289),
  /suspend (SuspendBusinessRequest, :308), /reinstate (:327)

### 1.7 Other
- `POST /api/v1/analytics/recommendations/onboarding` (Preference/Update, SubmitOnboardingRequest) - RecommendationsEndpoints.cs:89
  = cold-start preference onboarding, NOT role enrollment.
- No reviewer-onboarding endpoint exists.

---

## 2. Web-layer apply-page verdicts per actor (verified file:line)

- Provider application: CONSUMED. Provider/Views/Provider/Status.cshtml:14-20 links to Apply;
  Apply.cshtml:17-63 form; ProviderController.cs:35-63 (GET/POST /provider/apply);
  ProviderApiClient.cs:26-37 (/register, /apply, /reapply).
- Guide "become a guide" onboarding: PAGE-MISSING / NOT CONSUMED. No onboarding/register page or route;
  also no backend endpoint exists for it (backend + frontend gap).
- Guide apply-to-run-a-tour: CONSUMED. Guide/Views/Applications/Index.cshtml:33-65;
  Guide/Controllers/ApplicationsController.cs:45-73 (POST /guide/applications/apply);
  Guide/ApiClients/ApplicationsApiClient.cs:25-30 (POST /api/v1/tours/{tourId}/applications).
- Guide agency apply/invitations: CONSUMED. Guide/Views/Agency/Index.cshtml:33-47 + 64-117;
  Guide/Controllers/AgencyController.cs:33-50 & 52-79; Guide/ApiClients/AgencyApiClient.cs:15-28.
- Content creator application: PAGE-MISSING. No customer-facing creator application page in
  Areas/Content/Views/Creators (only Profile/MyBlogs/Write); web layer has NO call to
  /api/v1/blogs/creators/applications or /invitations/redeem. Only admin moderation exists
  (Admin/CreatorsController.cs:19-141, Admin/CreatorsApiClient.cs:16-49).
- Invitation redeem: CONSUMED but via Auth/AcceptInvite (POST /api/v1/auth/invitations/accept),
  NOT the creator-specific /invitations/redeem. Auth/AcceptInvite/Index.cshtml:13-30;
  AcceptInviteController.cs:16-44; AcceptInviteApiClient.cs:11-13.
- Business owner application: PAGE-MISSING for STARTING an application. Business area has management +
  resubmit only: MyBusinessesController.cs:20-35 & 71-79 (resubmit), MyBusinessesApiClient.cs:11-35
  (/places/businesses/mine, /{id}, /{id}/resubmit), Views MyBusinesses/Index.cshtml + Manage.cshtml.
  No web call to POST /api/v1/places/businesses (create) found.

---

## 3. The three real gaps

1. Become a tour guide: no path at all. No backend endpoint to submit a "become a guide" application
   and no web page. The only guide-related applications (apply to run a tour, join an agency) assume
   you are already a guide. Admin has a Guide-applications moderation screen but nothing feeds it from
   the user side. Needs a backend command first, then a page.
2. Content creator application: backend ready, page missing. All five endpoints + admin moderation exist;
   no user-facing page to start/submit an application or redeem a creator invitation. Frontend-only fix.
3. Business owner initial application: backend ready, start page missing. POST /places/businesses exists
   but no web call to it; the Business area only manages/resubmits existing businesses. Frontend-only fix.

---

## 4. Note: docs/endpoint-coverage-and-gaps.md is partially stale

Since that doc was written, the web project grew to 8 areas (Accounts, Admin, Auth, Business, Content,
Guide, Provider, Public). Several items it flagged as PAGE-MISSING have since been built:
- Guide area now full: Agency, Analytics, Applications, Availability, Dashboard, Discounts, Earnings,
  JoinRequests, MyTours, Profile, Proposals, Reviews (12 view folders).
- Business area now exists: Accessibility, Amenities, Hours, MyBusinesses, Services, Staff.
- Admin gained: Moderation, SeoFaq, SeoMetadata, SeoRedirects, SeoSitemap, SeoWeather,
  EntityCategories, EntityTags.
- Accounts gained: Invoices, Payments, Recommendations, Reviews.

That audit doc's gap claims should be re-verified against the current tree before being trusted.
