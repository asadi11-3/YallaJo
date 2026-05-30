using FluentValidation;

namespace ContentBlogs.Application.Commands.Creator.CreateApplication;

public sealed class CreateCreatorApplicationCommandValidator
    : AbstractValidator<CreateCreatorApplicationCommand>
{
    public CreateCreatorApplicationCommandValidator()
    {
        RuleFor(x => x.Bio!)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Bio));

        RuleFor(x => x.PortfolioUrls!)
            .Must(urls => urls.Count <= 10)
                .WithMessage("Maximum 10 portfolio URLs.")
            .When(x => x.PortfolioUrls is { Count: > 0 });

        RuleFor(x => x.SampleWorkUrls!)
            .Must(urls => urls.Count <= 10)
                .WithMessage("Maximum 10 sample work URLs.")
            .When(x => x.SampleWorkUrls is { Count: > 0 });

        RuleFor(x => x.NicheIds!)
            .Must(ids => ids.Count <= 5)
                .WithMessage("Maximum 5 niches.")
            .When(x => x.NicheIds is { Count: > 0 });

        RuleFor(x => x.FreeTags!)
            .Must(tags => tags.Count <= 20)
                .WithMessage("Maximum 20 free tags.")
            .When(x => x.FreeTags is { Count: > 0 });

        RuleForEach(x => x.FreeTags!)
            .MaximumLength(50)
            .When(x => x.FreeTags is { Count: > 0 });
    }
}
