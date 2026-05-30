# Playwright MCP Test Scenarios — ContentCore

> **API-ONLY MODE.** This checkout has no `YallaJo.Web`. Every `browser_navigate("https://localhost:57065/swagger…")` step below is a docking step; the actual request runs through `window.__yj.apiFetch(...)` defined in [`Playwright-APIOnly-Adapter.md`](./Playwright-APIOnly-Adapter.md). Read that adapter once at the start of every Playwright session — it also lists the 8 seeded test users and their credentials.

## Source Plans

| Source | Status / test impact |
|---|---|
| `Agents/Plans/ContentCore-Workflow.md` | Implemented shared infrastructure: attachments/images, categories, tags, entity tags/categories, languages, translations; actual endpoint count 52. |
| `Agents/Plans/ContentCore-Audit-Report.md` | Confirms 52 endpoints, 12 entities, 12 integration events; flags category depth/cycle, image validation, attachment limit, permission, and EntityImage gaps. |
| `Agents/Plans/ContentCore-FixPlan.md` | Defines NOT_BUILT/fix coverage: `AttachmentLimits.MaxFileSize`, language validators, category hierarchy guard, translation cache sizing, `IsMarkedForDeletion`, idempotency. |
| `Agents/Plans/Master-RoadmapTo10.md` | W4-A: ContentCore score target, EF migration/OpenAPI/cycle detection follow-ups. |
| `Agents/Plans/CrossDocumentAnalysisReport.md` | REDUNDANCY-4: EntityImage handling duplicated across 5 modules; tests must surface this divergence. |

**Scenario count:** 65 total = 52 active, 8 NOT_BUILT, 5 DEFERRED.

## 0. Prerequisites

- **App state:** not running; SQL bug blocks execution; reCAPTCHA disabled.
- **Web URL:** `https://localhost:57065/swagger`; **API URL:** `https://localhost:57065`.
- **Seed users:** `admin@yallajo.test` / `TestPass!23` has content-admin; `userA@yallajo.test` / `TestPass!23` is read-only public.
- **Seed data:** use Jordan/Amman/Petra IDs where an `EntityType=Place` target is needed; create isolated records named `pw-core-<timestamp>`.
- **API route prefix:** `/api/v1/content-core`.
- **MCP tools:** use standard Playwright MCP plus `browser_take_screenshot` for image gallery proof. Prefer Web Content area pages when present (`YallaJo.Web/Areas/Content/Features/{Categories,Tags}`); otherwise call API through `browser_evaluate(async () => fetch(...))` and assert UI/API state.
- **Auth helper:** login through Web if available, otherwise obtain API token via auth endpoint and attach `Authorization: Bearer <token>` in `browser_evaluate` fetch calls.

## 1. Built — Active Scenarios

### Attachments / EntityImage

#### CC001 — Admin uploads attachment image
- POST `/api/v1/content-core/attachments` multipart for `EntityType=Place`, seeded Petra id, image file. Assert `201/200`, URL returned, magic-byte validation accepts real image, image appears in entity attachment list.

#### CC002 — Upload rejects unsupported file signature
- Submit `.jpg` filename with non-image bytes. Assert validation/problem response and no attachment row appears in subsequent list.

#### CC003 — Upload enforces attachment count limit
- Given Place limit is 30, seed/create 30 attachments, upload 31st. Assert `Attachment.LimitReached` or equivalent validation message.

#### CC004 — Upload enforces canonical file-size rules
- Submit boundary metadata for Image 10MB, Video 500MB, Document 25MB, Audio 50MB. Assert at limit passes and over limit rejects.

#### CC005 — List entity attachments
- GET `/attachments?entityType=Place&entityId={id}` as admin/read-permitted user. Assert paginated/list response contains uploaded attachment and stable metadata.

#### CC006 — Get attachment by id
- GET `/attachments/{id}`. Assert file name, content type, size, storage URL, entity type/id.

#### CC007 — Delete attachment
- DELETE `/attachments/{id}` as admin. Assert success; subsequent GET/list does not expose it or marks deletion state.

#### CC008 — Reorder attachments
- PUT `/attachments/reorder` with two uploaded IDs and sort orders. Assert order persists after list refresh.

#### CC009 — Set primary image
- PUT `/attachments/primary` for one image. Assert only that image has primary marker in list/gallery.

#### CC010 — Bulk upload EntityImages
- POST `/attachments/images` with multiple images, alt text, sort order. Assert all image variants are returned and list/gallery renders them.

#### CC011 — Image gallery ordering screenshot
- Open UI/API-backed gallery for target entity, assert order by `SortOrder`, primary badge, alt text; capture `browser_take_screenshot`.

### Categories

#### CC012 — Public lists active categories localized EN
- Anonymous GET `/categories?activeOnly=true` with `Accept-Language: en`. Assert active categories only and English name/slug.

#### CC013 — Public lists categories localized AR
- Anonymous GET `/categories?activeOnly=true` with `Accept-Language: ar`. Assert Arabic localized fields where seeded, fallback safe otherwise.

#### CC014 — Public get category by id
- Anonymous GET `/categories/{id}`. Assert no admin-only deleted data leaks.

#### CC015 — Admin list categories includes inactive/deleted flags
- Admin GET `/categories/admin?includeDeleted=true`. Assert active/inactive/deleted metadata is visible.

#### CC016 — Admin get category by id includes admin metadata
- Admin GET `/categories/admin/{id}`. Assert translations, parent, sort/order fields.

#### CC017 — Admin creates category
- POST `/categories` with EN/AR names, generated slug omitted or explicit valid slug. Assert created id and slug rules.

#### CC018 — Category slug generation and normalization
- Create category named `PW Core Café & Tours!`; assert slug lowercases, strips invalid chars, hyphenates, and is unique.

#### CC019 — Admin updates category localization
- PUT `/categories/{id}` changing `en` and `ar` fields. Assert both locales return expected content.

#### CC020 — Category max depth 3 guard
- Create parent→child→grandchild, attempt fourth-level child. Assert validation/business rejection per business rules.

#### CC021 — Reject category self-parent
- Direct update with `parentCategoryId == id`. Assert validation/problem response; tree unchanged.

#### CC022 — Delete category
- DELETE `/categories/{id}`. Assert public list hides it; admin include-deleted can observe deleted state.

#### CC023 — Deactivate category
- PATCH `/categories/{id}/deactivate`. Assert public active list excludes it; admin list shows inactive.

#### CC024 — Activate category
- PATCH `/categories/{id}/activate`. Assert public active list includes it again.

#### CC025 — Restore category
- PATCH `/categories/{id}/restore` after soft delete. Assert category reappears and audit metadata updates.

#### CC026 — Reorder categories
- PUT `/categories/reorder` for sibling categories. Assert list order stable and cycle-safe tree rendering.

### Entity categories

#### CC027 — Public lists entity categories
- GET `/entity-categories?entityType=Place&entityId={id}` anonymous. Assert assigned categories visible.

#### CC028 — Admin assigns 1-5 categories to entity
- POST `/entity-categories` with 1 then 5 category IDs. Assert success and list matches exactly.

#### CC029 — Reject >5 categories for tour/entity category assignment
- POST with 6 category IDs for a Tour-like target. Assert validation rejection and no partial write.

#### CC030 — Remove category from entity
- DELETE `/entity-categories?entityType=Place&entityId={id}&categoryId={id}`. Assert list no longer contains it.

### Tags

#### CC031 — Public lists tags localized EN/AR
- Anonymous GET `/tags?activeOnly=true` with `Accept-Language` `en` then `ar`. Assert localized name behavior.

#### CC032 — Public get tag by id
- GET `/tags/{id}` anonymous. Assert active tag details.

#### CC033 — Admin creates tag with slug
- POST `/tags`; assert slug generated/normalized and unique.

#### CC034 — Admin updates tag
- PUT `/tags/{id}` with new localized name/slug. Assert list/detail reflects update.

#### CC035 — Delete tag
- DELETE `/tags/{id}`. Assert public list hides deleted tag.

#### CC036 — Activate tag
- PATCH `/tags/{id}/activate`; assert active list includes it.

#### CC037 — Deactivate tag
- PATCH `/tags/{id}/deactivate`; assert active list excludes it.

### Entity tags

#### CC038 — Public lists entity tags
- GET `/entity-tags?entityType=Place&entityId={id}` anonymous. Assert tag chips visible.

#### CC039 — Admin assigns tags to entity
- POST `/entity-tags` with one/multiple tag IDs. Assert list includes them and duplicate assignment is idempotent/rejected cleanly.

#### CC040 — Remove tag from entity
- DELETE `/entity-tags?entityType=Place&entityId={id}&tagId={id}`. Assert tag removed and no catalog tag deletion occurs.

### Languages and localized content

#### CC041 — Public lists active languages
- GET `/languages?activeOnly=true`. Assert `ar` and `en` appear per seed/config.

#### CC042 — Public get language by id
- GET `/languages/{id}`. Assert culture code, display name, active flag.

#### CC043 — Admin creates language
- POST `/languages` with valid code/name. Assert created and appears in admin/public when active.

#### CC044 — Admin updates language
- PUT `/languages/{id}` changing display name/active flag. Assert state and no duplicate activation events on no-op update.

#### CC045 — Delete language
- DELETE `/languages/{id}`. Assert soft-deleted/excluded from active list.

#### CC046 — Activate language
- PATCH `/languages/{id}/activate`. Assert active list includes it and downstream translation backfill event is expected.

#### CC047 — Deactivate language
- PATCH `/languages/{id}/deactivate`. Assert active list excludes it.

### Specializations

#### CC048 — Public lists specializations
- GET `/specializations?activeOnly=true` anonymous. Assert guide-only specializations appear.

#### CC049 — Public get specialization by id
- GET `/specializations/{id}` anonymous. Assert details.

#### CC050 — Admin creates/updates/deletes specialization
- POST then PUT then DELETE `/specializations`. Assert state changes and public visibility.

#### CC051 — Activate/deactivate specialization idempotently
- PATCH activate/deactivate twice. Assert first changes state; second is no-op or clean success with no duplicate visible effects.

### Translations

#### CC052 — Translate text, batch translate, get/update/approve translations
- Admin exercises `/translations/translate`, `/batch`, `/{entityType}/{entityId}`, `PUT /{id}`, `POST /{id}/approve`, `/backfill/{entityKind}`, `/approve-batch`. Assert Result pattern errors are visible and localized content becomes approved.

## 2. NOT_BUILT

1. **NB001 — Full category cycle detection beyond self-parent.** Fix plan defers multi-level cycle detection to application layer; test should fail if A→B→A can be saved.
2. **NB002 — Persisted `Attachment.IsMarkedForDeletion` migration.** Fix plan calls for a state flag/migration; verify before asserting persisted marker.
3. **NB003 — Unified EntityImage pipeline across modules.** Cross-document REDUNDANCY-4: upload/order/alt/delete rules remain duplicated across ContentPlaces, ContentTours, ContentBlogs, Social, Accounts.
4. **NB004 — Endpoint OpenAPI error annotations.** Audit flags missing `Produces*`; Playwright can only verify runtime behavior, not Swagger completeness unless Swagger UI is available.
5. **NB005 — Analytics consumers for EntityTag events.** `EntityTagAssigned/Removed` events exist but Analytics consumer was a follow-up.
6. **NB006 — ContentSeo consumers for EntityTag events.** SEO metadata/sitemap refresh on tag changes is planned follow-up.
7. **NB007 — ContentCore category depth 3 may be partially enforced.** Business rule exists; audit highlighted domain gap, so treat as acceptance gate.
8. **NB008 — Single shared CDN/thumbnail generation contract.** Media processing exists, but full CDN/thumbnail unification is not established.

## 3. DEFERRED

1. **D001 — Value objects for FileName/MimeType/FileSize.** Audit marks as aspirational/info.
2. **D002 — Category soft-delete inheritance cleanup.** `new` vs `override` noted as low/info.
3. **D003 — Attachment audit trail refactor to AuditableEntity.** Requires product/domain decision.
4. **D004 — Provider/creator self-service permission breadth hardening.** Current design relies on OwnershipGuard and role mapping.
5. **D005 — Real Azure Translate provider abstraction replacement.** Current Azure service remains MVP behind interface.

## 4. Integration Events

- Publishes: `AttachmentUploaded`, `AttachmentDeleted`, `CategoryCreated/Updated/Deleted/Restored`, `EntityCategoryAssigned/Removed`, `EntityTagAssigned/Removed`, `LanguageActivated/Deactivated`.
- Expected consumers: ContentPlaces/ContentTours language activation; Analytics and ContentSeo tag changes are follow-up.
- Event scenarios: after tag/category assignment, poll entity list and optionally outbox diagnostics; after language activation, assert downstream modules eventually show translation/backfill signals if app exposes them.

## 5. Validation Matrix

| Area | Valid | Invalid / expected rejection |
|---|---|---|
| Image upload | jpg/png/webp real signatures; within type limits | Spoofed extension, unsupported signature, over limit |
| Attachment count | Place/Tour <=30, Business/Blog <=20, Review <=5, TourGuide <=10 | Next upload after max |
| Category depth | Root, child, grandchild | Fourth level, self-parent, cycle |
| Entity categories | 1-5 category IDs | 0, >5, duplicates, inactive/deleted ids |
| Slug | lowercase alnum hyphen, unique | spaces, special chars unnormalized, duplicate slug |
| Locales | `Accept-Language: en/ar` | unsupported locale falls back safely |
| Language code | valid culture code | blank, duplicate, invalid format |

## 6. Auth Matrix

| Capability | Anonymous | userA read-only | content-admin |
|---|---:|---:|---:|
| List/get public tags/categories/languages/specializations | Yes | Yes | Yes |
| List entity tags/categories | Yes | Yes | Yes |
| Upload/delete/reorder attachments | No | No | Yes |
| Tag/category catalog CRUD | No | No | Yes |
| Assign/remove entity tags/categories | No | Usually no unless owner+permission | Yes |
| Language/specialization mutations | No | No | Yes |
| Translation mutations/backfill/approve | No | No | Yes |

## 7. State Machines

### Category
```text
Active → Inactive → Active
Active/Inactive → Deleted → Restored → Active/Inactive
Parent tree max depth 3; cycles invalid.
```

### Tag / Specialization / Language
```text
Created Active → Deactivated → Activated
Any mutable state → Deleted/SoftDeleted where endpoint exists
Repeated activate/deactivate should be idempotent.
```

### Attachment / EntityImage
```text
Uploaded → Ordered/Reordered → PrimarySelected → MarkedForDeletion/Deleted
Bulk images create ordered EntityImage records for the parent entity.
```

## 8. Known Divergence

1. **EntityImage redundancy:** Cross-document REDUNDANCY-4 says image handling remains duplicated across 5 modules; ContentCore tests should expose inconsistent limits/order/alt behavior rather than assuming a universal pipeline.
2. **Endpoint count docs:** current workflow notes actual 52 endpoints; older lines said 48.
3. **Attachment file-size source:** audit found validator/canonical mismatch; tests use canonical limits from FixPlan.
4. **Role self-service:** plan chose role permission mapping + OwnershipGuard, not relaxed anonymous/authenticated endpoints.
5. **App execution blocker:** app is not running and SQL bug exists; these are scenario designs, not executed results.
