namespace YallaJo.SharedKernel.Application.Abstractions.Results
{
    public sealed record Error(string Code, string Message)
    {
        public static Error Failure(string code, string message)
            => new(code, message);

        public static Error NotFound(string entity)
            => new($"{entity}.NotFound", $"{entity} was not found.");

        public static Error NotFound(string field, string message)
          => new($"NotFound.{field}", message);
        public static Error Validation(string field, string message)
            => new($"Validation.{field}", message);

        public static Error Conflict(string entity, string message)
            => new($"{entity}.Conflict", message);

        public static Error Unauthorized(string message = "Unauthorized")
            => new("Auth.Unauthorized", message);

        public static Error Forbidden(string message = "Forbidden")
            => new("Auth.Forbidden", message);
    }
}
