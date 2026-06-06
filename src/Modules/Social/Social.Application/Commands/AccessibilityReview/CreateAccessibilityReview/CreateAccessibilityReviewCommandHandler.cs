using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.AccessibilityReview.CreateAccessibilityReview;

public sealed class CreateAccessibilityReviewCommandHandler(
    IAccessibilityReviewRepository repository,
    ISocialUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<CreateAccessibilityReviewCommandHandler> logger)
    : ICommandHandler<CreateAccessibilityReviewCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateAccessibilityReviewCommand request, CancellationToken ct)
    {
        try
        {
            // S-AR1: one accessibility review per (user, target)
            var existing = await repository.GetByUserAndTargetAsync(
                request.UserId, request.TargetType, request.TargetId, ct).ConfigureAwait(false);
            if (existing is not null)
            {
                return Result.Failure<Guid>(
                    new Error("AccessibilityReview.Duplicate",
                        "You have already submitted an accessibility review for this entity."),
                    Outcome.Conflict);
            }

            var review = Social.Domain.Entities.AccessibilityReview.Create(
                request.UserId,
                request.TargetType,
                request.TargetId,
                request.Rating,
                request.Title,
                request.Content,
                request.VisitDate,
                request.FeatureTypesCsv,
                timeProvider);

            await repository.AddAsync(review, ct).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            logger.LogInformation(
                "Created accessibility review {ReviewId} for {TargetType}/{TargetId} by user {UserId}",
                review.Id, request.TargetType, request.TargetId, request.UserId);
            return Result.Success(review.Id);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result.Failure<Guid>(
                new Error("AccessibilityReview.Invalid", ex.Message), Outcome.Invalid);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Guid>(
                new Error("AccessibilityReview.Invalid", ex.Message), Outcome.Invalid);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<Guid>(
                new Error("AccessibilityReview.Conflict", "Concurrent update."), Outcome.Conflict);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<Guid>(
                new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
