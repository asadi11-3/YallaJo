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
        if (await dbContext.Sessions.AnyAsync(cancellationToken))
        {
            return;
        }

        var devices = CreateDevices();
        var sessions = CreateSessions(devices);
        var refreshTokens = CreateRefreshTokens(sessions);
        var otps = CreateOtps();
        var providers = CreateExternalProviders();

        dbContext.Devices.AddRange(devices);
        dbContext.Sessions.AddRange(sessions);
        dbContext.RefreshTokens.AddRange(refreshTokens);
        dbContext.Otps.AddRange(otps);
        dbContext.ExternalProviders.AddRange(providers);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<Device> CreateDevices()
    {
        return SeedIdentityProfiles.All.Select((profile, index) =>
        {
            var device = Device.Create(
                profile.UserId,
                $"{profile.Role.ToLowerInvariant()}-device-{index + 1}",
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
                $"10.10.0.{index + 10}");

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
