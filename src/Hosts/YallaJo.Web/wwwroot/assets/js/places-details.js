// Tooltip init for the guest "Log in to save" hint. The shared authed-only
// ~/assets/js/favorites.js (loaded in _Layout) handles the .js-favorite heart POST.
(function () {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) { new bootstrap.Tooltip(el); });
})();
