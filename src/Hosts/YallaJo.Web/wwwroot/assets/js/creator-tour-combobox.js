// Creator → Article editor → "Related tours" async combobox.
// F10: resolves a tour by name to a hidden GUID so the page never exposes a raw
// entity-ID input. S2: 300ms debounce. JS6: AbortController cancels stale lookups.
// PE1: with JS disabled the visible input posts as `tourQuery` and the server
// resolves the name, so linking still works.
(function () {
    "use strict";

    if (window.yjTourComboboxInit) {
        return;
    }
    window.yjTourComboboxInit = true;

    function ready(fn) {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", fn, { once: true });
        } else {
            fn();
        }
    }

    ready(function () {
        var root = document.querySelector('[data-yj-component="tour-combobox"]');
        if (!root || !window.YallaJo || !window.YallaJo.api) {
            return;
        }

        var input = root.querySelector("#tourSearch");
        var hidden = root.querySelector("#tourId");
        var list = root.querySelector("#tourSearchList");
        var empty = root.querySelector("[data-tour-empty]");
        var lookupUrl = root.getAttribute("data-lookup-url") || "/creator/articles/tour-lookup";
        if (!input || !hidden || !list) {
            return;
        }

        var debounceTimer = null;
        var controller = null;
        var activeIndex = -1;
        var options = [];

        function closeList() {
            list.innerHTML = "";
            list.hidden = true;
            input.setAttribute("aria-expanded", "false");
            input.removeAttribute("aria-activedescendant");
            activeIndex = -1;
            options = [];
        }

        function setActive(index) {
            var i = 0;
            for (i = 0; i < options.length; i += 1) {
                if (i === index) {
                    options[i].classList.add("active");
                    options[i].setAttribute("aria-selected", "true");
                    input.setAttribute("aria-activedescendant", options[i].id);
                } else {
                    options[i].classList.remove("active");
                    options[i].setAttribute("aria-selected", "false");
                }
            }
            activeIndex = index;
        }

        function choose(index) {
            if (index < 0 || index >= options.length) {
                return;
            }
            var opt = options[index];
            hidden.value = opt.getAttribute("data-id") || "";
            input.value = opt.getAttribute("data-name") || "";
            closeList();
        }

        function render(items) {
            var i = 0;
            var li = null;
            list.innerHTML = "";
            options = [];
            if (!items || items.length === 0) {
                closeList();
                if (empty) {
                    empty.hidden = false;
                }
                return;
            }
            if (empty) {
                empty.hidden = true;
            }
            for (i = 0; i < items.length; i += 1) {
                li = document.createElement("li");
                li.id = "tourOption-" + i;
                li.className = "list-group-item list-group-item-action";
                li.setAttribute("role", "option");
                li.setAttribute("aria-selected", "false");
                li.setAttribute("data-id", items[i].id);
                li.setAttribute("data-name", items[i].name);
                li.textContent = items[i].name;
                li.addEventListener("mousedown", function (e) {
                    e.preventDefault();
                    var idx = options.indexOf(this);
                    choose(idx);
                });
                list.appendChild(li);
                options.push(li);
            }
            list.hidden = false;
            input.setAttribute("aria-expanded", "true");
            activeIndex = -1;
        }

        function search(term) {
            if (controller) {
                controller.abort();
            }
            controller = new AbortController();
            var url = lookupUrl + (lookupUrl.indexOf("?") >= 0 ? "&" : "?") + "q=" + encodeURIComponent(term);
            window.YallaJo.api.get(url, { signal: controller.signal })
                .then(function (data) {
                    render(data && data.items ? data.items : []);
                })
                .catch(function () {
                    // Aborted or failed lookups simply leave the list closed (PE1 fallback still works).
                });
        }

        input.addEventListener("input", function () {
            // Typing a fresh query invalidates any previously resolved GUID.
            hidden.value = "";
            var term = input.value.trim();
            if (debounceTimer) {
                window.clearTimeout(debounceTimer);
            }
            if (term.length < 2) {
                closeList();
                if (empty) {
                    empty.hidden = true;
                }
                return;
            }
            debounceTimer = window.setTimeout(function () {
                search(term);
            }, 300);
        });

        input.addEventListener("keydown", function (e) {
            if (list.hidden && (e.key === "ArrowDown" || e.key === "ArrowUp")) {
                return;
            }
            if (e.key === "ArrowDown") {
                e.preventDefault();
                setActive(activeIndex + 1 >= options.length ? 0 : activeIndex + 1);
            } else if (e.key === "ArrowUp") {
                e.preventDefault();
                setActive(activeIndex - 1 < 0 ? options.length - 1 : activeIndex - 1);
            } else if (e.key === "Enter") {
                if (activeIndex >= 0) {
                    e.preventDefault();
                    choose(activeIndex);
                }
            } else if (e.key === "Escape") {
                closeList();
            }
        });

        document.addEventListener("click", function (e) {
            if (!root.contains(e.target)) {
                closeList();
            }
        });
    });
})();
