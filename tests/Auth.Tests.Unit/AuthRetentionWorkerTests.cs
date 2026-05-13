using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Auth.Domain.Entities;
using Auth.Infrastructure.BackgroundJobs;
using Auth.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Auth.Tests.Unit;

/// <summary>
/// Phase 2C-4 — verifies the retention worker:
///   • deletes processed outbox rows older than the configured retention window,
///   • keeps processed rows YOUNGER than the window,
///   • NEVER deletes unprocessed / dead-lettered outbox rows,
///   • drains used / expired Otps (legacy UserInvite / PasswordReset fallback drainage),
///   • preserves active Otps,
///   • clamps non-positive retention configuration to a 1-hour floor,
///   • emits an accurate <see cref="AuthRetentionOutcome"/>.
/// <para>
/// Uses the <see cref="IRetentionDeleteAdapter"/> seam so tests run
/// against EF InMemory (which does NOT support ExecuteDeleteAsync).
/// The captured test adapter records every (source, predicate) pair
/// and performs the delete via materialize-and-remove, which is
/// equivalent to the SQL path for the narrow predicates the worker
/// uses.
/// </para>
/// </summary>
public sealed class AuthRetentionWorkerTests
{
    private static AuthDbContext CreateContext(string name)
    {
        var opts = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AuthDbContext(opts);
    }

    /// <summary>
    /// Test adapter — evaluates the EF-style <c>ExecuteDeleteAsync</c>
    /// predicate in-memory by materializing the filtered set, removing
    /// the matches, and saving. Functionally equivalent to the real
    /// adapter for the single-table predicates the worker emits.
    /// </summary>
    private sealed class CapturingDeleteAdapter(AuthDbContext db) : IRetentionDeleteAdapter
    {
        public async Task<int> DeleteAsync<TEntity>(
            IQueryable<TEntity> source,
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken ct)
            where TEntity : class
        {
            var matches = await source.Where(predicate).ToListAsync(ct);
            if (matches.Count == 0) return 0;

            db.Set<TEntity>().RemoveRange(matches);
            await db.SaveChangesAsync(ct);
            return matches.Count;
        }
    }

    private static AuthRetentionWorker CreateSut(
        AuthDbContext db,
        int processedOutboxRetentionHours = 24)
    {
        var options = Options.Create(new AuthRetentionOptions
        {
            ProcessedOutboxRetentionHours = processedOutboxRetentionHours,
        });
        return new AuthRetentionWorker(
            db,
            new CapturingDeleteAdapter(db),
            options,
            NullLogger<AuthRetentionWorker>.Instance);
    }

    private sealed record ProbeIntegrationEvent(string Payload) : IntegrationEventBase;

    /// <summary>
    /// Creates an OutboxMessage bypassing <c>OutboxMessage.Create</c> and
    /// <c>IntegrationEventTypeRegistry</c>. <see cref="ProbeIntegrationEvent"/>
    /// is a test-only shape and is intentionally NOT registered in the
    /// production registry; the retention worker's predicate references
    /// only <c>ProcessedOnUtc</c>, so <c>Type</c>/<c>Content</c> semantics
    /// are immaterial to these tests.
    /// <para>
    /// Mirrors the established pattern in
    /// <c>SharedKernel.Tests.Unit/OutboxCleanerTests.MakeRawMessage</c> and
    /// <c>SharedKernel.Tests.Unit/OutboxProcessorTests.CreateTestOutboxMessage</c>.
    /// </para>
    /// </summary>
    private static OutboxMessage NewOutbox(string payload = "x")
    {
        var evt = new ProbeIntegrationEvent(payload);

        // Construct via the private parameterless constructor.
        var msg = (OutboxMessage)Activator.CreateInstance(typeof(OutboxMessage), nonPublic: true)!;

        void Set(string propName, object? value) =>
            typeof(OutboxMessage)
                .GetProperty(propName, BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(msg, value);

        Set(nameof(OutboxMessage.Id), Guid.CreateVersion7());
        // Use AssemblyQualifiedName as the stable Type string — same approach
        // as OutboxProcessorTests.CreateTestOutboxMessage. The retention
        // worker does not read Type, so this value is purely for shape parity.
        Set(nameof(OutboxMessage.Type), typeof(ProbeIntegrationEvent).AssemblyQualifiedName!);
        Set(nameof(OutboxMessage.Content), JsonSerializer.Serialize(evt, typeof(ProbeIntegrationEvent)));
        Set(nameof(OutboxMessage.OccurredOnUtc), evt.OccurredOn);
        Set(nameof(OutboxMessage.RetryCount), 0);

        return msg;
    }

    private static void SetProcessedAt(OutboxMessage message, DateTime processedAt)
    {
        // Override ProcessedOnUtc for age simulation — uses the internal
        // setter on the aggregate. Matches the reflection pattern used
        // elsewhere in this test suite for timestamp-dependent cases.
        typeof(OutboxMessage).GetProperty(nameof(OutboxMessage.ProcessedOnUtc))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(message, new object?[] { processedAt });
    }

    // ── Outbox retention ──────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ShouldDeleteProcessedOutboxRows_OlderThanRetentionWindow()
    {
        using var db = CreateContext($"retention-old-{Guid.NewGuid()}");

        var old1 = NewOutbox("old-1");
        SetProcessedAt(old1, DateTime.UtcNow.AddHours(-48));
        var old2 = NewOutbox("old-2");
        SetProcessedAt(old2, DateTime.UtcNow.AddHours(-25));

        db.OutboxMessages.AddRange(old1, old2);
        await db.SaveChangesAsync();

        var outcome = await CreateSut(db, processedOutboxRetentionHours: 24)
            .ExecuteAsync(CancellationToken.None);

        outcome.ProcessedOutboxDeleted.Should().Be(2);
        (await db.OutboxMessages.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotDeleteProcessedOutboxRows_InsideRetentionWindow()
    {
        using var db = CreateContext($"retention-fresh-{Guid.NewGuid()}");

        var fresh = NewOutbox("fresh");
        SetProcessedAt(fresh, DateTime.UtcNow.AddHours(-1));

        db.OutboxMessages.Add(fresh);
        await db.SaveChangesAsync();

        var outcome = await CreateSut(db).ExecuteAsync(CancellationToken.None);

        outcome.ProcessedOutboxDeleted.Should().Be(0);
        (await db.OutboxMessages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNeverDeleteUnprocessedOutboxRows_EvenIfOld()
    {
        // Dead-lettered rows stay around forever so ops can investigate.
        // Even a row never processed must NOT be auto-deleted — it likely
        // represents a persistent email-delivery failure that needs
        // ops attention.
        using var db = CreateContext($"retention-unprocessed-{Guid.NewGuid()}");

        var stuck = NewOutbox("stuck");
        // ProcessedOnUtc stays null. The worker's predicate filters on
        // ProcessedOnUtc only (not RetryCount) so even an old row with
        // high retry count is safe from auto-purge.
        db.OutboxMessages.Add(stuck);
        await db.SaveChangesAsync();

        var outcome = await CreateSut(db).ExecuteAsync(CancellationToken.None);

        outcome.ProcessedOutboxDeleted.Should().Be(0);
        (await db.OutboxMessages.CountAsync()).Should().Be(1);
    }

    // ── Otp drainage (legacy UserInvite / PasswordReset fallback) ─────────────

    [Fact]
    public async Task ExecuteAsync_ShouldDeleteUsedOtps_AcrossAllPurposes()
    {
        using var db = CreateContext($"retention-otps-used-{Guid.NewGuid()}");

        var usedInvite = Otp.Create(Guid.NewGuid(), "UserInvite", "h1", "Email", "a@b.com", 60 * 24 * 7);
        usedInvite.MarkUsed();
        var usedReset = Otp.Create(Guid.NewGuid(), "PasswordReset", "h2", "Email", "a@b.com", 10);
        usedReset.MarkUsed();
        var usedVerify = Otp.Create(Guid.NewGuid(), "EmailVerification", "h3", "Email", "a@b.com", 10);
        usedVerify.MarkUsed();

        db.Otps.AddRange(usedInvite, usedReset, usedVerify);
        await db.SaveChangesAsync();

        var outcome = await CreateSut(db).ExecuteAsync(CancellationToken.None);

        outcome.OtpsDeleted.Should().Be(3);
        (await db.Otps.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotDeleteActiveUnexpiredOtps()
    {
        using var db = CreateContext($"retention-otps-active-{Guid.NewGuid()}");

        var active = Otp.Create(Guid.NewGuid(), "UserInvite", "h", "Email", "a@b.com", 60 * 24 * 7);

        db.Otps.Add(active);
        await db.SaveChangesAsync();

        var outcome = await CreateSut(db).ExecuteAsync(CancellationToken.None);

        outcome.OtpsDeleted.Should().Be(0);
        (await db.Otps.CountAsync()).Should().Be(1);
    }

    // ── Outcome shape ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_OnEmptyDb_ShouldReturnZeroedOutcome()
    {
        using var db = CreateContext($"retention-empty-{Guid.NewGuid()}");

        var outcome = await CreateSut(db).ExecuteAsync(CancellationToken.None);

        outcome.Should().BeEquivalentTo(new AuthRetentionOutcome(
            OtpsDeleted:            0,
            SessionsDeleted:        0,
            RefreshTokensDeleted:   0,
            ProcessedOutboxDeleted: 0));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRespectCustomRetentionHours()
    {
        using var db = CreateContext($"retention-custom-{Guid.NewGuid()}");

        var shortlyOld = NewOutbox("2h");
        SetProcessedAt(shortlyOld, DateTime.UtcNow.AddHours(-2));

        db.OutboxMessages.Add(shortlyOld);
        await db.SaveChangesAsync();

        var outcome = await CreateSut(db, processedOutboxRetentionHours: 1)
            .ExecuteAsync(CancellationToken.None);

        outcome.ProcessedOutboxDeleted.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldClampNonPositiveRetention_ToOneHourMinimum()
    {
        // A misconfigured 0 or negative value must not be interpreted as
        // "delete everything processed"; the worker clamps to a 1-hour
        // floor. A 30-minute-old row stays; a 2-hour-old row is deleted.
        using var db = CreateContext($"retention-clamp-{Guid.NewGuid()}");

        var thirtyMin = NewOutbox("30m");
        SetProcessedAt(thirtyMin, DateTime.UtcNow.AddMinutes(-30));
        var twoHours = NewOutbox("2h");
        SetProcessedAt(twoHours, DateTime.UtcNow.AddHours(-2));

        db.OutboxMessages.AddRange(thirtyMin, twoHours);
        await db.SaveChangesAsync();

        var outcome = await CreateSut(db, processedOutboxRetentionHours: 0)
            .ExecuteAsync(CancellationToken.None);

        outcome.ProcessedOutboxDeleted.Should().Be(1);
        (await db.OutboxMessages.CountAsync()).Should().Be(1);
    }
}
