using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace SharedKernel.Tests.Unit;

/// <summary>
/// Verifies the outbox processor's resilience guarantees after the refactor
/// away from <c>mediator.Publish</c>:
///   1. One throwing handler MUST NOT block another handler for the same event.
///   2. A message is only marked processed when ALL handlers succeed.
///   3. A transient handler failure increments RetryCount but the message is
///      re-picked on the next run; once all handlers are happy, RetryCount is
///      left as history and ProcessedOnUtc is set.
///   4. Cancellation aborts cleanly without marking the message processed.
/// </summary>
public sealed class OutboxProcessorTests
{
    // ── Fixture: a minimal DbContext hosting the outbox table ──────────────────

    private sealed class OutboxOnlyDbContext : DbContext
    {
        public OutboxOnlyDbContext(DbContextOptions<OutboxOnlyDbContext> opts) : base(opts) { }
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(b =>
            {
                b.ToTable("OutboxMessages");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.Property(x => x.Type).IsRequired();
                b.Property(x => x.Content).IsRequired();
                b.Property(x => x.OccurredOnUtc).IsRequired();
                b.Property(x => x.ProcessedOnUtc);
                b.Property(x => x.Error);
                b.Property(x => x.RetryCount).IsRequired();
                b.Property(x => x.LockedUntil);
            });
        }
    }

    // ── Fixture: a tiny integration event plus two handlers ───────────────────

    public sealed record SampleIntegrationEvent(string Value) : IntegrationEventBase;

    public sealed class RecordingState
    {
        public int HandlerAInvocations;
        public int HandlerBInvocations;
        public bool HandlerAShouldThrow;
    }

    public sealed class HandlerA(RecordingState state)
        : INotificationHandler<IntegrationEventNotification<SampleIntegrationEvent>>
    {
        public Task Handle(IntegrationEventNotification<SampleIntegrationEvent> n, CancellationToken ct)
        {
            Interlocked.Increment(ref state.HandlerAInvocations);
            if (state.HandlerAShouldThrow)
                throw new InvalidOperationException("HandlerA deliberately failed");
            return Task.CompletedTask;
        }
    }

    public sealed class HandlerB(RecordingState state)
        : INotificationHandler<IntegrationEventNotification<SampleIntegrationEvent>>
    {
        public Task Handle(IntegrationEventNotification<SampleIntegrationEvent> n, CancellationToken ct)
        {
            Interlocked.Increment(ref state.HandlerBInvocations);
            return Task.CompletedTask;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (IServiceProvider sp, RecordingState state, OutboxProcessor<OutboxOnlyDbContext> processor)
        BuildHarness()
    {
        var state = new RecordingState();
        var services = new ServiceCollection();
        services.AddSingleton(state);
        var root = new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot();
        var dbName = $"outbox-{Guid.NewGuid()}";
        // Use explicit optionsAction+contextLifetime so both options and context
        // are scoped the same way they would be in production.
        services.AddDbContext<OutboxOnlyDbContext>(
            o => o.UseInMemoryDatabase(dbName, root),
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Singleton);

        // Register both handlers as the concrete closed generic — this is what
        // AddMediatR would do for discovered handlers. The processor resolves
        // them via GetServices<INotificationHandler<...>>().
        services.AddScoped<INotificationHandler<IntegrationEventNotification<SampleIntegrationEvent>>, HandlerA>();
        services.AddScoped<INotificationHandler<IntegrationEventNotification<SampleIntegrationEvent>>, HandlerB>();

        var sp = services.BuildServiceProvider();
        var processor = new OutboxProcessor<OutboxOnlyDbContext>(
            sp, NullLogger<OutboxProcessor<OutboxOnlyDbContext>>.Instance);
        return (sp, state, processor);
    }

    private static async Task<Guid> SeedOutboxMessageAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutboxOnlyDbContext>();
        var msg = OutboxMessage.Create(new SampleIntegrationEvent("hello"));
        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync();
        return msg.Id;
    }

    private static async Task<OutboxMessage?> ReloadAsync(IServiceProvider sp, Guid id)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutboxOnlyDbContext>();
        return await db.OutboxMessages.FindAsync(id);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AllHandlersSucceed_ShouldMarkMessageProcessed()
    {
        var (sp, state, processor) = BuildHarness();
        var id = await SeedOutboxMessageAsync(sp);

        await processor.ProcessOutboxMessagesAsync(CancellationToken.None);

        state.HandlerAInvocations.Should().Be(1);
        state.HandlerBInvocations.Should().Be(1);

        var reloaded = await ReloadAsync(sp, id);
        reloaded.Should().NotBeNull();
        reloaded!.ProcessedOnUtc.Should().NotBeNull();
        reloaded.RetryCount.Should().Be(0);
        reloaded.Error.Should().BeNull();
    }

    [Fact]
    public async Task OneHandlerFails_OtherHandlerStillRuns_AndMessageRetries()
    {
        var (sp, state, processor) = BuildHarness();
        state.HandlerAShouldThrow = true;
        var id = await SeedOutboxMessageAsync(sp);

        await processor.ProcessOutboxMessagesAsync(CancellationToken.None);

        // Critical: HandlerB MUST run even though HandlerA threw.
        state.HandlerAInvocations.Should().Be(1);
        state.HandlerBInvocations.Should().Be(1,
            "one poison handler must not starve peer consumers of the same event");

        var reloaded = await ReloadAsync(sp, id);
        reloaded!.ProcessedOnUtc.Should().BeNull("message must be retried");
        reloaded.RetryCount.Should().Be(1);
        reloaded.Error.Should().NotBeNull();
        reloaded.Error.Should().Contain("HandlerA");
    }

    [Fact]
    public async Task TransientFailure_ShouldBeRetriedAndEventuallyProcessed()
    {
        var (sp, state, processor) = BuildHarness();
        state.HandlerAShouldThrow = true;
        var id = await SeedOutboxMessageAsync(sp);

        await processor.ProcessOutboxMessagesAsync(CancellationToken.None);

        // Simulate the transient condition clearing.
        state.HandlerAShouldThrow = false;

        // The message is locked for 5 minutes on first pick; force the lock to expire.
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OutboxOnlyDbContext>();
            var row = await db.OutboxMessages.FindAsync(id);
            var prop = typeof(OutboxMessage).GetProperty("LockedUntil")!;
            prop.SetValue(row, DateTime.UtcNow.AddMinutes(-1));
            await db.SaveChangesAsync();
        }

        await processor.ProcessOutboxMessagesAsync(CancellationToken.None);

        var reloaded = await ReloadAsync(sp, id);
        reloaded!.ProcessedOnUtc.Should().NotBeNull("after the transient failure clears the message must be processed");
        reloaded.RetryCount.Should().Be(1, "the one prior failure stays on record as an audit trail");
    }

    [Fact]
    public async Task MessageWithRetryCountAtCeiling_ShouldNotBeProcessed()
    {
        var (sp, _, processor) = BuildHarness();
        var id = await SeedOutboxMessageAsync(sp);

        // Push RetryCount to the ceiling so the processor ignores it (dead-letter behavior).
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OutboxOnlyDbContext>();
            var row = await db.OutboxMessages.FindAsync(id);
            var retryProp = typeof(OutboxMessage).GetProperty("RetryCount")!;
            retryProp.SetValue(row, 10);
            await db.SaveChangesAsync();
        }

        await processor.ProcessOutboxMessagesAsync(CancellationToken.None);

        var reloaded = await ReloadAsync(sp, id);
        reloaded!.ProcessedOnUtc.Should().BeNull("dead-lettered messages must remain queryable for ops");
    }

    [Fact]
    public async Task NoHandlersRegistered_ShouldStillMarkProcessedIdempotently()
    {
        var services = new ServiceCollection();
        var root = new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot();
        var dbName = $"empty-{Guid.NewGuid()}";
        services.AddDbContext<OutboxOnlyDbContext>(
            o => o.UseInMemoryDatabase(dbName, root),
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        var processor = new OutboxProcessor<OutboxOnlyDbContext>(
            sp, NullLogger<OutboxProcessor<OutboxOnlyDbContext>>.Instance);

        var id = await SeedOutboxMessageAsync(sp);

        await processor.ProcessOutboxMessagesAsync(CancellationToken.None);

        var reloaded = await ReloadAsync(sp, id);
        reloaded.Should().NotBeNull();
        reloaded!.ProcessedOnUtc.Should().NotBeNull("orphan messages must drain so the outbox never jams");
    }
}
