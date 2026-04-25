using Auth.Application.Interfaces;

namespace Auth.Tests.Unit.TestDoubles;

/// <summary>
/// Test double for <see cref="ITransactionalExecutor"/> that runs the delegate
/// exactly once, inline, with no transaction enlistment. Handlers can be tested
/// in isolation without pulling in EF / SqlServer / System.Transactions.
/// </summary>
internal sealed class PassThroughTransactionalExecutor : ITransactionalExecutor
{
    public int InvocationCount { get; private set; }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
    {
        InvocationCount++;
        return await operation(ct).ConfigureAwait(false);
    }
}
