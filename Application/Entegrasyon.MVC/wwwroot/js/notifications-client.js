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

    // Tek aktif EventSource referansı — çift bağlantıyı önler.
    let activeSource = null;

    function closeConnection() {
        if (activeSource) {
            activeSource.close();
            activeSource = null;
        }
    }

    function connect() {
        // Zaten açık bir bağlantı varsa yenisini açma (HTTP/1.1 bağlantı havuzunu koru).
        if (activeSource) return;

        const es = new EventSource('/events/notifications');
        activeSource = es;
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
        // browser auto-reconnects on error; referansımızı kaybetmemek için temizleme
        // pagehide/closeConnection üzerinden yapılır (activeSource hep geçerli kalır).
    }

    // Sayfadan ayrılırken (navigasyon, sekme kapanışı) bağlantıyı HEMEN serbest bırak.
    // HTTP/1.1'de host başına ~6 bağlantı sınırı var; kalıcı SSE kapanmazsa hızlı
    // gezinmede bağlantılar birikir, havuz tükenir ve sonraki istekler takılır.
    // pagehide bfcache-safe ve mobil dahil güvenilir tetiklenir.
    window.addEventListener('pagehide', closeConnection);

    async function registerPush() {
        if (!('serviceWorker' in navigator) || !('PushManager' in window)) return;
        try {
            const registration = await navigator.serviceWorker.register('/sw-admin.js');
            let subscription = await registration.pushManager.getSubscription();

            if (!subscription) {
                const permission = await Notification.requestPermission();
                if (permission !== 'granted') return;

                const vapidKeyResp = await fetch('/admin-push/vapid-public-key');
                if (!vapidKeyResp.ok) return;
                const vapidKey = await vapidKeyResp.text();
                if (!vapidKey) return;  // server returned empty key — VAPID not configured

                subscription = await registration.pushManager.subscribe({
                    userVisibleOnly: true,
                    applicationServerKey: urlBase64ToUint8Array(vapidKey)
                });
            }

            await fetch('/admin-push/subscribe', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                credentials: 'same-origin',
                body: JSON.stringify({
                    endpoint: subscription.endpoint,
                    p256dh: arrayBufferToBase64(subscription.getKey('p256dh')),
                    auth: arrayBufferToBase64(subscription.getKey('auth'))
                })
            });
        } catch (e) {
            console.warn('Push registration failed', e);
        }
    }

    function urlBase64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
        const raw = atob(base64);
        const out = new Uint8Array(raw.length);
        for (let i = 0; i < raw.length; i++) out[i] = raw.charCodeAt(i);
        return out;
    }

    function arrayBufferToBase64(buffer) {
        const bytes = new Uint8Array(buffer);
        let binary = '';
        for (let i = 0; i < bytes.byteLength; i++) binary += String.fromCharCode(bytes[i]);
        return btoa(binary);
    }

    // SSE bağlantısını sayfa boşa çıktıktan SONRA aç.
    // Neden: kalıcı SSE bağlantısı açıkken tarayıcı ağı asla "idle" olmaz; bu da
    // hem otomasyon (Playwright NetworkIdle) hem de ilk-yükleme önceliği için sorun.
    // İlk paint/asset yüklemesini bloklamadan, kısa bir idle penceresinden sonra bağlanırız.
    function connectWhenIdle() {
        if (window.requestIdleCallback) {
            requestIdleCallback(connect, { timeout: 2000 });
        } else {
            setTimeout(connect, 1000);
        }
    }

    function init() {
        connectWhenIdle();
        registerPush();
    }

    if (document.readyState === 'complete') {
        init();
    } else {
        window.addEventListener('load', init, { once: true });
    }
})();
