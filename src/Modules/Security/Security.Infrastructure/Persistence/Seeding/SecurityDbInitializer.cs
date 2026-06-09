using Microsoft.EntityFrameworkCore;
using Security.Application.Interfaces;
using Security.Contracts.Authorization;
using Security.Domain.Entities;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Security.Infrastructure.Persistence.Seeding;

public sealed class SecurityDbInitializer(
    SecurityDbContext dbContext,
    IPasswordHasher passwordHasher) : IModuleDbInitializer
{
    private static readonly Dictionary<string, Guid> RoleIds = new(StringComparer.OrdinalIgnoreCase)
    {
        [AppRoles.Owner]      = Guid.Parse("a0000000-0000-0000-0000-000000000000"),
        [AppRoles.SuperAdmin] = Guid.Parse("a0000000-0000-0000-0000-000000000001"),
        [AppRoles.Admin]      = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
        [AppRoles.TourGuide]  = Guid.Parse("a2222222-2222-2222-2222-222222222222"),
        [AppRoles.Provider]   = Guid.Parse("a3333333-3333-3333-3333-333333333333"),
        [AppRoles.User]       = Guid.Parse("a4444444-4444-4444-4444-444444444444"),
        [AppRoles.Creator]    = Guid.Parse("a5555555-5555-5555-5555-555555555555"),
        [AppRoles.Guest]      = Guid.Parse("a6666666-6666-6666-6666-666666666666"),
    };

    public int Order => 30;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Roles + claims: idempotent per-row. Insert only what's missing so newly
        // added roles (e.g. Provider/Creator/Guest) seed into an already-seeded DB
        // without skipping them just because *some* roles already exist.
        var existingRoleIds = (await dbContext.Roles
                .Select(r => r.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var newRoles = CreateRoles()
            .Where(r => !existingRoleIds.Contains(r.Id))
            .ToList();

        if (newRoles.Count > 0)
        {
            dbContext.Roles.AddRange(newRoles);
        }

        // Role claims: idempotent per (RoleId, ClaimValue) pair.
        var existingClaimKeys = (await dbContext.RoleClaims
                .Select(c => new { c.RoleId, c.ClaimValue })
                .ToListAsync(cancellationToken))
            .Select(c => (c.RoleId, c.ClaimValue))
            .ToHashSet();

        var newClaims = CreateRoleClaims()
            .Where(c => !existingClaimKeys.Contains((c.RoleId, c.ClaimValue)))
            .ToList();

        if (newClaims.Count > 0)
        {
            dbContext.RoleClaims.AddRange(newClaims);
        }

        // Users: idempotent per-user - allows appending new test users to an
        // already-seeded database without breaking the existing rows.
        var existingUserIds = await dbContext.Users
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        var existingUserIdSet = new HashSet<Guid>(existingUserIds);

        var newProfiles = SeedIdentityProfiles.All
            .Where(p => !existingUserIdSet.Contains(p.UserId))
            .ToList();

        if (newProfiles.Count > 0)
        {
            var users = CreateUsers(newProfiles);
            var userRoles = CreateUserRoles(users, newProfiles);

            dbContext.Users.AddRange(users);
            dbContext.UserRoles.AddRange(userRoles);
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static List<Role> CreateRoles()
    {
        var roleDefinitions = new[]
        {
            (Name: AppRoles.Owner,      Description: "Platform owner"),
            (Name: AppRoles.SuperAdmin, Description: "Platform super administrator"),
            (Name: AppRoles.Admin,      Description: "Platform administrator"),
            (Name: AppRoles.TourGuide,  Description: "Tour guide"),
            (Name: AppRoles.Provider,   Description: "Business or accommodation provider"),
            (Name: AppRoles.User,       Description: "Traveler customer"),
            (Name: AppRoles.Creator,    Description: "Blog content creator"),
            (Name: AppRoles.Guest,      Description: "Unverified registered user"),
        };

        return roleDefinitions.Select(role =>
        {
            var entity = Role.Create(role.Name, role.Description);
            SetProperty(entity, nameof(Role.Id), RoleIds[role.Name]);
            return entity;
        }).ToList();
    }

    private List<User> CreateUsers(IReadOnlyList<SeedUserProfile> profiles)
    {
        return profiles.Select(profile =>
        {
            var user = User.Register(profile.Email, profile.FirstName, profile.LastName);
            SetProperty(user, nameof(User.Id), profile.UserId);
            user.SetPasswordHash(passwordHasher.Hash(profile.Password));

            ApplyLifecycle(user, profile.Status);

            return user;
        }).ToList();
    }

    private static void ApplyLifecycle(User user, string status)
    {
        switch (status?.Trim().ToLowerInvariant())
        {
            case "pending":
                // Provisioned -> PendingActivation. Email stays unverified.
                user.MarkPendingActivation();
                break;

            case "suspended":
                // Provisioned -> Active (verified) -> Suspended.
                user.Activate();
                user.GetPrimaryEmail()?.MarkVerified();
                user.Suspend();
                break;

            case "active":
            case null:
            case "":
            default:
                // Provisioned -> Active + verified email.
                user.Activate();
                user.GetPrimaryEmail()?.MarkVerified();
                break;
        }
    }

    private static List<UserRole> CreateUserRoles(
        IEnumerable<User> users,
        IReadOnlyList<SeedUserProfile> profiles)
    {
        var byId = profiles.ToDictionary(p => p.UserId);
        return users.Select(user =>
        {
            var profile = byId[user.Id];
            if (!RoleIds.TryGetValue(profile.Role, out var roleId))
            {
                throw new InvalidOperationException(
                    $"Seed user '{profile.Email}' references role '{profile.Role}' which is not seeded. " +
                    "Add it to SecurityDbInitializer.RoleIds and CreateRoles().");
            }
            return UserRole.Create(user.Id, roleId);
        }).ToList();
    }

    private static List<RoleClaim> CreateRoleClaims()
    {
        var claimMap = new Dictionary<string, string[]>
        {
            [AppRoles.Owner]      = ["*"],
            [AppRoles.SuperAdmin] = ["*"],
            [AppRoles.Admin]      = ["users:manage", "roles:manage", "claims:manage"],
            [AppRoles.TourGuide]  = ["tours:write", "bookings:read", "bookings:update"],
            [AppRoles.Provider]   = ["listings:write", "listings:read", "bookings:read", "reviews:read"],
            [AppRoles.User]       = ["bookings:create", "bookings:read", "reviews:write"],
            [AppRoles.Creator]    = ["blog:write", "blog:read"],
            [AppRoles.Guest]      = ["profile:read"],
        };

        var claims = new List<RoleClaim>();
        foreach (var (roleName, values) in claimMap)
        {
            var roleId = RoleIds[roleName];
            claims.AddRange(values.Select(value => RoleClaim.Create(roleId, "permission", value)));
        }

        return claims;
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
