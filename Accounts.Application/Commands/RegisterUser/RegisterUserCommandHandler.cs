using Accounts.Domain.Entities;
using Accounts.Domain.Interfaces;
using Accounts.Domain.ValueObjects;
using Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Results;

namespace Accounts.Application.Commands.RegisterUser
{
    public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly AccountsDbContext _dbContext;
        private readonly IUnitOfWork<AccountsDbContext> _unitOfWork;

        public RegisterUserCommandHandler(
            IUserRepository userRepository,
            AccountsDbContext dbContext,
            IUnitOfWork<AccountsDbContext> unitOfWork)
        {
            _userRepository = userRepository;
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
           
            var emailResult = EmailAddress.Create(request.Email);
            if (emailResult.IsFailure)
                return Result.Failure<Guid>(emailResult.Error);           

            var emailExists = await _dbContext.UserEmails
                .AnyAsync(e => e.Address.Value == emailResult.Value.Value, cancellationToken);
            if (emailExists)
                return Result.Failure<Guid>(Error.Conflict("User.Email", "Email is already registered"));

            var userResult = User.Create(request.FirstName, request.LastName, emailResult.Value);
            if (userResult.IsFailure)
                return Result.Failure<Guid>(userResult.Error);

            await _userRepository.AddAsync(userResult.Value, cancellationToken);
           
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(userResult.Value.Id);
        }
    }
}
