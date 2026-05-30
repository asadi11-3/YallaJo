using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messaging.Infrastructure.Persistence.Configurations;

public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("SupportTickets", "messaging");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CreatedByUserId).IsRequired();

        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.InitialBody)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<byte>()
            .HasDefaultValue(TicketStatus.Open);

        builder.Property(x => x.Priority)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.Category)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(x => x.AssignedToUserId).IsRequired(false);
        builder.Property(x => x.AssignedAt).IsRequired(false);
        builder.Property(x => x.ResolvedByUserId).IsRequired(false);
        builder.Property(x => x.ResolvedAt).IsRequired(false);
        builder.Property(x => x.ResolutionNotes).IsRequired(false).HasMaxLength(2000);
        builder.Property(x => x.ClosedAt).IsRequired(false);
        builder.Property(x => x.SlaBreachAt).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.TicketMessages)
            .WithOne(x => x.SupportTicket)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.SlaBreachAt);
        builder.HasIndex(x => new { x.CreatedByUserId, x.Status });
    }
}
