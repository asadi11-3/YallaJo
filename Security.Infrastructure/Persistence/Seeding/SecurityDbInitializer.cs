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
        [AppRoles.Owner] = Guid.Parse("a0000000-0000-0000-0000-000000000000"),
        [AppRoles.SuperAdmin] = Guid.Parse("a0000000-0000-0000-0000-000000000001"),
        [AppRoles.Admin] = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
        [AppRoles.TourGuide] = Guid.Parse("a2222222-2222-2222-2222-222222222222"),
        [AppRoles.User] = Guid.Parse("a4444444-4444-4444-4444-444444444444")
    };

    public int Order => 30;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var roles = CreateRoles();
        var users = CreateUsers();
        var userRoles = CreateUserRoles(users);
        var roleClaims = CreateRoleClaims();

        dbContext.Roles.AddRange(roles);
        dbContext.Users.AddRange(users);
        dbContext.UserRoles.AddRange(userRoles);
        dbContext.RoleClaims.AddRange(roleClaims);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<Role> CreateRoles()
    {
        var roleDefinitions = new[]
        {
            (Name: AppRoles.Owner, Description: "Platform owner"),
            (Name: AppRoles.SuperAdmin, Description: "Platform super administrator"),
            (Name: AppRoles.Admin, Description: "Platform administrator"),
            (Name: AppRoles.TourGuide, Description: "Tour guide"),
            (Name: AppRoles.User, Description: "Traveler customer")
        };

        return roleDefinitions.Select(role =>
        {
            var entity = Role.Create(role.Name, role.Description);
            SetProperty(entity, nameof(Role.Id), RoleIds[role.Name]);
            return entity;
        }).ToList();
    }

    private List<User> CreateUsers()
    {
        return SeedIdentityProfiles.All.Select(profile =>
        {
            var user = User.Register(profile.Email, profile.FirstName, profile.LastName);
            SetProperty(user, nameof(User.Id), profile.UserId);
            user.SetPasswordHash(passwordHasher.Hash(profile.Password));
            user.Activate();

            var primaryEmail = user.GetPrimaryEmail();
            primaryEmail?.MarkVerified();

            return user;
        }).ToList();
    }

    private static List<UserRole> CreateUserRoles(IEnumerable<User> users)
    {
        return users.Select(user =>
        {
            var profile = SeedIdentityProfiles.All.First(p => p.UserId == user.Id);
            return UserRole.Create(user.Id, RoleIds[profile.Role]);
        }).ToList();
    }

    private static List<RoleClaim> CreateRoleClaims()
    {
        var claimMap = new Dictionary<string, string[]>
        {
            [AppRoles.Owner] = ["*"],
            [AppRoles.SuperAdmin] = ["*"],
            [AppRoles.Admin] = ["users:manage", "roles:manage", "claims:manage"],
            [AppRoles.TourGuide] = ["tours:write", "bookings:read", "bookings:update"],
            [AppRoles.User] = ["bookings:create", "bookings:read", "reviews:write"]
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
