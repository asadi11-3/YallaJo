// <copyright file="CreateRedirectCommandValidator.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.Redirect.CreateRedirect;

using FluentValidation;

public sealed class CreateRedirectCommandValidator : AbstractValidator<CreateRedirectCommand>
{
    public CreateRedirectCommandValidator()
    {
        RuleFor(x => x.OldUrl)
            .NotEmpty()
            .Matches("^/[a-z0-9\\-/]+$")
            .WithErrorCode("Redirect.OldUrlInvalid")
            .WithMessage("OldUrl must be a relative path matching ^/[a-z0-9\\-/]+$.");

        RuleFor(x => x.NewUrl)
            .NotEmpty()
            .Matches("^(/[a-z0-9\\-/]+|https?://[^\\s]+)$")
            .WithErrorCode("Redirect.NewUrlInvalid")
            .WithMessage("NewUrl must be a relative path or absolute URL.");

        RuleFor(x => x.StatusCode)
            .Must(c => c == 301 || c == 302)
            .WithErrorCode("Redirect.InvalidStatusCode")
            .WithMessage("StatusCode must be 301 or 302.");

        RuleFor(x => x)
            .Must(x => !string.Equals(x.OldUrl, x.NewUrl, StringComparison.OrdinalIgnoreCase))
            .WithErrorCode("Redirect.SameSourceTarget")
            .WithMessage("OldUrl and NewUrl must differ.");
    }
}
