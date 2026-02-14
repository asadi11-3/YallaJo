using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YallaJo.SharedKernel.Application.Abstractions.Results
{


    public sealed class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public Outcome Outcome { get; }
        public IReadOnlyList<string> Messages { get; }
        public IReadOnlyList<Error> Errors { get; }

        private Result(
            bool isSuccess,
            Outcome outcome,
            IReadOnlyList<string>? messages = null,
            IReadOnlyList<Error>? errors = null)
        {
            IsSuccess = isSuccess;
            Outcome = outcome;
            Messages = messages ?? Array.Empty<string>();
            Errors = errors ?? Array.Empty<Error>();
        }

      

        public static Result Success(params string[] messages)
            => new(true, Outcome.Ok, messages: messages);

        public static Result Created(params string[] messages)
            => new(true, Outcome.Created, messages: messages);

      

        public static Result Failure(Error error, Outcome outcome = Outcome.Invalid)
            => new(false, outcome, errors: new[] { error });

        public static Result NotFound(string? message = null)
            => new(false, Outcome.NotFound,
                messages: message != null ? new[] { message } : null);

        public static Result Unauthorized(string? message = null)
            => new(false, Outcome.Unauthorized,
                messages: message != null ? new[] { message } : null);

        public static Result Forbidden(string? message = null)
            => new(false, Outcome.Forbidden,
                messages: message != null ? new[] { message } : null);

        public static Result Conflict(string? message = null)
            => new(false, Outcome.Conflict,
                messages: message != null ? new[] { message } : null);

        public static Result ServerError(string? message = null)
            => new(false, Outcome.ServerError,
                messages: message != null ? new[] { message } : null);

        public static Result Canceled(params string[] messages)
            => new(false, Outcome.Canceled, messages: messages);

       

       
        public static Result Invalid(params Error[] errors)
            => new(false, Outcome.Invalid, errors: errors);

       
        public static Result Fail(Outcome outcome, params Error[] errors)
            => new(false, outcome, errors: errors);

        public static Result Fail(Outcome outcome, string message, params Error[] errors)
            => new(false, outcome, messages: new[] { message }, errors: errors);

      

        public static implicit operator Result(Error error)
            => Failure(error);

        public static implicit operator bool(Result result)
            => result.IsSuccess;
    }
}
