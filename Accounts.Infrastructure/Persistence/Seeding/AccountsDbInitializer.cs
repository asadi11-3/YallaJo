using Accounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Accounts.Infrastructure.Persistence.Seeding;

public sealed class AccountsDbInitializer(AccountsDbContext dbContext) : IModuleDbInitializer
{
    private static readonly (Guid UserId, string FirstName, string LastName)[] Profiles =
    [
        (Guid.Parse("11111111-1111-1111-1111-111111111111"), "Ayman", "Haddad"),
        (Guid.Parse("22222222-2222-2222-2222-222222222222"), "Lina", "Nasser"),
        (Guid.Parse("33333333-3333-3333-3333-333333333333"), "Omar", "Khalil"),
        (Guid.Parse("44444444-4444-4444-4444-444444444444"), "Rana", "Masri"),
        (Guid.Parse("55555555-5555-5555-5555-555555555555"), "Yousef", "Shamali"),
        (Guid.Parse("66666666-6666-6666-6666-666666666666"), "Noor", "Saad"),
        (Guid.Parse("77777777-7777-7777-7777-777777777777"), "Hadi", "Darwish"),
        (Guid.Parse("88888888-8888-8888-8888-888888888888"), "Maya", "Khoury"),
        (Guid.Parse("99999999-9999-9999-9999-999999999999"), "Jad", "Salem"),
        (Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Sara", "Qattan")
    ];

    public int Order => 50;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Profiles.AnyAsync(cancellationToken))
        {
            return;
        }

        var profiles = Profiles.Select(profile =>
        {
            var entity = Profile.Create(profile.UserId, profile.FirstName, profile.LastName);
            entity.SetDisplayName($"{profile.FirstName} {profile.LastName}");
            entity.SetAvatarUrl($"https://cdn.yallajo.local/avatars/{profile.UserId:N}.png");
            return entity;
        }).ToList();

        dbContext.Profiles.AddRange(profiles);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
