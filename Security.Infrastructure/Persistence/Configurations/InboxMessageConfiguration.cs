// Security.Infrastructure/Persistence/Configurations/InboxMessageConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YallaJo.SharedKernel.Infrastructure.Inbox;

namespace Security.Infrastructure.Persistence.Configurations;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages", "security");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.ProcessedAt).IsRequired();

        builder.HasIndex(m => m.ProcessedAt)
            .HasDatabaseName("IX_InboxMessages_ProcessedAt");
    }
}
