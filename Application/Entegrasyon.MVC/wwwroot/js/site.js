// ── Theme Toggle ─────────────────────────────────────────────────────

(function () {
    var toggle = document.getElementById('theme-toggle');
    var icon = document.getElementById('theme-icon');
    if (!toggle || !icon) return;

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        localStorage.setItem('tabler-theme', theme);
        icon.className = theme === 'dark' ? 'ti ti-sun' : 'ti ti-moon';
        onThemeChange(theme);
    }

    // Sayfa yüklendiğinde ikon durumunu ayarla
    var current = localStorage.getItem('tabler-theme') || 'light';
    icon.className = current === 'dark' ? 'ti ti-sun' : 'ti ti-moon';

    toggle.addEventListener('click', function (e) {
        e.preventDefault();
        var next = document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
        applyTheme(next);
    });
})();

// ── Chart Theme Hook ─────────────────────────────────────────────────

function onThemeChange(theme) {
    var isDark = theme === 'dark';
    var textColor = isDark ? '#a0aec0' : '#666';
    var gridColor = isDark ? '#2c3e56' : '#e0e0e0';

    document.querySelectorAll('[data-apex-chart]').forEach(function (el) {
        var chart = ApexCharts.getChartByID(el.id);
        if (chart) {
            chart.updateOptions({
                theme: { mode: theme },
                xaxis: { labels: { style: { colors: textColor } } },
                yaxis: { labels: { style: { colors: textColor } } },
                grid: { borderColor: gridColor }
            });
        }
    });
}

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

// TODO: SSE bildirim client'ı şimdilik devre dışı — backend endpoint kaldırıldı.
// İleride düzgün SSE/SignalR implementasyonu ile birlikte aktif edilecek.
