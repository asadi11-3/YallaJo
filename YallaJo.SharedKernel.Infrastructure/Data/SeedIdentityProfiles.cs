namespace YallaJo.SharedKernel.Infrastructure.Data
{
    public sealed record SeedUserProfile(
        Guid UserId,
        string Email,
        string FirstName,
        string LastName,
        string Role,
        string Password);

    public static class SeedIdentityProfiles
    {
        public static readonly IReadOnlyList<SeedUserProfile> All =
        [
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "admin@yallajo.local", "Ayman", "Haddad", "Admin", "P@ssw0rd!"),
            new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "guide.petra@yallajo.local", "Lina", "Nasser", "Guide", "P@ssw0rd!"),
            new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "guide.wadi@yallajo.local", "Omar", "Khalil", "Guide", "P@ssw0rd!"),
            new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "owner.amman@yallajo.local", "Rana", "Masri", "BusinessOwner", "P@ssw0rd!"),
            new(Guid.Parse("55555555-5555-5555-5555-555555555555"), "owner.aqaba@yallajo.local", "Yousef", "Shamali", "BusinessOwner", "P@ssw0rd!"),
            new(Guid.Parse("66666666-6666-6666-6666-666666666666"), "traveler.1@yallajo.local", "Noor", "Saad", "User", "P@ssw0rd!"),
            new(Guid.Parse("77777777-7777-7777-7777-777777777777"), "traveler.2@yallajo.local", "Hadi", "Darwish", "User", "P@ssw0rd!"),
            new(Guid.Parse("88888888-8888-8888-8888-888888888888"), "traveler.3@yallajo.local", "Maya", "Khoury", "User", "P@ssw0rd!"),
            new(Guid.Parse("99999999-9999-9999-9999-999999999999"), "traveler.4@yallajo.local", "Jad", "Salem", "User", "P@ssw0rd!"),
            new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "traveler.5@yallajo.local", "Sara", "Qattan", "User", "P@ssw0rd!")
        ];
    }
}
