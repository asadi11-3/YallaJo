using System.Linq;
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

    [Fact]
    public void AccountsDbContext_ExposesProviderDocumentFilesDbSet()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new AccountsDbContext(options);

        // Patch 2A: provider-document -> FileAsset ownership link table
        context.ProviderDocumentFiles.Should().NotBeNull();
    }

    [Fact]
    public void ProviderDocumentFile_IsMappedTo_Accounts_ProviderDocumentFiles_Table()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new AccountsDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(Accounts.Domain.Entities.ProviderDocumentFile));

        entityType.Should().NotBeNull("ProviderDocumentFile must be registered in AccountsDbContext");
        entityType!.GetTableName().Should().Be("ProviderDocumentFiles");
        entityType.GetSchema().Should().Be("accounts");
    }

    [Fact]
    public void ProviderDocumentFile_Has_Unique_ProviderDocumentId_Index()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new AccountsDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(Accounts.Domain.Entities.ProviderDocumentFile))!;

        var providerDocumentIndex = entityType.GetIndexes()
            .SingleOrDefault(i => i.GetDatabaseName() == "UX_ProviderDocumentFiles_ProviderDocumentId");

        providerDocumentIndex.Should().NotBeNull(
            "one ProviderDocument must map to exactly one current FileAsset (unique ProviderDocumentId)");
        providerDocumentIndex!.IsUnique.Should().BeTrue();
        providerDocumentIndex.Properties.Should().ContainSingle()
            .Which.Name.Should().Be("ProviderDocumentId");
    }

    [Fact]
    public void ProviderDocumentFile_Has_NonUnique_FileAssetId_Index()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new AccountsDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(Accounts.Domain.Entities.ProviderDocumentFile))!;

        var fileAssetIndex = entityType.GetIndexes()
            .SingleOrDefault(i => i.GetDatabaseName() == "IX_ProviderDocumentFiles_FileAssetId");

        fileAssetIndex.Should().NotBeNull("FileAssetId is indexed for reverse lookups");
        fileAssetIndex!.IsUnique.Should().BeFalse("FileAssetId index must be non-unique");
        fileAssetIndex.Properties.Should().ContainSingle()
            .Which.Name.Should().Be("FileAssetId");
    }

    [Fact]
    public void ProviderDocumentFile_HasNo_ForeignKey_Into_FileAssets()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new AccountsDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(Accounts.Domain.Entities.ProviderDocumentFile))!;

        // Cross-module integrity to content_core.FileAssets is enforced in the app layer,
        // never via a database FK (module boundary). The only FK is the in-module
        // FileAssetId-less link to ProviderDocuments.
        entityType.GetForeignKeys()
            .Should().NotContain(fk => fk.Properties.Any(p => p.Name == "FileAssetId"),
                "FileAssetId must remain a plain Guid with no cross-module database FK");
    }
}
