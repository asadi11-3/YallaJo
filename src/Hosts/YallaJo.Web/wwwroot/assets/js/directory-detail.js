(function () {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) { new bootstrap.Tooltip(el); });

    if (window.GLightbox) { GLightbox({ selector: '.glightbox' }); }
})();

/* Phase 8.5: opening-hours polish — highlight today's row and show an "Open now" /
   "Closed now" badge. Uses the visitor's local clock as an approximation of the
   business's local time (no timezone data in the API yet). Progressive enhancement:
   without JS the plain hours table still renders (UI-UX-PE2). */
(function () {
    var root = document.querySelector('[data-yj-component="business-hours"]');
    if (!root || root.dataset.yjInit === "1") { return; }
    root.dataset.yjInit = "1";

    var DAYS = ["sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday"];
    var now = new Date();
    var todayName = DAYS[now.getDay()];

    var rows = root.querySelectorAll("[data-yj-day]");
    var todayRow = null;
    rows.forEach(function (row) {
        if ((row.dataset.yjDay || "").toLowerCase() === todayName) { todayRow = row; }
    });
    if (!todayRow) { return; }

    // Highlight today's row.
    todayRow.classList.remove("px-0");
    todayRow.classList.add("bg-primary", "bg-opacity-10", "rounded", "px-2");

    // Compute open/closed state.
    function minutesOf(text) {
        if (!text) { return null; }
        var parts = String(text).split(":");
        var h = parseInt(parts[0], 10);
        var m = parseInt(parts[1], 10);
        if (isNaN(h) || isNaN(m)) { return null; }
        return (h * 60) + m;
    }

    var isOpen = false;
    var open;
    var close;
    var current;
    if (todayRow.dataset.yjClosed !== "true") {
        open = minutesOf(todayRow.dataset.yjOpen);
        close = minutesOf(todayRow.dataset.yjClose);
        current = (now.getHours() * 60) + now.getMinutes();
        if (open !== null && close !== null) {
            if (close > open) {
                isOpen = current >= open && current < close;
            } else if (close < open) {
                // Overnight hours (e.g. 18:00 - 02:00).
                isOpen = current >= open || current < close;
            }
        }
    }

    var badge = root.querySelector("[data-yj-open-badge]");
    if (badge) {
        badge.textContent = isOpen ? (root.dataset.openLabel || "Open now") : (root.dataset.closedLabel || "Closed now");
        badge.classList.remove("d-none");
        badge.classList.add(isOpen ? "text-bg-success" : "text-bg-secondary");
    }
})();
