namespace YallaJo.SharedKernel.Infrastructure.Data
{
    public sealed record SeedUserProfile(
        Guid UserId,
        string Email,
        string FirstName,
        string LastName,
        string Role,
        string Password,
        string Status = "Active");

    public static class SeedIdentityProfiles
    {
        public static readonly IReadOnlyList<SeedUserProfile> All =
        [
            // ── Owner ─────────────────────────────────────────────────────────
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "owner@yallajo.local", "Ayman", "Haddad", "Owner", "P@ssw0rd!"),

            // ── SuperAdmin ────────────────────────────────────────────────────
            new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "superadmin.1@yallajo.local", "Lina",   "Nasser",   "SuperAdmin", "P@ssw0rd!"),
            new(Guid.Parse("23232323-2323-2323-2323-232323232323"), "superadmin.2@yallajo.local", "Kareem", "Hammouri", "SuperAdmin", "P@ssw0rd!"),
            new(Guid.Parse("24242424-2424-2424-2424-242424242424"), "superadmin.3@yallajo.local", "Dima",   "Barakat",  "SuperAdmin", "P@ssw0rd!"),
            new(Guid.Parse("25252525-2525-2525-2525-252525252525"), "superadmin.4@yallajo.local", "Samer",  "Najjar",   "SuperAdmin", "P@ssw0rd!"),

            // ── Admin ─────────────────────────────────────────────────────────
            new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "admin.1@yallajo.local", "Omar",   "Khalil",  "Admin", "P@ssw0rd!"),
            new(Guid.Parse("34343434-3434-3434-3434-343434343434"), "admin.2@yallajo.local", "Raghad", "Nimri",   "Admin", "P@ssw0rd!"),
            new(Guid.Parse("35353535-3535-3535-3535-353535353535"), "admin.3@yallajo.local", "Tareq",  "Malkawi", "Admin", "P@ssw0rd!"),
            new(Guid.Parse("36363636-3636-3636-3636-363636363636"), "admin.4@yallajo.local", "Yara",   "Obeidat", "Admin", "P@ssw0rd!"),

            // ── TourGuide ─────────────────────────────────────────────────────
            new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "guide.petra@yallajo.local", "Rana",   "Masri",   "TourGuide", "P@ssw0rd!"),
            new(Guid.Parse("55555555-5555-5555-5555-555555555555"), "guide.wadi@yallajo.local",  "Yousef", "Shamali", "TourGuide", "P@ssw0rd!"),
            new(Guid.Parse("56565656-5656-5656-5656-565656565656"), "guide.aqaba@yallajo.local", "Leen",   "Khalidi", "TourGuide", "P@ssw0rd!"),
            new(Guid.Parse("57575757-5757-5757-5757-575757575757"), "guide.amman@yallajo.local", "Basim",  "Ayyub",   "TourGuide", "P@ssw0rd!"),

            // ── Provider ──────────────────────────────────────────────────────
            new(Guid.Parse("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1"), "provider.1@yallajo.local", "Ahmad", "Farhat", "Provider", "P@ssw0rd!"),
            new(Guid.Parse("d2d2d2d2-d2d2-d2d2-d2d2-d2d2d2d2d2d2"), "provider.2@yallajo.local", "Hana",  "Assaf",  "Provider", "P@ssw0rd!"),
            new(Guid.Parse("d3d3d3d3-d3d3-d3d3-d3d3-d3d3d3d3d3d3"), "provider.3@yallajo.local", "Rami",  "Hamed",  "Provider", "P@ssw0rd!"),

            // ── User ──────────────────────────────────────────────────────────
            new(Guid.Parse("66666666-6666-6666-6666-666666666666"), "traveler.1@yallajo.local", "Noor", "Saad",    "User", "P@ssw0rd!"),
            new(Guid.Parse("77777777-7777-7777-7777-777777777777"), "traveler.2@yallajo.local", "Hadi", "Darwish", "User", "P@ssw0rd!"),
            new(Guid.Parse("88888888-8888-8888-8888-888888888888"), "traveler.3@yallajo.local", "Maya", "Khoury",  "User", "P@ssw0rd!"),
            new(Guid.Parse("99999999-9999-9999-9999-999999999999"), "traveler.4@yallajo.local", "Jad",  "Salem",   "User", "P@ssw0rd!"),
            new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "traveler.5@yallajo.local", "Sara", "Qattan",  "User", "P@ssw0rd!"),
            new(Guid.Parse("abababab-abab-abab-abab-abababababab"), "traveler.6@yallajo.local", "Fadi", "Issa",    "User", "P@ssw0rd!"),
            new(Guid.Parse("acacac0c-acac-acac-acac-0c0c0c0c0c0c"), "traveler.7@yallajo.local", "Dala", "Nassar",  "User", "P@ssw0rd!"),

            // ── Creator ───────────────────────────────────────────────────────
            new(Guid.Parse("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1"), "creator.1@yallajo.local", "Lara", "Nassar",  "Creator", "P@ssw0rd!"),
            new(Guid.Parse("e2e2e2e2-e2e2-e2e2-e2e2-e2e2e2e2e2e2"), "creator.2@yallajo.local", "Ziad", "Barakat", "Creator", "P@ssw0rd!"),
            new(Guid.Parse("e3e3e3e3-e3e3-e3e3-e3e3-e3e3e3e3e3e3"), "creator.3@yallajo.local", "Nadia","Shami",   "Creator", "P@ssw0rd!"),

            // ── Guest ─────────────────────────────────────────────────────────
            new(Guid.Parse("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1"), "guest.1@yallajo.local", "Ali",  "Hamdan", "Guest", "P@ssw0rd!"),
            new(Guid.Parse("f2f2f2f2-f2f2-f2f2-f2f2-f2f2f2f2f2f2"), "guest.2@yallajo.local", "Sana", "Tamimi", "Guest", "P@ssw0rd!"),

            // ---------------------------------------------------------------
            // Playwright test scenario users (see Agents/Tests/Playwright-*.md).
            // Password: TestPass!23 for all entries below.
            // Status: Active by default; "Suspended" / "Pending" supported.
            // ---------------------------------------------------------------
            new(Guid.Parse("b0000000-0000-0000-0000-000000000001"), "admin@yallajo.test",          "Test",      "Admin",   "Admin",     "TestPass!23"),
            new(Guid.Parse("b0000000-0000-0000-0000-000000000002"), "userA@yallajo.test",          "User",      "Alpha",   "User",      "TestPass!23"),
            new(Guid.Parse("b0000000-0000-0000-0000-000000000003"), "userB@yallajo.test",          "User",      "Beta",    "User",      "TestPass!23"),
            new(Guid.Parse("b0000000-0000-0000-0000-000000000004"), "guide-pending@yallajo.test",  "Guide",     "Pending", "TourGuide", "TestPass!23"),
            new(Guid.Parse("b0000000-0000-0000-0000-000000000005"), "guide-approved@yallajo.test", "Guide",     "Approved","TourGuide", "TestPass!23"),
            new(Guid.Parse("b0000000-0000-0000-0000-000000000006"), "business@yallajo.test",       "Business",  "Owner",   "TourGuide", "TestPass!23"),
            new(Guid.Parse("b0000000-0000-0000-0000-000000000007"), "agency@yallajo.test",         "Agency",    "Manager", "TourGuide", "TestPass!23"),
            new(Guid.Parse("b0000000-0000-0000-0000-000000000008"), "suspended@yallajo.test",      "Suspended", "User",    "User",      "TestPass!23", "Suspended")
        ];
    }
}
