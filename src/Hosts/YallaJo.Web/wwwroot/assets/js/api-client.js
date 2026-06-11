// Shared AJAX client (rules: UI-JS JS5 — single apiClient wrapper; components never raw fetch).
// Exposed as window.YallaJo.api so classic page scripts can consume it without ES modules (JS1).
// Responsibilities: antiforgery header (SEC7), request timeout, 401 -> sign-in redirect with
// returnUrl, X-Requested-With marker consumed by BaseController.WantsAjax(), typed errors.
(function () {
    "use strict";

    var DEFAULT_TIMEOUT_MS = 10000;

    window.YallaJo = window.YallaJo || {};

    function antiForgeryToken() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : "";
    }

    function signInUrl() {
        var returnUrl = window.location.pathname + window.location.search;
        return "/auth/sign-in?returnUrl=" + encodeURIComponent(returnUrl);
    }

    function combineSignals(callerSignal, timeoutMs) {
        var timeoutSignal = typeof AbortSignal !== "undefined" && AbortSignal.timeout
            ? AbortSignal.timeout(timeoutMs)
            : null;
        if (callerSignal && timeoutSignal && AbortSignal.any) {
            return AbortSignal.any([callerSignal, timeoutSignal]);
        }
        return callerSignal || timeoutSignal || undefined;
    }

    /**
     * Core transport. Throws {status, message, aborted} on failure.
     * 401 redirects to sign-in (with returnUrl) unless options.redirectOn401 === false.
     */
    async function send(url, options) {
        options = options || {};
        var headers = Object.assign({ "X-Requested-With": "fetch" }, options.headers || {});
        var response;
        var message = "";
        var data = null;

        try {
            response = await fetch(url, {
                method: options.method || "GET",
                headers: headers,
                body: options.body,
                credentials: "same-origin",
                signal: combineSignals(options.signal, options.timeout || DEFAULT_TIMEOUT_MS)
            });
        } catch (e) {
            if (e && (e.name === "AbortError" || e.name === "TimeoutError")) {
                throw { status: 0, message: "", aborted: true };
            }
            throw { status: 0, message: "" };
        }

        if (response.status === 401 && options.redirectOn401 !== false) {
            window.location.href = signInUrl();
            throw { status: 401, message: "", handled: true };
        }

        if (!response.ok) {
            try {
                data = await response.json();
                if (data) { message = data.error || data.message || ""; }
            } catch (_) { /* non-JSON error body */ }
            // errors: optional field->messages dictionary (server validation), additive for callers.
            throw { status: response.status, message: message, errors: data && data.errors ? data.errors : null };
        }

        return response;
    }

    /** GET expecting JSON. Returns parsed body (null on 204). */
    async function apiGet(url, options) {
        options = options || {};
        options.method = "GET";
        options.headers = Object.assign({ "Accept": "application/json" }, options.headers || {});
        var response = await send(url, options);
        return response.status === 204 ? null : response.json();
    }

    /** POST (no body or JSON body) with antiforgery header. Returns parsed JSON (null on 204). */
    async function apiPost(url, options) {
        options = options || {};
        options.method = options.method || "POST";
        options.headers = Object.assign({
            "Accept": "application/json",
            "RequestVerificationToken": antiForgeryToken()
        }, options.headers || {});
        if (options.json !== undefined) {
            options.headers["Content-Type"] = "application/json";
            options.body = JSON.stringify(options.json);
        }
        var response = await send(url, options);
        if (response.status === 204) { return null; }
        var contentType = response.headers.get("Content-Type") || "";
        return contentType.indexOf("application/json") >= 0 ? response.json() : null;
    }

    /** POST a FormData (e.g. new FormData(form)) with antiforgery header. Returns the Response. */
    async function apiPostForm(url, formData, options) {
        options = options || {};
        options.method = "POST";
        options.body = formData;
        options.headers = Object.assign({
            "RequestVerificationToken": antiForgeryToken()
        }, options.headers || {});
        return send(url, options);
    }

    /** GET expecting an HTML partial (server returns PartialView when WantsAjax()). Returns markup string. */
    async function loadPartial(url, options) {
        options = options || {};
        options.method = "GET";
        options.headers = Object.assign({ "Accept": "text/html" }, options.headers || {});
        var response = await send(url, options);
        return response.text();
    }

    window.YallaJo.api = {
        antiForgeryToken: antiForgeryToken,
        send: send,
        get: apiGet,
        post: apiPost,
        postForm: apiPostForm,
        loadPartial: loadPartial
    };
})();
