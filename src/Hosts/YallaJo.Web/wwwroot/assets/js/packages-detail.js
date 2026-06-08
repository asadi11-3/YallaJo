// Tooltip init only; favourite POSTs handled by shared authed-only favorites.js in _Layout.
(function () {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) { new bootstrap.Tooltip(el); });
})();
