using System.ComponentModel.DataAnnotations.Schema;
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

        [NotMapped]
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
        public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
        public void ClearDomainEvents() => _domainEvents.Clear();

        public bool IsTransient()
        {
            return EqualityComparer<TKey>.Default.Equals(Id, default);
        }

        public bool Equals(BaseEntity<TKey>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;

            if (IsTransient() || other.IsTransient())
                return false;

            return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
        }

        public override bool Equals(object? obj) => Equals(obj as BaseEntity<TKey>);
        
        public override int GetHashCode()
        {
            if (IsTransient())
            {
                return base.GetHashCode();
            }
            return EqualityComparer<TKey>.Default.GetHashCode(Id);
        }

        public static bool operator ==(BaseEntity<TKey>? left, BaseEntity<TKey>? right) => Equals(left, right);
        public static bool operator !=(BaseEntity<TKey>? left, BaseEntity<TKey>? right) => !Equals(left, right);
    }

    public abstract class BaseEntity : BaseEntity<Guid>
    {
        protected BaseEntity() { Id = Guid.CreateVersion7(); }
    }
}
