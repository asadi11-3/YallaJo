using ContentTours.Domain.Enums;
using FluentValidation;

namespace ContentTours.Application.Queries.Tour.ListAdminTours;

public sealed class ListAdminToursQueryValidator : AbstractValidator<ListAdminToursQuery>
{
    private static readonly string[] AllowedSorts = ["newest", "oldest"];

    public ListAdminToursQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || Enum.TryParse<TourStatus>(s, ignoreCase: true, out _))
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<TourStatus>())}.");

        RuleFor(x => x.Sort)
            .Must(s => string.IsNullOrEmpty(s) || AllowedSorts.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Sort must be one of: {string.Join(", ", AllowedSorts)}.");
    }
}
