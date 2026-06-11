using Finance.Application.Interfaces;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Disputes;

public sealed record OpenDisputeCommand(Guid PaymentId, Guid UserId, string Reason, string Description) : IRequest<Result<DisputeDto>>;

public sealed record MarkDisputeUnderReviewCommand(Guid DisputeId, Guid AdminId) : IRequest<Result<DisputeDto>>;

public sealed record ResolveDisputeCommand(Guid DisputeId, Guid AdminId, DisputeResolution Resolution, string? Notes) : IRequest<Result<DisputeDto>>;

public sealed record EscalateDisputeCommand(Guid DisputeId, Guid AdminId, string Reason) : IRequest<Result<DisputeDto>>;

public sealed record GetMyDisputesQuery(Guid UserId) : IRequest<Result<IReadOnlyList<DisputeDto>>>;

public sealed record GetOpenDisputesQuery : IRequest<Result<IReadOnlyList<DisputeDto>>>;

public sealed record GetDisputeStatusCountsQuery : IRequest<Result<DisputeStatusCountsDto>>;

public sealed class OpenDisputeCommandHandler(
    IPaymentRepository paymentRepository,
    IDisputeRepository disputeRepository,
    IFinanceUnitOfWork unitOfWork,
    ILogger<OpenDisputeCommandHandler> logger)
    : IRequestHandler<OpenDisputeCommand, Result<DisputeDto>>
{
    public async Task<Result<DisputeDto>> Handle(OpenDisputeCommand request, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByIdAsync(request.PaymentId, ct);
        if (payment is null)
        {
            return Result.Failure<DisputeDto>(new Error("Payment.NotFound", "Payment not found."), Outcome.NotFound);
        }

        if (payment.UserId != request.UserId)
        {
            return Result.Failure<DisputeDto>(new Error("Dispute.Forbidden", "Only the payment owner can open a dispute."), Outcome.Forbidden);
        }

        var openResult = Dispute.Open(request.PaymentId, request.UserId, request.Reason, request.Description);
        if (openResult.IsFailure)
        {
            return Result.Failure<DisputeDto>(openResult.Errors[0], openResult.Outcome);
        }

        var dispute = openResult.Value!;
        await disputeRepository.AddAsync(dispute, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Opened dispute {DisputeId} for payment {PaymentId}.", dispute.Id, dispute.PaymentId);
        return Result.Success(DisputeMapper.ToDto(dispute));
    }
}

public sealed class MarkDisputeUnderReviewCommandHandler(
    IDisputeRepository disputeRepository,
    IFinanceUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<MarkDisputeUnderReviewCommandHandler> logger)
    : IRequestHandler<MarkDisputeUnderReviewCommand, Result<DisputeDto>>
{
    public async Task<Result<DisputeDto>> Handle(MarkDisputeUnderReviewCommand request, CancellationToken ct)
    {
        var dispute = await disputeRepository.GetByIdAsync(request.DisputeId, ct);
        if (dispute is null)
        {
            return Result.Failure<DisputeDto>(new Error("Dispute.NotFound", "Dispute not found."), Outcome.NotFound);
        }

        var result = dispute.MarkUnderReview(request.AdminId, timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return Result.Failure<DisputeDto>(result.Errors[0], result.Outcome);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<DisputeDto>(
                new Error("Dispute.ConcurrencyConflict", "The dispute was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        logger.LogInformation("Dispute {DisputeId} marked under review.", dispute.Id);
        return Result.Success(DisputeMapper.ToDto(dispute));
    }
}

public sealed class ResolveDisputeCommandHandler(
    IDisputeRepository disputeRepository,
    IFinanceUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ResolveDisputeCommandHandler> logger)
    : IRequestHandler<ResolveDisputeCommand, Result<DisputeDto>>
{
    public async Task<Result<DisputeDto>> Handle(ResolveDisputeCommand request, CancellationToken ct)
    {
        var dispute = await disputeRepository.GetByIdAsync(request.DisputeId, ct);
        if (dispute is null)
        {
            return Result.Failure<DisputeDto>(new Error("Dispute.NotFound", "Dispute not found."), Outcome.NotFound);
        }

        var result = dispute.Resolve(request.AdminId, request.Resolution, request.Notes, timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return Result.Failure<DisputeDto>(result.Errors[0], result.Outcome);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<DisputeDto>(
                new Error("Dispute.ConcurrencyConflict", "The dispute was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        logger.LogInformation("Dispute {DisputeId} resolved as {Resolution}.", dispute.Id, dispute.Resolution);
        return Result.Success(DisputeMapper.ToDto(dispute));
    }
}

public sealed class EscalateDisputeCommandHandler(
    IDisputeRepository disputeRepository,
    IFinanceUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<EscalateDisputeCommandHandler> logger)
    : IRequestHandler<EscalateDisputeCommand, Result<DisputeDto>>
{
    public async Task<Result<DisputeDto>> Handle(EscalateDisputeCommand request, CancellationToken ct)
    {
        var dispute = await disputeRepository.GetByIdAsync(request.DisputeId, ct);
        if (dispute is null)
        {
            return Result.Failure<DisputeDto>(new Error("Dispute.NotFound", "Dispute not found."), Outcome.NotFound);
        }

        var result = dispute.Escalate(request.AdminId, request.Reason, timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return Result.Failure<DisputeDto>(result.Errors[0], result.Outcome);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<DisputeDto>(
                new Error("Dispute.ConcurrencyConflict", "The dispute was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        logger.LogInformation("Dispute {DisputeId} escalated.", dispute.Id);
        return Result.Success(DisputeMapper.ToDto(dispute));
    }
}

public sealed class GetMyDisputesQueryHandler(IDisputeRepository disputeRepository, ILogger<GetMyDisputesQueryHandler> logger)
    : IRequestHandler<GetMyDisputesQuery, Result<IReadOnlyList<DisputeDto>>>
{
    public async Task<Result<IReadOnlyList<DisputeDto>>> Handle(GetMyDisputesQuery request, CancellationToken ct)
    {
        var disputes = await disputeRepository.GetByUserIdAsync(request.UserId, ct);
        logger.LogDebug("Read {Count} disputes for user {UserId}.", disputes.Count, request.UserId);
        return Result.Success<IReadOnlyList<DisputeDto>>(disputes.Select(DisputeMapper.ToDto).ToList());
    }
}

public sealed class GetOpenDisputesQueryHandler(IDisputeRepository disputeRepository, ILogger<GetOpenDisputesQueryHandler> logger)
    : IRequestHandler<GetOpenDisputesQuery, Result<IReadOnlyList<DisputeDto>>>
{
    public async Task<Result<IReadOnlyList<DisputeDto>>> Handle(GetOpenDisputesQuery request, CancellationToken ct)
    {
        var disputes = await disputeRepository.GetOpenDisputesAsync(ct);
        logger.LogDebug("Read {Count} open disputes.", disputes.Count);
        return Result.Success<IReadOnlyList<DisputeDto>>(disputes.Select(DisputeMapper.ToDto).ToList());
    }
}

public sealed class GetDisputeStatusCountsQueryHandler(IDisputeRepository disputeRepository, ILogger<GetDisputeStatusCountsQueryHandler> logger)
    : IRequestHandler<GetDisputeStatusCountsQuery, Result<DisputeStatusCountsDto>>
{
    public async Task<Result<DisputeStatusCountsDto>> Handle(GetDisputeStatusCountsQuery request, CancellationToken ct)
    {
        try
        {
            var counts = await disputeRepository.GetStatusCountsAsync(ct).ConfigureAwait(false);
            var dto = new DisputeStatusCountsDto(
                Open: counts.GetValueOrDefault(DisputeStatus.Open),
                UnderReview: counts.GetValueOrDefault(DisputeStatus.UnderReview),
                Resolved: counts.GetValueOrDefault(DisputeStatus.Resolved),
                Escalated: counts.GetValueOrDefault(DisputeStatus.Escalated),
                Closed: counts.GetValueOrDefault(DisputeStatus.Closed));
            logger.LogDebug("Read dispute status counts.");
            return Result.Success(dto);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure<DisputeStatusCountsDto>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}

internal static class DisputeMapper
{
    public static DisputeDto ToDto(Dispute dispute)
        => new(
            dispute.Id,
            dispute.PaymentId,
            dispute.UserId,
            dispute.Reason,
            dispute.Description,
            dispute.Status,
            dispute.Resolution,
            dispute.ResolvedAt,
            dispute.ResolvedByUserId,
            dispute.ResolutionNotes,
            dispute.CreatedAt);
}
