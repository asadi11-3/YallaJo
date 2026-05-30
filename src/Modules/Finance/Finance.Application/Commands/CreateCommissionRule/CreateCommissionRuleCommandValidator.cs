using FluentValidation;

namespace Finance.Application.Commands.CreateCommissionRule;

public sealed class CreateCommissionRuleCommandValidator : AbstractValidator<CreateCommissionRuleCommand>
{
    public CreateCommissionRuleCommandValidator()
    {
        RuleFor(x => x.Tier).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Currency).NotEmpty().Length(3).Matches("^[A-Z]{3}$");
        RuleFor(x => x.MinMonthlyRevenue).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.MaxMonthlyRevenue)
            .GreaterThan(x => x.MinMonthlyRevenue)
            .When(x => x.MaxMonthlyRevenue.HasValue);
        RuleFor(x => x.Percentage).GreaterThan(0m).LessThan(100m);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
