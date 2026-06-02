using MediatR;
using Messaging.Application.Caching;
using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
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
    DateTime? UpdatedAt,
    string RowVersion);

public sealed record GetNotificationTemplatesQuery : IRequest<Result<IReadOnlyList<NotificationTemplateDto>>>, ICacheableQuery
{
    public string CacheKey => MessagingCacheKeys.NotificationTemplates;
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags => [MessagingCacheKeys.NotificationTemplatesTag];
}
