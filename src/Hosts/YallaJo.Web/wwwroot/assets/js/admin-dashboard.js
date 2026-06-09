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
// Server data arrives via a non-executable <script type="application/json"
// data-yj-component="admin-dashboard"> island (no JWTs or secrets — X9/X15).
(function () {
    "use strict";

    function init() {
        var root = document.querySelector('[data-yj-component="admin-dashboard"]');
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

        var revenueData = Array.isArray(data.revenue) ? data.revenue : [];
        var bookingsData = Array.isArray(data.bookings) ? data.bookings : [];
        var usersData = Array.isArray(data.users) ? data.users : [];
        var labels = data.labels || {};

        // Theme-aware colours: read the live Bootstrap body colour so charts
        // track light/dark mode (T3) without hard-coding ink.
        var ink = getComputedStyle(document.body).getPropertyValue("--bs-body-color").trim() || "#212529";
        var grid = "rgba(120,120,120,.15)";

        // M3: respect reduced-motion — disable ApexCharts entrance animation.
        var reduceMotion = window.matchMedia
            && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        var animations = { enabled: !reduceMotion };

        function el(id) {
            return document.getElementById(id);
        }

        var revenueEl = el("ChartRevenue");
        if (revenueEl && revenueData.length) {
            new ApexCharts(revenueEl, {
                chart: { type: "area", height: 300, toolbar: { show: false }, fontFamily: "inherit", animations: animations },
                series: [{ name: labels.revenue || "", data: revenueData }],
                colors: ["#5143d9"],
                dataLabels: { enabled: false },
                stroke: { curve: "smooth", width: 2 },
                fill: { type: "gradient", gradient: { opacityFrom: 0.25, opacityTo: 0.02 } },
                xaxis: { type: "datetime", labels: { style: { colors: ink } } },
                yaxis: { labels: { style: { colors: ink } } },
                grid: { borderColor: grid },
                tooltip: { x: { format: "dd MMM yyyy" } }
            }).render();
        }

        var bookingsEl = el("ChartBookings");
        if (bookingsEl && bookingsData.some(function (v) { return v > 0; })) {
            new ApexCharts(bookingsEl, {
                chart: { type: "donut", height: 300, fontFamily: "inherit", animations: animations },
                series: bookingsData,
                labels: [labels.completed || "", labels.cancelled || "", labels.other || ""],
                colors: ["#0cbc87", "#d6293e", "#4f9ef8"],
                legend: { position: "bottom", labels: { colors: ink } },
                dataLabels: { enabled: true }
            }).render();
        }

        var usersEl = el("ChartUsers");
        if (usersEl && usersData.some(function (v) { return v > 0; })) {
            new ApexCharts(usersEl, {
                chart: { type: "bar", height: 300, toolbar: { show: false }, fontFamily: "inherit", animations: animations },
                series: [{ name: labels.users || "", data: usersData }],
                colors: ["#4f9ef8"],
                plotOptions: { bar: { columnWidth: "45%", borderRadius: 4, distributed: true } },
                legend: { show: false },
                dataLabels: { enabled: false },
                xaxis: { categories: [labels.totalUsers || "", labels.newUsers || ""], labels: { style: { colors: ink } } },
                yaxis: { labels: { style: { colors: ink } } },
                grid: { borderColor: grid }
            }).render();
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
