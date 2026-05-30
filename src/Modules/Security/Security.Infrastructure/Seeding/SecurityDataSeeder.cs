using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;

namespace Security.Infrastructure.Seeding;

public sealed class SecurityDataSeeder(
    IRoleRepository roleRepository,
    IRoleClaimRepository roleClaimRepository,
    ISecurityUnitOfWork unitOfWork,
    RolePermissionMapping rolePermissionMapping,
    ILogger<SecurityDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedRolesAsync(ct);
        await SeedRoleClaimsAsync(ct);

        // P1 (2026-05-30): AUDIT-ONLY reconciliation report. This wave does NOT
        // delete stale RoleClaims. It only logs drift between the authoritative
        // RolePermissionMapping and the persisted RoleClaims so operators can plan
        // a future cleanup. No DB rows are removed here.
        await AuditRoleClaimsAsync(ct);
    }

    /// <summary>
    /// AUDIT-ONLY: reports (logs) RoleClaims that are STALE (present in the DB but no
    /// longer in the authoritative mapping) and MISSING (in the mapping but not yet
    /// persisted). Performs NO deletions and NO inserts — purely diagnostic.
    /// </summary>
    public async Task AuditRoleClaimsAsync(CancellationToken ct = default)
    {
        var roles = await roleRepository.GetAllAsync(
            filter: r => r.IsActive,
            asNoTracking: true,
            ct: ct);

        var totalStale = 0;
        var totalMissing = 0;

        foreach (var role in roles)
        {
            var authoritative = rolePermissionMapping.GetPermissionsForRole(role.Name)
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);

            var persisted = (await roleClaimRepository.GetAllAsync(
                    filter: rc => rc.RoleId == role.Id && rc.ClaimType == "Permission",
                    asNoTracking: true,
                    ct: ct))
                .Select(rc => rc.ClaimValue)
                .ToHashSet(StringComparer.Ordinal);

            var stale = persisted.Where(p => !authoritative.Contains(p))
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();
            var missing = authoritative.Where(p => !persisted.Contains(p))
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();

            totalStale += stale.Count;
            totalMissing += missing.Count;

            if (stale.Count > 0)
            {
                logger.LogWarning(
                    "RoleClaim AUDIT — role '{Role}' has {Count} STALE permission claim(s) " +
                    "(present in DB, not in authoritative mapping; NOT deleted): {Claims}",
                    role.Name, stale.Count, string.Join(", ", stale));
            }

            if (missing.Count > 0)
            {
                logger.LogWarning(
                    "RoleClaim AUDIT — role '{Role}' is MISSING {Count} permission claim(s) " +
                    "(in mapping, not yet persisted): {Claims}",
                    role.Name, missing.Count, string.Join(", ", missing));
            }
        }

        logger.LogInformation(
            "RoleClaim AUDIT complete — {Stale} stale and {Missing} missing claim(s) across {Roles} role(s). " +
            "No claims were deleted (audit-only wave).",
            totalStale, totalMissing, roles.Count);
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        var changed = false;

        foreach (var roleName in AppRoles.AllRoles)
        {
            if (!await roleRepository.AnyAsync(r => r.Name == roleName, ct))
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
        var roles = await roleRepository.GetAllAsync(
            filter: r => r.IsActive,
            asNoTracking: true,
            ct: ct);

        var changed = false;

        foreach (var role in roles)
        {
            // Dedup defensively: multiple IPermissionCatalog implementations may register
            // the same permission name (e.g. across overlapping bounded contexts), which
            // would otherwise produce duplicate RoleClaim inserts within a single batch
            // and trigger the IX_RoleClaims_RoleId_ClaimType_ClaimValue_Unique violation.
            var permissions = rolePermissionMapping.GetPermissionsForRole(role.Name)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            foreach (var permission in permissions)
            {
                var alreadyExists = await roleClaimRepository.AnyAsync(
                    rc => rc.RoleId == role.Id
                       && rc.ClaimType == "Permission"
                       && rc.ClaimValue == permission,
                    ct);

                if (!alreadyExists)
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
