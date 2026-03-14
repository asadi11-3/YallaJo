using FluentValidation;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentCore.Application.Commands.Tag.CreateTag
{
	public class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
	{
		// 👇 عملنا Constructor هون عشان نحط جواته الرولز
		public CreateTagCommandValidator()
		{
			RuleFor(c => c.Name)
				.NotEmpty().WithMessage("Tag name required")
				.MaximumLength(100).WithMessage("The tag name cannot exceed 100 characters"); 

			RuleFor(c => c.Slug)
				.NotEmpty().WithMessage("Slug is required")
				.MaximumLength(100).WithMessage("The slug cannot exceed 100 characters")
				.Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("The slug must be in the correct format (lowercase letters, numbers, and dashes)");
		}
	}
}