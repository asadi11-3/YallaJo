using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.UpdateNotificationTemplate;

public sealed record UpdateNotificationTemplateCommand(
    Guid Id,
    byte[] RowVersion,
    string Title,
    string Body,
    string? HtmlBody) : IRequest<Result>;
