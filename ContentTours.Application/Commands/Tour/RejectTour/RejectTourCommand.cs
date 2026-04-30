using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.Tour.RejectTour;

public sealed record RejectTourCommand(Guid Id, byte[] RowVersion, string Reason) : ICommand;
