/*
 * admin-users.js — Admin › Users (list + details) page enhancements.
 *
 * Satisfies:
 *   A6 / SEC1 / J3  — external script, no inline JS, no inline on* handlers (CSP-safe).
 *   JS2             — declarative init: scans for [data-yj-component] on DOMContentLoaded.
 *   JS4             — idempotent init guard (dataset.yjInit) survives AJAX/DOM re-injection.
 *   PE1 / PE4       — progressive enhancement; every feature degrades to a working
 *                     server-rendered baseline (filter shows all rows, server-side
 *                     validators enforce archive/reassign confirmation regardless of JS).
 *
 * Two independent components share this file (only one is present per page):
 *   [data-yj-component="admin-users-list"]    → the Index row filter.
 *   [data-yj-component="admin-users-details"] → modal gating + double-submit guard.
 */
(function () {
    "use strict";

    // ── Index: client-side row filter (Email + Roles) ──────────────────────
    function initList(root) {
        if (root.dataset.yjInit === "1") { return; }
        root.dataset.yjInit = "1";

        const input = root.querySelector("[data-yj-users-filter]");
        if (!input) { return; }

        // Rows are queried lazily so the filter keeps working after listing.js
        // swaps the results fragment (AJAX pagination re-renders the table).
        input.addEventListener("input", function () {
            const q = this.value.trim().toLowerCase();
            const rows = root.querySelectorAll("#users-table tbody tr[data-filter-text]");
            Array.prototype.forEach.call(rows, function (row) {
                const hay = (row.getAttribute("data-filter-text") || "").toLowerCase();
                row.hidden = !(q.length === 0 || hay.indexOf(q) !== -1);
            });
        });
    }

    // ── Details: destructive-modal gates + double-submit guard ─────────────
    function initDetails(root) {
        if (root.dataset.yjInit === "1") { return; }
        root.dataset.yjInit = "1";

        // 1) Archive: enable Submit only when the localized confirmation
        //    token (data-confirm-token, e.g. "ARCHIVE" / "أرشفة") is typed.
        const archiveForm = document.getElementById("archive-form");
        if (archiveForm) {
            const aInput = archiveForm.querySelector('input[name="ConfirmText"]');
            const aSubmit = archiveForm.querySelector("[data-archive-submit]");
            if (aInput && aSubmit) {
                const aToken = aInput.getAttribute("data-confirm-token") || "ARCHIVE";
                const syncArchive = function () { aSubmit.disabled = (aInput.value.trim() !== aToken); };
                aInput.addEventListener("input", syncArchive);
                syncArchive();
            }
        }

        // 2) Reassign: enable Submit only when "I understand" is ticked AND
        //    the new-email field is non-empty.
        const reassignForm = document.getElementById("reassign-form");
        if (reassignForm) {
            const cb = reassignForm.querySelector('input[name="IUnderstand"]');
            const email = reassignForm.querySelector('input[name="NewEmail"]');
            const rSubmit = reassignForm.querySelector("[data-reassign-submit]");
            if (cb && email && rSubmit) {
                const syncReassign = function () {
                    rSubmit.disabled = !(cb.checked && email.value.trim().length > 0);
                };
                cb.addEventListener("change", syncReassign);
                email.addEventListener("input", syncReassign);
                syncReassign();
            }
        }

        // 3) Disable the submit button on every admin-action modal form to
        //    block a double-fire while the PRG redirect runs.
        const forms = document.querySelectorAll(".admin-action-modal form");
        Array.prototype.forEach.call(forms, function (form) {
            form.addEventListener("submit", function () {
                const btn = form.querySelector('button[type="submit"]');
                if (btn) { btn.disabled = true; }
            });
        });
    }

    function init() {
        const list = document.querySelector('[data-yj-component="admin-users-list"]');
        if (list) { initList(list); }

        const details = document.querySelector('[data-yj-component="admin-users-details"]');
        if (details) { initDetails(details); }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
