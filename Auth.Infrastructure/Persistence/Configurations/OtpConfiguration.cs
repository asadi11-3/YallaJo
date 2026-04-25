using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

public sealed class OtpConfiguration : IEntityTypeConfiguration<Otp>
{
    public void Configure(EntityTypeBuilder<Otp> builder)
    {
        builder.ToTable("Otps", "auth");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.UserId).IsRequired();

        builder.Property(o => o.Purpose)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.CodeHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(o => o.DeliveryChannel)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.DeliveryAddress)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(o => o.ExpiresAt).IsRequired();

        builder.Property(o => o.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(o => o.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(o => o.UsedAt).IsRequired(false);

        // Auditable fields
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired(false);
        builder.Property(o => o.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(o => o.DeletedAt).IsRequired(false);
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasQueryFilter(o => !o.IsDeleted);

        // Composite covering index for the canonical 3-column OTP lookup pattern:
        //   WHERE UserId = @u AND Purpose = @p AND IsUsed = 0
        //   ORDER BY CreatedAt DESC
        // The partial filter on IsUsed = 0 shrinks the index to active OTPs only,
        // keeping it small as records are marked used and eventually cleaned up.
        //
        // UNIQUE filter: at most ONE active (unused, non-deleted) OTP can exist
        // per (UserId, Purpose). This eliminates concurrent-issuance races
        // (ResendOtp × 2 tabs, ForgotPassword × 2 tabs) that would otherwise
        // leave multiple valid reset/verification codes outstanding. Handlers
        // invalidate prior OTPs before inserting a new one; if a race slips
        // through, the DB rejects the second insert with a unique-constraint
        // violation (handled as a transient conflict by the caller).
        builder.HasIndex(o => new { o.UserId, o.Purpose })
            .IsUnique()
            .HasFilter("[IsUsed] = 0 AND [IsDeleted] = 0")
            .HasDatabaseName("IX_Otps_UserId_Purpose_Active_Unique");

        // Retained for the cleanup job's range scan on ExpiresAt
        builder.HasIndex(o => o.ExpiresAt)
            .HasDatabaseName("IX_Otps_ExpiresAt");
    }
}
