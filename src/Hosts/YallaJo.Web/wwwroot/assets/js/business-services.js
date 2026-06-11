// Business services page: load the service edit form into an offcanvas via AJAX.
// Progressive enhancement (PE1): without JS the edit link performs a full page
// load and the form renders inline on Services/Index.
(function () {
    'use strict';

    function init() {
        var offcanvasEl = document.getElementById('service-edit-offcanvas');
        var bodyEl = document.getElementById('service-edit-offcanvas-body');
        if (!offcanvasEl || !bodyEl) { return; }
        if (offcanvasEl.dataset.yjInit === '1') { return; } // idempotent (JS4)
        offcanvasEl.dataset.yjInit = '1';

        var api = window.YallaJo && window.YallaJo.api;
        var hasBootstrap = typeof bootstrap !== 'undefined' && bootstrap.Offcanvas;
        if (!api || !hasBootstrap) { return; } // graceful fallback to full page load

        document.addEventListener('click', function (event) {
            var link = event.target.closest('a[data-service-edit]');
            if (!link) { return; }
            event.preventDefault();

            bodyEl.innerHTML = '<div class="placeholder-glow" aria-hidden="true">'
                + '<span class="placeholder col-8 mb-2"></span>'
                + '<span class="placeholder col-12 mb-2"></span>'
                + '<span class="placeholder col-10 mb-2"></span>'
                + '<span class="placeholder col-6"></span>'
                + '</div>';

            var offcanvas = bootstrap.Offcanvas.getOrCreateInstance(offcanvasEl);
            offcanvas.show();

            api.loadPartial(link.href)
                .then(function (markup) {
                    bodyEl.innerHTML = markup;
                })
                .catch(function () {
                    offcanvas.hide();
                    window.location.href = link.href; // fallback: full page (PE1)
                });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
