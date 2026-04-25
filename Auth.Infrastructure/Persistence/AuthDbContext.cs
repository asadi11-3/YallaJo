using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Inbox;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Auth.Infrastructure.Persistence;

public sealed class AuthDbContext : DbContext, IDbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
    : base(options)
    {
    }

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Otp> Otps => Set<Otp>();
    public DbSet<ActivationToken> ActivationTokens => Set<ActivationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<ExternalProvider> ExternalProviders => Set<ExternalProvider>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("auth");

        // New entities — apply file-based configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AuthDbContext).Assembly,
            type => type.Namespace?.Contains("Auth.Infrastructure.Persistence.Configurations") ?? false
        );

        base.OnModelCreating(modelBuilder);
    }
}
