// provider-listing.js — AJAX pagination/filtering for Provider listing pages.
// Mirrors the Public listing.js pattern (UI-UX-PE1 progressive enhancement,
// S1 pushState + popstate restore, L1/L4 table skeleton, NF2 inline retry, JS5
// all traffic through window.YallaJo.api). Adds cursor "Load more" support for
// the high-volume bookings list (plan §4.6 sanctioned exception):
//   <button data-yj-load-more data-url="...">  inside the fragment appends the
//   next fragment's [data-yj-rows] rows and replaces the button.
(function () {
    "use strict";

    var root = document.querySelector('[data-yj-component="provider-listing"]');
    if (!root || root.dataset.yjInit === "1") { return; }
    if (!window.YallaJo || !window.YallaJo.api) { return; } // PE1: keep native navigation
    root.dataset.yjInit = "1";

    var api = window.YallaJo.api;
    var loadToken = 0;

    function skeleton() {
        var card = document.createElement("div");
        var rows = "";
        var i;
        for (i = 0; i < 6; i++) {
            rows += '<div class="d-flex gap-3 py-3 border-bottom px-3">' +
                '<span class="placeholder col-3"></span>' +
                '<span class="placeholder col-2"></span>' +
                '<span class="placeholder col-2"></span>' +
                '<span class="placeholder col-1 ms-auto"></span>' +
                "</div>";
        }
        card.className = "card border placeholder-glow";
        card.setAttribute("aria-hidden", "true");
        card.innerHTML = rows;
        root.replaceChildren(card);
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
            if (push) { window.history.pushState({ yjListing: true }, "", url); }
            var top = root.getBoundingClientRect().top + window.pageYOffset - 80;
            window.scrollTo({ top: top > 0 ? top : 0, behavior: "smooth" });
        }).catch(function () {
            if (token !== loadToken) { return; }
            root.setAttribute("aria-busy", "false");
            showError(url);
        });
    }

    // Cursor "Load more": fetch the next fragment, append its rows, swap the button.
    function loadMore(button) {
        var url = button.dataset.url;
        if (!url || button.disabled) { return; }
        var tbody = root.querySelector("[data-yj-rows]");
        button.disabled = true;
        button.setAttribute("aria-busy", "true");
        api.loadPartial(url).then(function (html) {
            var template = document.createElement("template");
            template.innerHTML = html;
            var newRows = template.content.querySelector("[data-yj-rows]");
            var newButton = template.content.querySelector("[data-yj-load-more]");
            if (tbody && newRows) {
                while (newRows.firstChild) { tbody.appendChild(newRows.firstChild); }
            }
            if (newButton) {
                button.replaceWith(newButton);
            } else {
                var wrap = button.closest("[data-yj-load-more-wrap]");
                (wrap || button).remove();
            }
        }).catch(function () {
            button.disabled = false;
            button.setAttribute("aria-busy", "false");
            if (window.YallaJo.toast) {
                window.YallaJo.toast(root.dataset.errorText || "Couldn't load results.", "error");
            }
        });
    }

    // Pagination links inside the results fragment. Only same-origin links to the
    // SAME path are intercepted (row action links navigate to other routes untouched).
    root.addEventListener("click", function (e) {
        var more = e.target.closest ? e.target.closest("[data-yj-load-more]") : null;
        if (more) { e.preventDefault(); loadMore(more); return; }
        var anchor = e.target.closest ? e.target.closest("a[href]") : null;
        var url;
        if (!anchor || anchor.target === "_blank" || e.ctrlKey || e.metaKey || e.shiftKey) { return; }
        try { url = new URL(anchor.href, window.location.href); } catch (err) { return; }
        if (url.origin !== window.location.origin || url.pathname !== window.location.pathname) { return; }
        e.preventDefault();
        loadResults(url.toString(), true);
    });

    // Filter GET forms opt in via data-yj-listing (they live outside the fragment).
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
