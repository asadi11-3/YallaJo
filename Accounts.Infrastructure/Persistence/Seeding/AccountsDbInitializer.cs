using Accounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Accounts.Infrastructure.Persistence.Seeding;

public sealed class AccountsDbInitializer(AccountsDbContext dbContext) : IModuleDbInitializer
{
    public int Order => 50;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Idempotent: derive profiles from the canonical SeedIdentityProfiles registry
        // and only insert rows for users that do not yet have a Profile.
        var existingProfileUserIds = await dbContext.Profiles
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);
        var existing = new HashSet<Guid>(existingProfileUserIds);

        var newProfiles = SeedIdentityProfiles.All
            .Where(p => !existing.Contains(p.UserId))
            .Select(p =>
            {
                var entity = Profile.Create(p.UserId, p.FirstName, p.LastName);
                entity.SetDisplayName($"{p.FirstName} {p.LastName}");
                entity.SetAvatarUrl($"https://cdn.yallajo.local/avatars/{p.UserId:N}.png");
                return entity;
            })
            .ToList();

        if (newProfiles.Count == 0)
        {
            return;
        }

        dbContext.Profiles.AddRange(newProfiles);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
