<#
.SYNOPSIS
    Scaffolds all CQRS boilerplate files for a new entity in a YallaJo module.

.DESCRIPTION
    Creates Domain entity, domain events, repository interface, CQRS commands/queries 
    (with handlers and validators), EF configuration, and endpoint stubs.
    All files follow YallaJo conventions from guide.md.

.PARAMETER Module
    The module name (e.g., ContentPlaces, ContentTours, Booking)

.PARAMETER Entity  
    The entity name in PascalCase (e.g., Place, Tour, Booking)

.PARAMETER Entities
    The plural entity name (e.g., Places, Tours, Bookings). Defaults to {Entity}s.

.PARAMETER Schema
    The DB schema name (e.g., content_places, booking). Defaults to module name lowered with underscores.

.PARAMETER Type
    Entity type: AggregateRoot (default), BaseEntity, or Junction

.EXAMPLE
    .\scaffold.ps1 -Module ContentPlaces -Entity Place -Schema content_places
    .\scaffold.ps1 -Module Booking -Entity Booking -Entities Bookings -Type AggregateRoot
    .\scaffold.ps1 -Module ContentCore -Entity EntityMedia -Type Junction
#>

param(
    [Parameter(Mandatory)] [string] $Module,
    [Parameter(Mandatory)] [string] $Entity,
    [string] $Entities = "${Entity}s",
    [string] $Schema = "",
    [ValidateSet("AggregateRoot", "BaseEntity", "Junction")]
    [string] $Type = "AggregateRoot"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot  # YallaJo solution root

# Default schema: ContentPlaces → content_places
if (-not $Schema) {
    $Schema = ($Module -creplace '([A-Z])', '_$1').TrimStart('_').ToLower()
}

$entity = $Entity.Substring(0,1).ToLower() + $Entity.Substring(1)  # camelCase
$created = @()

function New-File($path, $content) {
    $dir = Split-Path $path -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    if (Test-Path $path) { Write-Host "  SKIP (exists): $path" -ForegroundColor Yellow; return }
    Set-Content -Path $path -Value $content -Encoding utf8
    Write-Host "  CREATED: $path" -ForegroundColor Green
    $script:created += $path
}

Write-Host "`n=== Scaffolding $Entity in $Module (Type: $Type, Schema: $Schema) ===" -ForegroundColor Cyan

# ── DOMAIN LAYER ──
Write-Host "`n[Domain Layer]" -ForegroundColor Magenta

if ($Type -eq "AggregateRoot") {
    # Entity
    New-File "$root\$Module.Domain\Entities\$Entity.cs" @"
using $Module.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace $Module.Domain.Entities;

public sealed class $Entity : AuditableEntity, IAggregateRoot
{
    private ${Entity}() { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    // TODO: Add entity-specific properties

    public static $Entity Create(string name, string slug, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("$Entity name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("$Entity slug is required.", nameof(slug));

        var entity = new $Entity { Name = name.Trim(), Slug = slug.Trim(), Description = description?.Trim(), IsActive = true };
        entity.AddDomainEvent(new ${Entity}CreatedDomainEvent(entity.Id, entity.Name));
        return entity;
    }

    public void Update(string name, string slug, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("$Entity name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("$Entity slug is required.", nameof(slug));
        Name = name.Trim(); Slug = slug.Trim(); Description = description?.Trim(); MarkUpdated();
        AddDomainEvent(new ${Entity}UpdatedDomainEvent(Id, Name));
    }

    public void Activate() { IsActive = true; MarkUpdated(); }
    public void Deactivate() { IsActive = false; MarkUpdated(); }
}
"@

    # Domain Events
    New-File "$root\$Module.Domain\Events\${Entity}CreatedDomainEvent.cs" @"
using YallaJo.SharedKernel.Domain.Event;
namespace $Module.Domain.Events;
public sealed record ${Entity}CreatedDomainEvent(Guid ${Entity}Id, string Name) : DomainEventBase;
"@

    New-File "$root\$Module.Domain\Events\${Entity}UpdatedDomainEvent.cs" @"
using YallaJo.SharedKernel.Domain.Event;
namespace $Module.Domain.Events;
public sealed record ${Entity}UpdatedDomainEvent(Guid ${Entity}Id, string Name) : DomainEventBase;
"@

    # Repository Interface
    New-File "$root\$Module.Domain\Repositories\I${Entity}Repository.cs" @"
using $Module.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;
namespace $Module.Domain.Repositories;
public interface I${Entity}Repository : IRepository<$Entity, Guid> { }
"@
}

# ── APPLICATION LAYER ──
Write-Host "`n[Application Layer]" -ForegroundColor Magenta

# CreateCommand
New-File "$root\$Module.Application\Commands\$Entity\Create$Entity\Create${Entity}Command.cs" @"
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace $Module.Application.Commands.$Entity.Create$Entity;
public sealed record Create${Entity}Result(Guid Id, string Name, string Slug);
public sealed record Create${Entity}Command(string Name, string Slug, string? Description = null) : ICommand<Create${Entity}Result>;
"@

New-File "$root\$Module.Application\Commands\$Entity\Create$Entity\Create${Entity}CommandHandler.cs" @"
using $Module.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
namespace $Module.Application.Commands.$Entity.Create$Entity;
public sealed class Create${Entity}CommandHandler(I${Entity}Repository ${entity}Repository, I${Module}UnitOfWork unitOfWork)
    : ICommandHandler<Create${Entity}Command, Create${Entity}Result>
{
    public async Task<Result<Create${Entity}Result>> Handle(Create${Entity}Command request, CancellationToken ct)
    {
        var $entity = Domain.Entities.$Entity.Create(request.Name, request.Slug, request.Description);
        await ${entity}Repository.AddAsync($entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<Create${Entity}Result>.Created(new Create${Entity}Result($entity.Id, $entity.Name, $entity.Slug));
    }
}
"@

New-File "$root\$Module.Application\Commands\$Entity\Create$Entity\Create${Entity}CommandValidator.cs" @"
using FluentValidation;
namespace $Module.Application.Commands.$Entity.Create$Entity;
public sealed class Create${Entity}CommandValidator : AbstractValidator<Create${Entity}Command>
{
    public Create${Entity}CommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200).Matches(@"^[a-z0-9\-]+$").WithMessage("Slug must contain only lowercase letters, digits, and hyphens.");
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}
"@

# UpdateCommand
New-File "$root\$Module.Application\Commands\$Entity\Update$Entity\Update${Entity}Command.cs" @"
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace $Module.Application.Commands.$Entity.Update$Entity;
public sealed record Update${Entity}Command(Guid Id, string Name, string Slug, string? Description = null) : ICommand<Guid>;
"@

New-File "$root\$Module.Application\Commands\$Entity\Update$Entity\Update${Entity}CommandHandler.cs" @"
using $Module.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
namespace $Module.Application.Commands.$Entity.Update$Entity;
public sealed class Update${Entity}CommandHandler(I${Entity}Repository ${entity}Repository, I${Module}UnitOfWork unitOfWork)
    : ICommandHandler<Update${Entity}Command, Guid>
{
    public async Task<Result<Guid>> Handle(Update${Entity}Command request, CancellationToken ct)
    {
        var $entity = await ${entity}Repository.GetByIdAsync(request.Id, ct);
        if ($entity is null) return Result<Guid>.NotFound("$Entity '" + request.Id + "' not found.");
        $entity.Update(request.Name, request.Slug, request.Description);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<Guid>.Success($entity.Id);
    }
}
"@

New-File "$root\$Module.Application\Commands\$Entity\Update$Entity\Update${Entity}CommandValidator.cs" @"
using FluentValidation;
namespace $Module.Application.Commands.$Entity.Update$Entity;
public sealed class Update${Entity}CommandValidator : AbstractValidator<Update${Entity}Command>
{
    public Update${Entity}CommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200).Matches(@"^[a-z0-9\-]+$").WithMessage("Slug must contain only lowercase letters, digits, and hyphens.");
    }
}
"@

# DeleteCommand
New-File "$root\$Module.Application\Commands\$Entity\Delete$Entity\Delete${Entity}Command.cs" @"
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace $Module.Application.Commands.$Entity.Delete$Entity;
public sealed record Delete${Entity}Command(Guid Id) : ICommand<Guid>;
"@

New-File "$root\$Module.Application\Commands\$Entity\Delete$Entity\Delete${Entity}CommandHandler.cs" @"
using $Module.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
namespace $Module.Application.Commands.$Entity.Delete$Entity;
public sealed class Delete${Entity}CommandHandler(I${Entity}Repository ${entity}Repository, I${Module}UnitOfWork unitOfWork)
    : ICommandHandler<Delete${Entity}Command, Guid>
{
    public async Task<Result<Guid>> Handle(Delete${Entity}Command request, CancellationToken ct)
    {
        var $entity = await ${entity}Repository.GetByIdAsync(request.Id, ct);
        if ($entity is null) return Result<Guid>.NotFound("$Entity '" + request.Id + "' not found.");
        $entity.SoftDelete();
        await unitOfWork.SaveChangesAsync(ct);
        return Result<Guid>.Success($entity.Id);
    }
}
"@

New-File "$root\$Module.Application\Commands\$Entity\Delete$Entity\Delete${Entity}CommandValidator.cs" @"
using FluentValidation;
namespace $Module.Application.Commands.$Entity.Delete$Entity;
public sealed class Delete${Entity}CommandValidator : AbstractValidator<Delete${Entity}Command>
{
    public Delete${Entity}CommandValidator() { RuleFor(x => x.Id).NotEqual(Guid.Empty); }
}
"@

# ListQuery
New-File "$root\$Module.Application\Queries\$Entity\List$Entities\List${Entities}Query.cs" @"
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace $Module.Application.Queries.$Entity.List$Entities;
public sealed record ${Entity}SummaryDto(Guid Id, string Name, string Slug, bool IsActive, DateTime CreatedAt);
public sealed record List${Entities}Query(bool ActiveOnly = false, int Page = 1, int PageSize = 20) : IQuery<IReadOnlyList<${Entity}SummaryDto>>;
"@

New-File "$root\$Module.Application\Queries\$Entity\List$Entities\List${Entities}QueryHandler.cs" @"
using $Module.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
namespace $Module.Application.Queries.$Entity.List$Entities;
public sealed class List${Entities}QueryHandler(I${Entity}Repository ${entity}Repository)
    : IQueryHandler<List${Entities}Query, IReadOnlyList<${Entity}SummaryDto>>
{
    public async Task<Result<IReadOnlyList<${Entity}SummaryDto>>> Handle(List${Entities}Query request, CancellationToken ct)
    {
        var entities = await ${entity}Repository.GetAllAsync(filter: request.ActiveOnly ? e => e.IsActive : null, orderBy: q => q.OrderBy(e => e.Name), ct: ct);
        var dtos = entities.Select(e => new ${Entity}SummaryDto(e.Id, e.Name, e.Slug, e.IsActive, e.CreatedAt)).ToList() as IReadOnlyList<${Entity}SummaryDto>;
        return Result<IReadOnlyList<${Entity}SummaryDto>>.Success(dtos);
    }
}
"@

# GetByIdQuery
New-File "$root\$Module.Application\Queries\$Entity\Get${Entity}ById\Get${Entity}ByIdQuery.cs" @"
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace $Module.Application.Queries.$Entity.Get${Entity}ById;
public sealed record ${Entity}DetailDto(Guid Id, string Name, string Slug, string? Description, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record Get${Entity}ByIdQuery(Guid Id) : IQuery<${Entity}DetailDto>;
"@

New-File "$root\$Module.Application\Queries\$Entity\Get${Entity}ById\Get${Entity}ByIdQueryHandler.cs" @"
using $Module.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
namespace $Module.Application.Queries.$Entity.Get${Entity}ById;
public sealed class Get${Entity}ByIdQueryHandler(I${Entity}Repository ${entity}Repository)
    : IQueryHandler<Get${Entity}ByIdQuery, ${Entity}DetailDto>
{
    public async Task<Result<${Entity}DetailDto>> Handle(Get${Entity}ByIdQuery request, CancellationToken ct)
    {
        var $entity = await ${entity}Repository.GetByIdAsync(request.Id, ct);
        if ($entity is null) return Result<${Entity}DetailDto>.NotFound("$Entity '" + request.Id + "' not found.");
        return Result<${Entity}DetailDto>.Success(new ${Entity}DetailDto($entity.Id, $entity.Name, $entity.Slug, $entity.Description, $entity.IsActive, $entity.CreatedAt, $entity.UpdatedAt));
    }
}
"@

# ── INFRASTRUCTURE LAYER ──
Write-Host "`n[Infrastructure Layer]" -ForegroundColor Magenta

New-File "$root\$Module.Infrastructure\Persistence\Configurations\${Entity}Configuration.cs" @"
using $Module.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace $Module.Infrastructure.Persistence.Configurations;
public class ${Entity}Configuration : IEntityTypeConfiguration<$Entity>
{
    public void Configure(EntityTypeBuilder<$Entity> builder)
    {
        builder.ToTable("$Entities", "$Schema");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().IsUnicode(false).HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired(false).HasMaxLength(2000);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
"@

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "  Files created: $($created.Count)" -ForegroundColor Green
Write-Host "`n  REMAINING MANUAL STEPS:" -ForegroundColor Yellow
Write-Host "  1. Add DbSet<$Entity> to ${Module}DbContext"
Write-Host "  2. Add services.AddScoped<I${Entity}Repository, ${Entity}Repository>() to DependencyInjection.cs"
Write-Host "  3. Create ${Entity}Repository.cs in $Module.Infrastructure\Repositories\"
Write-Host "  4. Add endpoint wiring in ${Module}Endpoints.cs"
Write-Host "  5. Run: dotnet ef migrations add Add$Entity --project $Module.Infrastructure --startup-project YallaJo.Api"
Write-Host "  6. Run: dotnet build"
Write-Host ""
Write-Host "  ⚠️  MANDATORY REVIEW (do NOT skip):" -ForegroundColor Red
Write-Host "  Every generated file contains TODO markers. You MUST:" -ForegroundColor Red
Write-Host "  - Add ALL entity-specific properties and business methods"
Write-Host "  - Add ALL validation rules from Business Rules PDF"
Write-Host "  - Add ALL EF relationships and indexes"
Write-Host "  - Add ALL Swagger annotations to endpoints"
Write-Host "  - Review EVERY file against Agents/agent-context.md rules"
Write-Host "  - See 'Scaffold & Template Usage Rules' in agent-context.md for full checklist"
Write-Host ""
Write-Host "  Generated files are BOILERPLATE. They are NOT production-ready until reviewed." -ForegroundColor Red
Write-Host ""
