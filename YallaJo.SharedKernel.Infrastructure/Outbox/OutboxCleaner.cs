using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

namespace YallaJo.SharedKernel.Infrastructure.Outbox;

/// <summary>
/// EF Core implementation of <see cref="IOutboxCleaner"/> typed to a specific module DbContext.
///
/// Deletes successfully processed outbox rows in batches.
/// Uses <c>ExecuteDeleteAsync</c> (bulk SQL DELETE) on providers that support it (SQL Server).
/// Falls back to load-and-remove for providers that don't (e.g. InMemory in tests).
///
/// Dead-lettered rows (RetryCount &gt;= <see cref="OutboxProcessor{TContext}.MaxRetryCount"/>)
/// are NEVER deleted — they require manual investigation and optional replay.
/// </summary>
public sealed class OutboxCleaner<TContext>(IServiceScopeFactory scopeFactory) : IOutboxCleaner
    where TContext : DbContext
{
    public string ModuleName => typeof(TContext).Name;

    public async Task<int> DeleteProcessedBeforeAsync(
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken ct)
    {
        int totalDeleted = 0;
        int deletedInBatch;

        do
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TContext>();

            deletedInBatch = await DeleteBatchAsync(db, cutoffUtc, batchSize, ct);
            totalDeleted += deletedInBatch;

        } while (deletedInBatch == batchSize && !ct.IsCancellationRequested);

        return totalDeleted;
    }

    public async Task<int> CountDeadLetteredAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        return await db.Set<OutboxMessage>()
            .CountAsync(m => m.RetryCount >= OutboxProcessor<TContext>.MaxRetryCount, ct);
    }

    public async Task<IReadOnlyList<OutboxDeadLetterDto>> ListDeadLetteredAsync(
        int limit,
        CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        var rows = await db.Set<OutboxMessage>()
            .Where(m => m.RetryCount >= OutboxProcessor<TContext>.MaxRetryCount)
            .OrderByDescending(m => m.OccurredOnUtc)
            .Take(limit)
            .ToListAsync(ct);

        return rows
            .Select(m => new OutboxDeadLetterDto(
                m.Id, ModuleName, m.Type, m.OccurredOnUtc, m.RetryCount, m.Error))
            .ToList();
    }

    public async Task<bool> ReplayDeadLetterAsync(Guid messageId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        var original = await db.Set<OutboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId
                && m.RetryCount >= OutboxProcessor<TContext>.MaxRetryCount, ct);

        if (original is null) return false;

        var clone = original.CreateReplayCopy();
        db.Set<OutboxMessage>().Add(clone);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static async Task<int> DeleteBatchAsync(
        TContext db,
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken ct)
    {
        var query = db.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc != null
                     && m.ProcessedOnUtc < cutoffUtc
                     && m.RetryCount < OutboxProcessor<TContext>.MaxRetryCount)
            .Take(batchSize);

        // Use ExecuteDeleteAsync (bulk SQL DELETE) for providers that support it.
        // InMemory and SQLite (CI) do not — detect by provider name and fall back.
        var providerName = db.Database.ProviderName ?? string.Empty;
        var supportsExecuteDelete =
            providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) ||
            providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ||
            providerName.Contains("Pomelo", StringComparison.OrdinalIgnoreCase);

        if (supportsExecuteDelete)
        {
            return await query.ExecuteDeleteAsync(ct);
        }

        // Fallback: load-and-remove (InMemory provider used in unit tests)
        var rows = await query.ToListAsync(ct);
        if (rows.Count == 0) return 0;

        db.Set<OutboxMessage>().RemoveRange(rows);
        await db.SaveChangesAsync(ct);
        return rows.Count;
    }
}
