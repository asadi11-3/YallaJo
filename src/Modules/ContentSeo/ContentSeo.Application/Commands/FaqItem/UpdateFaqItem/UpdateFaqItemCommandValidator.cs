// <copyright file="UpdateFaqItemCommandValidator.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.FaqItem.UpdateFaqItem;

using FluentValidation;

public sealed class UpdateFaqItemCommandValidator : AbstractValidator<UpdateFaqItemCommand>
{
    public UpdateFaqItemCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty).WithErrorCode("FaqItem.NotFound");
        RuleFor(x => x.Question).NotEmpty().MaximumLength(500).WithErrorCode("FaqItem.QuestionInvalid");
        RuleFor(x => x.Answer).NotEmpty().MaximumLength(5000).WithErrorCode("FaqItem.AnswerInvalid");
    }
}
