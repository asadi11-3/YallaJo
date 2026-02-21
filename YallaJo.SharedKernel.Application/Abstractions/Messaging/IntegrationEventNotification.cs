using MediatR;
using YallaJo.SharedKernel.Domain.Event;

namespace YallaJo.SharedKernel.Application.Abstractions.Messaging
{
    /// <summary>
    /// Wraps an integration event as a MediatR INotification so the OutboxProcessor
    /// can publish it in-process via MediatR.
    /// Integration event handlers implement: INotificationHandler&lt;IntegrationEventNotification&lt;TEvent&gt;&gt;
    /// </summary>
    public sealed record IntegrationEventNotification<TEvent>(TEvent Event) : INotification
        where TEvent : IIntegrationEvent;
}
