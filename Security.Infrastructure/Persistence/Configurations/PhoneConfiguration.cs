using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Entities;

namespace Security.Infrastructure.Persistence.Configurations;

public class PhoneConfiguration : IEntityTypeConfiguration<Phone>
{
    public void Configure(EntityTypeBuilder<Phone> builder)
    {
        builder.ToTable("Phones", "security");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.UserId).IsRequired();

        builder.Property(p => p.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.IsPrimary)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.IsVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.VerifiedAt).IsRequired(false);

        // Auditable fields
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired(false);
        builder.Property(p => p.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).IsRequired(false);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasQueryFilter(p => !p.IsDeleted);

        // Indexes
        builder.HasIndex(p => p.PhoneNumber)
            .HasDatabaseName("IX_Phones_PhoneNumber")
            .HasFilter("[IsVerified] = 1");
        builder.HasIndex(p => new { p.UserId, p.IsPrimary });
        builder.HasIndex(p => p.IsVerified);
    }
}
