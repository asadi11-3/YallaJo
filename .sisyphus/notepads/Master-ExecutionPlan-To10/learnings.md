
## W3-B Social completion - 2026-05-26
- Social endpoint base routes now use /api/v1/social/*; public review listing supports /reviews/{entityType}/{entityId} plus legacy query-string route.
- Warn/ban direct moderation endpoints reuse existing UserModerationRecord and append ContentModerationLog; unban soft-deletes active BanUser record and logs UnbanUser.
- Moderation command handlers invalidate HybridCache tag moderation:logs after ISocialUnitOfWork.SaveChangesAsync.
