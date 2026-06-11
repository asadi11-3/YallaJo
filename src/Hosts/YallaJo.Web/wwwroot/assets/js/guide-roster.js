// Guide → Agency Roster: shared reason modal for reject/remove actions (MOD8).
//
// CSP-safe external file (A6/SEC1/J3): no inline scripts, no inline on*
// attributes, a single capture-phase delegated submit listener that survives
// DOM swaps. No hardcoded user-facing English (CON1) — the modal title and
// confirm-button text come from the triggering form's data-reason-title /
// data-reason-action attributes (rendered from the Localizer), and the modal
// body (label/help/cancel) is server-rendered.
//
// Contract:
//   - Forms opt in with `data-yj-reason-form` and carry a hidden
//     input[name="reason"]. On first submit we intercept, show
//     #roster-reason-modal, and on accept write the reason into the hidden
//     input and re-submit with dataset.yjReasonOk="1" so the submit proceeds —
//     provider-actions.js then handles the AJAX post (the forms also carry
//     data-yj-ajax); without the JS api client they post natively (PRG).
//   - No-JS path (PE2): the reason input stays hidden+empty, the server-side
//     RequireReason guard rejects with a localized flash message. Same
//     degradation applies if Bootstrap is missing: we let the native submit
//     through with an empty reason and the server flashes the error
//     (window.prompt fallback is deliberately NOT used — hardcoded English).
(function () {
    "use strict";

    if (window.__yjGuideRosterInit) { return; } // guard double-init across re-injection
    window.__yjGuideRosterInit = true;

    var pendingForm = null;

    function modalEl() {
        return document.getElementById("roster-reason-modal");
    }

    function bootstrapModal(el) {
        if (!window.bootstrap || !window.bootstrap.Modal) { return null; }
        return window.bootstrap.Modal.getOrCreateInstance(el);
    }

    // Capture phase so we run before provider-actions.js's bubble-phase
    // data-yj-ajax handler — the reason must be collected first.
    document.addEventListener("submit", function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.hasAttribute("data-yj-reason-form")) {
            return;
        }

        if (form.dataset.yjReasonOk === "1") {
            // Reason already collected — clear the flag and let the submit
            // proceed (provider-actions.js takes over for data-yj-ajax forms).
            delete form.dataset.yjReasonOk;
            return;
        }

        var el = modalEl();
        var modal = el ? bootstrapModal(el) : null;
        if (!modal) {
            // Bootstrap (or the modal markup) missing — degrade to a native
            // submit with an empty reason; the server-side RequireReason guard
            // flashes the localized error (PE2). Never window.prompt.
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        pendingForm = form;

        var title = el.querySelector("#roster-reason-title");
        if (title) { title.textContent = form.dataset.reasonTitle || ""; }
        var accept = el.querySelector("#roster-reason-accept");
        if (accept) { accept.textContent = form.dataset.reasonAction || ""; }

        var textarea = el.querySelector("#roster-reason-text");
        if (textarea) {
            textarea.value = "";
            textarea.classList.remove("is-invalid");
        }

        modal.show();
    }, true);

    document.addEventListener("click", function (event) {
        var accept = event.target instanceof Element
            ? event.target.closest("#roster-reason-accept")
            : null;
        if (!accept) { return; }

        var el = modalEl();
        var textarea = el ? el.querySelector("#roster-reason-text") : null;
        if (!(textarea instanceof HTMLTextAreaElement)) { return; }

        var reason = textarea.value.trim();
        if (reason === "") {
            textarea.classList.add("is-invalid");
            textarea.focus();
            return;
        }

        var form = pendingForm;
        pendingForm = null;
        if (!(form instanceof HTMLFormElement) || !form.isConnected) { return; }

        var reasonInput = form.querySelector('input[name="reason"]');
        if (reasonInput instanceof HTMLInputElement) {
            reasonInput.value = reason;
        }

        var modal = el ? bootstrapModal(el) : null;
        if (modal) { modal.hide(); }

        form.dataset.yjReasonOk = "1";
        form.requestSubmit();
    });

    document.addEventListener("shown.bs.modal", function (event) {
        if (!(event.target instanceof Element) || event.target.id !== "roster-reason-modal") { return; }
        var textarea = event.target.querySelector("#roster-reason-text");
        if (textarea) { textarea.focus(); }
    });
})();
