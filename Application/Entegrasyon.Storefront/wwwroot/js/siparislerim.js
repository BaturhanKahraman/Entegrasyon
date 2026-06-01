/* ============ Zekids Bebe — Siparişlerim ============ */
(function () {
    'use strict';

    var IMG = function (id, w) { return 'https://images.unsplash.com/photo-' + id + '?auto=format&fit=crop&w=' + (w || 200) + '&q=80'; };
    var FB = function (s) { return 'https://loremflickr.com/200/250/' + s; };
    function notify(msg) { if (window.toast) window.toast(msg); }

    /* ===== mobile account sheet ===== */
    var sheet = document.getElementById('accSheet');
    function openAcc() { if (sheet) { sheet.classList.remove('hidden'); document.body.style.overflow = 'hidden'; } }
    function closeAcc() { if (sheet) { sheet.classList.add('hidden'); document.body.style.overflow = ''; } }
    document.querySelectorAll('[data-acc-open]').forEach(function (b) { b.addEventListener('click', openAcc); });
    document.querySelectorAll('[data-acc-close]').forEach(function (b) { b.addEventListener('click', closeAcc); });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') closeAcc(); });

    /* ===== generic toast triggers (pager) ===== */
    document.querySelectorAll('[data-toast]').forEach(function (b) {
        b.addEventListener('click', function () { notify(b.dataset.toast); });
    });

    /* ===== orders data ===== */
    var IMGS = ['1620774760711-caa4c94d683a', '1604482858862-1db908a653e4', '1622290291468-a28f7a7dc6a8', '1560506840-ec148e82a604', '1622290319146-7b63df48a635', '1566454544259-f4b94c3d758c', '1632337950445-ba446cb0e26f', '1590995385808-7eb142fde16a'];
    function pickImgs(n, seed) { var a = []; for (var i = 0; i < n; i++) a.push(IMGS[(seed + i) % IMGS.length]); return a; }
    var TONE = { 'Beklemede': 'bg-primary/10 text-primary', 'Hazırlanıyor': 'bg-secondary/15 text-secondary', 'Kargoda': 'bg-accent/25 text-warning', 'Teslim Edildi': 'bg-success/15 text-success', 'İptal': 'bg-danger/10 text-danger' };
    var ORDERS = [
        { no: 'ZB-2026-1234', date: '23 Mayıs 2026', status: 'Kargoda', count: 5, total: '1.247₺', imgs: pickImgs(5, 0) },
        { no: 'ZB-2026-1221', date: '20 Mayıs 2026', status: 'Hazırlanıyor', count: 3, total: '687₺', imgs: pickImgs(3, 2) },
        { no: 'ZB-2026-1210', date: '18 Mayıs 2026', status: 'Hazırlanıyor', count: 2, total: '398₺', imgs: pickImgs(2, 4) },
        { no: 'ZB-2026-1199', date: '14 Mayıs 2026', status: 'Beklemede', count: 1, total: '249₺', imgs: pickImgs(1, 1) },
        { no: 'ZB-2026-1187', date: '8 Mayıs 2026', status: 'Teslim Edildi', count: 4, total: '912₺', imgs: pickImgs(4, 3) },
        { no: 'ZB-2026-1170', date: '2 Mayıs 2026', status: 'Teslim Edildi', count: 2, total: '458₺', imgs: pickImgs(2, 5) },
        { no: 'ZB-2026-1155', date: '27 Nisan 2026', status: 'Teslim Edildi', count: 6, total: '1.534₺', imgs: pickImgs(6, 0) },
        { no: 'ZB-2026-1142', date: '21 Nisan 2026', status: 'Teslim Edildi', count: 1, total: '199₺', imgs: pickImgs(1, 6) },
        { no: 'ZB-2026-1133', date: '15 Nisan 2026', status: 'İptal', count: 2, total: '378₺', imgs: pickImgs(2, 2) },
        { no: 'ZB-2026-1120', date: '9 Nisan 2026', status: 'Teslim Edildi', count: 3, total: '624₺', imgs: pickImgs(3, 4) },
        { no: 'ZB-2026-1108', date: '3 Nisan 2026', status: 'Teslim Edildi', count: 2, total: '349₺', imgs: pickImgs(2, 1) },
        { no: 'ZB-2026-1095', date: '28 Mart 2026', status: 'Teslim Edildi', count: 4, total: '876₺', imgs: pickImgs(4, 5) }
    ];
    var FILTERS = ['Tümü', 'Beklemede', 'Hazırlanıyor', 'Kargoda', 'Teslim Edildi', 'İptal'];
    var activeFilter = 'Tümü';
    var searchQ = '';

    function renderFilters() {
        var el = document.getElementById('orderFilters');
        if (!el) return;
        el.innerHTML = FILTERS.map(function (f) {
            var c = f === 'Tümü' ? ORDERS.length : ORDERS.filter(function (o) { return o.status === f; }).length;
            var on = activeFilter === f;
            return '<button type="button" data-filter="' + f + '" class="shrink-0 px-4 py-2 rounded-full border text-sm font-medium transition ' + (on ? 'bg-primary text-white border-primary' : 'border-cream-300 text-charcoal hover:border-primary') + '">' + f + ' <span class="' + (on ? 'text-white/80' : 'text-muted') + '">(' + c + ')</span></button>';
        }).join('');
        el.querySelectorAll('[data-filter]').forEach(function (btn) {
            btn.addEventListener('click', function () { setFilter(btn.dataset.filter); });
        });
    }

    function orderCard(o) {
        var thumbs = o.imgs.slice(0, 4).map(function (id, i) {
            return '<div class="ph w-16 h-16 rounded-xl overflow-hidden bg-cream shrink-0"><img class="photo" loading="lazy" alt="" src="' + IMG(id, 120) + '" onerror="this.onerror=null;this.src=\'' + FB('kids,clothes?lock=' + (170 + i)) + '\'" /></div>';
        }).join('');
        var overflow = (o.count > 4) ? '<div class="w-16 h-16 rounded-xl bg-cream flex items-center justify-center text-sm font-medium text-muted shrink-0">+' + (o.count - 4) + '</div>' : '';
        var actions = '<a href="/hesabim/siparis/' + o.no + '" class="px-4 py-2 rounded-full border border-primary text-primary text-sm font-semibold hover:bg-primary/5 transition">Detay</a>';
        if (o.status === 'Kargoda') {
            actions = '<button type="button" data-toast="Kargo takibi açılıyor" class="text-sm font-medium text-primary hover:underline">Takip Et</button>' + actions;
        }
        if (o.status === 'Teslim Edildi') {
            actions = '<button type="button" data-toast="Sepete eklendi" class="text-sm font-medium text-primary hover:underline">Yine Al</button>' +
                '<button type="button" data-toast="İade talebi başlatılıyor" class="text-sm font-medium text-muted hover:text-charcoal hover:underline">İade Aç</button>' + actions;
        }
        return '<div class="bg-white border border-cream-300 rounded-2xl p-5 sm:p-6">' +
            '<div class="flex items-center justify-between gap-3 mb-4">' +
            '<div><p class="font-mono text-sm font-semibold text-charcoal">#' + o.no + '</p><p class="text-xs text-muted mt-0.5">' + o.date + '</p></div>' +
            '<span class="text-xs font-medium px-2.5 py-1 rounded-full ' + (TONE[o.status] || TONE['Beklemede']) + '">' + o.status + '</span>' +
            '</div>' +
            '<div class="flex gap-2.5 overflow-x-auto no-scrollbar mb-4">' + thumbs + overflow + '</div>' +
            '<div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pt-1">' +
            '<p class="text-sm font-medium text-charcoal">' + o.count + ' ürün · ' + o.total + '</p>' +
            '<div class="flex items-center gap-3 flex-wrap">' + actions + '</div>' +
            '</div></div>';
    }

    function renderOrders() {
        var list = ORDERS.filter(function (o) { return activeFilter === 'Tümü' || o.status === activeFilter; });
        if (searchQ) list = list.filter(function (o) { return o.no.toLowerCase().indexOf(searchQ.toLowerCase()) !== -1; });
        var el = document.getElementById('orderList');
        var empty = document.getElementById('ordersEmpty');
        var pager = document.getElementById('ordersPager');
        if (!el) return;
        if (list.length === 0) {
            el.innerHTML = '';
            if (empty) empty.classList.remove('hidden');
            if (pager) pager.classList.add('hidden');
            return;
        }
        if (empty) empty.classList.add('hidden');
        if (pager) pager.classList.toggle('hidden', activeFilter !== 'Tümü' || !!searchQ);
        el.innerHTML = list.map(orderCard).join('');
        el.querySelectorAll('[data-toast]').forEach(function (b) {
            b.addEventListener('click', function () { notify(b.dataset.toast); });
        });
    }

    function setFilter(f) { activeFilter = f; renderFilters(); renderOrders(); }

    var searchInput = document.querySelector('[data-order-search]');
    if (searchInput) {
        searchInput.addEventListener('input', function () { searchQ = this.value; renderOrders(); });
    }

    /* ===== init ===== */
    renderFilters();
    renderOrders();
})();
