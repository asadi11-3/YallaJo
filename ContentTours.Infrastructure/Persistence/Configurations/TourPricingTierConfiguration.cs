using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public class TourPricingTierConfiguration : IEntityTypeConfiguration<TourPricingTier>
{
    public void Configure(EntityTypeBuilder<TourPricingTier> builder)
    {
        builder.ToTable("TourPricingTiers", "content_tours");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TourId).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.OwnsOne(e => e.Price, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Price").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("PriceCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .IsUnicode(false);

        builder.Property(x => x.MinParticipants)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(x => x.MaxParticipants).IsRequired(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.Tour)
            .WithMany(x => x.TourPricingTiers)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
