/*
    Admin modal CRUD (F8, MOD3, MOD5, PE1).

    Progressive enhancement for taxonomy/simple-entity edit modals hosted on Index
    pages. Row triggers are plain anchors to the GET edit deep link (no-JS path);
    with JS, the click is intercepted and the shared modal opens in place:

        <a href="/admin/tags/{id}/edit"
           data-yj-modal-target="#editTagModal"
           data-action="/admin/tags/{id}/edit"
           data-field-name="..." data-field-slug="...">Edit</a>

        <div id="editTagModal" data-yj-component="modal-crud">
            <form ...>
                <input data-yj-field="name" ... />

    Every data-field-* attribute on the trigger is copied to the matching
    [data-yj-field] input inside the modal (camelCase dataset key, minus the
    "field" prefix). Checkboxes map "true"/"false" to checked state.

    Focus: first visible field on open (MOD5); trigger restored on close (MOD3).
    Idempotent (JS2/JS4); vanilla JS, declarative init (JS1).
*/
(function () {
    "use strict";

    var lastTrigger;

    if (window.YallaJo && window.YallaJo.adminModalCrud) { return; }
    window.YallaJo = window.YallaJo || {};
    window.YallaJo.adminModalCrud = true;

    lastTrigger = null;

    function applyFields(modal, trigger) {
        var keys = Object.keys(trigger.dataset);
        var i;
        var key;
        var fieldName;
        var input;
        var value;
        for (i = 0; i < keys.length; i += 1) {
            key = keys[i];
            if (key.indexOf("field") !== 0 || key.length < 6) { continue; }
            fieldName = key.charAt(5).toLowerCase() + key.slice(6);
            input = modal.querySelector('[data-yj-field="' + fieldName + '"]');
            if (!input) { continue; }
            value = trigger.dataset[key];
            if (input.type === "checkbox") {
                input.checked = value.toLowerCase() === "true";
            } else {
                input.value = value;
            }
        }
    }

    function focusFirstField(modal) {
        var el = modal.querySelector(
            "input:not([type='hidden']):not([disabled]), select:not([disabled]), textarea:not([disabled])"
        );
        if (el) { el.focus(); }
    }

    function onShown(event) {
        focusFirstField(event.target);
    }

    function onHidden() {
        if (lastTrigger && document.contains(lastTrigger)) { lastTrigger.focus(); }
        lastTrigger = null;
    }

    function onClick(event) {
        var trigger;
        var modal;
        var form;
        if (!window.bootstrap || !event.target.closest) { return; }
        trigger = event.target.closest("[data-yj-modal-target]");
        if (!trigger) { return; }
        modal = document.querySelector(trigger.getAttribute("data-yj-modal-target"));
        if (!modal || modal.getAttribute("data-yj-component") !== "modal-crud") { return; }
        event.preventDefault();
        form = modal.querySelector("form");
        if (form && trigger.dataset.action) {
            form.setAttribute("action", trigger.dataset.action);
        }
        applyFields(modal, trigger);
        lastTrigger = trigger;
        if (!modal.dataset.yjModalCrudBound) {
            modal.dataset.yjModalCrudBound = "1";
            modal.addEventListener("shown.bs.modal", onShown);
            modal.addEventListener("hidden.bs.modal", onHidden);
        }
        window.bootstrap.Modal.getOrCreateInstance(modal).show();
    }

    document.addEventListener("click", onClick);
}());
