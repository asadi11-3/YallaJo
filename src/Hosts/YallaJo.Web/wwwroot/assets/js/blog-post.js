// Tooltip init; favourites handled by shared authed-only favorites.js in _Layout.
(function () {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) { new bootstrap.Tooltip(el); });
    // View beacon (BFF fire-and-forget; API debounces per viewer).
    var script = document.getElementById('blog-post-script');
    var viewUrl = script ? script.getAttribute('data-view-url') : '';
    if (!viewUrl) { return; }
    try {
        if (navigator.sendBeacon) {
            navigator.sendBeacon(viewUrl);
        } else {
            fetch(viewUrl, { method: 'POST', keepalive: true, credentials: 'same-origin' }).catch(function () { });
        }
    } catch (e) { }
})();
