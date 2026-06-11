/*
 * creator-article-editor.js — Content Creator blog editor enhancements (plan §7.2).
 * CSP-safe: external file, IIFE, no inline handlers, no eval.
 *   J1  — Quill rich-text editor on the Content field (graceful fallback to <textarea>).
 *   F5  — autosave every 30s to localStorage + recovery banner on next visit
 *         (localStorage is the source of truth; no server drafts API is required).
 *   F9  — beforeunload warning whenever any tracked field is dirty (suppressed on submit).
 */
(function () {
    "use strict";

    var form = document.getElementById("articleEditorForm");
    if (!form) {
        return;
    }

    var backing = form.querySelector("[data-quill-backing]");
    var host = document.getElementById("contentEditor");
    var articleId = form.getAttribute("data-article-id") || "new";
    var storageKey = "yj.article.draft." + articleId;
    var AUTOSAVE_MS = 30000;

    // Fields tracked for autosave + dirty detection. Content is handled separately
    // (it lives in Quill when available, otherwise in the backing textarea).
    var fieldNames = ["Title", "Slug", "SourceLanguageCode", "Summary", "Content", "MetaTitle", "MetaDescription"];

    function fieldEl(name) {
        return form.querySelector("[name='" + name + "']");
    }

    var quill = null;
    var submitting = false;

    // ── J1: Quill rich-text ────────────────────────────────────────────────
    function initQuill() {
        if (typeof window.Quill === "undefined" || !host || !backing) {
            // Fallback: no Quill — keep the plain textarea visible and editable.
            if (host) {
                host.style.display = "none";
            }
            if (backing) {
                backing.classList.remove("d-none");
            }
            return;
        }

        // Hide the raw textarea; Quill becomes the visible editor, textarea stays as the posted value.
        backing.classList.add("d-none");

        quill = new window.Quill(host, {
            theme: "snow",
            modules: {
                toolbar: [
                    [{ header: [2, 3, 4, false] }],
                    ["bold", "italic", "underline", "strike"],
                    ["blockquote", "code-block"],
                    [{ list: "ordered" }, { list: "bullet" }],
                    ["link"],
                    ["clean"]
                ]
            }
        });

        // Seed Quill from the backing field's current HTML.
        if (backing.value) {
            quill.clipboard.dangerouslyPasteHTML(backing.value);
        }

        // Keep the backing textarea in sync so the posted Content is the Quill HTML.
        quill.on("text-change", function () {
            backing.value = quill.root.innerHTML;
        });
    }

    function syncContent() {
        if (quill && backing) {
            backing.value = quill.root.innerHTML;
        }
    }

    // ── F5: autosave to localStorage + recovery banner ─────────────────────
    function hasStorage() {
        var k = "__yj_test__";
        try {
            window.localStorage.setItem(k, "1");
            window.localStorage.removeItem(k);
            return true;
        } catch (e) {
            return false;
        }
    }

    var storageOk = hasStorage();

    function snapshot() {
        syncContent();
        var data = { savedAt: new Date().toISOString() };
        fieldNames.forEach(function (n) {
            var el = fieldEl(n);
            if (el) {
                data[n] = el.value;
            }
        });
        return data;
    }

    function saveDraft() {
        if (!storageOk) {
            return;
        }
        try {
            window.localStorage.setItem(storageKey, JSON.stringify(snapshot()));
        } catch (e) {
            /* quota / disabled — non-blocking */
        }
    }

    function clearDraft() {
        if (!storageOk) {
            return;
        }
        try {
            window.localStorage.removeItem(storageKey);
        } catch (e) { /* ignore */ }
    }

    function applyDraft(data) {
        fieldNames.forEach(function (n) {
            if (typeof data[n] === "undefined") {
                return;
            }
            var el = fieldEl(n);
            if (el) {
                el.value = data[n];
            }
        });
        if (quill && typeof data.Content === "string") {
            quill.root.innerHTML = data.Content;
        }
        markPristine();
    }

    function showRecoveryBanner(data) {
        var banner = document.createElement("div");
        banner.className = "alert alert-warning d-flex justify-content-between align-items-center";
        banner.setAttribute("role", "alert");

        var when = "";
        try {
            when = data.savedAt ? new Date(data.savedAt).toLocaleString() : "";
        } catch (e) { /* ignore */ }

        var msg = document.createElement("span");
        msg.textContent = "An unsaved draft from " + (when || "a previous session") + " was found.";

        var actions = document.createElement("span");
        var restore = document.createElement("button");
        restore.type = "button";
        restore.className = "btn btn-sm btn-warning me-2";
        restore.textContent = "Restore draft";
        var discard = document.createElement("button");
        discard.type = "button";
        discard.className = "btn btn-sm btn-secondary-soft";
        discard.textContent = "Discard";

        restore.addEventListener("click", function () {
            applyDraft(data);
            banner.remove();
        });
        discard.addEventListener("click", function () {
            clearDraft();
            banner.remove();
        });

        actions.appendChild(restore);
        actions.appendChild(discard);
        banner.appendChild(msg);
        banner.appendChild(actions);
        form.parentNode.insertBefore(banner, form);
    }

    function maybeOfferRecovery() {
        if (!storageOk) {
            return;
        }
        var raw = null;
        try {
            raw = window.localStorage.getItem(storageKey);
        } catch (e) {
            return;
        }
        if (!raw) {
            return;
        }
        var data = null;
        try {
            data = JSON.parse(raw);
            if (data && typeof data === "object") {
                showRecoveryBanner(data);
            }
        } catch (e) {
            clearDraft();
        }
    }

    // ── F9: beforeunload when dirty ────────────────────────────────────────
    var initialState = "";

    function currentState() {
        syncContent();
        return fieldNames.map(function (n) {
            var el = fieldEl(n);
            return el ? el.value : "";
        }).join("\u0001");
    }

    function markPristine() {
        initialState = currentState();
    }

    function isDirty() {
        return currentState() !== initialState;
    }

    function onBeforeUnload(e) {
        if (submitting || !isDirty()) {
            return undefined;
        }
        e.preventDefault();
        e.returnValue = "";
        return "";
    }

    // ── wire-up ────────────────────────────────────────────────────────────
    initQuill();
    markPristine();
    maybeOfferRecovery();

    var timer = window.setInterval(saveDraft, AUTOSAVE_MS);

    form.addEventListener("submit", function () {
        submitting = true;
        syncContent();
        window.clearInterval(timer);
        window.removeEventListener("beforeunload", onBeforeUnload);
        // Successful PRG navigates away; clear the local draft so it can't shadow the saved version.
        clearDraft();
    });

    window.addEventListener("beforeunload", onBeforeUnload);
})();
