using FluentAssertions;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Messaging.IntegrationTests;

/// <summary>
/// Regression tests for the Notification soft-delete bug.
///
/// Bug: <see cref="Notification"/> uses <c>SoftDelete()</c> (called from
/// DeleteNotificationCommandHandler and BatchDeleteNotificationsCommandHandler), but
/// NotificationConfiguration did not declare <c>HasQueryFilter(x =&gt; !x.IsDeleted)</c>.
/// As a result soft-deleted notifications still appeared in user lists, unread counts,
/// mark-all-read, pending dispatch and cleanup queries.
///
/// Fix: NotificationConfiguration now calls <c>builder.HasQueryFilter(x =&gt; !x.IsDeleted)</c>.
///
/// These tests run against the REAL production <see cref="MessagingDbContext"/> over the
/// EF Core InMemory provider. InMemory honours global query filters and ignores SQL
/// Server-specific concerns (<c>nvarchar(max)</c>, <c>rowversion</c>) that would block
/// SQLite DDL generation for this context. The query SHAPES below mirror
/// <c>NotificationRepository</c> exactly (they all run through
/// <c>_context.Notifications</c>, which is where the global filter is applied), so the
/// repository inherits this behaviour automatically. <c>NotificationRepository</c> is
/// <c>internal</c>, so it cannot be referenced directly from this test assembly.
///
/// xUnit creates a fresh instance per [Fact] with a uniquely-named InMemory database,
/// so there is no cross-test pollution.
/// </summary>
public sealed class NotificationSoftDeleteQueryFilterTests : IAsyncLifetime
{
    private readonly MessagingDbContext _ctx;
    private readonly Guid _userId = Guid.NewGuid();

    public NotificationSoftDeleteQueryFilterTests()
    {
        var options = new DbContextOptionsBuilder<MessagingDbContext>()
            .UseInMemoryDatabase($"messaging-notif-softdelete-{Guid.NewGuid():N}")
            .Options;

        _ctx = new MessagingDbContext(options);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _ctx.DisposeAsync();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Notification NewNotification(NotificationType type = NotificationType.BookingConfirmed)
        => Notification.Create(_userId, type, NotificationChannel.InApp, "Title", "Body");

    private async Task<Notification> AddAsync(Notification n)
    {
        _ctx.Notifications.Add(n);
        await _ctx.SaveChangesAsync();
        return n;
    }

    private async Task SoftDeleteAsync(Notification n)
    {
        n.SoftDelete();
        await _ctx.SaveChangesAsync();
    }

    // ── 1. EF model — query filter must be registered ────────────────────────

    [Fact]
    public void Notification_EfModel_HasSoftDeleteQueryFilterRegistered()
    {
        var entityType = _ctx.Model.FindEntityType(typeof(Notification));
        entityType.Should().NotBeNull("Notification must be mapped in the Messaging model");

        var filter = entityType!.GetQueryFilter();
        filter.Should().NotBeNull(
            "NotificationConfiguration must call HasQueryFilter(x => !x.IsDeleted) so soft-deleted " +
            "notifications are excluded from default queries");
        filter!.ToString().Should().Contain("IsDeleted",
            "the registered query filter must reference IsDeleted");
    }

    // ── 2. User list excludes soft-deleted notifications ─────────────────────
    //      Mirrors NotificationRepository.GetByUserPagedAsync query shape.

    [Fact]
    public async Task UserList_ExcludesSoftDeletedNotifications()
    {
        var visible = await AddAsync(NewNotification());
        var deleted = await AddAsync(NewNotification());
        await SoftDeleteAsync(deleted);

        var items = await _ctx.Notifications.AsNoTracking()
            .Where(n => n.UserId == _userId)
            .OrderByDescending(n => n.Id)
            .ToListAsync();

        items.Should().ContainSingle("only the non-deleted notification must appear in the user list");
        items[0].Id.Should().Be(visible.Id);
        items.Should().NotContain(n => n.Id == deleted.Id);
    }

    // ── 3. Unread count excludes soft-deleted notifications ──────────────────
    //      Mirrors NotificationRepository.GetUnreadCountByUserAsync.

    [Fact]
    public async Task UnreadCount_DoesNotCountSoftDeleted()
    {
        await AddAsync(NewNotification());               // unread, visible
        var deleted = await AddAsync(NewNotification()); // unread, then deleted
        await SoftDeleteAsync(deleted);

        var count = await _ctx.Notifications.AsNoTracking()
            .CountAsync(n => n.UserId == _userId && n.ReadAt == null);

        count.Should().Be(1, "a soft-deleted notification must not be counted as unread");
    }

    // ── 4. MarkAllAsRead does not touch soft-deleted notifications ───────────
    //      Mirrors NotificationRepository.MarkAllAsReadByUserAsync's filter predicate.
    //      NOTE: ExecuteUpdate is unsupported by the EF InMemory provider, so we assert
    //      the *update set* (the rows the WHERE clause selects) excludes the soft-deleted
    //      row. The global query filter is what governs that set, which is the behaviour
    //      under test. We then apply the read transition to confirm the deleted row is
    //      untouched.

    [Fact]
    public async Task MarkAllAsRead_DoesNotAffectSoftDeletedNotifications()
    {
        var visible = await AddAsync(NewNotification());
        var deleted = await AddAsync(NewNotification());
        await SoftDeleteAsync(deleted);

        // The exact predicate MarkAllAsReadByUserAsync runs over _context.Notifications.
        var toMarkRead = await _ctx.Notifications
            .Where(n => n.UserId == _userId && n.ReadAt == null)
            .ToListAsync();

        toMarkRead.Should().ContainSingle("only the visible unread notification is in the update set");
        toMarkRead[0].Id.Should().Be(visible.Id);
        toMarkRead.Should().NotContain(n => n.Id == deleted.Id,
            "the soft-deleted notification must be excluded from the mark-all-read set");

        foreach (var n in toMarkRead) n.MarkRead(TimeProvider.System);
        await _ctx.SaveChangesAsync();

        // The deleted row, read back ignoring filters, must remain unread.
        var reloaded = await _ctx.Notifications
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(n => n.Id == deleted.Id);

        reloaded.ReadAt.Should().BeNull(
            "MarkAllAsRead must skip soft-deleted notifications (filtered out of the update set)");
        reloaded.IsDeleted.Should().BeTrue();
    }

    // ── 5. Pending dispatch excludes soft-deleted notifications ──────────────
    //      Mirrors NotificationRepository.GetPendingDispatchAsync.

    [Fact]
    public async Task PendingDispatch_ExcludesSoftDeletedNotifications()
    {
        var pending = await AddAsync(NewNotification());  // SentAt == null, not deleted
        var deleted = await AddAsync(NewNotification());  // SentAt == null, then deleted
        await SoftDeleteAsync(deleted);

        var result = await _ctx.Notifications.AsNoTracking()
            .Where(n => n.SentAt == null && n.FailureReason == null)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync();

        result.Should().ContainSingle("only the non-deleted pending notification must be dispatched");
        result[0].Id.Should().Be(pending.Id);
        result.Should().NotContain(n => n.Id == deleted.Id,
            "a soft-deleted notification must never be sent");
    }

    // ── 6. Cleanup excludes soft-deleted notifications ───────────────────────
    //      Mirrors NotificationRepository.GetOldReadForCleanupAsync.

    [Fact]
    public async Task Cleanup_ExcludesSoftDeletedNotifications()
    {
        var olderThan = DateTime.UtcNow.AddDays(-1);

        // A read, old, non-deleted notification -> eligible for cleanup.
        var eligible = NewNotification();
        eligible.MarkRead(TimeProvider.System);
        Backdate(eligible, DateTime.UtcNow.AddDays(-10));
        await AddAsync(eligible);

        // A read, old, soft-deleted notification -> excluded by the query filter.
        var deleted = NewNotification();
        deleted.MarkRead(TimeProvider.System);
        Backdate(deleted, DateTime.UtcNow.AddDays(-10));
        deleted.SoftDelete();
        await AddAsync(deleted);

        var result = await _ctx.Notifications.AsNoTracking()
            .Where(n => n.ReadAt != null && n.CreatedAt < olderThan)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync();

        result.Should().ContainSingle("cleanup operates only on visible (non-deleted) rows");
        result[0].Id.Should().Be(eligible.Id);
        result.Should().NotContain(n => n.Id == deleted.Id);
    }

    // ── 7. Delete-handler semantics: re-fetch of a soft-deleted row is NotFound

    /// <summary>
    /// Documents and locks in the chosen delete-handler behaviour: a filter-respecting
    /// lookup of an already soft-deleted notification returns null.
    /// DeleteNotificationCommandHandler / BatchDeleteNotificationsCommandHandler now use
    /// <c>GetByIdForUpdateAsync</c> (a CHANGE-TRACKED lookup that still honors the global
    /// !IsDeleted filter), so re-deleting an already-deleted notification yields NotFound
    /// rather than an idempotent success. This is intentional and consistent with the
    /// soft-delete semantics; IgnoreQueryFilters() is deliberately NOT used in those
    /// handlers.
    /// </summary>
    [Fact]
    public async Task SoftDeletedNotification_IsNotFound_ByDefaultLookup()
    {
        var deleted = await AddAsync(NewNotification());
        await SoftDeleteAsync(deleted);

        var refetched = await _ctx.Notifications.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == deleted.Id);

        refetched.Should().BeNull(
            "a soft-deleted notification must not be retrievable via the default filtered query, " +
            "so re-delete returns NotFound (chosen non-idempotent behaviour)");

        // Sanity: it still physically exists and is retrievable when filters are ignored.
        var raw = await _ctx.Notifications.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == deleted.Id);
        raw.Should().NotBeNull("soft delete must not physically remove the row");
    }

    /// <summary>
    /// CreatedAt is set to UtcNow at construction and is protected; backdate it via
    /// reflection so the cleanup cutoff (CreatedAt &lt; olderThan) can match in tests.
    /// </summary>
    private static void Backdate(Notification n, DateTime createdAt)
    {
        var prop = typeof(YallaJo.SharedKernel.Domain.Entities.BaseEntity<Guid>)
            .GetProperty(nameof(Notification.CreatedAt))!;
        prop.SetValue(n, createdAt);
    }
}
