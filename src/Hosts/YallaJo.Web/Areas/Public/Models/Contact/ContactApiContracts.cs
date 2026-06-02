namespace YallaJo.Web.Areas.Public.Models.Contact;

/// <summary>
/// Outbound payload for POST /api/v1/support/tickets.
/// The API takes the creator from the authenticated token, so no name/email is sent.
/// Category is the string name of the TicketCategory enum.
/// </summary>
public sealed record CreateTicketRequest(string Category, string Subject, string Body);
