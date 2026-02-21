using Auth.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Auth.Domain.Repositories;

public interface IUserCredentialsRepository : IRepository<UserCredentials, Guid>
{
    /// <summary>Finds credentials by email address for login flows.</summary>
    Task<UserCredentials?> FindByEmailAsync(string email, CancellationToken ct = default);
}
