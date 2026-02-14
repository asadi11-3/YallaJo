using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YallaJo.SharedKernel.Application.Abstractions.Results
{


  
    public sealed record Error(string Code, string Message)
    {
        
        public static Error Failure(string code, string message)
            => new(code, message);

     
        public static Error NotFound(string entity)
            => new($"{entity}.NotFound", $"{entity}.NotFound");

        
        public static Error Validation(string field, string reason)
            => new($"Validation.{field}.{reason}", $"Validation.{field}.{reason}");

        public static Error Conflict(string entity, string reason)
            => new($"{entity}.{reason}", $"{entity}.{reason}");

        public static Error Unauthorized(string reason = "Unauthorized")
            => new($"Auth.{reason}", $"Auth.{reason}");

        
        public static Error Forbidden(string reason = "Forbidden")
            => new($"Auth.{reason}", $"Auth.{reason}");
    }
}
