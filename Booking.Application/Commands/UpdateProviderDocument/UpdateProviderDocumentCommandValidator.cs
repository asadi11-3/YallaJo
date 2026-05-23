using System;
using FluentValidation;

namespace Booking.Application.Commands.UpdateProviderDocument;

public sealed class UpdateProviderDocumentCommandValidator : AbstractValidator<UpdateProviderDocumentCommand>
{
    public UpdateProviderDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow.Date).When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiry date must be in the future.");

        RuleFor(x => x)
            .Must(x => x.File != null || x.ExpiresAt.HasValue)
            .WithMessage("You must provide either a new file or a new expiry date.");
    }
}
