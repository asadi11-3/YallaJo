using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

internal sealed class GuideAvailabilityBlockConfiguration : IEntityTypeConfiguration<GuideAvailabilityBlock>
{
    public void Configure(EntityTypeBuilder<GuideAvailabilityBlock> builder)
    {
        builder.ToTable("GuideAvailabilityBlocks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GuideId).IsRequired();
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired();
        builder.Property(x => x.Reason).IsRequired(false).HasMaxLength(200);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.GuideId);
        builder.HasIndex(x => new { x.GuideId, x.StartDate, x.EndDate });

        builder.HasOne<TourGuide>()
            .WithMany()
            .HasForeignKey(x => x.GuideId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
