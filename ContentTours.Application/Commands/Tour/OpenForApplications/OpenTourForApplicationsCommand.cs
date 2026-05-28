using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace ContentTours.Application.Commands.Tour.OpenForApplications;

public sealed record OpenTourForApplicationsCommand(Guid TourId) : ICommand;
