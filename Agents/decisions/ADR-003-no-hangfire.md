# ADR-003: BackgroundService + Channel<T> Instead of Hangfire/Quartz

**Date**: 2025  
**Status**: Accepted  
**Deciders**: Project owner

## Context

Need background processing for media (image thumbnails, video metadata), outbox processing, and scheduled tasks.

## Decision

Use .NET's built-in **BackgroundService** with **Channel\<T\>** for in-memory job queuing. No Hangfire, no Quartz.

## Rationale

- **Zero dependencies** — no additional NuGet packages, no database tables for job storage
- **Bounded channels** — built-in backpressure prevents memory exhaustion
- **Simplicity** — producers write to channel, single consumer processes items
- **Sufficient for current scale** — single server deployment doesn't need distributed job queues
- **Type-safe** — strongly typed job items, no serialization overhead

## Consequences

- **Jobs are lost on app restart** — in-memory queue means pending jobs disappear if the process dies. Acceptable for media processing (can be re-triggered). Not acceptable for critical financial operations.
- **No retry with exponential backoff** — must implement manually if needed
- **No dashboard** — no Hangfire-style UI to monitor jobs
- **Single consumer** — can be extended to multiple consumers per channel if throughput requires it

## When to Reconsider

- If the app moves to multiple server instances (channels are per-process)
- If critical jobs (payments, notifications) need guaranteed delivery — consider outbox pattern or external queue (Azure Service Bus)

## Alternatives Considered

- **Hangfire**: Rejected — adds DB dependency for job storage, heavier than needed
- **Quartz.NET**: Rejected — overkill for current requirements
- **Azure Service Bus**: Deferred — will consider when scaling to multiple instances
