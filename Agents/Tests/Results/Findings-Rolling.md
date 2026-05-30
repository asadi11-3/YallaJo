# YallaJo — Rolling Findings Log

## 🎯 BATCH RESOLUTION 2026-05-30 — 6 perm-gates closed via ConsumerPermissions HashSet edit

The following 6 OPEN findings are **✅ FIXED** by a single edit to `RolePermissionMapping.cs` (added 9 strings to `ConsumerPermissions` HashSet). API restarted, JWT re-issued on next login, all auth gates now pass. Each finding's previous "OPEN" status below is superseded by this resolution.

- **F79 FIXED** — added `"Permission.ProviderApplication.Read"`. Verified: userA `GET /provider/status` → 200 `{status: 0, businessName: 'UserA Adventures'}` (was 403).
- **F87 FIXED** — added `"Permission.Session.Read"`. Verified: userA `GET /auth/sessions` → 200 array.length=45 sessions (was 403).
- **F94 FIXED** — added `"Permission.SupportTicket.Close"`. Verified: userA `POST /support/tickets/{id}/close` → 200 after creating ticket 019e799a-... (was 403).
- **F96 FIXED (auth gate)** — added `"Permission.BlogComment.Create"` + `"Permission.BlogComment.Read"`. Verified: userA `POST /blogs/{id}/comments` now passes auth gate, returns 400 Validation.Content (DTO field `content` not `body` — see F123).
- **F101 FIXED (auth gate)** — added `"Permission.TourProposal.Create"`, `"Permission.TourProposal.Read"`, `"Permission.TourProposal.Submit"`. Verified: userA `GET /tours/proposals` → 200 (was 403). POST passes auth gate, returns 400 Validation.MaxGroupSize (see F124).
- **F117 FIXED (auth gate)** — added `"Permission.Session.Delete"` (covers `/logout`, `/logout-all`, `DELETE /sessions/{id}`). Verified: userA `POST /auth/logout` now passes auth gate, returns 400 (DTO shape issue — see F125).

**Regression:** All GREEN. Admin /finance/admin/dashboard → 200, userA /accounts/profile → 200 after re-login.

---

## F125 — `POST /auth/logout` DTO shape unknown (returns 400 after perm fix)

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** After F117 perm fix (`Permission.Session.Delete` added), userA `POST /api/v1/auth/logout` with empty body → 400. Auth gate passes (no longer 403); now blocked by missing DTO field(s).

**Investigation needed:** Read `Auth.Presentation/Endpoints/Session/SessionEndpoints.cs` line 38 + `LogoutCommand` record signature to determine required fields (likely `refreshToken` or `sessionId`).

---

## F124 — TourProposal POST DTO requires `maxGroupSize` field

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** After F101 perm fix, userA `POST /api/v1/tours/proposals` with `{title, description, placeId, durationMinutes}` → 400 `Validation.MaxGroupSize` 'must be between 1 and 100. You entered 0.'

**Conclusion:** Full DTO includes (at least): `title`, `description`, `placeId`, `durationMinutes`, `maxGroupSize`. Add to adapter docs.

---

## F123 — BlogComment POST DTO uses `content` not `body`

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** After F96 perm fix, userA `POST /api/v1/blogs/{id}/comments` with `{body: 'text'}` → 400 `Validation.Content` 'Content is required.' Field name is `content` (matches Reviews module per F25 family).

**Conclusion:** Standardize naming pattern across modules using user-visible text fields. Reviews, BlogComments, SupportTickets all should use `body` or all `content`. Current: Reviews=`content`, BlogComments=`content`, SupportTickets=`body` (F32).

---

## F122 — Favorites DELETE with empty GUID returns 400 (not 404)

**Severity:** LOW · **Status:** ℹ️ INFO 2026-05-30

**Reproduce:** `DELETE /api/v1/social/favorites/0/00000000-0000-0000-0000-000000000000` → 400. Reasonable since favorite key is invalid pair.

---

## F121 — OTP resend DTO confirmed shape `{email, purpose}` with STRING purpose

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `POST /api/v1/auth/resend-otp` with `{email, purpose: 'EmailVerification'}` → 200. With `{type, otpPurpose, etc.}` variants → 400 `Validation.Purpose`. Resolves F113. Real DTO: `email` + `purpose` (string enum: EmailVerification, PasswordReset, etc.).

---

## F120 — Parallel 30 GETs from JS Promise.all all return non-200/non-429

**Severity:** LOW · **Status:** ❓ INVESTIGATE 2026-05-30

**Reproduce:** `Promise.all(30 × GET /security/me)` → 30 responses, 0×200, 0×429, all 'other'. Possibly browser network throttling, fetch concurrency limit, or browser-side rate limiter rejecting before HTTP. Not necessarily a server-side bug.

---

## F119 — `/api/v1/places` ignores Accept-Language for translated name

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `GET /api/v1/places` with `Accept-Language: en | ar | es | xx-XX` all return identical `name: 'Petra Archaeological Park'`. Translations seeded but only default English surfaced. Workflow §10 mentions multi-language support; places list may need translation resolution layer.

---

## F118 — Admin `/security/users?role=Admin` filter ignored

**Severity:** LOW · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** `GET /api/v1/security/users?role=Admin` returns 20 items (page default) but they're a mix of all roles. Filter parameter accepted by validator but ignored by handler.

**Suggested fix:** Apply role filter in `GetUsersQueryHandler`.

---

## F48 — `POST /auth/forgot-password` returned 500 for existing emails (CRITICAL password recovery broken)

**Severity:** HIGH · **Status:** ✅ FIXED 2026-05-30 (root cause IDENTICAL to F1 family)

**Reproduce (pre-fix):** `POST /api/v1/auth/forgot-password` with `{email: 'userA@yallajo.test'}` → **500** with detail `Integration event type 'Auth.Contracts.IntegrationEvents.PasswordResetTokenIssuedIntegrationEvent' is not registered in IntegrationEventTypeRegistry`. Non-existent emails returned 200 correctly (no enumeration). Existing emails broke completely. Password recovery 100% non-functional for actual users.

**Root cause:** Same anti-pattern as F1 (TourGuideRegisteredIntegrationEvent missing). `PasswordResetTokenIssuedIntegrationEvent` is raised by the forgot-password command handler → persisted to outbox → OutboxProcessor.cs:128 tries to wrap it in `IntegrationEventNotification<TEvent>` via `IntegrationEventTypeRegistry.TryGetType(message.Type, out eventType)` → FAILS because the event wasn't registered → throws InvalidOperationException → 500.

**Fix:** Added 1 line to `YallaJo.SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs` Auth section: `["auth.password-reset.token-issued.v1"] = typeof(PasswordResetTokenIssuedIntegrationEvent)`. Auth.Contracts.IntegrationEvents namespace already imported at line 3.

**Verified post-restart 2026-05-30:** Both `POST /auth/forgot-password` with existing email AND non-existent email return **200** `{message: 'If this email exists, a reset code was sent.'}` (identical messages — no enumeration). Regressions F4/F9/F17/F18 + F23 cancel + F45 dashboards all 200.

---

## F117 — `POST /auth/logout` returns 403 for User role

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as userA → `POST /api/v1/auth/logout` → 403. User cannot log out via API. Likely missing `Permission.Session.Revoke` (or similar) in ConsumerPermissions.

**Suggested fix:** Add appropriate permission to ConsumerPermissions HashSet after identifying real permission name.

---

## F116 — `POST /auth/refresh` returns 401

**Severity:** LOW · **Status:** ❌ OPEN 2026-05-30 (investigation needed)

**Reproduce:** Login as userA, store refreshToken, immediately POST /auth/refresh with that token → 401. May be adapter token format issue or genuine bug.

**Investigation:** Inspect refresh token format in stored adapter state vs RefreshTokenCommand expected DTO shape.

---

## F115 — Notification template POST DTO `type` field is INT enum, not string

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `POST /api/v1/admin/notification-templates` with `{name, type: 'in_app', ...}` → **400 JsonException** at `$.type`. Same F26 family of broken enum-from-string body binding.

---

## F114 — Missing security/observability response headers

**Severity:** HIGH · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** GET /api/v1/places response headers — ALL of these missing: `Content-Security-Policy`, `X-Frame-Options`, `Strict-Transport-Security`, `X-Content-Type-Options`, `X-XSS-Protection`, `traceparent` (W3C observability), `X-Correlation-ID`. Production deployment security posture is incomplete; observability cannot trace requests across services.

**Suggested fix:** Register security headers middleware (`Microsoft.AspNetCore.SecurityHeaders` or custom) in `YallaJo.Api/Program.cs`. Add OpenTelemetry trace context propagation middleware. Set CSP appropriately (allow inline scripts only if needed for Swagger UI).

---

## F113 — POST /auth/resend-otp DTO shape unknown

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30 · **Module:** Auth

POST /auth/resend-otp with `{email, purpose: 1}` → 400 'Failed to read parameter ResendOtpRequest from JSON'. DTO field names different from expected.

---

## F112 — /payouts/provider 403 for admin (correct by design)

**Severity:** LOW · **Status:** ✅ NOT_A_BUG 2026-05-30 · **Module:** Finance

Admin GET /payouts/provider → 403 `Payout.OwnerMismatch` 'Provider claim required'. Admin doesn't have provider claim so can't query own-payouts endpoint. Should use admin-specific path /admin/payouts (404 — separate F111).

---

## F111 — /payouts/admin 404

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30 · **Module:** Finance

GET /api/v1/payouts/admin → 404. Real admin payout listing path needs inventory.

---

## F110 — /credit-notes 404

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30 · **Module:** Finance

GET /api/v1/credit-notes → 404. Real path needs inventory or feature not exposed.

---

## F109 — Finance admin force-refund 404

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30 · **Module:** Finance

POST /api/v1/finance/admin/payments/{id}/force-refund → 404. Real path differs.

---

## F108 — CORS preflight OPTIONS 405

**Severity:** LOW · **Status:** ❌ OPEN 2026-05-30 · **Module:** API/Infrastructure

OPTIONS /api/v1/places with Access-Control-Request-* headers → 405. No CORS preflight handler registered. Affects browser-based cross-origin clients.

---

## F107 — /guides/me/applications requires ?page query param

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30 · **Module:** ContentTours

GET /guides/me/applications → 400 'Required parameter int page'. Endpoint EXISTS but requires `?page=` (consistent with other paginated endpoints). Doc should note pagination is mandatory.

---

## F106 — Tour application DTO uses `message` not `notes`

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30 · **Module:** ContentTours

POST /tours/{id}/applications with `{notes}` → 400 Validation.Message 'Message must not be empty'. Real field name is `message`.

---

## F105 — POST /tours/admin/{id}/suspend on Draft tour returns 500 NRE

**Severity:** HIGH · **Status:** ❌ OPEN 2026-05-30 · **Module:** ContentTours

Admin POST /tours/admin/{tourId}/suspend on a Draft tour → **500 NRE**. Should return 409 `Tour.InvalidTransition` (same pattern as approve which correctly returns 409). The handler doesn't guard against invalid state before dereferencing.

**Same family as F104** — both reject/suspend lack the state-machine guard that approve has.

**Suggested fix:** Add `Result.Failure(Error("Tour.InvalidTransition", $"Cannot suspend a tour with status {tour.Status}. Required: Approved/Published."), Outcome.Conflict)` in `SuspendTourCommandHandler` before any property access.

---

## F104 — POST /tours/admin/{id}/reject on Draft tour returns 500 NRE

**Severity:** HIGH · **Status:** ❌ OPEN 2026-05-30 · **Module:** ContentTours

Admin POST /tours/admin/{tourId}/reject on a Draft tour → **500 NRE**. Should return 409 `Tour.InvalidTransition` (same pattern as approve which correctly returns 409 'Cannot approve a tour with status Draft. Required: Pending.'). The handler doesn't validate state before NRE-ing on a missing field.

**Suggested fix:** Add `Result.Failure(Error("Tour.InvalidTransition", $"Cannot reject a tour with status {tour.Status}. Required: Pending."), Outcome.Conflict)` in `RejectTourCommandHandler` before any property dereferencing.

---

## F103 — `GET /api/v1/auth/external-providers` returns 405 Method Not Allowed

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** userA `GET /api/v1/auth/external-providers` → 405. Endpoint at this path is POST-only (likely for linking new external providers, not listing).

**Impact:** No way to list own linked external providers via this path. Real listing endpoint TBD. Doc divergence.

---

## F102 — JoinRequest endpoints 404 at multiple guessed paths

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** Tried 4 paths:
- `GET /api/v1/tours/{id}/join-requests` → 404
- `GET /api/v1/join-requests/mine` → 404
- `GET /api/v1/booking/join-requests/mine` → 404
- `POST /api/v1/tours/{id}/join-requests` → 404

**Impact:** JoinRequest feature path TBD. Workflow plan mentions JoinRequest as a feature but real path not yet discovered. Doc divergence — Master scenarios doc needs path update.

---

## F101 — User role cannot create or list TourProposals (perm gap)

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as userA → `GET /api/v1/tours/proposals?page=1&pageSize=10` → 403. `POST /api/v1/tours/proposals` with valid body → 403. Admin gets 200 on the list.

**Root cause hypothesis:** `ConsumerPermissions` HashSet missing `Permission.TourProposal.Create` and `Permission.TourProposal.Read`. Workflow plan §15 (Tour Proposals) says ANY user can submit proposals; this is currently broken at the perm gate.

**Suggested fix:** Add to `ConsumerPermissions` in `Security.Infrastructure/Seeding/RolePermissionMapping.cs`:
- `"Permission.TourProposal.Create"`
- `"Permission.TourProposal.Read"`

Then restart API. Same family as F30/F33/F44/F53/F69/F79/F87/F94/F96/F100 — accumulating perm gaps that need a single batch fix.

---

## F100 — Currency filter not validated, silently ignored

**Severity:** LOW · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** `GET /api/v1/tours?currency=XYZ` → 200 with same items as `currency=JOD` (1 result). Invalid currency code silently ignored, query falls through to default filter.

**Impact:** Either accept and document the silent-ignore behavior OR reject with 400 Validation.Currency. Currently inconsistent with other validations (e.g. pageSize bound errors).

---

## F99 — Blog comments path uses ID not slug (F89 correction)

**Severity:** INFO · **Status:** ✅ NOT_A_BUG 2026-05-30

**Verdict:** F89 said `/blogs/comments/{slug}` 404. The CORRECT path is `/api/v1/blogs/{blogId}/comments` — uses blog GUID, not slug. Returns 200 with 2 seeded comments. Doc-only correction.

---

## F98 — Guide profile self-update path differs

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `PUT /api/v1/guides/me/profile` as guide-approved → 404. `GET /api/v1/guides/me` works fine. Real update path may be `PUT /guides/me`, `PUT /guides/{id}`, or `PATCH /guides/me`.

**Impact:** Cannot update own bio/yearsOfExperience/hasFirstAid until canonical path is found. Workflow §1 says guides update own profiles.

---

## F97 — Blog reaction POST path differs

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `POST /api/v1/blogs/{blogId}/reactions` → 404. Real path differs (likely `POST /blogs/{id}/react` or `PUT /blogs/{id}/reaction`). Blog `reactionCount` field not exposed on details response either.

---

## F96 — User cannot post blog comments

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as userA → `POST /api/v1/blogs/{blogId}/comments` with `{content: "Great article!"}` → **403 Forbidden** despite 2 comments existing in DB.

**Root cause hypothesis:** `Permission.BlogComment.Create` missing from `ConsumerPermissions` HashSet. Same family as F23/F30/F33/F44/F53/F69 fixed earlier. Real perm name needs lookup via `Comment.Create` or `BlogComment.Create` in `ContentBlogs.Contracts/Authorization/ContentBlogsFeatures.cs`.

**Suggested fix:** Add real permission to `ConsumerPermissions` in `Security.Infrastructure/Seeding/RolePermissionMapping.cs`. Same perm-gap pattern.

---

## F95 — Admin support tickets list path 404

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `GET /api/v1/support/admin/tickets?page=1&pageSize=10` as admin → 404 Not Found. Individual admin actions work (`/admin/tickets/{id}/assign`, `/admin/tickets/{id}/resolve` both 200) but the listing endpoint is absent.

**Impact:** Admin must know ticket IDs out-of-band to manage them. No global queue view via REST. Real path may be at `/api/v1/admin/support` or differ. Needs swagger inspection.

---

## F94 — User cannot close own support ticket

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as userA → create ticket (200) → `POST /api/v1/support/tickets/{ownTicketId}/close` → **403 Forbidden** with bare `{type, title, status, traceId}` (no detail). Admin actions on the same ticket (`/admin/tickets/{id}/{assign,resolve}`) succeed.

**Root cause hypothesis:** Either (a) close action requires admin permission (workflow design), or (b) `Permission.SupportTicket.Close` missing from `ConsumerPermissions` HashSet. Same family as F23/F30/F33/F44/F53/F69 fixed earlier. Workflow §12 likely allows user to resolve own ticket.

**Suggested fix:** Add `"Permission.SupportTicket.Close"` (or actual name from `SupportTicketEndpoints.cs:close`) to `ConsumerPermissions` in `Security.Infrastructure/Seeding/RolePermissionMapping.cs`. If admin-only by design, document and remove from user-facing API.

**Verified workaround:** Admin resolve via `POST /admin/tickets/{id}/resolve` with `{resolution: "..."}` → 200 then GET ticket shows status="Resolved".

---

## F93 — Weather endpoint returns `Weather.NotFound` for any coords (no data populated)

**Severity:** LOW · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** `GET /api/v1/seo/weather?lat=30.3285&lng=35.4444` (Petra) → 404 `Weather.NotFound`. Bad coords lat=200/lng=200 → same 404 (no validation).

**Verdict:** Endpoint exists but either weather data not seeded OR external provider not configured in dev. Per workflow §11 Weather: 12h cache TTL, 7-day forecast, IWeatherProvider, 1000 calls/day budget.

**Suggested fix:** Either seed weather data for default tour places OR configure dev IWeatherProvider OR add explicit "weather data unavailable" 503 response.

---

## F92 — `POST /api/v1/tours/{tourId}/guide-offerings` returns 405 Method Not Allowed

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** Login as guide → `POST /api/v1/tours/{tourId}/guide-offerings` with `{languageIds, notes}` → 405. GET on same path → 200 with offerings list.

**Likely cause:** Verb mismatch — endpoint registered as PUT instead of POST.

---

## F91 — `POST /api/v1/social/reports` create returns 400 (DTO shape mismatch)

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `POST /api/v1/social/reports` with `{entityType, entityId, reason, description}` → 400 Bad Request. Real DTO probably uses `targetType`/`targetId` (matching F25 Reviews pattern) or different field names.

**Impact:** Doc-only. Admin lifecycle (resolve) DOES work (verified r4 200). Only the create payload shape is unclear.

---

## F90 — `/api/v1/seo/hreflang/{slug}` returns 404

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** Login as admin → `GET /api/v1/seo/hreflang/petra-full-day-explorer` → 404. Workflow §8 SEO references hreflang generation.

**Likely cause:** Path differs (maybe `/api/v1/seo/hreflang?slug=...` query-based or `/api/v1/seo/{slug}/hreflang`).

---

## F89 — Blog comments at `/api/v1/blogs/{slug}/comments` returns 404

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** Anon `GET /api/v1/blogs/petra-sunrise-practical-tips/comments` → 404. UserA POST same path → 404. Workflow §9 Blog/Content mentions comments nested 2 levels deep.

**Likely cause:** Path differs (maybe `/api/v1/blogs/{id}/comments` with GUID not slug, or `/api/v1/blog-comments?blogSlug=...`).

---

## F88 — `/api/v1/languages` and `/api/v1/languages/active` return 404

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** Anon `GET /api/v1/languages` → 404. `GET /api/v1/languages/active` → 404. Languages used in I18n tests work (Accept-Language headers honored) but admin/list endpoint not at expected path.

**Likely cause:** Path is at `/api/v1/seo/languages` or `/api/v1/content-core/languages` based on translation-cache pattern.

---

## F87 — User role cannot view own `/api/v1/auth/sessions`

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as userA → `GET /api/v1/auth/sessions` → 403 Forbidden. Admin GET same endpoint → 200. User should see own active sessions for security awareness ("which devices am I logged in on").

**Root cause:** `ConsumerPermissions` HashSet lacks the permission for self-session-read. Same family as F30/F33/F44/F53/F69/F79.

**Suggested fix:** Find real permission name (likely `Permission.Session.Read` or `Permission.AuthSession.ReadOwn`) by inspecting `SessionEndpoints.cs`, then add to `ConsumerPermissions`.

---

## F86 — Discount/promotion endpoints not exposed via REST (workflow §24 should be built)

**Severity:** LOW · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** `GET /api/v1/discounts/active`, `/discounts/mine`, `POST /api/v1/discounts` → all **404**.

**Workflow says:** §24 Discount & Promotions: detailed feature (Percentage/Fixed/EarlyBird/LastMinute/GroupSize types; provider creates → Pending → admin approve; max 95%, min 5 JOD floor; 2-stack max).

**Likely cause:** Either endpoints at non-standard path (under `/social/discounts`?), or feature not built.

**Impact:** Booking pricing cannot use discount codes via REST. Workflow §24 feature inaccessible.

---

## F85 — Audit log endpoint paths discovered (`/admin/audit-logs` + `/security/audit-logs`)

**Severity:** INFO · **Status:** ✅ NOT_A_BUG 2026-05-30

**Reproduce:** `GET /api/v1/admin/audit-logs?page=1&pageSize=10` → 200 items=7. `GET /api/v1/security/audit-logs?page=1&pageSize=10` → 200 items=10/132.

**Note:** Two paths exposed (admin-scope + security-scope). F72 partially resolved — audit logs ARE accessible via admin paths, just NOT at `/admin/audit-log` (singular) which is the master doc's path. Real paths use plural `audit-logs`.

---

## F84 — Subscriptions/Loyalty/Referrals endpoints absent (workflow §16/17 — DEFERRED per roadmap)

**Severity:** INFO · **Status:** ✅ NOT_A_BUG 2026-05-30

**Reproduce:** `GET /api/v1/subscriptions/{plans,mine,me}`, `/loyalty/{me,points}`, `/referrals/{me,code}` → all **404**.

**Workflow says:** §16 Subscriptions (provider tiers Free/Basic/Premium/Enterprise) and §17 Referral & Loyalty (YJ-XXXXXX codes, 100pts=1JOD) — both marked as **DEFERRED post-MVP** per Master-RoadmapTo10.md Phase 3 Growth.

**Verdict:** Not bugs. Features intentionally not built. May need future implementation per roadmap.

---

## F83 — `POST /api/v1/auth/external-providers` returns 405; only `iv2 /invitations/roles` works for admin

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `GET /api/v1/auth/external-providers` → **405 Method Not Allowed**. `GET /api/v1/auth/invitations` → **405 Method Not Allowed**. `GET /api/v1/auth/invitations/roles` → **200 n=6** (works).

**Likely cause:** Verb mismatch. external-providers + invitations registered for POST only; GET overload missing.

**Impact:** Admin can't list pending OAuth connections or pending invitations via REST.

---

## F82 — Moderation log + user moderation endpoints return 404 (workflow §6 should be exposed)

**Severity:** LOW · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as admin → all return **404**:
- `GET /admin/moderation-log`, `/social/moderation-log`, `/admin/social/moderation-log`
- `GET /admin/users/moderation`, `/social/admin/moderation/users`, `/social/users/moderation`

**Workflow says:** §6 Reviews & Ratings auto-hide flag at 3 reports + moderation queue for admin review.

**Likely cause:** Either endpoint at non-standard path or moderation features not exposed for inspection.

**Impact:** Admin moderation log + user-ban history not visible via REST.

---

## F81 — Search edge cases all WORK correctly (no findings)

**Severity:** INFO · **Status:** ✅ NOT_A_BUG 2026-05-30

**Reproduce:** Search edge cases:
- q='p' → 200 items=1 (matches Petra)
- q=500-char string → 400 Validation.Request.Q (length cap enforced)
- q=%$&# special chars → 200 items=0 ✅
- q='البتراء' Arabic → 200 items=0 (no match but accepted)
- q='café' Unicode → 200 items=0 ✅
- q='' empty → 200 items=1 (returns all)
- no q → 200 items=1

**Verdict:** Search input handling robust. Validation cap works. Unicode + special chars handled gracefully.

---

## F80 — Guide may not receive notifications on bookings (inconclusive)

**Severity:** LOW · **Status:** 🔍 INVESTIGATE 2026-05-30

**Reproduce:** Login as guide-approved → `GET /api/v1/notifications` after userB created a booking on seeded tour FFFFFFFF-...001 → items=0.

**Inconclusive because:** Slot's TourGuideId is `ABABABAB-3333-...` (seeded fake guide), NOT guide-approved's TourGuideId `5E8149CA-...`. So no notification may be correct (notifications fire to the ASSIGNED guide, not arbitrary other guides). However, if seeded tour's `ProviderUserId` is guide-approved's user id, then they SHOULD receive owner-side notifications.

**Suggested investigation:** Check `TourBookingCreatedDomainEvent` handler → confirm which entity is notified (slot.TourGuideId vs tour.ProviderUserId vs both). Also check whether booking-related domain events are emitted to MessageBus or only to Outbox.

---

## F79 — `GET /provider/status` returns 403 immediately after `POST /provider/register` succeeds

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as userA (User role) → `POST /api/v1/provider/register` with valid body → **200** returns `applicationId`. Then `GET /api/v1/provider/status` → **403 Forbidden**.

**Root cause:** User role HAS `Permission.ProviderApplication.Register/Submit` (added in batch fix for F44) but lacks `Permission.ProviderApplication.Read`. After successful register, user cannot inspect their own application status.

**Workflow says:** §1 Provider Registration — after submission, user must be able to track their application status to see admin's decisions.

**Suggested fix:** Add `"Permission.ProviderApplication.Read"` to `ConsumerPermissions` HashSet at `Security.Infrastructure/Seeding/RolePermissionMapping.cs`. Same family as F30/F33/F44/F53/F69 batch fix.

---

## F78 — `POST /api/v1/booking/{id}/admin/confirm` returns 404 Not Found

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as admin → `POST /api/v1/booking/{id}/admin/confirm` for an AwaitingPayment booking → 404 Not Found. Admin endpoint to manually confirm a booking doesn't exist at the expected path.

**Likely cause:** Admin confirm endpoint at different path (e.g. `/booking/admin/{id}/confirm` or `/booking/{id}/confirm` with admin perm) OR the manual-confirm flow doesn't exist (only payment-driven confirmation).

**Impact:** Admin cannot manually confirm bookings stuck in AwaitingPayment when payment gateway is unavailable. Workaround: SQL update directly.

---

## F77 — Tour proposal DTO uses `durationMinutes` (not `estimatedDurationMinutes`), differs from workflow doc

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** Login as guide → `POST /api/v1/tours/proposals` with `{title, description, placeId, estimatedDurationMinutes, estimatedBasePrice, currency}` → **400 `Validation.DurationMinutes`** 'DurationMinutes must be greater than 0'. Real DTO uses `durationMinutes` and probably `basePrice` (not `estimated*` prefix).

**Impact:** Doc-only. Clients using master doc fields fail validation.

---

## F76 — Tour Package DTO uses `name` (not `title`), inconsistent with Tour Proposal (which uses `title`)

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** Login as guide → `POST /api/v1/tours/packages` with `{title, slug, description, ...}` → **400 `Validation.Name`** 'Name must not be empty'. Real DTO requires `name` field.

**Inconsistency triangle:** Tour create → `name`, Tour proposal → `title`, Tour package → `name`. Three sibling endpoints under `/tours/*` use TWO different naming conventions for the same concept.

**Impact:** Doc divergence + inconsistency. Same family as F25, F29, F32, F71.

---

## F75 — `/api/v1/health` returns 404 (real path is `/health` at root)

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `GET /api/v1/health` → 404. Real health endpoint is at `/health` (no `/api/v1/` prefix).

**Impact:** Doc-only — adapter cheat-sheet should clarify. Standard ASP.NET Core convention is `/health` at root.

---

## F74 — Notification preferences PUT requires `{updates: [...]}` array shape

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `PUT /api/v1/notifications/preferences` with `{eventType, channel, enabled}` flat shape → 400 `Validation.Updates`. Real DTO requires `{updates: [{eventType, channel, enabled}, ...]}` — batch update shape.

**Impact:** Doc divergence. Clients must wrap in `updates` array even for single change.

---

## F73 — `GET /api/v1/security/roles/{name}/claims` returns 405 Method Not Allowed

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** Login as admin → `GET /api/v1/security/roles/User/claims` → 405.

**Likely cause:** Endpoint registered with different verb (PUT for set-claims operation?) OR path uses role ID GUID instead of role name string.

---

## F72 — Admin analytics + user mgmt paths 404 (`admin/users/dashboard`, `admin/moderation/users`, `admin/analytics/users`)

**Severity:** LOW · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as admin → multiple admin paths return 404. Endpoints likely at different prefixes or not registered.

**Impact:** Master doc has outdated paths. Workaround: SQL queries directly.

---

## F71 — Tour proposal DTO uses `title` not `name` (vs Tour create which uses `name`)

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** Login as guide → `POST /api/v1/tours/proposals` with `{name, slug, ...}` → **400 `Validation.Title`** 'Title must not be empty'. Real DTO requires `title` field.

**Impact:** Inconsistency within same module — Tour create uses `name`, Tour proposal uses `title`. Same naming-divergence family as F29.

---

## F70 — Multiple admin user-management endpoints return 405 Method Not Allowed

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as admin → following all return **405 Method Not Allowed**:
- `GET /api/v1/auth/admin/users/{id}/sessions`
- `POST /api/v1/auth/admin/users/{id}/suspend`
- `POST /api/v1/auth/admin/users/{id}/archive`

**Likely cause:** Endpoints registered with different verbs (e.g. PUT instead of POST for state changes, or PATCH for sessions).

**Impact:** Admin cannot manage user state (suspend, archive) or inspect sessions via REST until correct verbs are discovered.

---

## F69 — User cannot apply to become Creator (`POST /api/v1/blogs/creators/applications` → 403)

**Severity:** MED · **Status:** ✅ FIXED 2026-05-30 (added `Permission.Creator.Submit` + `Permission.Creator.Read` to ConsumerPermissions). Verified userA POST returns **201 Created**.

**Reproduce:** Login as userA → `POST /api/v1/blogs/creators/applications` with `{displayName, bio, niches}` → 403 Forbidden. Even with full body shape.

**Root cause:** `ConsumerPermissions` HashSet lacks `Permission.Creator.Apply` (or equivalent). User role cannot start a creator application.

**Workflow says:** §9 Blog/Content allows any registered user to apply as creator. The application is reviewed by admin.

**Suggested fix:** Add `"Permission.Creator.Apply"` (or correct permission name from `ContentBlogs.Contracts.Authorization.BlogFeatures`) to `ConsumerPermissions` HashSet at `Security.Infrastructure/Seeding/RolePermissionMapping.cs`.

**Same family:** F30 (Review.ReadOwn), F33 (DeviceToken.Read), F44 (ProviderApplication.Create), F53 (Profile.Delete) — all permission gap findings.

---

## F68 — Multiple admin moderation queue paths return 404 (likely wrong prefix)

**Severity:** MED · **Status:** 📝 DOC 2026-05-30

**Reproduce:** Login as admin → following 6 paths all return **404 Not Found**:
- `GET /api/v1/admin/tours/proposals`
- `GET /api/v1/admin/blogs?page=1&pageSize=10`
- `GET /api/v1/blogs/admin?page=1&pageSize=10`
- `GET /api/v1/admin/moderation-log?page=1&pageSize=10`
- `GET /api/v1/admin/audit-log?page=1&pageSize=10`
- `GET /api/v1/admin/users/moderation?page=1&pageSize=10`

The `/admin/providers`, `/blogs/admin/creators/applications`, `/social/reports/admin` paths work, so some admin namespaces exist. The 404s indicate inconsistent admin path conventions.

**Likely cause:** Master scenarios doc paths are outdated. Real paths may be at `/tours/admin/{proposals}` (per swagger inventory in b93), `/blogs/admin/{posts}`, `/admin/moderation/log`, etc.

**Suggested fix (docs):** Pull live swagger.json and rebuild adapter's admin endpoint map. Affected modules: Tours, Blogs, Audit, Moderation.

---

## F67 — Agency role cannot access agency self-management endpoints

**Severity:** HIGH · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as agency@yallajo.test → following all return **404 Not Found**:
- `GET /api/v1/agencies/me/roster`
- `GET /api/v1/agencies/me/invitations`
- `POST /api/v1/agencies/me/invitations` with `{guideEmail}`

Yet `/api/v1/guides/me/invitations` (guide-side) returns 200 ✅.

**Likely cause:** Agency endpoint prefix is different (`/api/v1/agency/me/*` singular?) OR endpoints genuinely not implemented (workflow plan §1 says Agency requires ≥1 guide; affiliation management is critical).

**Impact:** Agency provider cannot send invitations, view roster, or manage affiliations. Workflow §1 Agency Registration broken.

**Suggested fix:** Check Accounts.Presentation for AgencyEndpoints path; verify swagger has /agency/* or /agencies/* paths.

---

## F66 — `/tours/search/suggest` only accepts `q` parameter (resolves F62 ambiguity)

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `/api/v1/tours/search/suggest?q=pet` → 200 ✅. Same path with `?query=`, `?term=`, or `?keyword=` → 400 Bad Request.

**Impact:** F62 narrows — the param IS named `q`, doc just didn't specify. Update master scenarios doc + adapter.

---

## F65 — `Tour.NotOwner` correctly enforced (NOT_A_BUG)

**Severity:** INFO · **Status:** ✅ NOT_A_BUG 2026-05-30

**Reproduce:** Guide-approved tried to add schedule/waypoint/children-info on seeded tour `FFFFFFFF-...001` → 403 `Tour.NotOwner`. The seeded tour's TourGuideId is `ABABABAB-3333-3333-3333-333333333333`, not guide-approved's user/guide id. Correct auth gate.

---

## F64 — Tour pricing tier DTO uses `participantType` (not separate adult/child/infant prices)

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `POST /api/v1/tours/{id}/pricing` with `{name, adultPrice, childPrice, infantPrice, currency, validFrom, validUntil}` → 400 `Validation.ParticipantType` 'Participant Type must not be empty'. Real DTO requires `{name, participantType, price, currency, validFrom, validUntil}` — one tier per participant type, multiple tiers per pricing scheme.

**Impact:** Doc-only. Master scenarios doc adultPrice/childPrice/infantPrice shape doesn't match real DTO.

---

## F63 — Guide cannot archive own tour (`POST /api/v1/tours/{id}/archive` → 403)

**Severity:** LOW · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as guide-approved → `POST /api/v1/tours/{ownTourId}/archive` → 403 Forbidden.

**Root cause hypothesis:** Either (a) archive is admin-only by design (workflow says provider can request retire but admin must archive), OR (b) `Permission.Tour.Archive` missing from `ProviderSelfPermissions`. Behaviour matches admin-only design but should be documented.

**Suggested fix:** If by design, document; if perm gap, add `Permission.Tour.Archive` to `ProviderSelfPermissions` HashSet.

---

## F62 — `/api/v1/tours/search/suggest` returns 400 without query param docs

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `GET /api/v1/tours/search/suggest?query=wadi` → 400 Bad Request. Required query param name differs from `query`. Master doc needs the correct param name.

---

## F61 — Two paths to same endpoint with different permissions: `/tours/provider/my-tours` vs `/provider/my-tours`

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as guide-approved → `GET /api/v1/tours/provider/my-tours?page=1&pageSize=10` → **403 Forbidden**. Same query against `/api/v1/provider/my-tours?page=1&pageSize=10` → 200. Two route mappings for the same endpoint, but the `/tours/provider/*` variant has a stricter permission gate than `/provider/*`.

**Suggested fix:** Consolidate to ONE canonical path (recommend `/api/v1/provider/my-tours` since it works); remove the duplicate route OR align permissions. Add the canonical path to ConsumerPermissions/ProviderSelfPermissions consistently.

---

## F60 — `/provider/my-tours` returns items=0 even after guide creates a tour

**Severity:** HIGH · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as guide-approved → `POST /api/v1/tours` with full body → 201 `{tourId: 019e794e-...}`. Immediately `GET /api/v1/provider/my-tours?page=1&pageSize=10` → 200 items=0. Same on `/guide/my-tours` and `/tours/provider/my-tours`.

**Root cause hypothesis:** (a) My-tours endpoint queries by `TourGuideId` instead of `ProviderId` (the seeded tour's TourGuideId is `ABABABAB-3333-3333-3333-333333333333`, NOT guide-approved's TourGuide.Id `5E8149CA-...`); (b) Status filter excludes Draft tours; (c) Cache invalidation issue.

**Impact:** Critical — creator cannot see their own drafts after creation. Combined with F45 (single GET fails) and F58 (can't submit), the creator workflow is completely blocked.

**Suggested fix:** Align my-tours query to use `ProviderId == currentUser.UserId` (or `currentUser.TourGuide.Id` if that's the design). Include all statuses or filter to `Status != Archived`.

---

## F59 — Even admin cannot see Draft tour via `GET /api/v1/tours/{id}` (extends F45)

**Severity:** HIGH · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Admin token → `GET /api/v1/tours/{draftTourId}` → 404 Tour.NotFound. The Draft tour is HIDDEN even from admin via single-GET. List endpoint with providerId filter shows it; single-GET filters too aggressively.

**Impact:** Critical — admin moderation workflow broken. Admin must approve tours but can't preview the tour body via REST. Workaround: query DB directly.

**Suggested fix:** Combined with F45 fix. Single-GET should bypass status filter for admin role OR for owner.

---

## F58 — Guide cannot submit own tour for review (`POST /api/v1/tours/{id}/submit` → 403)

**Severity:** HIGH · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as guide-approved → after `POST /tours` returns 201 with tourId → `POST /api/v1/tours/{tourId}/submit` → 403 Forbidden. Tour stays in Draft state with no way to progress.

**Root cause:** `ProviderSelfPermissions` HashSet (`Security.Infrastructure/Seeding/RolePermissionMapping.cs`) has `Permission.Tour.{Create,Update,Delete}` but NOT `Permission.Tour.Submit`. The submit endpoint requires the missing perm, blocking the creator workflow at the Draft → Pending transition.

**Suggested fix:** Add `"Permission.Tour.Submit"` to `ProviderSelfPermissions` HashSet. SecurityDataSeeder reconciles on restart.

**Same family as:** F23 (TourBooking.Cancel), F30 (Review.ReadOwn), F33 (DeviceToken.Read), F44 (ProviderApplication.Create), F53 (Profile.Delete). Pattern: every state-transition action needs its own Permission and most are missing.

---

## F57 — `/api/v1/provider-bank-accounts` returns 404 (provider can't manage bank accounts via REST)

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as guide-approved → `GET /api/v1/provider-bank-accounts` → 404 Not Found. The `/provider-payment-methods` works (200), so payment-method management is exposed, but bank-account management isn't. ConsumerPermissions HashSet HAS `Permission.ProviderBankAccount.{Create,Read,Update,Delete}` so the perm is wired but the endpoint isn't.

**Likely cause:** Endpoint either at different path (`/provider/bank-accounts`?) or not registered. Master doc may have outdated path. Workflow §5 Payment & Refund references provider bank account management.

**Impact:** Provider can't add/view bank accounts → cannot receive payouts.

---

## F56 — Consumer payment endpoints (`/payments`, `/payments/me`) return 404

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as userA → `POST /api/v1/payments` with `{bookingId, paymentMethodId, amount, currency}` → **404 Not Found**. `GET /api/v1/payments/me?page=1&pageSize=10` → 404. The `/disputes` endpoint works at `/disputes/my` so the Finance module is wired; payment-specific paths are not.

**Likely cause:** Payments either exposed at non-`/payments/*` prefix OR the consumer-side payment flow is gated behind a payment gateway integration that's not active in this build.

**Impact:** Users can't initiate payments via REST. Bookings created with status `AwaitingPayment` (status=10) cannot transition to Confirmed (status=1) until payments flow exists. F24 slot underflow may be related to this.

**Suggested investigation:** Check `Finance.Presentation/PaymentEndpoints.cs` for actual mount prefix; verify payment provider integration status.

---

## F55 — Multiple Finance admin paths return 404 (commission-rules, payouts, invoices, credit-notes, refund-policies)

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** Login as admin → following all return **404 Not Found**:
- `GET /api/v1/finance/admin/commission-rules`
- `GET /api/v1/finance/admin/payouts?page=1&pageSize=10`
- `GET /api/v1/finance/admin/invoices?page=1&pageSize=10`
- `GET /api/v1/finance/admin/credit-notes?page=1&pageSize=10`
- `GET /api/v1/finance/admin/refund-policies`
- `POST /api/v1/finance/admin/commission-rules` (create)

The `/api/v1/finance/admin/dashboard` works (200) so the Finance admin namespace exists, but these sub-routes are at different paths or not registered.

**Likely cause:** Master scenarios doc has outdated `/finance/admin/*` paths. Real paths may be at `/api/v1/admin/finance/*` or `/api/v1/admin/commission-rules` etc.

**Impact:** Admin cannot perform Finance management via REST until correct paths are discovered. Workaround: query DB directly.

**Suggested fix (docs):** Pull live swagger.json and update master adapter doc's endpoint map.

---

## F54 — Avatar update endpoint requires multipart, not JSON

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** Login as userA → `PUT /api/v1/accounts/profile/avatar` with JSON body `{avatarUrl: '<cdn-url>'}` → **415 Unsupported Media Type**.

**Root cause:** Endpoint expects `multipart/form-data` with file upload, not JSON URL update. Master scenarios doc + adapter cheat-sheet have JSON shape; real DTO is multipart.

**Suggested fix (docs):** Update adapter `apiUpload` example to use multipart for avatar; document content-type expectations.

---

## F53 — User cannot soft-delete own profile (`DELETE /api/v1/accounts/profile` → 403)

**Severity:** MED · **Status:** ✅ FIXED 2026-05-30 (added `Permission.Profile.Delete` + `Permission.Profile.SoftDelete` to ConsumerPermissions). Verified userA DELETE returns **200**, restore returns 200.

**Reproduce:** Login as userA → `DELETE /api/v1/accounts/profile` → 403 Forbidden. The `POST /accounts/profile/restore` works (200), confirming the symmetric endpoint exists but the delete gate is too strict.

**Root cause:** `ConsumerPermissions` HashSet lacks `Permission.Profile.Delete` (or `Permission.Profile.SoftDelete`). User has Profile.Read/Update but not Delete.

**Impact:** GDPR right-to-erasure broken for self-service. User must contact support to delete own account.

**Suggested fix:** Add `"Permission.Profile.Delete"` (or `Permission.Profile.SoftDelete` if separate) to `ConsumerPermissions` HashSet at `Security.Infrastructure/Seeding/RolePermissionMapping.cs`.

---

## F52 — Password change + phone change endpoints return 405 Method Not Allowed on POST

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as userA → `POST /api/v1/security/account/password` with `{currentPassword, newPassword}` → **405 Method Not Allowed**. Same on `POST /api/v1/security/account/phone`.

**Likely cause:** Endpoint registered as PUT not POST. Swagger should confirm.

**Impact:** Users cannot change their password or phone via the documented path.

**Suggested fix:** Either change endpoint to POST OR document that PUT is required. Update adapter cheat-sheet.

---

## F51 — Resend OTP requires `purpose` field (DTO divergence)

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `POST /api/v1/auth/resend-otp` with `{email}` → 400 `Validation.Purpose` 'Purpose must not be empty'. Real DTO requires `{email, purpose}` where purpose is an enum (probably `EmailVerification | PasswordReset | etc.`).

**Impact:** Doc-only — clients using documented shape fail validation.

---

## F50 — Verify email requires `otpCode` (not `otp`) — DTO field name divergence

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `POST /api/v1/auth/verify-email` with `{email, otp}` → 400 `Validation.OtpCode` 'OtpCode must not be empty'. Field name is `otpCode` not `otp`.

**Impact:** Doc-only — clients using documented shape fail validation.

---

## F49 — Reset password DTO uses email+otp not token (DTO divergence)

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `POST /api/v1/auth/reset-password` with `{token, newPassword}` → 400 `Validation.Email` 'Email must not be empty'. Real DTO requires `{email, otpCode, newPassword}` (uses OTP-based flow, not opaque-token-based).

**Impact:** Doc-only — clients using documented token-based shape fail validation.

---

## F48 — `POST /api/v1/auth/forgot-password` for EXISTING email returns 500

**Severity:** HIGH · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** `POST /api/v1/auth/forgot-password` with `{email: 'admin@yallajo.test'}` → **500 Internal Server Error**. UNKNOWN email returns 200 correctly (no enumeration leak). The bug surfaces only when the email DOES exist.

**Root cause hypothesis:** Handler crashes when creating the OTP record OR sending the reset email. Possible causes: (a) `OtpService` NRE on user with no primary email VO, (b) `IEmailSender` throws because SMTP isn't configured in Development, (c) `ForgotPasswordCommandHandler` dereferences a nullable navigation property.

**Impact:** Critical — password reset BROKEN for legitimate users. Users locked out of their accounts cannot recover.

**Suggested investigation:** Read `Auth.Application/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs`; check API log for stack trace at trace ID. Likely fix: null guard or environment-specific email sender fallback.

---

## F47 — Tour create `PlaceId` validation is handler-level ArgumentException (should be FluentValidation)

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as guide-approved → `POST /api/v1/tours` with valid name+slug+durationMinutes+rest but no placeId → 400 `Bad Request` 'PlaceId is required. (Parameter `placeId`)'. Title is generic 'Bad Request' (from F19 GlobalExceptionHandler.ArgumentException case), not `Validation.PlaceId`.

**Root cause:** `CreateTourCommand` handler throws `ArgumentException("PlaceId is required.")` when null/empty. `CreateTourCommandValidator` lacks `RuleFor(x => x.PlaceId).NotEmpty()` rule. Same anti-pattern as F21 (TourBooking.GuideId).

**Suggested fix:** Add `RuleFor(x => x.PlaceId).NotEmpty().WithMessage("PlaceId is required.")` to `ContentTours.Application/Commands/CreateTour/CreateTourCommandValidator.cs`. Removes the handler-level guard.

---

## F46 — `PUT /api/v1/tours/{id}` returns **500 NRE** on update

**Severity:** HIGH · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as guide-approved → after `POST /tours` returns 201 with tourId → `PUT /api/v1/tours/{tourId}` with same field set as create → 500 'Object reference not set to an instance of an object.' (System.NullReferenceException). Same NRE when owner retries with full body.

**Root cause hypothesis:** (a) `UpdateTourCommand` DTO requires more/different fields than `CreateTourCommand` and a required field is null on the handler path; (b) The handler dereferences a navigation property that wasn't loaded; (c) The validator/handler expects something not in the request DTO.

**Suggested investigation:** Read `ContentTours.Application/Commands/UpdateTour/UpdateTourCommand.cs` + `UpdateTourCommandHandler.cs` to identify the NRE site. Then either fix handler null guard or add validator rule.

---

## F45 — Tour created with 201 but immediately invisible via `GET /api/v1/tours/{id}` for owner

**Severity:** HIGH · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as guide-approved → `POST /api/v1/tours` with full body → 201 `{tourId: 019e794e-2065-703a-b67f-b7e48300456a, name, slug}`. Immediately `GET /api/v1/tours/{tourId}` (same session, same token) → **404 Tour.NotFound**. Same tour DOES appear via `GET /api/v1/tours?providerId={guideUserId}` → items=1.

**Inconsistency:** List endpoint (with providerId filter) returns the tour, but single-GET returns 404. Single-GET probably filters by `Status >= Approved/Published` even for the owner.

**Impact:** Blocks normal CMS workflow — creator cannot preview/edit own draft via single-GET right after creation. Must use the list endpoint with providerId filter as a workaround.

**Suggested fix:** `GET /api/v1/tours/{id}` should bypass the status filter when `currentUser.UserId == tour.ProviderId` (owner-view) OR when caller has admin permission. Pattern: split into `/tours/{id}` (public, status-filtered) and `/tours/{id}/admin` (admin-view), OR add `ownerView=true` query param.

---

## F44 — User role cannot apply to become provider via `POST /api/v1/provider/apply`

**Severity:** MED · **Status:** ✅ FIXED 2026-05-30 (perm gate). Added `Permission.ProviderApplication.Submit` + `Permission.ProviderApplication.Register` to ConsumerPermissions. Verified: 403 → 404 ProviderApplication.NotFound (auth gate passes; 404 because /apply expects an existing Draft application — user must POST /provider/register first).

**Reproduce:** Login as userB → `POST /api/v1/provider/apply` with empty body OR with full `{providerType, businessName, contactEmail, contactPhone, address, description}` → 403 Forbidden in both cases.

**Root cause:** `ConsumerPermissions` HashSet (`Security.Infrastructure/Seeding/RolePermissionMapping.cs`) lacks `Permission.ProviderApplication.Create`. User role's RoleClaims do not include it. The TourGuide role HAS it via `ProviderSelfPermissions`, but a freshly-registered User cannot apply for provider status.

**Workflow says:** §1 Provider Registration: Any registered user can apply for provider status. The application is reviewed by admin; on approval, the role is upgraded.

**Suggested fix:** Add `"Permission.ProviderApplication.Create"` to `ConsumerPermissions` HashSet at line ~60 (after `Permission.Report.Create`). SecurityDataSeeder reconciles on next API startup.

---

## F43 — `GET /api/v1/analytics/admin/popularity` returns 404 Not Found

**Severity:** LOW · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as admin → `GET /api/v1/analytics/admin/popularity` → 404.

**Likely causes:** (a) Admin endpoint doesn't exist — PopularityScore is computed via background job but not exposed for admin inspection; (b) Path is wrong (master scenarios doc may have outdated path); (c) Endpoint exists but at different prefix.

**Impact:** Admin cannot inspect popularity scores via REST. Workaround: query `popularity_scores` table directly.

---

## F42 — `GET /api/v1/provider/documents` returns 405 Method Not Allowed

**Severity:** LOW · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as guide-approved → `GET /api/v1/provider/documents` → 405 Method Not Allowed.

**Root cause:** Endpoint registered for POST/PUT/DELETE only. No GET overload to list own documents.

**Impact:** Provider can upload/update/delete documents but cannot list them via REST. Workaround: provider dashboard endpoint (`/provider/dashboard`) includes document summary.

**Suggested fix:** Add `MapGet("/documents")` to `ProviderEndpoints` returning provider's own documents (filtered by `ProviderApplication.UserId == currentUser.UserId`).

---

## F41 — `POST /api/v1/analytics/interactions` returns 404 Not Found

**Severity:** LOW · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** Login as userA → `POST /api/v1/analytics/interactions` with `{interactionType, entityType, entityId}` → 404 (with both int and string enum form).

**Likely causes:** (a) Endpoint path differs (`/analytics/interaction` singular or `/analytics/ingest`?); (b) Endpoint may be deliberately not exposed externally — interactions ingested only via server-side events; (c) Endpoint genuinely missing.

**Workflow says:** §21 Recommendation System references `UserInteractions` tracking with explicit user-fired events.

**Impact:** Client cannot fire interaction events for recommendation engine. If background events suffice, this is by design.

---

## F40 — Inconsistent `pageSize` over-limit handling (`/places` rejects vs `/places/businesses` clamps)

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** `GET /api/v1/places?pageSize=999` → **400 `Validation.PageSize`** 'must be between 1 and 50. You entered 999.' But `GET /api/v1/places/businesses?pageSize=999` → **200** with `pageSize: 50` (silently clamped). Same upper bound, two strategies in the same sub-module.

**Suggested fix:** Pick one and apply uniformly. Clamping is friendlier for consumers; strict reject is more predictable for tests. Recommend clamping with optional `Warning` header documenting the clamp.

---

## F39 — `/robots.txt` returns 404

**Severity:** LOW · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** `GET https://localhost:57065/robots.txt` → **404 application/problem+json**. Most SaaS APIs ship a minimal robots.txt (`User-agent: *`, `Disallow: /api/`). Not blocking, but visible to crawlers.

---

## F38 — `Guid.Empty` lookup returns `Validation.{ResourceId}` instead of NotFound (cancel returns 403 with same input)

**Severity:** LOW · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** `GET /api/v1/places/00000000-0000-0000-0000-000000000000` → 400 `Validation.PlaceId` 'must not be empty'. But `POST /api/v1/booking/00000000-.../cancel` → **403** (F22 path). Different endpoints treat Guid.Empty differently (Validator vs Auth gate vs NotFound).

**Impact:** Tests probing invalid IDs see three different status codes for the same logical case. Consider standardizing: all should be 400 Validation.{ResourceId} via FluentValidation rule `RuleFor(x => x.Id).NotEmpty()`.

---

## F37 — `/api/v1/seo/redirects` is admin-only (NOT_A_BUG)

**Severity:** LOW · **Status:** ✅ NOT_A_BUG 2026-05-30

**Reproduce:** userA → 403, admin → 200 with seeded redirect. Admin-only is correct since redirects affect SEO routing globally.

---

## F36 — `/content-core/attachments` + `/entity-tags` are not generic list endpoints

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** Admin `GET /api/v1/content-core/attachments?page=1&pageSize=10` → 400 Bad Request (additional required params missing). Admin `GET /api/v1/content-core/entity-tags?page=1&pageSize=10` → 400 detail 'Required parameter "string entityType" was not provided'. With `entityType=Tour` → 400 detail 'Required parameter "Guid entityId" was not provided'.

**Impact:** Naming is misleading — these are "tags/attachments FOR a specific entity" endpoints, not generic browse. Master scenarios doc lists them as list endpoints. Fix: rename query params clarifies intent OR add a true list endpoint at `/content-core/tags?page=1` (which DOES exist and works at the simple /tags route).

---

## F35 — `/content-core/attachments` is admin-only (NOT_A_BUG)

**Severity:** LOW · **Status:** ✅ NOT_A_BUG 2026-05-30

**Reproduce:** userA → 403 Forbidden. Admin → 400 (additional required params, see F36) — proves it's behind admin permission gate, not broken.

---

## F34 — POST /support/tickets and /devices/token return raw GUID strings (no JSON envelope)

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `POST /api/v1/support/tickets` with valid body → 200. Response body is a raw GUID string `019e7XXX-...` (Content-Type still `application/json`). Same on `POST /api/v1/devices/token`. Departure from typical `{id: guid}` envelope used elsewhere (e.g. POST /booking/tour returns full booking object).

**Impact:** Client SDKs must handle two response shapes (object vs raw string). Inconsistent SDK ergonomics.

---

## F33 — `GET /api/v1/devices/tokens` returns 403 for User role

**Severity:** MED · **Status:** ✅ FIXED 2026-05-30 (added `Permission.DeviceToken.Read` to ConsumerPermissions). Verified userA GET returns **200**.

**Reproduce:** Login as userA → `GET /api/v1/devices/tokens` → **403**. Yet userA CAN successfully `POST /api/v1/devices/token` to register a token (works with `{deviceId, token, platform}`). Asymmetric — user can write but not read own tokens.

**Root cause hypothesis:** Either `Permission.DeviceToken.Read` is missing from `ConsumerPermissions` HashSet (currently has `DeviceToken.{Create, Delete}` but not Read), OR the endpoint is intentionally admin-only. Needs endpoint code inspection.

**Suggested fix:** Add `"Permission.DeviceToken.Read"` to `ConsumerPermissions` if the endpoint should be self-readable; otherwise leave as admin-only and document.

---

## F32 — Support ticket + device token DTO field naming divergence

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** `POST /api/v1/support/tickets` with `{subject, description, category}` → 400 `Validation.Body` 'Body must not be empty'. Real field name is `body` (not `description`). `POST /api/v1/devices/token` with `{token, platform}` → 400 `Validation.DeviceId` 'Device Id must not be empty'. Real DTO requires `deviceId`.

**Impact:** Doc divergence — affects Playwright-APIOnly-Adapter.md endpoint map + master scenarios doc.

---

## F31 — Messaging endpoint path divergence

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** Adapter doc says `/api/v1/support` for support tickets — real is `/api/v1/support/tickets`. Doc says `/notifications/mark-all-read` — real is `/notifications/read-all`. Doc says `/notifications/device-tokens` — real is `/devices/token`.

**Impact:** Adapter endpoint map needs updating; tests using doc paths will get 404/405.

---

## F30 — `GET /api/v1/social/reviews/my-reviews` returns 403 for User role

**Severity:** MED · **Status:** ✅ FIXED 2026-05-30 (perm gate). Real permission: `SocialFeatures.Review + AppAction.Read` (NOT ReadOwn). Added `Permission.Review.Read` to ConsumerPermissions. Verified: 403 → 400 (auth gate passes; 400 because the endpoint requires query params).

**Reproduce:** Login as userA → `GET /api/v1/social/reviews/my-reviews` → 403. UserA HAS `Permission.Review.Create` per ConsumerPermissions HashSet but not `Permission.Review.ReadOwn`.

**Root cause hypothesis:** `Permission.Review.ReadOwn` not in `ConsumerPermissions`. Same family as F23 (TourBooking.Cancel) and F33.

**Suggested fix:** Add `"Permission.Review.ReadOwn"` to `ConsumerPermissions` HashSet at `Security.Infrastructure/Seeding/RolePermissionMapping.cs`. SecurityDataSeeder picks it up on restart.

---

## F29 — Reviews module internal naming inconsistency (`targetType` vs `entityType`)

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** `POST /api/v1/social/reviews` body uses `{targetType, targetId, content}`. `GET /api/v1/social/reviews/ratings?entityType=...&entityId=...` query params use `entityType`/`entityId`. SAME MODULE, TWO NAMING CONVENTIONS.

**Impact:** Swagger gen + client SDK consistency. Tests pivoting between read/write within Reviews must use both naming conventions.

**Suggested fix:** Pick one (target* or entity*) and align everything in `Social.Application.Reviews` and `Social.Presentation/Endpoints/ReviewEndpoints.cs`.

---

## F28 — Review.BookingVerificationRequired correctly enforced (NOT_A_BUG)

**Severity:** INFO · **Status:** ✅ NOT_A_BUG 2026-05-30

**Reproduce:** userA `POST /api/v1/social/reviews` with valid rating+content → **403 `Review.BookingVerificationRequired`** 'Only users with a recent completed booking can review this item.'

**Verdict:** Correct per workflow SKIP-RV-01. userA only has Awaiting Payment booking (status=10), not Completed (status=4). To review, user must have at least one completed booking on the target tour.

---

## F27 — Admin reports path correction (NOT_A_BUG)

**Severity:** LOW · **Status:** ✅ NOT_A_BUG 2026-05-30

**Reproduce:** Adapter doc hinted `/api/v1/admin/reports` — real is `/api/v1/social/reports/admin`. Same path-correction pattern as F12 (recommendations/me), F16 (places/search), F17 (disputes path).

---

## F26 — Enum body binding broken (rejects string enum names in DTOs)

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30

**Reproduce:** `POST /api/v1/social/favorites` with `{entityType: 'Tour', entityId: <guid>}` → **400 JsonException** at `$.entityType`. With `{entityType: 0, entityId: <guid>}` → 200. Path-bound enums DO accept strings (verified via `DELETE /favorites/Tour/{id}` → 204). Same broken behavior on `POST /reviews` body's `targetType`.

**Root cause:** `JsonStringEnumConverter` not registered in ASP.NET Core JSON options for minimal API binding. Path binding uses different converters and works.

**Suggested fix:** `builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));` in `YallaJo.Api/Program.cs`. Fixes ALL body-bound enum properties across all modules.

---

## F25 — Reviews DTO field name divergence (`targetType` vs doc's `entityType`)

**Severity:** LOW · **Status:** 📝 DOC 2026-05-30

**Reproduce:** Master scenarios doc + workflow plan + adapter say Reviews POST body uses `{entityType, entityId, body}`. Real DTO is `{targetType, targetId, content}`. Affects ~20 scenarios in master doc.

**Impact:** Doc-only. Tests using doc fields get 400 with Validation.TargetId / Validation.Content.

---

## F24 — Slot capacity `LockedCount` can underflow on cancel after interrupted lifecycle

**Severity:** MED · **Status:** ❌ OPEN 2026-05-30 (state corruption / race)

**Reproduce:** Cancel an `AwaitingPayment` booking via `POST /api/v1/booking/{id}/cancel` after the slot has been touched by interrupted/parallel cancels → 500 with detail `Cannot release 1 locked seats; only 0 are locked.`. Surfaced when userA tried to cancel booking `019E745D-3B01-7DFE-9012-ACA1BBC787A5` (status 10) on slot `ABABABAB-1111-...` that had `LockedCount=0` (out of sync with the actual active locks).

**Root cause hypothesis:** `RestoreSlotCapacityOnCancelHandler` (`Booking.Infrastructure/EventHandlers/SlotCapacityRestoreHandlers.cs:40`) unconditionally calls `AvailabilitySlot.ReleaseLock(participantCount)`, which throws `BusinessRuleViolationException` when `LockedCount < participantCount`. The slot tracking gets out of sync when `BookingAutoExpireService` already released the lock OR when an earlier cancel attempt errored mid-way and rolled back domain state but not the in-memory slot counters.

**Suggested fix:** (a) Add idempotency in `RestoreSlotCapacityOnCancelHandler` — `if (slot.LockedCount >= participantCount) slot.ReleaseLock(participantCount); else log warn + skip`. (b) Or change `AvailabilitySlot.ReleaseLock` to clamp at 0 instead of throwing. (c) Or run a slot-counter reconciler that recomputes `LockedCount` from active SlotLocks table on each cancel.

**Reproduce trace:** `correlationId 00-41e17451da2f9cd5d8123da59535b247-aec2ed36be55c5c3-01` shows full stack ending at `AvailabilitySlot.cs:116`.

---

## F23 — User role cannot cancel own booking (missing `Permission.TourBooking.Cancel`)

**Severity:** MED · **Status:** ✅ FIXED 2026-05-29

**Reproduce (pre-fix):** Login as userB → `POST /api/v1/booking/{ownBookingId}/cancel` → 403 Forbidden even as owner. `TourBookingEndpoints.cs:69` `MapPost("/{id:guid}/cancel")` requires `MustHavePermission(BookingFeatures.TourBooking, AppAction.Cancel)` which maps to `Permission.TourBooking.Cancel`. `RolePermissionMapping.ConsumerPermissions` HashSet had `TourBooking.{Create, ReadOwn}` but not `Cancel`.

**Root cause:** Missing string `"Permission.TourBooking.Cancel"` in `ConsumerPermissions` HashSet at `Security.Infrastructure/Seeding/RolePermissionMapping.cs`. Without it, the User role's RoleClaims (auto-reconciled by `SecurityDataSeeder` on startup) didn't include the cancel permission, so the authorization gate blocked it.

**Fix:** Added `"Permission.TourBooking.Cancel"` to `ConsumerPermissions` HashSet directly after `Permission.TourBooking.ReadOwn`. SecurityDataSeeder picks it up on next API startup. JWT refresh/re-login required to surface the new claim.

**Verified post-restart 2026-05-30:** userB `POST /booking/019E7485-.../cancel` with reason → **200** body `{bookingId, status: 4 (Cancelled), cancelledAt, source: 0, reason: 'Testing F23 fix', refundAmount: 30, currency: 'JOD'}`. GET after cancel confirms status=4. Regressions F4/F17/F18 all 200.

---

## F21 — `CreateTourBookingCommand` missing GuideId returns misleading `TourBooking.InvalidState`

**Severity:** MED · **Status:** ✅ FIXED 2026-05-29

**Reproduce (pre-fix):** Login as userB → `POST /api/v1/booking/tour` with valid `tourId`+`availabilitySlotId`+`participantBreakdown` but no `guideId` → 400 with `title: "TourBooking.InvalidState"` and `detail: "GuideId must be provided."`

**Root cause:** `CreateTourBookingCommandValidator` didn't have a `RuleFor(x => x.GuideId).NotEmpty()` rule. Missing GuideId fell through to the handler's own guard, which raised a domain-level "InvalidState" error. Misleading: this was a missing-required-field validation error, not a state-machine error. ValidationBehavior was wired correctly all along at `YallaJo.SharedKernel.Infrastructure/DependencyInjection.cs:37` — the validator itself was missing the rule.

**Fix:** Added `RuleFor(x => x.GuideId).NotEmpty().WithMessage("GuideId is required.")` to `CreateTourBookingCommandValidator.cs:22-23`. FluentValidation now intercepts BEFORE handler runs, returning canonical 400 with `title: "Validation.GuideId"`.

**Verified post-restart:** userB POST with missing guideId → 400 `Validation.GuideId` 'GuideId is required.'

---

## F20 — `/api/v1/tours/by-slug/{slug}` 404 (convention divergence)

**Severity:** LOW · **Status:** ✅ FIXED 2026-05-29 (added by-slug route; kept /slug/ for backward compat)

**Reproduce (pre-fix):** Anon → `GET /api/v1/tours/by-slug/petra-full-day-explorer` → 404. Real path was `/api/v1/tours/slug/{slug}`. Inconsistent with `/api/v1/guides/by-slug/{slug}` convention used elsewhere.

**Root cause:** Two slug-route conventions in the codebase: `TourGuideProfileEndpoints.cs:54` uses `/by-slug/{slug}` (canonical), but `TourEndpoints.cs:75` uses `/slug/{slug}` only.

**Fix:** Added `MapGet("/by-slug/{slug}", ...)` to `TourEndpoints.cs:96-111` that delegates to the same `GetTourBySlugQuery`. Kept `/slug/{slug}` for backward compatibility. `WithName("GetTourByCanonicalSlug")`.

**Note:** Required clean rebuild (`rm bin/ obj/` + `dotnet build --no-incremental`) — stale build artifact had loaded the DLL without the new route.

**Verified post-restart:** `/tours/by-slug/petra-full-day-explorer` → 200 with id. `/tours/slug/{slug}` still 200 (backward compat preserved).

---

## F19 — `POST /booking/tour` with empty `{}` body returns 500 (NullReferenceException)

**Severity:** MED · **Status:** ✅ FIXED 2026-05-29

**Reproduce (pre-fix):** Anon → `POST /api/v1/booking/tour` with body `{}` → 500 'Object reference not set to an instance of an object.' (NullReferenceException). Should be 400 with FluentValidation field-level errors.

**Root cause (Oracle bg_f38f05f9):** `{}` deserializes successfully into `CreateTourBookingRequest` with `Guid.Empty` defaults AND `ParticipantBreakdown == null` (because `ParticipantBreakdownRequest` was a non-nullable parameter, but JSON omits it). Then `CreateTourBookingRequest.ToCommand()` at line 18 calls `ParticipantBreakdown.ToDomain()` which NREs on null. The endpoint invokes `ToCommand` BEFORE the validator pipeline runs (validator runs on `Command`, not `Request`).

**Fix:** Made `ParticipantBreakdownRequest` nullable in `CreateTourBookingRequest.cs` + propagated null with `?.ToDomain()!`. Now empty `{}` deserializes successfully, `ToCommand()` propagates null, and the existing `RuleFor(x => x.ParticipantBreakdown).NotNull()` validator (line 28) returns 400 BEFORE the handler dereferences. Bonus: `GlobalExceptionHandler.cs` got a new `JsonException → 400` case for malformed JSON.

**Verified post-restart:** POST `/booking/tour` `{}` → 400 `Validation.TourId` 'TourId is required.' (the first failing rule). Empty body (no JSON at all) → 400 `Bad Request` (from BadHttpRequestException case added in F5/F13).

---

## F18 — `User` role missing `Permission.Recommendation.Read` (analytics 403)

**Severity:** MED · **Status:** ✅ FIXED 2026-05-29

**Reproduce (pre-fix):** Login as `userA` → `GET /api/v1/analytics/recommendations` → 403 Forbidden.

**Root cause:** `RolePermissionMapping.ConsumerPermissions` HashSet did not include `Permission.Recommendation.Read`. User role couldn't read its personalized recommendations.

**Fix:** Added `"Permission.Recommendation.Read"` (line 60 of HashSet). SecurityDataSeeder reconciles role claims on every API startup; userA picked up the claim after restart.

**Verified post-restart:** userA `/analytics/recommendations` → 200 with empty list.

---

## F17 — `/finance/disputes` 404 (path correction + missing perms)

**Severity:** MED · **Status:** ✅ FIXED 2026-05-29 (path correction + Refund perms added to User role)

**Reproduce (pre-fix):** Login as `userA` → `GET /api/v1/finance/disputes/my` → 404 Not Found. The smoke sweep had assumed `/api/v1/finance/disputes` was the prefix.

**Root cause #1 (path):** `FinanceEndpoints.cs:35` mounts disputes at `/api/v1/disputes` (top-level group), NOT `/api/v1/finance/disputes`. Correct path is `/api/v1/disputes/my`.

**Root cause #2 (permission):** With correct path, userA gets **403** instead of 404. `DisputeEndpoints.cs:26` requires `FinanceFeatures.Refund Read` permission. User role didn't have it.

**Fix:** Added `"Permission.Refund.Read"` and `"Permission.Refund.Create"` to `ConsumerPermissions` HashSet in `RolePermissionMapping.cs` (lines 64-65).

**Verified post-restart:** userA `GET /api/v1/disputes/my` → 200 `[]`.

**Note:** `GetMyDisputesQueryHandler` (`Finance.Application/Disputes/DisputeHandlers.cs:138-147`) was already correct (returns `Result.Success(emptyList)` on no rows — not the F7 anti-pattern). The 404 was purely a path artifact.

---

## F16 — `/places/search` 404 (NOT_A_BUG — path correction)

**Severity:** N/A · **Status:** ✅ NOT_A_BUG 2026-05-29

**Reproduce:** Test hit `GET /api/v1/places/search?country=Jordan` → 404 `Place.NotFound`.

**Root cause:** Test path was wrong. `PlaceEndpoints.cs:32-51` already exposes `places.MapGet("/", ...)` (ListPlaces) with full search params (Page/PageSize/CategoryId/RatingMin/RatingMax/City/Country/HasActiveTours).

**Correct path:** `GET /api/v1/places?country=Jordan&page=1&pageSize=10` → 200 with 2 items.

**Note:** Same pattern as F12.1/F12.2/F11/F10 — test sweep used assumed-wrong paths. No bug. Pattern catalog now suggests verifying actual `MapGet` registration before flagging an endpoint as missing.

---

## F15 — `/api/v1/guides/me` always returns 404 (wrong-parameter bug)

**Severity:** HIGH (every approved guide blocked from reading own profile)
**Status:** ✅ FIXED 2026-05-29

**Reproduce (pre-fix):** Login as approved guide → `GET /api/v1/guides/me` → 404 `TourGuide.NotFound`. Same row reachable via `/guides/by-slug/...`, `/guides/{id}`, `/guides` list (all 200). JWT `sub` matches `TourGuide.UserId` in DB.

**Root cause (found by Oracle):** `TourGuideProfileEndpoints.cs:228` sent `new GetTourGuideByIdQuery(currentUser.UserId!.Value)`. That query's parameter is the **aggregate Id**, not the **owning user Id**. Handler filtered by `guide.Id == userId` → 404.

**Fix applied (6 files):**
- NEW `ContentTours.Application/Queries/TourGuides/GetByUserId/GetTourGuideByUserIdQuery.cs`
- NEW `ContentTours.Application/Queries/TourGuides/GetByUserId/GetTourGuideByUserIdQueryHandler.cs`
- EDIT `ContentTours.Domain/Repositories/ITourGuideRepository.cs` — added `GetWithDetailsByUserIdAsync(Guid userId, ...)`
- EDIT `ContentTours.Infrastructure/Repositories/TourGuideRepository.cs` — implementation
- EDIT `ContentTours.Application/Caching/TourGuideCacheKeys.cs` — `ProfileByUser` + `TagForProfileByUser`
- EDIT `ContentTours.Presentation/Endpoints/TourGuide/TourGuideProfileEndpoints.cs:228` — query swap

**Bonus smells also fixed (Oracle):**
- **Smell #1:** `ListActiveAsync` now filters `Status == TourGuideStatus.Active` (was relying on soft-delete only — Suspended guides could leak into public list).
- **Smell #2:** Removed misleading `GetBySlugAsync(Guid id)` overload that actually filtered by aggregate Id (not on interface, only confusing).

LSP diagnostics clean on all 6 files.

---


> **Canonical source of truth for all gaps discovered during Playwright MCP runs.** Each finding has a stable ID (`F#`). When a finding is fixed, mark it `[CLOSED]` here AND add the commit hash + closing run report. Never delete — keep the historical record.

**Updated:** 2026-05-29
**Open findings:** 13 (F1-F13)
**Closed findings:** 0
**Severity counts:** HIGH 6 · MED 5 · LOW 2

---

## How to read a finding

```
F# — Title                                      [SEVERITY] [STATUS]
  Module:    Which module/file owns the bug
  Repro:     Exact API call + expected vs actual
  Impact:    What user-visible behavior breaks
  Root cause:What's actually wrong in code
  Fix:       Concrete code change (file + approach)
```

Severities:
- **HIGH** — Blocks a published workflow, leaks security info, breaks an entire role's flow.
- **MED**  — Wrong behavior in an edge case, plan/code divergence, incorrect HTTP status.
- **LOW**  — Cosmetic, response shape inconsistency, docs.

---

## F1 — TourGuide aggregate never auto-created on ProviderApplication.Approve   [HIGH] [OPEN]

- **Module:** `ContentTours.Application` (cross-module event handler)
- **Repro:** login as `guide-approved@yallajo.test` → `GET /api/v1/guides/me` → 404 `TourGuide.NotFound`
- **Impact:** ALL approved guides cannot view their own profile, tier, availability blocks, applications, or earnings. The entire TourGuide self-service flow is dead. After admin approves a ProviderApplication, the corresponding TourGuide aggregate is never created.
- **Root cause:** `ContentTours.Application` has 0 references to the `ProviderApplicationApproved` integration event. The cross-module wiring `Accounts.IntegrationEvents.ProviderApplicationApproved → ContentTours.TourGuide.Create` is NOT_BUILT.
- **Fix:**
  1. Verify `ProviderApplicationApproved` integration event is emitted by `Accounts.Application.Features.ProviderApplications.ApproveCommandHandler`. Check outbox table after approving a new application.
  2. Add new handler `ContentTours.Application\Features\TourGuides\IntegrationEventHandlers\OnProviderApplicationApprovedHandler.cs : INotificationHandler<ProviderApplicationApproved>` that:
     - Filters: react only when `e.ProviderType == ProviderType.IndependentGuide` (or also Agency if Agency guides need a TourGuide row).
     - Calls `TourGuide.CreateFromApprovedApplication(e.UserId, e.ApplicationId, e.BusinessName, ...)` — may need to add this factory to `ContentTours.Domain.TourGuide`.
     - Persists via `ITourGuideRepository.AddAsync`.
  3. Register handler in `ContentTours.Application.DependencyInjection` if not auto-discovered.
  4. Backfill for 3 already-approved guides (`...005, ...006, ...007`): one-shot script or extend `AccountsProviderApplicationSeeder` to fire a backfill event.

---

## F2 — 500 responses leak full stack trace + absolute file paths   [HIGH] [OPEN]

- **Module:** `YallaJo.Api\Middleware\GlobalExceptionMiddleware.cs` (or equivalent)
- **Repro:** any of the F13 endpoints when called without required query param → 500 with body containing `exception` field, full stack trace, and absolute paths like `C:\Users\admin1\source\repos\YallaJo\YallaJo.Api\Middleware\SeoRedirectMiddleware.cs:line 26`
- **Impact:** Information disclosure (CWE-209). In production this exposes server filesystem layout, .NET version, internal architecture. Compliance fail (SOC 2 / ISO 27001 / OWASP A09).
- **Root cause:** The global exception handler does NOT conditionally strip the `exception` field based on `IHostEnvironment.IsDevelopment()`. Likely the same shape is returned in Production.
- **Fix:**
  1. Locate global exception handler (search for the type that emits `{ title, status, detail, instance, correlationId, timestamp, exception }`).
  2. Inject `IHostEnvironment env`. If `!env.IsDevelopment()`, drop the `exception` field before serializing.
  3. Replace `correlationId` if it leaks internal IDs; otherwise keep but document.
  4. Add a TC-CC-2.x scenario that asserts no `exception` field on 500 in non-Development.

---

## F3 — TourGuide role lacks Provider.* RoleClaims   [MED] [OPEN]

- **Module:** `Security.Infrastructure\Persistence\Seeding\SecurityDbInitializer.cs::SeedRolePermissions`
- **Repro:** login as `guide-approved` → `GET /api/v1/provider/status` → 403, `GET /api/v1/provider/dashboard` → 403, `GET /api/v1/provider-payment-methods` → 403
- **Impact:** Approved guides cannot read their own provider dashboard or manage payment methods. Workflow §1 requires this.
- **Root cause:** `SecurityDbInitializer.SeedRolePermissions` does not grant the `TourGuide` role any `Permission.Provider.*` claims. Missing: `Permission.Provider.Read`, `Permission.ProviderDashboard.Read`, `Permission.ProviderApplication.Read`, `Permission.ProviderDocument.{Read,Create,Update}`, `Permission.ProviderPaymentMethod.{Read,Create,Update,Delete}`, `Permission.ProviderBankAccount.{Read,Create,Update}`.
- **Fix:** In `SecurityDbInitializer.SeedRolePermissions` add to the TourGuide role row all `Permission.Provider*`, `Permission.ProviderDashboard.Read`, and the 3 ProviderApplication.Read/Update/Resubmit. Same for the `Provider` role (currently not seeded — see F11 sub-note).

---

## F4 — User & TourGuide roles lack Profile self-read   [MED] [OPEN]   (widened by F9)

- **Module:** `Security.Infrastructure\Persistence\Seeding\SecurityDbInitializer.cs::SeedRolePermissions`
- **Repro:** login as `userA` → `GET /api/v1/accounts/profile` → 403; login as `guide-approved` → same 403
- **Impact:** No non-admin user can read their own profile. Workflow §0 requires this.
- **Root cause:** `Permission.Profile.Read` is only seeded for Admin/SuperAdmin/Owner roles.
- **Fix:** Grant `Permission.Profile.{Read,Update,Delete,SoftDelete}` + `Permission.Account.{Read,Update}` to BOTH User and TourGuide roles in `SeedRolePermissions`. See F9 for the broader set in the same edit.

---

## F5 — Missing query param crashes 500 instead of returning 400   [MED] [OPEN]   (superseded/widened by F13)

- **Module:** `YallaJo.Api\Configuration\ApiBehaviorOptions` (or any endpoint group with `[FromQuery] int page`)
- **Repro:** `GET /api/v1/admin/providers` (no `?page=N`) → 500 LEAK (instead of 400 with field-level detail)
- **Impact:** Bad UX (clients see 500 not 400); compounds F2 by triggering the leak path.
- **Root cause:** Required primitive query params don't bind → ASP.NET pipeline throws `BadHttpRequestException` → falls through to global exception handler → returns 500 with full stack trace.
- **Fix:** Either (a) make all `int page` params optional with default `1` via `int page = 1`, OR (b) configure `ApiBehaviorOptions.InvalidModelStateResponseFactory` and handle `BadHttpRequestException` in middleware to convert to 400 RFC 7807. **Either fix also eliminates the LEAK path from F2 for query-bind crashes.**

---

## F6 — Route ordering: `/places/businesses` swallowed by `/places/{slug}`   [MED] [OPEN]

- **Module:** `ContentPlaces.Presentation\Endpoints\PlaceEndpoints.cs` (route order)
- **Repro:** `GET /api/v1/places/businesses` → 404 `Place.NotFound: Place with slug 'businesses' was not found`
- **Impact:** The `/places/businesses` collection endpoint is unreachable; clients always hit the slug catch-all.
- **Root cause:** Route table registers `/places/{slug}` before `/places/businesses`, so the parameterized template matches the literal segment first.
- **Fix:** In `PlaceEndpoints.MapPlaces`, register all literal sub-routes (`businesses`, `nearby`, `map/viewport`) BEFORE the `/places/{slug}` catch-all.

---

## F7 — Domain `NotReady` errors return 500 instead of 503   [MED] [OPEN]

- **Module:** `Analytics.Presentation\Endpoints\TrendingEndpoints.cs` (or shared Result→HTTP mapper)
- **Repro:** `GET /api/v1/trending` → 500 `Trending.WindowNotReady: Trending window is not ready.`
- **Impact:** A real domain-level "not ready" surfaces as a 500 server error → false alarms in monitoring. Should be 503 (transient) or 200 with empty array (degraded).
- **Root cause:** Result→HTTP mapper doesn't have a case for `*.NotReady` / `*.PreconditionFailed` error codes; falls through to 500.
- **Fix:** In `YallaJo.SharedKernel.Presentation\Mapping\ProblemDetailsMapper` (or equivalent), add: `code.EndsWith(".NotReady") → 503`, `code.EndsWith(".PreconditionFailed") → 412`. Verify across Analytics, Booking (slot not ready), Finance (payout window not ready) modules.

---

## F8 — Paged response shape inconsistent (Booking vs everyone else)   [LOW] [OPEN]

- **Module:** `Booking.Presentation\Endpoints\BookingEndpoints.cs`
- **Repro:** `GET /api/v1/booking/my-bookings` returns `{items}` only; `GET /api/v1/admin/providers` returns `{items, totalCount, page, pageSize}`.
- **Impact:** Clients can't render "Page 3 of 12" UI; pagination state lost.
- **Root cause:** Booking endpoints map domain pages to a different DTO that drops the envelope.
- **Fix:** Standardize on `Paged<T> { items, totalCount, page, pageSize }` envelope and update Booking endpoints to use it.

---

## F9 — User role cannot read own bookings, cannot create bookings   [HIGH] [OPEN]

- **Module:** `Security.Infrastructure\Persistence\Seeding\SecurityDbInitializer.cs::SeedRolePermissions`
- **Repro:**
  - login as `userA` → `GET /api/v1/booking/my-bookings` → 403
  - login as `userA` → `POST /api/v1/booking/tour` → 403
- **Impact:** **The entire consumer booking workflow is non-functional.** PDF §4 + Booking-Workflow.md require Users can create bookings. This is the most consequential gap discovered.
- **Root cause:** The `User` role only gets `Permission.User.Read` (basic profile lookup). It does NOT get `Permission.Booking.*`, `Permission.Profile.*`, `Permission.Review.*`, `Permission.Favorite.*`, `Permission.Notification.*`, `Permission.Account.*`.
- **Fix:** In `SecurityDbInitializer.SeedRolePermissions`, grant the User role:
  ```
  Permission.Account.Read, Permission.Account.Update,
  Permission.Profile.Read, Permission.Profile.Update, Permission.Profile.SoftDelete,
  Permission.Booking.Create, Permission.Booking.Read,
  Permission.Payment.Create, Permission.Payment.Read,
  Permission.Refund.Create, Permission.Refund.Read,
  Permission.Review.Create, Permission.Review.Read, Permission.Review.Update,
  Permission.Favorite.Create, Permission.Favorite.Read, Permission.Favorite.Delete,
  Permission.Notification.Read, Permission.Notification.Update,
  Permission.NotificationPreference.Read, Permission.NotificationPreference.Update,
  Permission.Interaction.Create,
  Permission.Recommendation.Read,
  Permission.JoinRequest.Create, Permission.JoinRequest.Read,
  Permission.Report.Create,
  Permission.DeviceToken.Create, Permission.DeviceToken.Delete,
  Permission.Attachment.Create.
  ```
  Also grant TourGuide all of the above PLUS the F3 list.

---

## F10 — SignalR hub negotiate endpoints not reachable   [HIGH] [OPEN]

- **Module:** `YallaJo.Api\Program.cs` (hub mapping) or `Messaging.Presentation\Hubs\NotificationHub.cs`
- **Repro:**
  - `POST https://localhost:57065/api/v1/hubs/notifications/negotiate` → 404
  - `POST https://localhost:57065/api/v1/hubs/tracking/negotiate` → 404
- **Impact:** **No browser can establish a SignalR connection.** All real-time notifications, live tour tracking, and dispute messages fall back to polling or are broken.
- **Root cause:** Either the hub is mapped at a different path (likely `/hubs/notification` singular, or under `/_hubs/...`), or the route is missing entirely. The Playwright APIOnly adapter §2.3 assumed the documented path.
- **Fix:** Verify in `Program.cs` what path `app.MapHub<NotificationHub>(...)` uses. Either move the hub mapping to match docs (`/api/v1/hubs/notifications`) or update `Playwright-APIOnly-Adapter.md §2.3` + every workflow plan to use the real path. Add an integration test that confirms `POST {hub}/negotiate` returns 200 with `connectionId`.

---

## F11 — Documented "Built" features that are actually NOT_BUILT (404)   [HIGH] [OPEN]

- **Module:** Multiple (Social, Messaging, Blogs)
- **Repro:** clean 404 responses on:
  - `GET /api/v1/wishlist`             — Social workflow §7 says Built
  - `GET /api/v1/support-tickets`      — Messaging workflow §12 says Built
  - `GET /api/v1/blogs/creators`       — BlogCreatorPost-Merger.md says Built
  - `GET /api/v1/admin/users/dashboard`— Admin dashboard sub-route documented but missing
- **Impact:** Workflow scoreboard (Master-RoadmapTo10.md) is inaccurate — claims modules are at 8.0+/10 but core endpoints don't exist.
- **Root cause:** Plan/code drift. Endpoints were never implemented OR they live at a different path.
- **Fix:** For each:
  1. Confirm path via `/swagger/v1/swagger.json`. If endpoint exists at a different path, update workflow plan + adapter.
  2. If endpoint truly absent, downgrade scoreboard from Built → NOT_BUILT and add to backlog.
  3. Build the missing endpoints in order: wishlist (consumer-facing, blocks F9 testing) → support-tickets (Messaging core) → blogs/creators (admin onboarding).

---

## F12 — Endpoints return 405 Method Not Allowed for GET   [MED] [OPEN]

- **Module:** Multiple presentation projects
- **Repro:**
  - `GET /api/v1/analytics/recommendations/me` → 405
  - `GET /api/v1/disputes?page=1&pageSize=10` → 405
  - `GET /api/v1/seo/faq?page=1&pageSize=10` → 405
- **Impact:** Common REST clients (and Playwright tests) cannot read these resources. Workflow plans imply GET is the natural method.
- **Root cause:** Endpoints registered with POST-only or wrong verb. Likely refactor leftover.
- **Fix:** Audit `RecommendationsEndpoints`, `DisputeEndpoints`, `FaqEndpoints` — add `MapGet` overloads for the resource-collection routes. Update Playwright tests once verbs confirmed.

---

## F13 — Unified: missing required query param = 500 with full leak   [HIGH] [OPEN]   (rolls up F2 + F5)

- **Module:** ASP.NET Core pipeline + global exception middleware
- **Repro pattern:** ANY endpoint with required `[FromQuery]` primitive (int, double, decimal) that's omitted from URL:
  - `int page` missing on `/admin/providers, /guides/me/applications, /content-core/{attachments,entity-tags}`
  - `int pageSize` missing on `/guides/me/applications`
  - `double northLat` missing on `/places/map/viewport`
  - `decimal lat` missing on `/seo/weather`
- **Impact:** Universal 500 LEAK on a Class A bug pattern. Trivially triggered by any client misreading the API. Compliance + security failure.
- **Root cause:** Two-bug cascade. (1) Model binding for required primitive query params fails → throws `BadHttpRequestException`. (2) The global exception middleware catches it but emits the full `exception` field even outside Development.
- **Fix (two coordinated changes):**
  1. **Pattern fix (preferred):** Replace every `[FromQuery] int page, int pageSize` with `[FromQuery] PagingQuery query` where `record PagingQuery(int Page = 1, int PageSize = 20)`. Eliminates the crash entirely.
  2. **Defense-in-depth:** Strip `exception` field outside Development (see F2 fix). This stops the leak even if (1) is incomplete.
  3. Add 2 cross-cutting tests: missing-param returns 400 (not 500); 500 responses contain no `exception` field outside Dev.

---

## Cross-references

- **F2 + F5 + F13** share the same root cause (info-leak global handler). Fix them together.
- **F3 + F4 + F9** share the same root cause (SecurityDbInitializer.SeedRolePermissions too restrictive). One edit fixes all three.
- **F1 + F11.wishlist + F11.support-tickets** are independent feature gaps that need dedicated implementation.
- **F10** may explain why some "missing" Messaging endpoints in F11 appear gone — hub routing namespace issue.

---

## Verified working — DO NOT REGRESS

These are confirmed working end-to-end. Any regression should be caught by the matching scenario in `Agents\Tests\Playwright-*.md`:

- [x] 24 seeded users (16 .local + 8 .test) all present
- [x] 8 canonical roles (matches `AppRoles` enum)
- [x] 4 ProviderApplication rows (1 Pending + 3 Approved) with correct types
- [x] 2 places, 2 bookings, 1 blog, 1 redirect, 2 sitemap entries, 1 notification template
- [x] JWT issue + verify: admin gets ~300 Permission claims
- [x] Rate limiter triggers at 13 requests / 10 sec → 429
- [x] RFC 7807 problem doc format on 401, 404 (clean) and 500 (leaky)
- [x] Suspended account blocked from login (401 "not currently active")
- [x] `/health` reports 4 healthy checks (sqlserver, azure-translator, outbox-dead-letters, booking-bg)
- [x] Outbox shows no dead letters
- [x] traceId propagation works through success and error responses
- [x] `/sitemap.xml` returns valid XML
- [x] FluentValidation fires correctly on empty-GUID route params
- [x] ContentCore categories return 3 items with 3 translations each (i18n verified)
- [x] ContentCore tags return 6 items
- [x] Admin dashboard returns `{revenue, bookings, users, alerts}`
- [x] Finance admin dashboard returns `{grossRevenue, platformCommissionEstimate, providerNetEstimate, completedPaymentCount}`

---

**Next:** see `Gap-Report-2026-05-29.md` for the prioritized batch-fix plan.
