// Provider area: AJAX submit for row-level action forms (JS5 — all traffic via
// window.YallaJo.api; SEC7 antiforgery handled by api-client postForm).
//
// Contract:
//   - Forms opt in with `data-yj-ajax`. On success the server (WantsAjax branch)
//     returns the refreshed fragment HTML; we swap the nearest section marked
//     `data-yj-swap` (or the one named by form's `data-yj-swap-target`).
//   - `data-success-message` carries the localized toast text (NF1).
//   - Forms with `data-confirm` are left to form-ux.js until confirmed
//     (dataset.yjConfirmed === "1"), so the shared confirm modal still runs (F8/MOD5).
//   - Without JS, every form falls back to its normal PRG POST (PE1).
(function () {
    "use strict";

    function api() {
        return window.YallaJo && window.YallaJo.api ? window.YallaJo.api : null;
    }

    function toast(message, type) {
        if (window.YallaJo && typeof window.YallaJo.toast === "function" && message) {
            window.YallaJo.toast(message, type);
        }
    }

    function resetLoading(form) {
        if (window.YallaJo && window.YallaJo.formUx && typeof window.YallaJo.formUx.resetLoading === "function") {
            window.YallaJo.formUx.resetLoading(form);
        }
    }

    function hideOpenModal(form) {
        var modalEl = form.closest(".modal");
        if (!modalEl || !window.bootstrap || !window.bootstrap.Modal) { return; }
        var instance = window.bootstrap.Modal.getInstance(modalEl);
        if (instance) { instance.dispose(); }
        // The modal element is about to be replaced mid-animation — clear any
        // orphaned backdrop/body state Bootstrap would otherwise leave behind.
        document.querySelectorAll(".modal-backdrop").forEach(function (el) { el.remove(); });
        document.body.classList.remove("modal-open");
        document.body.style.removeProperty("overflow");
        document.body.style.removeProperty("padding-right");
    }

    function findSwapTarget(form) {
        var name = form.dataset.yjSwapTarget;
        if (name) {
            return document.querySelector('[data-yj-swap="' + name + '"]');
        }
        return form.closest("[data-yj-swap]");
    }

    function swapSection(section, html) {
        if (!section) { return false; }
        var template = document.createElement("template");
        template.innerHTML = html.trim();
        var next = template.content.firstElementChild;
        if (!next) { return false; }
        section.replaceWith(next);
        // Notify enhancers (e.g. profile-avatar.js) that DOM was swapped so they
        // can rebind controls inside the new fragment.
        document.dispatchEvent(new CustomEvent("yj:swapped", { detail: { target: next } }));
        return true;
    }

    function handleForm(form) {
        var client = api();
        if (!client || form.dataset.yjBusy === "1") { return; }
        form.dataset.yjBusy = "1";

        var section = findSwapTarget(form);
        var errorText = form.dataset.errorText || "";

        client.postForm(form.action, new FormData(form))
            .then(function (response) {
                if (response.ok) {
                    return response.text().then(function (html) {
                        hideOpenModal(form);
                        var swapped = swapSection(section, html);
                        toast(form.dataset.successMessage, "success");
                        if (!swapped) {
                            // Fragment contract broken — fall back to a full reload (PE2).
                            window.location.reload();
                        }
                    });
                }
                var contentType = response.headers.get("Content-Type") || "";
                if (contentType.indexOf("text/html") !== -1) {
                    // Validation failure: server returned the refreshed fragment with
                    // inline field errors (e.g. 400 _ProfileCard). Swap it in place; the
                    // inline errors are the feedback, so no toast here.
                    return response.text().then(function (html) {
                        resetLoading(form);
                        var swapped = swapSection(section, html);
                        if (!swapped) {
                            window.location.reload();
                        }
                    });
                }
                return response.json().catch(function () { return {}; }).then(function (body) {
                    resetLoading(form);
                    toast(body.error || errorText, "error");
                });
            })
            .catch(function (err) {
                resetLoading(form);
                if (!err || !err.handled) {
                    toast(errorText, "error");
                }
            })
            .finally(function () {
                delete form.dataset.yjBusy;
            });
    }

    document.addEventListener("submit", function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.hasAttribute("data-yj-ajax")) { return; }
        if (!api()) { return; } // PE1: no client → native PRG
        // Let form-ux.js run the shared confirm modal first (F8); it re-submits
        // with yjConfirmed=1 once the user accepts.
        if (form.dataset.confirm && form.dataset.yjConfirmed !== "1") { return; }
        event.preventDefault();
        handleForm(form);
    });
})();
