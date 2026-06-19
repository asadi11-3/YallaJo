using System.Runtime.CompilerServices;

// Expose internal query/command handlers (e.g. GetPublicReviewsQueryHandler) to the
// Social.Tests.Unit project so they can be covered by pure unit tests without going
// through the public messaging surface.
[assembly: InternalsVisibleTo("Social.Tests.Unit")]
