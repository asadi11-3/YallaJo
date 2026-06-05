# Content Creator Dashboard - Build Plan

> Status: PROPOSED (awaiting approval). Planning only, nothing built yet.
> Scope: a content-creator-FACING self-service area (the creator's own dashboard and pages).
> This is NOT the admin moderation of creators, which already exists under `Areas/Admin/Creators`.

## 1. Goal

Give an authenticated content creator their own area in the existing web app (`src/Hosts/YallaJo.Web`) to apply or onboard, manage their creator profile, write and manage their articles, submit them for review, and see their audience, reusing the backend endpoints that already exist and the existing webestica Bootstrap theme. No new backend endpoints are required. The only backend-side change is extending a web permission group (the routes already enforce it server-side).

## 1a. Pages and endpoints at a glance

How many pages: 5 core pages, plus 2 optional pages, so 5 to 7 total. There is no Earnings page because no creator monetization endpoint exists.

Every page is one route in a new `Areas/Creator`. Creator-self routes live under `/api/v1/blogs/creators`; article routes live under `/api/v1/blogs`.

| # | Page | Endpoint(s) it calls | Permission gate |
| --- | --- | --- | --- |
| 1 | Dashboard | GET `/blogs/creators/profile/mine`; GET `/blogs/my-blogs?status=Draft` (drafts and pending needing attention) | Creator.Read, Blog.ReadOwn |
| 2 | My profile | GET `/blogs/creators/profile/mine`; PUT `/blogs/creators/profile/mine`; PUT `/blogs/creators/profile/mine/avatar`; PUT `/blogs/creators/profile/mine/cover-image`; DELETE `/blogs/creators/profile/mine` (self-deactivate) | Creator.Read / Update / Delete |
| 3 | My articles (list) | GET `/blogs/my-blogs?page&pageSize&status` | Blog.ReadOwn |
| 4 | Article editor (create and edit) | POST `/blogs`; GET `/blogs/{id}` (read own to get RowVersion); PUT `/blogs/{id}`; POST `/blogs/{id}/submit-for-review`; DELETE `/blogs/{id}`; POST `/blogs/{id}/restore`; optional POST/DELETE `/blogs/{id}/tours` (link tours) | Blog.Create / Update / Submit / DeleteOwn (+ BlogTourLink if link-tours kept) |
| 5 | Application and onboarding (conditional, shown until approved) | GET `/blogs/creators/applications/mine`; POST `/blogs/creators/applications`; PUT `/blogs/creators/applications/{id}`; POST `/blogs/creators/applications/{id}/submit`; POST `/blogs/creators/invitations/redeem`; GET `/blogs/creators/niches` (niche picker) | Creator.Read / Submit / Update / RedeemInvitation |
| 6 | Audience and followers (optional) | GET `/blogs/creators/profiles/{profileId}/followers`; profile FollowerCount from page 1 | Creator.Read |
| 7 | Public profile preview (optional) | GET `/blogs/creators/profiles/{slug}`; GET `/blogs/creators/profiles/{slug}/blogs` | (public) |

Notes:
- The creator publishes by submitting for review (`submit-for-review`, gated Blog.Submit). The direct publish, unpublish, and archive transitions are gated Blog.Approve, which is an admin or elevated action, so they are not on the creator dashboard.
- No new backend endpoints are required. The only backend-side change is extending the web `WebPermission.Creator` group to add `Update`, `Delete`, and `RedeemInvitation` (the routes already enforce those server-side). The web `Blog` group is already complete.
- Images (avatar, cover image, and any article image) are NOT uploaded through the page endpoints above. Every image is handled as an attachment through the shared ContentCore attachment endpoints (see section 2.5). The page endpoints that take an image URL (for example PUT `/profile/mine/avatar`) receive the URL that the attachment upload returns; the upload itself is always a separate multipart call to ContentCore first.

## 2. What already exists (verified)

### 2.1 Creator-self endpoints (group `/api/v1/blogs/creators`, permission `Creator`)

| Capability | Method + route | Permission |
| --- | --- | --- |
| Own profile | GET `/profile/mine` -> `CreatorProfileDto` | Creator.Read |
| Update profile | PUT `/profile/mine` (DisplayName, Bio, AvatarUrl, NewSlug) | Creator.Update |
| Avatar / cover | PUT `/profile/mine/avatar` ; PUT `/profile/mine/cover-image` | Creator.Update |
| Self-deactivate | DELETE `/profile/mine` (soft delete, 60-day hard delete) | Creator.Delete |
| My application | GET `/applications/mine` -> `CreatorApplicationDto` | Creator.Read |
| Create application | POST `/applications` (Bio, PortfolioUrls, SampleWorkUrls, NicheIds, FreeTags, LanguageIds, PreferredRegionIds, SocialHandles) | Creator.Submit |
| Update application | PUT `/applications/{id}` (Draft or MoreInfoNeeded only) | Creator.Update |
| Submit application | POST `/applications/{id}/submit` | Creator.Submit |
| Redeem invitation | POST `/invitations/redeem` (Token) | Creator.RedeemInvitation |
| Niches | GET `/niches` -> `List<CreatorNicheDto>` | (public) |
| Public profile | GET `/profiles/{slug}` -> `CreatorProfileDto` | (public) |
| Followers | GET `/profiles/{profileId}/followers?page&pageSize` | (public) |
| Follow / unfollow | POST / DELETE `/profiles/{profileId}/follow` | Creator.Follow / Unfollow |
| Public posts | GET `/profiles/{slug}/blogs?page&pageSize` -> `PaginatedResult<BlogSummaryDto>` | (public) |

`CreatorProfileDto`: `Id, UserId, Slug, DisplayName, Bio?, AvatarUrl?, TrustTier, Status, ArticleCount, TotalViewCount, TotalReactionCount, TotalCommentCount, FollowerCount, LinkedProviderId?, CreatedAt`.

`CreatorApplicationStatus`: Draft, Pending, Approved, Rejected, MoreInfoNeeded.

### 2.2 Article (blog) endpoints (group `/api/v1/blogs`, permission `Blog`)

| Capability | Method + route | Permission |
| --- | --- | --- |
| My articles | GET `/my-blogs?page&pageSize&status` -> `PaginatedResult<BlogSummaryDto>` | Blog.ReadOwn |
| Create draft | POST `/` (Title, Content, SourceLanguageCode, Slug, Summary, MetaTitle, MetaDescription, PlaceId) -> `CreateBlogResult` | Blog.Create |
| Read one | GET `/{id}` -> `BlogDetailDto` (also `/slug/{slug}`) | (public) |
| Update | PUT `/{id}` (RowVersion, Title, Slug, Content, Summary, MetaTitle, MetaDescription, PlaceId, ReadTimeMinutes) | Blog.Update |
| Submit for review | POST `/{id}/submit-for-review` (RowVersion) | Blog.Submit |
| Delete / restore | DELETE `/{id}` (RowVersion) ; POST `/{id}/restore` (RowVersion) | Blog.DeleteOwn |
| Link / unlink tours | POST `/{id}/tours` ; DELETE `/{id}/tours/{tourId}` | BlogTourLink.Create / Delete |
| Track view | POST `/{id}/views` | (public) |

`BlogStatus` drives the `/my-blogs` status filter (Draft, PendingReview, Published, Archived, Hidden, and similar; exact members to confirm at build). `BlogSummaryDto` and `BlogDetailDto` exact field lists are to be confirmed at build time.

### 2.3 Web shell to mirror

`Areas/Provider` is the closest existing self-service area and the template to copy (per-feature ApiClients / Controllers / Facades, per-feature Models, `Shared/_ProviderSidebar.cshtml` + `Shared/ProviderSidebarVm.cs`, Views per feature). `Areas/Creator` does not exist yet.

### 2.4 Permissions

`WebPermission.cs` already has a `Creator` group with `Read, Submit, Follow, Unfollow` and a complete `Blog` group. The `Creator` group is missing `Update`, `Delete`, and `RedeemInvitation`, which the profile edit, self-deactivate, and invitation-redeem flows need. These must be added (extend, do not duplicate). If a `BlogTourLink` web group does not exist and the link-tours feature is kept, add it with `Create` and `Delete`.

### 2.5 Image and file handling (ContentCore attachments)

Every image in this area (creator avatar, cover image, and any image inside an article) is handled as an attachment through the shared ContentCore attachment endpoints, not through bespoke per-page upload routes. These endpoints already exist and are reused as-is.

| Capability | Method + route | Notes |
| --- | --- | --- |
| Upload one file | POST `/attachments` (multipart/form-data: `IFormFile file` + `EntityType`, `EntityId`, `AttachmentType`, optional `Width`, `Height`, `DurationSeconds`, `SortOrder`) -> `UploadAttachmentResult` | Attachment.Create |
| Bulk upload images | POST `/attachments/images?entityType&entityId` (multipart/form-data: `IFormFileCollection files`, up to 20) -> `BulkUploadImagesResult` | Attachment.Create |
| List for an entity | GET `/attachments?entityType&entityId` -> `IReadOnlyList<AttachmentDto>` | Attachment.Read |
| Get one | GET `/attachments/{id}` -> `AttachmentDto` | Attachment.Read |
| Delete | DELETE `/attachments/{id}` | Attachment.Delete |
| Reorder | PUT `/attachments/reorder` (EntityType, EntityId, OrderedAttachmentIds) | Attachment.Update |
| Set primary image | PUT `/attachments/primary` (EntityType, EntityId, AttachmentId) | EntityImage.Update |

How this maps to creator pages:

- `EntityType` for an article image is `Blog`; for the creator avatar and cover it is the creator/profile entity type the resolver supports. The `EntityType` enum is shared across modules (Place, Business, Tour, Blog, Review, TourGuide).
- Flow for a profile avatar or cover: upload the file to POST `/attachments` (or `/attachments/images`) with the correct `EntityType` and `EntityId`, take the returned attachment URL, then call the profile route that stores it (PUT `/profile/mine/avatar` or `/profile/mine/cover-image`). The profile and cover routes only persist a URL; they never receive the file bytes.
- Flow for article images: upload to `/attachments` (or bulk `/attachments/images`) with `EntityType=Blog` and the blog id, then reorder or set-primary as needed. The article create and update routes carry text only.
- Permissions are `ContentCoreFeatures.Attachment` (Create, Read, Update, Delete) and `ContentCoreFeatures.EntityImage` (Update, for set-primary). The web shell needs matching `WebPermission.Attachment` and `WebPermission.EntityImage` groups; verify whether they already exist before adding (extend, do not duplicate).
- The upload endpoints use `multipart/form-data` and disable antiforgery, so the web ApiClient must post a real multipart body (file stream), not JSON, for these calls.

## 3. Design decisions

### 3.1 Register and theme (impeccable)

Register: product. The design serves the writing and management task.

Theme scene sentence: "a Jordanian travel creator in a cafe in daylight, drafting an article on a laptop and glancing at this week's views and which drafts still need finishing." That forces a light, calm, reading-and-writing interface, not a dark analytics console.

Color strategy: Restrained. Reuse the existing webestica tinted-neutral surfaces plus a single accent, the project primary `#5143d9`, kept under ~10% of any screen. Status colors (`success #0cbc87`, `warning #f7c32e`, `danger #d6293e`, `info #4f9ef8`) encode state only (draft, pending review, published, rejected) and always carry a text label.

### 3.2 Quality bar (all four skills, applied within Razor + Bootstrap)

- No hero-metric template. The Dashboard leads with drafts that need finishing and articles awaiting review, then a calm activity summary (articles, views, reactions, comments, followers), not a vanity-number wall.
- No identical card grids and no row of three equal cards. Group with whitespace and dividers. A card only where elevation marks a real boundary.
- No side-stripe accent borders, no gradient text, no default glassmorphism. Modals only for short confirmations.
- No em dashes in copy. No emoji as icons, use Bootstrap Icons (`bi-*`). No `Inter` display font. No placeholder filler such as "John Doe" or "99.99%".
- `.font-data` on every count, id, slug, and timestamp.
- Accessibility: body contrast at least 4.5:1, 4/8px spacing rhythm, touch targets at least 44px, color never the only signal, form labels above inputs with inline validation and visible focus.
- The article editor is the most important surface: a clean title and body with a clear draft, submit, and delete affordance, plus an honest first-run empty state for a creator with no articles yet.
- Every page ships its empty, loading, and error states, plus the onboarding state (a user who is not yet an approved creator sees the Application page, not the article tools).

## 4. Information architecture (pages)

Each page is one vertical slice: `Models/{X}/{XResponse, XRequest, XVm, XMapper}.cs` + `ApiClients/{X}ApiClient.cs` + `Facades/{X}Facade.cs` + `Controllers/{X}Controller.cs` + `Views/{X}/*.cshtml`, gated by a permission, with a sidebar entry. The detail per page is in the table in section 1a and the endpoint tables in section 2.

1. Dashboard. Calm activity summary plus a needs-attention list (drafts to finish, articles in review). Not a metric wall.
2. My profile. Inline edit of display name, bio, slug, avatar, cover, plus self-deactivate behind a confirmation.
3. My articles. A filterable list of the creator's own posts with status badges and per-row actions.
4. Article editor. Create a draft, edit it, submit for review, delete, restore, and optionally link tours. Reads the article by id to carry the RowVersion for concurrency.
5. Application and onboarding. Shown until the user is an approved creator: create, edit, and submit the application, redeem an invitation, pick niches, and track status with clear status badges and any reviewer note.
6. Audience and followers (optional). Follower count and list.
7. Public profile preview (optional). A read-only view of how the public profile and posts look.

## 5. Shell (new `Areas/Creator`)

Mirror `Areas/Provider`:

- `Areas/Creator/Views/_ViewImports.cshtml` and `_ViewStart.cshtml` matching the Provider convention.
- `Areas/Creator/Shared/_CreatorSidebar.cshtml` + `CreatorSidebarVm.cs`, one nav entry per page, each wrapped in `<permission require="@WebPermission.X.Y">`.
- An active-key helper so each controller marks its current nav item.

## 6. Permission additions

- Extend `WebPermission.Creator` to add `Update`, `Delete`, `RedeemInvitation`.
- Add `WebPermission.BlogTourLink { Create, Delete }` only if the link-tours feature is kept in v1 (search first to avoid a duplicate class).
- The `Blog` group is already complete.

## 7. Build order

Build clean after each slice with `dotnet build src/Hosts/YallaJo.Web/YallaJo.Web.csproj`.

1. Shell: `Areas/Creator` skeleton, `_CreatorSidebar` + VM, view imports and start, the `Creator` permission extension.
2. Dashboard.
3. My profile.
4. My articles.
5. Article editor.
6. Application and onboarding.
7. Audience and followers (optional).
8. Public profile preview (optional).

## 8. How each skill shapes the work

| Skill | What it governs here |
| --- | --- |
| impeccable | Register (product), information architecture, the scene-driven light theme, the Restrained single-accent color strategy, the absolute bans, and the required empty / error / onboarding states. The primary lens. |
| design-taste-frontend | Anti-slop discipline: icon and color restraint, no three-equal-card rows, dividers over card overuse, honest data, label-above-input forms, tactile pressed states. |
| ui-ux-pro-max | Concrete checks: WCAG contrast, the 4/8px spacing rhythm, 44px touch targets, form labels with inline validation and focus management, loading and empty states. |
| huashu-design | Optional first step: a standalone HTML hi-fi prototype of the Dashboard and the article editor (and 2 to 3 layout variations) to confirm direction before writing Razor, plus its anti-AI-slop checklist. |

## 9. Open questions / verify at build time

- Exact field lists for `BlogSummaryDto`, `BlogDetailDto`, `CreatorNicheDto`, and `CreatorApplicationDto`.
- How the creator reads their own draft to obtain the RowVersion for an edit (whether GET `/{id}` returns the owner's draft, or another read is needed).
- The exact `BlogStatus` enum members for the status filter.
- Whether `Areas/Provider` uses a dedicated layout or the shared `_Layout`.
- Confirmation that there is no creator monetization or earnings endpoint (so no Earnings page).
- The exact `EntityType` value the attachment resolver expects for a creator avatar and cover (article images use `EntityType=Blog`), and whether `WebPermission.Attachment` / `WebPermission.EntityImage` groups already exist.

## 10. Decision needed

Approve this plan to start with step 1 (the shell), or request changes first. Optionally, ask for the huashu HTML hi-fi prototype of the Dashboard and article editor before any Razor is written.
