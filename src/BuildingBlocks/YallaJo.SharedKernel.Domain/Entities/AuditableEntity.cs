using System.ComponentModel.DataAnnotations;

namespace YallaJo.SharedKernel.Domain.Entities
{
    public abstract class AuditableEntity<TKey> : BaseEntity<TKey>, ISoftDeletable
        where TKey : notnull
    {
        public bool IsDeleted { get; protected set; }
        public DateTime? DeletedAt { get; protected set; }

        [Timestamp]
        public byte[] RowVersion { get; protected set; } = [];

        public void SoftDelete()
        {
            if (IsDeleted) return;

            var now = DateTime.UtcNow;
            IsDeleted = true;
            DeletedAt = now;
            UpdatedAt = now;
        }

        public void Restore()
        {
            if (!IsDeleted) return;

            IsDeleted = false;
            DeletedAt = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkUpdated() => UpdatedAt = DateTime.UtcNow;
    }

    public abstract class AuditableEntity : AuditableEntity<Guid>
    {
        protected AuditableEntity() { Id = Guid.CreateVersion7(); }
    }
}
