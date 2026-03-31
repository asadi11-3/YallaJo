using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentCore.Application.Commands.Translation.ApproveTranslation
{
    public class ApproveTranslationCommandValidator :AbstractValidator<ApproveTranslationCommand>
    {
        public ApproveTranslationCommandValidator()
        {
            RuleFor(x => x.Id)
            .NotEmpty();

        }
    }
}
