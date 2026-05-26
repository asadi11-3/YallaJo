using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Domain.Entities;

public sealed class GuideAvailabilityBlock : BaseEntity
{
    private GuideAvailabilityBlock() { }

    public Guid GuideId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string? Reason { get; private set; }

    public static Result<GuideAvailabilityBlock> Create(Guid guideId, DateOnly startDate, DateOnly endDate, string? reason)
    {
        if (guideId == Guid.Empty)
            return Result.Failure<GuideAvailabilityBlock>(new Error("GuideAvailabilityBlock.GuideRequired", "GuideId is required."));

        if (startDate > endDate)
            return Result.Failure<GuideAvailabilityBlock>(new Error("GuideAvailabilityBlock.InvalidDateRange", "StartDate must be on or before EndDate."));

        return Result.Success(new GuideAvailabilityBlock
        {
            GuideId = guideId,
            StartDate = startDate,
            EndDate = endDate,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
        });
    }
}
