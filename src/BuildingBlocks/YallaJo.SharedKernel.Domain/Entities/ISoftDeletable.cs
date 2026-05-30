
namespace YallaJo.SharedKernel.Domain.Entities
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; }
        DateTime? DeletedAt { get; }
        void SoftDelete();
        void Restore();
    }

}
