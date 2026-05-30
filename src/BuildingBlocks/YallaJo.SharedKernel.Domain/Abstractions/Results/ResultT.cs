using System.Text.Json.Serialization;

namespace YallaJo.SharedKernel.Domain.Abstractions.Results
{
    public sealed class Result<T>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public Outcome Outcome { get; }
        public T? Value { get; }
        public IReadOnlyList<string> Messages { get; }
        public IReadOnlyList<Error> Errors { get; }

        public Error? Error => Errors.FirstOrDefault();

        [JsonConstructor]
        public Result(
            bool isSuccess,
            Outcome outcome,
            T? value,
            IReadOnlyList<string>? messages,
            IReadOnlyList<Error>? errors)
        {
            IsSuccess = isSuccess;
            Outcome = outcome;
            Value = value;
            Messages = messages ?? Array.Empty<string>();
            Errors = errors ?? Array.Empty<Error>();
        }



        public static Result<T> Success(T value, params string[] messages)
            => new(true, Outcome.Ok, value, messages, null);

        public static Result<T> Created(T value, params string[] messages)
            => new(true, Outcome.Created, value, messages, null);

        public static Result<T> Failure(Error error, Outcome outcome = Outcome.Invalid)
            => new(false, outcome, default, null, new[] { error });

        public static Result<T> NotFound(string? message = null)
            => new(false, Outcome.NotFound, default,
                message != null ? new[] { message } : null, null);

        public static Result<T> Unauthorized(string? message = null)
            => new(false, Outcome.Unauthorized, default,
                message != null ? new[] { message } : null, null);

        public static Result<T> Forbidden(string? message = null)
            => new(false, Outcome.Forbidden, default,
                message != null ? new[] { message } : null, null);

        public static Result<T> Conflict(string? message = null)
            => new(false, Outcome.Conflict, default,
                message != null ? new[] { message } : null, null);

        public static Result<T> Conflict(Error error)
            => new(false, Outcome.Conflict, default, null, new[] { error });

        public static Result<T> ServerError(string? message = null)
            => new(false, Outcome.ServerError, default,
                message != null ? new[] { message } : null, null);

        public static Result<T> Canceled(params string[] messages)
            => new(false, Outcome.Canceled, default, messages, null);

        public static Result<T> Invalid(params Error[] errors)
            => new(false, Outcome.Invalid, default, null, errors);

        public static Result<T> Fail(Outcome outcome, params Error[] errors)
            => new(false, outcome, default, null, errors);

        public static Result<T> Fail(Outcome outcome, string message, params Error[] errors)
            => new(false, outcome, default, new[] { message }, errors);

        public static Result<T> Fail(Outcome outcome, string message, T? data)
            => new(false, outcome, data, new[] { message }, null);

        public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        {
            if (IsFailure)
                return Result<TNew>.Fail(Outcome, Messages.FirstOrDefault() ?? string.Empty, Errors.ToArray());
            return Result<TNew>.Success(mapper(Value!), Messages.ToArray());
        }

        public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
        {
            if (IsFailure)
                return Result<TNew>.Fail(Outcome, Messages.FirstOrDefault() ?? string.Empty, Errors.ToArray());
            return binder(Value!);
        }

        public Result<T> OnSuccess(Action<T> action)
        {
            if (IsSuccess && Value is not null) action(Value);
            return this;
        }

        public Result<T> OnFailure(Action<Result<T>> action)
        {
            if (IsFailure) action(this);
            return this;
        }

        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Result<T>, TResult> onFailure)
            => IsSuccess ? onSuccess(Value!) : onFailure(this);

        public static implicit operator Result<T>(T value) => Success(value);
        public static implicit operator Result<T>(Error error) => Failure(error);
        public static implicit operator bool(Result<T> result) => result.IsSuccess;
    }
}
