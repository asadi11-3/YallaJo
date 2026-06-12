// listing.js — generic AJAX pagination/filtering for Public listing pages (Phase 7).
// Progressive enhancement (UI-UX-PE1): pagination links and filter forms are real
// GET navigations without JS; with JS the results fragment is swapped in place
// (UI-UX-S1 pushState + popstate restore, UI-UX-L1/L4/L5 skeletons, NF2 inline retry).
(function () {
    "use strict";

    var root = document.querySelector('[data-yj-component="listing"]');
    if (!root || root.dataset.yjInit === "1") { return; }
    if (!window.YallaJo || !window.YallaJo.api) { return; } // PE1: keep native navigation
    root.dataset.yjInit = "1";

    var api = window.YallaJo.api;
    var loadToken = 0;

    function skeleton() {
        var row = document.createElement("div");
        var i;
        var col;
        row.className = "row g-4";
        for (i = 0; i < 6; i++) {
            col = document.createElement("div");
            col.className = "col-md-6 col-xl-4";
            col.innerHTML =
                '<div class="card h-100 placeholder-glow" aria-hidden="true">' +
                '<div class="placeholder card-img-h220 w-100"></div>' +
                '<div class="card-body">' +
                '<span class="placeholder col-8 mb-2"></span> ' +
                '<span class="placeholder col-5"></span>' +
                "</div></div>";
            row.appendChild(col);
        }
        root.replaceChildren(row);
    }

    function showError(url) {
        var alert = document.createElement("div");
        var span = document.createElement("span");
        var btn = document.createElement("button");
        alert.className = "alert alert-warning d-flex justify-content-between align-items-center gap-3";
        alert.setAttribute("role", "alert");
        span.textContent = root.dataset.errorText || "Couldn't load results.";
        btn.type = "button";
        btn.className = "btn btn-sm btn-outline-secondary flex-shrink-0";
        btn.textContent = root.dataset.retryText || "Try again";
        btn.addEventListener("click", function () { loadResults(url, false); });
        alert.appendChild(span);
        alert.appendChild(btn);
        root.prepend(alert);
    }

    function loadResults(url, push) {
        var token = ++loadToken;
        root.setAttribute("aria-busy", "true");
        skeleton();
        api.loadPartial(url).then(function (html) {
            if (token !== loadToken) { return; }
            root.innerHTML = html;
            root.setAttribute("aria-busy", "false");
            // V13 maps: boot any map containers that arrived with the swapped fragment.
            if (window.YallaJo.maps) { window.YallaJo.maps.scan(); }
            if (push) { window.history.pushState({ yjListing: true }, "", url); }
            var top = root.getBoundingClientRect().top + window.pageYOffset - 80;
            window.scrollTo({ top: top > 0 ? top : 0, behavior: "smooth" });
        }).catch(function () {
            if (token !== loadToken) { return; }
            root.setAttribute("aria-busy", "false");
            showError(url);
        });
    }

    // Pagination links inside the results fragment. Only same-origin links to the
    // SAME path are intercepted (card links navigate to detail routes untouched).
    root.addEventListener("click", function (e) {
        var anchor = e.target.closest ? e.target.closest("a[href]") : null;
        var url;
        if (!anchor || anchor.target === "_blank" || e.ctrlKey || e.metaKey || e.shiftKey) { return; }
        try { url = new URL(anchor.href, window.location.href); } catch (err) { return; }
        if (url.origin !== window.location.origin || url.pathname !== window.location.pathname) { return; }
        e.preventDefault();
        loadResults(url.toString(), true);
    });

    // Filter/search GET forms opt in via data-yj-listing (they live outside the
    // results fragment, e.g. the Places filter bar and the Directory hero search).
    document.addEventListener("submit", function (e) {
        var form = e.target.closest ? e.target.closest("form[data-yj-listing]") : null;
        if (!form) { return; }
        e.preventDefault();
        var params = new URLSearchParams();
        new FormData(form).forEach(function (value, key) {
            if (value !== null && String(value).trim() !== "") { params.append(key, value); }
        });
        var qs = params.toString();
        var action = form.getAttribute("action") || window.location.pathname;
        loadResults(qs ? action + "?" + qs : action, true);
    });

    window.addEventListener("popstate", function () {
        loadResults(window.location.href, false);
    });
})();
