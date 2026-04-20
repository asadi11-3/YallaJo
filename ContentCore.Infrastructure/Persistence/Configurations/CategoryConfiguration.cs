using ContentCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentCore.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", "content_core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ParentCategoryId).IsRequired(false);

        builder.Property(x => x.Name)
            .IsRequired()
            .IsUnicode(true)
            .HasMaxLength(200);

        builder.Property(x => x.Slug)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(200);

        builder.Property(x => x.Icon)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(100);

        builder.Property(x => x.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.SubCategories)
            .WithOne(x => x.ParentCategory)
            .HasForeignKey(x => x.ParentCategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Translations)
            .WithOne(x => x.Category)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
