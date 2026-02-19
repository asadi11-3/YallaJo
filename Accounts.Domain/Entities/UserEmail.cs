using Accounts.Domain.ValueObjects;
using YallaJo.SharedKernel.Application.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace Accounts.Domain.Entities
{
    public sealed class UserEmail : BaseEntity<Guid>
    {
        private UserEmail() { } // EF Core

        private UserEmail(Guid userId, EmailAddress emailAddress, bool isPrimary) 
        {
            UserId = userId;
            Address = emailAddress;
            IsPrimary = isPrimary;
            IsVerified = false;
            VerifiedAt = null;
        }

        public Guid UserId { get; private set; }
        public EmailAddress Address { get; private set; }
        public bool IsPrimary { get; private set; }
        public bool IsVerified { get; private set; }
        public DateTime? VerifiedAt { get; private set; }

       
        public User User { get; private set; }

        public static Result<UserEmail> Create(Guid userId, EmailAddress emailAddress, bool isPrimary)
        {
            var email = new UserEmail( userId, emailAddress, isPrimary);
            return Result.Success(email);
        }

        public void MarkAsVerified()
        {
            IsVerified = true;
            VerifiedAt = DateTime.UtcNow;
        }

        public void SetPrimary(bool isPrimary)
        {
            IsPrimary = isPrimary;
        }
    }
}
