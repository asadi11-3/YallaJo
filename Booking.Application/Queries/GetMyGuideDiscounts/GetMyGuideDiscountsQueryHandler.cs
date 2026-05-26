using Booking.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetMyGuideDiscounts;

public sealed class GetMyGuideDiscountsQueryHandler(
    IGuideDiscountRepository guideDiscountRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyGuideDiscountsQuery, IReadOnlyList<GuideDiscountDto>>
{
    public async Task<Result<IReadOnlyList<GuideDiscountDto>>> Handle(GetMyGuideDiscountsQuery request, CancellationToken cancellationToken)
    {
        var guideUserId = currentUser.UserId!.Value;
        var discounts = await guideDiscountRepository
            .GetByGuideUserIdAsync(guideUserId, cancellationToken)
            .ConfigureAwait(false);

        var dtos = discounts.Select(d => new GuideDiscountDto(
            d.Id, d.TourId, d.Name, d.Description, d.DiscountType, d.DiscountValue,
            d.Currency, d.ValidFrom, d.ValidUntil, d.MaxUsageCount, d.CurrentUsageCount, d.IsActive))
            .ToList();

        return Result.Success<IReadOnlyList<GuideDiscountDto>>(dtos);
    }
}
