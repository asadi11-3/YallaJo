
using YallaJo.SharedKernel.Application.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Accounts.Domain.ValueObjects
{
    public sealed class PhoneNumber : ValueObject
    {
        private PhoneNumber(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static Result<PhoneNumber> Create(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return Result.Failure<PhoneNumber>(Error.Validation("Phone", "Phone number cannot be empty"));

            phone = phone.Trim();

          
            if (phone.Length < 10 || phone.Length > 15)
                return Result.Failure<PhoneNumber>(Error.Validation("Phone", "Phone number must be 10-15 digits"));

            return Result.Success(new PhoneNumber(phone));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
