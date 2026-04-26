self.addEventListener('install', function (e) {
    self.skipWaiting();
});

self.addEventListener('activate', function (e) {
    e.waitUntil(self.clients.claim());
});

self.addEventListener('push', function (event) {
    if (!event.data) return;
    var data = {};
    try { data = event.data.json(); } catch (err) { data = { title: 'Bildirim', body: event.data.text() }; }

    event.waitUntil(self.registration.showNotification(data.title || 'Bildirim', {
        body: data.body || '',
        icon: '/images/logo-192.png',
        badge: '/images/badge-72.png',
        data: { url: data.actionUrl, notificationId: data.notificationId },
        tag: 'notification-' + (data.notificationId || Date.now()),
        renotify: false
    }));
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    var url = (event.notification.data && event.notification.data.url) || '/notifications';
    event.waitUntil(self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (clients) {
        for (var i = 0; i < clients.length; i++) {
            var client = clients[i];
            if (client.url.endsWith(url) && 'focus' in client) return client.focus();
        }
        return self.clients.openWindow(url);
    }));
});
