using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Event;

namespace YallaJo.SharedKernel.Domain.Entities
{
    public interface IAggregateRoot
    {
        /// <summary>
        /// Domain Events للـ Aggregate
        /// </summary>
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

        /// <summary>
        /// مسح الـ Events (بعد النشر)
        /// </summary>
        void ClearDomainEvents();
    }

}
