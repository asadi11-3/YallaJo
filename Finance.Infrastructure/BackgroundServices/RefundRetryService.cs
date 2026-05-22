using Finance.Application.Interfaces;
using Finance.Contracts.Services;
using Finance.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Finance.Infrastructure.BackgroundServices;

/// <summary>
/// T5: periodic retry sweep for failed refunds (every 15 min by default).
/// </summary>
public sealed class RefundRetryService(
    IServiceScopeFactory scopeFactory,
    IOptions<RefundRetryOptions> options,
    TimeProvider timeProvider,
    ILogger<RefundRetryService> logger) : BackgroundService
{
    private readonly RefundRetryOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("RefundRetryService disabled by config; exiting.");
            return;
        }

        logger.LogInformation(
            "RefundRetryService started. InitialDelay={InitialDelay}, Period={Period}, Cooloff={Cooloff}, MaxRetries={MaxRetries}, BatchSize={BatchSize}.",
            _options.InitialDelay, _options.Period, _options.Cooloff, _options.MaxRetries, _options.BatchSize);

        await Task.Delay(_options.InitialDelay, stoppingToken);

        using var timer = new PeriodicTimer(_options.Period);
        do
        {
            try
            {
                await SweepOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RefundRetryService sweep failed; will retry on next tick.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));

        logger.LogInformation("RefundRetryService stopped.");
    }

    private async Task SweepOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var paymentRepo = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var gateway = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();
        var uow = scope.ServiceProvider.GetRequiredService<IFinanceUnitOfWork>();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var olderThan = now - _options.Cooloff;
        var batch = await paymentRepo.GetPendingRefundsOlderThanAsync(olderThan, _options.MaxRetries, _options.BatchSize, ct);
        if (batch.Count == 0)
        {
            return;
        }

        logger.LogInformation("RefundRetryService found {Count} failed refunds to retry.", batch.Count);

        foreach (var refund in batch)
        {
            try
            {
                refund.IncrementRetryCount();
                var gatewayResult = await gateway.RefundAsync(
                    new RefundRequest(
                        refund.GatewayTransactionId ?? refund.TransactionId ?? "",
                        Math.Abs(refund.Amount.Amount),
                        refund.Currency,
                        "Retry"),
                    ct);

                switch (gatewayResult.Status)
                {
                    case RefundStatus.Completed:
                        refund.StampGatewayRefund(gatewayResult.GatewayRefundId);
                        refund.MarkRefundCompleted(gatewayResult.GatewayRefundId, now);
                        break;
                    case RefundStatus.Pending:
                        // Webhook will finalize
                        break;
                    case RefundStatus.Failed:
                        refund.MarkRefundFailed(gatewayResult.FailureCode ?? "Gateway.Failed", now);
                        if (refund.RetryCount >= _options.MaxRetries)
                        {
                            refund.RaiseRefundFinallyFailed(gatewayResult.FailureCode ?? "Gateway.Failed", now);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Refund retry for payment {PaymentId} threw; will retry next sweep.", refund.Id);
                refund.MarkRefundFailed("Retry.Exception", now);
            }
        }

        await uow.SaveChangesAsync(ct);
    }
}

public sealed class RefundRetryOptions
{
    public const string SectionName = "Finance:BackgroundServices:RefundRetry";
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan Period { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan Cooloff { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxRetries { get; set; } = 3;
    public int BatchSize { get; set; } = 200;
}
