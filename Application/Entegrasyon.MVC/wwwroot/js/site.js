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

// ── Currency Input (IMask) ───────────────────────────────────────────
// .currency-input class'li input'lara Turkce para formatlama uygular.
// Backend'e submit edilmeden once deger normalize edilir: "1.234,56" -> "1234.56"

function initCurrencyInputs(root) {
    if (typeof IMask === 'undefined') return;
    var scope = root || document;
    var inputs = scope.querySelectorAll('.currency-input');
    inputs.forEach(function (el) {
        if (el.dataset.currencyMasked === '1') return;
        IMask(el, {
            mask: Number,
            scale: 2,
            signed: false,
            thousandsSeparator: '.',
            radix: ',',
            mapToRadix: ['.'],
            normalizeZeros: true,
            padFractionalZeros: false
        });
        el.dataset.currencyMasked = '1';
    });
}

// Ilk sayfa yuklendiginde calistir
document.addEventListener('DOMContentLoaded', function () { initCurrencyInputs(); });
// HTMX swap sonrasi yeni partial'a da uygula
document.body.addEventListener('htmx:afterSwap', function (evt) {
    initCurrencyInputs(evt.detail.target);
});

// Form submit edilmeden once currency input'larini normalize et ("1.234,56" -> "1234.56")
// Capture phase kullaniyoruz ki HTMX'in submit yakalamasindan once calisalim
document.addEventListener('submit', function (evt) {
    var form = evt.target;
    if (!form || typeof form.querySelectorAll !== 'function') return;
    form.querySelectorAll('.currency-input').forEach(function (el) {
        var val = el.value;
        if (!val) return;
        // "1.234,56" -> "1234.56"
        var normalized = val.replace(/\./g, '').replace(',', '.');
        // Geriye sadece rakam ve nokta birak (guvenlik)
        normalized = normalized.replace(/[^\d.]/g, '');
        el.value = normalized;
    });
}, true);

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

// HTMX response'larından gelen showToast trigger'ını dinle.
// Gerçek "toast" = floating corner notification — Notyf (sağ üstte) bu işi yapar.
// Inline alerts (banner) için _Toast.cshtml partial + TempData kullanılır.
document.body.addEventListener('showToast', function (event) {
    var detail = event.detail || {};
    var message = detail.message || 'İşlem tamamlandı';
    var type = detail.type || 'info';
    if (typeof window.showNotify === 'function') {
        window.showNotify(message, type);
    }
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

// ── Loading State / Çift-Submit Önleme (GLOBAL — alışkanlık) ─────────
//
// Amaç: Her async HTMX isteğinde, isteği tetikleyen form/element içindeki
// submit buton(lar)ı otomatik kilitlensin + Tabler spinner göstersin.
// Böylece kullanıcı 3-4 sn süren bir kayıtta butona ikinci kez basıp
// çift-submit YAPAMAZ. Per-buton iş gerekmez — global hook her formda çalışır.
//
// Tabler deseni (doğrulandı, docs.tabler.io/ui/components/buttons):
//   .btn-loading  → pure CSS; spinner gösterir, label'ı gizler ama markup'ta
//                   label kaldığı için buton genişliği sabit kalar.
//   disabled      → tıklamayı tamamen engeller (çift-submit guard).
//
// Top progress bar (#htmx-progress) ayrı çalışır — buna DOKUNMUYORUZ.
//
// NOT: Raw fetch() kullanan yerler bu hook'un DIŞINDADIR (htmx eventi yok).
//      Oralarda elle disable+spinner gerekir (bkz. _CreateStep3Variants).
(function () {
    'use strict';

    // İsteği tetikleyen butona (submitter) işaret koy ki o butonda spinner
    // gösterelim; diğer submit butonları sadece disabled olsun.
    // Form-level hx-post'ta htmx:beforeRequest.elt = form olur, hangi butona
    // basıldığı kaybolur → submit event'inden submitter'ı forma iliştiriyoruz.
    document.addEventListener('submit', function (evt) {
        var form = evt.target;
        if (!form || form.nodeName !== 'FORM') return;
        form._submitter = evt.submitter || null;
    }, true);

    // Verilen kök (form veya buton) için kilitlenecek submit buton(lar)ını bul.
    function collectButtons(elt) {
        if (!elt) return [];
        // Element bir form ise → içindeki tüm submit butonları (+ submit input).
        if (elt.nodeName === 'FORM') {
            return Array.prototype.slice.call(
                elt.querySelectorAll('button[type="submit"], button:not([type]), input[type="submit"]')
            );
        }
        // İsteği doğrudan bir buton/link tetiklediyse → yalnızca o element.
        if (elt.matches && elt.matches('button, input[type="submit"], a.btn, .btn')) {
            return [elt];
        }
        return [];
    }

    function lockButtons(elt) {
        var buttons = collectButtons(elt);
        if (buttons.length === 0) return;

        // Spinner gösterilecek buton: form'a basılan submitter; yoksa ilk buton.
        var spinnerTarget;
        if (elt.nodeName === 'FORM' && elt._submitter && buttons.indexOf(elt._submitter) !== -1) {
            spinnerTarget = elt._submitter;
        } else if (elt.nodeName !== 'FORM') {
            spinnerTarget = elt;
        } else {
            spinnerTarget = buttons[0];
        }

        buttons.forEach(function (btn) {
            if (btn.dataset.loadingLocked === '1') return;
            btn.dataset.loadingLocked = '1';
            // <a> kullanılıyorsa disabled attribute işe yaramaz → .disabled class.
            if (btn.nodeName === 'A') {
                btn.classList.add('disabled');
                btn.setAttribute('aria-disabled', 'true');
            } else {
                btn.disabled = true;
            }
            if (btn === spinnerTarget) {
                btn.classList.add('btn-loading');
                btn.dataset.loadingSpinner = '1';
            }
        });
    }

    function unlockButtons() {
        // afterRequest.elt aynı olsa da garanti için global tarama: kilitli
        // her butonu eski haline döndür (yarış/iptal durumlarında da temiz).
        var locked = document.querySelectorAll('[data-loading-locked="1"]');
        Array.prototype.forEach.call(locked, function (btn) {
            delete btn.dataset.loadingLocked;
            if (btn.nodeName === 'A') {
                btn.classList.remove('disabled');
                btn.removeAttribute('aria-disabled');
            } else {
                btn.disabled = false;
            }
            if (btn.dataset.loadingSpinner === '1') {
                btn.classList.remove('btn-loading');
                delete btn.dataset.loadingSpinner;
            }
        });
    }

    document.body.addEventListener('htmx:beforeRequest', function (evt) {
        lockButtons(evt.detail.elt);
    });

    // afterRequest hem başarı hem hata (4xx/5xx) durumunda atılır.
    document.body.addEventListener('htmx:afterRequest', function () { unlockButtons(); });
    // Ağ hatası / timeout / iptal — afterRequest atılmayabilir, garanti için.
    document.body.addEventListener('htmx:sendError', function () { unlockButtons(); });
    document.body.addEventListener('htmx:timeout', function () { unlockButtons(); });
    document.body.addEventListener('htmx:responseError', function () { unlockButtons(); });
    document.body.addEventListener('htmx:abort', function () { unlockButtons(); });
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
document.body.addEventListener('htmx:confirm', function (htmxEvent) {
    htmxEvent.preventDefault();
    var message = htmxEvent.detail.question;
    if (!message) { htmxEvent.detail.issueRequest(true); return; }

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
    btnCancel.type = 'button';
    btnCancel.className = 'btn w-100';
    btnCancel.textContent = 'Vazgec';
    colCancel.appendChild(btnCancel);

    var colConfirm = document.createElement('div');
    colConfirm.className = 'col';
    var btnConfirm = document.createElement('button');
    btnConfirm.type = 'button';
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
        document.removeEventListener('keydown', onKey);
    }

    function confirmAction(clickEv) {
        if (clickEv) { clickEv.preventDefault(); clickEv.stopPropagation(); }
        cleanup();
        htmxEvent.detail.issueRequest(true);
    }

    btnCancel.addEventListener('click', function (ev) { if (ev) ev.preventDefault(); cleanup(); });
    btnConfirm.addEventListener('click', confirmAction);
    backdrop.addEventListener('click', cleanup);
    modal.addEventListener('click', function (e) { if (e.target === modal) cleanup(); });

    function onKey(e) {
        if (e.key === 'Escape') { cleanup(); }
        else if (e.key === 'Enter') { confirmAction(e); }
    }
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

// ── Clickable Table Rows (tr.table-row-clickable[data-href]) ─────────

// Tablo satırının tamamını tıklanabilir yapar; aksiyonlar/detay satıra devredilir.
// Delegated handler → HTMX swap sonrası da çalışır (yeniden bağlama gerekmez).
// Satır içindeki gerçek link/buton/checkbox/form/HTMX kontrolüne tıklama
// yanlış navigasyona YOL AÇMAZ — bunlar kendi davranışını korur.
(function () {
    var INTERACTIVE = 'a, button, input, select, textarea, label, summary, ' +
        '.dropdown, .form-check, [role="button"], [hx-get], [hx-post], ' +
        '[hx-put], [hx-delete], [hx-patch], [data-bs-toggle], [onclick]';

    function rowFor(target) {
        if (!target || !target.closest) return null;
        var row = target.closest('tr.table-row-clickable[data-href]');
        if (!row) return null;
        // Satır içindeki interaktif öğeye tıklandıysa navigasyonu engelle.
        var interactive = target.closest(INTERACTIVE);
        if (interactive && row.contains(interactive)) return null;
        return row;
    }

    document.addEventListener('click', function (e) {
        if (e.button !== 0) return; // yalnızca sol tık
        var row = rowFor(e.target);
        if (!row) return;
        if (e.metaKey || e.ctrlKey) {
            window.open(row.dataset.href, '_blank');
        } else {
            window.location = row.dataset.href;
        }
    });

    // Orta tık (button === 1) → yeni sekme
    document.addEventListener('auxclick', function (e) {
        if (e.button !== 1) return;
        var row = rowFor(e.target);
        if (!row) return;
        window.open(row.dataset.href, '_blank');
    });

    // Klavye erişilebilirliği: odaklı satırda Enter → git
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Enter') return;
        var target = e.target;
        if (!target.classList || !target.classList.contains('table-row-clickable')) return;
        if (!target.dataset.href) return;
        e.preventDefault();
        window.location = target.dataset.href;
    });
})();

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

// ── Bootstrap Popover ────────────────────────────────────────────────
// data-bs-toggle="popover" olan elemanları başlatır. HTMX swap sonrası
// yeni gelen partial'lardaki popover'lar da otomatik aktive edilir.

function initPopovers(root) {
    if (typeof bootstrap === 'undefined' || !bootstrap.Popover) return;
    var scope = root || document;
    if (typeof scope.querySelectorAll !== 'function') return;
    scope.querySelectorAll('[data-bs-toggle="popover"]').forEach(function (el) {
        bootstrap.Popover.getOrCreateInstance(el);
    });
}

document.addEventListener('DOMContentLoaded', function () { initPopovers(); });
document.body.addEventListener('htmx:afterSwap', function (evt) {
    initPopovers(evt.detail.target);
});

// ── Deselectable Radio ───────────────────────────────────────────────
// data-deselectable attribute'lu radio input'lar tekrar tıklanınca
// seçimi bırakır (örn. kategori özellik panelindeki Zorunlu/Varyanter/
// Dilimleyici seçimleri). Label'a tıklamak da input'u tetiklediği için
// mousedown anındaki checked durumu saklanır, click'te karşılaştırılır.

(function () {
    function resolveRadio(target) {
        if (!target || typeof target.closest !== 'function') return null;
        if (target.matches && target.matches('input[type="radio"][data-deselectable]')) return target;
        var label = target.closest('label');
        return label ? label.querySelector('input[type="radio"][data-deselectable]') : null;
    }

    document.addEventListener('mousedown', function (evt) {
        var radio = resolveRadio(evt.target);
        if (radio) radio.dataset.wasChecked = radio.checked ? '1' : '0';
    });

    // Yalnız input'un kendi click'inde davran: label tıklamasında tarayıcı
    // önce label click'ini, sonra input'a synthetic click'i dispatch eder —
    // label click'inde uncheck yapılırsa synthetic click tekrar seçerdi.
    document.addEventListener('click', function (evt) {
        var t = evt.target;
        if (t && t.matches && t.matches('input[type="radio"][data-deselectable]') && t.dataset.wasChecked === '1') {
            t.checked = false;
            t.dataset.wasChecked = '0';
        }
    });
})();

// ── Liste Arama Filtresi ─────────────────────────────────────────────
// data-attr-filter="<liste seçici>" olan input'a yazılınca, hedef listedeki
// [data-filter-name] öğeleri TR-duyarsız substring eşleşmesine göre süzülür.
// Hiç eşleşme kalmazsa [data-filter-empty] öğesi gösterilir. HTMX swap
// sonrası input boş geldiği için ekstra reset gerekmez (delegasyon).

(function () {
    function normalize(s) {
        return (s || '').toLocaleLowerCase('tr-TR');
    }

    function applyFilter(input) {
        var list = document.querySelector(input.getAttribute('data-attr-filter'));
        if (!list) return;
        var term = normalize(input.value.trim());
        var items = list.querySelectorAll('[data-filter-name]');
        var visible = 0;
        items.forEach(function (item) {
            var match = !term || normalize(item.getAttribute('data-filter-name')).indexOf(term) !== -1;
            item.classList.toggle('d-none', !match);
            if (match) visible++;
        });
        var empty = list.querySelector('[data-filter-empty]');
        if (empty) empty.classList.toggle('d-none', visible > 0);
    }

    document.addEventListener('input', function (evt) {
        var input = evt.target;
        if (input && input.matches && input.matches('input[data-attr-filter]')) {
            applyFilter(input);
        }
    });
})();
