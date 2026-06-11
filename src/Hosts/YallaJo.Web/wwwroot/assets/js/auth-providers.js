/**
 * auth-providers.js — AJAX refinement for the linked-accounts page (Unlink).
 *
 * Progressive enhancement (PE1): the Unlink forms are plain POSTs that work
 * without JavaScript (PRG + flash). This script intercepts submits on forms
 * marked `data-providers-ajax`, posts them via the shared API client
 * (JS5/SEC7), swaps the refreshed `_ProvidersList` partial into
 * `#providers-list-container`, and surfaces the outcome as a toast (NF1).
 * The 409 last-login-method guard arrives as a localized error message from
 * the controller and is shown as a danger toast.
 *
 * The OAuth link/challenge forms are NOT intercepted — linking is a full
 * navigation to the provider by design.
 *
 * Plays with form-ux.js: its data-confirm interceptor runs in the CAPTURE
 * phase and cancels unconfirmed submits, so by the time the submit event
 * reaches this bubble-phase listener the confirm (if any) was accepted.
 *
 * Idempotent init (JS4): guarded by a data flag on <html>.
 */
(function () {
    'use strict';

    var root = document.documentElement;
    if (root.dataset.yjAuthProvidersWired === '1') { return; }
    root.dataset.yjAuthProvidersWired = '1';

    var CONTAINER_ID = 'providers-list-container';

    function setBusy(form, busy) {
        form.querySelectorAll('button[type="submit"]').forEach(function (btn) {
            btn.disabled = busy;
            if (busy) {
                btn.setAttribute('aria-busy', 'true');
                btn.dataset.yjHtml = btn.innerHTML;
                btn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>';
            } else {
                btn.removeAttribute('aria-busy');
                if (btn.dataset.yjHtml) {
                    btn.innerHTML = btn.dataset.yjHtml;
                    delete btn.dataset.yjHtml;
                }
            }
        });
    }

    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement)) { return; }
        if (!form.hasAttribute('data-providers-ajax')) { return; }
        if (!(window.YallaJo && window.YallaJo.api)) { return; } // no API client → native POST (PE1)

        var container = document.getElementById(CONTAINER_ID);
        if (!container) { return; }

        event.preventDefault();
        if (form.dataset.yjBusy === '1') { return; }
        form.dataset.yjBusy = '1';
        setBusy(form, true);

        window.YallaJo.api.postForm(form.action, new FormData(form))
            .then(function (response) { return response.text(); })
            .then(function (html) {
                container.innerHTML = html;
                if (form.dataset.successMsg && window.YallaJo.toast) {
                    window.YallaJo.toast(form.dataset.successMsg, 'success');
                }
            })
            .catch(function (err) {
                var message = (err && err.message) || form.dataset.errorMsg || '';
                if (message && window.YallaJo.toast) {
                    window.YallaJo.toast(message, 'danger');
                }
            })
            .finally(function () {
                delete form.dataset.yjBusy;
                // Forms inside the container were replaced on success; reset only
                // if this one is still attached (failure path).
                if (document.contains(form)) { setBusy(form, false); }
            });
    });
})();
