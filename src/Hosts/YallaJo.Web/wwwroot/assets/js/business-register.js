/*
 * business-register.js
 * CSP-safe place -> coordinates prefill for the business registration form.
 * Satisfies: A6 / SEC1 / J3 (no inline scripts, no inline handlers),
 *            JS2 (declarative init via data attribute), JS4 (idempotent init).
 * Reads a non-executable JSON island (<script type="application/json"
 * data-yj-component="business-register">) for the place->coords map and, when
 * the owner has not already entered their own coordinates, fills lat/lng from
 * the chosen place. Pure progressive enhancement: the form works without it.
 */
(function () {
    "use strict";

    function init() {
        var root = document.querySelector('[data-yj-component="business-register"]');
        if (!root || root.dataset.yjInit === "1") {
            return;
        }
        root.dataset.yjInit = "1";

        var coords;
        try {
            coords = JSON.parse(root.textContent || "{}");
        } catch (e) {
            return;
        }

        var select = document.getElementById("placeSelect");
        var lat = document.getElementById("Form_Latitude");
        var lng = document.getElementById("Form_Longitude");
        if (!select || !lat || !lng) {
            return;
        }

        select.addEventListener("change", function () {
            var c = coords[select.value];
            if (!c) {
                return;
            }
            if (!lat.value || lat.value === "0") {
                lat.value = c.Lat;
            }
            if (!lng.value || lng.value === "0") {
                lng.value = c.Lng;
            }
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
