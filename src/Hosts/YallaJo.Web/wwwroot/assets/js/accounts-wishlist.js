// accounts-wishlist.js
// Phase 4b (Accounts plan): wishlist grid client sort + optimistic remove with Undo
// (UI-UX-WL2/WL3, NF6, PE1). No-JS path: the per-item remove forms POST natively (PRG)
// and the sort control is simply inert.
(function () {
    'use strict';

    var grid = document.getElementById('wishlistGrid');
    var sort = document.getElementById('wishlistSort');

    // ---- Sort (moved out of the inline script) ----
    if (sort && grid) {
        sort.addEventListener('change', function () {
            var items = Array.prototype.slice.call(grid.querySelectorAll('.wishlist-item'));
            var mode = sort.value;
            items.sort(function (a, b) {
                switch (mode) {
                    case 'title':
                        return (a.dataset.title || '').localeCompare(b.dataset.title || '');
                    case 'rating':
                        return parseFloat(b.dataset.rating || '0') - parseFloat(a.dataset.rating || '0');
                    case 'price':
                        return parseFloat(a.dataset.price || '0') - parseFloat(b.dataset.price || '0');
                    default:
                        return new Date(b.dataset.added) - new Date(a.dataset.added);
                }
            });
            items.forEach(function (el) { grid.appendChild(el); });
        });
    }

    // ---- Optimistic remove + Undo ----
    var api = window.YallaJo && window.YallaJo.api;
    if (!grid || !api) { return; } // PE1: native form POST still works

    var removedText = grid.dataset.removedText || 'Removed from your wishlist.';
    var undoText = grid.dataset.undoText || 'Undo';
    var failedText = grid.dataset.failedText || 'Could not remove the item. Please try again.';

    var banner = null;
    var pendingCommit = null; // force-commit the previous (still-undoable) removal

    function toast(message, type) {
        if (window.YallaJo && typeof window.YallaJo.toast === 'function') {
            window.YallaJo.toast(message, type);
        }
    }

    function ensureBanner() {
        if (banner) { return banner; }
        banner = document.createElement('div');
        banner.className = 'alert alert-secondary d-flex justify-content-between align-items-center d-none mb-3';
        banner.setAttribute('role', 'status');
        banner.innerHTML =
            '<span class="js-wishlist-undo-text"></span>' +
            '<button type="button" class="btn btn-sm btn-link p-0 ms-3 js-wishlist-undo"></button>';
        grid.parentNode.insertBefore(banner, grid);
        var btn = banner.querySelector('.js-wishlist-undo');
        btn.textContent = undoText;
        btn.addEventListener('click', function () {
            if (typeof banner._undo === 'function') { banner._undo(); }
        });
        return banner;
    }

    function showUndo(undoFn) {
        var b = ensureBanner();
        b.querySelector('.js-wishlist-undo-text').textContent = removedText;
        b._undo = undoFn;
        b.classList.remove('d-none');
    }

    function hideUndo() {
        if (banner) { banner.classList.add('d-none'); banner._undo = null; }
    }

    function scheduleRemove(form) {
        var action = form.getAttribute('action') || '';
        var card = form.closest('.wishlist-item');
        if (!card) { return; }

        // Flush any previous still-undoable removal before starting a new one.
        if (typeof pendingCommit === 'function') { pendingCommit(); }

        var parent = card.parentNode;
        var placeholder = document.createComment('wishlist-removed');
        var fd = new FormData(form);
        var committed = false;
        var timer = 0;

        parent.insertBefore(placeholder, card);
        var detached = parent.removeChild(card);

        function restore() {
            if (placeholder.parentNode) {
                placeholder.parentNode.insertBefore(detached, placeholder);
                placeholder.parentNode.removeChild(placeholder);
            }
        }

        function commit() {
            if (committed) { return; }
            committed = true;
            pendingCommit = null;
            if (timer) { clearTimeout(timer); }
            hideUndo();
            api.postForm(action, fd).then(function (res) {
                if (!res || !res.ok) { throw new Error(failedText); }
                if (placeholder.parentNode) { placeholder.parentNode.removeChild(placeholder); }
            }).catch(function () {
                restore();
                toast(failedText, 'error');
            });
        }

        function undo() {
            if (committed) { return; }
            committed = true;
            pendingCommit = null;
            if (timer) { clearTimeout(timer); }
            hideUndo();
            restore(); // server was never touched
        }

        pendingCommit = commit;
        timer = setTimeout(commit, 6000);
        showUndo(undo);
    }

    grid.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form || form.tagName !== 'FORM') { return; }
        var action = form.getAttribute('action') || '';
        if (action.indexOf('/wishlist/remove/') === -1) { return; } // only per-item removes
        e.preventDefault();
        scheduleRemove(form);
    });

    // Commit any pending removal if the user leaves the page.
    window.addEventListener('pagehide', function () {
        if (typeof pendingCommit === 'function') { pendingCommit(); }
    });
})();
