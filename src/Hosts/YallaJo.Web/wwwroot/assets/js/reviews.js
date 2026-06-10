/*
 * reviews.js — AJAX layer for the unified Reviews controller (Phase 5.3).
 * Progressive enhancement (UI-UX-PE1): every form posts natively without JS;
 * with JS we intercept, post via the shared api client (UI-JS JS5) and swap
 * the refreshed partial in place (no full reload). Helpful votes are
 * optimistic (UI-UX-NF6). Delete forms are NOT intercepted — they go through
 * the confirm modal (form-ux data-confirm) and classic PRG.
 */
(function () {
    "use strict";

    var REVIEWS_ACTION = /\/reviews\/(tour|place|business|guide)\//;

    function api() {
        return window.YallaJo && window.YallaJo.api ? window.YallaJo.api : null;
    }

    function toast(message, type) {
        if (window.YallaJo && typeof window.YallaJo.toast === "function" && message) {
            window.YallaJo.toast(message, type || "info");
        }
    }

    function resetLoading(form) {
        if (window.YallaJo && window.YallaJo.formUx) {
            window.YallaJo.formUx.resetLoading(form);
        }
    }

    /** #accessibility-reviews for accessibility routes, #reviews otherwise. */
    function targetSection(actionUrl) {
        var id = actionUrl.indexOf("/accessibility") !== -1 ? "accessibility-reviews" : "reviews";
        return document.getElementById(id);
    }

    function swapSection(section, html) {
        if (!section || !html) { return false; }
        var tpl = document.createElement("template");
        tpl.innerHTML = html.trim();
        var next = tpl.content.firstElementChild;
        if (!next) { return false; }
        section.replaceWith(next);
        return true;
    }

    function errorMessage(err, form) {
        if (err && err.message) { return err.message; }
        var root = form.closest("[data-error-text]");
        return (root && root.dataset.errorText) || (form.dataset.errorText || "");
    }

    /* Optimistic helpful vote (NF6): disable instantly, post JSON, then
       quietly refresh the reviews section so count + button state update. */
    function handleHelpful(form, client) {
        var button = form.querySelector("button[type=submit], button:not([type])");
        if (form.dataset.yjBusy === "1") { return; }
        form.dataset.yjBusy = "1";
        if (button) { button.disabled = true; }

        var action = form.action;
        client.post(action)
            .then(function () {
                // /reviews/{t}/{id}/{reviewId}/(un)helpful → /reviews/{t}/{id}/list
                var listUrl = action.replace(/\/[0-9a-fA-F-]{36}\/(helpful|unhelpful)(\?.*)?$/, "/list");
                return client.loadPartial(listUrl).then(function (html) {
                    swapSection(document.getElementById("reviews"), html);
                });
            })
            .catch(function (err) {
                if (button) { button.disabled = false; }
                toast(errorMessage(err, form), "error");
            })
            .then(function () {
                delete form.dataset.yjBusy;
            });
    }

    /* Create/edit/report forms: post FormData, swap the refreshed partial. */
    function handleForm(form, client) {
        if (form.dataset.yjBusy === "1") { return; }
        form.dataset.yjBusy = "1";

        client.postForm(form.action, new FormData(form))
            .then(function (response) {
                if (response.ok) {
                    return response.text().then(function (html) {
                        var swapped = swapSection(targetSection(form.action), html);
                        toast(form.dataset.successMessage, "success");
                        if (!swapped) { window.location.reload(); }
                    });
                }
                return response.json()
                    .catch(function () { return {}; })
                    .then(function (body) {
                        resetLoading(form);
                        toast(body.error || errorMessage(null, form), "error");
                    });
            })
            .catch(function (err) {
                resetLoading(form);
                if (!err || !err.handled) { toast(errorMessage(err, form), "error"); }
            })
            .then(function () {
                delete form.dataset.yjBusy;
            });
    }

    document.addEventListener("submit", function (event) {
        var client = api();
        if (!client) { return; } // PE1: no api client → native POST

        var form = event.target;
        if (!(form instanceof HTMLFormElement)) { return; }
        if (!REVIEWS_ACTION.test(form.action || "")) { return; }

        // Deletes stay native: confirm modal (data-confirm) + PRG.
        if (/\/delete(\?.*)?$/.test(form.action)) { return; }

        // Respect the confirm modal contract: if a data-confirm form has not
        // been confirmed yet, let form-ux handle it first.
        if (form.hasAttribute("data-confirm") && form.dataset.yjConfirmed !== "1") { return; }

        event.preventDefault();

        if (form.classList.contains("js-helpful-form")) {
            handleHelpful(form, client);
        } else {
            handleForm(form, client);
        }
    });
})();
