/*
 * business-register.js
 * CSP-safe enhancements for the business registration form.
 * Satisfies: A6 / SEC1 / J3 (no inline scripts, no inline handlers),
 *            JS2 (declarative init via data attribute), JS4 (idempotent init),
 *            JS5 (all AJAX through window.YallaJo.api), JS6 (AbortController),
 *            J4 (300ms debounce), PE1 (pure progressive enhancement).
 *
 * 1) Place -> coordinates prefill: reads a non-executable JSON island
 *    (<script type="application/json" data-yj-component="business-register">)
 *    and, when the owner has not already entered their own coordinates, fills
 *    lat/lng from the chosen place.
 * 2) Place typeahead: upgrades #placeSelect with a search box backed by the
 *    Web proxy (data-lookup-url). The select stays in the DOM and remains the
 *    no-JS path; picking a result selects (or appends) the matching option.
 */
(function () {
    "use strict";

    var DEBOUNCE_MS = 300;

    function api() {
        return window.YallaJo && window.YallaJo.api ? window.YallaJo.api : null;
    }

    function initCoordsPrefill() {
        var root = document.querySelector('[data-yj-component="business-register"]');
        if (!root || root.dataset.yjInit === "1") {
            return;
        }
        root.dataset.yjInit = "1";

        var coords;
        try {
            coords = JSON.parse(root.textContent || "{}");
        } catch (e) {
            return;
        }

        var select = document.getElementById("placeSelect");
        var lat = document.getElementById("Form_Latitude");
        var lng = document.getElementById("Form_Longitude");
        if (!select || !lat || !lng) {
            return;
        }

        select.addEventListener("change", function () {
            var c = coords[select.value];
            if (!c) {
                return;
            }
            if (!lat.value || lat.value === "0") {
                lat.value = c.Lat;
            }
            if (!lng.value || lng.value === "0") {
                lng.value = c.Lng;
            }
        });
    }

    function initPlaceTypeahead() {
        var select = document.getElementById("placeSelect");
        if (!select || select.dataset.yjTypeahead === "1") {
            return;
        }
        var lookupUrl = select.dataset.lookupUrl;
        if (!lookupUrl || !api()) {
            return; // no-JS / no-foundation: plain select keeps working (PE1)
        }
        select.dataset.yjTypeahead = "1";

        var wrap = document.createElement("div");
        wrap.className = "position-relative mb-2";

        var search = document.createElement("input");
        search.type = "text";
        search.className = "form-control";
        search.id = "place-search";
        search.autocomplete = "off";
        search.placeholder = select.dataset.searchPlaceholder || "";
        search.setAttribute("role", "combobox");
        search.setAttribute("aria-expanded", "false");
        search.setAttribute("aria-autocomplete", "list");
        search.setAttribute("aria-controls", "place-search-listbox");
        if (select.getAttribute("aria-describedby")) {
            search.setAttribute("aria-describedby", select.getAttribute("aria-describedby"));
        }
        search.setAttribute("aria-label", select.dataset.searchLabel || search.placeholder);

        var listbox = document.createElement("ul");
        listbox.id = "place-search-listbox";
        listbox.className = "list-group position-absolute top-100 start-0 w-100 shadow z-3 d-none";
        listbox.setAttribute("role", "listbox");

        wrap.appendChild(search);
        wrap.appendChild(listbox);
        select.parentNode.insertBefore(wrap, select);

        var controller = null;
        var timer = null;
        var items = [];
        var active = -1;

        function close() {
            listbox.classList.add("d-none");
            listbox.innerHTML = "";
            search.setAttribute("aria-expanded", "false");
            search.removeAttribute("aria-activedescendant");
            items = [];
            active = -1;
        }

        function setActive(index) {
            var options = listbox.querySelectorAll('[role="option"]');
            options.forEach(function (el, i) {
                el.classList.toggle("active", i === index);
                el.setAttribute("aria-selected", i === index ? "true" : "false");
            });
            active = index;
            if (index >= 0 && options[index]) {
                search.setAttribute("aria-activedescendant", options[index].id);
                options[index].scrollIntoView({ block: "nearest" });
            } else {
                search.removeAttribute("aria-activedescendant");
            }
        }

        function pick(index) {
            var item = items[index];
            if (!item) {
                return;
            }
            var value = String(item.id);
            var option = select.querySelector('option[value="' + value + '"]');
            if (!option) {
                option = document.createElement("option");
                option.value = value;
                option.textContent = item.city ? item.name + " (" + item.city + ")" : item.name;
                select.appendChild(option);
            }
            select.value = value;
            select.dispatchEvent(new Event("change", { bubbles: true }));
            search.value = option.textContent;
            close();
        }

        function render(results) {
            items = results || [];
            listbox.innerHTML = "";
            if (!items.length) {
                var empty = document.createElement("li");
                empty.className = "list-group-item text-body-secondary small";
                empty.textContent = select.dataset.noResults || "";
                listbox.appendChild(empty);
            } else {
                items.forEach(function (item, i) {
                    var li = document.createElement("li");
                    li.className = "list-group-item list-group-item-action";
                    li.id = "place-search-option-" + i;
                    li.setAttribute("role", "option");
                    li.setAttribute("aria-selected", "false");
                    var name = document.createElement("span");
                    name.className = "fw-semibold";
                    name.textContent = item.name;
                    li.appendChild(name);
                    if (item.city) {
                        var city = document.createElement("small");
                        city.className = "text-body-secondary ms-2";
                        city.textContent = item.city;
                        li.appendChild(city);
                    }
                    li.addEventListener("mousedown", function (e) {
                        e.preventDefault();
                        pick(i);
                    });
                    listbox.appendChild(li);
                });
            }
            listbox.classList.remove("d-none");
            search.setAttribute("aria-expanded", "true");
            setActive(-1);
        }

        search.addEventListener("input", function () {
            var term = search.value.trim();
            if (timer) {
                clearTimeout(timer);
            }
            if (controller) {
                controller.abort();
                controller = null;
            }
            if (term.length < 2) {
                close();
                return;
            }
            timer = setTimeout(function () {
                controller = new AbortController();
                api().get(lookupUrl + (lookupUrl.indexOf("?") >= 0 ? "&" : "?") + "q=" + encodeURIComponent(term), { signal: controller.signal })
                    .then(render)
                    .catch(function (err) {
                        if (!err || !err.aborted) {
                            close();
                        }
                    });
            }, DEBOUNCE_MS);
        });

        search.addEventListener("keydown", function (e) {
            if (listbox.classList.contains("d-none")) {
                return;
            }
            if (e.key === "ArrowDown") {
                e.preventDefault();
                setActive(Math.min(active + 1, items.length - 1));
            } else if (e.key === "ArrowUp") {
                e.preventDefault();
                setActive(Math.max(active - 1, 0));
            } else if (e.key === "Enter") {
                if (active >= 0) {
                    e.preventDefault();
                    pick(active);
                }
            } else if (e.key === "Escape") {
                close();
            }
        });

        document.addEventListener("click", function (e) {
            if (!wrap.contains(e.target)) {
                close();
            }
        });
    }

    function init() {
        initCoordsPrefill();
        initPlaceTypeahead();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
