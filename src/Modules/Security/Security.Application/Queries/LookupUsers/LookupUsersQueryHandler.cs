using Security.Application.Queries.Dtos;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Queries.LookupUsers;

public sealed class LookupUsersQueryHandler(IUserRepository userRepository)
    : IQueryHandler<LookupUsersQuery, IReadOnlyList<UserLookupDto>>
{
    public async Task<Result<IReadOnlyList<UserLookupDto>>> Handle(
        LookupUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await userRepository.SearchAsync(
            request.Query, request.Ids, request.Limit, cancellationToken);

        IReadOnlyList<UserLookupDto> dtos = users.Select(user =>
        {
            var email = user.GetPrimaryEmail()?.Address;
            return new UserLookupDto(
                user.Id,
                DisplayNameFrom(email, user.Id),
                email,
                AvatarUrl: null);
        }).ToList();

        return Result<IReadOnlyList<UserLookupDto>>.Success(dtos);
    }

    /// <summary>
    /// Security module stores no profile names; derive a friendly label from the
    /// primary-email local-part, falling back to a shortened id.
    /// </summary>
    private static string DisplayNameFrom(string? email, Guid id)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var at = email.IndexOf('@');
            var local = at > 0 ? email[..at] : email;
            if (local.Length > 0)
                return local;
        }

        return id.ToString("N")[..8];
    }
}
