using Auth.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Auth.Domain.Repositories;

public interface IDeviceRepository : IRepository<Device, Guid> { }
public interface ISessionRepository : IRepository<Session, Guid> { }
public interface IRefreshTokenRepository : IRepository<RefreshToken, Guid> { }
public interface IOtpRepository : IWriteRepository<Otp, Guid> ,IReadRepository<Otp, Guid> { }
public interface IExternalProviderRepository : IRepository<ExternalProvider, Guid> { }
