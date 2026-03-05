# YallaJo Seed Data Initializers Plan

## TL;DR

> **Quick Summary**: Implement Startup DbInitializers across all 13 modules of YallaJo to generate realistic fake data based on the Multi-Vendor Tour & Experience domain. These initializers will be easily removable/disableable in production.
> 
> **Deliverables**:
> - Bogus-based Data Seeders for 13 modules
> - Shared Application Builder Extension to orchestrate seeding order
> - Environment/Config based execution guard (enabled only in Development/testing)
> 
> **Estimated Effort**: Large
> **Parallel Execution**: YES - 5 waves based on data dependencies
> **Critical Path**: Core/Auth/Accounts → Places → Tours → Booking → Finance

---

## Context

### Original Request
Read each module and seed data for it based on the project scenario. User requested "a Startup DbInitializer in files so on production i can remove it".

### Interview Summary
**Key Discussions**:
- **Execution Approach**: Startup DbInitializer classes that can be optionally excluded from production builds or disabled via configuration.
- **Data Reality**: YallaJo is a complex multi-vendor tour & experience booking platform with users, guides, businesses, places, tours, bookings, payments, reviews, and tracking. Data needs to reflect this domain accurately.

**Research Findings**:
- **Module Structure**: 13 distinct modules. Data depends on cross-module relationships (e.g. Tour Bookings need Users and Tours; Tours need Places and Core Categories).
- **Execution Order**: Must follow a strict topological sort to avoid foreign key or logical reference failures.

### Metis Review
**Identified Gaps** (addressed):
- **Cross-Module ID Sharing**: Initializers need to query the database to find valid referenced IDs (e.g. randomly pick a valid User ID when creating a Booking) rather than hardcoding.
- **Idempotency**: Seeders must check if data already exists to avoid duplication on subsequent startups.

---

## Work Objectives

### Core Objective
Implement robust, realistic, domain-specific data seeders for all YallaJo modules that execute on startup and can be disabled in production.

### Concrete Deliverables
- A `SeedDataExtensions` class in `YallaJo.Api` to register and trigger seeders in correct order
- `IDbInitializer` implementations in the Infrastructure layer of each module
- Bogus `Faker` configurations for all core entities

### Definition of Done
- [ ] Running the application in Development mode populates the database with realistic interconnected data (Users -> Places -> Tours -> Bookings).
- [ ] Subsequent startups do not duplicate data (seeders check `.Any()`).
- [ ] No compilation or runtime errors due to missing dependencies.

### Must Have
- Realistic Bogus data (real-sounding names, locations, tour descriptions)
- Strict execution order managed by a central orchestrator in `YallaJo.Api`
- Feature flag or environment check (e.g. `if (app.Environment.IsDevelopment())`)

### Must NOT Have (Guardrails)
- NO raw SQL scripts. Use EF Core `DbContext` to insert data.
- NO EF Core `HasData()` calls in `OnModelCreating` for dynamic Bogus data (causes migration bloat).
- NO seeding in production environments.

---

## Verification Strategy

> **ZERO HUMAN INTERVENTION** — ALL verification is agent-executed. No exceptions.

### Test Decision
- **Infrastructure exists**: YES
- **Automated tests**: Tests-after / Agent-Executed QA
- **QA Policy**: Use interactive_bash / curl / EF Core tools to verify data exists in the database.

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 1 (Base & Identity):
├── Task 1: Scaffolding & Shared Infrastructure [quick]
├── Task 2: ContentCore Module Seeding (Languages, Categories) [unspecified-low]
├── Task 3: Security & Auth Module Seeding (Roles, Users, Tokens) [unspecified-low]
└── Task 4: Accounts Module Seeding (Profiles, Guide Data) [unspecified-low]

Wave 2 (Base Content - depends on W1):
├── Task 5: ContentPlaces Module Seeding (Businesses, Places) [unspecified-high]
└── Task 6: Social Module Seeding (Reviews, Reports) [unspecified-high]

Wave 3 (Core Business - depends on W2):
└── Task 7: ContentTours Module Seeding (Tours, Packages, Schedules) [deep]

Wave 4 (Transactions - depends on W3):
├── Task 8: Booking Module Seeding (Tour Bookings) [deep]
└── Task 9: Finance Module Seeding (Payments, Subscriptions) [unspecified-high]

Wave 5 (Auxiliary - depends on W4):
├── Task 10: ContentBlogs Seeding [unspecified-low]
├── Task 11: Messaging Seeding [unspecified-low]
├── Task 12: Analytics & Tracking Seeding [unspecified-high]
└── Task 13: ContentSeo Seeding & Final Integration [unspecified-low]

Wave FINAL (Review):
├── Task F1: Plan compliance audit (oracle)
├── Task F2: Code quality review (unspecified-high)
├── Task F3: Real manual QA (unspecified-high)
└── Task F4: Scope fidelity check (deep)
```

---

## TODOs

- [ ] 1. Shared Infrastructure & Base Orchestrator

  **What to do**:
  - In `YallaJo.Api` or `YallaJo.SharedKernel.Infrastructure`, create an interface `IModuleDbInitializer` with an async `InitializeAsync()` method.
  - Create an extension method in `YallaJo.Api` (e.g., `AddDataSeeders` and `UseDataSeeders`) that registers all initializers and executes them conditionally based on `app.Environment.IsDevelopment()`.
  - Ensure the executor runs the initializers in the defined sequence (Core -> Auth -> Accounts -> Places -> Tours -> etc.) by explicitly resolving them in order or injecting an ordered `IEnumerable<IModuleDbInitializer>`.

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Creating the base interfaces and the extension method is straightforward infrastructure wiring.
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Sequential (first task)
  - **Blocks**: All subsequent tasks

  **References**:
  - `YallaJo.Api/Program.cs` - Where the extension method will be called.

  **Acceptance Criteria**:
  - [ ] Interface and extension method exist.
  - [ ] Conditional execution logic is correctly implemented (`IsDevelopment()`).

  **QA Scenarios**:
  ```
  Scenario: Extension method compiles and registers
    Tool: interactive_bash
    Preconditions: Extension method added to Program.cs
    Steps:
      1. Run `dotnet build YallaJo.Api`
    Expected Result: Build succeeds with 0 errors
    Failure Indicators: Build errors
    Evidence: .sisyphus/evidence/task-1-build.txt
  ```

- [ ] 2. ContentCore Module Seeding

  **What to do**:
  - In `ContentCore.Infrastructure`, implement `IModuleDbInitializer`.
  - Check if any data exists. If not, seed base Languages (English, Arabic, Spanish), Categories (Adventure, Historical, Culinary), Tags, and Specializations using EF Core `DbContext`.
  - Save changes.

  **Recommended Agent Profile**:
  - **Category**: `unspecified-low`
    - Reason: Simple entity creation and saving without complex Bogus needs.
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocked By**: Task 1

  **References**:
  - `ContentCore.Domain/Entities` - The entities to seed.
  - `ContentCore.Infrastructure/Persistence/ContentCoreDbContext.cs`

  **Acceptance Criteria**:
  - [ ] Idempotency check exists (`.Any()`).
  - [ ] Entities added to DB context and saved.

  **QA Scenarios**:
  ```
  Scenario: ContentCore module data generation logic implemented
    Tool: interactive_bash
    Preconditions: Seeder implementation file exists
    Steps:
      1. Run `grep -i 'AnyAsync' ContentCore.Infrastructure/**/*Initializer.cs`
    Expected Result: Finds the idempotency check
    Failure Indicators: No idempotency check
    Evidence: .sisyphus/evidence/task-2-grep.txt
  ```

- [ ] 3. Security & Auth Module Seeding

  **What to do**:
  - In `Security.Infrastructure` and `Auth.Infrastructure`, implement `IModuleDbInitializer` (or one per module).
  - Seed baseline Roles (Admin, User, Guide, BusinessOwner) and default Permissions.
  - Seed 10-20 Users (mix of roles) using `Bogus.Faker` for realistic names, emails (e.g., test@example.com), and hashed passwords (use a dummy hash for test).
  - Save to DB. Ensure you assign static GUIDs to a few key users so other modules can reference them easily if needed, or rely on fetching them.

  **Recommended Agent Profile**:
  - **Category**: `unspecified-low`
    - Reason: Generating fake users with Bogus and inserting them.
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocked By**: Task 1

  **References**:
  - `Security.Domain` & `Auth.Domain` entities.
  - `Bogus` library.

  **Acceptance Criteria**:
  - [ ] Uses Bogus for user names/emails.
  - [ ] Roles and Users generated.

  **QA Scenarios**:
  ```
  Scenario: Auth Seeder logic is valid
    Tool: interactive_bash
    Preconditions: Auth seeder file exists
    Steps:
      1. Run `dotnet build Auth.Infrastructure`
    Expected Result: Build succeeds
    Failure Indicators: Build errors
    Evidence: .sisyphus/evidence/task-3-build.txt
  ```

- [ ] 4. Accounts Module Seeding

  **What to do**:
  - In `Accounts.Infrastructure`, implement `IModuleDbInitializer`.
  - Fetch existing User IDs from the database (e.g. by querying the schema/tables where Users are stored).
  - Seed Profile and GuideData for these users using Bogus (avatars, bios, languages spoken).

  **Recommended Agent Profile**:
  - **Category**: `unspecified-low`
    - Reason: Generating profiles linked to Auth IDs.
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocked By**: Task 3

  **References**:
  - `Accounts.Domain/Entities/Profile.cs`

  **Acceptance Criteria**:
  - [ ] Profiles use fetched user GUIDs.
  - [ ] Uses Bogus.

  **QA Scenarios**:
  ```
  Scenario: Accounts Seeder builds
    Tool: interactive_bash
    Preconditions: Accounts seeder file exists
    Steps:
      1. Run `dotnet build Accounts.Infrastructure`
    Expected Result: Build succeeds
    Failure Indicators: Build errors
    Evidence: .sisyphus/evidence/task-4-build.txt
  ```

- [ ] 5. ContentPlaces Module Seeding

  **What to do**:
  - In `ContentPlaces.Infrastructure`, implement `IModuleDbInitializer`.
  - Fetch existing User IDs (for Business Owners/Managers).
  - Seed 10-20 Businesses using Bogus (e.g., "Amman Adventure Co.").
  - Seed 30-50 Places linked to Businesses (cities, locations, coordinates).
  - Seed Amenities (Wi-Fi, Parking, etc.) and link them to Places.
  - Seed Operating Hours for Places.

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: More complex Bogus relationships (Businesses -> Places -> Amenities -> Hours).
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2
  - **Blocked By**: Task 3 (needs Users)

  **References**:
  - `ContentPlaces.Domain/Entities`

  **Acceptance Criteria**:
  - [ ] Idempotency check exists.
  - [ ] Places have valid Coordinates (Jordan bounding box ideally: Lat 29.0-33.0, Lon 35.0-39.0).
  - [ ] Linked to valid Business Owners.

  **QA Scenarios**:
  ```
  Scenario: ContentPlaces Seeder logic is valid
    Tool: interactive_bash
    Preconditions: Seeder exists
    Steps:
      1. Run `dotnet build ContentPlaces.Infrastructure`
    Expected Result: Build succeeds
    Failure Indicators: Build errors
    Evidence: .sisyphus/evidence/task-5-build.txt
  ```

- [ ] 6. Social Module Seeding

  **What to do**:
  - In `Social.Infrastructure`, implement `IModuleDbInitializer`.
  - Fetch existing User IDs.
  - Seed baseline generic platform reviews or reports if applicable. If Social only attaches to specific entities (like Tours or Places) that don't exist yet, defer until Wave 5 or only seed independent social entities.
  - *Correction*: If Social module requires Tours/Bookings, move this to Wave 5. But if it handles direct User-to-User connections, seed them here. Let's assume it handles generic user reviews and reports.

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: Needs to check Social domain model for dependencies.
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2
  - **Blocked By**: Task 3

  **References**:
  - `Social.Domain/Entities`

  **Acceptance Criteria**:
  - [ ] Valid dependencies selected.
  - [ ] Data seeded using Bogus.

  **QA Scenarios**:
  ```
  Scenario: Social Seeder logic is valid
    Tool: interactive_bash
    Preconditions: Seeder exists
    Steps:
      1. Run `dotnet build Social.Infrastructure`
    Expected Result: Build succeeds
    Failure Indicators: Build errors
    Evidence: .sisyphus/evidence/task-6-build.txt
  ```

- [ ] 7. ContentTours Module Seeding

  **What to do**:
  - In `ContentTours.Infrastructure`, implement `IModuleDbInitializer`.
  - Fetch existing User IDs (for Guides) and Place IDs.
  - Seed 20-50 Tours using Bogus (titles like "Petra Full Day Tour", descriptions, durations).
  - Seed Tour Translations (English/Arabic).
  - Seed Tour Packages (Standard, VIP) and Pricing Tiers.
  - Seed Schedules and Waypoints for the tours.
  - IMPORTANT: This is the core domain. Generate rich, realistic data.

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: Complex entity graph (Tours -> Packages -> Pricing -> Schedules -> Waypoints -> Translations). Needs careful Bogus configuration.
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 3
  - **Blocked By**: Task 5, Task 2 (needs Categories/Languages), Task 3 (needs Guides)

  **References**:
  - `ContentTours.Domain/Entities`

  **Acceptance Criteria**:
  - [ ] Complex entity graph generated accurately.
  - [ ] Bogus uses realistic tourism data.

  **QA Scenarios**:
  ```
  Scenario: ContentTours Seeder logic is valid
    Tool: interactive_bash
    Preconditions: Seeder exists
    Steps:
      1. Run `dotnet build ContentTours.Infrastructure`
    Expected Result: Build succeeds
    Failure Indicators: Build errors
    Evidence: .sisyphus/evidence/task-7-build.txt
  ```

- [ ] 8. Booking Module Seeding

  **What to do**:
  - In `Booking.Infrastructure`, implement `IModuleDbInitializer`.
  - Fetch existing User IDs (Customers and Guides).
  - Fetch existing Tour IDs and Schedule IDs.
  - Seed 100-500 TourBookings in various states (Pending, Confirmed, Completed, Cancelled).
  - Link bookings to specific schedules, packages, and pricing tiers.
  - Generate special requests and party details (number of adults/children).

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: Needs to resolve correct combinations of Tour + Package + Schedule to create valid Booking entities.
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 4
  - **Blocked By**: Task 7 (needs Tours), Task 3 (needs Users)

  **References**:
  - `Booking.Domain/Entities`

  **Acceptance Criteria**:
  - [ ] Valid bookings linked to existing tours.
  - [ ] Various booking statuses represented.

  **QA Scenarios**:
  ```
  Scenario: Booking Seeder logic is valid
    Tool: interactive_bash
    Preconditions: Seeder exists
    Steps:
      1. Run `dotnet build Booking.Infrastructure`
    Expected Result: Build succeeds
    Failure Indicators: Build errors
    Evidence: .sisyphus/evidence/task-8-build.txt
  ```

- [ ] 9. Finance Module Seeding

  **What to do**:
  - In `Finance.Infrastructure`, implement `IModuleDbInitializer`.
  - Fetch existing User IDs and Booking IDs.
  - Seed Payments linked to Bookings (Stripe/PayPal simulated).
  - Seed Loyalty Points for customers.
  - Seed Guide Payouts and Subscriptions.
  - Generate referral codes and discounts.

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: Needs to calculate totals that roughly match Booking amounts.
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 4
  - **Blocked By**: Task 8 (needs Bookings)

  **References**:
  - `Finance.Domain/Entities`

  **Acceptance Criteria**:
  - [ ] Payments generated.
  - [ ] Loyalty points and subscriptions seeded.

  **QA Scenarios**:
  ```
  Scenario: Finance Seeder logic is valid
    Tool: interactive_bash
    Preconditions: Seeder exists
    Steps:
      1. Run `dotnet build Finance.Infrastructure`
    Expected Result: Build succeeds
    Failure Indicators: Build errors
    Evidence: .sisyphus/evidence/task-9-build.txt
  ```

- [ ] 10. ContentBlogs Seeding

  **What to do**:
  - In `ContentBlogs.Infrastructure`, implement `IModuleDbInitializer`.
  - Fetch User IDs (Authors).
  - Seed Blogs with rich markdown content, cover images, and tags.
  - Seed Comments and Reactions on blogs.

  **Recommended Agent Profile**:
  - **Category**: `unspecified-low`
    - Reason: Standard Bogus text generation.
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 5
  - **Blocked By**: Task 3

  **References**:
  - `ContentBlogs.Domain/Entities`

  **Acceptance Criteria**:
  - [ ] Blogs generated with paragraphs of lorem ipsum or real-sounding travel tips.
  - [ ] Comments attached.

  **QA Scenarios**:
  ```
  Scenario: ContentBlogs Seeder logic is valid
    Tool: interactive_bash
    Preconditions: Seeder exists
    Steps:
      1. Run `dotnet build ContentBlogs.Infrastructure`
    Expected Result: Build succeeds
    Failure Indicators: Build errors
    Evidence: .sisyphus/evidence/task-10-build.txt
  ```

- [ ] 11. Messaging Seeding

  **What to do**:
  - In `Messaging.Infrastructure`, implement `IModuleDbInitializer`.
  - Fetch User IDs.
  - Seed Notifications (System, Booking Updates, Reminders).
  - Seed Support Tickets and Chatbot Conversations.

  **Recommended Agent Profile**:
  - **Category**: `unspecified-low`
    - Reason: Simple entity insertions.
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 5
  - **Blocked By**: Task 3

  **References**:
  - `Messaging.Domain/Entities`

  **Acceptance Criteria**:
  - [ ] Notifications and tickets generated.

  **QA Scenarios**:
  ```
  Scenario: Messaging Seeder logic is valid
    Tool: interactive_bash
    Preconditions: Seeder exists
    Steps:
      1. Run `dotnet build Messaging.Infrastructure`
    Expected Result: Build succeeds
    Failure Indicators: Build errors
    Evidence: .sisyphus/evidence/task-11-build.txt
  ```

- [ ] 12. Analytics & Tracking Seeding

  **What to do**:
  - In `Analytics.Infrastructure` and `Tracking.Infrastructure`, implement `IModuleDbInitializer`.
  - Generate fake User Interactions, Popularity Scores, and Live Sessions.
  - Generate Checkpoints and Location Snapshots for Tracking (Lat/Lng paths).

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: Generating realistic coordinate sequences for Tracking.
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 5
  - **Blocked By**: Task 3

  **References**:
  - `Analytics.Domain` & `Tracking.Domain`

  **Acceptance Criteria**:
  - [ ] Analytics and tracking data generated.

  **QA Scenarios**:
  ```
  Scenario: Analytics & Tracking Seeders build
    Tool: interactive_bash
    Preconditions: Seeders exist
    Steps:
      1. Run `dotnet build Analytics.Infrastructure && dotnet build Tracking.Infrastructure`
    Expected Result: Builds succeed
    Failure Indicators: Build errors
    Evidence: .sisyphus/evidence/task-12-build.txt
  ```

- [ ] 13. ContentSeo Seeding & Final Integration

  **What to do**:
  - In `ContentSeo.Infrastructure`, implement `IModuleDbInitializer`.
  - Seed SEO metadata for static pages, FAQs, and common redirects.
  - In `YallaJo.Api/Program.cs`, hook up all seeders in the DI container and ensure `UseDataSeeders()` (or equivalent extension method) is called inside `if (app.Environment.IsDevelopment())`.

  **Recommended Agent Profile**:
  - **Category**: `unspecified-low`
    - Reason: Final wiring and simple SEO entity seeding.
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave Wave 5
  - **Blocked By**: All previous tasks (needs DI registration of all initializers)

  **References**:
  - `ContentSeo.Domain`
  - `YallaJo.Api/Program.cs`

  **Acceptance Criteria**:
  - [ ] SEO metadata seeded.
  - [ ] ALL Initializers registered in API DI container in topological order.
  - [ ] API project builds successfully.

  **QA Scenarios**:
  ```
  Scenario: API Integrates all seeders and builds
    Tool: interactive_bash
    Preconditions: All code written
    Steps:
      1. Run `dotnet build YallaJo.Api`
    Expected Result: Build succeeds with 0 errors
    Failure Indicators: Compilation errors in Program.cs
    Evidence: .sisyphus/evidence/task-13-build.txt
  ```

---

## Final Verification Wave

- [ ] F1. **Plan Compliance Audit** — `oracle`
  Read the plan end-to-end. Verify implementation exists (all 13 modules seeded). Check evidence files exist.
  Output: `Must Have [N/N] | Must NOT Have [N/N] | Tasks [N/N] | VERDICT: APPROVE/REJECT`

- [ ] F2. **Code Quality Review** — `unspecified-high`
  Run build command. Review changed files for proper Bogus usage, idempotency checks (`Any()`), and environment guards.
  Output: `Build [PASS/FAIL] | Files [N clean/N issues] | VERDICT`

- [ ] F3. **Real Manual QA** — `unspecified-high`
  Start the application. Send a request to an endpoint (e.g. Get Tours) to verify data is returned.
  Output: `Data Verified [PASS/FAIL] | VERDICT`

- [ ] F4. **Scope Fidelity Check** — `deep`
  Verify we only added seeder files and registered them, without modifying core domain logic or causing migration issues.
  Output: `Tasks [N/N compliant] | Unaccounted [CLEAN/N files] | VERDICT`

---

## Commit Strategy

- **NO COMMITS**: User explicitly requested no commits for this task.

---

## Success Criteria

### Final Checklist
- [ ] 13 Module DbInitializers implemented using Bogus
- [ ] Idempotent execution (doesn't duplicate data on restart)
- [ ] Guarded behind `IsDevelopment()` check
- [ ] Cross-module data dependencies respected