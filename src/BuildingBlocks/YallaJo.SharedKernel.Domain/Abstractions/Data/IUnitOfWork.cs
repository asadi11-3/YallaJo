namespace YallaJo.SharedKernel.Domain.Abstractions.Data
{
    /// <summary>
    /// Plain unit-of-work abstraction. No EF Core / DbContext dependency.
    /// The EF-specific IUnitOfWork&lt;TContext&gt; lives in SharedKernel.Infrastructure.
    /// </summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
