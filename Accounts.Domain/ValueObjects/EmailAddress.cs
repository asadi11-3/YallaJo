
using YallaJo.SharedKernel.Application.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Accounts.Domain.ValueObjects
{
    public sealed class EmailAddress : ValueObject
    {
        private EmailAddress(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static Result<EmailAddress> Create(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Result.Failure<EmailAddress>(Error.Validation("Email", "Email cannot be empty"));

            email = email.Trim().ToLowerInvariant();

            if (!IsValidEmail(email))
                return Result.Failure<EmailAddress>(Error.Validation("Email", "Invalid email format"));

            return Result.Success(new EmailAddress(email));
        }

        private static bool IsValidEmail(string email)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
    }
