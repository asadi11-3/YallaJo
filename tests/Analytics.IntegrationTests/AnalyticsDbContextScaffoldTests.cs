using Analytics.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Analytics.IntegrationTests;

public class AnalyticsDbContextScaffoldTests
{
    [Fact]
    public void DbContext_ShouldExposeAllDbSets()
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase("analytics-scaffold-test")
            .Options;

        using var context = new AnalyticsDbContext(options);

        context.AuditLogs.Should().NotBeNull();
        context.UserInteractions.Should().NotBeNull();
        context.UserPreferences.Should().NotBeNull();
        context.PopularityScores.Should().NotBeNull();
        context.RecommendationCaches.Should().NotBeNull();
        context.OutboxMessages.Should().NotBeNull();
    }
}
