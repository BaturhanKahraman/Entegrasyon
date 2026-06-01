/* ============ Zekids Bebe — Hesap Özeti (Dashboard) ============ */
(function () {
    'use strict';

    var IMG = function (id, w) { return 'https://images.unsplash.com/photo-' + id + '?auto=format&fit=crop&w=' + (w || 500) + '&q=80'; };
    var FB = function (s) { return 'https://loremflickr.com/600/750/' + s; };
    function notify(msg) { if (window.toast) window.toast(msg); }

    /* ===== mobile account sheet ===== */
    var sheet = document.getElementById('accSheet');
    function openAcc() { if (sheet) { sheet.classList.remove('hidden'); document.body.style.overflow = 'hidden'; } }
    function closeAcc() { if (sheet) { sheet.classList.add('hidden'); document.body.style.overflow = ''; } }
    document.querySelectorAll('[data-acc-open]').forEach(function (b) { b.addEventListener('click', openAcc); });
    document.querySelectorAll('[data-acc-close]').forEach(function (b) { b.addEventListener('click', closeAcc); });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') closeAcc(); });

    /* ===== coupons ===== */
    var COUPONS = [
        { code: 'BAHAR50', amount: '-50₺', exp: '31 Aralık\'a kadar' },
        { code: 'KARGO0', amount: 'Bedava Kargo', exp: '15 Haziran\'a kadar' },
        { code: 'ILK100', amount: '-100₺', exp: 'İlk siparişe özel' }
    ];
    function renderCoupons() {
        var el = document.getElementById('coupons');
        if (!el) return;
        el.innerHTML = COUPONS.map(function (c) {
            return '<div class="shrink-0 w-56 border-2 border-dashed border-primary/50 rounded-2xl p-4 bg-white hover:scale-[1.02] transition">' +
                '<div class="flex items-center gap-2 text-primary mb-2"><svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M9 5H4a2 2 0 0 0-2 2v3a2 2 0 0 1 0 4v3a2 2 0 0 0 2 2h5"/><path d="M15 5h5a2 2 0 0 1 2 2v3a2 2 0 0 0 0 4v3a2 2 0 0 1-2 2h-5"/><path d="M15 5v14"/></svg><span class="font-mono text-sm font-semibold tracking-wide">' + c.code + '</span></div>' +
                '<p class="text-xl font-semibold text-primary">' + c.amount + '</p>' +
                '<p class="text-xs text-muted mt-1">' + c.exp + '</p>' +
                '<button type="button" data-copy-coupon="' + c.code + '" class="mt-2.5 text-xs font-medium text-charcoal border border-cream-300 rounded-full px-3 py-1 hover:border-primary hover:text-primary transition">Kopyala</button>' +
                '</div>';
        }).join('');
        el.querySelectorAll('[data-copy-coupon]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var code = btn.dataset.copyCoupon;
                if (navigator.clipboard) { navigator.clipboard.writeText(code).catch(function () { }); }
                notify(code + ' kopyalandı');
            });
        });
    }

    /* ===== recent orders ===== */
    var ORDERS = [
        { no: 'ZB-2026-1234', date: '23 Mayıs 2026', status: 'Kargoda', tone: 'accent', total: '946₺', imgs: ['1620774760711-caa4c94d683a', '1604482858862-1db908a653e4', '1622290291468-a28f7a7dc6a8'] },
        { no: 'ZB-2026-1187', date: '8 Mayıs 2026', status: 'Teslim Edildi', tone: 'success', total: '512₺', imgs: ['1560506840-ec148e82a604', '1622290319146-7b63df48a635'] },
        { no: 'ZB-2026-1102', date: '21 Nisan 2026', status: 'Teslim Edildi', tone: 'success', total: '289₺', imgs: ['1566454544259-f4b94c3d758c'] }
    ];
    var TONE = { accent: 'bg-accent/25 text-warning', success: 'bg-success/15 text-success', primary: 'bg-primary/10 text-primary', danger: 'bg-danger/15 text-danger' };
    function statusChip(tone, label) { return '<span class="text-xs font-medium px-2.5 py-1 rounded-full ' + (TONE[tone] || TONE.primary) + '">' + label + '</span>'; }
    function renderOrders() {
        var el = document.getElementById('recentOrders');
        if (!el) return;
        el.innerHTML = ORDERS.map(function (o) {
            var thumbs = o.imgs.map(function (id, i) {
                return '<div class="ph w-12 h-12 rounded-xl overflow-hidden bg-cream ring-2 ring-white"><img class="photo" loading="lazy" alt="" src="' + IMG(id, 100) + '" onerror="this.onerror=null;this.src=\'' + FB('kids,clothes?lock=' + (40 + i)) + '\'" /></div>';
            }).join('');
            return '<div class="flex flex-col sm:flex-row sm:items-center gap-4 px-6 py-4">' +
                '<div class="flex -space-x-3">' + thumbs + '</div>' +
                '<div class="flex-1 min-w-0"><p class="font-medium text-charcoal text-sm">#' + o.no + '</p><p class="text-xs text-muted">' + o.date + '</p></div>' +
                statusChip(o.tone, o.status) +
                '<p class="font-semibold text-charcoal text-sm sm:w-20 sm:text-right">' + o.total + '</p>' +
                '<a href="/hesabim/siparis/' + o.no + '" class="text-sm font-medium text-primary hover:underline whitespace-nowrap">Detay →</a>' +
                '</div>';
        }).join('');
    }

    /* ===== recommendations ===== */
    var REC = [
        { name: 'Fırfırlı Yazlık Tunik', brand: 'Zekids', price: 199, old: null, badge: 'new', tint: 'bg-accent/25', img: '1604482858862-1db908a653e4', fb: 'girl,kid?lock=161' },
        { name: 'Tavşan Desenli Pijama', brand: 'Uykucu', price: 259, old: 319, badge: 'sale', tint: 'bg-primary/15', img: '1622290319146-7b63df48a635', fb: 'baby,clothes?lock=162' },
        { name: 'Salopet Kot Tulum', brand: 'Pamuk', price: 389, old: null, badge: 'new', tint: 'bg-secondary/20', img: '1632337950445-ba446cb0e26f', fb: 'kids,clothes?lock=163' },
        { name: 'Çiçekli Saç Bandı Seti', brand: 'Mavi Kids', price: 89, old: 129, badge: 'sale', tint: 'bg-primary/20', img: '1560506840-ec148e82a604', fb: 'kids,clothes?lock=164' }
    ];
    function badgeHtml(t) {
        if (t === 'new') return '<span class="px-2.5 py-1 rounded-full bg-primary text-white text-[11px] font-semibold">Yeni</span>';
        if (t === 'sale') return '<span class="px-2.5 py-1 rounded-full bg-success text-white text-[11px] font-semibold">İndirim</span>';
        return '';
    }
    function productCard(p) {
        var pct = p.old ? Math.round((1 - p.price / p.old) * 100) : null;
        var dpct = pct ? '<span class="px-2.5 py-1 rounded-full bg-success text-white text-[11px] font-semibold">%' + pct + '</span>' : '';
        return '<div class="group snap-start">' +
            '<a href="/urun" class="block" aria-label="' + p.name + '">' +
            '<div class="ph relative aspect-[4/5] rounded-2xl ' + p.tint + ' overflow-hidden" data-ph="ürün">' +
            '<img class="photo" loading="lazy" alt="' + p.name + '" src="' + IMG(p.img, 500) + '" onerror="this.onerror=null;this.src=\'' + FB(p.fb) + '\'" />' +
            '<div class="absolute top-3 left-3 flex flex-col gap-1.5 items-start">' + badgeHtml(p.badge) + dpct + '</div>' +
            '<button type="button" data-wishlist-toggle data-on="0" aria-label="Favorilere ekle" class="absolute top-3 right-3 w-9 h-9 rounded-full bg-white/90 backdrop-blur flex items-center justify-center text-charcoal hover:text-primary transition shadow-sm"><svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M19 14c1.5-1.5 3-3.2 3-5.5A4.5 4.5 0 0 0 12 5.5 4.5 4.5 0 0 0 2 8.5C2 10.8 3.5 12.5 5 14l7 7Z"/></svg></button>' +
            '</div></a>' +
            '<div class="mt-3"><p class="text-xs text-muted">' + p.brand + '</p>' +
            '<a href="/urun" class="block font-medium text-charcoal line-clamp-2 leading-snug mt-0.5 hover:text-primary transition">' + p.name + '</a>' +
            '<div class="mt-2 flex items-baseline gap-2">' + (p.old ? '<span class="text-sm text-muted line-through">' + p.old + '₺</span>' : '') + '<span class="text-lg font-semibold text-primary">' + p.price + '₺</span></div>' +
            '</div></div>';
    }
    function renderRecommendations() {
        var el = document.getElementById('recScroll');
        if (!el) return;
        el.innerHTML = REC.map(productCard).join('');
        if (window.bindWishlistToggles) window.bindWishlistToggles(el);
    }

    /* ===== init ===== */
    renderCoupons();
    renderOrders();
    renderRecommendations();
})();
