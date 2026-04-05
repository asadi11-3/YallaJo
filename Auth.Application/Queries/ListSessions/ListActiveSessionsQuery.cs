
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Queries.ListSessions;

public sealed record ListActiveSessionsQuery() : IQuery<IReadOnlyList<ActiveSessionDto>>;
