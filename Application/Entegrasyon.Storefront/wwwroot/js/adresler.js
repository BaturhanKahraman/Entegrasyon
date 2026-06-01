/* ============ Zekids Bebe — Adreslerim ============ */
(function () {
    'use strict';

    /* ===== mobile account sheet ===== */
    var accSheet = document.getElementById('accSheet');
    function openAcc() { if (accSheet) { accSheet.classList.remove('hidden'); document.body.style.overflow = 'hidden'; } }
    function closeAcc() { if (accSheet) { accSheet.classList.add('hidden'); document.body.style.overflow = ''; } }
    document.querySelectorAll('[data-acc-open]').forEach(function (b) { b.addEventListener('click', openAcc); });
    document.querySelectorAll('[data-acc-close]').forEach(function (b) { b.addEventListener('click', closeAcc); });

    /* ===== address modal ===== */
    var modal = document.getElementById('addrModal');
    var modalTitle = document.getElementById('addrModalTitle');
    function openModal() { if (modal) { modal.classList.remove('hidden'); document.body.style.overflow = 'hidden'; } }
    function closeModal() { if (modal) { modal.classList.add('hidden'); document.body.style.overflow = ''; } }

    function val(id) { var el = document.getElementById(id); return el ? el : null; }
    function resetForm() {
        ['af_id', 'af_label', 'af_name', 'af_phone', 'af_zip', 'af_mahalle', 'af_full'].forEach(function (id) {
            var el = val(id); if (el) el.value = '';
        });
        var il = val('af_il'); if (il) il.value = '';
        fillIlce();
        var def = val('af_default'); if (def) def.checked = false;
    }

    function openNew() {
        if (modalTitle) modalTitle.textContent = 'Yeni Adres';
        resetForm();
        openModal();
    }

    function ensureOption(select, value) {
        if (!select || !value) return;
        var exists = Array.prototype.some.call(select.options, function (o) { return o.value === value; });
        if (!exists) {
            var opt = document.createElement('option');
            opt.value = value; opt.textContent = value;
            select.appendChild(opt);
        }
        select.value = value;
    }

    function openEdit(card) {
        if (modalTitle) modalTitle.textContent = 'Adresi Düzenle';
        resetForm();
        var idEl = val('af_id'); if (idEl) idEl.value = card.dataset.addrId || '';
        var labelEl = val('af_label'); if (labelEl) labelEl.value = card.dataset.addrLabel || '';
        var nameEl = val('af_name'); if (nameEl) nameEl.value = card.dataset.addrName || '';
        var phoneEl = val('af_phone'); if (phoneEl) phoneEl.value = card.dataset.addrPhone || '';
        var zipEl = val('af_zip'); if (zipEl) zipEl.value = card.dataset.addrPostalcode || '';
        var mahalleEl = val('af_mahalle'); if (mahalleEl) mahalleEl.value = card.dataset.addrNeighborhood || '';
        var fullEl = val('af_full'); if (fullEl) fullEl.value = card.dataset.addrLine || '';
        // İl/ilçe demo listesinde olmayan kayıtlı değerleri seçenek olarak ekle (düzenlemede veri kaybını önler)
        ensureOption(val('af_il'), card.dataset.addrCity || '');
        fillIlce();
        ensureOption(val('af_ilce'), card.dataset.addrDistrict || '');
        var defEl = val('af_default'); if (defEl) defEl.checked = card.dataset.addrDefault === '1';
        openModal();
    }

    document.querySelectorAll('[data-addr-open]').forEach(function (b) { b.addEventListener('click', openNew); });
    document.querySelectorAll('[data-addr-close]').forEach(function (b) { b.addEventListener('click', closeModal); });

    /* ===== ESC closes overlays ===== */
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') { closeAcc(); closeModal(); }
    });

    /* ===== masks ===== */
    document.querySelectorAll('[data-mask-digits]').forEach(function (el) {
        var len = parseInt(el.dataset.maskDigits, 10) || 10;
        el.addEventListener('input', function () {
            el.value = el.value.replace(/\D/g, '').slice(0, len);
        });
    });
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

    /* ===== il / ilçe ===== */
    var IL_ILCE = {
        'İstanbul': ['Kadıköy', 'Beşiktaş', 'Maltepe', 'Ataşehir', 'Üsküdar', 'Bakırköy'],
        'Ankara': ['Çankaya', 'Keçiören', 'Yenimahalle', 'Mamak'],
        'İzmir': ['Konak', 'Karşıyaka', 'Bornova', 'Buca'],
        'Bursa': ['Nilüfer', 'Osmangazi', 'Yıldırım'],
    };
    function fillIlSelect() {
        var il = val('af_il'); if (!il) return;
        il.innerHTML = '<option value="">İl seçin</option>' + Object.keys(IL_ILCE).map(function (c) {
            return '<option value="' + c + '">' + c + '</option>';
        }).join('');
    }
    function fillIlce() {
        var ilEl = val('af_il'); var ilceEl = val('af_ilce');
        if (!ilceEl) return;
        var il = ilEl ? ilEl.value : '';
        ilceEl.innerHTML = '<option value="">İlçe seçin</option>' + (IL_ILCE[il] || []).map(function (d) {
            return '<option value="' + d + '">' + d + '</option>';
        }).join('');
    }
    document.querySelectorAll('[data-il-select]').forEach(function (el) { el.addEventListener('change', fillIlce); });

    /* ===== card actions (sample, client-side) ===== */
    function bindCard(card) {
        var editBtn = card.querySelector('[data-addr-edit]');
        if (editBtn) editBtn.addEventListener('click', function () { openEdit(card); });

        var delBtn = card.querySelector('[data-addr-delete]');
        if (delBtn) delBtn.addEventListener('click', function () {
            card.remove();
            if (window.toast) window.toast('Adres silindi');
        });

        var defBtn = card.querySelector('[data-addr-default-action]');
        if (defBtn) defBtn.addEventListener('click', function () { makeDefault(card); });
    }

    function makeDefault(target) {
        document.querySelectorAll('[data-addr-card]').forEach(function (card) {
            var isTarget = card === target;
            card.dataset.addrDefault = isTarget ? '1' : '0';
            var badge = card.querySelector('[data-default-badge]');
            var actionRow = card.querySelector('.justify-end');
            var defAction = card.querySelector('[data-addr-default-action]');

            if (isTarget) {
                if (!badge) {
                    var titleRow = card.querySelector('.items-center.gap-2');
                    if (titleRow) {
                        var span = document.createElement('span');
                        span.setAttribute('data-default-badge', '');
                        span.className = 'text-[11px] font-medium bg-primary/10 text-primary px-2.5 py-0.5 rounded-full';
                        span.textContent = 'Varsayılan';
                        titleRow.appendChild(span);
                    }
                }
                if (defAction) defAction.remove();
            } else {
                if (badge) badge.remove();
                if (!defAction && actionRow) {
                    var btn = document.createElement('button');
                    btn.type = 'button';
                    btn.setAttribute('data-addr-default-action', '');
                    btn.className = 'font-medium text-charcoal hover:text-primary transition';
                    btn.textContent = 'Varsayılan Yap';
                    actionRow.insertBefore(btn, actionRow.firstChild);
                    btn.addEventListener('click', function () { makeDefault(card); });
                }
            }
        });
        if (window.toast) window.toast('Varsayılan adres güncellendi');
    }

    document.querySelectorAll('[data-addr-card]').forEach(bindCard);

    /* ===== init ===== */
    fillIlSelect();
    fillIlce();
})();
