using MediatR;
using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetNotificationTemplates;

public sealed record NotificationTemplateDto(
    Guid Id,
    string Type,
    string Channel,
    string LanguageCode,
    string Title,
    string Body,
    string? HtmlBody,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record GetNotificationTemplatesQuery : IRequest<Result<IReadOnlyList<NotificationTemplateDto>>>;
