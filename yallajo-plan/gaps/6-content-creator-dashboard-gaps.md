# Gap Report — `6-content-creator-dashboard.md` vs. shipped code

> **Method:** deep code-vs-plan audit against the **shipped** `Areas/Creator/Controllers/*` (6 controllers) **and the facade/VM layer**
> (`CreatorAudienceFacade`, `CreatorAudienceVm`, `CreatorArticlesApiClient`, `PreviewController`) + the shipped sidebar nav.
> The plan's routes/permissions and the RowVersion/admin-get insight were already corrected in the earlier §6 audit and are **accurate**;
> this pass goes to the data-flow layer to verify what each page actually fetches.
>
> **Scope:** `yallajo-plan/6-content-creator-dashboard.md` (§7 creator).
> **Severity:** 🔴 plan contradicts code · 🟠 over-claim/structure · 🟡 cosmetic.
> **Status:** ❌ not built · ✏️ differs · ➕ shipped but missing from plan · ✅ matches.

---

## 0. Summary

| Topic | Verdict |
|-------|---------|
| Routes / perms / verbs | ✅ Accurate (prior §6 audit) — re-verified against all 6 controllers' `[RequirePermission]` + routes. |
| §7.2 editor RowVersion/admin-get | ✅ Correct — confirmed `CreatorArticlesApiClient` is RowVersion-guarded; list read is `GET /blogs/my-blogs?page&pageSize&status`. |
| **§7.4 Audience** | 🔴 **Over-claims comments.** Plan says "view followers **+ comments on their posts**" and lists `GET /blogs/{id}/comments`. The shipped `CreatorAudienceFacade` fetches **followers only**; `CreatorAudienceVm` has **no Comments** — it's a **followers-only** page. |
| **Preview page** | ➕ A shipped **`PreviewController` (`/creator/preview`, CCD-6)** + sidebar nav "Preview" — the plan has **no §7.x page** for it (only references it as a §7.2 button). |
| Sidebar nav | ➕ 6 items: Dashboard, Articles, Audience, Profile, Application, **Preview**. |

---

## 1. 🔴 Plan-contradicts-code

### G1 — §7.4 Audience is followers-only (no comments view)
- **Plan says (§7.4):** *"From this page a creator can only view followers **+ the comments on their posts**"* and lists `GET /blogs/{id}/comments` as a read.
- **Code reality:**
  - `CreatorAudienceFacade.GetAudienceAsync(page)` calls **only** `ListFollowersAsync(mine.Id, page, PageSize)` → `GET /blogs/creators/profiles/{profileId}/followers`. **No comments fetch.**
  - `CreatorAudienceVm` exposes `State` (AudienceState lifecycle: NoProfile/…), `StatusLabel`, **`FollowerCount`**, **`Followers`** — **no `Comments` property.**
  - Followers are **public-safe summaries** (`FollowerSummaryResponse` — opaque identifier, not full identity; "Gap 3 Phase A").
- **Action:** remove the comments claim and the `GET /blogs/{id}/comments` read from §7.4. The Audience page is **followers-only**, showing a follower count + a list of public-safe follower rows, gated by an `AudienceState` (no-profile / active). Comment moderation is **not** a creator surface (comment edit/delete/react are commenter-owned on the public post, §2.9 — the plan already says this for the buttons, but the *reads* still wrongly include comments).

---

## 2. ➕ Shipped-but-missing-from-plan

### G2 — Creator Preview page (`/creator/preview`) has no §7.x
- **Code (`PreviewController`):** `GET /creator/preview` (`Creator.Read`) — *"Read-only public-profile preview (CCD-6): shows the creator how visitors see their profile; profile states drive a preview-unavailable notice for non-Active profiles; Active → public-safe read-only preview."* Sidebar nav item **"Preview"**.
- **vs plan:** §7.2 references `/creator/preview` only as a **Preview button target**; there is no dedicated page entry. It's a real shipped page (its own controller + nav item).
- **Action:** add a **§7.6 Preview** page (`/creator/preview`, `Creator.Read`, read-only) — "see your public creator profile as visitors do; shows a *preview-unavailable* notice when the profile isn't Active."

### G3 — Sidebar nav has 6 items; align the page map
- **Code nav:** Dashboard · Articles · Audience · Profile · Application · **Preview**.
- **Plan pages:** §7.0 Dashboard · §7.1 Articles · §7.2 Editor (sub of Articles) · §7.3 Profile · §7.4 Audience · §7.5 Application. **Missing: Preview.**
- **Action:** add Preview (G2); note §7.2 Editor is part of the Articles controller (not a separate nav item) — the nav is **6**, the plan's logical pages are **7** (Editor folded under Articles).

---

## 3. ✅ Confirmed-correct (re-verified, incl. facade layer)

- **§7.0 Dashboard** — `GET /creator`, `/creator/dashboard`, `[Authorize]` only ✅.
- **§7.1 My Blogs** — list read is `GET /blogs/my-blogs?page&pageSize&status` (`Blog.ReadOwn`); new/submit/delete/restore are page-scoped POST, RowVersion-guarded ✅.
- **§7.2 Editor** — admin-get prefetch (`GET /blogs/admin/{id}`) is the RowVersion source ✅; save/tour-links/images all shipped with the exact perms the plan lists (`Blog.Update`, `BlogTourLink.*`, `Attachment.*`, `EntityImage.Update`) ✅.
- **§7.3 Profile** — `GET /creator/profile` (`Creator.Read`), `POST /creator/profile` (`Update`), `/avatar` (`Update`), `/deactivate` (`Delete`); no `ST1` ✅.
- **§7.5 Application** — `create`/`submit` (`Creator.Submit`), `update` (`Creator.Update`), `invite` (`Creator.RedeemInvitation`) ✅.
- **Permissions** — every per-action constant matches the shipped `[RequirePermission]` ✅.

---

## 4. Recommended plan edits (apply order)

1. **G1** §7.4 — remove the "comments on their posts" claim + the `GET /blogs/{id}/comments` read; Audience is **followers-only** (public-safe summaries + `AudienceState` lifecycle + `FollowerCount`).
2. **G2** add **§7.6 Preview** (`/creator/preview`, `Creator.Read`, read-only public-profile preview; non-Active → preview-unavailable notice).
3. **G3** add Preview to the page table; note Editor is folded under Articles (nav = 6 items).

> **Net:** the creator plan is the **most accurate dashboard file** (routes, perms, verbs, and the tricky RowVersion/admin-get flow all verified). The deep facade read surfaced two gaps: §7.4 Audience **over-claims a comments view it doesn't fetch** (followers-only), and a whole **Preview page ships with no §7.x**. Both were invisible at the controller-attribute level — only the facade/VM read (Audience fetches followers only) and the nav/Preview controller revealed them.
