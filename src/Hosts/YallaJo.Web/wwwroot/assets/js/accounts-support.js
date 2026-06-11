/*
 * Accounts › Support thread (Phase 4d)
 * AJAX reply: posts via window.YallaJo.api and swaps the #supportThread region
 * with the refreshed partial. Falls back to native PRG when JS/api unavailable (PE1).
 */
(function () {
    "use strict";

    var api = window.YallaJo && window.YallaJo.api;
    if (!api) {
        return; // PE1: the reply form posts natively without JS.
    }

    var root = document.getElementById("supportThread");
    if (!root || root.dataset.yjInit === "1") {
        return;
    }
    root.dataset.yjInit = "1";

    function text(name, fallback) {
        return root.getAttribute(name) || fallback;
    }

    function scrollToEnd() {
        var list = root.querySelector("ul");
        if (list && list.lastElementChild) {
            list.lastElementChild.scrollIntoView({ behavior: "smooth", block: "nearest" });
        }
    }

    function swap(html) {
        var tpl = document.createElement("template");
        tpl.innerHTML = html.trim();
        root.replaceChildren.apply(root, Array.prototype.slice.call(tpl.content.childNodes));
    }

    root.addEventListener("submit", function (e) {
        var form = e.target;
        var action;
        var data;

        if (!form || !form.classList || !form.classList.contains("js-support-reply")) {
            return;
        }
        e.preventDefault();

        action = form.getAttribute("action");
        data = new FormData(form);

        api.postForm(action, data).then(function (res) {
            if (!res.ok) {
                return res.text().then(function (body) {
                    var message = text("data-failed-text", "Could not post your reply.");
                    var parsed;
                    try {
                        parsed = JSON.parse(body);
                        if (parsed && parsed.error) {
                            message = parsed.error;
                        }
                    } catch (ignore) { /* response was not JSON */ }
                    throw new Error(message);
                });
            }
            return res.text();
        }).then(function (html) {
            swap(html);
            if (window.YallaJo.toast) {
                window.YallaJo.toast(text("data-sent-text", "Your reply was sent."), "success");
            }
            scrollToEnd();
        }).catch(function (err) {
            if (window.YallaJo.formUx) {
                window.YallaJo.formUx.resetLoading(form);
            }
            if (window.YallaJo.toast) {
                window.YallaJo.toast((err && err.message) || text("data-failed-text", "Could not post your reply."), "error");
            }
        });
    });
}());
