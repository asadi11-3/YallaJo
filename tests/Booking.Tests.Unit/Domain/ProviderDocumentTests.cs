using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Events;
using FluentAssertions;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Tests.Unit.Domain;

public sealed class ProviderDocumentTests
{
    private static readonly Guid TourGuideId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ReviewerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // ── Factories ────────────────────────────────────────────────────────────

    [Fact]
    public void CreateForTourGuide_with_required_expiry_succeeds_and_starts_Pending()
    {
        var expires = DateTime.UtcNow.AddYears(1);

        var doc = ProviderDocument.CreateForTourGuide(
            tourGuideId: TourGuideId,
            documentType: DocumentType.MoTALicense,
            documentUrl: "/uploads/mota.pdf",
            originalFileName: "mota.pdf",
            expiresAtUtc: expires);

        doc.TourGuideId.Should().Be(TourGuideId);
        doc.BusinessId.Should().BeNull();
        doc.DocumentType.Should().Be(DocumentType.MoTALicense);
        doc.Status.Should().Be(DocumentStatus.Pending);
        doc.ExpiresAt.Should().Be(expires);
        doc.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void CreateForTourGuide_throws_when_required_expiry_is_missing()
    {
        var act = () => ProviderDocument.CreateForTourGuide(
            tourGuideId: TourGuideId,
            documentType: DocumentType.MoTALicense,
            documentUrl: "/uploads/mota.pdf",
            originalFileName: "mota.pdf",
            expiresAtUtc: null);

        act.Should()
            .Throw<BusinessRuleViolationException>()
            .WithMessage("*requires an ExpiresAt*");
    }

    [Fact]
    public void CreateForTourGuide_throws_on_past_expiry()
    {
        var act = () => ProviderDocument.CreateForTourGuide(
            tourGuideId: TourGuideId,
            documentType: DocumentType.MoTALicense,
            documentUrl: "/uploads/mota.pdf",
            originalFileName: "mota.pdf",
            expiresAtUtc: DateTime.UtcNow.AddDays(-1));

        act.Should()
            .Throw<BusinessRuleViolationException>()
            .WithMessage("*must be in the future*");
    }

    [Fact]
    public void CreateForTourGuide_does_not_require_expiry_for_GovernmentID()
    {
        var doc = ProviderDocument.CreateForTourGuide(
            tourGuideId: TourGuideId,
            documentType: DocumentType.GovernmentID,
            documentUrl: "/uploads/id.png",
            originalFileName: "id.png",
            expiresAtUtc: null);

        doc.ExpiresAt.Should().BeNull();
        doc.Status.Should().Be(DocumentStatus.Pending);
    }

    // ── ReplaceFile / UpdateExpiry ───────────────────────────────────────────

    [Fact]
    public void ReplaceFile_resets_to_Pending_and_clears_review_state()
    {
        var doc = ApprovedDoc();

        doc.ReplaceFile("/uploads/new.pdf", "new.pdf");

        doc.Status.Should().Be(DocumentStatus.Pending);
        doc.DocumentUrl.Should().Be("/uploads/new.pdf");
        doc.OriginalFileName.Should().Be("new.pdf");
        doc.ReviewedAt.Should().BeNull();
        doc.ReviewedByUserId.Should().BeNull();
        doc.RejectionReason.Should().BeNull();
        doc.ExpiringNotificationSentAt.Should().BeNull();
        doc.ExpiredNotificationSentAt.Should().BeNull();
        doc.SuspensionDispatchedAt.Should().BeNull();
    }

    [Fact]
    public void ReplaceFile_throws_on_Expired_document()
    {
        var doc = ApprovedDoc();
        doc.MarkExpired(DateTime.UtcNow);
        doc.Status.Should().Be(DocumentStatus.Expired);

        var act = () => doc.ReplaceFile("/uploads/replacement.pdf", "replacement.pdf");

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*Expired document*");
    }

    [Fact]
    public void UpdateExpiry_changes_value_and_clears_notification_timestamps()
    {
        var doc = ApprovedDoc();
        var oldExpiry = doc.ExpiresAt!.Value;
        doc.MarkExpiring(DateTime.UtcNow);
        doc.ExpiringNotificationSentAt.Should().NotBeNull();

        var newExpiry = oldExpiry.AddDays(60);
        doc.UpdateExpiry(newExpiry);

        doc.ExpiresAt.Should().Be(newExpiry);
        doc.ExpiringNotificationSentAt.Should().BeNull();
        doc.ExpiredNotificationSentAt.Should().BeNull();
    }

    // ── Approve / Reject ────────────────────────────────────────────────────

    [Fact]
    public void Approve_from_Pending_sets_Approved_status_and_reviewer_metadata()
    {
        var doc = PendingDoc();
        var nowUtc = DateTime.UtcNow;

        doc.Approve(ReviewerId, nowUtc);

        doc.Status.Should().Be(DocumentStatus.Approved);
        doc.ReviewedAt.Should().Be(nowUtc);
        doc.ReviewedByUserId.Should().Be(ReviewerId);
        doc.RejectionReason.Should().BeNull();
    }

    [Fact]
    public void Reject_from_Pending_stores_reason()
    {
        var doc = PendingDoc();
        var nowUtc = DateTime.UtcNow;

        doc.Reject(ReviewerId, "Blurry scan", nowUtc);

        doc.Status.Should().Be(DocumentStatus.Rejected);
        doc.RejectionReason.Should().Be("Blurry scan");
        doc.ReviewedAt.Should().Be(nowUtc);
        doc.ReviewedByUserId.Should().Be(ReviewerId);
    }

    [Fact]
    public void Approve_throws_when_already_Approved()
    {
        var doc = ApprovedDoc();
        var act = () => doc.Approve(ReviewerId, DateTime.UtcNow);
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*Approved*");
    }

    [Fact]
    public void Reject_throws_with_empty_reason()
    {
        var doc = PendingDoc();
        var act = () => doc.Reject(ReviewerId, " ", DateTime.UtcNow);
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*reason*");
    }

    // ── MarkExpired / MarkSuspensionDispatched separation (C2) ──────────────

    [Fact]
    public void MarkExpired_raises_only_ProviderDocumentExpiredDomainEvent_even_for_critical_types()
    {
        var doc = ApprovedDoc(DocumentType.MoTALicense);

        doc.MarkExpired(DateTime.UtcNow);

        doc.DomainEvents.OfType<ProviderDocumentExpiredDomainEvent>().Should().ContainSingle();
        doc.DomainEvents.OfType<ProviderSuspendedDocumentExpiredDomainEvent>()
            .Should().BeEmpty("MarkExpired and MarkSuspensionDispatched are decoupled (C2)");
    }

    [Fact]
    public void MarkExpired_raises_only_expired_event_for_non_critical_types()
    {
        var doc = ApprovedDoc(DocumentType.GovernmentID);

        doc.MarkExpired(DateTime.UtcNow);

        doc.DomainEvents.OfType<ProviderDocumentExpiredDomainEvent>().Should().ContainSingle();
        doc.DomainEvents.OfType<ProviderSuspendedDocumentExpiredDomainEvent>().Should().BeEmpty();
    }

    [Fact]
    public void MarkSuspensionDispatched_on_critical_expired_doc_raises_suspended_event_once()
    {
        var doc = ApprovedDoc(DocumentType.MoTALicense);
        doc.MarkExpired(DateTime.UtcNow);
        doc.ClearDomainEvents();
        var suspendedAt = DateTime.UtcNow.AddMinutes(5);

        doc.MarkSuspensionDispatched(suspendedAt);

        doc.SuspensionDispatchedAt.Should().Be(suspendedAt);
        var raised = doc.DomainEvents.OfType<ProviderSuspendedDocumentExpiredDomainEvent>().ToList();
        raised.Should().ContainSingle();
        raised[0].DocumentId.Should().Be(doc.Id);
        raised[0].DocumentType.Should().Be(DocumentType.MoTALicense);
        raised[0].TourGuideId.Should().Be(TourGuideId);
        raised[0].SuspendedAtUtc.Should().Be(suspendedAt);
    }

    [Fact]
    public void MarkSuspensionDispatched_second_call_is_silent_no_op()
    {
        var doc = ApprovedDoc(DocumentType.MoTALicense);
        doc.MarkExpired(DateTime.UtcNow);
        doc.MarkSuspensionDispatched(DateTime.UtcNow.AddMinutes(5));
        doc.ClearDomainEvents();

        doc.MarkSuspensionDispatched(DateTime.UtcNow.AddMinutes(10));

        doc.DomainEvents.OfType<ProviderSuspendedDocumentExpiredDomainEvent>().Should().BeEmpty();
    }

    [Fact]
    public void MarkSuspensionDispatched_ignores_non_critical_doc()
    {
        var doc = ApprovedDoc(DocumentType.GovernmentID);
        doc.MarkExpired(DateTime.UtcNow);
        doc.ClearDomainEvents();

        doc.MarkSuspensionDispatched(DateTime.UtcNow);

        doc.SuspensionDispatchedAt.Should().BeNull();
        doc.DomainEvents.OfType<ProviderSuspendedDocumentExpiredDomainEvent>().Should().BeEmpty();
    }

    [Fact]
    public void MarkSuspensionDispatched_ignores_non_expired_doc()
    {
        var doc = ApprovedDoc(DocumentType.MoTALicense);
        doc.Status.Should().Be(DocumentStatus.Approved);

        doc.MarkSuspensionDispatched(DateTime.UtcNow);

        doc.SuspensionDispatchedAt.Should().BeNull();
        doc.DomainEvents.OfType<ProviderSuspendedDocumentExpiredDomainEvent>().Should().BeEmpty();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ProviderDocument PendingDoc(DocumentType type = DocumentType.MoTALicense)
    {
        var doc = ProviderDocument.CreateForTourGuide(
            tourGuideId: TourGuideId,
            documentType: type,
            documentUrl: "/uploads/x.pdf",
            originalFileName: "x.pdf",
            expiresAtUtc: DateTime.UtcNow.AddYears(1));
        doc.ClearDomainEvents();
        return doc;
    }

    private static ProviderDocument ApprovedDoc(DocumentType type = DocumentType.MoTALicense)
    {
        var doc = PendingDoc(type);
        doc.Approve(ReviewerId, DateTime.UtcNow);
        doc.ClearDomainEvents();
        return doc;
    }
}
