using Auth.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Queries.ListLinkedProviders;

/// <summary>
/// Self-only listing of a user's active external provider links. Mirrors the
/// ownership pattern of <see cref="ListSessions.ListActiveSessionsQueryHandler"/>:
/// authentication is required and the caller may only list their own links.
/// Projects to <see cref="LinkedProviderDto"/> — ProviderUserId never leaves the module.
/// </summary>
public sealed class ListLinkedProvidersQueryHandler(
    IExternalProviderRepository externalProviderRepository,
    ICurrentUser currentUser)
    : IQueryHandler<ListLinkedProvidersQuery, IReadOnlyList<LinkedProviderDto>>
{
    public async Task<Result<IReadOnlyList<LinkedProviderDto>>> Handle(
        ListLinkedProvidersQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<IReadOnlyList<LinkedProviderDto>>.Unauthorized(
                "Authentication is required.");
        }

        if (currentUser.UserId.Value != request.UserId)
        {
            return Result<IReadOnlyList<LinkedProviderDto>>.Forbidden(
                "You may only list your own linked providers.");
        }

        var links = await externalProviderRepository.GetAllAsync(
            filter: p => p.UserId == request.UserId && p.IsActive,
            orderBy: q => q.OrderBy(p => p.Provider),
            asNoTracking: true,
            ct: cancellationToken);

        var dtos = links
            .Select(p => new LinkedProviderDto(
                ProviderId: p.Id,
                Provider: p.Provider,
                ProviderEmail: p.ProviderEmail,
                LinkedAt: p.VerifiedAt ?? p.CreatedAt))
            .ToList();

        return Result<IReadOnlyList<LinkedProviderDto>>.Success(dtos);
    }
}
