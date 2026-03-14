using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Specialization.CreateTag
{
	public sealed record CreateSpecializationCommand(
	string Name,
	string? Description,
	string? Icon) : ICommand<Guid>;
}
