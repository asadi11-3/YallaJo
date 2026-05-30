using Accounts.Contracts.Abstractions;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Infrastructure.Persistence;
using Messaging.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Analytics.Infrastructure.BackgroundServices;

/// <summary>
/// Weekly email digest: Mondays 09:00 UTC for users with MarketingConsent.EmailDigest = true.
/// Sends top 5 recommendations per user via IEmailSender.
/// </summary>
public sealed class EmailDigestBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailDigestBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Only run on Mondays between 09:00 and 10:00 UTC
                if (now.DayOfWeek == DayOfWeek.Monday && now.Hour == 9)
                {
                    await SendDigestAsync(stoppingToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Email digest service failed");
            }

            // Check every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task SendDigestAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var emailLookup = scope.ServiceProvider.GetService<IUserEmailLookupService>();

        // Get top popular items for digest
        var topItems = await context.Set<Domain.Entities.EntityAttributeSnapshot>()
            .Where(s => !s.IsDeleted && (s.Status == "Published" || s.Status == "Approved"))
            .OrderByDescending(s => s.BookingCount)
            .ThenByDescending(s => s.AverageRating)
            .Take(5)
            .ToListAsync(ct);

        if (topItems.Count == 0)
        {
            logger.LogWarning("No items available for email digest");
            return;
        }

        // Build email body
        var itemsHtml = string.Join("",
            topItems.Select(i => $"<li><strong>{i.Name}</strong> — ★{i.AverageRating:F1} ({i.BookingCount} bookings)</li>"));

        var body = $"""
            <h2>Your Weekly YallaJo Picks</h2>
            <p>Here are this week's top recommendations:</p>
            <ul>{itemsHtml}</ul>
            <p>Happy exploring!</p>
            <p style="font-size:12px;color:#999;">
            You're receiving this because you opted in to email digests.
            <a href="https://yallajo.com/settings/notifications">Manage preferences</a>
            </p>
            """;

        if (emailLookup is null)
        {
            logger.LogWarning("IUserEmailLookupService not registered — email digest prepared with {Count} items but cannot send", topItems.Count);
            return;
        }

        var subscribers = await emailLookup.GetDigestSubscribersAsync(1000, ct).ConfigureAwait(false);
        var sent = 0;

        foreach (var subscriber in subscribers)
        {
            try
            {
                var personalBody = body.Replace("Your Weekly", $"{subscriber.DisplayName ?? "Your"} Weekly");
                var message = new EmailMessage(subscriber.Email, "Your Weekly YallaJo Picks", personalBody, IsHtml: true);
                await emailSender.SendAsync(message, ct).ConfigureAwait(false);
                sent++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send digest to {UserId}", subscriber.UserId);
            }
        }

        logger.LogInformation("Email digest sent to {Sent}/{Total} subscribers with {Items} items", sent, subscribers.Count, topItems.Count);
    }
}
