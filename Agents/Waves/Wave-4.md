# Wave 4 — Deep Content & Enrichment

> **Sources:** `Agents/agent-context.md` §Wave 4 (Endpoints) + `Agents/guide.md` §8 (SEO), §9 (Blog), §13 (Children-Friendly)
> **Dependencies:** Wave 3 (Tours)
> **Focus:** Tour schedules, pricing tiers, waypoints, children-info, Blog system, SEO, FAQ, Translations
> **Status:** ✅ **100% COMPLETE** — all endpoints built per PDF1

---

## 1. Coverage Summary

| Area | Endpoints | Status |
|---|---|---|
| Tour pricing tiers | 4/4 | ✅ |
| Tour schedules | 4/4 | ✅ |
| Tour waypoints | 4/4 | ✅ |
| Tour children-info | 2/2 | ✅ |
| Tour admin approve/reject | 2/2 | ✅ |
| Tour submit-for-review | 1/1 | ✅ |
| Blog CRUD + publish/unpublish + tour-link | 16/16 | ✅ |
| Blog comments | 6/6 | ✅ |
| FAQ items (CRUD + reorder + GET-by-entity) | 5/5 | ✅ |
| SEO metadata | 3/3 | ✅ |
| Redirects | 3/3 | ✅ |
| Translations (CRUD + batch + on-demand + approve) | 7/7 | ✅ |

---

## 2. Business Rules Verification (guide.md §8, §9, §13)

### 2.1 Blog (guide §9.2)
- ✅ Admin-only authoring (provider blogs deferred to future)
- ✅ Title max 500 chars
- ✅ Slug auto-generated, globally unique (random 4-char suffix on collision per §8.3)
- ✅ Summary max 1000 chars (optional)
- ✅ Rich-text content (nvarchar(max)); images via external URL referencing Attachments
- ✅ ReadTimeMinutes auto-calculated (word count / 200)
- ✅ Max 1 Place link + 10 Tour links per blog (via BlogTours junction with SortOrder)
- ✅ AR + EN translations required
- ✅ Status: Draft / Published / Archived
- ✅ Draft = admin-only visible
- ✅ Archived = direct URL accessible (SEO preservation) with "outdated" banner
- ✅ PublishedAt set on first publish; subsequent edits update UpdatedAt only
- ✅ ViewCount incremented per unique user per 30-min window (debounced)

### 2.2 Blog Comments (guide §9.2)
- ✅ Login required
- ✅ Nested max 2 levels deep (comment → reply → reply-to-reply)
- ✅ Max 1000 chars
- ✅ Profanity filter (same as reviews — uses Social module's `IProfanityFilter`)
- ✅ Reactions: Like / Helpful / Insightful — one per user per comment, change replaces
- ✅ Soft-delete preserves replies (shows "[deleted]" for parent)

### 2.3 SEO Generation (guide §8.2)
- ✅ Sitemap regen every 6 hours (cron 00:00, 06:00, 12:00, 18:00 UTC) — `SitemapRegenerationService` BG
- ✅ Active + non-deleted entities only
- ✅ Each SitemapEntry: URL, ChangeFrequency, Priority, LastModified, EntityType, EntityId
- ✅ Priority: Tours=0.8, Places=0.9, Businesses=0.7, Blogs=0.6, Static=0.3
- ✅ ChangeFrequency: Tours=weekly, Places=monthly, Businesses=weekly, Blogs=monthly
- ✅ Max 50,000 URLs/file; sitemap index if exceeded, split by EntityType
- ✅ Google Search Console + Bing Webmaster ping after regen
- ✅ Meta tags: title (max 60), description (max 160), og:title, og:description, og:image, canonical URL, hreflang ar↔en
- ✅ OG image fallback to YallaJo default if none
- ✅ Canonical always slug-based to avoid duplicate content

### 2.4 Redirects (guide §8.2)
- ✅ Slug-change triggers auto 301
- ✅ Admin can manually create 301/302
- ✅ OldUrl uniqueness enforced
- ✅ Circular redirect detection
- ✅ Chain limit: max 3 hops; system auto-flattens A→B + B→C as A→C
- ✅ HitCount tracked for analytics
- ✅ Middleware checks redirects on 404 before returning

### 2.5 Children-Friendly (guide §13)
- ✅ `IsChildFriendly` toggle on Tour (default false)
- ✅ When true: MinAge required (0-17), MaxAge optional (defaults to 17 if not set)
- ✅ MaxAge must be > MinAge
- ✅ `AgeRestriction` (separate, hard restriction): if set with IsChildFriendly=true, must be ≤ MinAge
- ✅ Search filter: "Family Friendly" toggle + optional child age (filters where MinAge ≤ age ≤ MaxAge)

### 2.6 Pricing Tiers (PDF1 Wave 4)
- ✅ Tier types: Adult, Child, Infant, Senior, Group, Private
- ✅ At least one Adult tier required
- ✅ Child tier must have age range; price can be 0 (free)
- ✅ Group tier activates when MinQuantity reached
- ✅ Sale price field separate (computed by DiscountLifecycleService on active discount)

### 2.7 Schedules (PDF1 Wave 4)
- ✅ Date, startTime, endTime, capacity ≥1
- ✅ Recurrence: once / daily / weekly / custom (specific days of week)
- ✅ For recurring: generate slots up to 90 days ahead
- ✅ No overlapping schedules for same tour
- ✅ Blackout dates supported
- ✅ Delete blocked if has bookings

---

## 3. Maintenance Tasks (No Active Sprint Work)

Even though Wave 4 is complete, monitor for:

### 3.1 ViewCount debounce verification
- Test that same user viewing blog within 30min increments only once
- If broken: check Redis/HybridCache key `blog:view-debounce:{blogId}:{userId}` with 30-min TTL

### 3.2 Sitemap regen health
- Monitor `SitemapRegenerationService` logs
- Alert if regen hasn't run in 7 hours (something's wrong with the cron)

### 3.3 Redirect chain flattening
- If admin creates A→B then B→C, verify the existing A→B was rewritten to A→C
- Failure mode: chains grow → middleware returns 404 after 3 hops

### 3.4 Translation provider quota
- ContentSeo.Translation uses external API for auto-translation
- Monitor API quota (1000/day free tier)
- Verify fallback to "Untranslated — Pending" status when quota exhausted

---

## 4. Potential Future Improvements (NOT Required for PDF Parity)

- **Provider-authored blogs:** PDF2 §9.2 mentions as "future feature" — backlog
- **Blog series/categories:** Currently flat; could add `BlogSeriesId` for multi-part posts
- **AMP/structured data:** schema.org JSON-LD generation for Article, Tour, Place entities
- **Image lazy-loading hints:** Already in EntityImages structure but verify frontend uses srcset
- **Translation memory:** Reuse approved translations across similar entities (terminology consistency)

---

## 5. Acceptance Criteria (already met ✅)

- [x] All 42+ Wave 4 endpoints respond per PDF1 spec
- [x] Sitemap regenerates every 6 hours
- [x] Blog publish triggers SEO metadata creation + sitemap update
- [x] Comment nesting max 2 levels enforced
- [x] FAQ reorder works
- [x] Redirect chain flattening verified
- [x] All endpoints use `MustHavePermissionAttribute`
- [x] `dotnet build` green for ContentBlogs.*, ContentCore.*, ContentSeo.*, ContentTours.*

---

## 6. Cross-Cutting Concerns Addressed in This Wave

- **Translation pipeline:** ContentCreatedIntegrationEvent → auto-translation worker → TranslationStatus tracked
- **Profanity filter:** Single `IProfanityFilter` in Social.Contracts used by Reviews + BlogComments
- **Attachment polymorphism:** Single `Attachments` table with `(EntityType, EntityId)` discriminator; reused across all modules

This wave required NO new BG services and NO new migrations to reach feature-parity.
