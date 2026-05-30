using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", "content_blogs");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(o => o.OccurredOnUtc).IsRequired();
        builder.Property(o => o.ProcessedOnUtc).IsRequired(false);

        builder.Property(o => o.Error)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.Property(o => o.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(o => o.TraceContext)
                    .IsRequired(false)
                    .HasMaxLength(500);
        
                builder.Property(o => o.Status)
                            .IsRequired()
                            .HasDefaultValue(OutboxMessageStatus.Pending)
                            .HasConversion<int>();
        
                builder.HasIndex(o => new { o.ProcessedOnUtc, o.RetryCount, o.OccurredOnUtc })
                    .HasDatabaseName("IX_OutboxMessages_Unprocessed");
    }
}
