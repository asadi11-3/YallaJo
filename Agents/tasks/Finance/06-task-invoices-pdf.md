# TASK 3 — Invoices + PDF Generation

> **Owner:** Fadwa — **Hours:** 36h — **Hard deadline:** Sun **2026-09-27 17:00**
> **Earliest start:** Wed 2026-08-19 (parallel with T1; reads PaymentCompleted event)
> **Endpoints:** 3 + 1 domain event handler
> **Depends on:** T1 (Payment.MarkCompleted), PW (Invoice aggregate)

---

## 1. Endpoint Surface

| Method | Path | Permission | Returns |
|---|---|---|---|
| GET | `/api/v1/invoices/my-invoices` | `Invoice.Read` | Cursor envelope, self-filter |
| GET | `/api/v1/invoices/provider/my-invoices` | `Invoice.Read` | Self-filter by provider role |
| GET | `/api/v1/invoices/{id}` | `Invoice.Read` | Self/provider/admin ownership |
| GET | `/api/v1/invoices/{id}/download` | `Invoice.Download` | application/pdf stream |

**Domain event handler** (NOT HTTP):
- `OnPaymentCompletedGenerateInvoiceHandler` in `Finance.Infrastructure/EventHandlers/` — consumes `PaymentCompletedDomainEvent` → calls `Invoice.GenerateForPayment` factory + adds to DbContext + emits `InvoiceGeneratedDomainEvent`. INDEX §4 R15 — does NOT call SaveChangesAsync; the same UoW commit that wrote Payment completion writes the Invoice.

---

## 2. Invoice Aggregate Design

```csharp
public sealed class Invoice : AuditableEntity, IAggregateRoot
{
    public string InvoiceNumber { get; private set; }   // INV-{YYYYMM}-{seq6}
    public Guid PaymentId { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ProviderId { get; private set; }
    public Money AmountSubtotal { get; private set; }   // booking total
    public Money AmountTax { get; private set; }        // computed per region; this sprint stub = 0
    public Money AmountDiscount { get; private set; }   // sum of DiscountUsage; this sprint stub = 0
    public Money AmountTotal { get; private set; }      // == subtotal - discount + tax
    public InvoiceStatus Status { get; private set; }   // {Issued, Cancelled, Refunded}
    public DateTime IssuedAt { get; private set; }
    public string BuyerName { get; private set; }
    public string BuyerEmail { get; private set; }
    public string SellerName { get; private set; }      // provider business name
    public string SellerTaxId { get; private set; }
    public string? PdfStoragePath { get; private set; } // null until first download triggers render
    public ICollection<InvoiceItem> Items { get; private set; } = [];

    public static Invoice GenerateForPayment(
        Payment payment,
        BookingSnapshot booking,
        ProviderSnapshot provider,
        UserSnapshot buyer,
        IInvoiceNumberGenerator numberGen,
        TimeProvider timeProvider);

    public Result CancelDueToRefund(string reason);
    public Result MarkPdfRendered(string storagePath);
}
```

`InvoiceItem` (owned, BaseEntity):
```csharp
public sealed class InvoiceItem
{
    public string Description { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money Subtotal { get; private set; }
}
```

---

## 3. InvoiceNumber Generator (F-R8)

```csharp
public interface IInvoiceNumberGenerator
{
    Task<string> NextAsync(DateTime monthAnchorUtc, CancellationToken ct);
}
```

Implementation `SqlInvoiceNumberGenerator` (singleton):
- Stores month/sequence in `finance.InvoiceNumberSequence` table (PK = YYYYMM).
- Atomic increment via `UPDATE … SET seq = seq + 1 OUTPUT inserted.seq WHERE month = @m` (UPSERT pattern).
- Returns `$"INV-{YYYYMM}-{seq:D6}"`.

**Race safety:** SQL row-level lock during UPDATE. Concurrent webhooks safe because each gets a unique seq.

**Per-month sequence reset:** automatic — new month gets seq=1 because INSERT path creates the row.

---

## 4. PDF Generation (QuestPDF)

Add package: `QuestPDF` v2024.12+ to Finance.Infrastructure.csproj. **License flag:** QuestPDF Community license requires app config call `QuestPDF.Settings.License = LicenseType.Community;` in Program.cs (OK for YallaJo's annual revenue threshold; verify with Tech Lead).

**Template `InvoicePdfTemplate` in `Finance.Infrastructure/Pdf/`:**
- Header: YallaJo logo (from `/wwwroot/branding/logo.png`), "INVOICE", invoice number, issue date.
- Buyer block (right): name, email.
- Seller block (left): name, tax ID.
- Booking metadata table: tour name, date, group size, currency.
- Line items table: description / qty / unit price / subtotal.
- Totals: subtotal, discount, tax, total — bold, larger font.
- Footer: payment method, gateway txn ID (last 4 chars only — PCI), "Thank you for choosing YallaJo".

**Localization:** EN baseline. AR translation deferred (string keys ready but only EN populated). `Accept-Language: ar` will fall back to EN this sprint.

**Storage:** rendered PDFs go to a configurable blob path:
- Local dev: `App_Data/invoices/{userId}/{invoiceId}.pdf`.
- Prod: Azure Blob `invoices/{userId}/{invoiceId}.pdf` (binding via `IStorageProvider` already in SharedKernel.Infrastructure).
- Path stamped onto Invoice.PdfStoragePath on first render. Subsequent downloads stream from storage.

**Cache busting:** if Invoice is Cancelled / Refunded → PDF re-rendered next download (storage path nulled by `CancelDueToRefund`).

---

## 5. GET /invoices/{id}/download Flow

```csharp
public async Task<Result<InvoiceDownloadResponse>> Handle(DownloadInvoiceQuery q, CancellationToken ct)
{
    var invoice = await _invoiceRepo.GetByIdAsync(q.InvoiceId, ct);
    if (invoice is null) return Result.Failure<InvoiceDownloadResponse>(new Error("Invoice.NotFound", ""), Outcome.NotFound);

    // Ownership check
    if (!await OwnedByCallerAsync(invoice, ct))
        return Result.Failure<InvoiceDownloadResponse>(new Error("Invoice.OwnerMismatch", ""), Outcome.Forbidden);

    if (invoice.PdfStoragePath is null)
    {
        // Lazy render
        var pdfBytes = _pdfRenderer.Render(invoice);
        var path = await _storage.StoreAsync($"invoices/{invoice.UserId}/{invoice.Id}.pdf", pdfBytes, ct);
        invoice.MarkPdfRendered(path);
        await _uow.SaveChangesAsync(ct);
    }

    var stream = await _storage.OpenReadAsync(invoice.PdfStoragePath, ct);
    return Result.Success(new InvoiceDownloadResponse(stream, $"{invoice.InvoiceNumber}.pdf", "application/pdf"));
}
```

Endpoint returns `Results.Stream(stream, "application/pdf", invoice.InvoiceNumber + ".pdf")`.

**Performance:** first download <2 s, subsequent <300 ms (storage fetch only).

---

## 6. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | `Invoice` aggregate + `InvoiceItem` owned + factory + tests | 5 | 2026-08-26 |
| 2 | Migration `FinanceAddInvoiceTable` + `IInvoiceNumberSequence` (1 table for seq counter) | 3 | 2026-08-27 |
| 3 | `SqlInvoiceNumberGenerator` + concurrency tests (100 parallel calls assert unique seqs) | 4 | 2026-08-31 |
| 4 | `OnPaymentCompletedGenerateInvoiceHandler` domain event handler + test | 3 | 2026-09-02 |
| 5 | QuestPDF package + license setup + `InvoicePdfTemplate` + visual review | 6 | 2026-09-07 |
| 6 | `IStorageProvider` wiring (local + Azure Blob backends) | 4 | 2026-09-09 |
| 7 | GET /my-invoices + GET /provider/my-invoices queries + handlers + cache | 4 | 2026-09-14 |
| 8 | GET /invoices/{id} + ownership guard | 2 | 2026-09-16 |
| 9 | GET /invoices/{id}/download endpoint + lazy render flow | 3 | 2026-09-20 |
| 10 | Unit tests for InvoiceNumberGenerator + Invoice factory invariants | 1 | 2026-09-23 |
| 11 | PR review + visual PDF QA | 1 | 2026-09-27 |
| **Total** | | **36h** | |

---

## 7. Edge cases

1. Two webhooks for same payment fire concurrently → `OnPaymentCompletedGenerateInvoiceHandler` is idempotent via `_invoiceRepo.GetByPaymentIdAsync` check at top; second call is a noop.
2. Booking refunded → `BookingTourBookingCancelledHandler` (in T2) raises domain event; T3 adds a sibling handler `OnBookingCancelledMarkInvoiceCancelledHandler` that calls `Invoice.CancelDueToRefund`. PDF re-rendered with watermark "CANCELLED" on next download.
3. Provider/User snapshot stale → invoice uses snapshot at moment of generation; subsequent provider name change does NOT rewrite issued invoices (audit purity).
4. QuestPDF license throws at runtime → log critical, return 500 with `Invoice.PdfRenderError`. Tech Lead alerted.
5. Storage write fails → don't mark invoice rendered; user gets 503 + retry-after. Next attempt succeeds.
6. Concurrent download of same not-yet-rendered invoice → unique-index `(InvoiceId, PdfStoragePath)` prevents double-stamp; loser uses winner's path next attempt.
7. Tax = 0 this sprint everywhere → display "Tax: JOD 0.00" not hidden (regulatory hygiene).
8. Invoice month boundary (12/31 23:59:59 UTC payment) → uses `IssuedAt` UTC date; month sequence comes from `IssuedAt` month.
9. InvoiceNumber sequence overflow (>999999/month) → unlikely; raise critical alert if >900K; expand to 7 digits in a v2 migration.
