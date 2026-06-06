# Content Creator Dashboard — Area

**Actor:** Content Creator · **Plan section:** §7 · **Namespaces:** `/blogs/*`, `/blogs/creators/*`

> **Source of truth:** YallaJo code (`yallajo-endpoints.txt`, 549 endpoints). All routes are `/api/v1`-prefixed.
> **Architecture & rules:** see [`0-architecture-and-rules.md`](0-architecture-and-rules.md) — four-tier pipeline, cache tiers, permissions, UI-UX rule IDs.
> **Code area:** **`Creator`** (`/creator/*`). All pages authenticated → **`NoStore`** (UI-PERF-C2). Gated to the **Creator role** via `WebPermission.{Feature}.{Action}` claims.
> **No Webestica template page maps to the creator console** — built by you. The blog editor (§7.2) can **reuse the Quill** rich-text setup from `add-listing.html` / `blog-detail.html`.

**Status legend:** ✅ Wire · ♻️ Repurpose · ⏭️ Skip · 🟥 USER builds (no template page)

**Shared shell:** top bar = global search + language switcher + notification bell (`GET /notifications/unread-count` `AJAX⟳`) + avatar menu (`GET /accounts/profile`). Nav driver = `GET /security/me`.
**Load tags:** `SSR` / `AJAX` / `AJAX⟳` / `AJAX↑`. Cover/media via the attachments subsystem (`AJAX↑`).

---

## Pages this area should have

| # | Page | Template | Redirects to |
|---|------|----------|--------------|
| 7.1 | My Blogs | 🟥 USER builds | edit → §7.2 Editor · view public → §2.9 Blog post |
| 7.2 | Blog Editor | 🟥 USER builds (reuse Quill) | save → §7.1 · preview → §2.9 |
| 7.3 | Creator Profile | 🟥 USER builds | view public → §2.9 Creator profile |
| 7.4 | Audience | 🟥 USER builds | follower → public profile · comment → §7.2 |
| 7.5 | Application | 🟥 USER builds | approved → §7.1 / §7.3 |

---

## Endpoints by page

### 7.1 My Blogs
- `GET /blogs/my-blogs?status=` `SSR`
- `POST /blogs` `AJAX`
- `POST /blogs/{id}/submit-for-review` · `/publish` · `/unpublish` · `/archive` `AJAX`
- `DELETE /blogs/{id}` · `POST /blogs/{id}/restore` `AJAX`
- **Buttons:** **New Post** → `POST /blogs` → §7.2 · **Submit for Review** → `/submit-for-review` · **Publish** → `/publish` · **Unpublish** → `/unpublish` · **Archive** → `/archive` · **Delete** → `DELETE /blogs/{id}` (confirm) · **Restore** → `/restore` · **Edit** → §7.2 (nav).
- **Stack:** **Area** `Creator` · **Route** `/creator/blogs` · **Cache** `NoStore` · **Perm** `WebPermission.Blogs.Write` · **Rules** `R2 SSR list · D1 paging(status filter) · status-as-first-class badge+menu · F8 confirm Delete · C3 evict blog/homepage tags on publish/unpublish · NF1 toast · L6 empty-CTA · SEC7`

### 7.2 Blog Editor
- `GET /blogs/{id}` (or `GET /blogs/admin/{id}` for own draft) `SSR`
- `PUT /blogs/{id}` `AJAX`
- `POST /blogs/{id}/tours` · `DELETE /blogs/{id}/tours/{tourId}` `AJAX`
- Cover image via attachments subsystem `AJAX↑`
- **Buttons:** **Save** → `PUT /blogs/{id}` · **Upload Cover** → attachments (`AJAX↑`) · **Link Tour** → `POST /blogs/{id}/tours` · **Unlink Tour** → `DELETE /blogs/{id}/tours/{tourId}` · **Submit for Review** → `/submit-for-review` · **Preview** → §2.9 (nav).
- **Stack:** **Area** `Creator` · **Route** `/creator/blogs/{id}/edit` · **Cache** `NoStore` · **Perm** `WebPermission.Blogs.Write` · **Rules** `Quill rich-text(J1) · F5 autosave-30s localStorage · F9 beforeunload-dirty · ST1 RowVersion · SEC3 HTML sanitized(// SANITIZED) · cover AJAX↑ SEC4 magic-byte · C3 evict blog:{id} on publish · SEC7`

### 7.3 Creator Profile
- `GET /blogs/creators/profile/mine` `SSR`
- `PUT /blogs/creators/profile/mine` · `DELETE /blogs/creators/profile/mine` `AJAX`
- `PUT /blogs/creators/profile/mine/avatar` `AJAX↑`
- `GET /blogs/creators/niches` `AJAX`
- **Buttons:** **Save Profile** → `PUT /blogs/creators/profile/mine` · **Change Avatar** → `PUT .../mine/avatar` (`AJAX↑`) · **Delete Profile** → `DELETE .../mine` (confirm) · **View Public** → §2.9 (nav).
- **Stack:** **Area** `Creator` · **Route** `/creator/profile` · **Cache** `NoStore` · **Perm** `WebPermission.Creator.Write` · **Rules** `F1-F3 forms · avatar AJAX↑ SEC4 · ST1 RowVersion · F8 confirm Delete Profile · public→§2.9 · SEC7`

### 7.4 Audience
- `GET /blogs/creators/profiles/{profileId}/followers` `SSR`
- `GET /blogs/{id}/comments` `AJAX`
- `PUT /blogs/comments/{commentId}` · `DELETE /blogs/comments/{commentId}` `AJAX`
- `POST /blogs/comments/{commentId}/reactions` · `DELETE .../reactions` `AJAX`
- **Buttons:** **Edit Comment** → `PUT /blogs/comments/{commentId}` (≤30 min) · **Delete Comment** → `DELETE /blogs/comments/{commentId}` (confirm) · **React** → `POST .../reactions` · **Unreact** → `DELETE .../reactions`.
- **Stack:** **Area** `Creator` · **Route** `/creator/audience` · **Cache** `NoStore` · **Perm** `WebPermission.Creator.Read` · **Rules** `R2 SSR followers · D1 paging · comment edit ≤30min window · F8 confirm Delete Comment · NF6 optimistic react · SEC7`

### 7.5 Application
- `GET /blogs/creators/applications/mine` `SSR`
- `POST /blogs/creators/applications` · `PUT /blogs/creators/applications/{applicationId}` `AJAX`
- `POST /blogs/creators/applications/{applicationId}/submit` `AJAX`
- `POST /blogs/creators/invitations/redeem` `AJAX`
- **Buttons:** **Start Application** → `POST /blogs/creators/applications` · **Save Draft** → `PUT .../{applicationId}` · **Submit** → `POST .../{applicationId}/submit` · **Redeem Invitation** → `POST /blogs/creators/invitations/redeem`.
- **Stack:** **Area** `Creator` · **Route** `/creator/application` · **Cache** `NoStore` · **Perm** `[Authorize]` (pre-creator applicant) + `WebPermission.Creator.Apply` · **Rules** `F1-F3 + F5 save-draft · status-as-first-class badge · L6 empty-CTA · NF1 toast · SEC7`
