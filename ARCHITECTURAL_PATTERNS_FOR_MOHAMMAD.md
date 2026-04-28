# YallaJo Architectural Patterns for Mohammad - Tasks 2 & 3

## CRITICAL PATHS & FILES

### 1. BASE CLASSES (SharedKernel)
- BaseEntity: YallaJo.SharedKernel.Domain\Entities\BaseEntity.cs
- AuditableEntity: YallaJo.SharedKernel.Domain\Entities\AuditableEntity.cs
- IAggregateRoot: YallaJo.SharedKernel.Domain\Entities\IAggregateRoot.cs

### 2. RESULT TYPES (SharedKernel)
- Result<T>: YallaJo.SharedKernel.Domain\Abstractions\Results\ResultT.cs
- Error: YallaJo.SharedKernel.Domain\Abstractions\Results\Error.cs
- Outcome: YallaJo.SharedKernel.Domain\Abstractions\Results\Outcome.cs

### 3. UNIT OF WORK (SharedKernel)
- IUnitOfWork: YallaJo.SharedKernel.Domain\Abstractions\Data\IUnitOfWork.cs
- IUnitOfWork<TContext>: YallaJo.SharedKernel.Infrastructure\Data\IUnitOfWorkTyped.cs
- UnitOfWork<TContext>: YallaJo.SharedKernel.Infrastructure\Data\UnitOfWork.cs
- IContentToursUnitOfWork: ContentTours.Application\Interfaces\IContentToursUnitOfWork.cs
- IContentToursEventUnitOfWork: ContentTours.Application\Interfaces\IContentToursEventUnitOfWork.cs

### 4. OUTBOX PATTERN (SharedKernel)
- OutboxMessage: YallaJo.SharedKernel.Infrastructure\Outbox\OutboxMessage.cs
- IntegrationEventTypeRegistry: YallaJo.SharedKernel.Infrastructure\Abstractions\Integration\IntegrationEventTypeRegistry.cs
- IContentToursOutboxWriter: ContentTours.Application\Interfaces\IContentToursOutboxWriter.cs

### 5. CACHING (SharedKernel)
- ICacheableQuery: YallaJo.SharedKernel.Application\Abstractions\Messaging\ICacheableQuery.cs
- QueryCachingBehavior: YallaJo.SharedKernel.Application\Abstractions\Behaviors\QueryCachingBehavior.cs
- TourCacheKeys: ContentTours.Application\Caching\TourCacheKeys.cs

### 6. MEDIATR (SharedKernel)
- ICommand: YallaJo.SharedKernel.Application\Abstractions\Messaging\ICommand.cs
- IQuery: YallaJo.SharedKernel.Application\Abstractions\Messaging\IQuery.cs
- ICommandHandler: YallaJo.SharedKernel.Application\Abstractions\Messaging\ICommandHandler.cs
- IQueryHandler: YallaJo.SharedKernel.Application\Abstractions\Messaging\IQueryHandler.cs
- ValidationBehavior: YallaJo.SharedKernel.Application\Abstractions\Behaviors\ValidationBehavior.cs

### 7. ENDPOINTS (SharedKernel)
- MustHavePermissionAttribute: YallaJo.SharedKernel.Presentation\Authorization\MustHavePermissionAttribute.cs
- ResultExtensions: YallaJo.SharedKernel.Presentation\ResultExtensions.cs

### 8. CURRENTUSER (SharedKernel)
- ICurrentUser: YallaJo.SharedKernel.Application\Abstractions\Context\ICurrentUser.cs

### 9. REPOSITORY (SharedKernel)
- IRepository: YallaJo.SharedKernel.Domain\Abstractions\Data\IRepository.cs
- EfRepository: YallaJo.SharedKernel.Infrastructure\Data\Repositories\EfRepository.cs

### 10. DOMAIN EVENTS (SharedKernel)
- IDomainEvent: YallaJo.SharedKernel.Domain\Events\IDomainEvent.cs
- DomainEventNotification: YallaJo.SharedKernel.Application\Abstractions\Messaging\DomainEventNotification.cs

### 11. INTEGRATION EVENTS (SharedKernel)
- IntegrationEventBase: YallaJo.SharedKernel.Domain\Events\IntegrationEvent.cs

### 12. DBCONTEXT (ContentTours)
- ContentToursDbContext: ContentTours.Infrastructure\Persistence\ContentToursDbContext.cs

### 13. PERMISSIONS (ContentTours)
- ContentToursFeatures: ContentTours.Contracts\Authorization\ContentToursFeatures.cs
- ContentToursPermissionCatalog: ContentTours.Contracts\Authorization\ContentToursPermissionCatalog.cs

---

## KEY PATTERNS

### Pattern 1: Aggregate vs Non-Aggregate
- Aggregate (Tour): Extends AuditableEntity, implements IAggregateRoot
  Use IContentToursEventUnitOfWork (dispatches domain events)
  
- Non-Aggregate (TourSchedule, TourPricingTier): Extends AuditableEntity, NO IAggregateRoot
  Use IContentToursUnitOfWork (no event dispatch)
  Use IContentToursOutboxWriter to enqueue integration events manually

### Pattern 2: Domain Event to Integration Event
1. Aggregate raises domain event in business method
2. UnitOfWork<TContext> dispatches domain event BEFORE SaveChanges
3. Domain event handler converts to integration event
4. Handler writes OutboxMessage to DbContext
5. Single atomic SaveChanges commits both aggregate change and outbox message

---

## TASK 2: TourSchedule & TourPricingTier (Non-Aggregates)

Domain Layer:
1. Create TourSchedule : AuditableEntity (NO IAggregateRoot)
2. Create TourPricingTier : AuditableEntity (NO IAggregateRoot)
3. Create ITourScheduleRepository : IRepository<TourSchedule, Guid>
4. Create ITourPricingTierRepository : IRepository<TourPricingTier, Guid>

Application Layer:
1. Create commands: CreateTourScheduleCommand, UpdateTourScheduleCommand, DeleteTourScheduleCommand
2. Create command handlers implementing ICommandHandler<T>
3. Create validators extending AbstractValidator<T>
4. Create queries: ListTourSchedulesQuery, ListTourPricingTiersQuery
5. Create query handlers implementing IQueryHandler<T, R>
6. Use IContentToursUnitOfWork (NOT event-dispatching)
7. Use IContentToursOutboxWriter to enqueue integration events
8. Create integration events in Contracts project
9. Register events in IntegrationEventTypeRegistry

Infrastructure Layer:
1. Create repository implementations extending EfRepository<T, Guid>
2. Create entity configurations (EF Core)
3. Create event handlers for incoming integration events (if needed)
4. Register repositories and services in DI

Presentation Layer:
1. Create endpoints using Minimal APIs
2. Use ISender to dispatch commands/queries
3. Use .ToApiResult() to convert results to HTTP responses
4. Add MustHavePermissionAttribute for protected endpoints

Contracts Layer:
1. Define integration events extending IntegrationEventBase
2. Define permission features and catalog

---

## TASK 3: Tour Queries & Commands (Aggregate)

Domain Layer:
1. Tour already exists as AuditableEntity, IAggregateRoot
2. Add domain events (e.g., TourFeaturedChangedDomainEvent)
3. Add business methods that raise events (e.g., SetFeatured())

Application Layer:
1. Create commands: ToggleTourFeaturedCommand, etc.
2. Create command handlers implementing ICommandHandler<T>
3. Use IContentToursEventUnitOfWork (event-dispatching)
4. Create queries: SearchToursQuery, ListFeaturedToursQuery, ListMyToursQuery, SuggestToursQuery
5. Implement ICacheableQuery on queries
6. Create query handlers
7. Use HybridCache.RemoveByTagAsync() for cache invalidation

Infrastructure Layer:
1. Create domain event handlers converting to integration events
2. Write OutboxMessages directly to DbContext
3. Do NOT call SaveChangesAsync in event handlers

Presentation Layer:
1. Create endpoints for all queries and commands
2. Use .ToApiResult() for responses
3. Add permission checks

Contracts Layer:
1. Define integration events
2. Register in IntegrationEventTypeRegistry

---

END OF DOCUMENT

