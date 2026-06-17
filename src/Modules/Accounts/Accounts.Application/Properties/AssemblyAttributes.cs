using System.Runtime.CompilerServices;

// Expose internal helpers (e.g. SafeFileNameSanitizer) to the Accounts.Tests.Unit
// project so they can be covered by pure unit tests without going through the public
// command surface. (Patch 2G removed the ProviderDocumentFileUrlParser helper.)
[assembly: InternalsVisibleTo("Accounts.Tests.Unit")]
