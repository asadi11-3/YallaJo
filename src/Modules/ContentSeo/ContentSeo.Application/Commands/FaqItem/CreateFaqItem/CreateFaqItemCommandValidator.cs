// <copyright file="CreateFaqItemCommandValidator.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.FaqItem.CreateFaqItem;

using FluentValidation;

public sealed class CreateFaqItemCommandValidator : AbstractValidator<CreateFaqItemCommand>
{
    public CreateFaqItemCommandValidator()
    {
        RuleFor(x => x.EntityType)
            .IsInEnum()
            .WithErrorCode("FaqItem.InvalidEntityType");

        RuleFor(x => x.EntityId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("FaqItem.InvalidEntityId");

        RuleFor(x => x.Question)
            .NotEmpty().WithErrorCode("FaqItem.QuestionInvalid")
            .MaximumLength(500).WithErrorCode("FaqItem.QuestionInvalid");

        RuleFor(x => x.Answer)
            .NotEmpty().WithErrorCode("FaqItem.AnswerInvalid")
            .MaximumLength(5000).WithErrorCode("FaqItem.AnswerInvalid");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("FaqItem.OutOfRange");
    }
}
