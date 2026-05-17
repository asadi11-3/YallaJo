# Finance Module — Final Acceptance Gate

> **Tech Lead signs off before declaring the Finance sprint closed (Thu 2026-10-15 17:00).**

---

## 1. Code Quality
- [ ] All 6 task PRs (T1..T6) merged.
- [ ] `dotnet build` green for every Finance project + 2 test projects.
- [ ] No new TODOs in `Finance/` outside `# TODO Phase 3` markers.
- [ ] All command handlers inject `ILogger<THandler>` + invalidate `RemoveByTagAsync` after SaveChanges.
- [ ] All queries implement `ICacheableQuery`.
- [ ] No bare `RequireAuthorization()` — `rg "RequireAuthorization\(\)\s*$" Finance/Finance.Presentation/` returns 0.
- [ ] Webhook endpoint uses `.AllowAnonymous()` (verified by `rg ".AllowAnonymous()" Finance/Finance.Presentation/Webhook` returns 1).
- [ ] No `ICurrentUser` in handlers EXCEPT documented ownership comparisons (02-critical-rules.md §F-R11).
- [ ] `Finance.Tests.Unit` ≥ 60 tests green.
- [ ] `Finance.IntegrationTests` ≥ 15 tests green.
- [ ] Permission catalog parity test passes (10-cross-cutting.md §3).
- [ ] PCI audit tests pass (10-cross-cutting.md §7).

## 2. Endpoint Smoke Test

| # | Method | Path | Expected | Notes |
|---|---|---|---|---|
| 1 | POST | `/payments/initiate` | 201 + `paymentId, gatewayPaymentId, redirectUrl` | T1 happy |
| 2 | POST | `/payments/initiate` | 422 `Payment.BookingNotEligible` | T1, booking not in AwaitingPayment |
| 3 | POST | `/payments/initiate` | 200 + existing details (idempotent) | T1, duplicate call |
| 4 | POST | `/payments/webhook` (signed) | 200 | T2 happy |
| 5 | POST | `/payments/webhook` (bad signature) | 403 `Payment.WebhookSignatureMismatch` | T2 |
| 6 | POST | `/payments/webhook` (duplicate eventId) | 200 noop | T2 idempotency |
| 7 | POST | `/payments/{id}/refund` | 201 + refund | T2 happy |
| 8 | POST | `/payments/{id}/refund` | 422 `Refund.AmountExceedsRefundable` | T2 |
| 9 | POST | `/payments/{id}/refund` | 409 `Refund.PaymentNotCompleted` | T2 |
| 10 | GET | `/payments/{id}` | 200 owner/provider/admin | T2 |
| 11 | GET | `/payments/{id}` | 403 stranger | T2 |
| 12 | GET | `/payments/my-payments` | 200 cursor | T2 |
| 13 | GET | `/payments/admin/all` | 200 + filters | T2 |
| 14 | GET | `/invoices/my-invoices` | 200 | T3 |
| 15 | GET | `/invoices/provider/my-invoices` | 200 (provider role) | T3 |
| 16 | GET | `/invoices/{id}` | 200 | T3 |
| 17 | GET | `/invoices/{id}/download` | 200 application/pdf | T3 |
| 18 | GET | `/payouts/admin/pending` | 200 list | T4 |
| 19 | GET | `/payouts/provider` | 200 self-filter | T4 |
| 20 | GET | `/payouts/{id}` | 200 | T4 |
| 21 | POST | `/payouts/admin/trigger` | 202 + `{created, skipped, onHold}` summary | T4 |
| 22 | POST | `/payouts/{id}/approve` | 200, status Completed (small) OR Pending until gateway webhook (large) | T4 |
| 23 | POST | `/payouts/{id}/approve` | 409 `Payout.NotPending` (already approved) | T4 |
| 24 | GET | `/commissions` | 200 list | T6 |
| 25 | POST | `/commissions` | 201 + commissionRuleId | T6 |
| 26 | POST | `/commissions` | 409 `CommissionRule.OverlapTier` | T6 |
| 27 | PUT | `/commissions/{id}` | 200 | T6 |
| 28 | DELETE | `/commissions/{id}` | 204 + outbox row | T6 |

## 3. Outbox / Inbox Round-Trip

| Action | Outbox name | Downstream effect |
|---|---|---|
| Payment succeeds (T2) | `finance.payment.completed.v1` | Booking marks booking Confirmed; Invoice generates (T3 handler); Messaging sends receipt |
| Payment fails (T2) | `finance.payment.failed.v1` | Booking releases SlotLock + restores capacity |
| Refund completes (T2) | `finance.refund.completed.v1` | Booking updates refundedAmount; Messaging sends refund confirmation |
| Refund finally fails after 3 retries (T5) | `finance.refund.failed.v1` | Messaging admin alert (page on-call) |
| Invoice generated (T3) | `finance.invoice.generated.v1` | Messaging "invoice ready" email |
| Payout batch created (T5) | `finance.payout.scheduled.v1` | Messaging provider summary email |
| Payout completed (T4 approve) | `finance.payout.completed.v1` | Messaging notifies provider with gateway reference |
| Commission rule upserted (T6) | `finance.commission-rule.upserted.v1` | Booking refreshes BookingCommissionSnapshot |
| Commission rule deleted (T6) | `finance.commission-rule.deleted.v1` | Booking removes snapshot |

## 4. Background Services Live Test (24h soak)

- [ ] `RefundRetryService` ticks ≈ 96 times in 24h; processes any failed refund within 15 min of failure.
- [ ] `PayoutBatchingService` schedules next Sunday correctly; manual override `TargetTimeUtc=now+2min` triggers a batch.
- [ ] OTEL counters all zero failures.
- [ ] No uncaught exceptions in Serilog file.

## 5. Performance Sanity

| Endpoint | p95 | Hard ceiling |
|---|---|---|
| POST `/payments/initiate` | < 600 ms | < 1.5 s (gateway call dominates) |
| POST `/payments/webhook` | < 200 ms | < 500 ms |
| POST `/payments/{id}/refund` | < 800 ms | < 2 s |
| GET `/payments/{id}` (cached) | < 80 ms | < 200 ms |
| GET `/invoices/{id}/download` first render | < 2 s | < 5 s |
| GET `/invoices/{id}/download` cached PDF | < 300 ms | < 600 ms |
| POST `/payouts/admin/trigger` 100 providers | < 5 s | < 15 s |

## 6. Documentation Hygiene
- [ ] XML docs on every new Command/Query record.
- [ ] All 22 Finance permissions listed in `Agents/permissions-inventory.md`.
- [ ] Sprint folder moves to `Agents/decisions/closed/Finance/` (preserve all 9 files).
- [ ] Master `Phase1-Phase2-Completion-INDEX.md` Finance row → 🟢 + closed/ link.
- [ ] `AGENTS.md` (repo-root) updated.
- [ ] `agent-context.md §11.1` Finance row → ✅ Complete (with caveat: "Phase 1 only — Discount/Dispute/Loyalty/Referral/Subscription deferred to Phase 3").
- [ ] `Agents/error-log.md` new entries for any new gotchas hit.
- [ ] `Agents/decisions/` — ADR-007 "Commission CRUD lives in Finance; Booking holds snapshot via inbox" (already proposed in Booking sprint, FINALIZE here).

## 7. Sprint Retro & Demo (Fri 2026-10-16 11:00 AST)

Demo by Mohammad:
1. Live POST /payments/initiate → gateway redirect → simulated success webhook → booking Confirmed.
2. Refund flow demo with refund failure + retry recovery.
3. Weekly payout batch live demo (advance clock) → admin approves → gateway completes.
4. Commission rule edit demo + verify Booking inbox snapshot updates within 30s.
5. Invoice PDF download (visual quality check).

Retro doc at `Agents/decisions/closed/Finance/_retro.md`.

## 8. Sign-Off

| Role | Name | Date | Signature |
|---|---|---|---|
| T1 owner | Mahmoud | _____ | _____ |
| T2 owner | Mahmoud | _____ | _____ |
| T3 owner | Fadwa | _____ | _____ |
| T4 owner | Mohammad | _____ | _____ |
| T5 owner | Mohammad | _____ | _____ |
| T6 owner | _____ | _____ | _____ |
| Tech Lead | _____ | _____ | _____ |
