using FluentValidation;

namespace ContentCore.Application.Commands.Category.ReorderCategories;

public sealed class ReorderCategoriesCommandValidator : AbstractValidator<ReorderCategoriesCommand>
{
    public ReorderCategoriesCommandValidator()
    {
        RuleFor(x => x.SortOrders).NotEmpty().WithMessage("At least one category sort order update is required.");
        RuleForEach(x => x.SortOrders).ChildRules(item =>
        {
            item.RuleFor(x => x.CategoryId).NotEmpty();
            item.RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        });
    }
}
