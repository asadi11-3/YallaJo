using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.DeleteNotificationTemplate;

public sealed record DeleteNotificationTemplateCommand(Guid Id) : IRequest<Result>;
