using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Specialization.UpdateSpecialization
{
	public sealed record UpdateSpecializationCommand(
	Guid Id,
	string Name,
	string? Description,
	string? Icon,
	bool IsActive) : ICommand<Guid>;
}
