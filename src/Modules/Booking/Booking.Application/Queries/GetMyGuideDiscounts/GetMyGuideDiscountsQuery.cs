using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetMyGuideDiscounts;

/// <summary>Gets all discounts for the current guide.</summary>
public sealed record GetMyGuideDiscountsQuery : IQuery<IReadOnlyList<GuideDiscountDto>>;
