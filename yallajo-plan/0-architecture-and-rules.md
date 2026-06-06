# 0 — Architecture & UI/UX Rules (binding conventions)

> **This document binds every page in this plan to two authoritative specs.** Read it first.
> All pages in the master plan and the eight actor area-files **must** conform to it.

**Authoritative sources** (repo root — code is the only source of truth):
- `CONTROLLER_AUTHORING_GUIDE.md` — MVC Web layer structure, areas, four-tier pipeline, permissions, paging.
- `Agents\UI\UI-UX-Design.md` — UI/UX + performance + security rule families (cited by stable rule ID).
- `yallajo-endpoints.txt` — the 549-endpoint API route table that every button/endpoint is drawn from.

The Web app is **`src/Hosts/YallaJo.Web`** — a Razor MVC **BFF** (Backend-for-Frontend). It renders server-side HTML (Webestica Bootstrap 5) and calls **`YallaJo.Api`** through a typed pipeline. The browser **never** talks to the API directly and **never** sees the JWT.

---

## 1. The four-tier pipeline (mandatory on every page)

```
Controller  →  Facade  →  ApiClient  →  IApiClient  →  YallaJo.Api
```

- **Controller** — thin. `[Area]` + `[Authorize]` + `[RequirePermission]`. Injects **Facades only** (never `IApiClient`/`HttpClient`). Reads = `[HttpGet]`; writes = `[HttpPost]` + `[ValidateAntiForgeryToken]`. Every action is `async Task<IActionResult>` with a trailing `CancellationToken ct`. **PRG** (Post → Redirect → Get) after every successful write.
- **Facade** (`sealed`, suffix `Facade`) — maps `ApiResult<T>` → ViewModel, translates failures to friendly text, evicts output-cache tags after writes (`IOutputCacheStore.EvictByTagAsync`). No `HttpContext`/`TempData`.
- **ApiClient** (`sealed`, suffix `ApiClient`) — one line per endpoint binding verb + URL + DTO via `IApiClient` (`GetAsync<T>`, `PostAsync<T>`, `PutAsync`, `PatchAsync`, `DeleteAsync`, `PostFileAsync<T>`…).
- **IApiClient** — the single HTTP boundary. **API errors are values, not exceptions**: every call returns `ApiResult` / `ApiResult<T>` (`IsSuccess`, `Data`, `Error`, `StatusCode`, `ValidationErrors`, flags `IsUnauthorized`/`IsForbidden`/`IsNotFound`/`IsConflict`/`IsValidationError`/`IsTooManyRequests`).

`*ApiClient` and `*Facade` are **auto-registered Scoped** by reflection (`AddFeatureServices()`) — the suffix is a contract; no manual `AddScoped`.

**BaseController helpers** (inherit `BaseController`): `GuardSignOut(result)` (401 → `/auth/sign-in`, after **every** facade call), `SetSuccess`/`SetError`/`SetFlash` (TempData → `_Alerts` partial), `ApplyValidationErrors(result)` (API field errors → `ModelState`).

**Composite pages** (multi-facade): inject several sibling Facades (Pattern A — default), or one aggregate Facade (Pattern B), or a `ViewComponent` for a cross-page widget (Pattern C). Injecting `IApiClient`/`HttpClient` directly is the only thing forbidden.

---

## 2. The nine code areas (URLs lowercase)

The Web app is split into **nine `[Area]`s**. This plan keeps its **8 actor-based files** for readability; each page is tagged with the code **Area** it lives in so the mapping to `Areas/{Area}/…` is explicit.

| # | Code Area | URL prefix | Plan coverage (actor file) |
|---|-----------|-----------|----------------------------|
| 1 | **Public** | `/`, `/tours`, `/places`, `/businesses`, `/blog`, `/guides`, `/agencies` | §2 Storefront → `1-public-storefront.md` |
| 2 | **Auth** | `/auth/*` | §2.10 Auth funnel → `1-public-storefront.md` |
| 3 | **Accounts** | `/accounts/*` | §3 Customer → `2-customer-dashboard.md` |
| 4 | **Provider** | `/provider/*` | §4 Provider → `3-provider-dashboard.md` |
| 5 | **Guide** | `/guide/*` | §5 Tour Guide → `4-tour-guide-dashboard.md` |
| 6 | **Business** | `/business/*` | §4.5 business mgmt + §2.4 business detail → `3-provider-dashboard.md` / `1-public-storefront.md` |
| 7 | **Creator** | `/creator/*` | §7 Content Creator → `6-content-creator-dashboard.md` |
| 8 | **Content** | `/content/*` | §8.9 Content Ops + §8.10 SEO Console (lookups: categories/tags/languages/specializations/translations/attachments/seo) → `7-admin-dashboard.md` |
| 9 | **Admin** | `/admin/*` | §8 Admin (minus Content), §6 Agency, §9 RBAC (`security/*`) → `7-admin-dashboard.md`, `5-agency-dashboard.md`, `8-superadmin-rbac.md` |

**Actor → area notes**
- **Agency** (§6) and **SuperAdmin/RBAC** (§9) have **no dedicated code area**: Agency lives under **Admin** (or a Provider sub-feature for self-service), RBAC lives under **Admin** bound to the `security/*` API. They keep their own plan files for actor clarity.
- **Business** is its own area because business (place-business) management and the public business detail are a distinct feature slice, even though the *Provider* actor operates it.
- **Content** is its own area for platform lookups + SEO console; the *Admin* actor operates it.

**Folder shape per area:** `Areas/{Area}/{Controllers,Facades,ApiClients,Models/{Feature},Views/{Controller}}`. Extras: Admin adds `Validators/` (FluentValidation) + `Helpers/`; Accounts adds `Shared/_AccountSidebar`; Auth adds `Shared/_RecaptchaField`. Global shared: `_Layout`, `_Navbar`, `_Alerts`, `_ValidationScriptsPartial`.

---

## 3. Per-page annotation legend

Every page in the master plan and area files carries one **`**Stack:**`** line after its `**Buttons:**` line:

> **Stack:** **Area** `Public` · **Route** `/tours/{slug}` · **Cache** `PublicMedium` · **Perm** `[AllowAnonymous]` · **Rules** `R2, S1, CAL1-3, IMG1, A11Y1`

| Field | Meaning |
|-------|---------|
| **Area** | One of the nine code areas above. |
| **Route** | The lowercase MVC route the page is served at (BFF route, not the API route). |
| **Cache** | Output-cache policy (see §4). Public pages get a `Public*` tier; **all authenticated pages get `NoStore`** (UI-PERF-C2). |
| **Perm** | `[AllowAnonymous]`, `[Authorize]`, a policy (`Provider`/`Admin`), or a `WebPermission.{Feature}.{Action}` constant. Never an inline string. |
| **Rules** | The governing `Agents\UI\UI-UX-Design.md` rule IDs for that page (load strategy, forms, real-time, a11y, etc.). |

---

## 4. Caching (UI-PERF §5 / C1–C5)

| Policy | TTL | Used by |
|--------|-----|---------|
| `PublicShort` | 5 min | Home, catalog/listing pages |
| `PublicMedium` | 30 min | Detail pages, business profiles |
| `PublicLong` | 1 h | Blog, help, offers |
| `PublicDay` | 24 h | About, FAQ, static legal |
| **`NoStore`** | — | **Every authenticated page** (`/accounts/*`, `/provider/*`, `/guide/*`, `/business/*`, `/creator/*`, `/content/*`, `/admin/*`, `/auth/*`) → `Cache-Control: no-store` (C2) |

- **Tag invalidation (C3):** writes evict `tour:{id}` / `place:{id}` / `category:tree` / `homepage` / `lookups` via `IOutputCacheStore.EvictByTagAsync` in the Facade.
- **ETag + Last-Modified (C5):** detail pages hash `UpdatedAt` → 304.
- `IMemoryCache`: categories-tree 1 h, languages 24 h, commission-rates 10 min.

---

## 5. Rendering & load strategy (UI-PERF R / UI-UX S, the load tags)

The plan's load tags map to these rules:

| Tag in plan | Rule | Meaning |
|-------------|------|---------|
| `SSR` | **R2** | First paint is **server-rendered**. Primary content (hero, listing, detail) is never fetched by AJAX. |
| `AJAX` | **S1 / R2** | Post-SSR refinement only: filter changes, favorite toggle, comment, infinite scroll. Search = SSR first, then AJAX + `history.pushState`. |
| `AJAX-poll` (⟳) | **S2/RT** | Live data via **SignalR** (slot capacity, bell). No `setInterval` polling while a hub is connected (X12). |
| `AJAX-upload` (↑) | **SEC4** | Multipart upload (attachments, avatars) — magic-byte validated, EXIF stripped, stored outside webroot, served via authenticated proxy. |

**SignalR (S2–S5):** one hub per page max (X11). Connect on authenticated pages **and** public tour-detail/booking (group `tour:{tourId}` for live slots only). Groups: `user:{userId}`, `provider:{providerId}`, `admin`. Broadcast-to-all is forbidden. `withAutomaticReconnect([0,2000,10000,30000,60000])`.

---

## 6. Permissions (CONTROLLER_AUTHORING_GUIDE §9)

- Constants live in `Infrastructure/Authorization/WebPermission.cs` as nested static classes; string form `Permission.{Feature}.{Action}` (matches backend `AppPermission.NameFor()`). **Never build the string inline.**
- Apply three ways: `[RequirePermission(WebPermission.X.Y)]` on controller/action, `<permission require="@WebPermission.X.Y">…</permission>` in views (`@using YallaJo.Web.Infrastructure.Authorization`), or `ICurrentUser.HasPermission(...)`.
- Policies: **`Provider`** = role `Provider`; **`Admin`** = role `Admin` or `SuperAdmin`. Default class-level permission = the read; strictest permission on the write action.
- In this plan the **Perm** field names the *representative* permission for the page; a real controller may layer a stricter one per write action.

---

## 7. Paging (CONTROLLER_AUTHORING_GUIDE §13 / UI-PERF D1, R4)

Per-feature paged Response DTO: `Items, PageNumber, PageSize, TotalCount, HasPreviousPage, HasNextPage`. **No** generic `PagedResponse<T>`, **no** `TotalPages` on the wire — the pager is driven only by `HasPrevious`/`HasNext`. 1-based page, default **20**, max **50** (`Math.Clamp`). Filters preserved across page links via `asp-route-*`. Tables > 500 rows = server-side DataTables (R6); exports > 10K = `IAsyncEnumerable`.

---

## 8. Cross-cutting baselines (apply to all pages unless noted)

- **Forms/Security:** ViewModels + DataAnnotations; every POST has `@Html.AntiForgeryToken()` + `[ValidateAntiForgeryToken]`; **anti-forgery on AJAX writes too** (`RequestVerificationToken` header, SEC7); GET never mutates. CSP + hardening headers on every response (SEC1–2). Rate limits (SEC5): sign-in 5/min/IP, OTP 5/min/user, search 30/min/IP, review 10/hr/user → 429 + `Retry-After`.
- **i18n/RTL (§2, RTL1–4):** cultures `en` + `ar`, **default `ar`** (Jordan-first), `dir=rtl` for Arabic, logical CSS properties, `<bdi>` for Latin/number runs, strings via `IStringLocalizer`/`.resx` (CON1). Test both directions (merge-gate).
- **SEO (§3):** per-action `Title` ≤60 / `MetaDescription` ≤160 / `OgImage` / canonical; `robots.txt` disallows every authenticated area; slug routes for tours/places/businesses/blog/help/offers; JSON-LD `TouristAttraction` on tour detail.
- **A11Y (A11Y1–9):** WCAG 2.1 AA — contrast, full keyboard, ARIA labels on icon buttons, one `<h1>`, status = colour **+ icon + text**, `aria-live` polite on the bell badge + toasts, skip link.
- **Theme (T1–5):** respect `prefers-color-scheme`; manual toggle persists (localStorage + cookie); Bootstrap 5.3 `data-bs-theme`.
- **Motion (M1–4):** 200–300 ms, animate `transform`/`opacity` only, respect `prefers-reduced-motion`, no animation on checkout/payment.
- **Errors (ERR1–4):** branded 400/403/404/429/500/503 pages; failed page sections degrade to a "Couldn't load — [Retry]" card, never a whole-page 500; `X-Correlation-ID` per request.
- **Concurrency (ST1):** `RowVersion`/`UpdatedAt` optimistic concurrency → 409 shows "changed by someone else — [Reload]".
- **Geo (GEO1–3):** default JOD (3-decimal fils) + `ar` + `+962`; Gregorian default; UTC stored, `Asia/Amman` displayed.

---

## 9. Files in this folder

| File | Contents |
|------|----------|
| `0-architecture-and-rules.md` | **this file** — binding conventions |
| `yallajo-dashboard-ui-ux-plan.md` | master plan (all sections §0–§11, every page annotated) |
| `yallajo-template-page-wiring.md` | Webestica template → plan-section wiring |
| `1-public-storefront.md` … `8-superadmin-rbac.md` | the 8 per-actor area files |
| `README.md` | index of the eight actor areas |
