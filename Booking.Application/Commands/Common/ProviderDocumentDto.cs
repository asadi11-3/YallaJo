using Booking.Domain.Enums;

namespace Booking.Application.Commands.Common;

public sealed record ProviderDocumentDto(
    Guid Id,
    Guid? TourGuideId,
    Guid? BusinessId,
    DocumentType Type,
    string? FileName,
    string DocumentUrl,
    DateTime? ExpiresAt,
    DocumentStatus Status,
    DateTime? ReviewedAt,
    string? RejectionReason,
    DateTime CreatedAt,
    string RowVersion);
