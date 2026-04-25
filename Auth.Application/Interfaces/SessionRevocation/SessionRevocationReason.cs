using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Application.Interfaces.SessionRevocation
{
    public enum SessionRevocationReason
    {
        /// <summary>User completed a self-service password reset.</summary>
        PasswordReplacedBySelf = 1,

        /// <summary>Account was just activated (tears down any stray state — defensive).</summary>
        AccountActivated = 2,

        /// <summary>
        /// Phase 3A — an admin initiated a forced password reset for the
        /// user. The admin does NOT replace the password directly; the
        /// user completes the reset via the emailed code. Sessions are
        /// revoked at the moment of initiation so the target cannot keep
        /// using an existing live session until they complete the reset.
        /// </summary>
        PasswordResetByAdmin = 3,

        /// <summary>
        /// Phase 3B — an admin suspended the account. Every active session and
        /// refresh token is revoked immediately so access is blocked at once.
        /// </summary>
        AccountSuspended = 4,

        /// <summary>
        /// Phase 3C — an admin reassigned the account to a new owner (new
        /// primary email). The old owner must lose access immediately, so
        /// every active session and refresh token is revoked as part of
        /// the reassignment operation.
        /// </summary>
        AccountReassigned = 5,

        /// <summary>
        /// Phase 3B — an admin archived the account (terminal lifecycle state).
        /// Active sessions/tokens are revoked as part of the archive operation.
        /// </summary>
        AccountArchived = 6,
    }
}
