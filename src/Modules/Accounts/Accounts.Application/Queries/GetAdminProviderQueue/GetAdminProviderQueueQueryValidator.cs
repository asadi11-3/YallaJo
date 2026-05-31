using FluentValidation;

namespace Accounts.Application.Queries.GetAdminProviderQueue;

public sealed class GetAdminProviderQueueQueryValidator : AbstractValidator<GetAdminProviderQueueQuery>
{
    public GetAdminProviderQueueQueryValidator()
    {
        RuleFor(x => x.TypeFilter).IsInEnum().When(x => x.TypeFilter.HasValue);
    }
}
