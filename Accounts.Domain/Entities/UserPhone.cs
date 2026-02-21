using Accounts.Domain.ValueObjects;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace Accounts.Domain.Entities
{
    public sealed class UserPhone : BaseEntity<Guid>
    {
        private UserPhone() { } 

        private UserPhone(Guid userId, PhoneNumber phoneNumber, bool isPrimary) 
        {
            UserId = userId;
            Number = phoneNumber;
            IsPrimary = isPrimary;
            IsVerified = false;
            VerifiedAt = null;
        }

        public Guid UserId { get; private set; }
        public PhoneNumber Number { get; private set; } = default!;
        public bool IsPrimary { get; private set; }
        public bool IsVerified { get; private set; }
        public DateTime? VerifiedAt { get; private set; }

        
        public User User { get; private set; } = default!;

        public static Result<UserPhone> Create(Guid userId, PhoneNumber phoneNumber, bool isPrimary)
        {
            var phone = new UserPhone(userId, phoneNumber, isPrimary);
            return Result.Success(phone);
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
