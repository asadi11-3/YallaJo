# YallaJo — Agent Error Log

> **Purpose**: Every AI agent MUST read this file at the start of each session and append to it when errors occur during work. This is how agents learn from each other's mistakes.

> **Rules**: Append-only. Never delete entries. Never edit past entries. Number sequentially.

---

## How to Use This File

1. **Read ALL entries below before writing any code** — these are mistakes previous agents made
2. **When you hit an error during work**, add a new entry IMMEDIATELY (before fixing)
3. **After fixing**, update your entry with the root cause and prevention rule
4. **Pay special attention to the "Prevention Rule"** field — these are the rules that matter most

---

## Error Log

### ERR-001: BusinessHours unique index prevents split-shift support
- **Date**: 2026-03-17
- **Module**: ContentPlaces
- **What Happened**: Planning the BusinessHours batch upsert endpoint. Discovered that `BusinessHoursConfiguration` had `HasIndex(x => new { x.BusinessId, x.DayOfWeek }).IsUnique()` which only allows one row per (BusinessId, DayOfWeek). But `YallaJo.md` explicitly requires split-shift support (two entries per day).
- **Error Message**: Schema conflict — DB enforces uniqueness per day, spec requires multiple entries per day.
- **Root Cause**: The EF config was written before the split-shift business rule was finalized. The unique index was a naive constraint that hardcoded the one-row-per-day assumption.
- **Fix Applied**: Removed unique index from `BusinessHoursConfiguration.cs`. Added regular index on `BusinessId` only. Added design-time factory `ContentPlacesDbContextFactory.cs`. Generated migration `UpdateBusinessHoursAllowSplitShifts`. Overlap validation moved to `SetBusinessHoursCommandValidator` (application layer).
- **Prevention Rule**: Before implementing any batch-replace entity, read the EF config carefully and cross-check the unique indexes against the spec. If the spec says "multiple entries per parent key", the DB index must allow it.

### ERR-002: ReorderCategoriesCommand and Handler were file-truncated (corrupted)
- **Date**: 2026-03-17
- **Module**: ContentCore
- **What Happened**: `dotnet build` failed with 8 errors in `ReorderCategoriesCommand.cs` and `ReorderCategoriesCommandHandler.cs`. Both files were truncated mid-content — the command record was missing its body, the handler was missing the IRequestHandler interface and the Handle method signature.
- **Error Message**: `CS1026: ) expected`, `CS1514: { expected`, `CS1044: Cannot use more than one type in a for statement`, etc.
- **Root Cause**: The files were likely written by a previous agent session that was interrupted or ran out of context, leaving the files in a partial/corrupt state.
- **Fix Applied**: Restored `ReorderCategoriesCommand.cs` with `record ReorderCategoriesCommand(IReadOnlyList<CategoryOrderItem> Items) : ICommand`. Restored handler using `categoryRepository.GetAllAsync(filter: c => ids.Contains(c.Id), asNoTracking: false)` and `category.SetSortOrder(newOrder)`.
- **Prevention Rule**: Before running any `dotnet ef migrations` command, always run `dotnet build` first and fix ALL errors. Do not attempt migrations on a broken build. If a file looks suspiciously short (< 10 lines for a handler), check it before proceeding.

### ERR-003: Missing design-time factory prevents EF migrations on ContentPlaces
- **Date**: 2026-03-17
- **Module**: ContentPlaces
- **What Happened**: Ran `dotnet ef migrations add` for ContentPlaces.Infrastructure. Got error: "Unable to create a 'DbContext' of type 'RuntimeType'. The exception 'Unable to resolve service for type DbContextOptions`1[ContentPlacesDbContext]' was thrown."
- **Error Message**: `Unable to create a 'DbContext' of type 'RuntimeType'`
- **Root Cause**: `ContentPlacesDbContext` had no `IDesignTimeDbContextFactory<T>` implementation. EF tools need this at design time to construct the DbContext independently of the DI container.
- **Fix Applied**: Created `ContentPlaces.Infrastructure/Persistence/ContentPlacesDbContextFactory.cs` extending `ModuleDesignTimeDbContextFactoryBase<ContentPlacesDbContext>`. This is the SharedKernel base class designed exactly for this purpose.
- **Prevention Rule**: Every module that needs `dotnet ef migrations` MUST have a design-time factory extending `ModuleDesignTimeDbContextFactoryBase<TContext>` in its Infrastructure/Persistence folder. Check for this file before running any EF CLI commands.

<!-- 
### ERR-001: {Short descriptive title}
- **Date**: {YYYY-MM-DD}
- **Module**: {Which module were you working on}
- **What Happened**: {What you did that caused the error — be specific}
- **Error Message**: {Exact error message or symptom}
- **Root Cause**: {WHY it happened — the actual underlying reason}
- **Fix Applied**: {What you did to fix it}
- **Prevention Rule**: {A concrete rule future agents must follow to avoid this}
-->

### ERR-004: Message-only Result overloads silently drop error detail from HTTP responses
- **Date**: 2026-03-17
- **Module**: ContentCore (all modules affected)
- **What Happened**: 20+ handlers used `.NotFound("message string")`, `.Conflict("message string")` overloads. These store the message in `result.Messages` (not `result.Errors`). `ToProblem` in the endpoint file only read from `result.Errors`, so `title` and `detail` in the HTTP ProblemDetails response were always `null`. Client got a 404/409 with zero context.
- **Error Message**: No compile error — silent runtime bug. Client receives `{"status": 404}` with no `title` or `detail`.
- **Root Cause**: `Result<T>` has two separate storage slots: `Messages` (string list) and `Errors` (Error record list). The message-only overloads like `.NotFound(string? message)` populate `Messages`, not `Errors`. The `ToProblem` helper only read `Errors`. Two paths for failure responses existed but only one was wired.
- **Fix Applied**: (1) Replaced all message-only overloads with `Result.Failure(new Error("{Entity}.{Reason}", "message"), Outcome.XYZ)` throughout ContentCore. (2) Updated `ToProblem` in `ContentCoreEndpoints.cs` to check `Errors` first, fall back to `Messages[0]` as detail.
- **Prevention Rule**: NEVER use `.NotFound("string")`, `.Conflict("string")`, `.Unauthorized("string")` etc. in handlers. ALWAYS use `Result.Failure(new Error("{Entity}.{Reason}", "message"), Outcome.XYZ)` or the typed overload `Result<T>.Conflict(new Error(...))`. Error codes MUST follow `{Entity}.{Reason}` format. Run a grep for `Result.*\.NotFound\(\"` in any module before considering it complete.

### ERR-005: No try/catch in Application handlers — concurrency conflicts and cancellations propagate as unhandled 500s
- **Date**: 2026-03-17
- **Module**: ContentCore (all modules affected)
- **What Happened**: All 36 ContentCore handlers had no try/catch. `DbUpdateConcurrencyException` (RowVersion conflict) would propagate as an unhandled exception → global handler returns a generic 500. `OperationCanceledException` when the client disconnects would also bubble up rather than returning a clean 499. External service handlers (TranslateText, BatchTranslate) had no `HttpRequestException` handling — a translation API failure would return 500 with no useful error code.
- **Error Message**: No compile error. Runtime: `DbUpdateConcurrencyException` uncaught → `500 Internal Server Error`. `OperationCanceledException` uncaught → `499` (global handler) but no `Request.Cancelled` code in response.
- **Root Cause**: Handlers were written following the Result pattern correctly for business errors but the infrastructure-level exception patterns from `Agents/patterns/error-handling-patterns.md` were never applied.
- **Fix Applied**: Added try/catch to all 36 handlers using 3 patterns: (A) Command handlers: outer `OperationCanceledException when ct.IsCancellationRequested` + inner `DbUpdateConcurrencyException` around `SaveChangesAsync`. (B) Query handlers: outer `OperationCanceledException` only. (C) External service handlers: `HttpRequestException` + `TaskCanceledException when !ct.IsCancellationRequested` + `OperationCanceledException`.
- **Prevention Rule**: Every handler in EVERY module MUST have try/catch per the patterns in `Agents/patterns/error-handling-patterns.md`. Command handlers: always wrap `SaveChangesAsync` in `DbUpdateConcurrencyException` catch. All handlers: always wrap body in `OperationCanceledException when ct.IsCancellationRequested` catch. External service callers in Application layer: add `HttpRequestException` + `TaskCanceledException when !ct.IsCancellationRequested`. Add `using Microsoft.EntityFrameworkCore;` when using `DbUpdateConcurrencyException`.

### ERR-006: ContentPlaces Application handlers used `DbUpdateConcurrencyException` directly — missing package reference
- **Date**: 2026-04-16
- **Module**: ContentPlaces
- **What Happened**: Multiple ContentPlaces.Application command handlers (`ResubmitBusiness`, `SuspendBusiness`, `SetBusinessHours`) used `catch (DbUpdateConcurrencyException)` directly. The Application layer `.csproj` does NOT reference `Microsoft.EntityFrameworkCore`, causing `CS0234`/`CS0246` build errors.
- **Error Message**: `CS0234: The type or namespace name 'EntityFrameworkCore' does not exist` / `CS0246: 'DbUpdateConcurrencyException' could not be found`
- **Root Cause**: ContentPlaces deliberately wraps EF concurrency in `ContentPlacesConcurrencyException` (domain exception) so the Application layer has no EF dependency. The pre-existing UnitOfWork catches `DbUpdateConcurrencyException` and re-throws `ContentPlacesConcurrencyException`. Handlers must catch the domain exception, not the EF one.
- **Fix Applied**: Replaced `catch (DbUpdateConcurrencyException)` with `catch (ContentPlacesConcurrencyException)` and added `using ContentPlaces.Domain.Exceptions`. Also removed `using Microsoft.EntityFrameworkCore` from handlers.
- **Prevention Rule**: In ContentPlaces (and any module that uses a custom concurrency wrapper), NEVER catch `DbUpdateConcurrencyException` in Application handlers. Always catch `{Module}ConcurrencyException` from the Domain.Exceptions namespace. Check if a module has a custom concurrency exception before writing handler catch blocks.

### ERR-007: `Result.Conflict(new Error(...))` — wrong overload, silently compiles but fails at runtime in some contexts
- **Date**: 2026-04-16
- **Module**: ContentPlaces
- **What Happened**: Several handlers used `Result.Conflict(new Error("Business.X", "msg"))`. `Result.Conflict()` only accepts `string?`, not an `Error` record. Caused `CS1503: Argument 1: cannot convert from Error to string?` in the Application layer.
- **Error Message**: `CS1503: Argument 1: cannot convert from 'YallaJo.SharedKernel.Domain.Abstractions.Results.Error' to 'string?'`
- **Root Cause**: Two `Conflict` overloads exist on `Result`: `Conflict(string? message)` and the generic `Result<T>.Conflict(Error error)`. When called on the non-generic `Result`, only the string overload exists.
- **Fix Applied**: Changed all occurrences to `Result.Failure(new Error("Business.X", "msg"), Outcome.Conflict)`.
- **Prevention Rule**: On the non-generic `Result` class, ALWAYS use `Result.Failure(new Error(...), Outcome.Conflict)` — NOT `Result.Conflict(new Error(...))`. The `Conflict(Error)` overload only exists on `Result<T>`.

### ERR-008: Agent made Domain entity state-machine methods return `Result` — violates guide.md architecture rule
- **Date**: 2026-04-16
- **Module**: ContentPlaces
- **What Happened**: Refactored `Business.Approve/Reject/Resubmit/Suspend/Reinstate` to return `Result` instead of `void`+throw, believing it was cleaner. This violates the explicit rule in `agent-context.md` §Error Handling Rules - Domain Layer: "MUST NOT return `Result<T>` from entity methods — entities return `void` or the entity itself. Domain has no dependency on Application abstractions."
- **Error Message**: No build error (Domain references SharedKernel.Domain which has Result). Caught during self-review against guide.md.
- **Root Cause**: Agent incorrectly generalized the Result pattern to the Domain layer. The rule exists because domain entities express business state through throws (`InvalidOperationException`) for programming/invariant violations, while `Result` is an Application-layer concern for user-facing error paths.
- **Fix Applied**: Reverted all 5 methods back to `void` + `throw InvalidOperationException`. Restored state-guard checks inside each Application handler before calling the domain method.
- **Prevention Rule**: NEVER add `Result`, `Error`, or `Outcome` return types to Domain entity methods. Domain entity methods are `void` (state transitions) or return primitives/the entity. Business-rule violations in the domain use `throw InvalidOperationException`. State machine guards belong in BOTH the handler (for early `Result.Failure` return) AND the domain (as a throw for programming error protection).

### ERR-009: `UpdateLanguageCommandHandler` fires `LanguageActivatedDomainEvent` even when language is already active — duplicate outbox writes
- **Date**: 2026-04-17
- **Module**: ContentCore
- **What Happened**: `UpdateLanguageCommandHandler` calls `language.Activate()` unconditionally when `request.IsActive == true`. `Language.Activate()` always raises `LanguageActivatedDomainEvent`, which writes an `OutboxMessage` row. If the language was already active, the event fires anyway — triggering a duplicate backfill of all Place/Business translations.
- **Root Cause**: No state guard before calling `Activate()`/`Deactivate()`. The domain method raises the event without checking if the state actually changed.
- **Fix Applied**: Add state change guards in the handler: `if (request.IsActive && !language.IsActive) language.Activate(); else if (!request.IsActive && language.IsActive) language.Deactivate();`
- **Prevention Rule**: Before calling any domain method that raises a domain event (e.g., `Activate()`, `Approve()`, `Deactivate()`), ALWAYS check that the entity is NOT already in the target state. Calling state-change methods idempotently (without the guard) causes duplicate domain events → duplicate outbox rows → duplicate integration events → duplicate downstream work. Pattern: `if (entity.Status != targetStatus) entity.ChangeStatus(...)`.

### ERR-010: `DeleteAttachmentCommandHandler` uses coarse cache tag `"attachments"` — evicts ALL entity attachment caches system-wide
- **Date**: 2026-04-17
- **Module**: ContentCore
- **What Happened**: `DeleteAttachmentCommandHandler` calls `RemoveByTagAsync("attachments")`. While functionally it does evict the correct cache entries (via the coarse tag), it also evicts attachment lists for ALL entities in the system — not just the entity whose attachment was deleted. This is an unnecessary cache stampede: all entities will miss the cache on their next attachment request, causing N DB queries.
- **Root Cause**: Used the broadest possible tag (`"attachments"`) instead of the fine-grained entity-specific tag (`"attachments:{EntityType}:{EntityId}"`).
- **Fix Applied**: Replace `RemoveByTagAsync("attachments")` with `RemoveByTagAsync($"attachments:{attachment.EntityType}:{attachment.EntityId}")` + `RemoveByTagAsync($"attachment:{request.AttachmentId}")`.
- **Prevention Rule**: Always use the MOST SPECIFIC cache tag available for invalidation. Coarse tags (`"attachments"`, `"categories"`) should only be evicted by operations that truly affect ALL instances (e.g., a schema change or bulk delete). Single-entity mutations must evict only `$"{entity}:{id}"` fine-grained tags. Before writing `RemoveByTagAsync("coarse-tag")`, ask: "Does this mutation affect ALL entries with this tag, or just one?"

### ERR-011: Multiple ContentCore handlers missing `ILogger<THandler>` — guide rule violated across 21 files
- **Date**: 2026-04-17
- **Module**: ContentCore
- **What Happened**: Full audit found 21 handler files (commands and queries) that do not inject `ILogger<THandler>`. This violates the mandatory rule in `agent-context.md`: "Add `ILogger<THandler>` to every handler."
- **Root Cause**: Handlers were written before the ILogger rule was strictly enforced, or the rule was overlooked during review. Query handlers are often forgotten because they "just read data."
- **Fix Applied**: Add `ILogger<THandler>` to all 21 affected files (see ContentCore-fixes-required.md §BUG-005 for the complete list).
- **Prevention Rule**: ILogger is MANDATORY in every handler — commands AND queries. This is on the Completion Checklist and the New Entity Checklist. Query handlers are NOT exempt. Add ILogger to the primary constructor and log at least: (1) entry with request parameters for commands, (2) result count for list queries, (3) success/failure for mutating commands.

### ERR-012: `ListCategoriesQueryHandler.BuildNode` has no cycle detection — stack overflow on circular parent references in DB
- **Date**: 2026-04-17
- **Module**: ContentCore
- **What Happened**: The recursive `BuildNode()` method in `ListCategoriesQueryHandler` has no visited-set or depth limit. If the DB contains a circular parent reference (Category A → parent B → parent A), the recursion never terminates, causing a `StackOverflowException` that crashes the process.
- **Root Cause**: Recursive tree builders always need cycle detection. The DB has a unique index on `(Id)` but no DB-level constraint preventing circular parent chains.
- **Fix Applied**: Add a `HashSet<Guid> visited` parameter to `BuildNode()`. Before recursing into children, check `if (!visited.Add(category.Id)) return leaf node;`
- **Prevention Rule**: Any recursive method that traverses user-supplied or DB-supplied graph data MUST have cycle detection via a `HashSet<T>` visited set AND/OR a max-depth limit. Never assume the data is a clean tree just because the schema suggests it should be.

### ERR-013: Registration email flow marked inbox processed before SMTP success — users saw success while no verification email was sent
- **Date**: 2026-04-21
- **Module**: Auth
- **What Happened**: Registration returned `"Registration successful. A verification email has been sent."`, but `UserCreatedIntegrationEventHandler` persisted the OTP, marked the inbox message processed, saved, then swallowed any `IEmailService.SendAsync` exception. If Gmail/SMTP failed, the user got a success response and no retry happened because the outbox message was already considered processed.
- **Error Message**: User-facing symptom: registration succeeds but verification email never arrives. Server logs show SMTP/send exceptions only.
- **Root Cause**: The handler acknowledged completion of an outbox/inbox-driven side effect before the external side effect actually succeeded. Email delivery failure was treated as non-fatal even though the registration UX depends on receiving the OTP.
- **Fix Applied**: `UserCreatedIntegrationEventHandler` now saves the OTP first, sends the email, marks the inbox processed only after a successful send, invalidates the failed OTP on send errors, and rethrows so the outbox retry policy can retry cleanly. `ResendOtpCommandHandler` now also invalidates unsent OTPs and returns `Otp.EmailDeliveryFailed` instead of leaving dead active codes behind.
- **Prevention Rule**: Never mark an inbox/outbox message as processed before the external side effect succeeds. For OTP/email flows: persist the token, attempt delivery, invalidate failed tokens, and let the outbox retry mechanism handle transient delivery failures.

### ERR-014: Gmail app password copied with display spaces caused SMTP auth failure despite valid message data
- **Date**: 2026-04-21
- **Module**: Auth
- **What Happened**: `await client.SendMailAsync(message, ct)` looked like the failing line even though sender, recipient, subject, and body were populated. Investigation found `Gmail:AppPassword` stored as a spaced string (Google UI display format like `xxxx xxxx xxxx xxxx`).
- **Error Message**: SMTP send/authentication failure at `SendMailAsync` with apparently valid message data.
- **Root Cause**: Google displays app passwords in grouped chunks for readability, but `NetworkCredential` requires the compact password with no spaces. The transport credentials were invalid, not the `MailMessage` payload.
- **Fix Applied**: `GmailEmailService` now strips spaces and trims the app password before constructing `NetworkCredential`, validates trimmed sender/recipient addresses with `MailAddress`, and sets a 30-second SMTP timeout.
- **Prevention Rule**: Treat Google app passwords as display-formatted secrets. Normalize by removing spaces and trimming before SMTP authentication, and validate transport settings separately from message payload data.

### ERR-015: Registration returned before Accounts profile existed, causing immediate get-profile 404s
- **Date**: 2026-04-21
- **Module**: Auth / Accounts
- **What Happened**: A newly registered user could successfully authenticate, but `GET /api/v1/accounts/profile` returned `Profile not found.` because normal registration only guaranteed Security user creation. The Accounts profile depended on the integration-event pipeline, so the profile row could arrive later than the first profile request.
- **Error Message**: 404 `NotFound.Profile` / `Profile not found.` immediately after successful registration.
- **Root Cause**: The registration UX required the Accounts profile to exist immediately, but the implementation left that creation to the asynchronous cross-module event flow. The invited-user path already created the profile synchronously, while self-registration did not.
- **Fix Applied**: Added a synchronous `CreateForUserAsync(ProfileCreationRequest)` capability to `IProfileCreationService`, reused shared profile-creation logic in `ProfileCreationService`, and updated `RegisterCommandHandler` to create the Accounts profile before returning success. Conflict outcomes are treated as success so the event-driven fallback remains idempotent.
- **Prevention Rule**: If a follow-up endpoint is expected to work immediately after a successful command, create its required cross-module read-model/data synchronously before returning success, or explicitly design the API contract around eventual consistency.

### ERR-016: `string.StartsWith` overload mismatch caused Accounts.Application build break
- **Date**: 2026-04-21
- **Module**: Accounts.Application
- **What Happened**: While updating `UpdateAvatarCommandValidator` to accept rooted relative URLs, code used `url.StartsWith('/', StringComparison.Ordinal)`.
- **Error Message**: `CS1503: Argument 1: cannot convert from 'char' to 'string'`.
- **Root Cause**: The `StartsWith` overload that accepts `StringComparison` requires a `string`, not a `char`.
- **Fix Applied**: Changed to `url.StartsWith("/", StringComparison.Ordinal)` and re-ran build/tests.
- **Prevention Rule**: When using `StringComparison` with `StartsWith`, always pass a string literal (e.g., `"/"`), never a char literal.
