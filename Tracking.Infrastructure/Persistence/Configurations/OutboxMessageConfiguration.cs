using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Tracking.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", "tracking");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.OccurredOnUtc).IsRequired();
        builder.Property(x => x.ProcessedOnUtc).IsRequired(false);

        builder.Property(x => x.Error)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.Property(x => x.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.TraceContext)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasDefaultValue(OutboxMessageStatus.Pending)
            .HasConversion<int>();

        builder.HasIndex(x => new { x.ProcessedOnUtc, x.RetryCount, x.OccurredOnUtc })
            .HasDatabaseName("IX_OutboxMessages_Unprocessed");
    }
}
