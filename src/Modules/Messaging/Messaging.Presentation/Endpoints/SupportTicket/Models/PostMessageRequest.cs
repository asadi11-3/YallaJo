namespace Messaging.Presentation.Endpoints.SupportTicket.Models;

internal sealed record PostMessageRequest(string Body, bool? IsInternal);
