# YallaJo — UI/UX Pattern Report

> **Source:** Derived from the full HTTP surface (483 endpoints + 1 SignalR hub across 13 modules) audited 2025-01-27.
> **Purpose:** Translate the backend's capability shape into a coherent product information architecture, page inventory, component library, and interaction patterns. Every claim here is traceable to a real endpoint.

---

## 1. Personas & Permission Model

The auth system exposes 8 roles in a privilege-tier hierarchy. UI must adapt accordingly.

| Persona | Privilege Tier | Primary Surface | Key Capabilities |
|--------|---|---|---|
| **Guest** (unauthenticated) | 0 | Public catalog | Browse tours/places/blogs/businesses, view reviews, register, login |
| **User** | 10 | Traveler dashboard | Book tours, write reviews, manage favorites, message providers |
| **Creator** (10) | 10 | Creator studio | Author blogs, manage profile, view earnings, link tours |
| **Provider** (10) | 10 | Business console | Manage places/businesses/tours, accept bookings, payouts |
| **TourGuide** (10) | 10 | Guide cockpit | Manage profile, accept guide applications, run availability, earnings |
| **Admin** | 60 | Admin console | Moderate, approve providers/blogs, support tickets |
| **SuperAdmin** | 80 | Admin console + system | All admin + system settings |
| **Owner** | 100 | Full platform | All capabilities |

**Design implication:** A single auth shell with role-aware navigation — DO NOT build separate apps. Roles are additive (a user can be Creator + Provider). Persona switching UI must be persistent (e.g. top-right "Viewing as: Traveler / Provider / Guide" switcher when multiple roles attached).

---

## 2. Top-Level Information Architecture

Every URL ships under `/api/v1/`. The frontend mirrors this with two route trees:

### Public Surface (no auth required)
```
/                         → Home (featured tours, popular places, blogs, search)
/tours                    → Browse tours (search, filter, map, popular, trending)
/tours/{slug}             → Tour detail (incl. children info, packages, reviews, related, weather)
/places                   → Browse places (list, nearby, map viewport)
/places/{slug}            → Place detail (businesses, accessibility, weather, FAQ)
/businesses               → Browse businesses (search, nearby)
/businesses/{slug}        → Business detail (services, amenities, hours, accessibility, reviews)
/blogs                    → Blog index
/blogs/{slug}             → Blog post (comments, reactions)
/creators/{slug}          → Creator profile + their blogs + followers
/guides                   → Browse guides (list, by-slug, their tours)
/guides/{slug}            → Guide profile (specializations, languages, tours, reviews)
/agencies                 → Browse agencies
/agencies/{id}            → Agency detail
/search                   → Unified search (tours + places + businesses + blogs + guides)
/auth/login               → Login
/auth/register            → Sign up
/auth/forgot-password     → Forgot password
/auth/verify-email        → Email verification landing
/auth/reset-password      → Password reset landing
/auth/external-providers/login → OAuth callback
/auth/invitations/accept  → Invitation acceptance landing
/sitemap.xml              → SEO
/sitemaps/{type}.xml      → SEO per-entity sitemap
```

### Authenticated Surface (role-gated)
```
/me                       → My profile (account)
/me/profile               → Edit profile (avatar, cover, bio, marketing-consent)
/me/sessions              → Active sessions (logout / logout-all / revoke)
/me/external-providers    → Linked OAuth accounts
/me/devices               → Trusted devices + push tokens
/me/security              → Change password, change phone
/me/notifications         → Inbox + preferences
/me/favorites             → Saved items
/me/reviews               → My reviews
/me/bookings              → My bookings
/me/payments              → Payment history + invoices
/me/disputes              → My disputes
/me/support               → My support tickets

/become                   → Onboarding hub (Provider / Creator / Guide application entry)

/provider/...             → Provider console (see §5)
/guide/...                → Guide cockpit (see §5)
/creator/...              → Creator studio (see §5)
/admin/...                → Admin console (see §6)
```

---

## 3. Page Inventory (derived from GET endpoints)

### 3.1 Public Catalog Pages

| Page | Endpoint(s) | UX Pattern |
|---|---|---|
| Home | `/popular/tours`, `/popular/places`, `/popular/businesses`, `/trending`, `/tours/featured` | Hero search + content rails (carousels) |
| Tours Browse | `/tours`, `/tours/search`, `/tours/search/suggest` | List/grid + filter sidebar + map toggle + autosuggest |
| Tour Detail | `/tours/slug/{slug}`, schedules, pricing, waypoints, children-info, guides, packages, `/social/reviews/{entityType}/{entityId}`, `/seo/weather/{placeId}`, `/seo/faq/{entityType}/{entityId}` | Multi-section scroll page: gallery → overview → itinerary (waypoints) → schedule picker → pricing → guides → packages → reviews → FAQ → weather → related |
| Places Browse | `/places`, `/places/nearby`, `/places/map/viewport` | Map-first with list overlay, viewport-bound queries |
| Place Detail | `/places/by-slug/{slug}`, businesses list, accessibility, FAQ, weather | Hero + businesses-in-area + accessibility badges + weather widget |
| Businesses Browse | `/businesses`, `/businesses/search`, `/businesses/nearby` | List + filters (type, amenities, accessibility) + map |
| Business Detail | `/businesses/{id}`, services, amenities, hours, accessibility, reviews | Hero + services menu + hours card + amenities chips + reviews |
| Blog Index | `/blogs` | Magazine-style grid with niche filters |
| Blog Detail | `/blogs/{id}`, comments, reactions, blog→tours links | Article + sticky author card + comments + reactions + related |
| Creator Profile | `/blogs/creators/profiles/{slug}`, their blogs, followers | Profile header + follow CTA + blog grid + followers count |
| Guide Profile | `/guides/by-slug/{slug}`, `/guides/{id}/tours` | Profile header + languages/specs badges + tours rail + reviews |
| Agency Browse/Detail | `/agency`, `/agency/{id}` | Agency directory + agency detail with affiliated guides |
| Search Unified | combined of catalog search endpoints | Tabs (All / Tours / Places / Businesses / Blogs / Guides) + facets |

### 3.2 Traveler (User) Pages

| Page | Endpoint(s) | Pattern |
|---|---|---|
| Account Profile | `/accounts/profile` | Form (avatar, name, bio, contact) |
| Favorites | `/social/favorites/`, `/social/favorites/check/...` | Grid with entityType filter (Tours / Places / Businesses) |
| My Bookings | `/booking/my-bookings`, `/booking/{id}` | Status-tabbed list (Upcoming / Confirmed / Completed / Cancelled) → detail with timeline |
| My Reviews | `/social/reviews/my-reviews` | List + edit/delete inline |
| Payment History | `/payments/my-payments`, `/invoices/my-invoices`, `/invoices/{id}/download` | Table with download buttons |
| Disputes | `/disputes/my` | Status-tabbed cards |
| Notifications | `/notifications/`, `/notifications/unread-count` | Inbox with badge + filter (read/unread) + bulk delete |
| Notification Preferences | `/notifications/preferences` | Channel × Type matrix toggle grid |
| Devices | `/devices/tokens` | List of registered devices + revoke |
| Support Tickets | `/support/tickets`, `/support/tickets/{id}` | Ticket list + thread view |
| Sessions | `/auth/sessions` | Device list + revoke + logout-all |

### 3.3 Onboarding Hub `/become`

A single landing that gates application creation:
- **Become a Provider** → `/provider/apply` (multi-step form)
- **Become a Guide** → either `/guides/agencies/{id}/apply` (join agency) OR direct guide path
- **Become a Creator** → creator application

Pattern: 3-card decision page → wizard. Wizards must support **save-as-draft** (every application has a Draft state).

---

## 4. Wizard / Multi-step Form Patterns

Backend reveals 4 long-running stateful wizards. Each follows a **Submit-Review-Resubmit** state machine and needs identical UX scaffolding.

### 4.1 Provider Application Wizard
Endpoints: `POST /provider/register` → `POST /provider/documents` → `POST /provider/apply` → (admin review) → optional `POST /provider/reapply`

States: `Draft → Submitted → MoreDocsRequested → Approved | Rejected → (Reapply within 7d cooling period, max 3 attempts)`

UI requirements:
- Step indicator (Type → Business Info → Documents → Review → Submit)
- Provider type selector (TourOperator / IndependentGuide / HotelResort / ActivityCenter / Agency / BusinessOwner) — each type changes required documents
- Document uploader with type tags + expiry dates + replace flow
- Status banner: "Pending review (since ...)", "More docs requested: <reason>", "Cooling period ends ..."
- Reapply CTA appears only on Rejected status after cooling period

### 4.2 Creator Application Wizard
Endpoints: `POST /blogs/creators/applications` → `PUT .../applications/{id}` → `POST .../applications/{id}/submit`

Same state machine — pull-out reusable `<ApplicationWizardShell>` component.

### 4.3 Tour Proposal Wizard (Guide)
Endpoints: `POST /tours/proposals` → `POST .../submit/{id}` → admin `approve | reject`

States: Draft → Submitted → Approved (creates Tour) / Rejected

### 4.4 Tour Submission Wizard (Provider)
Endpoints: `POST /tours` → schedules → pricing → waypoints → packages → children-info → `POST /tours/{id}/submit` → admin approval

Heavy — needs side-nav with section completion checkmarks + autosave.

---

## 5. Persona Consoles

### 5.1 Provider Console `/provider`

Left nav with these sections (each derived from real endpoints):

| Nav Item | Endpoints |
|---|---|
| Dashboard | `/provider/dashboard/overview`, `/provider/dashboard/pending-actions`, `/provider/dashboard/notifications`, `/analytics/provider/dashboard`, `/analytics/provider/analytics` |
| My Places | `/places` CRUD scoped to ownership |
| My Businesses | `/places/businesses/mine` + business CRUD + hours + amenities + staff + accessibility |
| My Tours | `/analytics/provider/my-tours`, tour CRUD + schedules + pricing + waypoints + packages + children-info |
| Tour Guides | `/tours/{id}/guides` assign/unassign + `/tours/{tourId}/applications` review |
| Bookings | bookings inbound + `/booking/admin/all` (filtered to provider scope) |
| Availability | `/booking/slots` CRUD |
| Payouts | `/payouts/provider`, `/finance/guide/summary` |
| Invoices | `/invoices/provider/my-invoices` |
| Payment Methods | `/provider-payment-methods` CRUD |
| Documents | `/provider/documents` (re-upload, expiry warnings) |
| Settings | `/provider/settings` |

**Dashboard widgets:**
- Pending Actions card (red badge, drives task list)
- KPI tiles (revenue / bookings / avg rating / pending payouts)
- Revenue chart (time-series)
- Recent bookings feed
- Notification stream
- Document expiry warnings

### 5.2 Guide Cockpit `/guide`

| Nav Item | Endpoints |
|---|---|
| Dashboard | `/analytics/guide/dashboard`, `/analytics/guide/analytics`, `/analytics/guide/my-tours` |
| My Profile | `/guides/me`, `/guides/me/avatar`, `/guides/me/cover-image` |
| My Tours | `/analytics/guide/my-tours` + guide-offerings management |
| Applications | `/guides/me/applications`, `/tours/{tourId}/applications` (apply to tours) |
| Tour Proposals | `/tours/proposals` (propose new tours) |
| Availability | `/guides/me/availability-blocks` GET/POST/DELETE |
| Schedules & Pricing | `/tours/{tourId}/guide-offerings/{guideId}/schedules` + `/pricing-tiers` |
| Private Tours | `/tours/{tourId}/guide-offerings/{guideId}/private-tour` |
| Earnings | `/guides/me/earnings/summary`, `/by-tour`, `/history` + `/finance/guide` |
| Bookings | bookings where I'm the guide |
| Tier Progress | `/guides/me/tier` |
| Agency | invitations, leave-agency, apply-to-agency |
| Reviews | reviews about me |

**Cockpit-specific patterns:**
- Calendar widget driven by `availability-blocks` + `schedules`
- Tier badge with progress bar to next tier
- Earnings dashboard with by-tour breakdown
- Agency invitation banner (accept/decline)

### 5.3 Creator Studio `/creator`

| Nav Item | Endpoints |
|---|---|
| Dashboard | followers, blog stats |
| My Blogs | `/blogs/my-blogs` + blog CRUD + submit-for-review + publish/unpublish/archive |
| Drafts | filter on my-blogs |
| Comments Inbox | `/blogs/{id}/comments` + reply/delete |
| Profile | `/blogs/creators/profile/mine` GET/PUT/DELETE + avatar/cover |
| Tier | tier display |
| Earnings | (when monetization ships) |
| Invitations | redeem invitation |
| Followers | profile followers |

### 5.4 Common Console Shell
All three consoles share:
- Same header (workspace switcher, notification bell, profile menu)
- Same left nav structure (collapsible, icon-only on mobile)
- Same KPI tile component
- Same chart components (revenue / bookings / engagement)
- Same notification dropdown wired to `/notifications`
- Same SignalR connection to `/hubs/notifications`

---

## 6. Admin Console `/admin`

Heaviest surface — needs clear category grouping:

### 6.1 Moderation
- **Provider Applications Queue** → `/admin/providers` (approve/reject/request-docs/suspend/reinstate)
- **Creator Applications Queue** → `/blogs/admin/creators/applications`
- **Blog Moderation Queue** → `/blogs/admin/queue` (approve/reject/remove)
- **Review Moderation** → `/social/reviews/admin/flagged`
- **Report Queue** → `/social/reports/admin`
- **Support Tickets** → `/support/admin/tickets/...`
- **Moderation Logs** → `/social/moderation/logs`

### 6.2 User Management
- Users list / detail → `/security/users/...`
- Roles → `/security/roles/...`
- Sessions force-revoke → `/auth/admin/users/.../sessions`
- Reset password, suspend, reactivate, archive, reassign
- Warn/Ban users → `/social/moderation/warn|ban|ban/{id}`
- Audit Logs → `/security/audit-logs`, `/analytics/admin/audit-logs` (with redact + export)

### 6.3 Catalog Curation
- Categories CRUD → `/api/v1/categories/...`
- Tags CRUD → `/api/v1/tags/...`
- Specializations CRUD → `/api/v1/specializations/...`
- Languages CRUD → `/api/v1/languages/...`
- Translations workflow → `/api/v1/translations/...` (translate, batch, approve, backfill)
- SEO Metadata → `/seo/metadata`
- Redirects → `/seo/redirects`
- FAQs → `/seo/faq/...`
- Sitemaps → `/seo/sitemap/...`

### 6.4 Bookings Operations
- All Bookings → `/booking/admin/all` with filters
- Force-Refund → `/admin/bookings/{id}/force-refund`
- Disputes → `/disputes/admin/open` (review/resolve/escalate)
- Payouts → `/payouts/admin/pending`, `/admin/trigger`, `/{id}/approve`
- Commission Rules → `/commissions` CRUD

### 6.5 Finance
- Admin Finance Dashboard → `/finance/admin/dashboard`
- All Payments → `/payments/admin/all`
- Invoices, Payouts, Disputes oversight

### 6.6 Discovery Operations (Analytics)
- Admin Dashboard → `/analytics/admin/dashboard` + revenue / bookings / users
- Boost Packages → `/analytics/admin/boosts` CRUD
- Editorial Pins → `/analytics/admin/pins` CRUD
- Seasonality Rules → `/analytics/admin/seasonality` CRUD
- Holiday Calendar → `/analytics/admin/holidays`
- A/B Experiments → `/analytics/admin/experiments` (create / start / complete)
- Reengagement Segments → `/analytics/admin/segments`
- Photogenic Tagging → `/analytics/admin/entities/{kind}/{id}/photogenic`
- Metrics → `/analytics/admin/metrics`
- Batch Refresh → `/analytics/admin/batches/refresh`

### 6.7 Notification Operations
- Templates → `/admin/notification-templates` CRUD
- Outbox Dead Letters → `/api/v1/ops/outbox/dead-letters` + replay

### 6.8 Tier Management
- Promote/Demote Creator tier → `/blogs/admin/creators/profiles/{id}/promote|demote`
- Promote/Demote Guide tier → guide tier endpoints

### Admin Layout Pattern
- Permanent left nav (3 levels: section → category → page)
- Top utility bar (global search across users/entities, alerts)
- Sticky table headers, virtualized rows for queues
- Bulk actions toolbar on table selection
- Detail drawer pattern for queue items (don't navigate away)
- Audit trail panel on every entity detail

---

## 7. Component Library

### 7.1 Atoms
- StatusBadge (per state machine: ApplicationStatus / BookingStatus / TourStatus / BlogStatus / DisputeStatus / TicketStatus)
- TierBadge (Bronze / Silver / Gold / Platinum)
- RoleChip (provider / guide / creator / admin)
- VerifiedTick, FeaturedStar, SponsoredLabel
- CurrencyAmount (multi-currency; auto-format)
- LanguageFlag, AccessibilityIcon (5 types: Wheelchair / Visual / Hearing / Cognitive / Mobility / Other)
- AmenityChip
- ReactionEmoji
- RatingStars (always 5-star scale)
- AvatarStack (for followers / participants)

### 7.2 Molecules
- EntityCard (Tour / Place / Business / Blog / Guide variants — same skeleton)
- ReviewCard (rating + body + reply + helpful vote + report)
- NotificationItem (icon + title + body + relative time + actions)
- BookingTimelineStep
- DocumentRow (with expiry warning state)
- PriceBreakdown (subtotal / discount / tax / total + commission line for providers)
- AvailabilitySlotCard
- StatusTimeline (renders state machine progress)

### 7.3 Organisms
- SearchBar (global) with autosuggest from `/tours/search/suggest`
- FilterSidebar (faceted, configurable)
- MapView (Leaflet/Mapbox) with viewport-bound queries
- Calendar (availability picker + booking flow)
- WizardShell (multi-step + autosave + section completion)
- DataTable (sortable, paginated, bulk-actionable, virtualized)
- DetailDrawer (slide-in for admin queues)
- KpiTile, ChartCard
- ReviewsBlock (list + filters + sort + ratings histogram)
- CommentsThread (nested reply support)
- NotificationDropdown (bell icon + popover)
- ConsoleShell (header + nav + main + breadcrumbs)
- AdminQueueLayout (table + drawer + bulk toolbar)

### 7.4 Templates
- PublicCatalogTemplate (hero + filters + grid + map)
- EntityDetailTemplate (gallery + tabs + sticky CTA)
- ConsoleTemplate (nav + KPI strip + content)
- AdminTemplate (nav + queue table + drawer)
- WizardTemplate (steps + form + nav buttons + save status)
- AuthTemplate (centered card + brand)

---

## 8. State Machines That Drive UX Copy

Every entity below has a status field rendered as a **StatusBadge** with colour, icon, and tooltip. Copy must reflect the *next user action*, not just the state name.

| Entity | States | UX Implication |
|---|---|---|
| ProviderApplication | Draft / Submitted / MoreDocsNeeded / Approved / Rejected | Draft → "Continue" CTA; MoreDocsNeeded → red banner with reason; Approved → "Go to dashboard" |
| CreatorApplication | Draft / Submitted / MoreInfoRequested / Approved / Rejected | Same pattern as Provider |
| TourProposal | Draft / Submitted / Approved / Rejected | After Approved, show link to created Tour |
| GuideApplication (to tour) | Pending / Approved / Rejected / Withdrawn | Provider sees Pending count badge |
| Tour | Draft / PendingReview / Approved / Suspended / Archived | Provider sees workflow buttons accordingly |
| Blog | Draft / PendingReview / Published / Hidden / Removed / Archived | Creator sees "Submit for review" CTA on Draft |
| Booking | AwaitingPayment / PendingConfirmation / Confirmed / Completed / Cancelled / Rejected / PaymentExpired | Buyer sees countdown for AwaitingPayment; Confirmed shows ticket QR |
| JoinRequest | Pending / Approved / Rejected / Cancelled / Expired | Show 48h countdown when Pending |
| Payment | Pending / Completed / Failed / Refunded | Use Stripe-style status pill |
| Dispute | Open / UnderReview / Resolved / Escalated | Admin sees red dot until resolved |
| SupportTicket | Open / Assigned / InProgress / AwaitingUser / Resolved / Closed | Show SLA countdown badge |
| Notification | Unread / Read | Bold for Unread; collapse Read |
| AgencyInvitation | Pending / Accepted / Declined / Expired | 7-day expiry timer |
| AgencyAffiliation | Active / Terminated | "Leave agency" destructive confirm |
| Review | AwaitingModeration / Published / Removed | Hide AwaitingModeration from public |
| Report | Submitted / UnderReview / Resolved / Dismissed | Admin queue priority |

---

## 9. Interaction Patterns

### 9.1 Search & Discovery
- Single global search bar in header (always visible on public + traveler surfaces)
- Search backed by `/tours/search/suggest` for autocomplete
- Search results page tabs (All / Tours / Places / Businesses / Blogs / Guides)
- Each tab supports faceted filters in sidebar
- Map toggle on Tours / Places / Businesses
- "Popular" + "Trending" rails on Home and Browse pages
- Recommendations rail driven by `/analytics/recommendations`
- "Similar to this" rail on every detail page driven by `/analytics/recommendations/similar/{entityId}`

### 9.2 Booking Flow
1. Tour detail → click "Book"
2. Date picker (loads `/booking/slots`)
3. Participant counter (clamped to tier min/max)
4. Pricing tier selector
5. Add-ons (packages)
6. Login gate (if not authenticated)
7. Contact + special requests
8. Payment redirect (`POST /payments/initiate`)
9. Confirmation page (polling on `/booking/{id}`)
10. Email + push notification on confirm/reject

Critical UX:
- Show **payment expiry countdown** for AwaitingPayment status
- Show **provider response deadline** for PendingConfirmation
- Refund policy snapshot **must** be shown pre-purchase (read from booking entity)
- Cancellation flow shows refund percentage based on cancellation context

### 9.3 Real-time (SignalR)
- Single hub `/hubs/notifications`
- Connects on app boot after auth
- Pushes: new notification, ticket message, booking status change, support assignment, SLA breach
- Frontend reconciles bell badge count without polling
- Reconnect-on-disconnect with exponential backoff

### 9.4 File Uploads
Endpoints accept attachments for: blog attachments, place photos, business photos, tour photos, profile avatars, cover images, documents (provider/guide), service item photos.
Pattern:
- Drag-drop zone with preview
- Progress bar per file
- Type/size validation client-side mirroring server limits (see `AttachmentLimits`)
- Reorder via drag handle (`POST /attachments/.../reorder`)
- Set-primary action on photo grid
- Expiry warning chip for provider documents

### 9.5 Comments & Reactions
- Threaded comments on blogs (1 reply level)
- Emoji reactions on blog + comments
- Optimistic UI for reaction add/remove
- Report action available on every comment/review

### 9.6 Translations
Some admin entities have translation workflow. Pattern:
- Language switcher tabs on edit form
- "Translate with AI" button → calls `POST /translations/translate`
- Approval workflow: translation `AwaitingApproval` → admin approves
- Bulk batch approve for translation queues

### 9.7 Following & Favorites
- Heart icon on every entity card → toggles favorite (`/social/favorites`)
- Follow button on creator profiles → `POST /blogs/creators/profiles/{id}/follow`
- Favorites list grouped by entity type
- Following list shows new content from followed creators

---

## 10. Empty States, Loading, Errors

Standard treatment across every list:

| State | Treatment |
|---|---|
| Loading | Skeleton screens matching final layout (not spinners) |
| Empty (zero results) | Illustration + helpful copy + primary CTA (e.g. "No bookings yet → Browse tours") |
| Empty (filter applied) | "No results for these filters → Reset filters" |
| Error | Inline error card with retry button (do not break layout) |
| Permission denied | Friendly 403 page with "Switch role" or "Request access" CTA |
| Not found | 404 with search bar |
| Network offline | Toast + retry on reconnect |
| Server error (5xx) | Friendly 500 page with support link |

---

## 11. Form Patterns

Backend uses FluentValidation; frontend must mirror exactly to avoid round-trip rejection.

- **Validation timing:** onBlur for individual fields, onSubmit for full form
- **Server errors:** map to field-level errors via `result.Errors` structure
- **Optimistic patterns:** disabled only for destructive actions (delete, cancel)
- **Autosave:** for all wizards; show "Saved 2 min ago" status
- **Confirmation modals:** required for destructive actions (delete, cancel, ban, suspend, force-refund)
- **Multi-currency inputs:** always show currency selector adjacent
- **Phone inputs:** country code picker + format-as-you-type
- **Date pickers:** localized; respect timezone
- **Slug fields:** auto-generate from name with manual override + uniqueness check

---

## 12. Responsive Strategy

| Breakpoint | Behavior |
|---|---|
| ≥ 1280 (desktop) | Full layout with sidebars |
| 768-1280 (tablet) | Collapse filter sidebar to drawer; nav becomes hamburger |
| < 768 (mobile) | Bottom tab bar replaces sidebar on traveler/guide surfaces; cards stack |

**Admin console**: Desktop-first only; show "Use desktop for full admin features" on mobile.

**Public catalog + traveler surface**: Mobile-first; map view becomes full-screen on mobile.

---

## 13. Accessibility & Internationalisation

- All entity catalogs surface accessibility features (places, businesses, tours show 5 a11y feature types as icons)
- Language switcher in header (driven by `/api/v1/languages` GET — active languages only)
- Right-to-left (RTL) support required (Arabic is a likely language given the platform name "YallaJo")
- WCAG 2.1 AA compliance on all interactive elements
- Keyboard navigation throughout (especially admin queues)
- Screen-reader labels on all icon-only buttons
- Focus visible always
- Sufficient colour contrast on status badges

---

## 14. Notification & Messaging UX

Notification system has channels + types + preferences matrix.

- **Channels:** InApp, Email, Push, SMS (per backend NotificationChannel enum)
- **Types:** 50+ types (booking_confirmed, booking_cancelled, guide_offering_suspended, guide_application_submitted, etc.)
- **Preference matrix:** rows = types, columns = channels, cells = toggle
- **Defaults:** sensible per type (critical = all channels, marketing = email only)
- **Quiet hours:** user-configurable in preferences
- **Digest mode:** scaffold exists for daily/weekly summaries

**In-app notification UX:**
- Bell with badge in header
- Dropdown shows last 5 + "See all"
- Inbox page with filter (Read/Unread/All) + bulk delete
- Click notification → navigates to source entity
- Mark-all-read action

**Support ticket UX:**
- Like a lightweight email thread
- Status pill at top
- SLA countdown if Pending
- Reply form at bottom
- Admin can assign / resolve / close
- File attachments in messages

---

## 15. Reusable Page Templates

The 483 endpoints collapse into ~10 reusable templates. Building these well gives 80% of the product.

| Template | Used By |
|---|---|
| **PublicListTemplate** | Tours / Places / Businesses / Blogs / Guides / Agencies browse |
| **PublicDetailTemplate** | Tour / Place / Business / Blog / Guide / Agency detail |
| **AuthCardTemplate** | Login / Register / Forgot / Reset / Verify / OAuth callback / Invitation accept |
| **WizardTemplate** | Provider apply / Creator apply / Tour create / Proposal create |
| **ConsoleDashboardTemplate** | Provider dashboard / Guide dashboard / Creator dashboard |
| **ConsoleListTemplate** | My Bookings / My Reviews / My Favorites / My Blogs / My Tours / etc. |
| **ConsoleDetailTemplate** | Booking detail / Tour detail (provider view) / Blog editor / etc. |
| **AdminQueueTemplate** | All admin moderation queues |
| **AdminCrudTemplate** | All admin catalog management (categories / tags / languages / etc.) |
| **SettingsTemplate** | Profile / Notification prefs / Devices / Sessions / Security |

---

## 16. Cross-Persona Flows (Multi-Role Journeys)

Several flows span personas — UX must handle the handoff gracefully.

| Flow | Sequence |
|---|---|
| **Book a tour with a guide** | User books → Provider confirms → Guide assigned → Guide sees booking → Tour completes → User reviews guide → Guide earnings update |
| **Provider onboarding** | Guest registers → User applies as Provider → Admin reviews → Admin requests docs → Provider re-submits → Admin approves → Provider creates Place → Creates Business → Creates Tour → Submits → Admin approves → Tour bookable |
| **Guide joins agency** | TourGuide applies to Agency → Agency reviews → Agency approves → Affiliation active → Agency can manage Guide's offerings → Guide leaves agency |
| **Dispute resolution** | User opens dispute → Admin marks UnderReview → Admin gathers evidence → Admin resolves (refund/decline/partial/escalate) → User + Provider notified |
| **Support ticket SLA** | User opens ticket → Auto-assigned to admin → SLA timer starts → If SLA approaches breach → System notifies admin → Admin responds → Status transitions → Auto-close on inactivity |

---

## 17. Recommendation & Personalisation Surface

The Analytics module exposes a rich recommendation engine. Front-end must consume it consistently.

- **Onboarding survey** → `POST /analytics/recommendations/onboarding` (after first signup, optional)
- **Recommendation rails** → `GET /analytics/recommendations` (driven by user history)
- **Similar items** → `GET /analytics/recommendations/similar/{entityId}` (on every detail page)
- **"For you" suggestions** → `GET /analytics/recommendations/for/{kind}/{entityId}` (contextual)
- **Itinerary builder** → `GET /analytics/recommendations/itinerary` (multi-day trip planner)
- **"Not interested"** → `POST /analytics/recommendations/not-interested` (3-dot menu on every rec card)
- **GDPR** → User can request data export + deletion + cancel deletion (settings page)
- **Sponsored content** → Cards have explicit `Sponsored` label; clicks tracked
- **Metrics** → Impressions tracked client-side via `POST /analytics/recommendations/metrics`

---

## 18. Provider/Guide Earnings Dashboard

Distinct from generic charts — earnings has specific UX needs:

- Headline KPIs: This month / Last month / All time / Pending payout
- Breakdown by tour
- Booking-level transaction list (paid / pending / refunded)
- Commission line item visible
- Payout schedule + next payout date
- Tax document downloads (invoices)
- Currency consistent per provider/guide

---

## 19. Implementation Phases (Recommended)

Given the surface size (483 endpoints), build in waves:

**Wave 1 — Public Catalog & Auth (visitor-only)**
- Home, Tours/Places/Businesses/Blogs browse + detail
- Search
- Auth flows (register, login, verify, reset)
- Map views
- Reviews read-only
- Blog read

**Wave 2 — Traveler Surface (logged-in user)**
- Favorites, My Reviews
- Booking flow + payment
- My Bookings, Payment History
- Notification inbox + preferences
- Profile management
- Support tickets
- Disputes (read)

**Wave 3 — Onboarding (become a provider/creator/guide)**
- `/become` hub
- All 4 application wizards
- Document uploader
- Status tracking

**Wave 4 — Provider Console**
- Provider dashboard
- Place / Business / Tour CRUD
- Availability + bookings inbound
- Payouts + invoices

**Wave 5 — Guide Cockpit & Creator Studio**
- Guide profile + tours
- Guide offerings (schedules, pricing, private tours)
- Creator blog editor + workflow
- Tier displays

**Wave 6 — Admin Console**
- Moderation queues
- User management
- Catalog curation
- Bookings ops
- Analytics ops (boosts, pins, experiments)

**Wave 7 — Polish**
- Recommendation rails everywhere
- Translation workflow UI
- SEO admin
- Accessibility audit
- i18n + RTL
- Mobile optimisations

---

## 20. Component Inventory Summary

To deliver the full product, expect roughly:
- **~10 page templates** (see §15)
- **~30 organism-level components** (see §7.3)
- **~50 molecule-level components** (see §7.2)
- **~80 atom-level components** (see §7.1)
- **~25 specialised forms** (one per wizard step / settings page)
- **~15 specialised charts** (revenue, bookings, engagement, recs metrics)

A well-tooled design system with these primitives unlocks all 483 endpoints with high reuse.

---

## Appendix A — Endpoint-to-UI Cross-Reference

(Full endpoint inventory remains in compressed blocks `(b59)` Accounts/Auth/Security, `(b60)` Messaging/Social/Booking/Finance/Analytics, `(b61)` Content modules. Use those as the authoritative routing source when implementing.)

---

*Report generated 2025-01-27 from full backend audit. Update whenever the endpoint surface changes (new module / new role / new state).*
