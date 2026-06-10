---
id: 1ad97537-078d-403f-a7cd-2d3446c22d67
created: '2026-06-10T18:48:34.532Z'
modified: '2026-06-10T18:48:34.532Z'
memory_type: issue
tags:
  - security
  - tech-debt
  - secrets
  - oauth
  - follow-up
---
## OAuth provider secrets committed to source — rotate and move to user-secrets/env

**Status:** Open / outstanding security debt (NOT addressed in the CSP fix commit `fix(security): allow oauth provider form redirects in csp`)

### Location
- File: `src/Hosts/YallaJo.Web/appsettings.json`

### Live secrets committed in source control
Under the `ExternalProviders` and `ExternalAuth` sections:
- `ExternalProviders:Google:ClientSecret` (Google OAuth client secret)
- `ExternalProviders:Facebook:AppSecret` (Facebook/Meta app secret)
- `ExternalAuth:SigningKey` (HMAC-SHA256 key shared with the API)

### Risk
These are recoverable from git history; treat as **compromised**.

### Required action
1. Rotate Google ClientSecret in Google Cloud Console.
2. Rotate Facebook AppSecret in Meta for Developers.
3. Rotate `ExternalAuth:SigningKey` (must be updated on **BOTH** Web and API hosts simultaneously since it is shared).
4. Move all three out of `appsettings.json` into user-secrets (dev) and environment variables / secret store (prod).
5. Consider scrubbing git history if the repo is or will be shared.

### Context
Discovered while diagnosing/fixing the external-login CSP regression (`form-action 'self'` in `SecurityHeadersMiddleware` blocked Google/Facebook OAuth redirects, introduced in commit `e1d06a40` 'Gap fixing').
