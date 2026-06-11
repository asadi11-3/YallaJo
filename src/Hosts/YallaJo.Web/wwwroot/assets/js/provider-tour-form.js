// Provider tour form enhancements.
// [Backend] B2 / F10: upgrades the SSR place <select> into an async searchable
// combobox backed by the Web proxy (JS5 — browser never calls the API host).
// Without JS (or when Choices.js is unavailable) the plain select keeps working (PE1).
(function () {
    "use strict";

    var select = document.querySelector("[data-yj-place-combobox]");
    if (!select || select.dataset.yjInit === "1") return;
    if (typeof window.Choices !== "function") return; // PE1: keep native select
    select.dataset.yjInit = "1";

    var lookupUrl = select.dataset.yjLookupUrl;
    var noResults = select.dataset.yjNoResults || "";
    var searchPlaceholder = select.dataset.yjSearchPlaceholder || "";

    var choices = new window.Choices(select, {
        searchEnabled: true,
        shouldSort: false,
        searchResultLimit: 10,
        searchPlaceholderValue: searchPlaceholder,
        noResultsText: noResults,
        itemSelectText: "",
        allowHTML: false
    });

    if (!lookupUrl) return;

    var debounceTimer = null;
    var controller = null;

    select.addEventListener("search", function (event) {
        var term = event.detail && event.detail.value ? event.detail.value.trim() : "";
        if (term.length < 2) return;

        // J7: debounce ≥300ms; JS6: abort the in-flight lookup.
        if (debounceTimer) clearTimeout(debounceTimer);
        debounceTimer = setTimeout(function () {
            if (controller) controller.abort();
            controller = new AbortController();

            var url = lookupUrl + (lookupUrl.indexOf("?") >= 0 ? "&" : "?") +
                "term=" + encodeURIComponent(term);

            fetchSuggestions(url, controller.signal);
        }, 300);
    });

    function fetchSuggestions(url, signal) {
        // JS5: same-origin Web proxy via the typed client.
        if (!window.YallaJo || !window.YallaJo.api) return;
        window.YallaJo.api.get(url, { signal: signal })
            .then(function (items) {
                if (!Array.isArray(items)) return;
                var current = choices.getValue(true);
                choices.setChoices(
                    items.map(function (p) {
                        return {
                            value: String(p.id),
                            label: p.city ? p.name + " — " + p.city : p.name,
                            selected: String(p.id) === String(current)
                        };
                    }),
                    "value", "label", true);
            })
            .catch(function () { /* suggestions are best-effort; SSR options remain */ });
    }
})();

/*
 * Tour draft autosave (F5/PROV3/PROV4/F9).
 * - Saves form fields to localStorage under data-yj-draft-key (debounced).
 * - Shows a recovery banner when a draft exists on load (PROV3).
 * - Clears the draft on successful submit; warns before unload when dirty (F9).
 * Progressive enhancement only: without JS the form posts normally (PE1).
 */
(function () {
    "use strict";

    var form = document.querySelector("form[data-yj-tour-form]");
    if (!form || form.dataset.yjDraftInit === "1") { return; }
    form.dataset.yjDraftInit = "1";

    var key = form.dataset.yjDraftKey;
    if (!key || !window.localStorage) { return; }

    var dirty = false;
    var saveTimer = null;

    function fieldList() {
        return Array.prototype.filter.call(
            form.querySelectorAll("input[name], select[name], textarea[name]"),
            function (el) {
                return el.type !== "hidden" && el.type !== "submit" && el.name !== "__RequestVerificationToken";
            });
    }

    function snapshot() {
        var data = {};
        fieldList().forEach(function (el) {
            if (el.type === "checkbox") { data[el.name] = el.checked; }
            else { data[el.name] = el.value; }
        });
        return data;
    }

    function save() {
        try { window.localStorage.setItem(key, JSON.stringify({ savedAt: Date.now(), data: snapshot() })); }
        catch (_) { /* storage full/blocked — autosave silently off */ }
    }

    function clearDraft() {
        try { window.localStorage.removeItem(key); } catch (_) { }
        dirty = false;
    }

    function restore(data) {
        fieldList().forEach(function (el) {
            if (!(el.name in data)) { return; }
            if (el.type === "checkbox") { el.checked = !!data[el.name]; }
            else { el.value = data[el.name]; }
        });
        if (window.YallaJo && window.YallaJo.toast) {
            window.YallaJo.toast(form.dataset.yjDraftRestored || "Draft restored.", "success");
        }
    }

    function showBanner(data) {
        var banner = document.createElement("div");
        banner.className = "alert alert-info d-flex flex-wrap align-items-center gap-2 mb-3";
        banner.setAttribute("role", "status");
        var msg = document.createElement("span");
        msg.className = "me-auto";
        msg.textContent = form.dataset.yjDraftFound || "You have an unsaved draft.";
        var restoreBtn = document.createElement("button");
        restoreBtn.type = "button";
        restoreBtn.className = "btn btn-sm btn-primary";
        restoreBtn.textContent = form.dataset.yjDraftRestore || "Restore draft";
        var discardBtn = document.createElement("button");
        discardBtn.type = "button";
        discardBtn.className = "btn btn-sm btn-outline-secondary";
        discardBtn.textContent = form.dataset.yjDraftDiscard || "Discard draft";
        restoreBtn.addEventListener("click", function () {
            restore(data);
            banner.remove();
        });
        discardBtn.addEventListener("click", function () {
            clearDraft();
            banner.remove();
        });
        banner.appendChild(msg);
        banner.appendChild(restoreBtn);
        banner.appendChild(discardBtn);
        form.insertBefore(banner, form.firstChild);
    }

    // PROV3: offer recovery when a previous draft exists.
    try {
        var raw = window.localStorage.getItem(key);
        if (raw) {
            var parsed = JSON.parse(raw);
            if (parsed && parsed.data) { showBanner(parsed.data); }
        }
    } catch (_) { clearDraft(); }

    // Debounced autosave on input/change (F5).
    function onEdit() {
        dirty = true;
        if (saveTimer) { window.clearTimeout(saveTimer); }
        saveTimer = window.setTimeout(save, 800);
    }
    form.addEventListener("input", onEdit);
    form.addEventListener("change", onEdit);

    // Successful submit clears the draft and disarms the unload guard.
    form.addEventListener("submit", function () {
        clearDraft();
    });

    // F9: warn before navigating away with unsaved edits.
    window.addEventListener("beforeunload", function (e) {
        if (!dirty) { return; }
        e.preventDefault();
        e.returnValue = "";
    });
}());
