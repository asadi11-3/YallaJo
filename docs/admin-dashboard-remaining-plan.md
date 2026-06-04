# Admin Dashboard - Remaining Sections Build Plan

Status: Priorities A-D are complete and build-clean. This plan covers the
remaining greenfield sections from the original scope. Every route and
permission below was verified against the module `*Endpoints.cs` sources.

All slices follow the established pattern: each is
`Models/{X}/{XResponse,XRequest,XVm,XMapper}.cs` + `ApiClients/XApiClient.cs`
+ `Facades/XFacade.cs` + `Controllers/XController.cs` + `Views/X/Index.cshtml`
+ a WebPermission group + an `_AdminSidebar` nav entry, cloning the
ProvidersController gold pattern. Add or extend the WebPermission group and the
sidebar nav before gating. Build clean after each slice.

Role visibility rule (applied to nav gating throughout):
Owner = all; SuperAdmin = all minus System.Update;
Admin = all minus System.Update, User.DeleteAny, Outbox.*.

---

## Phase E - Commerce deepening (Finance + Booking)

Lands in the existing COMMERCE sidebar cluster, beside Earnings.

### E1. Payouts  (Finance module, PayoutEndpoints.cs)
- LIST: `GET /api/v1/payouts/admin/pending` -> perm `Payout.Read`. A real
  cross-entity admin queue (no lookup needed). Good first slice.
- `GET /api/v1/payouts/{id:guid}` detail -> `Payout.Read`.
- `POST /api/v1/payouts/admin/trigger` -> `Payout.Trigger` (no body; runs the
  payout batch). Surface as a top-of-page action button.
- `POST /api/v1/payouts/{id:guid}/approve` -> `Payout.Approve`.
- WebPermission group to add: `Payout` (Read, Trigger, Approve).
- Design: queue table + per-row Approve + a Trigger-batch button.
- Backend: none needed (pure web).

### E2. Disputes  (Finance module, DisputeEndpoints.cs)
- LIST: `GET /api/v1/disputes/admin/open` -> perm `AdminFinanceDashboard.Read`
  (reuses the existing group already added for B2).
- `POST /api/v1/disputes/{id:guid}/review` -> `AdminFinanceDashboard.Update`.
- `POST /api/v1/disputes/{id:guid}/resolve` -> `AdminFinanceDashboard.Approve`
  (body `ResolveDisputeRequest`).
- `POST /api/v1/disputes/{id:guid}/escalate` -> `AdminFinanceDashboard.Update`
  (body `EscalateDisputeRequest`).
- WebPermission group to add: extend `AdminFinanceDashboard` with
  Update + Approve (currently Read only).
- Design: open-disputes queue + per-row review/resolve/escalate (modals for
  reason inputs).
- Backend: none needed.

### E3. Commission rules  (Finance module, CommissionRuleEndpoints.cs)
- `GET /` list, `POST /` create, `PUT /{id}` update, `DELETE /{id}` delete ->
  perms `CommissionRule.Read/Create/Update/Delete`.
- WebPermission group to add: `CommissionRule` (Read, Create, Update, Delete).
- Design: this is true CRUD, so use the modal-CRUD refactor style (list +
  create/edit modals + delete confirm).
- Backend: none needed.

### E4. Booking overrides  (Booking module, AdminBookingEndpoints.cs)
- `POST /{id:guid}/force-refund` -> perm `AdminBookingDashboard.Update`.
- NOTE: AdminBookingEndpoints exposes ONLY the force-refund action; there is no
  admin booking LIST endpoint here. So this is a lookup-by-bookingId + act
  page (mirrors the Trips/Guides lookup pattern), unless a booking list is
  found in TourBookingEndpoints.cs (verify before building).
- WebPermission group to add: `AdminBookingDashboard` (Update; add Read if a
  list endpoint exists).
- Backend: likely needs a small admin booking-list query if a browseable
  queue is wanted (flag for user; like Guide-applications needed).

---

## Phase F - Trust & moderation deepening (Social + Messaging)

Lands in the existing TRUST & MODERATION sidebar cluster.

### F1. Content reports / moderation queue  (Social module)
- ReportEndpoints.cs: `GET /api/v1/reports/admin` list ->
  `AdminModerationQueue.Read`; `POST /api/v1/reports/admin/{id}/resolve` ->
  `AdminModerationQueue.Resolve`.
- ModerationEndpoints.cs: `GET /api/v1/moderation/logs` ->
  `ContentModerationLog.Read`; `POST /warn` -> `AdminModerationQueue.Warn`;
  `POST /ban` + `DELETE /ban/{userId}` -> `AdminModerationQueue.Ban`.
- WebPermission groups to add: `AdminModerationQueue` (Read, Resolve, Warn,
  Ban, Approve, Remove) and `ContentModerationLog` (Read).
- Design: reports queue + resolve; user warn/ban actions; moderation log view.
- Backend: none needed.

### F2. Flagged reviews  (Social module, ReviewEndpoints.cs)
- `GET /api/v1/reviews/admin/flagged` -> `AdminModerationQueue.Read`.
- `POST /api/v1/reviews/admin/{id}/approve` -> `AdminModerationQueue.Approve`.
- `POST /api/v1/reviews/admin/{id}/remove` -> `AdminModerationQueue.Remove`.
- WebPermission: reuses the `AdminModerationQueue` group from F1.
- Design: flagged-reviews queue + per-row approve/remove. Can be a second tab
  or a sibling screen to F1.
- Backend: none needed.

### F3. Support tickets  (Messaging module, SupportTicketEndpoints.cs)
- `GET /api/v1/support/tickets` list -> `SupportTicket.Read`;
  `GET /tickets/{id}` detail -> `SupportTicket.Read`;
  `POST /tickets/{id}/close` -> `SupportTicket.Close`;
  `POST /tickets/{id}/messages` -> `SupportTicket.Read`.
- Admin: `POST /api/v1/support/admin/tickets/{id}/assign` ->
  `AdminSupportQueue.Assign`; `POST .../resolve` -> `AdminSupportQueue.Resolve`.
- WebPermission groups to add: `SupportTicket` (Read, Close) and
  `AdminSupportQueue` (Assign, Resolve).
- Design: tickets list + detail (thread + reply) + assign/resolve actions.
  This is the richest section (real list + detail + message thread).
- Backend: none needed.

---

## Phase G - Ops / System (Messaging + Analytics) - SuperAdmin/Owner gated

New OPS & SYSTEM sidebar cluster. Gate the whole cluster so Admin role does not
see it (Owner/SuperAdmin only), per the role rule.

### G1. Notification templates  (Messaging, NotificationTemplateEndpoints.cs)
- `GET /` list, `POST /` create, `PUT /{id}` update, `DELETE /{id}` delete ->
  `NotificationTemplate.Read/Create/Update/Delete`.
- WebPermission group to add: `NotificationTemplate` (Read/Create/Update/Delete).
- Design: CRUD list + create/edit modals (or a full editor page for the
  template body, since template content can be long - prefer a dedicated edit
  page over a modal for the body field).
- Backend: none needed.

### G2. Recommendations / Analytics admin  (Analytics, RecommendationsEndpoints.cs)
This module is large. Suggested grouping into one Analytics-admin screen with
tabs, or several small slices:
- Batches: `GET/POST /analytics/admin/batches[/refresh]` -> `Batch.Read/Refresh`
  (Batch.Read/Refresh WebPermission already exists from earlier work).
- Boost packages: `POST/DELETE /analytics/admin/boosts` + `/cpc` ->
  `BoostPackage.Create/Delete` (already exists).
- Editorial pins: `POST/DELETE /analytics/admin/pins` ->
  `EditorialPin.Create/Delete` (already exists).
- Also available (add perms as needed): seasonality rules, holiday calendar,
  entity photogenic, experiments, metrics, segments. WebPermission groups to
  add for those: `SeasonalityRule`, `HolidayCalendar`, `Photogenic`,
  `Experiment` (each Create/Update/Delete/Read as used). Metrics/segments reuse
  `Batch.Read`.
- Design: start minimal (batches + boosts + pins, perms already exist), expand
  later. Backend: none needed.

### G3. Outbox  (Ops) - Owner/SuperAdmin only
- No Outbox `*Endpoints.cs` surfaced in the module scan; verify whether an
  outbox/ops admin endpoint exists before planning a screen. If absent, this
  needs backend work (flag for user). Gate strictly to Owner/SuperAdmin.

### G4. System config - Owner only
- No dedicated system-config endpoint surfaced. `System.Update` is the gating
  permission referenced by the role rule. Verify the actual endpoints before
  building; likely needs backend. Owner-only.

---

## Recommended build sequence

1. E1 Payouts (real list, perms simple, pure web) - best starting point.
2. E2 Disputes (real list, extends an existing perm group).
3. F1 Content reports + F2 flagged reviews (shared AdminModerationQueue group).
4. F3 Support tickets (richest; real list + detail + thread).
5. E3 Commission rules + G1 Notification templates (CRUD/modal slices).
6. E4 Booking overrides (lookup-by-id; may need a small backend list query).
7. G2 Recommendations admin (start minimal with existing perms).
8. G3 Outbox + G4 System config (verify/likely backend; Owner/SuperAdmin only).

## Sections that may need backend work (like Guide-applications did)
- E4 Booking overrides: needs an admin booking-list query for a browseable
  queue (otherwise lookup-by-id only).
- G3 Outbox and G4 System config: no admin endpoints surfaced; confirm or build.

All other sections are pure web slices against verified existing endpoints.
