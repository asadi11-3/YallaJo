/* accounts-recommendations.js - Phase 4c (Accounts plan)
   Optimistic "not interested" dismissal (NF6) + best-effort click tracking (JS5).
   Native form POST (PRG) and link navigation still work without JS (PE1). */
(function () {
    "use strict";

    var yj = window.YallaJo;
    if (!yj || !yj.api) {
        return; // PE1: native form POST / link navigation already works.
    }

    var root = document.getElementById("recommendationsGrid");
    if (!root || root.dataset.yjInit === "1") {
        return;
    }
    root.dataset.yjInit = "1";

    var api = yj.api;
    var failedText = root.getAttribute("data-failed-text") ||
        "Could not update your recommendations. Please try again.";

    function toast(message, type) {
        if (yj.toast) {
            yj.toast(message, type);
        }
    }

    // --- Optimistic "not interested" dismissal (NF6) ---
    root.addEventListener("submit", function (e) {
        var form = e.target;
        var action = "";
        var card = null;
        var placeholder = null;

        if (!form || form.tagName !== "FORM") {
            return;
        }
        action = (form.getAttribute("action") || "").toLowerCase();
        if (action.indexOf("/recommendations/not-interested") === -1) {
            return;
        }

        e.preventDefault();
        card = form.closest(".js-rec-item");
        if (!card || !card.parentNode) {
            form.submit();
            return;
        }

        placeholder = document.createComment("yj-rec-removed");
        card.parentNode.insertBefore(placeholder, card);
        card.parentNode.removeChild(card);

        api.postForm(form.getAttribute("action"), new FormData(form))
            .then(function (res) {
                if (!res || !res.ok) {
                    throw new Error("dismiss-failed");
                }
                if (placeholder.parentNode) {
                    placeholder.parentNode.removeChild(placeholder);
                }
            })
            .catch(function () {
                if (placeholder.parentNode) {
                    placeholder.parentNode.insertBefore(card, placeholder);
                    placeholder.parentNode.removeChild(placeholder);
                }
                toast(failedText, "error");
            });
    });

    // --- Fire-and-forget click tracking (JS5; analytics best-effort, API2) ---
    root.addEventListener("click", function (e) {
        var link = e.target && e.target.closest ? e.target.closest(".js-rec-link") : null;
        var data = null;
        var type = "";
        var id = "";

        if (!link) {
            return;
        }
        type = link.getAttribute("data-track-type") || "";
        id = link.getAttribute("data-track-id") || "";
        if (!type || !id) {
            return;
        }

        data = new FormData();
        data.append("entityType", type);
        data.append("entityId", id);
        data.append("interactionType", "Click");
        // Do not block navigation; tracking is best-effort.
        api.postForm("/accounts/recommendations/track", data).catch(function () { });
    });
}());
