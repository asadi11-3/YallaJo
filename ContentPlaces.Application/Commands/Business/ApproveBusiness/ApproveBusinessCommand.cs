using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.Business.ApproveBusiness;

public sealed record ApproveBusinessCommand(Guid Id, Guid ApprovedByUserId) : ICommand;
