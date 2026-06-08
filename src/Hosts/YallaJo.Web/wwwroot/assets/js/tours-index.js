(function () {
    // Enable Bootstrap tooltips (used by the disabled guest wishlist button).
    var tooltipTriggers = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggers.forEach(function (el) {
        if (window.bootstrap && bootstrap.Tooltip) new bootstrap.Tooltip(el);
    });

    // Client-side text filter over the loaded cards.
    var searchInput = document.getElementById('tourSearchInput');
    var cards = [].slice.call(document.querySelectorAll('.js-tour-card'));
    var noMatch = document.getElementById('noFilterMatch');

    function applyFilter() {
        var term = (searchInput && searchInput.value ? searchInput.value : '').trim().toLowerCase();
        var visible = 0;
        cards.forEach(function (card) {
            var title = card.getAttribute('data-title') || '';
            var show = term === '' || title.indexOf(term) !== -1;
            card.classList.toggle('d-none', !show);
            if (show) visible++;
        });
        if (noMatch) noMatch.classList.toggle('d-none', visible !== 0 || cards.length === 0);
    }
    if (searchInput) searchInput.addEventListener('input', applyFilter);

    // Category chips are a client-side affordance: the /tours list endpoint has no
    // category filter, so chips just highlight; selecting one routes to a filtered
    // search would require the search endpoint. For now they only toggle active state.
    var chips = [].slice.call(document.querySelectorAll('.js-category-chip'));
    chips.forEach(function (chip) {
        chip.addEventListener('click', function () {
            chips.forEach(function (c) { c.classList.remove('active', 'btn-primary'); c.classList.add('btn-light'); });
            chip.classList.add('active', 'btn-primary');
            chip.classList.remove('btn-light');
        });
    });

})();
