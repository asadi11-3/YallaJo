# Authorization-Cleanup — Pre-Work Kickoff Briefing

> Sprint window: 2027-02-28 → 03-11. Owner: Tech Lead.

## What's already wired

- `AppAction` enum extended to 33 verbs in `YallaJo.SharedKernel.Application/Authorization/AppAction.cs` — every verb referenced in the violation catalog now resolves.
- **Violation catalog** at `Agents/endpoint-violations-2027-02-28.csv`: 102 `RequireAuthorization` usages classified as STRING_POLICY (5), AUTH_AND_PERM_MIX (~90), AUTH_ONLY (~7) across 24 endpoint files.
- **Permission coverage gaps** at `Agents/permission-coverage-gaps-2027-02-28.md`: maps every violation to its `{Module}Features.X + AppAction.Y` target. All required verbs present in AppAction.cs.
- **RED sanity tests** at `tests/Authorization.IntegrationTests/AuthorizationSanityTests.cs` tagged `[Trait("Category", "authorization-debt")]` so CI default run excludes them:
  1. `Every_endpoint_must_have_MustHavePermission_or_AllowAnonymous`
  2. `No_endpoint_may_use_string_policy_authorization`
  3. `Every_MustHavePermission_must_reference_a_registered_permission`
- `MustHavePermissionAttribute` lives in `YallaJo.SharedKernel.Presentation.Authorization` (inherits `AuthorizeAttribute`, has `Feature/Action` properties + `(string feature, string action)` constructor).

## Day-0 sprint tasks

1. Walk the violation CSV row-by-row:
   - **STRING_POLICY** (5 rows in `ContentPlaces/BusinessEndpoints.cs` lines 151, 187, 205, 223, 239) — replace each `.RequireAuthorization("Admin")` with the mapped `MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.{UpdateAny|Approve|Reject|Suspend|Reinstate})`.
   - **AUTH_AND_PERM_MIX** (~90 rows) — drop the redundant `.RequireAuthorization()` call; `MustHavePermissionAttribute` already inherits `AuthorizeAttribute`.
   - **AUTH_ONLY** (~7 rows in Auth/Device, Auth/ExternalProvider, Accounts/Profile, Security/Account, Security/AuditLog, Security/Role, YallaJo.Api/OpsEndpoints) — add explicit `MustHavePermissionAttribute` (mapped in PW-2 doc) **or** `[AllowAnonymous]` if the endpoint is genuinely public.
2. After each violation cluster is fixed, re-run `dotnet test --filter "Category=authorization-debt"` until the 3 sanity tests turn GREEN.
3. Remove the `[Trait("Category", "authorization-debt")]` exclusion from CI defaults — the tests become first-class regression guards.
4. Audit any new `IPermissionCatalog` entries needed (AccountsFeatures.Profile, SecurityFeatures.{User,Role,AuditLog}, ContentPlacesFeatures.Business) per PW-2 gap analysis.
5. Wire `SeoRedirectMiddleware` (separate scope per INDEX §1) — pre-work scope did not include this since it has no permission surface.

## Watchpoints

- The 3 RED tests scan via `EndpointDataSource.Endpoints` — they will pick up endpoints from every module, including any added during Wave 5/6 sprints. Run them at the END of the cleanup window, not mid-sprint, to avoid noise.
- `IPermissionCatalog` registrations are Singleton — boot log should print 11 catalogs (6 pre-existing + Booking, Finance, Social, Messaging, Analytics). Verify before declaring cleanup done.
- Estimated effort from PW-2 doc: **4-6h of mechanical cleanup** once the catalog gaps are filled.
