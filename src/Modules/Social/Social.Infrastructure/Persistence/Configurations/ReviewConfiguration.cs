using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;
using Social.Domain.Enums;

namespace Social.Infrastructure.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews", "social");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // Ownership
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.TargetType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.TargetId).IsRequired();

        // Content
        builder.Property(x => x.Rating).IsRequired().HasPrecision(2, 1);
        builder.Property(x => x.Title).IsRequired(false).HasMaxLength(200);
        builder.Property(x => x.Content).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.VisitDate).IsRequired(false);

        // Status flags
        builder.Property(x => x.Status).IsRequired().HasConversion<byte>().HasDefaultValue(ReviewStatus.Published);
        builder.Property(x => x.IsVerifiedBooking).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.ProfanityFlagged).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CurrentReportCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.HelpfulVoteCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.AutoHiddenAt).IsRequired(false);
        builder.Property(x => x.LastEditedAt).IsRequired(false);

        // Audit columns (from AuditableEntity)
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);

        // ReviewReply as owned collection
        builder.OwnsMany(x => x.Replies, reply =>
        {
            reply.ToTable("ReviewReplies", "social");
            reply.HasKey(r => r.Id);
            reply.Property(r => r.Id).ValueGeneratedNever();
            reply.WithOwner().HasForeignKey(r => r.ReviewId);
            reply.Property(r => r.ReviewId).IsRequired();
            reply.Property(r => r.ProviderUserId).IsRequired();
            reply.Property(r => r.Content).IsRequired().HasMaxLength(2000);
            reply.Property(r => r.IsDeleted).IsRequired().HasDefaultValue(false);
            reply.Property(r => r.CreatedAt).IsRequired();
            reply.Property(r => r.LastEditedAt).IsRequired(false);
            reply.HasIndex(r => r.ReviewId);
        });

        // Indexes
        // S-R2: one review per (UserId, TargetType, TargetId) — unique filtered
        builder.HasIndex(x => new { x.UserId, x.TargetType, x.TargetId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Reviews_User_Target_Unique");

        builder.HasIndex(x => new { x.TargetType, x.TargetId })
            .HasDatabaseName("IX_Reviews_TargetType_TargetId");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_Reviews_UserId");

        builder.HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("IX_Reviews_Status_CreatedAt");
    }
}
