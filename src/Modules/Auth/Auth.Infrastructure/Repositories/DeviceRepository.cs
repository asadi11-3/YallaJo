using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Auth.Infrastructure.Repositories;

public sealed class DeviceRepository(AuthDbContext context) : EfRepository<Device, Guid>(context), IDeviceRepository
{
}
