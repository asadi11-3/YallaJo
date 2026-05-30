using MediatR;
using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.CreateNotificationTemplate;

public sealed record CreateNotificationTemplateCommand(
    NotificationType Type,
    NotificationChannel Channel,
    string LanguageCode,
    string Title,
    string Body,
    string? HtmlBody) : IRequest<Result<Guid>>;
