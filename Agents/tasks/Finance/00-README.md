# Finance Module — Sprint Manifest

> **Predecessor sprint:** [`../Booking/`](../Booking/00-README.md) (closes Sun 2026-08-09)
> **This sprint covers:** Wave 5 second half — Payments (escrow + webhook + refund), Payouts (admin trigger + weekly batch with tiered commission), Invoices (PDF), Commission Rules CRUD, plus 2 hosted services.
> **Difficulty vs Booking:** ⚙️⚙️⚙️⚙️ (4/5) — money handling + PCI proximity + idempotency-critical webhooks
> **Endpoint count:** **17 HTTP endpoints + 2 BG services**
> **Working-day estimate:** **40 working days × 4 devs ≈ 200 person-hours**
> **Window:** Mon **2026-08-17** → Thu **2026-10-15** (9 weeks, 45 working days incl. buffer)

---

## 0. Sprint Window & Hard Deadlines

| Milestone | Date | Owner |
|---|---|---|
| Pre-work cut | Fri 2026-08-14 17:00 | Tech Lead |
| Sprint kickoff (all-hands 60 min) | Mon **2026-08-17** 09:00 AST | Tech Lead |
| PW merge deadline | Tue 2026-08-18 17:00 | Tech Lead |
| Earliest task start | Wed 2026-08-19 09:00 | Per-task owner |
| Mid-sprint integration freeze | Sun 2026-09-21 17:00 | All devs |
| Hard PR cutoff | Wed 2026-10-14 17:00 | All devs |
| Hard merge-to-main cutoff | Thu 2026-10-15 17:00 | Tech Lead |
| Sprint retro + demo | Fri 2026-10-16 11:00 AST | Tech Lead |

Working week Sun→Thu (5 days). Daily standup 09:30 AST 15 min hard cap.

---

## 1. Working Days & Person-Hour Budget

| Metric | Value |
|---|---|
| Working days | 45 |
| Hours / day / dev | 5 (focus time after standup + ceremony) |
| Devs | 4 |
| Total hours | 900 |
| Task hours | 200 (real estimate below) |
| Review hours | 60 |
| Ceremony hours | 60 (kickoff + standup + retro) |
| Buffer | 580 (huge buffer because money-handling never goes to plan) |

---

## 2. Team Members & High-Level Allocation

| Name | Level | Tasks | Endpoints | BG services | Est hours | Hard deadline |
|---|---|---|---|---|---|---|
| **Mohammad** (LEAD) | Senior | T4 (Payouts), T5 (BG services) | 5 | 2 | 60 | Wed 2026-10-14 |
| **Mahmoud** | Intermediate | T1 (Payments init), T2 (Payments webhook + refund) | 6 | 0 | 60 | Wed 2026-10-14 |
| **Fadwa** | Intermediate | T3 (Invoices + PDF) | 3 | 0 | 36 | Wed 2026-10-14 |
| **NEW HIRE (TBD)** | Junior | T6 (Commission Rules CRUD) | 3 | 0 | 24 | Wed 2026-10-14 |
| Tech Lead | — | PW + review + PCI compliance audit | 0 | 0 | 20 | continuous |

If no junior is hired by 2026-08-17, Fadwa absorbs T6 and her T3 deadline shifts to Mon 2026-10-19. Master INDEX gets edited accordingly.

---

## 3. Task Summary

| Task | Owner | Hours | Endpoints | Description |
|---|---|---|---|---|
| [T1 — Payments initiate](04-task-payments-initiate.md) | Mahmoud | 28 | 1 | POST /payments/initiate — calls IPaymentGateway, creates Payment Pending |
| [T2 — Payments webhook + refund](05-task-payments-webhook-refund.md) | Mahmoud | 32 | 2 | POST /payments/webhook (idempotent), POST /payments/{id}/refund + 3 GET reads |
| [T3 — Invoices + PDF](06-task-invoices-pdf.md) | Fadwa | 36 | 3 | Auto-create invoice on payment success, GET my-invoices / provider/my-invoices / {id}/download (PDF via QuestPDF) |
| [T4 — Payouts](07-task-payouts.md) | Mohammad | 32 | 5 | GET admin/pending + provider + {id}, POST admin/trigger (weekly batch), POST {id}/approve |
| [T5 — BG services](08-task-background-services.md) | Mohammad | 28 | 0 | PayoutBatchingService (weekly Sun midnight UTC), RefundRetryService (15min) |
| [T6 — Commission Rules CRUD](09-task-commission-rules.md) | Junior/Fadwa | 24 | 3 | GET /commissions (admin), POST /commissions, PUT /commissions/{id}, DELETE — emits `finance.commission-rule.upserted/deleted.v1` consumed by Booking |
| **TOTAL** | | **180h** | **17** | |

(20h shortfall vs the 200 estimate is review/buffer; matches table 1.)

---

## 4. Integration Events Map

**This sprint emits (9 events):**
| Logical name | Producer task | Consumed by |
|---|---|---|
| `finance.payment.completed.v1` | T2 (webhook handler) | Booking inbox (confirms booking), Analytics (revenue tally), Messaging (receipt email) |
| `finance.payment.failed.v1` | T2 | Booking inbox (releases slot lock, restores capacity); Messaging (failure email) |
| `finance.refund.initiated.v1` | T2 | Booking inbox (updates booking refundedAmount) |
| `finance.refund.completed.v1` | T2 | Booking inbox + Messaging (refund confirmation email) |
| `finance.refund.failed.v1` | T5 (RefundRetryService after 3 attempts) | Messaging admin alert |
| `finance.invoice.generated.v1` | T3 | Messaging (invoice-available email) |
| `finance.payout.scheduled.v1` | T5 (PayoutBatchingService) | Messaging (provider payout summary email) |
| `finance.payout.completed.v1` | T4 (admin approve) | Messaging (provider notification with reference number) |
| `finance.commission-rule.upserted.v1` | T6 | Booking inbox (refreshes BookingCommissionSnapshot) |
| `finance.commission-rule.deleted.v1` | T6 | Booking inbox (removes BookingCommissionSnapshot) |

**This sprint consumes (5 events from Booking + Accounts):**
| Logical name | Producer | Handler in this sprint |
|---|---|---|
| `booking.tour-booking.created.v1` | Booking T4 | Stamps Payment.BookingId reference (Mohammad T4 pre-step) |
| `booking.tour-booking.completed.v1` | Booking T5 | Triggers escrow release scheduling (Mohammad T4) |
| `booking.tour-booking.cancelled.v1` | Booking T5 | Triggers refund creation (Mahmoud T2 inbox handler) |
| `accounts.provider.subscription-changed.v1` | Accounts (existing) | Re-evaluates commission tier on next payout (Mohammad T4) |
| `accounts.provider-bank-account.verified.v1` | Accounts (future) | Releases held payouts (deferred — flag as TODO if Accounts not ready) |

---

## 5. Out-of-Scope for This Sprint

| Item | Reason | When |
|---|---|---|
| Discount creation / approval / lifecycle | Phase 4 Feature 24 — needs its own sprint | Phase 4 |
| Loyalty points earning / redemption | Phase 3 Feature 17 | Phase 3 sprint |
| Subscription billing (Free / Basic / Premium) | Phase 3 Feature 16 | Phase 3 sprint |
| Dispute Center | Phase 3 Feature 19 | Phase 3 sprint |
| Referral system | Phase 3 Feature 17 | Phase 3 sprint |
| Subscription tier resolution in T4 commission lookup | Stub returns FreeTier; real lookup ships with Subscriptions sprint | Phase 3 |
| Multi-currency FX | Bookings stay in their booking currency through payout; cross-currency held as TODO | Phase 4 |

These features have entity rows already in `Finance.Domain/Entities/` (b5 — 19 entities); we leave them as stub property bags during this sprint, no endpoints, no handlers.

---

## 6. Reading Order for New Joiners

1. `Agents/agent-context.md` §0..§3 (rules + arch)
2. `Agents/guide.md` (code patterns)
3. `Agents/YallaJo.md` §Finance (endpoint spec)
4. `Agents/tasks/Phase1-Phase2-Completion-INDEX.md` §4 (16 critical rules)
5. `Agents/decisions/closed/ContentBlogs-ContentSeo-team-tasks.md` (template for sprint mechanics)
6. `Agents/decisions/closed/Booking/` (predecessor — same template)
7. **THIS folder** in order: `00-README` → `01-pre-work` → `02-critical-rules` → `03-entities-matrix` → tasks `04..09` → `10-cross-cutting` → `99-acceptance-gate`
8. PDFs: Endpoints.pdf §Wave 5, YallaJo Business Rules §1.5 (Payment & Refund) and §1.5 (Payout)
