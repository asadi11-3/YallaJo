using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceNumberCounterConfiguration : IEntityTypeConfiguration<InvoiceNumberCounter>
{
    public void Configure(EntityTypeBuilder<InvoiceNumberCounter> builder)
    {
        builder.ToTable("InvoiceNumberCounters", "finance");

        builder.HasKey(x => x.YearMonth);

        builder.Property(x => x.YearMonth)
            .HasMaxLength(6)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.Seq)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
