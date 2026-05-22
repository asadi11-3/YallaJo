using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.CreateNotificationTemplate;

internal sealed class CreateNotificationTemplateCommandHandler(
    INotificationTemplateRepository templateRepository,
    IMessagingUnitOfWork unitOfWork) : IRequestHandler<CreateNotificationTemplateCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicates (Type, Channel, LanguageCode) UNIQUE
        var existing = await templateRepository.GetByKeyAndLanguageAsync(request.Type, request.Channel, request.LanguageCode, cancellationToken);
        if (existing is not null)
            return Result.Failure<Guid>(
                new Error("NotificationTemplate.DuplicateKey",
                    $"A template for {request.Type}/{request.Channel}/{request.LanguageCode} already exists."),
                Outcome.Conflict);

        var template = NotificationTemplate.Create(request.Type, request.Channel, request.LanguageCode, request.Title, request.Body, request.HtmlBody);
        await templateRepository.AddAsync(template, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(template.Id);
    }
}
