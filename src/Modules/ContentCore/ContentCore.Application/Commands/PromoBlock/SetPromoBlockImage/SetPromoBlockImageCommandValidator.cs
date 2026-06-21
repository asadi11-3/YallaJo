using FluentValidation;

namespace ContentCore.Application.Commands.PromoBlock.SetPromoBlockImage;

public sealed class SetPromoBlockImageCommandValidator : AbstractValidator<SetPromoBlockImageCommand>
{
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly string[] AllowedContentTypes =
    {
        "image/png",
        "image/jpeg",
        "image/webp",
    };

    public SetPromoBlockImageCommandValidator()
    {
        RuleFor(x => x.PlacementKey)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(260);

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("The image file is empty.")
            .LessThanOrEqualTo(MaxBytes).WithMessage("The image must be 5 MB or smaller.");

        RuleFor(x => x.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Only PNG, JPEG, or WEBP images are allowed.");
    }
}
