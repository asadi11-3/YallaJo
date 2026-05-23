using Booking.Domain.Enums;
using Booking.Domain.Events;
using Booking.Domain.Extensions;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.Exceptions;

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
    public DateTime? ExpiringNotificationSentAt { get; private set; }
    public DateTime? ExpiredNotificationSentAt { get; private set; }
    public DateTime? SuspensionDispatchedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }

    public TourGuide TourGuide { get; private set; } = default!;

    public static ProviderDocument CreateForTourGuide(
        Guid tourGuideId,
        DocumentType documentType,
        string documentUrl,
        string originalFileName,
        DateTime? expiresAtUtc)
    {
        if (tourGuideId == Guid.Empty)
        {
            throw new BusinessRuleViolationException("ProviderDocument.tourGuideId must not be empty.");
        }

        return Create(
            tourGuideId: tourGuideId,
            businessId: null,
            documentType: documentType,
            documentUrl: documentUrl,
            originalFileName: originalFileName,
            expiresAtUtc: expiresAtUtc);
    }

    public static ProviderDocument CreateForBusiness(
        Guid businessId,
        DocumentType documentType,
        string documentUrl,
        string originalFileName,
        DateTime? expiresAtUtc)
    {
        if (businessId == Guid.Empty)
        {
            throw new BusinessRuleViolationException("ProviderDocument.businessId must not be empty.");
        }

        return Create(
            tourGuideId: null,
            businessId: businessId,
            documentType: documentType,
            documentUrl: documentUrl,
            originalFileName: originalFileName,
            expiresAtUtc: expiresAtUtc);
    }

    private static ProviderDocument Create(
        Guid? tourGuideId,
        Guid? businessId,
        DocumentType documentType,
        string documentUrl,
        string originalFileName,
        DateTime? expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(documentUrl))
        {
            throw new BusinessRuleViolationException("ProviderDocument.documentUrl must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new BusinessRuleViolationException("ProviderDocument.originalFileName must not be empty.");
        }

        if (documentType.RequiresExpiry() && expiresAtUtc is null)
        {
            throw new BusinessRuleViolationException(
                $"DocumentType {documentType} requires an ExpiresAt value.");
        }

        if (expiresAtUtc.HasValue && expiresAtUtc.Value <= DateTime.UtcNow)
        {
            throw new BusinessRuleViolationException("ProviderDocument.expiresAt must be in the future.");
        }

        return new ProviderDocument
        {
            Id = Guid.NewGuid(),
            TourGuideId = tourGuideId,
            BusinessId = businessId,
            DocumentType = documentType,
            DocumentUrl = documentUrl,
            OriginalFileName = originalFileName,
            ExpiresAt = expiresAtUtc,
            Status = DocumentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void ReplaceFile(string newDocumentUrl, string newOriginalFileName)
    {
        if (string.IsNullOrWhiteSpace(newDocumentUrl))
        {
            throw new BusinessRuleViolationException("ProviderDocument.documentUrl must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(newOriginalFileName))
        {
            throw new BusinessRuleViolationException("ProviderDocument.originalFileName must not be empty.");
        }

        if (Status == DocumentStatus.Expired)
        {
            throw new BusinessRuleViolationException(
                "Cannot replace an Expired document. Upload a new document of the same type instead.");
        }

        DocumentUrl = newDocumentUrl;
        OriginalFileName = newOriginalFileName;
        Status = DocumentStatus.Pending;
        ReviewedAt = null;
        ReviewedByUserId = null;
        RejectionReason = null;
        ExpiringNotificationSentAt = null;
        ExpiredNotificationSentAt = null;
        SuspensionDispatchedAt = null;
        MarkUpdated();
    }


    public void UpdateExpiry(DateTime? newExpiresAtUtc)
    {
        if (Status == DocumentStatus.Expired)
        {
            throw new BusinessRuleViolationException(
                "Cannot update expiry of an Expired document.");
        }

        if (DocumentType.RequiresExpiry() && newExpiresAtUtc is null)
        {
            throw new BusinessRuleViolationException(
                $"DocumentType {DocumentType} requires an ExpiresAt value.");
        }

        if (newExpiresAtUtc.HasValue && newExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            throw new BusinessRuleViolationException(
                "ProviderDocument.expiresAt must be in the future.");
        }

        ExpiresAt = newExpiresAtUtc;
        ExpiringNotificationSentAt = null;
        ExpiredNotificationSentAt = null;
        MarkUpdated();
    }

    public void Approve(Guid reviewerUserId, DateTime nowUtc)
    {
        if (reviewerUserId == Guid.Empty)
        {
            throw new BusinessRuleViolationException("ProviderDocument.reviewerUserId must not be empty.");
        }

        if (Status != DocumentStatus.Pending)
        {
            throw new BusinessRuleViolationException(
                $"Cannot approve a document in {Status} state.");
        }

        Status = DocumentStatus.Approved;
        ReviewedAt = nowUtc;
        ReviewedByUserId = reviewerUserId;
        RejectionReason = null;
        MarkUpdated();
    }

    /// <summary>Admin / reviewer marks the document Rejected. Only valid from Pending.</summary>
    public void Reject(Guid reviewerUserId, string reason, DateTime nowUtc)
    {
        if (reviewerUserId == Guid.Empty)
        {
            throw new BusinessRuleViolationException("ProviderDocument.reviewerUserId must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessRuleViolationException("ProviderDocument.reason must not be empty.");
        }

        if (Status != DocumentStatus.Pending)
        {
            throw new BusinessRuleViolationException(
                $"Cannot reject a document in {Status} state.");
        }

        Status = DocumentStatus.Rejected;
        ReviewedAt = nowUtc;
        ReviewedByUserId = reviewerUserId;
        RejectionReason = reason.Trim();
        MarkUpdated();
    }

    // ── BG-sweep mutations (idempotent silent guards) ────────────────────────

    public void MarkExpiring(DateTime nowUtc)
    {
        if (ExpiresAt is null || Status == DocumentStatus.Expired || ExpiringNotificationSentAt.HasValue)
        {
            return;
        }

        ExpiringNotificationSentAt = nowUtc;
        MarkUpdated();
        AddDomainEvent(new ProviderDocumentExpiringDomainEvent(Id, TourGuideId, BusinessId, ExpiresAt.Value));
    }

    public void MarkExpired(DateTime nowUtc)
    {
        if (ExpiresAt is null || ExpiredNotificationSentAt.HasValue)
        {
            return;
        }

        Status = DocumentStatus.Expired;
        ExpiredNotificationSentAt = nowUtc;
        MarkUpdated();
        AddDomainEvent(new ProviderDocumentExpiredDomainEvent(Id, TourGuideId, BusinessId));
    }

    public void MarkSuspensionDispatched(DateTime nowUtc)
    {
        if (!DocumentType.IsCritical())
        {
            return;
        }

        if (Status != DocumentStatus.Expired)
        {
            return;
        }

        if (SuspensionDispatchedAt.HasValue)
        {
            return;
        }

        SuspensionDispatchedAt = nowUtc;
        MarkUpdated();
        AddDomainEvent(new ProviderSuspendedDocumentExpiredDomainEvent(
            DocumentId: Id,
            TourGuideId: TourGuideId,
            BusinessId: BusinessId,
            DocumentType: DocumentType,
            SuspendedAtUtc: nowUtc));
    }
}
