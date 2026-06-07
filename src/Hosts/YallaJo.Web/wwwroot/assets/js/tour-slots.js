/*
 * tour-slots.js — live tour availability-slot capacity over SignalR.
 *
 * Binding rules (UI-UX-Design.md):
 *   S1  WebSockets + LongPolling (SSE skipped — server restricts transports).
 *   S2  Connects ONLY on the public tour-detail page, group tour:{tourId}.
 *   S3  Server sends IDs only ({ slotId, remainingCapacity }).
 *   S4  withAutomaticReconnect([0,2000,10000,30000,60000]); after exhaustion -> manual reconnect toast (PE2).
 *   X11 One hub connection per page.
 *   X12 No setInterval polling while connected (polling is the PE2 fallback only, when the hub is DOWN).
 *   CAL3/RT1  SlotCapacityChanged -> update the slot count; flash/animate UNLESS prefers-reduced-motion (M3).
 *   PE2 Degrade to a manual "Refresh availability" action when the hub cannot connect.
 *   §1  No JWT in JS — this is the public anonymous tour hub; it carries only the tourId.
 *
 * Activation: a root element with data-yj-component="tour-slots" data-tour-id="{guid}" data-hub-url="{apiOrigin}".
 * Each slot row carries data-slot-id="{guid}" and a [data-remaining] element to update.
 */
(function () {
    "use strict";

    var root = document.querySelector('[data-yj-component="tour-slots"]');
    if (!root || root.dataset.yjInit === "1") {
        return; // JS4 idempotent init
    }
    if (typeof window.signalR === "undefined") {
        return; // library failed to load — leave SSR markup as the baseline (PE3)
    }
    root.dataset.yjInit = "1";

    var tourId = root.getAttribute("data-tour-id");
    var hubBase = (root.getAttribute("data-hub-url") || "").replace(/\/+$/, "");
    if (!tourId) {
        return;
    }

    var hubUrl = hubBase + "/hubs/tour?tourId=" + encodeURIComponent(tourId);
    var reduceMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var failedReconnects = 0;
    // PE2 fallback poller. Runs ONLY while the hub is DOWN (X12: never alongside a live connection).
    var fallbackPollTimer = null;
    var FALLBACK_POLL_MS = 60000; // gentle 60s refresh while disconnected

    function stopFallbackPoll() {
        if (fallbackPollTimer) {
            clearInterval(fallbackPollTimer);
            fallbackPollTimer = null;
        }
    }

    // PE2: when the hub is unreachable, degrade to a polled refresh of the page's
    // availability (Web-origin reload of the current tour page — never a direct API call from JS).
    function startFallbackPoll() {
        if (fallbackPollTimer) {
            return;
        }
        fallbackPollTimer = setInterval(function () {
            // Only the booking sidebar is reloaded conceptually; the safe, dependency-free
            // degradation is a soft reload of the current Web page (cookie-authed, SSR-fresh).
            if (document.visibilityState === "visible") {
                window.location.reload();
            }
        }, FALLBACK_POLL_MS);
    }

    var connection = new window.signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
            // S1: WebSockets preferred, LongPolling fallback. SSE intentionally omitted.
            transport: window.signalR.HttpTransportType.WebSockets | window.signalR.HttpTransportType.LongPolling
        })
        .withAutomaticReconnect([0, 2000, 10000, 30000, 60000]) // S4
        .configureLogging(window.signalR.LogLevel.Warning)
        .build();

    function setStatus(state) {
        root.setAttribute("data-hub-state", state);
    }

    // CAL3/RT1: apply the new remaining capacity to the matching slot, with a suppressible flash (M3).
    function applyCapacity(slotId, remaining) {
        var slotEl = root.querySelector('[data-slot-id="' + cssEscape(slotId) + '"]');
        if (!slotEl) {
            return;
        }
        var target = slotEl.querySelector("[data-remaining]") || slotEl;
        var label = remaining <= 0 ? (root.getAttribute("data-full-label") || "Full") : String(remaining);
        if (target.textContent === label) {
            return; // no-op
        }
        target.textContent = label;
        slotEl.setAttribute("data-remaining-count", String(remaining));
        if (remaining <= 0) {
            slotEl.setAttribute("data-soldout", "1");
        } else {
            slotEl.removeAttribute("data-soldout");
        }

        if (!reduceMotion) {
            // animate transform/opacity only (M2). The .yj-slot-flash class is a 250ms opacity/transform pulse.
            slotEl.classList.remove("yj-slot-flash");
            // force reflow so the animation restarts
            void slotEl.offsetWidth;
            slotEl.classList.add("yj-slot-flash");
        }

        // notify any other widget (e.g. the booking stepper) that wants to react
        root.dispatchEvent(new CustomEvent("yj:slot-capacity-changed", {
            bubbles: true,
            detail: { slotId: slotId, remainingCapacity: remaining }
        }));
    }

    // Minimal CSS.escape polyfill for attribute selectors (GUIDs are safe, but be defensive).
    function cssEscape(value) {
        if (window.CSS && window.CSS.escape) {
            return window.CSS.escape(value);
        }
        return String(value).replace(/[^a-zA-Z0-9_-]/g, "\\$&");
    }

    connection.on("SlotCapacityChanged", function (payload) {
        if (!payload) {
            return;
        }
        applyCapacity(payload.slotId, payload.remainingCapacity);
    });

    connection.onreconnecting(function () {
        setStatus("reconnecting");
    });

    connection.onreconnected(function () {
        failedReconnects = 0;
        setStatus("connected");
        stopFallbackPoll(); // X12: no polling while the hub is live
        var bar = document.getElementById("yj-slots-fallback");
        if (bar) { bar.remove(); }
        // re-join the group after a reconnect (server also auto-joins from ?tourId=, this is belt-and-braces)
        connection.invoke("JoinTour", tourId).catch(function () { /* ignore */ });
    });

    connection.onclose(function () {
        setStatus("closed");
        // S4 / PE2: automatic reconnect exhausted -> offer a manual reconnect AND start the polled fallback.
        failedReconnects += 1;
        showReconnectFallback();
        startFallbackPoll(); // PE2: degrade to polled refresh while the hub is down
    });

    function start() {
        setStatus("connecting");
        connection.start()
            .then(function () {
                failedReconnects = 0;
                setStatus("connected");
                stopFallbackPoll(); // X12: never poll while connected
                connection.invoke("JoinTour", tourId).catch(function () { /* server already auto-joined */ });
            })
            .catch(function () {
                failedReconnects += 1;
                if (failedReconnects >= 5) {
                    showReconnectFallback(); // PE2
                    startFallbackPoll();
                } else {
                    setTimeout(start, 2000);
                }
            });
    }

    // PE2: surface a non-blocking "live updates paused" notice with a manual reconnect + refresh action.
    // X12: we do NOT start any setInterval while the hub is connected; this only runs when it's down.
    function showReconnectFallback() {
        var existing = document.getElementById("yj-slots-fallback");
        if (existing) {
            return;
        }
        var bar = document.createElement("div");
        bar.id = "yj-slots-fallback";
        bar.setAttribute("role", "status");
        bar.setAttribute("aria-live", "polite");
        bar.className = "yj-slots-fallback toast align-items-center";
        bar.innerHTML =
            '<span class="yj-slots-fallback__msg">' +
            (root.getAttribute("data-offline-label") || "Live availability paused.") +
            '</span> <button type="button" class="btn btn-sm btn-link yj-slots-reconnect">' +
            (root.getAttribute("data-reconnect-label") || "Reconnect") + "</button>";
        root.appendChild(bar);
        bar.querySelector(".yj-slots-reconnect").addEventListener("click", function () {
            bar.remove();
            failedReconnects = 0;
            stopFallbackPoll();
            start();
        });
    }

    // X11: single connection; start on DOMContentLoaded (or now if already parsed).
    start();

    // Clean up on navigation: stop the connection and any fallback poller.
    window.addEventListener("pagehide", function () {
        stopFallbackPoll();
        try { connection.stop(); } catch (e) { /* ignore */ }
    });
})();
