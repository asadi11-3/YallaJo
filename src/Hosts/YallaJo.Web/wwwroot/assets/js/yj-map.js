// yj-map.js — progressive Mapbox GL bootstrapper (V13 maps).
// Scans for <div data-yj-map="pin|pins|route|picker"> containers rendered by the
// shared _Map partial. The vendor css/js (self-hosted under
// assets/vendor/mapbox-gl/) loads only when the first container nears the
// viewport, keeping pages without visible maps free of the ~500 KB payload.
// RTL: nav controls flip to the start side and Arabic tile labels are applied
// via the name_ar coalesce when the document language is Arabic.
(function () {
    "use strict";

    var docEl = document.documentElement;
    var isRtl = (docEl.getAttribute("dir") || "").toLowerCase() === "rtl";
    var lang = (docEl.getAttribute("lang") || "en").split("-")[0].toLowerCase();

    function isDark() {
        return docEl.getAttribute("data-bs-theme") === "dark"
            || document.body.getAttribute("data-bs-theme") === "dark";
    }

    var vendorPromise = null;
    function ensureVendor(base) {
        if (vendorPromise) { return vendorPromise; }
        vendorPromise = new Promise(function (resolve, reject) {
            var css = document.createElement("link");
            css.rel = "stylesheet";
            css.href = base + "mapbox-gl.css";
            document.head.appendChild(css);

            var s = document.createElement("script");
            s.src = base + "mapbox-gl.js";
            s.onload = function () { resolve(window.mapboxgl); };
            s.onerror = function () { reject(new Error("mapbox-gl failed to load")); };
            document.head.appendChild(s);
        });
        return vendorPromise;
    }

    var rtlConfigured = false;
    function setupRtl(mapboxgl, base) {
        if (rtlConfigured || typeof mapboxgl.setRTLTextPlugin !== "function") { return; }
        rtlConfigured = true;
        try {
            // Lazy: the plugin only downloads when Arabic/Hebrew glyphs appear.
            mapboxgl.setRTLTextPlugin(base + "mapbox-gl-rtl-text.js", null, true);
        } catch (e) { /* plugin already registered */ }
    }

    // Brand marker: Jordan-red map pin with a white core (matches the logo pin).
    function brandMarkerEl() {
        var el = document.createElement("div");
        el.className = "yj-map-marker";
        el.innerHTML =
            '<svg viewBox="0 0 24 24" width="34" height="34" aria-hidden="true" focusable="false">' +
            '<path d="M12 1C7 1 3.2 4.9 3.2 9.8c0 5 4.4 7.7 7.2 12.1.7 1.1 2.5 1.1 3.2 0 2.8-4.4 7.2-7.1 7.2-12.1C20.8 4.9 17 1 12 1Z" fill="#b02a2a"/>' +
            '<circle cx="12" cy="9.8" r="4.1" fill="#fff"/>' +
            "</svg>";
        return el;
    }

    function stopMarkerEl(text, isMeeting) {
        var el = document.createElement("div");
        el.className = "yj-map-stop" + (isMeeting ? " yj-map-stop-meeting" : "");
        el.textContent = text;
        return el;
    }

    // Arabic map labels: prefer the localized name field on every symbol layer.
    function localizeLabels(map) {
        var layers;
        if (lang !== "ar") { return; }
        try {
            layers = (map.getStyle() && map.getStyle().layers) || [];
            layers.forEach(function (layer) {
                if (layer.type !== "symbol") { return; }
                var tf = map.getLayoutProperty(layer.id, "text-field");
                if (tf && JSON.stringify(tf).indexOf("name") !== -1) {
                    map.setLayoutProperty(layer.id, "text-field",
                        ["coalesce", ["get", "name_ar"], ["get", "name"]]);
                }
            });
        } catch (e) { /* style without name fields — ignore */ }
    }

    function popupHtml(label, url) {
        var safe = document.createElement("div");
        safe.textContent = label || "";
        var inner = "<strong>" + safe.innerHTML + "</strong>";
        if (url) { inner = '<a class="yj-map-popup-link" href="' + url + '">' + inner + "</a>"; }
        return inner;
    }

    function parseMarkers(el) {
        var raw = el.getAttribute("data-markers");
        if (!raw) { return []; }
        try { return JSON.parse(raw) || []; } catch (e) { return []; }
    }

    function initPin(mapboxgl, map, el, lngLat) {
        var marker = new mapboxgl.Marker({ element: brandMarkerEl(), anchor: "bottom" })
            .setLngLat(lngLat)
            .addTo(map);
        var label = el.getAttribute("data-label");
        if (label) {
            marker.setPopup(new mapboxgl.Popup({ offset: 30, closeButton: false, className: "yj-map-popup" })
                .setHTML(popupHtml(label)));
        }
    }

    function initPins(mapboxgl, map, el) {
        var items = parseMarkers(el).filter(function (m) {
            return isFinite(parseFloat(m.lat)) && isFinite(parseFloat(m.lng));
        });
        if (!items.length) { return; }
        var bounds = new mapboxgl.LngLatBounds();
        items.forEach(function (m) {
            var lngLat = [parseFloat(m.lng), parseFloat(m.lat)];
            var marker = new mapboxgl.Marker({ element: brandMarkerEl(), anchor: "bottom" })
                .setLngLat(lngLat)
                .addTo(map);
            if (m.label) {
                marker.setPopup(new mapboxgl.Popup({ offset: 30, closeButton: false, className: "yj-map-popup" })
                    .setHTML(popupHtml(m.label, m.url)));
            }
            bounds.extend(lngLat);
        });
        if (items.length === 1) {
            map.jumpTo({ center: bounds.getCenter(), zoom: 13 });
        } else {
            map.fitBounds(bounds, { padding: 48, maxZoom: 14, duration: 0 });
        }
    }

    function initRoute(mapboxgl, map, el) {
        var items = parseMarkers(el).filter(function (m) {
            return isFinite(parseFloat(m.lat)) && isFinite(parseFloat(m.lng));
        });
        if (!items.length) { return; }
        var bounds = new mapboxgl.LngLatBounds();
        var lineCoords = [];
        var stopNo = 0;
        items.forEach(function (m) {
            var lngLat = [parseFloat(m.lng), parseFloat(m.lat)];
            var isMeeting = m.type === "meeting";
            var markerEl;
            if (isMeeting) {
                markerEl = stopMarkerEl("\u2691", true); // flag glyph for the meeting point
            } else {
                stopNo += 1;
                markerEl = stopMarkerEl(String(stopNo), false);
                lineCoords.push(lngLat);
            }
            var marker = new mapboxgl.Marker({ element: markerEl })
                .setLngLat(lngLat)
                .addTo(map);
            if (m.label) {
                marker.setPopup(new mapboxgl.Popup({ offset: 18, closeButton: false, className: "yj-map-popup" })
                    .setHTML(popupHtml(m.label)));
            }
            bounds.extend(lngLat);
        });
        if (lineCoords.length >= 2) {
            map.on("load", function () {
                map.addSource("yj-route", {
                    type: "geojson",
                    data: { type: "Feature", properties: {}, geometry: { type: "LineString", coordinates: lineCoords } }
                });
                map.addLayer({
                    id: "yj-route-line",
                    type: "line",
                    source: "yj-route",
                    layout: { "line-cap": "round", "line-join": "round" },
                    paint: { "line-color": "#b02a2a", "line-width": 3, "line-dasharray": [2, 1.5], "line-opacity": 0.8 }
                });
            });
        }
        map.fitBounds(bounds, { padding: 56, maxZoom: 14, duration: 0 });
    }

    function initPicker(mapboxgl, map, el, initialLngLat) {
        var latInput = document.querySelector(el.getAttribute("data-input-lat") || "");
        var lngInput = document.querySelector(el.getAttribute("data-input-lng") || "");
        var marker = null;

        function writeInputs(lngLat) {
            if (latInput) {
                latInput.value = lngLat.lat.toFixed(6);
                latInput.dispatchEvent(new Event("input", { bubbles: true }));
            }
            if (lngInput) {
                lngInput.value = lngLat.lng.toFixed(6);
                lngInput.dispatchEvent(new Event("input", { bubbles: true }));
            }
        }

        function placeMarker(lngLat, write) {
            if (!marker) {
                marker = new mapboxgl.Marker({ element: brandMarkerEl(), anchor: "bottom", draggable: true })
                    .setLngLat(lngLat)
                    .addTo(map);
                marker.on("dragend", function () { writeInputs(marker.getLngLat()); });
            } else {
                marker.setLngLat(lngLat);
            }
            if (write) { writeInputs(marker.getLngLat()); }
        }

        if (initialLngLat) { placeMarker(initialLngLat, false); }

        map.on("click", function (e) {
            placeMarker(e.lngLat, true);
        });

        function onInputChange() {
            var lat = parseFloat(latInput ? latInput.value : "");
            var lng = parseFloat(lngInput ? lngInput.value : "");
            if (!isFinite(lat) || !isFinite(lng)) { return; }
            placeMarker({ lng: lng, lat: lat }, false);
            map.easeTo({ center: [lng, lat], zoom: Math.max(map.getZoom(), 12) });
        }
        if (latInput) { latInput.addEventListener("change", onInputChange); }
        if (lngInput) { lngInput.addEventListener("change", onInputChange); }
    }

    function initMap(el) {
        var base = el.getAttribute("data-vendor") || "/assets/vendor/mapbox-gl/";
        ensureVendor(base).then(function (mapboxgl) {
            if (!mapboxgl) { return; }
            mapboxgl.accessToken = el.getAttribute("data-token") || "";
            setupRtl(mapboxgl, base);

            var mode = el.getAttribute("data-yj-map") || "pin";
            var lat = parseFloat(el.getAttribute("data-lat"));
            var lng = parseFloat(el.getAttribute("data-lng"));
            var zoom = parseFloat(el.getAttribute("data-zoom"));
            var hasCenter = isFinite(lat) && isFinite(lng);
            // Jordan-wide fallback view (Amman) when no coordinates are supplied.
            var center = hasCenter ? [lng, lat] : [35.9106, 31.9539];

            var map = new mapboxgl.Map({
                container: el,
                style: isDark() ? "mapbox://styles/mapbox/dark-v11" : "mapbox://styles/mapbox/streets-v12",
                center: center,
                zoom: isFinite(zoom) && hasCenter ? zoom : (hasCenter ? 13 : 7),
                cooperativeGestures: true // ctrl/cmd + scroll to zoom — keeps page scrolling pleasant
            });
            map.addControl(new mapboxgl.NavigationControl({ showCompass: false }),
                isRtl ? "top-left" : "top-right");
            map.on("style.load", function () { localizeLabels(map); });

            if (mode === "pin" && hasCenter) {
                initPin(mapboxgl, map, el, [lng, lat]);
            } else if (mode === "pins") {
                initPins(mapboxgl, map, el);
            } else if (mode === "route") {
                initRoute(mapboxgl, map, el);
            } else if (mode === "picker") {
                initPicker(mapboxgl, map, el, hasCenter ? [lng, lat] : null);
            }

            el.classList.add("yj-map-ready");
        }).catch(function () {
            el.classList.add("d-none"); // vendor failed — hide the empty box, text fallback remains
        });
    }

    var io = null;
    if ("IntersectionObserver" in window) {
        io = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    io.unobserve(entry.target);
                    initMap(entry.target);
                }
            });
        }, { rootMargin: "200px" });
    }

    // Scans the DOM for not-yet-booted containers. Re-callable after AJAX swaps
    // (e.g. listing.js calls window.YallaJo.maps.scan() after replacing results).
    function scan() {
        var found = Array.prototype.slice.call(document.querySelectorAll("[data-yj-map]"));
        found.forEach(function (el) {
            if (el.dataset.yjMapInit === "1") { return; }
            el.dataset.yjMapInit = "1";
            if (io) { io.observe(el); } else { initMap(el); }
        });
    }

    window.YallaJo = window.YallaJo || {};
    window.YallaJo.maps = { scan: scan };
    scan();
})();
