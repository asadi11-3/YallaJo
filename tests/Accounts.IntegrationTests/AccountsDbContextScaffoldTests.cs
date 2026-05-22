using Accounts.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Accounts.IntegrationTests;

/// <summary>
/// Scaffold tests that verify AccountsDbContext can be constructed against SQLite
/// and that all DbSets (including Wave-2 provider application tables) are properly exposed.
/// </summary>
public sealed class AccountsDbContextScaffoldTests
{
    [Fact]
    public void AccountsDbContext_CanConstruct_WithSqliteOptions()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new AccountsDbContext(options);

        context.Should().NotBeNull();
    }

    [Fact]
    public void AccountsDbContext_ExposesCoreDbSets()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new AccountsDbContext(options);

        context.Profiles.Should().NotBeNull();
        context.OutboxMessages.Should().NotBeNull();
        context.InboxMessages.Should().NotBeNull();
    }

    [Fact]
    public void AccountsDbContext_ExposesProviderApplicationDbSets()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new AccountsDbContext(options);

        // Wave-2: provider application module
        context.ProviderApplications.Should().NotBeNull();
        context.ProviderDocuments.Should().NotBeNull();
    }

    /// <summary>
    /// Note: EnsureCreated() is intentionally NOT tested here because the EF configuration
    /// uses SQL Server-specific syntax (nvarchar(max), row_version) that SQLite does not support.
    /// Schema correctness is verified via the real SQL Server migration
    /// (AccountsAddProviderApplicationModule) which is tested by the migration build step.
    /// </summary>
    [Fact]
    public void AccountsDbContext_ModelContainsProviderApplicationEntity()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new AccountsDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(Accounts.Domain.Entities.ProviderApplication));
        entityType.Should().NotBeNull("ProviderApplication must be registered in AccountsDbContext");

        var documentEntityType = context.Model.FindEntityType(typeof(Accounts.Domain.Entities.ProviderDocument));
        documentEntityType.Should().NotBeNull("ProviderDocument must be registered in AccountsDbContext");
    }
}
