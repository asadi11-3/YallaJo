using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;

namespace Social.Infrastructure.Persistence.Configurations;

public sealed class ProfanityBlocklistEntryConfiguration : IEntityTypeConfiguration<ProfanityBlocklistEntry>
{
    public void Configure(EntityTypeBuilder<ProfanityBlocklistEntry> builder)
    {
        builder.ToTable("ProfanityBlocklistEntries", "social");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Word).IsRequired().HasMaxLength(200).IsUnicode(false);
        builder.Property(x => x.LanguageCode).IsRequired().HasMaxLength(10).IsUnicode(false);

        // BaseEntity only
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.Word, x.LanguageCode })
            .IsUnique()
            .HasDatabaseName("IX_ProfanityBlocklistEntries_Word_Language_Unique");

        builder.HasIndex(x => x.LanguageCode).HasDatabaseName("IX_ProfanityBlocklistEntries_LanguageCode");
    }
}
