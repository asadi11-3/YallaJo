/**
 * auth-sessions.js — AJAX refinement for the Sessions page (Trust / Untrust /
 * Revoke / Sign-out-other-devices).
 *
 * Progressive enhancement (PE1): every form on the page is a plain POST that
 * works without JavaScript (PRG + flash). This script intercepts submits on
 * forms marked `data-sessions-ajax`, posts them via the shared API client
 * (JS5/SEC7), swaps the refreshed `_SessionsTable` partial into
 * `#sessions-table-container`, and surfaces the outcome as a toast (NF1).
 *
 * Plays with form-ux.js: its data-confirm interceptor runs in the CAPTURE
 * phase and cancels unconfirmed submits, so by the time the submit event
 * reaches this bubble-phase listener the confirm (if any) has already been
 * accepted.
 *
 * Idempotent init (JS4): guarded by a data flag on <html>.
 */
(function () {
    'use strict';

    var root = document.documentElement;
    if (root.dataset.yjAuthSessionsWired === '1') { return; }
    root.dataset.yjAuthSessionsWired = '1';

    var CONTAINER_ID = 'sessions-table-container';

    function setBusy(form, busy) {
        var buttons = form.querySelectorAll('button[type="submit"]');
        buttons.forEach(function (btn) {
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
        if (!form.hasAttribute('data-sessions-ajax')) { return; }
        if (!(window.YallaJo && window.YallaJo.api)) { return; } // graceful no-JS-client fallback → native POST

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
                // Header forms (sign-out-other-devices) survive the partial swap;
                // row forms inside the container were replaced, so this is a no-op
                // for them.
                if (document.contains(form)) { setBusy(form, false); }
            });
    });
})();
