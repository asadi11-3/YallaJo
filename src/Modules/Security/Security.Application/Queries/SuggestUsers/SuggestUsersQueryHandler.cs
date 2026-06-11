using Security.Application.Queries.Dtos;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Queries.SuggestUsers;

public sealed class SuggestUsersQueryHandler(IUserRepository userRepository)
    : IQueryHandler<SuggestUsersQuery, IReadOnlyList<UserSuggestDto>>
{
    private const int MaxSuggestions = 10;

    public async Task<Result<IReadOnlyList<UserSuggestDto>>> Handle(
        SuggestUsersQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var term = query.Query?.Trim() ?? string.Empty;

            // Too-short fragments would scan the whole table; return an empty
            // suggestion list instead of failing (keeps the typeahead quiet).
            if (term.Length < 2)
            {
                return Result<IReadOnlyList<UserSuggestDto>>.Success([]);
            }

            var users = await userRepository
                .SuggestByEmailAsync(term, MaxSuggestions, cancellationToken)
                .ConfigureAwait(false);

            IReadOnlyList<UserSuggestDto> dtos = users
                .Select(u => new UserSuggestDto(u.Id, u.GetPrimaryEmail()?.Address ?? string.Empty))
                .ToList();

            return Result<IReadOnlyList<UserSuggestDto>>.Success(dtos);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyList<UserSuggestDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
