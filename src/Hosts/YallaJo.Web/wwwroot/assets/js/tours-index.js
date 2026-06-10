// Tours index page behavior (Phase 4.4).
// - Tooltips, decorative category chips, client-side text filter (rebindable after AJAX swaps).
// - AJAX refinement of the results region per UI-UX-S1: SSR first paint, then
//   pagination / filter chips / sort / filter form submits fetch the _TourResults
//   partial via the shared api client (JS5), swap #tourResults, and pushState so
//   Back restores previous results. Full no-JS fallback: all links and forms are
//   real GETs (PE1).
(function () {
    "use strict";

    var results = document.querySelector('[data-yj-component="tour-results"]');
    var api = window.YallaJo && window.YallaJo.api;

    // ---- Static page chrome -------------------------------------------------

    function initTooltips() {
        if (!window.bootstrap || !window.bootstrap.Tooltip) {
            return;
        }
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
            window.bootstrap.Tooltip.getOrCreateInstance(el);
        });
    }

    // Decorative category chips: active-state toggle only.
    // NOTE(backend): /api/v1/tours/search has no categoryId parameter today.
    document.addEventListener("click", function (e) {
        var chip = e.target.closest(".js-category-chip");
        if (!chip) {
            return;
        }
        e.preventDefault();
        document.querySelectorAll(".js-category-chip").forEach(function (c) {
            c.classList.remove("active");
        });
        chip.classList.add("active");
    });

    // Client-side text filter over the cards on the current page.
    var searchInput = document.getElementById("tourSearchInput");
    function applyTextFilter() {
        if (!searchInput) {
            return;
        }
        var term = searchInput.value.trim().toLowerCase();
        var cards = document.querySelectorAll(".js-tour-card");
        var visible = 0;
        cards.forEach(function (card) {
            var match = !term || (card.dataset.title || "").indexOf(term) !== -1;
            card.classList.toggle("d-none", !match);
            if (match) {
                visible += 1;
            }
        });
        var noMatch = document.getElementById("noFilterMatch");
        if (noMatch) {
            noMatch.classList.toggle("d-none", visible !== 0 || cards.length === 0);
        }
    }
    if (searchInput) {
        searchInput.addEventListener("input", applyTextFilter);
    }

    // ---- AJAX results refinement (UI-UX-S1, L1/L4/L5 skeletons, NF2 errors) --

    if (!results || !api) {
        initTooltips();
        return;
    }

    var loadToken = 0;

    function skeleton() {
        // Skeleton cards mimic the grid layout (UI-UX-L1/L4), shown within 100ms (L5).
        var cols = "";
        var i;
        for (i = 0; i < 6; i += 1) {
            cols +=
                '<div class="col-md-6 col-xl-4">' +
                '<div class="card h-100" aria-hidden="true">' +
                '<div class="card-img-h220 bg-light placeholder-glow"><span class="placeholder w-100 h-100 d-block"></span></div>' +
                '<div class="card-body placeholder-glow">' +
                '<span class="placeholder col-8 mb-2"></span>' +
                '<span class="placeholder col-5 mb-3"></span>' +
                '<span class="placeholder col-4"></span>' +
                "</div></div></div>";
        }
        return '<div class="row g-4">' + cols + "</div>";
    }

    function showError(url) {
        var alert = document.createElement("div");
        alert.className = "alert alert-warning d-flex align-items-center justify-content-between gap-3";
        alert.setAttribute("role", "alert");
        var msg = document.createElement("span");
        msg.textContent = results.dataset.errorText || "Couldn't load results.";
        var retry = document.createElement("button");
        retry.type = "button";
        retry.className = "btn btn-sm btn-outline-secondary";
        retry.textContent = results.dataset.retryText || "Try again";
        retry.addEventListener("click", function () {
            loadResults(url, false);
        });
        alert.appendChild(msg);
        alert.appendChild(retry);
        results.innerHTML = "";
        results.appendChild(alert);
    }

    function rebind() {
        initTooltips();
        applyTextFilter();
    }

    function loadResults(url, push) {
        var token = ++loadToken;
        results.setAttribute("aria-busy", "true");
        results.innerHTML = skeleton();
        api.loadPartial(url)
            .then(function (html) {
                if (token !== loadToken) {
                    return;
                }
                results.innerHTML = html;
                results.setAttribute("aria-busy", "false");
                if (push) {
                    window.history.pushState({ yjTours: true }, "", url);
                }
                rebind();
                var top = results.getBoundingClientRect().top + window.pageYOffset - 80;
                window.scrollTo({ top: Math.max(top, 0), behavior: "smooth" });
            })
            .catch(function () {
                if (token !== loadToken) {
                    return;
                }
                results.setAttribute("aria-busy", "false");
                showError(url);
            });
    }

    // Pagination + filter-chip links inside the results region.
    results.addEventListener("click", function (e) {
        var link = e.target.closest("a[href]");
        if (!link || link.target === "_blank" || e.ctrlKey || e.metaKey || e.shiftKey) {
            return;
        }
        var inPagination = link.closest(".pagination");
        var isChip = link.classList.contains("js-filter-chip") || link.closest("#tourActiveFilters");
        if (!inPagination && !isChip) {
            return;
        }
        e.preventDefault();
        loadResults(link.href, true);
    });

    // Sort + filter forms: serialize to a GET URL (empty values dropped) and fetch.
    function interceptForm(form) {
        if (!form) {
            return;
        }
        form.addEventListener("submit", function (e) {
            e.preventDefault();
            var data = new FormData(form);
            var params = new URLSearchParams();
            data.forEach(function (value, key) {
                if (value !== null && String(value).trim() !== "") {
                    params.append(key, String(value));
                }
            });
            var qs = params.toString();
            var url = form.getAttribute("action") || window.location.pathname;
            url = qs ? url + "?" + qs : url;
            var offcanvasEl = form.closest(".offcanvas");
            var oc = null;
            if (offcanvasEl && window.bootstrap && window.bootstrap.Offcanvas) {
                oc = window.bootstrap.Offcanvas.getInstance(offcanvasEl);
                if (oc) {
                    oc.hide();
                }
            }
            loadResults(url, true);
        });
    }
    interceptForm(document.getElementById("sortForm"));
    interceptForm(document.getElementById("tourFiltersForm"));

    // Back/forward restores results (UI-UX-S1).
    window.addEventListener("popstate", function () {
        loadResults(window.location.href, false);
    });

    rebind();
})();
