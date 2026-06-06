# YallaJo Frontend — Rules

ASP.NET Core MVC + Razor + Webestica Bootstrap 5 template.
Single source of truth for performance, UX, accessibility, and security rules. Code architecture lives in `CONTROLLER_AUTHORING_GUIDE.md` (see **Related Architecture** below).

> All rules are **mandatory** unless explicitly marked optional or guidance.
> Rule IDs are stable references — cite them in code review and CI checks
> (e.g., `UI-PERF-A1`, `UI-UX-NF6`). Section numbers may change; IDs may not.

---

## Related Architecture

> **Source of truth for *how* the frontend is built:** [`CONTROLLER_AUTHORING_GUIDE.md`](../../CONTROLLER_AUTHORING_GUIDE.md).
> This document owns performance, UX, accessibility, and security rules; the authoring guide owns the code architecture. Where they meet, the guide wins on structure and this doc wins on budgets/behavior.

- **Four-tier pipeline (mandatory):** `Controller → Facade → ApiClient → IApiClient`. Controllers never inject `IApiClient` / `HttpClient`; they call Facades.
- **API errors are values, not exceptions:** every call returns `ApiResult` / `ApiResult<T>` (`IsSuccess`, `Data`, `Error`, `ValidationErrors`, `IsUnauthorized` / `IsNotFound` / `IsConflict` / …). Do not throw/catch for API failures.
- **`BaseController` helpers:** `GuardSignOut(result)` (bounces 401 → `/auth/sign-in`; call after every Facade call), `SetSuccess` / `SetError` (flash → `_Alerts`), `ApplyValidationErrors` (API field errors → `ModelState`), and PRG after every successful write.
- **Suffix-based DI:** any `*ApiClient` / `*Facade` is auto-registered Scoped — no manual `AddScoped`.
- **Shared partials:** `_Alerts` (flash), `_ValidationScriptsPartial` (client validation), `_Navbar`. Use `<partial>` / `PartialAsync`, never `Html.Partial` / `RenderPartial`.
- **Permissions:** `WebPermission.{Feature}.{Action}` constants, applied via `[RequirePermission(...)]`, `<permission require="…">`, or `ICurrentUser.HasPermission(...)`.
- **Real areas (9):** `Accounts`, `Admin`, `Auth`, `Business`, `Content`, `Creator`, `Guide`, `Provider`, `Public`. Generated URLs are lowercase (`/accounts/bookings`, `/auth/sign-in`, `/provider/dashboard`).

---

## 0. Design Skill Mapping

Before starting **any task** that touches a Razor view, layout, partial, or design token, load the matching skill once for the task duration.

| Task type | Skill | Why |
|---|---|---|
| Standard view design + iteration | `ui-ux-pro-max` | Default workflow guidance |
| Polish, critique, refactor existing view | `impeccable` | Senior review lens |
| Performance budgets, component architecture | `design-taste-frontend` | Metric-driven engineering |
| Hi-fi prototypes, variant exploration, motion demos | `huashu-design` | Prototype-first explorations |

**Anti-patterns to avoid:**
- AI-slop gradient soup; generic SaaS aesthetic
- Bland default Bootstrap with no brand identity
- Accessibility or RTL regressions
- Reinventing Bootstrap patterns that already exist

---

## 1. Authentication

- **Browser ↔ Web (MVC):** Cookie auth.
  - `HttpOnly = true`, `Secure = true`
  - **`SameSite = Lax`** — required for OAuth redirect callbacks (Apple/Google/Facebook). `Strict` will break third-party sign-in.
  - 30-day sliding expiration
  - Cookie name: `YallaJo.Auth`
- **Web (MVC) ↔ Api:** Bearer JWT, server-to-server only. JWT lives inside encrypted cookie claims; **never** exposed to client-side JS.
- Inject `AuthTokenHandler` (`DelegatingHandler`) into the API `HttpClient`:
  - Attaches `Authorization: Bearer {token}`
  - On `401`, refreshes once with refresh token and retries once
- Authorization policies:
  - `"Provider"` requires role `Provider`
  - `"Admin"` requires role `Admin` or `SuperAdmin`
  - `RequirePermission` attribute (real type `RequirePermissionAttribute`, ctor takes a permission string) mirrors the backend `MustHavePermissionAttribute` claim. Permission constants come from `WebPermission.{Feature}.{Action}` — never build permission strings inline.
- Identity paths (real, from `Program.cs`): `LoginPath = "/auth/sign-in"`, `LogoutPath = "/auth/sign-out"`, `AccessDeniedPath = "/auth/sign-in"`. (The `/api/v1/auth/login` endpoint is the server-to-server API call, not the MVC page.)

---

## 2. Internationalization & RTL

- Supported cultures: `en`, `ar`.
- **Default culture: `ar`** — aligns with Jordan-first launch market (Program.cs `DefaultRequestCulture = new("ar")`).
- Culture provider chain: `QueryStringRequestCultureProvider` → `CookieRequestCultureProvider` → `AcceptLanguageHeaderRequestCultureProvider`.
- `_Layout.cshtml`: `<html lang="@culture" dir="@dir">`. Set `dir="rtl"` when `ar`.
- Load `style.rtl.css` when culture is RTL (swap stylesheet, not append).
- Render `hreflang` alternate links for AR↔EN on every page.
- All user-facing strings via `IStringLocalizer` / `IViewLocalizer`. Resources in `Resources/Views/...` and `Resources/SharedResource.{culture}.resx`.

---

## 3. SEO

- Per-action `ViewData`:
  - `Title` (max 60 chars)
  - `MetaDescription` (max 160 chars)
  - `OgImage` (absolute URL, 1200×630 minimum)
  - `Canonical` (absolute URL, lowercase, no tracking params)
- JSON-LD on tour detail pages: `TouristAttraction` schema (`name`, `description`, `image`, `geo`, `offers`).
- `/sitemap.xml` proxied from API (`app.Map("/sitemap.xml", ...)`).
- `/robots.txt` static; disallow `/accounts/`, `/provider/`, `/admin/` (and other authenticated areas: `/business/`, `/creator/`, `/guide/`).
- Slug routes: `/tours/{slug}`, `/places/{slug}`, `/businesses/{slug}`, `/blog/{slug}`, `/help/{slug}`, `/offers/{slug}`.

---

## 4. Forms (Server)

- All forms use ViewModels with `DataAnnotations` (`[Required]`, `[StringLength]`, `[Range]`, `[RegularExpression]`).
- Every POST endpoint:
  - Controller action: `[ValidateAntiForgeryToken]`
  - View: `@Html.AntiForgeryToken()`
- Multi-step wizards persist via `TempData` (≤ 4 KB total) or hidden inputs. Larger state goes to a server-side draft store (see UI-UX-F5).

---

## 5. Caching (Server)

- `OutputCache` policies:
  - `PublicShort` = 5 min (homepage, catalog)
  - `PublicMedium` = 30 min (detail pages, business profiles)
  - `PublicLong` = 1 hour (blog, help, offers)
  - `PublicDay` = 24 hours (about, FAQ, static legal pages)
- `IMemoryCache` for:
  - Categories tree: 1 hour
  - Languages list: 24 hours
  - Commission rates: 10 min
- `ETag` + `Last-Modified` on detail endpoints by hashing entity `UpdatedAt` → produces `304 Not Modified` on revisit.

---

## UI-PERF — Performance Rules (CI-gated)

CI fails any build that violates these rules.

### §UI-PERF-A — Asset Delivery

| ID | Rule |
|---|---|
| **A1** | Bundle + minify all CSS/JS via `WebOptimizer`. Unminified assets forbidden in production builds. |
| **A2** | Vendor libs: CDN-first with `integrity="sha384-..."` + `crossorigin="anonymous"`. Local fallback in `wwwroot/lib/` for offline dev and CDN outages. |
| **A3** | Versioned static URLs via `asp-append-version="true"`. Hashed URLs serve `Cache-Control: public, max-age=31536000, immutable`. |
| **A4** | Response compression: Brotli (preferred) + Gzip fallback. Enable in `Program.cs`. |
| **A5** | Preconnect critical origins only: `api.yallajo.com`, `api.mapbox.com`. `dns-prefetch cdn.jsdelivr.net` for vendor libs. **No Google Fonts preconnect** — fonts are self-hosted (see V3). |
| **A6** | All `<script>` tags use `defer` or `async`. Inline scripts ≤ 2 KB **and must carry a CSP nonce per SEC1** (the only inline script in practice is the T5 theme bootstrap). |
| **A7** | Lazy-load below-the-fold media: `loading="lazy"` on `<img>`, `IntersectionObserver` for Mapbox / embeds. |
| **A8** | Inline critical CSS (~14 KB max) for homepage, tour grid, tour detail. Rest of CSS loads async. |

### §UI-PERF-C — Output & CDN Caching

| ID | Rule |
|---|---|
| **C1** | Public pages: `OutputCache` policies per §5. TTLs: `/` = 5 min, `/tours` = 5 min, tour detail = 30 min, business detail = 30 min, blog = 1 h, FAQ/about = 24 h, help = 1 h. |
| **C2** | Authenticated areas (`/accounts/*`, `/provider/*`, `/admin/*`, and other non-public areas): `NoStore = true`, `Cache-Control: no-store, no-cache, must-revalidate`. |
| **C3** | Tag-based invalidation via integration events: `tour:{id}`, `place:{id}`, `category:tree`, `homepage`, and `lookups` (ContentCore reference lists — languages, categories). Facades evict after a successful write via `IOutputCacheStore.EvictByTagAsync("lookups", ct)` (see CONTROLLER_AUTHORING_GUIDE §7). |
| **C4** | `IMemoryCache` for catalog-supporting data per §5. |
| **C5** | `ETag` + `Last-Modified` on detail pages — hash `UpdatedAt` for 304s. |
| **C6** | CDN edge caching (Cloudflare). Bypass on presence of auth cookie. |

### §UI-PERF-API — API Consumption

| ID | Rule |
|---|---|
| **API1** | Use `Task.WhenAll` for independent API calls. Sequential `await` for independent calls is forbidden. When a controller composes several independent Facades (CONTROLLER_AUTHORING_GUIDE §9.5 Pattern A), gather them with `Task.WhenAll`, then run `GuardSignOut` on each result. |
| **API2** | Fire-and-forget analytics: `_ = analyticsApi.RecordInteractionAsync(...)` (do not await on render path). |
| **API3** | Polly: 3 retries with exponential backoff (200 ms, 800 ms, 3.2 s) on 5xx only. Circuit breaker: 5 failures / 30 s window, half-open after 30 s. |
| **API4** | Hard timeouts: default 30 s, homepage 5 s, autocomplete 2 s. |
| **API5** | All HTTP via `IHttpClientFactory`. `new HttpClient()` is forbidden. |
| **API6** | `Accept-Encoding: gzip, deflate, br` on every outbound request (`AutomaticDecompression`). |
| **API7** | Use batch endpoints over loops. `foreach` + single-GET inside views or controllers is forbidden. |
| **API8** | `SocketsHttpHandler`: `MaxConnectionsPerServer = 100`, `PooledConnectionLifetime = 5 min`. |

### §UI-PERF-R — Page Rendering

| ID | Rule |
|---|---|
| **R1** | Controllers and views are async-only. `.Result` and `.Wait()` are forbidden. |
| **R2** | **First paint must be server-rendered.** No AJAX for primary content (hero, listing, detail). AJAX is permitted only for *post-SSR refinement* (filter changes, favorite toggle, comment submit, infinite scroll). |
| **R3** | `<RazorCompileOnBuild>true</RazorCompileOnBuild>` in csproj. |
| **R4** | Pagination default 20, max 50. Always `Math.Clamp(pageSize, 1, 50)`. |
| **R5** | `TempData` max 4 KB total. Wizard state larger than that goes to DB / draft store. |
| **R6** | Stream large lists: DataTables server-side mode for tables > 500 rows; `IAsyncEnumerable` for exports > 10 K rows. |
| **R7** | No N+1 in views. Lazy-loading navigation properties inside `@foreach` is forbidden. |

### §UI-PERF-I — Images

| ID | Rule |
|---|---|
| **I1** | Serve modern formats via `<picture>`: AVIF → WebP → JPEG/PNG fallback. |
| **I2** | `srcset` + `sizes` with 5 widths: 320w / 640w / 960w / 1280w / 1920w. |
| **I3** | `width` + `height` attrs on every `<img>`. CSS `aspect-ratio` for dynamic sizing. |
| **I4** | `<link rel="preload" as="image">` for hero LCP image on landing pages. |
| **I5** | Image weight budgets: thumb 30 KB, hero 200 KB, detail primary 150 KB, gallery 100 KB each, avatar 20 KB. Total page images ≤ 1 MB. |
| **I6** | LQIP: 16×16 base64 blur inlined as `background-image` until full image loads. |
| **I7** | Image CDN (Cloudflare Images / Azure Front Door / ImageKit). `Cache-Control: public, max-age=31536000, immutable`. |

### §UI-PERF-J — JavaScript

| ID | Rule |
|---|---|
| **J1** | **New code is vanilla JS.** jQuery is permitted *only* for (a) template-bundled scripts that ship with Webestica and (b) the `jquery-validation` / `jquery-validation-unobtrusive` stack loaded through `_ValidationScriptsPartial` for server-rendered form validation (CONTROLLER_AUTHORING_GUIDE §10/§14). Do not author new application code against jQuery. |
| **J2** | Page-specific bundles, not one monolith: `home.bundle.js` (TinySlider/AOS/PureCounter), `catalog.bundle.js` (Choices.js/noUiSlider/Mapbox), `booking.bundle.js` (Flatpickr/Stepper), `dashboard.bundle.js` (ApexCharts/Dropzone), `editor.bundle.js` (Quill/GLightbox). |
| **J3** | No inline `onclick=` etc. Attach handlers via `addEventListener` (CSP-friendly). |
| **J4** | Debounce: autocomplete input 300 ms, scroll handlers via `requestAnimationFrame` 100 ms, resize 250 ms. |
| **J5** | Batch DOM reads before writes. Animate via transforms, not geometric properties (see M2). |
| **J6** | *Guidance:* offload synchronous JS work > 50 ms on the main thread to a Web Worker. Rare in SSR MVC — apply when measured, not preemptively. |
| **J7** | Minify and tree-shake all bundles. Custom Bootstrap SASS build excluding unused components (see V1). |

### §UI-PERF-S — SignalR

| ID | Rule |
|---|---|
| **S1** | WebSocket transport with LongPolling fallback. Skip SSE. |
| **S2** | Connect on authenticated pages, **and** on the public tour-detail / booking page for the `tour:{tourId}` group only (live slot capacity per CAL3 / RT1). No hub connection on any other public page. |
| **S3** | Send IDs only (e.g., `{notificationId}`). Client fetches detail on demand. |
| **S4** | Client: `withAutomaticReconnect([0, 2000, 10000, 30000, 60000])`. After 5 failed reconnect attempts, surface a manual "Reconnect" toast. |
| **S5** | Groups: `user:{userId}`, `provider:{providerId}`, `admin`. **Broadcasting to all clients is forbidden.** |
| **S6** | Max 5 concurrent connections per user. Backend disconnects oldest on 6th. |
| **S7** | Client `keepAliveIntervalInMilliseconds` 15 s. Server timeout 30 s. |

### §UI-PERF-D — Data

| ID | Rule |
|---|---|
| **D1** | Page-number paging is the current standard. Each feature declares its own paged response (`Items`, `PageNumber`, `PageSize`, `TotalCount`, `HasPreviousPage`, `HasNextPage`) — no generic `PagedResponse<T>`, no `TotalPages` on the wire. Drive the pager only from `HasPreviousPage`/`HasNextPage`; never compute page counts in the view. Clamp page size per UI-PERF-R4 (`Math.Clamp(pageSize, 1, 50)`). Cursor-based paging is reserved for high-volume / infinite-scroll lists (future). See CONTROLLER_AUTHORING_GUIDE §13. |
| **D2** | DTOs projected at query level — never serialize entity classes. |
| **D3** | Reuse one `JsonSerializerOptions` instance per app domain. |

### §UI-PERF-V — Bootstrap & Vendor

| ID | Rule |
|---|---|
| **V1** | Custom Bootstrap SASS build with unused components stripped (~230 KB → ~80 KB target). |
| **V2** | Page-specific vendor JS loading (see J2). |
| **V3** | **Self-host fonts** in `wwwroot/fonts/` with `font-display: swap`. **Google Fonts CDN is forbidden** (privacy + perf + GDPR/PDPL exposure). |
| **V4** | Subset fonts to Latin + Arabic only (~100 KB → ~30 KB target). |

### §UI-PERF-H — Hosting

| ID | Rule |
|---|---|
| **H1** | HTTP/2 enabled (Kestrel default on .NET 9). |
| **H2** | `UseHsts()` with 1-year `max-age` + `includeSubDomains` + `preload`. TLS 1.3 minimum. |
| **H3** | Cloudflare: origin shielding ON, auto-minify, Brotli at edge, 1-year immutable on hashed static URLs. |
| **H4** | `/health` (liveness) + `/health/ready` (readiness). Auto-scale rules: CPU > 70% or request queue depth > 50. |
| **H5** | `StaticFileOptions.OnPrepareResponse` sets immutable headers for hashed URLs. |
| **H6** | **Compress once.** Production: Brotli at the Cloudflare edge, Kestrel compression off on responses Cloudflare will rewrite. Origin-only environments (staging without CDN): Kestrel Brotli/Gzip on. Double compression is forbidden. |

### §UI-PERF-BUDGETS — Performance Budgets (CI gates)

**Core Web Vitals (mobile):**

| Metric | Homepage (slow 3G) | Catalog & detail (3G fast) | Notes |
|---|---|---|---|
| LCP | 1.5 s / 2.0 s | 2.5 s / 3.0 s | target / ceiling |
| FCP | 1.2 s / 1.5 s | 1.8 s / 2.2 s | target / ceiling |
| TTI | 2.5 s / 3.0 s | 3.5 s / 4.0 s | target / ceiling |
| TBT | 150 / 200 ms | 200 / 300 ms | target / ceiling |
| CLS | 0.05 / 0.10 | 0.05 / 0.10 | target / ceiling |
| INP | 150 / 200 ms | 150 / 200 ms | target / ceiling |

> **FID is not tracked.** Google deprecated FID in March 2024; INP replaces it.
> Slow-3G targets apply to the homepage only. Catalog and detail pages target *3G fast* — slow-3G LCP < 2.0 s on a 1.5 MB tour detail page is physically impossible (~30 s wire time at 400 kbps).

**Network / page weight (gzipped):**

| Asset | Target | Ceiling |
|---|---|---|
| HTML | 60 KB | 100 KB |
| JS (per page bundle) | 150 KB | 200 KB |
| CSS | 30 KB | 50 KB |
| Images (total per page) | 800 KB | 1 MB |
| Fonts | 30 KB | 50 KB |
| HTTP requests | 60 | **75** |
| Total page weight | 1.2 MB | 1.5 MB |

**Server p95 TTFB:**

| Page tier | Target | Ceiling |
|---|---|---|
| Anonymous cached | 100 ms | 200 ms |
| Anonymous uncached | 300 ms | 500 ms |
| Authenticated views | 500 ms | 1000 ms |
| Provider / admin dashboards | 600 ms | 1200 ms |
| Search results | 400 ms | 800 ms |

**SignalR:** hub message p95 latency < 200 ms; reconnect median < 5 s; 10 K+ concurrent connections per server.

### §UI-PERF-M — Monitoring

| ID | Rule |
|---|---|
| **M1** | Application Insights tracks p50/p95/p99. Alert when p95 > ceiling for 5+ min. |
| **M2** | RUM via `web-vitals`: `onLCP`, `onINP`, `onCLS`. POST to `/api/v1/analytics/rum`. |
| **M3** | Lighthouse CI in pipeline. `categories:performance` minimum 0.9. `largest-contentful-paint` budget 2500 ms. |
| **M4** | Synthetic checks every 5 min from 3 geos (Pingdom / UptimeRobot). |
| **M5** | Bundle-size tracking in CI. Fails if any bundle grows > 10% vs main branch. |

> Note: bare `M3` references elsewhere in this document (NF7, CAL3, T4, RT1, M2) refer to **UI-UX-M3 (reduced motion)**, not UI-PERF-M3. When citing in code review, prefix the family: `UI-PERF-M3` for Lighthouse, `UI-UX-M3` for reduced motion.

### §UI-PERF-X — Forbidden Anti-Patterns

| ID | Forbidden |
|---|---|
| **X1** | `<script>` in `<head>` without `defer` or `async` (except the theme-bootstrap inline script per T5). |
| **X2** | Inline base64 images > 5 KB. |
| **X3** | Synchronous `XMLHttpRequest`. |
| **X4** | `Thread.Sleep` / `Task.Delay().Wait()` in controllers or middleware. |
| **X5** | `new HttpClient()` anywhere in the codebase. |
| **X6** | Inline `style="..."` for **static** styling. Inline styles **are permitted** for dynamic computed values (per-row progress widths, calculated colors, CSP nonces). |
| **X7** | `document.write()`. |
| **X8** | Loading full jQuery solely for `$.ajax` — use `fetch`. |
| **X9** | Sending JWTs to client-side JS as variables. |
| **X10** | `font-display: block`. |
| **X11** | More than one SignalR hub connection per page. |
| **X12** | `setInterval` polling while SignalR is connected. (Polled manual refresh is allowed only as the PE2 fallback when the hub is down.) |
| **X13** | Animating `width`, `height`, `top`, `left`. Use `transform`. |
| **X14** | ApexCharts on public marketing pages (dashboards only). |
| **X15** | JWT in `localStorage` or `sessionStorage`. |

### §UI-PERF-PAGE — Per-Page Targets

| Page | LCP target | TTI target | Page weight target |
|---|---|---|---|
| `/` (Home) | 1.5 s | 2.5 s | 1.0 MB |
| `/tours` (catalog) | 1.8 s | 3.0 s | 1.2 MB |
| `/tours/{slug}` (detail) | 2.0 s | 3.5 s | 1.5 MB |
| `/auth/sign-in` | 1.2 s | 2.0 s | 300 KB |
| `/accounts/bookings` | 1.5 s | 2.5 s | 500 KB |
| `/provider/dashboard` | 1.8 s | 3.0 s | 800 KB |
| `/admin/dashboard` | 2.0 s | 3.5 s | 1.0 MB |
| `/search` | 1.5 s | 2.5 s | 600 KB |
| `/blog/{slug}` | 1.8 s | 3.0 s | 1.0 MB |

### §UI-PERF-CHECKLIST — Per-Page Implementation Checklist

Before merging a new page:
- [ ] `OutputCache` policy assigned (or `NoStore` for authenticated)
- [ ] Independent API calls use `Task.WhenAll`
- [ ] Every `<img>` has explicit `width` + `height`
- [ ] `loading="lazy"` on all below-fold images
- [ ] Page JS scoped in `@section Scripts`
- [ ] Razor-rendered HTML < 30 KB
- [ ] No N+1 in views (verified with profiler or eager-load assertion)
- [ ] `ETag` on detail pages
- [ ] `[RequirePermission(WebPermission.X.Y)]` on protected routes (mirrors backend `MustHavePermissionAttribute`)
- [ ] Tested on throttled 3G (slow on homepage, fast 3G elsewhere)
- [ ] Lighthouse mobile score ≥ 90

---

## UI-UX — User Experience Rules

### §UI-UX-D — Device & Layout

| ID | Rule |
|---|---|
| **D1** | Mobile and desktop are equal-priority. Support 320 – 2560 px. Bootstrap breakpoints: xs 0, sm 576, md 768, lg 992, xl 1200, xxl 1400. |
| **D2** | Touch targets ≥ 44 × 44 px. `min-height: 2.75rem`, `min-width: 2.75rem` on all interactive controls. |
| **D3** | Mobile bottom nav at ≤ 768 px (5 slots): Browse / Search / Wishlist / Bookings / Profile + hamburger overflow. Hide on auth screens. `navbar fixed-bottom d-md-none`. |
| **D4** | Filter sidebar on desktop becomes a bottom-sheet `offcanvas` on mobile. Sticky `Apply` button + active filter count badge. |
| **D5** | Sticky-bottom CTAs on mobile (`Book Now` on tour detail, `Save & Continue` on multi-step wizards). |

### §UI-UX-N — Network Adaptation

| ID | Rule |
|---|---|
| **N1** | Honor the `Save-Data` request header on the server (`Vary: Save-Data`) and `navigator.connection.saveData` on the client. When enabled: lower image quality tier, no autoplay video, no parallax. |
| **N2** | *Progressive enhancement only:* if `navigator.connection.effectiveType` is available (Chromium browsers), reduce image quality further on `3g` / `2g` and disable autoplay carousels. Features must not depend on this API — Safari and Firefox do not ship it stably. |
| **N3** | *Optional, defer unless PWA is a roadmap goal:* a service worker may cache static assets only (CSS/JS/fonts/logos). Caching HTML, API responses, or user data is forbidden. CDN `Cache-Control: immutable` already handles the asset case — do not implement SW without a clear use case. |
| **N4** | If a service worker is added: on update, show a non-blocking toast "App updated. [Reload]". Never auto-reload. |
| **N5** | If a service worker is added: ship `offline.html` fallback with a Retry button. |

### §UI-UX-PE — Progressive Enhancement

| ID | Rule |
|---|---|
| **PE1** | Core functionality works without JS. Forms POST natively. `href="javascript:..."` is forbidden. |
| **PE2** | Graceful degradation when JS-dependent features are unavailable: Mapbox → text list of stops; SignalR → polled manual refresh button; Web Speech API absent → hide voice input. |
| **PE3** | Hydration-free. Server renders complete HTML. AJAX may refine (per R2) but never re-renders the initial page from a JSON payload. |
| **PE4** | Native feature detection (`'IntersectionObserver' in window`). No Modernizr. |

### §UI-UX-A11Y — Accessibility (WCAG 2.1 AA)

| ID | Rule |
|---|---|
| **A11Y1** | WCAG 2.1 **AA is mandatory**. Contrast 4.5:1 normal text, 3:1 large text. Full keyboard nav: Tab / Shift+Tab / Enter / Space / Esc / arrow keys. ARIA labels on icon-only buttons. `:focus-visible` ring 2 px solid primary. |
| **A11Y2** | Skip link: `<a href="#main" class="visually-hidden-focusable">Skip to main content</a>`. |
| **A11Y3** | Semantic HTML. `<div onclick>` is forbidden — use `<button>` or `<a>`. |
| **A11Y4** | Form labels via `<label asp-for>`. Validation messages via `<span asp-validation-for role="alert">`. Associate errors with inputs via `aria-describedby`. |
| **A11Y5** | Status badges use color **and** icon **and** text — never color alone. Charts use patterns + color. |
| **A11Y6** | Exactly one `<h1>` per page. No skipping heading levels. |
| **A11Y7** | `alt` on every meaningful image. Decorative images: `alt=""`. `aria-hidden="true"` on icons that accompany a labeled action. |
| **A11Y8** | Keyboard shortcuts: `Ctrl+/` or `?` opens shortcut help; `Esc` closes modals/offcanvas; `Ctrl+K` opens search palette. |
| **A11Y9** | `aria-live="polite"` on the notification bell badge. Toasts: `role="status" aria-live="polite"`. Critical alerts: `role="alert" aria-live="assertive"`. |

> WCAG AAA features (high-contrast theme, font scaling controls, color-blindness simulators, screen-reader verbose mode) are **not** in scope. WCAG AA is the contractual baseline. Reduced-motion preference is already handled by M3.

### §UI-UX-L — Loading States

| ID | Rule |
|---|---|
| **L1** | Skeleton loaders matching final layout (4–6 cards for grids). Bootstrap `placeholder` classes. |
| **L2** | Submit buttons: spinner replaces label text; button is `disabled` during action. |
| **L3** | NProgress-style top progress bar for navigation between pages. |
| **L4** | Skeletons mimic structure (not generic grey rectangles). Subtle 1.5 s pulse animation. |
| **L5** | Skeletons appear within 100 ms of the user action. |
| **L6** | Empty states have a CTA: "No bookings yet. [Browse tours →]", "Save tours you love for later", "No tours match. [Clear filters]". |

### §UI-UX-F — Forms

| ID | Rule |
|---|---|
| **F1** | Validate on `blur` after the first interaction. Validate all fields on submit. |
| **F2** | `<small class="form-text">` helper text always visible (no hover-to-reveal). Required: `<span class="text-danger">*</span>` next to the label. |
| **F3** | Password strength meter (200 ms debounce). Bar: red → yellow → green. List unmet requirements explicitly. |
| **F4** | Provider tour creation wizard, 5 steps: Basic / Pricing / Schedule / Images / Review. Per-step validation. Back navigation preserves entered data. |
| **F5** | Auto-save every 30 s to `localStorage`. *If* `POST /api/v1/drafts/{type}` ships on the backend, also sync there with a "Saved Xs ago" indicator. Until that endpoint exists, `localStorage` is the source of truth — do not block UX on the missing API. Show recovery banner on next visit. |
| **F6** | Inline errors in red below each field. On submit failure, scroll to the first invalid field. |
| **F7** | Button states: disabled when invalid (after first submit attempt), spinner during submit, re-enabled if validation fails again. |
| **F8** | Destructive confirmation modal: title = question, body = consequences + (for bookings) refund preview, two buttons — "Keep booking" primary (autofocus), "Yes, cancel" danger style. |
| **F9** | `beforeunload` warning whenever any form field is dirty — no field-count exception. |

### §UI-UX-NF — Notifications

| ID | Rule |
|---|---|
| **NF1** | Toast position bottom-right. Max 3 visible. Auto-dismiss 5 s. Bootstrap Toast component. Toasts carry AJAX / optimistic feedback only; full-page flash after a PRG redirect uses the server-rendered `_Alerts` partial (`TempData["Success"]` / `["Error"]`) — inline alert markup is legacy (CONTROLLER_AUTHORING_GUIDE §10). |
| **NF2** | Inline errors below the field or submit button. Include `[Retry]` on API errors. Don't show modals for recoverable errors. |
| **NF3** | Modals reserved for critical failures only: payment failed, account locked, session expired. |
| **NF4** | Max 3 simultaneous toasts; queue the rest. Dedupe identical messages within 2 s. |
| **NF5** | `role="status" aria-live="polite"` on toast container. Dismiss button is keyboard-focusable. |
| **NF6** | Optimistic UI: heart fills instantly on tap; revert + toast on API failure. Applies to favorites and review-helpful upvotes. |
| **NF7** | New SignalR notification: bell badge increments + subtle 200 ms shake. **Skip the shake when `prefers-reduced-motion: reduce` (see M3).** Click opens dropdown with the latest 5 items. "View all" → `/accounts/settings/notifications`. |

### §UI-UX-M — Motion

| ID | Rule |
|---|---|
| **M1** | Duration 200–300 ms. Material standard `cubic-bezier(0.4, 0, 0.2, 1)`. Enter 200 ms, exit 150 ms. Component-specific gestures may exceed this when justified (theme cross-fade T4 = 250 ms; Bootstrap-native modal/offcanvas transitions). |
| **M2** | Prefer animating `transform` and `opacity`. Animating `width`, `height`, `top`, `left` is forbidden. `margin` is discouraged — only animate it when a layout collapse genuinely requires it; keep duration ≤ 200 ms and minimize the repainted area. |
| **M3** | Respect `prefers-reduced-motion: reduce`: `animation-duration: 0.01ms !important; transition-duration: 0.01ms !important;`. Disable autoplay carousels, parallax, AOS, and the NF7 bell shake. |
| **M4** | No animation on critical paths: checkout flow steps, payment forms. |
| **M5** | Motion must be meaningful: state change, focus shift, loading feedback. Decorative wiggles, bouncing, parallax-for-fun are forbidden. |
| **M6** | Page transitions are instant. Optional NProgress bar at the top of the viewport during navigation. |

### §UI-UX-T — Theme

| ID | Rule |
|---|---|
| **T1** | On first visit, respect `prefers-color-scheme`. |
| **T2** | Manual toggle overrides system. Persist choice in `localStorage` and a cookie (for SSR class). |
| **T3** | Use Bootstrap 5.3 native dark mode: CSS variables `--bs-*`, `<html data-bs-theme="dark">`. |
| **T4** | Theme switch animates 250 ms cross-fade. Skip when `prefers-reduced-motion: reduce`. |
| **T5** | Inline `<script>` at the very top of `<head>` reads cookie/localStorage and sets `data-bs-theme` **before** CSS loads (prevents FOWT — flash of wrong theme). This is the one allowed exception to X1. |
| **T6** | `<picture>` with `media="(prefers-color-scheme: dark)"` for dark-mode-specific image variants. |

### §UI-UX-S — Search & Filtering

| ID | Rule |
|---|---|
| **S1** | Hybrid model: initial search results are server-rendered. Subsequent filter changes use AJAX `fetch` + `history.pushState`. Browser back restores filter state. Aligns with R2 (first paint SSR, refinement AJAX). |
| **S2** | Autocomplete: 300 ms debounce. Top 5 suggestions categorized (Tours / Places / Categories). "View all results for 'X'" link at the bottom. |
| **S3** | Desktop: filter sidebar (collapsible accordion). Mobile: bottom-sheet offcanvas + sticky Apply. Active filter chips above results. Total active-filter count badge. |
| **S4** | Filter state resets on full browser close. In-session back/forward restores state. |
| **S5** | Empty state: "No tours match" + [Clear all] + 3 popular fallback recommendations. |
| **S6** | Default sort: Popularity (Bayesian smoothed). Available sorts: Price asc/desc, Rating, Newest, Duration, Distance. Sort selector is sticky inside the filter bar. |

### §UI-UX-IMG — Images (UX layer)

Inherits weights and formats from UI-PERF-I.

| ID | Rule |
|---|---|
| **IMG1** | Tour detail: hero + thumbnail strip. Lightbox via GLightbox. |
| **IMG2** | LQIP placeholder: 16×16 base64 inlined as `background-image` until full image loads. |
| **IMG3** | Below-fold images: `loading="lazy"`. Hero image: `loading="eager" fetchpriority="high"` + `<link rel="preload" as="image">`. |
| **IMG4** | Explicit `width` + `height` on every `<img>`. Container uses CSS `aspect-ratio: 16/9` for dynamic sources. `object-fit: cover`. |
| **IMG5** | Per-image budgets per UI-PERF-I5. |

### §UI-UX-MAP — Map

| ID | Rule |
|---|---|
| **MAP1** | `IntersectionObserver` lazy-loads Mapbox GL JS (~250 KB saved on pages where map is below the fold). |
| **MAP2** | Static Mapbox image (~30 KB) placeholder until the user scrolls the map into view. |
| **MAP3** | One Mapbox instance per page maximum. |
| **MAP4** | Render polyline by `SortOrder`. Meeting-point uses a flag marker. Waypoints are numbered markers. Nearby businesses use a secondary marker style. |
| **MAP5** | Zoom < 13: cluster pins. Zoom ≥ 13: individual pins. |
| **MAP6** | If Mapbox fails to load: fall back to a text list of stops sorted by distance + Retry button. |

### §UI-UX-CAL — Booking & Calendar

| ID | Rule |
|---|---|
| **CAL1** | Calendar heatmap with **per-month lazy load** — render only the visible month, fetch adjacent months on navigation. Do not render 90 days inline upfront. Markers: 🟢 available > 50% / 🟡 limited ≤ 50% / 🔴 full / ⚫ past or blacked-out. Click a date → time-slot list. |
| **CAL2** | Mobile: bottom-sheet calendar + sticky Apply. |
| **CAL3** | SignalR group `tour:{tourId}`. On `SlotCapacityChanged`: smooth count animation + optional flash (both suppressed under M3 reduced-motion). |
| **CAL4** | 10-min slot lock countdown is always visible during checkout. 1-min warning toast. On expiry, redirect to slot picker. |
| **CAL5** | Booking stepper: Date + Participants / Traveler details / Add-ons / Promo + Loyalty / Payment. Per-step validation. Back never loses data. |
| **CAL6** | Disabled dates show a tooltip: "No availability" / "Blocked out" / "Past date". |
| **CAL7** | 2-hour minimum lead time. Nearby slots greyed with tooltip "Bookings need 2 hours notice". |
| **CAL8** | `localStorage` saves abandoned booking state. On return: "You had a booking in progress for [Tour]. [Continue] [Discard]". |

### §UI-UX-WL — Wishlist

| ID | Rule |
|---|---|
| **WL1** | Guests: heart is disabled with tooltip "Sign in to save". Click opens sign-in modal with return URL preserved. |
| **WL2** | Optimistic toggle (fill instantly). Revert + toast on API failure. |
| **WL3** | Toast "Added to wishlist" links to `/accounts/wishlist`. "Removed from wishlist" toast includes `[Undo]`. |
| **WL4** | Logged-in users see counter badge in nav: "Wishlist (12)". |
| **WL5** | Hard cap: 500 items. At limit, modal "Wishlist full. Remove an item first. [Manage wishlist]". |

### §UI-UX-PROV — Provider Tour Wizard

| ID | Rule |
|---|---|
| **PROV1** | Follows F4 — 5 steps, per-step validation, back navigation preserves data. |
| **PROV2** | `localStorage` key `yallajo:provider:tour-draft:{userId}`. If the draft endpoint exists per F5, server sync every 2 min with "Saved Xs ago" indicator. |
| **PROV3** | On wizard reload with existing draft: "You have an unsaved draft from 2 hours ago. [Continue] [Discard]". |
| **PROV4** | Cancel modal: "Discard changes? All progress will be lost." → [Keep editing] primary / [Discard] danger. |
| **PROV5** | Sidebar is collapsible to icon-only. State persists in `localStorage`. |
| **PROV6** | Sticky top bar: bell + language + profile + Quick-add dropdown (New tour / New discount / New review reply). |

### §UI-UX-PRINT — Print & Export

| ID | Rule |
|---|---|
| **PRINT1** | `@media print` on booking-confirm page: hide nav/footer; show details + QR code for `ConfirmationCode`. Print button calls `window.print()`. |
| **PRINT2** | "Download PDF" → `GET /api/v1/invoices/{id}/download` (backend QuestPDF). |
| **PRINT3** | `.ics` export generated client-side: `SUMMARY` (tour name), `DTSTART` (slot time UTC), `LOCATION` (meeting point), `DESCRIPTION` (details + confirmation code). Compatible with Apple Calendar / Google Calendar / Outlook. |

### §UI-UX-TIP — Tooltips

| ID | Rule |
|---|---|
| **TIP1** | Mix: always-visible helper text for form fields; tooltip for icon-only buttons; info-icon → modal for complex rules (e.g., refund policy). |
| **TIP2** | Bootstrap: `data-bs-toggle="tooltip"`. 500 ms hover delay. Mobile dismisses on tap-outside. |
| **TIP3** | `aria-describedby` always points at the tooltip content for screen readers. |
| **TIP4** | Complex topics: "Learn more →" deep-link to `/help/{slug}`. |

### §UI-UX-REV — Reviews

| ID | Rule |
|---|---|
| **REV1** | Post-experience email links to `/accounts/bookings/{id}?action=review`, which opens the review modal. |
| **REV2** | "Write a review" button on tour detail page for users whose bookings on that tour are `Completed`. |
| **REV3** | Form: stars 1–5 in 0.5 increments (required), title ≤ 150 chars (optional), content 20–2000 chars (required), photos max 3 / 5 MB each (JPG/PNG/WebP, optional, Dropzone). |
| **REV4** | 30-day review window from completion date. Reminder banner "You can review until [date]". After 30 days the action is disabled with tooltip "Review window closed". |
| **REV5** | 48-hour edit window after posting. Countdown badge "Edit (12h left)". After 48 h the badge is replaced with "Posted" + timestamp. |
| **REV6** | Profanity-flagged review: "Pending moderation. We'll publish it after review." User may edit and resubmit. |

### §UI-UX-CC — Cookie Consent

| ID | Rule |
|---|---|
| **CC1** | **If any non-essential cookies are loaded** (analytics, marketing, A/B testing — including GA4, Mixpanel, Meta Pixel), a consent banner is required, regardless of launch market. Jordan's PDPL (2023) and EU GDPR both classify analytics cookies as non-essential. Categories: Necessary (always-on) / Functional / Analytics / Marketing. Block all non-essential scripts until consent. |
| **CC2** | If the platform ships only auth + functional cookies, no banner is required. A privacy policy link in the footer suffices. |

> Implementation rule: either (a) defer GA4/Mixpanel until the banner ships, or (b) ship the banner before merging any analytics SDK. Do not merge analytics without one of the two in place.

### §UI-UX-GEO — Geographic & Currency

| ID | Rule |
|---|---|
| **GEO1** | Jordan-first market: default currency JOD, default language Arabic (`ar`), phone prefix `+962`. (Aligns with §2.) |
| **GEO2** | Show tour base currency in catalog (JOD/USD/EUR). User toggles preferred display currency in `/accounts/settings`. Server-side FX cache (daily refresh) is a future enhancement. |
| **GEO3** | Gregorian calendar default. Optional Hijri toggle for Arabic users. UTC stored, IANA timezone for display (default `Asia/Amman`). |
| **GEO4** | Addresses: free-text on launch. Future: Mapbox Geocoding API for Jordan addresses. |

### §UI-UX-RT — Real-Time

| ID | Rule |
|---|---|
| **RT1** | `SlotCapacityChanged` on `tour:{tourId}` group → `{slotId, remainingCapacity}`. UI updates count + optional flash (suppressed under M3). |
| **RT2** | Cross-device booking sync via `user:{userId}` group. |
| **RT3** | Bell live updates per NF7. |
| **RT4** | Provider booking alerts on `provider:{providerId}` group: SignalR push + toast. |
| **RT5** | Admin moderation badge updates on the `admin` group. |

---

## UI-SEC — Security

| ID | Rule |
|---|---|
| **SEC1** | Send a Content-Security-Policy header on every response. `default-src 'self'`; explicitly allowlist Mapbox, the asset CDN, and App Insights origins. No `unsafe-inline` for scripts — the theme bootstrap script (T5) uses a per-request nonce. Report violations to `/api/v1/csp-report`. |
| **SEC2** | Send hardening headers on every response: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY` (or CSP `frame-ancestors 'none'`), `Referrer-Policy: strict-origin-when-cross-origin`, and a `Permissions-Policy` that disables unused features (camera, microphone, geolocation unless needed, payment). |
| **SEC3** | Razor output is HTML-encoded by default. `@Html.Raw()` is forbidden unless the value was server-side sanitized (Ganss.Xss / HtmlSanitizer) and the call site carries a `// SANITIZED:` comment naming the sanitizer. |
| **SEC4** | File uploads are validated by magic-byte signature, never by extension or client MIME type. Cap size per type. Strip EXIF. Store outside the web root (blob/disk) with randomized names; serve via an authenticated proxy action, never a direct static path. |
| **SEC5** | Rate-limit with the .NET 9 `RateLimiter`: sign-in 5/min/IP, OTP 5/min/user, search 30/min/IP, review submit 10/hour/user. On limit return `429` with a `Retry-After` header. |
| **SEC6** | No secrets in client code, committed config, or logs. Use Azure Key Vault (prod) and user-secrets (dev). A `gitleaks` scan is a CI gate. |
| **SEC7** | Anti-forgery on every state-changing request, including AJAX (send the `RequestVerificationToken` header). GET requests never mutate state. |

---

## UI-ERR — Error Handling & Resilience

| ID | Rule |
|---|---|
| **ERR1** | Custom branded error pages for 400 / 403 / 404 / 429 / 500 / 503, each with a search box and a back-to-home CTA. Stack traces are never shown in production. |
| **ERR2** | Register `UseExceptionHandler("/error")` + `UseStatusCodePagesWithReExecute`. Log the full exception with a correlation ID; show the user only the correlation ID ("Reference: ABC-123"). |
| **ERR3** | API failures degrade gracefully — a failed homepage section hides itself rather than 500-ing the whole page, rendering an inline "Couldn't load recommendations [Retry]" card instead. |
| **ERR4** | Generate a correlation ID (`X-Correlation-ID`) per request and propagate it to the API call, the logs, and App Insights. |
| **ERR5** | Serve a `503` maintenance page with a `Retry-After` header during deploys. |

---

## UI-OBS — Logging & Observability

| ID | Rule |
|---|---|
| **OBS1** | Structured logging (Serilog) with `UserId`, `CorrelationId`, `Route`, and `DurationMs` on every request; JSON sink to App Insights / Seq. |
| **OBS2** | Disciplined log levels and no PII in logs (redact emails, phone numbers, payment data): `Information` = requests, `Warning` = handled degradation, `Error` = unhandled. |
| **OBS3** | Audit log for sensitive actions (booking cancel, refund, provider approval, role/permission change) capturing who / what / when, written to an immutable store. |

---

## UI-CONTENT — Content & Copy

| ID | Rule |
|---|---|
| **CON1** | All UI copy lives in `.resx` (per §2). No hardcoded strings in Razor or JS. A CI grep gate flags literal user-facing text. |
| **CON2** | Voice is clear, concise, action-oriented. Buttons are verbs ("Book now", not "Submit"). Errors state what happened and how to fix it. No jargon, no blame. |
| **CON3** | Numbers, dates, and currency are formatted per culture via `IFormatProvider`, never string-concatenated. JOD uses 3 decimals (fils). Arabic-Indic numerals are configurable. |
| **CON4** | Truncate long text with the full value available on hover/expand. Tour titles clamp to 2 lines (`-webkit-line-clamp`). |

---

## UI-RTL — RTL-Specific

| ID | Rule |
|---|---|
| **RTL1** | Use CSS logical properties (`margin-inline-start`, `padding-inline-end`, `inset-inline`) instead of physical `left`/`right`. |
| **RTL2** | Directional icons (arrows, chevrons, back) flip in RTL; non-directional icons (clock, star, heart) do not. |
| **RTL3** | Numbers, phone numbers, codes, and Latin brand names stay LTR inside RTL text (use `<bdi>` or `dir="ltr"` spans). |
| **RTL4** | Every view is tested in both `ar` and `en` before merge — RTL verification is a merge-gate checklist item. |

---

## UI-TEST — Testing

| ID | Rule |
|---|---|
| **TEST1** | Every controller action has ≥ 1 integration test (happy path + auth-denied) via `WebApplicationFactory`. |
| **TEST2** | Critical flows have E2E coverage (Playwright): sign-in, search → book → pay, provider tour create, review submit. They run in CI on every PR. |
| **TEST3** | Automated accessibility checks (axe-core / Playwright-axe) run on key pages in CI and fail the build on WCAG AA violations. |
| **TEST4** | Visual regression snapshots cover core components (cards, nav, modals) in both themes and both directions. |

---

## UI-PAY — Payment UX

| ID | Rule |
|---|---|
| **PAY1** | Never store or log full card data. Payment fields are iframe/SDK-hosted by the PSP (PCI SAQ-A scope); no card data touches the server. |
| **PAY2** | Send an idempotency key on payment submission to prevent double-charge, and disable the pay button on click (per F7). |
| **PAY3** | Before the pay action, show the exact total, currency, refund terms, and the lock-expiry countdown (CAL4). |
| **PAY4** | Payment results are server-confirmed via webhook, never trusted from the client redirect alone. Show "Processing…" until confirmed. |

---

## UI-STATE — Session & Concurrency

| ID | Rule |
|---|---|
| **ST1** | Optimistic concurrency on edits (`RowVersion` / `UpdatedAt`). On conflict, show "This was changed by someone else. [Reload]". |
| **ST2** | Warn 2 minutes before session timeout with a "Stay signed in" modal (per NF3); save any draft before redirecting. |
| **ST3** | Keep multi-tab state consistent (theme, auth, wishlist) via `BroadcastChannel` or the `storage` event. |

---

## UI-JS — JavaScript Architecture

| ID | Rule |
|---|---|
| **JS1** | Module pattern, no globals. All page JS ships as ES modules (`<script type="module">`); nothing is attached to `window`. A single `window.YallaJo` namespace is allowed only for cross-bundle shared utilities (analytics, toast, i18n helper). |
| **JS2** | Declarative init via data-attributes. Components self-initialize by scanning the DOM for `data-yj-component="map\|calendar\|wishlist-toggle"` on `DOMContentLoaded`. No per-page hand-wired bootstrap. This lets SSR and partial reloads "just work." |
| **JS3** | Single delegated event root. Attach one delegated listener per event type at a page root using `data-yj-action="..."`, not N listeners per element. Pairs with J3 (no inline `onclick`) and survives DOM swaps from AJAX refinement (UI-UX-S1). |
| **JS4** | Idempotent init. Every component guards against double-init (`if (el.dataset.yjInit) return; el.dataset.yjInit = '1';`). Required because AJAX filter refinement (UI-UX-S1) and SignalR DOM updates (RT/CAL) re-inject markup. |
| **JS5** | Typed API client, no raw `fetch` in components. Client JS calls **MVC endpoints** (cookie-authenticated), never the API directly — Bearer/JWT and 401-refresh live server-side in `AuthTokenHandler` (§1), never in client JS. The one `apiClient` wrapper injects the anti-forgery token (SEC7), correlation ID (ERR4), and timeout/abort (JS6). Components never call `fetch()` directly. |
| **JS6** | `AbortController` on every request. Cancel in-flight requests on navigation and supersede stale autocomplete/filter calls — last-write-wins, no race-condition flicker. |
| **JS7** | Progressive-enhancement contract. Every interactive component degrades to a working server-rendered baseline (PE1–PE2). A CI lint forbids `href="javascript:"` and `<a>` acting as a button without a fallback. |
| **JS8** | No build-time framework creep. Vanilla + targeted libraries only (J1). Introducing React/Vue/Alpine/htmx requires an ADR and sign-off; it is not allowed ad hoc. |
| **JS9** | JS error boundary. A global `window.onerror` / `unhandledrejection` handler logs to `/api/v1/analytics/rum` (M2) with the correlation ID; a JS failure falls back to the SSR baseline rather than freezing a widget. |
| **JS10** | State lives in DOM/server, not a JS store. Source of truth is server-rendered HTML + `data-*`. Cross-tab sync uses `BroadcastChannel`/`storage` events (ST3). Prevents desync after back/forward (UI-UX-S4 restore). |

---

## UI-MODAL — Dialogs, Overlays & Behavior

| ID | Rule |
|---|---|
| **MOD1** | One modal at a time. Never stack modals; opening a second closes/replaces the first, or routes into a single multi-step modal. |
| **MOD2** | Focus trap + restore. Focus enters the modal on open, is trapped inside, and returns to the triggering element on close (A11Y1). Use Bootstrap's native modal — do not hand-roll the trap. |
| **MOD3** | Dismiss rules by severity. Non-destructive modals close on Esc + backdrop-click. Destructive/critical modals (F8 cancel, NF3 payment-failed/session-expired) allow Esc but disable backdrop-click (`data-bs-backdrop="static"`). |
| **MOD4** | `role="dialog"` + `aria-modal="true"` + `aria-labelledby` (title) + `aria-describedby` (body) (A11Y4). Critical blocking alerts use `role="alertdialog"`. |
| **MOD5** | Autofocus the safe action. The primary/safe button receives focus (F8: "Keep booking", never "Yes, cancel"). Never autofocus a destructive action. |
| **MOD6** | Mobile modals become bottom sheets. At ≤ 768px, modals render as bottom-sheet offcanvas with a sticky action bar (consistent with D4, CAL2, S3). Full-screen is reserved for multi-step wizards (CAL5, PROV1). |
| **MOD7** | Body scroll lock without layout shift. Lock background scroll while open and compensate scrollbar width (Bootstrap handles this — do not override). |
| **MOD8** | Async inside modals. An async action disables its button + shows an inline spinner (F7/L2) and never closes the modal until the server confirms (PAY4). On failure, keep the modal open with an inline error (NF2) — never toast-and-close. |
| **MOD9** | Deep-linkable critical modals. The review modal (REV1) and sign-in modal (WL1) are URL-addressable (`?action=review`, return-URL) so they survive refresh/share and the back button closes them via `history.pushState` (UI-UX-S1). |
| **MOD10** | Reduced-motion + RTL aware. Open/close animation respects `prefers-reduced-motion` (M3 → instant). Slide direction and close-button placement mirror in RTL (RTL1/RTL2). |
| **MOD11** | Toasts are not modals. Recoverable feedback uses toasts (NF1). Modals are reserved for destructive confirm (F8), critical blocking errors (NF3), required input (sign-in, review), and multi-step flows. |
| **MOD12** | Confirm only destructive/irreversible actions. Reversible actions (wishlist remove, mark-read) use optimistic UI + Undo toast (NF6/WL3), not a confirm modal. Reserve confirms for cancel-booking, refund, delete-tour, role change. |
| **MOD13** | Predictable overlay layering. A single documented z-index scale (CSS custom properties): dropdown < sticky CTA < offcanvas < modal < toast < tooltip. No ad-hoc `z-index: 9999`. |

---

## Rule Change Log

When a rule is added, removed, or modified, append a row here with rule ID, date, and one-sentence reason. The CI gate references rule IDs, not section numbers — keep IDs stable.

| Date | Rule ID(s) | Change | Reason |
|---|---|---|---|
| init | All | Initial extraction | Rules-only consolidation of the original UI-UX-Design.md spec. |
| init | A5 | Removed `fonts.googleapis.com` preconnect | Fonts are self-hosted per V3; preconnect was wasted. |
| init | R2 | Permitted post-SSR AJAX refinement | Resolves contradiction with S1. |
| init | J1 | Allowed jQuery for template-bundled scripts | Resolves friction with Webestica's jQuery dependencies. |
| init | H6 | "Compress once" — edge OR origin, never both | Removes redundant double-compression. |
| init | BUDGETS | Dropped FID | Deprecated by Google March 2024 in favor of INP. |
| init | BUDGETS | Split slow-3G vs 3G-fast by page tier | Slow-3G LCP < 2.0 s on 1.5 MB pages is physically impossible. |
| init | BUDGETS | Raised HTTP-request ceiling 50 → 75 | Realistic for travel-content pages. |
| init | X6 | Allowed inline styles for dynamic computed values | Per-row colors, calculated positions, CSP nonces. |
| init | N2 | Reframed as progressive enhancement only | NetInfo API is Chromium-only stable. |
| init | N3 | Marked optional, defer unless PWA is a goal | CDN immutable headers already solve asset caching. |
| init | J6 | Reframed as guidance, not hard rule | Web Workers are rarely needed in SSR MVC. |
| init | AAA suite | Removed entirely | WCAG AA is the contractual baseline; AAA features deferred indefinitely. |
| init | AAA4 | Removed colorblind SVG filter | Misconceived — a simulation filter does not help colorblind users. |
| init | F5 | localStorage authoritative; server sync conditional on endpoint shipping | `POST /api/v1/drafts/{type}` not in backend audit. |
| init | F9 | Removed "< 10 fields" exception | Arbitrary threshold caused inconsistent UX. |
| init | NF7 | Cross-referenced M3 for reduced-motion | Shake animation must respect user preference. |
| init | M2 | Softened margin animation from forbidden to discouraged | Sometimes required for legitimate layout collapse. |
| init | CAL1 | Switched to per-month lazy load | 90-day inline heatmap was performance-prohibitive. |
| init | CC1 | Banner required whenever analytics ship | GA4 under Jordan PDPL requires consent; previous "no banner" rule was legally exposed. |
| init | GEO1 / §2 | Aligned default culture as `ar` | Resolves contradiction between GEO1 and prior `en` default in the old §8 (now §2). |
| init | §1 Auth cookie | `SameSite=Lax` instead of `Strict` | `Strict` breaks OAuth redirect callbacks. |
| init | §0 | "Per task" instead of "per view" | Per-view skill load was friction without benefit. |
| init | SEC1–SEC7 | Added UI-SEC family | CSP, security headers, sanitization, upload validation, rate limiting, secret hygiene, anti-forgery were previously implicit. |
| init | ERR1–ERR5 | Added UI-ERR family | Branded error pages, global handler, graceful degradation, correlation IDs, maintenance page. |
| init | OBS1–OBS3 | Added UI-OBS family | Structured logging, PII discipline, immutable audit log. |
| init | CON1–CON4 | Added UI-CONTENT family | resx-only copy, voice standards, culture-aware formatting, truncation. |
| init | RTL1–RTL4 | Added UI-RTL family | Logical properties, icon flipping, LTR-in-RTL spans, RTL merge gate. |
| init | TEST1–TEST4 | Added UI-TEST family | Integration, E2E, automated a11y, and visual-regression coverage. |
| init | PAY1–PAY4 | Added UI-PAY family | PCI scope, idempotency, pre-pay disclosure, webhook confirmation. |
| init | ST1–ST3 | Added UI-STATE family | Optimistic concurrency, session-expiry warning, multi-tab consistency. |
| init | JS1–JS10 | Added UI-JS family | Module architecture, declarative/idempotent init, delegated events, typed API client, abort, error boundary — prevents DOM-swap bugs from S1/RT/CAL. |
| init | MOD1–MOD13 | Added UI-MODAL family | Unified dialog behavior: single-modal, focus trap, severity-based dismissal, ARIA, mobile bottom sheets, async handling, deep-linking, z-index scale. |
| init | S2 (UI-PERF) | Allowed hub on public tour-detail/booking page | CAL3/RT1 live slot capacity needs a hub, but tour detail is a public page — prior "auth pages only" made those rules unreachable. |
| init | JS3, JS4, JS5, MOD9 | Disambiguated `S1` → `UI-UX-S1` | `S1` collides (UI-PERF-S1 transport vs UI-UX-S1 search); bare cite was ambiguous for CI. |
| init | JS5 | Scoped client apiClient to MVC + dropped client-side 401-refresh | JWT/Bearer never reach client JS (§1); refresh lives in server-side AuthTokenHandler. |
| init | CAL3 | Suppress count flash under reduced motion | Consistency with RT1/NF7 which already honor UI-UX-M3. |
| init | M1 (UI-UX) | Permitted justified component-specific durations | Theme T4 250 ms and Bootstrap modal transitions legitimately exceed 200–300 ms. |
| init | A6 | Inline scripts must carry a CSP nonce | SEC1 forbids `unsafe-inline`; A6's allowed inline scripts must be nonce'd. |
| init | UI-PERF-M | Added prefix-disambiguation note for `M3` | UI-PERF-M and UI-UX-M both have an M3; clarifies reduced-motion cites. |
| init | X12 | Clarified "while SignalR is connected" | Removes apparent conflict with PE2's polled-fallback-when-down. |
| init | JS10 | Disambiguated `S4` → `UI-UX-S4` | Bare `S4` cite was ambiguous for CI; matches the `S1` disambiguation precedent. |
| init | Headings | Normalized heading hierarchy — rule families to H2, sub-families to H3 | UI-PERF/UI-UX were H1 while standalone families (UI-SEC…UI-MODAL) were H2; now all families are H2 peers under the single H1 title, sub-families are H3. |
| init | §1 | `YallaJoRequirePermission` → `RequirePermission` | Real web attribute is `RequirePermissionAttribute` (verified in `src/Hosts/YallaJo.Web/…/RequirePermissionAttribute.cs`). |
| init | §1 | Identity paths → `/auth/sign-in`, `/auth/sign-out` | Matches real `Program.cs` cookie options; prior `/sign-in`, `/logout`, `/access-denied` were wrong. |
| init | UI-PERF-D1 | Cursor → page-number paging as the standard | Real code uses per-feature page-number responses (Items/PageNumber/HasPrevious/HasNext) per CONTROLLER_AUTHORING_GUIDE §13; cursor deferred to high-volume lists. |
| init | UI-PERF-J1 | Allowed `jquery-validation(-unobtrusive)` | Server-rendered validation stack ships via `_ValidationScriptsPartial`; an accepted jQuery dependency. |
| init | UI-PERF-C3 | Added `lookups` cache tag | ContentCore reference lists evicted via `EvictByTagAsync("lookups")` in Facades (guide §7). |
| init | UI-PERF-API1 | Noted Pattern-A parallel Facades | Composite controllers gather independent Facades with `Task.WhenAll` then `GuardSignOut` each (guide §9.5). |
| init | UI-UX-NF1 | Distinguished toast vs `_Alerts` flash | Toasts = AJAX/optimistic; PRG full-page flash = `_Alerts` (guide §10); inline alert markup is legacy. |
| init | Routes | Lowercased real area paths (`/accounts/`, `/provider/`, `/admin/`, `/auth/sign-in`) | Customer area is `Accounts` (plural); generated URLs are lowercase; verified against `[Area("Accounts")]` controllers. |
| init | Related Architecture | Added cross-reference section | Points to CONTROLLER_AUTHORING_GUIDE.md as source-of-truth for pipeline/ApiResult/BaseController/DI; clarifies doc-ownership split. |
