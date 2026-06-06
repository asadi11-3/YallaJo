using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.AccessibilityReview.UpdateAccessibilityReview;

public sealed class UpdateAccessibilityReviewCommandHandler(
    IAccessibilityReviewRepository repository,
    ISocialUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<UpdateAccessibilityReviewCommandHandler> logger)
    : ICommandHandler<UpdateAccessibilityReviewCommand>
{
    public async Task<Result> Handle(UpdateAccessibilityReviewCommand request, CancellationToken ct)
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

            if (review.UserId != request.UserId)
            {
                return Result.Failure(
                    new Error("AccessibilityReview.OwnerMismatch",
                        "Only the original author may edit this accessibility review."),
                    Outcome.Forbidden);
            }

            review.Edit(
                request.Rating,
                request.Title,
                request.Content,
                request.VisitDate,
                request.FeatureTypesCsv,
                timeProvider);

            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Edited accessibility review {ReviewId} by user {UserId}",
                review.Id, request.UserId);
            return Result.Success();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("EditWindowExpired", StringComparison.Ordinal))
        {
            return Result.Failure(
                new Error("AccessibilityReview.EditWindowExpired",
                    "Accessibility reviews can only be edited within 48 hours of submission."),
                Outcome.Invalid);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(
                new Error("AccessibilityReview.Invalid", ex.Message), Outcome.Invalid);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result.Failure(
                new Error("AccessibilityReview.Invalid", ex.Message), Outcome.Invalid);
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
