using Accounts.Domain.Events;
using Accounts.Domain.ValueObjects;
using YallaJo.SharedKernel.Application.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace Accounts.Domain.Entities
{
    public sealed class User : AuditableEntity, IAggregateRoot
    {
        private readonly List<UserEmail> _emails = new();
        private readonly List<UserPhone> _phones = new();

        private User() { } 

        private User(string firstName, string lastName)  
        {
            FirstName = firstName;
            LastName = lastName;
            IsActive = false;
        }

        public string FirstName { get; private set; } = default!;
        public string LastName { get; private set; } = default!;

        public bool IsActive { get; private set; }

        public IReadOnlyCollection<UserEmail> Emails => _emails.AsReadOnly();
        public IReadOnlyCollection<UserPhone> Phones => _phones.AsReadOnly();

        
        public static Result<User> Create(string firstName, string lastName, EmailAddress emailAddress)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                return Result.Failure<User>(Error.Validation("User.FirstName", "First name is required"));

            if (string.IsNullOrWhiteSpace(lastName))
                return Result.Failure<User>(Error.Validation("User.LastName", "Last name is required"));

            var user = new User(firstName, lastName);

            var emailResult = UserEmail.Create(user.Id, emailAddress, isPrimary: true);
            if (emailResult.IsFailure || emailResult.Value is null)
            {
                return Result.Failure<User>(
                    emailResult.Error ?? Error.Validation("User.Email", "Email is invalid"));
            }

            user._emails.Add(emailResult.Value);

            user.AddDomainEvent(new UserRegisteredEvent(user.Id, emailAddress.Value));

            return Result.Success(user); 
        }
      

        public Result Activate()
        {
            if (!_emails.Any(e => e.IsVerified))
                return Result.Failure(Error.Validation("User.Activation", "User must have at least one verified email"));

            IsActive = true;
            return Result.Success();
        }

        public Result AddEmail(EmailAddress emailAddress, bool isPrimary = false)
        {
            if (_emails.Any(e => e.Address.Equals(emailAddress)))
                return Result.Failure(Error.Conflict("User.Email", "Email already added to this user"));

            var emailResult = UserEmail.Create(Id, emailAddress, isPrimary);
            if (emailResult.IsFailure || emailResult.Value is null)
            {
                return Result.Failure(
                    emailResult.Error ?? Error.Validation("User.Email", "Email is invalid"));
            }

            _emails.Add(emailResult.Value);
            return Result.Success();
        }

        public Result SetPrimaryEmail(Guid emailId)
        {
            var email = _emails.FirstOrDefault(e => e.Id == emailId);
            if (email is null)
                return Result.Failure(Error.NotFound("User.Email", "Email not found"));

            if (!email.IsVerified)
                return Result.Failure(Error.Validation("User.Email", "Cannot set unverified email as primary"));

            foreach (var e in _emails)
                e.SetPrimary(false);

            email.SetPrimary(true);
            return Result.Success();
        }

    }
}
