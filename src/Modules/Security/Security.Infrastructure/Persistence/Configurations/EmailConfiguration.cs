using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Entities;

namespace Security.Infrastructure.Persistence.Configurations;

public sealed class EmailConfiguration : IEntityTypeConfiguration<Email>
{
    public void Configure(EntityTypeBuilder<Email> builder)
    {
        builder.ToTable("Emails", "security");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.UserId).IsRequired();

        builder.Property(e => e.Address)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(e => e.IsPrimary)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.IsVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.VerifiedAt).IsRequired(false);

        // Auditable fields
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired(false);
        builder.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.DeletedAt).IsRequired(false);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasQueryFilter(e => !e.IsDeleted);

        // Indexes
        builder.HasIndex(e => e.Address).IsUnique()
            .HasDatabaseName("IX_Emails_Address_Unique");
        builder.HasIndex(e => new { e.UserId, e.IsPrimary });
        builder.HasIndex(e => e.IsVerified);
    }
}
