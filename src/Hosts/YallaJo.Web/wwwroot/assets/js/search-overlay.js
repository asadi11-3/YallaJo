/*
 * search-overlay.js — global navbar search overlay (UI-UX-S2).
 * - Triggers: any .js-search-toggle element (navbar icon + mobile nav Search slot).
 *   PE1: triggers are real links to /tours, so search still works without JS.
 * - Autocomplete: 300ms debounced GET /search/suggest via window.YallaJo.api (JS5).
 * - Recent searches: localStorage, capped at 5 (functional storage only, no consent needed per CC2).
 * Conventions: classic IIFE, data-yj-component self-init, idempotent (JS2/JS4).
 */
(function () {
    "use strict";

    var root = document.querySelector('[data-yj-component="search-overlay"]');
    if (!root || root.dataset.yjInit === "1") { return; }
    root.dataset.yjInit = "1";

    var api = window.YallaJo && window.YallaJo.api;
    var input = document.getElementById("searchOverlayInput");
    var form = document.getElementById("searchOverlayForm");
    var suggestBox = document.getElementById("searchOverlaySuggest");
    var recentWrap = document.getElementById("searchOverlayRecent");
    var recentChips = document.getElementById("searchOverlayRecentChips");
    var clearRecentBtn = document.getElementById("searchOverlayClearRecent");
    if (!input || !form || !suggestBox) { return; }

    var suggestUrl = root.dataset.suggestUrl || "/search/suggest";
    var toursUrl = root.dataset.toursUrl || "/tours";
    var noResultsText = root.dataset.noResults || "No matches found";
    var viewAllText = root.dataset.viewAll || "View all results";
    var STORAGE_KEY = "yj.recentSearches";
    var debounceTimer = null;
    var abortController = null;

    function escapeHtml(value) {
        var div = document.createElement("div");
        div.textContent = value == null ? "" : String(value);
        return div.innerHTML;
    }

    /* ---------- overlay open ---------- */

    function openOverlay() {
        if (!window.bootstrap || !window.bootstrap.Offcanvas) { return false; }
        window.bootstrap.Offcanvas.getOrCreateInstance(root).show();
        return true;
    }

    document.addEventListener("click", function (e) {
        var trigger = e.target.closest(".js-search-toggle");
        if (!trigger) { return; }
        if (openOverlay()) { e.preventDefault(); }
        /* else: fall through to the real /tours link (PE1) */
    });

    root.addEventListener("shown.bs.offcanvas", function () {
        input.focus();
        renderRecent();
    });

    root.addEventListener("hidden.bs.offcanvas", function () {
        hideSuggestions();
    });

    /* ---------- autocomplete (S2: 300ms debounce, top results + view-all) ---------- */

    function hideSuggestions() {
        suggestBox.classList.add("d-none");
        suggestBox.innerHTML = "";
        input.setAttribute("aria-expanded", "false");
    }

    function showSuggestions(html) {
        suggestBox.innerHTML = html;
        suggestBox.classList.remove("d-none");
        input.setAttribute("aria-expanded", "true");
    }

    function renderSuggestions(items, query) {
        var html = "";
        if (!items || items.length === 0) {
            html = '<div class="list-group-item text-muted small">' + escapeHtml(noResultsText) + "</div>";
        } else {
            html = items.map(function (item) {
                var slug = encodeURIComponent(item.slug || "");
                var title = escapeHtml(item.title || item.name || "");
                return '<a class="list-group-item list-group-item-action" role="option" href="' + toursUrl + "/" + slug + '">' +
                    '<i class="bi bi-search me-2 small text-muted" aria-hidden="true"></i>' + title + "</a>";
            }).join("");
        }
        html += '<a class="list-group-item list-group-item-action fw-semibold" role="option" href="' +
            toursUrl + "?q=" + encodeURIComponent(query) + '">' + escapeHtml(viewAllText) + "</a>";
        showSuggestions(html);
    }

    input.addEventListener("input", function () {
        var query = input.value.trim();
        if (debounceTimer) { window.clearTimeout(debounceTimer); }
        if (abortController) { abortController.abort(); abortController = null; }
        if (query.length < 2 || !api) {
            hideSuggestions();
            return;
        }
        debounceTimer = window.setTimeout(function () {
            abortController = new AbortController();
            api.get(suggestUrl + "?q=" + encodeURIComponent(query), { signal: abortController.signal })
                .then(function (items) { renderSuggestions(items, query); })
                .catch(function (err) {
                    if (err && err.aborted) { return; }
                    hideSuggestions(); /* NF2-lite: autocomplete failure degrades silently; form submit still works */
                });
        }, 300);
    });

    document.addEventListener("click", function (e) {
        if (!suggestBox.contains(e.target) && e.target !== input) { hideSuggestions(); }
    });

    /* ---------- recent searches (localStorage, cap 5) ---------- */

    function readRecent() {
        var raw;
        var parsed;
        try {
            raw = window.localStorage.getItem(STORAGE_KEY);
            parsed = raw ? JSON.parse(raw) : [];
            return Array.isArray(parsed) ? parsed.filter(function (v) { return typeof v === "string" && v.trim(); }) : [];
        } catch (e) { return []; }
    }

    function writeRecent(list) {
        try { window.localStorage.setItem(STORAGE_KEY, JSON.stringify(list.slice(0, 5))); } catch (e) { /* storage unavailable */ }
    }

    function addRecent(query) {
        var list = readRecent().filter(function (v) { return v.toLowerCase() !== query.toLowerCase(); });
        list.unshift(query);
        writeRecent(list);
    }

    function renderRecent() {
        if (!recentWrap || !recentChips) { return; }
        var list = readRecent();
        if (list.length === 0) {
            recentWrap.classList.add("d-none");
            recentChips.innerHTML = "";
            return;
        }
        recentChips.innerHTML = list.map(function (term) {
            return '<a class="btn btn-sm btn-light rounded-pill" href="' + toursUrl + "?q=" + encodeURIComponent(term) + '">' + escapeHtml(term) + "</a>";
        }).join("");
        recentWrap.classList.remove("d-none");
    }

    form.addEventListener("submit", function () {
        var query = input.value.trim();
        if (query) { addRecent(query); }
        /* native GET submit proceeds (PE1) */
    });

    if (clearRecentBtn) {
        clearRecentBtn.addEventListener("click", function () {
            try { window.localStorage.removeItem(STORAGE_KEY); } catch (e) { /* ignore */ }
            renderRecent();
        });
    }
})();
