/*
 * Waypoint drag-handle reorder (NF6, Phase 5).
 * Vanilla pointer events — no new dependency. Optimistically moves the row,
 * POSTs the new order to the existing Reorder action via window.YallaJo.api (JS5),
 * and rolls back + toasts on failure. The SSR move up/down forms remain the
 * no-JS path (PE1); the handles stay hidden when JS or the api client is missing.
 */
(function () {
    "use strict";

    var tbody = document.querySelector("tbody[data-yj-waypoints]");
    if (!tbody || tbody.dataset.yjInit === "1") { return; }
    if (!(window.YallaJo && window.YallaJo.api)) { return; }
    tbody.dataset.yjInit = "1";

    var reorderUrl = tbody.dataset.yjReorderUrl;
    if (!reorderUrl) { return; }

    // Reveal drag handles only when the enhanced path is available.
    tbody.querySelectorAll("[data-yj-drag-handle]").forEach(function (h) {
        h.classList.remove("d-none");
    });

    var dragRow = null;
    var startOrder = null;

    function rows() {
        return Array.prototype.slice.call(tbody.querySelectorAll("tr[data-yj-waypoint-id]"));
    }

    function currentOrder() {
        return rows().map(function (r) { return r.dataset.yjWaypointId; });
    }

    function renumber() {
        rows().forEach(function (r, i) {
            var cell = r.querySelector("td bdi.font-data");
            if (cell) { cell.textContent = String(i + 1); }
        });
    }

    function restore(order) {
        var map = {};
        rows().forEach(function (r) { map[r.dataset.yjWaypointId] = r; });
        order.forEach(function (id) {
            if (map[id]) { tbody.appendChild(map[id]); }
        });
        renumber();
    }

    function toast(msg, type) {
        if (window.YallaJo && window.YallaJo.toast) { window.YallaJo.toast(msg, type); }
    }

    function persist() {
        var order = currentOrder();
        if (startOrder && order.join() === startOrder.join()) { return; }

        var fd = new FormData();
        order.forEach(function (id) { fd.append("waypointIds", id); });

        window.YallaJo.api.postForm(reorderUrl, fd)
            .then(function (response) {
                if (response.ok) {
                    toast(tbody.dataset.yjSavedText || "Order saved.", "success");
                    return;
                }
                // NF6 rollback on rejection.
                if (startOrder) { restore(startOrder); }
                toast(tbody.dataset.yjFailedText || "Could not save the new order.", "error");
            })
            .catch(function () {
                if (startOrder) { restore(startOrder); }
                toast(tbody.dataset.yjFailedText || "Could not save the new order.", "error");
            });
    }

    tbody.addEventListener("pointerdown", function (e) {
        var handle = e.target.closest("[data-yj-drag-handle]");
        if (!handle) { return; }
        e.preventDefault();

        dragRow = handle.closest("tr[data-yj-waypoint-id]");
        if (!dragRow) { return; }

        startOrder = currentOrder();
        dragRow.classList.add("table-active");
        handle.setPointerCapture(e.pointerId);

        function onMove(ev) {
            var over = document.elementFromPoint(ev.clientX, ev.clientY);
            var target = over && over.closest("tr[data-yj-waypoint-id]");
            if (!target || target === dragRow || target.parentElement !== tbody) { return; }

            var rect = target.getBoundingClientRect();
            var before = ev.clientY < rect.top + rect.height / 2;
            tbody.insertBefore(dragRow, before ? target : target.nextSibling);
            renumber();
        }

        function onUp(ev) {
            handle.releasePointerCapture(ev.pointerId);
            handle.removeEventListener("pointermove", onMove);
            handle.removeEventListener("pointerup", onUp);
            handle.removeEventListener("pointercancel", onCancel);
            dragRow.classList.remove("table-active");
            persist();
            dragRow = null;
        }

        function onCancel(ev) {
            handle.releasePointerCapture(ev.pointerId);
            handle.removeEventListener("pointermove", onMove);
            handle.removeEventListener("pointerup", onUp);
            handle.removeEventListener("pointercancel", onCancel);
            dragRow.classList.remove("table-active");
            if (startOrder) { restore(startOrder); }
            dragRow = null;
        }

        handle.addEventListener("pointermove", onMove);
        handle.addEventListener("pointerup", onUp);
        handle.addEventListener("pointercancel", onCancel);
    });
}());
