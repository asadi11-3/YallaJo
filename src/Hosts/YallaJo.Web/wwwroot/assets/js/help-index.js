(function () {
    // Client-side FAQ filter (Help index).
    var input = document.getElementById('faqSearch');
    var items = Array.prototype.slice.call(document.querySelectorAll('.js-faq-item'));
    var noResults = document.getElementById('faqNoResults');

    if (input) {
        input.addEventListener('input', function () {
            var q = (input.value || '').trim().toLowerCase();
            var visible = 0;
            items.forEach(function (el) {
                var hay = el.getAttribute('data-faq') || '';
                var match = q === '' || hay.indexOf(q) !== -1;
                el.classList.toggle('d-none', !match);
                if (match) visible++;
            });
            if (noResults) noResults.classList.toggle('d-none', visible !== 0);
        });
    }

    // Phase 6: deep-link support — /help/{id} 301s to /help#faq-{id}; expand
    // the targeted accordion item and bring it into view. Without JS the
    // browser still scrolls to the anchor (PE1).
    function openFromHash() {
        var hash = window.location.hash || '';
        if (hash.indexOf('#faq-') !== 0) return;

        var item;
        try {
            item = document.querySelector(hash);
        } catch (e) {
            return;
        }
        if (!item) return;

        var collapseEl = item.querySelector('.accordion-collapse');
        if (collapseEl && window.bootstrap && window.bootstrap.Collapse) {
            window.bootstrap.Collapse.getOrCreateInstance(collapseEl, { toggle: false }).show();
        }
        item.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    openFromHash();
    window.addEventListener('hashchange', openFromHash);
})();
