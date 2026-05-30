using Messaging.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messaging.Infrastructure.Persistence.Configurations;

internal sealed class UserSnapshotConfiguration : IEntityTypeConfiguration<UserSnapshot>
{
    public void Configure(EntityTypeBuilder<UserSnapshot> builder)
    {
        builder.ToTable("UserSnapshots", "messaging");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Email).IsRequired().IsUnicode(false).HasMaxLength(256);
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.LanguageCode).IsRequired().IsUnicode(false).HasMaxLength(10).HasDefaultValue("en");
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
