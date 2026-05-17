using FluentAssertions;
using Messaging.Contracts.Authorization;
using Xunit;

namespace Messaging.Tests.Unit;

public class MessagingPermissionCatalogTests
{
    [Fact]
    public void ModuleName_Should_Be_Messaging()
    {
        var sut = new MessagingPermissionCatalog();
        sut.ModuleName.Should().Be("Messaging");
    }

    [Fact]
    public void Permissions_Should_Not_Be_Empty()
    {
        var sut = new MessagingPermissionCatalog();
        sut.Permissions.Should().NotBeEmpty();
        sut.Permissions.Should().HaveCountGreaterThan(15);
    }
}
