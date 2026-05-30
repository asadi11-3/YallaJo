using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.Business.ResubmitBusiness;

public sealed record ResubmitBusinessCommand(Guid Id) : ICommand;
