using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;

namespace Social.Infrastructure.Persistence.Configurations;

public sealed class UserModerationRecordConfiguration : IEntityTypeConfiguration<UserModerationRecord>
{
    public void Configure(EntityTypeBuilder<UserModerationRecord> builder)
    {
        builder.ToTable("UserModerationRecords", "social");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.EntityType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.Action).IsRequired().HasConversion<byte>();
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.ExpiresAt).IsRequired(false);
        builder.Property(x => x.IssuedByAdminId).IsRequired();
        builder.Property(x => x.IssuedAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_UserModerationRecords_UserId");
        builder.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("IX_UserModerationRecords_Entity");
    }
}
