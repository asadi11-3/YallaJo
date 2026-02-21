using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;

namespace Auth.Infrastructure.Persistence;

public sealed class AuthDbContext : DbContext, IDbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<UserCredentials> UserCredentials => Set<UserCredentials>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("auth");

        modelBuilder.Entity<UserCredentials>(entity =>
        {
            entity.ToTable("UserCredentials");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // ID comes from Accounts module
            entity.Property(e => e.Email).HasMaxLength(320).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).HasMaxLength(512).IsRequired(false);
            entity.Property(e => e.PasswordSalt).HasMaxLength(256).IsRequired(false);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
