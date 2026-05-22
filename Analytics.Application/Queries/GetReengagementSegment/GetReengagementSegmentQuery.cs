using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetReengagementSegment;

public sealed record GetReengagementSegmentQuery(
    string Rule,
    EntityType? EntityKind = null,
    Guid? EntityId = null) : IQuery<SegmentResponse>;

public sealed record SegmentResponse(IReadOnlyList<Guid> UserIds, int Count);

public sealed class GetReengagementSegmentQueryHandler(
    IUserInteractionRepository interactionRepo) : IQueryHandler<GetReengagementSegmentQuery, SegmentResponse>
{
    private const int PageSize = 500;

    public async Task<Result<SegmentResponse>> Handle(GetReengagementSegmentQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        return request.Rule switch
        {
            "viewed-not-booked" => await ViewedNotBooked(request, now, ct),
            "lapsed-booker" => await LapsedBooker(now, ct),
            "abandoned-checkout" => await AbandonedCheckout(now, ct),
            "reviewer-no-return" => await ReviewerNoReturn(now, ct),
            _ => Result.Failure<SegmentResponse>(
                new Error("Segment.UnknownRule", $"Unknown rule: {request.Rule}. Valid: viewed-not-booked, lapsed-booker, abandoned-checkout, reviewer-no-return."))
        };
    }

    private async Task<Result<SegmentResponse>> ViewedNotBooked(GetReengagementSegmentQuery request, DateTime now, CancellationToken ct)
    {
        if (request.EntityKind is null || request.EntityId is null)
            return Result.Failure<SegmentResponse>(new Error("Segment.MissingEntity", "Entity required for this rule."));

        var viewerSet = new HashSet<Guid>();
        var bookerSet = new HashSet<Guid>();
        var from = now.AddDays(-7);

        await PageInteractionsAsync(from, now, ct, interaction =>
        {
            if (!interaction.UserId.HasValue || interaction.EntityType != request.EntityKind || interaction.EntityId != request.EntityId)
                return;

            if (interaction.InteractionType == InteractionType.View)
                viewerSet.Add(interaction.UserId.Value);
            else if (interaction.InteractionType == InteractionType.BookingCompleted)
                bookerSet.Add(interaction.UserId.Value);
        });

        var userIds = viewerSet.Where(v => !bookerSet.Contains(v)).ToList();
        return Result.Success(new SegmentResponse(userIds, userIds.Count));
    }

    private async Task<Result<SegmentResponse>> LapsedBooker(DateTime now, CancellationToken ct)
    {
        var userBookings = new Dictionary<Guid, (int Count, DateTime Last)>();
        var from = now.AddDays(-180); // look back 180 days for lapsed bookers

        await PageInteractionsAsync(from, now, ct, interaction =>
        {
            if (!interaction.UserId.HasValue || interaction.InteractionType != InteractionType.BookingCompleted)
                return;

            var uid = interaction.UserId.Value;
            if (userBookings.TryGetValue(uid, out var current))
                userBookings[uid] = (current.Count + 1, interaction.OccurredAt > current.Last ? interaction.OccurredAt : current.Last);
            else
                userBookings[uid] = (1, interaction.OccurredAt);
        });

        var userIds = userBookings
            .Where(kv => kv.Value.Count == 1 && kv.Value.Last < now.AddDays(-90))
            .Select(kv => kv.Key)
            .ToList();
        return Result.Success(new SegmentResponse(userIds, userIds.Count));
    }

    private async Task<Result<SegmentResponse>> AbandonedCheckout(DateTime now, CancellationToken ct)
    {
        var starters = new HashSet<Guid>();
        var completers = new HashSet<Guid>();
        var from = now.AddDays(-30);

        await PageInteractionsAsync(from, now, ct, interaction =>
        {
            if (!interaction.UserId.HasValue) return;

            if (interaction.InteractionType == InteractionType.BookingStarted)
                starters.Add(interaction.UserId.Value);
            else if (interaction.InteractionType == InteractionType.BookingCompleted)
                completers.Add(interaction.UserId.Value);
        });

        var userIds = starters.Where(s => !completers.Contains(s)).ToList();
        return Result.Success(new SegmentResponse(userIds, userIds.Count));
    }

    private async Task<Result<SegmentResponse>> ReviewerNoReturn(DateTime now, CancellationToken ct)
    {
        var reviewers = new HashSet<Guid>();
        var recentBookers = new HashSet<Guid>();
        var from = now.AddDays(-120); // look back 120 days for reviewer context

        await PageInteractionsAsync(from, now, ct, interaction =>
        {
            if (!interaction.UserId.HasValue) return;

            if (interaction.InteractionType == InteractionType.ReviewSubmitted && interaction.OccurredAt < now.AddDays(-60))
                reviewers.Add(interaction.UserId.Value);
            else if (interaction.InteractionType == InteractionType.BookingCompleted && interaction.OccurredAt >= now.AddDays(-60))
                recentBookers.Add(interaction.UserId.Value);
        });

        var userIds = reviewers.Where(r => !recentBookers.Contains(r)).ToList();
        return Result.Success(new SegmentResponse(userIds, userIds.Count));
    }

    /// <summary>Pages through interactions in batches of 500, calling the action per interaction.</summary>
    private async Task PageInteractionsAsync(DateTime from, DateTime to, CancellationToken ct, Action<Domain.Entities.UserInteraction> action)
    {
        long? afterId = null;
        while (true)
        {
            var (interactions, nextId) = await interactionRepo.GetPageAsync(
                userId: null, entityType: null, entityId: null, type: null,
                from: from, to: to, afterId: afterId, pageSize: PageSize, ct: ct);

            foreach (var interaction in interactions)
                action(interaction);

            if (interactions.Count < PageSize || nextId is null) break;
            afterId = nextId;
        }
    }
}
