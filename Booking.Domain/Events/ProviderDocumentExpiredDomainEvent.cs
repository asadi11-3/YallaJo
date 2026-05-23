using Booking.Domain.Enums;
using System;
using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record ProviderDocumentExpiredDomainEvent(
    Guid DocumentId,
    Guid ProviderId,
    DocumentType DocumentType,
    bool IsCritical) : DomainEventBase;
