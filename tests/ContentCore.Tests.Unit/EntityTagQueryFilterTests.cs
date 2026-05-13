using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Infrastructure.Persistence;
using ContentCore.Infrastructure.Persistence.Configurations;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ContentCore.Tests.Unit;

// ── Minimal test DbContext ────────────────────────────────────────────────────

/// <summary>
/// Minimal DbContext that exercises only the two production EF configurations
/// relevant to CONTENTCORE-STD-P1-002.
///
/// WHY NOT use <see cref="ContentCoreDbContext"/> directly:
///   <see cref="OutboxMessageConfiguration"/> calls
///   <c>HasColumnType("nvarchar(max)")</c> — a SQL Server-specific column type
///   that causes SQLite to throw "near 'max': syntax error" when
///   <c>EnsureCreated()</c> is called for schema generation. All other ContentCore
///   configurations are SQLite-compatible; it is safe to apply only the two that
///   matter for this filter test.
///
/// The production <see cref="TagConfiguration"/> and
/// <see cref="EntityTagConfiguration"/> classes are applied verbatim —
/// the test exercises real production artifacts, not copies.
/// </summary>
internal sealed class TagFilterTestContext(DbContextOptions<TagFilterTestContext> options)
    : DbContext(options)
{
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<EntityTag> EntityTags => Set<EntityTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new TagConfiguration().Configure(modelBuilder.Entity<Tag>());
        new EntityTagConfiguration().Configure(modelBuilder.Entity<EntityTag>());

        // SQLite workaround — [Timestamp] on AuditableEntity.RowVersion
        // ---------------------------------------------------------------
        // EF Core convention for [Timestamp] sets ValueGeneratedOnAddOrUpdate(),
        // which causes EF Core to OMIT RowVersion from INSERT statements because it
        // expects the database to supply the value (as SQL Server's rowversion does).
        // SQLite has no such mechanism, so the column stays null and the NOT NULL
        // constraint throws "SQLite Error 19: NOT NULL constraint failed: Tags.RowVersion".
        //
        // Overriding to ValueGeneratedNever() tells EF Core to include the C# default
        // value (byte[]{} = empty blob) in INSERT statements, satisfying the constraint.
        // This does not affect filter behavior — we are testing filter semantics only.
        modelBuilder.Entity<Tag>()
            .Property(x => x.RowVersion)
            .ValueGeneratedNever();

        base.OnModelCreating(modelBuilder);
    }
}

// ── EntityTag query filter tests (CONTENTCORE-STD-P1-002) ────────────────────

/// <summary>
/// SQL-level regression tests for <see cref="EntityTagConfiguration"/>'s global
/// EF Core query filter: <c>builder.HasQueryFilter(x => !x.Tag.IsDeleted);</c>
///
/// xUnit creates a new instance per [Fact], so every test gets its own isolated
/// SQLite in-memory database — no cross-test pollution possible.
/// </summary>
public sealed class EntityTagQueryFilterTests : IDisposable
{
    private readonly TagFilterTestContext _ctx;
    private readonly SqliteConnection _conn;

    public EntityTagQueryFilterTests()
    {
        // Keep connection open for the lifetime of this test instance.
        // SQLite in-memory databases are destroyed when the last connection closes.
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opts = new DbContextOptionsBuilder<TagFilterTestContext>()
            .UseSqlite(_conn)
            .Options;

        _ctx = new TagFilterTestContext(opts);
        _ctx.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _conn.Dispose(); // also calls Close()
    }

    // ── active parent Tag ─────────────────────────────────────────────────────

    [Fact]
    public async Task EntityTag_ShouldBeReturned_WhenParentTagIsActive()
    {
        var entityId = Guid.NewGuid();
        var tag = Tag.Create("Adventure", "adventure", "en");
        // IsDeleted = false by default

        _ctx.Tags.Add(tag);
        _ctx.EntityTags.Add(EntityTag.Create(EntityType.Tour, entityId, tag.Id));
        await _ctx.SaveChangesAsync();

        var result = await _ctx.EntityTags
            .Include(et => et.Tag)
            .Where(et => et.EntityId == entityId)
            .ToListAsync();

        result.Should().ContainSingle("an EntityTag whose parent Tag is active must survive the query filter");
        result[0].TagId.Should().Be(tag.Id);
    }

    // ── soft-deleted parent Tag ───────────────────────────────────────────────

    [Fact]
    public async Task EntityTag_ShouldBeExcluded_WhenParentTagIsSoftDeleted()
    {
        var entityId = Guid.NewGuid();
        var tag = Tag.Create("Culture", "culture", "en");
        tag.SoftDelete(); // IsDeleted = true — filter must exclude the EntityTag

        _ctx.Tags.Add(tag);
        _ctx.EntityTags.Add(EntityTag.Create(EntityType.Tour, entityId, tag.Id));
        await _ctx.SaveChangesAsync();

        var result = await _ctx.EntityTags
            .Include(et => et.Tag)
            .Where(et => et.EntityId == entityId)
            .ToListAsync();

        result.Should().BeEmpty(
            "HasQueryFilter(x => !x.Tag.IsDeleted) must exclude any EntityTag whose parent Tag.IsDeleted == true");
    }

    // ── mixed: one active, one soft-deleted ───────────────────────────────────

    [Fact]
    public async Task EntityTag_ShouldReturnOnlyActiveTag_WhenEntityHasMixedActiveAndSoftDeletedParentTags()
    {
        var entityId = Guid.NewGuid();
        var activeTag = Tag.Create("Nature", "nature", "en");
        var deletedTag = Tag.Create("Removed", "removed", "en");
        deletedTag.SoftDelete();

        _ctx.Tags.AddRange(activeTag, deletedTag);
        _ctx.EntityTags.AddRange(
            EntityTag.Create(EntityType.Tour, entityId, activeTag.Id),
            EntityTag.Create(EntityType.Tour, entityId, deletedTag.Id));
        await _ctx.SaveChangesAsync();

        var result = await _ctx.EntityTags
            .Include(et => et.Tag)
            .Where(et => et.EntityId == entityId)
            .ToListAsync();

        result.Should().ContainSingle(
            "only the EntityTag linked to the active Tag must survive the filter when parent Tags are mixed");
        result[0].TagId.Should().Be(activeTag.Id);
    }
}

// ── EntityCategory parity (EF model-metadata assertions) ─────────────────────

/// <summary>
/// Parity check confirming that both <see cref="EntityTagConfiguration"/> and
/// <see cref="EntityCategoryConfiguration"/> have their global query filters
/// registered in the <em>production</em> <see cref="ContentCoreDbContext"/> model.
///
/// Implementation note: <see cref="ContentCoreDbContext"/> is used for model
/// inspection only — <c>EnsureCreated()</c> is intentionally NOT called
/// (<c>nvarchar(max)</c> in OutboxMessageConfiguration would fail SQLite DDL).
/// Accessing <c>context.Model</c> triggers EF Core's model-building pipeline
/// (all configurations applied) without executing any SQL.
/// </summary>
public sealed class EntityTagAndCategoryFilterModelTests
{
    [Fact]
    public void EntityTagConfiguration_ShouldRegisterQueryFilter_InProductionEFModel()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var opts = new DbContextOptionsBuilder<ContentCoreDbContext>()
            .UseSqlite(conn)
            .Options;

        using var context = new ContentCoreDbContext(opts);

        // Accessing context.Model triggers OnModelCreating — no DDL generated
        var entityType = context.Model.FindEntityType(typeof(EntityTag));
        entityType.Should().NotBeNull();

        var filter = entityType!.GetQueryFilter();
        filter.Should().NotBeNull(
            "EntityTagConfiguration must call HasQueryFilter(x => !x.Tag.IsDeleted)");
        filter!.ToString().Should().Contain("IsDeleted",
            "the filter expression must reference Tag.IsDeleted");
    }

    [Fact]
    public void EntityCategoryConfiguration_ShouldRegisterQueryFilter_InProductionEFModel()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var opts = new DbContextOptionsBuilder<ContentCoreDbContext>()
            .UseSqlite(conn)
            .Options;

        using var context = new ContentCoreDbContext(opts);

        var entityType = context.Model.FindEntityType(typeof(EntityCategory));
        entityType.Should().NotBeNull();

        var filter = entityType!.GetQueryFilter();
        filter.Should().NotBeNull(
            "EntityCategoryConfiguration must call HasQueryFilter(x => !x.Category.IsDeleted)");
        filter!.ToString().Should().Contain("IsDeleted",
            "the filter expression must reference Category.IsDeleted");
    }
}
