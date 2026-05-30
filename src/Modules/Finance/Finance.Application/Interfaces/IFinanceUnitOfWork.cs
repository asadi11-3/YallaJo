namespace Finance.Application.Interfaces;

/// <summary>
/// Unit-of-work facade for the Finance bounded context.
/// Delegates to <see cref="YallaJo.SharedKernel.Infrastructure.Data.IUnitOfWork{TContext}"/>
/// so domain-event dispatch is guaranteed before every commit.
/// </summary>
public interface IFinanceUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
