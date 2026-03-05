using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public class TourTranslationConfiguration : IEntityTypeConfiguration<TourTranslation>
{
    public void Configure(EntityTypeBuilder<TourTranslation> builder)
    {
        builder.ToTable("TourTranslations", "content_tours");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TourId).IsRequired();
        builder.Property(x => x.LanguageId).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Description)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.ShortDescription)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(x => x.MeetingPoint)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.Tour)
            .WithMany(x => x.TourTranslations)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.Tour.IsDeleted);
    }
}
