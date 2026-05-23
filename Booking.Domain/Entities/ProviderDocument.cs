using System;
using Booking.Domain.Enums;
using Booking.Domain.Extensions;
using Booking.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Domain.Entities;

public sealed class ProviderDocument : AuditableEntity, IAggregateRoot
{
    private ProviderDocument() { } // EF Core

    public Guid ProviderId { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public Guid AttachmentId { get; private set; }
    public string? OriginalFileName { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DocumentStatus Status { get; private set; } = DocumentStatus.Pending;
    public bool ExpiryWarningSent { get; private set; }
    public bool ExpiryProcessed { get; private set; }

    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    public static ProviderDocument CreateForProvider(
        Guid providerId,
        DocumentType type,
        Guid attachmentId,
        string originalFileName,
        DateTime? expiresAt)
    {
        var doc = new ProviderDocument
        {
            Id = Guid.CreateVersion7(),
            ProviderId = providerId,
            DocumentType = type,
            AttachmentId = attachmentId,
            OriginalFileName = originalFileName,
            ExpiresAt = expiresAt,
            Status = DocumentStatus.Pending,
            ExpiryWarningSent = false,
            ExpiryProcessed = false
        };

        return doc;
    }

    public Result Approve(Guid adminUserId)
    {
        if (Status == DocumentStatus.Approved)
            return Result.Failure(new Error("ProviderDocument.AlreadyApproved", "Document already approved."));

        Status = DocumentStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedByUserId = adminUserId;
        MarkUpdated();

        return Result.Success();
    }

    public Result Reject(string reason)
    {
        if (Status == DocumentStatus.Rejected)
            return Result.Failure(new Error("ProviderDocument.AlreadyRejected", "Document already rejected."));

        Status = DocumentStatus.Rejected;
        RejectedAt = DateTime.UtcNow;
        RejectionReason = reason;
        MarkUpdated();

        return Result.Success();
    }

    public Result MarkExpiringSoon(int daysRemaining)
    {
        if (ExpiryWarningSent)
            return Result.Failure(new Error("ProviderDocument.WarningAlreadySent", "Expiry warning already raised."));

        ExpiryWarningSent = true;

        if (ExpiresAt.HasValue)
        {
            AddDomainEvent(new ProviderDocumentExpiringDomainEvent(Id, ProviderId, DocumentType, ExpiresAt.Value, daysRemaining));
        }

        MarkUpdated();
        return Result.Success();
    }

    public Result MarkExpired()
    {
        if (ExpiryProcessed)
            return Result.Failure(new Error("ProviderDocument.AlreadyProcessed", "Expiry already processed."));

        Status = DocumentStatus.Expired;
        ExpiryProcessed = true;

        var isCritical = DocumentTypeExtensions.IsCritical(DocumentType);
        AddDomainEvent(new ProviderDocumentExpiredDomainEvent(Id, ProviderId, DocumentType, isCritical));

        MarkUpdated();
        return Result.Success();
    }

    public Result UpdateDetails(Guid? newAttachmentId, string? newFileName, DateTime? newExpiresAt)
    {
        if (Status == DocumentStatus.Expired)
            return Result.Failure(new Error("ProviderDocument.InvalidState", "Cannot edit an expired document. Please upload a new one."));

        if (newAttachmentId.HasValue)
        {
            AttachmentId = newAttachmentId.Value;
            OriginalFileName = newFileName;
        }

        if (newExpiresAt.HasValue)
        {
            ExpiresAt = newExpiresAt;
        }

        if (newAttachmentId.HasValue)
        {
            Status = DocumentStatus.Pending;
        }

        MarkUpdated();
        return Result.Success();
    }
}
