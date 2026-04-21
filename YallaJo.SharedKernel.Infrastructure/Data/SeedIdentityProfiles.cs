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
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "owner@yallajo.local", "Ayman", "Haddad", "Owner", "P@ssw0rd!"),
            new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "superadmin.1@yallajo.local", "Lina", "Nasser", "SuperAdmin", "P@ssw0rd!"),
            new(Guid.Parse("23232323-2323-2323-2323-232323232323"), "superadmin.2@yallajo.local", "Kareem", "Hammouri", "SuperAdmin", "P@ssw0rd!"),
            new(Guid.Parse("24242424-2424-2424-2424-242424242424"), "superadmin.3@yallajo.local", "Dima", "Barakat", "SuperAdmin", "P@ssw0rd!"),
            new(Guid.Parse("25252525-2525-2525-2525-252525252525"), "superadmin.4@yallajo.local", "Samer", "Najjar", "SuperAdmin", "P@ssw0rd!"),
            new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "admin.1@yallajo.local", "Omar", "Khalil", "Admin", "P@ssw0rd!"),
            new(Guid.Parse("34343434-3434-3434-3434-343434343434"), "admin.2@yallajo.local", "Raghad", "Nimri", "Admin", "P@ssw0rd!"),
            new(Guid.Parse("35353535-3535-3535-3535-353535353535"), "admin.3@yallajo.local", "Tareq", "Malkawi", "Admin", "P@ssw0rd!"),
            new(Guid.Parse("36363636-3636-3636-3636-363636363636"), "admin.4@yallajo.local", "Yara", "Obeidat", "Admin", "P@ssw0rd!"),
            new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "guide.petra@yallajo.local", "Rana", "Masri", "TourGuide", "P@ssw0rd!"),
            new(Guid.Parse("55555555-5555-5555-5555-555555555555"), "guide.wadi@yallajo.local", "Yousef", "Shamali", "TourGuide", "P@ssw0rd!"),
            new(Guid.Parse("66666666-6666-6666-6666-666666666666"), "traveler.1@yallajo.local", "Noor", "Saad", "User", "P@ssw0rd!"),
            new(Guid.Parse("77777777-7777-7777-7777-777777777777"), "traveler.2@yallajo.local", "Hadi", "Darwish", "User", "P@ssw0rd!"),
            new(Guid.Parse("88888888-8888-8888-8888-888888888888"), "traveler.3@yallajo.local", "Maya", "Khoury", "User", "P@ssw0rd!"),
            new(Guid.Parse("99999999-9999-9999-9999-999999999999"), "traveler.4@yallajo.local", "Jad", "Salem", "User", "P@ssw0rd!"),
            new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "traveler.5@yallajo.local", "Sara", "Qattan", "User", "P@ssw0rd!")
        ];
    }
}
