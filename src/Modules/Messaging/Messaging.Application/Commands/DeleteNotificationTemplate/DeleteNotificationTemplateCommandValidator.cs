using FluentValidation;

namespace Messaging.Application.Commands.DeleteNotificationTemplate;

internal sealed class DeleteNotificationTemplateCommandValidator : AbstractValidator<DeleteNotificationTemplateCommand>
{
    public DeleteNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv is { Length: > 0 })
            .WithMessage("RowVersion is required for optimistic concurrency.");
    }
}
