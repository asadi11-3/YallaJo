using Accounts.Domain.Entities;
using Accounts.Domain.Interfaces;
using Accounts.Domain.ValueObjects;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Results;

namespace Accounts.Application.Commands.RegisterUser
{
    public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IAccountsUnitOfWork _unitOfWork;

        public RegisterUserCommandHandler(
            IUserRepository userRepository,
            IAccountsUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var emailResult = EmailAddress.Create(request.Email);
            if (emailResult.IsFailure || emailResult.Value is null)
            {
                return Result.Failure<Guid>(
                    emailResult.Error ?? Error.Validation("User.Email", "Invalid email"));
            }

            var emailAddress = emailResult.Value;

            var emailExists = await _userRepository.AnyAsync(
                u => u.Emails.Any(e => e.Address.Value == emailAddress.Value),
                cancellationToken);

            if (emailExists)
                return Result.Failure<Guid>(Error.Conflict("User.Email", "Email is already registered"));

            var userResult = User.Create(request.FirstName, request.LastName, emailAddress);
            if (userResult.IsFailure || userResult.Value is null)
            {
                return Result.Failure<Guid>(
                    userResult.Error ?? Error.Validation("User", "Unable to create user"));
            }

            await _userRepository.AddAsync(userResult.Value, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(userResult.Value.Id);
        }
    }
}
