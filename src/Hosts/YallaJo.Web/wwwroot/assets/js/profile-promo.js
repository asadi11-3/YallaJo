// profile-promo.js
// Inline promotional-block editor for the My Profile page (Accounts area).
//
// A single editor panel (#promo-editor, rendered by _PromoEditor.cshtml) is opened
// from any promo card's pencil control [data-promo-edit-trigger]. The editor and all
// pencils are only present in the DOM for Admin/SuperAdmin/Owner (role-gated in the
// Razor partials), so this script no-ops when #promo-editor is absent.
//
// Flow:
//   * Click pencil -> prefill fields + preview from the card's data-promo-* attributes.
//   * Choose image -> validate (type + 5MB) -> live preview (not committed).
//   * Cancel/Escape/outside-click -> discard pending changes.
//   * Save -> (if new image) POST image first, then POST content fields; swap the
//             card in place from the returned promo JSON. Nothing is committed until Save.
(function () {
    "use strict";

    var MAX_BYTES = 5 * 1024 * 1024; // keep in sync with the backend 5 MB limit
    var ALLOWED_TYPES = ["image/png", "image/jpeg", "image/webp"];
    var FIELDS = ["title", "description", "buttonText", "buttonUrl", "badgeText", "iconName"];

    function init() {
        var editor = document.getElementById("promo-editor");
        if (!editor) { return; }

        var panel = editor.querySelector(".promo-editor__panel");
        var input = editor.querySelector("[data-promo-editor-input]");
        var preview = editor.querySelector("[data-promo-editor-preview]");
        var errorEl = editor.querySelector("[data-promo-editor-error]");
        var spinner = editor.querySelector("[data-promo-editor-spinner]");
        var saveBtn = editor.querySelector("[data-promo-editor-save]");
        var tokenEl = editor.querySelector('input[name="__RequestVerificationToken"]');
        var activeEl = editor.querySelector('[data-promo-editor-field="isActive"]');

        if (!panel || !input || !preview || !saveBtn || !tokenEl) { return; }

        if (editor.parentNode !== document.body) {
            document.body.appendChild(editor);
        }

        var fieldEls = {};
        FIELDS.forEach(function (f) {
            fieldEls[f] = editor.querySelector('[data-promo-editor-field="' + f + '"]');
        });

        var currentKey = null;
        var currentTrigger = null;
        var originalImage = "";
        var pendingFile = null;

        // ── Panel open / close ───────────────────────────────────────────────
        function openPanel(trigger) {
            currentTrigger = trigger;
            position(trigger);
            editor.hidden = false;
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
            var rect = trigger.getBoundingClientRect();
            var panelWidth = panel.offsetWidth || 320;
            var panelHeight = panel.offsetHeight || 0;
            var margin = 8;
            var top = window.scrollY + rect.bottom + margin;
            // Flip above the trigger if it would overflow the viewport bottom.
            if (panelHeight && rect.bottom + margin + panelHeight > document.documentElement.clientHeight) {
                var above = window.scrollY + rect.top - margin - panelHeight;
                if (above > window.scrollY) { top = above; }
            }
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
            if (e.target.closest("[data-promo-edit-trigger]")) { return; }
            closePanel();
        }

        function onKeydown(e) {
            if (e.key === "Escape") { closePanel(); }
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        function showError(message) {
            if (!errorEl) { return; }
            errorEl.textContent = message || "";
            errorEl.hidden = !message;
        }

        function setBusy(busy) {
            if (spinner) { spinner.classList.toggle("d-none", !busy); }
            if (saveBtn) { saveBtn.disabled = busy; }
        }

        function getVal(name) {
            var el = fieldEls[name];
            return el ? (el.value || "") : "";
        }

        function prefill(trigger) {
            currentKey = trigger.getAttribute("data-promo-key") || "";
            originalImage = trigger.getAttribute("data-promo-image") || "";
            pendingFile = null;
            input.value = "";
            showError("");

            if (fieldEls.title) { fieldEls.title.value = trigger.getAttribute("data-promo-title") || ""; }
            if (fieldEls.description) { fieldEls.description.value = trigger.getAttribute("data-promo-description") || ""; }
            if (fieldEls.buttonText) { fieldEls.buttonText.value = trigger.getAttribute("data-promo-buttontext") || ""; }
            if (fieldEls.buttonUrl) { fieldEls.buttonUrl.value = trigger.getAttribute("data-promo-buttonurl") || ""; }
            if (fieldEls.badgeText) { fieldEls.badgeText.value = trigger.getAttribute("data-promo-badge") || ""; }
            if (fieldEls.iconName) { fieldEls.iconName.value = trigger.getAttribute("data-promo-icon") || ""; }
            if (activeEl) { activeEl.checked = trigger.getAttribute("data-promo-active") === "true"; }

            preview.setAttribute("src", originalImage || "");
            preview.classList.toggle("d-none", !originalImage);
        }

        // ── Choose image ─────────────────────────────────────────────────────
        editor.querySelectorAll("[data-promo-editor-choose]").forEach(function (btn) {
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
            pendingFile = file;
            var reader = new FileReader();
            reader.onload = function (ev) {
                preview.setAttribute("src", ev.target.result);
                preview.classList.remove("d-none");
            };
            reader.readAsDataURL(file);
        });

        // ── Cancel ───────────────────────────────────────────────────────────
        editor.querySelectorAll("[data-promo-editor-cancel]").forEach(function (btn) {
            btn.addEventListener("click", function () { closePanel(); });
        });

        // ── Save ─────────────────────────────────────────────────────────────
        saveBtn.addEventListener("click", function () { save(); });

        function save() {
            if (!currentKey) { return; }
            var token = tokenEl.value;
            var base = "/accounts/promo/" + encodeURIComponent(currentKey);
            setBusy(true);
            showError("");

            var chain = Promise.resolve();
            if (pendingFile) {
                var imgData = new FormData();
                imgData.append("file", pendingFile);
                imgData.append("__RequestVerificationToken", token);
                chain = postForm(base + "/image", imgData);
            }

            chain.then(function () {
                var form = new FormData();
                form.append("Title", getVal("title"));
                form.append("Description", getVal("description"));
                form.append("ButtonText", getVal("buttonText"));
                form.append("ButtonUrl", getVal("buttonUrl"));
                form.append("BadgeText", getVal("badgeText"));
                form.append("IconName", getVal("iconName"));
                form.append("IsActive", activeEl && activeEl.checked ? "true" : "false");
                form.append("SortOrder", "0");
                form.append("__RequestVerificationToken", token);
                return postForm(base + "/update", form);
            }).then(function (data) {
                applyPromo(data.promo);
                setBusy(false);
                closePanel();
                toast(editor.dataset.msgSaved || "Saved.", "success");
            }).catch(function (err) {
                setBusy(false);
                handleError(err);
            });
        }

        function postForm(url, body) {
            return fetch(url, {
                method: "POST",
                credentials: "same-origin",
                headers: { "X-Requested-With": "fetch", "Accept": "application/json" },
                body: body
            }).then(function (res) {
                return res.json().catch(function () { return {}; }).then(function (json) {
                    if (res.ok && json && json.success) { return json; }
                    var err = new Error((json && json.error) || "Request failed.");
                    err.status = res.status;
                    err.payload = json || {};
                    throw err;
                });
            });
        }

        function handleError(err) {
            var payload = err.payload || {};
            if (payload.signOut) {
                window.location.reload();
                return;
            }
            if (payload.errors) {
                var first = null;
                Object.keys(payload.errors).some(function (k) {
                    var arr = payload.errors[k];
                    if (arr && arr.length) { first = arr[0]; return true; }
                    return false;
                });
                showError(first || payload.error || "Validation failed.");
                return;
            }
            showError(payload.error || err.message || "Could not save.");
        }

        // ── In-place card swap ───────────────────────────────────────────────
        function applyPromo(promo) {
            if (!promo || !promo.PlacementKey) { return; }
            // Simplest robust refresh: re-render the affected card by reloading the
            // page section is avoided; instead update the card's known nodes + the
            // pencil's data-* attributes so subsequent edits prefill correctly.
            var card = document.querySelector('[data-promo-card][data-promo-key="' + cssEscape(promo.PlacementKey) + '"]');
            var trigger = document.querySelector('[data-promo-edit-trigger][data-promo-key="' + cssEscape(promo.PlacementKey) + '"]');

            if (trigger) {
                trigger.setAttribute("data-promo-title", promo.Title || "");
                trigger.setAttribute("data-promo-description", promo.Description || "");
                trigger.setAttribute("data-promo-buttontext", promo.ButtonText || "");
                trigger.setAttribute("data-promo-buttonurl", promo.ButtonUrl || "");
                trigger.setAttribute("data-promo-badge", promo.BadgeText || "");
                trigger.setAttribute("data-promo-icon", promo.IconName || "");
                trigger.setAttribute("data-promo-active", promo.IsActive ? "true" : "false");
                trigger.setAttribute("data-promo-image", promo.ImageUrl || "");
            }

            if (card) {
                var titleEl = card.querySelector("[data-promo-card-title]");
                if (titleEl) { titleEl.textContent = promo.Title || ""; }
                var descEl = card.querySelector("[data-promo-card-desc]");
                if (descEl) { descEl.textContent = promo.Description || ""; }
                var imgEl = card.querySelector("[data-promo-card-image]");
                if (imgEl && promo.ImageUrl) { imgEl.setAttribute("src", promo.ImageUrl); }
                var ctaEl = card.querySelector("[data-promo-card-cta]");
                if (ctaEl) {
                    if (promo.ButtonText) { ctaEl.textContent = promo.ButtonText; }
                    if (promo.ButtonUrl) { ctaEl.setAttribute("href", promo.ButtonUrl); }
                }
                var badgeEl = card.querySelector("[data-promo-card-badge]");
                if (badgeEl && promo.BadgeText) { badgeEl.textContent = promo.BadgeText; }
                card.classList.toggle("promo-card--inactive", !promo.IsActive);
            } else {
                // Card structure changed (e.g. an empty placeholder became active, or
                // visibility/content materially changed). A reload yields the correct
                // server-rendered markup for the new state.
                window.location.reload();
            }
        }

        function cssEscape(value) {
            if (window.CSS && typeof window.CSS.escape === "function") {
                return window.CSS.escape(value);
            }
            return String(value).replace(/["\\]/g, "\\$&");
        }

        function toast(message, type) {
            if (window.YallaJo && typeof window.YallaJo.toast === "function") {
                window.YallaJo.toast(message, type || "success");
            }
        }

        // ── Triggers (one editor shared by every promo pencil) ───────────────
        document.querySelectorAll("[data-promo-edit-trigger]").forEach(function (trigger) {
            trigger.addEventListener("click", function (e) {
                e.preventDefault();
                if (!editor.hidden && currentTrigger === trigger) {
                    closePanel();
                    return;
                }
                prefill(trigger);
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
