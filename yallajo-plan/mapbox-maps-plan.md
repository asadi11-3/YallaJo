# Mapbox Maps Rollout Plan (V13)

Add Mapbox GL JS (free tier) maps to every page with real coordinate data, in the
YallaJo brand style, fully LTR/RTL aware, with graceful degradation when no token
is configured. Web layer is the primary surface; API layer is touched only where
a DTO must expose data that already exists in the DB.

> **Free tier**: Mapbox GL JS needs a public access token (`pk.*`) — free plan =
> 50,000 map loads/month. Maps lazy-initialize only when scrolled into view, so
> real usage stays far below the cap. **Until a token is set in
> `appsettings.json → Mapbox:AccessToken`, maps silently don't render** and all
> pages keep their current text-only location fallback.

## Weather widget check (asked in the task)

**Place Details already has a weather widget** (`Areas/Public/Views/Places/Details.cshtml`
L142–177: condition, temp, feels-like, humidity, wind, stale label), and Business
Detail has a similar card. Stored in DB but **not displayed**: weather `Icon`,
`UvIndex`, and a 3-day forecast (`WeatherCache.ForecastJson`). This plan surfaces
icon + UV immediately (web-only) and the 3-day forecast in Phase C (API DTO add).
Note: the weather provider is `"none"` in prod config (`"weatherapi"` in dev) —
the widget only renders when the provider is enabled.

## Phase 0 — Foundation (shared, one-time)

| # | Change | Files |
|---|--------|-------|
| 0.1 | Config section `"Mapbox": { "AccessToken": "" }` | `YallaJo.Web/appsettings.json` |
| 0.2 | `MapboxOptions` + `IMapboxScriptService` (mirrors the Recaptcha options→service→`@inject` pattern; views never read raw config) | new `Infrastructure/Maps/`, register in `Program.cs` |
| 0.3 | Self-host vendor files: `mapbox-gl.js`, `mapbox-gl.css`, `mapbox-gl-rtl-text.js` | new `wwwroot/assets/vendor/mapbox-gl/` |
| 0.4 | CSP in `SecurityHeadersMiddleware`: `worker-src 'self' blob:`, `child-src blob:`, `blob:` in img-src, Mapbox API origins in connect-src. Keep `Permissions-Policy: geolocation=()` (static pins only) | 1 file |
| 0.5 | `yj-map.js`: scans `[data-yj-map]`, IntersectionObserver lazy-boot, lazy-loads vendor css/js on demand. Modes: `pin`, `pins` (fitBounds + popups), `route` (numbered stops + meeting point + dashed line), `picker` (drag/click ⇄ lat/lng inputs two-way sync). Brand red marker, Montserrat popups, `cooperativeGestures` | new `wwwroot/assets/js/yj-map.js` |
| 0.6 | Shared partial `Views/Shared/_Map.cshtml` + `MapVm` — emits one `div[data-yj-map]` with invariant-culture data attrs; renders nothing when disabled | new partial + VM |
| 0.7 | RTL/LTR: `setRTLTextPlugin` (self-hosted), Arabic tile labels via `name_ar` coalesce when `lang=ar`, nav controls flipped to the start side in RTL, logical CSS properties; dark mode → `dark-v11` style | yj-map.js + site.css |
| 0.8 | `.yj-map` styles (rounded, brand shadow, height variants, popup/stop-bubble skins) + resx keys EN/AR | site.css, both `SharedResource.*.resx` |

## Phase A — Ready now (coords already in the detail VMs)

1. **Place Details** — single-pin map inside the Location card + "Open in Google
   Maps" link. **Weather upgrade**: show the stored icon + UV index.
2. **Business Detail** (`Directory/Detail.cshtml`) — single-pin map in the
   Contact & info card, guarded on `Latitude.HasValue`.

## Phase B — Web-layer plumbing only (API already returns coords; facades drop them)

3. **Tour Detail** — add `Latitude/Longitude/MeetingPoint*` to `TourDetailVm`,
   lat/lng to `TourWaypointVm`, map them in `ToursFacade.GetDetailAsync` →
   itinerary **route map** (numbered stops, green meeting-point pin).
4. **Directory Index** — add `Lat/Lng` to `BusinessCardVm` (web
   `BusinessSummaryResponse` already has them) → multi-pin results map.
5. **Places Index** — API `PlaceSummaryDto` already has coords; add them to web
   `PlaceSummaryResponse` + `PlaceCardVm` → destinations overview map.
6. **Search results** — `SearchVm` already carries coords for places +
   businesses → combined pins map.

## Phase C — API-layer additions (optional value-adds)

7. **WeatherDto + forecast**: add `IReadOnlyList<DailyForecastDto>` (data already
   in `WeatherCache.ForecastJson`) → 3-day forecast chips on Place Details.
8. **TourSummaryDto lat/lng** → map toggle on Tours Index / search.
9. Leverage existing `GET /places/nearby`, `/places/businesses/nearby`,
   `/places/map/viewport` endpoints for "nearby on map" rails (stretch).

## Phase D — Back-office "pick on map" pickers (`picker` mode)

Business **Register/Manage**, Admin **Places `_Form`**, Provider **Tours `_Form`**
(location + meeting point), Provider **TourWaypoints `_Form`**, Admin **SeoWeather**.
Each picker writes `lat/lng` into the existing inputs (6-dp invariant) and follows
manual input edits back onto the map.

## Verification

- `dotnet build src\Hosts\YallaJo.Web` (+ API build when Phase C touches DTOs).
- Manual: EN/AR (RTL labels + control side), dark mode, token-removed fallback,
  no CSP violations in console, AJAX filter swaps keep maps alive.

## Risks

- Token missing → maps absent by design (no broken UI).
- ~500 KB vendor JS → loaded lazily, only on pages with a map in view.
- Mapbox telemetry (`events.mapbox.com`) is part of the GL JS ToS — allowed in CSP.
