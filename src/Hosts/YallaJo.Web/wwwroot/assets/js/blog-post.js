// Blog post page: tooltips, view beacon, AJAX comment thread (Phase 5.4).
// Favourites handled by shared authed-only favorites.js in _Layout.
(function () {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) { new bootstrap.Tooltip(el); });
    // View beacon (BFF fire-and-forget; API debounces per viewer).
    var script = document.getElementById('blog-post-script');
    var viewUrl = script ? script.getAttribute('data-view-url') : '';
    if (!viewUrl) { return; }
    try {
        if (navigator.sendBeacon) {
            navigator.sendBeacon(viewUrl);
        } else {
            // Raw fetch is intentional (JS5 exception): fire-and-forget view beacon needs
            // `keepalive` semantics which YallaJo.api does not expose; endpoint is
            // [IgnoreAntiforgeryToken] and the result is never consumed.
            fetch(viewUrl, { method: 'POST', keepalive: true, credentials: 'same-origin' }).catch(function () { });
        }
    } catch (e) { }
})();

// AJAX comment thread: create/edit comments and reactions swap the #comments
// partial in place (UI-UX-S1 refinement); deletes stay native PRG behind the
// confirm modal (F8). Without JS every form still POSTs natively (PE1).
(function () {
    'use strict';

    if (!window.YallaJo || !window.YallaJo.api) { return; }
    var api = window.YallaJo.api;

    var COMMENT_ACTION = /\/blog\/(?:[^/]+\/comments|comments\/)/;

    function toast(message, type) {
        if (message && window.YallaJo.toast) { window.YallaJo.toast(message, type); }
    }

    function resetLoading(form) {
        if (window.YallaJo.formUx) { window.YallaJo.formUx.resetLoading(form); }
    }

    function errorMessage(err) {
        return (err && err.message) || (document.getElementById('comments') || {}).dataset && document.getElementById('comments').dataset.errorText || '';
    }

    function swapComments(html) {
        var current = document.getElementById('comments');
        if (!current) { window.location.reload(); return; }
        var tpl = document.createElement('template');
        tpl.innerHTML = html.trim();
        var next = tpl.content.querySelector('#comments') || tpl.content.firstElementChild;
        if (!next) { window.location.reload(); return; }
        current.replaceWith(next);
    }

    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!(form instanceof HTMLFormElement)) { return; }
        var action = form.getAttribute('action') || form.action || '';
        if (!COMMENT_ACTION.test(action)) { return; }
        // Deletes stay native PRG (confirm modal via form-ux data-confirm).
        if (/\/delete(\?.*)?$/.test(action)) { return; }
        if (form.dataset.yjBusy === '1') { e.preventDefault(); return; }

        e.preventDefault();
        form.dataset.yjBusy = '1';
        var isReaction = form.classList.contains('js-comment-react');

        api.postForm(action, new FormData(form))
            .then(function (response) {
                if (!response.ok) {
                    return response.json().catch(function () { return {}; }).then(function (body) {
                        throw { status: response.status, message: body && body.error };
                    });
                }
                return response.text();
            })
            .then(function (html) {
                form.dataset.yjBusy = '';
                swapComments(html);
                if (!isReaction) { toast(form.dataset.successMessage, 'success'); }
            })
            .catch(function (err) {
                form.dataset.yjBusy = '';
                resetLoading(form);
                toast(errorMessage(err), 'error');
            });
    }, true);
})();
