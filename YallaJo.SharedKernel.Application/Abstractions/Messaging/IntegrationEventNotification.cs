using MediatR;
using YallaJo.SharedKernel.Domain.Event;

namespace YallaJo.SharedKernel.Application.Abstractions.Messaging
{
    /// <summary>
    /// Wraps an integration event as a MediatR INotification so the OutboxProcessor
    /// can publish it in-process via MediatR.
    /// MessageId is the OutboxMessage.Id — handlers use it to record in their Inbox (idempotency).
    /// Integration event handlers implement: INotificationHandler&lt;IntegrationEventNotification&lt;TEvent&gt;&gt;
    /// </summary>
    public sealed record IntegrationEventNotification<TEvent>(Guid MessageId, TEvent Event) : INotification
        where TEvent : IIntegrationEvent;
}
