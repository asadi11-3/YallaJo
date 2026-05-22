using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.RemoveReview;

/// <summary>Admin removes (hard-deletes by status) a review from the platform.</summary>
public sealed record RemoveReviewCommand(
    Guid AdminUserId,
    Guid ReviewId,
    string? Reason = null) : IRequest<Result>;
