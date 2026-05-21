# YallaJo Frontend — ASP.NET Core MVC Design + Endpoint Map

> **Source template:** `C:\Users\admin1\Desktop\Template\hotel-management-syste-main\booking.webestica.com`
> **Template:** Webestica "Booking" — 74 Bootstrap 5 HTML pages, RTL-ready
> **Pattern:** **ASP.NET Core MVC (Razor Views)** — server-rendered, no SPA framework
> **Existing project:** `YallaJo.Web` already in solution
> **Backend reference:** `Agents/agent-context.md` (PDF1 endpoints), `Agents/guide.md` (PDF2 rules)

---

## 1. Why MVC + This Template = Natural Fit

The Webestica template is **pure Bootstrap 5 + vanilla JS** (no React/Vue/Angular). Perfect for Razor:

1. **Direct HTML reuse** — Rename `.html` → `.cshtml`, add `@model`, replace dummy data with Razor expressions.
2. **Server-side SEO** — Razor renders meta tags, hreflang, OG tags, sitemap.xml natively. No hydration penalty.
3. **Single language** — C# end-to-end. Reuse `*.Contracts` DTOs directly from solution.
4. **Auth integration** — `Microsoft.AspNetCore.Authentication.Cookies` + antiforgery built-in.
5. **SignalR client** — Native JS client (`@microsoft/signalr` CDN) drops into `_Layout.cshtml`.
6. **i18n + RTL** — `IViewLocalizer` + `RequestLocalization` middleware + `<html dir="@dir">`.
7. **No build step** — Bootstrap referenced as static asset; optional bundling via WebOptimizer.
8. **Existing infrastructure** — `YallaJo.Web` project already exists; we extend it.

---

## 2. Template Inventory (74 pages)

### 2.1 Public / Marketing (15)
| Template file | YallaJo use |
|---|---|
| `index-tour.html` | **Primary homepage** |
| `index.html`, `index-hotel-chain.html`, `index-resort.html`, `index-cab.html`, `index-flight.html`, `index-directory.html` | Alt homepages (unused) |
| `about.html`, `contact.html`, `contact-2.html`, `faq.html`, `team.html` | Static info pages |
| `pricing.html` | Phase 3 — Subscription plans |
| `blog.html`, `blog-detail.html` | Blog list + detail |
| `privacy-policy.html`, `terms-of-service.html` | Legal |
| `help-center.html`, `help-detail.html` | Knowledge base |
| `coming-soon.html`, `error.html` | Status pages |
| `join-us.html` | Provider recruitment landing |

### 2.2 Auth (4)
`sign-in.html`, `sign-up.html`, `forgot-password.html`, `two-factor-auth.html`

### 2.3 Tour module (3 — CORE)
`tour-grid.html`, `tour-detail.html`, `tour-booking.html`

### 2.4 Repurposed for YallaJo
| Template file | Repurposed as |
|---|---|
| `hotel-grid.html`, `hotel-list.html` | Business listing |
| `hotel-detail.html`, `room-detail.html` | Business detail + ServiceItem detail |
| `hotel-booking.html` | Business reservation |
| `directory-detail.html` | Place detail |
| `add-listing.html`, `add-listing-minimal.html` | Provider tour creation (full + quick) |
| `compare-listing.html` | Compare tours |
| `offer-detail.html` | Discount/promotion landing |
| `listing-added.html` | Tour-creation success |
| `booking-confirm.html` | Post-payment confirmation |

### 2.5 User Account (7)
`account-bookings.html`, `account-wishlist.html`, `account-profile.html`, `account-settings.html`, `account-payment-details.html` (Phase 3), `account-travelers.html`, `account-delete.html`

### 2.6 Provider Dashboard (7 — template's "Agent")
`agent-dashboard.html`, `agent-listings.html`, `agent-activities.html`, `agent-bookings.html`, `agent-earnings.html`, `agent-reviews.html`, `agent-settings.html`

### 2.7 Admin Dashboard (9)
`admin-dashboard.html`, `admin-agent-list.html`, `admin-agent-detail.html`, `admin-booking-list.html`, `admin-booking-detail.html`, `admin-guest-list.html`, `admin-guest-detail.html`, `admin-earnings.html`, `admin-reviews.html`, `admin-settings.html`

### 2.8 Vendor assets to copy to `YallaJo.Web/wwwroot/lib/`
Bootstrap 5, Bootstrap Icons, Font Awesome, Tiny Slider, Splide, Choices.js, Flatpickr, noUiSlider, Dropzone, Stepper, Quill, ApexCharts, AOS, GLightbox, Jarallax, Sticky-js, Overlay Scrollbar, PureCounterJS

---

## 3. YallaJo.Web Project Structure

```
YallaJo.Web/
├── Areas/
│   ├── Account/        # logged-in user
│   │   ├── Controllers/
│   │   │   ├── BookingsController.cs
│   │   │   ├── WishlistController.cs
│   │   │   ├── ProfileController.cs
│   │   │   ├── SettingsController.cs
│   │   │   ├── PaymentMethodsController.cs (Phase 3)
│   │   │   ├── TravelersController.cs
│   │   │   ├── SupportController.cs
│   │   │   └── BookingConfirmController.cs
│   │   └── Views/...
│   │
│   ├── Provider/       # Approved providers
│   │   ├── Controllers/
│   │   │   ├── ApplyController.cs           # /provider/apply  (Backend Wave 2 needed)
│   │   │   ├── StatusController.cs          # /provider/status
│   │   │   ├── DocumentsController.cs
│   │   │   ├── DashboardController.cs
│   │   │   ├── ListingsController.cs        # CRUD + submit
│   │   │   ├── AvailabilityController.cs    # Wave 5
│   │   │   ├── BookingsController.cs        # pending/upcoming/history
│   │   │   ├── JoinRequestsController.cs    # Wave 5
│   │   │   ├── EarningsController.cs
│   │   │   ├── ReviewsController.cs
│   │   │   ├── DiscountsController.cs       # Phase 3
│   │   │   ├── RefundPoliciesController.cs  # Wave 5
│   │   │   ├── ActivitiesController.cs
│   │   │   └── SettingsController.cs
│   │   └── Views/...
│   │
│   └── Admin/
│       ├── Controllers/
│       │   ├── DashboardController.cs
│       │   ├── ProvidersController.cs       # Backend Wave 2
│       │   ├── BookingsController.cs
│       │   ├── UsersController.cs
│       │   ├── ToursController.cs           # approval queue
│       │   ├── PlacesController.cs          # admin-only Place CRUD
│       │   ├── CategoriesController.cs      # tree editor
│       │   ├── TagsController.cs
│       │   ├── SpecializationsController.cs
│       │   ├── LanguagesController.cs
│       │   ├── RolesController.cs           # + Claims
│       │   ├── EarningsController.cs
│       │   ├── PayoutsController.cs
│       │   ├── CommissionsController.cs
│       │   ├── ReviewsController.cs
│       │   ├── ReportsController.cs
│       │   ├── AuditLogsController.cs
│       │   ├── NotificationTemplatesController.cs
│       │   ├── SupportController.cs
│       │   ├── SeoController.cs             # metadata + redirects
│       │   └── SettingsController.cs
│       └── Views/...
│
├── Controllers/        # public (anonymous + authenticated browse)
│   ├── HomeController.cs        # Index, About, Contact, Faq, Pricing, Team, Privacy, Terms, Error
│   ├── ToursController.cs       # Index, Detail, Compare, Book
│   ├── PlacesController.cs      # Index, Detail
│   ├── BusinessesController.cs  # Index, Detail, Book
│   ├── BlogController.cs        # Index, Detail
│   ├── HelpController.cs        # Index, Detail
│   ├── SearchController.cs      # Index
│   ├── OffersController.cs      # Detail
│   ├── JoinUsController.cs
│   └── Auth/AuthController.cs   # SignIn, SignUp, VerifyEmail, ForgotPassword, ResetPassword, Logout, External
│
├── Models/ViewModels/
│   ├── Public/  (HomeViewModel, TourListViewModel, TourDetailViewModel, TourBookingViewModel, PlaceDetailViewModel, BusinessDetailViewModel, BlogIndexViewModel, BlogDetailViewModel, SearchViewModel, CompareViewModel)
│   ├── Account/ (BookingsViewModel, WishlistViewModel, ProfileViewModel, SettingsViewModel, SupportViewModel)
│   ├── Provider/...
│   ├── Admin/...
│   └── Auth/    (SignInViewModel, SignUpViewModel, VerifyOtpViewModel, ForgotPasswordViewModel)
│
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml             # Public (header+footer)
│   │   ├── _LayoutAuth.cshtml         # Auth (centered card)
│   │   ├── _LayoutAccount.cshtml      # User with sidebar
│   │   ├── _LayoutProvider.cshtml     # Provider dashboard
│   │   ├── _LayoutAdmin.cshtml        # Admin dashboard
│   │   ├── _Header.cshtml             # Header partial
│   │   ├── _Footer.cshtml             # Footer partial
│   │   ├── _NavSidebar.cshtml         # Sidebar for Account/Provider/Admin
│   │   ├── _NotificationBell.cshtml   # SignalR bell
│   │   ├── _SearchBar.cshtml          # Reusable search
│   │   ├── _TourCard.cshtml           # Grid item
│   │   ├── _PlaceCard.cshtml
│   │   ├── _BusinessCard.cshtml
│   │   ├── _BlogCard.cshtml
│   │   ├── _ReviewItem.cshtml         # Review + reply thread
│   │   ├── _PriceDisplay.cshtml       # Price + discount + loyalty
│   │   ├── _StatusBadge.cshtml
│   │   ├── _Stepper.cshtml
│   │   ├── _RatingStars.cshtml
│   │   ├── _Pager.cshtml
│   │   ├── _FilterSidebar.cshtml
│   │   ├── _MapView.cshtml            # Mapbox embed
│   │   ├── Error.cshtml
│   │   └── _ValidationScriptsPartial.cshtml
│   ├── Home/                # Index, About, Contact, Faq, Pricing, ...
│   ├── Tours/               # Index, Detail, Compare, Book
│   ├── Places/{Index,Detail}.cshtml
│   ├── Businesses/{Index,Detail,Book}.cshtml
│   ├── Blog/{Index,Detail}.cshtml
│   ├── Help/{Index,Detail}.cshtml
│   ├── Search/Index.cshtml
│   ├── Offers/Detail.cshtml
│   ├── JoinUs/Index.cshtml
│   ├── Auth/                # SignIn, SignUp, VerifyOtp, ForgotPassword, ResetPassword
│   ├── Booking/Confirm.cshtml
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
│
├── Services/
│   ├── ApiClients/                    # Typed HttpClient services (one per backend module)
│   │   ├── IToursApiClient.cs / ToursApiClient.cs
│   │   ├── IBookingsApiClient.cs / BookingsApiClient.cs
│   │   ├── IPaymentsApiClient.cs / PaymentsApiClient.cs
│   │   ├── IProviderApiClient.cs / ProviderApiClient.cs
│   │   ├── IAdminApiClient.cs / AdminApiClient.cs
│   │   ├── IFavoritesApiClient.cs / FavoritesApiClient.cs
│   │   ├── ISearchApiClient.cs / SearchApiClient.cs
│   │   ├── IReviewsApiClient.cs / ReviewsApiClient.cs
│   │   ├── INotificationsApiClient.cs / NotificationsApiClient.cs
│   │   ├── IAnalyticsApiClient.cs / AnalyticsApiClient.cs
│   │   ├── ISupportTicketsApiClient.cs / SupportTicketsApiClient.cs
│   │   ├── IWeatherApiClient.cs / WeatherApiClient.cs
│   │   ├── IInvoicesApiClient.cs / InvoicesApiClient.cs
│   │   ├── IPayoutsApiClient.cs / PayoutsApiClient.cs
│   │   ├── ICategoriesApiClient.cs / CategoriesApiClient.cs
│   │   ├── ILanguagesApiClient.cs / LanguagesApiClient.cs
│   │   ├── IRolesApiClient.cs / RolesApiClient.cs
│   │   ├── IPlacesApiClient.cs / PlacesApiClient.cs
│   │   ├── IBusinessesApiClient.cs / BusinessesApiClient.cs
│   │   ├── IBlogsApiClient.cs / BlogsApiClient.cs
│   │   └── ... (~30 clients total)
│   ├── Auth/
│   │   ├── ITokenStore.cs
│   │   ├── TokenStore.cs
│   │   ├── AuthTokenHandler.cs        # DelegatingHandler attaches Bearer
│   │   └── PermissionAuthorizationHandler.cs  # custom IAuthorizationHandler
│   ├── Localization/CultureProviders.cs
│   ├── Caching/IResponseCacheService.cs + ResponseCacheService.cs
│   └── ViewHelpers/TagHelpers/
│       ├── RatingStarsTagHelper.cs
│       ├── StatusBadgeTagHelper.cs
│       ├── PriceDisplayTagHelper.cs
│       └── PagerTagHelper.cs
│
├── wwwroot/
│   ├── css/style.css                  # from template
│   ├── css/style.rtl.css              # from rtl/ folder
│   ├── js/functions.js                # from template
│   ├── js/signalr-client.js           # custom SignalR connection
│   ├── js/api-client.js               # AJAX helper
│   ├── lib/                           # vendor libs
│   ├── images/                        # static + placeholders
│   └── favicon.ico
│
├── Resources/                         # i18n .resx files
│   ├── Views/Home/Index.en.resx + Index.ar.resx
│   ├── Views/Tours/...
│   └── SharedResource.en.resx + SharedResource.ar.resx
│
├── Program.cs
├── appsettings.json                   # Api:BaseUrl, Auth:CookieDomain, etc.
└── YallaJo.Web.csproj
```

---

## 4. Routing — Full Map

### 4.1 Program.cs

```csharp
app.MapControllerRoute("areas", "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

// SEO slug routes
app.MapControllerRoute("tour-detail",     "tours/{slug}",      defaults: new { controller = "Tours", action = "Detail" });
app.MapControllerRoute("place-detail",    "places/{slug}",     defaults: new { controller = "Places", action = "Detail" });
app.MapControllerRoute("business-detail", "businesses/{slug}", defaults: new { controller = "Businesses", action = "Detail" });
app.MapControllerRoute("blog-detail",     "blog/{slug}",       defaults: new { controller = "Blog", action = "Detail" });
app.MapControllerRoute("help-detail",     "help/{slug}",       defaults: new { controller = "Help", action = "Detail" });
app.MapControllerRoute("offer-detail",    "offers/{slug}",     defaults: new { controller = "Offers", action = "Detail" });
```

### 4.2 Public (anonymous + auth)

| URL | Controller.Action | Template | Backend endpoint |
|---|---|---|---|
| `/` | Home.Index | index-tour.html | `GET /api/v1/tours/featured` + `/popular/places` + `/trending` + `/categories?level=1` |
| `/about` | Home.About | about.html | static |
| `/contact` (GET/POST) | Home.Contact | contact.html | TODO contact endpoint |
| `/faq` | Home.Faq | faq.html | `GET /api/v1/faq/global` (TODO) |
| `/pricing` | Home.Pricing | pricing.html | Phase 3 `GET /api/v1/subscription-plans` |
| `/team` | Home.Team | team.html | static |
| `/privacy` | Home.Privacy | privacy-policy.html | static |
| `/terms` | Home.Terms | terms-of-service.html | static |
| `/error` | Home.Error | error.html | — |
| `/coming-soon` | Home.ComingSoon | coming-soon.html | — |
| `/help` | Help.Index | help-center.html | `GET /api/v1/help-articles` (verify) |
| `/help/{slug}` | Help.Detail | help-detail.html | `GET /api/v1/help-articles/{slug}` |
| `/blog` | Blog.Index | blog.html | `GET /api/v1/blogs?status=Published&page=1` |
| `/blog/{slug}` | Blog.Detail | blog-detail.html | `GET /api/v1/blogs/{slug}` + `/blogs/{id}/comments` |
| `/tours` | Tours.Index | tour-grid.html | `GET /api/v1/tours?page=1&filters=...` |
| `/tours/{slug}` | Tours.Detail | tour-detail.html | `GET /api/v1/tours/by-slug/{slug}` + waypoints + reviews + weather + availability |
| `/tours/{slug}/book` (GET/POST) | Tours.Book | tour-booking.html | `POST /api/v1/bookings/tour` + `POST /api/v1/payments/initiate` |
| `/tours/compare` | Tours.Compare | compare-listing.html | `GET /api/v1/tours?ids=A,B,C` |
| `/places` | Places.Index | (new — adapt directory) | `GET /api/v1/places?page=1` |
| `/places/{slug}` | Places.Detail | directory-detail.html | `GET /api/v1/places/by-slug/{slug}` + businesses |
| `/businesses` | Businesses.Index | hotel-grid.html | `GET /api/v1/businesses?page=1` |
| `/businesses/{slug}` | Businesses.Detail | hotel-detail.html | `GET /api/v1/businesses/by-slug/{slug}` + hours + amenities |
| `/businesses/{slug}/book` | Businesses.Book | hotel-booking.html | `POST /api/v1/reservations` |
| `/search` | Search.Index | (new) | `GET /api/v1/search?q=X&filters=...` (Wave 6 ✅) |
| `/offers/{slug}` | Offers.Detail | offer-detail.html | `GET /api/v1/discounts/{slug}` (Phase 3) |
| `/join-us` | JoinUs.Index | join-us.html | static |
| `/sign-in` (GET/POST) | Auth.SignIn | sign-in.html | `POST /api/v1/auth/login` → set cookie |
| `/sign-up` (GET/POST) | Auth.SignUp | sign-up.html | `POST /api/v1/auth/register` |
| `/verify-email` | Auth.VerifyOtp | two-factor-auth.html | `POST /api/v1/auth/verify-email` |
| `/forgot-password` (GET/POST) | Auth.ForgotPassword | forgot-password.html | `POST /api/v1/auth/forgot-password` |
| `/reset-password` (GET/POST) | Auth.ResetPassword | (combine with verify) | `POST /api/v1/auth/reset-password` |
| `/sign-in/apple` | Auth.SignInApple | redirect | `POST /api/v1/auth/external/apple` (**Wave 1** — needs explicit endpoints) |
| `/sign-in/facebook` | Auth.SignInFacebook | redirect | `POST /api/v1/auth/external/facebook` |
| `/sign-in/google` | Auth.SignInGoogle | redirect | `POST /api/v1/auth/external/google` |
| `/logout` (POST) | Auth.Logout | — | `POST /api/v1/auth/logout` + cookie clear |

### 4.3 Account area `/Account/...`

| URL | Controller.Action | Template | Backend |
|---|---|---|---|
| `/Account/Bookings` | Account.Bookings.Index | account-bookings.html | `GET /api/v1/bookings/my-bookings` |
| `/Account/Bookings/{id}` | Account.Bookings.Detail | (detail page) | `GET /api/v1/bookings/{id}` |
| `/Account/Bookings/{id}/Cancel` (POST) | Account.Bookings.Cancel | confirm modal | `POST /api/v1/bookings/{id}/cancel` |
| `/Account/Wishlist` | Account.Wishlist.Index | account-wishlist.html | `GET /api/v1/favorites` |
| `/Account/Wishlist/Remove` (POST) | Account.Wishlist.Remove | — | `DELETE /api/v1/favorites/{entityType}/{entityId}` |
| `/Account/Profile` | Account.Profile.Index | account-profile.html | `GET /api/v1/profile` |
| `/Account/Profile/Save` (POST) | Account.Profile.Save | — | `PUT /api/v1/profile` |
| `/Account/Profile/Avatar` (POST) | Account.Profile.Avatar | — | `POST /api/v1/profile/avatar` (Wave 1) |
| `/Account/Profile/ChangePassword` (POST) | Account.Profile.ChangePassword | — | `POST /api/v1/auth/change-password` |
| `/Account/Settings` | Account.Settings.Index | account-settings.html | `GET /api/v1/notifications/preferences` |
| `/Account/Settings/Save` (POST) | Account.Settings.Save | — | `PUT /api/v1/notifications/preferences` |
| `/Account/Settings/Devices` | Account.Settings.Devices | (sub-tab) | `GET /api/v1/auth/sessions` |
| `/Account/Settings/Devices/Revoke` (POST) | Account.Settings.RevokeDevice | — | `DELETE /api/v1/auth/sessions/{id}` |
| `/Account/PaymentMethods` | Account.PaymentMethods.Index | account-payment-details.html | Phase 3 |
| `/Account/Travelers` | Account.Travelers.Index | account-travelers.html | TODO endpoint |
| `/Account/Delete` | Account.Delete.Index | account-delete.html | TODO endpoint |
| `/Account/Support` | Account.Support.Index | (sidebar entry) | `GET /api/v1/support/tickets` |
| `/Account/Support/Create` | Account.Support.Create | — | `POST /api/v1/support/tickets` |
| `/Account/Support/{id}` | Account.Support.Detail | — | `GET /api/v1/support/tickets/{id}` |
| `/Account/Support/{id}/Reply` (POST) | Account.Support.Reply | — | `POST /api/v1/support/tickets/{id}/messages` |
| `/BookingConfirm/{id}` | Account.BookingConfirm.Detail | booking-confirm.html | `GET /api/v1/bookings/{id}` + `/invoices/by-booking/{id}` |

### 4.4 Provider area `/Provider/...`

| URL | Controller.Action | Template | Backend |
|---|---|---|---|
| `/Provider/Apply` | Provider.Apply.Index | add-listing-minimal.html | `POST /api/v1/provider/register` + `/apply` (**Wave 2** — backend not built) |
| `/Provider/Status` | Provider.Status.Index | (state-machine page) | `GET /api/v1/provider/status` |
| `/Provider/Documents` | Provider.Documents.Index | (multi-upload) | `POST /api/v1/provider/documents` + `PUT .../{id}` |
| `/Provider/Dashboard` | Provider.Dashboard.Index | agent-dashboard.html | `GET /api/v1/provider/dashboard` |
| `/Provider/Listings` | Provider.Listings.Index | agent-listings.html | `GET /api/v1/tours/provider/my-tours` |
| `/Provider/Listings/Create` | Provider.Listings.Create | add-listing.html | `POST /api/v1/tours` |
| `/Provider/Listings/{id}/Edit` | Provider.Listings.Edit | add-listing.html | `PUT /api/v1/tours/{id}` |
| `/Provider/Listings/{id}/Submit` (POST) | Provider.Listings.Submit | — | `POST /api/v1/tours/{id}/submit` |
| `/Provider/Listings/{id}/Availability` | Provider.Availability.Index | (calendar) | `GET /api/v1/availability/{tourId}` + slot CRUD (**Wave 5**) |
| `/Provider/Listings/{id}/Pricing` | Provider.Listings.Pricing | (sub-page) | tour pricing CRUD |
| `/Provider/Listings/{id}/Schedules` | Provider.Listings.Schedules | (sub-page) | tour schedules CRUD |
| `/Provider/Listings/{id}/Waypoints` | Provider.Listings.Waypoints | (Mapbox + reorder) | waypoint CRUD |
| `/Provider/Listings/{id}/Images` | Provider.Listings.Images | (Dropzone) | `POST /api/v1/attachments/Tour/{id}/images` |
| `/Provider/Listings/{id}/RefundPolicy` | Provider.RefundPolicies.Detail | — | refund policy CRUD (**Wave 5**) |
| `/Provider/Bookings` (Pending/Upcoming/History tabs) | Provider.Bookings.Pending/Upcoming/History | agent-bookings.html | `GET /api/v1/bookings/provider/{pending\|upcoming\|history}` (**Wave 5**) |
| `/Provider/Bookings/{id}/Confirm` (POST) | Provider.Bookings.Confirm | — | `POST /api/v1/bookings/{id}/confirm` |
| `/Provider/Bookings/{id}/Reject` (POST) | Provider.Bookings.Reject | — | `POST /api/v1/bookings/{id}/reject` |
| `/Provider/JoinRequests` | Provider.JoinRequests.Index | — | `GET /api/v1/bookings/join-requests/provider` (**Wave 5**) |
| `/Provider/JoinRequests/{id}/Approve` (POST) | Provider.JoinRequests.Approve | — | `POST /api/v1/bookings/join-request/{id}/approve` |
| `/Provider/JoinRequests/{id}/Reject` (POST) | Provider.JoinRequests.Reject | — | `POST /api/v1/bookings/join-request/{id}/reject` |
| `/Provider/Earnings` | Provider.Earnings.Index | agent-earnings.html | `GET /api/v1/payouts/provider` + `/invoices/provider/my-invoices` |
| `/Provider/Reviews` | Provider.Reviews.Index | agent-reviews.html | `GET /api/v1/reviews?providerId=self` |
| `/Provider/Reviews/{id}/Reply` (POST) | Provider.Reviews.Reply | — | `POST /api/v1/reviews/{id}/reply` |
| `/Provider/Discounts` | Provider.Discounts.Index | — | Phase 3 |
| `/Provider/Activities` | Provider.Activities.Index | agent-activities.html | derived from notifications + audit log |
| `/Provider/Settings` | Provider.Settings.Index | agent-settings.html | profile + provider fields |

### 4.5 Admin area `/Admin/...`

| URL | Controller.Action | Template | Backend |
|---|---|---|---|
| `/Admin/Dashboard` | Admin.Dashboard.Index | admin-dashboard.html | `GET /api/v1/admin/dashboard` + revenue/bookings/users sub-charts |
| `/Admin/Providers` | Admin.Providers.Index | admin-agent-list.html | `GET /api/v1/admin/providers` (**Wave 2**) |
| `/Admin/Providers/Pending` | Admin.Providers.Pending | (filter view) | `GET /api/v1/admin/providers?status=Pending` |
| `/Admin/Providers/{id}` | Admin.Providers.Detail | admin-agent-detail.html | `GET /api/v1/admin/providers/{id}` |
| `/Admin/Providers/{id}/Approve` (POST) | Admin.Providers.Approve | — | `POST /api/v1/admin/providers/{id}/approve` |
| `/Admin/Providers/{id}/Reject` (POST) | Admin.Providers.Reject | — | `POST /api/v1/admin/providers/{id}/reject` |
| `/Admin/Providers/{id}/RequestDocs` (POST) | Admin.Providers.RequestDocs | — | `POST /api/v1/admin/providers/{id}/request-docs` |
| `/Admin/Providers/{id}/Suspend` (POST) | Admin.Providers.Suspend | — | `POST /api/v1/admin/providers/{id}/suspend` |
| `/Admin/Bookings` | Admin.Bookings.Index | admin-booking-list.html | `GET /api/v1/bookings/admin/all` |
| `/Admin/Bookings/{id}` | Admin.Bookings.Detail | admin-booking-detail.html | `GET /api/v1/bookings/{id}` |
| `/Admin/Bookings/{id}/ForceRefund` (POST) | Admin.Bookings.ForceRefund | — | `POST /api/v1/admin/bookings/{id}/force-refund` |
| `/Admin/Users` | Admin.Users.Index | admin-guest-list.html | `GET /api/v1/admin/users` |
| `/Admin/Users/{id}` | Admin.Users.Detail | admin-guest-detail.html | `GET /api/v1/admin/users/{id}` |
| `/Admin/Users/{id}/Status` (POST) | Admin.Users.Status | — | `PUT /api/v1/admin/users/{id}/status` |
| `/Admin/Users/{id}/Roles/Add` (POST) | Admin.Users.AddRole | — | `POST /api/v1/admin/users/{id}/roles` |
| `/Admin/Users/{id}/Roles/{roleName}` (POST DELETE) | Admin.Users.RemoveRole | — | `DELETE /api/v1/admin/users/{id}/roles/{roleName}` |
| `/Admin/Tours` | Admin.Tours.Index | — | `GET /api/v1/tours/admin/all` |
| `/Admin/Tours/Pending` | Admin.Tours.Pending | approval queue | `GET /api/v1/tours/admin?status=Pending` |
| `/Admin/Tours/{id}/Approve` (POST) | Admin.Tours.Approve | — | `POST /api/v1/tours/admin/{id}/approve` |
| `/Admin/Tours/{id}/Reject` (POST) | Admin.Tours.Reject | — | `POST /api/v1/tours/admin/{id}/reject` |
| `/Admin/Places` | Admin.Places.Index | — | `GET /api/v1/places` |
| `/Admin/Places/Create` (POST) | Admin.Places.Create | — | `POST /api/v1/places` |
| `/Admin/Places/{id}/Edit` | Admin.Places.Edit | — | `PUT /api/v1/places/{id}` |
| `/Admin/Categories` | Admin.Categories.Index | tree editor | `GET /api/v1/categories` |
| `/Admin/Categories/Create` (POST) | Admin.Categories.Create | — | `POST /api/v1/categories` |
| `/Admin/Categories/Reorder` (PUT) | Admin.Categories.Reorder | — | `PUT /api/v1/categories/reorder` |
| `/Admin/Tags` | Admin.Tags.Index | — | `GET/POST/PUT /api/v1/tags` |
| `/Admin/Specializations` | Admin.Specializations.Index | — | `GET/POST/PUT /api/v1/specializations` |
| `/Admin/Languages` | Admin.Languages.Index | — | `GET/POST/PUT /api/v1/languages` |
| `/Admin/Roles` | Admin.Roles.Index | — | `GET /api/v1/admin/roles` |
| `/Admin/Roles/{id}/Claims` | Admin.Roles.Claims | — | `GET /api/v1/admin/roles/{id}/claims` (**Wave 1 — missing**) |
| `/Admin/Earnings` | Admin.Earnings.Index | admin-earnings.html | `GET /api/v1/admin/dashboard/revenue` |
| `/Admin/Payouts/Pending` | Admin.Payouts.Pending | — | `GET /api/v1/payouts/admin/pending` |
| `/Admin/Payouts/{id}/Approve` (POST) | Admin.Payouts.Approve | — | `POST /api/v1/payouts/{id}/approve` |
| `/Admin/Payouts/Trigger` (POST) | Admin.Payouts.Trigger | — | `POST /api/v1/payouts/admin/trigger` |
| `/Admin/Commissions` | Admin.Commissions.Index | — | full CRUD `/api/v1/commissions` |
| `/Admin/Reviews/Flagged` | Admin.Reviews.Flagged | admin-reviews.html | `GET /api/v1/reviews/admin/flagged` |
| `/Admin/Reviews/{id}/Approve` (POST) | Admin.Reviews.Approve | — | `POST /api/v1/reviews/admin/{id}/approve` |
| `/Admin/Reviews/{id}/Remove` (POST) | Admin.Reviews.Remove | — | `POST /api/v1/reviews/admin/{id}/remove` |
| `/Admin/Reports` | Admin.Reports.Index | — | `GET /api/v1/reports/admin` |
| `/Admin/Reports/{id}/Resolve` (POST) | Admin.Reports.Resolve | — | `POST /api/v1/reports/admin/{id}/resolve` |
| `/Admin/AuditLogs` | Admin.AuditLogs.Index | — | `GET /api/v1/admin/audit-logs` |
| `/Admin/NotificationTemplates` | Admin.NotificationTemplates.Index | — | full CRUD `/api/v1/admin/notification-templates` |
| `/Admin/Support` | Admin.Support.Index | — | `GET /api/v1/support/admin/tickets` |
| `/Admin/Support/{id}/Assign` (POST) | Admin.Support.Assign | — | `POST /api/v1/support/admin/tickets/{id}/assign` |
| `/Admin/Support/{id}/Resolve` (POST) | Admin.Support.Resolve | — | `POST /api/v1/support/admin/tickets/{id}/resolve` |
| `/Admin/Seo/Metadata` | Admin.Seo.Metadata | — | `GET/POST/PUT /api/v1/metadata` |
| `/Admin/Seo/Redirects` | Admin.Seo.Redirects | — | `GET/POST/DELETE /api/v1/redirects` |
| `/Admin/Settings` | Admin.Settings.Index | admin-settings.html | platform config |

### 4.6 Utility routes
- `/sitemap.xml` → reverse-proxy `GET /api/v1/sitemap.xml`
- `/robots.txt` → static file in wwwroot

**Total MVC routes: ~120**

---

## 5. API Consumption Strategy

### 5.1 HttpClient (BFF pattern)

Keep `YallaJo.Web` and `YallaJo.Api` as **separate processes**:
- ✅ Independent deployment + scaling
- ✅ Caching layer on Web tier
- ✅ Future mobile app + 3rd-party API consumers share single API surface
- ✅ Web tier acts as Backend-For-Frontend, can transform/sanitize responses
- ✅ Forms POST to MVC (with antiforgery), MVC calls API server-to-server

### 5.2 Typed HttpClient registration

```csharp
// Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthTokenHandler>();

void RegisterClient<TInterface, TImpl>() where TInterface : class where TImpl : class, TInterface
{
    builder.Services.AddHttpClient<TInterface, TImpl>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    })
    .AddHttpMessageHandler<AuthTokenHandler>()
    .AddPolicyHandler(GetRetryPolicy())           // Polly 3x exponential backoff on 5xx
    .AddPolicyHandler(GetCircuitBreakerPolicy());  // open after 5 failures in 30s
}

RegisterClient<IToursApiClient, ToursApiClient>();
RegisterClient<IBookingsApiClient, BookingsApiClient>();
RegisterClient<IPaymentsApiClient, PaymentsApiClient>();
// ... ~30 clients
```

### 5.3 Client interface example

```csharp
public interface IToursApiClient
{
    Task<TourListResponse> ListAsync(TourFilters filters, CancellationToken ct);
    Task<TourDetailDto?> GetBySlugAsync(string slug, CancellationToken ct);
    Task<TourDetailDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Guid> CreateAsync(CreateTourRequest req, CancellationToken ct);
    Task UpdateAsync(Guid id, UpdateTourRequest req, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task SubmitForApprovalAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<TourCardDto>> ListMyToursAsync(CancellationToken ct);
}

internal sealed class ToursApiClient(HttpClient http) : IToursApiClient
{
    public async Task<TourListResponse> ListAsync(TourFilters filters, CancellationToken ct)
    {
        var query = QueryString.Create(filters.ToDictionary());
        var resp = await http.GetAsync($"/api/v1/tours{query}", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<TourListResponse>(cancellationToken: ct) ?? new();
    }

    public async Task<TourDetailDto?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        var resp = await http.GetAsync($"/api/v1/tours/by-slug/{Uri.EscapeDataString(slug)}", ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<TourDetailDto>(cancellationToken: ct);
    }
    // ... etc.
}
```

### 5.4 DTO reuse via `*.Contracts` project references

```xml
<!-- YallaJo.Web.csproj -->
<ProjectReference Include="..\ContentTours.Contracts\ContentTours.Contracts.csproj" />
<ProjectReference Include="..\Booking.Contracts\Booking.Contracts.csproj" />
<!-- ... all 14 *.Contracts projects -->
```

Use Contracts DTOs as response types directly. Wrap in ViewModels for UI-specific fields:

```csharp
public sealed class TourDetailViewModel
{
    public required TourDetailDto Tour { get; init; }
    public required IReadOnlyList<ReviewDto> Reviews { get; init; }
    public required WeatherForecastDto? Weather { get; init; }
    public required IReadOnlyList<AvailabilitySlotDto> UpcomingSlots { get; init; }
    public bool IsInWishlist { get; init; }
    public string FormattedPrice => Tour.BasePrice.ToString("C", CultureInfo.GetCultureInfo(Tour.Currency));
    public string DurationLabel => $"{Tour.DurationMinutes / 60}h {Tour.DurationMinutes % 60}m";
}
```

### 5.5 Controller pattern (example)

```csharp
public sealed class ToursController(
    IToursApiClient toursApi,
    IReviewsApiClient reviewsApi,
    IWeatherApiClient weatherApi,
    IFavoritesApiClient favoritesApi,
    IAnalyticsApiClient analyticsApi,
    ICurrentUser currentUser) : Controller
{
    [HttpGet("tours/{slug}")]
    [OutputCache(Duration = 300, VaryByQueryKeys = new[] { "slug" })]  // 5-min for anonymous
    public async Task<IActionResult> Detail(string slug, CancellationToken ct)
    {
        var tour = await toursApi.GetBySlugAsync(slug, ct);
        if (tour is null) return NotFound();

        // Parallel fetch
        var reviewsTask = reviewsApi.ListAsync(new() { TargetType = "Tour", TargetId = tour.Id, PageSize = 10 }, ct);
        var weatherTask = weatherApi.GetForecastAsync(tour.PlaceId, ct);
        var wishlistTask = currentUser.IsAuthenticated
            ? favoritesApi.IsInWishlistAsync("Tour", tour.Id, ct)
            : Task.FromResult(false);
        await Task.WhenAll(reviewsTask, weatherTask, wishlistTask);

        var vm = new TourDetailViewModel
        {
            Tour = tour,
            Reviews = reviewsTask.Result.Items,
            Weather = weatherTask.Result,
            UpcomingSlots = tour.UpcomingSlots,
            IsInWishlist = wishlistTask.Result
        };

        // Fire-and-forget analytics (A-R1)
        _ = analyticsApi.RecordInteractionAsync(new()
        {
            EntityType = "Tour",
            EntityId = tour.Id,
            InteractionType = "View",
            UserId = currentUser.UserId
        }, ct);

        // SEO
        ViewData["Title"] = $"{tour.Title} | YallaJo";
        ViewData["MetaDescription"] = tour.Description?.Truncate(160);
        ViewData["OgImage"] = tour.PrimaryImageUrl;
        ViewData["Canonical"] = $"https://yallajo.com/tours/{tour.Slug}";

        return View(vm);
    }
}
```

---

## 6. Authentication (Cookie + JWT)

### 6.1 Architecture
- **Browser ↔ Web:** Cookie auth (HttpOnly, Secure, SameSite=Strict)
- **Web ↔ Api:** Bearer JWT in `Authorization` header (server-to-server)
- **Token storage:** JWT inside encrypted cookie claims

### 6.2 Program.cs auth setup

```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opts =>
    {
        opts.LoginPath = "/sign-in";
        opts.LogoutPath = "/logout";
        opts.AccessDeniedPath = "/access-denied";
        opts.ExpireTimeSpan = TimeSpan.FromDays(30);
        opts.SlidingExpiration = true;
        opts.Cookie.HttpOnly = true;
        opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        opts.Cookie.SameSite = SameSiteMode.Strict;
        opts.Cookie.Name = "YallaJo.Auth";
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("Provider", p => p.RequireRole("Provider"));
    opts.AddPolicy("Admin", p => p.RequireRole("Admin", "SuperAdmin"));
});
```

### 6.3 Sign-in flow

```csharp
[HttpPost("/sign-in")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SignIn(SignInViewModel vm, CancellationToken ct)
{
    if (!ModelState.IsValid) return View(vm);

    var result = await authApi.SignInAsync(new()
    {
        Email = vm.Email,
        Password = vm.Password
    }, ct);

    if (!result.Success)
    {
        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Sign-in failed");
        return View(vm);
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, result.UserId.ToString()),
        new(ClaimTypes.Email, vm.Email),
        new("access_token", result.AccessToken),
        new("refresh_token", result.RefreshToken),
        new("token_expires_at", result.ExpiresAt.ToString("O"))
    };
    foreach (var role in result.Roles)
        claims.Add(new(ClaimTypes.Role, role));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity),
        new AuthenticationProperties
        {
            IsPersistent = vm.RememberMe,
            ExpiresUtc = DateTime.UtcNow.AddDays(30)
        });

    return LocalRedirect(vm.ReturnUrl ?? "/");
}
```

### 6.4 AuthTokenHandler — outbound Bearer + silent refresh

```csharp
public sealed class AuthTokenHandler(IHttpContextAccessor accessor, IAuthApiClient authApi)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var ctx = accessor.HttpContext;
        var token = ctx?.User?.FindFirst("access_token")?.Value;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized && ctx is not null)
        {
            var refreshToken = ctx.User?.FindFirst("refresh_token")?.Value;
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var refreshed = await authApi.RefreshAsync(new() { RefreshToken = refreshToken }, ct);
                if (refreshed.Success)
                {
                    // Update cookie claims (re-sign-in)
                    // ... rebuild ClaimsPrincipal with new access_token, re-issue cookie ...
                    // Retry original request once:
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
                    response = await base.SendAsync(request, ct);
                }
            }
        }
        return response;
    }
}
```

### 6.5 Authorization

Role-based (simple):
```csharp
[Area("Admin")]
[Authorize(Policy = "Admin")]
public sealed class DashboardController : Controller { ... }

[Area("Provider")]
[Authorize(Policy = "Provider")]
public sealed class ListingsController : Controller { ... }
```

Permission-based (mirror backend's `MustHavePermissionAttribute`):
```csharp
[YallaJoRequirePermission(BookingFeatures.Booking, AppAction.Cancel)]
public IActionResult Cancel(Guid id) { ... }
```

Custom `IAuthorizationHandler` checks JWT claims against permission catalog.

---

## 7. SignalR Notifications

### 7.1 Client setup in `_LayoutAccount.cshtml` / `_LayoutProvider.cshtml` / `_LayoutAdmin.cshtml`

```html
@if (User.Identity?.IsAuthenticated == true)
{
    <script src="~/lib/signalr/dist/browser/signalr.min.js"></script>
    <script>
    (function () {
        const accessToken = '@(User.FindFirst("access_token")?.Value)';
        const apiBaseUrl = '@Configuration["Api:BaseUrl"]';
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(apiBaseUrl + '/hubs/notifications', {
                accessTokenFactory: () => accessToken,
                transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
            })
            .withAutomaticReconnect([0, 2000, 10000, 30000])
            .build();

        connection.on('NotificationReceived', (notif) => {
            // Update bell badge
            const badge = document.querySelector('[data-notification-badge]');
            if (badge) {
                const current = parseInt(badge.textContent || '0', 10);
                badge.textContent = (current + 1).toString();
                badge.classList.remove('d-none');
            }
            // Show Bootstrap toast
            showToast(notif.title, notif.body, notif.type);
        });

        connection.on('UnreadCountUpdated', (count) => {
            const badge = document.querySelector('[data-notification-badge]');
            if (badge) {
                badge.textContent = count > 0 ? count.toString() : '';
                badge.classList.toggle('d-none', count === 0);
            }
        });

        connection.start().catch(err => console.error('SignalR connection failed:', err));
    })();
    </script>
}
```

### 7.2 Bell partial `_NotificationBell.cshtml`

```html
@model NotificationBellViewModel
<div class="dropdown ms-2">
    <button class="btn btn-link nav-link p-0 position-relative" data-bs-toggle="dropdown">
        <i class="bi bi-bell fs-5"></i>
        <span data-notification-badge
              class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger @(Model.UnreadCount == 0 ? "d-none" : "")">
            @(Model.UnreadCount > 0 ? Model.UnreadCount.ToString() : "")
        </span>
    </button>
    <ul class="dropdown-menu dropdown-menu-end" style="min-width: 320px;">
        <li><h6 class="dropdown-header">Notifications</h6></li>
        @foreach (var n in Model.RecentNotifications)
        {
            <li>
                <a class="dropdown-item @(n.IsRead ? "" : "fw-bold")" href="@n.ActionUrl">
                    <small>@n.Title</small><br/>
                    <small class="text-muted">@n.CreatedAt.Humanize()</small>
                </a>
            </li>
        }
        <li><hr class="dropdown-divider" /></li>
        <li>
            <a class="dropdown-item text-center" asp-area="Account" asp-controller="Settings" asp-action="Notifications">
                View all
            </a>
        </li>
    </ul>
</div>
```

---

## 8. i18n + RTL

### 8.1 Program.cs

```csharp
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");
builder.Services.AddMvc()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
builder.Services.Configure<RequestLocalizationOptions>(opts =>
{
    opts.DefaultRequestCulture = new RequestCulture("en");
    opts.SupportedCultures = supportedCultures;
    opts.SupportedUICultures = supportedCultures;
    opts.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

app.UseRequestLocalization();
```

### 8.2 _Layout.cshtml RTL switching

```html
@inject IHttpContextAccessor Accessor
@{
    var culture = Accessor.HttpContext?.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture.Name ?? "en";
    var isRtl = culture.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
    var dir = isRtl ? "rtl" : "ltr";
}
<!DOCTYPE html>
<html lang="@culture" dir="@dir">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] | YallaJo</title>

    @if (isRtl)
    {
        <link rel="stylesheet" href="~/css/style.rtl.css" />
    }
    else
    {
        <link rel="stylesheet" href="~/css/style.css" />
    }
    <link rel="stylesheet" href="~/lib/bootstrap-icons/font/bootstrap-icons.css" />
    @await RenderSectionAsync("Styles", required: false)

    <!-- SEO -->
    <meta name="description" content="@ViewData["MetaDescription"]" />
    <link rel="canonical" href="@ViewData["Canonical"]" />
    <meta property="og:title" content="@ViewData["Title"]" />
    <meta property="og:description" content="@ViewData["MetaDescription"]" />
    <meta property="og:image" content="@ViewData["OgImage"]" />
    <link rel="alternate" hreflang="en" href="@(ViewData["Canonical"])?culture=en" />
    <link rel="alternate" hreflang="ar" href="@(ViewData["Canonical"])?culture=ar" />

    @await RenderSectionAsync("Head", required: false)
</head>
<body>
    @await Html.PartialAsync("_Header")
    <main>@RenderBody()</main>
    @await Html.PartialAsync("_Footer")

    <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    <script src="~/js/functions.js"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

### 8.3 Language switcher in `_Header.cshtml`

```html
<div class="dropdown">
    <button class="btn btn-link nav-link" data-bs-toggle="dropdown">
        <i class="bi bi-globe"></i> @(culture == "ar" ? "العربية" : "English")
    </button>
    <ul class="dropdown-menu">
        <li><a class="dropdown-item" href="?culture=en">English</a></li>
        <li><a class="dropdown-item" href="?culture=ar">العربية</a></li>
    </ul>
</div>
```

---

## 9. SEO Strategy

### 9.1 Per-action ViewData

Set in every action:
```csharp
ViewData["Title"] = "Petra by Night";
ViewData["MetaDescription"] = "Experience the magic of Petra...";
ViewData["OgImage"] = tour.PrimaryImageUrl;
ViewData["Canonical"] = $"https://yallajo.com/tours/{tour.Slug}";
```

### 9.2 JSON-LD structured data on tour detail

```html
@section Head {
<script type="application/ld+json">
{
  "@@context": "https://schema.org",
  "@@type": "TouristAttraction",
  "name": "@Model.Tour.Title",
  "description": "@Model.Tour.Description",
  "image": "@Model.Tour.PrimaryImageUrl",
  "geo": {
    "@@type": "GeoCoordinates",
    "latitude": @Model.Tour.MeetingPointLatitude,
    "longitude": @Model.Tour.MeetingPointLongitude
  },
  "offers": {
    "@@type": "Offer",
    "price": "@Model.Tour.BasePrice",
    "priceCurrency": "@Model.Tour.Currency"
  }
}
</script>
}
```

### 9.3 Sitemap.xml + robots.txt

In Program.cs:
```csharp
app.Map("/sitemap.xml", async (HttpContext ctx, ISitemapApiClient client) =>
{
    var stream = await client.GetSitemapStreamAsync(ctx.RequestAborted);
    ctx.Response.ContentType = "application/xml";
    await stream.CopyToAsync(ctx.Response.Body);
});
```

`robots.txt` in `wwwroot/`:
```
User-agent: *
Allow: /
Disallow: /Account/
Disallow: /Provider/
Disallow: /Admin/
Sitemap: https://yallajo.com/sitemap.xml
```

---

## 10. Forms, Validation, Antiforgery

### 10.1 ViewModel with DataAnnotations

```csharp
public sealed class CreateReviewViewModel
{
    [Required(ErrorMessage = "Please select a rating")]
    [Range(1.0, 5.0)]
    public decimal Rating { get; set; }

    [StringLength(150)]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Review text is required")]
    [StringLength(2000, MinimumLength = 20, ErrorMessage = "Review must be 20-2000 characters")]
    public string Content { get; set; } = "";

    [Display(Name = "Visit date")]
    public DateOnly? VisitDate { get; set; }

    public Guid TargetId { get; set; }
    public ReviewTargetType TargetType { get; set; }
}
```

### 10.2 Razor form

```html
<form asp-action="Create" method="post" enctype="multipart/form-data">
    @Html.AntiForgeryToken()
    <input asp-for="TargetId" type="hidden" />
    <input asp-for="TargetType" type="hidden" />

    <div class="mb-3">
        <label asp-for="Rating" class="form-label"></label>
        <partial name="_RatingInput" model="Model" />
        <span asp-validation-for="Rating" class="text-danger"></span>
    </div>

    <div class="mb-3">
        <label asp-for="Content" class="form-label"></label>
        <textarea asp-for="Content" class="form-control" rows="5"></textarea>
        <span asp-validation-for="Content" class="text-danger"></span>
    </div>

    <button type="submit" class="btn btn-primary">Submit review</button>
</form>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

### 10.3 Multi-step booking (template Stepper)

Use `TempData` for cross-step state OR hidden fields carry state to the next step. Each step is a separate POST action that returns the next step's view.

---

## 11. Caching

### 11.1 OutputCache

```csharp
builder.Services.AddOutputCache(opts =>
{
    opts.AddPolicy("PublicShort", b => b.Expire(TimeSpan.FromMinutes(5)));
    opts.AddPolicy("PublicLong", b => b.Expire(TimeSpan.FromHours(1)));
});
```

```csharp
[OutputCache(PolicyName = "PublicShort", VaryByQueryKeys = new[] { "page", "categoryId" })]
public IActionResult Index(TourFilters filters) { ... }
```

### 11.2 IMemoryCache

For typed API responses (e.g., categories tree refreshed every hour):
```csharp
public async Task<IReadOnlyList<CategoryTreeDto>> GetTreeAsync(CancellationToken ct)
{
    return await _memoryCache.GetOrCreateAsync("categories:tree", async entry =>
    {
        entry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
        return await _categoriesApi.GetTreeAsync(ct);
    });
}
```

### 11.3 ETag / Last-Modified

For tour/place/blog detail pages, set ETag based on entity UpdatedAt → CDN/browser caches 304 Not Modified.

---

## 12. Wave-Aligned Sprint Plan

| Sprint | Duration | Deliverable | Backend Dep |
|---|---|---|---|
| **UI-0: Bootstrap** | 1 week | Scaffolding: layouts, identity cookie, HttpClient infra, RTL/i18n base, template assets copied | — |
| **UI-1: Public foundation** | 1 week | Home, About, Contact, FAQ, Pricing, Terms, Privacy, Error, ComingSoon | Wave 1 ✅ |
| **UI-2: Auth** | 1 week | SignIn, SignUp, VerifyEmail, ForgotPassword, ResetPassword, OAuth | Wave 1 (need 3 OAuth) |
| **UI-3: Catalog** | 2 weeks | Tours grid+detail+compare, Places, Businesses, Search, Map | Waves 3-4 ✅ |
| **UI-4: Blog + Help** | 1 week | Blog index/detail/comment, Help center | Wave 4 ✅ |
| **UI-5: Booking flow** | 2 weeks | Tour booking multi-step + Payment redirect + Confirmation | Wave 5 (Availability + RefundPolicy) |
| **UI-6: User account** | 1.5 weeks | Bookings, Wishlist, Profile, Settings, Travelers, Support | Wave 6 ✅ |
| **UI-7: Provider onboarding** | 2 weeks | Apply, Status, Documents | **Wave 2 NOT BUILT — backend prerequisite** |
| **UI-8: Provider dashboard** | 2 weeks | Dashboard, Listings (CRUD), Bookings (3 tabs), Earnings, Reviews | Wave 5 + 3 provider read endpoints |
| **UI-9: Provider advanced** | 1 week | Availability, Refund policy, Join requests, Discounts (Phase 3 stub) | Wave 5 ✅ |
| **UI-10: Admin dashboard** | 2 weeks | Dashboard, Providers, Bookings, Users, Tours queue, Reviews moderation | All ✅ |
| **UI-11: Admin settings** | 1 week | Categories tree, Tags, Specializations, Languages, Roles, Commissions, NotificationTemplates, SEO redirects | All ✅ |
| **UI-12: Notifications + Support** | 1 week | SignalR bell, In-app panel, Support tickets (user + admin) | Wave 6 ✅ |
| **UI-13: Polish** | 1.5 weeks | Performance, accessibility (WCAG AA), i18n review, error pages | All |

**Total: ~19 weeks (~4.5 months for 2 senior MVC devs)**

---

## 13. Critical Backend Dependencies

UI sprints are blocked on these backend deliverables:

- **UI-2 (Auth):** Wave 1 needs 3 explicit OAuth endpoints (`/auth/external/apple|facebook|google`)
- **UI-5 (Booking):** Wave 5 needs Availability Slots (6 endpoints) + Refund Policy (3 endpoints)
- **UI-7 (Provider onboarding):** Wave 2 needs entire Provider Application module (11 endpoints) — **largest gap**
- **UI-8 (Provider dashboard):** Wave 5 needs 3 provider booking read endpoints (`/bookings/provider/{pending|upcoming|history}`)
- **UI-9 (Provider advanced):** Wave 5 needs Join Requests (3 endpoints)

See `Agents/Waves/Wave-1.md`, `Wave-2.md`, `Wave-5.md` for backend work breakdowns.

---

## 14. NuGet Packages

```xml
<ItemGroup>
  <!-- ASP.NET Core MVC -->
  <PackageReference Include="Microsoft.AspNetCore.Authentication.Cookies" />
  <PackageReference Include="Microsoft.AspNetCore.Mvc.NewtonsoftJson" />
  <PackageReference Include="Microsoft.Extensions.Localization" />

  <!-- HTTP client + resilience -->
  <PackageReference Include="Microsoft.Extensions.Http" Version="9.0.0" />
  <PackageReference Include="Microsoft.Extensions.Http.Polly" Version="9.0.0" />
  <PackageReference Include="Polly" Version="8.4.0" />

  <!-- SignalR client (server-side wrapper if needed; JS client via CDN/wwwroot) -->
  <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="9.0.0" />

  <!-- Caching -->
  <PackageReference Include="Microsoft.Extensions.Caching.Memory" />
  <PackageReference Include="Microsoft.AspNetCore.OutputCaching" />

  <!-- Humanizer (relative time) -->
  <PackageReference Include="Humanizer.Core" Version="2.14.1" />
  <PackageReference Include="Humanizer.Core.ar" Version="2.14.1" />

  <!-- Markdown (if blog content is markdown) -->
  <PackageReference Include="Markdig" Version="0.38.0" />

  <!-- Image processing (avatar resize fallback) -->
  <PackageReference Include="SixLabors.ImageSharp" Version="3.1.4" />

  <!-- Optional: WebOptimizer for bundling -->
  <PackageReference Include="LigerShark.WebOptimizer.Core" Version="3.0.421" />
</ItemGroup>

<ItemGroup>
  <!-- Reuse Contracts DTOs from solution -->
  <ProjectReference Include="..\Auth.Contracts\Auth.Contracts.csproj" />
  <ProjectReference Include="..\Accounts.Contracts\Accounts.Contracts.csproj" />
  <ProjectReference Include="..\ContentTours.Contracts\ContentTours.Contracts.csproj" />
  <ProjectReference Include="..\ContentPlaces.Contracts\ContentPlaces.Contracts.csproj" />
  <ProjectReference Include="..\ContentBlogs.Contracts\ContentBlogs.Contracts.csproj" />
  <ProjectReference Include="..\ContentCore.Contracts\ContentCore.Contracts.csproj" />
  <ProjectReference Include="..\ContentSeo.Contracts\ContentSeo.Contracts.csproj" />
  <ProjectReference Include="..\Booking.Contracts\Booking.Contracts.csproj" />
  <ProjectReference Include="..\Finance.Contracts\Finance.Contracts.csproj" />
  <ProjectReference Include="..\Social.Contracts\Social.Contracts.csproj" />
  <ProjectReference Include="..\Messaging.Contracts\Messaging.Contracts.csproj" />
  <ProjectReference Include="..\Analytics.Contracts\Analytics.Contracts.csproj" />
  <ProjectReference Include="..\Security.Contracts\Security.Contracts.csproj" />
  <ProjectReference Include="..\YallaJo.SharedKernel.Application\YallaJo.SharedKernel.Application.csproj" />
</ItemGroup>
```

---

## 15. Migration Steps — First 2 Weeks

### Day 1-2: Asset migration
1. `xcopy /E template\assets\* YallaJo.Web\wwwroot\lib\` for vendor libs
2. Copy `template\assets\css\style.css` → `wwwroot\css\style.css`
3. Copy `template\rtl\style.rtl.css` → `wwwroot\css\style.rtl.css`
4. Copy `template\assets\js\functions.js` → `wwwroot\js\functions.js`
5. Copy `template\assets\images\` → `wwwroot\images\` (placeholders for now)

### Day 3-4: Auth + HttpClient infra
1. Configure cookie authentication in `Program.cs`
2. Create `AuthTokenHandler` DelegatingHandler
3. Register typed HttpClients for top 5 services (Auth, Tours, Bookings, Profile, Notifications)
4. Test sign-in flow end-to-end

### Day 5-7: Layouts + shared partials
1. Build `_Layout.cshtml` from `index-tour.html` (extract header + footer)
2. Build `_LayoutAuth.cshtml` (centered card from sign-in.html)
3. Build `_Header.cshtml` partial with language switcher + notification bell placeholder
4. Build `_Footer.cshtml` partial
5. Build `_TourCard.cshtml` (extract from tour-grid.html)
6. Build `_RatingStars.cshtml`, `_StatusBadge.cshtml`, `_Pager.cshtml`

### Day 8-10: First end-to-end page
1. Home controller + Index view (replace dummy data from index-tour.html with API data)
2. Featured tours carousel calling `IToursApiClient.GetFeaturedAsync()`
3. Categories navigation calling `ICategoriesApiClient.GetTreeAsync()`
4. Sign-in/Sign-up working end-to-end

### Day 11-14: Tour catalog
1. Tours controller (Index + Detail + Compare)
2. Tour grid with filters (sidebar)
3. Tour detail with reviews, weather, availability calendar
4. Tour booking flow (multi-step stepper) — stub until Wave 5 endpoints ready

---

## 16. Open Decisions

1. **Bundling:** WebOptimizer for CSS/JS bundling? → **Yes** (modern, dev-friendly)
2. **Cookie domain:** `.yallajo.com` for cross-subdomain SSO? → **Yes**
3. **CDN:** Cloudflare for static assets? → **Yes** + versioned URLs for cache-busting
4. **Image hosting:** Azure Blob (matches backend) or Cloudinary? → **Azure Blob**
5. **Mapbox:** Get production API key + monitor quota
6. **Analytics:** Google Analytics 4 + own `POST /api/v1/interactions`? → **Yes**
7. **Error tracking:** Sentry, Application Insights, Datadog? → **Application Insights** (Azure-native)
8. **Custom domain for Web vs Api:** `yallajo.com` (Web) + `api.yallajo.com` (Api)? → **Yes**

---

## 17. Strengths of MVC Approach for YallaJo

| Aspect | MVC | SPA alternative |
|---|---|---|
| Initial dev velocity | ⭐⭐⭐⭐⭐ (template drops in) | ⭐⭐ (rebuild components) |
| SEO | ⭐⭐⭐⭐⭐ (native server-render) | ⭐⭐⭐ (needs SSR setup) |
| Page transitions | ⭐⭐⭐ (full reloads acceptable) | ⭐⭐⭐⭐⭐ |
| Real-time interactivity | ⭐⭐⭐⭐ (SignalR + jQuery as needed) | ⭐⭐⭐⭐ (React state) |
| Mobile app reuse | ⭐⭐ (separate React Native build) | ⭐⭐⭐⭐ |
| Hosting cost | ⭐⭐⭐⭐ (single .NET process) | ⭐⭐⭐ (CDN + edge functions) |
| Team productivity | ⭐⭐⭐⭐⭐ (one stack, C# end-to-end) | ⭐⭐⭐ (two stacks) |
| Form validation | ⭐⭐⭐⭐⭐ (DataAnnotations + Razor + jQuery validate) | ⭐⭐⭐⭐ (react-hook-form + zod) |
| Antiforgery | ⭐⭐⭐⭐⭐ (built-in) | ⭐⭐⭐ (CSRF tokens manually) |

**Conclusion:** MVC is the right choice. Future mobile app can be a thin React Native wrapper calling the same YallaJo.Api directly — both clients share the API and DTOs.

---

## 18. Performance Rules (UI-PERF-*)

All UI-PERF rules are mandatory. CI builds fail if budgets in **§18.11 (UI-PERF-B)** are exceeded.

### 18.1 Asset Delivery (UI-PERF-A)

**UI-PERF-A1 — Bundle + minify production assets**
- Use `LigerShark.WebOptimizer.Core` to bundle CSS/JS into `bundle.css` and `bundle.js`
- Configure in `Program.cs`:
  ```csharp
  builder.Services.AddWebOptimizer(pipeline =>
  {
      pipeline.AddCssBundle("/css/bundle.css", "css/style.css", "lib/bootstrap-icons/font/bootstrap-icons.css");
      pipeline.AddJavaScriptBundle("/js/bundle.js", "lib/bootstrap/dist/js/bootstrap.bundle.min.js", "js/functions.js");
  });
  ```
- **Forbidden in production:** unminified CSS/JS, multiple `<link>` tags per stylesheet category.

**UI-PERF-A2 — CDN-first for vendor libraries**
- Load Bootstrap, jQuery, SignalR, Mapbox from CDN with `integrity` (SRI) hash + `crossorigin="anonymous"`.
- Local copy in `wwwroot/lib/` as fallback (offline-dev mode + CDN outage).
- **Example:**
  ```html
  <link rel="stylesheet"
        href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css"
        integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH"
        crossorigin="anonymous">
  ```

**UI-PERF-A3 — Versioned static URLs**
- Use `asp-append-version="true"` on every `<link>` / `<script>` / `<img>` for local assets:
  ```html
  <link rel="stylesheet" href="~/css/bundle.css" asp-append-version="true">
  ```
- Allows immutable cache headers (`Cache-Control: public, max-age=31536000, immutable`).

**UI-PERF-A4 — Brotli + Gzip response compression**
- Configure in `Program.cs`:
  ```csharp
  builder.Services.AddResponseCompression(opts =>
  {
      opts.EnableForHttps = true;
      opts.Providers.Add<BrotliCompressionProvider>();
      opts.Providers.Add<GzipCompressionProvider>();
      opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "image/svg+xml", "application/json" });
  });
  builder.Services.Configure<BrotliCompressionProviderOptions>(opts => opts.Level = CompressionLevel.Optimal);
  app.UseResponseCompression();
  ```

**UI-PERF-A5 — Preconnect critical origins**
- Add to `<head>` of `_Layout.cshtml`:
  ```html
  <link rel="preconnect" href="https://api.yallajo.com" crossorigin>
  <link rel="preconnect" href="https://fonts.googleapis.com" crossorigin>
  <link rel="preconnect" href="https://api.mapbox.com" crossorigin>
  <link rel="dns-prefetch" href="https://cdn.jsdelivr.net">
  ```

**UI-PERF-A6 — Defer non-critical JS**
- All `<script>` tags must use `defer` or `async` except inline-critical code.
- Inline scripts maxed at <2KB; larger logic goes to external file.

**UI-PERF-A7 — Lazy load below-the-fold content**
- All images below the fold: `loading="lazy"` attribute.
- Carousels/sliders use `IntersectionObserver` to init on scroll.

**UI-PERF-A8 — Inline critical CSS (above-the-fold)**
- Extract above-the-fold CSS for homepage + tour-grid + tour-detail (most-visited routes) and inline in `<head>` (~14KB max per Google guidance).
- Remaining CSS loaded async via `<link rel="preload" as="style" onload="this.rel='stylesheet'">`.

### 18.2 Output + Response Caching (UI-PERF-C)

**UI-PERF-C1 — OutputCache anonymous pages aggressively**
- Define policies in `Program.cs`:
  ```csharp
  builder.Services.AddOutputCache(opts =>
  {
      opts.AddPolicy("PublicShort", b => b.Expire(TimeSpan.FromMinutes(5)).SetVaryByQuery("page", "categoryId", "placeId"));
      opts.AddPolicy("PublicMedium", b => b.Expire(TimeSpan.FromMinutes(30)));
      opts.AddPolicy("PublicLong", b => b.Expire(TimeSpan.FromHours(1)));
      opts.AddPolicy("PublicDay", b => b.Expire(TimeSpan.FromDays(1)));
  });
  ```
- Recommended TTLs:
  | Page | Policy | TTL |
  |---|---|---|
  | Home `/` | PublicShort | 5 min |
  | Tour grid `/tours` | PublicShort | 5 min |
  | Tour detail `/tours/{slug}` | PublicMedium | 30 min |
  | Place/Business detail | PublicMedium | 30 min |
  | Blog index/detail | PublicLong | 1 hour |
  | FAQ, About, Contact, Privacy, Terms | PublicDay | 24 hours |
  | Help center | PublicLong | 1 hour |

**UI-PERF-C2 — No-cache for authenticated personalized content**
- Add `[OutputCache(NoStore = true)]` to all `/Account/*`, `/Provider/*`, `/Admin/*` actions.
- Set headers: `Cache-Control: no-store, no-cache, must-revalidate`.
- **Forbidden:** caching wishlist state, booking lists, dashboard data.

**UI-PERF-C3 — Tag-based invalidation via integration events**
- Subscribe to backend integration events from Web tier; invalidate cache tags on entity update:
  ```csharp
  // BackgroundService listens to tour.updated.v1 → invalidates tour:{id} cache
  await outputCacheStore.EvictByTagAsync($"tour:{tourId}", ct);
  ```
- Cache tags pattern: `tour:{id}`, `place:{id}`, `category:tree`, `homepage`.

**UI-PERF-C4 — IMemoryCache for high-frequency typed data**
- Cache categories tree (1 hour), language list (24 hours), commission rules (10 min).
- Use sliding expiration for hot keys, absolute for global config.

**UI-PERF-C5 — ETag + Last-Modified on detail pages**
- Hash entity's `UpdatedAt` → ETag header.
- Return `304 Not Modified` if `If-None-Match` matches.
- Saves 80%+ bandwidth for repeat visitors.

**UI-PERF-C6 — CDN edge caching (Cloudflare)**
- Public anonymous routes cacheable at edge with `Cache-Control: public, s-maxage=300, stale-while-revalidate=600`.
- Authenticated routes: `Cache-Control: private, no-cache`.
- Bypass CDN for `Cookie: YallaJo.Auth=...` requests via Cloudflare cache rule.

### 18.3 API Call Patterns (UI-PERF-API)

**UI-PERF-API1 — Always parallelize independent API calls**
- Use `Task.WhenAll`:
  ```csharp
  var tourTask = toursApi.GetBySlugAsync(slug, ct);
  var reviewsTask = reviewsApi.ListAsync(filters, ct);
  var weatherTask = weatherApi.GetForecastAsync(placeId, ct);
  await Task.WhenAll(tourTask, reviewsTask, weatherTask);
  ```
- **Forbidden:** Sequential `await` chains for independent data.

**UI-PERF-API2 — Fire-and-forget analytics**
- Never `await` analytics interaction calls in the request path:
  ```csharp
  _ = analyticsApi.RecordInteractionAsync(...);  // intentional discard
  ```
- Use `Task.Run` only if backend client doesn't already buffer.

**UI-PERF-API3 — Polly retry + circuit breaker on all clients**
- Retry policy: 3x exponential backoff (200ms, 800ms, 3.2s) on `5xx` and `HttpRequestException` only — never on `4xx`.
- Circuit breaker: open after 5 failures in 30s, half-open after 30s.
- Register globally:
  ```csharp
  static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
      HttpPolicyExtensions.HandleTransientHttpError()
          .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(4, attempt - 1)));
  ```

**UI-PERF-API4 — Hard timeouts on every HttpClient**
- Default timeout 30s; lower for hot paths (5s for homepage feed, 2s for autocomplete).
- Set per-client in registration:
  ```csharp
  client.Timeout = TimeSpan.FromSeconds(5);
  ```

**UI-PERF-API5 — Reuse HttpClient via IHttpClientFactory**
- **Forbidden:** `new HttpClient(...)` anywhere in codebase (socket exhaustion).
- All HTTP via `IXxxApiClient` injected via DI.

**UI-PERF-API6 — Request response compression**
- All HttpClient instances send `Accept-Encoding: gzip, deflate, br` header (default in .NET 9 via `HttpClientHandler.AutomaticDecompression`).

**UI-PERF-API7 — Batch endpoints over loops**
- **Forbidden:** Loop calling single-item GET in controller.
  ```csharp
  // BAD
  foreach (var id in tourIds) var t = await api.GetByIdAsync(id);

  // GOOD
  var tours = await api.GetByIdsAsync(tourIds);  // backend exposes batch endpoint
  ```

**UI-PERF-API8 — Connection pool sizing**
- Default `HttpClient` connection pool is 10 per server. For high-traffic pages calling API >10x:
  ```csharp
  builder.Services.AddHttpClient<IToursApiClient, ToursApiClient>()
      .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
      {
          MaxConnectionsPerServer = 100,
          PooledConnectionLifetime = TimeSpan.FromMinutes(5)
      });
  ```

### 18.4 Page Rendering (UI-PERF-R)

**UI-PERF-R1 — Async controllers + views only**
- All actions return `Task<IActionResult>`.
- All Razor I/O uses `await` (`@await Html.PartialAsync(...)`).
- **Forbidden:** Synchronous `.Result` or `.Wait()` calls.

**UI-PERF-R2 — Server-side rendering only — no view-side fetching**
- All data fetched in the controller; passed to view via ViewModel.
- **Forbidden:** AJAX calls from Razor for primary page content (only for interactive features like favorite toggle, comment submit).

**UI-PERF-R3 — Pre-compile Razor views in production**
- Set `<RazorCompileOnBuild>true</RazorCompileOnBuild>` in csproj.
- Saves 200-500ms on first request after deploy.

**UI-PERF-R4 — Pagination limits**
- Default page size 20; max 50. Enforced at controller:
  ```csharp
  pageSize = Math.Clamp(pageSize, 1, 50);
  ```
- **Forbidden:** unbounded lists in views.

**UI-PERF-R5 — Avoid TempData abuse**
- TempData uses cookies/session — large payloads slow every request.
- Max TempData size: 4KB. Use database for larger flows (multi-step wizards).

**UI-PERF-R6 — Stream large list pages**
- For admin tables >500 rows: use server-side DataTables pagination, never load all rows.
- For exports >10K rows: streaming response with `IAsyncEnumerable` + chunked transfer encoding.

**UI-PERF-R7 — No N+1 in views**
- Project ViewModels with all related data pre-loaded.
- **Forbidden:** lazy-loading entity navigation properties inside `@foreach`.

### 18.5 Image Optimization (UI-PERF-I)

**UI-PERF-I1 — Modern format priority: AVIF → WebP → JPEG/PNG**
- Backend's `/images/{id}?format=avif&w=400` endpoint serves optimized variants.
- Razor partial `_ResponsiveImage.cshtml`:
  ```html
  <picture>
      <source srcset="@Model.AvifUrl" type="image/avif">
      <source srcset="@Model.WebpUrl" type="image/webp">
      <img src="@Model.JpegUrl" alt="@Model.Alt" loading="lazy" width="@Model.Width" height="@Model.Height">
  </picture>
  ```

**UI-PERF-I2 — Responsive images with srcset**
- 5 sizes per image: 320w, 640w, 960w, 1280w, 1920w.
- Use `sizes` attribute matching layout breakpoints:
  ```html
  <img srcset="img-320.jpg 320w, img-640.jpg 640w, img-1280.jpg 1280w"
       sizes="(max-width: 600px) 100vw, (max-width: 1200px) 50vw, 33vw"
       loading="lazy">
  ```

**UI-PERF-I3 — Always set `width` + `height` attributes**
- Prevents Cumulative Layout Shift (CLS).
- For dynamic images, set CSS aspect-ratio:
  ```css
  .tour-card-image { aspect-ratio: 16 / 9; object-fit: cover; }
  ```

**UI-PERF-I4 — Preload hero image on landing pages**
- Add to `_Layout.cshtml` section:
  ```html
  @section Head {
      <link rel="preload" as="image" href="@Model.HeroImageUrl" imagesrcset="..." imagesizes="100vw">
  }
  ```

**UI-PERF-I5 — Image weight budgets**
| Context | Max size |
|---|---|
| Thumbnail (card) | 30 KB |
| Hero image (homepage) | 200 KB |
| Detail page primary | 150 KB |
| Detail page gallery | 100 KB each |
| Avatar | 20 KB |
| Total page image weight | 1 MB |

**UI-PERF-I6 — Low-quality image placeholder (LQIP)**
- Generate 16x16 base64 blurred preview server-side.
- Embed inline as `background-image` on `<img>` container while real image loads.

**UI-PERF-I7 — Image CDN**
- Use Cloudflare Images OR Azure CDN OR ImageKit.
- Cache headers: `public, max-age=31536000, immutable` on hashed URLs.

### 18.6 JavaScript (UI-PERF-J)

**UI-PERF-J1 — Vanilla JS preferred over jQuery**
- New code: Vanilla DOM APIs (`querySelector`, `addEventListener`, `fetch`).
- jQuery only where vendor lib requires it (some template scripts).

**UI-PERF-J2 — Page-specific JS bundles**
- Don't load all JS on every page. Split bundles per route group:
  - `home.bundle.js` (Tiny Slider, AOS, PureCounter)
  - `catalog.bundle.js` (Choices.js, noUiSlider, Mapbox)
  - `booking.bundle.js` (Flatpickr, Stepper)
  - `dashboard.bundle.js` (ApexCharts, Dropzone)
  - `editor.bundle.js` (Quill, GLightbox)
- Load via `@section Scripts { <script src="~/js/booking.bundle.js"></script> }`.

**UI-PERF-J3 — No inline event handlers**
- **Forbidden:** `onclick="..."` in markup.
- Attach via `addEventListener` in deferred script for CSP compatibility.

**UI-PERF-J4 — Debounce expensive operations**
- Search autocomplete: debounce 300ms.
- Scroll handlers: throttle to 100ms via `requestAnimationFrame`.
- Window resize: debounce 250ms.

**UI-PERF-J5 — Avoid layout thrashing**
- Batch DOM reads then writes (no interleaving).
- Use CSS transforms (`translate`, `scale`) over geometric properties (`top`, `left`, `width`).

**UI-PERF-J6 — Web Workers for heavy tasks**
- Image preview generation, large JSON parsing, complex filters → Web Worker.
- Threshold: any sync task >50ms on main thread.

**UI-PERF-J7 — Minify + tree-shake in production**
- WebOptimizer with `Minify()` for production builds.
- Custom Bootstrap build excluding unused components (alert, modal, etc. only).

### 18.7 SignalR (UI-PERF-S)

**UI-PERF-S1 — WebSocket transport, long-polling fallback**
- Configure client:
  ```javascript
  .withUrl(url, {
      transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      skipNegotiation: false
  })
  ```
- Skip server-sent events (legacy, no advantage).

**UI-PERF-S2 — Connect only on authenticated pages**
- **Forbidden:** SignalR connection in `_Layout.cshtml` for anonymous users.
- Wrap connection script in `@if (User.Identity?.IsAuthenticated == true)`.

**UI-PERF-S3 — Send IDs, not payloads**
- Hub broadcasts `{ notificationId }` only.
- Client fetches full notification via `GET /api/v1/notifications/{id}` only if user clicks bell.
- Saves bandwidth on broadcasts to 1000s of connected users.

**UI-PERF-S4 — Auto-reconnect with capped backoff**
- `.withAutomaticReconnect([0, 2000, 10000, 30000, 60000])`
- After 5 attempts → show toast: "Reconnecting..." with manual reconnect button.

**UI-PERF-S5 — Group scoping**
- Each connection joins exactly: `user:{userId}` + optionally `provider:{providerId}` + `admin` (if admin role).
- **Forbidden:** broadcasting to all-connections group.

**UI-PERF-S6 — Connection limit per user**
- Max 5 active SignalR connections per user (multi-tab).
- Backend disconnects oldest if exceeded.

**UI-PERF-S7 — Keep-alive interval**
- Client: 15s ping. Server: 30s timeout.
- Faster detection of disconnects without flooding ping traffic.

### 18.8 Database/Data (UI-PERF-D — applies to API tier consumed by Web)

These ride along with backend rules but reinforce UI-tier expectations:

**UI-PERF-D1 — Cursor pagination > offset pagination**
- All list endpoints use cursor (already enforced by backend per M-R10, F-R10, S-R9, B-R10).

**UI-PERF-D2 — Projection DTOs, not entity dumps**
- Backend returns lean DTOs (already enforced). Web tier must not request unnecessary fields.

**UI-PERF-D3 — Compiled HTTP request bodies**
- Use `JsonSerializerOptions` instance reused across calls (don't create per-request):
  ```csharp
  private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
  {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };
  ```

### 18.9 Bootstrap + Vendor Optimization (UI-PERF-V)

**UI-PERF-V1 — Custom Bootstrap build**
- Use SASS to compile only used components (skip Carousel if using Splide, skip Toast if using custom).
- Cuts Bootstrap CSS from ~230KB to ~80KB.

**UI-PERF-V2 — Page-specific vendor loading**
- Don't load Mapbox on pages without maps.
- Don't load ApexCharts on non-dashboard pages.
- Use `@section Scripts` per view to load only needed libs.

**UI-PERF-V3 — Self-host fonts**
- Don't load fonts from Google Fonts (extra DNS + TLS handshake).
- Self-host woff2 in `wwwroot/fonts/` with `font-display: swap`.

**UI-PERF-V4 — Subset fonts**
- Latin + Arabic subset only. Cuts font weight from 100KB → 30KB.

### 18.10 Hosting + Infrastructure (UI-PERF-H)

**UI-PERF-H1 — HTTP/2 enabled**
- Kestrel default in .NET 9. Verify `appsettings.json`:
  ```json
  "Kestrel": { "Endpoints": { "Https": { "Protocols": "Http2" } } }
  ```

**UI-PERF-H2 — HSTS + TLS 1.3**
- `app.UseHsts()` with 1-year max-age in production.
- Force TLS 1.3 minimum.

**UI-PERF-H3 — Cloudflare in front**
- Origin shielding ON.
- Auto-minify HTML/CSS/JS.
- Brotli auto-compression at edge.
- Page rules: cache static assets (`*.css`, `*.js`, `*.png`) for 1 year with `Cache-Control: immutable`.

**UI-PERF-H4 — Health checks + auto-scale**
- Expose `/health` + `/health/ready` endpoints (already in YallaJo.Api; mirror in Web).
- Auto-scale Web tier on CPU >70% or request queue >50.

**UI-PERF-H5 — Static file middleware tuning**
- Configure `app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = ... })` to set `Cache-Control: public, max-age=31536000, immutable` for hashed URLs.

**UI-PERF-H6 — Compression at all layers**
- Brotli at Cloudflare edge.
- Brotli/Gzip at Kestrel.
- HTTP/2 frame-level compression.

### 18.11 Performance Budgets (UI-PERF-B)

Measured via **WebPageTest** + **Lighthouse CI** + **Real User Monitoring** (Application Insights). CI gates fail builds exceeding limits.

**Core Web Vitals (mobile, 3G slow):**
| Metric | Target | Hard ceiling |
|---|---|---|
| Largest Contentful Paint (LCP) | < 2.0s | < 2.5s |
| First Contentful Paint (FCP) | < 1.2s | < 1.5s |
| Time to Interactive (TTI) | < 3.0s | < 3.5s |
| Total Blocking Time (TBT) | < 150ms | < 200ms |
| Cumulative Layout Shift (CLS) | < 0.05 | < 0.1 |
| Interaction to Next Paint (INP) | < 150ms | < 200ms |
| First Input Delay (FID) | < 50ms | < 100ms |

**Network budgets (per page):**
| Resource | Max |
|---|---|
| HTML document size | 100 KB (uncompressed) |
| Total JS (gzipped) | 200 KB |
| Total CSS (gzipped) | 50 KB |
| Total image weight | 1 MB |
| Total font weight | 50 KB |
| HTTP requests | 50 |
| Total page weight | 1.5 MB |

**Server response budgets (p95):**
| Page type | TTFB target | Hard ceiling |
|---|---|---|
| Anonymous cached (homepage, tour-grid) | < 100ms | < 200ms |
| Anonymous uncached | < 300ms | < 500ms |
| Authenticated personalized | < 500ms | < 1000ms |
| Dashboard pages | < 600ms | < 1200ms |
| Search results | < 400ms | < 800ms |

**SignalR budgets:**
| Metric | Target |
|---|---|
| Hub message latency p95 | < 200ms |
| Reconnection time (median) | < 5s |
| Concurrent connections per server | 10,000+ |

### 18.12 Monitoring + Observability (UI-PERF-M)

**UI-PERF-M1 — Application Insights for server-side**
- Track per-route p50/p95/p99 latency.
- Custom metric: API call count per page.
- Alert: p95 > hard ceiling for 5 min.

**UI-PERF-M2 — Real User Monitoring (RUM)**
- Use Application Insights JavaScript SDK OR `web-vitals` library:
  ```javascript
  import { onLCP, onFID, onCLS, onINP } from 'web-vitals';
  onLCP(metric => sendToBackend(metric));
  onFID(metric => sendToBackend(metric));
  ```
- Send to `POST /api/v1/analytics/rum` (verify endpoint or use Application Insights).

**UI-PERF-M3 — Lighthouse CI in pipeline**
- Run on every PR against staging deployment.
- Fail build if any budget exceeded.
- Config in `.lighthouserc.json`:
  ```json
  {
    "ci": {
      "assert": {
        "assertions": {
          "categories:performance": ["error", { "minScore": 0.9 }],
          "largest-contentful-paint": ["error", { "maxNumericValue": 2500 }]
        }
      }
    }
  }
  ```

**UI-PERF-M4 — Synthetic monitoring**
- Pingdom OR UptimeRobot every 5 min for `/`, `/tours`, `/sign-in` from 3 geo locations.
- Alert if response time > 2x baseline.

**UI-PERF-M5 — Bundle size tracking**
- Webpack-bundle-analyzer OR equivalent.
- CI fails if bundle grows >10% without justification.

### 18.13 Anti-patterns — Forbidden (UI-PERF-X)

**UI-PERF-X1 —** `<script>` tags in `<head>` without `defer` or `async` (blocks render)
**UI-PERF-X2 —** Inline images >5KB as base64 (bloats HTML)
**UI-PERF-X3 —** Synchronous XMLHttpRequest (deprecated, blocks main thread)
**UI-PERF-X4 —** Sync `Thread.Sleep` or `Task.Delay(...).Wait()` in controllers
**UI-PERF-X5 —** `new HttpClient()` in any class (use `IHttpClientFactory`)
**UI-PERF-X6 —** Inline styles `style="..."` in Razor markup (no caching, bloats HTML)
**UI-PERF-X7 —** `document.write()` (blocks parser)
**UI-PERF-X8 —** Loading entire jQuery just for `$.ajax` (use `fetch`)
**UI-PERF-X9 —** Sending JWTs to clients via JavaScript variables (use HttpOnly cookies)
**UI-PERF-X10 —** Web fonts with `font-display: block` (FOIT — flash of invisible text)
**UI-PERF-X11 —** Multiple SignalR connections from same page (one global per layout)
**UI-PERF-X12 —** Polling endpoints (`setInterval`) when SignalR is available
**UI-PERF-X13 —** Animating `width`/`height`/`top`/`left` (use `transform`)
**UI-PERF-X14 —** Loading admin charts library (ApexCharts) on public pages
**UI-PERF-X15 —** Storing JWT in `localStorage` or `sessionStorage` (XSS risk; use cookies)

### 18.14 Per-Page Performance Targets

Each route has a specific budget enforced in CI:

| Route | LCP target | TTI target | Page weight target | Notes |
|---|---|---|---|---|
| `/` (Home) | 1.5s | 2.5s | 1 MB | Hero image preloaded, above-fold CSS inlined |
| `/tours` (grid) | 1.8s | 3.0s | 1.2 MB | Lazy-load below-fold cards |
| `/tours/{slug}` (detail) | 2.0s | 3.5s | 1.5 MB | Hero gallery, Mapbox deferred |
| `/sign-in` | 1.2s | 2.0s | 300 KB | Minimal CSS, no Mapbox/charts |
| `/Account/Bookings` | 1.5s | 2.5s | 500 KB | Personalized, no edge cache |
| `/Provider/Dashboard` | 1.8s | 3.0s | 800 KB | Charts lazy-loaded |
| `/Admin/Dashboard` | 2.0s | 3.5s | 1 MB | Heavy charts, deferred |
| `/search` | 1.5s | 2.5s | 600 KB | Server-side facets, lazy results |
| `/blog/{slug}` | 1.8s | 3.0s | 1 MB | Featured image preloaded |

### 18.15 Implementation Checklist

For every new page, verify:
- [ ] OutputCache policy assigned (or `NoStore = true` for personalized)
- [ ] Parallel `Task.WhenAll` for independent API calls
- [ ] `width` + `height` on every `<img>`
- [ ] `loading="lazy"` on below-fold images
- [ ] `@section Scripts` loads only required vendor libs
- [ ] Razor view < 30KB rendered HTML
- [ ] No N+1 queries (ViewModel pre-loaded with all deps)
- [ ] ETag header on detail pages
- [ ] `MustHavePermissionAttribute` (Auth-Cleanup sprint rule)
- [ ] Mobile-tested at 3G slow speeds (Chrome DevTools)
- [ ] Lighthouse score >= 90 (performance category)

---

## 19. Next Steps

1. Run `dotnet new mvc -o YallaJo.Web` if project doesn't fully exist yet (it does — extend existing)
2. Day 1-2: Copy template assets per §15
3. Day 3-4: Set up cookie auth + HttpClient infrastructure
4. Day 5-7: Build base layouts + shared partials
5. Day 8-10: First end-to-end page (Home + Sign-in)
6. Configure performance budgets in CI per §18.11 before first deploy
7. Start UI-1 sprint per §12 plan

**Companion files to create later:**
- `Agents/UI/Controllers-Checklist.md` — every controller/action TODO with route + endpoint + ViewModel
- `Agents/UI/Component-Library.md` — reusable Razor partials documentation
- `Agents/UI/Style-Guide.md` — design tokens, typography, spacing extracted from template's style.css
- `Agents/UI/Performance-Runbook.md` — production performance incident response procedures
