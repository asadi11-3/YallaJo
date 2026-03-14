# ADR-004: Result Pattern Instead of Exceptions for Business Logic

**Date**: 2025  
**Status**: Accepted  
**Deciders**: Project owner

## Context

Need a consistent way to handle success/failure in command and query handlers without relying on exceptions for control flow.

## Decision

Use a custom **Result\<T\>** pattern. All handlers return `Result<T>` with typed outcomes (Success, Created, NotFound, Conflict, Validation).

## Rationale

- **No exceptions for business logic** — exceptions are for exceptional circumstances, not "user not found"
- **Explicit error handling** — callers must handle the Result, can't accidentally ignore failures
- **Typed outcomes** — `Result.NotFound()`, `Result.Conflict()` map directly to HTTP status codes
- **Error codes** — `Error(string Code, string Message)` provides machine-readable error identification
- **Composable** — handlers can chain results without try/catch nesting

## Consequences

- Every handler's return type is `Task<Result<T>>` — slightly more verbose than raw returns
- Endpoint mapping (`ToApiResult()`) translates Result outcomes to HTTP status codes
- FluentValidation still throws `ValidationException` (caught by `ValidationExceptionHandler`) — this is the only exception-based flow

## Alternatives Considered

- **Throwing exceptions for all errors**: Rejected — performance cost of exception throwing, unclear what's "expected" vs "unexpected"
- **OneOf/discriminated unions**: Rejected — less readable, less established in .NET ecosystem
- **FluentResults**: Rejected — custom implementation gives full control over the Error record shape
