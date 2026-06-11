/*
 * password-meter.js — shared password-strength meter for the auth funnel (F3).
 * Replaces the byte-identical auth-signup.js / auth-resetpassword.js pair.
 *
 * Wiring: any <input data-psw-meter="#meterBarSelector"> gets a strength meter;
 * the selector defaults to "#pswMeter" when the attribute value is empty.
 * Idempotent (JS4): safe to include twice — inputs are wired at most once.
 */
(function () {
    'use strict';

    function wire(input) {
        if (input.dataset.yjMeterWired === '1') return;
        var meter = document.querySelector(input.getAttribute('data-psw-meter') || '#pswMeter');
        if (!meter) return;
        input.dataset.yjMeterWired = '1';
        input.addEventListener('input', function () {
            var v = input.value, score = 0;
            if (v.length >= 8) score++;
            if (/[a-z]/.test(v)) score++;
            if (/[A-Z]/.test(v)) score++;
            if (/[0-9]/.test(v)) score++;
            if (/[^A-Za-z0-9]/.test(v)) score++;
            var pct = (score / 5) * 100;
            var cls = score <= 2 ? 'bg-danger' : (score <= 4 ? 'bg-warning' : 'bg-success');
            meter.className = 'progress-bar ' + cls;
            // X6: width is a dynamic computed value driven by input scoring.
            meter.style.width = pct + '%';
            meter.setAttribute('aria-valuenow', pct);
        });
    }

    function init() {
        document.querySelectorAll('input[data-psw-meter]').forEach(wire);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
