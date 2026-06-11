// Guide proposals page enhancements.
// [Backend] B2 / F10: upgrades the SSR place <select> into an async searchable
// combobox backed by the Web proxy (JS5 — browser never calls the API host).
// Without JS (or when Choices.js is unavailable) the plain select keeps working (PE1).
(function () {
    "use strict";

    var select = document.querySelector(".js-place-picker");
    if (!select || select.dataset.yjInit === "1") return;
    if (typeof window.Choices !== "function") return; // PE1: keep native select
    select.dataset.yjInit = "1";

    var lookupUrl = select.dataset.lookupUrl;
    var noResults = select.dataset.noResults || "";

    var choices = new window.Choices(select, {
        searchEnabled: true,
        shouldSort: false,
        searchResultLimit: 20,
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

            fetchOptions(url, controller.signal);
        }, 300);
    });

    function fetchOptions(url, signal) {
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
                            label: p.label,
                            selected: String(p.id) === String(current)
                        };
                    }),
                    "value", "label", true);
            })
            .catch(function () { /* options are best-effort; SSR options remain */ });
    }
})();
