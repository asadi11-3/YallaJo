// Admin statistics chart: ApexCharts init for the Admin > Statistics > Index page.
//
// CSP-safe replacement for the previous inline <script> in
// Areas/Admin/Views/Statistics/Index.cshtml (UI-UX rules A6/SEC1: no inline
// executable scripts without a nonce; this file is external so no nonce needed).
//
// JS2: page-specific bundle, not a monolith. JS4: idempotent init guard so the
// chart never double-renders when markup is re-injected. M3: chart animations
// are disabled under prefers-reduced-motion. CON1: all chart copy comes from the
// server-localized JSON data island, never hard-coded here.
//
// Server data arrives via a non-executable <script type="application/json"
// data-yj-component="admin-statistics"> island (no JWTs or secrets — X9/X15).
(function () {
    "use strict";

    function init() {
        var root = document.querySelector('[data-yj-component="admin-statistics"]');
        if (!root) {
            return;
        }
        // Idempotent init (JS4): bail if we already rendered for this island.
        if (root.dataset.yjInit === "1") {
            return;
        }
        root.dataset.yjInit = "1";

        if (typeof ApexCharts === "undefined") {
            return;
        }

        var data;
        try {
            data = JSON.parse(root.textContent || "{}");
        } catch (e) {
            return;
        }

        var typeLabels = Array.isArray(data.typeLabels) ? data.typeLabels : [];
        var typeCounts = Array.isArray(data.typeCounts) ? data.typeCounts : [];

        // Theme-aware colours: read the live Bootstrap body colour so the chart
        // tracks light/dark mode (T3) without hard-coding ink.
        var ink = getComputedStyle(document.body).getPropertyValue("--bs-body-color").trim() || "#212529";

        // M3: respect reduced-motion — disable ApexCharts entrance animation.
        var reduceMotion = window.matchMedia
            && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        var animations = { enabled: !reduceMotion };

        var chartEl = document.getElementById("ChartTypes");
        if (chartEl && typeCounts.length) {
            new ApexCharts(chartEl, {
                chart: { type: "donut", height: 320, fontFamily: "inherit", animations: animations },
                series: typeCounts,
                labels: typeLabels,
                colors: ["#5143d9", "#0cbc87", "#4f9ef8", "#f7c32e", "#d6293e"],
                legend: { position: "bottom", labels: { colors: ink } },
                dataLabels: { enabled: true }
            }).render();
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
