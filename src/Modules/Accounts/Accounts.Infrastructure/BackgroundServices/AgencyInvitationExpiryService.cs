using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Accounts.Infrastructure.BackgroundServices;

/// <summary>
/// Expires pending agency invitations that have passed their 7-day TTL.
/// Runs every hour.
/// </summary>
internal sealed class AgencyInvitationExpiryService(
    IServiceScopeFactory scopeFactory,
    IOptions<AgencyInvitationExpiryOptions> options,
    ILogger<AgencyInvitationExpiryService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly AgencyInvitationExpiryOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("AgencyInvitationExpiryService disabled");
            return;
        }

        await Task.Delay(_options.InitialDelay, stoppingToken);
        using var timer = new PeriodicTimer(_options.PollInterval);

        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AgencyInvitationExpiryService tick failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var invitationRepo = scope.ServiceProvider.GetRequiredService<IAgencyInvitationRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IAccountsUnitOfWork>();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var expired = await invitationRepo.GetPendingExpiredAsync(nowUtc, ct);
        if (expired.Count == 0)
            return;

        foreach (var invitation in expired)
        {
            invitation.Expire();
        }

        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Expired {Count} agency invitations past their TTL", expired.Count);
    }
}

public sealed class AgencyInvitationExpiryOptions
{
    public const string SectionName = "Accounts:BackgroundServices:AgencyInvitationExpiry";
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromHours(1);
}
