# ContentPlaces — Quality Review Backlog

**Module:** ContentPlaces
**Source:** Full Module Quality Review (discovery-only pass)
**Purpose:** Record the accepted findings from the ContentPlaces full quality review so they can be scheduled and actioned in future phases. This file is a backlog record only — no application code, migrations, or existing docs were changed as part of creating it.

> Scope note: Only the accepted findings below are tracked as active backlog items. Findings CP-05, CP-06, CP-07, and CP-08 are intentionally **not** tracked as active fix items; the relevant ones are summarized under "Not actioned now".

---

## Active Backlog Items

### CP-01 — Public ServiceItem detail ignores `IsAvailable`
- **Phase:** Security phase
- **Status:** Deferred
- **Priority:** Medium
- **Note:** Needs approval before implementation.
- **Summary:** The public (`AllowAnonymous`) ServiceItem detail endpoint returns a service item by Id without gating on `IsAvailable`, unlike the list endpoint which separates public vs. elevated (owner/admin) visibility. This allows anonymous callers to fetch hidden/unavailable items directly by Id.

### CP-02 — External translation orchestration inside create-path domain event handler
- **Phase:** Reliability / Domain Events phase
- **Status:** Deferred
- **Priority:** Medium
- **Note:** Needs targeted verification before implementation.
- **Summary:** The business-create path invokes the external translation orchestrator synchronously within the same unit of work via a domain event handler. A slow or failing translation service can delay or fail the create transaction even though translation is non-critical enrichment. Verify whether the place-create path shares the same pattern before acting.

### CP-03 — No integration test project for ContentPlaces
- **Phase:** Test Coverage phase
- **Status:** Deferred
- **Priority:** Medium
- **Note:** Additive tests only.
- **Summary:** Unit tests exist, but there is no integration test project exercising EF query filters, filtered unique indexes, the `PlaceBusiness` composite filter, geo (Haversine) SQL, and cascade behavior end-to-end against a real provider.

### CP-04 — `UpdateBusiness` PlaceId relocation needs verification
- **Phase:** Architecture / Domain Rules verification
- **Status:** Needs verification
- **Priority:** Low
- **Note:** Do not implement until business rule is confirmed.
- **Summary:** On a PlaceId change, `UpdateBusiness` validates that the place exists and that the caller owns the business, but does not re-check the create-time provider-type rules or the unique `(PlaceId, BusinessType, OwnerId)` constraint. Confirm whether relocation re-validation is the intended business rule before any change.

---

## Not actioned now

- **SetBusinessHours `ExecuteDeleteAsync`** is treated as a design choice for now.
- **Business detail full collection loading** is a performance optimization for later, not a current bug.
- **ServiceItem cache literal** is low-value cleanup, not worth touching now.
- **Place unique-index nullable `Country` behavior** is intentional design.
