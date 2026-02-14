using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Event;

namespace YallaJo.SharedKernel.Domain.Entities
{
    public abstract class BaseEntity<TKey> : IEquatable<BaseEntity<TKey>>
     where TKey : notnull
    {
        public TKey Id { get; protected set; } = default!;
        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; protected set; }

        private readonly List<IDomainEvent> _domainEvents = [];
        public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
        public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
        public void ClearDomainEvents() => _domainEvents.Clear();

        public bool Equals(BaseEntity<TKey>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
        }

        public override bool Equals(object? obj) => Equals(obj as BaseEntity<TKey>);
        public override int GetHashCode() => EqualityComparer<TKey>.Default.GetHashCode(Id);
        public static bool operator ==(BaseEntity<TKey>? left, BaseEntity<TKey>? right) => Equals(left, right);
        public static bool operator !=(BaseEntity<TKey>? left, BaseEntity<TKey>? right) => !Equals(left, right);
    }

    public abstract class BaseEntity : BaseEntity<Guid>
    {
        protected BaseEntity() { Id = Guid.CreateVersion7(); }
    }
}
