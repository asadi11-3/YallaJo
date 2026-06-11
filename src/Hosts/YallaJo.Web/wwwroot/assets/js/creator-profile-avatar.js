(function () {
    "use strict";

    if (window.yjProfileAvatarInit) {
        return;
    }
    window.yjProfileAvatarInit = true;

    function ready(fn) {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", fn, { once: true });
        } else {
            fn();
        }
    }

    ready(function () {
        var root = document.querySelector('[data-yj-component="avatar-upload"]');
        if (!root) {
            return;
        }

        var input = root.querySelector("#avatarFile");
        var preview = root.querySelector("#avatarPreview");
        if (!input || !preview) {
            return;
        }

        var MAX_BYTES = 10 * 1024 * 1024;
        var ALLOWED = ["image/jpeg", "image/png", "image/gif", "image/webp"];
        var msgType = root.getAttribute("data-msg-type") || "Invalid image type.";
        var msgSize = root.getAttribute("data-msg-size") || "The image is too large.";
        var previousUrl = null;

        function toast(message) {
            if (window.YallaJo && typeof window.YallaJo.toast === "function") {
                window.YallaJo.toast(message, "error");
            }
        }

        input.addEventListener("change", function () {
            var file = input.files && input.files[0];
            if (!file) {
                return;
            }

            if (ALLOWED.indexOf(file.type) === -1) {
                toast(msgType);
                input.value = "";
                return;
            }

            if (file.size > MAX_BYTES) {
                toast(msgSize);
                input.value = "";
                return;
            }

            if (previousUrl) {
                URL.revokeObjectURL(previousUrl);
            }
            previousUrl = URL.createObjectURL(file);
            preview.src = previousUrl;
        });
    });
})();
