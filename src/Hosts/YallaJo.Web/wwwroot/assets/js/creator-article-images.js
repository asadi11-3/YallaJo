// creator-article-images.js
// AJAX enhancement for the article image gallery (upload / delete / set-primary / reorder).
// Progressive enhancement (PE1): when this script is absent the forms still POST natively
// and the controller falls back to PRG. JS5: all requests go through window.YallaJo.api.
(function () {
    "use strict";

    if (window.yjArticleImagesInit) {
        return;
    }
    window.yjArticleImagesInit = true;

    function ready(fn) {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", fn, { once: true });
        } else {
            fn();
        }
    }

    ready(function () {
        var root = document.querySelector('[data-yj-component="article-images"]');
        if (!root || !window.YallaJo || !window.YallaJo.api) {
            return; // PE1: native POST + PRG still works.
        }

        var MAX_BYTES = 10 * 1024 * 1024; // 10 MB per image (mirrors server-side SEC4 limit).

        function successMessage(action) {
            var a = (action || "").toLowerCase();
            if (a.indexOf("/images/upload") !== -1) return root.dataset.msgUploaded;
            if (a.indexOf("/delete") !== -1) return root.dataset.msgDeleted;
            if (a.indexOf("/primary") !== -1) return root.dataset.msgPrimary;
            if (a.indexOf("/reorder") !== -1) return root.dataset.msgReordered;
            return null;
        }

        function toast(message, type) {
            if (message && window.YallaJo && typeof window.YallaJo.toast === "function") {
                window.YallaJo.toast(message, type);
            }
        }

        // Intercept submits that bubble up to the (stable) gallery root.
        root.addEventListener("submit", function (e) {
            // Let form-ux.js run first: data-confirm (capture phase) cancels and re-submits
            // once confirmed; data-loading adds the spinner. We only act on the final submit.
            if (e.defaultPrevented) {
                return;
            }

            var form = e.target.closest("form");
            if (!form || !root.contains(form)) {
                return;
            }

            e.preventDefault();
            root.setAttribute("aria-busy", "true");

            var action = form.getAttribute("action") || form.action;

            window.YallaJo.api.postForm(action, new FormData(form))
                .then(function (resp) { return resp.text(); })
                .then(function (html) {
                    root.innerHTML = html;
                    root.setAttribute("aria-busy", "false");
                    toast(successMessage(action), "success");
                })
                .catch(function () {
                    root.setAttribute("aria-busy", "false");
                    toast(root.dataset.msgError, "error");
                });
        });

        // File-input feedback: count + total size, with a soft warning for oversize/over-limit.
        root.addEventListener("change", function (e) {
            var input = e.target;
            var info;
            var files;
            var total;
            var oversize;
            var i;
            var mb;
            var uploadForm;
            var remaining;
            var overLimit;

            if (!input || input.id !== "imageFiles") {
                return;
            }

            info = document.getElementById("imageFilesInfo");
            if (!info) {
                return;
            }

            files = input.files;
            if (!files || files.length === 0) {
                info.textContent = "";
                info.classList.remove("text-danger");
                return;
            }

            total = 0;
            oversize = false;
            for (i = 0; i < files.length; i++) {
                total += files[i].size;
                if (files[i].size > MAX_BYTES) {
                    oversize = true;
                }
            }

            mb = (total / (1024 * 1024)).toFixed(1);
            uploadForm = input.closest("form");
            remaining = uploadForm ? parseInt(uploadForm.dataset.remaining || "", 10) : NaN;
            overLimit = !isNaN(remaining) && files.length > remaining;

            info.textContent = files.length + " \u00b7 " + mb + " MB";
            info.classList.toggle("text-danger", oversize || overLimit);
        });
    });
})();
