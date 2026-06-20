# Development Seed Data (DEV-SEED-B1)

> ⚠️ **DEVELOPMENT / QA ONLY.** Every seeder described here is guarded internally by
> `IHostEnvironment.IsDevelopment()` and will **never** run in Production. This dataset
> exists solely to support manual QA of the current media/files work. Do **not** rely on
> these records in any non-Development environment.

## What this is

`DEV-SEED-B1` is the first, foundation slice of a namespaced, deterministic development
dataset. It creates a small, self-contained set of users, profiles, a provider application,
a place, two tours, a tour guide, reviews, images, and basic bookings — enough to exercise:

- tour cards **with a real primary image** and **placeholder fallback** (no image),
- public reviews **with** and **without** images,
- a **hidden** review whose images must **not** leak to public surfaces,
- provider documents stored through the **secured** (404-shielded) file path,
- basic booking states (AwaitingPayment / Confirmed / Cancelled).

All records use the reserved **`5eed0000-…`** GUID block and **`seed.*@yallajo.dev`** email
namespace so they are obviously dev-only and never collide with the existing
production-style (`@yallajo.local`) or Playwright (`@yallajo.test`) seed data, which remain
**untouched**.

## Seeded accounts

| Email | Role | Status | Password |
|---|---|---|---|
| `seed.customer@yallajo.dev`  | User      | Active    | `DevSeed!23` |
| `seed.provider@yallajo.dev`  | Provider  | Active    | `DevSeed!23` |
| `seed.guide@yallajo.dev`     | TourGuide | Active    | `DevSeed!23` |
| `seed.admin@yallajo.dev`     | Admin     | Active    | `DevSeed!23` |
| `seed.suspended@yallajo.dev` | User      | Suspended | `DevSeed!23` |

**Password for all dev seed accounts: `DevSeed!23`**

> The `Suspended` account is created Active, its primary email is verified, then it is
> suspended — so you can exercise the suspended-login path. The `pending` lifecycle is
> supported by the seeder but not currently used by B1.

## Scenarios covered by B1

- **Identity & profiles**
  - Active customer, provider, tour guide, and admin accounts.
  - A suspended account.
  - A `Profile` for every seeded user (display name + avatar URL).
- **Provider onboarding**
  - One **approved** `ProviderApplication` (Independent Guide) for `seed.provider@yallajo.dev`,
    with all required `ProviderDocument`s materialized as real placeholder-PDF `FileAsset`s
    via the production write path (`IFileStorageService` → `IFileAssetRegistrar` →
    `IProviderDocumentFileWriter`). Files land under the **404-shielded**
    `wwwroot/uploads/provider-application-documents` folder and are only retrievable via the
    authorized download endpoint.
- **Content**
  - One approved `Place` (`dev-seed-heritage-site`).
  - Two approved `Tour`s sharing that place:
    - `dev-seed-tour-with-image` — receives a primary image.
    - `dev-seed-tour-without-image` — intentionally **no** image (placeholder fallback test).
  - Each tour gets a schedule, an adult pricing tier, an English translation, a waypoint,
    and a primary tour-guide link.
  - One `TourGuide` aggregate (`dev-seed-guide`) with a managed `AvatarUrl`.
- **Images** (ContentCore `Attachment` + `EntityImage`, DB-only static URLs)
  - Tour primary image(s) for `dev-seed-tour-with-image`.
  - Review images for the public "review with images".
  - Review images for the **hidden** review (to prove they do **not** appear publicly).
  - All image URLs point at existing static gallery assets
    (`/assets/images/gallery/*.jpg`). **No binary files are written into the repo and no
    random upload files are generated.**
- **Reviews** (Social)
  - One **published** review **with** images (targets the tour with image).
  - One **published** review **without** images (targets the tour without image).
  - One **hidden** (`AutoHidden`) review **with** images (targets the place) — used to verify
    that images attached to a non-published review never surface on public endpoints.
- **Bookings** (basic states)
  - `AwaitingPayment`
  - `Confirmed`
  - `Cancelled` (with a refund amount recorded)

## Intentionally deferred to B2 / B3

The following are **not** part of B1 and will be delivered in later patches:

- **Finance** dev seeder (payments, payouts, invoices, commission rules, refunds).
- **Messaging** dev seeder (notifications, support tickets, device tokens).
- **ContentBlogs** dev seeder (creator profiles, articles).
- **Full Agency matrix** (affiliations, applications, invitations).
- **Full disputes / refunds / payouts / support** matrices.
- The complete **27-row** scenario matrix (rejected/suspended provider cases, no-show,
  refunded, paid/unpaid finance bridges, packages, itineraries beyond the basics, etc.).

## How to run

The dev seeders are plugged into the existing seeding pipeline as `IModuleDbInitializer`
implementations (Order `160`–`166`, after all baseline seeders). They run automatically
when the API host starts **in the Development environment**:

1. Ensure `ASPNETCORE_ENVIRONMENT=Development`.
2. Start the API host (`src/Hosts/YallaJo.Api`). On startup the seeding pipeline applies
   migrations and runs all initializers, including the `Dev*Seeder`s.
3. Watch the logs for `DEV-SEED-B1: …` messages confirming what was seeded.

No configuration flag is required in Development. (The pipeline gate is
`IsDevelopment() || Seeding:Enabled`; each Dev seeder additionally self-guards on
`IsDevelopment()`, so even if `Seeding:Enabled=true` is set in another environment the dev
records are **not** created.)

## How to reset

There is **no destructive reset code** and none is added by this patch. To get a clean
dataset:

1. **Stop** the API host.
2. **Drop** the database manually (e.g. drop the SQL database, or delete the local
   dev database).
3. **Restart** the API host — migrations re-apply and all seeders (baseline + dev) re-run
   against the fresh database.

## Idempotency

Every dev seeder is **idempotent** and safe to run on every startup:

- Per-row existence checks (no coarse `if (Any()) return;`).
- `IgnoreQueryFilters()` is used where soft-delete filters would otherwise hide an existing
  seeded row and cause a duplicate insert.
- Deterministic `5eed0000-…` GUIDs mean re-runs match existing rows instead of creating new
  ones.

Restarting the host a second time against the same database makes **no** changes and creates
**no** duplicates.

## Safety summary

- Development-only (internal `IHostEnvironment.IsDevelopment()` guard in every seeder).
- Additive only — existing seeders and data are untouched.
- No schema changes, no migrations, no entity/configuration edits, no `Program.cs` edits.
- No large binaries committed; images are DB-only static URLs; provider documents reuse the
  existing placeholder-PDF / `FileAsset` pattern through the secured path.
