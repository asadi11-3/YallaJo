// <copyright file="ReorderFaqItemsCommandValidator.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.FaqItem.ReorderFaqItems;

using FluentValidation;

public sealed class ReorderFaqItemsCommandValidator : AbstractValidator<ReorderFaqItemsCommand>
{
    public ReorderFaqItemsCommandValidator()
    {
        RuleFor(x => x.EntityType).IsInEnum().WithErrorCode("FaqItem.InvalidEntityType");
        RuleFor(x => x.EntityId).NotEqual(Guid.Empty).WithErrorCode("FaqItem.InvalidEntityId");
        RuleFor(x => x.Items)
            .NotEmpty().WithErrorCode("FaqItem.Empty")
            .Must(items => items.Count <= 50).WithErrorCode("FaqItem.OutOfRange").WithMessage("Cannot reorder more than 50 items.");
        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.SortOrder).Distinct().Count() == items.Count)
            .WithErrorCode("FaqItem.SortOrderConflict")
            .WithMessage("SortOrder values must be distinct.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Id).NotEqual(Guid.Empty).WithErrorCode("FaqItem.NotFound");
            item.RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0).WithErrorCode("FaqItem.OutOfRange");
        });
    }
}
