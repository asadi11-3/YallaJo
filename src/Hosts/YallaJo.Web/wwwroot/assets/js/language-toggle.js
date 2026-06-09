/*
 * language-toggle.js — wires the navbar EN/AR switch to ASP.NET Core request localization.
 * Satisfies: A6/SEC1/J3 (external CSP-safe script, no inline handlers), JS2/JS4 (idempotent, declarative),
 *            §2 i18n/RTL (sets the framework culture cookie so IStringLocalizer resolves the chosen culture).
 *
 * Why a cookie + reload: the active culture for IStringLocalizer/IViewLocalizer is server-side. Setting
 * lang/dir on <html> client-side does NOT change which .resx the server reads. The framework's
 * CookieRequestCultureProvider reads the ".AspNetCore.Culture" cookie (value format: "c=<culture>|uic=<culture>").
 * On click we write that cookie and reload so the next render uses the chosen culture (text + RTL direction).
 */
(function () {
    "use strict";

    var COOKIE_NAME = ".AspNetCore.Culture";

    function setCultureCookie(culture) {
        // Framework format: c=%LANGCODE%|uic=%LANGCODE% (URL-encoded by CookieRequestCultureProvider.MakeCookieValue)
        var value = "c=" + culture + "|uic=" + culture;
        var oneYear = 60 * 60 * 24 * 365;
        document.cookie =
            COOKIE_NAME + "=" + encodeURIComponent(value) +
            "; path=/; max-age=" + oneYear + "; samesite=lax";
    }

    function init() {
        var buttons = document.querySelectorAll(".lang-toggle-btn");
        if (!buttons.length) {
            return;
        }

        // Reflect the active culture (the framework already rendered <html lang/dir>); mark the matching button.
        var current = (document.documentElement.getAttribute("lang") || "en").slice(0, 2).toLowerCase();
        buttons.forEach(function (b) {
            if (b.dataset.yjLangInit === "1") {
                return; // JS4 idempotent
            }
            b.dataset.yjLangInit = "1";
            b.classList.toggle("active", (b.dataset.lang || "").toLowerCase() === current);
            b.addEventListener("click", function () {
                var lang = (this.dataset.lang || "en").toLowerCase();
                setCultureCookie(lang);
                // Reload so the server re-renders in the chosen culture (translated text + correct dir).
                window.location.reload();
            });
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
