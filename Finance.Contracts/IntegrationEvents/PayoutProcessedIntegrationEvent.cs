using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

public sealed record PayoutProcessedIntegrationEvent(
    Guid PayoutId,
    Guid RecipientUserId,
    decimal TotalAmount,
    string Currency,
    string TransactionId,
    DateTime ProcessedAt) : IntegrationEventBase;
