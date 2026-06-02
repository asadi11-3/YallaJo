# YallaJo Account Self-Service Pages — Implementation Plan

## Goal
Build 5 account self-service pages in `src/Hosts/YallaJo.Web/Areas/Accounts/` (vertical-slice,
full-chrome `_Layout`, Webestica dashboard shell): **Profile, Settings, Bookings, Wishlist, Delete**.

## Hard constraints / conventions
- Pipeline per slice: `XxxController : BaseController` -> `XxxFacade` -> `XxxApiClient` -> `IApiClient`.
- Controllers: `[Area("Accounts")] [Authorize]`, `: BaseController` (`YallaJo.Web.Infrastructure.Mvc`),
  every action `async Task<IActionResult>` + trailing `CancellationToken ct`, call
  `if (GuardSignOut(result) is { } so) return so;` after each facade call, use `SetSuccess`/`SetError` + PRG.
- Errors are values: `ApiResult` / `ApiResult<T>` (`YallaJo.Web.Infrastructure.Api.Contracts`).
- DI is suffix-based (FeatureServiceRegistration scans `*ApiClient`/`*Facade`) — **no Program.cs edits**.
- Controllers NEVER call `IApiClient` directly (only via Facade). Facades NEVER touch HttpContext/TempData.
- Images: inject `IApiAssetUrlResolver` (`YallaJo.Web.Services`, singleton) and call `Resolve(url)` on any
  API-relative attachment path before `<img src>`.
- All API routes are prefixed `/api/v1/...`.
- Flash via shared `_Alerts` partial (TempData Success/Error). No inline alert markup.
- `aos.js` is NOT loaded by `_Layout` — drop `data-aos` attributes (static render is fine).
  flatpickr + choices + functions.js (eye-toggle) ARE loaded.
- Webestica classes available: `card.border`, `card-header-title`, `nav-pills-primary-soft`,
  `btn-primary-soft`, `avatar avatar-xl`, `.fakepassword`/`.fakepasswordicon` eye-toggle, `.flatpickr`, `.js-choice`.
- Build/verify: `dotnet build src\Hosts\YallaJo.Web\YallaJo.Web.csproj` ALONE (VBCSCompiler lock); app may be
  running so a benign MSB3027/MSB3021 copy-step failure is NOT a compile error — verify via
  `Select-String 'error CS|error RZ|Build succeeded|MSB3027|MSB3021'` (only MSB lines = clean compile).

## Existing reusable pieces
- Profile slice: `ProfileController/ProfileFacade/ProfileApiClient`, `ProfileMapper`, `ProfileVm`,
  `UpdateProfileVm` (FirstName/LastName/DateOfBirth/Gender/Country/City/AddressLine),
  `UpdateAvatarVm{IFormFile File}`, `GenderOption{Male=0,Female=1}`, `_UpdateProfileForm.cshtml`.
  ProfileApiClient already has: GetProfileAsync (GET /api/v1/accounts/profile), UpdateProfileAsync
  (PUT), UpdateAvatarAsync (PUT /avatar PutFileAsync field "file"), DeleteAvatarAsync (DELETE /avatar),
  DeleteProfileAsync (DELETE /api/v1/accounts/profile).
- ChangePassword slice: PUT /api/v1/security/account/password (CurrentPassword/NewPassword/ConfirmNewPassword).
- UpdatePhone slice: PUT /api/v1/security/account/phone (PhoneNumber).
- Auth area already wires Sessions (GET/DELETE /api/v1/auth/sessions), Devices (PATCH
  /api/v1/auth/devices/{id}/trust), LogoutAll (POST /api/v1/auth/logout-all), Auth/Logout action.

## Confirmed API contracts (all /api/v1)
- Booking: GET /booking/my-bookings (query status=comma BookingStatus names, cursor, pageSize<=50,
  countTotal) -> MyBookingsPage{Items:[MyBookingItemDto{Id,Reference,Status,TourId,ProviderId,
  ParticipantCount,TotalAmount,Currency,IsInstantBooking,PaymentExpiresAt,ConfirmedAt,CancelledAt,
  CompletedAt,CreatedAt}],NextCursor,TotalCount}. GET /booking/{id} -> TourBookingDetailDto
  (Reference,Status,TourId,ParticipantCount,Pricing{Subtotal,DiscountAmount,LoyaltyAmount,TotalAmount,
  Currency,LineItems[{TierType,Count,UnitPrice}]},SpecialRequests,Confirmation/Rejection/Cancellation/
  Completion,CreatedAt). POST /booking/{id}/cancel (CancelTourBookingRequest{string? Reason}) ->
  CancelTourBookingResult. BookingStatus token mapping (facade-side): Upcoming = AwaitingPayment +
  PendingConfirmation + Confirmed; Canceled = "Cancelled" (note spelling); Completed = "Completed".
- Social favorites: GET /social/favorites (afterCursor Guid?, pageSize<=50) ->
  FavoritePageDto{Items:[FavoriteDto{Id,UserId,EntityType(string),EntityId(Guid),AddedAt}],NextCursor}.
  DELETE /social/favorites/{entityType}/{entityId}. NO batch delete (remove-all loops DELETEs).
  FavoriteEntityType: Tour=0,Place=1,Business=2,Blog=3,TourGuide=4 (EntityType arrives as the string name).
- Detail (anonymous): GET /places/{id} -> PlaceDetailDto(Name,City,Country,AverageRating,...);
  GET /places/businesses/{id} -> BusinessDetailDto(Name,City,Country,AverageRating,...);
  GET /tours/{id} -> TourDetailDto(Name,BasePrice,SalePrice,Currency,AverageRating,PlaceId,...);
  GET /guides/{id} -> TourGuideProfileDto(DisplayName,AvatarUrl,...); GET /blogs/{id} ->
  BlogDetailDto(Title,Summary,...). NO image field except TourGuide.AvatarUrl.
- Images: GET /content-core/attachments?entityType=&entityId= -> [AttachmentDto{Id,EntityType,EntityId,
  Type,Url,ThumbnailUrl,SortOrder,...}] (use first by SortOrder: ThumbnailUrl ?? Url, Resolve()).
- Notifications: GET/PUT /notifications/preferences. GET -> [NotificationPreferenceDto{Type,Channel,
  IsEnabled}]. PUT UpdatePreferencesRequest{Updates:[PreferenceUpdate(Type,Channel,IsEnabled)]}.
- Marketing consent: GET/PUT /accounts/me/marketing-consent
  (MarketingConsentRequest/Result{EmailDigest,PushNotifications,ReEngagementCampaigns}).
- Profile soft-delete + restore: DELETE /accounts/profile (wired); POST /accounts/profile/restore (ADD).

## Hydration strategy (Wishlist + Bookings cards)
No batch-by-ids endpoint. Per entity: (1) detail endpoint (switch on EntityType) for title/price/location,
(2) attachments?entityType=&entityId= for image (first AttachmentDto ThumbnailUrl??Url -> Resolve()).
Parallelize in facade with Task.WhenAll. Cap to the page size (<=50). Tolerate per-item failures
(render a minimal card if hydration of one item fails — never fail the whole page).

## Work items (delegate in this order)

### WI-1 — Shared sidebar shell
- `Areas/Accounts/Shared/AccountSidebarVm.cs` (namespace `YallaJo.Web.Areas.Accounts.Shared`):
  `{ string? AvatarUrl; string DisplayName = ""; string? Email; }`.
- `Areas/Accounts/Shared/_AccountSidebar.cshtml` (@model AccountSidebarVm): offcanvas-lg offcanvas-end
  card.bg-light w/ edit shortcut, mini-profile (avatar fallback `~/assets/images/avatar/01.jpg`, name,
  email), `ul.nav.nav-pills-primary-soft.flex-column`: My Profile (Profile/Index, bi-person),
  My Bookings (Bookings/Index, bi-ticket-perforated), Wishlist (Wishlist/Index, bi-heart),
  Settings (Settings/Index, bi-gear), Delete Profile (Delete/Index, bi-trash, text-danger), Sign Out =
  POST form asp-area=Auth asp-controller=Auth asp-action=Logout (.d-grid + @Html.AntiForgeryToken() +
  button.nav-link). Active item via `ViewData["AccountNav"]` (Profile/Bookings/Wishlist/Settings/Delete).
- Helper to build AccountSidebarVm from ProfileVm (each page loads minimal profile for the sidebar).

### WI-2 — Profile redesign (reuse existing slice + add email/password cards)
- Rewrite `Profile/Views/Index.cshtml` to Webestica two-column dashboard (set ViewData["AccountNav"]="Profile";
  Layout full-chrome; section.pt-3 > .container > .row > [<partial _AccountSidebar/>] +
  [main col-lg-8 col-xl-9 with mobile offcanvas toggle + vstack.gap-4]). Cards:
  - Complete Your Profile (static progress bar, no aos; derive % from ProfileVm field presence).
  - Personal Information: real form asp-action=Update (reuse _UpdateProfileForm) + avatar uploader form
    asp-action=UpdateAvatar (input name=File, enctype=multipart/form-data) + delete-avatar form.
  - Update email card (decide: new ProfileFacade.UpdateEmailAsync OR document that email change goes through
    a verification flow — if no endpoint, render the card but point to existing flow / flag).
  - Update Password card: real form asp-area=Accounts asp-controller=ChangePassword asp-action=Index
    (CurrentPassword/NewPassword/ConfirmNewPassword, fakepassword eye-toggle).
- Controller loads AccountSidebarVm too (or build in view from ProfileVm).

### WI-3 — Settings slice (new Features/Settings/)
- SettingsController/SettingsFacade/SettingsApiClient + ViewModels/Requests/Responses/Mappers.
- Notification preferences: GET/PUT /notifications/preferences rendered as a dynamic matrix
  (Type x Channel switches), NOT the template's fixed 7 toggles.
- Marketing consent: GET/PUT /accounts/me/marketing-consent (3 switches).
- Phone: reuse UpdatePhone (PUT /security/account/phone).
- Security: sessions list + revoke, trust device, logout-all (reuse Auth wiring or call directly via
  SettingsApiClient). OMIT 2FA phone Send-Code (no endpoint).
- ViewData["AccountNav"]="Settings".

### WI-4 — Bookings slice (new Features/Bookings/)
- BookingsController/BookingsFacade/BookingsApiClient + DTOs/VMs/Mapper.
- GET /booking/my-bookings with facade-side tab mapping (Upcoming/Canceled/Completed). Detail GET
  /booking/{id}. Cancel POST /booking/{id}/cancel (Reason?). Hydrate tour Name via /tours/{tourId} +
  image via attachments?entityType=Tour&entityId=tourId (parallel, tolerate failures). Render real tour
  bookings (NOT template flight/car cards). Tabs via nav-tabs; ViewData["AccountNav"]="Bookings".

### WI-5 — Wishlist slice (new Features/Wishlist/)
- WishlistController/WishlistFacade/WishlistApiClient + DTOs/VMs/Mapper.
- GET /social/favorites; per-favorite hydrate (switch EntityType -> detail endpoint) + attachments image.
- Remove-one DELETE /social/favorites/{entityType}/{entityId}. Remove-all = facade loops DELETEs.
- Sort client-side. ViewData["AccountNav"]="Wishlist".

### WI-6 — Delete slice (new Features/Delete/)
- DeleteController/DeleteFacade (reuse ProfileApiClient or new DeleteApiClient).
- Add `RestoreAsync` (POST /accounts/profile/restore) to ProfileApiClient (or DeleteApiClient).
- Soft-delete DELETE /accounts/profile. Webestica delete card: warning + confirm checkbox +
  "Keep my account" / "Delete my account". ViewData["AccountNav"]="Delete".

## Final
After all WIs: build clean (0 CS/RZ errors). Sidebar links require Bookings/Wishlist/Settings/Delete
controllers to exist (Index at minimum) so links resolve at runtime.
