# Phase 3 → 100 % Plan (Growth & Monetization)

**Status:** Phase-3 doc-vs-reality audit complete. ~33 % coverage today (Finance dispute aggregate is the strongest piece; Booking dispute / Social accessibility / Subscriptions / Loyalty / Referrals are missing or stub-only).
**Methodology:** Same pipeline used for Phase 1 + Phase 2 — audit → plan md → TodoWrite → per-workstream implement + build-verify → focused component-level tests → final summary with action items.
**User constraint (m0036, still active):** *“i do not have any payment gateway for now”* — FakePaymentGateway remains the intentional stand-in. Monetization features that depend on a real PSP are **deferred to Phase 3.5**.

---

## Context

Phase 3 in `Agents/YallaJo.md` lists ~35 endpoints across five surfaces:

1. ContentPlaces accessibility (admin CRUD for the AccessibilityFeature catalog).
2. ContentTours packages (multi-tour bundles + lifecycle).
3. Booking *package* bookings + *dispute* flow (Completed ↔ Disputed within 48 h → Resolved).
4. Finance monetization: subscriptions (Provider Free / Basic 29 / Premium 79 / Enterprise 199 JOD/mo + User YallaJo+ 9.99), loyalty (1 pt / JOD, 12-month FIFO expiry, redeem 100 pts = 1 JOD), referrals (50 + 50 pts after referee’s first **completed** booking), dispute management.
5. Social accessibility-reviews surface (separate from generic reviews).

Five parallel explore-agent audits returned with file-and-line confirmation; the consolidated gap map is below. Each row references the actual source files audited (paths under `src/Modules/*` and route paths under `/api/v1/*`).

---

## Scope Decisions

| ID | Gap | Verdict | Workstream | Notes |
|----|-----|---------|------------|-------|
| **G1** | Social accessibility-reviews subsystem | **IN-SCOPE** | WS-2 | Greenfield (~25 files). Spec wants `/api/v1/social/accessibility/reviews`. |
| **G2** | ContentPlaces AccessibilityFeature admin CRUD catalog | **IN-SCOPE** | WS-1 | Assignment endpoints already exist; only catalog mutation missing. Pragmatic approach: surface the static `AccessibilityFeatureType` enum as a *read-only catalog* endpoint + extend assignment commands. (No DB-schema change.) |
| **G3a** | ContentTours `TourPackage` state machine (Submit/Approve/Activate) | **IN-SCOPE** | WS-5a | Entity + CRUD + integration events already shipped; needs lifecycle parity with `Tour`. |
| **G3b** | TourProposal list/get queries (existing `GET /tours/proposals` returns `Array.Empty<object>()`) | **IN-SCOPE** | WS-5b | Trivial — wire stub to real handlers. |
| **G3c** | `TourPackagePricingTier` entity + commands | **DEFER** (Phase 3.5) | — | Base `Price[Money]` field on `TourPackage` covers MVP. |
| **G4a** | Booking dispute lifecycle (`BookingStatus.Disputed`/`Resolved`, OpenDispute/ResolveDispute, events, endpoints) | **IN-SCOPE** | WS-3a | Spec-critical. ~20 files incl. EF migration. |
| **G4b** | `PackageBooking` aggregate + endpoints + Booking-owned `PackageSnapshot` + ContentTours→Booking package-snapshot ingestion | **DEFER** (Phase 3.5) | — | ~40 files, mirrors Phase-1 TourSnapshot work; needs G3a state machine to be meaningful first. |
| **G5a** | Finance Subscriptions (Free/Basic/Premium/Enterprise tiers + recurring billing) | **DEFER** (Phase 3.5) | — | Blocked by user constraint m0036 (no PSP); needs recurring billing infrastructure. `SubscriptionTier` enum tiers are also wrong today (`Basic/Promotion/Enterprise`). |
| **G5b** | Finance Loyalty points (earn / FIFO expire / redeem) | **DEFER** (Phase 3.5) | — | Separate complex subsystem; only `_Deferred` shells exist. |
| **G5c** | Finance Referrals (50 + 50 pts on referee’s first completed booking) | **DEFER** (Phase 3.5) | — | Depends on Loyalty (G5b). |
| **G5d** | Finance `Dispute.Close()` + `DisputeEscalated` event + `DisputeResolved` / `DisputeEscalated` integration events + Messaging consumers | **IN-SCOPE** | WS-4 | Closes the loop on the dispute aggregate that already exists. Mirrors Phase-2 WS-3b `DisputeOpenedHandler` pattern. |
| **G6** | Messaging notification for `TourBookingDisputed` | **IN-SCOPE** | WS-3b | Travels with G4a (Booking dispute event consumer). |

**Defer list summary (Phase 3.5 candidates):** G3c, G4b, G5a, G5b, G5c. All are intentional, scope-aware deferrals — no silent stubbing.

---

## Workstreams

### WS-1 (G2) — ContentPlaces AccessibilityFeature catalog
**Decision:** Surface the existing `AccessibilityFeatureType` enum as a public catalog read endpoint, and add admin commands that operate on the *assignment* table (existing entity), since spec doesn’t require admin-mutable feature *types*.
- [ ] Add `GET /api/v1/places/accessibility/catalog` (AllowAnonymous) → returns IReadOnlyList<AccessibilityFeatureCatalogItem> derived from the enum.
- [ ] Add `ListAccessibilityFeatureAssignmentsQuery` + handler + admin endpoint `GET /api/v1/places/admin/accessibility/assignments` (paginated) [AccessibilityFeature, Read].
- [ ] Add `DeleteAccessibilityFeatureAssignmentCommand` + handler + endpoint `DELETE /api/v1/places/admin/accessibility/{id:guid}` [AccessibilityFeature, Delete].
- [ ] Extend `ContentPlacesPermissionCatalog` to include `AccessibilityFeature/Delete`.
- [ ] Build-verify via lean-ctx_ctx_shell.

### WS-2 (G1) — Social accessibility-reviews subsystem (greenfield)
**Pragmatic minimum-viable shape:** reuse the existing `Review` aggregate's invariants but tag accessibility reviews with an `AccessibilityRating` field instead of standing up a parallel aggregate. Avoids ~15 duplicate files and EF migration churn.
- [ ] Extend `Review` entity with nullable `AccessibilityRating (1-5)`, `AccessibilityNotes (string?)`, `IsAccessibilityReview (bool)`.
- [ ] Add `SubmitAccessibilityReviewCommand` + handler (reuses CreateReview pipeline, eligibility gate already in `BookingEligibilitySnapshot`).
- [ ] Add `GetAccessibilityReviewsQuery` + handler — filters `IsAccessibilityReview = true`.
- [ ] Add `SocialFeatures.AccessibilityReview` constant + permissions Create/Read.
- [ ] Add endpoints under `/api/v1/social/accessibility/reviews` (POST [AccessibilityReview, Create], GET anon).
- [ ] EF migration adding 3 columns to `social.Reviews`.
- [ ] Build-verify.

### WS-3a (G4a) — Booking dispute lifecycle
- [ ] Add `BookingStatus.Disputed` (= 10) + `Resolved` (= 11) members; update permissive transitions.
- [ ] Add `Dispute(reason)` / `Resolve(resolutionNotes)` methods on `TourBooking`; invariants: only `Completed` → `Disputed` within 48 h of `CompletedAt`; only admin can `Resolve`; raise `TourBookingDisputedDomainEvent` and `TourBookingDisputeResolvedDomainEvent`.
- [ ] Add 4 fields to `TourBooking`: `DisputedAt`, `DisputeReason`, `ResolvedAt`, `ResolutionNotes` + EF config + EF migration `AddBookingDisputeFields`.
- [ ] `BookingFeatures.BookingDispute` constant + permissions (Create + UpdateAny).
- [ ] `OpenBookingDisputeCommand` (user-facing) + `ResolveBookingDisputeCommand` (admin) + handlers + validators.
- [ ] `GetBookingDisputesQuery` for admin queue.
- [ ] Endpoints: `POST /api/v1/bookings/{id}/dispute` [BookingDispute, Create] + `POST /api/v1/admin/bookings/{id}/dispute/resolve` [BookingDispute, UpdateAny] + admin queue read.
- [ ] Integration events `TourBookingDisputedIntegrationEvent` + `TourBookingDisputeResolvedIntegrationEvent` in `Booking.Contracts`.
- [ ] Domain→integration handlers in `Booking.Infrastructure/EventHandlers/`.
- [ ] Build-verify.

### WS-3b (G6) — Messaging consumer for TourBookingDisputed
- [ ] `NotificationType.BookingDisputed = 53` (Critical) + `BookingDisputeResolved = 54`.
- [ ] `TourBookingDisputedHandler.cs` + `TourBookingDisputeResolvedHandler.cs` in `Messaging.Infrastructure/EventHandlers/` mirroring `DisputeOpenedHandler` from Phase-2 WS-3b (idempotency via `IMessagingInboxStore`, dual notification: user + provider).
- [ ] Build-verify.

### WS-4 (G5d) — Finance dispute close/escalated lifecycle + Messaging consumers
- [ ] Add `Dispute.Close()` method (terminal status) + `DisputeStatus.Closed`.
- [ ] Add `DisputeEscalatedDomainEvent` (Finance.Domain.Events).
- [ ] Add `DisputeResolvedIntegrationEvent` + `DisputeEscalatedIntegrationEvent` in `Finance.Contracts`.
- [ ] Add `PublishDisputeResolvedHandler` + `PublishDisputeEscalatedHandler` to `FinanceIntegrationConverters.cs`.
- [ ] `NotificationType.DisputeResolved = 55` + `DisputeEscalated = 56`.
- [ ] `DisputeResolvedHandler.cs` + `DisputeEscalatedHandler.cs` in `Messaging.Infrastructure/EventHandlers/`.
- [ ] Build-verify.

### WS-5a (G3a) — ContentTours TourPackage state machine
- [ ] Add `PackageStatus` enum (Draft / Submitted / Approved / Rejected).
- [ ] Add `Submit()` / `Approve()` / `Reject(reason)` methods to `TourPackage`.
- [ ] Add `TourPackageSubmittedDomainEvent` + `TourPackageApprovedDomainEvent` + `TourPackageRejectedDomainEvent`.
- [ ] `SubmitTourPackageCommand` + `ApproveTourPackageCommand` + `RejectTourPackageCommand` + handlers + validators.
- [ ] Endpoints: `POST /api/v1/tours/packages/{id}/submit`, `POST /api/v1/tours/packages/{id}/approve` (admin), `POST /api/v1/tours/packages/{id}/reject` (admin).
- [ ] EF migration adding `Status (byte)` + `SubmittedAt` + `ApprovedAt` + `RejectedAt` + `RejectionReason (string?)`.
- [ ] Map domain→integration events into outbox.
- [ ] Build-verify.

### WS-5b (G3b) — TourProposal list/get queries
- [ ] Add `Queries/TourProposal/Common/TourProposalDto.cs`.
- [ ] `GetTourProposalByIdQuery` + handler.
- [ ] `ListTourProposalsQuery` + handler (admin paginated, take ≤ 200).
- [ ] Replace `Array.Empty<object>()` stub in `TourProposalEndpoints.cs:24-36` with real handler; add `GET /api/v1/tours/proposals/{id:guid}`.
- [ ] Build-verify.

### WS-6 — Tests + full build verify
- [ ] One focused integration test per critical link:
  - Booking dispute round-trip: complete → dispute within 48 h → resolve → assert events emitted (TestUnitOfWork + EF InMemory).
  - Finance dispute lifecycle round-trip: open → review → resolve → escalate → close.
  - TourPackage state machine: Create → Submit → Approve → assert status + events.
- [ ] Full host build `dotnet build "src\Hosts\YallaJo.Api\YallaJo.Api.csproj" --nologo -v q` → 0 errors.
- [ ] Existing Phase-1 critical link test (`tests/Booking.IntegrationTests/TourSnapshotReaderRoundTripTests.cs`) still 2/2 green.

---

## Sequencing

| Day | Workstreams |
|-----|-------------|
| 1 | WS-1 (G2) — smallest, isolated; warm up. WS-5b (G3b) — trivial query wiring. |
| 2 | WS-5a (G3a) — Package state machine + migration. |
| 3 | WS-3a (G4a) — Booking dispute lifecycle + migration (biggest workstream). |
| 4 | WS-3b (G6) — Messaging consumer for booking dispute. WS-4 (G5d) — Finance dispute close/escalated + Messaging consumers. |
| 5 | WS-2 (G1) — Social accessibility-reviews + migration. |
| 6 | WS-6 — Tests + final build verify + final summary delivery. |

Estimated: ~1 week (same as Phase 2). All build-verify steps go through `lean-ctx_ctx_shell` (NOT the Bash tool — Bash wraps via a broken lean-ctx.cmd prefix and 5 trips Powershell).

---

## Definition of Done

- ✅ All seven IN-SCOPE workstreams (WS-1 … WS-5b) shipped, each build-green.
- ✅ Three new EF migrations created (Booking dispute fields, TourPackage status fields, Social accessibility-review columns).
- ✅ At least three focused component-level integration tests for the critical links (booking dispute, Finance dispute, package state).
- ✅ Full `YallaJo.Api` host build green; Phase-1 `TourSnapshotReaderRoundTripTests` still 2/2 green.
- ✅ Final delivery summary to user covering:
  - shipped work per WS,
  - the five DEFER items (G3c, G4b, G5a, G5b, G5c) with explicit Phase-3.5 callouts,
  - Phase-1 & Phase-2 standing items re-surfaced (migration `20260605200222_AddMaxGroupSizeToTourSnapshot.cs` still NOT DB-applied, backfill ops endpoint POST `/api/v1/ops/content-tours/backfill/tour-snapshots` still not invoked in Prod, stale `Agents/endpoint-violations.csv` + stale module-status table, **critical secrets red flag** in all 3 `src/Hosts/YallaJo.Api/appsettings*.json`).

---

## Tooling Notes (carry forward from Phase 1 & 2 — verified)

- Build / test / git / EF: `lean-ctx_ctx_shell(command, cwd)` — **NEVER** the Bash tool (Bash auto-prepends a broken `/c/Users/admin1/AppData/Roaming/npm/lean-ctx.cmd -c …` wrapper that PowerShell rejects).
- All `dotnet ef` commands **MUST** pass `--context <ModuleDbContext>` because the host registers multiple DbContexts.
- File **content** lookups: prefer the standard `Read` tool (not lean-ctx `ctx_read`, which can serve stale compressed stubs).
- File**name** lookups: Bash `Get-ChildItem -Recurse -Filter '*PartialName*.cs'` (lean-ctx_ctx_search matches contents, not filenames).
