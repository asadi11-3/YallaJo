// Provider guide picker ([Backend] B7 / F10).
// Progressive enhancement: wraps the raw GUID input with a search box + suggestion
// list backed by the Web proxy (JS5). Without JS the GUID input still posts (PE1).
(function () {
    "use strict";

    var input = document.querySelector("[data-yj-guide-picker]");
    if (!input || input.dataset.yjInit === "1") return;
    if (!window.YallaJo || !window.YallaJo.api) return; // PE1
    input.dataset.yjInit = "1";

    var lookupUrl = input.dataset.yjLookupUrl;
    if (!lookupUrl) return;

    var searchPlaceholder = input.dataset.yjSearchPlaceholder || "";
    var noResultsText = input.dataset.yjNoResults || "";

    // Hide the GUID input (it stays in the form and carries the posted value).
    input.readOnly = true;
    input.classList.add("visually-hidden");
    input.setAttribute("tabindex", "-1");

    // Build search box + suggestion list.
    var wrap = document.createElement("div");
    wrap.className = "position-relative";

    var search = document.createElement("input");
    search.type = "text";
    search.className = "form-control";
    search.placeholder = searchPlaceholder;
    search.autocomplete = "off";
    search.setAttribute("role", "combobox");
    search.setAttribute("aria-expanded", "false");
    search.setAttribute("aria-autocomplete", "list");

    var list = document.createElement("div");
    list.className = "list-group position-absolute w-100 shadow-sm d-none";
    // X6: dynamic stacking over the form while suggestions are open.
    list.style.zIndex = "1056";
    list.setAttribute("role", "listbox");

    input.parentNode.insertBefore(wrap, input);
    wrap.appendChild(search);
    wrap.appendChild(list);

    var debounceTimer = null;
    var controller = null;

    search.addEventListener("input", function () {
        var term = search.value.trim();
        input.value = ""; // selection invalidated by typing
        if (debounceTimer) clearTimeout(debounceTimer);
        if (term.length < 2) { hide(); return; }

        // J7 debounce + JS6 abort.
        debounceTimer = setTimeout(function () {
            if (controller) controller.abort();
            controller = new AbortController();
            var url = lookupUrl + (lookupUrl.indexOf("?") >= 0 ? "&" : "?") +
                "term=" + encodeURIComponent(term);
            window.YallaJo.api.get(url, { signal: controller.signal })
                .then(render)
                .catch(function () { /* best-effort */ });
        }, 300);
    });

    document.addEventListener("click", function (e) {
        if (!wrap.contains(e.target)) hide();
    });

    function render(items) {
        list.innerHTML = "";
        if (!Array.isArray(items) || items.length === 0) {
            var empty = document.createElement("div");
            empty.className = "list-group-item text-body-secondary small";
            empty.textContent = noResultsText;
            list.appendChild(empty);
        } else {
            items.forEach(function (g) {
                var btn = document.createElement("button");
                btn.type = "button";
                btn.className = "list-group-item list-group-item-action d-flex align-items-center gap-2";
                btn.setAttribute("role", "option");
                if (g.avatarUrl) {
                    var img = document.createElement("img");
                    img.src = g.avatarUrl;
                    img.alt = "";
                    img.width = 24;
                    img.height = 24;
                    img.className = "rounded-circle object-cover";
                    btn.appendChild(img);
                }
                var name = document.createElement("span");
                name.textContent = g.displayName;
                btn.appendChild(name);
                btn.addEventListener("click", function () {
                    input.value = g.userId;
                    search.value = g.displayName;
                    hide();
                });
                list.appendChild(btn);
            });
        }
        list.classList.remove("d-none");
        search.setAttribute("aria-expanded", "true");
    }

    function hide() {
        list.classList.add("d-none");
        search.setAttribute("aria-expanded", "false");
    }
})();
