using System.Security.Cryptography;
using System.Text;
using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Auth.Infrastructure.Persistence.Seeding;

public sealed class AuthDbInitializer(AuthDbContext dbContext) : IModuleDbInitializer
{
    public int Order => 40;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Devices / Sessions / RefreshTokens: idempotent per-user.
        // Only seed for users that do not yet own any device.
        var existingDeviceUserIds = await dbContext.Devices
            .Select(d => d.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var existing = new HashSet<Guid>(existingDeviceUserIds);

        var newProfiles = SeedIdentityProfiles.All
            .Where(p => !existing.Contains(p.UserId))
            .ToList();

        var hasChanges = false;

        if (newProfiles.Count > 0)
        {
            var devices = CreateDevices(newProfiles);
            var sessions = CreateSessions(devices);
            var refreshTokens = CreateRefreshTokens(sessions);

            dbContext.Devices.AddRange(devices);
            dbContext.Sessions.AddRange(sessions);
            dbContext.RefreshTokens.AddRange(refreshTokens);
            hasChanges = true;
        }

        // Otps + ExternalProviders: original one-time bootstrap behaviour preserved.
        // Only seeded when their tables are empty (first run).
        if (!await dbContext.Otps.AnyAsync(cancellationToken))
        {
            dbContext.Otps.AddRange(CreateOtps());
            hasChanges = true;
        }

        if (!await dbContext.ExternalProviders.AnyAsync(cancellationToken))
        {
            dbContext.ExternalProviders.AddRange(CreateExternalProviders());
            hasChanges = true;
        }

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static List<Device> CreateDevices(IReadOnlyList<SeedUserProfile> profiles)
    {
        return profiles.Select((profile, index) =>
        {
            var device = Device.Create(
                profile.UserId,
                $"{profile.Role.ToLowerInvariant()}-device-{profile.UserId:N}",
                userAgent: "YallaJoSeedBot/1.0",
                deviceName: $"{profile.FirstName}'s Device");

            if (index % 2 == 0)
            {
                device.Trust();
            }

            return device;
        }).ToList();
    }

    private static List<Session> CreateSessions(IReadOnlyList<Device> devices)
    {
        return devices.Select((device, index) =>
        {
            var session = Session.Create(
                device.UserId,
                device.Id,
                DateTime.UtcNow.AddDays(30),
                $"10.10.0.{(index % 240) + 10}");

            if (index % 5 == 0)
            {
                session.Revoke();
            }

            return session;
        }).ToList();
    }

    private static List<RefreshToken> CreateRefreshTokens(IReadOnlyList<Session> sessions)
    {
        return sessions.Select(session =>
        {
            var rawToken = $"{session.UserId:N}-{session.Id:N}";
            return RefreshToken.Create(
                session.UserId,
                session.Id,
                ComputeSha256(rawToken),
                DateTime.UtcNow.AddDays(45));
        }).ToList();
    }

    private static List<Otp> CreateOtps()
    {
        return SeedIdentityProfiles.All.Take(4).Select(profile =>
            Otp.Create(
                profile.UserId,
                purpose: "email-verification",
                codeHash: ComputeSha256($"otp-{profile.UserId:N}"),
                deliveryChannel: "email",
                deliveryAddress: profile.Email,
                expiryMinutes: 15)).ToList();
    }

    private static List<ExternalProvider> CreateExternalProviders()
    {
        return SeedIdentityProfiles.All.Take(3).Select(profile =>
            ExternalProvider.Create(
                profile.UserId,
                provider: "google",
                providerUserId: $"google-{profile.UserId:N}",
                providerEmail: profile.Email)).ToList();
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
