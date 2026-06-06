# Actor application — build plan to close the gaps

Companion to `actor-application-audit.md`. That document found the missing
"apply to become an actor" flows. This document is the build plan to close them.

Status: planning / advisory only. Nothing has been built. No code is written until approved.

All three gaps are **frontend only**. Every backend endpoint already exists and is verified;
no new backend command, endpoint, or migration is required. The section 7 open questions have
all been **verified against source** (DTO shapes, the ProviderType enum, and the WebPermission
catalog). One small web-side change surfaced: `WebPermission.Creator` is missing `Update` and
`RedeemInvitation` constants (see sections 6 and 7).

---

## 1. Scope

Three user-facing entry points are missing. This plan adds them, each as a vertical slice
that mirrors the existing gold pattern (Models -> ApiClient -> Facade -> Controller -> Views,
gated by a WebPermission and surfaced in the relevant sidebar).

| # | Gap | Kind | Backend | Area to touch |
|---|---|---|---|---|
| G1 | Become a tour guide | Frontend only | Reuses Accounts provider apply (ProviderType = IndependentGuide) | Areas/Provider (or Public entry) |
| G2 | Apply to become a content creator | Frontend only | ContentBlogs creator application endpoints (all exist) | Areas/Content |
| G3 | Register a business (start application) | Frontend only | ContentPlaces `POST /api/v1/places/businesses` | Areas/Business |

---

## 1a. Pages and endpoints at a glance

| Page | Area / route | New or edit | Endpoint(s) it calls | Permission gate |
|---|---|---|---|---|
| Become a guide (entry point + preset) | Provider, reuse `/provider/apply?type=IndependentGuide` | Entry point + preset on EXISTING form (form already supports the type) | `GET /api/v1/provider/status`; `POST /api/v1/provider/register`; `POST /api/v1/provider/apply` (ProviderType = IndependentGuide) | ProviderApplication.Register, ProviderApplication.Submit |
| Become a creator (apply) | Content, `/content/creators/apply` | New | `GET /api/v1/blogs/creators/niches` (anon); `POST /api/v1/blogs/creators/applications`; `POST /api/v1/blogs/creators/applications/{id}/submit` | Creator.Submit |
| My creator application (status / edit) | Content, `/content/creators/application` | New | `GET /api/v1/blogs/creators/applications/mine`; `PUT /api/v1/blogs/creators/applications/{id}` | Creator.Read, Creator.Update |
| Redeem creator invitation | Content, `/content/creators/redeem` | New | `POST /api/v1/blogs/creators/invitations/redeem` | Creator.RedeemInvitation |
| Register a business (start) | Business, `/business/register` | New | `POST /api/v1/places/businesses` | Business.Create |

Five pages total: 1 for G1, 3 for G2, 1 for G3. (The creator status and redeem pages can be
merged into one if the team prefers four pages; see open questions.)

---

## 2. G1 — Become a tour guide

### 2.1 What exists today
Becoming a guide is NOT a separate flow. It runs entirely through the Accounts provider
application: a user registers as a provider with `ProviderType = IndependentGuide`, an admin
approves the application, and ContentTours auto-creates the `TourGuide` profile via the
`ProviderApprovedCreateTourGuideHandler` integration-event handler. There is no guide-specific
backend endpoint and none is needed.

The gap is purely discoverability: the existing Provider apply page does not present an
"I am an independent guide" path, so a normal user has no obvious way to become a guide.

### 2.2 Plan (verified: even smaller than first thought)
The existing Provider/Apply form **already supports IndependentGuide**:
`ProviderMapper.TypeOptions()` already lists all six provider types, the apply dropdown
(`ProviderApplyVm.Type`) already binds it, and `RegisterProviderRequest.Type` is sent as the
enum name string. So a user can technically already become a guide today by choosing
"Independent guide" in the provider apply form. The only real gap is **discoverability**.

Recommended (lightest) approach:
- Add a labelled "Become a guide" entry point (Public nav and/or Provider area) that links to
  the existing apply form with `IndependentGuide` **pre-selected** (e.g. `/provider/apply?type=IndependentGuide`).
- Optionally tailor copy/help text when arriving via that entry so it reads as a guide flow.
- Reuse `ProviderApiClient` (`/register`, `/apply`, `/status`) and the existing
  `ProviderApplyVm` / `ProviderMapper` — **no new ApiClient, VM, mapper, or controller action**
  strictly required; a small preset (query param honored by the GET action) is enough.
- After submit, route to the existing provider status page (pending review).
- Admin moderation already works via Areas/Admin/Providers (the provider queue). No new admin page.

### 2.3 No backend work, minimal frontend work
Confirmed: no new command, endpoint, permission, or migration. Frontend work is reduced to a
discoverable entry point + a pre-select preset on the existing form.

---

## 3. G2 — Apply to become a content creator

### 3.1 What exists today
`CreatorsController` in Areas/Content is a blog composer for already-approved creators
(Profile, Write, Edit, MyBlogs, Submit). It has no apply action and no redeem-invitation
action. The web layer never calls any `/api/v1/blogs/creators/applications` route. All five
backend endpoints exist and are unused by the web.

### 3.2 Plan (vertical slice in Areas/Content)
- New `CreatorApplicationApiClient` (auto-registered by name suffix) wiring:
  - `GET /api/v1/blogs/creators/niches` (anon, for the niche picker)
  - `POST /api/v1/blogs/creators/applications` (create)
  - `GET /api/v1/blogs/creators/applications/mine` (status)
  - `PUT /api/v1/blogs/creators/applications/{id}` (edit while Draft / MoreInfoNeeded)
  - `POST /api/v1/blogs/creators/applications/{id}/submit` (submit)
  - `POST /api/v1/blogs/creators/invitations/redeem` (invite path)
- New `CreatorApplicationFacade` mapping responses to view models.
- New controller actions: Apply (GET/POST), Application status (GET + PUT edit), Redeem (GET/POST).
- New Models/CreatorApplication/{Response,Request,Vm,Mapper}. Reuse existing
  `CreatorNicheResponse`, `CreatorProfileResponse` under Models/Blogs where possible.
- Views: Apply form (niche picker, bio, links), Application status (shows
  Draft/Pending/Approved/Rejected/MoreInfoNeeded with the right empty/loading/error states),
  Redeem form.
- Permission gate: Creator.Submit / Creator.Read / Creator.Update / Creator.RedeemInvitation.
- Sidebar: add "Become a creator" entry; hide the composer pages until approved.

### 3.3 Status-driven UX
CreatorApplicationStatus = Draft, Pending, Approved, Rejected, MoreInfoNeeded. The status page
must render each state explicitly (color is not the sole indicator; use a text label + bi-* icon).

---

## 4. G3 — Register a business (start application)

### 4.1 What exists today
`MyBusinessesApiClient` has GetMine, GetById, Update, Resubmit — but NO Create. The Business
area only manages and resubmits businesses that already exist. The backend
`POST /api/v1/places/businesses` (Business.Create, CreateBusinessRequest) exists and is unused
by the web.

### 4.2 Plan (small extension in Areas/Business)
- Add `CreateAsync(POST /api/v1/places/businesses, CreateBusinessApiRequest)` to the existing
  `MyBusinessesApiClient`.
- Add a Register action (GET form + POST submit) to `MyBusinessesController`.
- Add Models/MyBusinesses/CreateBusinessApiRequest (mirror backend CreateBusinessRequest;
  confirm exact shape — see open questions). Reuse existing BusinessSummaryResponse /
  BusinessDetailResponse.
- View: Register a business form. After submit, route to the existing Manage page (pending review).
- Permission gate: Business.Create.
- Sidebar: add "Register a business" entry in the Business area; show it when the user has no
  business yet (empty-state CTA on MyBusinesses/Index).
- Admin review already works via Areas/Admin/Businesses. No new admin page.

---

## 5. Suggested build order

1. G3 (smallest: one ApiClient method + one form, reuses existing models).
2. G1 (reuses ProviderApiClient; mostly a new form + preset).
3. G2 (largest: full new vertical slice with 3 pages and status-driven UX).

Each step is independently shippable. After each, run the build check before moving on.

---

## 6. Permissions to confirm before build

The web `WebPermission` catalog must contain the gates each page uses. Verified against
`Infrastructure/Authorization/WebPermission.cs`:

- ProviderApplication.Register, ProviderApplication.Submit (G1) — **present** (lines 397-399).
- Business.Create (G3) — **present** (line 633).
- Creator.Submit, Creator.Read (G2) — **present** (lines 339-340).
- Creator.Update, Creator.RedeemInvitation (G2) — **MISSING**. The `WebPermission.Creator`
  class currently has only Read/Submit/Follow/Unfollow. Add:
  `public const string Update = "Permission.Creator.Update";` and
  `public const string RedeemInvitation = "Permission.Creator.RedeemInvitation";`
  (mirror the ContentBlogs backend names). This is the **only catalog change** this plan needs.

---

## 7. Open questions — RESOLVED (verified against source)

All five open questions were verified by reading the actual files. Results:

1. **G1 ProviderType selection — RESOLVED.** The enum is `Accounts.Domain.Enums.ProviderType : byte`
   with members `TourOperator=0, IndependentGuide=1, HotelResort=2, ActivityCenter=3, Agency=4,
   BusinessOwner=5`. The web Provider/Apply form **ALREADY supports selecting it**:
   `ProviderMapper.TypeOptions()` (Areas/Provider/Models/ProviderMapper.cs:7-18) returns all six
   types including `("IndependentGuide", "Independent guide")` and `("BusinessOwner", "Business owner")`,
   and `ProviderApplyVm.Type` (ProviderApplyVm.cs:10) is the bound dropdown value. The web
   `RegisterProviderRequest.Type` is sent as the enum **name string** (ProviderRequests.cs:7-8;
   `ProviderMapper.ToRegisterRequest` sends `vm.Type.Trim()`).
   => **G1 shrinks to discoverability only.** No new form is needed; options are: (a) deep-link/preset
   the existing apply form with IndependentGuide pre-selected, and/or (b) add a labelled "Become a
   guide" entry point that lands on the existing form. No new ApiClient, VM, or mapper required.

2. **G2 status vs redeem page count — DECISION (still a choice, not a blocker).** Recommended: keep
   status and redeem as **two** pages (different intents, different permission gates). The 1a table
   reflects this. Merging remains acceptable if the team prefers four total pages.

3. **G2 request shapes — RESOLVED.** From `ContentBlogs.Presentation/Endpoints/Creator/Models/CreatorRequests.cs`:
   - `CreateCreatorApplicationRequest(string? Bio, List<string>? PortfolioUrls, List<string>? SampleWorkUrls,
     List<Guid>? NicheIds, List<string>? FreeTags, List<Guid>? LanguageIds, List<Guid>? PreferredRegionIds,
     Dictionary<string,string>? SocialHandles)`
   - `UpdateCreatorApplicationRequest(...)` has the **identical** field set.
   - `RedeemCreatorInvitationRequest(string Token)`.
   - Niches are referenced by **`NicheIds` (List<Guid>)**, not slug. All fields are nullable/optional.
     Languages and preferred regions are also Guid lists; social handles are a string->string map.

4. **G3 CreateBusinessRequest shape — RESOLVED.** From
   `ContentPlaces.Presentation/Endpoints/Business/Models/CreateBusinessRequest.cs`:
   `CreateBusinessRequest(string Name, string? Slug, BusinessType BusinessType, Guid PlaceId,
   decimal Latitude, decimal Longitude, string? Description=null, string? Address=null, string? City=null,
   string? Country=null, string? PostalCode=null, string? Phone=null, string? Email=null, string? Website=null,
   string? LicenseNumber=null, string? TaxId=null, bool? IsHalal=null, bool? HasVegetarianOptions=null,
   bool? HasAlcoholFreeArea=null)`.
   Required fields: **Name, BusinessType (enum `ContentPlaces.Domain.Enums.BusinessType`), PlaceId (Guid),
   Latitude, Longitude**. Everything else is optional. The form needs a Place picker (PlaceId) and a
   BusinessType picker; hours/amenities/services are set later via the existing Business sub-pages, not at create.

5. **WebPermission groups — RESOLVED, with one gap.** Confirmed present in
   `Infrastructure/Authorization/WebPermission.cs`: `ProviderApplication.Register/Submit/Update` (lines 397-402),
   `Business.Create` (line 633). **GAP:** `WebPermission.Creator` (lines 337-343) only defines
   `Read, Submit, Follow, Unfollow` — it is **MISSING `Update` and `RedeemInvitation`**. These two
   constants must be **added** to the Creator class before G2b (edit application) and G2c (redeem) can
   be gated. This is the only catalog change required by this plan (a constants addition, not a backend change).

---

## 8. Design and process constraints

Honor the standing rules: Light "Restrained" theme (webestica tinted neutrals, one accent
#5143d9 under 10%, status colors for state only with text labels), 4/8px spacing tiers,
touch targets >= 44px, WCAG body contrast >= 4.5:1, color never the sole indicator, label-above-input
with inline error and visible focus, explicit empty/loading/error/onboarding states, `.font-data`
on numbers/ids/amounts/timestamps. No hero-metric template, no 3-equal-card rows, no side-stripe
borders, no gradient text, no glassmorphism, modals only for short CRUD, no em dashes, bi-* icons
only (no emoji), no Inter font, no placeholder filler names.

Planning/advisory only. No build, no git commit or push, until explicitly approved.
