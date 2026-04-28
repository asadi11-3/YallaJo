namespace YallaJo.SharedKernel.Domain.Abstractions.Results
{
    public enum Outcome
    {
        Ok = 200,
        Created = 201,
        Invalid = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        Conflict = 409,
        UnprocessableEntity = 422,
        ServerError = 500,
        TooManyRequests = 429,
        Canceled = 499
    }
}
