using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Auth.Infrastructure.Repositories;

public sealed class DeviceRepository(AuthDbContext context) : EfRepository<Device, Guid>(context), IDeviceRepository { }
public sealed class SessionRepository(AuthDbContext context) : EfRepository<Session, Guid>(context), ISessionRepository { }
public sealed class RefreshTokenRepository(AuthDbContext context) : EfRepository<RefreshToken, Guid>(context), IRefreshTokenRepository { }
public sealed class OtpRepository(AuthDbContext context) : EfEntityRepository<Otp, Guid>(context), IOtpRepository { }
public sealed class ExternalProviderRepository(AuthDbContext context) : EfRepository<ExternalProvider, Guid>(context), IExternalProviderRepository { }
