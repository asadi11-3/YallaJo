using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Commands.ApprovePayout;

public sealed record ApprovePayoutCommand(
    Guid PayoutId,
    Guid ApproverUserId) : IRequest<Result<ApprovePayoutResult>>;

public sealed record ApprovePayoutResult(
    Guid PayoutId,
    string Status,
    string? GatewayPayoutId);
