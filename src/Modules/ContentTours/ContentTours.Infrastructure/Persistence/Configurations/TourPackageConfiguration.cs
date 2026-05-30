using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public class TourPackageConfiguration : IEntityTypeConfiguration<TourPackage>
{
    public void Configure(EntityTypeBuilder<TourPackage> builder)
    {
        builder.ToTable("TourPackages", "content_tours");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CreatedByUserId).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.OwnsOne(e => e.Price, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Price").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("PriceCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });

        // Legacy "Currency" column preserved (no schema change). The domain
        // keeps it in sync with Money.Currency on every write, so reads from
        // either property are always consistent.
        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .IsUnicode(false);

        builder.Property(x => x.MaxParticipants).IsRequired(false);
        builder.Property(x => x.ValidFrom).IsRequired(false);
        builder.Property(x => x.ValidTo).IsRequired(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        // Task 5 — bundle membership through the junction (TourPackageTour).
        builder.HasMany(x => x.IncludedTours)
            .WithOne(x => x.TourPackage)
            .HasForeignKey(x => x.TourPackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Inclusions)
            .WithOne(x => x.TourPackage)
            .HasForeignKey(x => x.TourPackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CreatedByUserId);
        builder.HasIndex(x => x.ValidTo);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
