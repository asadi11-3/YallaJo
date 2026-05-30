using FluentValidation;

namespace ContentSeo.Application.Commands.Sitemap.UpdateSitemapEntry;

public sealed class UpdateSitemapEntryCommandValidator : AbstractValidator<UpdateSitemapEntryCommand>
{
    private static readonly string[] AllowedFrequencies = ["always", "hourly", "daily", "weekly", "monthly", "yearly", "never"];

    public UpdateSitemapEntryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        When(x => x.Priority.HasValue, () => RuleFor(x => x.Priority!.Value).InclusiveBetween(0m, 1m));
        When(x => !string.IsNullOrWhiteSpace(x.ChangeFrequency), () =>
        {
            RuleFor(x => x.ChangeFrequency!)
                .Must(value => AllowedFrequencies.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase))
                .WithMessage("ChangeFrequency must be a valid sitemap frequency.");
        });
    }
}
