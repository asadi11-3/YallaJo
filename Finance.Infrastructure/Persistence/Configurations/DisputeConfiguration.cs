using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

public class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.ToTable("Disputes", "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.PaymentId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Description).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.Status).IsRequired().HasConversion<int>().HasDefaultValue(DisputeStatus.Open);
        builder.Property(x => x.Resolution).IsRequired(false).HasConversion<int>();
        builder.Property(x => x.ResolvedAt).IsRequired(false);
        builder.Property(x => x.ResolvedByUserId).IsRequired(false);
        builder.Property(x => x.ResolutionNotes).IsRequired(false).HasColumnType("nvarchar(max)");

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.Payment)
            .WithMany(x => x.Disputes)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
