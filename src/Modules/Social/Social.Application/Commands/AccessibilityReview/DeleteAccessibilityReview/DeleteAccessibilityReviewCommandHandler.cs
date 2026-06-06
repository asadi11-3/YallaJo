using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.AccessibilityReview.DeleteAccessibilityReview;

public sealed class DeleteAccessibilityReviewCommandHandler(
    IAccessibilityReviewRepository repository,
    ISocialUnitOfWork unitOfWork,
    ILogger<DeleteAccessibilityReviewCommandHandler> logger)
    : ICommandHandler<DeleteAccessibilityReviewCommand>
{
    public async Task<Result> Handle(DeleteAccessibilityReviewCommand request, CancellationToken ct)
    {
        try
        {
            var review = await repository.GetByIdAsync(request.ReviewId, ct).ConfigureAwait(false);
            if (review is null)
            {
                return Result.Failure(
                    new Error("AccessibilityReview.NotFound",
                        $"Accessibility review {request.ReviewId} not found."),
                    Outcome.NotFound);
            }

            if (!request.IsAdmin && review.UserId != request.CallerUserId)
            {
                return Result.Failure(
                    new Error("AccessibilityReview.OwnerMismatch",
                        "Only the author or an admin may delete this accessibility review."),
                    Outcome.Forbidden);
            }

            review.Delete(isAdmin: request.IsAdmin);
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation(
                "Soft-deleted accessibility review {ReviewId} by {Actor} (isAdmin={IsAdmin})",
                review.Id, request.CallerUserId, request.IsAdmin);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("AccessibilityReview.Conflict", "Concurrent update."), Outcome.Conflict);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
