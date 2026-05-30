using FluentValidation;

namespace ContentSeo.Application.Commands.Redirect.UpdateRedirect;

public sealed class UpdateRedirectCommandValidator : AbstractValidator<UpdateRedirectCommand>
{
    public UpdateRedirectCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        When(x => !string.IsNullOrWhiteSpace(x.NewUrl), () =>
        {
            RuleFor(x => x.NewUrl!)
                .MaximumLength(2048)
                .Must(url => url.StartsWith("/", StringComparison.Ordinal) || Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("NewUrl must be a relative path or absolute URL.");
        });

        When(x => x.StatusCode.HasValue, () =>
        {
            RuleFor(x => x.StatusCode!.Value).Must(code => code is 301 or 302);
        });
    }
}
