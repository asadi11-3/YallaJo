// Guide → Agency Roster: reject-application reason prompt.
//
// CSP-safe replacement for the previous inline <script> in
// Areas/Guide/Views/AgencyRoster/Index.cshtml (UI-UX rules A6/SEC1: no inline
// scripts without a nonce; J3/JS3: attach handlers via a single delegated
// listener, no inline on* attributes).
//
// The backend REQUIRES a non-empty rejection reason, so before a reject form
// submits we prompt for one and write it into the hidden `reason` input. A
// cancelled or blank prompt aborts the submit.
(function () {
    "use strict";

    // One delegated submit listener at the document root (JS3). Survives any
    // DOM swaps and covers every reject form on the page.
    document.addEventListener("submit", function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement)) {
            return;
        }
        if (!form.matches("[data-yj-reject-reason-form]")) {
            return;
        }

        var reasonInput = form.querySelector('input[name="reason"]');
        if (!(reasonInput instanceof HTMLInputElement)) {
            // No place to store the reason — fail safe by blocking the submit.
            event.preventDefault();
            return;
        }

        var reason = window.prompt("Reason for rejecting this application:");
        if (reason === null || reason.trim() === "") {
            event.preventDefault();
            return;
        }

        reasonInput.value = reason.trim();
    });
})();
