using System.Globalization;
using Finance.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Persistence;

/// <summary>
/// Generates the next invoice number for the current UTC month using an EF Core tracked
/// per-month counter with optimistic concurrency. Format: <c>INV-{yyyyMM}-{seq6}</c>.
/// </summary>
internal sealed class SqlInvoiceNumberGenerator(FinanceDbContext context, TimeProvider timeProvider) : IInvoiceNumberGenerator
{
    private const int MaxRetryCount = 8;

    public async Task<string> NextAsync(CancellationToken ct = default)
    {
        var yearMonth = timeProvider.GetUtcNow().UtcDateTime.ToString("yyyyMM", CultureInfo.InvariantCulture);
        var counters = context.Set<InvoiceNumberCounter>();

        for (var attempt = 1; attempt <= MaxRetryCount; attempt++)
        {
            InvoiceNumberCounter? counter = null;
            var insertedNewCounter = false;

            try
            {
                counter = await counters
                    .FirstOrDefaultAsync(c => c.YearMonth == yearMonth, ct)
                    .ConfigureAwait(false);

                int sequence;
                if (counter is null)
                {
                    counter = InvoiceNumberCounter.Create(yearMonth);
                    await counters.AddAsync(counter, ct).ConfigureAwait(false);
                    insertedNewCounter = true;
                    sequence = counter.Seq;
                }
                else
                {
                    sequence = counter.Increment();
                }

                await context.SaveChangesAsync(ct).ConfigureAwait(false);

                return string.Format(CultureInfo.InvariantCulture, "INV-{0}-{1:D6}", yearMonth, sequence);
            }
            catch (DbUpdateConcurrencyException)
            {
                Detach(counter);
            }
            catch (DbUpdateException) when (insertedNewCounter)
            {
                Detach(counter);
            }
        }

        throw new InvalidOperationException($"Failed to acquire next invoice sequence after {MaxRetryCount} retries");
    }

    private void Detach(InvoiceNumberCounter? counter)
    {
        if (counter is null)
        {
            return;
        }

        context.Entry(counter).State = EntityState.Detached;
    }
}
