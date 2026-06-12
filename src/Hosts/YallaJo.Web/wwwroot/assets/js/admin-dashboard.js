// Admin dashboard charts: ApexCharts init for the Admin > Home > Index page.
//
// CSP-safe replacement for the previous inline <script> in
// Areas/Admin/Views/Home/Index.cshtml (UI-UX rules A6/SEC1: no inline executable
// scripts without a nonce; this file is external so no nonce is needed).
//
// JS2: page-specific bundle, not a monolith. JS4: idempotent init guard so the
// charts never double-render when markup is re-injected. M3: chart animations
// are disabled under prefers-reduced-motion. CON1: all chart copy comes from the
// server-localized JSON data island, never hard-coded here.
//
// Phase 9 four-states (§4.8): each chart host renders with a server-side shimmer
// skeleton that reserves the final height (zero CLS — L1). This script removes
// the skeleton when the chart settles (settle), or swaps in an inline retryable
// error (ERR3) using the host's localized data-error-text/data-retry-text.
// Each section resolves independently (L4) and announces politely (A11Y9).
//
// Server data arrives via a non-executable <script type="application/json"
// data-yj-component="admin-dashboard"> island (no JWTs or secrets — X9/X15).
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
    // server-localized data attributes (CON1); the button re-runs the supplied
    // retry callback after clearing the region.
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

    // Renders one ApexCharts instance and settles/fails its region independently
    // (L4: the page never blocks on the slowest section).
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
        var revenueData;
        var bookingsData;
        var usersData;
        var labels;
        var ink;
        var grid;
        var reduceMotion;
        var animations;
        var revenueEl;
        var bookingsEl;
        var usersEl;
        var hosts;

        root = document.querySelector('[data-yj-component="admin-dashboard"]');
        if (!root) {
            return;
        }
        // Idempotent init (JS4): bail if we already rendered for this island.
        if (root.dataset.yjInit === "1") {
            return;
        }
        root.dataset.yjInit = "1";

        revenueEl = document.getElementById("ChartRevenue");
        bookingsEl = document.getElementById("ChartBookings");
        usersEl = document.getElementById("ChartUsers");
        hosts = [revenueEl, bookingsEl, usersEl];

        // Global failure (ApexCharts missing / island unreadable): every present
        // chart region gets its own inline retryable error (ERR3).
        function failAll() {
            hosts.forEach(function (h) {
                if (h) {
                    showChartError(h, function () {
                        root.dataset.yjInit = "";
                        init();
                    });
                }
            });
        }

        if (typeof ApexCharts === "undefined") {
            failAll();
            return;
        }

        try {
            data = JSON.parse(root.textContent || "{}");
        } catch (e) {
            failAll();
            return;
        }

        revenueData = Array.isArray(data.revenue) ? data.revenue : [];
        bookingsData = Array.isArray(data.bookings) ? data.bookings : [];
        usersData = Array.isArray(data.users) ? data.users : [];
        labels = data.labels || {};

        // Theme-aware colours: read the live Bootstrap body colour so charts
        // track light/dark mode (T3) without hard-coding ink.
        ink = getComputedStyle(document.body).getPropertyValue("--bs-body-color").trim() || "#212529";
        grid = "rgba(120,120,120,.15)";

        // M3: respect reduced-motion — disable ApexCharts entrance animation.
        reduceMotion = window.matchMedia
            && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        animations = { enabled: !reduceMotion };

        if (revenueEl && revenueData.length) {
            renderChart(revenueEl, {
                chart: { type: "area", height: 300, toolbar: { show: false }, fontFamily: "inherit", animations: animations },
                series: [{ name: labels.revenue || "", data: revenueData }],
                colors: ["#b02a2a"],
                dataLabels: { enabled: false },
                stroke: { curve: "smooth", width: 2 },
                fill: { type: "gradient", gradient: { opacityFrom: 0.25, opacityTo: 0.02 } },
                xaxis: { type: "datetime", labels: { style: { colors: ink } } },
                yaxis: { labels: { style: { colors: ink } } },
                grid: { borderColor: grid },
                tooltip: { x: { format: "dd MMM yyyy" } }
            });
        } else {
            // Server renders the composed empty state instead of the host; if the
            // host exists with no data, clear its skeleton so it never shimmers
            // forever.
            settle(revenueEl);
        }

        if (bookingsEl && bookingsData.some(function (v) { return v > 0; })) {
            renderChart(bookingsEl, {
                chart: { type: "donut", height: 300, fontFamily: "inherit", animations: animations },
                series: bookingsData,
                labels: [labels.completed || "", labels.cancelled || "", labels.other || ""],
                colors: ["#3e6b4a", "#d6293e", "#4f9ef8"],
                legend: { position: "bottom", labels: { colors: ink } },
                dataLabels: { enabled: true }
            });
        } else {
            settle(bookingsEl);
        }

        if (usersEl && usersData.some(function (v) { return v > 0; })) {
            renderChart(usersEl, {
                chart: { type: "bar", height: 300, toolbar: { show: false }, fontFamily: "inherit", animations: animations },
                series: [{ name: labels.users || "", data: usersData }],
                colors: ["#4f9ef8"],
                plotOptions: { bar: { columnWidth: "45%", borderRadius: 4, distributed: true } },
                legend: { show: false },
                dataLabels: { enabled: false },
                xaxis: { categories: [labels.totalUsers || "", labels.newUsers || ""], labels: { style: { colors: ink } } },
                yaxis: { labels: { style: { colors: ink } } },
                grid: { borderColor: grid }
            });
        } else {
            settle(usersEl);
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
