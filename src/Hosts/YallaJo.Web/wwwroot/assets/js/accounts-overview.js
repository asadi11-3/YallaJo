// Accounts Overview — ERR3 retryable rails (Phase 5a).
// Each [data-yj-component="overview-section"] whose data failed to load shows a
// "Couldn't load · Retry" card. The Retry link is a real anchor to the full Overview
// page (PE1 fallback); when JS is available we intercept it and reload just that
// section via the shared api-client (window.YallaJo.api.loadPartial).
(function () {
    "use strict";

    var api = window.YallaJo && window.YallaJo.api;
    if (!api) {
        return; // PE1: retry links fall back to a full-page reload.
    }

    var sections = document.querySelectorAll('[data-yj-component="overview-section"]');
    var i;
    for (i = 0; i < sections.length; i++) {
        initSection(sections[i]);
    }

    function initSection(root) {
        if (!root || root.dataset.yjInit === "1") {
            return;
        }
        root.dataset.yjInit = "1";
        root.addEventListener("click", function (e) {
            var trigger = e.target && e.target.closest ? e.target.closest(".js-overview-retry") : null;
            if (!trigger || !root.contains(trigger)) {
                return;
            }
            e.preventDefault();
            reload(root);
        });
    }

    function reload(root) {
        var url = root.dataset.reloadUrl;
        if (!url) {
            return;
        }
        root.setAttribute("aria-busy", "true");
        api.loadPartial(url).then(function (html) {
            root.innerHTML = html;
            root.removeAttribute("aria-busy");
        }).catch(function (err) {
            root.removeAttribute("aria-busy");
            if (window.YallaJo.toast) {
                window.YallaJo.toast((err && err.message) || "Could not reload this section.", "error");
            }
        });
    }
}());
