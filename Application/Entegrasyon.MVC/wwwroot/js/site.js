// ── HTMX Global Configuration ─────────────────────────────────────────

// HTMX 2.0: swap edilen HTML'deki script taglerini calistir
// (Dashboard chart, ApexCharts init gibi inline script'ler icin gerekli)
htmx.config.allowScriptTags = true;

// Anti-forgery token: her HTMX isteğine otomatik ekle
document.body.addEventListener('htmx:configRequest', function (event) {
    var token = document.querySelector('meta[name="csrf-token"]');
    if (token) {
        event.detail.headers['RequestVerificationToken'] = token.content;
    }
});

// ── Toast Notifications ───────────────────────────────────────────────

// HTMX response'larından gelen showToast trigger'ını dinle
document.body.addEventListener('showToast', function (event) {
    var detail = event.detail || {};
    var message = detail.message || 'Islem tamamlandi';
    var type = detail.type || 'info';
    var container = document.getElementById('toast-container');
    if (!container) return;

    var alert = document.createElement('div');
    alert.className = 'alert alert-' + type + ' alert-dismissible fade show';
    alert.setAttribute('role', 'alert');
    alert.textContent = message;

    var closeBtn = document.createElement('a');
    closeBtn.className = 'btn-close';
    closeBtn.setAttribute('data-bs-dismiss', 'alert');
    closeBtn.setAttribute('aria-label', 'Kapat');
    alert.appendChild(closeBtn);

    container.appendChild(alert);
    setTimeout(function () { alert.remove(); }, 5000);
});

// ── Modal Management ──────────────────────────────────────────────────

// HTMX response'larından gelen closeModal trigger'ını dinle
document.body.addEventListener('closeModal', function () {
    var container = document.getElementById('modal-container');
    if (container) {
        while (container.firstChild) container.removeChild(container.firstChild);
    }
    var backdrop = document.querySelector('.modal-backdrop');
    if (backdrop) backdrop.remove();
    document.body.classList.remove('modal-open');
});

// Modal dışına tıklayınca kapat
document.addEventListener('click', function (event) {
    if (event.target.hasAttribute('data-dismiss-modal')) {
        document.body.dispatchEvent(new Event('closeModal'));
    }
});

// ── HTMX Error Handling ───────────────────────────────────────────────

// Network hatalarını kullanıcıya göster
document.body.addEventListener('htmx:responseError', function (event) {
    if (event.detail.xhr.status === 401) {
        window.location.href = '/auth/login';
    }
});

// ── Notification Bell (Real-time via SSE — Server-Sent Events) ───────

(function () {
    // SSE sadece authenticated kullanicilar icin calısır
    if (!document.getElementById('notification-bell')) return;

    var source = new EventSource('/notifications/stream');

    source.addEventListener('notification', function (event) {
        try {
            var data = JSON.parse(event.data);

            // Badge guncelle
            var badge = document.getElementById('notification-badge');
            if (badge) {
                var count = parseInt(badge.textContent || '0') + 1;
                badge.textContent = count;
                badge.style.display = '';
            } else {
                var link = document.querySelector('#notification-bell a');
                if (link) {
                    var newBadge = document.createElement('span');
                    newBadge.className = 'badge bg-red badge-notification';
                    newBadge.id = 'notification-badge';
                    newBadge.textContent = '1';
                    link.appendChild(newBadge);
                }
            }

            // Toast goster
            document.body.dispatchEvent(new CustomEvent('showToast', {
                detail: {
                    message: data.Header || 'Yeni bildirim',
                    type: 'info'
                }
            }));
        } catch (e) {
            console.warn('SSE notification parse error:', e);
        }
    });

    source.onerror = function () {
        console.warn('SSE bildirim baglantisi kesildi, tarayici otomatik yeniden deneyecek...');
    };
})();
