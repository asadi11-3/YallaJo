(function () {
    var input = document.getElementById('faqSearch');
    if (!input) return;
    var items = Array.prototype.slice.call(document.querySelectorAll('.js-faq-item'));
    var noResults = document.getElementById('faqNoResults');

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
})();
