# Public Views LTR/RTL Support — Audit & Verification Plan

> Goal: systematically verify (and where needed, fix) bidirectional support across every
> **non-admin** view in `src/Hosts/YallaJo.Web`. The platform is **Jordan-first: the default
> culture is `ar`** (`Program.cs` L187 `SetDefaultCulture("ar")`), so **RTL is the default
> experience** — any RTL defect is a defect on the primary path, not an edge case.
>
> Companion docs: `docs/yallajo-plan/UI-UX-Design.md` (binding RTL1–RTL4 rules),
> `docs/Admin-area-master-plan.md` (the admin area already completed this treatment, P0–P12).

---

## 1. Scope — what "public views" means here

Everything under `src/Hosts/YallaJo.Web` **except** `Areas/Admin` (already audited):

| Bucket | Views | Lines | Layout used |
|---|---:|---:|---|
| `Views/**` (root: navbar, alerts, view components, weather widget) | 12 | 781 | `Views/Shared/_Layout.cshtml` |
| `Areas/Public` (marketplace: tours, places, guides, booking, search) | 38 | 5,674 | public `_Layout` |
| `Areas/Accounts` (traveler account: bookings, payments, reviews, wishlist) | 35 | 3,011 | public `_Layout` |
| `Areas/Auth` (sign-in/up, MFA, recovery) | 15 | 1,107 | **`Areas/Auth/Views/Shared/_AuthLayout.cshtml`** |
| `Areas/Business` | 9 | 1,411 | public `_Layout` |
| `Areas/Guide` | 17 | 3,140 | public `_Layout` |
| `Areas/Provider` | 40 | 5,411 | public `_Layout` |
| `Areas/Creator` | 14 | 1,526 | public `_Layout` |
| **Total** | **180** | **22,061** | 2 layouts |

Both layouts are in scope: `Views/Shared/_Layout.cshtml` (268L) and `_AuthLayout.cshtml`.
`wwwroot/assets/css/rtl.css` (221L, 67 `[dir="rtl"]` selectors) and `site.css` are in scope
additively (never rewrite vendor CSS).

---

## 2. Ground truth (scripted survey, 2026-06-12)

Survey scripts: `C:\Users\admin1\AppData\Local\Temp\opencode\rtl-survey.ps1` / `rtl-survey2.ps1`
(re-runnable; BOM-safe, read-only).

**What already works:**

- ✅ **RTL1 (logical utilities): zero violations.** No `ml-*/mr-*/pl-*/pr-*/text-left/text-right/
  float-left/float-right/border-left/...` anywhere in the 180 views. Logical-utility adoption is
  healthy (Public 189, Provider 256, Guide 116 `ms-/me-/ps-/pe-/text-start/text-end` hits).
- ✅ **Layout wiring exists:** public `_Layout` L20–24 computes
  `seoDir = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? "rtl" : "ltr"` and renders
  `<html lang="@..." dir="@seoDir">`; rtl.css is loaded inside `@if (seoDir == "rtl")` (L85–87).
  `_AuthLayout` L16/L48 has equivalent `dir="@authDir"` + rtl.css wiring.
- ✅ **Localization pipeline:** `ar`+`en` supported cultures, `UseRequestLocalization`
  (query → cookie → Accept-Language), `AddViewLocalization` + `AddDataAnnotationsLocalization`.

**Gaps found (the audit targets):**

| # | Gap | Evidence | Rule |
|---|---|---|---|
| G1 | **`<bdi>`/`.font-data` coverage very uneven.** Numbers, prices, ratings, dates rendered without bidi isolation will reorder/garble in RTL. | Areas/Public: 39 numeric-format sites vs **1 `<bdi>`, 0 `.font-data`**. Areas/Accounts: 26 sites vs 2 `<bdi>`. Views/**: 3 sites, 0 `<bdi>`. (Contrast: Provider 129 `<bdi>`, Guide 74 — those areas were treated previously.) | RTL3 |
| G2 | **Inline `style=` attributes** — may carry physical `left/right/margin/padding` values invisible to utility-class grep. | Areas/Guide ×26 (Dashboard ×8, Tier ×6, Analytics ×4…), Auth ×2, Provider ×2, Accounts ×1, Public ×1 | RTL1/X-family |
| G3 | **Runtime RTL behavior never verified** for public pages (mirroring of carousels/sliders/offcanvas/dropdowns, chevron icons, truncation/ellipsis side, toast placement, form validation message alignment, `<input type="tel/email">` direction). | No prior AR walkthrough of public surface | RTL2/RTL4 |
| G4 | **rtl.css override completeness unknown** for public-only components (hero search, range sliders, star ratings, image carousels, navbar mega-menu, weather widget). 67 selectors exist but were authored before recent features. | rtl.css 221L | RTL2 |
| G5 | **Mixed-direction text risk** in user-generated content (review text, place names, guide bios shown in opposite-direction UI) — needs `dir="auto"`/`<bdi>` policy check. | Reviews/_Reviews, cards, detail pages | RTL3/RTL4 |

---

## 3. Verification matrix (what "supports LTR+RTL" means)

Each page must pass all rows in **both** `?culture=en` (LTR) and `?culture=ar` (RTL), light + dark,
375px + 1280px:

| Check | How |
|---|---|
| V1 `<html dir>` + `lang` correct; rtl.css present only in RTL | DOM inspect |
| V2 No horizontal overflow / clipped content | viewport scan |
| V3 Layout mirrors: nav, breadcrumbs, cards, sidebars, dropdown alignment, offcanvas slide side | visual |
| V4 Directional icons (chevrons, arrows, "back") point logically, not physically | visual |
| V5 Numbers/prices/dates/IDs render LTR inside RTL text, no reordering (`<bdi>`/`dir="ltr"` + `.font-data`) | visual + DOM |
| V6 Forms: labels/inputs/validation aligned to reading direction; email/phone/URL inputs stay LTR | visual |
| V7 Truncation/ellipsis on the logical end | visual |
| V8 Toasts, modals, confirm dialogs positioned/aligned correctly | interactive |
| V9 Mixed-direction UGC isolated (Arabic review in EN UI and vice versa) | seeded data |
| V10 Culture switcher round-trips and persists (cookie) | interactive |

---

## 4. Phases

### Phase A — Static audit sweep (0.5d) `[no behavior change]`

Scripted, reusing the proven BOM-safe recipes (`bdi-scan.ps1` pattern):

1. **A1 — bdi/font-data gap inventory:** grep every `ToString("N|C|P|0`, `@Money(`, `@Count(`,
   `@Rating(`, raw `@Model.Price`-style expressions and date formats across the 8 buckets;
   exclude already-wrapped lines. Output: per-file fix list (expect ~70–90 real sites; the known
   hotspots: `Public/Places/Details` ×11, `Accounts/Reviews/_MyReviewsTab` ×12,
   `Accounts/Bookings/Detail` ×6, `Public/Directory/Detail` ×5, `Public/Tours/Detail` ×4,
   all `_*Results`/`_*Card` partials ×1–2 each).
2. **A2 — inline style triage:** dump the 32 `style=` occurrences with context; classify
   *(a)* physical-direction values (must fix), *(b)* dimension-only (`width/height/background-image`
   — allowed, leave), *(c)* movable to utilities (opportunistic).
3. **A3 — `dir` attribute audit:** find email/phone/URL/code inputs and UGC text nodes lacking
   `dir="ltr"`/`dir="auto"`; verify the 4 root view components (NotificationBell, NavbarAvatar,
   WeatherWidget, AdminNav) render bidi-safe content.
4. **A4 — rtl.css coverage diff:** list public components with positional CSS in `site.css`/inline
   (absolute `left/right`, `transform: translateX`, `border-radius` corner pairs, carousel controls)
   and check each has an `[dir="rtl"]` counterpart or uses logical properties.
5. **A5 — resx parity spot-check:** confirm no public view hard-codes English UI strings
   (grep quoted literals in markup; CON1).

**Deliverable:** `docs/Public-rtl-audit-findings.md` — table of every defect with file:line, rule ID,
severity. **Gate:** findings doc reviewed; no code changed yet.

### Phase B — Code fixes from static audit (1–1.5d)

1. **B1 — `<bdi>`/`.font-data` sweep** over the Phase A1 list (mirror of admin P0 bdi sweep):
   wrap culture-formatted numerals as `<bdi dir="ltr" class="font-data">…</bdi>`; prices/dates in
   cards, detail pages, results partials. Partials fix once, benefit all consumers.
2. **B2 — inline-style remediation** for class-(a) hits only (convert to logical utilities or
   move to `site.css` with `[dir="rtl"]` override if needed).
3. **B3 — `dir` attributes:** `dir="ltr"` on email/phone/URL/code inputs and technical IDs;
   `dir="auto"` (or `<bdi>`) on UGC titles/review bodies.
4. **B4 — rtl.css additive overrides** for any A4 gaps (append-only; comment each with the
   component it serves).

**Gate:** `dotnet build src/Hosts/YallaJo.Web/YallaJo.Web.csproj --no-incremental` — exactly
**71 warnings / 0 errors** (zero new). One conventional commit per logical unit, e.g.
`fix(public): bdi and font-data sweep across marketplace and accounts [RTL3]`,
`fix(guide): replace directional inline styles with logical utilities [RTL1 X13]`.

### Phase C — Runtime visual verification (1d) `[requires running app]`

Run the site (`dotnet run` on YallaJo.Web) and drive it with the Playwright MCP tools:

1. **Page matrix** (each × `?culture=en` / `?culture=ar` × light/dark × 375/1280):
   Home, Tours index + detail, Places details, Directory detail, Guides detail, Booking Book +
   Confirmation, Search results (AJAX swap), Help; Accounts Overview/Bookings/Payments/Reviews/
   Wishlist; Auth sign-in/sign-up/MFA; one representative page each from Business/Guide/Provider/
   Creator dashboards.
2. Per page: assert `html[dir]`, screenshot pair (en/ar), check V2–V8 from the matrix, capture
   console errors, exercise one interactive flow in AR (open modal, submit invalid form, toast).
3. **V9 seeding:** create one Arabic-text review viewed under EN, one English review under AR.
4. **V10:** toggle culture switcher; verify cookie persistence + correct `dir` flip without
   layout breakage on return navigation.

**Deliverable:** screenshot archive + pass/fail per matrix cell appended to the findings doc.
Defects found here loop back through Phase B (small follow-up commits).

### Phase D — Regression guard (0.5d, optional but recommended)

1. Promote the audit greps into `tools/rtl-lint.ps1` committed to the repo (fails on: physical
   directional utilities, new unwrapped `ToString("N|C` in views, `style=` containing
   `left:`/`right:`/`margin-left`/`padding-right`, missing `dir` on `type="email|tel|url"`).
2. Document the EN+AR walkthrough as the standing merge gate in `docs/yallajo-plan/UI-UX-Design.md`
   §RTL (already policy — add the script reference).

---

## 5. Execution order & estimates

| Phase | Effort | Depends on |
|---|---|---|
| A static audit | 0.5d | — |
| B code fixes | 1–1.5d | A |
| C runtime verification | 1d | B, running app |
| D regression guard | 0.5d | A (script exists as by-product) |

Total ≈ 3–3.5 days. Phases commit independently; B may split into per-area commits
(Public → Accounts → root Views → Guide inline styles → Auth/Business leftovers).

## 6. Guardrails

- Touch only non-admin views, `rtl.css`/`site.css` (additive), and both resx files. Never vendor CSS.
- All shells: `lean-ctx_ctx_shell` for git/dotnet; audit scripts as `.ps1` files (ASCII-only).
- resx edits: both files, grep-first, `Edit` tool only (UTF-8); en 2-space-indented, ar flush-left.
- Build gate after every fix batch: Web **71w/0e exactly**.
- Severity triage: anything broken in **AR (default culture)** is P0; LTR-only issues are P1.
