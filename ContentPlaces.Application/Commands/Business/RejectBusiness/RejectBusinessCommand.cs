using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.Business.RejectBusiness;

public sealed record RejectBusinessCommand(Guid Id, string Reason) : ICommand;
