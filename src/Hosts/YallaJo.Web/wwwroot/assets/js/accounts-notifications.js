/*
 * Phase 4 (Accounts plan): notifications inbox AJAX.
 *  - "Load more" appends the next cursor page without a full reload (UI-UX-S1/L1).
 *  - "Mark all read" updates the UI optimistically then posts in the background (NF6).
 * Degrades gracefully: the link is a real <a href> and the form is a real POST (PE1).
 */
(function () {
    "use strict";

    var api = window.YallaJo && window.YallaJo.api;
    var root = document.getElementById("notificationsList");
    var markAll;
    if (!root || !api) {
        return;
    }

    // ---- Load more (append next page) ----
    root.addEventListener("click", function (e) {
        var link;
        var url;

        link = e.target.closest(".js-notifications-more");
        if (!link || !root.contains(link)) {
            return;
        }
        if (e.ctrlKey || e.metaKey || e.shiftKey) {
            return;
        }
        e.preventDefault();

        url = link.getAttribute("href");
        if (!url || link.dataset.yjBusy === "1") {
            return;
        }
        link.dataset.yjBusy = "1";
        link.classList.add("disabled");
        root.setAttribute("aria-busy", "true");

        api.loadPartial(url).then(function (html) {
            var tpl = document.createElement("template");
            var fetchedList;
            var list;
            var rows;
            var oldMore;
            var newMore;
            var i;

            tpl.innerHTML = html;
            fetchedList = tpl.content.querySelector(".js-notifications-items");
            list = root.querySelector(".js-notifications-items");
            if (fetchedList && list) {
                rows = fetchedList.querySelectorAll(".list-group-item");
                for (i = 0; i < rows.length; i++) {
                    list.appendChild(rows[i]);
                }
            }

            oldMore = root.querySelector(".js-notifications-loadmore");
            newMore = tpl.content.querySelector(".js-notifications-loadmore");
            if (oldMore) {
                if (newMore) {
                    oldMore.replaceWith(newMore);
                } else {
                    oldMore.remove();
                }
            }
            root.setAttribute("aria-busy", "false");
        }).catch(function (err) {
            link.dataset.yjBusy = "";
            link.classList.remove("disabled");
            root.setAttribute("aria-busy", "false");
            if (window.YallaJo.toast) {
                window.YallaJo.toast((err && err.message) || "", "error");
            }
        });
    });

    // ---- Mark all read (optimistic) ----
    markAll = document.querySelector(".js-notifications-markall");
    if (markAll) {
        markAll.addEventListener("submit", function (e) {
            var unread;
            var heading;
            var checks;
            var form;
            var badge;
            var i;

            e.preventDefault();
            if (markAll.dataset.yjBusy === "1") {
                return;
            }
            markAll.dataset.yjBusy = "1";

            // Optimistic UI: clear the unread styling immediately (NF6).
            unread = root.querySelectorAll(".list-group-item.bg-primary");
            for (i = 0; i < unread.length; i++) {
                unread[i].classList.remove("bg-primary", "bg-opacity-10");
                heading = unread[i].querySelector("h6.fw-bold");
                if (heading) {
                    heading.classList.remove("fw-bold");
                    heading.classList.add("text-body");
                }
            }
            checks = root.querySelectorAll(".list-group-item .bi-check2");
            for (i = 0; i < checks.length; i++) {
                form = checks[i].closest("form");
                if (form) {
                    form.remove();
                }
            }
            badge = document.querySelector(".card-header-title .badge.bg-danger");
            if (badge) {
                badge.remove();
            }

            api.postForm(markAll.getAttribute("action"), new FormData(markAll)).then(function (res) {
                if (!res.ok) {
                    throw new Error();
                }
                markAll.remove();
            }).catch(function () {
                markAll.dataset.yjBusy = "";
                if (window.YallaJo.toast) {
                    window.YallaJo.toast("", "error");
                }
                window.location.reload();
            });
        });
    }
})();
