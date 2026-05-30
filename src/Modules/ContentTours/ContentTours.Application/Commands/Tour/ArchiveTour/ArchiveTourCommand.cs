using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.Tour.ArchiveTour;

public sealed record ArchiveTourCommand(Guid Id, byte[] RowVersion) : ICommand;
