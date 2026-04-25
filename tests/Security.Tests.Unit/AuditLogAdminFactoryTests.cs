using FluentAssertions;
using Security.Domain.Entities;

namespace Security.Tests.Unit;

/// <summary>
/// Phase 4 — covers the new <see cref="AuditLog.CreateAdmin"/> factory.
/// Verifies:
///   • All Phase 4 columns are populated (ActorUserId, Reason, Metadata).
///   • Empty/whitespace Reason is normalized to null.
///   • Target user lands in the existing UserId column to keep the
///     per-user index useful for "what happened to user X" queries.
///   • OccurredAt is UTC.
///   • Argument guards reject empty actor / target / action / resource
///     type so call sites cannot bypass them.
///   • Legacy <see cref="AuditLog.Create"/> still leaves the new admin
///     columns null.
/// </summary>
public sealed class AuditLogAdminFactoryTests
{
    private static readonly Guid Actor  = Guid.NewGuid();
    private static readonly Guid Target = Guid.NewGuid();

    [Fact]
    public void CreateAdmin_ShouldPopulate_ActorReasonMetadata_AndKeepTargetAsUserId()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var log = AuditLog.CreateAdmin(
            actorUserId:  Actor,
            targetUserId: Target,
            action:       "ADMIN_SUSPEND_USER",
            resourceType: "User",
            resourceId:   Target,
            ipAddress:    "203.0.113.7",
            reason:       "left company",
            metadata:     "{\"foo\":\"bar\"}");

        log.ActorUserId.Should().Be(Actor);
        log.UserId.Should().Be(Target, "target user id must be stored in UserId so the per-user index keeps working");
        log.Action.Should().Be("ADMIN_SUSPEND_USER");
        log.ResourceType.Should().Be("User");
        log.ResourceId.Should().Be(Target);
        log.IpAddress.Should().Be("203.0.113.7");
        log.Reason.Should().Be("left company");
        log.Metadata.Should().Be("{\"foo\":\"bar\"}");

        log.OccurredAt.Should().BeOnOrAfter(before);
        log.OccurredAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateAdmin_WithBlankReason_ShouldStoreNull(string? reason)
    {
        var log = AuditLog.CreateAdmin(
            actorUserId:  Actor,
            targetUserId: Target,
            action:       "ADMIN_SUSPEND_USER",
            resourceType: "User",
            reason:       reason);

        log.Reason.Should().BeNull();
    }

    [Fact]
    public void CreateAdmin_WithReasonSurroundedByWhitespace_ShouldTrim()
    {
        var log = AuditLog.CreateAdmin(
            actorUserId:  Actor,
            targetUserId: Target,
            action:       "ADMIN_SUSPEND_USER",
            resourceType: "User",
            reason:       "   policy violation   ");

        log.Reason.Should().Be("policy violation");
    }

    [Fact]
    public void CreateAdmin_ShouldThrow_WhenActorIsEmpty()
    {
        FluentActions.Invoking(() => AuditLog.CreateAdmin(
                actorUserId:  Guid.Empty,
                targetUserId: Target,
                action:       "ADMIN_SUSPEND_USER",
                resourceType: "User"))
            .Should().Throw<ArgumentException>().WithParameterName("actorUserId");
    }

    [Fact]
    public void CreateAdmin_ShouldThrow_WhenTargetIsEmpty()
    {
        FluentActions.Invoking(() => AuditLog.CreateAdmin(
                actorUserId:  Actor,
                targetUserId: Guid.Empty,
                action:       "ADMIN_SUSPEND_USER",
                resourceType: "User"))
            .Should().Throw<ArgumentException>().WithParameterName("targetUserId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateAdmin_ShouldThrow_WhenActionIsBlank(string? action)
    {
        FluentActions.Invoking(() => AuditLog.CreateAdmin(
                actorUserId:  Actor,
                targetUserId: Target,
                action:       action!,
                resourceType: "User"))
            .Should().Throw<ArgumentException>().WithParameterName("action");
    }

    [Fact]
    public void LegacyCreate_ShouldLeave_AdminColumnsNull()
    {
        var log = AuditLog.Create(
            userId:       Target,
            action:       "REGISTER",
            resourceType: "User",
            resourceId:   Target);

        log.ActorUserId.Should().BeNull("legacy audit handlers do not populate ActorUserId");
        log.Reason.Should().BeNull();
        log.Metadata.Should().BeNull();
    }
}
