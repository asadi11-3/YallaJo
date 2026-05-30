namespace YallaJo.SharedKernel.Application.Authorization;

/// <summary>
/// Cross-module ownership probe result for a single target entity.
/// Used by ownership-service contracts (e.g. <c>IPlaceOwnershipService</c>,
/// <c>ITourOwnershipService</c>) to let foreign modules answer authorization
/// questions about an entity they own without exposing their domain entities.
/// </summary>
/// <param name="IsSupported">
/// <c>true</c> when the underlying service understands the requested entity kind.
/// A resolver that fans out by <c>EntityType</c> may return <c>false</c> for
/// kinds it does not handle.
/// </param>
/// <param name="Exists">
/// <c>true</c> when a row matching the supplied id was found (including soft-deleted rows).
/// </param>
/// <param name="IsDeleted">
/// <c>true</c> when the entity exists but has been soft-deleted (or its module-specific
/// equivalent, e.g. <c>IsActive=false</c> for entities that do not carry an
/// <c>IsDeleted</c> column).
/// </param>
/// <param name="OwnerUserId">
/// The user id that owns / authored / created the entity, when known. May be <c>null</c>
/// for unsupported, missing, or owner-less entities.
/// </param>
public sealed record EntityOwnershipResolution(
    bool IsSupported,
    bool Exists,
    bool IsDeleted,
    Guid? OwnerUserId);
