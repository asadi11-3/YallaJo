using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;
//using YallaJo.SharedKernel.Domain.Event;
//using YallaJo.SharedKernel.Infrastructure.Outbox;

//namespace YallaJo.SharedKernel.Infrastructure.BackgroundJobs
//{
   
//    public sealed class OutboxProcessor : BackgroundService
//    {
//        private readonly IServiceProvider _serviceProvider;
//        private readonly ILogger<OutboxProcessor> _logger;
//        private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

//        public OutboxProcessor(
//            IServiceProvider serviceProvider,
//            ILogger<OutboxProcessor> logger)
//        {
//            _serviceProvider = serviceProvider;
//            _logger = logger;
//        }

        //protected override async Task ExecuteAsync(CancellationToken ct)
        //{
        //    _logger.LogInformation("Outbox Processor started");

        //    while (!ct.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            await ProcessOutboxMessagesAsync(ct);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Error processing outbox messages");
        //        }

        //        await Task.Delay(_interval, ct);
        //    }

        //    _logger.LogInformation("Outbox Processor stopped");
        //}

        //private async Task ProcessOutboxMessagesAsync(CancellationToken ct)
        //{
        //    using var scope = _serviceProvider.CreateScope();

        //    var dbContext = scope.ServiceProvider
        //        .GetRequiredService<ApplicationDbContext>();

        //    var eventBus = scope.ServiceProvider
        //        .GetRequiredService<IEventBus>();

        //    // ═══════════════════════════════════════
        //    // 1. جيب الرسائل اللي ما انعالجت
        //    // ═══════════════════════════════════════

        //    var messages = await dbContext
        //        .Set<OutboxMessage>()
        //        .Where(m => m.ProcessedOnUtc == null)
        //        .Where(m => m.RetryCount < 3) // max 3 retries
        //        .OrderBy(m => m.OccurredOnUtc)
        //        .Take(20) // معالجة 20 رسالة بالمرة
        //        .ToListAsync(ct);

        //    if (!messages.Any())
        //        return;

        //    _logger.LogInformation(
        //        "Processing {Count} outbox messages",
        //        messages.Count);

        //    // ═══════════════════════════════════════
        //    // 2. عالج كل رسالة
        //    // ═══════════════════════════════════════

        //    foreach (var message in messages)
        //    {
        //        try
        //        {
        //            // Deserialize الـ Event
        //            var eventType = Type.GetType(message.Type);

        //            if (eventType is null)
        //            {
        //                _logger.LogWarning(
        //                    "Unknown event type: {Type}",
        //                    message.Type);
        //                continue;
        //            }

        //            var integrationEvent = JsonSerializer.Deserialize(
        //                message.Content,
        //                eventType) as IIntegrationEvent;

        //            if (integrationEvent is null)
        //            {
        //                _logger.LogWarning(
        //                    "Failed to deserialize event {Id}",
        //                    message.Id);
        //                continue;
        //            }

        //            // ✅ انشر Event
        //            await eventBus.PublishAsync(integrationEvent, ct);

        //            // ✅ علمه كـ processed
        //            message.MarkAsProcessed();

        //            _logger.LogInformation(
        //                "Processed outbox message {Id}",
        //                message.Id);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(
        //                ex,
        //                "Failed to process outbox message {Id}",
        //                message.Id);

        //            message.MarkAsFailed(ex.Message);
        //        }
        //    }

        //    // ═══════════════════════════════════════
        //    // 3. احفظ التغييرات
        //    // ═══════════════════════════════════════

        //    await dbContext.SaveChangesAsync(ct);
//        //}
//    }
//}
