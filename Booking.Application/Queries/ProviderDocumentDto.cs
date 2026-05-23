using System;
using Booking.Domain.Enums;

namespace Booking.Application.Queries;

public sealed record ProviderDocumentDto(
    Guid Id,
    Guid ProviderId,
    DocumentType DocumentType,
    Guid AttachmentId,
    string? OriginalFileName,
    DateTime? ExpiresAt,
    DocumentStatus Status,
    bool ExpiryWarningSent,
    bool ExpiryProcessed
);
