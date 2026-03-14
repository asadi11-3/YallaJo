# ADR-001: Modular Monolith Architecture

**Date**: 2025 (project inception)  
**Status**: Accepted  
**Deciders**: Project owner

## Context

YallaJo is a tourism/booking platform for Jordan. It needs multiple domains (content, booking, finance, messaging, social, analytics) that share a database but must remain independently maintainable.

## Decision

Use a **Modular Monolith** architecture with Clean Architecture per module instead of microservices.

## Rationale

- **Single deployment unit** — simpler ops for a startup-stage product
- **Module isolation via separate schemas** — each module has its own DB schema, preventing accidental coupling
- **Migration path to microservices** — modules communicate via Contracts and integration events, making future extraction possible
- **Shared kernel** — common abstractions (Result, CQRS, events, repositories) live in SharedKernel, reducing duplication
- **No distributed system complexity** — no service mesh, no API gateway, no distributed transactions at this stage

## Consequences

- Must enforce module boundaries via project references (Presentation → Application only)
- Cross-module communication MUST go through Contracts projects or integration events
- Each module has its own DbContext with its own schema
- Cannot independently scale individual modules (acceptable for current scale)

## Alternatives Considered

- **Microservices**: Rejected — too much operational overhead for team size and scale
- **Traditional layered monolith**: Rejected — no module isolation, harder to evolve
