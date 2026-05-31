using Messaging.Domain.Enums;

namespace Messaging.Presentation.Endpoints.SupportTicket.Models;

internal sealed record CreateTicketRequest(TicketCategory Category, string Subject, string Body);
