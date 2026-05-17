# TASK 7 — Notification Templates (Admin CRUD)

> **Owner:** Junior (or Fadwa fallback) — **Hours:** 18h — **Hard deadline:** Sun **2026-12-20 17:00**
> **Endpoints:** 4 HTTP
> **Depends on:** PW-2, PW-6 (INotificationTemplateRenderer Stubble integration)

This task is the simplest endpoint set in the sprint — pure admin CRUD against the `NotificationTemplates` table. The renderer itself was scaffolded in PW-6; this task adds the management surface.

---

## Endpoint List

| # | Method | Path | Permission |
|---|---|---|---|
| 1 | GET | `/api/v1/admin/notification-templates` | `MustHavePermission(NotificationTemplate, Read)` (admin role) |
| 2 | POST | `/api/v1/admin/notification-templates` | `MustHavePermission(NotificationTemplate, Create)` |
| 3 | PUT | `/api/v1/admin/notification-templates/{id}` | `MustHavePermission(NotificationTemplate, Update)` |
| 4 | DELETE | `/api/v1/admin/notification-templates/{id}` | `MustHavePermission(NotificationTemplate, Delete)` |

No public GET — templates are admin-only configuration data.

---

## Domain Methods (NotificationTemplate aggregate)

| Method | Raises |
|---|---|
| `Create(type, channel, languageCode, title, body, htmlBody?)` factory | NotificationTemplateCreated |
| `Update(title, body, htmlBody?)` | NotificationTemplateUpdated |
| `Delete()` | NotificationTemplateDeleted |

---

## Validation Rules

- `(Type, Channel, LanguageCode)` UNIQUE — DB index enforces; handler returns 409 `NotificationTemplate.DuplicateKey` if pre-checked.
- `Type` valid enum.
- `Channel` valid enum.
- `LanguageCode` from `Languages` table active set (cache 1h, see ContentCore.Languages prior sprint).
- `Title` 1-200 chars; supports `{{Placeholder}}` tokens.
- `Body` 1-5000 chars; supports tokens.
- `HtmlBody` optional, ≤ 50000 chars, **sanitized via `HtmlSanitizer` NuGet** (no `<script>`, no `on*=` attributes, no `javascript:`).
- **Placeholder validation:** parse template via Stubble's static parser → verify all `{{token}}` names are on an allow-list (M-R4 standard placeholders + type-specific extras). Error code `NotificationTemplate.InvalidPlaceholderSyntax` (lists unknown tokens).

---

## Cache Strategy

| Query | Tag | TTL |
|---|---|---|
| `GET /admin/notification-templates` | `notification-templates` | 5min |
| Internal: `INotificationTemplateRenderer` template lookup | `notification-template:{type}:{channel}:{lang}` | 30min |

**Invalidation:** all 3 mutation endpoints invalidate `notification-templates` tag (broad — admin operations are rare).

**Renderer-level cache invalidation:** when template is Updated/Deleted, also `RemoveByTagAsync($"notification-template:{type}:{channel}:{lang}")` so EmailSender BG immediately uses new copy.

---

## DTO Format

```json
{
  "id": "guid",
  "type": "BookingConfirmed",
  "channel": "Email",
  "languageCode": "en",
  "title": "Booking confirmed — {{TourName}}",
  "body": "Hello {{UserName}}, your booking is confirmed for {{Date}}. Your confirmation code is {{ConfirmationCode}}.",
  "htmlBody": "<html><body><h1>Confirmed</h1><p>Hello {{UserName}}...</p></body></html>",
  "discoveredPlaceholders": ["UserName", "TourName", "Date", "ConfirmationCode"],
  "createdAt": "2027-01-15T10:30:00Z",
  "updatedAt": "2027-01-15T10:30:00Z"
}
```

`discoveredPlaceholders` is computed from Stubble's parser at GET time — helps admin UI build a placeholder picker.

---

## Default Seeded Templates (Migration 7: `MessagingSeedDefaultNotificationTemplates`)

Idempotent INSERT-IF-NOT-EXISTS for the core 30+ notification types × InApp + Email × en + ar (so ~120 rows). Listed in `Messaging.Infrastructure/Persistence/Seeds/DefaultNotificationTemplates.cs`.

**Examples:**

| Type | Channel | Lang | Title | Body |
|---|---|---|---|---|
| WelcomeEmail | Email | en | Welcome to YallaJo, {{UserName}}! | Thanks for joining YallaJo. Browse top tours in Jordan: {{Url}} |
| OtpDelivery | Email | en | Your YallaJo verification code | Your code is {{ConfirmationCode}}. It expires in 10 minutes. Never share this code. |
| BookingConfirmed | InApp | en | Booking confirmed — {{TourName}} | See you on {{Date}}. Confirmation: {{ConfirmationCode}} |
| PaymentCompleted | Email | en | Payment received: {{Amount}} {{Currency}} | Thanks {{UserName}}. View invoice: {{Url}} |
| (… 30+ types) | … | … | … | … |

**Arabic equivalents:** same set with translated text. Where uncertain, leave English fallback (renderer falls back per M-R4).

---

## WBS (18h)

| # | Step | Hours |
|---|---|---|
| 1 | NotificationTemplate aggregate Create/Update/Delete + tests | 3 |
| 2 | 4 endpoints + commands/queries/handlers/validators/DTOs | 5 |
| 3 | Placeholder allow-list validator (M-R4 list + extension hooks for type-specific) | 2 |
| 4 | HtmlSanitizer integration + tests | 1 |
| 5 | Cache wiring (broad + renderer-level invalidation) | 1 |
| 6 | DefaultNotificationTemplates seed file (~120 rows EN+AR) | 4 |
| 7 | Integration tests + admin endpoint smoke | 2 |
| **Total** | | **18h** |

---

## Acceptance Tests

1. POST template with `(BookingConfirmed, Email, en)` first time → 201.
2. POST duplicate key → 409 `NotificationTemplate.DuplicateKey`.
3. POST template with unknown placeholder `{{NonExistent}}` → 400 `NotificationTemplate.InvalidPlaceholderSyntax` with token name in error.
4. POST template HTML with `<script>alert(1)</script>` → sanitized to empty content; warning logged.
5. PUT template → template-level cache invalidated → EmailSender BG sends with new body within next 30-sec tick.
6. DELETE template → 204 + renderer falls back per M-R4 chain (test by deleting `(BookingConfirmed, Email, en)` then verifying renderer uses fallback inline string).
7. GET list pagination (rarely needed; few templates).
8. Seeded templates count = expected (~120) after migration applied.
9. Render `(BookingConfirmed, Email, en)` with placeholders `{TourName: "Petra Trek", UserName: "Ali"}` → title/body have substitutions; missing `{{Date}}` becomes empty string (M-R4).
10. Render unknown type `(NonExistent, Email, en)` → fallback to inline last-resort string (no exception).
