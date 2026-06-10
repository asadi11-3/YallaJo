(function () {
    // Bootstrap tooltips (disabled guest wishlist buttons).
    var tooltipTriggers = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggers.forEach(function (el) {
        if (window.bootstrap && bootstrap.Tooltip) new bootstrap.Tooltip(el);
    });
    // GLightbox gallery (glightbox is loaded by the layout).
    if (window.GLightbox) { GLightbox({ selector: '.glightbox' }); }
})();

(function () {
    // Phase 8.5: tab <-> URL hash sync so a refresh/share keeps the active tab (UI-UX-S1 spirit).
    // PE1: without JS the hash is harmless and the Overview tab renders server-side.
    var tabList = document.getElementById("tour-pills-tab");
    if (!tabList || !window.bootstrap || !bootstrap.Tab) {
        return;
    }
    var buttons = [].slice.call(tabList.querySelectorAll('[data-bs-target^="#pane-"]'));
    if (!buttons.length) {
        return;
    }

    function buttonForHash(hash) {
        var i;
        if (!hash || hash.indexOf("#pane-") !== 0) {
            return null;
        }
        for (i = 0; i < buttons.length; i++) {
            if (buttons[i].getAttribute("data-bs-target") === hash) {
                return buttons[i];
            }
        }
        return null;
    }

    function openFromHash() {
        var btn = buttonForHash(window.location.hash);
        if (btn) {
            bootstrap.Tab.getOrCreateInstance(btn).show();
        }
    }

    buttons.forEach(function (btn) {
        btn.addEventListener("shown.bs.tab", function () {
            var target = btn.getAttribute("data-bs-target");
            if (target && window.location.hash !== target) {
                // replaceState avoids polluting back-button history with every tab click,
                // and avoids the native anchor jump.
                history.replaceState(null, "", target);
            }
        });
    });

    window.addEventListener("hashchange", openFromHash);
    openFromHash();
})();
