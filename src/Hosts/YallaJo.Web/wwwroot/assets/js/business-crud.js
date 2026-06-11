/**
 * Business area AJAX CRUD enhancement (JS1-JS6, PE1, NF1, SEC7, A11Y4).
 *
 * Progressive enhancement on top of the PRG no-JS flows:
 *  - [data-yj-component="business-crud"] roots: add/remove/edit forms post via
 *    window.YallaJo.api.postForm and swap the refreshed list partial returned by
 *    the controller (WantsAjax). Success toast comes from the X-Yj-Toast header
 *    (URL-encoded) or the form's data-crud-success attribute.
 *  - 400 responses carry { error, errors } — field errors are written into the
 *    matching asp-validation-for spans (role="alert" already present, A11Y4).
 *  - [data-yj-component="business-hours"] form: the IsClosed checkbox disables
 *    that row's time inputs (D-14); disabled inputs are re-enabled on submit so
 *    values still post.
 * Without window.YallaJo.api the script is a no-op and native submits run (PE1).
 */
(function () {
    "use strict";

    function api() {
        return window.YallaJo && window.YallaJo.api ? window.YallaJo.api : null;
    }

    function formUx() {
        return window.YallaJo && window.YallaJo.formUx ? window.YallaJo.formUx : null;
    }

    function toast(message, type) {
        if (message && window.YallaJo && typeof window.YallaJo.toast === "function") {
            window.YallaJo.toast(message, type || "success");
        }
    }

    /* ---------------------------------------------------------------- *
     *  AJAX CRUD: list swap                                            *
     * ---------------------------------------------------------------- */

    var controllers = new WeakMap(); // root -> AbortController (JS6)

    function skeleton() {
        return '<div class="card shadow" aria-hidden="true"><div class="card-body placeholder-glow">' +
            '<span class="placeholder col-7 mb-2"></span><span class="placeholder col-9 mb-2"></span>' +
            '<span class="placeholder col-5 mb-2"></span><span class="placeholder col-8"></span>' +
            "</div></div>";
    }

    function clearFieldErrors(form) {
        form.querySelectorAll("span[data-valmsg-for]").forEach(function (span) {
            span.textContent = "";
            span.classList.remove("field-validation-error");
            span.classList.add("field-validation-valid");
        });
    }

    function showFieldErrors(form, errors) {
        var shown = false;
        Object.keys(errors || {}).forEach(function (key) {
            var messages = errors[key];
            if (!messages || !messages.length) { return; }
            var span = form.querySelector('span[data-valmsg-for="' + CSS.escape(key) + '"]');
            if (!span) {
                // Server keys may be unprefixed ("Name") while inputs bind as "Form.Name".
                span = form.querySelector('span[data-valmsg-for="Form.' + CSS.escape(key) + '"]');
            }
            if (span) {
                span.textContent = messages[0];
                span.classList.remove("field-validation-valid");
                span.classList.add("field-validation-error");
                shown = true;
            }
        });
        return shown;
    }

    function decodeToast(response) {
        var raw = response.headers.get("X-Yj-Toast");
        if (!raw) { return ""; }
        try { return decodeURIComponent(raw); } catch (_) { return ""; }
    }

    async function handleCrudSubmit(form, root, listEl) {
        var client = api();
        var ux = formUx();
        var previous = controllers.get(root);
        if (previous) { previous.abort(); }
        var controller = new AbortController();
        controllers.set(root, controller);

        var isDelete = form.hasAttribute("data-confirm");
        var previousMarkup = listEl.innerHTML;
        listEl.innerHTML = skeleton();
        clearFieldErrors(form);

        try {
            var response = await client.postForm(form.action, new FormData(form), { signal: controller.signal });
            var markup = await response.text();

            if (response.redirected) {
                // Controller fell back to PRG (e.g. reload failed) — follow it.
                window.location.href = response.url;
                return;
            }

            listEl.innerHTML = markup;
            toast(decodeToast(response) || form.dataset.crudSuccess || "", "success");
            if (!isDelete) { form.reset(); }
        } catch (err) {
            listEl.innerHTML = previousMarkup;
            if (err && err.aborted) { return; }
            if (err && err.handled) { return; } // 401 redirect already in flight
            var inline = err && err.errors ? showFieldErrors(form, err.errors) : false;
            if (!inline) {
                toast((err && err.message) || form.dataset.crudError || "Something went wrong. Please try again.", "error");
            }
        } finally {
            controllers.delete(root);
            if (ux) { ux.resetLoading(form); }
        }
    }

    /* ---------------------------------------------------------------- *
     *  Hours: closed checkbox disables that row's time inputs (D-14)   *
     * ---------------------------------------------------------------- */

    function syncHoursRow(checkbox) {
        var row = checkbox.closest("tr");
        if (!row) { return; }
        row.querySelectorAll('input[type="time"]').forEach(function (input) {
            input.disabled = checkbox.checked;
        });
    }

    function initHoursForm(form) {
        if (form.dataset.yjHoursInit === "1") { return; } // idempotent (JS4)
        form.dataset.yjHoursInit = "1";

        var checkboxes = form.querySelectorAll('input[type="checkbox"][id$="IsClosed"]');
        checkboxes.forEach(syncHoursRow);

        form.addEventListener("change", function (event) {
            var target = event.target;
            if (target && target.matches('input[type="checkbox"][id$="IsClosed"]')) {
                syncHoursRow(target);
            }
        });

        // Re-enable before submit so disabled time values still post unchanged.
        form.addEventListener("submit", function () {
            form.querySelectorAll('input[type="time"][disabled]').forEach(function (input) {
                input.disabled = false;
            });
        });
    }

    /* ---------------------------------------------------------------- *
     *  Wiring                                                          *
     * ---------------------------------------------------------------- */

    function init() {
        if (document.documentElement.dataset.yjBusinessCrud === "1") { return; } // idempotent (JS4)
        document.documentElement.dataset.yjBusinessCrud = "1";

        document.querySelectorAll('form[data-yj-component="business-hours"]').forEach(initHoursForm);

        if (!api()) { return; } // graceful no-op: native submits keep working (PE1)

        // Bubble phase so form-ux's capture-phase confirm/data-loading logic runs first.
        document.addEventListener("submit", function (event) {
            var form = event.target;
            if (!(form instanceof HTMLFormElement)) { return; }

            var root = form.closest('[data-yj-component="business-crud"]');
            if (!root) { return; }
            if (event.defaultPrevented) { return; } // confirm modal pending

            var listEl = root.dataset.crudList ? document.querySelector(root.dataset.crudList) : null;
            if (!listEl) { return; }

            event.preventDefault();
            handleCrudSubmit(form, root, listEl);
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
