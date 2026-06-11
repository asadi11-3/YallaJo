// provider-finance.js — hash-based tab activation for /provider/finance (Provider area).
// The retired /provider/invoices and /provider/payment-methods pages 301 into
// /provider/finance#invoices | #methods; this script opens the matching tab.
// All three tabs are SSR'd by the controller, so the page works without JS (PE1);
// this is a pure refinement layer (PE2). No fetch here — nothing leaves the page (JS5).
(function () {
    "use strict";

    var HASH_TO_TAB = {
        "#invoices": "tab-invoices",
        "#methods": "tab-methods",
        "#payouts": "tab-payouts"
    };

    function activateFromHash() {
        var tabId = HASH_TO_TAB[(location.hash || "").toLowerCase()];
        if (!tabId) { return; }
        var trigger = document.getElementById(tabId);
        if (!trigger || !window.bootstrap || !window.bootstrap.Tab) { return; }
        window.bootstrap.Tab.getOrCreateInstance(trigger).show();
    }

    function init() {
        // Only on pages that actually have the finance tab strip (JS4 idempotent).
        if (!document.getElementById("tab-payouts")) { return; }

        activateFromHash();
        window.addEventListener("hashchange", activateFromHash);

        // Keep the hash in sync when the user switches tabs, so reloads and
        // copied links restore the same tab (S1-style state in the URL).
        ["tab-payouts", "tab-invoices", "tab-methods"].forEach(function (id) {
            var btn = document.getElementById(id);
            if (!btn) { return; }
            btn.addEventListener("shown.bs.tab", function () {
                var hash = "#" + id.replace("tab-", "");
                if (location.hash !== hash) {
                    history.replaceState(null, "", hash);
                }
            });
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
