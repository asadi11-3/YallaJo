using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", "auth");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.SessionId).IsRequired();

        builder.Property(r => r.TokenHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(r => r.ExpiresAt).IsRequired();

        builder.Property(r => r.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.RevokedAt).IsRequired(false);
        builder.Property(r => r.ReplacedByTokenId).IsRequired(false);

        // Auditable fields
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired(false);
        builder.Property(r => r.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(r => r.DeletedAt).IsRequired(false);
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.HasOne<Session>()
            .WithMany()
            .HasForeignKey(r => r.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.TokenHash).IsUnique();
        builder.HasIndex(r => r.SessionId);
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.IsRevoked);
        builder.HasIndex(r => r.ExpiresAt);
    }
}
