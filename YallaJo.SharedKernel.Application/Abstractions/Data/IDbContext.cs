namespace YallaJo.SharedKernel.Application.Abstractions.Data
{
    /// <summary>
    /// Persistence-agnostic database context abstraction for the Application layer.
    /// Keeps EF Core types out of Application — concrete DbContext implementations
    /// in Infrastructure satisfy this contract.
    /// </summary>
    public interface IDbContext
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
