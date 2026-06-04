using Finance.Application.Interfaces;
using Finance.Domain.Entities;
using Finance.Domain.Events;
using Finance.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Finance.Application.EventHandlers;

/// <summary>
/// T3 / F-R8: When a payment is marked Completed, automatically generate an invoice.
/// INDEX §4 R15 — runs inside the same UoW so the invoice is persisted in the same transaction.
/// </summary>
public sealed class OnPaymentCompletedGenerateInvoiceHandler(
    IPaymentRepository paymentRepository,
    IInvoiceRepository invoiceRepository,
    IPaymentExpectationRepository expectationRepository,
    IInvoiceNumberGenerator numberGenerator,
    ILogger<OnPaymentCompletedGenerateInvoiceHandler> logger,
    TimeProvider timeProvider)
    : INotificationHandler<DomainEventNotification<PaymentCompletedDomainEvent>>
{
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository = invoiceRepository;
    private readonly IPaymentExpectationRepository _expectationRepository = expectationRepository;
    private readonly IInvoiceNumberGenerator _numberGenerator = numberGenerator;
    private readonly ILogger<OnPaymentCompletedGenerateInvoiceHandler> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task Handle(DomainEventNotification<PaymentCompletedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var evt = notification.Event;
        if (evt.BookingId == Guid.Empty)
        {
            _logger.LogDebug("Skipping invoice generation for payment {PaymentId} — no BookingId.", evt.PaymentId);
            return;
        }

        // Idempotency — one invoice per booking
        var existing = await _invoiceRepository.GetByBookingIdAsync(evt.BookingId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogDebug("Invoice already exists for booking {BookingId}; skipping.", evt.BookingId);
            return;
        }

        var payment = await _paymentRepository.GetByIdAsync(evt.PaymentId, cancellationToken, asNoTracking: false);
        if (payment is null)
        {
            _logger.LogWarning("PaymentCompletedDomainEvent received but payment {PaymentId} not found.", evt.PaymentId);
            return;
        }

        var expectation = await _expectationRepository.GetByBookingIdAsync(evt.BookingId, cancellationToken);
        var reference = expectation?.Reference ?? evt.BookingId.ToString("N");

        var invoiceNumber = await _numberGenerator.NextAsync(cancellationToken);

        // v1: buyer/seller snapshots are minimal placeholders. Cross-module lookups
        // (Accounts.UserSnapshot, Accounts.ProviderSnapshot) are deferred to Phase 3.
        var lineItems = new List<InvoiceLineDraft>
        {
            new($"Booking reference {reference}", 1, evt.Amount),
        };

        var invoice = Invoice.GenerateForPayment(
            payment,
            invoiceNumber,
            buyerName: "Customer",
            buyerEmail: string.Empty,
            sellerName: "YallaJo Provider",
            sellerTaxId: null,
            taxAmount: 0m,
            discountAmount: 0m,
            lineItems,
            _timeProvider);

        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        _logger.LogInformation(
            "Generated invoice {InvoiceNumber} for booking {BookingId} (payment {PaymentId}, total {Total} {Currency}).",
            invoice.InvoiceNumber, evt.BookingId, evt.PaymentId, invoice.AmountTotal.Amount, invoice.Currency);
    }
}
