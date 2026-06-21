/*
 * change-password.js
 * Dedicated Change Password page interactions:
 *  (a) per-field show/hide password toggles ([data-cp-toggle])
 *  (b) live password requirement checklist (#cpChecklist) driven by #cpNew
 * Progressive enhancement; no-ops when the elements are absent.
 */
(function () {
    'use strict';

    function resolveTarget(toggle) {
        var selector = toggle.getAttribute('data-bs-target');
        if (!selector) {
            return null;
        }
        try {
            return document.querySelector(selector);
        } catch (e) {
            return null;
        }
    }

    function wireToggle(toggle) {
        if (toggle.dataset.cpToggleWired === '1') {
            return;
        }
        toggle.dataset.cpToggleWired = '1';

        var input = resolveTarget(toggle);
        if (!input) {
            return;
        }

        var icon = toggle.querySelector('i');
        var showLabel = toggle.getAttribute('data-cp-show') || 'Show password';
        var hideLabel = toggle.getAttribute('data-cp-hide') || 'Hide password';

        toggle.addEventListener('click', function () {
            var reveal = input.type === 'password';
            input.type = reveal ? 'text' : 'password';
            toggle.setAttribute('aria-pressed', reveal ? 'true' : 'false');
            toggle.setAttribute('aria-label', reveal ? hideLabel : showLabel);
            toggle.setAttribute('title', reveal ? hideLabel : showLabel);
            if (icon) {
                icon.classList.toggle('bi-eye', !reveal);
                icon.classList.toggle('bi-eye-slash', reveal);
            }
        });
    }

    var RULES = {
        length: function (value) { return value.length >= 8; },
        upper: function (value) { return /[A-Z]/.test(value); },
        number: function (value) { return /[0-9]/.test(value); },
        special: function (value) { return /[^A-Za-z0-9]/.test(value); }
    };

    function wireChecklist() {
        var input = document.getElementById('cpNew');
        var checklist = document.getElementById('cpChecklist');
        if (!input || !checklist || checklist.dataset.cpChecklistWired === '1') {
            return;
        }
        checklist.dataset.cpChecklistWired = '1';

        var items = checklist.querySelectorAll('[data-cp-rule]');

        function evaluate() {
            var value = input.value || '';
            items.forEach(function (item) {
                var rule = RULES[item.getAttribute('data-cp-rule')];
                var met = typeof rule === 'function' ? rule(value) : false;
                item.classList.toggle('is-met', met);
                var icon = item.querySelector('i');
                if (icon) {
                    icon.classList.toggle('bi-circle', !met);
                    icon.classList.toggle('bi-check-circle-fill', met);
                    icon.classList.toggle('text-success', met);
                }
            });
        }

        input.addEventListener('input', evaluate);
        evaluate();
    }

    function init() {
        document.querySelectorAll('[data-cp-toggle]').forEach(wireToggle);
        wireChecklist();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
