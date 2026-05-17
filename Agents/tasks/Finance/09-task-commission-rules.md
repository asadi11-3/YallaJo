# TASK 6 — Commission Rules CRUD

> **Owner:** Junior Dev (TBD) or Fadwa fallback — **Hours:** 24h — **Hard deadline:** Sun **2026-09-27 17:00**
> **Earliest start:** Wed 2026-08-19 (parallel — Booking module will start consuming `finance.commission-rule.*` events when this ships)
> **Endpoints:** 3
> **Depends on:** PW only

This task ships the admin-only commission-rule CRUD that Booking module consumes via the `BookingCommissionSnapshot` inbox handler documented in Booking 05-task.

---

## 1. Endpoint Surface

| # | Method | Path | Permission | Notes |
|---|---|---|---|---|
| 1 | GET | `/api/v1/commissions` | `CommissionRule.Read` (admin) | Paginated list, filter `tier`, `currency`, `revenueRange` |
| 2 | POST | `/api/v1/commissions` | `CommissionRule.Create` | Admin upserts a rule |
| 3 | PUT | `/api/v1/commissions/{id}` | `CommissionRule.Update` | Admin edits |
| 4 | DELETE | `/api/v1/commissions/{id}` | `CommissionRule.Delete` | Soft delete + emits `finance.commission-rule.deleted.v1` |

(GET is intentionally a single endpoint; 4 total. The master INDEX counts the four endpoints in the same row as "Commission Rules CRUD" = 3 because we group the GET into one row of the manifest.)

---

## 2. CommissionRule Aggregate

```csharp
public sealed class CommissionRule : AuditableEntity, IAggregateRoot
{
    public string Tier { get; private set; }              // "Free", "Basic", "Premium", "Enterprise" — free text v1
    public decimal MinMonthlyRevenue { get; private set; } // inclusive lower bound
    public decimal? MaxMonthlyRevenue { get; private set; } // null = open-ended top tier
    public string Currency { get; private set; }           // ISO 3-letter
    public decimal Percentage { get; private set; }        // 0 < p < 100
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }

    public static CommissionRule Create(string tier, decimal minMonthlyRevenue, decimal? maxMonthlyRevenue, string currency, decimal percentage, string? notes, TimeProvider tp);
    public Result Update(decimal minMonthlyRevenue, decimal? maxMonthlyRevenue, decimal percentage, string? notes);
    public Result Deactivate(string reason);
}
```

Factory raises `CommissionRuleUpsertedDomainEvent`; `Update` raises it again; `Deactivate` raises `CommissionRuleDeletedDomainEvent`.

---

## 3. Validation Rules

- `Tier` not empty, max 50 chars
- `Currency` in {JOD, USD, EUR}
- `MinMonthlyRevenue >= 0`
- `MaxMonthlyRevenue is null OR MaxMonthlyRevenue > MinMonthlyRevenue`
- `0 < Percentage < 100`
- **Overlap detection** (handler-level): for the same `(Tier, Currency)`, no two rules can have overlapping `[Min, Max]` ranges. Error code `CommissionRule.OverlapTier`.

---

## 4. Handler Flow — POST /commissions

```csharp
public async Task<Result<Guid>> Handle(UpsertCommissionRuleCommand cmd, CancellationToken ct)
{
    // Overlap check
    var overlaps = await _ruleRepo.GetOverlappingAsync(cmd.Tier, cmd.Currency, cmd.MinMonthlyRevenue, cmd.MaxMonthlyRevenue, excludeId: null, ct);
    if (overlaps.Count > 0)
        return Result.Failure<Guid>(new Error("CommissionRule.OverlapTier", $"Overlaps with rule {overlaps[0].Id}"), Outcome.Conflict);

    var rule = CommissionRule.Create(cmd.Tier, cmd.MinMonthlyRevenue, cmd.MaxMonthlyRevenue, cmd.Currency, cmd.Percentage, cmd.Notes, _timeProvider);
    await _ruleRepo.AddAsync(rule, ct);
    await _uow.SaveChangesAsync(ct);
    await _cache.RemoveByTagAsync("commission-rules", ct);
    // Outbox emits `finance.commission-rule.upserted.v1` → Booking inbox refreshes BookingCommissionSnapshot
    return Result.Success(rule.Id);
}
```

PUT does the same minus `Create`; uses `Update` method; raises `CommissionRuleUpsertedDomainEvent` again (Booking inbox idempotency handles).

DELETE calls `Deactivate("admin deleted")` → `CommissionRuleDeletedDomainEvent` → outbox `finance.commission-rule.deleted.v1` → Booking removes snapshot.

**Soft delete preserves history** for audit; `IsActive=false` rows excluded from GET unless `?includeInactive=true`.

---

## 5. Default Seed Rules (DataSeeder for clean envs)

`Finance.Infrastructure/Persistence/FinanceDbInitializer.cs` seeds these on fresh DB or empty CommissionRules table:

| Tier | MinRev | MaxRev | Currency | % | Notes |
|---|---|---|---|---|---|
| Free | 0 | null | JOD | 15.00 | Default free tier (PDF 2 §1.5) |
| Free | 0 | null | USD | 15.00 | |
| Free | 0 | null | EUR | 15.00 | |
| Basic | 0 | null | JOD | 10.00 | Reserved for Phase 3 subscription |
| Premium | 0 | null | JOD | 7.00 | Reserved for Phase 3 subscription |

Seeded only on env `Development` and `Staging` — production seed is admin-controlled.

---

## 6. Cache & Tags

| Cache | Key | TTL | Tag |
|---|---|---|---|
| All rules list | `commission-rules:list:{filterHash}` | 5 min | `commission-rules` |
| Single rule | `commission-rule:{id}` | 10 min | `commission-rules`, `commission-rule:{id}` |

Any POST/PUT/DELETE invalidates `commission-rules` tag.

---

## 7. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | `CommissionRule` aggregate + tests | 3 | 2026-08-26 |
| 2 | Migration `FinanceAddCommissionRuleConstraints` (unique index, soft-delete filter) | 1 | 2026-08-27 |
| 3 | `ICommissionRuleRepository` + impl + overlap finder | 3 | 2026-08-31 |
| 4 | Default seed in DbInitializer | 1 | 2026-08-31 |
| 5 | POST endpoint + handler + validator | 4 | 2026-09-03 |
| 6 | PUT endpoint + handler + validator | 3 | 2026-09-07 |
| 7 | DELETE endpoint + handler + soft-delete behavior | 2 | 2026-09-09 |
| 8 | GET (list) endpoint + filter + cache + tag | 3 | 2026-09-14 |
| 9 | Unit tests for overlap detection edge cases | 2 | 2026-09-21 |
| 10 | Integration test: POST commission → outbox row → Booking inbox stub consumes | 1 | 2026-09-23 |
| 11 | PR review | 1 | 2026-09-27 |
| **Total** | | **24h** | |

---

## 8. Edge cases

1. Tier renamed (e.g. "Premium" → "Pro") — out of scope; PUT only changes numeric fields. Use DELETE + POST to rename.
2. New tier introduced (e.g. "Enterprise") with no booking yet → no Booking inbox row, just empty snapshot category; safe.
3. Overlap detection includes the rule itself on PUT → exclude `cmd.Id`.
4. Open-ended top tier (MaxMonthlyRevenue=null) → considered as "Min, +∞"; allowed at most ONE such rule per (Tier, Currency).
5. Currency added later (e.g. SAR) → CommissionRule.Currency validator includes only JOD/USD/EUR this sprint; expansion is future migration.
6. Percentage of 0 → blocked (must be > 0).
7. Booking sprint's BookingCommissionSnapshot consumer fails repeatedly → outbox row stays unprocessed; eventually retries — Booking ops responsibility.
