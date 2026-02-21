using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Accounts.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core tooling (migrations) at design time only.
/// </summary>
internal sealed class AccountsDbContextFactory : IDesignTimeDbContextFactory<AccountsDbContext>
{
    public AccountsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=YallaJo;Trusted_Connection=True;")
            .Options;

        return new AccountsDbContext(options);
    }
}
