namespace Messaging.Presentation.Endpoints.SupportTicket.Models;

internal sealed record ResolveTicketRequest(string? Notes, string? RowVersion);
