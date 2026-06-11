# Public Views — LTR/RTL Audit Findings (Phase A)

> Output of Phase A of `docs/Public-rtl-audit-plan.md`. Audited 2026-06-12 against
> 190 non-admin views (22k lines) across `Views/**` and Areas `Public, Accounts,
> Auth, Business, Guide, Provider, Creator`. Raw scan output:
> `rtl-phaseA-results.txt` (re-runnable via `rtl-phaseA.ps1`).
>
> Severity: **P0** = broken/in-English in the *default Arabic* experience ·
> **P1** = correctness gap, low blast radius · **P2** = cosmetic/deferred.
> Fix class: **B1** bdi/font-data wrap · **B2** localize string · **B3** dir attr.

## 1. Verdicts by audit lane

| Lane | Result |
| --- | --- |
| A1 numeric/date sites | 160 raw hits → **~20 real fixes** (see §2); rest excluded by rationale in §4 |
| A2 inline styles | 31 hits, **all dimension-only** (widths/heights/progress bars/bg-image). Zero physical-direction values. **No fixes needed.** |
| A3 `dir` attributes | **1 gap**: `Areas/Public/Views/Contact/Index.cshtml:86` email input lacks `dir="ltr"` |
| A4 rtl.css / site.css | No blocking gaps; 3 notes in §5 |
| A5 hardcoded English | 17 raw hits → **8 real fixes** (§3), rest brand/technical (§4) |
| RTL1 physical utilities | **Zero violations** in all 180+ views (verified in pre-plan survey) |

## 2. B1 — composite numeric renders to wrap in `<bdi dir="ltr" class="font-data">`

| # | File:Line | What | Sev |
| --- | --- | --- | --- |
| 1 | Areas/Public/Views/Booking/Book.cshtml:226 | `@Money(EffectivePrice)` | P1 |
| 2 | Booking/Book.cshtml:232 | strikethrough `@Money(BasePrice)` | P1 |
| 3 | Booking/Book.cshtml:241 | `@Money(EstimatedTotal)` (span w/ data-currency) | P1 |
| 4 | Booking/Confirmation.cshtml:87 | `@Money(UnitPrice * Count)` | P1 |
| 5 | Booking/Confirmation.cshtml:93 | `@Money(Subtotal)` | P1 |
| 6 | Booking/Confirmation.cshtml:99 | `-@Money(DiscountAmount)` — wrap **including minus** | **P0** |
| 7 | Booking/Confirmation.cshtml:105 | `-@Money(LoyaltyAmount)` — wrap **including minus** | **P0** |
| 8 | Booking/Confirmation.cshtml:109 | `@Money(TotalAmount)` | P1 |
| 9 | Directory/Detail.cshtml:189 | `@Money(s.Price)` | P1 |
| 10 | Directory/Detail.cshtml:238 | temperature `…ToString("0")&deg;C` | P1 |
| 11 | Places/Details.cshtml:150 | temperature `…&deg;C` | P1 |
| 12 | Places/Details.cshtml:337 | composite price ternary | P1 |
| 13 | Places/Details.cshtml:369 | `rating (@business.ReviewCount)` parens composite | P1 |
| 14 | Places/Details.cshtml:402 | composite price ternary | P1 |
| 15 | Packages/Detail.cshtml:34 | `@Money(PriceAmount)` | P1 |
| 16 | Shared/_PackagesResults.cshtml:23 | `@Money(package.PriceAmount)` | P1 |
| 17 | Guides/Detail.cshtml:133 | `"0.0" / 5` composite | P2 |

Leading-minus money (rows 6–7) is the highest-risk class: the sign can detach
from the amount in an RTL paragraph.

## 3. B2/B3 — hardcoded English & dir gaps

| # | File:Line | Issue | Fix | Sev |
| --- | --- | --- | --- | --- |
| 1 | Areas/Public/Views/Places/Details.cshtml:164 | `Feels like X°C` raw English | reuse `Public.FeelsLike` (pattern: Directory/Detail:248) | **P0** |
| 2 | Places/Details.cshtml:172 | `X km/h` raw English | reuse `Public.KmPerHour` (pattern: Directory/Detail:256) | **P0** |
| 3 | Areas/Guide/Views/Reviews/Index.cshtml:100 | `Visited <date>` raw English | reuse `Public.Visited` | **P0** |
| 4 | Views/Shared/Components/NotificationBell/Default.cshtml:73 | `title="Mark as read"` | localize (grep for existing key first) | **P0** |
| 5 | Views/Shared/Components/WeatherWidget/Default.cshtml:4 | `aria-label="Local weather"` | localize | P1 |
| 6 | Views/Shared/_JoinRequestModal.cshtml:8 | `aria-label="Close"` | reuse `Public.Close` | P1 |
| 7 | _JoinRequestModal.cshtml:17 | GUID example placeholder, English | localize/neutralize + ensure `dir="ltr"` on input | P1 |
| 8 | Areas/Accounts/Views/Settings/_PrivacyTab.cshtml:79 | typed-confirm token `placeholder="DELETE"` hardcoded | full localized-token treatment (resx key + placeholder + instruction + server compare + JS gate), same as admin archive-token fix | **P0** |
| 9 | Areas/Public/Views/Contact/Index.cshtml:86 | email input lacks `dir` | add `dir="ltr"` (B3) | P1 |

## 4. Deliberate exclusions (do not "fix")

- **Helper definitions** (`string Money(...)` etc. at file tops) are not render
  sites. All Business/Guide/Provider/Creator A1 hits are definitions or sit in
  already-`<bdi>`-treated areas (Provider 129 / Guide 74 / Business 20 /
  Creator 17 existing wraps).
- **Culture-formatted dates** (`ToString("dd MMM yyyy")` etc.): under the `ar`
  culture these render native Arabic month names — no bidi risk. Skipped
  (Book:118 legend, _InvoicesTab:60, _PaymentsTab:59/62, _MyReviewsTab dates…).
- **Pure decimal ratings** (`"0.0"`): neutral digits only, no reorder risk
  (admin precedent). Skipped: _BusinessCard:61, _GuidesResults:37,
  _PlaceCircleCard:52, _PlacesResults:113, _Reviews:38, _TourCard:56,
  _StarRating:22, Tours/Detail:88/409/510, Guides/Detail:45,
  Places/Details:45/331/399, Directory/Detail:68, Book:98, Wishlist:100,
  _OverviewPicks:32, Guide Reviews:83/117.
- **Localizer-composed values** (`Public.FeelsLike`, `Public.KmPerHour`,
  `Public.Visited`, `Public.StarsPlus`… with format args): translator controls
  placement. Skipped.
- **Form plumbing**: `value="yyyy-MM-dd"` date inputs, number-input values.
- **Brand/technical strings**: `alt="YallaJo"`, Auth `alt="logo"` ×5 (P2),
  Home `alt="Tour landscape"/"About YallaJo"` (P2), `placeholder="fa-wifi"`,
  `placeholder="my-tour-slug"` technical examples.
- **Inline styles**: all 31 dimension-only (progress-bar widths, avatar sizes,
  max-width truncation, bg-image). RTL-neutral.

## 5. CSS notes (A4)

1. **rtl.css sections 3 and 7 are literal duplicates** (same 4 navbar rule
   blocks). Harmless; left in place (additive-only guardrail). Note for a
   future cleanup pass.
2. **`.offcanvas-end` is not mirrored in RTL** — desktop filter panel slides
   from the physical right in Arabic. Not broken, arguably acceptable UX;
   flagged for Phase C runtime judgment, not a blind CSS change. The mobile
   bottom-sheet variant (site.css L217-224, symmetric left:0+right:0) is
   unaffected.
3. **rtl.css §6 design**: email/url/tel/.font-data inputs keep `dir="ltr"`
   markup (character order) but `text-align: right` (visual alignment with the
   RTL form). Deliberate; verify visually in Phase C.
4. `style.css` is a 21k-line compiled LTR theme (vendor-equivalent). The
   architecture is *LTR build + rtl.css overrides* — per guardrails it is not
   edited; gaps are closed in rtl.css only.

## 6. Phase C runtime checklist additions

Beyond the standard V1–V10 matrix in the plan:
- Verify rtl.css §6 input alignment feels right in AR (email field in Contact,
  Auth sign-in).
- Judge `.offcanvas-end` placement in AR on Tours/Places index (desktop).
- Confirm weather strings render Arabic after the B2 fixes (Places/Details).
- Confirm minus-money rows on Booking/Confirmation render `-45 JOD` as one run.
- NotificationBell tooltip + WeatherWidget SR label in AR.
- Accounts → Settings → Privacy typed-confirm flow end-to-end in AR.
