using System.Runtime.CompilerServices;

// Patch 2B: expose internal helpers (ProviderDocumentFileUrlParser,
// SafeFileNameSanitizer) to the Accounts.Tests.Unit project so they can be
// covered by pure unit tests without going through the public command surface.
[assembly: InternalsVisibleTo("Accounts.Tests.Unit")]
