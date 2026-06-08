// Tooltip init (Creator is not a FavoriteEntityType; follow CTA is a sign-in link for guests)
(function () {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) { new bootstrap.Tooltip(el); });
})();
