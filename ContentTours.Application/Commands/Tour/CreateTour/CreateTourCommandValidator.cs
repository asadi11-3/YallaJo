using FluentValidation;

namespace ContentTours.Application.Commands.Tour.CreateTour;

public sealed class CreateTourCommandValidator : AbstractValidator<CreateTourCommand>
{
    public CreateTourCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(300)
            .Matches(@"^[a-z0-9\-]+$")
            .WithMessage("Slug must contain only lowercase letters, digits, and hyphens.");

        RuleFor(x => x.Difficulty)
            .IsInEnum();

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(43200);

        RuleFor(x => x.MaxGroupSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(500);

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0m);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m);

        RuleFor(x => x.MeetingPointLatitude)
            .InclusiveBetween(-90m, 90m)
            .When(x => x.MeetingPointLatitude.HasValue);

        RuleFor(x => x.MeetingPointLongitude)
            .InclusiveBetween(-180m, 180m)
            .When(x => x.MeetingPointLongitude.HasValue);

        RuleFor(x => x)
            .Must(x =>
                (x.MeetingPointLatitude.HasValue && x.MeetingPointLongitude.HasValue) ||
                (!x.MeetingPointLatitude.HasValue && !x.MeetingPointLongitude.HasValue))
            .WithMessage("MeetingPointLatitude and MeetingPointLongitude must be set together.")
            .WithName("MeetingPoint");

        RuleFor(x => x.PlaceId)
            .NotEqual(Guid.Empty)
            .When(x => x.PlaceId.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .When(x => x.Description is not null);

        RuleFor(x => x.ShortDescription)
            .MaximumLength(1000)
            .When(x => x.ShortDescription is not null);

        RuleFor(x => x.MinAge)
            .InclusiveBetween(0, 120)
            .When(x => x.MinAge.HasValue);

        RuleFor(x => x.AgeRestriction)
            .InclusiveBetween(0, 120)
            .When(x => x.AgeRestriction.HasValue);

        RuleFor(x => x.CancellationPolicyHours)
            .InclusiveBetween(0, 168);

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200)
            .When(x => x.MetaTitle is not null);

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500)
            .When(x => x.MetaDescription is not null);
    }
}
