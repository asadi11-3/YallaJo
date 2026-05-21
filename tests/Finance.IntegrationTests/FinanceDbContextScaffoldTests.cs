using Finance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finance.IntegrationTests;

public sealed class FinanceDbContextScaffoldTests
{
    [Fact]
    public void FinanceDbContext_ShouldExposePaymentsDbSet()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var context = new FinanceDbContext(options);
        context.Payments.Should().NotBeNull();
        context.Payouts.Should().NotBeNull();
        context.Invoices.Should().NotBeNull();
        context.OutboxMessages.Should().NotBeNull();
    }
}
