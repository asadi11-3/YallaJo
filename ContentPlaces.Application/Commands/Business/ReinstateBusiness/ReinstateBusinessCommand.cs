using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.Business.ReinstateBusiness;

public sealed record ReinstateBusinessCommand(Guid Id) : ICommand;
