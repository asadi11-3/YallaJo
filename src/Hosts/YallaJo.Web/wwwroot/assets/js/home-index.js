/* Home page interactive bits.
   CSP-safe: no inline script. Each vendor init is feature-detected so missing
   libs degrade gracefully (per UI-UX-Design.md ERR3).
   - Choices.js on the Location <select>
   - Flatpickr range picker on the Date input -> hidden from/to inputs
   - Adults + Children counter -> hidden participants total
   - GLightbox init on .glightbox links (Watch our story modal)
   - Bootstrap Tooltip init (retains existing behavior) */
(function () {
    'use strict';

    function initLocationChoices() {
        var el = document.getElementById('heroLocation');
        if (!el || typeof window.Choices !== 'function') return;
        try {
            new window.Choices(el, {
                searchEnabled: true,
                shouldSort: false,
                itemSelectText: '',
                allowHTML: false
            });
        } catch (e) { /* leave as native <select> */ }
    }

    function initDateRange() {
        var dateInput = document.getElementById('heroDate');
        var fromInput = document.getElementById('heroDateFrom');
        var toInput = document.getElementById('heroDateTo');
        if (!dateInput || !fromInput || !toInput) return;
        if (typeof window.flatpickr !== 'function') return;

        // ISO yyyy-mm-dd is safer for query params than the display format.
        function isoDate(d) {
            var y = d.getFullYear();
            var m = String(d.getMonth() + 1).padStart(2, '0');
            var day = String(d.getDate()).padStart(2, '0');
            return y + '-' + m + '-' + day;
        }

        try {
            window.flatpickr(dateInput, {
                mode: 'range',
                minDate: 'today',
                dateFormat: 'M j, Y',
                onChange: function (selectedDates) {
                    if (selectedDates.length === 2) {
                        fromInput.value = isoDate(selectedDates[0]);
                        toInput.value = isoDate(selectedDates[1]);
                    } else if (selectedDates.length === 1) {
                        fromInput.value = isoDate(selectedDates[0]);
                        toInput.value = '';
                    } else {
                        fromInput.value = '';
                        toInput.value = '';
                    }
                }
            });
        } catch (e) { /* fall back to plain text input */ }
    }

    function initParticipantsCounter() {
        var label = document.getElementById('heroParticipantsLabel');
        var total = document.getElementById('heroParticipants');
        if (!label || !total) return;

        var state = { adults: 1, children: 0 };
        var min = { adults: 1, children: 0 };
        var max = { adults: 20, children: 20 };

        function refresh() {
            ['adults', 'children'].forEach(function (key) {
                var node = document.querySelector('[data-counter-value="' + key + '"]');
                if (node) node.textContent = String(state[key]);
                document.querySelectorAll('[data-counter="' + key + '"][data-action="dec"]').forEach(function (b) {
                    b.disabled = state[key] <= min[key];
                });
                document.querySelectorAll('[data-counter="' + key + '"][data-action="inc"]').forEach(function (b) {
                    b.disabled = state[key] >= max[key];
                });
            });
            var totalTravelers = state.adults + state.children;
            // Phase 9 (CON1): localized words come from data-word-* on the label; EN fallbacks for safety.
            var words = label.dataset || {};
            var pluralAdults = state.adults === 1 ? (words.wordAdult || 'adult') : (words.wordAdults || 'adults');
            var pluralChildren = state.children === 1 ? (words.wordChild || 'child') : (words.wordChildren || 'children');
            total.value = String(totalTravelers);
            if (state.children === 0) {
                label.textContent = state.adults + ' ' + pluralAdults;
            } else {
                label.textContent = state.adults + ' ' + pluralAdults + ', ' + state.children + ' ' + pluralChildren;
            }
        }

        document.querySelectorAll('[data-counter][data-action]').forEach(function (btn) {
            btn.addEventListener('click', function (ev) {
                ev.preventDefault();
                var key = btn.getAttribute('data-counter');
                var action = btn.getAttribute('data-action');
                if (!Object.prototype.hasOwnProperty.call(state, key)) return;
                var next = state[key] + (action === 'inc' ? 1 : -1);
                if (next < min[key] || next > max[key]) return;
                state[key] = next;
                refresh();
            });
        });

        refresh();
    }

    function initGLightbox() {
        if (typeof window.GLightbox !== 'function') return;
        try {
            window.GLightbox({ selector: '.glightbox' });
        } catch (e) { /* leave links as plain anchors */ }
    }

    function initTooltips() {
        if (!window.bootstrap || !window.bootstrap.Tooltip) return;
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
            try { new window.bootstrap.Tooltip(el); } catch (e) { /* noop */ }
        });
    }

    /* Popular-right-now horizontal rail: prev/next buttons scroll the track.
       RTL-aware (scroll sign flips automatically because scrollLeft is negative
       in RTL on modern engines; we read the computed direction to be safe). */
    function initRails() {
        document.querySelectorAll('[data-rail]').forEach(function (rail) {
            var track = rail.querySelector('[data-rail-track]');
            if (!track) return;
            var prev = rail.querySelector('[data-rail-prev]');
            var next = rail.querySelector('[data-rail-next]');
            var isRtl = (getComputedStyle(rail).direction === 'rtl');

            function step() {
                var card = track.querySelector('.yj-pop-card');
                var amount = card ? (card.getBoundingClientRect().width + 16) * 2 : track.clientWidth * 0.8;
                return Math.max(amount, 200);
            }
            function scrollByDir(forward) {
                var delta = step() * (forward ? 1 : -1) * (isRtl ? -1 : 1);
                try { track.scrollBy({ left: delta, behavior: 'smooth' }); }
                catch (e) { track.scrollLeft += delta; }
            }
            function refreshNav() {
                if (!prev && !next) return;
                var max = track.scrollWidth - track.clientWidth - 1;
                var pos = Math.abs(track.scrollLeft);
                if (prev) prev.disabled = pos <= 0;
                if (next) next.disabled = pos >= max;
            }
            if (prev) prev.addEventListener('click', function () { scrollByDir(false); });
            if (next) next.addEventListener('click', function () { scrollByDir(true); });
            track.addEventListener('scroll', refreshNav, { passive: true });
            refreshNav();
        });
    }

    /* Featured category chips: progressive client-side highlight only.
       Chips remain real links (?categoryId=...) so no-JS users still navigate.
       The "All" chip [data-chip-filter="all"] just toggles the active class. */
    function initChips() {
        document.querySelectorAll('.yj-chips').forEach(function (group) {
            var chips = group.querySelectorAll('.yj-chip');
            if (!chips.length) return;
            chips.forEach(function (chip) {
                chip.addEventListener('click', function () {
                    chips.forEach(function (c) { c.classList.remove('active'); });
                    chip.classList.add('active');
                });
            });
        });
    }

    function init() {
        initLocationChoices();
        initDateRange();
        initParticipantsCounter();
        initGLightbox();
        initTooltips();
        initRails();
        initChips();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
