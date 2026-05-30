using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YallaJo.SharedKernel.Infrastructure.Inbox;

namespace ContentSeo.Infrastructure.Persistence.Configurations;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages", "content_seo");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.ProcessedAt).IsRequired();

        builder.HasIndex(m => m.ProcessedAt)
            .HasDatabaseName("IX_InboxMessages_ProcessedAt");
    }
}
