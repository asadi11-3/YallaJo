using System.Text.RegularExpressions;
using FluentValidation;

namespace ContentBlogs.Application.Commands.Creator.UpdateProfile;

public sealed class UpdateCreatorProfileCommandValidator
    : AbstractValidator<UpdateCreatorProfileCommand>
{
    private static readonly Regex SlugRegex =
        new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public UpdateCreatorProfileCommandValidator()
    {
        RuleFor(x => x.DisplayName!)
            .NotEmpty().WithMessage("Display name cannot be empty when provided.")
            .MaximumLength(200)
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.Bio!)
            .MaximumLength(2000)
            .When(x => x.Bio is not null);

        RuleFor(x => x.AvatarUrl!)
            .MaximumLength(500)
            .When(x => x.AvatarUrl is not null);

        RuleFor(x => x.Slug!)
            .Must(slug => SlugRegex.IsMatch(slug))
                .WithMessage("Slug must be lowercase alphanumeric segments separated by '-'.")
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Slug));
    }
}
