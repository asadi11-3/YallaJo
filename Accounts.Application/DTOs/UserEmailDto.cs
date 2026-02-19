

namespace Accounts.Application.DTOs
{
    public sealed record UserEmailDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public bool IsPrimary { get; init; }
        public bool IsVerified { get; init; }
        public DateTime? VerifiedAt { get; init; }
    }

}
