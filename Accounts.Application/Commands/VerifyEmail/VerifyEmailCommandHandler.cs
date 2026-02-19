using Accounts.Application.Specifications;
using Accounts.Domain.Events;
using Accounts.Domain.Interfaces;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Results;

namespace Accounts.Application.Commands.VerifyEmail
{
    public sealed class VerifyEmailCommandHandler : ICommandHandler<VerifyEmailCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IAccountsUnitOfWork _unitOfWork;

        public VerifyEmailCommandHandler(
            IUserRepository userRepository,
            IAccountsUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        {
           
            var userSpec = new UserWithEmailsSpecification(request.UserId);
            var user = await _userRepository.FirstOrDefaultAsync(userSpec, cancellationToken);

            if (user is null)
                return Result.Failure(Error.NotFound("User", "User not found"));

            var email = user.Emails.FirstOrDefault(e => e.Id == request.EmailId);
            if (email is null)
                return Result.Failure(Error.NotFound("UserEmail", "Email not found for this user"));

            email.MarkAsVerified();
            if (!user.IsActive)
            {
                var activationResult = user.Activate();
                if (activationResult.IsFailure)
                    return activationResult;
            }
           
            user.AddDomainEvent(new EmailVerifiedEvent(user.Id, email.Id, email.Address.Value));
            
             _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
