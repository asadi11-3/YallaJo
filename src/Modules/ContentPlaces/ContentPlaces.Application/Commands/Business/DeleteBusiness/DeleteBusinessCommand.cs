using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.Business.DeleteBusiness;

public sealed record DeleteBusinessCommand(Guid Id) : ICommand;
