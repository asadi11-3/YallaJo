using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Auth.Infrastructure.Persistence;

/// <summary>Used by EF Core tooling (migrations) at design time only.</summary>
internal sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=YallaJo;Trusted_Connection=True;")
            .Options;

        return new AuthDbContext(options);
    }
}
