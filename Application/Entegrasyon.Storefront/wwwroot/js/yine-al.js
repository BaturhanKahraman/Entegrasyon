/* Zekids — Yine Al: filtre chip + 12 sample item grid render. */
(function () {
    'use strict';

    var IMG = function (id, w) { return 'https://images.unsplash.com/photo-' + id + '?auto=format&fit=crop&w=' + (w || 500) + '&q=80'; };
    function toast(m) { if (window.toast) window.toast(m); }

    var ITEMS = [
        { id: 1,  name: 'Fırfırlı Yazlık Tunik',            brand: 'Zekids',   price: 199, old: null, size: '3-4 yaş',   last: 'Mayıs 2026',   times: 2, filters: ['season', 'samesize'],   stock: true,  tint: 'bg-accent/25',    img: '1604482858862-1db908a653e4' },
        { id: 2,  name: 'Tavşan Desenli Pijama Takımı',     brand: 'Uykucu',   price: 259, old: 319,  size: '5-6 yaş',   last: 'Nisan 2026',   times: 3, filters: ['samesize', 'restock'],  stock: true,  tint: 'bg-primary/15',   img: '1622290319146-7b63df48a635' },
        { id: 3,  name: 'Salopet Kot Tulum',                brand: 'Pamuk',    price: 389, old: null, size: '2-3 yaş',   last: 'Mart 2026',    times: 1, filters: ['season'],                stock: true,  tint: 'bg-secondary/20', img: '1632337950445-ba446cb0e26f' },
        { id: 4,  name: 'Çiçekli Saç Bandı Seti',           brand: 'Mavi Kids', price: 89, old: 129,  size: 'Standart',  last: 'Mart 2026',    times: 4, filters: ['samesize', 'restock'],  stock: true,  tint: 'bg-primary/20',   img: '1560506840-ec148e82a604' },
        { id: 5,  name: 'Pamuklu Body 3\'lü Paket',         brand: 'Zekids',   price: 229, old: null, size: '6-9 ay',    last: 'Şubat 2026',   times: 2, filters: ['season'],                stock: true,  tint: 'bg-secondary/15', img: '1566454544259-f4b94c3d758c' },
        { id: 6,  name: 'Kareli Bahçıvan Şort',             brand: 'Pamuk',    price: 169, old: 219,  size: '4-5 yaş',   last: 'Haziran 2025', times: 1, filters: ['season', 'restock'],    stock: true,  tint: 'bg-accent/20',    img: '1620774760711-caa4c94d683a' },
        { id: 7,  name: 'Yıldız Desenli Sweat',             brand: 'Mavi Kids', price: 279, old: null, size: '7-8 yaş',   last: 'Ocak 2026',    times: 2, filters: ['samesize'],              stock: false, tint: 'bg-primary/15',   img: '1622290291468-a28f7a7dc6a8' },
        { id: 8,  name: 'Hastane Çıkışı 5\'li Set',         brand: 'Zekids',   price: 449, old: 549,  size: 'Yeni doğan', last: 'Aralık 2025',  times: 1, filters: ['restock'],               stock: true,  tint: 'bg-secondary/20', img: '1590995385808-7eb142fde16a' },
        { id: 9,  name: 'Çizgili Denizci Tişört',           brand: 'Pamuk',    price: 149, old: null, size: '3-4 yaş',   last: 'Nisan 2026',   times: 3, filters: ['season', 'samesize'],   stock: true,  tint: 'bg-secondary/15', img: '1519238263530-99bdd11df2ea' },
        { id: 10, name: 'Yumuşak Polar Tulum',              brand: 'Uykucu',   price: 329, old: 399,  size: '12-18 ay',  last: 'Kasım 2025',   times: 2, filters: ['restock'],               stock: true,  tint: 'bg-primary/15',   img: '1518831959646-742c3a14ebf7' },
        { id: 11, name: 'Çiçek Baskılı Yazlık Elbise',      brand: 'Zekids',   price: 219, old: null, size: '5-6 yaş',   last: 'Mayıs 2026',   times: 1, filters: ['season'],                stock: true,  tint: 'bg-accent/25',    img: '1503454537195-1dcabb73ffb9' },
        { id: 12, name: 'Çorap 6\'lı Paket',                brand: 'Mavi Kids', price: 99, old: 139,  size: '2-4 yaş',   last: 'Mart 2026',    times: 5, filters: ['samesize', 'restock'],  stock: true,  tint: 'bg-secondary/20', img: '1515488042361-ee00e0ddd4e4' },
    ];

    var FILTERS = [
        { key: 'all',      label: 'Tümü' },
        { key: 'season',   label: 'Bu sezon' },
        { key: 'samesize', label: 'Bedeni değişmedi' },
        { key: 'restock',  label: 'Stoğa düştü' },
    ];
    var activeFilter = 'all';

    function countFor(key) {
        return key === 'all' ? ITEMS.length : ITEMS.filter(function (p) { return p.filters.indexOf(key) >= 0; }).length;
    }

    function renderChips() {
        var html = FILTERS.map(function (f) {
            var on = f.key === activeFilter;
            var btnCls = on ? 'chip-active' : 'border-cream-300 text-charcoal hover:border-primary hover:text-primary';
            var countCls = on ? '' : 'bg-cream text-muted';
            return '<button type="button" data-filter="' + f.key + '" class="chip flex items-center gap-2 px-4 py-2 rounded-full border text-sm font-medium transition ' + btnCls + '">' +
                f.label +
                ' <span class="chip-count text-[11px] font-semibold px-1.5 py-0.5 rounded-full ' + countCls + '">' + countFor(f.key) + '</span>' +
                '</button>';
        }).join('');
        document.getElementById('chips').innerHTML = html;
        document.querySelectorAll('[data-filter]').forEach(function (b) {
            b.addEventListener('click', function () { setFilter(b.dataset.filter); });
        });
    }

    function buyAgainCard(p) {
        var pct = p.old ? Math.round((1 - p.price / p.old) * 100) : null;
        var dpct = pct ? '<span class="px-2.5 py-1 rounded-full bg-success text-white text-[11px] font-semibold">%' + pct + '</span>' : '';
        var restock = p.filters.indexOf('restock') >= 0;
        var soldOut = !p.stock;
        var topBadge = soldOut
            ? '<span class="px-2.5 py-1 rounded-full bg-charcoal text-white text-[11px] font-semibold">Tükendi</span>'
            : (restock ? '<span class="px-2.5 py-1 rounded-full bg-secondary text-white text-[11px] font-semibold">Tekrar stokta</span>' : dpct);
        var sameSize = p.filters.indexOf('samesize') >= 0;
        var sizeChip = sameSize
            ? '<span class="inline-flex items-center gap-1 text-success font-medium"><svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 6 9 17l-5-5"/></svg>' + p.size + '</span>'
            : '<span class="inline-block px-2 py-0.5 rounded-md bg-cream text-muted">' + p.size + '</span>';
        var oldHtml = p.old ? '<span class="text-sm text-muted line-through">' + p.old + '₺</span>' : '';
        var timesBadge = p.times > 1
            ? '<span class="absolute top-3 right-3 px-2 py-1 rounded-full bg-white/90 backdrop-blur text-[11px] font-semibold text-charcoal shadow-sm">' + p.times + '× aldınız</span>'
            : '';
        var cta = soldOut
            ? '<button type="button" disabled class="mt-3 w-full inline-flex items-center justify-center gap-1.5 py-2.5 rounded-full bg-cream text-muted text-sm font-semibold cursor-not-allowed">Stok bekleniyor</button>'
            : '<button type="button" data-reorder="' + p.id + '" class="mt-3 w-full inline-flex items-center justify-center gap-1.5 py-2.5 rounded-full border border-primary text-primary text-sm font-semibold hover:bg-primary hover:text-white transition"><svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m17 2 4 4-4 4"/><path d="M3 11v-1a4 4 0 0 1 4-4h14"/><path d="m7 22-4-4 4-4"/><path d="M21 13v1a4 4 0 0 1-4 4H3"/></svg>Yeniden Sepete Ekle</button>';
        return '<div class="group" data-id="' + p.id + '" data-filters="' + p.filters.join(' ') + '">' +
            '<a href="/urun/' + p.id + '" class="block" aria-label="' + p.name + '">' +
            '<div class="ph relative aspect-[4/5] rounded-2xl ' + p.tint + ' overflow-hidden" data-ph="ürün görseli">' +
            '<img class="photo' + (soldOut ? ' grayscale opacity-70' : '') + '" loading="lazy" alt="' + p.name + '" src="' + IMG(p.img, 500) + '" />' +
            '<div class="absolute top-3 left-3 flex flex-col gap-1.5 items-start">' + topBadge + '</div>' +
            timesBadge +
            '</div></a>' +
            '<div class="mt-3"><p class="text-xs text-muted">' + p.brand + '</p>' +
            '<a href="/urun/' + p.id + '" class="block font-medium text-charcoal line-clamp-2 leading-snug mt-0.5 hover:text-primary transition">' + p.name + '</a>' +
            '<div class="flex flex-wrap items-center gap-x-2 gap-y-1 mt-2 text-[11px]">' +
            '<span class="text-muted">Son sipariş · ' + p.last + '</span>' + sizeChip + '</div>' +
            '<div class="mt-2.5 flex items-baseline gap-2">' + oldHtml + '<span class="text-lg font-semibold text-primary">' + p.price + '₺</span></div>' +
            cta + '</div></div>';
    }

    function renderGrid() {
        var grid = document.getElementById('grid');
        var noMatch = document.getElementById('noMatch');
        var empty = document.getElementById('empty');
        if (ITEMS.length === 0) {
            grid.classList.add('hidden');
            noMatch.classList.add('hidden');
            empty.classList.remove('hidden');
            return;
        }
        empty.classList.add('hidden');
        var list = activeFilter === 'all'
            ? ITEMS
            : ITEMS.filter(function (p) { return p.filters.indexOf(activeFilter) >= 0; });
        if (list.length === 0) {
            grid.classList.add('hidden');
            noMatch.classList.remove('hidden');
            return;
        }
        noMatch.classList.add('hidden');
        grid.classList.remove('hidden');
        grid.innerHTML = list.map(buyAgainCard).join('');
        document.querySelectorAll('[data-reorder]').forEach(function (b) {
            b.addEventListener('click', function () {
                var id = parseInt(b.dataset.reorder, 10);
                var p = ITEMS.find(function (x) { return x.id === id; });
                if (p) toast(p.name + ' (' + p.size + ') sepete eklendi');
            });
        });
    }

    function setFilter(key) {
        activeFilter = key;
        renderChips();
        renderGrid();
    }

    /* Mobil hesap sheet */
    document.querySelectorAll('[data-acc-open]').forEach(function (b) {
        b.addEventListener('click', function () {
            var s = document.getElementById('accSheet');
            if (s) { s.classList.remove('hidden'); document.body.style.overflow = 'hidden'; }
        });
    });
    document.querySelectorAll('[data-acc-close]').forEach(function (b) {
        b.addEventListener('click', function () {
            var s = document.getElementById('accSheet');
            if (s) { s.classList.add('hidden'); document.body.style.overflow = ''; }
        });
    });

    /* Empty state — reset filter */
    document.querySelectorAll('[data-filter-reset]').forEach(function (b) {
        b.addEventListener('click', function () { setFilter('all'); });
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            var s = document.getElementById('accSheet');
            if (s && !s.classList.contains('hidden')) { s.classList.add('hidden'); document.body.style.overflow = ''; }
        }
    });

    /* init */
    renderChips();
    renderGrid();
})();
