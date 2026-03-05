using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Messaging.Infrastructure.Persistence.Seeding;

public sealed class MessagingDbInitializer(MessagingDbContext dbContext) : IModuleDbInitializer
{
    private static readonly Guid TravelerOne = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid TravelerTwo = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid AdminUser = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BookingTemplateId = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000001");
    private static readonly Guid PromoTemplateId = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000002");
    private static readonly Guid PreferenceOneId = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000003");
    private static readonly Guid PreferenceTwoId = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000004");
    private static readonly Guid NotificationId = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000005");
    private static readonly Guid DeviceTokenId = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000006");
    private static readonly Guid TicketId = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000007");
    private static readonly Guid TicketMessageId = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000008");
    private static readonly Guid ConversationId = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000009");
    private static readonly Guid UserMessageId = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000010");
    private static readonly Guid BotMessageId = Guid.Parse("d1d1d1d1-0000-0000-0000-000000000011");

    public int Order => 120;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.NotificationTemplates.AnyAsync(cancellationToken))
        {
            return;
        }

        var templates = CreateTemplates();
        var preferences = CreatePreferences();
        var notifications = CreateNotifications();
        var tokens = CreateDeviceTokens();
        var tickets = CreateSupportTickets();
        var ticketMessages = CreateTicketMessages();
        var conversations = CreateConversations();
        var chatMessages = CreateChatMessages();

        dbContext.NotificationTemplates.AddRange(templates);
        dbContext.NotificationPreferences.AddRange(preferences);
        dbContext.Notifications.AddRange(notifications);
        dbContext.DeviceTokens.AddRange(tokens);
        dbContext.SupportTickets.AddRange(tickets);
        dbContext.TicketMessages.AddRange(ticketMessages);
        dbContext.ChatBotConversations.AddRange(conversations);
        dbContext.ChatBotMessages.AddRange(chatMessages);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<NotificationTemplate> CreateTemplates()
    {
        var booking = CreateEntity<NotificationTemplate>();
        SetProperty(booking, nameof(NotificationTemplate.Id), BookingTemplateId);
        SetProperty(booking, nameof(NotificationTemplate.Name), "Booking Confirmed");
        SetProperty(booking, nameof(NotificationTemplate.Code), "booking.confirmed.inapp");
        SetProperty(booking, nameof(NotificationTemplate.Type), NotificationType.Booking);
        SetProperty(booking, nameof(NotificationTemplate.Channel), NotificationChannel.InApp);
        SetProperty(booking, nameof(NotificationTemplate.Subject), "Your booking is confirmed");
        SetProperty(booking, nameof(NotificationTemplate.BodyTemplate), "Hi {{firstName}}, your tour is confirmed for {{date}}.");
        SetProperty(booking, nameof(NotificationTemplate.IsActive), true);

        var promotion = CreateEntity<NotificationTemplate>();
        SetProperty(promotion, nameof(NotificationTemplate.Id), PromoTemplateId);
        SetProperty(promotion, nameof(NotificationTemplate.Name), "Weekend Offer");
        SetProperty(promotion, nameof(NotificationTemplate.Code), "promo.weekend.push");
        SetProperty(promotion, nameof(NotificationTemplate.Type), NotificationType.Promotion);
        SetProperty(promotion, nameof(NotificationTemplate.Channel), NotificationChannel.Push);
        SetProperty(promotion, nameof(NotificationTemplate.Subject), "Weekend offer");
        SetProperty(promotion, nameof(NotificationTemplate.BodyTemplate), "Save 10% on selected Petra tours this weekend.");
        SetProperty(promotion, nameof(NotificationTemplate.IsActive), true);

        return [booking, promotion];
    }

    private static List<NotificationPreference> CreatePreferences()
    {
        var prefOne = CreateEntity<NotificationPreference>();
        SetProperty(prefOne, nameof(NotificationPreference.Id), PreferenceOneId);
        SetProperty(prefOne, nameof(NotificationPreference.UserId), TravelerOne);
        SetProperty(prefOne, nameof(NotificationPreference.NotificationType), NotificationType.Booking);
        SetProperty(prefOne, nameof(NotificationPreference.Channel), NotificationChannel.InApp);
        SetProperty(prefOne, nameof(NotificationPreference.IsEnabled), true);

        var prefTwo = CreateEntity<NotificationPreference>();
        SetProperty(prefTwo, nameof(NotificationPreference.Id), PreferenceTwoId);
        SetProperty(prefTwo, nameof(NotificationPreference.UserId), TravelerTwo);
        SetProperty(prefTwo, nameof(NotificationPreference.NotificationType), NotificationType.Promotion);
        SetProperty(prefTwo, nameof(NotificationPreference.Channel), NotificationChannel.Push);
        SetProperty(prefTwo, nameof(NotificationPreference.IsEnabled), true);

        return [prefOne, prefTwo];
    }

    private static List<Notification> CreateNotifications()
    {
        var notification = CreateEntity<Notification>();
        SetProperty(notification, nameof(Notification.Id), NotificationId);
        SetProperty(notification, nameof(Notification.UserId), TravelerOne);
        SetProperty(notification, nameof(Notification.Type), NotificationType.Booking);
        SetProperty(notification, nameof(Notification.Channel), NotificationChannel.InApp);
        SetProperty(notification, nameof(Notification.Priority), NotificationPriority.High);
        SetProperty(notification, nameof(Notification.Title), "Booking confirmed");
        SetProperty(notification, nameof(Notification.Body), "Your Petra Full Day Explorer booking is confirmed.");
        SetProperty(notification, nameof(Notification.Data), "{\"bookingId\":\"abababab-0000-0000-0000-000000000001\"}");
        SetProperty(notification, nameof(Notification.IsRead), false);
        SetProperty(notification, nameof(Notification.SentAt), DateTime.UtcNow.AddHours(-6));
        SetProperty(notification, nameof(Notification.EntityType), "TourBooking");
        SetProperty(notification, nameof(Notification.EntityId), SeedBookingIds.BookingOne);
        return [notification];
    }

    private static List<DeviceToken> CreateDeviceTokens()
    {
        var token = CreateEntity<DeviceToken>();
        SetProperty(token, nameof(DeviceToken.Id), DeviceTokenId);
        SetProperty(token, nameof(DeviceToken.UserId), TravelerTwo);
        SetProperty(token, nameof(DeviceToken.Token), "fcm-test-token-7777");
        SetProperty(token, nameof(DeviceToken.Platform), DevicePlatform.Android);
        SetProperty(token, nameof(DeviceToken.DeviceName), "Pixel 8");
        SetProperty(token, nameof(DeviceToken.IsActive), true);
        SetProperty(token, nameof(DeviceToken.LastUsedAt), DateTime.UtcNow.AddMinutes(-30));
        return [token];
    }

    private static List<SupportTicket> CreateSupportTickets()
    {
        var ticket = CreateEntity<SupportTicket>();
        SetProperty(ticket, nameof(SupportTicket.Id), TicketId);
        SetProperty(ticket, nameof(SupportTicket.UserId), TravelerOne);
        SetProperty(ticket, nameof(SupportTicket.Subject), "Pickup point clarification");
        SetProperty(ticket, nameof(SupportTicket.Description), "Can you confirm the exact meeting point near Petra Visitor Center?");
        SetProperty(ticket, nameof(SupportTicket.Status), TicketStatus.InProgress);
        SetProperty(ticket, nameof(SupportTicket.Priority), TicketPriority.Medium);
        SetProperty(ticket, nameof(SupportTicket.AssignedToUserId), AdminUser);
        SetProperty(ticket, nameof(SupportTicket.Category), "Booking");
        return [ticket];
    }

    private static List<TicketMessage> CreateTicketMessages()
    {
        var message = CreateEntity<TicketMessage>();
        SetProperty(message, nameof(TicketMessage.Id), TicketMessageId);
        SetProperty(message, nameof(TicketMessage.TicketId), TicketId);
        SetProperty(message, nameof(TicketMessage.SenderUserId), AdminUser);
        SetProperty(message, nameof(TicketMessage.Message), "The meeting point is the main gate beside the ticket counters.");
        SetProperty(message, nameof(TicketMessage.IsStaffReply), true);
        return [message];
    }

    private static List<ChatBotConversation> CreateConversations()
    {
        var conversation = CreateEntity<ChatBotConversation>();
        SetProperty(conversation, nameof(ChatBotConversation.Id), ConversationId);
        SetProperty(conversation, nameof(ChatBotConversation.UserId), TravelerTwo);
        SetProperty(conversation, nameof(ChatBotConversation.Title), "Petra packing checklist");
        SetProperty(conversation, nameof(ChatBotConversation.IsActive), true);
        SetProperty(conversation, nameof(ChatBotConversation.LastMessageAt), DateTime.UtcNow.AddMinutes(-10));
        return [conversation];
    }

    private static List<ChatBotMessage> CreateChatMessages()
    {
        var user = CreateEntity<ChatBotMessage>();
        SetProperty(user, nameof(ChatBotMessage.Id), UserMessageId);
        SetProperty(user, nameof(ChatBotMessage.ConversationId), ConversationId);
        SetProperty(user, nameof(ChatBotMessage.IsFromBot), false);
        SetProperty(user, nameof(ChatBotMessage.Message), "What should I bring for a summer Petra tour?");
        SetProperty<decimal?>(user, nameof(ChatBotMessage.Confidence), null);
        SetProperty(user, nameof(ChatBotMessage.Intent), "packing_advice");

        var bot = CreateEntity<ChatBotMessage>();
        SetProperty(bot, nameof(ChatBotMessage.Id), BotMessageId);
        SetProperty(bot, nameof(ChatBotMessage.ConversationId), ConversationId);
        SetProperty(bot, nameof(ChatBotMessage.IsFromBot), true);
        SetProperty(bot, nameof(ChatBotMessage.Message), "Bring water, sunscreen, a hat, and sturdy shoes. Start early to avoid heat.");
        SetProperty(bot, nameof(ChatBotMessage.Confidence), 0.9625m);
        SetProperty(bot, nameof(ChatBotMessage.Intent), "packing_advice");

        return [user, bot];
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
