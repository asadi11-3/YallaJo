(function () {
    // Bootstrap tooltips (disabled guest wishlist buttons).
    var tooltipTriggers = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggers.forEach(function (el) {
        if (window.bootstrap && bootstrap.Tooltip) new bootstrap.Tooltip(el);
    });
    // GLightbox gallery (glightbox is loaded by the layout).
    if (window.GLightbox) { GLightbox({ selector: '.glightbox' }); }
})();
