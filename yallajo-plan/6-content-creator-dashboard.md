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
| 7.2 | Blog Editor *(shipped `ArticlesController`, reuse Quill)* | `/creator/articles/{id}/edit` | save → §7.1 · preview → §2.9 |
| 7.3 | Creator Profile *(shipped `ProfileController`)* | `/creator/profile` | view public → §2.9 Creator profile |
| 7.4 | Audience *(shipped `AudienceController`, read-only)* | `/creator/audience` | follower → public profile |
| 7.5 | Application *(shipped `ApplicationController`)* | `/creator/application` | approved → §7.1 / §7.3 |

---

## Endpoints by page

### 7.0 Overview *(shipped `DashboardController`)*
- `GET /creator` / `GET /creator/dashboard` `SSR` — creator KPIs/landing.
- **Buttons:** My Blogs → §7.1 (nav) · Edit Profile → §7.3 (nav). Read-only.
- **Stack:** **Area** `Creator` · **Route** `/creator`, `/creator/dashboard` · **Cache** `NoStore` · **Perm** `[Authorize]` (shipped `DashboardController` has no `[RequirePermission]`) · **Rules** `R2 SSR · ERR3 section-degrade · A11Y1`

### 7.1 My Blogs *(shipped `ArticlesController`)*
- `GET /blogs/my-blogs?status=` `SSR`
- BFF mutations are `[HttpPost]` (PRG): new (`POST /creator/articles/new` → `POST /blogs`) · submit (`POST /creator/articles/{id}/submit` → `POST /blogs/{id}/submit-for-review`, **RowVersion**) · delete (`POST /creator/articles/{id}/delete` → `DELETE /blogs/{id}`, **RowVersion**) · restore (`POST /creator/articles/{id}/restore` → `POST /blogs/{id}/restore`, **RowVersion**). *(publish/unpublish/archive are admin-side moderation transitions — see §8.6.)*
- **Buttons:** **New Post** → `POST /creator/articles/new` → §7.2 · **Submit for Review** → `POST /creator/articles/{id}/submit` · **Delete** → `POST /creator/articles/{id}/delete` (F8 confirm) · **Restore** → `POST /creator/articles/{id}/restore` (post-delete Undo) · **Edit** → §7.2 (nav).
- **Stack:** **Area** `Creator` · **Route** `/creator/articles` · **Cache** `NoStore` · **Perm** `WebPermission.Blog.ReadOwn` (list); writes layer `Blog.Create` (new), `Blog.Submit`, `Blog.DeleteOwn` (delete+restore) (shipped) · **Rules** `R2 SSR list · D1 paging(status filter) · status-as-first-class badge+menu · F8 confirm Delete (RowVersion) · WL3-style Undo toast on delete · C3 evict blog/homepage tags · NF1 toast · L6 empty-CTA · SEC7`

### 7.2 Blog Editor *(shipped `ArticlesController`)*
- **Edit prefetch MUST use `GET /blogs/admin/{id}`** `SSR` — it is the only source of **RowVersion + Status**; the anonymous `GET /blogs/{id}` is Published-only with **no RowVersion** and cannot drive the editor.
- Save: `POST /creator/articles/{id}/edit` → `PUT /blogs/{id}` (text save).
- Tour links (RowVersion-guarded, separate from text-save): `POST /creator/articles/{id}/tours` → `POST /blogs/{id}/tours` · `POST /creator/articles/{id}/tours/{tourId}/unlink` → `DELETE /blogs/{id}/tours/{tourId}`.
- Images (attachments subsystem, `AJAX↑`): `POST /creator/articles/{id}/images/upload` · `.../images/{attachmentId}/delete` · `.../images/{attachmentId}/primary` · `.../images/reorder`.
- **Buttons:** **Save** → `POST /creator/articles/{id}/edit` · **Upload/Delete/Set-Primary/Reorder Image** → `/creator/articles/{id}/images/*` (`AJAX↑`) · **Link Tour** → `POST /creator/articles/{id}/tours` · **Unlink Tour** → `POST /creator/articles/{id}/tours/{tourId}/unlink` · **Submit for Review** → `POST /creator/articles/{id}/submit` · **Preview** → `/creator/preview` / §2.9 (nav).
- **Stack:** **Area** `Creator` · **Route** `/creator/articles/{id}/edit` · **Cache** `NoStore` · **Perm** `WebPermission.Blog.Read` (edit-get) / `Blog.Update` (save) / `Blog.Submit`; tour-links `BlogTourLink.Create`/`Delete`; images `Attachment.Create`/`Delete`/`Update` + `EntityImage.Update` (set-primary) (all shipped) · **Rules** `Quill rich-text(J1) · F5 autosave-30s localStorage · F9 beforeunload-dirty · **ST1 RowVersion (round-tripped from the admin-get prefetch on save/submit/delete/link-tour)** · SEC3 HTML sanitized(// SANITIZED) · images AJAX↑ SEC4 magic-byte · C3 evict blog:{id} · SEC7`

### 7.3 Creator Profile
- `GET /blogs/creators/profile/mine` `SSR`
- `PUT /blogs/creators/profile/mine` · `DELETE /blogs/creators/profile/mine` `AJAX`
- `PUT /blogs/creators/profile/mine/avatar` `AJAX↑`
- `GET /blogs/creators/niches` `AJAX`
- **Buttons:** **Save Profile** → `POST /creator/profile` → `PUT /blogs/creators/profile/mine` · **Change Avatar** → `POST /creator/profile/avatar` → `PUT .../mine/avatar` (`AJAX↑`) · **Delete Profile** → `POST /creator/profile/deactivate` → `DELETE .../mine` (confirm) · **View Public** → §2.9 (nav).
- **Stack:** **Area** `Creator` · **Route** `/creator/profile` · **Cache** `NoStore` · **Perm** `WebPermission.Creator.Read` (get) / `Creator.Update` (save+avatar) / `Creator.Delete` (deactivate) (shipped) · **Rules** `F1-F3 forms · avatar AJAX↑ SEC4 · F8 confirm Delete Profile · public→§2.9 · SEC7` *(no `ST1`: the shipped Creator profile DTOs expose no `RowVersion`)*

### 7.4 Audience *(shipped `AudienceController` — read-only)*
> The shipped `AudienceController` exposes **only `GET /creator/audience`** (`Creator.Read`). Comment **edit/delete** (`PUT/DELETE /blogs/comments/{commentId}`, owner ≤30 min) and **reactions** (`POST/DELETE /blogs/comments/{commentId}/reactions`) are **commenter-owned actions on the public blog post (§2.9)** — they are **not** creator-area moderation and are **not** wired here. From this page a creator can only view followers + the comments on their posts.
- `GET /blogs/creators/profiles/{profileId}/followers` `SSR`
- `GET /blogs/{id}/comments` `AJAX` (read-only view)
- **Buttons:** **View Follower** → public profile (nav) · **Go to Comment** → §2.9 post (nav). Read-only.
- **Stack:** **Area** `Creator` · **Route** `/creator/audience` · **Cache** `NoStore` · **Perm** `WebPermission.Creator.Read` (shipped) · **Rules** `R2 SSR followers · D1 paging · L6 empty-CTA · SEC7`

### 7.5 Application
- `GET /blogs/creators/applications/mine` `SSR`
- `POST /blogs/creators/applications` · `PUT /blogs/creators/applications/{applicationId}` `AJAX`
- `POST /blogs/creators/applications/{applicationId}/submit` `AJAX`
- `POST /blogs/creators/invitations/redeem` `AJAX`
- **Buttons:** **Start Application** → `POST /creator/application/create` → `POST /blogs/creators/applications` · **Save Draft** → `POST /creator/application/update` → `PUT .../{applicationId}` · **Submit** → `POST /creator/application/submit` → `POST .../{applicationId}/submit` · **Redeem Invitation** → `POST /creator/application/invite` → `POST /blogs/creators/invitations/redeem`.
- **Stack:** **Area** `Creator` · **Route** `/creator/application` · **Cache** `NoStore` · **Perm** `WebPermission.Creator.Read` (get) / `Creator.Submit` (create+submit) / `Creator.Update` (update) / `Creator.RedeemInvitation` (redeem) (shipped) · **Rules** `F1-F3 + F5 save-draft · status-as-first-class badge · L6 empty-CTA · NF1 toast · SEC7`
