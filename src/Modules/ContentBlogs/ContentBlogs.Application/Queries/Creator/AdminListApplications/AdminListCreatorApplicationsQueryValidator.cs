using FluentValidation;

namespace ContentBlogs.Application.Queries.Creator.AdminListApplications;

public sealed class AdminListCreatorApplicationsQueryValidator : AbstractValidator<AdminListCreatorApplicationsQuery>
{
    public AdminListCreatorApplicationsQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
    }
}
