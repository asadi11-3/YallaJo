using Messaging.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messaging.Infrastructure.Persistence.Configurations;

public class ChatBotMessageConfiguration : IEntityTypeConfiguration<ChatBotMessage>
{
    public void Configure(EntityTypeBuilder<ChatBotMessage> builder)
    {
        builder.ToTable("ChatBotMessages", "messaging");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ConversationId).IsRequired();

        builder.Property(x => x.IsFromBot).IsRequired();

        builder.Property(x => x.Message)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Confidence)
            .IsRequired(false)
            .HasPrecision(5, 4);

        builder.Property(x => x.Intent)
            .IsRequired(false)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
    }
}
