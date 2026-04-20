# ContentCore Audit Remediation Plan

## Objective

Remediate the confirmed and likely-high-value risks found during the ContentCore audit without introducing broad refactors or cross-module churn.

## Scope

In scope:
- ContentCore.Domain
- ContentCore.Application
- ContentCore.Infrastructure
- ContentCore.Presentation
- Direct SharedKernel or security touchpoints only where required to enforce ownership, authorization, or infrastructure safety

Out of scope:
- Unrelated module refactors
- Feature expansion beyond the audited gaps
- Large architectural rewrites unless a smaller safe fix is not possible

## Desired End State

- Public callers cannot access inactive category data.
- Arabic and translated content persist correctly in the database.
- Category hierarchy validation is cycle-safe and cannot crash the process.
- Attachment file lifecycle is transactionally safer.
- Attachment mutations enforce ownership or explicit admin-only access.
- Upload validation checks real file signatures, not just extension and MIME type.
- Manual translations are not silently overwritten by auto-translation.
- Translation cache behavior is concurrency-safe enough for production use.
- Endpoints propagate `CancellationToken` correctly.
- Docs and tests reflect the actual API contract.

---

## Priority Order

1. Lock down inactive category exposure.
2. Fix Unicode/localization persistence.
3. Add cycle safety to hierarchy validation.
4. Fix attachment delete/upload transactional consistency.
5. Add attachment ownership and IDOR protection.
6. Harden upload validation with file signature checks.
7. Protect manual translations from auto-overwrite.
8. Tighten translation cache concurrency/uniqueness.
9. Propagate `CancellationToken` through endpoints.
10. Align docs, tests, and regression coverage.

---

## Workstream 1 — Public access to inactive categories

### Problem
Anonymous callers can currently request inactive categories through query parameters.

### Files
- `ContentCore.Presentation/Endpoints/Category/CategoryEndpoints.cs`
- `ContentCore.Application/Queries/Category/ListCategories/ListCategoriesQuery.cs`
- `ContentCore.Application/Queries/Category/ListCategories/ListCategoriesQueryHandler.cs`
- `ContentCore.Application/Queries/Category/GetCategoryById/GetCategoryByIdQuery.cs`
- `ContentCore.Application/Queries/Category/GetCategoryById/GetCategoryByIdQueryHandler.cs`

### Plan
- Make public endpoints always force active-only behavior.
- Introduce a separate admin-only behavior for inactive-category access, either by:
  - split endpoints, or
  - passing caller context and honoring `IncludeInactive` / `ActiveOnly=false` only for authorized admins.
- Ensure cache keys vary by caller visibility if an auth-varied query remains.

### Acceptance criteria
- Anonymous callers cannot retrieve inactive categories by any parameter combination.
- Admin callers can still retrieve inactive categories through an explicit authorized flow.
- Cached public and admin views cannot collide.

### QA scenario
- **Tool:** Swagger or curl/Postman with anonymous + admin tokens
- **Steps:**
  1. Call `GET /categories?isActive=false` anonymously.
  2. Call `GET /categories/{inactiveCategoryId}?includeInactive=true` anonymously.
  3. Repeat both calls with an admin token against the admin-authorized path/behavior.
- **Expected results:**
  - Anonymous list call returns `200 OK` and contains no inactive categories.
  - Anonymous single-item call returns `404 Not Found` for an inactive category.
  - Admin-authorized calls return `200 OK` and include inactive category data.
  - Public and admin responses do not share stale cache results.

---

## Workstream 2 — Unicode/localization persistence fix

### Problem
Arabic and translated text fields are configured as non-Unicode.

### Files
- `ContentCore.Infrastructure/Persistence/Configurations/CategoryConfiguration.cs`
- `ContentCore.Infrastructure/Persistence/Configurations/CategoryTranslationConfiguration.cs`
- `ContentCore.Infrastructure/Persistence/Configurations/LanguageConfiguration.cs`
- `ContentCore.Infrastructure/Persistence/Configurations/TagConfiguration.cs`
- `ContentCore.Infrastructure/Persistence/Configurations/SpecializationConfiguration.cs`
- new EF migration in `ContentCore.Infrastructure/Persistence/Migrations/`

### Plan
- Keep ASCII-only fields non-Unicode: slugs, codes, icons where appropriate.
- Change user-facing text fields to Unicode:
  - category/tag/specialization names
  - specialization descriptions
  - language `Name` and `NativeName`
  - category translation `Name`
- Generate a migration to alter the affected columns.
- Verify no conflicting assumptions exist in DTOs, validators, or search logic.

### Acceptance criteria
- Arabic text round-trips correctly through create/update/read flows.
- No slug/code rules regress.
- Migration is safe and deterministic.

### QA scenario
- **Tool:** Swagger or integration test + SQL verification
- **Steps:**
  1. Create or update a language with Arabic `NativeName`.
  2. Create/update category, tag, and specialization records using Arabic text.
  3. Read the entities back through the API and verify persisted DB values.
- **Expected results:**
  - Create/update requests return `200 OK` or `201 Created` as appropriate.
  - API responses return the exact Arabic text that was submitted.
  - DB assertions confirm the stored values match exactly and are not garbled or truncated.

---

## Workstream 3 — Category hierarchy cycle safety

### Problem
`GetSubtreeHeightAsync` is recursive and not cycle-safe.

### Files
- `ContentCore.Infrastructure/Services/CategoryHierarchyService.cs`
- `ContentCore.Application/Commands/Category/UpdateCategory/UpdateCategoryCommandHandler.cs`

### Plan
- Add cycle detection using a visited set.
- Prefer iterative traversal if it simplifies reasoning and avoids recursion depth risk.
- Keep explicit max traversal guard as a second safety net.
- Verify all hierarchy checks use the hardened service rather than duplicating logic.

### Acceptance criteria
- Cyclic category data cannot cause stack overflow.
- Invalid parent reassignment is rejected with a stable business error.
- Existing valid 3-level behavior remains intact.

### QA scenario
- **Tool:** `dotnet test` integration test with seeded cyclic data
- **Steps:**
  1. Seed a cycle in a test database fixture (for example `A -> B -> A`).
  2. Execute the category parent-change/update path that evaluates subtree height.
  3. Execute the same path on a valid non-cyclic 3-level hierarchy.
- **Expected results:**
  - Cyclic input returns `409 Conflict` with a stable business error code such as `Category.InvalidParentHierarchy`.
  - The process does not crash or hang.
  - Valid 3-level hierarchy updates still succeed with `200 OK`.

---

## Workstream 4 — Attachment file lifecycle consistency

### Problem
Physical file deletion can happen before DB commit, and uploads can orphan files if DB persistence fails.

### Files
- `ContentCore.Domain/Entities/Attachment.cs`
- `ContentCore.Application/Commands/Attachment/DeleteAttachment/DeleteAttachmentCommandHandler.cs`
- `ContentCore.Application/Commands/Attachment/UploadAttachment/UploadAttachmentCommandHandler.cs`
- `ContentCore.Infrastructure/EventHandlers/AttachmentDeletedDomainEventHandler.cs`
- `ContentCore.Infrastructure/Services/LocalFileStorageService.cs`
- optionally `YallaJo.SharedKernel.Application/Abstractions/Storage/IFileStorageService.cs`

### Plan
- Remove pre-commit physical deletion from the current domain-event flow.
- Move delete side effects to a post-commit step or durable async cleanup mechanism.
- Add best-effort cleanup for uploaded files when DB save fails after storage upload.
- Decide whether deletion should use a storage key instead of deriving path from URL.

### Acceptance criteria
- DB rollback cannot leave a deleted file behind a live attachment record.
- Failed upload persistence cannot silently leak files without cleanup/logging.
- Delete flow remains observable and retryable.

### QA scenario
- **Tool:** `dotnet test` integration tests with local file storage and forced persistence failure
- **Steps:**
  1. Run a test that triggers attachment delete while forcing DB save failure.
  2. Run a test that uploads a file and forces DB save failure after physical upload.
  3. Inspect DB state and file-system state after each failure path.
- **Expected results:**
  - Failed delete leaves the physical file intact and the DB record intact.
  - Failed upload leaves no orphaned file, or logs the cleanup failure explicitly if cleanup also fails.
  - Successful delete removes both DB record and physical file.

---

## Workstream 5 — Attachment ownership / IDOR protection

### Problem
Attachment endpoints and handlers rely on permissions but do not clearly enforce entity ownership.

### Files
- `ContentCore.Presentation/Endpoints/Attachment/AttachmentEndpoints.cs`
- `ContentCore.Application/Commands/Attachment/UploadAttachment/UploadAttachmentCommandHandler.cs`
- `ContentCore.Application/Commands/Attachment/DeleteAttachment/DeleteAttachmentCommandHandler.cs`
- `ContentCore.Application/Commands/Attachment/ReorderAttachments/ReorderAttachmentsCommandHandler.cs`
- `ContentCore.Application/Commands/Attachment/SetPrimaryImage/SetPrimaryImageCommandHandler.cs`
- any required SharedKernel/security abstraction introduced for ownership checks

### Plan
- Decide the intended access model explicitly:
  - admin-only for attachment mutation, or
  - provider/user ownership-aware mutation.
- Enforce ownership in handlers, not only at endpoint policy level.
- Validate that the target entity exists and is mutable by the current caller.
- Fail with explicit forbidden/not-found semantics to avoid leaking data.

### Acceptance criteria
- A caller cannot mutate attachments for an entity they do not own.
- Nonexistent entity references are rejected before attachment creation.
- Logs/errors are clear without exposing sensitive ownership data.

### QA scenario
- **Tool:** Swagger or Postman with two non-admin identities plus admin identity
- **Steps:**
  1. Use caller A to create or reference an entity with an attachment.
  2. Use caller B to attempt upload, delete, reorder, and set-primary operations against caller A's entity/attachment IDs.
  3. Use admin identity to perform the same operations where allowed.
- **Expected results:**
  - Unauthorized cross-entity mutations return `403 Forbidden`.
  - Upload to a nonexistent entity returns `404 Not Found`.
  - Admin path succeeds with `200 OK`/`201 Created` where intended.

---

## Workstream 6 — Upload validation hardening

### Problem
Validation checks extension and MIME type only; file signatures are not verified.

### Files
- `ContentCore.Application/Commands/Attachment/UploadAttachment/UploadAttachmentCommandValidator.cs`
- `ContentCore.Application/Commands/Attachment/UploadAttachment/UploadAttachmentCommandHandler.cs`
- `ContentCore.Infrastructure/Services/LocalFileStorageService.cs`
- any new helper/service for file signature inspection

### Plan
- Add signature/header validation for supported file types.
- Reconcile size limits with documented product rules by media type.
- Reassess whether SVG should be allowed as-is or sanitized/restricted.
- Keep validation centralized and reusable so other storage providers inherit the same protections.

### Acceptance criteria
- Spoofed files cannot pass validation using only renamed extensions or fake MIME types.
- Size rules are explicit per supported file class.
- Unsafe formats are either sanitized or rejected.

### QA scenario
- **Tool:** curl/Postman with crafted files + automated validation tests
- **Steps:**
  1. Attempt upload of a renamed non-image file with an allowed image extension/MIME type.
  2. Attempt upload of an oversized file for each supported class.
  3. Attempt upload of SVG or any format that requires sanitization/restriction.
- **Expected results:**
  - Spoofed file content returns `400 Bad Request` or validation problem with a specific file-signature error.
  - Oversized uploads are rejected with validation errors.
  - Disallowed or unsafe formats are rejected consistently.

---

## Workstream 7 — Protect manual translations

### Problem
Category updates can overwrite manual translations through the auto-translation event flow.

### Files
- `ContentCore.Domain/Entities/Category.cs`
- `ContentCore.Infrastructure/EventHandlers/CategoryUpdatedDomainEventHandler.cs`
- translation-related application handlers/endpoints if status metadata is needed

### Plan
- Introduce or honor translation provenance/status before rewriting existing translations.
- Re-translate only when allowed:
  - missing translations
  - auto-generated translations
  - explicitly requested overwrite paths
- Avoid retranslation for irrelevant updates.

### Acceptance criteria
- Human-reviewed/manual translations survive normal category updates.
- Auto-translation still fills missing language coverage.
- External translation calls are reduced to necessary cases only.

### QA scenario
- **Tool:** `dotnet test` integration test with mocked translator
- **Steps:**
  1. Seed a category with one manual/human-reviewed translation and one auto-generated translation.
  2. Update the source category name.
  3. Observe the resulting translations and translator invocation count.
- **Expected results:**
  - Human-reviewed translation remains unchanged.
  - Auto-generated translation is updated only when allowed.
  - Translator is not invoked for languages that should be preserved untouched.

---

## Workstream 8 — Translation cache concurrency and duplication

### Problem
Concurrent translation requests may duplicate cache rows or external translation work.

### Files
- `ContentCore.Domain/Entities/TranslationCache.cs`
- `ContentCore.Infrastructure/Persistence/Configurations/TranslationCacheConfiguration.cs`
- `ContentCore.Infrastructure/Repositories/TranslationCacheRepository.cs`
- `ContentCore.Infrastructure/Services/AutoSaveTranslationService.cs`
- `ContentCore.Infrastructure/Services/AzureTranslateService.cs`

### Plan
- Review cache identity strategy for uniqueness under concurrency.
- Add a durable uniqueness mechanism if missing, likely via normalized key/hash.
- Ensure repository/service logic handles duplicate-insert races gracefully.
- Keep query performance acceptable for hot translation lookups.

### Acceptance criteria
- Repeated concurrent requests for the same translation do not create duplicate cache rows.
- External translation calls are not multiplied unnecessarily under load.
- Cache lookups remain index-friendly.

### QA scenario
- **Tool:** `dotnet test` concurrency/integration test with mocked translation provider
- **Steps:**
  1. Fire multiple parallel translation requests for the exact same source text and language pair.
  2. Wait for all requests to complete.
  3. Inspect translation cache row count and mocked provider invocation count.
- **Expected results:**
  - Only one cache row exists for the normalized translation key.
  - Provider invocation count is `1` for the duplicated concurrent request burst.
  - No duplicate-key or race exceptions escape to the caller.

---

## Workstream 9 — Endpoint cancellation propagation

### Problem
Presentation endpoints do not consistently pass `CancellationToken` into MediatR calls.

### Files
- `ContentCore.Presentation/Endpoints/**/*.cs`

### Plan
- Add `CancellationToken ct` to all endpoint delegates.
- Pass `ct` into every `sender.Send(..., ct)` call.
- Verify any stream/file operations also receive the same token.

### Acceptance criteria
- Request aborts propagate correctly to handlers and downstream IO.
- No endpoint in ContentCore omits cancellation propagation.

### QA scenario
- **Tool:** `dotnet test` integration test with cancellable request
- **Steps:**
  1. Execute a ContentCore endpoint request with a cancellation token.
  2. Cancel the token while the handler is running.
  3. Verify that no follow-up side effects occur after cancellation.
- **Expected results:**
  - The request terminates as canceled rather than completing normally.
  - Handler and downstream IO receive the same cancellation signal.
  - No partial mutation is committed after cancellation.

---

## Workstream 10 — Contract alignment, docs, and tests

### Problem
There is drift between the documented API/business rules and current implementation.

### Files
- `Agents/agent-context.md`
- `Agents/error-log.md`
- `Agents/guide.md`
- `Agents/YallaJo.md`
- ContentCore endpoint/request/response files
- relevant test projects under `tests/`

### Plan
- Reconcile current API behavior with the documented contract for:
  - attachments routes/visibility
  - category inactive visibility
  - upload constraints
  - translation lifecycle expectations
- Add or update tests for every confirmed issue fixed in this plan.
- Add regression coverage for the exact bugs found in the audit.
- Update project docs only after code behavior is final.

### Acceptance criteria
- Spec and implementation no longer disagree on ContentCore behavior.
- Each fixed issue has regression coverage.
- Audit learnings are reflected in docs and error log where appropriate.

### QA scenario
- **Tool:** Swagger/OpenAPI generation + `dotnet test`
- **Steps:**
  1. Generate or inspect the final Swagger/OpenAPI output for ContentCore endpoints.
  2. Compare routes, auth requirements, and request/response shapes with the updated docs.
  3. Run the regression suite covering all confirmed audit fixes.
- **Expected results:**
  - Swagger matches the final implemented routes and auth behavior.
  - Docs no longer contradict runtime behavior for the remediated areas.
  - Regression suite passes and explicitly covers every confirmed audit finding.

---

## Recommended Execution Sequence

### Phase A — Security and correctness blockers
1. Workstream 1
2. Workstream 3
3. Workstream 5
4. Workstream 6

### Phase B — Data safety and persistence
5. Workstream 2
6. Workstream 4
7. Workstream 8

### Phase C — Translation behavior and polish
8. Workstream 7
9. Workstream 9
10. Workstream 10

---

## Validation Strategy

For each workstream:
- run `lsp_diagnostics` on changed ContentCore projects/files
- run targeted unit/integration tests for the touched behavior
- run `dotnet build` with zero new errors

Recommended regression tests:
- anonymous category queries cannot reveal inactive categories
- Arabic names persist and round-trip correctly
- cyclic category graph cannot crash update validation
- failed DB save after upload cleans up or logs orphaned file path
- failed DB save after delete does not remove live file prematurely
- provider/user cannot mutate another entity’s attachments
- spoofed file content is rejected despite allowed extension/MIME
- manual translations survive normal category updates
- duplicate translation cache writes are prevented under concurrency
- endpoint cancellation cancels handler execution

---

## Open Decisions To Resolve Before Coding

1. Should attachment mutation be admin-only, or ownership-aware for providers/users?
2. Should attachment deletion be synchronous post-commit or async via durable cleanup job?
3. What translation status model should be canonical for “human-reviewed” protection?
4. What is the exact allowed media matrix and per-type size limit for ContentCore uploads?
5. Should attachment storage move toward storage-key-based deletion instead of URL-derived deletion?

---

## Deliverables

- code fixes for the prioritized workstreams
- EF migration for Unicode changes and any new DB constraints
- regression tests covering each confirmed issue
- updated docs reflecting final behavior
