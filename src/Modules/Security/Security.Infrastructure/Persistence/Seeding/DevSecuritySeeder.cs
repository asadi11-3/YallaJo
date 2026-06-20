using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Security.Application.Interfaces;
using Security.Domain.Entities;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Security.Infrastructure.Persistence.Seeding;

/// <summary>
/// DEV-SEED-B1 — Development / QA-only seeder that creates the <c>seed.*@yallajo.dev</c>
/// user accounts (see <see cref="DevSeedProfiles"/>).
/// <para>
/// Guarded by <see cref="IHostEnvironment.IsDevelopment"/> so it can NEVER run in Production.
/// Idempotent per-user (only creates users whose id is missing). Roles are NOT created here —
/// they are already seeded by <see cref="SecurityDbInitializer"/> (Order 30) and
/// <c>SecurityDataSeeder</c>; role ids are resolved from the existing <c>Roles</c> table.
/// </para>
/// </summary>
public sealed class DevSecuritySeeder(
    SecurityDbContext dbContext,
    IPasswordHasher passwordHasher,
    IHostEnvironment hostEnvironment,
    ILogger<DevSecuritySeeder> logger) : IModuleDbInitializer
{
    public int Order => 160;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        // Resolve role ids by NAME from the rows that actually exist (roles are seeded earlier).
        var roleIdByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in await dbContext.Roles
                     .Select(r => new { r.Id, r.Name })
                     .ToListAsync(cancellationToken))
        {
            roleIdByName[role.Name] = role.Id;
        }

        // Per-user idempotency: only create users whose id is not already present.
        var existingUserIds = (await dbContext.Users
                .IgnoreQueryFilters()
                .Select(u => u.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var pendingProfiles = DevSeedProfiles.All
            .Where(p => !existingUserIds.Contains(p.UserId))
            .ToList();

        if (pendingProfiles.Count == 0)
        {
            return;
        }

        var users = new List<User>();
        var userRoles = new List<UserRole>();

        foreach (var profile in pendingProfiles)
        {
            if (!roleIdByName.TryGetValue(profile.Role, out var roleId))
            {
                logger.LogWarning(
                    "Dev seed user '{Email}' references role '{Role}' which is not seeded; skipping.",
                    profile.Email, profile.Role);
                continue;
            }

            var user = User.Register(profile.Email, profile.FirstName, profile.LastName);
            SetProperty(user, nameof(User.Id), profile.UserId);
            user.SetPasswordHash(passwordHasher.Hash(profile.Password));
            ApplyLifecycle(user, profile.Status);

            users.Add(user);
            userRoles.Add(UserRole.Create(user.Id, roleId));
        }

        if (users.Count == 0)
        {
            return;
        }

        dbContext.Users.AddRange(users);
        dbContext.UserRoles.AddRange(userRoles);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("DEV-SEED-B1: seeded {Count} development user account(s).", users.Count);
    }

    private static void ApplyLifecycle(User user, string status)
    {
        switch (status?.Trim().ToLowerInvariant())
        {
            case "pending":
                user.MarkPendingActivation();
                break;

            case "suspended":
                user.Activate();
                user.GetPrimaryEmail()?.MarkVerified();
                user.Suspend();
                break;

            case "active":
            case null:
            case "":
            default:
                user.Activate();
                user.GetPrimaryEmail()?.MarkVerified();
                break;
        }
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (property is null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().FullName}.");
        }

        property.SetValue(target, value);
    }
}
