using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YallaJo.SharedKernel.Domain.Exceptions
{
    public abstract class DomainException : Exception
    {
        public string Code { get; }
        protected DomainException(string code, string message) : base(message) { Code = code; }
    }

    public sealed class EntityNotFoundException : DomainException
    {
        public EntityNotFoundException(string entityName, object key)
            : base("ENTITY_NOT_FOUND", $"{entityName} with key '{key}' was not found.") { }
    }

    public sealed class BusinessRuleViolationException : DomainException
    {
        public BusinessRuleViolationException(string message)
            : base("BUSINESS_RULE_VIOLATION", message) { }
    }

    public sealed class ConcurrencyException : DomainException
    {
        public ConcurrencyException(string entityName)
            : base("CONCURRENCY_CONFLICT", $"{entityName} has been modified by another user.") { }
    }
}
