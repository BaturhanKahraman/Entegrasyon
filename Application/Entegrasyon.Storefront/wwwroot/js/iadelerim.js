/* ============ Zekids Bebe — İadelerim ============ */
(function () {
    'use strict';

    var IMG = function (id, w) { return 'https://images.unsplash.com/photo-' + id + '?auto=format&fit=crop&w=' + (w || 200) + '&q=80'; };
    var FB = function (lock) { return 'https://loremflickr.com/200/250/kids,clothes?lock=' + lock; };
    function notify(msg) { if (window.toast) window.toast(msg); }

    /* ===== mobile account sheet ===== */
    var sheet = document.getElementById('accSheet');
    function openAcc() { if (sheet) { sheet.classList.remove('hidden'); document.body.style.overflow = 'hidden'; } }
    function closeAcc() { if (sheet) { sheet.classList.add('hidden'); document.body.style.overflow = ''; } }
    document.querySelectorAll('[data-acc-open]').forEach(function (b) { b.addEventListener('click', openAcc); });
    document.querySelectorAll('[data-acc-close]').forEach(function (b) { b.addEventListener('click', closeAcc); });

    /* ===== data ===== */
    var IMGS = ['1620774760711-caa4c94d683a', '1604482858862-1db908a653e4', '1622290291468-a28f7a7dc6a8', '1560506840-ec148e82a604', '1622290319146-7b63df48a635', '1566454544259-f4b94c3d758c', '1632337950445-ba446cb0e26f', '1590995385808-7eb142fde16a'];
    function pickImgs(n, seed) { var a = []; for (var i = 0; i < n; i++) a.push(IMGS[(seed + i) % IMGS.length]); return a; }

    var TONE = {
        'Talep Edildi': 'bg-primary/10 text-primary',
        'Onaylandı': 'bg-secondary/15 text-secondary',
        'Kargoda': 'bg-accent/25 text-warning',
        'Tamamlandı': 'bg-success/15 text-success',
        'Reddedildi': 'bg-danger/10 text-danger'
    };

    var RETURNS = [
        { id: 'IADE-2026-0042', order: 'ZB-2026-1234', date: '27 Mayıs 2026', status: 'Talep Edildi', count: 2, amount: '498₺', reason: 'Bedeni uymadı', imgs: pickImgs(2, 0) },
        { id: 'IADE-2026-0031', order: 'ZB-2026-1187', date: '12 Mayıs 2026', status: 'Kargoda', count: 1, amount: '249₺', reason: 'Beklediğim gibi değil', imgs: pickImgs(1, 3) },
        { id: 'IADE-2026-0019', order: 'ZB-2026-1142', date: '24 Nisan 2026', status: 'Tamamlandı', count: 3, amount: '687₺', reason: 'Hasarlı geldi', imgs: pickImgs(3, 5) }
    ];

    var FILTERS = [
        { key: 'Tümü', match: null },
        { key: 'Talep', match: 'Talep Edildi' },
        { key: 'Onaylandı', match: 'Onaylandı' },
        { key: 'Kargoda', match: 'Kargoda' },
        { key: 'Tamamlandı', match: 'Tamamlandı' },
        { key: 'Reddedildi', match: 'Reddedildi' }
    ];
    var activeFilter = 'Tümü';

    var filtersEl = document.querySelector('[data-return-filters]');
    var listEl = document.querySelector('[data-return-list]');
    var emptyEl = document.querySelector('[data-returns-empty]');

    function renderFilters() {
        if (!filtersEl) return;
        filtersEl.innerHTML = FILTERS.map(function (f) {
            var c = f.match === null ? RETURNS.length : RETURNS.filter(function (r) { return r.status === f.match; }).length;
            var on = activeFilter === f.key;
            return '<button type="button" data-filter="' + f.key + '" class="shrink-0 px-4 py-2 rounded-full border text-sm font-medium transition ' + (on ? 'bg-primary text-white border-primary' : 'border-cream-300 text-charcoal hover:border-primary') + '">' + f.key + ' <span class="' + (on ? 'text-white/80' : 'text-muted') + '">(' + c + ')</span></button>';
        }).join('');
        filtersEl.querySelectorAll('[data-filter]').forEach(function (btn) {
            btn.addEventListener('click', function () { setFilter(btn.dataset.filter); });
        });
    }

    function returnCard(r) {
        var thumbs = r.imgs.slice(0, 4).map(function (id, i) {
            return '<div class="ph w-16 h-16 rounded-xl overflow-hidden bg-cream shrink-0"><img class="photo" loading="lazy" alt="" src="' + IMG(id, 120) + '" onerror="this.onerror=null;this.src=\'' + FB(200 + i) + '\'" /></div>';
        }).join('');
        var overflow = (r.count > 4) ? '<div class="w-16 h-16 rounded-xl bg-cream flex items-center justify-center text-sm font-medium text-muted shrink-0">+' + (r.count - 4) + '</div>' : '';
        return '<div class="bg-white border border-cream-300 rounded-2xl p-5 sm:p-6">' +
            '<div class="flex items-center justify-between gap-3 mb-4">' +
            '<div><p class="font-mono text-sm font-semibold text-charcoal">#' + r.id + '</p>' +
            '<p class="text-xs text-muted mt-0.5">Talep: ' + r.date + ' · Sipariş #' + r.order + '</p></div>' +
            '<span class="text-xs font-medium px-2.5 py-1 rounded-full ' + (TONE[r.status] || TONE['Talep Edildi']) + ' shrink-0">' + r.status + '</span>' +
            '</div>' +
            '<div class="flex gap-2.5 overflow-x-auto no-scrollbar mb-4">' + thumbs + overflow + '</div>' +
            '<div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pt-1">' +
            '<div><p class="text-sm font-medium text-charcoal">' + r.count + ' ürün · İade tutarı <span class="text-primary font-semibold">' + r.amount + '</span></p>' +
            '<p class="text-xs text-muted mt-0.5">Sebep: ' + r.reason + '</p></div>' +
            '<button type="button" data-toast="İade detayı açılıyor" class="self-start sm:self-auto px-4 py-2 rounded-full border border-primary text-primary text-sm font-semibold hover:bg-primary/5 transition">Detay</button>' +
            '</div></div>';
    }

    function renderReturns() {
        if (!listEl) return;
        var f = FILTERS.find(function (x) { return x.key === activeFilter; });
        var list = f.match === null ? RETURNS : RETURNS.filter(function (r) { return r.status === f.match; });
        if (list.length === 0) {
            listEl.innerHTML = '';
            if (emptyEl) { emptyEl.classList.remove('hidden'); emptyEl.classList.add('grid'); }
            return;
        }
        if (emptyEl) { emptyEl.classList.add('hidden'); emptyEl.classList.remove('grid'); }
        listEl.innerHTML = list.map(returnCard).join('');
        listEl.querySelectorAll('[data-toast]').forEach(function (b) {
            b.addEventListener('click', function () { notify(b.dataset.toast); });
        });
    }

    function setFilter(k) { activeFilter = k; renderFilters(); renderReturns(); }

    /* ===== init ===== */
    renderFilters();
    renderReturns();
})();
