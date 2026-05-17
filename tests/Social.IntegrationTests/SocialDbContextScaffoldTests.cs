using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Social.Infrastructure.Persistence;
using Xunit;

namespace Social.IntegrationTests;

public sealed class SocialDbContextScaffoldTests
{
    [Fact]
    public void DbContext_Can_Be_Constructed()
    {
        var options = new DbContextOptionsBuilder<SocialDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        using var ctx = new SocialDbContext(options);
        ctx.Should().NotBeNull();
    }
}
