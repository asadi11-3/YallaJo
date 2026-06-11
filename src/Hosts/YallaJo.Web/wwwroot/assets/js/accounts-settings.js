/**
 * Accounts tab hubs (Phases 2-3, Accounts plan).
 * - Tab <-> URL hash sync for the settings/billing/reviews hubs (UI-UX-R2; PE1: the server
 *   honours ?tab= so every hub is fully usable without this script).
 * - Delete-account checkbox gate (moved here from the retired Delete/Index inline script).
 */
(function () {
    "use strict";

    // Tab <-> hash sync (JS2/JS4: idempotent, self-initialising). One hub root per page.
    function initTabs(root) {
        if (!root || root.dataset.yjInit || !window.bootstrap || !window.bootstrap.Tab) return;
        root.dataset.yjInit = "1";

        var buttons = Array.prototype.slice.call(
            root.querySelectorAll('[data-bs-target^="#pane-"]'));

        function buttonForHash(hash) {
            var i;
            if (!hash || hash.indexOf("#") !== 0) return null;
            var target = "#pane-" + hash.slice(1).replace(/^pane-/, "");
            for (i = 0; i < buttons.length; i += 1) {
                if (buttons[i].getAttribute("data-bs-target") === target) return buttons[i];
            }
            return null;
        }

        function openFromHash() {
            var btn = buttonForHash(window.location.hash);
            if (btn) window.bootstrap.Tab.getOrCreateInstance(btn).show();
        }

        buttons.forEach(function (btn) {
            btn.addEventListener("shown.bs.tab", function () {
                var target = btn.getAttribute("data-bs-target") || "";
                var hash = "#" + target.replace("#pane-", "");
                // replaceState avoids polluting history and the native anchor jump.
                if (window.location.hash !== hash) {
                    window.history.replaceState(null, "", hash);
                }
            });
        });

        window.addEventListener("hashchange", openFromHash);
        openFromHash();
    }

    Array.prototype.slice.call(document.querySelectorAll(
        '[data-yj-component="settings-tabs"], [data-yj-component="account-tabs"]'))
        .forEach(initTabs);

    // Delete-account gate: the destructive submit stays disabled until the user ticks the box.
    var check = document.getElementById("deleteaccountCheck");
    var btn = document.getElementById("deleteAccountBtn");
    if (check && btn) {
        check.addEventListener("change", function () {
            btn.disabled = !check.checked;
        });
    }
})();
