using ContentPlaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentPlaces.Infrastructure.Persistence.Configurations;

public class BusinessStaffConfiguration : IEntityTypeConfiguration<BusinessStaff>
{
    public void Configure(EntityTypeBuilder<BusinessStaff> builder)
    {
        builder.ToTable("BusinessStaff", "content_places");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.BusinessId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.JoinRequestId).IsRequired(false);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeactivatedAt).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.Business)
            .WithMany(x => x.Staff)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.BusinessId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.BusinessId, x.UserId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
