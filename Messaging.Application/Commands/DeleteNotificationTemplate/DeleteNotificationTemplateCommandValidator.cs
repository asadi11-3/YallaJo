using FluentValidation;

namespace Messaging.Application.Commands.DeleteNotificationTemplate;

internal sealed class DeleteNotificationTemplateCommandValidator : AbstractValidator<DeleteNotificationTemplateCommand>
{
    public DeleteNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
