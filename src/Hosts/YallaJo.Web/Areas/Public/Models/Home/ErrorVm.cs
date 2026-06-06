namespace YallaJo.Web.Areas.Public.Models.Home;

public sealed class ErrorVm
{
    public int StatusCode { get; init; }

    public string Title { get; init; } = "Something went wrong";

    public string Message { get; init; } = "We could not complete your request. Please try again later.";

    public string CorrelationId { get; init; } = string.Empty;
}
