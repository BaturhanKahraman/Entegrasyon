/* Zekids — Profil Bilgileri sayfası etkileşimleri.
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

    /* ---- telefon mask (0(5XX) XXX XX XX) ---- */
    document.querySelectorAll('[data-mask-phone]').forEach(function (el) {
        el.addEventListener('input', function () {
            var d = el.value.replace(/\D/g, '').slice(0, 11);
            if (d && d[0] !== '0') d = '0' + d.slice(0, 10);
            var out = '';
            if (d.length > 0) out = '0';
            if (d.length > 1) out += '(' + d.slice(1, 4);
            if (d.length >= 4) out += ') ' + d.slice(4, 7);
            if (d.length >= 7) out += ' ' + d.slice(7, 9);
            if (d.length >= 9) out += ' ' + d.slice(9, 11);
            el.value = out;
        });
    });

    /* ---- switch (görsel) ↔ gizli checkbox senkronu ---- */
    function syncSwitch(sw) {
        var on = sw.dataset.on === '1';
        sw.setAttribute('aria-checked', on ? 'true' : 'false');
        var target = sw.dataset.target && document.getElementById(sw.dataset.target);
        if (target) target.checked = on;
    }
    document.querySelectorAll('[data-toggle-switch]').forEach(function (sw) {
        syncSwitch(sw);
        function toggle() { sw.dataset.on = sw.dataset.on === '1' ? '0' : '1'; syncSwitch(sw); }
        sw.addEventListener('click', toggle);
        sw.addEventListener('keydown', function (e) {
            if (e.key === ' ' || e.key === 'Enter') { e.preventDefault(); toggle(); }
        });
    });

    /* ---- avatar yükle / önizleme / kaldır ---- */
    var avatarInput = document.querySelector('[data-avatar-input]');
    var avatarBadge = document.getElementById('avatarBadge');
    var avatarTrigger = document.querySelector('[data-avatar-trigger]');
    var avatarRemove = document.querySelector('[data-avatar-remove]');
    var avatarInitials = avatarBadge ? avatarBadge.textContent.trim() : '';
    if (avatarTrigger && avatarInput) {
        avatarTrigger.addEventListener('click', function () { avatarInput.click(); });
        avatarInput.addEventListener('change', function () {
            var file = avatarInput.files && avatarInput.files[0];
            if (!file) return;
            var url = URL.createObjectURL(file);
            avatarBadge.innerHTML = '<img src="' + url + '" alt="Profil fotoğrafı" class="w-full h-full object-cover" />';
            toast('Fotoğraf seçildi, kaydetmeyi unutmayın');
        });
    }
    if (avatarRemove && avatarBadge) {
        avatarRemove.addEventListener('click', function () {
            if (avatarInput) avatarInput.value = '';
            avatarBadge.textContent = avatarInitials;
            toast('Fotoğraf kaldırıldı');
        });
    }

    /* ---- çocuklar (demo veri; ileride server-side) ---- */
    var KIDS = [
        { id: 1, name: 'Selin', birth: '2022-03-10', gender: 'k' },
        { id: 2, name: 'Ege', birth: '2025-01-22', gender: 'e' },
    ];
    var editingKid = null;
    var grid = document.getElementById('kidsGrid');
    var modal = document.getElementById('childModal');
    var titleEl = document.getElementById('childModalTitle');
    var nameInput = document.getElementById('cf_name');
    var birthInput = document.getElementById('cf_birth');

    function ageFrom(birth) {
        if (!birth) return 0;
        var b = new Date(birth), n = new Date();
        var a = n.getFullYear() - b.getFullYear();
        var m = n.getMonth() - b.getMonth();
        if (m < 0 || (m === 0 && n.getDate() < b.getDate())) a--;
        return Math.max(0, a);
    }
    function esc(s) { var d = document.createElement('div'); d.textContent = s; return d.innerHTML; }

    function renderKids() {
        if (!grid) return;
        grid.innerHTML = KIDS.map(function (k) {
            var tint = k.gender === 'k' ? 'bg-primary/15 text-primary' : 'bg-secondary/25 text-secondary';
            return '<div class="flex items-center gap-3 bg-white rounded-2xl border border-cream-300 p-4">'
                + '<span class="w-11 h-11 rounded-full ' + tint + ' flex items-center justify-center font-semibold shrink-0">' + esc(k.name.charAt(0)) + '</span>'
                + '<div class="min-w-0 flex-1">'
                + '<p class="font-medium text-charcoal truncate">' + esc(k.name) + '</p>'
                + '<span class="inline-block mt-1 text-[11px] font-medium bg-cream text-charcoal/70 px-2 py-0.5 rounded-full">' + ageFrom(k.birth) + ' yaş</span>'
                + '</div>'
                + '<div class="flex items-center gap-3 text-sm shrink-0">'
                + '<button type="button" data-child-edit="' + k.id + '" class="font-medium text-primary hover:underline">Düzenle</button>'
                + '<button type="button" data-child-delete="' + k.id + '" class="font-medium text-danger hover:underline">Sil</button>'
                + '</div></div>';
        }).join('');
    }

    function openChild() {
        editingKid = null;
        if (titleEl) titleEl.textContent = 'Çocuk Ekle';
        if (nameInput) nameInput.value = '';
        if (birthInput) birthInput.value = '';
        var k = document.querySelector('input[name="cf_gender"][value="k"]'); if (k) k.checked = true;
        if (modal) { modal.classList.remove('hidden'); document.body.style.overflow = 'hidden'; }
    }
    function editKid(id) {
        var kid = KIDS.find(function (x) { return x.id === id; });
        if (!kid) return;
        editingKid = id;
        if (titleEl) titleEl.textContent = 'Çocuğu Düzenle';
        if (nameInput) nameInput.value = kid.name;
        if (birthInput) birthInput.value = kid.birth;
        var g = document.querySelector('input[name="cf_gender"][value="' + kid.gender + '"]'); if (g) g.checked = true;
        if (modal) { modal.classList.remove('hidden'); document.body.style.overflow = 'hidden'; }
    }
    function closeChild() { if (modal) { modal.classList.add('hidden'); document.body.style.overflow = ''; } }
    function saveChild() {
        var name = (nameInput && nameInput.value.trim()) || '';
        if (!name) { toast('Lütfen çocuğunuzun adını girin'); return; }
        var birth = birthInput ? birthInput.value : '';
        var genderEl = document.querySelector('input[name="cf_gender"]:checked');
        var gender = genderEl ? genderEl.value : 'k';
        if (editingKid) {
            Object.assign(KIDS.find(function (x) { return x.id === editingKid; }), { name: name, birth: birth, gender: gender });
            toast('Çocuk bilgisi güncellendi');
        } else {
            KIDS.push({ id: Date.now(), name: name, birth: birth, gender: gender });
            toast(name + ' eklendi');
        }
        closeChild();
        renderKids();
    }
    function deleteKid(id) { KIDS = KIDS.filter(function (k) { return k.id !== id; }); renderKids(); toast('Çocuk kaldırıldı'); }

    document.querySelectorAll('[data-child-add]').forEach(function (b) { b.addEventListener('click', openChild); });
    document.querySelectorAll('[data-close-child]').forEach(function (b) { b.addEventListener('click', closeChild); });
    document.querySelectorAll('[data-child-save]').forEach(function (b) { b.addEventListener('click', saveChild); });
    if (grid) {
        grid.addEventListener('click', function (e) {
            var ed = e.target.closest('[data-child-edit]');
            var dl = e.target.closest('[data-child-delete]');
            if (ed) editKid(parseInt(ed.dataset.childEdit, 10));
            else if (dl) deleteKid(parseInt(dl.dataset.childDelete, 10));
        });
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') { closeChild(); closeAcc(); }
    });

    renderKids();
})();
