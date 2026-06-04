# Messaging — Real-time & In-app Chat — Build Plan (Tier B)

> **Scope:** wire the **SignalR hub** for live push (notification badge, toasts) and build **in-app conversation threads** (customer ↔ provider / guide), on top of the notification *inbox* and *support tickets* already covered by `Accounts-Notifications-Support-Reviews-Plan.md`.
> **Companion docs:** `UI-UX-Design.md` §7 (SignalR) + §4.11.7 (notifications), `Agents/Plans/Messaging-*`, `Agents/Gaps/Messaging-Gaps.md`.
> **Authoring rules:** `CONTROLLER_AUTHORING_GUIDE.md` (authoritative).
> **API base path:** `/api/v1` · **Hub:** `/hubs/notifications` *(confirm exact route in Phase 0)*
> **Status:** 📋 Planned — not started

---

## Why this plan

The `Messaging` module ships **23 endpoints + 1 SignalR hub**. The Accounts plan builds the *static* pieces (notification inbox, preferences, support tickets). What remains is the **real-time** layer and **person-to-person messaging**:

1. **Live push** — the notification bell should update its unread count and surface toasts without a page refresh (SignalR).
2. **Conversations** — a threaded inbox so a traveler can message a provider/guide about a tour and vice-versa. The template has no chat screen, so the thread UI is **net-new** (reuse `help-detail.html` / `account-*` shells for chrome).

---

## Architecture conventions (read before building)

Layered-by-type per `CONTROLLER_AUTHORING_GUIDE.md` (same as the Accounts/Provider/Tier-A plans): `Areas/{Area}/{Controllers,Facades,ApiClients,Models/{Feature},Views/{Controller}}`; four-tier `Controller → Facade → ApiClient → IApiClient`; errors as `ApiResult`; suffix-based `AddFeatureServices()` DI; `BaseController` + `[Area]` + `[Authorize]` + `GuardSignOut` + PRG; per-feature paged `Response`; explicit `asp-area`/antiforgery/`_Alerts`/`<partial>` views; **per-user → no output cache**; build csproj **alone** (`CS2012`).

**SignalR specifics (the new part):**
- The hub is a **backend** concern; the Web app is a **client/relay**. Two valid patterns — decide in Phase 0:
  - **Pattern J (browser → API hub directly):** the browser opens a SignalR connection straight to the API hub, passing the JWT (negotiate with `accessTokenFactory`). The Web app only renders the JS + bell partial.
  - **Pattern K (Web BFF relay):** the Web host holds a hub connection and re-emits to browser clients via its own hub. Heavier; only if CORS/token constraints block Pattern J.
- A small **typed hub client wrapper** lives in `Infrastructure/Realtime/` (e.g. `NotificationHubClient`) — **not** an `ApiClient`/`Facade` (it isn't HTTP). Register it explicitly in `Program.cs` (the suffix auto-DI does not apply).
- Client JS goes in `wwwroot/assets/js/yallajo-notifications.js`; load `@microsoft/signalr` from the existing vendor assets (no new bundler step).
- **Graceful degradation:** if the socket drops, fall back to polling `GET /notifications/unread-count` on an interval. The bell must work without JS.

### Design & UI skills (mandatory for all views)

- **`ui-ux-pro-max`** + **`impeccable`** — bell/toast affordances, unread treatments, conversation bubble layout, typing/seen states, empty/error/offline states.
- **`design-taste-frontend`** — component architecture + hardware-accelerated CSS for toasts/badges (transform/opacity only).
- **`huashu-design`** — hi-fi exploration of the chat thread + toast system before committing markup; anti-AI-slop pass.

Constraint: stay within the Bootstrap 5 **Booking** template assets (`wwwroot/assets`).

---

## Template → page wiring (master map)

| Page / widget | Area / route | Template source | Reuse note |
|---|---|---|---|
| Live bell + toasts | global (in `_Navbar`) | nav bell in any `account-*.html` | ViewComponent rendered in shared layout |
| Conversations inbox | `Accounts/Messages` | `help-center.html` list + `account-bookings.html` shell | thread list + unread |
| Conversation thread | `Accounts/Messages/{id}` | `help-detail.html` article shell → chat bubbles | net-new bubble UI |
| Start conversation (modal) | (on) `Public/Tours/Detail`, `Content/Guides/Details` | `tour-detail.html` inquiry modal | "Message provider/guide" |

---

## Phase 0 — Grounding & hub decision (do first)

- [ ] Inspect `src/Modules/Messaging` → confirm the **hub route + method names** (e.g. `ReceiveNotification`, `UnreadCountChanged`), the negotiate/auth model, and whether **conversation/thread** endpoints exist (`GET /messages/conversations`, `GET /messages/conversations/{id}`, `POST /messages/conversations/{id}`, `POST /messages/conversations`) or are **gaps** (`Agents/Gaps/Messaging-Gaps.md`).
- [ ] **Choose Pattern J vs K** (browser-direct vs BFF relay) based on CORS + token negotiation.
- [ ] Read `UI-UX-Design.md` §7 + §4.11.7 (notification SignalR design, locked critical-pref types) so toasts respect preference gating.
- [ ] Confirm the Accounts **Notifications** feature exists (from the Accounts plan) — this plan **enhances** it, not replaces it.
- [ ] Add `WebPermission.Messaging.*` if conversations are permission-gated.

**Acceptance:** documented hub route/methods + chosen connection pattern; list of conversation endpoints that exist vs are gaps.

---

## Phase 1 — Live notification bell (SignalR push)

| Source | Use |
|---|---|
| Hub `UnreadCountChanged` / `ReceiveNotification` | push badge updates + toast on new notification |
| `GET /notifications/unread-count` | initial count + polling fallback |
| `POST /notifications/{id}/read` | clicking a toast/bell item marks read |

**Files**
- [ ] `Infrastructure/Realtime/NotificationHubClient.cs` (+ interface) — typed connection wrapper; registered in `Program.cs`
- [ ] `Areas/Shared` **ViewComponent** `NotificationBellViewComponent` (Pattern C cross-page widget) + `Views/Shared/Components/NotificationBell/Default.cshtml`
- [ ] `wwwroot/assets/js/yallajo-notifications.js` — connect, handle `accessTokenFactory`, update bell, render toasts, reconnect/poll fallback
- [ ] Render `<vc:notification-bell />` in `Views/Shared/_Navbar.cshtml`

**Acceptance:** a new notification (triggered from another session) updates the bell + shows a toast with no refresh; with JS disabled the bell still shows a server-rendered count.

---

## Phase 2 — Conversations inbox + thread (`Accounts/Messages`)

> Build only if Phase 0 confirms conversation endpoints exist; otherwise file the gap and stop at Phase 1.

| Endpoint *(confirm in P0)* | Use |
|---|---|
| `GET /messages/conversations` | inbox list (paged, unread-first) |
| `GET /messages/conversations/{id}` | thread messages |
| `POST /messages/conversations/{id}/messages` | send reply |
| `POST /messages/conversations` | start a new conversation (with target + tour/guide context) |
| hub `ReceiveMessage` | live-append incoming messages in an open thread |

**Files** (layered — under `Areas/Accounts/`)
- [ ] `ApiClients/MessagesApiClient.cs` · `Facades/MessagesFacade.cs`
- [ ] `Controllers/MessagesController.cs` (Index, Detail, Send, Start)
- [ ] `Models/Messages/` — `ConversationResponse`, `ConversationListResponse` (paged), `MessageResponse`, `SendMessageRequest`, `StartConversationRequest`, `ConversationVm`, `ThreadVm`, `MessageVm`, `MessagesMapper`
- [ ] `Views/Messages/Index.cshtml` (thread list) + `Detail.cshtml` (bubbles + composer) + `Views/Messages/Partials/{_Bubble,_Composer}.cshtml`
- [ ] Global `~/Views/Shared/_StartConversationModal.cshtml` included on `Public/Tours/Detail` + `Content/Guides/Details`
- [ ] Add **Messages** entry (with unread badge) to `AccountSidebarVm` + `_AccountSidebar.cshtml`

**Acceptance:** a traveler starts a conversation from a tour page, exchanges messages with the provider, and new replies appear live in an open thread (and as toasts elsewhere).

---

## Out of scope / blocked

| Item | Reason |
|---|---|
| Notification **preferences** | ✅ already in Accounts **Settings** |
| Notification **inbox** (static list) | ✅ already in Accounts **Notifications** (Accounts plan) |
| Support **tickets** | ✅ already in Accounts **Support** (Accounts plan) — distinct from peer messaging |
| Group/broadcast chat, attachments in chat | later; confirm backend support first |
| Conversation endpoints (if absent) | ⛔ backend gap → file in `Agents/Gaps/Messaging-Gaps.md`, build Phase 1 only |

---

## Sequencing & effort

1. **Phase 0** — hub route + pattern decision *(blocking; ~0.5 day)*.
2. **Phase 1** — live bell *(~1 sprint; the high-value, low-risk win)*.
3. **Phase 2** — conversations *(~1–1.5 sprints; only if endpoints exist)*.

**Note:** Phase 1 delivers most of the perceived value (live notifications) and is independent of conversations — ship it first.

## Open questions

- Exact hub route, method names, and negotiate/auth model in `src/Modules/Messaging`.
- Pattern J (browser→API hub) vs Pattern K (Web BFF relay) — CORS/token outcome from Phase 0.
- Do threaded peer-to-peer conversation endpoints exist, or only notifications + support tickets?
