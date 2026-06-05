// Wishlist favorite toggle.
// Delegated handler for any element with class "js-favorite" that carries
// data-entity-type and data-entity-id attributes (e.g. the heart button on
// tour/place/business cards). POSTs to the Accounts wishlist toggle endpoint
// and flips the bi-heart / bi-heart-fill icon to reflect the new state.
(function () {
    "use strict";

    function antiForgeryToken() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : "";
    }

    function setState(button, isFavorited) {
        var icon = button.querySelector("i.bi");
        if (icon) {
            icon.classList.toggle("bi-heart", !isFavorited);
            icon.classList.toggle("bi-heart-fill", isFavorited);
            icon.classList.toggle("text-danger", isFavorited);
        }
        button.classList.toggle("active", isFavorited);
        button.setAttribute("aria-pressed", isFavorited ? "true" : "false");
        button.title = isFavorited ? "Remove from wishlist" : "Save to wishlist";
    }

    function buildUrl(entityType, entityId) {
        return "/accounts/wishlist/toggle/" +
            encodeURIComponent(entityType) + "/" + encodeURIComponent(entityId);
    }

    async function toggle(button) {
        var entityType = button.getAttribute("data-entity-type");
        var entityId = button.getAttribute("data-entity-id");
        if (!entityType || !entityId || button.dataset.busy === "1") {
            return;
        }

        button.dataset.busy = "1";
        button.disabled = true;

        try {
            var response = await fetch(buildUrl(entityType, entityId), {
                method: "POST",
                headers: {
                    "RequestVerificationToken": antiForgeryToken(),
                    "Accept": "application/json"
                },
                credentials: "same-origin"
            });

            if (response.status === 401) {
                window.location.href = "/Auth/Auth/SignIn";
                return;
            }

            if (!response.ok) {
                return;
            }

            var data = await response.json();
            setState(button, data.isFavorited === true);
        } catch (e) {
            // network error — leave the button untouched
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
