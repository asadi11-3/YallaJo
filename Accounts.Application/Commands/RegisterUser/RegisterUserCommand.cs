
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.RegisterUser
{
    public sealed record RegisterUserCommand(
    string FirstName,
    string LastName,
    string Email) : ICommand<Guid>;

}
