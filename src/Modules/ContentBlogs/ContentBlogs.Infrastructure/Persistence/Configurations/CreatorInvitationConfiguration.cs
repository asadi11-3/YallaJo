using ContentBlogs.Domain.Entities.Creators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentBlogs.Infrastructure.Persistence.Configurations;

public class CreatorInvitationConfiguration : IEntityTypeConfiguration<CreatorInvitation>
{
    public void Configure(EntityTypeBuilder<CreatorInvitation> builder)
    {
        builder.ToTable("CreatorInvitations", "content_blogs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Kind)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Email)
            .IsRequired(false)
            .HasMaxLength(320);

        builder.Property(x => x.InvitedUserId).IsRequired(false);
        builder.Property(x => x.SentByAdminId).IsRequired();

        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.PersonalMessage)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.RedeemedByUserId).IsRequired(false);
        builder.Property(x => x.RedeemedAt).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => x.Email).HasFilter("[Email] IS NOT NULL");
        builder.HasIndex(x => x.InvitedUserId).HasFilter("[InvitedUserId] IS NOT NULL");
        builder.HasIndex(x => new { x.Status, x.ExpiresAt });
    }
}
