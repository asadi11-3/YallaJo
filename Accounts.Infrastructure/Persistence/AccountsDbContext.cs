using Accounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Inbox;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Accounts.Infrastructure.Persistence;

public sealed class AccountsDbContext : DbContext, IDbContext
{
    public AccountsDbContext(DbContextOptions<AccountsDbContext> options)
        : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<ProviderApplication> ProviderApplications => Set<ProviderApplication>();
    public DbSet<ProviderDocument> ProviderDocuments => Set<ProviderDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("accounts");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AccountsDbContext).Assembly,
            type => type.Namespace?.Contains("Accounts.Infrastructure.Persistence.Configurations") ?? false
        );

        base.OnModelCreating(modelBuilder);
    }
}
