using ContentBlogs.Domain.Entities.Creators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentBlogs.Infrastructure.Persistence.Configurations;

public class CreatorApplicationConfiguration : IEntityTypeConfiguration<CreatorApplication>
{
    public void Configure(EntityTypeBuilder<CreatorApplication> builder)
    {
        builder.ToTable("CreatorApplications", "content_blogs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ApplicantUserId).IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Source)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.InvitationId).IsRequired(false);

        builder.Property(x => x.Bio)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.Property(x => x.PortfolioUrls)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.SampleWorkUrls)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.NicheIds)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.FreeTags)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.LanguageIds)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.PreferredRegionIds)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.SocialHandles)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.ReviewedByAdminId).IsRequired(false);
        builder.Property(x => x.ReviewedAt).IsRequired(false);

        builder.Property(x => x.AdminNote)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.Property(x => x.ReapplicationCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastRejectedAt).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.ApplicantUserId);
        builder.HasIndex(x => x.Status);
    }
}
