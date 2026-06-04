# Marketing & Static Pages — Build Plan (Tier E)

> **Scope:** the lower-priority **marketing, informational, legal, and utility** pages from the template that round out the public site — About, Pricing, Offers, Compare, plus Coming-Soon / Error / legal pages.
> **Companion docs:** `UI-UX-Design.md` §2.1 (public/marketing inventory), §9 (SEO), Tier-D `CrossCutting-…-Plan.md` (SEO meta).
> **Authoring rules:** `CONTROLLER_AUTHORING_GUIDE.md` (authoritative).
> **API base path:** `/api/v1`
> **Status:** 📋 Planned — not started (lowest tier)

---

## Why this plan

These are the last template pages with no controller. They are **mostly static or near-static** content (few or no endpoints) and exist for trust, conversion, and legal compliance rather than core function. They're cheap to build but **deliberately deprioritized** behind the transactional and discovery work (Tiers A–C). A couple (Pricing, Offers, Compare) have a thin data dependency; the rest are content-only.

---

## Architecture conventions (read before building)

Layered-by-type per `CONTROLLER_AUTHORING_GUIDE.md`, but most pages are **content-only** so the slice is minimal (a controller action + a view; no ApiClient/Facade unless data-backed):

- Group the static pages under a single **`Public/PagesController`** (`[AllowAnonymous]`) with one action per page, or co-locate on the existing `Home` controller — decide in Phase 0. Data-backed pages (Pricing, Offers, Compare) get their own controller + Facade + ApiClient.
- **Caching:** these are public + shared + rarely-changing → apply the **`PublicList`** / a long-TTL output-cache policy.
- **SEO:** every page renders the shared `_SeoMeta` partial (from Tier D) — set title/description/OG per page.
- **Error/utility pages** wire into `Program.cs` (`UseStatusCodePages` / exception handler) → `error.html`, plus a maintenance/`coming-soon` route.
- Build csproj **alone** (`CS2012`).

### Design & UI skills (mandatory for all views)

- **`ui-ux-pro-max`** + **`impeccable`** — marketing hierarchy, conversion CTAs, pricing-table clarity, trust/legal readability, friendly error/empty states.
- **`design-taste-frontend`** — component architecture + hardware-accelerated CSS for hero/animation sections.
- **`huashu-design`** — hi-fi exploration of the marketing/landing variants; anti-AI-slop pass.

Constraint: stay within the Bootstrap 5 **Booking** template assets (`wwwroot/assets`).

---

## Template → page wiring (master map)

| Page | Area / route | Template source | Data dependency |
|---|---|---|---|
| About | `Public/Pages/About` | `about.html` | none (static) |
| Pricing | `Public/Pages/Pricing` | `pricing.html` | ⚠️ plans/tiers source — confirm in P0 (likely static or a plans endpoint) |
| Offers / promotions | `Public/Pages/Offers` · `Offers/{id}` | `offer-detail.html` | ⚠️ promotions endpoint if it exists, else static/CMS |
| Compare tours | `Public/Compare` | `compare-listing.html` | `GET /tours/{id}` per compared tour |
| Team (→ Guides) | covered elsewhere | `team.html` | ✅ Provider/Guide/Creator plan (creator profile) — cross-link |
| Coming Soon | `Public/Pages/ComingSoon` | `coming-soon.html` | none |
| Error | status-code/exception route | `error.html` | none |
| Privacy / Terms | `Public/Pages/Privacy` · `Terms` | `privacy-policy.html` · `terms-of-service.html` | none (legal content) |
| Help / FAQ | covered elsewhere | `help-center.html` · `faq.html` | ✅ already in `Public/Help` + Content `Faq` |

---

## Phase 0 — Grounding (do first)

- [ ] Decide grouping: a single `Public/PagesController` for content-only pages vs per-page actions on `Home`.
- [ ] Confirm whether **Pricing** plans and **Offers/promotions** come from an API endpoint or are static content (check `ContentSeo`/`ContentCore`/a promotions module; if none, treat as static/CMS-managed copy).
- [ ] Confirm `GET /tours/{id}` (and `/tours/slug/{slug}`) shape for the **Compare** page (reuse existing `Tours` models).
- [ ] Ensure the Tier-D `_SeoMeta` partial exists (or stub it) so these pages get proper metadata.
- [ ] Gather final legal copy (Privacy/Terms) from the product owner.

**Acceptance:** grouping decision + confirmed data sources for Pricing/Offers/Compare + legal copy in hand.

---

## Phase 1 — Static content pages (no data)

**Files** (layered — under `Areas/Public/`)
- [ ] `Controllers/PagesController.cs` (`[AllowAnonymous]`) — `About`, `Privacy`, `Terms`, `ComingSoon`
- [ ] `Views/Pages/{About,Privacy,Terms,ComingSoon}.cshtml` ← `about.html` / `privacy-policy.html` / `terms-of-service.html` / `coming-soon.html`
- [ ] Each renders `_SeoMeta`; apply long-TTL output cache

**Acceptance:** all four pages render with correct nav/footer, SEO meta, and are cached.

---

## Phase 2 — Error & utility wiring

**Files**
- [ ] `Program.cs` — `UseStatusCodePagesWithReExecute("/error/{0}")` + exception handler → friendly page
- [ ] `Controllers/ErrorController.cs` (`[AllowAnonymous]`) — `Index`/status actions
- [ ] `Views/Error/Index.cshtml` ← `error.html` (404/500 variants)

**Acceptance:** unhandled errors and 404s render the styled error page (not a raw stack/blank), with a path back to Home.

---

## Phase 3 — Pricing & Offers (thin-data)

| Endpoint *(confirm in P0)* | Use |
|---|---|
| plans/pricing source | render pricing tiers (or static if none) |
| promotions/offers source | offers list + `offer-detail` |

**Files** (layered — under `Areas/Public/`)
- [ ] `Controllers/PagesController.cs` — add `Pricing`, `Offers`, `OfferDetail`
- [ ] *(if data-backed)* `ApiClients/OffersApiClient.cs` · `Facades/OffersFacade.cs` · `Models/Offers/`
- [ ] `Views/Pages/Pricing.cshtml` ← `pricing.html`; `Views/Pages/Offers.cshtml` + `OfferDetail.cshtml` ← `offer-detail.html`

**Acceptance:** pricing table renders; offers list links to a detail page (or pages are clearly static if no endpoint exists).

---

## Phase 4 — Compare tours

| Endpoint | Use |
|---|---|
| `GET /tours/{id}` / `GET /tours/slug/{slug}` | fetch each tour in the comparison set |

**Files** (layered — under `Areas/Public/`)
- [ ] `Controllers/CompareController.cs` — `Index` (ids via query/cookie), `Add`/`Remove`
- [ ] `Facades/CompareFacade.cs` (reuse `Tours` ApiClient) · `Models/Compare/CompareVm`
- [ ] `Views/Compare/Index.cshtml` ← `compare-listing.html`; a "Compare" toggle on tour cards (cookie-backed set)

**Acceptance:** a user adds 2–3 tours and sees a side-by-side comparison table.

---

## Out of scope / blocked

| Item | Reason |
|---|---|
| `hotel-*`, `flight-*`, `cab-*`, `room-detail.html`, `index-resort/-hotel-chain/-flight/-cab.html` | ⛔ not part of the tour-centric API |
| `team.html` as a standalone | ✅ repurposed as **creator/guide profile** in the Provider/Guide/Creator plan |
| `help-center.html` / `faq.html` | ✅ already built (`Public/Help`, Content `Faq`) |
| Blog/marketing CMS authoring | ✅ creator composer (Provider/Guide/Creator plan Phase 3) |

---

## Sequencing & effort

- **Lowest priority — schedule after Tiers A–D**, or slot single pages in as filler between larger features.
- **Order:** Phase 0 → 1 (static) → 2 (error) → 3 (pricing/offers) → 4 (compare).
- **Effort:** Phases 1–2 ~0.5 sprint total; Phase 3 ~0.5 sprint; Phase 4 ~0.5 sprint.

## Open questions

- Are Pricing plans and Offers API-backed or static copy? (Determines whether Phase 3 needs a Facade/ApiClient.)
- Is `compare-listing` worth building for a tour product, or cut as hotel-template residue? (Confirm product intent.)
- Single `PagesController` vs per-page actions on `Home`.
