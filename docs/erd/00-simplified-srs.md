# Simplified ERD (for SRS / PDF)

A **simplified, documentation-friendly** view showing the **core business entities** only.
Infrastructure (Outbox/Inbox), caches, snapshots, and most join/child tables are **omitted**
for clarity. Only primary keys and a few business attributes are shown. Cross-module links
are **not** drawn here (see the master ERD for those).

> Audience: SRS / PDF / non-technical stakeholders.
> For the full technical model, see the per-module diagrams under `modules/`.

---

## Identity & Accounts

```mermaid
erDiagram
    User ||--o{ Email : has
    User ||--o{ Phone : has
    User ||--o{ UserRole : has
    Role ||--o{ UserRole : grants
    User ||--o{ Profile : "owns (logical)"
    User ||--o{ ProviderApplication : "submits (logical)"
    ProviderApplication ||--o{ ProviderDocument : includes

    User {
        guid Id PK
        string LifecycleState
    }
    Role {
        guid Id PK
        string Name
    }
    UserRole {
        guid Id PK
        guid UserId FK
        guid RoleId FK
    }
    Email {
        guid Id PK
        string Address
    }
    Phone {
        guid Id PK
        string Number
    }
    Profile {
        guid Id PK
        guid UserId
        string DisplayName
    }
    ProviderApplication {
        guid Id PK
        string Status
    }
    ProviderDocument {
        guid Id PK
        string DocumentType
    }
```

---

## Content (Tours, Places, Blogs)

```mermaid
erDiagram
    Place ||--o{ Business : "associated (PlaceBusiness)"
    Tour ||--o{ TourPricingTier : "priced by"
    Tour ||--o{ TourSchedule : "scheduled"
    TourPackage ||--o{ Tour : "bundles (TourPackageTour)"
    TourGuide ||--o{ GuideTourOffering : offers
    Blog ||--o{ BlogComment : has
    Blog ||--o{ Tour : "links (BlogTour)"

    Place {
        guid Id PK
        string Name
    }
    Business {
        guid Id PK
        string Name
    }
    Tour {
        guid Id PK
        string Title
        decimal BasePrice
    }
    TourPricingTier {
        guid Id PK
        decimal Price
    }
    TourSchedule {
        guid Id PK
        datetime StartTime
    }
    TourPackage {
        guid Id PK
        string Title
    }
    TourGuide {
        guid Id PK
        string DisplayName
    }
    GuideTourOffering {
        guid Id PK
        string Status
    }
    Blog {
        guid Id PK
        string Title
    }
    BlogComment {
        guid Id PK
        string Body
    }
```

---

## Commerce (Booking & Finance)

```mermaid
erDiagram
    TourBooking ||--o{ JoinRequest : "may receive"
    TourGuide ||--o{ AvailabilitySlot : publishes
    Payment ||--o{ Dispute : "may have"
    Payout ||--o{ PayoutItem : contains
    Invoice ||--o{ InvoiceItem : "owns (lines)"
    Discount ||--o{ DiscountUsage : "tracked by"

    TourBooking {
        guid Id PK
        string Reference
        string Status
        decimal TotalAmount
    }
    JoinRequest {
        guid Id PK
        string Status
    }
    AvailabilitySlot {
        guid Id PK
        datetime StartUtc
    }
    Payment {
        guid Id PK
        decimal Amount
        string Status
    }
    Dispute {
        guid Id PK
        string Status
    }
    Payout {
        guid Id PK
        decimal NetAmount
    }
    PayoutItem {
        guid Id PK
    }
    Invoice {
        guid Id PK
        string InvoiceNumber
        decimal AmountTotal
    }
    InvoiceItem {
        guid Id PK
        decimal Subtotal
    }
    Discount {
        guid Id PK
        string Code
    }
    DiscountUsage {
        guid Id PK
    }
```

---

## Engagement (Social, Messaging, Tracking)

```mermaid
erDiagram
    Review ||--o{ ReviewReply : "owns reply"
    SupportTicket ||--o{ TicketMessage : contains
    LiveTrackingSession ||--o{ LocationSnapshot : records
    LiveTrackingSession ||--o{ TourCheckpoint : "passes"

    Review {
        guid Id PK
        int Rating
        string TargetType
    }
    ReviewReply {
        guid Id PK
        string Body
    }
    Favorite {
        guid Id PK
        string EntityType
    }
    Report {
        guid Id PK
        string Status
    }
    SupportTicket {
        guid Id PK
        string Subject
        string Status
    }
    TicketMessage {
        guid Id PK
        string Body
    }
    Notification {
        guid Id PK
        string Channel
    }
    LiveTrackingSession {
        guid Id PK
        string Status
    }
    LocationSnapshot {
        guid Id PK
        decimal Latitude
        decimal Longitude
    }
    TourCheckpoint {
        guid Id PK
    }
```

> **Notes for documentation readers:**
> - All modules are isolated by schema; links between modules are *logical* (handled by
>   integration events), not physical foreign keys.
> - Audit fields (`CreatedAt`, `UpdatedAt`), soft-delete (`IsDeleted`), and concurrency
>   (`RowVersion`) exist on most entities but are omitted here for readability.
