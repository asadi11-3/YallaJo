# {ModuleName} Workflow Plan

> **Module**: {ModuleName} — {short description of module responsibility}
> **Status**: Plan — Not yet executed
> **Dependencies**: {list other modules this depends on, e.g. Accounts (ProviderApplication), ContentCore (Attachments)}
> **Compatible With**: {list plan docs this is compatible with}

---

## Design Decisions ({N} — ALL LOCKED)

| # | Decision | Detail |
|---|----------|--------|
| 1 | **{Decision title}** | {Rationale and implementation detail} |
| 2 | **{Decision title}** | {Rationale and implementation detail} |
| 3 | **{Decision title}** | {Rationale and implementation detail} |

---

## Current State Summary

### What's Built (COMPLETE)
- {N} entities: {list entity names}
- {N} enums: {list enum names with values}
- {N} endpoints across {N} groups, all correctly secured
- {N} validators, {N} command handlers, {N} query handlers
- {N} domain event handlers
- Integration events: {N} ({breakdown by category})
- {Other notable items: HybridCache, background jobs, etc.}

### What's NOT Built (Deferred / Planned)
- {Feature 1} — deferred to {reason/plan}
- {Feature 2} — requires {dependency}

### Gap Assessment
| Gap | Status | Action |
|-----|--------|--------|
| {Gap description} | ✅ RESOLVED / ❌ OPEN | {What was done or needs doing} |

---

## Domain Model

### Entities

```
{EntityName} ({N} lines)
  Properties: {list key properties}
  Methods: {list domain methods}
  Domain Events: {list raised events}
  Inherits: {AuditableEntity / BaseEntity}, {IAggregateRoot?}
```

### Enums

```csharp
public enum {EnumName}
{
    {Value1} = 0,
    {Value2} = 1,
    // ...
}
```

### Value Objects (if any)

```csharp
public record {ValueObjectName}({Type} {Property1}, {Type} {Property2});
```

---

## Application Layer

### Commands

| Command | Handler | Validator | Result |
|---------|---------|-----------|--------|
| `{CommandName}` | ✅ | ✅ | `{ResultType}` |
| `{CommandName}` | ✅ | ❌ | — |

### Queries

| Query | Handler | Cacheable | Tags |
|-------|---------|-----------|------|
| `{QueryName}` | ✅ | Yes | `{CacheTagKeys}` |
| `{QueryName}` | ✅ | No | — |

### Interfaces

```csharp
// Cross-module contracts exposed via {ModuleName}.Contracts
public interface I{ServiceName}
{
    Task<{ReturnType}> {MethodName}({params}, CancellationToken ct);
}
```

---

## Infrastructure

### EF Core Configurations

| Entity | Table | Schema | Notes |
|--------|-------|--------|-------|
| `{EntityName}` | `{TableName}` | `{schema}` | {unique indexes, FKs, query filters} |

### Repositories

| Interface | Implementation | Notes |
|-----------|----------------|-------|
| `I{Name}Repository` | `{Name}Repository` | {custom methods beyond CRUD} |

### Domain Event Handlers

| Domain Event | Handler | Integration Event Published |
|-------------|---------|----------------------------|
| `{EventName}DomainEvent` | `{EventName}DomainEventHandler` | `{EventName}IntegrationEvent` |

### Inbound Integration Event Handlers

| Integration Event (source) | Handler | Action |
|---------------------------|---------|--------|
| `{EventName}IntegrationEvent` from `{Module}` | `{HandlerName}` | {what it does} |

### Background Services

| Service | Interval | Purpose |
|---------|----------|---------|
| `{ServiceName}` | {every N min/h} | {purpose} |

---

## Presentation Layer

### Endpoint Groups

| Group | Route Prefix | Tag | Endpoints |
|-------|-------------|-----|-----------|
| `{GroupEndpoints}` | `/api/v1/{path}` | `{SwaggerTag}` | {N} |

### Endpoint Table

| Method | Route | Auth | Permission | Handler |
|--------|-------|------|------------|---------|
| `GET` | `/api/v1/{path}` | AllowAnonymous | — | `{QueryName}` |
| `POST` | `/api/v1/{path}` | Required | `{Feature}.{Action}` | `{CommandName}` |
| `PUT` | `/api/v1/{path}/{id}` | Required | `{Feature}.Update` | `{CommandName}` |
| `DELETE` | `/api/v1/{path}/{id}` | Required | `{Feature}.Delete` | `{CommandName}` |
| `PATCH` | `/api/v1/{path}/{id}/activate` | Required | `{Feature}.Update` | `{CommandName}` |

---

## Contracts

### Integration Events Published

| Event | Key (registry) | Properties |
|-------|---------------|------------|
| `{EventName}IntegrationEvent` | `{module}.{entity}.{action}.v1` | `{prop1}, {prop2}, ...` |

### Cross-Module Consumers

| Module | Events Consumed | Handler |
|--------|----------------|---------|
| `{ConsumerModule}` | `{EventName}` | `{HandlerName}` |

### Service Contracts (for other modules to consume)

```csharp
// In {ModuleName}.Contracts
public interface I{ServiceName}
{
    Task<bool> {MethodName}(Guid id, CancellationToken ct);
}
```

---

## Permissions

### Permission Catalog

| Feature | Actions | Roles |
|---------|---------|-------|
| `{FeatureName}` | Read, Create, Update, Delete | Provider, Creator, TourGuide, Admin |
| `{FeatureName}` | Read, Approve, Reject | Admin, SuperAdmin |

---

## Execution Plan

### Phase 1: Domain Layer
- [ ] Create entities: {list}
- [ ] Create enums: {list}
- [ ] Create domain events: {list}
- [ ] Add repository interfaces

### Phase 2: Application Layer
- [ ] Create commands + handlers + validators
- [ ] Create queries + handlers
- [ ] Add cache keys file: `{ModuleName}CacheKeys.cs`

### Phase 3: Infrastructure
- [ ] EF configurations for all entities
- [ ] Add DbSets to `{ModuleName}DbContext`
- [ ] Implement repositories
- [ ] Create domain event handlers (outbox writers)
- [ ] Add DI registrations to `DependencyInjection.cs`
- [ ] Add migration: `dotnet ef migrations add {MigrationName} --project {ModuleName}.Infrastructure`

### Phase 4: Contracts
- [ ] Create integration event records
- [ ] Register events in `IntegrationEventTypeRegistry.cs`
- [ ] Expose cross-module service interfaces

### Phase 5: Presentation
- [ ] Create endpoint files per group
- [ ] Create request model DTOs
- [ ] Wire into `{ModuleName}Endpoints.cs`

### Phase 6: Inbound Handlers
- [ ] Create handlers for incoming integration events from other modules
- [ ] Register in DI

### Phase 7: Background Services (if any)
- [ ] Create service + options class
- [ ] Register as HostedService in DI
- [ ] Add config binding

### Phase 8: Permissions
- [ ] Create `{ModuleName}PermissionCatalog.cs` in Contracts
- [ ] Register catalog in DI
- [ ] Update `RolePermissionMapping.cs` if needed

### Phase 9: Build & Verify
- [ ] `dotnet build YallaJo.sln --no-restore` → 0 errors
- [ ] Verify all endpoints return correct HTTP status codes
- [ ] Verify cache invalidation on mutations

---

## File Count Estimate

| Layer | New Files | Modified Files |
|-------|-----------|----------------|
| Domain (entities, enums, events) | ~{N} | ~{N} |
| Application (commands, queries, interfaces) | ~{N} | ~{N} |
| Infrastructure (configs, repos, handlers, services) | ~{N} | ~{N} |
| Contracts (events, interfaces) | ~{N} | ~{N} |
| Presentation (endpoints, models) | ~{N} | ~{N} |
| **Total** | **~{N}** | **~{N}** |

---

## Cross-Module Impact

| Affected Plan | Impact |
|--------------|--------|
| {PlanName} | {how this module affects it} |
| {PlanName} | {how this module affects it} |

---

## Risk Assessment

| Risk | Mitigation |
|------|-----------|
| {Risk description} | {How to mitigate} |
| {Risk description} | {How to mitigate} |

---

## Future Extensions (NOT in MVP)

- [ ] {Future feature 1}
- [ ] {Future feature 2}
- [ ] {Future feature 3}

---

## Implementation Notes

> Added during codebase audit — reflects actual implementation details not originally in the plan.

1. **{Note title}**: {detail}
2. **{Note title}**: {detail}
