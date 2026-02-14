using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YallaJo.SharedKernel.Infrastructure.Outbox
{
    public interface IOutboxProcessor
    {
       
        Task ProcessOutboxMessagesAsync(CancellationToken ct = default);
    }
}
