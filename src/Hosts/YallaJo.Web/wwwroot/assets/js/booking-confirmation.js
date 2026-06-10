// Booking confirmation page behavior (Phase 8.2).
// - Copy-to-clipboard for the booking reference (toast feedback per NF1).
// - Live countdown to payment expiry when the awaiting-payment alert exposes
//   data-expires-at (progressive enhancement; the absolute time is always shown).
(function () {
    "use strict";

    // Copy booking reference.
    document.addEventListener("click", function (event) {
        var button = event.target.closest(".js-copy-ref");
        var target;
        var text;
        if (!button) {
            return;
        }
        target = document.getElementById(button.getAttribute("data-copy-target") || "");
        text = target ? target.textContent.trim() : "";
        if (!text || !navigator.clipboard) {
            return;
        }
        navigator.clipboard.writeText(text).then(function () {
            if (window.YallaJo && typeof window.YallaJo.toast === "function") {
                window.YallaJo.toast(button.getAttribute("data-copied-text") || "", "success");
            }
        }).catch(function () { /* clipboard unavailable - absolute reference is still visible */ });
    });

    // Payment expiry countdown.
    var alertEl = document.querySelector("[data-expires-at]");
    var countdownEl = document.getElementById("paymentCountdown");
    var expiresAt;
    var timer;

    function pad(value) {
        return value < 10 ? "0" + value : String(value);
    }

    function tick() {
        var remaining = expiresAt - Date.now();
        var hours;
        var minutes;
        var seconds;
        if (remaining <= 0) {
            countdownEl.textContent = "00:00:00";
            window.clearInterval(timer);
            return;
        }
        hours = Math.floor(remaining / 3600000);
        minutes = Math.floor((remaining % 3600000) / 60000);
        seconds = Math.floor((remaining % 60000) / 1000);
        countdownEl.textContent = "(" + pad(hours) + ":" + pad(minutes) + ":" + pad(seconds) + ")";
    }

    if (alertEl && countdownEl) {
        expiresAt = Date.parse(alertEl.getAttribute("data-expires-at"));
        if (!isNaN(expiresAt)) {
            tick();
            timer = window.setInterval(tick, 1000);
        }
    }
}());
