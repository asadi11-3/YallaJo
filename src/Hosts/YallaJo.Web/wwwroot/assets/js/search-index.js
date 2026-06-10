// Search page behaviors. All HTTP goes through the shared api client (JS5).
(function () {
    var dataScript = document.getElementById('search-index-script');
    var data = dataScript ? dataScript.dataset : {};
    var listToggle = document.getElementById('searchListToggle');
    var mapToggle = document.getElementById('searchMapToggle');
    var locationButton = document.getElementById('searchUseLocation');
    var listRail = document.getElementById('searchListRail');
    var mapRail = document.getElementById('searchMapRail');
    var mapResults = document.getElementById('searchMapResults');
    var nearbyRail = document.getElementById('searchNearbyRail');
    var mapLoaded = false;
    var api = window.YallaJo.api;

    function escapeHtml(value) {
        return String(value || '').replace(/[<>&"]/g, function (c) { return '&#' + c.charCodeAt(0) + ';'; });
    }

    function setMode(mode) {
        var showMap = mode === 'map';
        listRail.classList.toggle('d-none', showMap);
        mapRail.classList.toggle('d-none', !showMap);
        listToggle.classList.toggle('btn-primary', !showMap);
        listToggle.classList.toggle('btn-outline-primary', showMap);
        mapToggle.classList.toggle('btn-primary', showMap);
        mapToggle.classList.toggle('btn-outline-primary', !showMap);
        listToggle.setAttribute('aria-pressed', String(!showMap));
        mapToggle.setAttribute('aria-pressed', String(showMap));
        if (showMap && !mapLoaded) { loadMap(); }
    }

    function loadBusinesses() {
        if (!listRail || !listRail.dataset.url) { return; }
        var url = listRail.dataset.url + '?pageSize=5';
        if (listRail.dataset.q) { url += '&q=' + encodeURIComponent(listRail.dataset.q); }
        api.get(url)
            .then(function (response) {
                var items = response && response.items ? response.items : [];
                if (!items.length) { listRail.textContent = data.noRelatedBusinesses || 'No related businesses found.'; return; }
                listRail.innerHTML = '<div class="fw-semibold mb-2">' + escapeHtml(data.relatedBusinesses || '') + '</div>' + items.map(function (it) {
                    var meta = [it.businessType, it.city].filter(Boolean).map(escapeHtml).join(' · ');
                    return '<a class="d-block text-decoration-none py-1" href="/businesses/' + encodeURIComponent(it.id) + '">' + escapeHtml(it.name) + (meta ? '<span class="text-secondary"> ' + meta + '</span>' : '') + '</a>';
                }).join('');
            })
            .catch(function () { listRail.textContent = data.couldNotLoadRelatedBusinesses || 'Could not load related businesses.'; });
    }

    function loadMap() {
        if (!mapRail || !mapRail.dataset.url || !mapResults) { return; }
        mapLoaded = true;
        mapResults.textContent = data.loadingMapPins || 'Loading map pins...';
        var qs = '?northLat=33.4&southLat=29.1&eastLng=39.4&westLng=34.8';
        api.get(mapRail.dataset.url + qs)
            .then(function (response) {
                var pins = response && response.pins ? response.pins : [];
                if (!pins.length) { mapResults.textContent = data.noPlacePins || 'No place pins in this viewport.'; return; }
                mapResults.innerHTML = pins.slice(0, 12).map(function (pin) {
                    return '<div class="py-1"><i class="bi bi-pin-map me-1" aria-hidden="true"></i>' + escapeHtml(pin.name) + '<span class="text-secondary"> ' + Number(pin.lat).toFixed(3) + ', ' + Number(pin.lng).toFixed(3) + '</span></div>';
                }).join('');
            })
            .catch(function () { mapResults.textContent = data.couldNotLoadMapPins || 'Could not load map pins.'; });
    }

    function loadNearby(lat, lng) {
        if (!nearbyRail || !nearbyRail.dataset.url) { return; }
        nearbyRail.textContent = data.findingNearby || 'Finding nearby places and businesses...';
        api.get(nearbyRail.dataset.url + '?lat=' + encodeURIComponent(lat) + '&lng=' + encodeURIComponent(lng) + '&radius=10&pageSize=6')
            .then(function (response) {
                var places = response && response.places ? response.places : [];
                var businesses = response && response.businesses ? response.businesses : [];
                var html = '<div class="fw-semibold mb-2">' + escapeHtml(data.nearYou || '') + '</div>';
                if (!places.length && !businesses.length) { nearbyRail.textContent = data.noNearbyResults || 'No nearby results found.'; return; }
                places.forEach(function (it) {
                    html += '<a class="d-block text-decoration-none py-1" href="/places/' + encodeURIComponent(it.slug) + '">Place ' + escapeHtml(it.name) + '<span class="text-secondary"> ' + Number(it.distanceKm).toFixed(1) + ' km</span></a>';
                });
                businesses.forEach(function (it) {
                    html += '<a class="d-block text-decoration-none py-1" href="/businesses/' + encodeURIComponent(it.id) + '">Business ' + escapeHtml(it.name) + '<span class="text-secondary"> ' + Number(it.distanceKm).toFixed(1) + ' km</span></a>';
                });
                nearbyRail.innerHTML = html;
            })
            .catch(function () { nearbyRail.textContent = data.couldNotLoadNearby || 'Could not load nearby results.'; });
    }

    if (listToggle && mapToggle && listRail && mapRail) {
        listToggle.addEventListener('click', function () { setMode('list'); });
        mapToggle.addEventListener('click', function () { setMode('map'); });
        loadBusinesses();
    }

    if (locationButton && navigator.geolocation) {
        locationButton.addEventListener('click', function () {
            navigator.geolocation.getCurrentPosition(function (pos) {
                loadNearby(pos.coords.latitude, pos.coords.longitude);
            }, function () { nearbyRail.textContent = data.locationNotGranted || 'Location access was not granted.'; });
        });
    } else if (locationButton) {
        locationButton.disabled = true;
    }
})();

// S2 autocomplete: debounce 300 ms; hits /search/suggest; renders as a list-group.
// Keeps an aria-expanded contract with the input + closes on outside click / Escape.
(function () {
    var input = document.getElementById('searchQuery');
    var box = document.getElementById('searchSuggestList');
    if (!input || !box) { return; }

    var t = null;
    var controller = null;

    function close() {
        box.classList.add('d-none');
        box.innerHTML = '';
        input.setAttribute('aria-expanded', 'false');
    }

    function render(items) {
        if (!items || items.length === 0) { close(); return; }
        var html = '';
        items.forEach(function (it) {
            var name = (it.name || '').replace(/[<>&"]/g, function (c) { return '&#' + c.charCodeAt(0) + ';'; });
            var slug = encodeURIComponent(it.slug || '');
            html += '<a class="list-group-item list-group-item-action" role="option" href="/tours/' + slug + '">' + name + '</a>';
        });
        box.innerHTML = html;
        box.classList.remove('d-none');
        input.setAttribute('aria-expanded', 'true');
    }

    input.addEventListener('input', function () {
        var q = input.value.trim();
        if (t) { clearTimeout(t); }
        if (controller) { controller.abort(); }
        if (q.length < 2) { close(); return; }
        t = setTimeout(function () {
            controller = new AbortController();
            window.YallaJo.api.get('/search/suggest?q=' + encodeURIComponent(q), { signal: controller.signal })
                .then(render)
                .catch(function () { /* aborted or network error — silently ignore */ });
        }, 300);
    });

    input.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') { close(); }
    });

    document.addEventListener('click', function (e) {
        if (!box.contains(e.target) && e.target !== input) { close(); }
    });
})();
