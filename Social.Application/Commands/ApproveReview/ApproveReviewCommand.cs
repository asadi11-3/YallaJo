using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.ApproveReview;

/// <summary>Admin approves (restores) a flagged or moderated review for publication.</summary>
public sealed record ApproveReviewCommand(
    Guid AdminUserId,
    Guid ReviewId,
    string? Notes = null) : IRequest<Result>;
