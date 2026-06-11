// Two-factor (email OTP) page: resend-code button with 60s cooldown.
// AJAX goes through window.YallaJo.api (JS5: no raw fetch — antiforgery SEC7,
// timeout, X-Requested-With come from the shared client). Feedback uses
// window.YallaJo.toast (NF1). Localized strings arrive via the
// #auth-twofactor-data JSON island (CON1); the literals below are last-resort
// fallbacks only.
(function () {
    "use strict";

    if (document.documentElement.dataset.yjAuthTwoFactorWired === "1") { return; } // JS4
    document.documentElement.dataset.yjAuthTwoFactorWired = "1";

    var dataElement = document.getElementById("auth-twofactor-data");
    var config = {};
    if (dataElement) {
        try { config = JSON.parse(dataElement.textContent || "{}"); } catch (e) { config = {}; }
    }

    var recaptchaEnabled = !!config.recaptchaEnabled;
    var recaptchaSiteKey = config.recaptchaSiteKey || "";
    var resendAction = config.resendAction || "";
    var strings = config.strings || {};
    var resendLabel = strings.resendLabel || "Click to resend";
    var countdownFormat = strings.countdownFormat || "Resend in {0}s"; // {0} = seconds remaining
    var sentMessage = strings.sent || "A new code has been sent.";
    var failedMessage = strings.failed || "Could not resend code.";
    var retryMessage = strings.retry || "Could not resend code. Please try again.";

    var btn = document.getElementById("resendBtn");
    if (!btn) { return; }

    function notify(message, type) {
        if (window.YallaJo && window.YallaJo.toast) {
            window.YallaJo.toast(message, type);
            return;
        }
        var msg = document.getElementById("resendMsg");
        if (msg) {
            msg.textContent = message;
            msg.className = "small mb-3 " + (type === "success" ? "text-success" : "text-danger");
        }
    }

    function getToken() {
        if (!recaptchaEnabled || !recaptchaSiteKey || typeof grecaptcha === "undefined") {
            return Promise.resolve("");
        }
        return new Promise(function (resolve) {
            grecaptcha.ready(function () {
                grecaptcha.execute(recaptchaSiteKey, { action: resendAction })
                    .then(resolve)
                    .catch(function () { resolve(""); });
            });
        });
    }

    function countdownText(remaining) {
        // RTL3: the seconds counter is wrapped in <bdi> so Latin digits do not
        // reorder the surrounding Arabic text.
        return countdownFormat.replace("{0}", '<bdi dir="ltr">' + remaining + "</bdi>");
    }

    function disable(seconds) {
        btn.disabled = true;
        var remaining = seconds;
        btn.innerHTML = countdownText(remaining);
        var timer = setInterval(function () {
            remaining -= 1;
            if (remaining <= 0) {
                clearInterval(timer);
                btn.disabled = false;
                btn.textContent = resendLabel;
            } else {
                btn.innerHTML = countdownText(remaining);
            }
        }, 1000);
    }

    btn.addEventListener("click", function () {
        btn.disabled = true;

        getToken().then(function (token) {
            // No email in the payload: the server resolves the pending address from
            // its encrypted pending-verification cookie (sent automatically).
            return window.YallaJo.api.post(btn.dataset.resendUrl, {
                json: { recaptchaToken: token },
                redirectOn401: false
            });
        }).then(function (data) {
            notify((data && data.message) || sentMessage, "success");
            disable(60);
        }).catch(function (err) {
            btn.disabled = false;
            notify((err && err.message) || (err && err.status ? failedMessage : retryMessage), "danger");
        });
    });
})();
