using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Messaging.Infrastructure.Persistence.Seeding;

public sealed class MessagingDbInitializer(MessagingDbContext dbContext) : IModuleDbInitializer
{
    private static readonly Guid TravelerOne  = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid TravelerTwo  = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid AdminUser    = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid TemplateId      = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000001");
    private static readonly Guid PreferenceOneId = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000003");
    private static readonly Guid PreferenceTwoId = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000004");
    private static readonly Guid NotificationId  = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000005");
    private static readonly Guid DeviceTokenId   = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000006");
    private static readonly Guid TicketId        = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000007");
    private static readonly Guid TicketMessageId = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000008");
    // Deferred post-MVP: ConversationId, UserMessageId, BotMessageId (ChatBot)

    public int Order => 120;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.NotificationTemplates.AnyAsync(cancellationToken))
        {
            return;
        }

        var templates      = CreateTemplates();
        var preferences    = CreatePreferences();
        var notifications  = CreateNotifications();
        var tokens         = CreateDeviceTokens();
        var tickets        = CreateSupportTickets();
        var ticketMessages = CreateTicketMessages();
        dbContext.NotificationTemplates.AddRange(templates);
        dbContext.NotificationPreferences.AddRange(preferences);
        dbContext.Notifications.AddRange(notifications);
        dbContext.DeviceTokens.AddRange(tokens);
        dbContext.SupportTickets.AddRange(tickets);
        dbContext.TicketMessages.AddRange(ticketMessages);
        // Deferred post-MVP: ChatBotConversations, ChatBotMessages

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // ── Templates ─────────────────────────────────────────────────────────────

    private static List<NotificationTemplate> CreateTemplates()
    {
        var tmpl = CreateEntity<NotificationTemplate>();
        SetProperty(tmpl, nameof(NotificationTemplate.Id),           TemplateId);
        SetProperty(tmpl, nameof(NotificationTemplate.Type),         NotificationType.BookingConfirmed);
        SetProperty(tmpl, nameof(NotificationTemplate.Channel),      NotificationChannel.InApp);
        SetProperty(tmpl, nameof(NotificationTemplate.LanguageCode), "en");
        SetProperty(tmpl, nameof(NotificationTemplate.Title),        "Your booking is confirmed");
        SetProperty(tmpl, nameof(NotificationTemplate.Body),         "Hi {{firstName}}, your tour is confirmed for {{date}}.");
        return [tmpl];
    }

    // ── Preferences ──────────────────────────────────────────────────────────

    private static List<NotificationPreference> CreatePreferences()
    {
        var prefOne = CreateEntity<NotificationPreference>();
        SetProperty(prefOne, nameof(NotificationPreference.Id),        PreferenceOneId);
        SetProperty(prefOne, nameof(NotificationPreference.UserId),    TravelerOne);
        SetProperty(prefOne, nameof(NotificationPreference.NotificationType), NotificationType.BookingConfirmed);
        SetProperty(prefOne, nameof(NotificationPreference.Channel),   NotificationChannel.InApp);
        SetProperty(prefOne, nameof(NotificationPreference.IsEnabled), true);

        var prefTwo = CreateEntity<NotificationPreference>();
        SetProperty(prefTwo, nameof(NotificationPreference.Id),        PreferenceTwoId);
        SetProperty(prefTwo, nameof(NotificationPreference.UserId),    TravelerTwo);
        SetProperty(prefTwo, nameof(NotificationPreference.NotificationType), NotificationType.ReviewPosted);
        SetProperty(prefTwo, nameof(NotificationPreference.Channel),   NotificationChannel.Push);
        SetProperty(prefTwo, nameof(NotificationPreference.IsEnabled), true);

        return [prefOne, prefTwo];
    }

    // ── Notifications ─────────────────────────────────────────────────────────

    private static List<Notification> CreateNotifications()
    {
        var notification = CreateEntity<Notification>();
        SetProperty(notification, nameof(Notification.Id),         NotificationId);
        SetProperty(notification, nameof(Notification.UserId),     TravelerOne);
        SetProperty(notification, nameof(Notification.Type),       NotificationType.BookingConfirmed);
        SetProperty(notification, nameof(Notification.Channel),    NotificationChannel.InApp);
        SetProperty(notification, nameof(Notification.Priority),   NotificationPriority.High);
        SetProperty(notification, nameof(Notification.Title),      "Booking confirmed");
        SetProperty(notification, nameof(Notification.Body),       "Your Petra Full Day Explorer booking is confirmed.");
        SetProperty(notification, nameof(Notification.Data),       "{\"bookingId\":\"abababab-0000-0000-0000-000000000001\"}");
        SetProperty(notification, nameof(Notification.IsRead),     false);
        SetProperty(notification, nameof(Notification.SentAt),     DateTime.UtcNow.AddHours(-6));
        SetProperty(notification, nameof(Notification.EntityType), "TourBooking");
        SetProperty(notification, nameof(Notification.EntityId),   SeedBookingIds.BookingOne);
        return [notification];
    }

    // ── Device Tokens ─────────────────────────────────────────────────────────

    private static List<DeviceToken> CreateDeviceTokens()
    {
        var token = CreateEntity<DeviceToken>();
        SetProperty(token, nameof(DeviceToken.Id),         DeviceTokenId);
        SetProperty(token, nameof(DeviceToken.UserId),     TravelerTwo);
        SetProperty(token, nameof(DeviceToken.Token),      "fcm-test-token-7777");
        SetProperty(token, nameof(DeviceToken.Platform),   DevicePlatform.Android);
        SetProperty(token, nameof(DeviceToken.DeviceId),   "pixel8-device-7777");
        SetProperty(token, nameof(DeviceToken.LastSeenAt), DateTime.UtcNow.AddMinutes(-30));
        return [token];
    }

    // ── Support Tickets ───────────────────────────────────────────────────────

    private static List<SupportTicket> CreateSupportTickets()
    {
        var ticket = CreateEntity<SupportTicket>();
        SetProperty(ticket, nameof(SupportTicket.Id),               TicketId);
        SetProperty(ticket, nameof(SupportTicket.CreatedByUserId),  TravelerOne);
        SetProperty(ticket, nameof(SupportTicket.Subject),          "Pickup point clarification");
        SetProperty(ticket, nameof(SupportTicket.InitialBody),      "Can you confirm the exact meeting point near Petra Visitor Center?");
        SetProperty(ticket, nameof(SupportTicket.Status),           TicketStatus.InProgress);
        SetProperty(ticket, nameof(SupportTicket.Priority),         TicketPriority.Medium);
        SetProperty(ticket, nameof(SupportTicket.Category),         TicketCategory.BookingIssue);
        SetProperty(ticket, nameof(SupportTicket.AssignedToUserId), AdminUser);
        SetProperty(ticket, nameof(SupportTicket.SlaBreachAt),      DateTime.UtcNow.AddHours(12));
        return [ticket];
    }

    // ── Ticket Messages ───────────────────────────────────────────────────────

    private static List<TicketMessage> CreateTicketMessages()
    {
        var message = CreateEntity<TicketMessage>();
        SetProperty(message, nameof(TicketMessage.Id),           TicketMessageId);
        SetProperty(message, nameof(TicketMessage.TicketId),     TicketId);
        SetProperty(message, nameof(TicketMessage.AuthorUserId), AdminUser);
        SetProperty(message, nameof(TicketMessage.Body),         "The meeting point is the main gate beside the ticket counters.");
        SetProperty(message, nameof(TicketMessage.IsInternal),   false);
        SetProperty(message, nameof(TicketMessage.CreatedAt),    DateTime.UtcNow.AddHours(-5));
        return [message];
    }


    private static TEntity CreateEntity<TEntity>() where TEntity : class
    {
        var entity = Activator.CreateInstance(typeof(TEntity), nonPublic: true) as TEntity;
        if (entity is null)
        {
            throw new InvalidOperationException($"Failed to create entity instance for {typeof(TEntity).FullName}.");
        }

        return entity;
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (property is null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().FullName}.");
        }

        property.SetValue(target, value);
    }
}
