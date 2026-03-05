// Auth.Infrastructure/Repositories/AuthRepositories.cs
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Auth.Infrastructure.Repositories;

public sealed class DeviceRepository(AuthDbContext context) : EfRepository<Device, Guid>(context), IDeviceRepository { }

public sealed class SessionRepository(AuthDbContext context)
    : EfRepository<Session, Guid>(context), ISessionRepository
{
    public Task<List<Session>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return context.Set<Session>()
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now)
            .OrderByDescending(s => s.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}

public sealed class RefreshTokenRepository(AuthDbContext context) : EfRepository<RefreshToken, Guid>(context), IRefreshTokenRepository { }
public sealed class OtpRepository(AuthDbContext context) : EfEntityRepository<Otp, Guid>(context), IOtpRepository { }
public sealed class ExternalProviderRepository(AuthDbContext context) : EfRepository<ExternalProvider, Guid>(context), IExternalProviderRepository { }
