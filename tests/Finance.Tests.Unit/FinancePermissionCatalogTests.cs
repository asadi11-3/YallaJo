using Finance.Contracts.Authorization;
using FluentAssertions;
using Xunit;

namespace Finance.Tests.Unit;

public sealed class FinancePermissionCatalogTests
{
    [Fact]
    public void ModuleName_ShouldBeFinance()
    {
        new FinancePermissionCatalog().ModuleName.Should().Be("Finance");
    }

    [Fact]
    public void Permissions_ShouldNotBeEmpty()
    {
        new FinancePermissionCatalog().Permissions.Should().NotBeEmpty();
    }

    [Fact]
    public void Permissions_ShouldAllUseFinanceOperationsGroup()
    {
        new FinancePermissionCatalog()
            .Permissions
            .Should()
            .OnlyContain(p => p.Group == "FinanceOperations");
    }
}
