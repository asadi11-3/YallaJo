namespace YallaJo.SharedKernel.Infrastructure.Data
{
    /// <summary>
    /// Deterministic identifiers for the <b>Development / QA-only</b> seed dataset (DEV-SEED-B1).
    /// <para>
    /// Every id in this registry uses the reserved <c>5eed0000-...</c> GUID block so that
    /// dev/QA records are visually distinct from production-style seed data
    /// (places <c>bbbbbbbb-</c>, tours <c>ffffffff-</c>, bookings <c>abababab-</c>, etc.).
    /// </para>
    /// <para>
    /// These records are created exclusively by the <c>DevXxxSeeder</c> initializers, which are
    /// guarded by <c>IHostEnvironment.IsDevelopment()</c> and must NEVER run in Production.
    /// </para>
    /// </summary>
    public static class DevSeedIds
    {
        // ── User accounts (5eed0000-0000-0000-0000-0001xxxxxxxx) ───────────────
        public static readonly Guid CustomerUserId = Guid.Parse("5eed0000-0000-0000-0000-000100000001");
        public static readonly Guid ProviderUserId = Guid.Parse("5eed0000-0000-0000-0000-000100000002");
        public static readonly Guid GuideUserId    = Guid.Parse("5eed0000-0000-0000-0000-000100000003");
        public static readonly Guid AdminUserId    = Guid.Parse("5eed0000-0000-0000-0000-000100000004");
        public static readonly Guid SuspendedUserId = Guid.Parse("5eed0000-0000-0000-0000-000100000005");

        // ── Provider application (5eed0000-...-0002xxxxxxxx) ───────────────────
        public static readonly Guid ProviderApplicationId = Guid.Parse("5eed0000-0000-0000-0000-000200000001");

        // ── Places (5eed0000-...-0003xxxxxxxx) ─────────────────────────────────
        public static readonly Guid PlaceId = Guid.Parse("5eed0000-0000-0000-0000-000300000001");

        // ── Tours (5eed0000-...-0004xxxxxxxx) ──────────────────────────────────
        /// <summary>Tour that WILL receive a primary image (image-present QA case).</summary>
        public static readonly Guid TourWithImageId = Guid.Parse("5eed0000-0000-0000-0000-000400000001");

        /// <summary>Tour that is intentionally left WITHOUT an image (placeholder-fallback QA case).</summary>
        public static readonly Guid TourWithoutImageId = Guid.Parse("5eed0000-0000-0000-0000-000400000002");

        // ── Tour guide aggregate (ContentTours.TourGuide) ─────────────────────
        public static readonly Guid TourGuideId = Guid.Parse("5eed0000-0000-0000-0000-000500000001");

        // ── Reviews (5eed0000-...-0006xxxxxxxx) ────────────────────────────────
        /// <summary>Published review that HAS images.</summary>
        public static readonly Guid ReviewWithImagesId = Guid.Parse("5eed0000-0000-0000-0000-000600000001");

        /// <summary>Published review WITHOUT images.</summary>
        public static readonly Guid ReviewWithoutImagesId = Guid.Parse("5eed0000-0000-0000-0000-000600000002");

        /// <summary>Hidden (non-published) review WITH images — verifies images never leak publicly.</summary>
        public static readonly Guid HiddenReviewWithImagesId = Guid.Parse("5eed0000-0000-0000-0000-000600000003");

        // ── Booking aggregates (5eed0000-...-0007xxxxxxxx) ─────────────────────
        public static readonly Guid BookingTourGuideId = Guid.Parse("5eed0000-0000-0000-0000-000700000001");
        public static readonly Guid AvailabilitySlotId = Guid.Parse("5eed0000-0000-0000-0000-000700000002");

        public static readonly Guid BookingAwaitingPaymentId = Guid.Parse("5eed0000-0000-0000-0000-000700000010");
        public static readonly Guid BookingConfirmedId       = Guid.Parse("5eed0000-0000-0000-0000-000700000011");
        public static readonly Guid BookingCancelledId       = Guid.Parse("5eed0000-0000-0000-0000-000700000012");

        // ── Provider id used as the logical ProviderId on bookings/tours ──────
        /// <summary>Logical provider id used on dev bookings (the dev provider's user id).</summary>
        public static readonly Guid ProviderId = ProviderUserId;
    }
}
