using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class ProviderDocumentConfiguration : IEntityTypeConfiguration<ProviderDocument>
{
    public void Configure(EntityTypeBuilder<ProviderDocument> builder)
    {
        builder.ToTable("ProviderDocuments", "booking", t => t.HasCheckConstraint(
            "CK_ProviderDocuments_SingleTarget",
            "(CASE WHEN [TourGuideId] IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN [BusinessId] IS NOT NULL THEN 1 ELSE 0 END) = 1"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TourGuideId).IsRequired(false);
        builder.Property(x => x.BusinessId).IsRequired(false);
        builder.Property(x => x.DocumentType).IsRequired().HasConversion<int>();
        builder.Property(x => x.DocumentUrl).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.OriginalFileName).IsRequired(false).HasMaxLength(500);
        builder.Property(x => x.ExpiresAt).IsRequired(false);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.ReviewedAt).IsRequired(false);
        builder.Property(x => x.ReviewedByUserId).IsRequired(false);
        builder.Property(x => x.RejectionReason).IsRequired(false).HasMaxLength(1000);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.TourGuide)
            .WithMany(x => x.ProviderDocuments)
            .HasForeignKey(x => x.TourGuideId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TourGuideId, x.DocumentType });
        builder.HasIndex(x => x.BusinessId).HasFilter("[BusinessId] IS NOT NULL");
    }
}
