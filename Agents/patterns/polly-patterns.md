# Polly Patterns — Code Patterns

> Reference file for `Agents/agent-context.md`. Contains code examples only.
> Rules and decision logic are in the main file — this file is for copy-paste code.

---

## HttpClient Registration with Standard Resilience

```csharp
// Package: Microsoft.Extensions.Http.Resilience (built on Polly v8 — already in .NET 8+)
// DO NOT install Polly directly — use Microsoft.Extensions.Http.Resilience instead

// In DI (per HttpClient, not globally):
services.AddHttpClient<AzureTranslateService>()
    .AddStandardResilienceHandler(); // Quick default — good for most cases
```

## Retry Policy Configuration

```csharp
// ✅ Custom retry policy for external API
services.AddHttpClient<AzureTranslateService>()
    .AddResilienceHandler("azure-translate", builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            // How many times to retry (NOT counting the original attempt)
            MaxRetryAttempts = 3,

            // Exponential backoff with jitter — prevents thundering herd
            // Delays: ~1s, ~2s, ~4s (with random jitter added)
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,  // ALWAYS true — prevents retry storms

            // Only retry on these status codes (transient errors)
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .HandleResult(r => r.StatusCode is
                    HttpStatusCode.RequestTimeout or      // 408
                    HttpStatusCode.TooManyRequests or     // 429
                    HttpStatusCode.InternalServerError or // 500
                    HttpStatusCode.BadGateway or          // 502
                    HttpStatusCode.ServiceUnavailable or  // 503
                    HttpStatusCode.GatewayTimeout)        // 504
        });
    });
```

## Non-Retryable Statuses Reference

```csharp
// ❌ NEVER retry these — they are NOT transient
// 400 Bad Request  — fix the payload, retry won't help
// 401 Unauthorized — token expired or invalid, retry same request won't work
// 403 Forbidden    — permissions issue, not transient
// 404 Not Found    — resource doesn't exist, retry won't create it
// 409 Conflict     — business conflict, retry will get the same result
// 422 Unprocessable — validation failed, retry won't change validation
```

## Retry-After Header Support

```csharp
builder.AddRetry(new HttpRetryStrategyOptions
{
    MaxRetryAttempts = 3,
    DelayGenerator = static args =>
    {
        // Honor the Retry-After header if present
        if (args.Outcome.Result?.Headers.RetryAfter is { } retryAfter)
        {
            var delay = retryAfter.Delta ?? (retryAfter.Date - DateTimeOffset.UtcNow);
            if (delay > TimeSpan.Zero && delay < TimeSpan.FromMinutes(5))
                return ValueTask.FromResult<TimeSpan?>(delay);
        }
        // Default exponential backoff
        return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber)));
    }
});
```

## Circuit Breaker Configuration

```csharp
builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
{
    // Open circuit after 5 failures in a 30-second window
    MinimumThroughput = 5,
    SamplingDuration = TimeSpan.FromSeconds(30),
    FailureRatio = 0.5,           // Open when 50%+ of requests fail

    // Stay open (reject all requests) for 30 seconds
    BreakDuration = TimeSpan.FromSeconds(30),

    // What counts as a failure
    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
        .Handle<HttpRequestException>()
        .HandleResult(r => r.StatusCode >= HttpStatusCode.InternalServerError),

    // Callback when circuit state changes (MUST log these)
    OnOpened = args =>
    {
        logger.LogError("Circuit breaker OPENED for {ServiceName} — too many failures", "AzureTranslate");
        return ValueTask.CompletedTask;
    },
    OnClosed = args =>
    {
        logger.LogInformation("Circuit breaker CLOSED — {ServiceName} recovered", "AzureTranslate");
        return ValueTask.CompletedTask;
    },
    OnHalfOpened = args =>
    {
        logger.LogInformation("Circuit breaker HALF-OPEN — testing {ServiceName}", "AzureTranslate");
        return ValueTask.CompletedTask;
    }
});
```

## BrokenCircuitException Handling

```csharp
// ✅ In Infrastructure service — catch open circuit and return graceful failure
public async Task<Result<TranslationResult>> TranslateAsync(string text, string lang, CancellationToken ct)
{
    try
    {
        var response = await _httpClient.PostAsync("/translate", content, ct);
        // ...
    }
    catch (BrokenCircuitException ex)
    {
        // Circuit is OPEN — don't even try, return gracefully
        _logger.LogWarning(ex, "Translation circuit is open — returning fallback for language {Language}", lang);
        return Result<TranslationResult>.Failure("Translation.CircuitOpen",
            "Translation service is temporarily unavailable. Please try again later.");
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Translation API failed for language {Language}", lang);
        return Result<TranslationResult>.Failure("Translation.ServiceUnavailable",
            "Translation service is unavailable.");
    }
}
```

## Timeout Policy Configuration

```csharp
builder.AddTimeout(new HttpTimeoutStrategyOptions
{
    // Total time allowed for the request + all retries
    Timeout = TimeSpan.FromSeconds(10)  // Adjust per service SLA
});

// Per-attempt timeout (set INSIDE retry, applied per attempt):
builder.AddRetry(new HttpRetryStrategyOptions
{
    MaxRetryAttempts = 3,
    // Each individual attempt gets 3 seconds max
    // Combined with 10s total timeout above
});
```

## Full Resilience Pipeline Composition

```csharp
// ✅ Production-grade pipeline for critical external services
services.AddHttpClient<PaymentGatewayService>()
    .AddResilienceHandler("payment-gateway", builder =>
    {
        // Order matters: outermost → innermost
        // 1. Total timeout — outermost, caps everything
        builder.AddTimeout(TimeSpan.FromSeconds(30));

        // 2. Retry — retries on transient failure with backoff
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 2,               // Only 2 retries for payments (idempotency risk)
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .HandleResult(r => r.StatusCode is
                    HttpStatusCode.RequestTimeout or
                    HttpStatusCode.ServiceUnavailable or
                    HttpStatusCode.GatewayTimeout)
            // Note: DO NOT retry 429 on payment APIs without Retry-After header
        });

        // 3. Circuit breaker — innermost, trips after sustained failures
        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            MinimumThroughput = 3,
            SamplingDuration = TimeSpan.FromSeconds(60),
            FailureRatio = 0.6,
            BreakDuration = TimeSpan.FromSeconds(60)
        });
    });
```

## EF Core Retry Note

```csharp
// ✅ This is already configured in every module's DI — handles transient SQL errors
sql.EnableRetryOnFailure(
    maxRetryCount: 3,
    maxRetryDelay: TimeSpan.FromSeconds(5),
    errorNumbersToAdd: null); // Retries on known transient SQL error codes

// ❌ NEVER wrap EF SaveChangesAsync or queries in Polly retry
// EF already retries internally. Double-retrying on non-idempotent writes = data corruption.
```

## Polly Logging Callback

```csharp
builder.AddRetry(new HttpRetryStrategyOptions
{
    MaxRetryAttempts = 3,
    OnRetry = args =>
    {
        logger.LogWarning(
            "Retry {AttemptNumber} of {MaxAttempts} for {ServiceName}. " +
            "StatusCode={StatusCode} Delay={Delay}ms",
            args.AttemptNumber,
            3,
            "AzureTranslate",
            (int?)args.Outcome.Result?.StatusCode,
            args.RetryDelay.TotalMilliseconds);
        return ValueTask.CompletedTask;
    }
});
```
