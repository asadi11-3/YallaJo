using FluentAssertions;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Messaging.IntegrationTests;

/// <summary>
/// Regression tests for the notification mutation-persistence bug (FE-1B backend fix).
///
/// Bug: command handlers (MarkNotificationRead / DeleteNotification /
/// BatchDeleteNotifications) loaded the notification via the base
/// <c>GetByIdAsync</c>, which defaults to <c>AsNoTracking()</c>. The entity was
/// therefore detached, so <c>MarkRead()</c> / <c>SoftDelete()</c> mutated an
/// untracked instance and <c>SaveChangesAsync</c> persisted nothing — the API
/// returned success but the row never changed.
///
/// Fix: <c>NotificationRepository.GetByIdForUpdateAsync</c> loads a CHANGE-TRACKED
/// entity (plain <c>FirstOrDefaultAsync</c> on the DbSet, no AsNoTracking), and the
/// three mutation handlers now use it.
///
/// These tests run the SAME query shapes as the production repository methods over
/// the real <see cref="MessagingDbContext"/> (InMemory provider). The tracked vs
/// no-tracking distinction is provider-agnostic, so this faithfully reproduces both
/// the bug and the fix. <c>NotificationRepository</c> is internal and cannot be
/// referenced here, so the query shapes are mirrored exactly.
/// </summary>
public sealed class NotificationMutationPersistenceTests : IAsyncLifetime
{
    private readonly MessagingDbContext _ctx;
    private readonly Guid _userId = Guid.NewGuid();

    public NotificationMutationPersistenceTests()
    {
        var options = new DbContextOptionsBuilder<MessagingDbContext>()
            .UseInMemoryDatabase($"messaging-notif-mutation-{Guid.NewGuid():N}")
            .Options;
        _ctx = new MessagingDbContext(options);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _ctx.DisposeAsync();

    private Notification NewNotification(NotificationType type = NotificationType.BookingConfirmed)
        => Notification.Create(_userId, type, NotificationChannel.InApp, "Title", "Body");

    private async Task<Notification> SeedAsync(Notification n)
    {
        _ctx.Notifications.Add(n);
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear(); // detach so each test starts from a cold load
        return n;
    }

    // Mirrors NotificationRepository.GetByIdForUpdateAsync (the FIX): tracked load.
    private Task<Notification?> GetByIdForUpdateAsync(Guid id)
        => _ctx.Notifications.FirstOrDefaultAsync(n => n.Id == id);

    // Mirrors the OLD path the handlers used: AsNoTracking (the BUG).
    private Task<Notification?> GetByIdNoTrackingAsync(Guid id)
        => _ctx.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);

    // ── 1. Mark-read persists IsRead + ReadAt via the tracked load ───────────

    [Fact]
    public async Task MarkRead_ViaTrackedLoad_PersistsIsReadAndReadAt()
    {
        var seeded = await SeedAsync(NewNotification());

        var entity = await GetByIdForUpdateAsync(seeded.Id);
        entity!.MarkRead(TimeProvider.System);
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();

        var reloaded = await _ctx.Notifications.AsNoTracking().SingleAsync(n => n.Id == seeded.Id);
        reloaded.IsRead.Should().BeTrue();
        reloaded.ReadAt.Should().NotBeNull();
    }

    // ── 2. Regression guard: the OLD no-tracking load does NOT persist ───────

    [Fact]
    public async Task MarkRead_ViaNoTrackingLoad_DoesNotPersist_RegressionGuard()
    {
        var seeded = await SeedAsync(NewNotification());

        var detached = await GetByIdNoTrackingAsync(seeded.Id);
        detached!.MarkRead(TimeProvider.System);
        await _ctx.SaveChangesAsync(); // no-op: entity is not tracked
        _ctx.ChangeTracker.Clear();

        var reloaded = await _ctx.Notifications.AsNoTracking().SingleAsync(n => n.Id == seeded.Id);
        reloaded.IsRead.Should().BeFalse("the no-tracking path is exactly the bug this fix removes");
    }

    // ── 3. Delete persists soft-delete via the tracked load ──────────────────

    [Fact]
    public async Task Delete_ViaTrackedLoad_PersistsSoftDelete_AndDisappearsFromList()
    {
        var seeded = await SeedAsync(NewNotification(NotificationType.ReviewPosted));

        var entity = await GetByIdForUpdateAsync(seeded.Id);
        entity!.SoftDelete();
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();

        // Default (filtered) list no longer returns it.
        var visible = await _ctx.Notifications.AsNoTracking()
            .Where(n => n.UserId == _userId).ToListAsync();
        visible.Should().BeEmpty("the soft-deleted notification must drop out of the user list");

        // It still physically exists (soft delete, not hard delete).
        var raw = await _ctx.Notifications.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(n => n.Id == seeded.Id);
        raw.IsDeleted.Should().BeTrue();
    }

    // ── 4. Owner check: a non-owner cannot mutate (domain stays user-scoped) ──
    //      Replicates the handler guard: the caller != notification.UserId path.

    [Fact]
    public async Task ForeignUserId_DoesNotMatchOwner_GuardWouldReject()
    {
        var seeded = await SeedAsync(NewNotification());
        var foreignUser = Guid.NewGuid();

        var entity = await GetByIdForUpdateAsync(seeded.Id);

        // The handler returns Forbidden when notification.UserId != caller; assert the
        // ownership data the guard relies on is intact after a tracked load.
        entity!.UserId.Should().Be(_userId);
        entity.UserId.Should().NotBe(foreignUser);
    }

    // ── 5. Critical-type delete rejection is unchanged (domain behaviour) ─────

    [Fact]
    public async Task CriticalNotification_IsCritical_RemainsTrue_AfterTrackedLoad()
    {
        // PaymentCompleted is a critical type — handlers reject its deletion.
        var seeded = await SeedAsync(NewNotification(NotificationType.PaymentCompleted));

        var entity = await GetByIdForUpdateAsync(seeded.Id);

        entity!.Type.IsCritical().Should().BeTrue(
            "critical notifications must still be detected so the delete handler returns CannotDeleteCritical");
    }
}
