namespace ContentTours.Presentation.Endpoints.Tour.Models;

public sealed record SuspendTourRequest(byte[] RowVersion, string Reason);
