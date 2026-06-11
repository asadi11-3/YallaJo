// Site-wide form UX helpers (rules: UI-UX-L2/F7 button loading states, NF1/NF4/NF5 toasts,
// F8/MOD3/MOD5 confirm modal). Exposed as window.YallaJo.toast / window.YallaJo.confirm (JS1).
//
// Usage:
//   <form data-loading>            -> submit button disabled + spinner replaces label on submit.
//   <form data-confirm="message"   -> intercepts submit, shows confirm modal first.
//         data-confirm-title="..." data-confirm-action="..." data-confirm-cancel="...">
//   YallaJo.toast(message, "success" | "error" | "info")
(function () {
    "use strict";

    window.YallaJo = window.YallaJo || {};

    /* ------------------------------------------------------------------ *
     * Toasts (NF1: bottom-right, max 3, auto-dismiss 5 s; NF4: dedupe 2 s;
     * NF5: role=status + aria-live=polite container).
     * ------------------------------------------------------------------ */

    var TOAST_LIMIT = 3;
    var TOAST_DISMISS_MS = 5000;
    var TOAST_DEDUPE_MS = 2000;
    var lastToastMessage = "";
    var lastToastAt = 0;

    function toastContainer() {
        var container = document.getElementById("yj-toasts");
        if (!container) {
            container = document.createElement("div");
            container.id = "yj-toasts";
            container.className = "toast-container position-fixed bottom-0 end-0 p-3";
            container.setAttribute("role", "status");
            container.setAttribute("aria-live", "polite");
            document.body.appendChild(container);
        }
        return container;
    }

    function toast(message, type) {
        if (!message) { return; }
        var now = Date.now();
        if (message === lastToastMessage && now - lastToastAt < TOAST_DEDUPE_MS) { return; }
        lastToastMessage = message;
        lastToastAt = now;

        var container = toastContainer();
        while (container.children.length >= TOAST_LIMIT) {
            container.removeChild(container.firstElementChild);
        }

        var border = type === "error" ? "border-danger" : type === "success" ? "border-success" : "";
        var icon = type === "error" ? "bi-exclamation-circle text-danger"
            : type === "success" ? "bi-check-circle text-success"
            : "bi-info-circle text-primary";

        var el = document.createElement("div");
        el.className = "toast align-items-center show " + border;
        el.innerHTML =
            '<div class="d-flex">' +
            '<div class="toast-body d-flex align-items-center gap-2">' +
            '<i class="bi ' + icon + '" aria-hidden="true"></i><span></span></div>' +
            '<button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>' +
            "</div>";
        el.querySelector(".toast-body span").textContent = message;
        container.appendChild(el);

        if (window.bootstrap && window.bootstrap.Toast) {
            new window.bootstrap.Toast(el, { delay: TOAST_DISMISS_MS, autohide: true }).show();
            el.addEventListener("hidden.bs.toast", function () { el.remove(); });
        } else {
            setTimeout(function () { el.remove(); }, TOAST_DISMISS_MS);
        }
    }

    /* ------------------------------------------------------------------ *
     * Submit loading state (L2: spinner replaces label; F7: disabled while
     * pending). Re-enables on bfcache restore so Back works.
     * ------------------------------------------------------------------ */

    function startLoading(form) {
        if (form.dataset.yjLoading === "1") { return false; }
        form.dataset.yjLoading = "1";
        var buttons = form.querySelectorAll('button[type="submit"], input[type="submit"]');
        buttons.forEach(function (button) {
            button.disabled = true;
            button.setAttribute("aria-busy", "true");
            if (button.tagName === "BUTTON") {
                button.dataset.yjLabel = button.innerHTML;
                button.setAttribute("aria-label", button.textContent.trim());
                button.innerHTML =
                    '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>';
            }
        });
        return true;
    }

    function resetLoading(form) {
        form.dataset.yjLoading = "0";
        var buttons = form.querySelectorAll('button[type="submit"], input[type="submit"]');
        buttons.forEach(function (button) {
            button.disabled = false;
            button.removeAttribute("aria-busy");
            if (button.dataset.yjLabel) {
                button.innerHTML = button.dataset.yjLabel;
                delete button.dataset.yjLabel;
            }
        });
    }

    window.addEventListener("pageshow", function (e) {
        if (!e.persisted) { return; }
        document.querySelectorAll('form[data-loading][data-yj-loading="1"]').forEach(resetLoading);
    });

    /* ------------------------------------------------------------------ *
     * Confirm modal (F8: title is a question, consequences in body, safe
     * action is primary + autofocused; MOD3: static backdrop, Esc allowed;
     * MOD5: autofocus the SAFE action, never the destructive one).
     * ------------------------------------------------------------------ */

    var confirmModalEl = null;
    var pendingForm = null;

    function buildConfirmModal() {
        if (confirmModalEl) { return confirmModalEl; }
        confirmModalEl = document.createElement("div");
        confirmModalEl.className = "modal fade";
        confirmModalEl.id = "yj-confirm-modal";
        confirmModalEl.tabIndex = -1;
        confirmModalEl.setAttribute("aria-hidden", "true");
        confirmModalEl.setAttribute("aria-labelledby", "yj-confirm-title");
        confirmModalEl.setAttribute("aria-describedby", "yj-confirm-body");
        confirmModalEl.setAttribute("data-bs-backdrop", "static");
        confirmModalEl.innerHTML =
            '<div class="modal-dialog modal-dialog-centered modal-sm-down">' +
            '<div class="modal-content" role="alertdialog" aria-modal="true">' +
            '<div class="modal-header"><h5 class="modal-title" id="yj-confirm-title"></h5>' +
            '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button></div>' +
            '<div class="modal-body" id="yj-confirm-body"></div>' +
            '<div class="modal-footer">' +
            '<button type="button" class="btn btn-primary" data-bs-dismiss="modal" id="yj-confirm-cancel"></button>' +
            '<button type="button" class="btn btn-outline-danger" id="yj-confirm-accept"></button>' +
            "</div></div></div>";
        document.body.appendChild(confirmModalEl);

        confirmModalEl.addEventListener("shown.bs.modal", function () {
            confirmModalEl.querySelector("#yj-confirm-cancel").focus();
        });
        confirmModalEl.querySelector("#yj-confirm-accept").addEventListener("click", function () {
            var form = pendingForm;
            pendingForm = null;
            if (window.bootstrap && window.bootstrap.Modal) {
                window.bootstrap.Modal.getInstance(confirmModalEl).hide();
            }
            if (form) {
                form.dataset.yjConfirmed = "1";
                if (form.requestSubmit) { form.requestSubmit(); } else { form.submit(); }
            }
        });
        return confirmModalEl;
    }

    function showConfirm(form) {
        if (!(window.bootstrap && window.bootstrap.Modal)) {
            // No Bootstrap available — degrade to native confirm.
            return window.confirm(form.dataset.confirm || "");
        }
        var modal = buildConfirmModal();
        modal.querySelector("#yj-confirm-title").textContent =
            form.dataset.confirmTitle || "Are you sure?";
        modal.querySelector("#yj-confirm-body").textContent = form.dataset.confirm || "";
        modal.querySelector("#yj-confirm-cancel").textContent =
            form.dataset.confirmCancel || "Cancel";
        modal.querySelector("#yj-confirm-accept").textContent =
            form.dataset.confirmAction || "Confirm";
        pendingForm = form;
        window.bootstrap.Modal.getOrCreateInstance(modal).show();
        return false;
    }

    /* ------------------------------------------------------------------ *
     * Delegated submit handling (capture so confirm runs before anything
     * else; loading state applied only when the submit goes through).
     * ------------------------------------------------------------------ */

    document.addEventListener("submit", function (e) {
        var form = e.target;
        var proceed;
        if (!(form instanceof HTMLFormElement)) { return; }

        if (form.hasAttribute("data-confirm") && form.dataset.yjConfirmed !== "1") {
            proceed = showConfirm(form);
            if (proceed === false) {
                e.preventDefault();
                return;
            }
        }
        if (form.dataset.yjConfirmed === "1") {
            delete form.dataset.yjConfirmed;
        }

        if (form.hasAttribute("data-loading")) {
            if (form.dataset.yjLoading === "1") {
                e.preventDefault(); // double-submit guard
                return;
            }
            // Defer past the bubble phase so client-side validation (jQuery
            // unobtrusive) or the reCAPTCHA interceptor can cancel the submit
            // first -- otherwise the spinner strands on a form that never
            // leaves the page (L2/F7). reCAPTCHA forms re-submit via
            // form.submit() (no submit event), so recaptcha-field.js engages
            // the loading state itself for that path.
            setTimeout(function () {
                if (!e.defaultPrevented) { startLoading(form); }
            }, 0);
        }
    }, true);

    window.YallaJo.toast = toast;
    window.YallaJo.formUx = { startLoading: startLoading, resetLoading: resetLoading };
})();
