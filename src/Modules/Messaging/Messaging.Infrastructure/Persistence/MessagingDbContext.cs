using Messaging.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Inbox;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Messaging.Infrastructure.Persistence;

public sealed class MessagingDbContext : DbContext, IDbContext
{
    public MessagingDbContext(DbContextOptions<MessagingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationDeliveryAttempt> NotificationDeliveryAttempts => Set<NotificationDeliveryAttempt>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<UserSnapshot> UserSnapshots => Set<UserSnapshot>();
    public DbSet<AdminAssignmentRoster> AdminAssignmentRosters => Set<AdminAssignmentRoster>();
    // Deferred (post-MVP, not mapped to the database):
    // public DbSet<ChatBotConversation> ChatBotConversations => Set<ChatBotConversation>();
    // public DbSet<ChatBotMessage> ChatBotMessages => Set<ChatBotMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("messaging");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(MessagingDbContext).Assembly,
            type => type.Namespace?.Contains("Messaging.Infrastructure.Persistence.Configurations") ?? false
        );

        base.OnModelCreating(modelBuilder);
    }
}
