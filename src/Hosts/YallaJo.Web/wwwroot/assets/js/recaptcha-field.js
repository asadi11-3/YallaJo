// CSP-safe externalized reCAPTCHA v3 field handler (replaces the former inline
// <script> in Areas/Auth/Shared/_RecaptchaField.cshtml so script-src needs no
// 'unsafe-inline' for the auth funnel). Config is read from data-* attributes on
// the hidden token input: data-recaptcha-sitekey, data-recaptcha-action.
// On the owning form's submit, executes grecaptcha, writes the token into the
// hidden input, then resubmits. Supports multiple reCAPTCHA fields per page.
(function () {
    "use strict";

    function wire(input) {
        var siteKey = input.getAttribute("data-recaptcha-sitekey");
        var action = input.getAttribute("data-recaptcha-action") || "generic";
        if (!siteKey) {
            return;
        }

        var form = input.closest("form");
        if (!form) {
            return;
        }

        form.addEventListener("submit", function (e) {
            // Already obtained a token on a previous pass -> let it submit.
            if (input.dataset.submitted === "1") {
                return;
            }

            // Client-side validation gate: when jQuery unobtrusive validation
            // is active and the form is invalid, stand down -- the validator
            // cancels this submit, and the form.submit() below must never
            // bypass it.
            if (window.jQuery && window.jQuery.fn && window.jQuery.fn.valid && !window.jQuery(form).valid()) {
                return;
            }

            e.preventDefault();

            // Engage the submit loading state here (L2/F7): this pass is
            // cancelled and the real submission goes through form.submit(),
            // which fires no submit event, so form-ux.js cannot pick it up.
            if (form.hasAttribute("data-loading") && window.YallaJo && window.YallaJo.formUx) {
                window.YallaJo.formUx.startLoading(form);
            }

            if (typeof grecaptcha === "undefined" || !grecaptcha.ready) {
                // reCAPTCHA script unavailable -> submit without blocking the user.
                input.value = "";
                input.dataset.submitted = "1";
                form.submit();
                return;
            }

            grecaptcha.ready(function () {
                grecaptcha
                    .execute(siteKey, { action: action })
                    .then(function (token) {
                        input.value = token;
                        input.dataset.submitted = "1";
                        form.submit();
                    })
                    .catch(function () {
                        input.value = "";
                        input.dataset.submitted = "1";
                        form.submit();
                    });
            });
        });
    }

    function init() {
        var inputs = document.querySelectorAll("input[data-recaptcha-sitekey]");
        for (var i = 0; i < inputs.length; i++) {
            wire(inputs[i]);
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
