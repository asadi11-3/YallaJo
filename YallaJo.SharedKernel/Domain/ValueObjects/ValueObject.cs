using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YallaJo.SharedKernel.Domain.ValueObjects
{
    public abstract class ValueObject : IEquatable<ValueObject>
    {
        protected abstract IEnumerable<object?> GetEqualityComponents();

        public bool Equals(ValueObject? other)
        {
            if (other is null || other.GetType() != GetType()) return false;
            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override bool Equals(object? obj) => Equals(obj as ValueObject);

        public override int GetHashCode()
            => GetEqualityComponents()
                .Aggregate(0, (hash, component) => HashCode.Combine(hash, component?.GetHashCode() ?? 0));

        public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);
        public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);
    }
}
