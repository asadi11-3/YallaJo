using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YallaJo.SharedKernel.Domain.ValueObjects
{
    public sealed class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        private Money() { Currency = "JOD"; }

        public Money(decimal amount, string currency = "JOD")
        {
            if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
            if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
                throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));
            Amount = Math.Round(amount, 2);
            Currency = currency.ToUpperInvariant();
        }

        public static Money Zero(string currency = "JOD") => new(0, currency);
        public Money Add(Money other) { EnsureSameCurrency(other); return new Money(Amount + other.Amount, Currency); }
        public Money Subtract(Money other) { EnsureSameCurrency(other); return new Money(Amount - other.Amount, Currency); }
        public Money MultiplyBy(decimal factor) => new(Amount * factor, Currency);

        public Money ApplyDiscount(decimal percentage)
        {
            if (percentage is < 0 or > 100) throw new ArgumentException("Percentage must be between 0 and 100.");
            return new Money(Amount * (1 - percentage / 100), Currency);
        }

        private void EnsureSameCurrency(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException($"Cannot operate on different currencies: {Currency} vs {other.Currency}");
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }

        public override string ToString() => $"{Amount:F2} {Currency}";
    }

}
