using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Common;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.DeleteNotificationTemplate;

internal sealed class DeleteNotificationTemplateCommandHandler(
    INotificationTemplateRepository templateRepository,
    IMessagingUnitOfWork unitOfWork) : IRequestHandler<DeleteNotificationTemplateCommand, Result>
{
    public async Task<Result> Handle(DeleteNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await templateRepository.GetByIdAsync(request.Id, cancellationToken);
        if (template is null)
            return Result.Failure(new Error("NotificationTemplate.NotFound", "Template not found."), Outcome.NotFound);

        if (!RowVersionUtil.Equal(template.RowVersion, request.RowVersion))
            return Result.Failure(
                new Error("NotificationTemplate.ConcurrencyConflict", "This template was modified by another user. Please refresh and try again."),
                Outcome.Conflict);

        template.Delete();

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("NotificationTemplate.ConcurrencyConflict", "This template was modified by another user. Please refresh and try again."),
                Outcome.Conflict);
        }

        return Result.Success();
    }
}
