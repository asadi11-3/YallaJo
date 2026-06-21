// profile-avatar.js
// Shared profile-photo edit flow for the My Profile page (Accounts area).
//
// A single editor panel (#avatar-editor, rendered by _AvatarEditor.cshtml) is opened
// from multiple triggers tagged [data-avatar-edit-trigger] — currently the camera
// control in the Personal Information card and the pencil overlay in the left sidebar.
// Both entry points share this exact logic and markup (no duplicate behaviours).
//
// Flow:
//   * Choose photo  -> validate (type + 5MB) -> live preview everywhere -> Save/Cancel.
//   * Remove photo  -> confirm -> stage default avatar -> Save/Cancel.
//   * Cancel        -> discard the pending change, restore the original image.
//   * Save          -> POST the chosen file (UpdateAvatar) or the removal (DeleteAvatar).
//
// Nothing is committed to the server until the user clicks Save.
(function () {
    "use strict";

    var MAX_BYTES = 5 * 1024 * 1024; // keep in sync with the backend 5 MB limit
    var ALLOWED_TYPES = ["image/png", "image/jpeg"];

    function init() {
        var editor = document.getElementById("avatar-editor");
        if (!editor) { return; }

        var panel = editor.querySelector(".avatar-editor__panel");
        var input = editor.querySelector("[data-avatar-editor-input]");
        var fileProxy = editor.querySelector("[data-avatar-editor-file-proxy]");
        var preview = editor.querySelector("[data-avatar-editor-preview]");
        var errorEl = editor.querySelector("[data-avatar-editor-error]");
        var actionsDefault = editor.querySelector('[data-avatar-editor-actions="default"]');
        var actionsConfirm = editor.querySelector('[data-avatar-editor-actions="confirm"]');
        var actionsPending = editor.querySelector('[data-avatar-editor-actions="pending"]');
        var updateForm = editor.querySelector('[data-avatar-editor-form="update"]');
        var deleteForm = editor.querySelector('[data-avatar-editor-form="delete"]');

        if (!panel || !input || !fileProxy || !updateForm || !deleteForm) { return; }

        // Relocate to <body> so the absolutely-positioned panel coordinates map to the
        // document (no clipping by, and no offset from, the section/card it lives in).
        if (editor.parentNode !== document.body) {
            document.body.appendChild(editor);
        }

        var defaultAvatar = editor.dataset.defaultAvatar || "";
        var liveImgs = function () { return document.querySelectorAll("[data-avatar-img]"); };
        // Remember the original image so Cancel can restore it.
        var originalSrc = preview ? preview.getAttribute("src") : defaultAvatar;
        var pendingMode = null; // null | "upload" | "remove"
        var currentTrigger = null;

        // ── Panel open / close ───────────────────────────────────────────────
        function openPanel(trigger) {
            currentTrigger = trigger;
            position(trigger);
            editor.hidden = false;
            // re-flow then add the visible class for a subtle transition
            window.requestAnimationFrame(function () { editor.classList.add("is-open"); });
            document.addEventListener("mousedown", onOutside, true);
            document.addEventListener("keydown", onKeydown, true);
            window.addEventListener("resize", onReposition);
            window.addEventListener("scroll", onReposition, true);
        }

        function closePanel() {
            editor.classList.remove("is-open");
            editor.hidden = true;
            document.removeEventListener("mousedown", onOutside, true);
            document.removeEventListener("keydown", onKeydown, true);
            window.removeEventListener("resize", onReposition);
            window.removeEventListener("scroll", onReposition, true);
            if (currentTrigger && typeof currentTrigger.focus === "function") {
                currentTrigger.focus();
            }
            currentTrigger = null;
        }

        function position(trigger) {
            // Anchor the panel just below the trigger, clamped to the viewport.
            var rect = trigger.getBoundingClientRect();
            var panelWidth = panel.offsetWidth || 280;
            var margin = 8;
            var top = window.scrollY + rect.bottom + margin;
            var left = window.scrollX + rect.left + (rect.width / 2) - (panelWidth / 2);
            var maxLeft = window.scrollX + document.documentElement.clientWidth - panelWidth - margin;
            if (left < window.scrollX + margin) { left = window.scrollX + margin; }
            if (left > maxLeft) { left = Math.max(window.scrollX + margin, maxLeft); }
            panel.style.top = top + "px";
            panel.style.left = left + "px";
        }

        function onReposition() {
            if (!editor.hidden && currentTrigger) { position(currentTrigger); }
        }

        function onOutside(e) {
            if (editor.hidden) { return; }
            if (panel.contains(e.target)) { return; }
            if (e.target.closest("[data-avatar-edit-trigger]")) { return; }
            cancelPending();
            closePanel();
        }

        function onKeydown(e) {
            if (e.key === "Escape") {
                cancelPending();
                closePanel();
            }
        }

        // ── Preview helpers ──────────────────────────────────────────────────
        function setPreview(src) {
            if (preview) { preview.setAttribute("src", src); }
            liveImgs().forEach(function (img) { img.setAttribute("src", src); });
        }

        function showError(message) {
            if (!errorEl) { return; }
            errorEl.textContent = message;
            errorEl.hidden = !message;
        }

        function showActions(which) {
            if (actionsDefault) { actionsDefault.hidden = which !== "default"; }
            if (actionsConfirm) { actionsConfirm.hidden = which !== "confirm"; }
            if (actionsPending) { actionsPending.hidden = which !== "pending"; }
        }

        function toPending() { showActions("pending"); }
        function toDefault() { showActions("default"); }
        function toConfirm() { showActions("confirm"); }

        function cancelPending() {
            pendingMode = null;
            showError("");
            input.value = "";
            setPreview(originalSrc);
            toDefault();
        }

        // ── Choose photo ─────────────────────────────────────────────────────
        editor.querySelectorAll("[data-avatar-editor-choose]").forEach(function (btn) {
            btn.addEventListener("click", function () { input.click(); });
        });

        input.addEventListener("change", function () {
            var file = input.files && input.files[0];
            if (!file) { return; }

            if (ALLOWED_TYPES.indexOf(file.type) === -1) {
                showError(editor.dataset.msgInvalidType || "Invalid file type.");
                input.value = "";
                return;
            }
            if (file.size > MAX_BYTES) {
                showError(editor.dataset.msgTooLarge || "File too large.");
                input.value = "";
                return;
            }

            showError("");
            var reader = new FileReader();
            reader.onload = function (ev) {
                setPreview(ev.target.result);
                pendingMode = "upload";
                toPending();
            };
            reader.readAsDataURL(file);
        });

        // ── Remove photo (inline confirmation step) ──────────────────────────
        editor.querySelectorAll("[data-avatar-editor-remove]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                showError("");
                toConfirm();
            });
        });

        // Back out of the confirmation prompt — keep the existing photo.
        editor.querySelectorAll("[data-avatar-editor-confirm-cancel]").forEach(function (btn) {
            btn.addEventListener("click", function () { toDefault(); });
        });

        // Confirmed: stage the removal (default avatar), reveal Save/Cancel.
        editor.querySelectorAll("[data-avatar-editor-confirm-remove]").forEach(function (btn) {
            btn.addEventListener("click", function () { stageRemoval(); });
        });

        function stageRemoval() {
            pendingMode = "remove";
            input.value = "";
            showError("");
            setPreview(defaultAvatar);
            toPending();
        }

        // ── Cancel ───────────────────────────────────────────────────────────
        editor.querySelectorAll("[data-avatar-editor-cancel]").forEach(function (btn) {
            btn.addEventListener("click", function () { cancelPending(); });
        });

        // ── Save ─────────────────────────────────────────────────────────────
        editor.querySelectorAll("[data-avatar-editor-save]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                if (pendingMode === "upload") {
                    var file = input.files && input.files[0];
                    if (!file) { cancelPending(); return; }
                    // Transfer the chosen file into the multipart form's input.
                    try {
                        var dt = new DataTransfer();
                        dt.items.add(file);
                        fileProxy.files = dt.files;
                    } catch (err) {
                        // Fallback: submit the live input directly by relocating it.
                        fileProxy.parentNode.replaceChild(input, fileProxy);
                        input.name = "File";
                    }
                    updateForm.submit();
                } else if (pendingMode === "remove") {
                    deleteForm.submit();
                }
            });
        });

        // ── Triggers (camera control + sidebar pencil) ───────────────────────
        document.querySelectorAll("[data-avatar-edit-trigger]").forEach(function (trigger) {
            trigger.addEventListener("click", function (e) {
                e.preventDefault();
                if (!editor.hidden && currentTrigger === trigger) {
                    cancelPending();
                    closePanel();
                    return;
                }
                cancelPending();
                openPanel(trigger);
            });
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
