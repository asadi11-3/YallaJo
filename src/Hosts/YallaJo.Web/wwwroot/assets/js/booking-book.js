// Booking page behavior (Phase 8.1, UI-UX-F6/F7/PE1).
// - Keeps the hidden guide input in sync with the selected availability slot.
// - Recalculates the estimated total when slot/participants change.
// - Client-side validation: a slot must be selected and travelers must fit the
//   available seats. Server-side validation remains the source of truth (PE1).
// All user-facing strings come from data-* attributes on #bookingForm (resx-localized).
(function () {
    "use strict";

    var form = document.getElementById("bookingForm");
    if (!form) {
        return;
    }

    var guideInput = document.getElementById("bookingGuideId");
    var estTotal = document.getElementById("estTotal");
    var estTravelers = document.getElementById("estTravelers");
    var seatsHint = document.getElementById("seatsHint");
    var slotError = document.getElementById("bookingSlotError");
    var seatsError = document.getElementById("bookingSeatsError");
    var participants = Array.prototype.slice.call(form.querySelectorAll(".js-participant"));
    var slots = Array.prototype.slice.call(form.querySelectorAll(".js-slot"));

    var msgSelectSlot = form.dataset.msgSelectSlot || "";
    var msgTooMany = form.dataset.msgTooMany || "";
    var seatsHintTemplate = form.dataset.seatsHint || "";
    var travelersTemplate = form.dataset.travelersLabel || "";

    function fmt(template, value) {
        return template.replace("{0}", String(value));
    }

    function selectedSlot() {
        var i;
        for (i = 0; i < slots.length; i += 1) {
            if (slots[i].checked) {
                return slots[i];
            }
        }
        return null;
    }

    function availableSeats(slot) {
        var avail = parseInt(slot.getAttribute("data-available"), 10);
        return isNaN(avail) ? 0 : avail;
    }

    function totalParticipants() {
        var total = 0;
        participants.forEach(function (input) {
            var value = parseInt(input.value, 10);
            if (!isNaN(value) && value > 0) {
                total += value;
            }
        });
        return total;
    }

    function showError(el, message) {
        if (el) {
            el.textContent = message;
            el.classList.remove("d-none");
        }
    }

    function hideError(el) {
        if (el) {
            el.textContent = "";
            el.classList.add("d-none");
        }
    }

    function syncGuide() {
        var slot = selectedSlot();
        if (!slot) {
            return;
        }
        if (guideInput) {
            guideInput.value = slot.getAttribute("data-guide") || "";
        }
        if (seatsHint && seatsHintTemplate) {
            seatsHint.textContent = fmt(seatsHintTemplate, availableSeats(slot));
        }
    }

    function recalc() {
        var count = Math.max(1, totalParticipants());
        var unit;
        var currency;
        if (estTravelers && travelersTemplate) {
            estTravelers.textContent = fmt(travelersTemplate, count);
        }
        if (estTotal) {
            unit = parseFloat(estTotal.getAttribute("data-unit"));
            currency = estTotal.getAttribute("data-currency") || "";
            if (!isNaN(unit)) {
                estTotal.textContent = Math.round(unit * count).toLocaleString() + " " + currency;
            }
        }
    }

    function setParticipantsInvalid(invalid) {
        participants.forEach(function (input) {
            input.classList.toggle("is-invalid", invalid);
        });
    }

    function validate() {
        var ok = true;
        var slot = selectedSlot();
        var avail;

        if (slots.length > 0 && !slot) {
            showError(slotError, msgSelectSlot);
            ok = false;
        } else {
            hideError(slotError);
        }

        if (slot) {
            avail = availableSeats(slot);
            if (avail > 0 && totalParticipants() > avail) {
                showError(seatsError, fmt(msgTooMany, avail));
                setParticipantsInvalid(true);
                ok = false;
            } else {
                hideError(seatsError);
                setParticipantsInvalid(false);
            }
        } else {
            hideError(seatsError);
            setParticipantsInvalid(false);
        }

        return ok;
    }

    function firstVisibleError() {
        if (slotError && !slotError.classList.contains("d-none")) {
            return slotError;
        }
        if (seatsError && !seatsError.classList.contains("d-none")) {
            return seatsError;
        }
        return null;
    }

    form.addEventListener("submit", function (event) {
        if (validate()) {
            return;
        }
        event.preventDefault();
        // form-ux's capture-phase listener already started the loading state; undo it.
        if (window.YallaJo && window.YallaJo.formUx) {
            window.YallaJo.formUx.resetLoading(form);
        }
        var error = firstVisibleError();
        if (error && typeof error.scrollIntoView === "function") {
            error.scrollIntoView({ behavior: "smooth", block: "center" });
        }
    });

    slots.forEach(function (slot) {
        slot.addEventListener("change", function () {
            syncGuide();
            recalc();
            validate();
        });
    });

    participants.forEach(function (input) {
        input.addEventListener("input", function () {
            recalc();
            if (selectedSlot()) {
                validate();
            }
        });
    });

    syncGuide();
    recalc();
}());
