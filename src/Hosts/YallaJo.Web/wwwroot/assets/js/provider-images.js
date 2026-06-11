/*
 * Tour image upload enhancements (Phase 5).
 * Client-side preview + size/type pre-check before the form posts.
 * The server's magic-byte validation (SEC4) remains authoritative —
 * this only gives faster feedback. Without JS the form posts normally (PE1).
 */
(function () {
    "use strict";

    var MAX_BYTES = 5 * 1024 * 1024;
    var ALLOWED = ["image/jpeg", "image/png", "image/webp"];

    var form = document.querySelector("form[data-yj-image-upload]");
    if (!form || form.dataset.yjInit === "1") { return; }
    form.dataset.yjInit = "1";

    var input = form.querySelector("input[type=file]");
    var wrap = form.querySelector("[data-yj-preview-wrap]");
    var img = form.querySelector("[data-yj-preview]");
    if (!input || !wrap || !img) { return; }

    var objectUrl = null;

    function toast(msg) {
        if (window.YallaJo && window.YallaJo.toast) { window.YallaJo.toast(msg, "error"); }
        else { window.alert(msg); }
    }

    function clearPreview() {
        wrap.classList.add("d-none");
        if (objectUrl) { URL.revokeObjectURL(objectUrl); objectUrl = null; }
        img.removeAttribute("src");
    }

    input.addEventListener("change", function () {
        clearPreview();
        var file = input.files && input.files[0];
        if (!file) { return; }

        if (ALLOWED.indexOf(file.type) === -1) {
            toast(form.dataset.yjInvalidType || "Unsupported image type.");
            input.value = "";
            return;
        }
        if (file.size > MAX_BYTES) {
            toast(form.dataset.yjTooLarge || "File is too large.");
            input.value = "";
            return;
        }

        objectUrl = URL.createObjectURL(file);
        img.src = objectUrl;
        img.alt = form.dataset.yjPreviewAlt || "Preview";
        wrap.classList.remove("d-none");
    });

    // Free the blob URL once the form actually submits.
    form.addEventListener("submit", function () {
        if (objectUrl) { URL.revokeObjectURL(objectUrl); objectUrl = null; }
    });
}());
