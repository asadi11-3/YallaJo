using System.Globalization;
using FluentValidation;

namespace ContentPlaces.Application.Commands.BusinessHours.SetBusinessHours;

public sealed class SetBusinessHoursCommandValidator : AbstractValidator<SetBusinessHoursCommand>
{
    public SetBusinessHoursCommandValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();

        RuleFor(x => x.Hours)
            .NotNull()
            .WithMessage("Hours list must not be null.");

        RuleFor(x => x.Hours)
            .Must(hours => hours.GroupBy(h => h.DayOfWeek).All(g => g.Count() <= 2))
            .WithMessage("A maximum of 2 time entries per day are allowed (split shifts).")
            .When(x => x.Hours is not null);

        RuleForEach(x => x.Hours).ChildRules(entry =>
        {
            entry.RuleFor(e => e.DayOfWeek)
                .InclusiveBetween(0, 6)
                .WithMessage("DayOfWeek must be between 0 (Sunday) and 6 (Saturday).");

            entry.When(e => !e.IsClosed, () =>
            {
                entry.RuleFor(e => e.OpenTime)
                    .NotEmpty()
                    .WithMessage("OpenTime is required when the business is open.")
                    .Must(t => TimeOnly.TryParse(t, CultureInfo.InvariantCulture, out _))
                    .WithMessage("OpenTime must be a valid time in HH:mm format.");

                entry.RuleFor(e => e.CloseTime)
                    .NotEmpty()
                    .WithMessage("CloseTime is required when the business is open.")
                    .Must(t => TimeOnly.TryParse(t, CultureInfo.InvariantCulture, out _))
                    .WithMessage("CloseTime must be a valid time in HH:mm format.");

                entry.RuleFor(e => e)
                    .Must(e =>
                    {
                        if (!TimeOnly.TryParse(e.OpenTime, CultureInfo.InvariantCulture, out var open) ||
                            !TimeOnly.TryParse(e.CloseTime, CultureInfo.InvariantCulture, out var close))
                        {
                            return true;
                        }

                        if (open == TimeOnly.MinValue && close == TimeOnly.MinValue)
                        {
                            return true;
                        }

                        return open < close;
                    })
                    .WithMessage("OpenTime must be earlier than CloseTime (use 00:00/00:00 for 24-hour operation).");
            });
        });

        RuleFor(x => x.Hours)
            .Must(hours =>
            {
                foreach (var dayGroup in hours.GroupBy(h => h.DayOfWeek))
                {
                    var open = dayGroup.Where(e => !e.IsClosed).ToList();
                    if (open.Count == 2)
                    {
                        if (!TimeOnly.TryParse(open[0].CloseTime, CultureInfo.InvariantCulture, out var close1) ||
                            !TimeOnly.TryParse(open[1].OpenTime, CultureInfo.InvariantCulture, out var open2))
                        {
                            return true;
                        }

                        if (open2 < close1)
                        {
                            return false;
                        }
                    }
                }

                return true;
            })
            .WithMessage("Overlapping shifts detected: the second shift must start at or after the first shift ends.")
            .When(x => x.Hours is not null);
    }
}
