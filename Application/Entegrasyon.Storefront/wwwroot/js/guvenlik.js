/* Zekids — Güvenlik sayfası etkileşimleri.
   Chrome (header/drawer/search) site.js'te; burada sadece sayfaya özel davranış. */
(function () {
    'use strict';

    function toast(msg) { if (window.toast) window.toast(msg); }

    /* ---- mobil hesap çekmecesi ---- */
    var accSheet = document.getElementById('accSheet');
    function openAcc() { if (accSheet) { accSheet.classList.remove('hidden'); document.body.style.overflow = 'hidden'; } }
    function closeAcc() { if (accSheet) { accSheet.classList.add('hidden'); document.body.style.overflow = ''; } }
    document.querySelectorAll('[data-open-acc]').forEach(function (b) { b.addEventListener('click', openAcc); });
    document.querySelectorAll('[data-close-acc]').forEach(function (b) { b.addEventListener('click', closeAcc); });

    /* ---- switch toggle (görsel) + toast + opsiyonel form submit ---- */
    document.querySelectorAll('[data-toggle-switch]').forEach(function (sw) {
        function toggle() {
            sw.dataset.on = sw.dataset.on === '1' ? '0' : '1';
            var on = sw.dataset.on === '1';
            sw.setAttribute('aria-checked', on ? 'true' : 'false');
            var msg = on ? sw.dataset.toastOn : sw.dataset.toastOff;
            if (msg) toast(msg);
            // data-autosubmit form içindeyse tercihi sunucuya kaydet (sayfa yeniden yüklenir).
            var form = sw.closest('form[data-autosubmit]');
            if (form) form.submit();
        }
        sw.addEventListener('click', toggle);
        sw.addEventListener('keydown', function (e) {
            if (e.key === ' ' || e.key === 'Enter') { e.preventDefault(); toggle(); }
        });
    });

    /* ---- aktif oturumlar (demo veri; ileride server-side) ---- */
    var SESSIONS = [
        { id: 1, device: 'MacBook Pro', kind: 'laptop', meta: 'İstanbul · 2 dk önce', current: true },
        { id: 2, device: 'iPhone 15', kind: 'phone', meta: 'İstanbul · 3 saat önce', current: false },
        { id: 3, device: 'Chrome · Windows', kind: 'laptop', meta: 'Ankara · 2 gün önce', current: false },
    ];
    var DEV_ICON = {
        laptop: '<rect x="3" y="4" width="18" height="12" rx="2"/><path d="M2 20h20"/>',
        phone: '<rect x="7" y="2" width="10" height="20" rx="2"/><path d="M12 18h.01"/>',
    };
    var sessionsEl = document.getElementById('sessions');
    function esc(s) { var d = document.createElement('div'); d.textContent = s; return d.innerHTML; }

    function renderSessions() {
        if (!sessionsEl) return;
        sessionsEl.innerHTML = SESSIONS.map(function (s) {
            return '<div class="flex items-center gap-3 py-3.5 first:pt-0 last:pb-0">'
                + '<span class="w-10 h-10 rounded-xl bg-cream text-charcoal/70 flex items-center justify-center shrink-0"><svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' + DEV_ICON[s.kind] + '</svg></span>'
                + '<div class="flex-1 min-w-0">'
                + '<p class="font-medium text-charcoal text-sm flex items-center gap-2">' + esc(s.device) + (s.current ? '<span class="text-[11px] font-medium bg-primary/10 text-primary px-2 py-0.5 rounded-full">Bu cihaz</span>' : '') + '</p>'
                + '<p class="text-xs text-muted mt-0.5">' + esc(s.meta) + '</p>'
                + '</div>'
                + (s.current ? '' : '<button type="button" data-force-logout="' + s.id + '" class="text-sm font-medium text-danger hover:underline shrink-0">Çıkış zorla</button>')
                + '</div>';
        }).join('');
    }
    function forceLogout(id) {
        var i = SESSIONS.findIndex(function (s) { return s.id === id; });
        if (i > -1) SESSIONS.splice(i, 1);
        renderSessions();
        toast('Oturum sonlandırıldı');
    }
    if (sessionsEl) {
        sessionsEl.addEventListener('click', function (e) {
            var btn = e.target.closest('[data-force-logout]');
            if (btn) forceLogout(parseInt(btn.dataset.forceLogout, 10));
        });
    }

    /* ---- hesap silme modalı ---- */
    var modal = document.getElementById('delAccModal');
    var confirmInput = document.querySelector('[data-del-confirm]');
    var delBtn = document.getElementById('delBtn');
    function openDelAcc() {
        if (confirmInput) confirmInput.value = '';
        if (delBtn) delBtn.disabled = true;
        if (modal) { modal.classList.remove('hidden'); document.body.style.overflow = 'hidden'; }
    }
    function closeDelAcc() { if (modal) { modal.classList.add('hidden'); document.body.style.overflow = ''; } }
    document.querySelectorAll('[data-open-delacc]').forEach(function (b) { b.addEventListener('click', openDelAcc); });
    document.querySelectorAll('[data-close-delacc]').forEach(function (b) { b.addEventListener('click', closeDelAcc); });
    if (confirmInput && delBtn) {
        confirmInput.addEventListener('input', function () {
            delBtn.disabled = confirmInput.value.trim().toUpperCase() !== 'SIL';
        });
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') { closeDelAcc(); closeAcc(); }
    });

    renderSessions();
})();
