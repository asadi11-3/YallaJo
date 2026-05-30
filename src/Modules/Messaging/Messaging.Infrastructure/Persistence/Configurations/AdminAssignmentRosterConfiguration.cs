using Messaging.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messaging.Infrastructure.Persistence.Configurations;

internal sealed class AdminAssignmentRosterConfiguration : IEntityTypeConfiguration<AdminAssignmentRoster>
{
    public void Configure(EntityTypeBuilder<AdminAssignmentRoster> builder)
    {
        builder.ToTable("AdminAssignmentRosters", "messaging");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.AdminUserId).IsRequired();
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.LastAssignedAt).IsRequired();
        builder.Property(x => x.IsOnLeave).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.AdminUserId).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.IsOnLeave, x.LastAssignedAt }); // round-robin index
    }
}
