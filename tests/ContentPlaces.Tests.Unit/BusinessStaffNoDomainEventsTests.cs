using ContentPlaces.Domain.Enums;
using FluentAssertions;
using StaffEntity = ContentPlaces.Domain.Entities.BusinessStaff;

namespace ContentPlaces.Tests.Unit;

/// <summary>
/// CONTENTPLACES-FOLLOWUP-AGGREGATE-ROOT-001 regression tests.
///
/// <para>
/// <see cref="StaffEntity"/> is a non-aggregate child of <c>Business</c> and
/// must NOT raise domain events.  The shared
/// <c>UnitOfWork&lt;TContext&gt;.SaveChangesAsync</c> dispatches domain events
/// only from <c>ChangeTracker.Entries&lt;IAggregateRoot&gt;()</c>; a domain
/// event raised by this entity would be silently dropped — and worse, would
/// cause <b>double-publish</b> if anyone later promoted the entity to
/// <c>IAggregateRoot</c> without also removing the manual outbox enqueue from
/// the command handlers.
/// </para>
///
/// <para>
/// Integration events for staff add/remove are produced exclusively via
/// <c>IContentPlacesOutboxWriter.Enqueue(...)</c> in
/// <c>AddBusinessStaffCommandHandler</c> / <c>RemoveBusinessStaffCommandHandler</c>
/// (canonical pattern, same as <c>ServiceItem</c>).  These tests pin the
/// "no domain events" invariant so the dead path cannot be silently
/// reintroduced.
/// </para>
/// </summary>
public sealed class BusinessStaffNoDomainEventsTests
{
    [Fact]
    public void BusinessStaff_Create_DoesNotRaiseDomainEvents()
    {
        var staff = StaffEntity.Create(
            businessId: Guid.NewGuid(),
            userId:     Guid.NewGuid(),
            role:       BusinessStaffRole.Manager);

        staff.DomainEvents.Should().BeEmpty(
            "BusinessStaff.Create must not raise domain events; integration events " +
            "are staged by the command handler via IContentPlacesOutboxWriter " +
            "(CONTENTPLACES-FOLLOWUP-AGGREGATE-ROOT-001).");
    }

    [Fact]
    public void BusinessStaff_Deactivate_DoesNotRaiseDomainEvents()
    {
        var staff = StaffEntity.Create(
            businessId: Guid.NewGuid(),
            userId:     Guid.NewGuid(),
            role:       BusinessStaffRole.Staff);

        // Pre-condition: Create must already produce zero domain events
        // (also pinned by BusinessStaff_Create_DoesNotRaiseDomainEvents).
        staff.DomainEvents.Should().BeEmpty(
            "pre-condition: Create must not raise domain events");

        staff.Deactivate();

        staff.DomainEvents.Should().BeEmpty(
            "BusinessStaff.Deactivate must not raise domain events; the " +
            "BusinessStaffRemovedIntegrationEvent is staged by the command handler " +
            "via IContentPlacesOutboxWriter (CONTENTPLACES-FOLLOWUP-AGGREGATE-ROOT-001).");

        staff.IsActive.Should().BeFalse(
            "Deactivate must still flip IsActive to false (state change preserved)");
        staff.DeactivatedAt.Should().NotBeNull(
            "Deactivate must still stamp DeactivatedAt (state change preserved)");
    }
}
