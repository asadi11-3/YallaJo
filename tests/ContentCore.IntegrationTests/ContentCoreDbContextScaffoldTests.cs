using System.Linq;
using ContentCore.Domain.Entities;
using ContentCore.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace YallaJo.ContentCore.IntegrationTests;

/// <summary>
/// Scaffold/mapping tests that verify the Patch 2A FileAsset V2 physical-metadata
/// model is registered in ContentCoreDbContext with the expected table, schema,
/// and indexes. EnsureCreated() is intentionally not used because the EF
/// configuration relies on SQL Server-specific types (varbinary rowversion,
/// filtered indexes); model-level assertions are sufficient and provider-agnostic.
/// </summary>
public sealed class ContentCoreDbContextScaffoldTests
{
    private static ContentCoreDbContext CreateContext()
    {
        // Model-only assertions: configure the real target provider (SQL Server) so the
        // FileAsset configuration (filtered index, rowversion) builds faithfully. No DB
        // connection is opened — only context.Model is inspected.
        var options = new DbContextOptionsBuilder<ContentCoreDbContext>()
            .UseSqlServer("Server=(localdb)\\unused;Database=model-only;Trusted_Connection=True;")
            .Options;

        return new ContentCoreDbContext(options);
    }

    [Fact]
    public void ContentCoreDbContext_ExposesFileAssetsDbSet()
    {
        using var context = CreateContext();

        context.FileAssets.Should().NotBeNull();
    }

    [Fact]
    public void FileAsset_IsMappedTo_ContentCore_FileAssets_Table()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(FileAsset));

        entityType.Should().NotBeNull("FileAsset must be registered in ContentCoreDbContext");
        entityType!.GetTableName().Should().Be("FileAssets");
        entityType.GetSchema().Should().Be("content_core");
    }

    [Fact]
    public void FileAsset_Has_Unique_StorageKey_Index()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(FileAsset))!;

        var storageKeyIndex = entityType.GetIndexes()
            .SingleOrDefault(i => i.GetDatabaseName() == "UX_FileAssets_StorageKey");

        storageKeyIndex.Should().NotBeNull("StorageKey must be uniquely indexed so a physical blob maps to one FileAsset row");
        storageKeyIndex!.IsUnique.Should().BeTrue();
        storageKeyIndex.Properties.Should().ContainSingle()
            .Which.Name.Should().Be(nameof(FileAsset.StorageKey));
    }

    [Fact]
    public void FileAsset_Has_Sha256_Index()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(FileAsset))!;

        var sha256Index = entityType.GetIndexes()
            .SingleOrDefault(i => i.GetDatabaseName() == "IX_FileAssets_Sha256");

        sha256Index.Should().NotBeNull("Sha256 is indexed (filtered) to support future de-duplication lookups");
        sha256Index!.Properties.Should().ContainSingle()
            .Which.Name.Should().Be(nameof(FileAsset.Sha256));
    }

    [Fact]
    public void FileAsset_Has_SoftDelete_QueryFilter()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(FileAsset))!;

        entityType.GetQueryFilter().Should().NotBeNull(
            "FileAsset is soft-deletable; a global query filter must exclude deleted rows");
    }
}
