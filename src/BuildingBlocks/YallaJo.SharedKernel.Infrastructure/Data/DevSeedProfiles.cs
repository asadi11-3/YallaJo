namespace YallaJo.SharedKernel.Infrastructure.Data
{
    /// <summary>
    /// The <b>Development / QA-only</b> user accounts created by <c>DevSecuritySeeder</c> (DEV-SEED-B1).
    /// <para>
    /// All accounts use the <c>seed.*@yallajo.dev</c> naming convention and the shared password
    /// <see cref="Password"/> (<c>DevSeed!23</c>). They are intentionally separate from the
    /// production-style (<c>@yallajo.local</c>) and Playwright (<c>@yallajo.test</c>) datasets so they
    /// can be added/removed without touching existing seeders.
    /// </para>
    /// <para>
    /// These accounts MUST only ever exist in Development — the seeders are guarded by
    /// <c>IHostEnvironment.IsDevelopment()</c>.
    /// </para>
    /// </summary>
    public static class DevSeedProfiles
    {
        /// <summary>Shared password for every dev/QA seed account. Development-only.</summary>
        public const string Password = "DevSeed!23";

        public static readonly SeedUserProfile Customer = new(
            DevSeedIds.CustomerUserId,
            "seed.customer@yallajo.dev",
            "Dev",
            "Customer",
            "User",
            Password);

        public static readonly SeedUserProfile Provider = new(
            DevSeedIds.ProviderUserId,
            "seed.provider@yallajo.dev",
            "Dev",
            "Provider",
            "Provider",
            Password);

        public static readonly SeedUserProfile Guide = new(
            DevSeedIds.GuideUserId,
            "seed.guide@yallajo.dev",
            "Dev",
            "Guide",
            "TourGuide",
            Password);

        public static readonly SeedUserProfile Admin = new(
            DevSeedIds.AdminUserId,
            "seed.admin@yallajo.dev",
            "Dev",
            "Admin",
            "Admin",
            Password);

        public static readonly SeedUserProfile Suspended = new(
            DevSeedIds.SuspendedUserId,
            "seed.suspended@yallajo.dev",
            "Dev",
            "Suspended",
            "User",
            Password,
            "Suspended");

        /// <summary>All dev/QA seed accounts, in seeding order.</summary>
        public static readonly IReadOnlyList<SeedUserProfile> All =
        [
            Customer,
            Provider,
            Guide,
            Admin,
            Suspended
        ];
    }
}
