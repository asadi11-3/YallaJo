using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Events;
using FluentAssertions;

namespace ContentCore.Tests.Unit;

/// <summary>
/// Domain-level unit tests for <see cref="Attachment.MarkForDeletion"/>.
/// Regression gate for CONTENTCORE-STD-P0-001: MarkForDeletion MUST be called
/// before Remove so the UoW picks up <see cref="AttachmentDeletedDomainEvent"/>
/// and the outbox flow can emit AttachmentDeletedIntegrationEvent.
/// </summary>
public sealed class AttachmentDomainEventTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static Attachment MakeFreshAttachment(
        EntityType entityType = EntityType.Tour,
        AttachmentType attachmentType = AttachmentType.Image,
        string url = "https://cdn/app/photo.jpg")
    {
        var attachment = Attachment.Create(
            entityType, Guid.NewGuid(), attachmentType, url, Guid.NewGuid());
        // Strip the AttachmentUploadedDomainEvent so tests only observe the delete event.
        attachment.ClearDomainEvents();
        return attachment;
    }

    // ── MarkForDeletion — presence ────────────────────────────────────────────

    [Fact]
    public void MarkForDeletion_ShouldRaiseExactlyOneAttachmentDeletedDomainEvent()
    {
        var attachment = MakeFreshAttachment();

        attachment.MarkForDeletion();

        attachment.DomainEvents
            .OfType<AttachmentDeletedDomainEvent>()
            .Should().ContainSingle("exactly one delete event must be raised per MarkForDeletion call");
    }

    // ── MarkForDeletion — payload fields ─────────────────────────────────────

    [Fact]
    public void MarkForDeletion_ShouldCarryCorrectAttachmentId()
    {
        var attachment = MakeFreshAttachment();

        attachment.MarkForDeletion();

        var evt = attachment.DomainEvents.OfType<AttachmentDeletedDomainEvent>().Single();
        evt.AttachmentId.Should().Be(attachment.Id);
    }

    [Fact]
    public void MarkForDeletion_ShouldCarryCorrectEntityTypeAndEntityId()
    {
        var entityId = Guid.NewGuid();
        var attachment = Attachment.Create(
            EntityType.Place, entityId, AttachmentType.Video,
            "https://cdn/app/video.mp4", Guid.NewGuid());
        attachment.ClearDomainEvents();

        attachment.MarkForDeletion();

        var evt = attachment.DomainEvents.OfType<AttachmentDeletedDomainEvent>().Single();
        evt.EntityType.Should().Be(EntityType.Place);
        evt.EntityId.Should().Be(entityId);
        evt.AttachmentType.Should().Be(AttachmentType.Video);
    }

    [Fact]
    public void MarkForDeletion_ShouldCarryCorrectUrl()
    {
        const string expectedUrl = "https://cdn/app/custom.jpg";
        var attachment = MakeFreshAttachment(url: expectedUrl);

        attachment.MarkForDeletion();

        var evt = attachment.DomainEvents.OfType<AttachmentDeletedDomainEvent>().Single();
        evt.Url.Should().Be(expectedUrl);
    }

    // ── MarkForDeletion — repeated calls ──────────────────────────────────────

    [Fact]
    public void MarkForDeletion_CalledTwice_AppendsTwoSeparateEvents()
    {
        var attachment = MakeFreshAttachment();

        attachment.MarkForDeletion();
        attachment.MarkForDeletion();

        attachment.DomainEvents
            .OfType<AttachmentDeletedDomainEvent>()
            .Should().HaveCount(2, because: "each MarkForDeletion call appends a fresh delete event");
    }

    // ── Create + MarkForDeletion — cumulative events ──────────────────────────

    [Fact]
    public void Create_RaisesUploadedEvent_Then_MarkForDeletion_RaisesDeletedEvent()
    {
        var attachment = Attachment.Create(
            EntityType.Tour, Guid.NewGuid(), AttachmentType.Image,
            "https://cdn/app/photo.jpg", Guid.NewGuid());

        // After Create: exactly one uploaded event, no deleted event.
        attachment.DomainEvents.Should().ContainSingle(e => e is AttachmentUploadedDomainEvent);
        attachment.DomainEvents.OfType<AttachmentDeletedDomainEvent>().Should().BeEmpty();

        attachment.MarkForDeletion();

        // After MarkForDeletion: both events present.
        attachment.DomainEvents.Should().HaveCount(2);
        attachment.DomainEvents.OfType<AttachmentUploadedDomainEvent>().Should().ContainSingle();
        attachment.DomainEvents.OfType<AttachmentDeletedDomainEvent>().Should().ContainSingle();
    }
}
