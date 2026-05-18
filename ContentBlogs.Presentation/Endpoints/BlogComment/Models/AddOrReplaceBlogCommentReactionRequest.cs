using ContentBlogs.Domain.Enums;

namespace ContentBlogs.Presentation.Endpoints.BlogComment.Models;

public sealed record AddOrReplaceBlogCommentReactionRequest(ReactionType ReactionType);
