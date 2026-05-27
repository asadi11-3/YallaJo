# YallaJo Frontend — ASP.NET Core MVC Design + Endpoint Map

> **Source template:** `C:\Users\admin1\Desktop\Template\hotel-management-syste-main\booking.webestica.com`
> **Template:** Webestica "Booking" — 74 Bootstrap 5 HTML pages, RTL-ready
> **Pattern:** **ASP.NET Core MVC (Razor Views)** — server-rendered, no SPA framework
> **Existing project:** `YallaJo.Web` already in solution
> **Backend reference:** `Agents/agent-context.md` (PDF1 endpoints), `Agents/guide.md` (PDF2 rules)
> **Pattern catalogue:** `Agents/Plans/UI-UX-Pattern-Report.md` — endpoint-derived persona/IA/component patterns (companion doc)
> **Backend surface (audited 2025-01-27):** **483 HTTP endpoints + 1 SignalR hub** across 13 modules. See §21 for the full coverage matrix.

---

## Backend Surface Snapshot (2025-01-27)

The MVC route plan in this document was originally drafted at ~70 backend endpoints. The current audit shows **483 endpoints + 1 SignalR hub** — many surfaces in this doc need to be re-scoped against the real shape:

| Module | Endpoints | Files | Notes |
|---|---|---|---|
| Accounts | 39 | 6 | Provider apply, agency roster, guide-agency, profile |
| Auth | 26 | 6 | Register/login/OTP/sessions/external/devices |
| Security | 19 | 4 | Users, roles, role claims, audit logs, account |
| ContentBlogs | 60 | 4 | Blog CRUD + comments + creators + admin |
| ContentCore | 44 | 8 | Language / Specialization / Tag / EntityTag / EntityCategory / Category / Translation / Attachment |
| ContentPlaces | 41 | 6 | Place / Business / ServiceItem / Amenity / Staff / Accessibility |
| ContentSeo | 23 | 5 | Weather / Sitemap / Metadata / Redirect / FAQ |
| ContentTours | 75 | 13 | Tour core + Schedule + Pricing + Search + Waypoint + Guide assignment + Package + ChildrenInfo + Applications + Offerings + Profile + Admin + Proposals |
| Booking | 21 | 5 | Slots / GuideDiscount / JoinRequest / TourBooking / AdminBooking |
| Finance | 33 | 7 | Commission / Dispute / Earnings / Invoice / Payment / Payout / ProviderPaymentMethod |
| Analytics | 49 | 3 | Analytics 20 + Recommendations 27 + Preferences 2 |
| Messaging | 23 + 1 hub | 5 | Notification / Device / SupportTicket / Template + SignalR `/hubs/notifications` |
| Social | 27 | 4 | Review / Favorite / Report / Moderation |
| Tracking | 0 | 0 | Background workers only — no HTTP surface |
| YallaJo.Api root | 3 | — | `/` health + 2 ops outbox endpoints |
| **Total** | **483 + 1 hub** | — | — |

**Implications for this design doc:**
- §4 route map covers ~140 MVC routes mapped to ~70 backend endpoints. The remaining ~413 endpoints either belong to admin surfaces already wave-scoped (Waves 1, 6, 7, 8) or to modules not yet covered (Messaging admin, Social moderation, ContentSeo admin, Analytics admin tooling).
- §13 dependency table tracks per-wave readiness; cross-check against the full inventory in `Agents/Plans/UI-UX-Pattern-Report.md` before each sprint kicks off.
- `/hubs/notifications` is the **only** SignalR hub — single client connection in `_Layout` partials (no per-module hubs).

---

## 0. Design Skills to Use When Building These UIs

> **Mandatory:** Before designing any view in this document, load the relevant skill below.
> These four skills collectively own the design quality bar for YallaJo's frontend.
> Invoke via the Skill tool: `skill(name="<skill-name>")`.

### 0.1 The four skills

| # | Skill | When to load | Primary value |
|---|---|---|---|
| 1 | `ui-ux-pro-max` | **Default for every new view.** Whenever you start a Razor page, dashboard, form, table, modal, or any interactive surface. | UI/UX design intelligence with searchable database — pattern lookup, component recipes, interaction heuristics. |
| 2 | `impeccable` | When polishing, hardening, or critiquing an existing UI. Use for visual hierarchy, accessibility audits, responsive bugs, theming, motion, micro-interactions, error/empty states, i18n, and design-system extraction. | Frontend craft — turns bland designs bolder, loud designs quieter, and ambitious effects technically extraordinary. |
| 3 | `design-taste-frontend` | When making *architecture* and *metric-based* design decisions: typography scale, spacing rhythm, contrast ratios, hardware-accelerated CSS, component boundaries, balance between design and engineering. | Senior UI/UX Engineer voice that overrides default LLM design biases with enforced rules. |
| 4 | `huashu-design` | When you need **high-fidelity HTML/CSS prototypes, interactive demos, slide decks, animations, app prototypes (iOS-style), or design variant exploration**. Also: design-direction consulting (recommends 3 differentiated philosophies from 5 schools × 20 design directions), expert 5-dimension review, MP4/GIF export pipelines, narration-driven long-form animations. | Embodied design specialist — picks the right expert persona per task (animator / prototyper / slide designer / consultant / reviewer). Use when the user is vague or asks for "looks great", "show me variants", "review this design". |

### 0.2 When to combine them

| Situation | Load order |
|---|---|
| New page from scratch | `ui-ux-pro-max` → produce structure → `impeccable` to polish → `design-taste-frontend` to enforce metrics |
| Vague request ("make a nice provider apply page") | `huashu-design` first for direction options → pick one → then `ui-ux-pro-max` + `impeccable` |
| Hi-fi interactive prototype before Razor | `huashu-design` only (it handles HTML/React+Babel/animation in one go) |
| Existing view feels "off" | `impeccable` for the critique → `design-taste-frontend` for the metric fix → `ui-ux-pro-max` for the pattern swap |
| Animation or video deliverable | `huashu-design` (handles 60 fps export + GIF/MP4 + BGM) |

### 0.3 Per-wave skill plan — quick reference

> Each wave's detailed UI spec lives in §4.6 – §4.13. Use this table to pick skills before opening the spec.

| Wave | Primary skill | Polish skill | Why |
|---|---|---|---|
| **Wave-1** Foundations (Auth, admin catalog CRUD, Roles, Users) | `ui-ux-pro-max` | `impeccable` | Patterns are well-known (OTP, OAuth row, admin tables); polish matters for first-impression Auth screens. |
| **Wave-2** Provider Application | `ui-ux-pro-max` + `huashu-design` (state-machine page) | `impeccable` | 6-state machine + cooling/reapply UX needs variant exploration. |
| **Wave-3** Public Catalog (Tours, Places, Businesses, Guides) | `huashu-design` (tour detail hero) | `design-taste-frontend` | Tour detail is the conversion page; needs taste-level design before any pattern lookup. |
| **Wave-4** Blog + Tour Enrichment + Admin SEO + Translations | `ui-ux-pro-max` (Blog) + `huashu-design` (article hero variants) | `impeccable` (typography rhythm) | Long-form reading needs strict typography; SEO admin needs density without clutter. |
| **Wave-5** Booking & Payment Engine | `huashu-design` (booking flow A+ design) | `impeccable` (payment + error states) | Booking is revenue-critical; payment screens unforgiving of friction. |
| **Wave-6** Discovery, Search, Map, Notifications, Support | `ui-ux-pro-max` (search/notifs) + `huashu-design` (map + analytics dashboards) | `design-taste-frontend` (dashboard density) | High data density needs metric-driven hierarchy; map is hi-fi prototype territory. |
| **Wave-7** Creator Identity + Blog Authoring | `ui-ux-pro-max` (state machine + admin queue rigor) + `huashu-design` (creator profile hero variants) | `impeccable` (community/identity surfaces) | Identity onboarding mirrors Wave-2 rigor; public creator profiles need brand-level taste; invitation composer needs UX clarity. |
| **Wave-8** Creator Multi-Type Posts + Tier Promotion + Moderation | `huashu-design` (multi-type compose interfaces) + `ui-ux-pro-max` (admin moderation queue + tier dashboards) | `design-taste-frontend` (reading pages perf + tier-up celebration) | 4 distinct post-type editors each need bespoke design; tier promotion celebration is conversion moment; moderation queue is high-density. |

### 0.4 Wave-2 Provider UI — view-by-view skills

| View | Primary skill | Polish skill |
|---|---|---|
| `/Provider/Apply` (multi-step wizard) | `ui-ux-pro-max` | `impeccable` |
| `/Provider/Status` (state-machine page, 6 states) | `huashu-design` (variant exploration first) | `design-taste-frontend` |
| `/Provider/Documents` (drag-drop, type checklist, expiry) | `ui-ux-pro-max` | `impeccable` |
| `/Admin/Providers` (queue + filters + bulk actions) | `ui-ux-pro-max` | `design-taste-frontend` |
| `/Admin/Providers/{id}` (detail + timeline + cascade preview) | `huashu-design` (hi-fi prototype first) | `impeccable` |
| Suspension cascade preview modal ("X tours / Y bookings / Z payouts") | `huashu-design` (animation for impact reveal) | `impeccable` |

### 0.5 Anti-patterns these skills exist to prevent

- Default LLM "AI slop": gradient soup, three-card hero, undifferentiated cards, generic shadcn-clone vibes → `design-taste-frontend` overrides.
- Bland, safe, forgettable UI → `huashu-design` pushes variants.
- Accessibility/contrast/RTL regressions → `impeccable` audits.
- Reinventing patterns that exist → `ui-ux-pro-max` database lookup.

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

> **Wave-2 backend = DONE.** Provider Application module is fully built (11 endpoints) — see §4.6 for the detailed Wave-2 UI spec.

| URL | Controller.Action | Template | Backend |
|---|---|---|---|
| `/Provider/Apply` | Provider.Apply.Index | add-listing-minimal.html | `POST /api/v1/provider/register` + `/apply` ✅ Wave 2 built |
| `/Provider/Status` | Provider.Status.Index | (state-machine page — see §4.6) | `GET /api/v1/provider/status` ✅ Wave 2 built |
| `/Provider/Documents` | Provider.Documents.Index | (drag-drop multi-upload — see §4.6) | `POST /api/v1/provider/documents` + `PUT .../{id}` ✅ Wave 2 built |
| `/Provider/Reapply` (POST) | Provider.Apply.Reapply | (cooling-aware) | `POST /api/v1/provider/apply` after 7-day cooling, max 3 attempts |
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
| `/Admin/Providers` | Admin.Providers.Index | admin-agent-list.html | `GET /api/v1/admin/providers` ✅ Wave 2 built |
| `/Admin/Providers?status=...&type=...&page=...` | Admin.Providers.Index | (filter view) | same endpoint — supports `StatusFilter`, `TypeFilter`, `Page`, `PageSize` query params |
| `/Admin/Providers/Pending` | Admin.Providers.Pending | (filter view) | `GET /api/v1/admin/providers?status=Pending` |
| `/Admin/Providers/{id}` | Admin.Providers.Detail | admin-agent-detail.html | ⚠️ **Backend gap:** no single-get endpoint exists; client must filter the paginated list, or backend needs `GET /api/v1/admin/providers/{id}` added |
| `/Admin/Providers/{id}/Approve` (POST) | Admin.Providers.Approve | — | `POST /api/v1/admin/providers/{id}/approve` ✅ |
| `/Admin/Providers/{id}/Reject` (POST) | Admin.Providers.Reject | — | `POST /api/v1/admin/providers/{id}/reject` (Reason required, triggers 7-day cooling) ✅ |
| `/Admin/Providers/{id}/RequestDocs` (POST) | Admin.Providers.RequestDocs | — | `POST /api/v1/admin/providers/{id}/request-docs` (Notes + MissingDocumentTypes[]) ✅ |
| `/Admin/Providers/{id}/Suspend` (POST) | Admin.Providers.Suspend | — | `POST /api/v1/admin/providers/{id}/suspend` (Reason required — cascades to tours/bookings/payouts) ✅ |
| `/Admin/Providers/{id}/Reinstate` (POST) | Admin.Providers.Reinstate | — | `POST /api/v1/admin/providers/{id}/reinstate` ✅ Wave 2 built — reverses suspension, restores tours |
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

### 4.6 Wave-2 Provider Application — detailed UI spec

> **Status:** Backend complete (Dec 2025). 11 endpoints across `/api/v1/provider/*` and `/api/v1/admin/providers/*`.
> **Recommended skills:** see §0 — start with `ui-ux-pro-max` + `huashu-design` for state-machine page exploration.

#### 4.6.1 Provider Type selector (`/Provider/Apply` — Step 1)

5 provider types with type-specific document requirements (validated server-side; UI must mirror):

| Type | enum | Required document types |
|---|---|---|
| TourOperator | `0` | BusinessLicense, TaxRegistration, TourismAuthorityLicense, InsuranceCertificate (4 docs) |
| IndependentGuide | `1` | GovernmentId, MotaLicense, TaxIdentificationNumber, InsuranceCertificate (4 docs) |
| HotelResort | `2` | BusinessLicense, ProofOfOwnership, TourismAuthorityLicense, HealthAndSafety, FireSafety (5 docs) |
| ActivityCenter | `3` | BusinessLicense, RelevantCertification, LiabilityInsurance, HealthAndSafety, FireSafety (5 docs) |
| Agency | `4` | BusinessLicense, TaxRegistration, TourismAuthorityLicense, InsuranceCertificate, AffiliatedGuidesList (5 docs) |

UI must show a per-type checklist that lights up green as each `DocumentType` is uploaded. Submit button stays disabled until all required types present.

#### 4.6.2 State machine — `/Provider/Status` page variants

6 distinct states, each needs its own banner + actions + visual treatment:

| Status (enum) | Banner | Allowed actions | Color band |
|---|---|---|---|
| `Draft` (0) | "Complete your application" | Continue editing → Upload docs → Submit | Neutral grey |
| `Pending` (1) | "Under review — expect a decision within 7 days" | Read-only; show SLA countdown | Blue |
| `MoreDocsNeeded` (2) | "Admin requested more documents" + admin Notes | Upload missing types (highlighted in checklist), Re-submit | Amber |
| `Approved` (3) | "You're a verified provider 🎉" | Go to Provider Dashboard; show doc-expiry warnings if any | Green |
| `Rejected` (4) | "Rejected — Reason: {RejectionReason}" + cooling countdown | Reapply (disabled until cooling ends); show attempt counter (e.g., "Attempt 2 of 3") | Red |
| `Suspended` (5) | "Suspended — Reason: {SuspensionReason}" | Read-only; "Contact support" link | Dark red |

**State-machine page must show:**
- Current status banner (top)
- Timeline of past status changes (SubmittedAt → ReviewedAt → cooling-ends → reapplied → …)
- Document grid with per-doc status (uploaded / expired / expiring-soon / replacement-needed)
- Reapplication counter when in Rejected state — `"Attempt {ReapplicationCount + 1} of 3"`
- Cooling countdown when in Rejected state — `"You can reapply in {N days, M hours}"` (`CoolingPeriodEndsAt`)

#### 4.6.3 Document upload widget (`/Provider/Documents`)

**Spec:**
- Drag-drop zone (`Dropzone.js` from template vendor list)
- File constraints (mirror backend exactly): **PDF / JPG / PNG only**, **10 MB max per file**, **max 10 docs total per application**
- Per-row metadata: `DocumentType` (dropdown), `FileName`, `FileSize`, `ExpiresAt` (optional)
- Endpoint: `POST /api/v1/provider/documents` for new, `PUT /api/v1/provider/documents/{id}` to replace
- **Replacement flow:** clicking "Replace" on an existing row preserves the `DocumentType` and only swaps the file
- **Duplicate prevention:** backend returns `DuplicateDocumentType` error if same type uploaded twice — UI should disable already-uploaded types in the dropdown
- **Expiry indicator:**
  - Green check if `ExpiresAt` > 30 days away
  - Amber warning if 14–30 days remain
  - Red error if < 14 days remain (grace period before auto-suspend)
- **Bulk upload:** for tours later, use `POST /api/v1/attachments/{entityType}/{entityId}/images` (max 20 files per call) — does **not** apply to provider docs (they need typed metadata)

#### 4.6.4 `/Provider/Apply` wizard flow

```
Step 1: Type selection (radio cards, one of 5)
   ↓
Step 2: Business info (BusinessName, ContactEmail, ContactPhone, Address, Description)
   ↓
Step 3: Type-specific data (TypeSpecificDataJson) — fields differ per provider type
   ↓
Step 4: Upload required docs (checklist from §4.6.1)
   ↓
Step 5: Review + Submit (calls POST /api/v1/provider/apply)
```

`POST /api/v1/provider/register` is called on Step 2 completion to create the `Draft` record. `POST /api/v1/provider/apply` transitions Draft → Pending after all docs present.

#### 4.6.5 `/Admin/Providers` queue view

- **Default filter:** `?status=Pending` (most urgent)
- **Filter chips:** Pending / MoreDocsNeeded / Approved / Rejected / Suspended (multi-select)
- **Type filter:** dropdown of 5 provider types
- **Sort:** by `SubmittedAt` ascending (oldest first — enforces 7-day SLA)
- **SLA indicator:** rows older than 5 days highlighted amber, older than 7 days red
- **Pagination:** `Page` + `PageSize` query params (default 1 / 20)
- **Row CTA:** "Review" → `/Admin/Providers/{id}` detail page

#### 4.6.6 `/Admin/Providers/{id}` detail view

Must display:
- Provider business card (Type, BusinessName, ContactEmail, Address)
- Application timeline (Registered → Submitted → Reviewed → Approved/Rejected → cooling/reapplied → …)
- Reapplication history (each prior attempt with its outcome)
- `ReapplicationCount` indicator ("This is attempt 2 of 3")
- Document grid (each doc clickable to view PDF/image inline; admin can mentally approve/reject — backend doesn't store per-doc decisions yet)
- Action buttons (per current status):
  - Pending → **Approve** / **Reject (with Reason)** / **Request more docs (with Notes + missing types)**
  - Approved → **Suspend (with Reason)** — opens cascade preview modal (§4.6.7)
  - Suspended → **Reinstate** — confirmation modal
  - Rejected → read-only (cooling period in effect)

#### 4.6.7 Suspension cascade preview modal

When admin clicks **Suspend** on an `Approved` provider, the confirmation modal must show:

```
⚠ Suspending {BusinessName} will cascade to:
   • {N} active tours → auto-suspended
   • {M} active bookings → auto-cancelled (with refund processing)
   • {K} pending/ready payouts → put on hold

   Reason (required, shown to provider):
   [_________________________________]

   [ Cancel ]   [ Confirm Suspension ]
```

Numbers come from a separate GET call (or the suspend endpoint can return a dry-run shape — backend addition needed). This previews the cascading impact of:
- `ProviderSuspendedSuspendToursHandler` (ContentTours)
- `ProviderSuspendedCancelBookingsHandler` (Booking)
- `ProviderSuspendedHoldPayoutsHandler` (Finance)

#### 4.6.8 Reinstate confirmation modal

When admin clicks **Reinstate** on a `Suspended` provider:

```
✓ Reinstating {BusinessName} will:
   • Restore {N} tours → back to active state
   • Release payouts on hold → re-enter payout queue
   • Notify provider via in-app + email

   Note: previously cancelled bookings are NOT restored.

   [ Cancel ]   [ Confirm Reinstatement ]
```

#### 4.6.9 Provider notifications (Messaging integration)

5 notification types delivered via `Messaging.Infrastructure` → bell dropdown + email (opt-in):

| Trigger | NotificationType | Channel | Priority |
|---|---|---|---|
| `ProviderApprovedIntegrationEvent` | Business | InApp + Email | High |
| `ProviderRejectedIntegrationEvent` | Business | InApp + Email | High |
| `ProviderSuspendedIntegrationEvent` | Business | InApp + Email | High |
| `ProviderReinstatedIntegrationEvent` | Business | InApp + Email | High |
| `ProviderStatusChangedIntegrationEvent` (NewStatus=MoreDocsNeeded) | Business | InApp + Email | High |

Bell dropdown link clicks navigate to `/Provider/Status`.

#### 4.6.10 Cross-module UI impact

When a provider is suspended/reinstated, **other UIs must surface the cascade**:

- **Customer-facing tour listings** — must hide tours where `SuspendedAt != null` (or show "Currently unavailable")
- **`/Account/Bookings`** — cancelled bookings show `CancellationSource = Admin, ProviderInitiated = false, ForceMajeureOverride = true` → display "Cancelled by platform — full refund issued"
- **`/Provider/Earnings`** — payouts with `Status = Hold` need an explicit indicator with reason
- **`/Provider/Listings`** — suspended tours show with a `Suspended` badge + the reason

#### 4.6.7+ Endpoint coverage matrix

| Wave-2 endpoint | UI route | Status |
|---|---|---|
| `GET /api/v1/admin/providers` | `/Admin/Providers` | ✅ mapped |
| `GET /api/v1/provider/status` | `/Provider/Status` | ✅ mapped |
| `POST /api/v1/provider/register` | `/Provider/Apply` (Step 2) | ✅ mapped |
| `POST /api/v1/provider/apply` | `/Provider/Apply` (Step 5) | ✅ mapped |
| `POST /api/v1/provider/documents` | `/Provider/Documents` upload | ✅ mapped |
| `PUT /api/v1/provider/documents/{id}` | `/Provider/Documents` replace | ✅ mapped |
| `POST /admin/providers/{id}/approve` | `/Admin/Providers/{id}/Approve` | ✅ mapped |
| `POST /admin/providers/{id}/reject` | `/Admin/Providers/{id}/Reject` | ✅ mapped |
| `POST /admin/providers/{id}/request-docs` | `/Admin/Providers/{id}/RequestDocs` | ✅ mapped |
| `POST /admin/providers/{id}/suspend` | `/Admin/Providers/{id}/Suspend` + cascade modal | ✅ mapped |
| `POST /admin/providers/{id}/reinstate` | `/Admin/Providers/{id}/Reinstate` | ✅ mapped |
| `POST /api/v1/attachments/{entityType}/{entityId}/images` (bulk) | `/Provider/Listings/{id}/Images` (tour images) | ✅ mapped to tour image upload, not provider docs |

**Backend gap to fill:** `GET /api/v1/admin/providers/{id}` (single-get) — needed for `/Admin/Providers/{id}` detail page. Currently the UI must filter the paginated list client-side. Add this in a Wave-2.1 patch.

---

### 4.7 Wave-1 Foundations — detailed UI spec

> **Backend status:** ~95% complete. 7 small backend gaps (OAuth split into 3, role claims listing, user role/status fixes — see `Agents/Waves/Wave-1.md`).
> **Skills:** `ui-ux-pro-max` (default) · `impeccable` (Auth pages must feel premium first-impression).

#### 4.7.1 Auth screens

| View | Backend | Notes |
|---|---|---|
| `/Auth/Login` | `POST /api/v1/auth/sessions` | Email + password + remember-me toggle; OAuth button row at bottom (Apple / Facebook / Google) |
| `/Auth/Register` | `POST /api/v1/auth/register` | Multi-field form (email, password, name); password-strength meter (8+ chars + upper + lower + digit + special) |
| `/Auth/VerifyEmail` | `POST /api/v1/auth/verify-email` | 6-digit OTP — 6 input boxes that auto-advance + accept paste-to-fill; resend-link cooldown (5/hour) |
| `/Auth/VerifyOtp` | `POST /api/v1/auth/verify-otp` | Same 6-box pattern; show 10-min TTL countdown |
| `/Auth/ForgotPassword` | `POST /api/v1/auth/forgot-password` | Single email field |
| `/Auth/ResetPassword` | `POST /api/v1/auth/reset-password` | OTP + new password + confirm; strength meter |
| `/Auth/External/{provider}` | 3 endpoints (`/external/apple` / `/facebook` / `/google`) ⚠️ Wave-1 backend gap | Provider-branded buttons; redirects to provider OAuth then back |

**Critical UX:**
- OTP screen must show **attempts remaining** (max 5) and **resend cooldown** (5/hour); failed attempts trigger lockout banner.
- Password meter must reflect backend rules exactly — fail closed on any missing class.
- "Login from new device" notification (SecurityAlert) surfaces in the bell dropdown post-Wave-6.

#### 4.7.2 Admin catalog CRUD screens

| View | Backend | UI pattern |
|---|---|---|
| `/Admin/Categories` | `GET /api/v1/categories` (tree) | Tree view, max 3 levels enforced visually (disable "Add child" at level 3); drag-to-reorder; bilingual edit (AR/EN side-by-side) |
| `/Admin/Languages` | `GET/POST/PUT/DELETE /api/v1/admin/languages` | Flat list + active toggle |
| `/Admin/Tags` | `GET/POST/PUT/DELETE /api/v1/admin/tags` | Flat searchable list; usage-count column |
| `/Admin/Specializations` | `GET/POST/PUT/DELETE /api/v1/admin/specializations` | Same pattern as tags |
| `/Admin/Roles` | `GET /api/v1/admin/roles` | List with reserved-role badge (Admin, User, Provider, SuperAdmin uneditable); claims viewer modal calls `GET /admin/roles/{id}/claims` (⚠️ Wave-1 backend gap) |
| `/Admin/Users` | `GET /api/v1/admin/users` | Filter by email (partial), role, status, registration date; status toggle PUT (⚠️ Wave-1 backend gap — currently split into activate/deactivate); pagination max 50 |

**Critical UX:**
- Reserved roles must be visually distinct (greyed-out delete, lock icon) — can never delete.
- User status change must show side effects banner: "Deactivating will revoke all sessions for this user."
- Self-deactivation prevention: hide/disable button when target is current admin.

#### 4.7.3 Public Profile screen

| View | Backend | Notes |
|---|---|---|
| `/Account/Profile` | `GET/PUT /api/v1/accounts/profile` + avatar | Already built ✅ — Wave-1 just needs to verify the `/profile` alias mount per PDF1 |

---

### 4.8 Wave-3 Public Catalog — detailed UI spec

> **Backend status:** Tours/Places/Businesses fully built; Tour Guide profile endpoints (5) implemented but tests + runtime smoke pending.
> **Skills:** `huashu-design` (tour detail hero — primary conversion surface) · `design-taste-frontend` (card hierarchy + gallery rhythm) · `impeccable` (polish + accessibility).

#### 4.8.1 Tour discovery surfaces

| View | Backend | UI focus |
|---|---|---|
| `/Tours` | `GET /api/v1/tours/search` (faceted) | List + grid toggle, filter sidebar (category, price range JOD/USD/EUR, language, duration, child-friendly toggle, dates), sort (relevance / price / rating / popularity), pagination |
| `/Tours/{slug}` | `GET /api/v1/tours/{slug}` | **Hero gallery** (full-bleed, swipeable), pricing tiers card, schedule picker, waypoints map, FAQ accordion, reviews summary, related tours, sticky "Book" CTA |
| `/Tours/Search?q=...` | `GET /api/v1/tours/search?q=...` | Debounced autocomplete in header, faceted result page, "no results" with suggestions |
| `/Tours/Featured` | `GET /api/v1/tours/featured` | Editorial layout |
| `/Tours/Popular` / `/Trending` | `GET /api/v1/popular/tours` / `/trending/tours` | Carousel + grid; trending = delta indicator (↑ vs prev 7d) |

**Critical UX for `/Tours/{slug}`:**
- Hero gallery must be hi-fi (use `huashu-design` for variant exploration; default templates feel generic).
- Pricing tiers card must show all enabled tier types (Adult/Child/Infant/Senior/Group/Private) with discounted sale-price strikethrough when active.
- Child-friendly badge shows MinAge–MaxAge range when present.
- Meeting point + waypoint map (Mapbox GL, Wave-6 dependency).
- "Book now" CTA opens Wave-5 booking flow (`/Tours/{slug}/Book`).

#### 4.8.2 Business surfaces

| View | Backend | UI focus |
|---|---|---|
| `/Businesses` | `GET /api/v1/businesses/search` | Grid + map toggle, filter by category + place |
| `/Businesses/{slug}` | `GET /api/v1/businesses/{slug}` | Hero + business hours (7-day grid with split-shift support), related tours, reviews |
| `/Businesses/Map` | `GET /api/v1/businesses/map` | Mapbox viewport (Wave-6) |

**Critical UX:** Business hours grid must render 24-hour spans (open all day), split shifts (multiple ranges per day), and closed days distinctly.

#### 4.8.3 Tour Guide profiles

| View | Backend | UI focus |
|---|---|---|
| `/Guides/{id}` | `GET /api/v1/guides/{id}` ✅ | Public profile: bio, years of experience, HasFirstAid badge, MoTA license, language list with proficiency (Native/Fluent/Conversational/Basic), specializations, tours led, average rating |
| `/Provider/Guide/Edit` | `PUT /api/v1/guides/{id}` + sub-resources ✅ | Owner-only (UserId match enforced); bio (max 2000), years (0-80), license, language manager (add/remove with proficiency picker — cannot remove last language), specialization manager |

**Critical UX:**
- Owner check must hide the edit page entirely for non-owners (don't show a disabled state).
- Language remove button disabled when only one language remains; tooltip "At least one language required."
- Proficiency picker must validate against backend enum.

#### 4.8.4 Provider listing management

| View | Backend | UI focus |
|---|---|---|
| `/Provider/Listings` | `GET /api/v1/tours?providerId=me` | Table: title, status (Draft/Pending/Approved/Suspended), bookings count, avg rating, last updated; per-row CTAs (edit, view, archive) |
| `/Provider/Listings/{id}/Edit` | Multiple endpoints — multi-tab editor | Tabs: Basic, Schedules, Pricing Tiers, Waypoints, Images, FAQ, Children-Info; per-tab save (don't lose work on tab switch); Wave-4 enrichment endpoints live here |
| `/Provider/Listings/{id}/Submit` | `POST /api/v1/tours/{id}/submit-for-review` | Pre-submit checklist: ≥1 image, ≥1 schedule, ≥1 pricing tier, description ≥100 chars, meeting point lat/lng set; disabled button until all checks pass |

**Critical UX:**
- Critical-field edit warning: "Changing BasePrice / Duration / MaxGroupSize / Coordinates will revert this tour to Pending review."
- Non-critical edits (description, images) save immediately without status revert — surface this distinction.
- Max 50 active tours per provider — show counter "X / 50 active" on listings page.

---

### 4.9 Wave-4 Blog + Tour Enrichment + Admin SEO — detailed UI spec

> **Backend status:** ✅ 100% complete (42+ endpoints, Sitemap regen BG service running).
> **Skills:** `ui-ux-pro-max` (Blog list + admin) · `huashu-design` (Blog article hero + typography variant exploration) · `impeccable` (long-form reading polish) · `design-taste-frontend` (article spacing / column width / font-size scale).

#### 4.9.1 Blog public surfaces

| View | Backend | UI focus |
|---|---|---|
| `/Blog` | `GET /api/v1/blogs?status=Published` | Featured post hero, recent grid, category filter, search |
| `/Blog/{slug}` | `GET /api/v1/blogs/{slug}` | Article: large hero image, title, read-time badge (auto-calc: words / 200), author + date, body (rich-text), linked tours (max 10), linked place (max 1), share buttons, comments section |
| `/Blog/{slug}` (archived) | same | "This article is outdated" banner at top; still indexable for SEO |
| `/Blog/Tag/{tag}` / `/Blog/Category/{cat}` | filter endpoints | Same grid layout, filtered |

**Critical UX for article page:**
- Reading-progress bar (sticky top).
- Sticky TOC (auto-generated from H2/H3) on desktop; collapsed accordion on mobile.
- Linked tours rendered as inline cards within the article body (not just appended at bottom).
- View count debounced per user per 30-min window — UI just renders the number from API.

#### 4.9.2 Blog comments

| Feature | Backend | UI pattern |
|---|---|---|
| Comment list | `GET /api/v1/blogs/{id}/comments` | Nested max 2 levels; "show N replies" collapse for level-2 |
| Submit comment | `POST /api/v1/blogs/{id}/comments` | Login wall for anons; profanity filter warns BEFORE submit; 1000-char counter |
| Reactions | `POST /api/v1/blog-comments/{id}/reactions` | 3 buttons: Like / Helpful / Insightful — one per user (change replaces); count display |
| Edit / delete | `PUT/DELETE` | Owner only; soft-delete shows "[deleted]" placeholder if parent of replies |

#### 4.9.3 Tour enrichment views (inside `/Tours/{slug}`)

These are panels on the tour detail page powered by Wave-4 endpoints:

| Panel | Backend | UI |
|---|---|---|
| Pricing tiers | `GET /api/v1/tours/{id}/pricing-tiers` | Cards per tier type (Adult/Child/Infant/Senior/Group/Private); active discount → strikethrough + sale price |
| Schedules | `GET /api/v1/tours/{id}/schedules` | Calendar grid (90 days); per-date capacity badge; blackout dates greyed |
| Waypoints | `GET /api/v1/tours/{id}/waypoints` | Ordered list + map markers (Mapbox in Wave-6) |
| FAQ | `GET /api/v1/tours/{id}/faqs` (via FAQ-by-entity) | Accordion |
| Children-info | embedded in tour DTO | Badge: "Family-friendly · ages X–Y" when `IsChildFriendly=true` |

#### 4.9.4 Admin SEO surfaces

| View | Backend | UI focus |
|---|---|---|
| `/Admin/Seo/Metadata` | `GET/POST/PUT /api/v1/metadata` | Per-entity editor: title (60-char counter), description (160-char counter), og:title, og:description, og:image picker, canonical URL, hreflang AR↔EN; live SERP preview card |
| `/Admin/Seo/Redirects` | `GET/POST/DELETE /api/v1/redirects` | List + create (oldUrl, newUrl, 301/302); chain visualization (highlight if A→B→C exists — system auto-flattens to A→C); circular detection warning |
| `/Admin/Seo/Sitemap` | `GET /api/v1/sitemap.xml` + on-demand regen | Last-gen timestamp; per-EntityType counts; force-regen button |

**Critical UX:** OG image picker must fall back to YallaJo default thumbnail if none selected. Canonical URL must auto-suggest from slug.

#### 4.9.5 Translations admin

| View | Backend | UI |
|---|---|---|
| `/Admin/Translations` | `GET /api/v1/translations` (filter by entity type / locale / status) | Table with status: Translated / Pending / Untranslated; approve action |
| Inline AR/EN editor | per-entity translation endpoints | Side-by-side textareas; auto-translate button (uses external API quota — show remaining quota indicator) |

---

### 4.10 Wave-5 Booking & Payment Engine — detailed UI spec

> **Backend status:** Booking + payments + invoices + payouts + commissions built ✅. **15 endpoints still missing**: Availability Slots (6), Refund Policies (3), Join Requests (3), Provider booking reads (3). Plan UI around these gaps — block the dependent screens until backend ships.
> **Skills:** `huashu-design` (booking flow needs A+ design — primary revenue path) · `impeccable` (payment + error states unforgiving) · `ui-ux-pro-max` (forms / lists) · `design-taste-frontend` (calendar / availability grid).

#### 4.10.1 Customer booking flow

| Step | View | Backend | Notes |
|---|---|---|---|
| 1 | `/Tours/{slug}/Book` — Step A | `GET /api/v1/availability/{tourId}` ⚠️ Wave-5 gap | Date picker shows per-day capacity badges; sold-out dates disabled |
| 2 | same — Step B | `GET /api/v1/availability/{tourId}/{date}` ⚠️ Wave-5 gap | Time slot picker for selected date |
| 3 | same — Step C | participant counter per tier (Adult / Child / Infant / Senior); price recalc live | Validation: min 2-hour lead time, total ≤ slot.RemainingCapacity |
| 4 | `/Bookings/{id}/Pay` | `POST /api/v1/payments/initiate` | Payment gateway redirect or inline card form (no raw card data per PCI); 10-min lock TTL countdown |
| 5 | `/Bookings/{id}/Confirmation` | webhook → email | Confirmation code `YJ-YYYYMM-XXXX`, add-to-calendar (.ics), share buttons |

**Critical UX:**
- Refund policy snapshot must be displayed BEFORE payment (full refund hours, partial window, partial percent — shown via tour's refund policy endpoint ⚠️ Wave-5 gap).
- 10-min lock countdown visible during payment; on expiry, friendly "your slot was released" screen with re-book CTA.
- Currency selector (JOD primary, USD, EUR) — locks at booking creation.
- Max 3 concurrent pending unpaid bookings enforced backend — UI must show "you have 3 pending — complete or cancel one before booking again."

#### 4.10.2 Customer bookings management

| View | Backend | UI focus |
|---|---|---|
| `/Account/Bookings` | `GET /api/v1/bookings/my` | Tabs: Upcoming / Past / Cancelled; cards with confirmation code, date, status badge |
| `/Account/Bookings/{id}` | `GET /api/v1/bookings/{id}` | Full detail: tour summary, participants, payment status, refund snapshot, cancellation deadline countdown |
| `/Account/Bookings/{id}/Cancel` | `POST /api/v1/bookings/{id}/cancel` | Confirmation modal showing **calculated refund amount** using stored snapshot (not current policy); "100% refund if cancelled now" / "Partial 50% / 0% — past partial window" |

**Critical UX:**
- Cancellation modal must show the exact refund amount BEFORE the user confirms — calculated from `RefundPolicySnapshotJson` on the booking, not from the tour's current policy.
- Provider-initiated cancellation banner: "Cancelled by provider — full refund issued automatically."
- Platform-initiated cancellation (force majeure / provider suspended): clear messaging that refund is in progress.

#### 4.10.3 Provider booking management

| View | Backend | UI |
|---|---|---|
| `/Provider/Bookings/Pending` | `GET /api/v1/bookings/provider/pending` ⚠️ Wave-5 gap | Non-instant bookings awaiting provider accept; **24h SLA countdown** per row (auto-confirm after); accept / reject CTAs |
| `/Provider/Bookings/Upcoming` | `GET /api/v1/bookings/provider/upcoming` ⚠️ Wave-5 gap | Confirmed bookings with future tour date |
| `/Provider/Bookings/History` | `GET /api/v1/bookings/provider/history` ⚠️ Wave-5 gap | Completed + Cancelled (paginated) |
| `/Provider/Bookings/{id}` | shared | Detail + accept/reject for pending; customer contact info; refund policy snapshot |

#### 4.10.4 Availability management (provider)

| View | Backend | UI |
|---|---|---|
| `/Provider/Tours/{id}/Availability` | All 6 slot endpoints ⚠️ Wave-5 gap | Calendar grid (90 days); per-slot capacity / booked / locked; drag-to-edit |
| Bulk-create modal | `POST /api/v1/availability/slots/bulk` ⚠️ | Pattern: once / daily / weekly / custom; horizon: up to 90 days; return `{created, skipped}` |
| Slot edit modal | `PUT /api/v1/availability/slots/{id}` ⚠️ | Capacity, price override, blackout toggle; **must block capacity reduction below BookedCount** (UI shows hard minimum) |
| Slot delete | `DELETE /api/v1/availability/slots/{id}` ⚠️ | Disabled if BookedCount > 0 OR LockedCount > 0 |

#### 4.10.5 Refund policies

| View | Backend | UI |
|---|---|---|
| `/Provider/Tours/{id}/RefundPolicy` | `GET/POST/PUT /api/v1/refund-policies` ⚠️ Wave-5 gap | Form: FullRefundHours (e.g., 48), PartialRefundHours (e.g., 24), PartialRefundPercent (50-100); admin enforces ≥50% when 48h+ |
| Customer display | embedded in tour detail | "Free cancellation up to 48h before / 50% refund 24-48h before / no refund within 24h" |

**Critical UX:** Save banner: "This change applies to NEW bookings only. Existing bookings keep their stored policy snapshot." (per guide §5.4).

#### 4.10.6 Join requests

| View | Backend | UI |
|---|---|---|
| `/Tours/{slug}/JoinRequest` | `POST /api/v1/bookings/join-request` ⚠️ Wave-5 gap | Only visible if `Tour.AllowsJoinRequests=true` AND existing confirmed group booking has remaining capacity; participant count selector |
| `/Provider/JoinRequests` | (filter on bookings) | Pending tab: approve / reject (with reason); auto-charge on approve via payment gateway |

#### 4.10.7 Earnings (provider)

| View | Backend | UI |
|---|---|---|
| `/Provider/Earnings` | `GET /api/v1/payouts/my` + `/invoices/my` | Current balance, escrow countdown (7-day after each completed tour), pending payouts, **hold reason if provider suspended** (link to Wave-2 reinstate flow), payout history |
| `/Provider/Earnings/Bank` | `GET/POST/PUT /api/v1/provider-bank-accounts/my` | Bank verification flow; payouts gated until verified |
| Payout-on-hold banner | when `Payout.Status=Hold` | Explicit message: "Payouts on hold because your provider account is suspended" with link to `/Provider/Status` |

#### 4.10.8 Admin bookings / payments / refunds / commissions

| View | Backend | UI |
|---|---|---|
| `/Admin/Bookings` | `GET /api/v1/bookings/admin/all` | Full table with filters: status, date range, provider, customer |
| `/Admin/Payments` | `GET /api/v1/payments/admin/all` | Payment status + retry actions |
| `/Admin/Refunds` | refund endpoints | Approve / deny; force-majeure override (100% refund regardless of policy snapshot) |
| `/Admin/Commissions` | commission CRUD | Tier rates (Free 15% / Basic 10% / Premium 7% / Enterprise custom); per-provider overrides |

---

### 4.11 Wave-6 Discovery, Search, Map, Notifications, Support — detailed UI spec

> **Backend status:** ✅ 100% complete (80+ endpoints + 11 BG services + SignalR Hub).
> **Skills:** `ui-ux-pro-max` (notifications, search, reviews) · `huashu-design` (map page + analytics dashboards) · `design-taste-frontend` (analytics density / chart hierarchy) · `impeccable` (SignalR notification bell + toast UX).

#### 4.11.1 Reviews

| Surface | Backend | UI |
|---|---|---|
| Reviews block on tour/place/business detail | `GET /api/v1/reviews?entityType=Tour&entityId=...` | Filter: Verified-only / by rating / by recency; per-review: rating (5-star with 0.5 increments), text, up to 3 photos, "Verified" badge if booking-backed, provider reply (threaded) |
| Submit review modal | `POST /api/v1/reviews` | 30-day window from tour completion (gated); rating slider, 20-2000 char text, photo upload (max 3, 5MB each, JPG/PNG/WebP); profanity filter warning |
| `/Account/Reviews` | `GET /api/v1/reviews?reviewerUserId=me` | My reviews with **48h edit window indicator** (countdown badge) — disabled after window |
| Provider reply box | `POST /api/v1/reviews/{id}/replies` | Inline expand on owned tours; profanity filter warning |

**Critical UX:**
- Min 3 reviews before aggregate rating displays — show "Not yet rated" until threshold.
- Bayesian-smoothed rating display: tooltip explains the smoothing for full transparency on edge cases.
- Provider replies do NOT affect aggregate rating — visually subordinate to the original review.

#### 4.11.2 Wishlist

| Surface | Backend | UI |
|---|---|---|
| Heart toggle (every card) | `POST /api/v1/wishlist/toggle` | Login wall for anons; optimistic toggle + revert on error |
| `/Account/Wishlist` | `GET /api/v1/wishlist/my` | Tabs by EntityType: Tour / Place / Business; bulk-delete; "X / 500" counter |
| Discount-on-wishlist notification | passive | Bell-dropdown item: "Tour {name} now 20% off — only 3/day per user" |

#### 4.11.3 Reports & Moderation

| Surface | Backend | UI |
|---|---|---|
| Report button (every UGC) | `POST /api/v1/reports` | Modal with reason selector (spam / inappropriate / harassment / misleading / other); free-text detail; "max 10/day" client-side hint |
| `/Admin/Moderation` | `GET /api/v1/admin/moderation-queue` | Auto-hidden items (5+ reports) prioritized; per-item: Restore / Permanently delete / Warn user / Ban user; repeat-offender flag (3+ hidden in 90d) |

#### 4.11.4 Analytics dashboards

| View | Backend | UI |
|---|---|---|
| `/Admin/Analytics` | interactions / popular / trending endpoints | Charts: top tours by views/bookings/conversions; trending delta arrows |
| `/Admin/Analytics/Audit` | `GET /api/v1/admin/audit-logs` | Table with filters (action, user, entity type, date range); per-row JSON detail expand |
| `/Provider/Analytics` | provider analytics endpoints | Own tours only: views, conversions, top tours, revenue trends |

**Critical UX:** Use `design-taste-frontend` to enforce **chart hierarchy** — dashboards drift into "everything-same-size" pattern without strict primary/secondary metric distinction.

#### 4.11.5 Map

| View | Backend | UI |
|---|---|---|
| `/Map` | `GET /api/v1/map/viewport` | Mapbox GL JS; default Jordan center (31.95, 35.93) zoom 8; clustering 0–12, individual pins 13+; pin colors by EntityType (Tour / Place / Business); max 5000 pins per viewport load |
| Tour/Place detail embed | nearby endpoint | Inline mini-map with meeting point + waypoints (Wave-4 enrichment data) |

**Critical UX:**
- Pin payload is lightweight (id, name, lat/lng, primaryImage, rating, tourCount) — full detail loaded on pin click.
- Coordinate validation: never render pins outside -90/90, -180/180.

#### 4.11.6 Search

| Surface | Backend | UI |
|---|---|---|
| Header search bar | `GET /api/v1/search/autocomplete` | Debounced (250ms), top 5 suggestions |
| `/Search?q=...` | `GET /api/v1/search?q=...` | Faceted: per-category counts, price range histogram, dates; "no results" with did-you-mean suggestions |

#### 4.11.7 Notifications (SignalR)

| Surface | Backend | UI |
|---|---|---|
| Bell icon (header) | `NotificationHub` at `/hubs/notifications` + `GET /api/v1/notifications/unread-count` | SignalR live updates; badge count; dropdown shows last 10 |
| `/Account/Notifications` | `GET /api/v1/notifications` | Full list with cursor pagination; mark-read; bulk-delete read |
| `/Account/Notifications/Preferences` | `GET/PUT /api/v1/notifications/preferences` | Matrix: event type × channel (InApp / Email / Push); **critical types locked** (OTP, PaymentCompleted, PaymentFailed, RefundInitiated, RefundCompleted, OtpDelivery, SecurityAlert, LoginFromNewDevice, EmailVerification, PasswordChanged) — show lock icon + tooltip |
| Toast notifications | SignalR push | For in-page events; auto-dismiss; click → relevant page |

**Critical UX:**
- SignalR group join: `user:{userId}` always; `provider:{providerId}` for providers; `admin` for admins.
- Bell badge resets on dropdown open (mark-as-seen ≠ mark-as-read).
- Email unsubscribe link in every digest — per-event-type granularity.

#### 4.11.8 Support tickets

| View | Backend | UI |
|---|---|---|
| `/Support` | `POST /api/v1/support/tickets` + `GET /my` | Create form: category (Booking Issue / Payment Problem / Provider Complaint / Account Help / Bug Report / Other) → auto-assigns priority (Payment=High, Booking=Medium, Other=Low); subject 10-200, message 20-5000; my tickets list |
| `/Support/{id}` | message endpoints | Thread view; attach files; mark-resolved (customer); close (admin) |
| `/Admin/Support` | `GET /api/v1/admin/support/tickets` | Queue with priority badges + **SLA timers** (High=4h, Medium=12h, Low=24h); auto-assigned admin badge (round-robin); bulk-reassign |

---

### 4.12 Wave-7 Creator Identity + Blog Authoring — detailed UI spec

> **Backend status:** spec written (`Agents/Waves/Wave-7.md`) — not yet implemented.
> **Skill plan:** Primary `ui-ux-pro-max` (state machine + admin queue rigor) + `huashu-design` (public creator profile hero); Polish `impeccable` (community feeds, invitation composer).
> **Hosting:** All routes live inside ContentBlogs module (per locked design decision m2399).

#### 4.12.1 Creator application wizard `/Creators/Apply`

Mirrors Wave-2's provider apply wizard, adapted for individual creators:

1. **Identity** — DisplayName (3-50 chars, live-uniqueness check), Bio (markdown, 50-500 chars, live char counter)
2. **Niches + Tags** — Niche multi-picker (3-5 from admin-curated active list with badge counts; required); Free-tag chips (0-10, autocomplete from existing Tag entity, type-to-create allowed)
3. **Languages + Regions** — Languages spoken (1+ from Wave-1 Languages, dropdown with flag icons); Preferred regions (0+ from Wave-3 Places, search-as-you-type)
4. **Portfolio + Samples** — Portfolio URL list (1-5, drag-reorder, format validation: must be http(s)); Sample work URL list (exactly 3, hint: "blog post, social profile, or video link")
5. **Social** — At-least-one of {Instagram, TikTok, YouTube, Twitter, Personal Site}; format validation per platform
6. **Review & Submit** — Read-only preview of all fields + checkbox "I confirm content originality and accept community guidelines"

Each step: progress dots top, Back/Next bottom, Save Draft persists locally + server-side. Total ~3 min on desktop.

#### 4.12.2 Creator status page `/Creators/Status`

State-machine page mirroring Wave-2 `/Provider/Status` but with creator-specific copy. 5 status variants:

| Status | Hero | Body | Actions |
|---|---|---|---|
| **Draft** | "Almost there!" + progress bar | Missing-fields checklist | "Continue Application" → wizard step that's incomplete |
| **Pending** | "Application under review" + SLA banner ("7-day SLA · Day X of 7") | Submitted-fields summary | "Edit" (disabled), "Withdraw application" (confirmation modal) |
| **MoreInfoNeeded** | "Action required" red banner | Admin's request notes (markdown) | "Provide more info" → wizard returns to relevant step |
| **Rejected** | "Application not approved" + cooling countdown timer ("Re-apply in X days") | Admin's rejection reason | Re-apply button disabled until cooling expires; "Read guidelines" link |
| **Approved** | "Welcome, Creator!" celebration + confetti animation | Profile slug + bio preview | "View public profile" → `/Creators/{slug}`, "Edit profile" → `/Creators/Me`, "Write your first article" → `/Creator/Articles/Compose` |

Show **ReapplicationCount: X/3** badge under the hero on Rejected variant. At 3/3, replace re-apply CTA with "Contact support to discuss".

#### 4.12.3 Invitation redeem `/Creators/Redeem-Invitation?token={token}`

Email-token landing page. Validates token server-side, then renders pre-filled wizard with admin's suggested DisplayName + Message banner ("You've been personally invited by [admin name]"). 14-day expiry visible. Token-exchange happens on Submit, not on page load (so reload doesn't burn the token).

In-app variant: bell notification opens `/Creators/Apply?invitationId={id}` directly — same wizard but pre-filled from invitation.

#### 4.12.4 Creator profile editor `/Creators/Me`

Two-column layout. Left: cover image (1600×400 hero, drag-to-upload, IsPrimary checkbox not applicable since 1 only) + avatar (250×250, circular crop). Right: editable fields (DisplayName read-only post-approval, Bio markdown editor with live preview, social handles, niches/tags multi-pickers, languages, regions).

Bottom: read-only stats card showing `ApprovedArticleCount, FollowerCount, TotalViewCount, ReportRate` with badges and tier badge ("Tier 0 — pre-moderated" / "Tier 1 — post-moderated" / "Tier 2 — featured-eligible"). Wave-8 will add tier-up progress indicators here.

#### 4.12.5 Public creator profile `/Creators/{slug}`

Brand-level page. Hero = cover image + circular avatar overlay + DisplayName + tier badge + niche chips + follower count. Sub-nav: Articles / Photos (Wave-8) / Videos (Wave-8) / Itineraries (Wave-8) / About.

Follow button = primary CTA; auto-hides when viewing own profile or when already following (replaced by "Following · Unfollow" pill). About tab shows full bio, languages spoken (flag chips), preferred regions (chip list), social handle links.

Anonymous viewer sees "Follow" button → on click triggers login modal. Authenticated viewer follows immediately + button updates optimistically.

If creator is `IsProvider`: show a small "Verified Provider" badge next to DisplayName with tooltip "This creator also operates listings on YallaJo".

#### 4.12.6 Article composer `/Creator/Articles/Compose` (and `/Edit/{id}`)

Tiptap or SimpleMDE-backed Markdown editor. Two-pane layout:
- **Left pane:** title input (10-200 chars), excerpt textarea (100-500 chars), Markdown body editor with toolbar (bold/italic/headings/links/lists/quote/code/image-insert), word count + reading time estimate
- **Right pane:** language picker (defaults to creator's primary), niches selector (1-3), free tags chips, cover image uploader, **tag entities** picker (search Tour/Place/Business from Wave-3 catalog — multi-select), **disclosure** section ("Are you promoting any of your own provider listings? If so, mark sponsored and disclose targets" — IsSponsored toggle + disclosure target list)

Save-draft auto-saves every 30s. "Submit for review" CTA disabled until required fields valid; on submit, modal explains "Admins review within 24-48h; you'll be notified."

#### 4.12.7 Followed-creators feed `/Me/Feed`

Vertical timeline. Each card: creator avatar + DisplayName + tier badge + posted-time, article cover image, title, excerpt, niche chips, reaction count + comment count + read time. Click → article page (existing Wave-4 reading template).

Empty state: "Follow creators to build your feed" with 3 suggested creators (server-curated by niche match — out of scope for Wave-7 v1; fallback: "Browse creators" CTA → `/Creators`).

Infinite scroll, page size 20, cached 2 min.

#### 4.12.8 Admin creators queue `/Admin/Creators`

Standard admin table with filters: Status (Pending / MoreInfoNeeded / Rejected / Approved), Source (PublicApplication / AdminInvitation), EligibleForTier1 (yes/no — Wave-8 flag).

Each row: avatar thumbnail, DisplayName, Source badge (📧 Invited / 🌐 Public), niche chips, submitted-at (relative time), reapplication count badge if > 0, status pill, action buttons (Approve / Reject / Request More Info — open inline modal).

Top-right primary CTA: **"+ Send Invitation"** → opens composer modal (§4.12.10).

#### 4.12.9 Admin creator detail `/Admin/Creators/{id}`

Full review interface:
- **Top:** application metadata (Source, submitted-at, reviewer attribution, reapplication count, cooling expires-at if rejected)
- **Identity panel:** DisplayName, Bio (rendered markdown), suggested slug (with override input)
- **Portfolio panel:** all 5 URLs with favicon + visit-link buttons, sample work URLs separately
- **Niches + tags:** chip lists
- **Social handles:** clickable platform icons → external profile
- **Action footer:**
  - **Approve** (primary): opens modal with optional slug override + confirmation
  - **Request More Info** (secondary): textarea for admin notes (50-1000 chars)
  - **Reject** (danger): rejection reason textarea (50-1000) + warning "User will see 14-day cooling period"

Show "Linked to Provider Application" badge if `IsProvider=true` (from Wave-7 cross-link handler) → click to open `/Admin/Providers/{providerApplicationId}` in new tab.

#### 4.12.10 Invitation composer `/Admin/Creators/Invitations/New` (modal or full page)

Two-toggle composer:
- **Recipient type:** Email (default) | Existing User
  - Email: email field + auto-suggestion if matches a known user
  - Existing User: user search picker (typeahead by email/name)
- **Suggested DisplayName** (admin pre-fills; recipient can override at redeem time)
- **Personal Message** (optional, 0-500 chars, markdown supported)
- **Send button** → server emits `creators.invitation.sent.v1`; for Email kind sends SMTP via Messaging handler; for InApp kind delivers bell notification

After send: show success toast + link to `/Admin/Creators/Invitations` (existing invitations list with Active/Redeemed/Expired/Revoked status pills + Revoke action for Active ones).

#### 4.12.11 Endpoint coverage matrix

| Route | Backend endpoint | Permission |
|---|---|---|
| `GET /Creators/Apply` (wizard) | `POST /api/v1/creators/apply` (submit) | `Creator.Application.Submit` |
| `GET /Creators/Redeem-Invitation` | `POST /api/v1/creators/redeem-invitation` | authenticated + token |
| `GET /Creators/Status` | `GET /api/v1/creators/status` | `Creator.Application.Read` |
| `GET /Creators/Me` | `GET /api/v1/creators/me` + `PUT /api/v1/creators/me` | `Creator.Profile.Read` / `Update` |
| `GET /Creators/{slug}` | `GET /api/v1/creators/{slug}` | anonymous |
| `POST /Creators/{slug}/Follow` | `POST /api/v1/creators/{slug}/follow` | authenticated |
| `GET /Creator/Articles/Compose` | `POST /api/v1/creators/me/articles` | `Creator.Article.Create` |
| `GET /Creator/Articles/Edit/{id}` | `PUT /api/v1/creators/me/articles/{id}` + submit-for-review | `Creator.Article.Update` |
| `GET /Me/Feed` | `GET /api/v1/me/creator-feed` | authenticated |
| `GET /Admin/Creators` | `GET /api/v1/admin/creators` | `AdminCreatorQueue.Read` |
| `GET /Admin/Creators/{id}` | `GET /api/v1/admin/creators/{id}` | `AdminCreatorQueue.Read` |
| `POST /Admin/Creators/Invitations/New` | `POST /api/v1/admin/creators/invitations` | `AdminCreatorQueue.Invite` |
| Action buttons (Approve/Reject/RequestMoreInfo/Suspend/Reinstate) | `POST /api/v1/admin/creators/{id}/{action}` | each gated by its respective permission |

#### 4.12.12 Notifications (delivered via Messaging — Wave-7 integration)

5 in-app + email templates:
- `CreatorApprovedNotificationHandler` — "Welcome, Creator!" + link to `/Creators/Me`
- `CreatorRejectedNotificationHandler` — reason + cooling countdown
- `CreatorSuspendedNotificationHandler` — reason + appeal-via-support CTA
- `CreatorReinstatedNotificationHandler` — "You're back" + link to compose
- `CreatorInvitationDeliveryHandler` — for Email: SMTP with secure token link; for InApp: bell notification with deep-link to apply page

Bell preferences (Wave-6 notification preferences page) gets two new toggles: "Creator application updates" and "New content from followed creators" (default ON for both for new creators).

---

### 4.13 Wave-8 Creator Multi-Type Posts + Tier Promotion + Moderation — detailed UI spec

> **Backend status:** spec written (`Agents/Waves/Wave-8.md`) — not yet implemented; depends on Wave-7 shipping.
> **Skill plan:** Primary `huashu-design` (4 type-specific composers need bespoke design) + `ui-ux-pro-max` (admin moderation queue + tier dashboards); Polish `design-taste-frontend` (universal post viewer + tier-up celebration).

#### 4.13.1 Compose hub `/Creator/Compose`

Type-selector page with 4 large cards. Each card: type icon, title, 1-line description, "Estimated time: X min" hint.

- **🎥 Video** → `/Creator/Compose/Video`
- **📸 Photo Story** → `/Creator/Compose/PhotoStory`
- **⭐ Long Review** → `/Creator/Compose/LongReview`
- **🗺️ Itinerary** → `/Creator/Compose/Itinerary`

Tier-0 creator sees soft banner: "Your first posts will be reviewed by an admin within 24-48h. After 5 approved posts you'll graduate to instant publishing."

#### 4.13.2 Video composer `/Creator/Compose/Video`

- Paste video URL → auto-detect provider (YouTube / Vimeo / Hosted) → render embed preview below the input
- Title + Excerpt (shared with all types)
- Optional: thumbnail override (upload custom thumb 1280×720), duration auto-extracted from oEmbed
- Optional: transcript paste (for accessibility + SEO)
- Captions-available toggle
- Tag entities (Tour/Place/Business multi-pick) + niches + tags + disclosure section
- Submit / Save Draft / Preview

Hosted video upload (if `videoProvider = Hosted`): drag-drop with progress bar, max 500MB, MP4/WebM, max 15 min duration — defer to Wave-8.1 if storage isn't ready.

#### 4.13.3 Photo Story composer `/Creator/Compose/PhotoStory`

Drag-drop gallery uploader. 3-30 images. Each tile shows thumbnail + caption input + alt-text input + reorder handle. First image flagged as "Cover". Bulk-upload + per-image edit.

Image specs: ≥1200px on long edge, max 5MB each, JPG/WebP/PNG. Auto-generates EXIF-stripped variants (uses existing Wave-2 bulk-image endpoint POST `/attachments/{entityType}/{entityId}/images`).

#### 4.13.4 Long Review composer `/Creator/Compose/LongReview`

- **Entity picker:** Tour / Place / Business radio → search-as-you-type → confirm selection (shows entity card)
- **Overall rating:** 5-star half-step picker (1.0–5.0)
- **Sub-ratings:** value, communication, accuracy, experience (4 sliders 1-5)
- **Pros + Cons:** two parallel chip lists (5 each, free text 50-150 chars)
- **Visited on:** date picker (≤ today)
- **Would recommend:** Y/N toggle
- **Body:** Markdown editor (same as Wave-7 article)

If creator owns reviewed entity (provider cross-check) → red banner "You're reviewing your own listing — disclosure required" + IsSponsored toggle + DisclosedTargets pre-populated.

#### 4.13.5 Itinerary composer `/Creator/Compose/Itinerary`

Most complex UI. Multi-day builder:
- **Top:** Duration days (1-30), best seasons multi-select, physical difficulty (Easy/Moderate/Challenging), recommended-for chips
- **Day blocks:** stacked vertically, drag-to-reorder. Each day: day-number badge, title, activities list
  - **Activity row:** entity picker (Tour/Place/Business) + custom title + notes textarea + estimated-hours number input + drag handle
  - "+ Add activity" button per day
- **Cost estimate** (USD, optional)
- Disclosure section (auto-triggers if any tagged entity is creator-owned)
- **Day picker shortcut:** clicking "Add day" duplicates last day's structure with empty fields

Save-draft auto-saves entire JSON tree every 30s.

#### 4.13.6 Universal post viewer `/Posts/{slug}`

Type-aware layout, sharing chrome:
- **Header:** creator avatar + DisplayName + tier badge + verified-provider badge if applicable
- **Hero:** type-specific
  - Video → embed player, max 1280px width
  - PhotoStory → full-bleed gallery with lightbox + caption strip below each
  - LongReview → entity card + rating badge + sub-ratings bar chart
  - Itinerary → expandable day-by-day list, each day cards-in-a-row layout
- **Body:** Markdown rendered
- **Disclosure banner:** if IsSponsored, prominent yellow ribbon "This is sponsored / Creator owns reviewed listings"
- **Tags + niches:** clickable chips
- **Reactions + comments:** reuse Wave-4 Blog reaction/comment widgets
- **Related posts:** carousel of 4 same-niche or same-creator posts

#### 4.13.7 Featured posts `/Posts/Featured`

Curated grid (Pinterest-style masonry). Tier 2 creators only. Filter chips by type. Each tile has small "Featured" ribbon badge.

#### 4.13.8 Admin post moderation queue `/Admin/Posts`

Tabular admin view with filters:
- **Status:** PendingReview (default) / Published / Rejected / Hidden / Removed
- **Type:** Video / PhotoStory / LongReview / Itinerary
- **Tier:** filter by creator tier
- **Has open reports:** Y/N (cross-ref Wave-6 reports)

Each row: thumbnail (type-specific), title, type icon, creator + tier badge, submitted-at, report-count badge (red if ≥3), disclosure flag indicator (🟡 if IsSponsored), bulk-select checkbox.

Bulk actions: Approve all selected / Reject all selected / Remove all selected (each with confirm modal listing affected creators + reasons).

#### 4.13.9 Admin post review `/Admin/Posts/{id}`

Full preview alongside admin action panel:
- **Left (60%):** rendered post (read-only, type-specific)
- **Right (40%):** action panel
  - Creator info card (DisplayName, tier badge, ApprovedPostCount, ReportRate)
  - Disclosure section: lists DisclosedTargets if any; warning if `_taggedEntityIds ∩ creator's provider entities ≠ ∅ AND !IsSponsored`
  - Reports list (if any) with reporter user IDs + reasons (from Wave-6)
  - Action buttons: **Approve** / **Reject (with reason)** / **Remove (with reason — published-only)** / **Feature (Tier-2 only, with optional expiry date)**
  - Internal notes textarea (admin-only, persisted to audit log)

#### 4.13.10 Tier promotion celebration modal

Triggered when `creators.tier.promoted.v1` arrives over SignalR (existing Wave-6 hub):
- Confetti animation
- Tier badge (gold for Tier 2, silver for Tier 1)
- Headline: "You've been promoted to Tier 1! 🎉" / "Tier 2! ⭐"
- Body: list of new perks ("No more pre-moderation", "Eligible for featuring", etc.)
- CTA: "Compose a new post" → `/Creator/Compose`
- Dismiss button

For admin one-click confirm: `/Admin/Creators/{id}` gets a new "Promote to Tier N" button when `EligibleForTier1=true` (or Tier 2). Confirm modal shows current stats + tier requirements satisfied.

#### 4.13.11 Creator stats dashboard `/Creators/Me/Stats`

Read-only analytics page (foundation for Wave-9 monetization):
- **Top cards:** ApprovedPostCount, FollowerCount, TotalViewCount, ReportRate (red if > 5%), TrustTier (with eligible-for-next-tier progress bar)
- **Time-series charts** (last 30/90/180 days):
  - Views per day
  - New followers per day
  - Reactions per day
- **Per-post table:** title, type, status, published-at, views, reactions, comments — sortable + paginated
- **Tier progression panel:** "X more approved posts to Tier N · Y% report-rate · need < Z%" with progress bars

#### 4.13.12 Endpoint coverage matrix

| Route | Backend endpoint | Permission |
|---|---|---|
| `GET /Creator/Compose/{Video\|PhotoStory\|LongReview\|Itinerary}` | `POST /api/v1/creators/me/posts` | `Creator.Post.Create` |
| `PUT /Creator/Posts/{id}/Edit` | `PUT /api/v1/creators/me/posts/{id}` | `Creator.Post.Update` |
| `GET /Posts/{slug}` | `GET /api/v1/posts/{slug}` | anonymous |
| `GET /Posts/Featured` | `GET /api/v1/posts/featured` | anonymous |
| `GET /Creators/{slug}/Posts` | (filtered list) | anonymous |
| `GET /Admin/Posts` | `GET /api/v1/admin/posts` | `AdminPostModeration.Read` |
| `GET /Admin/Posts/{id}` | `GET /api/v1/admin/posts/{id}` | `AdminPostModeration.Read` |
| Action buttons (Approve/Reject/Feature/Remove) | `POST /api/v1/admin/posts/{id}/{action}` | each gated |
| Tier promote/demote | `POST /api/v1/admin/creators/{id}/promote-tier` / demote-tier | `AdminCreatorQueue.PromoteTier` / DemoteTier |
| `GET /Creators/Me/Stats` | (read-only query, rolled up from BG service) | `Creator.Profile.Read` |

#### 4.13.13 Notifications (Wave-8 additions to Messaging)

- `CreatorPostPublishedNotifyFollowersHandler` — broadcasts to all followers (batch-paged 100/req)
- `CreatorPostRemovedNotifyCreatorHandler` — "Your post was removed" + admin's reason
- `CreatorPostFeaturedNotifyCreatorHandler` — celebration
- `CreatorTierPromotedNotifyHandler` — celebration + SignalR push triggers modal
- `CreatorTierDemotedNotifyHandler` — concern + guidelines link

Bell preferences additions: "Featured post celebrations", "Tier change alerts", "New post from followed creator" (separate from Wave-7's general creator updates).

---

### 4.14 Utility routes
- `/sitemap.xml` → reverse-proxy `GET /api/v1/sitemap.xml`
- `/robots.txt` → static file in wwwroot

**Total MVC routes: ~140**

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

UI sprints by-wave dependency map (last updated: Wave-7/8 specs added, Dec 2025):

| Wave | Backend status | UI blockers | UI spec section |
|---|---|---|---|
| **Wave-1** Foundations | ~95% built | 3 OAuth split endpoints (`/auth/external/apple|facebook|google`), `GET /admin/roles/{id}/claims`, `DELETE /admin/users/{id}/roles/{roleName}` (by name), unified `PUT /admin/users/{id}/status` | §4.7 |
| **Wave-2** Provider Application | ✅ **RESOLVED** (Dec 2025) | One gap remains: `GET /api/v1/admin/providers/{id}` single-get for admin detail page (Wave-2.1 patch) | §4.6 |
| **Wave-3** Catalog | ✅ built; Tour Guides 5 endpoints shipped, tests/runtime smoke pending | None blocking UI; verify guide profile endpoints in staging before launching `/Guides/{id}` and `/Provider/Guide/Edit` | §4.8 |
| **Wave-4** Blog + Enrichment + SEO | ✅ 100% built | None | §4.9 |
| **Wave-5** Booking & Payment | Partial — 15 endpoints missing | **Availability Slots (6)** blocks `/Tours/{slug}/Book` + `/Provider/Tours/{id}/Availability`. **Refund Policies (3)** blocks `/Provider/Tours/{id}/RefundPolicy` + cancellation modal. **Join Requests (3)** blocks `/Tours/{slug}/JoinRequest` + `/Provider/JoinRequests`. **Provider reads (3)** blocks `/Provider/Bookings/{Pending,Upcoming,History}`. | §4.10 |
| **Wave-6** Discovery + Notifications + Support | ✅ 100% built (80+ endpoints + 11 BG services + SignalR) | None | §4.11 |
| **Wave-7** Creator Identity + Blog Authoring | 📋 spec only — `Agents/Waves/Wave-7.md` | **ALL 22 endpoints + 5 cross-module handlers + 1 background service required before any UI**: `/api/v1/creators/{apply,redeem-invitation,status,me,{slug},{slug}/follow}`, `/api/v1/creators/me/{articles,...}`, `/api/v1/me/creator-feed`, `/api/v1/admin/creators/...` | §4.12 |
| **Wave-8** Creator Multi-Type Posts + Tier Promotion + Moderation | 📋 spec only — `Agents/Waves/Wave-8.md` | Depends on Wave-7 shipping first. **17 endpoints + 2 background services**: `POST /api/v1/creators/me/posts`, full /api/v1/posts public + /api/v1/admin/posts admin queue + promote-tier / demote-tier. | §4.13 |

### 13.1 Sprint sequencing

UI work order recommended by backend readiness:

1. **Now unblocked:** Wave-1 (residual), Wave-2 (just shipped), Wave-3, Wave-4, Wave-6.
2. **Wave-5 partial-build path:** ship booking flow against existing endpoints first; backfill availability / refund policy / join request UI as backend ships the missing 15 endpoints.
3. **Wave-2.1 patch:** add `GET /api/v1/admin/providers/{id}` so `/Admin/Providers/{id}` doesn't have to filter-the-list client-side.
4. **Wave-7 implementation gate:** Wave-7 backend (~81h estimated) must ship before any of §4.12 UI work begins. UI design can iterate on mockups in parallel (use `huashu-design` skill for hi-fi prototyping without backend).
5. **Wave-8 gate:** Wave-7 must be shipped and validated in staging before Wave-8 backend starts. Wave-8 retroactively adds `IsSponsored` + `DisclosedTargets` to Wave-7 `Article` entity — coordinate migration timing.

See `Agents/Waves/Wave-{1..8}.md` for full backend work breakdowns.

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

## 19. User Experience Rules (UI-UX-*)

These rules encode product decisions made via interactive Q&A. They complement §18 (Performance Rules) with user-facing behavior standards. All UI-UX rules are mandatory for production builds.

### 19.1 Device & Layout (UI-UX-D)

**UI-UX-D1 — Equal mobile + desktop priority**
- Build responsively from 320px → 2560px. No "mobile version" or "desktop version".
- All features available on all viewports (no feature-gating by screen size).
- Layout breakpoints: `xs:0`, `sm:576`, `md:768`, `lg:992`, `xl:1200`, `xxl:1400` (Bootstrap defaults).

**UI-UX-D2 — Touch target minimum 44×44px**
- All clickable elements (buttons, links, icons) must meet Apple HIG / WCAG 2.5.5 touch target size.
- Use `min-height: 2.75rem; min-width: 2.75rem` on icon buttons.

**UI-UX-D3 — Mobile bottom navigation**
- Bottom fixed nav bar on mobile (≤768px) with 4-5 primary actions: **Browse / Search / Wishlist / Bookings / Profile**.
- Plus hamburger (top-left) for full menu (categories, settings, support, sign-out).
- Hide bottom nav on auth screens (sign-in, sign-up, verify).
- Use `<nav class="navbar fixed-bottom d-md-none">` Bootstrap pattern.

**UI-UX-D4 — Bottom sheet for filters on mobile**
- Filter sidebar (`_FilterSidebar.cshtml`) becomes bottom sheet on mobile via Bootstrap offcanvas + `bottom` placement.
- Sticky "Apply" button at bottom of sheet.
- Show active filter count badge on filter trigger button.

**UI-UX-D5 — Sticky CTAs on mobile**
- "Book Now" button on tour detail page sticky-bottom on mobile.
- "Save & Continue" buttons in wizards sticky-bottom.

### 19.2 Network Adaptation (UI-UX-N)

**UI-UX-N1 — Honor Save-Data header**
- Detect `Save-Data: on` request header server-side AND `navigator.connection.saveData` client-side.
- Serve lower-quality images (60% JPEG quality, no AVIF), skip autoplay videos, disable parallax, no lazy-loaded video posters.
- Add `Vary: Save-Data` response header on cached pages.

**UI-UX-N2 — Adaptive image quality by Network Information API**
- Client-side `navigator.connection.effectiveType` detection:
  - `4g` / `wifi` → 85% quality hero, 75% gallery, 60% thumbs
  - `3g` → 75% quality hero, 60% gallery, 50% thumbs
  - `2g` / `slow-2g` → 60% quality across the board, no auto-play, no parallax
- Server-side `Sec-CH-Save-Data` Client Hints support.

**UI-UX-N3 — Service worker for asset caching (NOT content)**
- Register service worker that caches:
  - CSS, JS bundles (cache-first, 1 year)
  - Vendor libs (cache-first, 1 year)
  - Fonts (cache-first, 1 year)
  - Logo + static images (cache-first, 30 days)
- **Forbidden:** Caching API responses, HTML pages, or user data. (Per decision: asset caching only, no offline content.)
- Cache strategy via Workbox or vanilla SW with `caches.match()` + `caches.open()`.

**UI-UX-N4 — Service worker update lifecycle**
- On new SW deploy → show non-blocking toast "App updated. [Reload]" → user-triggered refresh.
- Don't auto-reload (interrupts user).

**UI-UX-N5 — Offline page fallback**
- Serve cached `offline.html` if network completely fails on navigation.
- Page shows: "You're offline. Some features may be unavailable. [Retry]"

### 19.3 Progressive Enhancement (UI-UX-PE)

**UI-UX-PE1 — Core works without JS**
- All forms submit via standard POST when JS disabled (MVC handles natively).
- All links navigate normally; no `href="javascript:..."`.
- Toggle states (favorite heart, expand/collapse) work via form submission fallback.

**UI-UX-PE2 — JS-required features must degrade**
- Mapbox failing → show text list of locations with distance.
- SignalR failing → fall back to manual page refresh for notifications.
- Web Speech API absent → hide voice search icon.
- Service Worker absent → just lose offline asset caching, no other impact.

**UI-UX-PE3 — Hydration-free**
- Server renders complete HTML; JS only enhances (event handlers, dynamic updates).
- **Forbidden:** Loading content via JS after initial render (use server-side data in ViewModel).

**UI-UX-PE4 — Modernizr-free feature detection**
- Use native `if ('IntersectionObserver' in window)`, `if ('serviceWorker' in navigator)`.
- No browser-detection libraries.

### 19.4 Accessibility (UI-UX-A11Y)

**UI-UX-A11Y1 — WCAG 2.1 AA mandatory across all pages**
- Color contrast 4.5:1 normal text, 3:1 large text (18pt+).
- All interactive elements keyboard-accessible (Tab, Shift+Tab, Enter, Space, Esc, arrows).
- ARIA labels on icon-only buttons (`aria-label="Add to wishlist"`).
- Focus visible via `:focus-visible` outline (2px solid primary).

**UI-UX-A11Y2 — Skip-to-main-content link**
- First focusable element on every page: `<a href="#main" class="visually-hidden-focusable">Skip to main content</a>`.

**UI-UX-A11Y3 — Semantic HTML**
- Use `<nav>`, `<main>`, `<article>`, `<aside>`, `<section>`, `<button>`, `<a>` correctly.
- **Forbidden:** `<div onclick>` (use `<button>` always).

**UI-UX-A11Y4 — Form labels + error association**
- Every input has `<label asp-for="X">` OR `aria-label`.
- Errors via `<span asp-validation-for="X" role="alert">`.
- `aria-describedby` linking helper text + errors to fields.

**UI-UX-A11Y5 — Color never the only indicator**
- Booking status uses color + icon + text (✓ "Confirmed", ✗ "Cancelled", ⏱ "Pending").
- Form errors: red color + error icon + text message.
- Charts: patterns/shapes in addition to colors.

**UI-UX-A11Y6 — Heading hierarchy**
- One `<h1>` per page (the main title).
- No skipping levels (h2 → h4 forbidden).
- Section headings ordered logically.

**UI-UX-A11Y7 — Image alt text**
- All `<img>` have meaningful `alt` text OR `alt=""` for decorative.
- Hero images: descriptive alt ("Sunset over Petra ruins").
- Avatars: `alt="{userName}'s avatar"`.
- Icon images: `aria-hidden="true"` if accompanied by text.

**UI-UX-A11Y8 — Keyboard shortcuts (provider/admin)**
- `Ctrl+/` or `?` opens keyboard shortcut help dialog.
- `Esc` closes any modal/sheet.
- `Ctrl+K` opens search.

**UI-UX-A11Y9 — Live regions for dynamic updates**
- Notification bell badge: `aria-live="polite"`.
- Toast container: `role="status" aria-live="polite"`.
- Critical alerts: `role="alert" aria-live="assertive"`.

### 19.5 AAA Accessibility Mode — Opt-in (UI-UX-AAA)

User toggle in `/Account/Settings/Accessibility` saves preference to backend + localStorage.

**UI-UX-AAA1 — High contrast mode**
- 7:1 contrast ratio (vs AA's 4.5:1).
- CSS variables `--bg: #000; --text: #fff; --accent: #ffff00; --border: 2px solid #fff`.
- Apply via `<body class="a11y-high-contrast">`.

**UI-UX-AAA2 — Font size scaling**
- 4 levels: Normal (100%), Large (125%), Extra Large (150%), Maximum (200%).
- CSS `--font-base` custom property scales all rem-based sizes.
- Line height increases proportionally to maintain readability.

**UI-UX-AAA3 — Reduced motion override**
- Force-disable all animations even if system preference is "no preference".
- All `transition` / `animation` set to `0.01ms` when class `a11y-reduced-motion` active.

**UI-UX-AAA4 — Color blindness filters**
- 3 sub-modes: Protanopia / Deuteranopia / Tritanopia.
- Apply via SVG color matrix filter on root element.
- All status badges + map pins use patterns/shapes in addition to color.

**UI-UX-AAA5 — Screen reader enrichment**
- Dynamically add extra ARIA labels (e.g., "Heart button, currently not in wishlist, click to add" instead of just "Add to wishlist").
- Verbose mode for complex widgets (date picker, map).

**UI-UX-AAA6 — Settings persistence**
- Save to backend `PUT /api/v1/profile/accessibility-preferences` (server-side for logged-in users).
- LocalStorage for guests, migrated to backend on signup.

### 19.6 Loading States (UI-UX-L)

**UI-UX-L1 — Skeleton loaders for list pages**
- Tour grid, place grid, bookings list, reviews list, notifications panel: render skeleton placeholders matching final card layout.
- Use Bootstrap placeholder utilities: `<span class="placeholder col-7"></span>`.
- Show 4-6 skeleton cards while data loads.

**UI-UX-L2 — Spinners for actions**
- Button clicks (Submit, Add to Cart, Pay): replace button text with spinner + "Processing..."
- Disable button during action.
- Use Bootstrap spinner: `<span class="spinner-border spinner-border-sm" role="status"></span>`.

**UI-UX-L3 — Progress bar for full-page navigation**
- NProgress-style top bar on route changes (if AJAX-navigating).
- Shows 0-100% based on `fetch` progress events where available.

**UI-UX-L4 — Skeleton matches final structure**
- Don't use generic grey rectangles. Match card structure: image placeholder + text lines + button placeholder.
- Smooth pulse animation (1.5s loop).

**UI-UX-L5 — Show skeletons within 100ms**
- Skeletons appear before SSR completes (CSS-only via `:has()` or class on body).
- Render skeleton, then progressively reveal real content as it loads.

**UI-UX-L6 — Empty states**
- Empty bookings: "No bookings yet. [Browse tours →]"
- Empty wishlist: "Save tours you love. Tap the heart icon to add. [Browse tours →]"
- Empty search results: "No tours match your filters. [Clear all filters]"

### 19.7 Form Validation (UI-UX-F)

**UI-UX-F1 — Validation on blur after first interaction**
- Field validates when user leaves it (blur event).
- Don't validate before user has typed anything.
- All errors shown on submit attempt.

**UI-UX-F2 — Helper text always visible**
- Below every input: `<small class="form-text text-muted">Password must be 8+ characters with upper, lower, digit, special</small>`
- Required field indicator: `<span class="text-danger" aria-label="required">*</span>` next to label.

**UI-UX-F3 — Real-time password strength meter**
- Strength bar updates as user types (debounced 200ms).
- Visual: red (weak) → yellow (medium) → green (strong).
- Show specific requirements not yet met.

**UI-UX-F4 — Multi-step wizard for provider tour creation**
- Bootstrap Stepper pattern from template.
- Steps: 1) Basic info → 2) Pricing → 3) Schedule → 4) Images → 5) Review.
- Validation per step before allowing Next.
- Allow back navigation without losing data.

**UI-UX-F5 — Auto-save drafts every 30s**
- Wizard forms (tour creation, support ticket, blog post for admins) auto-save to localStorage every 30s.
- Show indicator: "Saved 5s ago" near form title.
- Backend draft sync: POST `/api/v1/drafts/{type}` (TODO endpoint).
- Recovery banner on page reload: "You have an unsaved draft from 2 hours ago. [Continue editing] [Discard]"

**UI-UX-F6 — Inline error messages**
- Errors appear directly below the field in red.
- `<span asp-validation-for="X" class="text-danger small mt-1"></span>`
- Scroll to first error on submit failure.

**UI-UX-F7 — Form submission button states**
- Disabled state when form invalid (after first submit attempt).
- Loading state during submission (spinner + "Saving...").
- Re-enable on validation failure response.

**UI-UX-F8 — Confirmation for destructive actions**
- Delete account, cancel booking, remove review → modal with explicit confirm.
- Modal title: "Cancel this booking?"
- Body: details + refund amount preview.
- Two buttons: "Keep booking" (primary, autofocus) and "Yes, cancel" (danger).

**UI-UX-F9 — Forbid losing unsaved changes**
- `beforeunload` event handler on dirty forms.
- Warns user before navigating away with unsaved data.
- Skip warning for short forms (<10 fields) where re-typing is trivial.

### 19.8 Notifications & Feedback (UI-UX-NF)

**UI-UX-NF1 — Toast for success messages**
- Bottom-right placement, max 3 visible (queue rest).
- Auto-dismiss after 5s.
- Bootstrap toast component.
- Examples: "Booking confirmed", "Review posted", "Added to wishlist".

**UI-UX-NF2 — Inline errors next to action source**
- Validation errors below the field/button that triggered them.
- API errors below the action button: "Couldn't save changes. [Retry]"
- Don't blast modals for recoverable errors.

**UI-UX-NF3 — Modal dialogs for critical errors only**
- Payment failed: full modal blocking further interaction.
- Account locked: full modal with support contact.
- Session expired: modal redirecting to sign-in.
- All other errors → inline or toast.

**UI-UX-NF4 — Toast queue + stacking**
- Max 3 simultaneous toasts; new ones queue.
- Same-message toasts deduped (don't spam "Saved!" 5 times).

**UI-UX-NF5 — Toast accessibility**
- `role="status" aria-live="polite"` on container.
- Auto-focus dismiss button if user is keyboard-navigating.

**UI-UX-NF6 — Optimistic UI updates**
- Heart icon fills immediately on click (before API confirms).
- Revert + show error toast if API call fails.
- Examples: favorites, review-helpful reactions.

**UI-UX-NF7 — SignalR bell badge update**
- New notification arrives → badge count increments + subtle bell shake animation.
- Click bell opens dropdown with latest 5 notifications.
- "View all" link to `/Account/Settings/Notifications`.

### 19.9 Motion & Animation (UI-UX-M)

**UI-UX-M1 — Subtle Material Design easing**
- Default duration: 200-300ms.
- Default easing: `cubic-bezier(0.4, 0, 0.2, 1)` (Material standard).
- Enter animations: faster (200ms). Exit: snappier (150ms).

**UI-UX-M2 — Animate transform + opacity only**
- **Forbidden:** Animating `width`, `height`, `top`, `left`, `margin` (causes layout thrash).
- Use `transform: translate / scale / rotate` + `opacity` for GPU acceleration.

**UI-UX-M3 — prefers-reduced-motion respected**
- CSS:
  ```css
  @media (prefers-reduced-motion: reduce) {
    *, *::before, *::after {
      animation-duration: 0.01ms !important;
      transition-duration: 0.01ms !important;
    }
  }
  ```
- Disable autoplay carousels, parallax, AOS scroll animations.

**UI-UX-M4 — No animation on critical paths**
- Booking checkout flow: instant page transitions (no slide/fade between steps).
- Payment forms: no loading spinners that delay perception.

**UI-UX-M5 — Meaningful animations only**
- ✅ State change (toggle, expand/collapse, modal open/close)
- ✅ Focus shift (smooth scroll to error, focus indicator)
- ✅ Loading feedback (spinner, progress bar, skeleton)
- ❌ Decorative wiggles, bouncing buttons, parallax for show

**UI-UX-M6 — Page transition: instant**
- No SPA-style page transitions (MVC pattern).
- Server-side render with instant page swap.
- Optional: thin progress bar (NProgress) at top during navigation.

### 19.10 Theme — Light + Dark + System (UI-UX-T)

**UI-UX-T1 — System preference default**
- Detect `prefers-color-scheme` media query on first visit.
- Set initial theme accordingly.

**UI-UX-T2 — User toggle in header (sun/moon icon)**
- Manual toggle overrides system preference.
- Persist choice to localStorage + cookie (cross-tab + reload consistent).

**UI-UX-T3 — CSS custom properties for theming**
- All colors via `--bs-primary`, `--bs-body-bg`, `--bs-body-color`, etc.
- Dark mode: override values on `<html data-bs-theme="dark">`.
- Bootstrap 5.3+ native dark mode support.

**UI-UX-T4 — Theme switch animation**
- 250ms cross-fade between themes (no harsh swap).
- Skip animation if `prefers-reduced-motion: reduce`.

**UI-UX-T5 — No flash of wrong theme (FOWT)**
- Inline `<script>` at top of `<head>` reads localStorage + sets data-bs-theme BEFORE CSS loads.
- Prevents 1-frame flash of light mode on dark-preference users.

**UI-UX-T6 — Image adjustments per theme**
- Hero images may have dark-mode variants (`hero-dark.webp`).
- Use `<picture>` element with `media="(prefers-color-scheme: dark)"`.

### 19.11 Search & Filtering (UI-UX-S)

**UI-UX-S1 — Hybrid: SSR for SEO, AJAX for refinement**
- Initial page load: server-side render with query string filters (`/tours?category=hiking&minPrice=20`).
- Subsequent filter changes: AJAX `fetch` to MVC action returning partial view OR JSON.
- Update URL via `history.pushState` without full reload.
- Browser back button restores previous filter state.

**UI-UX-S2 — Text autocomplete (300ms debounce)**
- Search input shows top 5 suggestions as user types.
- Suggestions categorized: Tours, Places, Categories.
- Click suggestion → navigate directly to entity.
- "View all results for 'X'" link → full search page.

**UI-UX-S3 — Filter UI patterns**
- Desktop: left sidebar with collapsible accordion sections.
- Mobile: bottom sheet with all filters + "Apply" sticky button.
- Active filter chips above results: "Hiking ×" "20-100 JOD ×" "[Clear all]"
- Filter count badge on filter trigger.

**UI-UX-S4 — Filter persistence: session only**
- Filters reset on browser close (per user decision).
- Within same session, browser back/forward restores.

**UI-UX-S5 — No filter results state**
- "No tours match your filters."
- "[Clear all filters]" CTA.
- "Or try popular tours:" with 3 fallback recommendations.

**UI-UX-S6 — Sort options**
- Default sort: Popularity (Bayesian score from §18 Analytics).
- Options: Price asc/desc, Rating, Newest, Duration, Distance (if user shared location).
- Sticky sort dropdown on scroll (mobile + desktop).

### 19.12 Images (UI-UX-IMG)

**UI-UX-IMG1 — Variable quality by context (already specified in §18 UI-PERF-I5)**
- Hero (tour detail): 85% AVIF/WebP, max 200KB
- Gallery: 75% AVIF/WebP, max 150KB each
- Card thumbnails: 60% AVIF/WebP, max 30KB
- Avatars: 70% AVIF/WebP, max 20KB
- Save-Data mode: -15% quality across the board

**UI-UX-IMG2 — Gallery: Hero + Thumbnail Strip + GLightbox**
- Tour/place/business detail pages: 1 large hero image + 4-6 thumbnail strip below.
- Click any image → GLightbox opens with all gallery images, keyboard arrow navigation.
- Mobile: swipe gestures in lightbox.

**UI-UX-IMG3 — Skeleton placeholder during load**
- LQIP (16×16 blurred base64) inline as background-image on `<img>` wrapper.
- Real image fades in on load (`opacity 0 → 1` over 200ms).

**UI-UX-IMG4 — Lazy-load below-fold**
- `<img loading="lazy">` for all below-fold.
- Hero/above-fold images: `loading="eager"` + `<link rel="preload" as="image">`.

**UI-UX-IMG5 — Explicit dimensions**
- `width` + `height` attributes on every `<img>` to prevent CLS.
- For unknown aspect: use CSS `aspect-ratio: 16/9; object-fit: cover`.

### 19.13 Map (UI-UX-MAP)

**UI-UX-MAP1 — Lazy-load via IntersectionObserver**
- Mapbox library + tiles loaded only when map container scrolls into viewport.
- Saves ~250KB + many tile requests on initial page load.
- Show static map placeholder image (`<img>` of low-res preview) until interactive map ready.

**UI-UX-MAP2 — Static placeholder image**
- Server-generates or fetches static Mapbox image API (1 HTTP request, ~30KB).
- Replaced by interactive map on first scroll/click.

**UI-UX-MAP3 — Single map instance per page**
- **Forbidden:** Multiple Mapbox containers initialized.
- Destroy + recreate only on page navigation.

**UI-UX-MAP4 — Tour route on detail page**
- Polyline connecting all `TourWaypoints` ordered by SortOrder.
- Meeting point marker: flag icon, distinct color.
- Waypoint markers: numbered (1, 2, 3...).
- Linked businesses (via PlaceBusinesses): secondary marker style.

**UI-UX-MAP5 — Clustering on /map and /tours list view**
- Zoom <13: cluster markers by proximity.
- Zoom ≥13: show individual pins.

**UI-UX-MAP6 — List fallback if Mapbox fails**
- If `Mapbox GL JS` script blocked (ad blocker) or tiles fail:
- Replace map with text list of locations sorted by distance.
- "Map temporarily unavailable. [Retry]"

### 19.14 Booking & Calendar (UI-UX-CAL)

**UI-UX-CAL1 — Inline calendar with availability heatmap**
- Tour detail page: inline calendar widget showing next 90 days.
- Visual heatmap dots per date:
  - 🟢 Green = available (>50% capacity remaining)
  - 🟡 Yellow = limited (≤50% remaining)
  - 🔴 Red = full / unavailable
  - ⚫ Gray = past or blacked out
- Click date → reveals time slots for that date.

**UI-UX-CAL2 — Mobile: bottom sheet calendar**
- Tap date field on mobile → slides up bottom sheet with calendar.
- "Apply" button sticky at bottom.

**UI-UX-CAL3 — Real-time slot updates via SignalR**
- Hub group `tour:{tourId}` broadcasts slot capacity changes.
- Connected users see live decrement when another user books same slot.
- Visual: smooth count animation, optional brief flash on update.

**UI-UX-CAL4 — Slot lock countdown timer**
- Once user enters booking flow, 10-min slot lock starts.
- Visible countdown timer in checkout: "Slot held: 9:32"
- 1 min remaining: warning toast "Slot expiring soon. Complete payment to confirm."
- Expired: auto-redirect to slot-picker with error message.

**UI-UX-CAL5 — Multi-step booking wizard**
- Bootstrap Stepper:
  1. Date + Participants (autofilled from grid filter)
  2. Traveler details (autofill from saved travelers)
  3. Add-ons (optional)
  4. Promo code + Loyalty points
  5. Payment
- Validation per step; can navigate back without losing data.

**UI-UX-CAL6 — Disabled date tooltip**
- Hover/tap on gray date → tooltip: "No availability" / "Tour blocked out" / "Past date".

**UI-UX-CAL7 — 2-hour minimum lead time**
- Calendar disables time slots within 2 hours of current time (per guide §4.3).
- Visual: gray with tooltip "Bookings need 2 hours notice".

**UI-UX-CAL8 — Save abandoned booking**
- If user closes browser mid-booking, save state to localStorage.
- On return: "You had a booking in progress for [Tour]. [Continue] [Discard]"

### 19.15 Wishlist (UI-UX-WL)

**UI-UX-WL1 — Heart icon disabled for guests**
- Guest sees heart icon with `disabled` state + tooltip: "Sign in to save tours".
- Click → opens sign-in modal with return URL set to current page.

**UI-UX-WL2 — Optimistic toggle**
- Click heart → fills instantly (no waiting for API).
- API call in background.
- Revert + toast error if API fails.

**UI-UX-WL3 — Confirmation toast**
- "Added to wishlist" (with link to /Account/Wishlist).
- "Removed from wishlist" with [Undo] action.

**UI-UX-WL4 — Wishlist counter in nav**
- Logged-in users see badge count on nav heart icon: "Wishlist (12)".

**UI-UX-WL5 — Max 500 enforcement**
- Client prevents adding when count = 500.
- Modal: "Wishlist full. Remove an item first. [Manage wishlist]"

### 19.16 Provider Wizard (UI-UX-PROV)

**UI-UX-PROV1 — Multi-step tour creation wizard**
- 5 steps per §19.7 UI-UX-F4.
- Validation per step; can navigate back.

**UI-UX-PROV2 — Auto-save every 30s**
- LocalStorage key: `yallajo:provider:tour-draft:{userId}`.
- Server sync via `POST /api/v1/drafts/tour` every 2 minutes (TODO endpoint).
- Indicator: "Saved 12s ago" near step title.

**UI-UX-PROV3 — Draft recovery banner**
- On reload of `/Provider/Listings/Create`: "You have an unsaved tour draft from 2 hours ago. [Continue] [Discard]"

**UI-UX-PROV4 — Cancel confirmation**
- "Cancel" button on wizard → modal: "Discard changes? All progress will be lost."
- Two options: "Keep editing" (primary) / "Discard"

**UI-UX-PROV5 — Sidebar collapsible**
- Provider/Admin layouts: sidebar can collapse to icon-only state.
- Saves screen space on smaller laptops.
- State persists in localStorage.

**UI-UX-PROV6 — Sticky top bar**
- Notification bell, language switcher, profile dropdown sticky-top on scroll.
- Plus "Quick add" button (provider) → dropdown: New tour / New discount / New review reply.

### 19.17 Print & Export (UI-UX-PE2)

**UI-UX-PE21 — Print stylesheet for booking confirmation**
- `@media print` CSS optimized layout: hide nav/footer, show full booking details + QR code for ConfirmationCode.
- Print button on `/BookingConfirm/{id}` triggers `window.print()`.

**UI-UX-PE22 — PDF download (backend)**
- "Download PDF" button → `GET /api/v1/invoices/{id}/download` (already exists).
- Server-rendered PDF using QuestPDF (already integrated per Finance §T3).

**UI-UX-PE23 — Calendar .ics export**
- "Add to Calendar" button on confirmed booking page.
- Generates .ics file client-side with: SUMMARY (tour name), DTSTART (slot time), LOCATION (meeting point), DESCRIPTION (details + confirmation code).
- Works with Apple Calendar / Google Calendar / Outlook.

### 19.18 Tooltips & Help (UI-UX-TIP)

**UI-UX-TIP1 — Mix: helper text + tooltips**
- Form fields: always-visible helper text below input.
- Icon-only buttons: Bootstrap tooltip on hover/focus.
- Complex rules (commission tiers, cancellation policy): info icon → modal explainer.

**UI-UX-TIP2 — Bootstrap tooltips setup**
- Initialize via `data-bs-toggle="tooltip"`.
- Delay 500ms before showing on hover (avoid accidental triggers).
- Mobile: dismiss on tap-outside.

**UI-UX-TIP3 — ARIA-described-by**
- Tooltips linked via `aria-describedby` for screen readers.
- Helper text linked via same pattern.

**UI-UX-TIP4 — Help center deep links**
- Complex topics link to `/help/{slug}` from tooltips: "Learn more →".

### 19.19 Reviews (UI-UX-REV)

**UI-UX-REV1 — Email-triggered modal review submission**
- Post-tour email: "How was your tour? [Rate now]" → link to `/Account/Bookings/{id}` with `?action=review`.
- Modal opens on page load when query param present.
- Star rating + optional text + photos (max 3).

**UI-UX-REV2 — In-page submission for ad-hoc**
- "Write a review" button on tour detail page for users with completed bookings.
- Same modal pattern.

**UI-UX-REV3 — Review form fields**
- Star rating (1-5 in 0.5 increments) — required.
- Title (max 150 chars) — optional.
- Content (20-2000 chars) — required.
- Photos (max 3, max 5MB each) — optional via Dropzone.

**UI-UX-REV4 — 30-day window indicator**
- "You can review until [date]" reminder text.
- After 30 days: review button disabled with tooltip "Review window closed".

**UI-UX-REV5 — 48-hour edit window**
- Submitted reviews show "Edit (12h left)" button counting down.
- After 48h: edit button replaced with "Posted" with timestamp.

**UI-UX-REV6 — Profanity-flagged review state**
- If profanity filter triggers → review shows status "Pending moderation. We'll publish it after review."
- User can edit + resubmit.

### 19.20 Cookie Consent — Deferred to Phase 2 (UI-UX-CC)

**UI-UX-CC1 — Phase 1 stance: no banner**
- Per user decision, no cookie consent banner in Phase 1.
- All cookies (auth, preferences) are functional/necessary — no consent legally required in Jordan-only market.

**UI-UX-CC2 — Phase 2: add granular banner when expanding to EU/MENA broader**
- Bottom banner with Accept/Reject/Customize.
- Granular categories: Necessary (always on), Functional, Analytics, Marketing.
- Block GA/Mixpanel scripts until consent given.

### 19.21 Geographic + Currency (UI-UX-GEO)

**UI-UX-GEO1 — Jordan-only Phase 1**
- Default currency: JOD.
- Default language: Arabic (with English toggle).
- All prices stored in JOD; conversion to USD/EUR via Tour.Currency field.
- Phone format: +962 prefix.

**UI-UX-GEO2 — Currency display rule**
- Show tour's base currency in catalog (JOD/USD/EUR).
- User can toggle preferred display currency in `/Account/Settings`.
- Conversion via daily FX rate cached server-side (not implemented yet — future).

**UI-UX-GEO3 — Date/time format**
- Default: Gregorian calendar for date pickers.
- Optional Hijri toggle in `/Account/Settings` (Arabic users).
- All timestamps stored UTC; display in user's `IANA timezone` (default: `Asia/Amman`).

**UI-UX-GEO4 — Address autocomplete**
- Future: integrate Mapbox Geocoding API for Jordan addresses.
- Phase 1: free text field.

### 19.22 Real-Time Updates (UI-UX-RT)

**UI-UX-RT1 — SignalR slot capacity broadcasts**
- Group: `tour:{tourId}`.
- Event: `SlotCapacityChanged` payload `{ slotId, remainingCapacity }`.
- Client updates UI smoothly.

**UI-UX-RT2 — Cross-device booking sync**
- User books on phone → desktop tab updates booking count badge.
- Group: `user:{userId}`.

**UI-UX-RT3 — Notification bell live updates**
- New notification → bell badge increments + subtle animation.
- Per §19.8 UI-UX-NF7.

**UI-UX-RT4 — Provider booking alerts**
- New booking for provider's tour → provider dashboard receives SignalR push + toast.
- Group: `provider:{providerId}`.

**UI-UX-RT5 — Admin moderation queue updates**
- New flagged review/report → admin dashboard badge updates.
- Group: `admin`.

### 19.23 Implementation Priorities

UI-UX rules align with the §12 sprint plan but add UX-specific deliverables:

| Sprint | UX deliverables |
|---|---|
| UI-0 (Bootstrap) | Theme switching, RTL setup, Bootstrap toast container, base accessibility hooks |
| UI-1 (Public) | Skeleton loader components, empty state partials, helper text patterns |
| UI-2 (Auth) | Form validation, password strength meter, social login UI |
| UI-3 (Catalog) | Filter sidebar/bottom sheet, autocomplete search, sort dropdown, optimistic wishlist heart |
| UI-4 (Blog) | Reading-progress bar, comment nesting UI |
| UI-5 (Booking) | Inline calendar heatmap, slot countdown timer, multi-step wizard, real-time slot SignalR |
| UI-6 (Account) | Toast notifications, modal patterns, print stylesheets, .ics export |
| UI-7 (Provider onboarding) | Document upload UI, status state-machine visualization |
| UI-8 (Provider dashboard) | Wizard + auto-save, draft recovery, collapsible sidebar |
| UI-9 (Provider advanced) | Calendar slot management with bulk recurring, refund policy editor |
| UI-10 (Admin) | Data tables with virtualization, audit-log filtering, moderation modals |
| UI-11 (Admin settings) | Category tree drag-drop editor, role/claim management |
| UI-12 (Notifications) | SignalR bell integration, real-time badge updates |
| UI-13 (Polish) | AAA mode toggle, color blindness filters, screen reader enrichment |

---

## 20. Next Steps

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

**Companion files already created:**
- `Agents/Plans/UI-UX-Pattern-Report.md` — 20-section endpoint-derived UI/UX pattern catalogue (personas, IA, page inventory, component library, state-machine UX, interaction patterns, accessibility, responsive strategy, implementation waves)

---

## 21. Endpoint Coverage Audit (2025-01-27)

This section maps the full 483-endpoint surface to MVC coverage state. Anything not yet mapped in §4 belongs to a "next wave" backlog. **Use this as the sprint planning source of truth** alongside `Agents/Plans/UI-UX-Pattern-Report.md`.

### 21.1 Persona-to-route-prefix matrix

| Persona | Public catalog | Self-service | Console area | Notes |
|---|---|---|---|---|
| Guest | `/`, `/tours`, `/places`, `/businesses`, `/blog`, `/guides`, `/agency`, `/search`, `/auth/*` | — | — | Wishlist heart shows sign-in modal |
| User | + reviews, favorites read | `/Account/*` | — | Wave 6 + UI-6 |
| Provider | + provider-detail browse | `/Account/*` | `/Provider/*` | Waves 2, 5, 9 |
| TourGuide | + guide profile public | `/Account/*` + `/guides/me/*` | `/Guide/*` *(new console — not yet routed in §4)* | Routes to add: `/Guide/Dashboard`, `/Guide/Applications`, `/Guide/Proposals`, `/Guide/Offerings`, `/Guide/Availability`, `/Guide/Earnings`, `/Guide/Agency`, `/Guide/Tier` |
| Creator | + creator profile public | `/Account/*` + `/blogs/creators/me/*` | `/Creator/*` | Wave 7-8 |
| Admin | + everything | `/Account/*` | `/Admin/*` | Waves 1, 4, 6, 8 |

**Gap:** The Guide console (`/Guide/*`) is implied throughout the doc but never enumerated as a top-level area like `/Provider` or `/Admin`. Add a new §4.x or Wave-2.5 entry for it before Wave-5 work starts. Backend endpoints already exist:
- `/api/v1/guides/me`, `/me/avatar`, `/me/cover-image`, `/me/applications`, `/me/availability-blocks`, `/me/tier`, `/me/earnings/{summary|by-tour|history}`, `/me/analytics/{overview|booking-trends|popular-tours|peak-days}`
- `/api/v1/analytics/guide/{dashboard|analytics|my-tours}` (Wave-2A)
- `/api/v1/finance/guide`, `/finance/guide/summary` (Wave-2B)
- `/api/v1/tours/proposals/*` (5 routes)
- `/api/v1/tours/{tourId}/guide-offerings/{guideId}/{schedules|pricing-tiers|private-tour|suspend|reinstate|remove}` (15 routes)

### 21.2 Module-to-wave coverage

| Module | Endpoints | Status | Wave |
|---|---|---|---|
| Accounts | 39 | ✅ §4.6 covers Provider Apply + Admin queue; agencies & guide-agency partial | Wave 2 (provider) + 2.5 (agency/guide-agency, propose new) |
| Auth | 26 | ✅ §4.7 covers all 26 (3 OAuth + role-claims listing remain backend gaps) | Wave 1 |
| Security | 19 | ⚠️ §4.5 covers users + roles partially; **audit logs page underspecified**, role claims viewer/editor missing | Wave 1 |
| ContentBlogs | 60 | ⚠️ §4.5 covers blog admin; Wave 7-8 covers creator surfaces; **blog comments admin queue not mapped** | Waves 4, 7, 8 |
| ContentCore | 44 | ⚠️ §4.5 covers Categories/Tags/Specs/Languages; **Translations workflow, Attachments admin, EntityTag/EntityCategory admin tooling not mapped** | Wave 1, 4 |
| ContentPlaces | 41 | ⚠️ Business CRUD covered partially; **Service items, amenities, staff, accessibility admin not in §4** | Wave 3 |
| ContentSeo | 23 | ⚠️ §4.9.4 covers metadata + redirects; **Sitemap admin, FAQ admin, Weather admin not mapped** | Wave 4 |
| ContentTours | 75 | ⚠️ §4.8 covers core tour CRUD; **GuideOffering 15 routes, Proposal 5 routes, AdminTourGuide 5 routes, GuideApplication review 6 routes not in §4** | Wave 2.5, 3 |
| Booking | 21 | ⚠️ §4.10 covers customer flow; **Provider booking reads still backend-gap (3); GuideDiscount/JoinRequest admin missing** | Wave 5 |
| Finance | 33 | ⚠️ §4.5/§4.10 covers payments + invoices + payouts + commissions; **Disputes 6 routes, ProviderPaymentMethod 5 routes, Earnings 3 routes not in §4** | Wave 5 |
| Analytics | 49 | ⚠️ §4.11 covers admin analytics; **27 Recommendations routes underspecified (boosts, pins, seasonality, holidays, experiments, photogenic, segments, GDPR, sponsored-click, metrics, itinerary, similar)** | Wave 6 |
| Messaging | 23 + 1 hub | ⚠️ §4.11 covers notifications + support; **Notification templates admin (4 routes) not in §4, device tokens admin not mapped** | Wave 6 |
| Social | 27 | ⚠️ §4.5/§4.11 covers reviews + reports; **Moderation logs, warn/ban/unban (4 routes) not mapped explicitly** | Wave 6 |
| Tracking | 0 | ✅ No HTTP surface | — |

### 21.3 Net-new MVC routes to add post-audit

Add these to §4 in the next revision (grouped by area):

**`/Guide/*` area (new — promote from `/Provider/Guide/*`):**
- `/Guide/Dashboard`, `/Guide/Analytics`, `/Guide/MyTours`
- `/Guide/Profile/{Edit,Avatar,CoverImage,Languages,Specializations}`
- `/Guide/Applications` (list of guide's applications to tours) + `/Guide/Applications/{id}/Withdraw`
- `/Guide/Proposals` + `/Guide/Proposals/{Create,Edit,Submit}` + per-id detail
- `/Guide/Offerings/{tourId}/{Schedules,PricingTiers,PrivateTour,Suspend,Reinstate}`
- `/Guide/Availability` (availability-blocks CRUD)
- `/Guide/Earnings/{Summary,ByTour,History}`
- `/Guide/Tier`
- `/Guide/Agency` (invitations, leave-agency)

**`/Admin/*` area additions:**
- `/Admin/Guides` (list + suspend + reinstate + admin update + admin delete — 5 routes)
- `/Admin/Tours/Proposals` (review queue + approve/reject)
- `/Admin/Tours/{id}/GuideApplications` (review queue + approve/reject)
- `/Admin/Disputes` + `/Admin/Disputes/{id}/{Review,Resolve,Escalate}`
- `/Admin/Notifications/Templates` (4 routes)
- `/Admin/Analytics/{Boosts,Pins,Seasonality,Holidays,Experiments,Photogenic,Segments,Metrics}` (admin discovery operations — 12 routes)
- `/Admin/Translations` (already in §4.9.5 — formalize)
- `/Admin/Attachments` (delete + reorder admin)
- `/Admin/Moderation/{Warn,Ban,Unban}` (4 routes)
- `/Admin/Reports` + `/Admin/Reports/{id}/Resolve` (already partially in §4.5)
- `/Admin/AuditLogs/{Export,Redact}` (Analytics audit-log endpoints)

**`/Account/*` additions:**
- `/Account/Disputes` (user's own disputes)
- `/Account/Devices` (full device token list — currently only in Settings)
- `/Account/PaymentMethods` (already in §4.3 stub for Phase 3; promote)

**Public additions:**
- `/agencies` and `/agencies/{id}` (public agency list/detail — already in audit, not in §4)
- `/recommendations/{similar,for-you,itinerary}` (anonymous discovery surfaces from Analytics)
- `/popular`, `/trending` (already in §4.2 but only via Home — promote to full pages)

### 21.4 Backend gaps blocking UI (rolled-up)

Carried from §13 + new findings from this audit:

1. **Wave-1 leftovers (auth):** `/auth/external/{apple|facebook|google}` (3), `GET /admin/roles/{id}/claims` (1), unified `PUT /admin/users/{id}/status` (1), `DELETE /admin/users/{id}/roles/{roleName}` by name (1) — 6 endpoints.
2. **Wave-2.1 patch:** `GET /api/v1/admin/providers/{id}` single-get for detail page.
3. **Wave-5 partial:** Availability slot reads (6), Refund Policy CRUD (3), Join Request admin (3), Provider booking reads (3) — 15 endpoints.
4. **Wave-7:** all 22 creator endpoints + 5 cross-module handlers + 1 BG service.
5. **Wave-8:** all 17 multi-type-post endpoints + 2 BG services.
6. **New from this audit:** `GET /admin/messaging/templates` admin-only auth gate verification, `POST /admin/social/users/{userId}/warn` notification side-effect verification.

### 21.5 Cross-reference: Pattern Report → this doc

The companion `Agents/Plans/UI-UX-Pattern-Report.md` is **persona/component/pattern oriented**. This doc is **route/controller/wave oriented**. Reading order:
1. Pattern Report §1-3 (personas + IA + page inventory) → matches this doc §4 (route map).
2. Pattern Report §4-9 (wizards, consoles, components, state machines, interactions) → matches this doc §19 (UI-UX rules).
3. Pattern Report §10-13 (empty/loading/error, forms, responsive, a11y) → matches this doc §18 (UI-PERF) + §19 (UI-UX).
4. Pattern Report §14-18 (notifications, templates, recommendations, earnings) → matches this doc §4.10-4.11 (Wave 5-6 details).
5. Pattern Report §19-20 (implementation waves + component inventory numbers) → matches this doc §12 (sprint plan) + §15 (migration steps).

When pattern and route docs conflict, **this document is authoritative for the MVC implementation** (it knows about Razor partials, project structure, area routing). The pattern report is authoritative for **product UX decisions** (it knows about the 483-endpoint capability surface).
