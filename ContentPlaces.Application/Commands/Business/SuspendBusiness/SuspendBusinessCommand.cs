using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.Business.SuspendBusiness;

public sealed record SuspendBusinessCommand(Guid Id, string Reason, Guid SuspendedByUserId) : ICommand;
