# Cross-Cutting Platform — i18n/RTL · Performance · Accessibility · SEO — Build Plan (Tier D)

> **Scope:** the platform-wide layers that span every page rather than a single feature — **localization + RTL**, a **performance** pass, an **accessibility** audit, and **SEO/sitemap** finalization. These harden what Tiers A–C build.
> **Companion docs:** `UI-UX-Design.md` §8 (i18n+RTL), §9 (SEO), §18 (UI-PERF rules), §19 (UI-UX rules) · `Agents/UI/PERFORMANCE_OPTIMIZATION_GUIDE.md` · `Agents/Plans/ContentSeo-*`.
> **Authoring rules:** `CONTROLLER_AUTHORING_GUIDE.md` (authoritative).
> **API base path:** `/api/v1`
> **Status:** 📋 Planned — not started

---

## Why this plan

The Webestica template **ships RTL builds** and the backend exposes **translations, languages, SEO metadata, sitemaps, FAQs, and weather** — none of which is wired. The app also has no formal performance budget enforcement or accessibility audit. Unlike Tiers A–C (feature slices), this tier is mostly **infrastructure** (`Program.cs`, `_Layout`, shared partials, a thin SEO/translations client) plus a **rules-driven audit** across existing pages. It should run **after** the major page surfaces exist so there's something to localize, tune, and audit.

---

## Architecture conventions (read before building)

Where this tier adds MVC features (SEO admin, language switch endpoint) it follows layered-by-type per `CONTROLLER_AUTHORING_GUIDE.md` (four-tier `Controller → Facade → ApiClient → IApiClient`, `ApiResult`, suffix DI, `BaseController`, build csproj **alone**). Most of the work is **cross-cutting infra**, explicitly *outside* the per-feature layout:

- **Localization** = ASP.NET Core `RequestLocalization` + resource files + a culture cookie; configured in `Program.cs`, not a feature slice.
- **RTL** = a `dir`/`lang` toggle on `_Layout` driven by the current culture, swapping to the template's RTL stylesheet from `wwwroot/assets`.
- **Translations client** = a thin `TranslationsApiClient` + `TranslationsFacade` in `Areas/Content/` (reads `GET /content-core/translations/...`) — layered like any other feature; pages request localized entity text through it.
- **SEO** = a shared layout partial (`_SeoMeta`) fed per-page metadata + a couple of `[AllowAnonymous]` controller actions for `sitemap.xml` / `robots.txt`.
- **Performance/Accessibility** = audits, not code modules — enforce `UI-PERF-*` / `UI-UX-*` rules from `UI-UX-Design.md` §18/§19 + `PERFORMANCE_OPTIMIZATION_GUIDE.md`.

### Design & UI skills (mandatory for all view/UX work)

- **`ui-ux-pro-max`** + **`impeccable`** — RTL mirroring correctness, language-switcher UX, accessibility (focus order, ARIA, contrast, keyboard nav), Core-Web-Vitals-aware layout (no CLS).
- **`design-taste-frontend`** — hardware-accelerated CSS, critical-CSS, font-loading strategy, image/responsive tuning.
- **`huashu-design`** — RTL + localized variant exploration before committing markup; anti-AI-slop pass.

Constraint: stay within the Bootstrap 5 **Booking** template assets (including its **RTL** stylesheets in `wwwroot/assets`).

---

## Phase 0 — Grounding (do first)

- [ ] Read `UI-UX-Design.md` §8 (i18n+RTL), §9 (SEO), §18 (UI-PERF rules), §19 (UI-UX rules) and `PERFORMANCE_OPTIMIZATION_GUIDE.md` end-to-end — these are the acceptance bar.
- [ ] Confirm `src/Modules/ContentCore` paths: `GET /content-core/languages`, `GET /content-core/translations/{entityType}/{entityId}`.
- [ ] Confirm `src/Modules/ContentSeo` paths: `GET·PUT /seo/page-metadata`, `GET /seo/faqs`, `GET /seo/sitemap.xml`, `GET /seo/sitemaps/{entityType}.xml`, `GET /seo/weather`.
- [ ] Locate the template's **RTL** assets under `wwwroot/assets` (the `/rtl` builds) and the supported culture list.
- [ ] Baseline current **Lighthouse / Web Vitals** on Home, Tours/Index, Tours/Detail to measure against after the perf pass.

**Acceptance:** documented endpoint list, RTL asset paths, supported cultures, and a perf baseline.

---

## Phase 1 — Localization (i18n) + language switcher

| Endpoint | Use |
|---|---|
| `GET /content-core/languages` | populate the language switcher |
| `GET /content-core/translations/{entityType}/{entityId}` | localized entity text (tour/place/blog names + bodies) |

**Files**
- [ ] `Program.cs` — `AddLocalization` + `UseRequestLocalization` (cookie + `Accept-Language` providers; supported cultures from Phase 0)
- [ ] `Resources/` — `.resx` for shared UI chrome strings (nav, buttons, validation)
- [ ] `Areas/Content/ApiClients/TranslationsApiClient.cs` · `Facades/TranslationsFacade.cs` · `Models/Translations/`
- [ ] `Controllers/CultureController.cs` (Public) — `Set` (`[HttpPost]` culture cookie → PRG back)
- [ ] `Views/Shared/_LanguageSwitcher.cshtml` rendered in `_Navbar`
- [ ] Localize entity rendering on `Tours/Detail`, `Places`, `Blogs/Details` via `TranslationsFacade`

**Acceptance:** switching language persists across requests and localizes both chrome strings and entity content where translations exist.

---

## Phase 2 — RTL support

- [ ] `_Layout.cshtml` — set `<html lang="@culture" dir="@(rtl ? "rtl" : "ltr")">`; conditionally load the template's **RTL** stylesheet for RTL cultures (Arabic etc.).
- [ ] Audit custom CSS for logical-property correctness (margins/padding/floats mirror under RTL); fix any hard-coded `left`/`right`.
- [ ] Verify swiper/carousels, dropdowns, the checkout stepper (Tier A), and chat bubbles (Tier B) mirror correctly.

**Acceptance:** an RTL culture renders a fully mirrored layout with no clipped/overlapping components on Home, Tours, Booking, and Account pages.

---

## Phase 3 — Performance pass (enforce `UI-PERF-*`)

- [ ] Apply `PERFORMANCE_OPTIMIZATION_GUIDE.md` + `UI-UX-Design.md` §18: image `loading="lazy"` + responsive `srcset`, explicit width/height to kill CLS, `MapStaticAssets`/fingerprinted assets, critical-CSS / deferred non-critical CSS+JS, font-display strategy.
- [ ] Confirm **output cache** policies (`Lookups`, `PublicList`) are applied to public list/detail pages and **absent** on per-user pages (Account/Booking/Provider).
- [ ] Trim/duplicate vendor JS; ensure SignalR + Mapbox + typeahead scripts (Tiers B/C) load deferred and only where used.
- [ ] Re-measure against the Phase 0 baseline; record deltas.

**Acceptance:** Core Web Vitals (LCP/CLS/INP) within the guide's budget on the audited pages; measurable improvement vs baseline.

---

## Phase 4 — Accessibility audit (enforce `UI-UX-*`)

- [ ] Apply `UI-UX-Design.md` §19: semantic landmarks, heading order, labelled form controls, focus management for modals (checkout, cancel-refund, start-conversation), visible focus rings, color-contrast AA, `aria-live` for toasts/flash, keyboard-operable carousels/typeahead/map.
- [ ] Verify screen-reader flow on the **booking** path (highest-stakes) and **auth** path.

**Acceptance:** no critical axe violations on Home, Tours/Detail, Booking, Sign-in, Account; keyboard-only completion of a booking is possible.

---

## Phase 5 — SEO / sitemap finalization

| Endpoint | Use |
|---|---|
| `GET·PUT /seo/page-metadata` | per-page title/description/OG tags |
| `GET /seo/sitemap.xml` · `/seo/sitemaps/{entityType}.xml` | sitemaps |
| `GET /seo/faqs` | FAQ structured data / FAQ page |
| `GET /seo/weather` | optional destination weather widget |

**Files**
- [ ] `Areas/Public/ApiClients/SeoApiClient.cs` · `Facades/SeoFacade.cs` · `Models/Seo/`
- [ ] `Views/Shared/_SeoMeta.cshtml` (title/meta/OG/Twitter/canonical + JSON-LD) fed by each page's metadata
- [ ] `Controllers/SitemapController.cs` (Public, `[AllowAnonymous]`) — proxy/emit `sitemap.xml` + per-entity sitemaps; `robots.txt`
- [ ] JSON-LD structured data on `Tours/Detail` (Product/Trip), `Blogs/Details` (Article), `Directory/Detail` (LocalBusiness), `Faq` (FAQPage)
- [ ] (Admin) ensure SEO/FAQ **management** exists — covered by the Provider/Guide/Creator plan **Phase 5**; cross-link, don't duplicate.

**Acceptance:** every public page emits correct meta/OG/canonical + JSON-LD; sitemaps resolve and validate.

---

## Out of scope / blocked

| Item | Reason |
|---|---|
| Admin SEO/FAQ CRUD UI | ✅ Provider/Guide/Creator plan Phase 5 (cross-linked) |
| Machine-translation of entity content | backend concern; we render what `/translations` returns |
| CDN / infra-level caching, edge config | ops, not the Web app |

---

## Sequencing & effort

- **Run after Tiers A–C** so there are real pages to localize/tune/audit.
- **Order:** Phase 0 → 1 (i18n) → 2 (RTL) → 3 (perf) → 4 (a11y) → 5 (SEO). Phases 3–5 can run in parallel by different people.
- **Effort:** i18n+RTL ~1.5 sprints; perf ~1 sprint; a11y ~1 sprint; SEO ~0.5 sprint.

## Open questions

- Supported culture set + default culture (product decision).
- Are `sitemap.xml` / `robots.txt` served by the API (we proxy) or generated by the Web app from entity sitemaps?
- Does `/translations` cover form/validation chrome, or only entity content (chrome stays in `.resx`)?
