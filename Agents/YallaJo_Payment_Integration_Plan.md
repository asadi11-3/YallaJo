# YallaJo — Payment Service Provider (PSP) Integration Plan

> **Companion document to** `YallaJo_Business_Strategy_2026.md`
> **Scope**: Local Jordan rails (eFAWATEERcom, CliQ) + worldwide PSPs + a marketplace-grade multi-PSP routing architecture for YallaJo's .NET 8 Finance module.
> **Goal**: Minimize total payment cost (target <2.1% blended fee at Year 2 vs industry typical 2.9-3.4%) while maximizing approval rates, settlement speed, and refund/dispute control.

---

## 1. Executive Summary

YallaJo's marketplace economics live or die on payment fees. At Year 2 scale ($26M GBV), every **0.5% of payment cost = $130K/yr**. The right stack is **not one PSP — it's a multi-PSP router** that sends each transaction to the cheapest, highest-approval-rate provider available for that user's country, card, and currency.

**Recommended stack**:

| Layer | Primary | Backup | Why |
|---|---|---|---|
| **Jordan domestic — bank rails** | **CliQ via Network International** (QR + Request-to-Pay) | eFAWATEERcom via MadfoatCom (bill presentment) | Lowest cost. CliQ is near-free instant settlement; eFAWATEERcom is great for invoice-style bookings & GCC users with Jordan accounts |
| **Jordan domestic — cards** | **Network International Jordan** (local acquirer) | HyperPay (KSA→Jordan) | Local acquirer = better Visa/MC interchange, JOD settlement |
| **GCC users (Saudi, UAE, Kuwait, Qatar, Bahrain, Oman)** | **Tap Payments** | PayTabs (KSA focus) | Tap has widest GCC method coverage (mada, KNET, Benefit, STC Pay, QPAY, OmanNet); transparent 2.75% + $0.30 |
| **International (Europe, NA, Asia)** | **Stripe** | Checkout.com (at $50K+/mo) | Stripe = best developer ergonomics, 2.9% + $0.30; Checkout.com switches in at volume on Interchange++ |
| **BNPL (optional, GCC)** | **Tabby** + **Tamara** | — | 25-40% conversion lift in 25+ JOD basket sizes; ~5% fee but cart-lift > fee cost |
| **Payment orchestration** | Built in-house in `YallaJo.Finance` module | Yuno / PayModum as fallback | Already have CQRS Finance module — keep IP in-house |

**Expected blended payment cost at Year 2 mix** (40% Jordan domestic, 35% GCC, 25% international):

| Rail | Mix % | Effective fee | Weighted cost |
|---|---|---|---|
| CliQ | 18% | 0.6% | 0.108% |
| eFAWATEERcom | 12% | 0.8% | 0.096% |
| Local cards (Network Intl) | 10% | 2.2% | 0.220% |
| Tap Payments (GCC) | 35% | 2.55% | 0.892% |
| Stripe (intl) | 25% | 2.95% | 0.738% |
| **Blended** | **100%** | — | **~2.05%** |

vs. **single-PSP scenario** (Stripe-only): ~2.95% blended → **annual savings ≈ 0.9% × $26M GBV ≈ $234K/yr at Y2 scale; $700K+/yr at Y3 scale**.

---

## 2. Local Jordan Payment Rails

### 2.1 eFAWATEERcom — Bill Presentment & Payment

**What it is**
- Jordan's national **Electronic Bill Presentment and Payment** (EBPP) platform.
- Owned by **JoPACC** (Jordan Payments & Clearing Company), supervised by the **Central Bank of Jordan**, operated by **MadfoatCom** (Eng. Nasser Saleh, est. 2011, 200+ team in Amman).
- It is **not a card gateway** — it's a "bill-with-reference" system. The merchant publishes a bill; the customer pays via any participating channel (bank app, ATM, teller, Jordan Post, agent, eFAWATEERcom web/app).
- **Currency**: JOD only. **Geography**: Jordan only.

**Why it matters for YallaJo**
1. **Trust**: regulator-supervised, used daily by millions of Jordanians, government utility bills, taxes, customs, tuition fees, insurance, eCommerce.
2. **No card friction**: GCC visitors with Jordanian bank accounts (lots of Saudis), domestic Jordanians who avoid cards, corporate bookings, group bookings, multi-day tour invoices — all pay easier via eFAWATEERcom.
3. **Lower cost than cards**: Card processing is 2.5-3% + fees; eFAWATEERcom transaction fees are flat **JOD 0.20 – 5.00 depending on biller, service type, and channel**. Online payments via bank app are cheapest; cash-based POS slightly higher.
4. **Government tailwind**: All Jordanian government services route through eFAWATEERcom. The platform is the de-facto rail for serious payments.

**Integration mechanics**
- **Direct merchant onboarding**: contact MadfoatCom (Amman HQ) to register YallaJo as a **biller**. They issue Biller Code + service identifiers + sandbox + production credentials.
- **Through aggregators** (faster, slightly higher fee): Trinavo, ProgressSoft, or banks that resell eFAWATEERcom (Jordan Kuwait Bank, Cairo Amman Bank, Bank of Jordan, all major banks).
- **API model**: REST-based. YallaJo generates an invoice → POSTs to MadfoatCom with a reference number, amount, due date, customer reference → MadfoatCom returns a confirmation. Customer pays through any channel. MadfoatCom webhooks YallaJo on payment received.
- **Reconciliation**: end-of-day file + webhook callbacks. Settlement to YallaJo's Jordanian bank account is **T+1 to T+2**.

**Best-fit YallaJo use cases**
1. **Multi-day tour bookings** (3+ days, value > 200 JOD) — high-cart-value users prefer bank-rail payment.
2. **Group bookings** (5+ pax) — typically paid by one organizer via bank channels.
3. **Corporate bookings** — invoiced flow, customer pays from their corporate bank account.
4. **YallaJo+ annual subscriptions** — 99 JOD/yr or similar invoice flow.
5. **Provider subscription billing** — Basic 144 JOD/yr, Premium 336 JOD/yr — providers will much prefer to pay this via their business bank app.

**Watch-outs**
- eFAWATEERcom is **invoice/bill paradigm, not impulse checkout**. The user does not pay in real-time at checkout — they get a reference number and pay later. **Cannot enforce immediate booking confirmation** in the same way as a card transaction.
- For YallaJo's **10-min/15-min slot lock** flow, eFAWATEERcom needs special handling: either extend the lock to 60-90 min when "Pay via Bank" is selected, OR present eFAWATEERcom as a "Reserve & Pay Later" option with looser confirmation rules.
- Refunds are **NOT instant** through eFAWATEERcom — the merchant initiates a reverse payment, which can take 5-10 business days to land in the customer's bank account. Already aligned with YallaJo's documented "card/bank 5-14 business days" policy.

### 2.2 CliQ — Jordan Instant Payments

**What it is**
- Jordan's **real-time instant payment system**, owned and operated by **JoPACC**, supervised by CBJ.
- Conceptually equivalent to **UPI (India)**, **PIX (Brazil)**, **Aani (UAE)** — instant, 24/7, bank-account to bank-account, JOD only, Jordan only.
- **19 banks participate**, with **27 financial institutions** able to access QR payments.
- Limits: typical **10,000 JOD/day per individual** (set per bank, can vary).
- Two integration models for merchants:
  - **QR code at POS / online** — customer scans, pays instantly, you get notified.
  - **Request-to-Pay (RTP)** — merchant pushes a pay request to a phone number / CliQ alias; customer approves in their bank app.

**Why it matters for YallaJo**
1. **Near-zero cost**: P2P currently free; merchant MDR being introduced is **very minimal** (JoPACC explicitly stated goal: "reduce costs of digital payment at merchants"). Expected merchant fee range: **0.3-0.8% of transaction**, far below 2.5-3% cards.
2. **Instant settlement**: funds in your account in seconds, not T+1-T+3. **Massive cash-flow advantage** for a marketplace with escrow (you can release escrow faster + reinvest float).
3. **Mobile-first UX**: 84% of Jordanian adults are on social/mobile; CliQ is integrated in every bank's mobile app.
4. **No card data risk**: no PCI scope expansion.
5. **Refund-friendly**: instant return payments — aligns with YallaJo's "wallet/credit instant refund" rule.

**Integration mechanics**
- **Direct CliQ access = banks only.** YallaJo cannot become a CliQ participant directly.
- **Merchant integration** is through a **CliQ-enabled acquirer**:
  - **Network International Jordan** (launched CliQ on POS in July 2022) — primary recommended path.
  - **Bank-direct integration** — partner with a single bank (Cairo Amman, Bank of Jordan, JKB, ABC) that exposes a merchant CliQ API. Tighter integration but locks you to that bank's tech.
  - **MadfoatCom/JoPACC fintech partnerships** — increasingly available for licensed fintechs.
- **API contract** (via Network International or bank API):
  - `POST /cliq/qr-code` → returns dynamic QR code (encodes amount + reference)
  - `POST /cliq/request-to-pay` → sends RTP to customer's CliQ alias
  - Webhook `POST /yallajo/finance/webhooks/cliq` → notifies on payment success/fail
  - `POST /cliq/refund` → instant reverse payment

**Best-fit YallaJo use cases**
1. **Domestic Jordanian day-trip bookings** (<150 JOD) — Petra day tour, Wadi Rum jeep, Aqaba snorkel — QR at checkout = sub-30-second pay.
2. **In-tour purchases** (souvenir, lunch upgrade, extra activity during a tour) — Guide opens QR on his device, customer scans, instant.
3. **Provider payouts** — actually CliQ is bidirectional. **YallaJo can pay providers via CliQ** instead of bank-transfer-with-fee for the weekly Sunday batch. Each payout = instant + free or near-free.
4. **Refunds** — instant reverse to customer's bank or wallet.
5. **YallaJo Wallet top-ups** — users can top up their YallaJo wallet via CliQ.

**Watch-outs**
- 10,000 JOD/day per individual cap — fine for >99% of bookings, but consider for high-end multi-day packages (>10K JOD = pay over 2 days or use card/bank transfer for the balance).
- Currently no support for foreign-issued bank accounts. So CliQ is for Jordan-bank-account holders only.
- Limits and fee model are evolving — JoPACC may change rates. Build the integration to read fee config dynamically.

### 2.3 Local Jordan Card Acquirer — Network International (or alternative)

**What it is**
- For domestic Visa/Mastercard transactions from Jordan-issued cards, route through a **local acquirer** in Jordan rather than an international PSP. Local acquirer = lower interchange + JOD settlement + no FX cost.

**Recommended**
- **Network International Jordan** — the dominant card acquirer in Jordan, also operates CliQ POS rails. **One-vendor consolidation**: cards + CliQ + (potentially) eFAWATEERcom via their gateway.
- Alternative: **HyperPay** (KSA-headquartered, but supports Jordan as a market).

**Expected fees**
- Local Jordan card: **2.0-2.5% + JOD 0.10**, JOD settlement, T+1.
- Non-Jordanian card via local acquirer: 2.5-3.0% + FX margin if customer pays in JOD. Often **cheaper to route foreign cards through Stripe or Tap instead** for better approval rates.

---

## 3. Worldwide PSPs — Comparison & Recommendations

### 3.1 Headline Comparison Table (2025-2026 data)

| PSP | Standard Card Fee | International Card Fee | Settlement | MENA Methods | Marketplace Splits | Jordan Support | Best Use Case |
|---|---|---|---|---|---|---|---|
| **Tap Payments** | 2.75% + $0.30 | 3.25% + FX | T+2-3 | mada, KNET, Benefit, QPAY, OmanNet, STC Pay, Apple Pay | Basic (good for our needs) | ✅ Yes | **GCC users — RECOMMENDED PRIMARY for GCC** |
| **PayTabs** | 2.75-2.85% + $0.25 | ~3.9% | T+2-3 | mada, Sadad, Apple Pay, STC Pay (limited), KNET | **Advanced** (best marketplace splits in MENA) | ✅ Yes | Multi-country MENA + advanced split-payout marketplaces |
| **Stripe** | 2.9% + $0.30 (US/EU) / 3.65% non-US cards | +1% on intl + currency conversion | T+2-7 | Apple Pay, Google Pay, Klarna, no mada | Stripe Connect (excellent) | ❌ Limited (no direct Jordan acquiring, can take MENA cards but no JOD settlement) | **International (Europe, NA, Asia)** — best developer DX |
| **Checkout.com** | 2.3-2.9% (volume-tier) / Interchange++ | Same model | T+1-3 | mada, STC Pay, KNET, Tamara, Tabby (direct acquiring UAE) | Excellent | ✅ Yes via partner | **Enterprise — kick in at $50K+/mo, replace Stripe** |
| **HyperPay** | 2.5-3.5% | Custom | T+1-3 | mada, STC Pay, Apple Pay (KSA strong) | Standard | ✅ Yes | KSA-primary; backup to Tap |
| **MyFatoorah** | From 2.5% (+0.5-1% for marketplace split) | Custom | T+2-3 | KNET, mada, Benefit, Apple Pay, QPAY, recurring | Good (split payments + recurring) | ✅ Yes (GCC focus) | Kuwait-primary; backup for marketplace splits |
| **Network International** | 2.5-3.0% custom | + FX markup | T+1-3 | Cards + CliQ + KNET + mada | Custom | ✅ **Primary Jordan acquirer** | **Jordan domestic cards + CliQ** |
| **Amazon Payment Services** | 2.5-3.5% custom | Custom | T+2-3 | mada, SADAD, KNET, Apple Pay, installments | Yes | ✅ Yes | Enterprise high-volume, slower onboarding |
| **Telr** | From 2.49% + AED 0.50 | Variable | T+2-3 | mada, Apple Pay, social/links | Basic | ✅ Yes | UAE-primary; backup |
| **Tabby (BNPL)** | 2.79-5.99% | N/A | T+2-14 | Split into 4 payments | N/A (BNPL) | ❌ UAE+KSA+KW+BH+QA only | **BNPL add-on for GCC bookings** |
| **Tamara (BNPL)** | 2.5-6% | N/A | T+2-30 | Split into 3-4 payments | N/A | ❌ KSA+UAE+KW only | **BNPL add-on (Saudi-strong)** |
| **PayPal** | 3.49% + fixed fee | + 1.5% FX | Instant→T+1 | Universal but expensive | Limited | ✅ Available | Last resort; high cost. Some Europe/NA tourists still demand it. |
| **Helcim** (N. America only) | 1.93% + 8¢ in-person, 2.49% + 25¢ online (Interchange-plus) | Custom | T+1-2 | Cards + Apple/Google Pay | Good API | ❌ No MENA | **Cheapest if YallaJo had N. American volume — but no MENA support, skip** |

### 3.2 Why NOT Stripe-Only

Many startups default to "just use Stripe". For YallaJo this is wrong because:

| Issue | Impact |
|---|---|
| **No mada support** | Saudi users (564K visitors H1 2025!) often fail at checkout — mada is the dominant Saudi domestic card scheme |
| **No KNET, Benefit, QPAY, OmanNet** | GCC local schemes have higher approval rates than Visa/MC across most banks |
| **No JOD settlement** | Forces FX conversion + foreign-account holding costs |
| **3.65% on non-US cards** | Most MENA cards = non-US = punitive fee tier |
| **Limited Jordan presence** | Stripe is not fully launched as a payment acquirer in Jordan — relies on Atlas/intermediate setups for JO businesses |
| **No CliQ / no eFAWATEERcom** | Cannot tap into the cheapest local rails |

**Stripe is right for**: international tourists from EU/UK/US/Canada/Australia paying with a Visa/MC/Amex in their home currency. It's the best DX in the world for that segment. Keep it for those flows only.

### 3.3 Why Tap Payments as GCC Primary

- **Transparent published pricing**: 2.75% + $0.30 standard, 3.25% intl. No "call sales for quote" black box.
- **Widest GCC method coverage**: mada (KSA), KNET (Kuwait), Benefit (Bahrain), QPAY (Qatar), OmanNet, STC Pay, Apple Pay, Samsung Pay — all in one integration.
- **Marketplace split payouts** (basic but sufficient for YallaJo's "provider commission + platform commission" model).
- **Fast onboarding**: 3-5 days vs PayTabs 7-14 days.
- **Arabic checkout**, native RTL — important for AR-first YallaJo UX.
- **Jordan support**: confirmed in their 2025 country list.

### 3.4 When to Add Checkout.com (Year 2)

At ~$50K/mo+ processing volume, request a Checkout.com Interchange++ quote. Expected savings:
- Tap @ 2.75% blended → Checkout.com Interchange++ blended ~2.2-2.5% on equivalent traffic
- That's **0.25-0.55% saved on ~30-35% of GBV** = **$30-50K/yr at Y2 → $90-150K/yr at Y3**
- Worth the harder onboarding once you have the volume.

---

## 4. Multi-PSP Architecture for YallaJo's `Finance` Module

YallaJo's `Finance` module (13 migrations already applied) is the natural home for this. Recommended internal architecture:

```
┌─────────────────────────────────────────────────────────────────┐
│  YallaJo.Web / YallaJo.Api  →  Booking checkout                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  YallaJo.Finance.Application                                     │
│  ├─ Commands: InitiatePaymentCommand,                            │
│  │            ConfirmPaymentCommand, RefundPaymentCommand        │
│  ├─ PaymentRouter (decides which PSP based on context)           │
│  └─ PSP abstractions: IPaymentProvider, IRefundProvider          │
└─────────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┬────────────────┐
              ▼               ▼               ▼                ▼
       ┌───────────┐  ┌──────────────┐  ┌──────────┐  ┌──────────────┐
       │  CliQ     │  │  eFAWATEER   │  │  Tap     │  │  Stripe      │
       │  (Network │  │   (Madfoat)  │  │ Payments │  │              │
       │   Intl)   │  │              │  │          │  │              │
       └───────────┘  └──────────────┘  └──────────┘  └──────────────┘
              │              │                │              │
              ▼              ▼                ▼              ▼
        Webhook         Webhook          Webhook        Webhook
        Receivers       Receivers        Receivers      Receivers
              │              │                │              │
              └──────────────┴────────────────┴──────────────┘
                              │
                              ▼
            ┌──────────────────────────────────────┐
            │  Outbox-driven settlement processor  │
            │  (existing CompositeOutboxProcessor) │
            └──────────────────────────────────────┘
                              │
                              ▼
            ┌──────────────────────────────────────┐
            │  Booking confirmed → Escrow held 7d  │
            │  → Provider payout (CliQ or bank)    │
            └──────────────────────────────────────┘
```

### 4.1 Payment Routing Decision Logic

```
Routing rules (priority order):

1. If currency == JOD AND user.country == JO AND amount <= 10000 JOD:
   → Offer CliQ (default), eFAWATEERcom (alternative), Local card via Network Intl (alternative)

2. If user.country in [SA, AE, KW, BH, QA, OM]:
   → Tap Payments primary
   → Fallback to PayTabs if Tap declines or for advanced split-marketplace flows

3. If user.country in [EU, UK, US, CA, AU, NZ, JP, etc.]:
   → Stripe primary (best UX, Apple Pay/Google Pay)
   → Fallback to Checkout.com when monthly volume justifies (Y2+)

4. If amount > 200 JOD AND multi-day booking AND user.country in [JO, GCC]:
   → ALSO offer BNPL (Tabby or Tamara per geography)

5. If booking is corporate / B2B / annual subscription:
   → Default to eFAWATEERcom (JOD invoice flow)
   → Fallback to bank wire (Network Intl)

6. If all primary PSPs fail (3DS decline, network issue):
   → Cascade retry to fallback PSP within 90 seconds
   → After 2 failures, surface friendly error + log fraud signal
```

### 4.2 Provider Payout Routing (Reverse Flow)

YallaJo's documented "weekly Sunday batch payout, 10 JOD minimum, escrow 7 days" works like this with the new stack:

```
Provider payout decision:
- If provider.country == JO AND provider.cliqAlias != null:
  → CliQ payout (instant, free or near-free)
- Else if provider.country == JO AND has verified bank account:
  → Bank transfer (Network Intl rail or direct via JOD bank)
- Else if provider.country in [GCC]:
  → Tap Payments payout API or local wire
- Else if provider.country international:
  → Wise / Payoneer / direct SWIFT (cost-optimize by country)
```

**Cost savings**: Switching 60% of Year-2 provider payouts from bank transfer ($2-5/payout) to CliQ (~$0.10-0.50) saves ~$8-12K/yr at Y2, ~$30K/yr at Y3.

### 4.3 Data Model Hints (.NET 8 + EF Core)

Already present per business rules (`decimal(19,4)`, never store raw card data). Add these tables to `Finance` module:

```
PaymentProvider        — config per PSP (CliQ, eFAWATEERcom, Tap, Stripe, etc.)
   ├─ Code (enum)
   ├─ IsActive
   ├─ SupportedCurrencies
   ├─ SupportedCountries
   ├─ FeePercent + FeeFixed
   ├─ MinAmount + MaxAmount
   └─ Priority (for routing tie-break)

PaymentAttempt         — every initiation attempt
   ├─ BookingId
   ├─ ProviderCode
   ├─ Amount + Currency
   ├─ Status (Initiated, Pending, Succeeded, Failed, Cancelled, Refunded)
   ├─ ProviderTransactionId (external)
   ├─ FailureReason
   ├─ AttemptNumber (for retries)
   └─ NextProviderToTry (if cascade routing)

PaymentMethod          — saved customer methods (tokens only, never raw card)
   ├─ UserId
   ├─ ProviderCode
   ├─ ProviderToken (PSP-side tokenized identifier)
   ├─ MethodType (Card, CliQAlias, BankAccount, Wallet, BNPL)
   ├─ Last4 / Brand (display only)
   └─ ExpiresAt

PaymentRefund          — already partially in your schema
   ├─ PaymentAttemptId
   ├─ Reason
   ├─ Amount
   ├─ Status (Pending, Approved, Rejected, Processing, Completed)
   ├─ ProviderRefundId
   └─ DiscountUsages snapshot (per business rules)

PayoutBatch            — weekly Sunday batch
   ├─ ProviderId (the merchant provider on YallaJo, not the PSP)
   ├─ TotalAmount + Currency
   ├─ PaymentMethod (CliQ alias / Bank account / international wire)
   ├─ Status (Pending, Processing, Completed, Failed)
   └─ FailedAttemptsCount
```

---

## 5. Compliance & Risk Considerations

### 5.1 PCI-DSS Scope

- **YallaJo never touches raw card data** (already documented in business rules) — every PSP integration uses **hosted fields / iframes / tokenization**.
- This keeps YallaJo at **PCI-DSS SAQ A** scope (the lightest level) — significant cost & audit savings.
- For Tap, Stripe, Checkout.com, PayTabs: all support hosted-checkout & Elements/iframe patterns. Use these exclusively.

### 5.2 Regulatory

- **Jordan**: CBJ + JoPACC oversee eFAWATEERcom and CliQ. Working with a licensed acquirer (Network International Jordan) means YallaJo inherits much of the compliance work.
- **KSA expansion**: SAMA (Saudi Central Bank) licenses PSPs. Tap and PayTabs are both SAMA-licensed — no extra burden on YallaJo.
- **UAE expansion**: Central Bank of UAE licenses; same logic applies.
- **PDPL (KSA Personal Data Protection Law) + UAE NESA**: data residency. For users in those countries, payment metadata stored in Azure UAE North region is sufficient; raw card data is never on YallaJo servers anyway.
- **AML/KYC for providers** (per business rules already): not a payment-PSP problem but must be enforced before any payout is sent — already addressed in Finance module design.

### 5.3 Chargeback & Dispute Management

- Each PSP exposes dispute webhooks. Build a **unified `DisputeReceived` integration event** in the Outbox so YallaJo's existing 48h provider response SLA flow can pick up disputes from any PSP uniformly.
- For CliQ and eFAWATEERcom: chargeback risk is **near-zero** because these are bank-rail transactions with sender-initiated authorization (no card to dispute).
- For cards: industry chargeback rates 0.5-1.2%; YallaJo's coded controls (3-flag auto-decline, escrow, refund policy snapshot) push expected rate to **0.5-0.7%**.

### 5.4 3D Secure 2.x

- Mandatory in most MENA + EU markets now.
- All recommended PSPs handle 3DS 2.x natively with frictionless flow + step-up challenge as needed.
- Configure **risk-based routing**: high-value bookings (>500 JOD) always go through step-up 3DS; low-value via frictionless.

---

## 6. Cost Modeling at Year-2 Scale ($26M GBV)

### 6.1 Single-PSP Baseline (Stripe-only)

| Item | Value |
|---|---|
| GBV | $26,000,000 |
| Effective Stripe fee (mixed local + intl) | 2.95% + $0.30 × 1.1M txns = $767K + $330K = **$1.097M** |
| Payouts via bank wire (~$3 × 50K payouts/yr) | $150K |
| **Total payment cost** | **$1.247M / yr** |
| **% of GBV** | **4.80%** |

### 6.2 Multi-PSP Optimized Stack

| Rail | GBV share | Fee % | Cost |
|---|---|---|---|
| CliQ | 18% ($4.68M) | 0.6% | $28.1K |
| eFAWATEERcom | 12% ($3.12M) | 0.8% | $25.0K |
| Network Intl local cards | 10% ($2.60M) | 2.2% | $57.2K |
| Tap Payments (GCC) | 35% ($9.10M) | 2.55% | $232.1K |
| Stripe (international) | 25% ($6.50M) | 2.95% | $191.8K |
| **Subtotal PSP fees** | 100% | — | **$534.2K** |
| FX margin (turned positive for YallaJo on multi-currency: +) | — | — | -$65K (gain) |
| Payouts via CliQ (60%) + bank wire (40%) | — | — | $42K (vs $150K all-bank) |
| **Total payment cost (net)** | | — | **~$511K** |
| **% of GBV** | — | — | **1.97%** |

### 6.3 Bottom Line — Annual Savings

| Year | Single-PSP cost | Multi-PSP cost | Savings |
|---|---|---|---|
| Y1 ($4.2M GBV) | $202K | $84K | **$118K** |
| Y2 ($26M GBV) | $1.25M | $511K | **$736K** |
| Y3 ($78M GBV) | $3.74M | $1.42M | **$2.32M** |

This is **the single largest cost-cut lever in your operating budget**, dwarfing infrastructure ($45-65K) and most other line items. It directly drops to EBITDA.

---

## 7. Implementation Roadmap

### Phase 1 — Foundation (Months 1-2)
- [ ] Engage MadfoatCom: register YallaJo as eFAWATEERcom biller. Acquire Biller Code, sandbox creds.
- [ ] Engage Network International Jordan: card acquirer + CliQ POS integration agreement. Acquire merchant ID + sandbox.
- [ ] Sign Stripe (Atlas if needed for non-US entity) for international card processing.
- [ ] Sign Tap Payments for GCC card acceptance. Apply 3-5 day onboarding.
- [ ] Define `IPaymentProvider`, `IRefundProvider`, `IPayoutProvider` interfaces in `YallaJo.Finance.Application`.
- [ ] Implement `PaymentRouter` with routing rules from §4.1.

### Phase 2 — Build Providers (Months 2-3)
- [ ] Implement `CliQProvider` (via Network Intl API).
- [ ] Implement `EfawateercomProvider` (via Madfoat API).
- [ ] Implement `TapPaymentsProvider`.
- [ ] Implement `StripeProvider`.
- [ ] Implement webhook receivers + Outbox integration events for each PSP.
- [ ] Unit tests + integration tests per provider (target: extend existing 246-test discipline).

### Phase 3 — Routing + Failover (Month 3)
- [ ] Implement routing decision tree + cascading retry logic.
- [ ] Add `PaymentAttempt` tracking with attempt-number + next-provider-to-try.
- [ ] Build reconciliation jobs (daily) per PSP.
- [ ] Build admin dashboard view: PSP-level success rates, fees, settlement timing.

### Phase 4 — Payouts (Month 4)
- [ ] Implement `CliQPayoutProvider` for provider payouts (alongside existing weekly Sunday batch).
- [ ] Migration: existing providers prompt to add CliQ alias to their profile.
- [ ] Discount: providers who enable CliQ payout get **first month subscription free** (small incentive, big cost savings to YallaJo).

### Phase 5 — Optimization (Month 6+)
- [ ] Add BNPL (Tabby + Tamara) for GCC bookings ≥25 JOD.
- [ ] Year 2: negotiate Checkout.com Interchange++ at $50K+/mo volume.
- [ ] A/B test routing variants — does cascading to PayTabs as fallback to Tap improve approval rate?
- [ ] Negotiate fee reductions with Tap and Stripe based on volume.

---

## 8. Action Items for the Founding Team

### Week 1
1. Email MadfoatCom (eFAWATEERcom operator) requesting merchant onboarding pack — `bdd@madfoat.com` or via partner bank.
2. Email Network International Jordan sales — request CliQ + card acquirer pack.
3. Open Stripe Atlas application (if no US entity yet) — required for international card processing at any scale.
4. Apply to Tap Payments at https://www.tap.company — fast onboarding 3-5 days.

### Week 2-4
5. Sign agreements with: Madfoat, Network Intl, Stripe, Tap Payments.
6. Set up sandbox credentials and developer accounts.
7. Brief engineering team on `Finance` module extension plan.

### Month 2
8. Start implementation per Phase 2 plan.
9. Schedule weekly payments review during build.

### Month 3
10. Live in production with multi-PSP routing on a controlled cohort (10% of traffic).
11. Monitor approval rates, settlement timing, dispute counts per PSP.

### Month 4
12. Full rollout. Sunset Stripe-only fallback.

---

## 9. Open Decisions to Make

1. **CliQ alias strategy for YallaJo** — register a brand alias (e.g., `@yallajo`) for inbound receipts? Or per-booking dynamic QR only? Recommendation: both.
2. **eFAWATEERcom as default for >200 JOD bookings?** — extends slot lock window. Worth doing for cost optimization but adds UX friction. A/B test.
3. **CliQ for provider payouts** — make it mandatory or opt-in? Recommendation: opt-in initially with a clear incentive (1 month sub free), then mandatory for all Free-tier providers by Y2.
4. **PayPal — accept or skip?** — adds 1% blended cost. Some Europe/NA tourists demand it. Recommend: skip in Y1 (deflect to Stripe Apple Pay / Google Pay); add in Y2 if conversion data justifies.
5. **Cryptocurrency** — Some MENA tourism platforms accept USDT/BTC. **Recommendation: skip until Y3**. Reputational + compliance risk in Jordan + KSA currently outweighs the 1-3% of audience who'd use it.
6. **BNPL — Tabby AND Tamara, or pick one?** — Tabby leads UAE, Tamara leads KSA. Both integrate easily. **Recommend: enable both** — minimal incremental work, captures conversion lift in both markets.

---

## 10. References

- eFAWATEERcom: https://efawateercom.jo (official portal)
- JoPACC eFAWATEERcom FAQ: https://www.jopacc.com/faqs/efawateercom-faqs
- JoPACC CliQ FAQ: https://www.jopacc.com/faqs/cliq-faqs
- JoPACC CliQ User Handbook: https://www.jopacc.com/sites/default/files/2023-11/cliq_user_handbook.pdf
- Network International Jordan + CliQ launch (2022): https://ibsintelligence.com/ibsi-news/network-international-jordan-incorporates-instant-payments-through-cliq/
- Tap Payments: https://www.tap.company
- PayTabs: https://site.paytabs.com
- Stripe: https://stripe.com
- Checkout.com: https://www.checkout.com
- HyperPay: https://www.hyperpay.com
- MyFatoorah: https://myfatoorah.com
- Tabby: https://tabby.ai
- Tamara: https://tamara.co
- 2026 MENA Gateway comparisons: paymentproviders.io, gulfsaasreview.com, themiddleeastinsider.com
- World Bank Fast Payments / CliQ profile: https://fastpayments.worldbank.org/node/476

---

**Document version**: 1.0 — initial payment integration plan.
**Owner**: YallaJo Finance module + DevOps.
**Next review**: Quarterly, or whenever JoPACC fee model updates.
