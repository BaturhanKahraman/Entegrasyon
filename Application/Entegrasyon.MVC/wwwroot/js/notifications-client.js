(function () {
    'use strict';

    const bellButton = document.querySelector('[data-notification-bell]');
    const badgeEl = document.querySelector('[data-notification-badge]');
    const dropdownList = document.querySelector('[data-notification-dropdown-list]');

    if (!bellButton) return;  // bell partial not on this page

    let unreadCount = parseInt(bellButton.dataset.unreadCount || '0', 10);

    function setUnreadCount(n) {
        unreadCount = Math.max(0, n);
        if (!badgeEl) return;
        if (unreadCount === 0) {
            badgeEl.classList.add('d-none');
            badgeEl.textContent = '';
        } else {
            badgeEl.classList.remove('d-none');
            badgeEl.textContent = unreadCount > 99 ? '99+' : String(unreadCount);
        }
    }

    function severityColor(s) {
        if (s === 'Error') return 'red';
        if (s === 'Warning') return 'yellow';
        if (s === 'Info') return 'blue';
        return 'secondary';
    }

    function severityToNotyfType(s) {
        if (s === 'Error') return 'error';
        if (s === 'Warning') return 'warning';
        return 'success';
    }

    function getAntiForgeryToken() {
        const t = document.querySelector('input[name="__RequestVerificationToken"]');
        if (t) return t.value;
        // Also check meta tag (used in this layout)
        const m = document.querySelector('meta[name="csrf-token"]');
        return m ? m.getAttribute('content') : '';
    }

    // Build dropdown item using safe DOM methods only — NO innerHTML
    function buildListItem(n) {
        const a = document.createElement('a');
        a.href = n.actionUrl || '/notifications';
        a.className = 'list-group-item list-group-item-action';
        a.setAttribute('data-notification-id', String(n.notificationId));

        const row = document.createElement('div');
        row.className = 'row align-items-center';

        const colAuto = document.createElement('div');
        colAuto.className = 'col-auto';
        const dot = document.createElement('span');
        dot.className = 'status-dot bg-' + severityColor(n.severity);
        colAuto.appendChild(dot);

        const col = document.createElement('div');
        col.className = 'col text-truncate';
        const strong = document.createElement('strong');
        strong.textContent = n.header || '';
        col.appendChild(strong);

        const sub = document.createElement('div');
        sub.className = 'text-secondary small text-truncate';
        sub.textContent = n.content || '';
        col.appendChild(sub);

        row.appendChild(colAuto);
        row.appendChild(col);
        a.appendChild(row);

        // Mark as read on click (fire-and-forget; navigation continues)
        a.addEventListener('click', function () {
            fetch('/notifications/' + encodeURIComponent(n.notificationId) + '/read', {
                method: 'POST',
                headers: { 'RequestVerificationToken': getAntiForgeryToken() },
                credentials: 'same-origin'
            }).catch(function () { /* swallow */ });
        });

        return a;
    }

    function prependToDropdown(n) {
        if (!dropdownList) return;
        const empty = dropdownList.querySelector('[data-empty-state]');
        if (empty) empty.remove();
        const item = buildListItem(n);
        dropdownList.insertBefore(item, dropdownList.firstChild);
    }

    function removeFromDropdown(notificationId) {
        if (!dropdownList) return;
        const el = dropdownList.querySelector('[data-notification-id="' + CSS.escape(String(notificationId)) + '"]');
        if (el) el.remove();
    }

    function showToast(n) {
        if (window.notyf) {
            window.notyf.open({
                type: severityToNotyfType(n.severity),
                message: (n.header || '') + ': ' + (n.content || '')
            });
        }
    }

    function connect() {
        const es = new EventSource('/events/notifications');
        es.addEventListener('notification', function (e) {
            try {
                const data = JSON.parse(e.data);
                setUnreadCount(unreadCount + 1);
                prependToDropdown(data);
                showToast(data);
            } catch (err) { console.warn('notification parse failed', err); }
        });
        es.addEventListener('notification.read', function (e) {
            try {
                const data = JSON.parse(e.data);
                setUnreadCount(unreadCount - 1);
                removeFromDropdown(data.notificationId);
            } catch (err) { /* swallow */ }
        });
        es.addEventListener('notification.dismissed', function (e) {
            try {
                const data = JSON.parse(e.data);
                removeFromDropdown(data.notificationId);
            } catch (err) { /* swallow */ }
        });
        // heartbeat events: ignore (just keep-alive)
        // browser auto-reconnects on error
    }

    document.addEventListener('DOMContentLoaded', connect);
})();
