namespace Auth.Application.Interfaces;

public interface ITransactionalExecutor
{
    Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default);
}
