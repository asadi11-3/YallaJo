using FluentValidation;

namespace Accounts.Application.Commands.UpdateAvatar;

public sealed class UpdateAvatarCommandValidator : AbstractValidator<UpdateAvatarCommand>
{
    public UpdateAvatarCommandValidator()
    {
        RuleFor(x => x.AvatarUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(IsValidAvatarUrl)
            .WithMessage("AvatarUrl must be an absolute URL or a rooted relative path.");
    }

    private static bool IsValidAvatarUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        // Local file storage returns rooted relative URLs (e.g. /uploads/avatars/...).
        if (url.StartsWith("/", StringComparison.Ordinal))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}
