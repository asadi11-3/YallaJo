using ContentCore.Application.Promotions;
using FluentValidation;

namespace ContentCore.Application.Commands.PromoBlock.UpdatePromoBlock;

public sealed class UpdatePromoBlockCommandValidator : AbstractValidator<UpdatePromoBlockCommand>
{
    public UpdatePromoBlockCommandValidator()
    {
        RuleFor(x => x.PlacementKey)
            .NotEmpty().WithMessage("Placement key is required.")
            .MaximumLength(100);

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description is not null);

        RuleFor(x => x.ButtonText)
            .MaximumLength(100)
            .When(x => x.ButtonText is not null);

        RuleFor(x => x.ButtonUrl)
            .MaximumLength(2048)
            .When(x => x.ButtonUrl is not null);

        // Defence-in-depth: only site-relative or http/https absolute links are allowed.
        RuleFor(x => x.ButtonUrl)
            .Must(PromoLinkPolicy.IsSafe)
            .WithMessage("The link must be a site path or a valid http(s) URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.ButtonUrl));

        // A CTA URL without text (or vice-versa) is almost always a mistake.
        RuleFor(x => x.ButtonText)
            .NotEmpty().WithMessage("Button text is required when a link is provided.")
            .When(x => !string.IsNullOrWhiteSpace(x.ButtonUrl));

        RuleFor(x => x.BadgeText)
            .MaximumLength(60)
            .When(x => x.BadgeText is not null);

        RuleFor(x => x.IconName)
            .MaximumLength(60)
            .When(x => x.IconName is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.EndsAt)
            .GreaterThanOrEqualTo(x => x.StartsAt!.Value)
            .WithMessage("End date must be on or after the start date.")
            .When(x => x.StartsAt.HasValue && x.EndsAt.HasValue);
    }
}
