using YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.Mappers;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles;

public sealed class RolesFacade
{
    private readonly RolesApiClient _api;
    public RolesFacade(RolesApiClient api) => _api = api;

    // ── List ──────────────────────────────────────────────────────────────────

    public async Task<RolesFacadeResult<RoleListVm>> GetRolesAsync(CancellationToken ct = default)
    {
        var result = await _api.GetRolesAsync(ct);
        if (result.IsSuccess)
        {
            var vm = new RoleListVm
            {
                Roles = (result.Data ?? []).Select(RolesMapper.ToRowVm).ToList(),
            };
            return RolesFacadeResult<RoleListVm>.Ok(vm);
        }
        if (result.IsUnauthorized) return RolesFacadeResult<RoleListVm>.ForceSignOut();
        return RolesFacadeResult<RoleListVm>.Fail(result.Error ?? "Could not load roles.");
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<RolesFacadeResult> CreateAsync(CreateRoleVm vm, CancellationToken ct = default)
    {
        var result = await _api.CreateRoleAsync(RolesMapper.ToCreateRequest(vm), ct);
        if (result.IsSuccess)   return RolesFacadeResult.Ok();
        if (result.IsUnauthorized) return RolesFacadeResult.ForceSignOut();
        if (result.IsConflict)  return RolesFacadeResult.Fail("A role with this name already exists.");
        if (result.IsValidationError) return RolesFacadeResult.Invalid(result.ValidationErrors!);
        return RolesFacadeResult.Fail(result.Error ?? "Could not create role.");
    }

    // ── Update description ────────────────────────────────────────────────────

    public async Task<RolesFacadeResult> UpdateAsync(
        Guid roleId, UpdateRoleVm vm, CancellationToken ct = default)
    {
        var result = await _api.UpdateRoleAsync(roleId, RolesMapper.ToUpdateRequest(vm), ct);
        if (result.IsSuccess)   return RolesFacadeResult.Ok();
        if (result.IsUnauthorized) return RolesFacadeResult.ForceSignOut();
        if (result.IsNotFound)  return RolesFacadeResult.Fail("Role not found.");
        if (result.IsValidationError) return RolesFacadeResult.Invalid(result.ValidationErrors!);
        return RolesFacadeResult.Fail(result.Error ?? "Could not update role.");
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    public async Task<RolesFacadeResult> DeactivateAsync(Guid roleId, CancellationToken ct = default)
    {
        var result = await _api.DeactivateRoleAsync(roleId, ct);
        if (result.IsSuccess)   return RolesFacadeResult.Ok();
        if (result.IsUnauthorized) return RolesFacadeResult.ForceSignOut();
        if (result.IsNotFound)  return RolesFacadeResult.Fail("Role not found.");
        if (result.IsForbidden) return RolesFacadeResult.Fail("This role cannot be deactivated.");
        return RolesFacadeResult.Fail(result.Error ?? "Could not deactivate role.");
    }

    // ── Claims ────────────────────────────────────────────────────────────────

    public async Task<RolesFacadeResult> AddClaimAsync(
        Guid roleId, AddRoleClaimVm vm, CancellationToken ct = default)
    {
        var result = await _api.AddClaimAsync(roleId, RolesMapper.ToAddClaimRequest(vm), ct);
        if (result.IsSuccess)   return RolesFacadeResult.Ok();
        if (result.IsUnauthorized) return RolesFacadeResult.ForceSignOut();
        if (result.IsConflict)  return RolesFacadeResult.Fail("Claim already exists on this role.");
        if (result.IsValidationError) return RolesFacadeResult.Invalid(result.ValidationErrors!);
        return RolesFacadeResult.Fail(result.Error ?? "Could not add claim.");
    }

    public async Task<RolesFacadeResult> RemoveClaimAsync(
        Guid roleId, Guid claimId, CancellationToken ct = default)
    {
        var result = await _api.RemoveClaimAsync(roleId, claimId, ct);
        if (result.IsSuccess || result.IsNotFound) return RolesFacadeResult.Ok();
        if (result.IsUnauthorized) return RolesFacadeResult.ForceSignOut();
        return RolesFacadeResult.Fail(result.Error ?? "Could not remove claim.");
    }
}

// ── Facade result types ────────────────────────────────────────────────────────

public sealed class RolesFacadeResult
{
    public bool    IsSuccess      { get; private init; }
    public string? Error          { get; private init; }
    public bool    RequireSignOut { get; private init; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; private init; }

    public static RolesFacadeResult Ok()           => new() { IsSuccess = true };
    public static RolesFacadeResult Fail(string e) => new() { IsSuccess = false, Error = e };
    public static RolesFacadeResult ForceSignOut() => new() { IsSuccess = false, RequireSignOut = true };
    public static RolesFacadeResult Invalid(IReadOnlyDictionary<string, string[]> errs)
        => new() { IsSuccess = false, ValidationErrors = errs };
}

public sealed class RolesFacadeResult<T>
{
    public bool    IsSuccess      { get; private init; }
    public T?      Data           { get; private init; }
    public string? Error          { get; private init; }
    public bool    RequireSignOut { get; private init; }

    public static RolesFacadeResult<T> Ok(T data)    => new() { IsSuccess = true, Data = data };
    public static RolesFacadeResult<T> Fail(string e) => new() { IsSuccess = false, Error = e };
    public static RolesFacadeResult<T> ForceSignOut() => new() { IsSuccess = false, RequireSignOut = true };
}
