/*
 * Provider packages page enhancements (F10).
 * Upgrades [data-yj-multiselect] selects to Choices.js multi-pickers.
 * Progressive enhancement: without JS the SSR <select multiple> works (PE1).
 */
(function () {
    "use strict";

    if (typeof window.Choices !== "function") { return; }

    document.querySelectorAll("select[data-yj-multiselect]").forEach(function (select) {
        if (select.dataset.yjInit === "1") { return; }
        select.dataset.yjInit = "1";

        // eslint-disable-next-line no-new
        new window.Choices(select, {
            removeItemButton: true,
            shouldSort: false,
            searchEnabled: true,
            searchResultLimit: 20,
            itemSelectText: "",
            allowHTML: false
        });
    });
}());
