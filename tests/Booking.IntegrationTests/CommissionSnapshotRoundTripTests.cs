using System.Globalization;
using System.Reflection;
using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Booking.Infrastructure.BackgroundServices.Options;
using Booking.Infrastructure.EventHandlers;
using Booking.Infrastructure.Persistence;
using Booking.Infrastructure.Services;
using Finance.Contracts.IntegrationEvents;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Inbox;

namespace Booking.IntegrationTests;

/// <summary>
/// End-to-end: Finance commission rule integration event → Booking snapshot table →
/// SnapshotBookingCommissionLookup. Pure Sqlite in-memory; no MediatR pipeline needed —
/// invokes the handlers directly to keep the test deterministic.
/// </summary>
public sealed class CommissionSnapshotRoundTripTests
{
    [Fact]
    public async Task Upsert_event_persists_snapshot_and_lookup_returns_matching_rate()
    {
        var (context, snapshotRepo) = NewContext();
        var inbox = new BookingInboxStoreAdapter(context);

        var ruleId = Guid.NewGuid();
        var upsertHandler = new FinanceCommissionRuleUpsertedIntegrationEventHandler(
            snapshotRepo, inbox, new TestUow(context),
            NullLogger<FinanceCommissionRuleUpsertedIntegrationEventHandler>.Instance);

        await upsertHandler.Handle(
            new IntegrationEventNotification<CommissionRuleUpsertedIntegrationEvent>(
                MessageId: Guid.NewGuid(),
                Event: new CommissionRuleUpsertedIntegrationEvent(
                    RuleId: ruleId,
                    Tier: "Free",
                    MinMonthlyRevenue: 0m,
                    MaxMonthlyRevenue: null,
                    Currency: "JOD",
                    Percentage: 7.5m,
                    OccurredAt: DateTime.UtcNow)),
            CancellationToken.None);

        var stored = await context.CommissionSnapshots.AsNoTracking().FirstAsync(s => s.Id == ruleId);
        stored.Tier.Should().Be("Free");
        stored.Currency.Should().Be("JOD");
        stored.Percentage.Should().Be(7.5m);
        stored.IsActive.Should().BeTrue();

        var lookup = NewLookup(snapshotRepo);
        var result = await lookup.GetForTourAsync(Guid.NewGuid(), CancellationToken.None);
        result.Rate.Should().Be(0.075m);
    }

    [Fact]
    public async Task Delete_event_deactivates_snapshot_and_lookup_falls_back()
    {
        var (context, snapshotRepo) = NewContext();
        var inbox = new BookingInboxStoreAdapter(context);

        var ruleId = Guid.NewGuid();
        var upsertHandler = new FinanceCommissionRuleUpsertedIntegrationEventHandler(
            snapshotRepo, inbox, new TestUow(context),
            NullLogger<FinanceCommissionRuleUpsertedIntegrationEventHandler>.Instance);
        var deleteHandler = new FinanceCommissionRuleDeletedIntegrationEventHandler(
            snapshotRepo, inbox, new TestUow(context),
            NullLogger<FinanceCommissionRuleDeletedIntegrationEventHandler>.Instance);

        await upsertHandler.Handle(
            new IntegrationEventNotification<CommissionRuleUpsertedIntegrationEvent>(
                Guid.NewGuid(),
                new CommissionRuleUpsertedIntegrationEvent(
                    ruleId, "Free", 0m, null, "JOD", 7.5m, DateTime.UtcNow)),
            CancellationToken.None);

        await deleteHandler.Handle(
            new IntegrationEventNotification<CommissionRuleDeletedIntegrationEvent>(
                Guid.NewGuid(),
                new CommissionRuleDeletedIntegrationEvent(ruleId, DateTime.UtcNow.AddSeconds(1))),
            CancellationToken.None);

        var stored = await context.CommissionSnapshots.AsNoTracking().FirstAsync(s => s.Id == ruleId);
        stored.IsActive.Should().BeFalse();

        var lookup = NewLookup(snapshotRepo);
        var result = await lookup.GetForTourAsync(Guid.NewGuid(), CancellationToken.None);
        // Inactive → no match → fallback rate 0.10.
        result.Rate.Should().Be(0.10m);
    }

    [Fact]
    public async Task Idempotent_re_delivery_does_not_create_second_inbox_row()
    {
        var (context, snapshotRepo) = NewContext();
        var inbox = new BookingInboxStoreAdapter(context);

        var ruleId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var handler = new FinanceCommissionRuleUpsertedIntegrationEventHandler(
            snapshotRepo, inbox, new TestUow(context),
            NullLogger<FinanceCommissionRuleUpsertedIntegrationEventHandler>.Instance);

        var evt = new IntegrationEventNotification<CommissionRuleUpsertedIntegrationEvent>(
            messageId,
            new CommissionRuleUpsertedIntegrationEvent(
                ruleId, "Free", 0m, null, "JOD", 10m, DateTime.UtcNow));

        await handler.Handle(evt, CancellationToken.None);
        await handler.Handle(evt, CancellationToken.None);

        var inboxCount = await context.Set<InboxMessage>().CountAsync(m => m.Id == messageId);
        inboxCount.Should().Be(1);
        var snapshotCount = await context.CommissionSnapshots.CountAsync(s => s.Id == ruleId);
        snapshotCount.Should().Be(1);
    }

    // ── Test infrastructure ───────────────────────────────────────────────────

    private static (BookingDbContext context, ICommissionSnapshotRepository repo) NewContext()
    {
        var conn = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlite(conn)
            .Options;
        var ctx = new BookingDbContext(options);
        ctx.Database.EnsureCreated();
        var repo = CreateRepo(ctx);
        return (ctx, repo);
    }

    private static ICommissionSnapshotRepository CreateRepo(BookingDbContext ctx)
    {
        var type = typeof(Booking.Infrastructure.DependencyInjection).Assembly
            .GetType("Booking.Infrastructure.Repositories.CommissionSnapshotRepository")
            ?? throw new InvalidOperationException("Could not locate CommissionSnapshotRepository.");
        var instance = Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [ctx],
            culture: CultureInfo.InvariantCulture)
            ?? throw new InvalidOperationException("Failed to instantiate CommissionSnapshotRepository.");
        return (ICommissionSnapshotRepository)instance;
    }

    private static SnapshotBookingCommissionLookup NewLookup(ICommissionSnapshotRepository repo)
        => new(
            repo,
            Options.Create(new BookingCommissionDefaultsOptions
            {
                Tier = "Free",
                Currency = "JOD",
                FallbackRate = 0.10m,
            }),
            NullLogger<SnapshotBookingCommissionLookup>.Instance);

    private sealed class TestUow(BookingDbContext ctx) : IBookingUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => ctx.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Mirrors the production <c>BookingInboxStore</c> verbatim; needed because the prod type
    /// is <c>internal</c> and this test project has no <c>InternalsVisibleTo</c>.
    /// </summary>
    private sealed class BookingInboxStoreAdapter(BookingDbContext ctx) : IBookingInboxStore
    {
        public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct = default)
            => ctx.Set<InboxMessage>().AnyAsync(m => m.Id == messageId, ct);

        public void MarkAsProcessed(Guid messageId)
            => ctx.Set<InboxMessage>().Add(InboxMessage.Create(messageId));
    }
}
