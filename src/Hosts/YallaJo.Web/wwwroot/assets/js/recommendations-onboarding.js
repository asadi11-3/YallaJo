/*
 * recommendations-onboarding.js — §3.6 cold-start onboarding quiz.
 *
 * Each recommendation row offers a 👍 (Interested) / 👎 (NotInterested) radio pair whose
 * <label> carries data-onb-token="Kind:EntityId" + data-onb-list="Interested|NotInterested".
 * On submit we materialise the selected choices into hidden inputs named exactly
 * "Interested" / "NotInterested" (List<string> on OnboardingFormVm) so the page-scoped
 * POST /accounts/recommendations/onboarding binds correctly. CSP-friendly: external file,
 * no inline script (SEC1/A6); progressive-enhancement — if JS is off the form simply posts
 * empty lists and the server returns a validation message (PE1).
 */
(function () {
    "use strict";

    var form = document.querySelector('form[action$="/recommendations/onboarding"]');
    if (!form || form.dataset.yjInit === "1") {
        return; // JS4 idempotent init
    }
    form.dataset.yjInit = "1";

    var hiddenHost = form.querySelector("#onb-hidden-inputs");
    if (!hiddenHost) {
        return;
    }

    form.addEventListener("submit", function () {
        // Rebuild the hidden inputs from the currently-checked radios.
        hiddenHost.innerHTML = "";

        var checked = form.querySelectorAll("input.btn-check:checked");
        checked.forEach(function (radio) {
            var label = form.querySelector('label[for="' + cssEscape(radio.id) + '"]');
            if (!label) {
                return;
            }
            var token = label.getAttribute("data-onb-token");
            var list = label.getAttribute("data-onb-list");
            if (!token || (list !== "Interested" && list !== "NotInterested")) {
                return;
            }
            var input = document.createElement("input");
            input.type = "hidden";
            input.name = list; // binds to OnboardingFormVm.Interested / .NotInterested (List<string>)
            input.value = token;
            hiddenHost.appendChild(input);
        });
    });

    function cssEscape(value) {
        if (window.CSS && window.CSS.escape) {
            return window.CSS.escape(value);
        }
        return String(value).replace(/[^a-zA-Z0-9_-]/g, "\\$&");
    }
})();
