# Content Creator Dashboard — Area

**Actor:** Content Creator · **Plan section:** §7 · **Namespaces:** `/blogs/*`, `/blogs/creators/*`

> **Source of truth:** YallaJo code (`yallajo-endpoints.txt`, 549 endpoints). All routes are `/api/v1`-prefixed.
> **Architecture & rules:** see [`0-architecture-and-rules.md`](0-architecture-and-rules.md) — four-tier pipeline, cache tiers, permissions, UI-UX rule IDs.
> **Code area:** **`Creator`** (`/creator/*`). All pages authenticated → **`NoStore`** (UI-PERF-C2). The `Areas/Creator` controllers are **already implemented** (`Dashboard`, `Articles`, `Profile`, `Audience`, `Application`, `Preview`); the **Perm** + **Route** lines below are taken from the *shipped* attributes.
> **Permission reality (verified against `Areas/Creator/Controllers/*` + `WebPermission.cs`):** there is **no** `Blogs` or `Creator.Write`/`Creator.Apply` namespace. Real classes: `Blog.*` (`ReadOwn`/`Read`/`Create`/`Update`/`Submit`/`DeleteOwn`), `Creator.*` (`Read`/`Update`/`Submit`/`Delete`/`RedeemInvitation`), `BlogTourLink.Create`/`Delete`, `Attachment.*`, `EntityImage.Update`.
> **BFF verbs are `POST` (PRG):** the `PUT`/`DELETE` shown in endpoint/button lines are the **API-layer** verbs (ApiClient → API). Every BFF action is `[HttpPost]` + anti-forgery + PRG (e.g. `POST /creator/articles/{id}/delete`, `POST /creator/profile/avatar`, `POST /creator/application/submit`).
> **No Webestica template page maps to the creator console** — built by you. The blog editor (§7.2) can **reuse the Quill** rich-text setup from `add-listing.html` / `blog-detail.html`.

**Status legend:** ✅ Wire · ♻️ Repurpose · ⏭️ Skip · 🟥 USER builds (no template page)

**Shared shell:** top bar = global search + language switcher + notification bell (`GET /notifications/unread-count` `AJAX⟳`) + avatar menu (`GET /accounts/profile`). Nav driver = `GET /security/me`.
**Load tags:** `SSR` / `AJAX` / `AJAX⟳` / `AJAX↑`. Cover/media via the attachments subsystem (`AJAX↑`).

---

## Pages this area should have

> **Build status:** the whole `Areas/Creator` is **already shipped** (6 controllers). Views (`.cshtml`) may be redesigned, but the controllers already cover these actions — treat these as *view-redesign* pages, not greenfield.

| # | Page | Route (shipped) | Redirects to |
|---|------|----------------|--------------|
| 7.0 | **Overview** *(shipped `DashboardController`, was missing from the plan)* | `/creator`, `/creator/dashboard` | KPI cards → §7.1 / §7.3 |
| 7.1 | My Blogs *(shipped `ArticlesController`)* | `/creator/articles` | edit → §7.2 Editor · view public → §2.9 Blog post |
| 7.2 | Blog Editor *(shipped `ArticlesController`, reuse Quill)* | `/creator/articles/{id}/edit` | save → §7.1 · submit-for-review |
| 7.3 | Creator Profile *(shipped `ProfileController`)* | `/creator/profile` | public view → §7.6 Preview |
| 7.4 | Audience *(shipped `AudienceController`, read-only — **followers only**)* | `/creator/audience` | no per-follower nav · no profile → §7.5 |
| 7.5 | Application *(shipped `ApplicationController`)* | `/creator/application` | approved → §7.1 / §7.3 |
| 7.6 | Preview *(shipped `PreviewController`, read-only)* | `/creator/preview` | no profile → §7.5 · view public → §2.9 |

> **Sidebar nav = 6 items:** Dashboard (§7.0) · Articles (§7.1) · Audience (§7.4) · Profile (§7.3) · Application (§7.5) · Preview (§7.6). §7.2 **Blog Editor is folded under the Articles controller** (not its own nav item), so the logical page count is 7 while the nav shows 6.

---

## Endpoints by page

### 7.0 Overview *(shipped `DashboardController`)*
- `GET /creator` / `GET /creator/dashboard` `SSR` — creator KPIs/landing.
- **Buttons:** state-dependent CTAs (shipped `Dashboard/Index.cshtml`) — **Approved profile:** New Article → §7.2 · View all articles → §7.1 · Audience → §7.4 · Preview → §7.6 · **application states:** Become a creator / Continue / Update / Reapply → §7.5. Read-only (nav only, no POST).
- **Stack:** **Area** `Creator` · **Route** `/creator`, `/creator/dashboard` · **Cache** `NoStore` · **Perm** `[Authorize]` + `WebPermission.Creator.Read` (shipped `DashboardController` carries a class-level `[RequirePermission(WebPermission.Creator.Read)]`; safe for the "Become a creator" funnel because `Permission.Creator.Read` is part of `ConsumerPermissions` in `RolePermissionMapping.cs`, granted to every regular User) · **Rules** `R2 SSR · ERR3 section-degrade · A11Y1`

### 7.1 My Blogs *(shipped `ArticlesController`)*
- `GET /blogs/my-blogs?status=` `SSR`
- BFF mutations are `[HttpPost]` (PRG): new (`POST /creator/articles/new` → `POST /blogs`) · submit (`POST /creator/articles/{id}/submit` → `POST /blogs/{id}/submit-for-review`, **RowVersion**) · delete (`POST /creator/articles/{id}/delete` → `DELETE /blogs/{id}`, **RowVersion**) · restore (`POST /creator/articles/{id}/restore` → `POST /blogs/{id}/restore`, **RowVersion**). *(publish/unpublish/archive are admin-side moderation transitions — see §8.6.)*
- **Buttons** (shipped `Articles/Index.cshtml`): **New Article** → `POST /creator/articles/new` → §7.2 · **Edit** → §7.2 (nav) · **status filter** + **paging** · **Restore/Undo** (only after a delete) → `POST /creator/articles/{id}/restore`. *(Submit-for-Review and Delete are **editor** actions in §7.2, not list buttons.)*
- **Stack:** **Area** `Creator` · **Route** `/creator/articles` · **Cache** `NoStore` · **Perm** `WebPermission.Blog.ReadOwn` (list); writes layer `Blog.Create` (new), `Blog.Submit`, `Blog.DeleteOwn` (delete+restore) (shipped) · **Rules** `R2 SSR list · D1 paging(status filter) · status-as-first-class badge+menu · F8 confirm Delete (RowVersion) · WL3-style Undo toast on delete · C3 evict blog/homepage tags · NF1 toast · L6 empty-CTA · SEC7`

### 7.2 Blog Editor *(shipped `ArticlesController`)*
- **Edit prefetch MUST use `GET /blogs/admin/{id}`** `SSR` — it is the only source of **RowVersion + Status**; the anonymous `GET /blogs/{id}` is Published-only with **no RowVersion** and cannot drive the editor.
- Save: `POST /creator/articles/{id}/edit` → `PUT /blogs/{id}` (text save).
- Tour links (RowVersion-guarded, separate from text-save): `POST /creator/articles/{id}/tours` → `POST /blogs/{id}/tours` · `POST /creator/articles/{id}/tours/{tourId}/unlink` → `DELETE /blogs/{id}/tours/{tourId}`.
- Images (attachments subsystem) — **multipart `POST`/PRG** (anti-forgery + redirect, **not** AJAX): `POST /creator/articles/{id}/images/upload` · `.../images/{attachmentId}/delete` · `.../images/{attachmentId}/primary` · `.../images/reorder`. *(API layer = ContentCore Attachment endpoints, keyed by EntityType/EntityId.)*
- **Buttons** (shipped `Articles/Editor.cshtml`): **Back to list** → §7.1 · **Save** → `POST /creator/articles/{id}/edit` · **Upload/Delete/Set-Primary/Reorder Image** → `/creator/articles/{id}/images/*` (**multipart POST/PRG**) · **Link Tour** → `POST /creator/articles/{id}/tours` · **Unlink Tour** → `POST /creator/articles/{id}/tours/{tourId}/unlink` · **Submit for Review** → `POST /creator/articles/{id}/submit` · **Delete** → `POST /creator/articles/{id}/delete`. *(Preview is reached from the sidebar nav (§7.6), not an editor button.)*
- **Stack:** **Area** `Creator` · **Route** `/creator/articles/{id}/edit` · **Cache** `NoStore` · **Perm** `WebPermission.Blog.Read` (edit-get) / `Blog.Update` (save) / `Blog.Submit`; tour-links `BlogTourLink.Create`/`Delete`; images `Attachment.Create`/`Delete`/`Update` + `EntityImage.Update` (set-primary) (all shipped) · **Rules** `Quill rich-text(J1) · F5 autosave-30s localStorage · F9 beforeunload-dirty · **ST1 RowVersion (round-tripped from the admin-get prefetch on save/submit/delete/link-tour)** · SEC3 HTML sanitized(// SANITIZED) · images multipart POST/PRG · SEC4 magic-byte · C3 evict blog:{id} · SEC7`

### 7.3 Creator Profile
- `GET /blogs/creators/profile/mine` `SSR`
- `PUT /blogs/creators/profile/mine` · `DELETE /blogs/creators/profile/mine` `AJAX`
- `PUT /blogs/creators/profile/mine/avatar` — **avatar URL only** (normal BFF POST/PRG, **not** a file upload: the shipped `UpdateAvatarVm.AvatarUrl` is a `[Url]` string, no `IFormFile`; `CreatorProfileFacade` explicitly performs no upload).
- **Buttons:** **Save Profile** → `POST /creator/profile` → `PUT /blogs/creators/profile/mine` · **Change Avatar (URL)** → `POST /creator/profile/avatar` → `PUT .../mine/avatar` · **Delete Profile** → `POST /creator/profile/deactivate` → `DELETE .../mine` (confirm). *(Shipped `Profile/Index.cshtml` has Save profile, Update avatar URL, Deactivate — no public-view button; the public profile is reached via §7.6 Preview.)*
- **Stack:** **Area** `Creator` · **Route** `/creator/profile` · **Cache** `NoStore` · **Perm** `WebPermission.Creator.Read` (get) / `Creator.Update` (save+avatar) / `Creator.Delete` (deactivate) (shipped) · **Rules** `F1-F3 forms · avatar = URL update (no file upload, no SEC4) · F8 confirm Delete Profile · public view via §7.6 Preview · SEC7` *(no `ST1`: the shipped Creator profile DTOs expose no `RowVersion`)* *(niches are not loaded here — see §7.5)*

### 7.4 Audience *(shipped `AudienceController` — read-only, **followers only**)*
> **Followers-only page** (verified against `CreatorAudienceFacade` + `CreatorAudienceVm`). The shipped `AudienceController` exposes **only `GET /creator/audience`** (`Creator.Read`). The facade resolves the caller's own profile via `GET /blogs/creators/profile/mine` (authoritative `FollowerCount` + status) and — **only when the profile is `Active`** — fetches one page of **public-safe** follower summaries from `GET /blogs/creators/profiles/{profileId}/followers`. There is **no comments fetch**: `CreatorAudienceVm` has **no `Comments` property** (it exposes `State`/`StatusLabel`/`FollowerCount`/`Followers`/`Pager`). Comment **edit/delete** (`PUT/DELETE /blogs/comments/{commentId}`, owner ≤30 min) and **reactions** (`POST/DELETE /blogs/comments/{commentId}/reactions`) are **commenter-owned actions on the public blog post (§2.9)** — **not** a creator surface and **not** wired here.
- `GET /blogs/creators/profile/mine` `SSR` — drives `FollowerCount` + the `AudienceState` lifecycle (**NoProfile** → redirect to §7.5 Application · **Unavailable** for non-`Active` (Suspended/Deactivated) → unavailable notice, no follower fetch · **Active** → render the list).
- `GET /blogs/creators/profiles/{profileId}/followers` `SSR` — Active-only; returns **public-safe** rows rendered as anonymous ordinals ("Follower #N", `FollowedAt`) — **no user IDs/names/avatars ever reach the HTML**. No total-count envelope → "has next" is inferred from a full page of rows.
- **Buttons:** paging only (**Previous**/**Next** when available). **No per-follower navigation** — follower identities are intentionally not exposed (rows are anonymous "Follower #N" ordinals; the shipped `FollowerSummaryResponse` carries only `Ordinal` + `FollowedAt`, no id/slug/name/avatar). Read-only.
- **Stack:** **Area** `Creator` · **Route** `/creator/audience` · **Cache** `NoStore` · **Perm** `WebPermission.Creator.Read` (shipped) · **Rules** `R2 SSR followers · D1 paging(heuristic) · AudienceState-driven empty/unavailable · L6 empty-CTA · SEC7`

### 7.5 Application
- `GET /blogs/creators/applications/mine` `SSR`
- `GET /blogs/creators/niches` `AJAX` — niche options for the application form (loaded by `CreatorApplicationFacade`, **not** the Profile page).
- `POST /blogs/creators/applications` · `PUT /blogs/creators/applications/{applicationId}` `AJAX`
- `POST /blogs/creators/applications/{applicationId}/submit` `AJAX`
- `POST /blogs/creators/invitations/redeem` `AJAX`
- **Buttons:** **Start Application** → `POST /creator/application/create` → `POST /blogs/creators/applications` · **Save Draft** → `POST /creator/application/update` → `PUT .../{applicationId}` · **Submit** → `POST /creator/application/submit` → `POST .../{applicationId}/submit` · **Redeem Invitation** → `GET /creator/application/invite` (token form page) → `POST /creator/application/invite` → `POST /blogs/creators/invitations/redeem`.
- **Stack:** **Area** `Creator` · **Route** `/creator/application` · **Cache** `NoStore` · **Perm** `WebPermission.Creator.Read` (get) / `Creator.Submit` (create+submit) / `Creator.Update` (update) / `Creator.RedeemInvitation` (redeem) (shipped) · **Rules** `F1-F3 + F5 save-draft · status-as-first-class badge · L6 empty-CTA · NF1 toast · SEC7`

### 7.6 Preview *(shipped `PreviewController` — read-only, CCD-6)*
> **Read-only public-profile preview** (verified against `PreviewController` + `CreatorPreviewFacade`): shows the creator how visitors see their public profile, using the anonymous public-by-slug endpoints. A real shipped page with its own controller **and sidebar nav item** ("Preview") — §7.2 only references `/creator/preview` as a button target. The VM/mapper **drops internal fields** (UserId, Status, LinkedProviderId, CreatedAt) so nothing private reaches the HTML.
- `GET /creator/preview` `SSR` — `PreviewState` lifecycle: **NoProfile** → `RedirectToAction("Index","Application")` (§7.5) · **Unavailable** (profile not `Active`, or the public-by-slug read 404s) → preview-unavailable notice · **Active** → public-safe read-only preview.
- **Buttons** (shipped `Preview/Index.cshtml`): paging for published articles when needed; **published article titles link to public blog posts** (§2.9). Read-only — **no POST actions**, no public-profile "View Public" button.
- **Stack:** **Area** `Creator` · **Route** `/creator/preview` · **Cache** `NoStore` · **Perm** `[Authorize]` + `WebPermission.Creator.Read` (shipped) · **Rules** `R2 SSR · state-driven unavailable notice · public→§2.9 · SEC7`
