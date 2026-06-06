# YallaJo Dashboard — Areas Index

This folder splits the master plan into **one area file per actor**. Each area lists the pages that actor should have, with the mapped Webestica template page (and its status), where each page redirects, the endpoints behind each page, the action buttons it exposes, and a **Stack** annotation (code area, route, cache policy, permission, and governing UI-UX rule IDs).

> **Source of truth:** YallaJo code (`yallajo-endpoints.txt`, 549 endpoints, all `/api/v1`-prefixed except 4 system routes). Canonical combined reference: [`../yallajo-dashboard-ui-ux-plan.md`](yallajo-dashboard-ui-ux-plan.md). Template mapping detail: [`../yallajo-template-page-wiring.md`](yallajo-template-page-wiring.md).

> **Architecture & UI-UX rules:** every page also carries a **Stack** line — code **Area** (1 of 9: Public / Auth / Accounts / Provider / Guide / Business / Creator / Content / Admin) · **Route** · **Cache** (OutputCache tier or `NoStore`) · **Perm** · governing UI-UX rule IDs. The binding conventions live in [`0-architecture-and-rules.md`](0-architecture-and-rules.md), derived from `CONTROLLER_AUTHORING_GUIDE.md` + `Agents/UI/UI-UX-Design.md`.

**Status legend:** ✅ Wire · ♻️ Repurpose · ⏭️ Skip · 🟥 USER builds (no template page)

## Areas

| # | Area | Actor | Plan § | Pages | File |
|---|------|-------|--------|-------|------|
| 1 | Public Storefront | Anonymous / Public | §2 | 14 | [1-public-storefront.md](1-public-storefront.md) |
| 2 | Customer Dashboard | Customer / Tourist | §3 | 10 | [2-customer-dashboard.md](2-customer-dashboard.md) |
| 3 | Provider Dashboard | Provider / Host | §4 | 10 | [3-provider-dashboard.md](3-provider-dashboard.md) |
| 4 | Tour Guide Dashboard | Tour Guide | §5 | 11 | [4-tour-guide-dashboard.md](4-tour-guide-dashboard.md) |
| 5 | Agency Dashboard | Agency | §6 | 4 | [5-agency-dashboard.md](5-agency-dashboard.md) |
| 6 | Content Creator Dashboard | Content Creator | §7 | 5 | [6-content-creator-dashboard.md](6-content-creator-dashboard.md) |
| 7 | Admin Dashboard | Admin | §8 | 14 | [7-admin-dashboard.md](7-admin-dashboard.md) |
| 8 | SuperAdmin — RBAC Console | SuperAdmin / RBAC | §9 | 1 | [8-superadmin-rbac.md](8-superadmin-rbac.md) |

**Total:** 8 actor areas · ~69 distinct pages.

## What lives only in the master plan

These are cross-cutting and not actor-specific, so they stay in [`../yallajo-dashboard-ui-ux-plan.md`](yallajo-dashboard-ui-ux-plan.md):

- **§0 Global Conventions** — image/media (attachments subsystem), Place-contextual weather widget, load-strategy tags (`SSR` / `AJAX` / `AJAX⟳` / `AJAX↑`), shared shell (bell, avatar, `GET /security/me` nav driver).
- **§1 Role Inventory** — the 8-actor claim/namespace table; multi-role users get a workspace switcher (nav = union of claim-allowed dashboards).
- **§10 Cross-cutting Notes** — RowVersion/409 UX, status-as-first-class-UI, inline-mutation pattern, lazy tabs, media-tab, weather-widget, SignalR push, beacons, public-vs-management reads.
- **§11 Ambiguities-Resolved** — `{id}` ownership scoping, real `guideId` vs `me`, the three `my-tours` surfaces, missing list endpoints, plus the new-endpoints list.

## Shared shell (applies to every authenticated area)

Top bar: global search + language switcher + notification bell (`GET /notifications/unread-count` `AJAX⟳`, SignalR push) + avatar menu (`GET /accounts/profile`). Navigation is driven by `GET /security/me`.

## SEO entity types

`GET /seo/metadata|faq/{entityType}/{entityId}` is governed by the `SeoEntityType` enum — exactly **6 values**: `Place`, `Tour`, `Business`, `Blog`, `TourGuide`, `Creator`.
