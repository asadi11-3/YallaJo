using FluentAssertions;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Messaging.IntegrationTests;

public class MessagingDbContextScaffoldTests
{
    [Fact]
    public void DbContext_Should_Expose_Expected_DbSets()
    {
        var options = new DbContextOptionsBuilder<MessagingDbContext>()
            .UseSqlite("Filename=:memory:")
            .Options;

        using var ctx = new MessagingDbContext(options);

        ctx.Notifications.Should().NotBeNull();
        ctx.NotificationPreferences.Should().NotBeNull();
        ctx.NotificationTemplates.Should().NotBeNull();
        ctx.DeviceTokens.Should().NotBeNull();
        ctx.SupportTickets.Should().NotBeNull();
        ctx.OutboxMessages.Should().NotBeNull();
    }
}
