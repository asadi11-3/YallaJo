using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Entities;

namespace Security.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "security");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        // Nullable — system actions have no user. No FK (cross-module reference).
        builder.Property(a => a.UserId).IsRequired(false);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.ResourceType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.ResourceId).IsRequired(false);

        builder.Property(a => a.IpAddress)
            .IsRequired(false)
            .HasMaxLength(45); // IPv6 max

        builder.Property(a => a.OldValue)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.NewValue)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.OccurredAt).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        // Phase 4 — admin audit columns. All nullable so legacy audit
        // rows (REGISTER, LOGIN, LOGOUT, PASSWORD_CHANGED,
        // PASSWORD_RESET) keep working unchanged via AuditLog.Create.
        builder.Property(a => a.ActorUserId).IsRequired(false);

        builder.Property(a => a.Reason)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(a => a.Metadata)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.OccurredAt);
        builder.HasIndex(a => new { a.ResourceType, a.ResourceId });

        // Phase 4 — admin-by-admin queries (e.g. "what did admin X do
        // last week?") — composite index keeps OccurredAt sortable.
        builder.HasIndex(a => new { a.ActorUserId, a.OccurredAt })
            .HasDatabaseName("IX_AuditLogs_ActorUserId_OccurredAt");
    }
}
