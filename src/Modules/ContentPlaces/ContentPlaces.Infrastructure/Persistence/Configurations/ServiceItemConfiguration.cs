using ContentPlaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentPlaces.Infrastructure.Persistence.Configurations;

public class ServiceItemConfiguration : IEntityTypeConfiguration<ServiceItem>
{
    public void Configure(EntityTypeBuilder<ServiceItem> builder)
    {
        builder.ToTable("ServiceItems", "content_places");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.BusinessId).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Description)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.Property(x => x.Price)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(x => x.Currency)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(3);

        builder.Property(x => x.Category)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.DurationMinutes).IsRequired();
        builder.Property(x => x.MaxCapacity).IsRequired();
        builder.Property(x => x.IsAvailable).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();

        builder.Property(x => x.DiscountPercent)
            .IsRequired(false)
            .HasPrecision(5, 2);

        builder.Property(x => x.SalePrice)
            .IsRequired(false)
            .HasPrecision(19, 4);

        builder.Property(x => x.DiscountValidFrom).IsRequired(false);
        builder.Property(x => x.DiscountValidTo).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.Business)
            .WithMany(x => x.ServiceItems)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.BusinessId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
