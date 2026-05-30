using ContentPlaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentPlaces.Infrastructure.Persistence.Configurations;

public class BusinessHoursConfiguration : IEntityTypeConfiguration<BusinessHours>
{
    public void Configure(EntityTypeBuilder<BusinessHours> builder)
    {
        builder.ToTable("BusinessHours", "content_places");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.BusinessId).IsRequired();

        builder.Property(x => x.DayOfWeek)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.OpenTime).IsRequired();
        builder.Property(x => x.CloseTime).IsRequired();

        builder.Property(x => x.IsClosed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.Business)
            .WithMany(x => x.BusinessHours)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.Business.IsDeleted);

        // Removed unique index on (BusinessId, DayOfWeek) to support split shifts
        // (two entries per day, e.g. 9:00-13:00 and 17:00-22:00).
        // Overlap validation is enforced in SetBusinessHoursCommandHandler.
        builder.HasIndex(x => x.BusinessId);
    }
}
