// Provider availability calendar ([Backend] B3 / CAL1).
// Lazily loads a server-rendered month grid per month via the Web proxy
// (api.loadPartial, JS5). The slot table is the no-JS path (PE1) — this
// card stays hidden unless JS + the typed client are available.
(function () {
    "use strict";

    var card = document.querySelector('[data-yj-component="availability-calendar"]');
    if (!card || card.dataset.yjInit === "1") return;
    if (!window.YallaJo || !window.YallaJo.api) return; // PE1
    card.dataset.yjInit = "1";

    var host = card.querySelector("[data-yj-calendar-host]");
    var baseUrl = card.dataset.yjCalendarUrl;
    if (!host || !baseUrl) return;

    card.classList.remove("d-none");

    var loadToken = 0;

    function skeleton() {
        host.innerHTML =
            '<div class="placeholder-glow">' +
            '<span class="placeholder col-12 mb-2" style="height:2rem;"></span>' +
            '<span class="placeholder col-12" style="height:10rem;"></span>' +
            "</div>";
    }

    function showError(year, month) {
        var wrap = document.createElement("div");
        wrap.className = "alert alert-warning d-flex justify-content-between align-items-center";
        wrap.setAttribute("role", "alert");
        var msg = document.createElement("span");
        msg.textContent = card.dataset.errorText || "Error";
        var retry = document.createElement("button");
        retry.type = "button";
        retry.className = "btn btn-sm btn-outline-secondary";
        retry.textContent = card.dataset.retryText || "Retry";
        retry.addEventListener("click", function () { load(year, month); });
        wrap.appendChild(msg);
        wrap.appendChild(retry);
        host.innerHTML = "";
        host.appendChild(wrap);
    }

    function load(year, month) {
        var token = ++loadToken;
        host.setAttribute("aria-busy", "true");
        skeleton();

        var url = baseUrl + (baseUrl.indexOf("?") >= 0 ? "&" : "?") +
            "year=" + year + "&month=" + month;

        window.YallaJo.api.loadPartial(url)
            .then(function (html) {
                if (token !== loadToken) return; // stale response
                host.innerHTML = html;
                host.setAttribute("aria-busy", "false");
            })
            .catch(function () {
                if (token !== loadToken) return;
                host.setAttribute("aria-busy", "false");
                showError(year, month);
            });
    }

    // Month navigation (delegated — the grid is replaced each load).
    host.addEventListener("click", function (e) {
        var nav = e.target.closest("[data-yj-cal-nav]");
        if (!nav) return;
        load(parseInt(nav.dataset.yjCalYear, 10), parseInt(nav.dataset.yjCalMonth, 10));
    });

    var now = new Date();
    load(now.getFullYear(), now.getMonth() + 1);
})();
