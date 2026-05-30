using FluentValidation;

namespace ContentTours.Application.Queries.Tour.ListTours;

public sealed class ListToursQueryValidator : AbstractValidator<ListToursQuery>
{
    private static readonly string[] AllowedSorts =
        ["price_asc", "price_desc", "rating_desc", "popularity_desc", "newest"];

    public ListToursQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);

        RuleFor(x => x.Sort)
            .Must(s => string.IsNullOrEmpty(s) || AllowedSorts.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Sort must be one of: {string.Join(", ", AllowedSorts)}.");

        RuleFor(x => x.PlaceId)
            .NotEqual(Guid.Empty)
            .When(x => x.PlaceId.HasValue);
    }
}
