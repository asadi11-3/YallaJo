using Accounts.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Accounts.Infrastructure.Security;

internal sealed class SecurityUserExistenceChecker(IConfiguration configuration) : ISecurityUserExistenceChecker
{
    private readonly string _connectionString = configuration.GetConnectionString("SecurityConnection")
        ?? throw new InvalidOperationException("Connection string 'SecurityConnection' is not configured.");

    public async Task<bool> ExistsAsync(Guid userId, CancellationToken ct = default)
    {
        // Per architecture rules: No raw SQL string. Use EF Core DbContext to query the other database safely without tracking.
        var optionsBuilder = new DbContextOptionsBuilder<ReadOnlySecurityDbContext>()
            .UseSqlServer(_connectionString)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        using var context = new ReadOnlySecurityDbContext(optionsBuilder.Options);

        return await context.Users.AnyAsync(u => u.Id == userId && !u.IsDeleted, ct);
    }

    // A minimal DbContext localized to this checking need.
    private class ReadOnlySecurityDbContext : DbContext
    {
        public ReadOnlySecurityDbContext(DbContextOptions<ReadOnlySecurityDbContext> options) : base(options) { }

        public DbSet<SecurityUserStub> Users => Set<SecurityUserStub>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("security");
            modelBuilder.Entity<SecurityUserStub>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
            });
        }
    }

    private class SecurityUserStub
    {
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; }
    }
}
