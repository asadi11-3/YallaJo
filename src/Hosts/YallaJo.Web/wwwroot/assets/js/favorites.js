// Wishlist favorite toggle.
// Delegated handler for any element with class "js-favorite" that carries
// data-entity-type and data-entity-id attributes (e.g. the heart button on
// tour/place/business cards). POSTs to the Accounts wishlist toggle endpoint
// via the shared api client (JS5) and flips the bi-heart / bi-heart-fill icon.
// Guests get redirected to sign-in with a returnUrl by the api client (WL1).
(function () {
    "use strict";

    function setState(button, isFavorited) {
        var icon = button.querySelector("i.bi");
        if (icon) {
            icon.classList.toggle("bi-heart", !isFavorited);
            icon.classList.toggle("bi-heart-fill", isFavorited);
            icon.classList.toggle("text-danger", isFavorited);
        }
        button.classList.toggle("active", isFavorited);
        button.setAttribute("aria-pressed", isFavorited ? "true" : "false");
        // Localized titles come from data attributes when the view provides them.
        var title = isFavorited
            ? (button.dataset.titleRemove || "Remove from wishlist")
            : (button.dataset.titleAdd || "Save to wishlist");
        button.title = title;
        button.setAttribute("aria-label", title);
    }

    function buildUrl(entityType, entityId) {
        return "/accounts/wishlist/toggle/" +
            encodeURIComponent(entityType) + "/" + encodeURIComponent(entityId);
    }

    async function toggle(button) {
        var entityType = button.getAttribute("data-entity-type");
        var entityId = button.getAttribute("data-entity-id");
        var data;
        if (!entityType || !entityId || button.dataset.busy === "1") {
            return;
        }

        button.dataset.busy = "1";
        button.disabled = true;

        try {
            data = await window.YallaJo.api.post(buildUrl(entityType, entityId));
            setState(button, !!(data && data.isFavorited === true));
        } catch (e) {
            // 401 is handled by the api client (sign-in redirect); other
            // failures leave the button untouched.
        } finally {
            button.dataset.busy = "0";
            button.disabled = false;
        }
    }

    document.addEventListener("click", function (e) {
        var button = e.target.closest(".js-favorite");
        if (!button) {
            return;
        }
        e.preventDefault();
        toggle(button);
    });
})();
