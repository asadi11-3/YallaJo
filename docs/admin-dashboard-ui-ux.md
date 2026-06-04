# YallaJo Admin Dashboard, UI/UX Specification

> Companion to `admin-dashboard-endpoints.md`. That file is the API contract; this file is the design contract. Where the two disagree, the endpoint file wins on data and permissions, this file wins on layout and interaction.

## 0. How to read this document

This is a build-ready specification for the admin dashboard surface shared by the Owner, SuperAdmin, and Admin roles. It is written so a frontend engineer can implement screens without inventing structure, and so a designer can review intent without reading code.

The visual language is derived from the existing Webestica "Booking" admin theme already vendored in the repository (Bootstrap 5.3, `data-bs-theme` light/dark, DM Sans + Poppins, primary `#5143d9`). We keep that theme as the source of truth for tokens and components, and we apply a small set of corrections drawn from current design practice: one restrained accent, monospaced numerics in data tables, vector icons only, and a full set of loading, empty, and error states for every data view.

Three rules govern everything below:

1. **Permission-driven, not role-driven.** Nothing in the UI is gated on a role name. Every nav entry, button, tab, and bulk action is rendered from JWT permission claims. The three capabilities that sit above Admin (Outbox ops, hard-delete user, system config) are gated by their specific permissions, never by a hardcoded `if role == "Owner"`.
2. **Read before write.** Each section opens on a list or dashboard that requires only a `*.Read` permission. Mutating affordances appear only when the matching write permission is present.
3. **Every data view ships four states.** Loaded, loading (skeleton), empty (composed, with the path to populate), and error (inline, retryable). A screen without all four is incomplete.

---

## 1. Design foundations

### 1.1 Token reconciliation

The template ships a violet primary. Current dashboard guidance discourages the generic "AI purple" look. We resolve the tension deliberately rather than by reflex: `#5143d9` stays as the single brand accent because it is the platform's existing identity, but it is used as an accent (target under ~10 percent of any given surface), never as a fill for large regions, never as a glow, and never as gradient text. Neutrals carry the surfaces; violet marks the primary action and the active nav state.

| Token | Light | Dark | Usage |
|---|---|---|---|
| `--bs-primary` | `#5143d9` | `#5143d9` | Primary action, active nav, focus ring, selected tab. Accent only. |
| `--bs-primary-rgb` | `81,67,217` | same | For `rgba()` soft tints (`bg-opacity-10`). |
| `--bs-success` | `#0cbc87` | `#0cbc87` | Approved, paid, healthy, confirmed. |
| `--bs-warning` | `#f7c32e` | `#f7c32e` | Pending review, awaiting docs, at risk. |
| `--bs-danger` | `#d6293e` | `#d6293e` | Rejected, banned, refunded, dead-letter. |
| `--bs-info` | `#4f9ef8` | `#4f9ef8` | Informational, in-progress, escalated. |
| Body surface | `#ffffff` | `#212529` | Page background. Do not use pure `#000`; the template's `#0b0a12` charcoal is the darkest ink. |
| Sidebar surface | `#ffffff` | `#0b0a12` | Sidebar and offcanvas. |

Numeric and identifier columns (amounts, counts, IDs, timestamps, percentages) render in a monospaced face so figures align vertically and scan cleanly. The template loads DM Sans and Poppins; add a mono stack scoped to `.font-data` (for example `"DM Mono", ui-monospace, SFMono-Regular, monospace`) for table figures only. Headings stay Poppins, body stays DM Sans.

Sample data in any mockup or fixture must be organic: realistic Jordanian and international names, messy amounts (`47.2 JOD`, not `50.00`), real-looking timestamps. No `John Doe`, no `99.99%`, no `1234567`.

### 1.2 Theme selection

Concrete usage scene: an operations admin reviewing the provider approval queue and the finance dashboard on a 24 inch monitor in a Jordan office, in daylight, switching between dense tables and revenue charts for an hour at a time. That scene favors a light default for legibility of dense tabular data, with dark available via the existing `data-bs-theme` toggle for late shifts and personal preference. So: light is the default, dark is a first-class supported theme, and both are tested for contrast independently. The toggle is the template's existing `#bd-theme` dropdown (light / dark / auto, persisted in `localStorage`).

### 1.3 Iconography

Bootstrap Icons (`bi bi-*`) and Font Awesome (`fa-solid fa-*`) only, both already vendored. No emoji anywhere in the product surface, including notification copy, where the template's demo content uses them. Icon sizing follows the template's `.icon-lg.rounded-circle` for stat tiles and inline `bi` glyphs for actions. One icon style per hierarchy level.

### 1.4 Motion

Transitions are 150 to 300 ms, ease-out. Row hover highlight, tab change, dropdown open, filter apply. No bounce, no layout-property animation (animate `opacity` and `transform` only). Respect `prefers-reduced-motion`: when set, disable non-essential transitions and the notification badge blink. The template's `.animation-blink` on the notification badge must be gated behind this query.

### 1.5 Banned patterns (enforced)

Carried from the loaded design skills and applied to this build:

- No side-stripe accent borders (colored `border-left`/`border-right` over 1px) on cards, list items, or alerts. Use full borders, soft background tints, or leading status badges instead.
- No gradient text, no neon or outer glows, no glassmorphism as a default surface.
- No "hero metric" cliche (one giant number with a gradient accent). Stat tiles are grouped, labelled, and paired with context (trend or progress), per the template's two counter-card variants.
- No identical three-equal-card feature rows as a layout crutch.
- Modals are not a navigation crutch, but they are the standard for short focused CRUD forms. Do not build a separate full page for a simple create or edit (a tag, category, language, specialization, role name and description, invitation, commission rule, or template metadata): open it in a modal over the list. Reserve full pages for complex records (the role claim matrix, the tabbed user detail, the blog post editor, the notification template body editor with live preview). Use inline panels, drawers, and progressive disclosure for everything that is neither a quick form nor a complex record. See 4.7 for the modal CRUD pattern.
- No em dashes in product copy or this document.

---

## 2. Global application shell

Every screen inherits the template's admin chrome. The shell is identical across all 17 sections; only the content region changes.

### 2.1 Structure

```
<main>
  nav.navbar.sidebar            left sidebar, offcanvas on < xl
  div.page-content
    nav.navbar.top-bar          search, theme, notifications, profile
    div.page-content-wrapper    page title row + section content
```

### 2.2 Sidebar (primary navigation)

- Brand: `logo.svg` (light) / `logo-light.svg` (dark) in `.navbar-brand`.
- Menu: `ul.navbar-nav.flex-column#navbar-sidebar`. Top-level entries are `li.nav-item > a.nav-link`; the active entry carries `.active`. Sections with sub-pages use `data-bs-toggle="collapse"` with a nested `ul.nav.collapse.flex-column` bound by `data-bs-parent="#navbar-sidebar"` so only one group is open at a time.
- Group titles use the template's muted label row `li.nav-item.ms-2.my-2`.
- Footer pinned with `mt-auto p-3`: log out and a settings shortcut.

**Permission-driven rendering is mandatory.** Each nav entry is wrapped in a permission check. If the user lacks the section's read permission, the entry is not rendered (not merely disabled). Example contract:

```
hasPermission("Permission.AdminProviderQueue.Read")  -> show "Provider approvals"
hasPermission("Permission.Outbox.Read")              -> show "Ops" (SuperAdmin + Owner only)
hasPermission("Permission.System.Update")            -> show "System config" (Owner only)
```

The navigation information architecture groups the 17 endpoint sections into six menu clusters so the sidebar stays scannable. See section 3.

### 2.3 Top bar

- Sidebar toggler targeting `#offcanvasSidebar` (mobile).
- Global search: `form.position-relative` with `input.form-control.bg-light` and an absolutely positioned submit button carrying a search glyph. Scope: users, bookings, businesses, tickets by ID or name.
- Right cluster `ul.nav.flex-row`:
  - Theme dropdown `#bd-theme` (light / dark / auto).
  - Notification bell: `.nav-notification.btn.btn-light` with `.notif-badge` (blink gated by reduced-motion). Dropdown is a card with header, scrollable `list-group` body, and a footer link to the full activity view. Notification copy uses vector icons and plain text, no emoji.
  - Profile avatar dropdown: `.avatar.avatar-sm`, with profile summary and links. Sign out routes through the auth session endpoints.

### 2.4 Content region

`div.page-content-wrapper.p-xxl-4`. Standard page header is a flex row: `h1.h3` page title on the left, primary action on the right as `a.btn.btn-primary-soft` with a leading `bi` glyph. The primary action is permission-gated and section-specific (for example "Invite creator", "New role", "New template").

### 2.5 Responsiveness and RTL

- Breakpoints tested: 375, 768, 1024, 1440. Sidebar collapses to offcanvas below `xl`. Tables switch to a stacked card list below `md` (see 4.4).
- RTL is first-class. YallaJo serves Jordan and Arabic; the template already ships a complete `rtl/` mirror. The app sets `dir="rtl"` and `lang="ar"` when Arabic is active. Mirror: sidebar moves to the right, chevrons and progress fills flip, numeric and currency columns stay left-to-right inside otherwise RTL rows. Test every screen in both directions.

---

## 3. Information architecture, mapping all 17 sections

The 17 endpoint sections collapse into six sidebar clusters. Each row lists the cluster entry, the endpoint section it covers, the read permission that controls visibility, and the primary screen type.

### Cluster A, Overview

| Nav entry | Endpoint section | Read permission | Primary screen |
|---|---|---|---|
| Dashboard | 3. Analytics and Dashboards (`/admin/dashboard`, `/dashboard/revenue|bookings|users`) | `AdminDashboard.Read` | Analytics dashboard with KPI tiles and charts |
| Activity and audit | 3. Interactions + audit logs (`/admin/interactions`, `/admin/audit-logs`) | `Interaction.Read`, `AuditLog.Read` | Audit log table with redact and export |

### Cluster B, People and access

| Nav entry | Endpoint section | Read permission | Primary screen |
|---|---|---|---|
| Users | 1. Users (RBAC) (`/security` users) | `User.Read` | User list and detail with roles and claims |
| User lifecycle | 2. Auth lifecycle (`/auth/admin/users`, `/auth/invitations`) | `User.UpdateAny` | Sessions, suspend, reassign, invitations |
| Roles | 1. Roles (RBAC) (`/security` roles) | `Role.Read` (implied) | Role list and detail with claim matrix |

### Cluster C, Marketplace operations

| Nav entry | Endpoint section | Read permission | Primary screen |
|---|---|---|---|
| Provider approvals | 5. Provider approvals (`/admin/providers`) | `AdminProviderQueue.Read` | Approval queue |
| Business approvals | 6. Places businesses (`/places/businesses`) | `ContentPlaces.Business.*` + role-policy "Admin" | Approval queue |
| Tour guides | 7. Tour guide mgmt (`/guides/admin`) | `TourGuideProfile.*` | Profile list and detail |
| Tours | 8. Tour moderation (`/tours/admin`) | tour moderation perms | Moderation queue |
| Creators | 9. Creator mgmt (`/blogs/admin/creators`) | `AdminCreatorQueue.Read` | Applications and profiles |

### Cluster D, Content

| Nav entry | Endpoint section | Read permission | Primary screen |
|---|---|---|---|
| Blog moderation | 10. Blog moderation (`/blogs/admin`) | blog moderation perms | Moderation queue with translations |
| Taxonomy | 11. Content taxonomy (`/content-core`) | category/tag read perms | Categories, tags, languages, specializations |
| Recommendations | 4. Recommendations engine (`/analytics/admin`) | `Batch.Read` | Batches, boosts, pins, experiments |
| Notification templates | 16. Notification templates (`/admin/notification-templates`) | `NotificationTemplate.Read` (implied) | Template list and editor |

### Cluster E, Commerce

| Nav entry | Endpoint section | Read permission | Primary screen |
|---|---|---|---|
| Bookings | 12. Bookings (`/admin/bookings`, `/bookings/admin/all`) | `AdminBookingDashboard.Read` | Booking list, detail, overrides |
| Finance | 13. Finance (`/finance/admin`, payouts, disputes, commission) | `AdminFinanceDashboard.Read` | Finance dashboard, payouts, disputes |

### Cluster F, Trust, support, and system

| Nav entry | Endpoint section | Read permission | Primary screen |
|---|---|---|---|
| Moderation | 14. Social moderation (`/social`) | `ContentModerationLog.Read` | Logs, reports, reviews, bans |
| Support | 15. Support (`/support/tickets`) | `AdminSupportQueue.Read` | Ticket queue and detail |
| Ops | 17. Outbox ops (`OpsEndpoints.cs`) | `Outbox.Read` (SuperAdmin + Owner) | Dead-letter management. Hidden from Admin. |
| System config | cross-cutting | `System.Update` (Owner only) | Settings, hidden from non-Owners |

---

## 4. Shared component library

These components are reused across sections. Each maps to existing template markup so implementation is assembly, not invention.

### 4.1 KPI stat tile

Two template variants, used by context.

- **Soft tinted tile** (status-flavored counts, for queues): `card.card-body.bg-{warning|success|primary|info}.bg-opacity-10.border.border-{color}.border-opacity-25.p-4.h-100`, an `h4.font-data` figure, an `h6.fw-light` label, and an `.icon-lg.rounded-circle.bg-{color}.text-white` glyph. Use for "Pending review: 23", "Open disputes: 4".
- **Shadow plus progress tile** (trend metrics, for dashboards): `card.card-body.shadow.p-4`, label, `h3.font-data` figure, `.icon-lg.rounded-circle.bg-primary.bg-opacity-10.text-primary` glyph, an `.progress.progress-xs` bar, and a caption with a colored delta span. Use for revenue, bookings, active users.

Tiles are grouped in a row but never a bare row of three identical cards; pair counts with queues and trends with charts so each tile has a distinct job.

### 4.2 Status badge

Soft background plus matching text, the template pattern: `badge.bg-{color}.bg-opacity-10.text-{color}`. Canonical mapping used platform-wide:

| State | Color | Example labels |
|---|---|---|
| Positive, settled | success | Approved, Active, Paid, Confirmed, Resolved |
| Pending, awaiting | warning | Pending review, Awaiting docs, Suspended (temp) |
| Negative, blocked | danger | Rejected, Banned, Refunded, Dead-letter, Archived |
| In progress, info | info | Escalated, Processing, Under review |
| Neutral | `text-bg-dark` | Draft, Closed, Inactive |

Color is never the only signal: the badge always carries a text label, so colorblind and grayscale readers are covered.

### 4.3 Action affordances

- Row actions: a `btn-round.btn-light` three-dots (`bi-three-dots-vertical`) opening a `dropdown-menu.dropdown-menu-end`. Each item is permission-gated; destructive items (delete, ban, hard-delete) are visually separated and styled with danger text.
- Inline quick actions on queue rows: `btn-sm.btn-primary-soft.px-2` (approve) and `btn-sm.btn-danger-soft.px-2` (reject), shown only with the matching write permission.
- Page primary action: `btn.btn-primary-soft` in the page header.
- Tactile feedback: on `:active`, buttons nudge with `transform: translateY(1px)`; no layout shift.

### 4.4 Data table

The workhorse. Standard structure: a toolbar (search, filters, view toggle), the table, and a pager.

- Toolbar: `form.position-relative` search, filter dropdowns (status, date range via the vendored flatpickr, owner), and an optional grid/list toggle (`nav.nav-pills.nav-pills-dark` with `bi-grid-fill` / `bi-list-ul`).
- Rows: leading identity cell (`.avatar.avatar-sm` plus name and secondary id), data columns in `.font-data`, a status badge column, and a trailing action cell.
- Hover: full-row highlight (background tint), 150 ms.
- Selection: a leading checkbox column enables bulk actions; the bulk action bar appears above the table only when rows are selected and only exposes actions the user is permitted to run.
- Below `md`: each row becomes a stacked card (identity on top, label/value pairs beneath, actions in a footer), the template's responsive pattern.

### 4.5 Detail layout

Detail pages use a two-column composition, not nested cards: a primary column (the entity's core record and its action toolbar) and a secondary column (metadata, history, related items) separated by spacing and `divide-y` rules rather than boxing every block. Tabs (`ul.nav.nav-pills-shadow.nav-responsive`) segment long detail records (for example a user's Profile, Roles, Claims, Sessions, Audit).

### 4.6 Forms

Label sits above input, helper text optional below, error text below the field in danger color. Inputs grouped with consistent spacing. Submit buttons show a loading state (spinner plus disabled) on submit. Destructive forms require a typed confirmation for irreversible actions (hard-delete user, force refund).

### 4.7 CRUD forms in modals

Simple create and edit forms open in a modal over the list they belong to. This is the default for taxonomy entities (tags, categories, languages, specializations), role name and description, invitations, commission rules, and notification template metadata. Do not build a separate Edit page for these.

- Trigger: the list's primary action ("New tag") and each row's Edit action open the same modal component, create mode versus edit mode set by whether a record id is passed.
- Structure: Bootstrap modal (`modal` with `modal-dialog`), a titled header, the form body using the 4.6 rules (label above input, error below), and a footer with a primary Save and a secondary Cancel. The modal is centered and sized to its content (`modal-dialog-centered`, `modal-md` for most forms).
- Loading and result: Save shows the inline button spinner and disables the footer; on success the modal closes and the list row updates in place (or prepends on create) with a brief, reduced-motion-safe highlight; on failure an inline error panel appears inside the modal body and the form stays open with its values intact.
- Focus and keyboard: focus moves to the first field on open and returns to the trigger on close; Escape and the backdrop cancel; the focus trap keeps tabbing inside the modal.
- Edit prefill: edit mode fetches the record (or reuses the row's loaded data) and prefills fields; a small skeleton fills the body while a fetch is in flight.
- RTL: the modal mirrors with `dir="rtl"`; numeric and code fields stay LTR.
- Boundaries: modals are for short forms only. Irreversible actions still use the typed-confirmation pattern (4.6), and complex records (claim matrix, tabbed detail, body-plus-preview editors) stay on full pages. A modal never stacks on another modal.

### 4.8 Charts

ApexCharts (vendored). Revenue and booking trends as area or line; user growth as bar; category mix as donut. Charts get hover tooltips and click-to-zoom. Each chart has its own loading skeleton (a shimmer block at the chart's final height, not a spinner) and an empty state ("No data for this range").

### 4.9 The four states, defined once

Applied to every list, detail, and chart:

- **Loading**: skeleton rows or blocks matching the final layout's dimensions. No generic centered spinner for full views; spinners only for inline button actions.
- **Empty**: a composed panel with a vector icon, a one-line explanation, and the action that populates the view (for example "No pending providers. New applications appear here as providers submit them."). If the user can create the missing data and has permission, the empty state includes the primary action.
- **Error**: an inline, bordered panel with the failure summary and a Retry button. Never a blank screen. Permission errors (403) read as "You do not have access to this view" and the entry should not have been shown in the first place, so a 403 here is a bug signal.
- **Loaded**: the populated view.

---

## 5. Screen specifications, by section

Each section below gives the screen set, the key affordances with their controlling permissions, and any section-specific notes. Endpoints are abbreviated; the authoritative list is `admin-dashboard-endpoints.md`.

### 5.1 Dashboard (section 3, Analytics)

**Screen**: single scrollable analytics page. Permission `AdminDashboard.Read`.

- Top: four KPI tiles from `/admin/dashboard` summary, using the shadow-plus-progress variant: revenue, bookings, active users, conversion. Figures in `.font-data`, each with a period-over-period delta caption.
- Middle: two charts side by side, revenue trend (`/dashboard/revenue`) and bookings trend (`/dashboard/bookings`), with a shared date-range control (flatpickr) and a granularity toggle (day / week / month).
- Lower: user growth chart (`/dashboard/users`) and a compact "recent interactions" list (`/admin/interactions`, gated `Interaction.Read`) linking into the activity view.
- States: each tile and chart carries its own skeleton and empty state; the page does not block on the slowest query.

### 5.2 Activity and audit (section 3, audit logs)

**Screens**: audit log table; interaction drill-down.

- Audit table (`/admin/audit-logs`, `AuditLog.Read`): columns actor, action, target, timestamp (`.font-data`), result badge. Filters: actor, action type, date range. Row click opens a detail drawer with the full event payload.
- Redact action (`POST audit-logs/{id}/redact`, `AuditLog.Redact`): shown in the row action menu only with the permission. Requires a reason and a typed confirmation; redaction is irreversible and itself audited.
- Export (`GET audit-logs/export`, `AuditLog.Export`): a header button that streams the current filtered set; shows a progress toast and a download link on completion.
- Interactions (`/admin/interactions`, `/user/{id}`, `Interaction.Read`): a per-user timeline reachable from the user detail page.

### 5.3 Users (section 1, RBAC users)

**Screens**: user list; user detail (tabbed).

- List (`User.Read`): identity cell, status badge (Active / Inactive), role chips, last active. Filters: status, role, search. Bulk activate/deactivate appears only with `User.UpdateAny`.
- Detail tabs:
  - Profile: core record; Activate/Deactivate toggle (`PATCH`, `User.UpdateAny`).
  - Roles: current roles with remove (`DELETE`, `UserRole.Delete`) and an assign control (`POST`, `UserRole.Create`).
  - Claims: direct claims with add/remove (`POST`/`DELETE`, gated by the corresponding claim permissions).
  - Sessions and Audit: read views linking into lifecycle and audit.
- Hard delete is not here; it lives in lifecycle and is gated `User.DeleteAny` (SuperAdmin + Owner).

### 5.4 User lifecycle (section 2, Auth)

**Screen**: lifecycle actions on a selected user, reachable from Users. Base permission `User.UpdateAny`.

- Force logout: `DELETE` sessions, with a confirmation naming the active session count.
- Reset password: `POST reset-password`, sends a reset, shows a sent confirmation.
- Suspend / reactivate / archive: `PATCH` actions, each with a reason field; status badge updates optimistically with rollback on error.
- Reassign: `POST reassign`, a picker for the new owner.
- Invitations (`/auth/invitations`): a table of pending invites with resend; create invite is the page primary action and opens a modal (4.7) for the invitee email and role, not a separate page.
- Hard delete: a separate, clearly dangerous action rendered only when `User.DeleteAny` is present. Requires typed confirmation of the user's email. Hidden entirely for plain Admins.

### 5.5 Roles (section 1, RBAC roles)

**Screens**: role list; role detail with claim matrix.

- List (`Role.Read`): role name, description, member count (`.font-data`), status. Create role is the page primary action (`POST`, `Role.Create`) and opens a modal (4.7) for the name and description, not a separate page.
- Detail: editable name and description (`PATCH`, `Role.Update`), edited through the same modal from the detail header; a Deactivate control; and a claim matrix, the core of this screen. The matrix stays on the full page (it is a named exception to the modal CRUD rule). The matrix is a grouped checklist of permission claims (feature on the row, actions as columns), with add/remove writing to `POST`/`DELETE` claim endpoints (`RoleClaim.Create` / `RoleClaim.Delete`). Changes batch and save explicitly; a dirty-state bar appears with Save and Discard.

### 5.6 Provider approvals (section 5)

**Screen**: approval queue. Permission `AdminProviderQueue.Read`.

- Queue tabs by status: Pending, Needs docs, Approved, Rejected, Suspended. Counts as soft tinted tiles above.
- Row: provider identity, submitted date, document completeness indicator, status badge.
- Row actions, each permission-gated: Approve, Reject (reason required), Request docs (message), Suspend, Reinstate. Approve and Reject also available as inline quick buttons.
- Detail drawer: submitted documents, business details, and the full action set.

### 5.7 Business approvals (section 6, Places)

**Screen**: approval queue, structurally identical to provider approvals. This is the only section that also checks a named role policy "Admin" in addition to `ContentPlaces.Business.*` permissions; the UI still renders from permissions, the role policy is enforced server-side.

- Actions: Approve, Reject, Request more docs, Suspend, Reinstate (`POST admin/{id}/...`), and Delete (`DELETE {id}`) as a separated destructive action with confirmation.

### 5.8 Tour guides (section 7)

**Screens**: guide profile list; guide detail. Permission `TourGuideProfile.*`.

- List: guide identity, specialization tags, status, rating (`.font-data`).
- Detail: editable profile (`PUT {id}`), Suspend / Reinstate (`POST`), and Delete (`DELETE {id}`, separated, confirmed).

### 5.9 Tours (section 8)

**Screen**: tour moderation queue.

- Row: tour title, provider, submitted date, status, a Feature flag.
- Actions: Approve, Reject, Suspend, Reinstate (`POST {id}/...`); Feature toggle (`PATCH {id}/feature`) as an inline switch reflecting featured state.

### 5.10 Creators (section 9, Blogs)

**Screens**: applications queue; creator profiles. Permission `AdminCreatorQueue.*`.

- Applications: list and detail; Approve / Reject / Request more info (`POST`), each with the appropriate message field.
- Profiles: Suspend / Reinstate / Promote / Demote (`POST`); edit and delete (`GET`/`PUT`/`DELETE {id}`); invite creator (`POST invitations`) as the page primary action.

### 5.11 Blog moderation (section 10)

**Screen**: moderation queue with translation management.

- Queue (`GET queue`): pending posts with Approve / Reject / Remove (`POST`).
- Detail (`GET {id}`): post body plus a Translations panel listing language codes; each language has a view and an edit (`GET`/`PUT` by `languageCode`). A Deleted tab (`GET deleted`) lists removed posts for review.

### 5.12 Taxonomy (section 11, Content core)

**Screens**: categories, tags, languages, specializations, each a managed list.

- Categories: an admin list with deactivate, restore, delete, and reorder on the rows. Create and edit open in a modal (4.7), not a separate page. Reorder is a drag handle on rows writing to the reorder endpoint; the list shows a saving indicator during persistence.
- Translations: a workflow surface (translate, batch, update, approve, backfill, approve-batch) presented as a queue with per-item Approve and a batch Approve action.
- Tags, Languages, Specializations: standard CRUD lists; create and edit use the modal pattern (4.7).

### 5.13 Recommendations (section 4, Analytics admin)

**Screen**: a tabbed operations console. Base read `Batch.Read`.

- Tabs: Batches (refresh and read, `Batch.*`), Boosts (create / set CPC / delete, `BoostPackage.*`), Pins (create / delete editorial pins, `EditorialPin.*`), Seasonality (`SeasonalityRule.*`), Holidays (create and read by year, `HolidayCalendar.*`), Experiments (create / start / complete, `Experiment.*`), and Metrics and segments (read).
- Photogenic flag: a `PUT entities/{kind}/{id}/photogenic` toggle (`Photogenic.Update`) surfaced where entities are listed.
- Experiments tab shows status (draft / running / complete) as badges and exposes Start and Complete as state-appropriate actions.

### 5.14 Notification templates (section 16)

**Screens**: template list; template editor. Permission `NotificationTemplate.*`.

- List: template name, channel, last updated; create is the page primary action (`POST`).
- Editor: subject and body fields with variable tokens, a live preview pane, Save (`PUT {id}`), and Delete (`DELETE {id}`, confirmed). The body editor stays a full screen because of the live preview; it is one of the named exceptions to the modal CRUD rule. Creating a template or editing its metadata only (name, channel) uses the modal pattern (4.7), then opens the full editor for the body.

### 5.15 Bookings (section 12)

**Screens**: booking list; booking detail with admin overrides.

- List (`/bookings/admin/all`, `AdminBookingDashboard.Read`): booking id (`.font-data`), guest, item, dates, amount, status badge. Filters: status, date range, provider.
- Detail: full booking record plus an admin override toolbar gated `AdminBookingDashboard.*`: Confirm, Cancel, Reject, Complete (overrides), and Force refund (`POST {id}/force-refund`, `AdminBookingDashboard.Update`). Force refund requires typed confirmation and a reason; it is visually the most dangerous action on the page.

### 5.16 Finance (section 13)

**Screens**: finance dashboard; payouts; disputes; payments; commission rules; invoices. Base read `AdminFinanceDashboard.Read`.

- Dashboard (`/finance/admin/dashboard`): KPI tiles (gross, net, fees, pending payouts) and a revenue chart.
- Payouts: pending list (`/payouts/admin/pending`) with Trigger (`POST trigger`) and per-item Approve (`POST {id}/approve`). Bulk trigger appears with the write permission.
- Payments: all payments (`/payments/admin/all`), filterable, read-focused.
- Disputes: open list (`/disputes/admin/open`) with Review / Resolve / Escalate (`POST {id}/...`), each capturing notes. Status as badges (Open, Under review, Escalated, Resolved).
- Commission rules: a CRUD list; create and edit a rule open in a modal (4.7), not a separate page. Invoice admin flags: a managed flags panel on invoice detail.

### 5.17 Moderation (section 14, Social)

**Screens**: moderation logs; reports; reviews; bans.

- Logs (`ContentModerationLog.Read`): read table of moderation events.
- Reports: admin list and per-report Resolve (`POST {id}/resolve`).
- Reviews: flagged list with Approve / Remove.
- Bans: Warn and Ban actions (`AdminModerationQueue.Warn` / `Ban`) on a user, and Unban (`DELETE ban/{userId}`). Ban requires reason and duration; the action is danger-styled and confirmed.

### 5.18 Support (section 15)

**Screens**: ticket queue; ticket detail. Permission `AdminSupportQueue.Read` (admins see all tickets).

- Queue: ticket id, subject, requester, priority badge, assignee, status. Filters: status, priority, assignee.
- Detail: conversation thread plus an action rail: Assign (`POST {id}/assign`, with an assignee picker) and Resolve (`POST {id}/resolve`). Reply composer at the bottom; status updates reflect in the badge.

### 5.19 Ops (section 17, Outbox): SuperAdmin and Owner only

**Screen**: dead-letter management. Permission `Outbox.*`. This entry is hidden entirely from plain Admins; it must never render without `Outbox.Read`.

- Dead-letter table: message id (`.font-data`), type, failure reason, attempts, last attempt time, status (Dead-letter danger badge).
- Actions: Retry, Discard, and inspect payload, each `Outbox.*` gated and confirmed. A clear warning banner states that these actions affect live message processing.

### 5.20 System config: Owner only

A settings surface gated `System.Update`. Rendered only for Owners. Out of scope for Admin and SuperAdmin; included here so the nav contract is complete.

---

## 6. Page-to-endpoint mapping

This section binds the work to concrete template files. It has two parts: the 10 admin pages that already ship in the Webestica template (reuse as-is, rewire to YallaJo endpoints), and the new template-style pages to create for the YallaJo sections the template does not cover. Every page lists the endpoints it calls in the form `METHOD path | Permission | UI trigger`.

Conventions: base URL `/api/v1`, `Authorization: Bearer <jwt>` on every call. Each endpoint renders only when its permission claim is present; a `*.Read` call fires on page load, write calls fire on the named UI trigger. Paths are abbreviated per `admin-dashboard-endpoints.md`.

Most pages call more than one endpoint: one read on load, then one write per action affordance. Read the per-page lists below as the full contract for that page, not a single primary call. The table that follows is the at-a-glance index; the count is the number of distinct endpoints wired into that page.

| Page | Type | Section | Endpoints |
|---|---|---|---|
| `admin-dashboard.html` | reuse | 3 | 5 |
| `admin-booking-list.html` | reuse | 12 | 1 (+ query params) |
| `admin-booking-detail.html` | reuse | 12 | 6 |
| `admin-guest-list.html` | reuse | 1 | 3 |
| `admin-guest-detail.html` | reuse | 1, 2 | 12 |
| `admin-agent-list.html` | reuse | 5 | 6 |
| `admin-agent-detail.html` | reuse | 5 | 6 |
| `admin-reviews.html` | reuse | 14 | 3 |
| `admin-earnings.html` | reuse | 13 | 2 |
| `admin-settings.html` | reuse | Owner | 2 |
| `admin-activity-audit.html` | new | 3 | 4 |
| `admin-roles-list.html` | new | 1 | 2 |
| `admin-role-detail.html` | new | 1 | 5 |
| `admin-invitations.html` | new | 2 | 3 |
| `admin-business-list.html` | new | 6 | 7 |
| `admin-guides-list.html` | new | 7 | 6 |
| `admin-tours-list.html` | new | 8 | 6 |
| `admin-creators.html` | new | 9 | 13 |
| `admin-blog-moderation.html` | new | 10 | 8 |
| `admin-taxonomy.html` | new | 11 | 8 plus tabbed CRUD |
| `admin-recommendations.html` | new | 4 | 14 |
| `admin-notification-templates.html` | new | 16 | 4 |
| `admin-payouts.html` | new | 13 | 3 |
| `admin-disputes.html` | new | 13 | 4 |
| `admin-commission-rules.html` | new | 13 | 2 (list plus CRUD) |
| `admin-moderation.html` | new | 14 | 6 |
| `admin-support-list.html` | new | 15 | 1 |
| `admin-support-detail.html` | new | 15 | 3 |
| `admin-ops-outbox.html` | new | 17 | 3 |

### 6.1 Existing template pages (reuse, rewire)

#### `admin-dashboard.html` -> Overview / Dashboard (section 3)
- `GET /admin/dashboard` | `AdminDashboard.Read` | page load, KPI tiles
- `GET /admin/dashboard/revenue` | `AdminDashboard.Read` | revenue chart, on load and date-range change
- `GET /admin/dashboard/bookings` | `AdminDashboard.Read` | bookings chart, on load and date-range change
- `GET /admin/dashboard/users` | `AdminDashboard.Read` | user-growth chart on load
- `GET /admin/interactions` | `Interaction.Read` | recent-activity list on load

#### `admin-booking-list.html` -> Bookings list (section 12)
- `GET /bookings/admin/all` | `AdminBookingDashboard.Read` | page load, table; refetch on filter/sort/page
- (search and status filters are query params on the same call)

#### `admin-booking-detail.html` -> Booking detail and overrides (section 12)
- `GET /bookings/admin/all` (single by id) | `AdminBookingDashboard.Read` | page load, record
- `POST /admin/bookings/{id}/confirm` | `AdminBookingDashboard.Update` | Confirm override button
- `POST /admin/bookings/{id}/cancel` | `AdminBookingDashboard.Update` | Cancel override button
- `POST /admin/bookings/{id}/reject` | `AdminBookingDashboard.Update` | Reject override button
- `POST /admin/bookings/{id}/complete` | `AdminBookingDashboard.Update` | Complete override button
- `POST /admin/bookings/{id}/force-refund` | `AdminBookingDashboard.Update` | Force refund, typed confirmation

#### `admin-guest-list.html` -> Users list (section 1)
- `GET /security/users` | `User.Read` | page load, table; refetch on filter/search/page
- `PATCH /security/users/{id}/activate` | `User.UpdateAny` | row or bulk Activate
- `PATCH /security/users/{id}/deactivate` | `User.UpdateAny` | row or bulk Deactivate

#### `admin-guest-detail.html` -> User detail, tabbed (sections 1 and 2)
- `GET /security/users/{id}` | `User.Read` | page load, profile tab
- `POST /security/users/{id}/roles` | `UserRole.Create` | Roles tab, assign role
- `DELETE /security/users/{id}/roles/{roleId}` | `UserRole.Delete` | Roles tab, remove role
- `POST /security/users/{id}/claims` | claim create perm | Claims tab, add claim
- `DELETE /security/users/{id}/claims/{claimId}` | claim delete perm | Claims tab, remove claim
- `DELETE /auth/admin/users/{id}/sessions` | `User.UpdateAny` | Sessions tab, force logout
- `POST /auth/admin/users/{id}/reset-password` | `User.UpdateAny` | reset password action
- `PATCH /auth/admin/users/{id}/suspend` | `User.UpdateAny` | suspend, reason required
- `PATCH /auth/admin/users/{id}/reactivate` | `User.UpdateAny` | reactivate
- `PATCH /auth/admin/users/{id}/archive` | `User.UpdateAny` | archive
- `POST /auth/admin/users/{id}/reassign` | `User.UpdateAny` | reassign owner
- `DELETE /auth/admin/users/{id}` | `User.DeleteAny` | hard delete, hidden unless permitted, typed email confirmation

#### `admin-agent-list.html` -> Provider approvals queue (section 5)
- `GET /admin/providers` | `AdminProviderQueue.Read` | page load, queue; refetch on status tab
- `POST /admin/providers/{id}/approve` | `AdminProviderQueue.Approve` | row Approve
- `POST /admin/providers/{id}/reject` | `AdminProviderQueue.Reject` | row Reject, reason required
- `POST /admin/providers/{id}/request-docs` | `AdminProviderQueue.RequestDocs` | Request docs, message
- `POST /admin/providers/{id}/suspend` | `AdminProviderQueue.Suspend` | Suspend
- `POST /admin/providers/{id}/reinstate` | `AdminProviderQueue.Reinstate` | Reinstate

#### `admin-agent-detail.html` -> Provider detail (section 5)
- `GET /admin/providers/{id}` | `AdminProviderQueue.Read` | page load, documents and details
- same five `POST` actions as the list page, triggered from the detail action rail

#### `admin-reviews.html` -> Moderation: flagged reviews (section 14)
- `GET /social/reviews/admin/flagged` | `ContentModerationLog.Read` | page load, flagged list
- `POST /social/reviews/admin/{id}/approve` | `AdminModerationQueue.*` | Approve review
- `POST /social/reviews/admin/{id}/remove` | `AdminModerationQueue.*` | Remove review

#### `admin-earnings.html` -> Finance dashboard (section 13)
- `GET /finance/admin/dashboard` | `AdminFinanceDashboard.Read` | page load, KPI tiles and revenue chart
- `GET /payments/admin/all` | `AdminFinanceDashboard.Read` | payments table on load

#### `admin-settings.html` -> System config (Owner only)
- `GET /system/config` | `System.Update` (Owner) | page load, settings form; hidden for non-Owners
- `PUT /system/config` | `System.Update` (Owner) | Save settings

### 6.2 New pages to create (template-style, same shell)

Build these by cloning the closest existing admin page shell. The "Clone from" column names the template file whose layout to copy.

#### `admin-activity-audit.html` (clone from `admin-guest-list.html`) -> Activity and audit (section 3)
- `GET /admin/audit-logs` | `AuditLog.Read` | page load, audit table; refetch on filter
- `GET /admin/interactions/user/{id}` | `Interaction.Read` | per-user interaction drawer
- `POST /admin/audit-logs/{id}/redact` | `AuditLog.Redact` | Redact, reason and typed confirmation
- `GET /admin/audit-logs/export` | `AuditLog.Export` | Export filtered set

#### `admin-roles-list.html` (clone from `admin-guest-list.html`) -> Roles list (section 1)
- `GET /security/roles` | `Role.Read` | page load, table
- `POST /security/roles` | `Role.Create` | New role (page primary action)

#### `admin-role-detail.html` (clone from `admin-guest-detail.html`) -> Role detail and claim matrix (section 1)
- `GET /security/roles/{id}` | `Role.Read` | page load, record and claim matrix
- `PATCH /security/roles/{id}` | `Role.Update` | Save name/description
- `PATCH /security/roles/{id}/deactivate` | `Role.Update` | Deactivate
- `POST /security/roles/{id}/claims` | `RoleClaim.Create` | matrix check, add claim
- `DELETE /security/roles/{id}/claims/{claimId}` | `RoleClaim.Delete` | matrix uncheck, remove claim

#### `admin-invitations.html` (clone from `admin-guest-list.html`) -> Invitations (section 2)
- `GET /auth/invitations` | `User.UpdateAny` | page load, pending invites
- `POST /auth/invitations` | `User.UpdateAny` | Invite (page primary action)
- `POST /auth/invitations/{id}/resend` | `User.UpdateAny` | Resend

#### `admin-business-list.html` (clone from `admin-agent-list.html`) -> Business approvals (section 6)
- `GET /places/businesses` | `ContentPlaces.Business.Read` + role-policy "Admin" | page load, queue
- `POST /places/businesses/admin/{id}/approve` | `ContentPlaces.Business.*` | Approve
- `POST /places/businesses/admin/{id}/reject` | `ContentPlaces.Business.*` | Reject, reason
- `POST /places/businesses/admin/{id}/request-more-docs` | `ContentPlaces.Business.*` | Request docs
- `POST /places/businesses/admin/{id}/suspend` | `ContentPlaces.Business.*` | Suspend
- `POST /places/businesses/admin/{id}/reinstate` | `ContentPlaces.Business.*` | Reinstate
- `DELETE /places/businesses/{id}` | `ContentPlaces.Business.*` | Delete, confirmed

#### `admin-guides-list.html` (clone from `admin-agent-list.html`) -> Tour guides (section 7)
- `GET /guides/admin` | `TourGuideProfile.Read` | page load, list
- `GET /guides/admin/{id}` | `TourGuideProfile.Read` | detail drawer
- `PUT /guides/admin/{id}` | `TourGuideProfile.Update` | Save profile
- `POST /guides/admin/{id}/suspend` | `TourGuideProfile.*` | Suspend
- `POST /guides/admin/{id}/reinstate` | `TourGuideProfile.*` | Reinstate
- `DELETE /guides/admin/{id}` | `TourGuideProfile.*` | Delete, confirmed

#### `admin-tours-list.html` (clone from `admin-agent-list.html`) -> Tour moderation (section 8)
- `GET /tours/admin` | tour moderation read | page load, queue
- `POST /tours/admin/{id}/approve` | tour moderation | Approve
- `POST /tours/admin/{id}/reject` | tour moderation | Reject
- `POST /tours/admin/{id}/suspend` | tour moderation | Suspend
- `POST /tours/admin/{id}/reinstate` | tour moderation | Reinstate
- `PATCH /tours/admin/{id}/feature` | tour moderation | Feature toggle

#### `admin-creators.html` (clone from `admin-agent-list.html`) -> Creators (section 9)
- `GET /blogs/admin/creators/applications` | `AdminCreatorQueue.Read` | Applications tab, list
- `GET /blogs/admin/creators/applications/{id}` | `AdminCreatorQueue.Read` | application detail
- `POST /blogs/admin/creators/applications/{id}/approve` | `AdminCreatorQueue.*` | Approve
- `POST /blogs/admin/creators/applications/{id}/reject` | `AdminCreatorQueue.*` | Reject
- `POST /blogs/admin/creators/applications/{id}/request-more-info` | `AdminCreatorQueue.*` | Request info
- `GET /blogs/admin/creators/{id}` | `AdminCreatorQueue.Read` | Profiles tab, detail
- `PUT /blogs/admin/creators/{id}` | `AdminCreatorQueue.*` | Save profile
- `DELETE /blogs/admin/creators/{id}` | `AdminCreatorQueue.*` | Delete
- `POST /blogs/admin/creators/{id}/suspend` | `AdminCreatorQueue.*` | Suspend
- `POST /blogs/admin/creators/{id}/reinstate` | `AdminCreatorQueue.*` | Reinstate
- `POST /blogs/admin/creators/{id}/promote` | `AdminCreatorQueue.*` | Promote
- `POST /blogs/admin/creators/{id}/demote` | `AdminCreatorQueue.*` | Demote
- `POST /blogs/admin/creators/invitations` | `AdminCreatorQueue.*` | Invite creator (primary action)

#### `admin-blog-moderation.html` (clone from `admin-reviews.html`) -> Blog moderation (section 10)
- `GET /blogs/admin/queue` | blog moderation read | page load, queue
- `GET /blogs/admin/{id}` | blog moderation read | post detail
- `GET /blogs/admin/{id}/translations/{languageCode}` | blog moderation read | translation view
- `PUT /blogs/admin/{id}/translations/{languageCode}` | blog moderation | save translation
- `GET /blogs/admin/deleted` | blog moderation read | Deleted tab
- `POST /blogs/admin/{id}/approve` | blog moderation | Approve
- `POST /blogs/admin/{id}/reject` | blog moderation | Reject
- `POST /blogs/admin/{id}/remove` | blog moderation | Remove

#### `admin-taxonomy.html` (clone from `admin-guest-list.html`, tabbed) -> Content taxonomy (section 11)
- `GET /content-core/categories/admin` | category read | Categories tab, list
- `GET /content-core/categories/admin/{id}` | category read | category detail
- `POST /content-core/categories` | category write | create
- `PUT /content-core/categories/{id}` | category write | update
- `DELETE /content-core/categories/{id}` | category write | delete
- `PATCH /content-core/categories/{id}/deactivate` | category write | deactivate
- `PATCH /content-core/categories/{id}/restore` | category write | restore
- `PATCH /content-core/categories/reorder` | category write | drag reorder
- `POST /content-core/translations/translate` and `/batch` `/update` `/approve` `/backfill` `/approve-batch` | translation perms | Translations tab actions
- Tags, Languages, Specializations CRUD | respective perms | their tabs

#### `admin-recommendations.html` (clone from `admin-earnings.html`, tabbed) -> Recommendations (section 4)
- `GET /analytics/admin/batches` | `Batch.Read` | Batches tab on load
- `POST /analytics/admin/batches/refresh` | `Batch.*` | Refresh batch
- `GET /analytics/admin/metrics` and `/segments` | `Batch.Read` | Metrics tab
- `POST /analytics/admin/boosts` | `BoostPackage.*` | create boost
- `PUT /analytics/admin/boosts/{id}/cpc` | `BoostPackage.*` | set CPC
- `DELETE /analytics/admin/boosts/{id}` | `BoostPackage.*` | delete boost
- `POST /analytics/admin/pins` | `EditorialPin.*` | create pin
- `DELETE /analytics/admin/pins/{id}` | `EditorialPin.*` | delete pin
- `* /analytics/admin/seasonality` | `SeasonalityRule.*` | Seasonality tab CRUD
- `POST /analytics/admin/holidays` | `HolidayCalendar.Create` | add holiday
- `GET /analytics/admin/holidays/{year}` | `HolidayCalendar.*` | holidays by year
- `PUT /analytics/admin/entities/{kind}/{id}/photogenic` | `Photogenic.Update` | photogenic toggle
- `POST /analytics/admin/experiments` `/start` `/complete` | `Experiment.*` | Experiments tab actions

#### `admin-notification-templates.html` (clone from `admin-guest-list.html` + editor) -> Notification templates (section 16)
- `GET /admin/notification-templates` | `NotificationTemplate.Read` | page load, list
- `POST /admin/notification-templates` | `NotificationTemplate.*` | New template (primary action)
- `PUT /admin/notification-templates/{id}` | `NotificationTemplate.*` | Save in editor
- `DELETE /admin/notification-templates/{id}` | `NotificationTemplate.*` | Delete, confirmed

#### `admin-payouts.html` (clone from `admin-earnings.html`) -> Finance: payouts (section 13)
- `GET /payouts/admin/pending` | `AdminFinanceDashboard.Read` | page load, pending list
- `POST /payouts/admin/trigger` | finance write | Trigger payouts (bulk)
- `POST /payouts/{id}/approve` | finance write | row Approve

#### `admin-disputes.html` (clone from `admin-booking-list.html`) -> Finance: disputes (section 13)
- `GET /disputes/admin/open` | `AdminFinanceDashboard.Read` | page load, open disputes
- `POST /disputes/{id}/review` | finance write | Review, notes
- `POST /disputes/{id}/resolve` | finance write | Resolve, notes
- `POST /disputes/{id}/escalate` | finance write | Escalate, notes

#### `admin-commission-rules.html` (clone from `admin-settings.html`) -> Finance: commission (section 13)
- `GET /finance/commission-rules` | `AdminFinanceDashboard.Read` | page load, rules list
- `POST|PUT|DELETE /finance/commission-rules/{id?}` | finance write | rule CRUD

#### `admin-moderation.html` (clone from `admin-reviews.html`, tabbed) -> Social moderation (section 14)
- `GET /social/moderation/logs` | `ContentModerationLog.Read` | Logs tab on load
- `GET /social/reports/admin` | `AdminModerationQueue.*` | Reports tab
- `POST /social/reports/admin/{id}/resolve` | `AdminModerationQueue.*` | Resolve report
- `POST /social/moderation/warn/{userId}` | `AdminModerationQueue.Warn` | Warn user
- `POST /social/moderation/ban/{userId}` | `AdminModerationQueue.Ban` | Ban, reason and duration
- `DELETE /social/moderation/ban/{userId}` | `AdminModerationQueue.*` | Unban

#### `admin-support-list.html` (clone from `admin-booking-list.html`) -> Support queue (section 15)
- `GET /support/tickets` | `AdminSupportQueue.Read` | page load, queue (admins see all)

#### `admin-support-detail.html` (clone from `admin-booking-detail.html`) -> Support ticket detail (section 15)
- `GET /support/tickets/{id}` | `AdminSupportQueue.Read` | page load, thread
- `POST /support/tickets/{id}/assign` | `AdminSupportQueue.*` | Assign, assignee picker
- `POST /support/tickets/{id}/resolve` | `AdminSupportQueue.*` | Resolve

#### `admin-ops-outbox.html` (clone from `admin-booking-list.html`) -> Ops, SuperAdmin and Owner only (section 17)
- `GET /ops/outbox/dead-letters` | `Outbox.Read` | page load, dead-letter table; entire page hidden without `Outbox.Read`
- `POST /ops/outbox/dead-letters/{id}/retry` | `Outbox.*` | Retry, confirmed
- `DELETE /ops/outbox/dead-letters/{id}` | `Outbox.*` | Discard, confirmed

> Note on exact paths: routes marked with `*` or a generic verb follow the pattern in `admin-dashboard-endpoints.md`; confirm the precise segment against the endpoint source before wiring. Permissions shown as `Feature.*` mean the specific `Feature.Action` for that verb (Create/Update/Delete) as defined by `MustHavePermission`.

### 6.3 Build status in the Web layer

Audit of the existing MVC Web host (`src/Hosts/YallaJo.Web`, `Areas/Admin`) against the 29 pages above. The Web Admin area does not mirror the Webestica single-dashboard shape: it uses one controller per entity, each with its own `Index` and `Edit` views and explicit `[HttpGet/HttpPost("admin/...")]` routing, guarded by `[RequirePermission]` on actions and `<permission>` tag helpers in views. The separate `Edit` views for simple entities (Categories, Tags, Languages, Specializations, commission rules, role name and description) should be refactored into list-level modals per 4.7 rather than standalone pages; the controller actions stay the same, only the view delivery changes. The top navigation lives in one shared partial, `Views/Shared/_Navbar.cshtml`, where three permission-gated dropdowns (Security, Accounts, Content) surface the built pages.

Legend: Built means a controller, routes, and views exist and the page is reachable. Surfaced means it is linked from `_Navbar.cshtml`. Partial means a controller or view exists but the feature is thin or incomplete. Not built means no Admin-area controller exists yet.

| Page | Status | Surfaced in nav | Web controller (Areas/Admin) | Notes |
|---|---|---|---|---|
| `admin-guest-list.html` | Built | Yes (Security > Users) | `UsersController` | activate, deactivate, roles, claims |
| `admin-guest-detail.html` | Built | Yes (via Users) | `UsersController` + `LifecycleController` | details, roles, claims, suspend, reactivate, archive, reset-password, reassign |
| `admin-roles-list.html` | Built | Yes (Security > Roles) | `RolesController` | create, update, deactivate |
| `admin-role-detail.html` | Built | Yes (via Roles) | `RolesController` | claim matrix add and remove |
| `admin-invitations.html` | Built | Yes (Accounts > Invite, Resend) | `InvitationsController` | create, resend |
| `admin-activity-audit.html` | Partial | Yes (Security > Audit Timeline) | `AuditLogsController` | read and timeline only; redact and export routes not present |
| `admin-blog-moderation.html` | Built but not surfaced | No | `BlogsController`, `BlogTranslationsController` | full moderation (approve, reject, hide, remove, feature, publish, translations) exists but no nav link; reachable by direct URL |
| `admin-taxonomy.html` | Built, split | Yes (Content dropdown) | `CategoriesController`, `TagsController`, `LanguagesController`, `SpecializationsController`, `TranslationsController` | implemented as five separate pages, not one tabbed page; plus `AttachmentsController`, `EntityCategoriesController`, `EntityTagsController` |
| `admin-business-list.html` | Built (separate Businesses screen) | Yes (Marketplace > Business approvals) | `BusinessesController` | section 6 business approval queue; lookup by place id, then approve, reject, request-more-docs, suspend, reinstate, delete; backed by `BusinessesApiClient` + `BusinessesFacade` |
| (places) | Built | Yes (Marketplace > Places) | `PlacesController` | manages Place entities (not businesses) with verify, feature, delete vocabulary; details, create, edit present; intentionally not renamed |
| `admin-dashboard.html` | Partial | No | `HomeController` (thin) | landing view only, no KPI or chart wiring |
| `admin-earnings.html` | Partial | No | `PaymentsController` (thin) | view plus `PaymentsApiClient`, not a finished finance dashboard |
| (analytics) | Partial | No | `StatisticsController` (thin) | view plus `StatisticsApiClient`, not wired to the analytics endpoints |
| (trips) | Partial | No | `TripsController` (thin) | view plus `TripsApiClient`; unclear mapping to tour moderation |
| `admin-agent-list.html` | Not built | No | none | provider approvals queue; the separate Provider area is provider self-service, not the admin queue |
| `admin-agent-detail.html` | Not built | No | none | as above |
| `admin-booking-list.html` | Not built | No | none | no admin booking override controller in the Admin area |
| `admin-booking-detail.html` | Not built | No | none | as above |
| `admin-guides-list.html` | Not built | No | none | Content `GuidesController` is public; Guide area is guide self-service |
| `admin-tours-list.html` | Not built | No | none | `TripsController` is thin and unconfirmed for moderation |
| `admin-creators.html` | Not built | No | none | Content `CreatorsController` is public creator pages, not the admin queue |
| `admin-recommendations.html` | Not built | No | none | section 4 engine controls |
| `admin-notification-templates.html` | Not built | No | none | section 16 |
| `admin-payouts.html` | Not built | No | none | section 13 finance |
| `admin-disputes.html` | Not built | No | none | section 13 finance |
| `admin-commission-rules.html` | Not built | No | none | section 13 finance |
| `admin-moderation.html` | Not built | No | none | section 14 social moderation |
| `admin-reviews.html` | Not built | No | none | Provider area `ReviewsController` is the provider's own reviews, not admin flagged-review moderation |
| `admin-support-list.html` | Not built | No | none | section 15 |
| `admin-support-detail.html` | Not built | No | none | section 15 |
| `admin-ops-outbox.html` | Not built | No | none | section 17, SuperAdmin and Owner only |
| `admin-settings.html` | Not built | No | none | no admin system-config controller; Accounts and Provider areas have their own settings |

Roll-up (updated after Priorities A through D): the dashboard, analytics, earnings, and trips shells are now finished screens; the Marketplace approvals cluster is built (tours moderation via `TripsController`, creators via `CreatorsController`, guides via `GuidesController`, guide applications via `GuideApplicationsController` with a real backend list query, and business approvals via `BusinessesController`); Places manages Place entities with its own verify, feature, delete vocabulary. Still greenfield and not yet requested beyond Priorities A through D: Commerce (bookings overrides, payouts, disputes, commission), Trust and Ops (social moderation, flagged reviews, support, outbox), notification templates, the recommendations engine, and system config.

Build priorities implied by the gaps:
1. Wire a nav entry for the existing blog moderation pages so the built work is reachable.
2. Promote the four partial shells (dashboard, analytics, earnings, trips) to finished screens against their read endpoints.
3. Start the Marketplace approvals cluster, since provider, guide, tour, and creator queues share the same list-plus-action-rail pattern already proven in `UsersController` and `PlacesController`.
4. Resolved. `PlacesController` keeps its verify, feature, delete vocabulary because it manages Place entities, not businesses, and that is now documented above. The section 6 business approval queue is built separately as `BusinessesController` (approve, reject, request-more-docs, suspend, reinstate, delete), surfaced under Marketplace > Business approvals, so the doc and the code are in sync.

---

## 7. Accessibility and internationalization

- Contrast: body text at or above 4.5:1, large UI glyphs at or above 3:1, verified independently in light and dark. The violet primary on white and the violet active state on the dark sidebar are both checked.
- Focus: every interactive element has a visible focus ring (the primary color), keyboard order matches visual order, and dropdowns and drawers trap and restore focus.
- Color independence: status is always carried by a text label, not color alone.
- Reduced motion: honored globally; the notification blink and row hover transitions are disabled under the query.
- RTL and Arabic: `dir="rtl"`, mirrored layout via the template's `rtl/` build, numeric and currency runs kept LTR inside RTL rows. Date and number formatting localized. Every screen reviewed in both directions and both themes before sign-off.
- Touch and pointer: action targets are comfortably sized; row action menus are reachable without precise hovering on touch.

---

## 8. Build checklist (per screen)

Before a screen is considered done:

- [ ] Nav entry and every action render from permission claims, not role names.
- [ ] Read view requires only the section read permission; write affordances appear only with write permissions.
- [ ] Loading skeleton matches final layout dimensions.
- [ ] Empty state is composed and, where applicable, offers the populate action.
- [ ] Error state is inline and retryable.
- [ ] Numeric and id columns use `.font-data` (mono).
- [ ] Status uses the canonical badge mapping with a text label.
- [ ] Destructive actions are separated, danger-styled, and confirmed (typed confirmation for irreversible ones).
- [ ] No banned patterns (side-stripe borders, gradient text, glassmorphism default, hero-metric, identical 3-card rows, emoji, em dashes). Modals are not used as a navigation default, but short CRUD create and edit forms do use modals per 4.7.
- [ ] Verified at 375 / 768 / 1024 / 1440, in light and dark, in LTR and RTL.
- [ ] Motion within 150 to 300 ms, ease-out, reduced-motion respected.

---

## 9. Self-review against loaded design skills

A short critique pass using the loaded skills' own rubrics, kept honest.

**Philosophy consistency.** The spec commits to one idea: permission-driven, read-before-write, four-states-everywhere, on top of the existing template. It does not drift into a second visual language. Strong.

**Visual hierarchy.** Tiles are paired with context rather than dumped as equal cards; detail pages use spacing and dividers over nested cards; the violet accent marks one thing (primary action and active state). Aligned with the "cards are the lazy answer" and "one accent" guidance.

**Detail execution.** Mono numerics, canonical badge mapping, typed confirmations for irreversible actions, reduced-motion gating of the template's blink, and RTL numeric handling are specified rather than assumed.

**Functionality.** Every one of the 17 sections plus the two above-Admin capabilities is mapped to a screen, an action set, and the controlling permissions, traceable back to the endpoint file.

**Innovation, held in check.** This is a product surface, so restraint is the goal, not novelty. The one deliberate, non-default choice is reconciling the template's violet with the "no AI purple" guidance by demoting it to a strict accent rather than discarding it, which keeps brand continuity while avoiding the cliche.

**Known tension, stated plainly.** The template's stock identity is violet, which design guidance treats as a reflex color for this category. We keep it on purpose for brand continuity and constrain its usage; a future brand refresh could revisit the accent hue without changing any structure in this document.
