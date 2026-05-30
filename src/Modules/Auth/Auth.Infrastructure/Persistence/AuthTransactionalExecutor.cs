using System.Transactions;
using Auth.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

internal sealed class AuthTransactionalExecutor(AuthDbContext dbContext) : ITransactionalExecutor
{
    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(
            state: operation,
            operation: async (_, op, innerCt) =>
            {
                using var scope = new TransactionScope(
                    TransactionScopeOption.Required,
                    new TransactionOptions
                    {
                        IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
                    },
                    TransactionScopeAsyncFlowOption.Enabled);

                var result = await op(innerCt).ConfigureAwait(false);

                scope.Complete();
                return result;
            },
            verifySucceeded: null,
            cancellationToken: ct).ConfigureAwait(false);
    }
}
