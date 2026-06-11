using Security.Application.Queries.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Queries.LookupUsers;

/// <summary>
/// Typeahead/batch user lookup (B1). Requires a query term (min 2 chars) and/or
/// an explicit id set; the endpoint rejects bare requests with 400.
/// Not cacheable: results are small, user-specific and change frequently.
/// </summary>
public sealed record LookupUsersQuery(
    string? Query,
    IReadOnlyList<Guid>? Ids,
    int Limit) : IQuery<IReadOnlyList<UserLookupDto>>;
