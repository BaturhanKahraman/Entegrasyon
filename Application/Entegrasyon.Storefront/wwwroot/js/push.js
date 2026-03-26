(function() {
    'use strict';

    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
        return;
    }

    var pageViewCount = parseInt(sessionStorage.getItem('sf_page_views') || '0', 10) + 1;
    sessionStorage.setItem('sf_page_views', pageViewCount.toString());

    navigator.serviceWorker.register('/sw.js').then(function(registration) {
        // Only prompt after 2nd page view to avoid annoying first-time visitors
        if (pageViewCount < 2) return;

        if (Notification.permission === 'granted') {
            subscribeToPush(registration);
        } else if (Notification.permission !== 'denied') {
            Notification.requestPermission().then(function(permission) {
                if (permission === 'granted') {
                    subscribeToPush(registration);
                }
            });
        }
    });

    function subscribeToPush(registration) {
        registration.pushManager.getSubscription().then(function(subscription) {
            if (subscription) return; // Already subscribed

            // Without a real VAPID key, we just register the service worker
            // Actual push subscription requires VAPID public key from server
            // This is a placeholder for when VAPID keys are configured
        });
    }
})();
