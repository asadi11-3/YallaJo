// Tooltip init (Creator is not a FavoriteEntityType; follow CTA is a sign-in link for guests)
(function () {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) { new bootstrap.Tooltip(el); });
})();

// Phase 5.4: optimistic follow/unfollow (UI-UX-NF6). Without the shared API client
// the visible form submits natively and the controller falls back to PRG (PE1).
(function () {
    "use strict";

    var root = document.getElementById("creatorFollow");
    if (!root || !window.YallaJo || !window.YallaJo.api || root.dataset.yjInit === "1") { return; }
    root.dataset.yjInit = "1";

    function toggleForms(isFollowing) {
        var forms = root.querySelectorAll(".js-follow-form");
        var i;
        var matches;
        for (i = 0; i < forms.length; i += 1) {
            matches = forms[i].dataset.following === String(isFollowing);
            forms[i].classList.toggle("d-none", !matches);
        }
    }

    root.addEventListener("submit", function (event) {
        var form = event.target.closest ? event.target.closest(".js-follow-form") : null;
        if (!form) { return; }
        event.preventDefault();
        if (root.dataset.yjBusy === "1") { return; }
        root.dataset.yjBusy = "1";

        // Optimistic flip; revert + toast on failure (NF6).
        var wasFollowing = form.dataset.following === "true";
        toggleForms(!wasFollowing);

        window.YallaJo.api.postForm(form.action, new FormData(form))
            .then(function (response) {
                if (!response.ok) {
                    return response.json().catch(function () { return {}; }).then(function (body) {
                        throw new Error(body && body.error ? body.error : "");
                    });
                }
                return response.json().catch(function () { return null; });
            })
            .then(function (body) {
                if (body && typeof body.isFollowing === "boolean") { toggleForms(body.isFollowing); }
                if (body && body.message && window.YallaJo.toast) { window.YallaJo.toast(body.message, "success"); }
            })
            .catch(function (err) {
                toggleForms(wasFollowing);
                if (window.YallaJo.toast) { window.YallaJo.toast((err && err.message) || "", "error"); }
            })
            .then(function () { delete root.dataset.yjBusy; });
    });
})();
