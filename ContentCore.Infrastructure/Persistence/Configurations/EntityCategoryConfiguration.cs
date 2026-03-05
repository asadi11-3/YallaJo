using ContentCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentCore.Infrastructure.Persistence.Configurations;

public class EntityCategoryConfiguration : IEntityTypeConfiguration<EntityCategory>
{
    public void Configure(EntityTypeBuilder<EntityCategory> builder)
    {
        builder.ToTable("EntityCategories", "content_core");

        builder.HasKey(x => new { x.EntityType, x.EntityId, x.CategoryId });

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.CategoryId).IsRequired();

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
