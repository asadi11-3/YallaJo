using MediatR;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetNotificationTemplates;

internal sealed class GetNotificationTemplatesQueryHandler(
    INotificationTemplateRepository templateRepository) : IRequestHandler<GetNotificationTemplatesQuery, Result<IReadOnlyList<NotificationTemplateDto>>>
{
    public async Task<Result<IReadOnlyList<NotificationTemplateDto>>> Handle(GetNotificationTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await templateRepository.ListAllAsync(cancellationToken);
        var dtos = templates.Select(t => new NotificationTemplateDto(
            t.Id, t.Type.ToString(), t.Channel.ToString(), t.LanguageCode,
            t.Title, t.Body, t.HtmlBody, t.CreatedAt, t.UpdatedAt,
            t.RowVersion is null or { Length: 0 } ? string.Empty : Convert.ToBase64String(t.RowVersion))).ToList();
        return Result.Success<IReadOnlyList<NotificationTemplateDto>>(dtos);
    }
}
