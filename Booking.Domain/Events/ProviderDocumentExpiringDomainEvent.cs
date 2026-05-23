using Booking.Domain.Enums;
using System;
using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record ProviderDocumentExpiringDomainEvent(
    Guid DocumentId,
    Guid ProviderId,
    DocumentType DocumentType,
    DateTime ExpiresAt,
    int DaysRemaining) : DomainEventBase;
