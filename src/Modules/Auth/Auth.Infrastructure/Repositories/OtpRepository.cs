using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Auth.Infrastructure.Repositories;

public sealed class OtpRepository(AuthDbContext context) : EfEntityRepository<Otp, Guid>(context), IOtpRepository
{
}
