// Admin typeahead lookup (F10): generic accessible combobox over MVC JSON proxies.
// Generalizes creator-tour-combobox.js to multiple roots (JS1 vanilla, JS2 declarative,
// JS4 idempotent, JS5 same-origin proxy only, JS6 stale-cancel, S2 debounce >= 300ms).
//
// Markup contract per root:
//   <div data-yj-component="lookup" data-lookup-url="/admin/lookups/users">
//     <input type="text" data-lookup-input ... role="combobox" aria-expanded="false">
//     <input type="hidden" data-lookup-id name="UserId">
//     <ul data-lookup-list role="listbox" hidden></ul>
//     <small data-lookup-empty hidden>No matches found.</small>
//   </div>
// Proxy responses: { items: [{ id, name }] } (max 10).
// No-JS fallback (PE1): the visible input posts as a *Query field the server resolves.
(function () {
    "use strict";

    if (window.YallaJo && window.YallaJo.adminLookup) {
        return; // JS4: idempotent — never double-bind.
    }

    window.YallaJo = window.YallaJo || {};
    window.YallaJo.adminLookup = true;

    function ready(fn) {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", fn, { once: true });
        } else {
            fn();
        }
    }

    function initLookup(root, index) {
        var input = root.querySelector("[data-lookup-input]");
        var hidden = root.querySelector("[data-lookup-id]");
        var list = root.querySelector("[data-lookup-list]");
        var empty = root.querySelector("[data-lookup-empty]");
        var lookupUrl = root.getAttribute("data-lookup-url") || "";
        var debounceTimer = null;
        var controller = null;
        var activeIndex = -1;
        var options = [];

        if (!input || !hidden || !list || !lookupUrl) {
            return;
        }

        function hideEmpty() {
            if (empty) {
                empty.hidden = true;
            }
        }

        function closeList() {
            list.replaceChildren();
            list.hidden = true;
            input.setAttribute("aria-expanded", "false");
            input.removeAttribute("aria-activedescendant");
            activeIndex = -1;
            options = [];
        }

        function setActive(i) {
            var j;
            for (j = 0; j < options.length; j += 1) {
                options[j].classList.toggle("active", j === i);
                options[j].setAttribute("aria-selected", j === i ? "true" : "false");
            }
            activeIndex = i;
            if (i >= 0 && options[i]) {
                input.setAttribute("aria-activedescendant", options[i].id);
            } else {
                input.removeAttribute("aria-activedescendant");
            }
        }

        function choose(i) {
            var opt = options[i];
            if (!opt) {
                return;
            }
            hidden.value = opt.getAttribute("data-id") || "";
            input.value = opt.getAttribute("data-name") || "";
            hideEmpty();
            closeList();
        }

        function render(items) {
            var k;
            var li;
            closeList();
            if (!items || items.length === 0) {
                if (empty) {
                    empty.hidden = false;
                }
                return;
            }
            hideEmpty();
            for (k = 0; k < items.length; k += 1) {
                li = document.createElement("li");
                li.id = "yjLookupOption-" + index + "-" + k;
                li.setAttribute("role", "option");
                li.setAttribute("aria-selected", "false");
                li.className = "list-group-item list-group-item-action";
                li.setAttribute("data-id", items[k].id || "");
                li.setAttribute("data-name", items[k].name || "");
                li.textContent = items[k].name || "";
                li.addEventListener("mousedown", function (e) {
                    e.preventDefault();
                    choose(Array.prototype.indexOf.call(list.children, e.currentTarget));
                });
                list.appendChild(li);
            }
            options = Array.prototype.slice.call(list.children);
            list.hidden = false;
            input.setAttribute("aria-expanded", "true");
        }

        function search(term) {
            var url;
            if (controller) {
                controller.abort(); // JS6: cancel stale request.
            }
            controller = new AbortController();
            url = lookupUrl + (lookupUrl.indexOf("?") >= 0 ? "&" : "?") + "q=" + encodeURIComponent(term);
            window.YallaJo.api
                .get(url, { signal: controller.signal })
                .then(function (data) {
                    render(data && data.items ? data.items : []);
                })
                .catch(function () {
                    // Aborted or failed lookups simply leave the list closed.
                });
        }

        input.addEventListener("input", function () {
            var term = input.value.trim();
            hidden.value = ""; // A fresh query invalidates any chosen id.
            window.clearTimeout(debounceTimer);
            if (term.length < 2) {
                closeList();
                hideEmpty();
                return;
            }
            debounceTimer = window.setTimeout(function () {
                search(term);
            }, 300); // S2: debounce >= 300ms.
        });

        input.addEventListener("keydown", function (e) {
            if (e.key === "ArrowDown" && !list.hidden) {
                e.preventDefault();
                setActive(activeIndex + 1 >= options.length ? 0 : activeIndex + 1);
            } else if (e.key === "ArrowUp" && !list.hidden) {
                e.preventDefault();
                setActive(activeIndex - 1 < 0 ? options.length - 1 : activeIndex - 1);
            } else if (e.key === "Enter" && activeIndex >= 0 && !list.hidden) {
                e.preventDefault();
                choose(activeIndex);
            } else if (e.key === "Escape") {
                closeList();
                hideEmpty();
            }
        });

        document.addEventListener("click", function (e) {
            if (!root.contains(e.target)) {
                closeList();
            }
        });
    }

    ready(function () {
        var roots = document.querySelectorAll('[data-yj-component="lookup"]');
        var i;
        if (!window.YallaJo.api || roots.length === 0) {
            return;
        }
        for (i = 0; i < roots.length; i += 1) {
            initLookup(roots[i], i);
        }
    });
})();
