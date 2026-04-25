using Auth.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.Tests.Unit;

/// <summary>
/// Verifies the OTP active-uniqueness constraint is configured on the EF model.
/// This replaces the non-unique IX_Otps_UserId_Purpose_IsUsed_Active index with
/// a UNIQUE filtered index IX_Otps_UserId_Purpose_Active_Unique — ensuring the
/// DB physically rejects concurrent double-issuance of an active OTP for the
/// same (UserId, Purpose). The model-level assertion covers what the migration
/// emits to SQL Server; an integration test (against a real SQL instance) would
/// additionally verify the insert is rejected at runtime.
/// </summary>
public sealed class OtpUniqueIndexConfigurationTests
{
    private sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options) : DbContext(options)
    {
        public DbSet<Otp> Otps => Set<Otp>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("auth");
            modelBuilder.ApplyConfiguration(
                new global::Auth.Infrastructure.Persistence.Configurations.OtpConfiguration());
        }
    }

    [Fact]
    public void Otp_Should_Have_Unique_Filtered_Active_Index_On_UserId_Purpose()
    {
        var options = new DbContextOptionsBuilder<ProbeDbContext>()
            .UseInMemoryDatabase($"probe-{Guid.NewGuid()}")
            .Options;

        using var db = new ProbeDbContext(options);

        var entity = db.Model.FindEntityType(typeof(Otp));
        entity.Should().NotBeNull();

        var index = entity!.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 2 &&
                i.Properties.Any(p => p.Name == "UserId") &&
                i.Properties.Any(p => p.Name == "Purpose"));

        index.Should().NotBeNull(
            "a composite (UserId, Purpose) index is required for the active-OTP uniqueness contract");
        index!.IsUnique.Should().BeTrue(
            "the active-OTP index must be unique to prevent concurrent double-issuance");
        index.GetFilter().Should().Contain("[IsUsed] = 0", Exactly.Once());
        index.GetFilter().Should().Contain("[IsDeleted] = 0", Exactly.Once());
    }

    [Fact]
    public void Otp_Should_No_Longer_Have_The_Old_NonUnique_Composite_Index()
    {
        var options = new DbContextOptionsBuilder<ProbeDbContext>()
            .UseInMemoryDatabase($"probe-{Guid.NewGuid()}")
            .Options;

        using var db = new ProbeDbContext(options);

        var entity = db.Model.FindEntityType(typeof(Otp))!;

        var oldThreeColumn = entity.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 3 &&
                i.Properties.Any(p => p.Name == "UserId") &&
                i.Properties.Any(p => p.Name == "Purpose") &&
                i.Properties.Any(p => p.Name == "IsUsed"));

        oldThreeColumn.Should().BeNull(
            "the old non-unique 3-column index must be gone after the migration; " +
            "keeping it alongside the new unique 2-column one wastes storage and obscures intent");
    }
}
