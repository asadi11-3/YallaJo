using FluentValidation;

namespace ContentTours.Application.Queries.TourSchedule.ListTourSchedules;

public sealed class ListTourSchedulesQueryValidator : AbstractValidator<ListTourSchedulesQuery>
{
    public ListTourSchedulesQueryValidator()
    {
        RuleFor(x => x.TourId).NotEqual(Guid.Empty);
    }
}
