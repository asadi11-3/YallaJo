# Test Run — {Subject}

| Field | Value |
|---|---|
| **Date** | YYYY-MM-DD HH:MM |
| **Runner** | Playwright MCP |
| **Scenario file** | [`Playwright-{Module}.md`](../Playwright-{Module}.md) |
| **Adapter** | [`Playwright-APIOnly-Adapter.md`](../Playwright-APIOnly-Adapter.md) |
| **Environment** | API `https://localhost:57065` · DB `MOHAMMAD\SQLEXPRESS` · Recaptcha disabled |
| **Auth context** | `__yj.token` = {role} (`{userId}`) |
| **Build/commit** | `{git rev-parse HEAD short}` |

## Summary

- **Total scenarios:** N
- **PASS:** N
- **FAIL:** N
- **SKIP (NOT_BUILT):** N
- **Duration:** ~Nmin
- **New findings:** N (logged in [`Findings-Rolling.md`](Findings-Rolling.md))

## Scenarios

| TC | Endpoint | Auth | Expected | Actual | Status | Evidence |
|---|---|---|---|---|---|---|
| TC-X-1 | `GET /api/v1/...` | admin | 200 | 200 | ✅ | `{ items: [...] }` |
| TC-X-2 | `POST /api/v1/...` | userA | 403 | 403 | ✅ | `Forbidden` |

## Notes / Observations

- Free-form observations during the run.
- Things to revisit on next run.

## Findings (cross-reference)

- F1 confirmed (see Findings-Rolling.md#f1).
- New: F-NEW — short description, full detail in Findings-Rolling.md.

## Next actions

- [ ] Re-run after Finding F1 is fixed.
- [ ] Add coverage for X.
