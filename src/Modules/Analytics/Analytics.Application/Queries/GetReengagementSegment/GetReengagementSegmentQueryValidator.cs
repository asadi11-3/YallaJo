using FluentValidation;

namespace Analytics.Application.Queries.GetReengagementSegment;

public sealed class GetReengagementSegmentQueryValidator : AbstractValidator<GetReengagementSegmentQuery>
{
    public GetReengagementSegmentQueryValidator()
    {
        RuleFor(x => x.EntityKind).IsInEnum().When(x => x.EntityKind.HasValue);
    }
}
