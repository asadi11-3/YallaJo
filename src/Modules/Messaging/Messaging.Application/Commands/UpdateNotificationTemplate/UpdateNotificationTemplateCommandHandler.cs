using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.UpdateNotificationTemplate;

internal sealed class UpdateNotificationTemplateCommandHandler(
    INotificationTemplateRepository templateRepository,
    IMessagingUnitOfWork unitOfWork) : IRequestHandler<UpdateNotificationTemplateCommand, Result>
{
    public async Task<Result> Handle(UpdateNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await templateRepository.GetByIdAsync(request.Id, cancellationToken);
        if (template is null)
            return Result.Failure(new Error("NotificationTemplate.NotFound", "Template not found."), Outcome.NotFound);

        template.Update(request.Title, request.Body, request.HtmlBody);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
