/*
 * business-staff.js
 * Progressive enhancement for the "Add staff member" form (F10/D-4):
 * upgrades the raw UserId input into an accessible searchable combobox that
 * queries the Web lookup proxy (never the API host directly - JS5).
 * Satisfies: JS2 (data-yj-component init), JS4 (idempotent), JS5 (typed
 * apiClient only), JS6 (AbortController), J4 (300ms debounce), PE1 (without
 * JS the labeled UserId input keeps working), RTL3 (emails stay LTR via CSS
 * logical flow + bdi in results).
 */
(function () {
    "use strict";

    var DEBOUNCE_MS = 300;

    function api() {
        return window.YallaJo && window.YallaJo.api ? window.YallaJo.api : null;
    }

    function initPicker(root) {
        if (root.dataset.yjInit === "1") {
            return;
        }
        root.dataset.yjInit = "1";

        var lookupUrl = root.dataset.lookupUrl || "";
        var input = root.querySelector('input[name$="UserId"]');
        if (!lookupUrl || !input || !api()) {
            return; // PE1: leave the plain input untouched.
        }

        // Convert the server-rendered input into the hidden value holder.
        input.type = "hidden";

        // Build the visible combobox.
        var wrap = document.createElement("div");
        wrap.className = "position-relative";

        var search = document.createElement("input");
        search.type = "text";
        search.className = "form-control";
        search.id = "staff-user-search";
        search.autocomplete = "off";
        search.setAttribute("role", "combobox");
        search.setAttribute("aria-expanded", "false");
        search.setAttribute("aria-autocomplete", "list");
        search.setAttribute("aria-controls", "staff-user-listbox");
        search.placeholder = root.dataset.searchPlaceholder || "";

        var label = root.querySelector("label");
        if (label) {
            label.setAttribute("for", "staff-user-search");
        }
        var help = root.querySelector("#staff-user-help");
        if (help) {
            search.setAttribute("aria-describedby", "staff-user-help");
        }

        var list = document.createElement("ul");
        list.id = "staff-user-listbox";
        list.className = "list-group position-absolute top-100 start-0 w-100 shadow z-3 d-none";
        list.setAttribute("role", "listbox");

        wrap.appendChild(search);
        wrap.appendChild(list);
        input.insertAdjacentElement("beforebegin", wrap);

        var controller = null;
        var timer = null;
        var activeIndex = -1;

        function close() {
            list.classList.add("d-none");
            list.innerHTML = "";
            search.setAttribute("aria-expanded", "false");
            search.removeAttribute("aria-activedescendant");
            activeIndex = -1;
        }

        function options() {
            return Array.prototype.slice.call(list.querySelectorAll('[role="option"]'));
        }

        function setActive(index) {
            var opts = options();
            if (!opts.length) {
                return;
            }
            activeIndex = (index + opts.length) % opts.length;
            opts.forEach(function (o, i) {
                o.classList.toggle("active", i === activeIndex);
                o.setAttribute("aria-selected", i === activeIndex ? "true" : "false");
            });
            search.setAttribute("aria-activedescendant", opts[activeIndex].id);
        }

        function pick(opt) {
            if (!opt || !opt.dataset.userId) {
                return;
            }
            input.value = opt.dataset.userId;
            search.value = opt.dataset.label || "";
            close();
        }

        function render(items) {
            list.innerHTML = "";
            if (!items || !items.length) {
                var empty = document.createElement("li");
                empty.className = "list-group-item text-body-secondary small";
                empty.textContent = root.dataset.noResults || "";
                list.appendChild(empty);
            } else {
                items.forEach(function (u, i) {
                    var li = document.createElement("li");
                    li.id = "staff-user-option-" + i;
                    li.className = "list-group-item list-group-item-action";
                    li.setAttribute("role", "option");
                    li.setAttribute("aria-selected", "false");
                    li.dataset.userId = u.id;
                    li.dataset.label = u.displayName || u.email || u.id;
                    var name = document.createElement("span");
                    name.className = "fw-semibold d-block";
                    name.textContent = u.displayName || "";
                    li.appendChild(name);
                    if (u.email) {
                        var mail = document.createElement("small");
                        mail.className = "text-body-secondary";
                        var bdi = document.createElement("bdi");
                        bdi.setAttribute("dir", "ltr");
                        bdi.textContent = u.email;
                        mail.appendChild(bdi);
                        li.appendChild(mail);
                    }
                    list.appendChild(li);
                });
            }
            list.classList.remove("d-none");
            search.setAttribute("aria-expanded", "true");
            activeIndex = -1;
        }

        function query(term) {
            if (controller) {
                controller.abort();
            }
            controller = new AbortController();
            var url = lookupUrl + (lookupUrl.indexOf("?") >= 0 ? "&" : "?") + "q=" + encodeURIComponent(term);
            api().get(url, { signal: controller.signal })
                .then(function (items) { render(items || []); })
                .catch(function (err) {
                    if (!err || !err.aborted) {
                        close();
                    }
                });
        }

        search.addEventListener("input", function () {
            input.value = ""; // typing invalidates the previous selection
            var term = search.value.trim();
            if (timer) {
                clearTimeout(timer);
            }
            if (term.length < 2) {
                close();
                return;
            }
            timer = setTimeout(function () { query(term); }, DEBOUNCE_MS);
        });

        search.addEventListener("keydown", function (e) {
            if (list.classList.contains("d-none")) {
                return;
            }
            if (e.key === "ArrowDown") {
                e.preventDefault();
                setActive(activeIndex + 1);
            } else if (e.key === "ArrowUp") {
                e.preventDefault();
                setActive(activeIndex - 1);
            } else if (e.key === "Enter") {
                if (activeIndex >= 0) {
                    e.preventDefault();
                    pick(options()[activeIndex]);
                }
            } else if (e.key === "Escape") {
                close();
            }
        });

        list.addEventListener("mousedown", function (e) {
            var opt = e.target.closest('[role="option"]');
            if (opt) {
                e.preventDefault();
                pick(opt);
            }
        });

        document.addEventListener("click", function (e) {
            if (!wrap.contains(e.target)) {
                close();
            }
        });

        // Clear the search box after the AJAX add succeeds (form.reset()
        // resets the hidden input but not our injected search box value
        // because reset restores defaultValue - so hook the reset event).
        var form = root.closest("form");
        if (form) {
            form.addEventListener("reset", function () {
                search.value = "";
                close();
            });
        }
    }

    function init() {
        document.querySelectorAll('[data-yj-component="staff-picker"]').forEach(initPicker);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
