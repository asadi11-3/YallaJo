using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messaging.Infrastructure.BackgroundServices;

public sealed class ReadNotificationCleanupOptions
{
    public const string SectionName = "Messaging:BackgroundServices:ReadNotificationCleanup";

    public bool Enabled { get; init; } = true;
    public DayOfWeek TargetDayOfWeek { get; init; } = DayOfWeek.Sunday;
    public TimeOnly TargetTimeUtc { get; init; } = new(2, 0);
    public int RetentionDays { get; init; } = 30;
    public int MaxPerUser { get; init; } = 500;
}

internal sealed class ReadNotificationCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<ReadNotificationCleanupOptions> options,
    ILogger<ReadNotificationCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Read notification cleanup is disabled.");
            return;
        }

        await Task.Delay(ComputeDelayUntilNext(settings.TargetDayOfWeek, settings.TargetTimeUtc), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromDays(7));
        do
        {
            await CleanupAsync(settings, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CleanupAsync(ReadNotificationCleanupOptions settings, CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
            var now = DateTime.UtcNow;
            var cutoff = now.AddDays(-settings.RetentionDays);
            var criticalTypes = Enum.GetValues<NotificationType>().Where(t => t.IsCritical()).ToArray();

            var oldReadDeleted = await dbContext.Notifications
                .Where(n => n.IsRead && n.ReadAt < cutoff && !criticalTypes.Contains(n.Type))
                .ExecuteDeleteAsync(ct);

            var overCapIds = await dbContext.Notifications
                .Where(n => n.IsRead)
                .GroupBy(n => n.UserId)
                .Where(g => g.Count() > settings.MaxPerUser)
                .SelectMany(g => g.OrderBy(n => n.ReadAt).ThenBy(n => n.CreatedAt).Take(g.Count() - settings.MaxPerUser).Select(n => n.Id))
                .ToListAsync(ct);

            var cappedDeleted = 0;
            if (overCapIds.Count > 0)
            {
                cappedDeleted = await dbContext.Notifications
                    .Where(n => overCapIds.Contains(n.Id))
                    .ExecuteDeleteAsync(ct);
            }

            var staleTokensDeleted = await dbContext.DeviceTokens
                .Where(d => !d.IsDeleted && d.LastSeenAt < now.AddDays(-30))
                .ExecuteDeleteAsync(ct);

            logger.LogInformation(
                "Read notification cleanup deleted {OldReadDeleted} old read notifications, {CappedDeleted} over-cap notifications, {StaleTokensDeleted} stale device tokens.",
                oldReadDeleted,
                cappedDeleted,
                staleTokensDeleted);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Read notification cleanup failed.");
        }
    }

    private static TimeSpan ComputeDelayUntilNext(DayOfWeek targetDay, TimeOnly targetTimeUtc)
    {
        var now = DateTime.UtcNow;
        var target = now.Date.AddDays(((int)targetDay - (int)now.DayOfWeek + 7) % 7)
            .Add(targetTimeUtc.ToTimeSpan());

        if (target <= now)
        {
            target = target.AddDays(7);
        }

        return target - now;
    }
}
