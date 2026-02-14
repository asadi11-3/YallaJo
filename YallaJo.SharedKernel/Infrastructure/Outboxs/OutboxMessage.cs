using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Event;

namespace YallaJo.SharedKernel.Infrastructure.Outbox
{
   
    public sealed class OutboxMessage
    {
        public Guid Id { get; private set; }

        /// <summary>
        /// نوع الـ Event
        /// </summary>
        public string Type { get; private set; } = string.Empty;

        /// <summary>
        /// محتوى الـ Event (JSON)
        /// </summary>
        public string Content { get; private set; } = string.Empty;

        /// <summary>
        /// متى تم إنشاؤه
        /// </summary>
        public DateTime OccurredOnUtc { get; private set; }

        /// <summary>
        /// متى تم معالجته (null = لسه ما انعالج)
        /// </summary>
        public DateTime? ProcessedOnUtc { get; private set; }

        /// <summary>
        /// رسالة خطأ (لو فشل)
        /// </summary>
        public string? Error { get; private set; }

        /// <summary>
        /// عدد المحاولات
        /// </summary>
        public int RetryCount { get; private set; }

        private OutboxMessage() { }

        public static OutboxMessage Create(IIntegrationEvent integrationEvent)
        {
            return new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = integrationEvent.GetType().AssemblyQualifiedName!,
                Content = JsonSerializer.Serialize(
                    integrationEvent,
                    integrationEvent.GetType()),
                OccurredOnUtc = integrationEvent.OccurredOn,
                ProcessedOnUtc = null,
                Error = null,
                RetryCount = 0
            };
        }

        public void MarkAsProcessed()
        {
            ProcessedOnUtc = DateTime.UtcNow;
        }

        public void MarkAsFailed(string error)
        {
            Error = error;
            RetryCount++;
        }
    }
}
