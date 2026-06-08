# Gap Report — `6-content-creator-dashboard.md` vs. shipped code

> **STATUS: ✅ ALL GAPS RESOLVED** (G1/G2/G3 fixed in the plan; full end-to-end backend audit found **no code defects**). See §5 for the backend pass.
>
> **Method:** deep code-vs-plan audit against the **shipped** `Areas/Creator/Controllers/*` (6 controllers) **and the facade/VM layer**
> (`CreatorAudienceFacade`, `CreatorAudienceVm`, `CreatorArticlesApiClient`, `PreviewController`) + the shipped sidebar nav.
> The plan's routes/permissions and the RowVersion/admin-get insight were already corrected in the earlier §6 audit and are **accurate**;
> this pass goes to the data-flow layer to verify what each page actually fetches, **plus a code-first end-to-end backend trace** (Razor → controller → facade → ApiClient → `/api/v1` endpoint → CQRS handler → domain → EF).
>
> **Scope:** `yallajo-plan/6-content-creator-dashboard.md` (§7 creator) + backend module `ContentBlogs`.
> **Severity:** 🔴 plan contradicts code · 🟠 over-claim/structure · 🟡 cosmetic.
> **Status:** ❌ not built · ✏️ differs · ➕ shipped but missing from plan · ✅ matches · ✅ RESOLVED.

---

## 0. Summary

| Topic | Verdict |
|-------|---------|
| Routes / perms / verbs | ✅ Accurate (prior §6 audit) — re-verified against all 6 controllers' `[RequirePermission]` + routes. |
| §7.2 editor RowVersion/admin-get | ✅ Correct — confirmed `CreatorArticlesApiClient` is RowVersion-guarded; list read is `GET /blogs/my-blogs?page&pageSize&status`. |
| **§7.4 Audience** | ✅ **RESOLVED (G1).** Was 🔴 over-claiming comments; §7.4 rewritten to **followers-only** (no `GET /blogs/{id}/comments`). |
| **Preview page** | ✅ **RESOLVED (G2).** New **§7.6 Preview** page added for the shipped `PreviewController` (`/creator/preview`, CCD-6). |
| Sidebar nav | ✅ **RESOLVED (G3).** Page table now lists Preview; note added that nav = 6 items, Editor folded under Articles. |
| **§7.3 Profile data-flow** | ✅ **RESOLVED (G4, G5).** Niches were wrongly listed under Profile (belong to §7.5 Application); avatar was wrongly described as a file upload (it's URL-only). See §6. |
| **Backend (`ContentBlogs`) end-to-end** | ✅ **CLEAN.** No ownership/NRE/data-loss/concurrency/stub defects on the creator path (see §5). |

---

## 1. 🔴 Plan-contradicts-code

### G1 — §7.4 Audience is followers-only (no comments view)
- **Status:** ✅ **RESOLVED** — §7.4 rewritten to followers-only: removed the "comments on their posts" claim, the `GET /blogs/{id}/comments` read, and the "Go to Comment" button; documented the facade flow (`GET /blogs/creators/profile/mine` → `FollowerCount` + `AudienceState` NoProfile→redirect §7.5 / Unavailable / Active; Active-only `GET …/followers` returning public-safe anonymous "Follower #N" rows with a has-next heuristic, no total-count envelope). Verified against `CreatorAudienceFacade.cs` + `CreatorAudienceVm.cs`.
- **Plan says (§7.4):** *"From this page a creator can only view followers **+ the comments on their posts**"* and lists `GET /blogs/{id}/comments` as a read.
- **Code reality:**
  - `CreatorAudienceFacade.GetAudienceAsync(page)` calls **only** `ListFollowersAsync(mine.Id, page, PageSize)` → `GET /blogs/creators/profiles/{profileId}/followers`. **No comments fetch.**
  - `CreatorAudienceVm` exposes `State` (AudienceState lifecycle: NoProfile/…), `StatusLabel`, **`FollowerCount`**, **`Followers`** — **no `Comments` property.**
  - Followers are **public-safe summaries** (`FollowerSummaryResponse` — opaque identifier, not full identity; "Gap 3 Phase A").
- **Action:** remove the comments claim and the `GET /blogs/{id}/comments` read from §7.4. The Audience page is **followers-only**, showing a follower count + a list of public-safe follower rows, gated by an `AudienceState` (no-profile / active). Comment moderation is **not** a creator surface (comment edit/delete/react are commenter-owned on the public post, §2.9 — the plan already says this for the buttons, but the *reads* still wrongly include comments).

---

## 2. ➕ Shipped-but-missing-from-plan

### G2 — Creator Preview page (`/creator/preview`) has no §7.x
- **Status:** ✅ **RESOLVED** — added a new **§7.6 Preview** section (`GET /creator/preview` SSR, `[Authorize]` + `Creator.Read`, read-only no-POST; `PreviewState` NoProfile→RedirectToAction Application / Unavailable / Active; VM drops internal fields). Verified against `PreviewController.cs`.
- **Code (`PreviewController`):** `GET /creator/preview` (`Creator.Read`) — *"Read-only public-profile preview (CCD-6): shows the creator how visitors see their profile; profile states drive a preview-unavailable notice for non-Active profiles; Active → public-safe read-only preview."* Sidebar nav item **"Preview"**.
- **vs plan:** §7.2 references `/creator/preview` only as a **Preview button target**; there is no dedicated page entry. It's a real shipped page (its own controller + nav item).
- **Action:** add a **§7.6 Preview** page (`/creator/preview`, `Creator.Read`, read-only) — "see your public creator profile as visitors do; shows a *preview-unavailable* notice when the profile isn't Active."

### G3 — Sidebar nav has 6 items; align the page map
- **Status:** ✅ **RESOLVED** — page table now includes the §7.6 Preview row and a note that the sidebar = 6 items (Dashboard · Articles · Audience · Profile · Application · Preview), with §7.2 Blog Editor folded under the Articles controller (logical pages 7, nav 6). Verified Preview nav via `PreviewController.SetSidebar` (`ViewData["CreatorNav"]="Preview"`).
- **Code nav:** Dashboard · Articles · Audience · Profile · Application · **Preview**.
- **Plan pages:** §7.0 Dashboard · §7.1 Articles · §7.2 Editor (sub of Articles) · §7.3 Profile · §7.4 Audience · §7.5 Application. **Missing: Preview.**
- **Action:** add Preview (G2); note §7.2 Editor is part of the Articles controller (not a separate nav item) — the nav is **6**, the plan's logical pages are **7** (Editor folded under Articles).

---

## 3. ✅ Confirmed-correct (re-verified, incl. facade layer)

- **§7.0 Dashboard** — `GET /creator`, `/creator/dashboard`, `[Authorize]` only ✅.
- **§7.1 My Blogs** — list read is `GET /blogs/my-blogs?page&pageSize&status` (`Blog.ReadOwn`); new/submit/delete/restore are page-scoped POST, RowVersion-guarded ✅.
- **§7.2 Editor** — admin-get prefetch (`GET /blogs/admin/{id}`) is the RowVersion source ✅; save/tour-links/images all shipped with the exact perms the plan lists (`Blog.Update`, `BlogTourLink.*`, `Attachment.*`, `EntityImage.Update`) ✅.
- **§7.3 Profile** — routes/perms `GET /creator/profile` (`Creator.Read`), `POST /creator/profile` (`Update`), `/avatar` (`Update`), `/deactivate` (`Delete`), no `ST1` ✅. *(Two data-flow mismatches were found here and fixed — see **G4** (niches) and **G5** (avatar is URL-only, not a file upload) in §6.)*
- **§7.5 Application** — `create`/`submit` (`Creator.Submit`), `update` (`Creator.Update`), `invite` (`Creator.RedeemInvitation`) ✅.
- **Permissions** — every per-action constant matches the shipped `[RequirePermission]` ✅.

---

## 4. Recommended plan edits (apply order)

1. **G1** §7.4 — remove the "comments on their posts" claim + the `GET /blogs/{id}/comments` read; Audience is **followers-only** (public-safe summaries + `AudienceState` lifecycle + `FollowerCount`).
2. **G2** add **§7.6 Preview** (`/creator/preview`, `Creator.Read`, read-only public-profile preview; non-Active → preview-unavailable notice).
3. **G3** add Preview to the page table; note Editor is folded under Articles (nav = 6 items).

> **Net:** the creator plan is the **most accurate dashboard file** (routes, perms, verbs, and the tricky RowVersion/admin-get flow all verified). The deep facade read surfaced two gaps: §7.4 Audience **over-claimed a comments view it doesn't fetch** (followers-only), and a whole **Preview page shipped with no §7.x**. Both were invisible at the controller-attribute level — only the facade/VM read and the nav/Preview controller revealed them. **All three are now fixed in the plan.**

---

## 5. ✅ Backend end-to-end audit (`ContentBlogs` module) — CLEAN, no code defects

> Code-first trace of every Content-Creator feature past the web layer: `/api/v1/blogs/*` + `/blogs/creators/*` → CQRS handler → domain → EF. Module: `src/Modules/ContentBlogs`. Mounted at `/api/v1/blogs` (`ContentBlogsEndpoints.cs`). Blog **images** are not in this module — they go through `ContentCore` `AttachmentEndpoints.cs` keyed by EntityType/EntityId (ownership-gated, `.RequireAuthorization()`).

- **Ownership — no gap.** `UpdateBlog`/`DeleteBlog`/`RestoreBlog`/`LinkBlogTours`/`UnlinkBlogFromTour` all call `BlogAuthorHierarchyGuard.EnsureCanManageBlogOwnedByAsync(blog.AuthorId, …)` **before** mutating (compares the persisted `AuthorId`, not the route id; allows self or higher-privilege). `SubmitBlogForReviewCommandHandler` does an explicit owner-equality check. Creator self-service resolves the profile/application by `currentUser.UserId` / `ApplicantUserId` — a creator cannot touch another's resource.
- **NRE / `.Trim()` on null — none.** `Blog.cs` non-null trims (Title/Slug/Content) are guarded by `CreateBlog`/`UpdateBlog` validators (`NotEmpty`); `UpdateBlog` also enforces `RowVersion NotNull+len>0`. `CreatorProfile`/`CreatorApplication` have no `.Trim()` in mutation paths. No `F126`-class bug.
- **Data-loss / ignored Result / stubs — none.** No dropped command fields; no ignored domain `Result`; no `TODO`/`NotImplementedException`. Concurrency handled everywhere (`DbUpdateConcurrencyException`→Conflict; `RowVersionUtil.Equal` null-safe).
- **Missing validator files (all safe):** `SubmitBlogForReview` (BlogId+RowVersion only, owner-checked), `SubmitCreatorApplication` (Guid only), `UpdateCreatorAvatar` (ownership-by-user, no `.Trim`). Non-blocking nits: `UpdateCreatorApplicationCommandValidator` lacks the list-size caps that `Create` has; `CreateCreatorApplicationCommandValidator` doesn't cap `SocialHandles` — quality polish, not defects.
- **Web layer — clean.** `Areas/Creator/Views/*` contain **zero** `<script>` blocks (no AgencyRoster-style functional inline script). The only inline JS is `onsubmit="return confirm(...)"` (Editor) and `onchange="this.form.submit()"` (Articles index) — the accepted repo-wide progressive-enhancement convention (no active CSP header; no `data-yj-action` framework repo-wide), out of scope per the §4 ruling. Facades are exemplary (graceful degradation), auth wiring on every mutation is present.
- **Auth-metadata observation (style, not a hole):** ContentBlogs endpoints attach `.WithMetadata(new MustHavePermissionAttribute(feature, action))` but, unlike Accounts/ContentCore, omit an explicit `.RequireAuthorization()`. `MustHavePermissionAttribute : AuthorizeAttribute` sets `.Policy` and is `IAuthorizeData`, so the AuthorizationMiddleware (`app.UseAuthorization()`; no Fallback/Default policy configured) collects + enforces the `Permission.*` policy, which requires an authenticated user with the claim. **The endpoints are protected; the missing `.RequireAuthorization()` is a cross-module consistency nit, not an auth-bypass.** (If a future defense-in-depth pass wants uniformity, add `.RequireAuthorization()` to each authed ContentBlogs endpoint.) *Confirmed by Oracle (round-1 final gate): PASS on auth — evidence chain `MustHavePermissionAttribute.cs:6` → `PermissionPolicyProvider.cs:21` (`RequireAuthenticatedUser()` + `PermissionRequirement`) → `PermissionAuthorizationHandler.cs:14` → `Program.cs:383` `UseAuthorization()`; no source fix required.*

---

## 6. ✅ §7.3 Profile data-flow corrections (found by the Oracle final gate)

> The controller-attribute layer (routes/perms) for §7.3 was already correct, but the **facade data-flow** revealed two mismatches the original ledger missed. Both fixed in `6-content-creator-dashboard.md`.

### G4 — Creator niches belong to Application (§7.5), not Profile (§7.3)
- **Status:** ✅ **RESOLVED** — removed the `GET /blogs/creators/niches` read from §7.3 (added a "*niches are not loaded here — see §7.5*" note) and added it to the §7.5 Application endpoint list ("niche options for the application form, loaded by `CreatorApplicationFacade`").
- **Plan said (§7.3):** listed `GET /blogs/creators/niches` `AJAX` under Creator Profile.
- **Code reality:** `CreatorProfileFacade.cs:21` only reads `/profile/mine`; niches are loaded by `CreatorApplicationFacade.cs:22` for the application form.

### G5 — Avatar is a URL update, not a file upload
- **Status:** ✅ **RESOLVED** — §7.3 avatar line reworded to "avatar URL only (normal BFF POST/PRG, not a file upload)"; button changed to "Change Avatar (URL)" (dropped `AJAX↑`); Stack Rules changed `avatar AJAX↑ SEC4` → `avatar = URL update (no file upload, no SEC4)`.
- **Plan said (§7.3):** avatar described as `AJAX↑` / upload / SEC4 magic-byte.
- **Code reality:** `UpdateAvatarVm.AvatarUrl` is a `[Url]` string with **no `IFormFile`** (`CreatorProfileVms.cs:53`); `CreatorProfileFacade.cs:11` explicitly does no upload; `ProfileController.cs:75` posts a normal BFF form `POST /creator/profile/avatar` → `PUT /blogs/creators/profile/mine/avatar` (URL string).

## 7. ✅ §7.4/§7.5 final-gate corrections (G6, G7)

> A second Oracle final-gate pass found two more doc-vs-code mismatches the earlier rounds missed. Both fixed in `6-content-creator-dashboard.md`; docs-only, no source change.

### G6 — Audience has no per-follower navigation (followers are anonymous)
- **Status:** ✅ **RESOLVED** — §7.4 button line changed from "View Follower → public profile" to "paging only (Previous/Next); no per-follower navigation — follower identities intentionally not exposed".
- **Plan said (§7.4):** "**View Follower** → public profile (nav)".
- **Code reality:** `FollowerSummaryResponse` exposes only `Ordinal` + `FollowedAt` (`Models/Audience/FollowerSummaryResponse.cs:12`); `CreatorAudienceVm` carries no follower id/slug/name/avatar (`CreatorAudienceVm.cs:40`); `Audience/Index.cshtml:56` renders anonymous ordinals with no link.

### G7 — Redeem Invitation has a GET form page before the POST
- **Status:** ✅ **RESOLVED** — §7.5 Redeem flow changed to "GET /creator/application/invite (token form page) → POST /creator/application/invite → POST /blogs/creators/invitations/redeem".
- **Plan said (§7.5):** "Redeem Invitation → `POST /creator/application/invite` → `POST /blogs/creators/invitations/redeem`" (omitted the GET form page).
- **Code reality:** `ApplicationController.cs:118` = `GET /creator/application/invite` (renders the token form); `ApplicationController.cs:127` = `POST /creator/application/invite` (submits → `POST /blogs/creators/invitations/redeem`).

## 8. button/nav view-accuracy corrections (G8-G14)

Round-3 final-gate (fresh oracle) found 7 docs-only over-claims: the plan
enumerated buttons/nav targets that the shipped Razor views do not render.
Routes, verbs, permissions and module boundaries were all re-confirmed correct;
NO source-code fix required. All fixed in 6-content-creator-dashboard.md.

- **G8** - RESOLVED. Page-table 7.4 Audience Redirects cell said
  'follower -> public profile'. Code: followers are anonymous (no id/slug/name).
  Fix: 'no per-follower nav - no profile -> 7.5'.
- **G9** - RESOLVED. 7.0 Overview Buttons said 'My Blogs / Edit Profile'.
  Code Dashboard/Index.cshtml: state-dependent CTAs. Fix: Approved ->
  New Article/View all/Audience/Preview; application states ->
  Become a creator/Continue/Update/Reapply; read-only nav.
- **G10** - RESOLVED. 7.1 My Blogs Buttons listed Submit/Delete as list buttons.
  Code Articles/Index.cshtml: New Article, Edit, status filter, paging,
  Restore/Undo only after delete. Submit/Delete are editor actions (7.2).
  Endpoint list unchanged (endpoints are real; only view buttons over-claimed).
- **G11** - RESOLVED. 7.2 image actions labelled 'AJAX up'. Code: multipart
  POST/PRG with anti-forgery + redirect, via ContentCore Attachment endpoints
  keyed by EntityType/EntityId. SEC4 magic-byte retained (real file upload).
- **G12** - RESOLVED. 7.2 Editor Buttons listed a non-existent 'Preview' button.
  Code Editor.cshtml: Back to list, Save, image (multipart), Link Tour,
  Unlink Tour, Submit for Review, Delete. Preview is sidebar nav (7.6).
- **G13** - RESOLVED. 7.3 Profile Buttons listed 'View Public'. Code
  Profile/Index.cshtml: Save, Update avatar URL, Deactivate only. Removed;
  page-table + stack 'public view' now points to 7.6 Preview.
- **G14** - RESOLVED. 7.6 Preview Buttons listed 'View Public'. Code
  Preview/Index.cshtml: read-only; paging + published article titles link to
  public blog posts (2.9); no POST, no public-profile button. Page-table 7.2
  Redirects cell corrected to 'submit-for-review'.

STATUS: plan 6-content-creator-dashboard.md has all FOURTEEN gaps fixed
(G1-G14), sections 7.0-7.6. Docs-only; no source modified; no build needed.



---

## 9. Code-fix loop (plan+rules = spec; code audited & fixed against them)

This pass re-audited the SHIPPED CODE against the plan + governing rules as a CODE-FIX
loop (not a doc-reconciliation). Direction of truth: plan/rules say WHAT; code was fixed
to comply. Build: YallaJo.Web `0 Error(s)` / 81 pre-existing warnings (baseline). Three
required code gaps found and fixed; three candidates evaluated and ruled NOT-a-gap.

### GAP-15 -- SEC3 stored-XSS on public blog post render
- Status: RESOLVED
- Severity: BLOCKER
- Type: NON-COMPLIANT (INCORRECT)
- Plan requirement: 7.2 Stack rule SEC3 -- @Html.Raw only on server-side-sanitized HTML with a truthful // SANITIZED comment (UI-UX-Design.md:574).
- Code reality (before): Areas/Public/Views/Blog/Post.cshtml rendered @Html.Raw(Model.Content) where BlogPostVm.Content was the RAW API/translation content (BlogFacade.GetPostAsync assigned it unsanitized). The ContentHtmlSanitizer was only applied to a different, unused-here VM (BlogDetailsVm.ContentHtml via BlogsMapper.cs:47). The // SANITIZED comment falsely claimed write-side sanitization (write path only .Trim()s).
- Layer: web (BFF facade + Razor view)
- Rule impact: SEC3 / SEC1 (XSS)
- Fix: BlogFacade.GetPostAsync now computes `resolvedContent = TranslationOverlay.Apply(...) ?? d.Content` then assigns `Content = ContentHtmlSanitizer.Sanitize(resolvedContent)` (sanitize AFTER translation overlay so translated HTML is also covered); mirrors BlogsMapper.cs:45-47. Post.cshtml:71 comment corrected to truthfully name the sanitizer + boundary.
- Resolution: src/Hosts/YallaJo.Web/Areas/Public/Facades/BlogFacade.cs (using + GetPostAsync), src/Hosts/YallaJo.Web/Areas/Public/Views/Blog/Post.cshtml:71-73. Full @Html.Raw sweep across YallaJo.Web confirmed every other Raw site renders safe (SEO JSON-LD, integer-derived star HTML, server JSON serialization) -- no other unsanitized stored-content site.

### GAP-16 -- 7.2 Blog Editor missing rich-text + autosave + unsaved-changes guard
- Status: RESOLVED
- Severity: MEDIUM
- Type: INCOMPLETE
- Plan requirement: 7.2 Stack rules J1 (Quill rich-text) - F5 (autosave 30s to localStorage + recovery banner) - F9 (beforeunload-dirty warning).
- Code reality (before): Editor.cshtml content field was a plain <textarea>; Quill library was not loaded in any view; no autosave, no beforeunload.
- Layer: web (Razor view + static JS)
- Rule impact: J1 / F5 / F9
- Fix: Editor.cshtml -- main form id=articleEditorForm + data-article-id; #contentEditor host div + [data-quill-backing] textarea; @section Styles loads quill.snow.css; @section Scripts loads quill.min.js then creator-article-editor.js. New src/Hosts/YallaJo.Web/wwwroot/assets/js/creator-article-editor.js (CSP-safe IIFE, no inline handlers): Quill snow init seeded from + synced to the backing textarea; autosave every 30s to localStorage key yj.article.draft.<id> with a DOM-built recovery banner (Restore/Discard) on next visit, cleared on submit, non-blocking on missing drafts API; beforeunload warning when any tracked field is dirty, suppressed on submit; guards `typeof window.Quill === 'undefined'` for graceful textarea fallback.
- Resolution: src/Hosts/YallaJo.Web/Areas/Creator/Views/Articles/Editor.cshtml, src/Hosts/YallaJo.Web/wwwroot/assets/js/creator-article-editor.js (new).

### GAP-17 -- 7.1 post-delete Undo presented as inline alert instead of toast
- Status: RESOLVED
- Severity: LOW
- Type: INCORRECT
- Plan requirement: 7.1 Stack rule WL3-style Undo toast on delete - NF1 toast.
- Code reality (before): Areas/Creator/Views/Articles/Index.cshtml rendered the post-delete undo as an inline `alert alert-success` block.
- Layer: web (Razor view)
- Rule impact: WL3 / NF1 / MOD12
- Fix: converted to a CSP-safe Bootstrap toast (toast-container position-fixed bottom-0 end-0; .toast.show role=alert data-bs-autohide=false; toast-header with data-bs-dismiss close button handled by the bundled bootstrap JS; toast-body keeps the Deleted "<title>" text). PRESERVED the Restore POST form + hidden id + rowVersion + Undo submit button and the Model.ShowUndo guard; ArticlesController Restore action unchanged.
- Resolution: src/Hosts/YallaJo.Web/Areas/Creator/Views/Articles/Index.cshtml.

### Evaluated NOT-a-gap (Oracle-ruled this loop)
- SEC4 magic-byte (7.2 image upload): NOT a gap. Server-side ContentCore UploadAttachmentCommandHandler performs real signature/MIME validation; the creator-side extension/content-type/size checks are acceptable UX prechecks, not the security boundary. No bypass upload path exists.
- F8 delete-confirm (7.1/7.2 delete): NOT a gap. Editor.cshtml already has a Bootstrap confirmation modal (question title + consequence body + safe/danger buttons) that materially satisfies F8.
- ERR3 dashboard section-degrade (7.0): NOT a gap. CreatorDashboardFacade degrades recent-articles to [] on failure; profile/application are primary dashboard reads and the controller renders an empty VM + error (no 500), which is acceptable.

Build after fixes: YallaJo.Web 0 Error(s), 81 pre-existing warnings; no new warning from the touched files. BlogFacade.cs passed lsp_diagnostics clean.

---

## 10. ✅ Round-4 code-first re-audit — NO regression, NO new gaps

> Full code-first sweep of the Content Creator Dashboard against plan §7 + governing rules.
> Four parallel layers audited: (A) 6 BFF controllers, (B) facades/ApiClients/VMs, (C) prior
> fixes GAP-15/16/17 intact, (D) ContentBlogs backend creator path. Direction of truth: plan/rules
> say WHAT; code verified to comply. **Outcome: zero open code gaps — no source change required.**

### A. BFF controllers (6) — COMPLIANT
All of Dashboard/Articles/Profile/Audience/Application/Preview have `[Area("Creator")]`+`[Authorize]`,
inject Facades only, correct per-action `[RequirePermission]` matching the plan exactly, every POST
`[ValidateAntiForgeryToken]`+PRG, every write `async Task<IActionResult>`+trailing `ct`. The only
attribute-level observation — `ArticlesController.New()` + `ApplicationController.Invite()` are
synchronous `IActionResult` GET form-renders without `ct` — is **NOT a gap**: it is a repo-wide
accepted convention for static-form GET renders (13 identical instances across 12 controllers, e.g.
`BlogsController.Create()`, `AuthController.ForgotPassword()`); pure `View(new Vm())`, no async/I/O/facade
call. Flagging only the 2 Creator ones would invent inconsistency against an accepted pattern (prior §5
already ruled repo-wide conventions out of scope).

### B. Facades / ApiClients / VMs — COMPLIANT
7 Facades sealed+suffix, inject ApiClients only (no IApiClient/HttpContext/TempData), map ApiResult→VM,
friendly errors, graceful degradation. 3 ApiClients sealed+suffix, one-line endpoints. Every hotspot
re-verified correct: editor prefetch uses `GET /blogs/admin/{id}` (RowVersion+Status source) not the
anonymous get; RowVersion round-tripped on all guarded writes; `CreatorAudienceVm` has no Comments
property; followers anonymous `Follower #N` (no id/name/avatar); avatar is `[Url][Required][StringLength(500)]`
URL-only string (no IFormFile, no SEC4); preview VM drops internal fields; niches loaded by Application
facade not Profile; follower fetch gated to Active profiles. No IOutputCacheStore eviction — correct
(creator pages are NoStore). Creator views: 0 `Html.Raw`, 18 anti-forgery POST forms (SEC7 satisfied).

### C. Prior fixes GAP-15/16/17 — INTACT (no regression)
GAP-15 sanitize-after-translation present in `BlogFacade.GetPostAsync` + truthful `// SANITIZED` comment
in `Post.cshtml`. GAP-16 `creator-article-editor.js` present (Quill init + 30s autosave to
`yj.article.draft.<id>` + recovery banner + beforeunload-dirty guard) wired in `Editor.cshtml`.
GAP-17 CSP-safe Bootstrap Undo toast present in `Articles/Index.cshtml` (Restore POST + rowVersion,
guarded by `@if(Model.ShowUndo)`).

### D. ContentBlogs backend creator path — CLEAN (3 audit-agent findings all REFUTED by direct reads)
A re-audit agent over-flagged 3 items; each refuted code-first so a future auditor does not re-raise them:
- **UpdateCreatorAvatarCommand "missing validator → NRE"** — REFUTED. Domain `CreatorProfile.UpdateAvatar(string avatarUrl)` (CreatorProfile.cs:308-312) is a plain assignment `{ AvatarUrl = avatarUrl; MarkUpdated(); }` — **no `.Trim()`, no dereference**, backing prop `string? AvatarUrl` is nullable; Rule 10 (Trim/deref→NotEmpty validator) is **not triggered**. BFF already enforces `[Url][Required][StringLength(500)]`. Not a gap.
- **SubmitBlogForReviewCommand "missing validator → Guid.Empty/null RowVersion"** — REFUTED. Handler loads `GetByIdAsync(request.BlogId)`→null→`Blog.NotFound` (Guid.Empty just yields NotFound, no crash); owner-equality→Forbidden; `RowVersionUtil.Equal` (null-safe)→Conflict; no `.Trim()`/deref of any string field. Not a gap.
- **UpdateCreatorProfileCommand "DisplayName allows empty"** — REFUTED (agent misread). `UpdateCreatorProfileCommandValidator` already has `RuleFor(x => x.DisplayName!).NotEmpty()...MaximumLength(200).When(x => x.DisplayName is not null)`. Not a gap.
Ownership (14/14 via `BlogAuthorHierarchyGuard` + self-service `currentUser.UserId`), RowVersion/concurrency
(`DbUpdateConcurrencyException`→Conflict everywhere), data-loss (Result checked before persist), module
isolation (no cross-module FK), endpoint-auth (`MustHavePermissionAttribute` on all) all confirmed — prior §5
ContentBlogs-CLEAN conclusion HOLDS. Known non-blocking nits (`UpdateCreatorApplicationCommandValidator`
lacks list-size caps that Create has; `CreateCreatorApplicationCommandValidator` doesn't cap `SocialHandles`)
remain quality-polish, NOT defects — no rule violation, no plan requirement, not fixed.

**STATUS: Content Creator Dashboard = ZERO open code gaps. All G1-G17 RESOLVED + intact; Round-4 4-layer
re-audit found no regression and no new gaps. No source modified this round.**

