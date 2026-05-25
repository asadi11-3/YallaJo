using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace ContentTours.Application.Commands.Tour.CloseForApplications;

public sealed record CloseTourForApplicationsCommand(Guid TourId) : ICommand;
