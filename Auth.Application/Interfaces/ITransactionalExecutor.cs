namespace Auth.Application.Interfaces;

/// <summary>
/// Executes a block as a single retriable transactional unit spanning Auth and
/// any Security writes performed inside the delegate via
/// <see cref="Security.Contracts.Abstractions.ISecurityService"/>.
/// <para>
/// Why this lives in Auth.Application:
/// Auth is the lifecycle coordinator for flows that touch Security in-process
/// (<c>VerifyEmail</c>, <c>ResetPassword</c>). The executor hides the EF
/// execution-strategy + ambient-transaction plumbing behind an intent-revealing
/// abstraction so application handlers stay clean and framework-free.
/// </para>
/// <para>
/// Why NOT in SharedKernel/IUnitOfWork:
/// This is a cross-module orchestration concern, not a unit-of-work concern.
/// Folding it into a generic UoW would drag EF execution-strategy semantics
/// into every module that only needs a single-context SaveChanges.
/// </para>
/// <para>
/// Semantics:
/// - The delegate runs inside both a retriable execution strategy AND an
///   ambient <see cref="System.Transactions.TransactionScope"/>. Every
///   <c>SaveChangesAsync</c> call from any DbContext enlists in the scope.
/// - The delegate MUST be idempotent because the strategy may invoke it
///   multiple times on transient SQL failures.
/// - Returning success from the delegate commits the scope; throwing rolls
///   back both sides.
/// </para>
/// </summary>
public interface ITransactionalExecutor
{
    Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default);
}
