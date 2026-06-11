// Provider dashboard charts (UI-UX-Design X14: ApexCharts allowed on dashboards only).
// Progressive enhancement: the SSR status list next to the chart is the no-JS path (PE1).
(function () {
    "use strict";

    var el = document.querySelector('[data-yj-component="bookings-donut"]');
    if (!el || el.dataset.yjInit === "1" || typeof window.ApexCharts === "undefined") {
        return; // PE1: without JS/ApexCharts the SSR list still conveys the data.
    }
    el.dataset.yjInit = "1"; // JS4: idempotent init.

    var labels;
    var values;
    try {
        labels = JSON.parse(el.dataset.labels || "[]");
        values = JSON.parse(el.dataset.values || "[]");
    } catch (_) {
        return;
    }
    if (!Array.isArray(values) || values.length === 0 || values.every(function (v) { return !v; })) {
        return;
    }

    var isDark = document.documentElement.getAttribute("data-bs-theme") === "dark"; // T1-T5
    var isRtl = document.documentElement.getAttribute("dir") === "rtl"; // RTL: legend stays readable

    var chart = new window.ApexCharts(el, {
        chart: { type: "donut", height: 260, fontFamily: "inherit", background: "transparent" },
        theme: { mode: isDark ? "dark" : "light" },
        series: values,
        labels: labels,
        colors: ["#f7c32e", "#0cbc87", "#066ac9", "#6c757d", "#d6293e"],
        legend: { show: false },
        dataLabels: { enabled: false },
        stroke: { width: 0 },
        plotOptions: { pie: { donut: { size: "70%" } } },
        tooltip: { enabled: true, rtl: isRtl }
    });
    chart.render();

    // T5: re-render on theme switch so dark-mode tooltips/labels stay legible.
    new MutationObserver(function () {
        var dark = document.documentElement.getAttribute("data-bs-theme") === "dark";
        chart.updateOptions({ theme: { mode: dark ? "dark" : "light" } }, false, false);
    }).observe(document.documentElement, { attributes: true, attributeFilter: ["data-bs-theme"] });
})();
