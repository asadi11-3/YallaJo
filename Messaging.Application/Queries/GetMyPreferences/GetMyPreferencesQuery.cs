using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetMyPreferences;

public sealed record NotificationPreferenceDto(string Type, string Channel, bool IsEnabled);

public sealed record GetMyPreferencesQuery(Guid UserId) : IRequest<Result<IReadOnlyList<NotificationPreferenceDto>>>;
