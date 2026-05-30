using Messaging.Contracts.Services;
using Messaging.Domain.Enums;
using Messaging.Domain.Repositories;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messaging.Infrastructure.BackgroundServices;

public sealed class EmailNotificationSenderOptions
{
    public const string SectionName = "Messaging:BackgroundServices:EmailSender";

    public bool Enabled { get; init; } = true;
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMinutes(2);
    public int MaxRetries { get; init; } = 3;
    public int BatchSize { get; init; } = 50;
}

internal sealed class EmailNotificationSenderService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<EmailNotificationSenderOptions> options,
    ILogger<EmailNotificationSenderService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Email notification sender is disabled.");
            return;
        }

        await Task.Delay(settings.InitialDelay, stoppingToken);

        using var timer = new PeriodicTimer(settings.Interval);
        do
        {
            await ProcessBatchAsync(settings, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(EmailNotificationSenderOptions settings, CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
            var userSnapshots = scope.ServiceProvider.GetRequiredService<IUserSnapshotRepository>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var attempts = await dbContext.NotificationDeliveryAttempts
                .Where(a => a.Channel == NotificationChannel.Email
                    && a.Status == NotificationDeliveryStatus.Pending
                    && a.AttemptedAt <= now)
                .OrderBy(a => a.AttemptedAt)
                .Take(settings.BatchSize)
                .ToListAsync(ct);

            foreach (var attempt in attempts)
            {
                await ProcessAttemptAsync(dbContext, userSnapshots, emailSender, attempt.Id, settings, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email notification sender batch failed.");
        }
    }

    private async Task ProcessAttemptAsync(
        MessagingDbContext dbContext,
        IUserSnapshotRepository userSnapshots,
        IEmailSender emailSender,
        Guid attemptId,
        EmailNotificationSenderOptions settings,
        CancellationToken ct)
    {
        var attempt = await dbContext.NotificationDeliveryAttempts.FirstAsync(a => a.Id == attemptId, ct);
        var notification = await dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == attempt.NotificationId, ct);
        if (notification is null)
        {
            attempt.MarkFailed(timeProvider, "Notification not found.");
            await dbContext.SaveChangesAsync(ct);
            return;
        }

        var snapshot = await userSnapshots.GetByUserIdAsync(notification.UserId, ct);
        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.Email))
        {
            await MarkFailedOrRetryAsync(dbContext, attempt, notification, "User email snapshot not found.", settings, ct);
            return;
        }

        var result = await emailSender.SendAsync(new EmailMessage(snapshot.Email, notification.Title, notification.Body), ct);
        if (result.Success)
        {
            attempt.MarkSucceeded(timeProvider, result.ProviderMessageId);
            notification.MarkSent(timeProvider, result.ProviderMessageId);
            await dbContext.SaveChangesAsync(ct);
            return;
        }

        await MarkFailedOrRetryAsync(dbContext, attempt, notification, result.Error ?? "Email send failed.", settings, ct);
    }

    private async Task MarkFailedOrRetryAsync(
        MessagingDbContext dbContext,
        Messaging.Domain.Entities.NotificationDeliveryAttempt attempt,
        Messaging.Domain.Entities.Notification notification,
        string reason,
        EmailNotificationSenderOptions settings,
        CancellationToken ct)
    {
        attempt.MarkFailed(timeProvider, reason);

        if (attempt.AttemptNumber >= settings.MaxRetries)
        {
            notification.MarkFailed(reason);
            logger.LogWarning(
                "Email notification {NotificationId} permanently failed after {AttemptNumber} attempts: {Reason}",
                notification.Id,
                attempt.AttemptNumber,
                reason);
        }
        else
        {
            var nextAttempt = Messaging.Domain.Entities.NotificationDeliveryAttempt.Create(
                notification.Id,
                NotificationChannel.Email,
                timeProvider,
                attempt.AttemptNumber + 1);

            dbContext.NotificationDeliveryAttempts.Add(nextAttempt);
            dbContext.Entry(nextAttempt).Property(a => a.AttemptedAt).CurrentValue = timeProvider.GetUtcNow().UtcDateTime.Add(GetBackoff(attempt.AttemptNumber));
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static TimeSpan GetBackoff(int attemptNumber) => attemptNumber switch
    {
        1 => TimeSpan.FromMinutes(1),
        2 => TimeSpan.FromMinutes(5),
        _ => TimeSpan.FromMinutes(15),
    };
}
