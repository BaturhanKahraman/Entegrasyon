// ── Library Defaults ─────────────────────────────────────────────────

// Flatpickr: Turkce locale varsayilan
if (typeof flatpickr !== 'undefined' && flatpickr.l10ns && flatpickr.l10ns.tr) {
    flatpickr.localize(flatpickr.l10ns.tr);
}

// Notyf: Global instance — showNotify('Mesaj', 'success') ile kullanilir
var notyf = typeof Notyf !== 'undefined' ? new Notyf({
    duration: 4000,
    position: { x: 'right', y: 'top' },
    dismissible: true,
    ripple: true
}) : null;

window.showNotify = function (message, type) {
    if (!notyf) return;
    if (type === 'success') notyf.success(message);
    else if (type === 'error' || type === 'danger') notyf.error(message);
    else notyf.open({ type: type || 'info', message: message });
};

// GLightbox: otomatik baslat (class="glightbox" olan linkleri yakalar)
if (typeof GLightbox !== 'undefined') {
    var lightbox = GLightbox({ selector: '.glightbox', touchNavigation: true, loop: true });
    // HTMX swap sonrasi yeniden baslat
    document.body.addEventListener('htmx:afterSwap', function () {
        lightbox.reload();
    });
}

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

// ── Page Banner ──────────────────────────────────────────────────────

// Sayfanin ustunde kalici info/uyari/hata banner'i gosterir.
// Kullanim (JS):  showBanner({ message: '...', type: 'info' })
// Kullanim (HTMX): HX-Trigger: {"showBanner":{"message":"...","type":"warning"}}
// type: info | success | warning | danger
// icon: otomatik secilir, override edilebilir
// dismissible: true (varsayilan), false ise kapatma butonu olmaz

var bannerIcons = { info: 'ti-info-circle', success: 'ti-check', warning: 'ti-alert-triangle', danger: 'ti-alert-circle' };

window.showBanner = function (opts) {
    var container = document.getElementById('page-banner');
    if (!container) return;

    var type = opts.type || 'info';
    var icon = opts.icon || bannerIcons[type] || 'ti-info-circle';
    var dismissible = opts.dismissible !== false;
    var id = opts.id || '';

    if (id && container.querySelector('[data-banner-id="' + id + '"]')) return;

    var el = document.createElement('div');
    el.className = 'alert alert-' + type + (dismissible ? ' alert-dismissible' : '') + ' mb-3';
    el.setAttribute('role', 'alert');
    if (id) el.setAttribute('data-banner-id', id);

    var wrapper = document.createElement('div');
    wrapper.className = 'd-flex align-items-center';

    var iconEl = document.createElement('i');
    iconEl.className = 'ti ' + icon + ' me-2';
    iconEl.style.fontSize = '1.25rem';
    wrapper.appendChild(iconEl);

    var textEl = document.createElement('div');
    textEl.textContent = opts.message;
    wrapper.appendChild(textEl);

    el.appendChild(wrapper);

    if (dismissible) {
        var closeBtn = document.createElement('a');
        closeBtn.className = 'btn-close';
        closeBtn.setAttribute('data-bs-dismiss', 'alert');
        closeBtn.setAttribute('aria-label', 'Kapat');
        el.appendChild(closeBtn);
    }

    container.appendChild(el);
};

window.clearBanners = function (id) {
    var container = document.getElementById('page-banner');
    if (!container) return;
    if (id) {
        var el = container.querySelector('[data-banner-id="' + id + '"]');
        if (el) el.remove();
    } else {
        container.innerHTML = '';
    }
};

document.body.addEventListener('showBanner', function (event) {
    showBanner(event.detail || {});
});

document.body.addEventListener('clearBanners', function (event) {
    clearBanners((event.detail || {}).id);
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

// ── HTMX Loading Bar ─────────────────────────────────────────────────

(function () {
    var bar = document.getElementById('htmx-progress');
    if (!bar) return;
    var timer = null;

    document.body.addEventListener('htmx:beforeRequest', function () {
        clearTimeout(timer);
        bar.style.width = '0%';
        bar.style.opacity = '1';
        bar.style.transition = 'none';
        requestAnimationFrame(function () {
            bar.style.transition = 'width 8s cubic-bezier(0.1, 0.7, 0.3, 1)';
            bar.style.width = '90%';
        });
    });

    document.body.addEventListener('htmx:afterRequest', function () {
        bar.style.transition = 'width 0.2s ease';
        bar.style.width = '100%';
        timer = setTimeout(function () {
            bar.style.opacity = '0';
            setTimeout(function () { bar.style.width = '0%'; }, 300);
        }, 200);
    });

    document.body.addEventListener('htmx:sendError', function () {
        bar.style.width = '100%';
        bar.style.background = 'var(--tblr-danger)';
        timer = setTimeout(function () {
            bar.style.opacity = '0';
            setTimeout(function () { bar.style.width = '0%'; bar.style.background = ''; }, 300);
        }, 1000);
    });
})();

// ── HTMX Error Handling ───────────────────────────────────────────────

// Network hatalarını kullanıcıya göster
document.body.addEventListener('htmx:responseError', function (event) {
    if (event.detail.xhr.status === 401) {
        window.location.href = '/auth/login';
    }
});

// ── Offline / Connection Detection ───────────────────────────────────

(function () {
    function onOffline() {
        showBanner({ message: 'Internet baglantisi kesildi. Bazi islemler calismayabilir.', type: 'danger', id: 'offline', dismissible: false });
    }
    function onOnline() {
        clearBanners('offline');
        if (typeof showNotify === 'function') showNotify('Baglanti yeniden saglandi.', 'success');
    }
    window.addEventListener('offline', onOffline);
    window.addEventListener('online', onOnline);
    if (!navigator.onLine) onOffline();
})();

// ── Confirm Dialog (Tabler Modal) ────────────────────────────────────

// hx-confirm icin guzel bir Tabler modal gosterir (duz browser confirm yerine)
document.body.addEventListener('htmx:confirm', function (event) {
    event.preventDefault();
    var message = event.detail.question;
    if (!message) { event.detail.issueRequest(true); return; }

    var backdrop = document.createElement('div');
    backdrop.className = 'modal-backdrop fade show';

    var modal = document.createElement('div');
    modal.className = 'modal modal-blur fade show';
    modal.style.display = 'block';
    modal.setAttribute('tabindex', '-1');

    var dialog = document.createElement('div');
    dialog.className = 'modal-dialog modal-sm modal-dialog-centered';

    var content = document.createElement('div');
    content.className = 'modal-content';

    var statusDiv = document.createElement('div');
    statusDiv.className = 'modal-status bg-danger';
    content.appendChild(statusDiv);

    var body = document.createElement('div');
    body.className = 'modal-body text-center py-4';

    var icon = document.createElement('i');
    icon.className = 'ti ti-alert-triangle mb-2 text-danger';
    icon.style.fontSize = '3rem';
    body.appendChild(icon);

    var title = document.createElement('h3');
    title.textContent = 'Emin misiniz?';
    body.appendChild(title);

    var text = document.createElement('div');
    text.className = 'text-secondary';
    text.textContent = message;
    body.appendChild(text);

    content.appendChild(body);

    var footer = document.createElement('div');
    footer.className = 'modal-footer';

    var row = document.createElement('div');
    row.className = 'w-100';

    var rowInner = document.createElement('div');
    rowInner.className = 'row';

    var colCancel = document.createElement('div');
    colCancel.className = 'col';
    var btnCancel = document.createElement('button');
    btnCancel.className = 'btn w-100';
    btnCancel.textContent = 'Vazgec';
    colCancel.appendChild(btnCancel);

    var colConfirm = document.createElement('div');
    colConfirm.className = 'col';
    var btnConfirm = document.createElement('button');
    btnConfirm.className = 'btn btn-danger w-100';
    btnConfirm.textContent = 'Evet, devam et';
    colConfirm.appendChild(btnConfirm);

    rowInner.appendChild(colCancel);
    rowInner.appendChild(colConfirm);
    row.appendChild(rowInner);
    footer.appendChild(row);
    content.appendChild(footer);
    dialog.appendChild(content);
    modal.appendChild(dialog);

    document.body.appendChild(backdrop);
    document.body.appendChild(modal);
    document.body.classList.add('modal-open');

    function cleanup() {
        modal.remove();
        backdrop.remove();
        document.body.classList.remove('modal-open');
    }

    btnCancel.addEventListener('click', cleanup);
    btnConfirm.addEventListener('click', function () { cleanup(); event.detail.issueRequest(true); });
    backdrop.addEventListener('click', cleanup);
    modal.addEventListener('click', function (e) { if (e.target === modal) cleanup(); });

    // Esc ile kapat
    function onKey(e) { if (e.key === 'Escape') { cleanup(); document.removeEventListener('keydown', onKey); } }
    document.addEventListener('keydown', onKey);

    btnConfirm.focus();
});

// ── Keyboard Shortcuts ───────────────────────────────────────────────

document.addEventListener('keydown', function (event) {
    // Modal veya input aciksa atla (Escape haric)
    var tag = (event.target.tagName || '').toLowerCase();
    var isInput = tag === 'input' || tag === 'textarea' || tag === 'select' || event.target.isContentEditable;

    // Ctrl+K / Cmd+K — Command Palette
    if ((event.ctrlKey || event.metaKey) && event.key === 'k') {
        event.preventDefault();
        var palette = document.getElementById('command-palette');
        if (palette) {
            var bsModal = bootstrap.Modal.getOrCreateInstance(palette);
            bsModal.toggle();
        }
        return;
    }

    if (isInput) return;

    // Alt+N — Yeni Urun
    if (event.altKey && event.key === 'n') {
        event.preventDefault();
        window.location.href = '/products/add';
        return;
    }

    // Alt+S — Satis Yap (POS)
    if (event.altKey && event.key === 's') {
        event.preventDefault();
        window.location.href = '/pos';
        return;
    }

    // Alt+D — Dashboard
    if (event.altKey && event.key === 'd') {
        event.preventDefault();
        window.location.href = '/';
        return;
    }

    // ? — Klavye kisayollari yardimi
    if (event.key === '?') {
        event.preventDefault();
        var shortcutPalette = document.getElementById('shortcut-help');
        if (shortcutPalette) {
            bootstrap.Modal.getOrCreateInstance(shortcutPalette).toggle();
        }
    }
});

// ── Notification Bell (Real-time via SSE — Server-Sent Events) ───────

// TODO: SSE bildirim client'ı şimdilik devre dışı — backend endpoint kaldırıldı.
// İleride düzgün SSE/SignalR implementasyonu ile birlikte aktif edilecek.

// ── Sidebar: Scroll to Active Item ───────────────────────────────────

(function () {
    var activeLink = document.querySelector('#sidebar-menu .nav-link.active');
    if (activeLink) {
        setTimeout(function () {
            activeLink.scrollIntoView({ block: 'center', behavior: 'instant' });
        }, 100);
    }
})();
