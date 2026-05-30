# Playwright MCP — API-Only Adapter

> **Authoritative pattern guide for running every Playwright-*.md scenario in this folder against an API-only deployment.**
>
> Status of this checkout (confirmed): **YallaJo.Web does not exist / is not running.** Only the API runs at `https://localhost:57065`. Every Razor-page or Web-URL step in the per-module test docs must be translated through the helpers below.

---

## 0. Universal preflight (run once per Playwright session)

```js
// browser_navigate to any valid page so we have a browsing context
await page.goto("https://localhost:57065/swagger");

// browser_evaluate — install the global apiFetch helper
await page.evaluate(() => {
  const API = "https://localhost:57065";
  window.__yj = window.__yj || {
    api: API,
    token: null,
    refreshToken: null,
    me: null,
    /**
     * Universal API call. Returns { status, headers, body } regardless of HTTP result.
     * Never throws on non-2xx — caller asserts status.
     */
    async apiFetch(method, path, body, extraHeaders) {
      const headers = { "Content-Type": "application/json", "Accept": "application/json" };
      if (this.token) headers["Authorization"] = `Bearer ${this.token}`;
      Object.assign(headers, extraHeaders || {});
      const init = { method, headers, credentials: "omit" };
      if (body !== undefined && body !== null) init.body = JSON.stringify(body);
      const res = await fetch(`${this.api}${path}`, init);
      const text = await res.text();
      let parsed = null;
      try { parsed = text ? JSON.parse(text) : null; } catch { parsed = text; }
      const hdrs = {};
      res.headers.forEach((v, k) => { hdrs[k] = v; });
      return { status: res.status, headers: hdrs, body: parsed };
    },
    /**
     * Multipart upload (documents, images). `files` is an array of { field, name, type, base64 }.
     */
    async apiUpload(method, path, fields, files) {
      const fd = new FormData();
      for (const [k, v] of Object.entries(fields || {})) fd.append(k, v);
      for (const f of files || []) {
        const bin = Uint8Array.from(atob(f.base64), c => c.charCodeAt(0));
        fd.append(f.field, new Blob([bin], { type: f.type || "application/octet-stream" }), f.name);
      }
      const headers = {};
      if (this.token) headers["Authorization"] = `Bearer ${this.token}`;
      const res = await fetch(`${this.api}${path}`, { method, headers, body: fd, credentials: "omit" });
      const text = await res.text();
      let parsed = null;
      try { parsed = text ? JSON.parse(text) : null; } catch { parsed = text; }
      return { status: res.status, body: parsed };
    },
    /** Decode the JWT payload (no signature verification). */
    decodeJwt(token) {
      try {
        const [, payload] = token.split(".");
        return JSON.parse(atob(payload.replace(/-/g, "+").replace(/_/g, "/")));
      } catch { return null; }
    },
    /** Clear the in-memory token. Used for negative-auth tests. */
    logout() { this.token = null; this.refreshToken = null; this.me = null; }
  };
});
```

After this block runs, every subsequent `browser_evaluate` call can use `window.__yj.apiFetch(...)`.

---

## 1. Login → token capture (replaces every Web-UI login step)

```js
// browser_evaluate
const out = await window.__yj.apiFetch("POST", "/api/v1/auth/login", {
  email: "userA@yallajo.test",
  password: "TestPass!23",
  deviceId: "playwright-1",
  rememberMe: false
});
if (out.status !== 200) throw new Error(`Login failed: ${out.status} ${JSON.stringify(out.body)}`);
window.__yj.token = out.body.accessToken || out.body.token;
window.__yj.refreshToken = out.body.refreshToken;
window.__yj.me = window.__yj.decodeJwt(window.__yj.token);
return { status: out.status, sub: window.__yj.me?.sub, roles: window.__yj.me?.role };
```

**8 seeded users** (all password `TestPass!23`):

| Email | Role | Provider | State | Use for |
|---|---|---|---|---|
| `admin@yallajo.test` | Admin | — | Active | Admin actions (approve, suspend, dispute resolution) |
| `userA@yallajo.test` | User | — | Active | Traveler — booking, review, wishlist |
| `userB@yallajo.test` | User | — | Active | Second traveler for conflict / concurrency tests |
| `guide-pending@yallajo.test` | TourGuide | IndependentGuide | App=Pending | Onboarding tests (Approve / Reject / RequestMoreDocs) |
| `guide-approved@yallajo.test` | TourGuide | IndependentGuide | App=Approved | Tour CRUD, slots, earnings, payouts |
| `business@yallajo.test` | TourGuide | BusinessOwner | App=Approved | Business listings, services |
| `agency@yallajo.test` | TourGuide | Agency | App=Approved | Agency split-payouts, guide affiliations |
| `suspended@yallajo.test` | User | — | Account=Suspended | Negative auth (login should be 403) |

---

## 2. Pattern translation cheat-sheet

Every "navigate to Razor page" / "fill form" / "click submit" pattern maps to a single `apiFetch` (or two, when the original step also asserts a redirect).

| Old (Web-UI) | New (API-only) |
|---|---|
| `browser_navigate("https://localhost:57070/")` | `browser_navigate("https://localhost:57065/swagger")` (just a docking URL) |
| `browser_navigate("https://localhost:57070/Auth/Login")` + fill form + submit | `apiFetch("POST", "/api/v1/auth/login", {...})` (Section 1) |
| `browser_navigate("https://localhost:57070/Auth/Register")` + fill + submit | `apiFetch("POST", "/api/v1/auth/register", {...})` |
| `browser_navigate("https://localhost:57070/Auth/ForgotPassword")` + email + submit | `apiFetch("POST", "/api/v1/auth/forgot-password", { email })` |
| `browser_navigate("https://localhost:57070/Auth/VerifyEmail?token=...")` | `apiFetch("POST", "/api/v1/auth/verify-email", { token, otp })` |
| `browser_navigate("https://localhost:57070/Account/Profile")` + assert fields | `apiFetch("GET", "/api/v1/accounts/profiles/me")` then assert body shape |
| `browser_navigate("https://localhost:57070/TourGuide/Dashboard")` | n/a — assert via several `apiFetch` calls: `/api/v1/tour-guide/tours`, `/api/v1/finance/earnings`, etc. |
| `browser_navigate("https://localhost:57070/Tours/{id}")` + assert title | `apiFetch("GET", "/api/v1/content-tours/{id}")` |
| `browser_navigate("https://localhost:57070/Booking/Create?tourId=...")` + fill + submit | `apiFetch("POST", "/api/v1/bookings", { tourId, slotId, ... })` |
| `browser_navigate("https://localhost:57070/Booking/{id}/Pay")` | `apiFetch("POST", "/api/v1/bookings/{id}/pay", { paymentMethodId })` (if endpoint not yet built → tag NOT_BUILT) |
| Antiforgery / cookie assertions | Drop (API uses Bearer JWT — no antiforgery, no cookie auth) |
| WebHook simulation | `apiFetch("POST", "/api/v1/payments/webhook", payload)` with provider's signature header |

### 2.1 Token impersonation (admin-on-behalf-of)

```js
// browser_evaluate — log in as a different identity without losing the previous one
const adminTok = window.__yj.token;        // save
window.__yj.token = null;
await window.__yj.apiFetch("POST", "/api/v1/auth/login", { email: "userA@yallajo.test", password: "TestPass!23" })
  .then(r => { window.__yj.token = r.body.accessToken; });
// ... do user work ...
window.__yj.token = adminTok;              // restore admin
```

### 2.2 Multipart upload (provider documents, listing images)

```js
// browser_evaluate
const png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";
const out = await window.__yj.apiUpload(
  "POST",
  "/api/v1/accounts/provider-applications/{id}/documents",
  { documentType: "BusinessLicense" },
  [{ field: "file", name: "license.png", type: "image/png", base64: png }]
);
return out.status; // expect 200/201
```

### 2.3 SignalR (Notifications, LiveTracking) — connect from the browser

```js
// browser_evaluate
const script = document.createElement("script");
script.src = "https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.0/dist/browser/signalr.min.js";
document.head.appendChild(script);
await new Promise(r => script.onload = r);

const conn = new signalR.HubConnectionBuilder()
  .withUrl(`${window.__yj.api}/hubs/notifications`, { accessTokenFactory: () => window.__yj.token })
  .withAutomaticReconnect()
  .build();

window.__yj.received = [];
conn.on("ReceiveNotification", payload => window.__yj.received.push(payload));
await conn.start();
window.__yj.signalr = conn;
return conn.state; // "Connected"
```

### 2.4 Assertion idiom

After every `apiFetch`, the *test* asserts on `out.status` and `out.body` directly. Do **not** rely on UI state.

```js
// browser_evaluate
const r = await window.__yj.apiFetch("GET", "/api/v1/content-tours?status=Active");
if (r.status !== 200) throw new Error(`Expected 200, got ${r.status}`);
if (!Array.isArray(r.body?.items)) throw new Error("Expected items[]");
return { count: r.body.items.length, total: r.body.totalCount };
```

---

## 3. What changed in the per-module test docs

Each `Playwright-{Module}.md` file in this folder was generated against a Web+API world. The mechanical translation:

1. **§0 Prerequisites:** Web URL bullet removed; replaced with "API at https://localhost:57065 + this adapter." Bullets about starting `YallaJo.Web` were dropped.
2. **`browser_navigate("https://localhost:57070/...")` calls:** Replaced with `browser_navigate("https://localhost:57065/swagger")` for docking, and the *intent* of the navigation moves into an `apiFetch` block per the table in §2 above.
3. **"Fill form → click submit" steps:** Collapsed into a single `apiFetch("POST", ...)` per the API endpoint that the form posted to.
4. **Antiforgery / cookie assertions:** Removed. API uses Bearer JWT.
5. **NOT_BUILT scenarios** that depended on a Razor area that does not exist (`YallaJo.Web/Areas/TourGuide`, `YallaJo.Web/Areas/Booking`, `YallaJo.Web/Areas/PlatformOnboarding`, etc.): re-tagged as `NOT_BUILT (Web only)` and noted that the underlying API endpoints still need to be exercised via the adapter.

---

## 4. Common API endpoints (canonical, used across modules)

| Concern | Method | Path |
|---|---|---|
| Login | POST | `/api/v1/auth/login` |
| Register | POST | `/api/v1/auth/register` |
| Refresh | POST | `/api/v1/auth/refresh` |
| Logout | POST | `/api/v1/auth/logout` |
| Forgot password | POST | `/api/v1/auth/forgot-password` |
| Reset password | POST | `/api/v1/auth/reset-password` |
| Verify email | POST | `/api/v1/auth/verify-email` |
| Resend OTP | POST | `/api/v1/auth/resend-otp` |
| External login | POST | `/api/v1/auth/external-providers/login` |
| My profile | GET | `/api/v1/accounts/profiles/me` |
| Submit provider application | POST | `/api/v1/accounts/provider-applications` |
| Add doc to application | POST | `/api/v1/accounts/provider-applications/{id}/documents` |
| Admin approve application | POST | `/api/v1/accounts/provider-applications/{id}/approve` |
| Admin reject application | POST | `/api/v1/accounts/provider-applications/{id}/reject` |
| Tours list | GET | `/api/v1/content-tours` |
| Tour by id | GET | `/api/v1/content-tours/{id}` |
| Create booking | POST | `/api/v1/bookings` |
| Confirm slot | POST | `/api/v1/bookings/{id}/confirm` |
| Cancel booking | POST | `/api/v1/bookings/{id}/cancel` |
| Webhook | POST | `/api/v1/payments/webhook` |
| Notifications hub | WS | `/hubs/notifications` |
| Live-tracking hub | WS | `/hubs/live-tracking` |
| Health | GET | `/health` |

(Exact paths must be confirmed against Swagger; treat this table as a guidance map.)

---

## 5. Known divergences (still apply in API-only mode)

These are *behavioural*, not transport-level — they remain test material:

- Provider taxonomy: PDF=4 types, code=6 enum values. Assert against code enum.
- Doc size: PDF=5 MB, code=10 MB. Assert against code limit.
- Min payout: workflow=10 JOD, code=20 JOD.
- Commission: revenue-tier (code) wins over subscription-tier (PDF).
- Review eligibility: anyone (PDF) vs booking-verified (code) — code wins.
- Auto-hide reports threshold: 5 (PDF) vs 3 (code).
- Refund cutoff: 72h/24h (PDF) vs 24h single (code).
- `IHubContext<Hub>` → `IHubContext<NotificationHub>` Messaging SignalR bug (assert via SignalR connect failing or broadcasts not arriving).
- Double `/api/v1/provider` route potential — capture all 200s/404s in `browser_network_requests`.

---

## 6. When to fall back to Swagger UI manual exploration

Some scenarios in the per-module docs are written for an admin reviewer reading the UI. If the API surface doesn't yet support what the workflow describes, the doc tags them `NOT_BUILT` or `DEFERRED`. In API-only mode:

- Confirm `404 Not Found` (route missing) or `501 Not Implemented` (stub) for each `NOT_BUILT` endpoint instead of asserting UI state.
- Use Swagger (`https://localhost:57065/swagger`) to discover the exact route + payload shape when uncertain.

---

**Adapter version:** 1.0 · paired with seed users in `SeedIdentityProfiles.cs` and provider applications in `AccountsProviderApplicationSeeder.cs`.
