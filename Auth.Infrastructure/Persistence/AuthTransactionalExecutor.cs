using System.Transactions;
using Auth.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

/// <summary>
/// EF-aware implementation of <see cref="ITransactionalExecutor"/>.
/// <para>
/// When <c>EnableRetryOnFailure</c> is configured on the EF provider (all
/// modules in this codebase do this via <see cref="Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal.SqlServerOptionsExtension"/>),
/// EF refuses any user-initiated transaction unless the work is executed by an
/// <see cref="Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy"/>:
/// </para>
/// <code>
/// System.InvalidOperationException: The configured execution strategy
/// 'SqlServerRetryingExecutionStrategy' does not support user-initiated
/// transactions. Use the execution strategy returned by
/// 'DbContext.Database.CreateExecutionStrategy()' to execute all the
/// operations in the transaction as a retriable unit.
/// </code>
/// <para>
/// This executor wraps the caller's delegate with the strategy and then opens
/// an ambient <see cref="TransactionScope"/> inside it so every DbContext that
/// runs inside the block (Auth, Security, …) enlists in the same transaction.
/// Any transient SQL failure re-runs the entire block — which is exactly what
/// retry semantics demand.
/// </para>
/// <para>
/// We use <see cref="AuthDbContext"/>'s strategy because Auth owns the
/// lifecycle orchestration (<c>VerifyEmail</c>, <c>ResetPassword</c>) — the
/// strategy configuration is identical across modules, but anchoring on Auth
/// keeps ownership unambiguous.
/// </para>
/// </summary>
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
