using FluentValidation;

namespace Messaging.Application.Commands.UpdateNotificationTemplate;

internal sealed class UpdateNotificationTemplateCommandValidator : AbstractValidator<UpdateNotificationTemplateCommand>
{
    public UpdateNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.HtmlBody)
            .MaximumLength(20000)
            .When(x => x.HtmlBody is not null);
    }
}
