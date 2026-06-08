(function () {
    var guideInput = document.getElementById('bookingGuideId');
    var estTotal = document.getElementById('estTotal');
    var estCount = document.getElementById('estCount');
    var estPlural = document.getElementById('estPlural');
    var seatsHint = document.getElementById('seatsHint');
    var participantInputs = Array.prototype.slice.call(document.querySelectorAll('.js-participant'));
    var slots = Array.prototype.slice.call(document.querySelectorAll('.js-slot'));
    var avail;

    var unit = estTotal ? parseFloat(estTotal.getAttribute('data-unit')) || 0 : 0;
    var currency = estTotal ? (estTotal.getAttribute('data-currency') || '') : '';

    function selectedSlot() {
        return slots.find(function (s) { return s.checked; }) || null;
    }

    function totalParticipants() {
        return participantInputs.reduce(function (sum, el) {
            var v = parseInt(el.value, 10);
            return sum + (isNaN(v) || v < 0 ? 0 : v);
        }, 0);
    }

    function format(amount) {
        var rounded = Math.round(amount).toLocaleString();
        return currency ? (rounded + ' ' + currency) : rounded;
    }

    function syncGuide() {
        var slot = selectedSlot();
        if (slot && guideInput) {
            guideInput.value = slot.getAttribute('data-guide') || '';
        }
        if (slot && seatsHint) {
            avail = slot.getAttribute('data-available');
            if (avail) { seatsHint.textContent = 'Up to ' + avail + ' seat(s) available for the selected date.'; }
        }
    }

    function recalc() {
        var count = Math.max(1, totalParticipants());
        if (estCount) { estCount.textContent = count; }
        if (estPlural) { estPlural.textContent = count === 1 ? '' : 's'; }
        if (estTotal) { estTotal.textContent = format(unit * count); }
    }

    slots.forEach(function (s) { s.addEventListener('change', function () { syncGuide(); recalc(); }); });
    participantInputs.forEach(function (el) { el.addEventListener('input', recalc); });

    syncGuide();
    recalc();
})();
