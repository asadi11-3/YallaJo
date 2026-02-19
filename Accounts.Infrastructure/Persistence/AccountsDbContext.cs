using Accounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Domain.Entities;

namespace Accounts.Infrastructure.Persistence
{
    public class AccountsDbContext : DbContext, IDbContext
    {
        public AccountsDbContext(DbContextOptions<AccountsDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<UserEmail> UserEmails => Set<UserEmail>();
        public DbSet<UserPhone> UserPhones => Set<UserPhone>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("accounts");

            // Apply all configurations from this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(AccountsDbContext).Assembly,
                type => type.Namespace?.Contains("Accounts.Infrastructure.Persistence.Configurations") ?? false
            );

            base.OnModelCreating(modelBuilder);
        }

      
    }
}
