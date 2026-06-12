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
// Phase 9 four-states (§4.8): the chart host renders with a server-side shimmer
// skeleton reserving the final height (zero CLS — L1). This script removes it on
// render completion (settle) or swaps in an inline retryable error (ERR3) using
// the host's localized data-error-text/data-retry-text (A11Y9 aria-live).
//
// Server data arrives via a non-executable <script type="application/json"
// data-yj-component="admin-statistics"> island (no JWTs or secrets — X9/X15).
(function () {
    "use strict";

    // Removes the shimmer skeleton overlay and marks the chart region settled.
    function settle(host) {
        var skeleton;
        if (!host) {
            return;
        }
        skeleton = host.querySelector("[data-yj-skeleton]");
        if (skeleton && skeleton.parentNode) {
            skeleton.parentNode.removeChild(skeleton);
        }
        host.setAttribute("aria-busy", "false");
    }

    // Inline retryable error inside the chart region (ERR3). Copy comes from the
    // server-localized data attributes (CON1).
    function showChartError(host, retry) {
        var wrap;
        var text;
        var btn;
        if (!host) {
            return;
        }
        wrap = document.createElement("div");
        wrap.className = "alert alert-warning d-flex align-items-center justify-content-between gap-2 m-0";
        wrap.setAttribute("role", "alert");
        text = document.createElement("span");
        text.textContent = host.getAttribute("data-error-text") || "";
        btn = document.createElement("button");
        btn.type = "button";
        btn.className = "btn btn-sm btn-outline-secondary";
        btn.textContent = host.getAttribute("data-retry-text") || "";
        btn.addEventListener("click", function () {
            host.setAttribute("aria-busy", "true");
            while (host.firstChild) {
                host.removeChild(host.firstChild);
            }
            retry();
        });
        wrap.appendChild(text);
        wrap.appendChild(btn);
        while (host.firstChild) {
            host.removeChild(host.firstChild);
        }
        host.appendChild(wrap);
        host.setAttribute("aria-busy", "false");
    }

    // Renders the ApexCharts instance and settles/fails the region.
    function renderChart(host, options) {
        function attempt() {
            var chart;
            try {
                chart = new ApexCharts(host, options);
                chart.render().then(function () {
                    settle(host);
                }).catch(function () {
                    showChartError(host, attempt);
                });
            } catch (e) {
                showChartError(host, attempt);
            }
        }
        if (!host) {
            return;
        }
        attempt();
    }

    function init() {
        var root;
        var data;
        var typeLabels;
        var typeCounts;
        var ink;
        var reduceMotion;
        var animations;
        var chartEl;

        root = document.querySelector('[data-yj-component="admin-statistics"]');
        if (!root) {
            return;
        }
        // Idempotent init (JS4): bail if we already rendered for this island.
        if (root.dataset.yjInit === "1") {
            return;
        }
        root.dataset.yjInit = "1";

        chartEl = document.getElementById("ChartTypes");

        if (typeof ApexCharts === "undefined") {
            showChartError(chartEl, function () {
                root.dataset.yjInit = "";
                init();
            });
            return;
        }

        try {
            data = JSON.parse(root.textContent || "{}");
        } catch (e) {
            showChartError(chartEl, function () {
                root.dataset.yjInit = "";
                init();
            });
            return;
        }

        typeLabels = Array.isArray(data.typeLabels) ? data.typeLabels : [];
        typeCounts = Array.isArray(data.typeCounts) ? data.typeCounts : [];

        // Theme-aware colours: read the live Bootstrap body colour so the chart
        // tracks light/dark mode (T3) without hard-coding ink.
        ink = getComputedStyle(document.body).getPropertyValue("--bs-body-color").trim() || "#212529";

        // M3: respect reduced-motion — disable ApexCharts entrance animation.
        reduceMotion = window.matchMedia
            && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        animations = { enabled: !reduceMotion };

        if (chartEl && typeCounts.length) {
            renderChart(chartEl, {
                chart: { type: "donut", height: 320, fontFamily: "inherit", animations: animations },
                series: typeCounts,
                labels: typeLabels,
                colors: ["#b02a2a", "#3e6b4a", "#4f9ef8", "#c9a24b", "#d6293e"],
                legend: { position: "bottom", labels: { colors: ink } },
                dataLabels: { enabled: true }
            });
        } else {
            // Server renders the composed empty state instead of the host; clear
            // any orphan skeleton so it never shimmers forever.
            settle(chartEl);
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
