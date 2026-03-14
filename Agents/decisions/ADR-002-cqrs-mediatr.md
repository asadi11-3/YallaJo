# ADR-002: CQRS with MediatR

**Date**: 2025 (project inception)  
**Status**: Accepted  
**Deciders**: Project owner

## Context

Need a consistent pattern for handling commands (writes) and queries (reads) across all modules.

## Decision

Use **CQRS** pattern implemented via **MediatR** with pipeline behaviors.

## Rationale

- **Separation of concerns** — commands and queries have different models, optimized independently
- **Pipeline behaviors** — cross-cutting concerns (validation, logging, performance) applied automatically
- **Testability** — handlers are small, focused, and easily unit-tested
- **Consistency** — every module follows the same ICommand/IQuery/ICommandHandler/IQueryHandler pattern
- **Domain events** — MediatR's INotification provides clean domain event dispatching

## Consequences

- Every operation requires a Command/Query record, a Handler class, and often a Validator — more files but more clarity
- MediatR adds a small per-request overhead (negligible at expected scale)
- Pipeline behaviors run on EVERY request — must be lightweight

## Alternatives Considered

- **Direct service classes**: Rejected — no pipeline, inconsistent patterns across teams/agents
- **Wolverine**: Rejected — less mature ecosystem, smaller community for troubleshooting
