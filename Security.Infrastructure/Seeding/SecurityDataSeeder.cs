
using Security.Contracts.Authorization;
using Security.Domain.Entities;
using Security.Domain.Repositories;

namespace Security.Infrastructure.Seeding;

public sealed class SecurityDataSeeder(
    IRoleRepository roleRepository,
    IRoleClaimRepository roleClaimRepository,
    ISecurityUnitOfWork unitOfWork)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedRolesAsync(ct);
        await SeedRoleClaimsAsync(ct);
    }

   

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        var changed = false;

        foreach (var roleName in AppRoles.AllRoles)
        {
            if (!await roleRepository.ExistsByNameAsync(roleName, ct))
            {
                await roleRepository.AddAsync(Role.Create(roleName), ct);
                changed = true;
            }
        }

        if (changed)
            await unitOfWork.SaveChangesAsync(ct);
    }

   

    private async Task SeedRoleClaimsAsync(CancellationToken ct)
    {
        var roles = await roleRepository.GetAllActiveAsync(ct);
        var changed = false;

        foreach (var role in roles)
        {
            var permissions = AppPermissions.GetPermissionsForRole(role.Name);

            foreach (var permission in permissions)
            {
                if (!await roleClaimRepository.ExistsAsync(role.Id, "Permission", permission, ct))
                {
                    await roleClaimRepository.AddAsync(
                        RoleClaim.Create(role.Id, "Permission", permission), ct);

                    changed = true;
                }
            }
        }

        if (changed)
            await unitOfWork.SaveChangesAsync(ct);
    }
}
