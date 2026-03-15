using ContentCore.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Domain.Events;

public sealed record AttachmentDeletedDomainEvent(
    Guid AttachmentId,
    EntityType EntityType,
    Guid EntityId,
    AttachmentType AttachmentType,
    string Url) : DomainEventBase;
