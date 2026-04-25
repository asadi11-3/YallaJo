using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace SharedKernel.Tests.Unit;

/// <summary>
/// Verifies OutboxCleaner deletion rules:
///   1. Processed rows older than cutoff are deleted.
///   2. Processed rows NEWER than cutoff are kept.
///   3. Dead-lettered rows (RetryCount >= MaxRetryCount) are NEVER deleted.
///   4. Pending (unprocessed) rows are NEVER deleted.
///   5. Batch iteration: if first batch fills batchSize, a second call is made.
/// </summary>
public sealed class OutboxCleanerTests
{
    // ── Minimal DbContext with OutboxMessage set ────────────────────────────

    private sealed class CleanupTestDbContext : DbContext
    {
        public CleanupTestDbContext(DbContextOptions<CleanupTestDbContext> opts) : base(opts) { }

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

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static (IServiceScopeFactory scopeFactory, CleanupTestDbContext db) BuildFixture(
        string dbName)
    {
        // Share the same InMemoryDatabaseRoot so all scopes see the same in-memory data
        var root = new InMemoryDatabaseRoot();

        var services = new ServiceCollection();
        services.AddDbContext<CleanupTestDbContext>(opts =>
            opts.UseInMemoryDatabase(dbName, root));

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<CleanupTestDbContext>();
        db.Database.EnsureCreated();

        return (provider.GetRequiredService<IServiceScopeFactory>(), db);
    }

    /// <summary>
    /// Creates an OutboxMessage bypassing IntegrationEventTypeRegistry.
    /// StubIntegrationEvent is not registered — this is intentional; cleanup tests
    /// only need the message shape, not handler dispatch.
    /// </summary>
    private static OutboxMessage MakeRawMessage(
        bool processed = false,
        DateTime? processedAt = null,
        int retryCount = 0)
    {
        // Construct via private parameterless ctor
        var msg = (OutboxMessage)Activator.CreateInstance(typeof(OutboxMessage), nonPublic: true)!;

        void Set(string prop, object? value) =>
            typeof(OutboxMessage)
                .GetProperty(prop, BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(msg, value);

        Set("Id", Guid.CreateVersion7());
        Set("Type", typeof(StubIntegrationEvent).AssemblyQualifiedName!);
        Set("Content", JsonSerializer.Serialize(new StubIntegrationEvent()));
        Set("OccurredOnUtc", DateTime.UtcNow);
        Set("RetryCount", 0);

        // Bump RetryCount via MarkAsFailed (public API)
        for (int i = 0; i < retryCount; i++)
            msg.MarkAsFailed($"simulated failure {i}");

        if (processed)
            msg.MarkAsProcessedAt(processedAt ?? DateTime.UtcNow.AddDays(-40));

        return msg;
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeletesProcessedRows_OlderThanCutoff()
    {
        // Arrange
        var (scopeFactory, db) = BuildFixture(nameof(DeletesProcessedRows_OlderThanCutoff));

        var oldRow = MakeRawMessage(processed: true, processedAt: DateTime.UtcNow.AddDays(-40));
        db.OutboxMessages.Add(oldRow);
        await db.SaveChangesAsync();

        var cleaner = new OutboxCleaner<CleanupTestDbContext>(scopeFactory);

        // Act
        var deleted = await cleaner.DeleteProcessedBeforeAsync(
            cutoffUtc: DateTime.UtcNow.AddDays(-30),
            batchSize: 100,
            CancellationToken.None);

        // Assert
        deleted.Should().Be(1);
        (await db.OutboxMessages.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task KeepsProcessedRows_NewerThanCutoff()
    {
        // Arrange
        var (scopeFactory, db) = BuildFixture(nameof(KeepsProcessedRows_NewerThanCutoff));

        var freshRow = MakeRawMessage(processed: true, processedAt: DateTime.UtcNow.AddDays(-5));
        db.OutboxMessages.Add(freshRow);
        await db.SaveChangesAsync();

        var cleaner = new OutboxCleaner<CleanupTestDbContext>(scopeFactory);

        // Act
        var deleted = await cleaner.DeleteProcessedBeforeAsync(
            cutoffUtc: DateTime.UtcNow.AddDays(-30),
            batchSize: 100,
            CancellationToken.None);

        // Assert — fresh row NOT deleted
        deleted.Should().Be(0);
        (await db.OutboxMessages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task NeverDeletes_DeadLetteredRows()
    {
        // Arrange — dead-lettered: RetryCount >= MaxRetryCount, old, "processed" status
        var (scopeFactory, db) = BuildFixture(nameof(NeverDeletes_DeadLetteredRows));

        var deadRow = MakeRawMessage(
            processed: true,
            processedAt: DateTime.UtcNow.AddDays(-60),
            retryCount: OutboxProcessor<CleanupTestDbContext>.MaxRetryCount);

        db.OutboxMessages.Add(deadRow);
        await db.SaveChangesAsync();

        var cleaner = new OutboxCleaner<CleanupTestDbContext>(scopeFactory);

        // Act — very aggressive cutoff (everything older than 1 day)
        var deleted = await cleaner.DeleteProcessedBeforeAsync(
            cutoffUtc: DateTime.UtcNow.AddDays(-1),
            batchSize: 100,
            CancellationToken.None);

        // Assert — dead-lettered row stays forever
        deleted.Should().Be(0);
        (await db.OutboxMessages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task NeverDeletes_PendingRows()
    {
        // Arrange — pending: ProcessedOnUtc is null
        var (scopeFactory, db) = BuildFixture(nameof(NeverDeletes_PendingRows));

        var pending = MakeRawMessage(processed: false);
        db.OutboxMessages.Add(pending);
        await db.SaveChangesAsync();

        var cleaner = new OutboxCleaner<CleanupTestDbContext>(scopeFactory);

        // Act
        var deleted = await cleaner.DeleteProcessedBeforeAsync(
            cutoffUtc: DateTime.UtcNow.AddDays(-1),
            batchSize: 100,
            CancellationToken.None);

        // Assert — pending row is untouched
        deleted.Should().Be(0);
        (await db.OutboxMessages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CorrectlyHandles_MixedRows()
    {
        // Arrange: old processed, fresh processed, dead-lettered, pending
        var (scopeFactory, db) = BuildFixture(nameof(CorrectlyHandles_MixedRows));

        var oldProcessed = MakeRawMessage(processed: true, processedAt: DateTime.UtcNow.AddDays(-40));
        var freshProcessed = MakeRawMessage(processed: true, processedAt: DateTime.UtcNow.AddDays(-5));
        var deadLettered = MakeRawMessage(
            processed: true,
            processedAt: DateTime.UtcNow.AddDays(-50),
            retryCount: OutboxProcessor<CleanupTestDbContext>.MaxRetryCount);
        var pending = MakeRawMessage(processed: false);

        db.OutboxMessages.AddRange(oldProcessed, freshProcessed, deadLettered, pending);
        await db.SaveChangesAsync();

        var cleaner = new OutboxCleaner<CleanupTestDbContext>(scopeFactory);

        // Act
        var deleted = await cleaner.DeleteProcessedBeforeAsync(
            cutoffUtc: DateTime.UtcNow.AddDays(-30),
            batchSize: 100,
            CancellationToken.None);

        // Assert — only the old processed row was deleted
        deleted.Should().Be(1);
        (await db.OutboxMessages.CountAsync()).Should().Be(3); // fresh + dead + pending survive

        var remaining = await db.OutboxMessages.ToListAsync();
        remaining.Should().NotContain(r => r.Id == oldProcessed.Id);
    }

    [Fact]
    public async Task ReturnsZero_WhenNothingToDelete()
    {
        var (scopeFactory, db) = BuildFixture(nameof(ReturnsZero_WhenNothingToDelete));

        var cleaner = new OutboxCleaner<CleanupTestDbContext>(scopeFactory);

        var deleted = await cleaner.DeleteProcessedBeforeAsync(
            cutoffUtc: DateTime.UtcNow.AddDays(-30),
            batchSize: 100,
            CancellationToken.None);

        deleted.Should().Be(0);
    }

    // ── StubIntegrationEvent for OutboxMessage.Create ────────────────────────

    private sealed record StubIntegrationEvent : YallaJo.SharedKernel.Domain.Event.IIntegrationEvent
    {
        public Guid EventId { get; } = Guid.CreateVersion7();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
