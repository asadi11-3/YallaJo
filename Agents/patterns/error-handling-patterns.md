# Error Handling Patterns — Code Patterns

> Reference file for `Agents/agent-context.md`. Contains code examples only.
> Rules and decision logic are in the main file — this file is for copy-paste code.

---

## Domain Guard Clauses

```csharp
// ✅ Domain — guard clause for programming error
public static Place Create(string name, string slug)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Place name is required.", nameof(name));
    // ...
}

// ✅ Domain — business rule violation: let the state speak
public void Activate()
{
    if (IsDeleted) throw new InvalidOperationException("Cannot activate a deleted place.");
    IsActive = true;
    MarkUpdated();
}
```

## Application Result Handler Example

```csharp
// ✅ Application — all paths return Result<T>
public async Task<Result<Guid>> Handle(CancelBookingCommand request, CancellationToken ct)
{
    var booking = await repo.GetByIdAsync(request.Id, ct);
    if (booking is null)
        return Result<Guid>.NotFound("Booking.NotFound", $"Booking '{request.Id}' not found.");

    if (booking.Status == BookingStatus.Completed)
        return Result<Guid>.Failure("Booking.InvalidState", "Cannot cancel a completed booking.");

    if (booking.UserId != request.CurrentUserId)
        return Result<Guid>.Unauthorized("Booking.Unauthorized", "You can only cancel your own bookings.");

    booking.Cancel();
    await uow.SaveChangesAsync(ct);
    return Result<Guid>.Success(booking.Id);
}
```

## Result Factory Methods

```csharp
Result<T>.Success(value)          // 200 OK
Result<T>.Created(value)          // 201 Created
Result<T>.NotFound(code, msg)     // 404 Not Found
Result<T>.Conflict(code, msg)     // 409 Conflict
Result<T>.Unauthorized(code, msg) // 401/403
Result<T>.Failure(code, msg)      // 400 Bad Request (generic business failure)
```

## Safe Result Checking Pattern

```csharp
var result = await sender.Send(command, ct);
if (!result.IsSuccess)
    return result.ToApiResult(); // Short-circuit

// Continue with result.Value safely
var dto = result.Value;
```

## Safe .Value Access Pattern

```csharp
// ❌ WRONG — Value throws if IsSuccess is false
var value = result.Value;

// ✅ CORRECT — always guard
if (!result.IsSuccess) return result.ToApiResult();
var value = result.Value; // Safe here
```

## Global Exception Pipeline Flow

```
Unhandled exception thrown anywhere in the request pipeline
  → ASP.NET Core catches it
  → IExceptionHandler middleware runs
  → Logs: full exception with stack trace, inner exceptions, request context
  → Maps known types:
      ValidationException    → 400 ValidationProblemDetails (field-level errors)
      UnauthorizedAccessException → 401
      DbUpdateConcurrencyException → 409 (if not caught in handler)
      OperationCanceledException  → 499 (client disconnected)
      Everything else         → 500 ProblemDetails (generic message, no internal details)
  → Returns RFC 7807 ProblemDetails — NEVER exposes stack trace, SQL, or paths
```

## ProblemDetails JSON Shapes

```json
// Business error (404)
{
  "status": 404,
  "title": "Place.NotFound",
  "detail": "Place 'dead-sea-tour' not found.",
  "traceId": "00-abc123-def456-00"
}

// Validation error (400)
{
  "status": 400,
  "title": "One or more validation errors occurred.",
  "errors": {
    "Name": ["Name is required."],
    "Slug": ["Slug must contain only lowercase letters, digits, and hyphens."]
  }
}

// Unexpected error (500)
{
  "status": 500,
  "title": "An unexpected error occurred.",
  "traceId": "00-abc123-def456-00"
  // NO stack trace. NO SQL. NO file paths. NEVER.
}
```

## Custom Exception Example

```csharp
// ✅ CORRECT — custom exception with context, caught by global handler
public sealed class MediaProcessingException(string message, Guid attachmentId, Exception inner)
    : Exception(message, inner)
{
    public Guid AttachmentId { get; } = attachmentId;
}

// In global handler — maps to 422 Unprocessable Entity
if (exception is MediaProcessingException mpe)
{
    return Results.Problem(
        detail: $"Failed to process media for attachment {mpe.AttachmentId}.",
        statusCode: StatusCodes.Status422UnprocessableEntity);
}
```

## Try/Catch Golden Rule

```csharp
// ❌ WRONG — catching everything, doing nothing meaningful
try { await repo.AddAsync(entity, ct); }
catch (Exception) { }  // Swallowed. No one knows it failed.

// ❌ WRONG — catching and re-throwing loses the stack trace
try { await repo.AddAsync(entity, ct); }
catch (Exception ex) { throw new Exception("Failed", ex); }  // Pointless wrapper.

// ✅ CORRECT — let it propagate. Global handler catches it, logs it, returns 500.
await repo.AddAsync(entity, ct);
```

## Whitelist: External Service try/catch

```csharp
// ✅ In Infrastructure — wrapping external HTTP/API calls
public async Task<Result<TranslationResult>> TranslateAsync(string text, string targetLang, CancellationToken ct)
{
    try
    {
        var response = await _httpClient.PostAsync("/translate", content, ct);
        response.EnsureSuccessStatusCode();
        return Result<TranslationResult>.Success(await ParseResponse(response, ct));
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Translation API call failed for language {Language}", targetLang);
        return Result<TranslationResult>.Failure("Translation.ServiceUnavailable",
            "Translation service is temporarily unavailable.");
    }
    catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
    {
        _logger.LogWarning(ex, "Translation API timed out for language {Language}", targetLang);
        return Result<TranslationResult>.Failure("Translation.Timeout",
            "Translation request timed out.");
    }
    // Do NOT catch general Exception — let unexpected errors propagate
}
```

## Whitelist: Optimistic Concurrency try/catch

```csharp
// ✅ In a command handler that explicitly handles concurrency conflicts
try
{
    await unitOfWork.SaveChangesAsync(ct);
}
catch (DbUpdateConcurrencyException)
{
    return Result<Guid>.Conflict("Entity.ConcurrencyConflict",
        "This record was modified by another user. Please refresh and try again.");
}
```

## Whitelist: Background Worker try/catch

```csharp
// ✅ In BackgroundService — catch per-item to keep the worker alive
while (await _channel.Reader.WaitToReadAsync(ct))
{
    var item = await _channel.Reader.ReadAsync(ct);
    try
    {
        await ProcessItemAsync(item, ct);
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        break; // Graceful shutdown — stop the loop
    }
    catch (Exception ex)
    {
        // Log and continue — do NOT let one item crash the whole worker
        _logger.LogError(ex, "Failed to process media item {ItemId}", item.Id);
    }
}
```

## Whitelist: Cancellation Handling

```csharp
// ✅ Only when you need to differentiate cancellation sources
catch (OperationCanceledException) when (ct.IsCancellationRequested)
{
    // Request was cancelled by the client — ignore silently
    return Result<Guid>.Failure("Request.Cancelled", "Request was cancelled.");
}
```

## Logging in catch Blocks

```csharp
// ✅ CORRECT — logs with full context before converting to Result
catch (HttpRequestException ex)
{
    _logger.LogError(ex,                    // ← exception object FIRST (captures stack trace)
        "External API call failed. Url={Url} StatusCode={StatusCode}",
        requestUrl, ex.StatusCode);         // ← structured log properties
    return Result<T>.Failure("Service.Unavailable", "External service is unavailable.");
}

// ❌ WRONG — no logging
catch (HttpRequestException)
{
    return Result<T>.Failure("Service.Unavailable", "External service is unavailable.");
}

// ❌ WRONG — string interpolation instead of structured logging
catch (HttpRequestException ex)
{
    _logger.LogError($"Call to {url} failed: {ex.Message}"); // Not searchable in prod
    return Result<T>.Failure(...);
}
```

## finally Block Usage

```csharp
// ✅ CORRECT — cleanup in finally
var stream = File.OpenRead(path);
try
{
    await ProcessAsync(stream, ct);
}
finally
{
    await stream.DisposeAsync(); // Always runs, even if exception thrown
}

// ✅ BETTER — use 'using' instead of try/finally for IDisposable
await using var stream = File.OpenRead(path);
await ProcessAsync(stream, ct);

// ❌ WRONG — business logic in finally
try { ... }
finally
{
    await unitOfWork.SaveChangesAsync(ct); // Don't do this — may run after an exception
}
```

## Exception Wrapping Pattern

```csharp
// ✅ CORRECT — preserves original stack trace
catch (Exception ex)
{
    _logger.LogError(ex, "Media processing failed for attachment {AttachmentId}", attachmentId);
    throw new MediaProcessingException("Failed to process media file.", ex); // ex as inner
}

// ❌ WRONG — loses original stack trace
catch (Exception)
{
    throw new MediaProcessingException("Failed to process media file."); // Where did it fail?
}
```
