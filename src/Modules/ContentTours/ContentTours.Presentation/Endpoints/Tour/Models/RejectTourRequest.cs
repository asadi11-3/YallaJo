namespace ContentTours.Presentation.Endpoints.Tour.Models;

public sealed record RejectTourRequest(byte[] RowVersion, string Reason);
