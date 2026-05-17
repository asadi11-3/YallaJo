using Booking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Booking.Domain.Entities;

public sealed class ProviderDocument : AuditableEntity, IAggregateRoot
{
    private ProviderDocument() { } // EF Core

    public Guid? TourGuideId { get; private set; }
    public Guid? BusinessId { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public string DocumentUrl { get; private set; } = string.Empty;
    public string? OriginalFileName { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DocumentStatus Status { get; private set; } = DocumentStatus.Pending;
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }

    public TourGuide TourGuide { get; private set; } = default!;
}
