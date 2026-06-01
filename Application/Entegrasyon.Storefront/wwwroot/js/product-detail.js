/* Zekids Bebe — Ürün Detay etkileşimleri. */
(function () {
    'use strict';

    var IMG = function (id, w) { return 'https://images.unsplash.com/photo-' + id + '?auto=format&fit=crop&w=' + (w || 700) + '&q=80'; };

    var G = [
        { id: '1620774760711-caa4c94d683a' },
        { id: '1518831959646-742c3a14ebf7' },
        { id: '1560506840-ec148e82a604' },
        { id: '1604482858862-1db908a653e4' },
        { id: '1590995385808-7eb142fde16a' },
    ];

    var SIZES = [
        { code: '98',  age: '2-3 yaş', stock: 6 },
        { code: '104', age: '3-4 yaş', stock: 12 },
        { code: '110', age: '4-5 yaş', stock: 0 },
        { code: '116', age: '5-6 yaş', stock: 4 },
        { code: '122', age: '6-7 yaş', stock: 9 },
    ];

    var COLORS = [
        { name: 'Pudra Pembe',  hex: '#FFD0DC', img: 0 },
        { name: 'Bebek Mavisi', hex: '#B8E0F0', img: 1 },
        { name: 'Sarı',         hex: '#FFE39E', img: 4 },
    ];

    var SIZE_TABLE = [
        ['86',  '12-18 ay', '80-86',   '11-12'],
        ['92',  '18-24 ay', '86-92',   '12-13'],
        ['98',  '2-3 yaş',  '92-98',   '13-15'],
        ['104', '3-4 yaş',  '98-104',  '15-17'],
        ['110', '4-5 yaş',  '104-110', '17-19'],
        ['116', '5-6 yaş',  '110-116', '19-22'],
        ['122', '6-7 yaş',  '116-122', '22-25'],
    ];

    var REVIEWS = [
        { name: 'Elif K.',  date: '12 Mayıs 2026', rating: 5, verified: true,  text: 'Kumaşı inanılmaz yumuşak, kızım yazın hiç terlemedi. Çiçek deseni fotoğraftakinden bile güzel. Kesinlikle tavsiye ederim.', helpful: 12 },
        { name: 'Merve A.', date: '3 Mayıs 2026',  rating: 5, verified: true,  text: 'Beden tablosuna göre aldım, tam oturdu. Yıkamada hiç solmadı, rengi canlı kaldı.', helpful: 8 },
        { name: 'Zeynep T.', date: '28 Nisan 2026', rating: 4, verified: true,  text: 'Çok şirin bir elbise, tek eksisi biraz dar geldi. Bir beden büyük almanızı öneririm.', helpful: 5 },
        { name: 'Burcu Y.', date: '15 Nisan 2026', rating: 5, verified: false, text: 'Doğum günü için aldım, herkes çok beğendi. Fırfırları çok zarif.', helpful: 3 },
        { name: 'Gamze S.', date: '2 Nisan 2026',  rating: 4, verified: true,  text: 'Kargo gerçekten ertesi gün geldi. Ürün kaliteli, fiyatına değer.', helpful: 2 },
    ];

    var RATING_DIST = [['5', 78], ['4', 15], ['3', 5], ['2', 1], ['1', 1]];

    var SIMILAR = [
        { name: 'Fırfırlı Yazlık Tunik',          brand: 'Zekids',   price: 199, old: null, sizes: '3-7 yaş', badge: 'new',  tint: 'bg-accent/25',    img: '1604482858862-1db908a653e4' },
        { name: 'Çiçekli Fitilli Kadife Elbise',  brand: 'Mavi Kids', price: 449, old: 599,  sizes: '2-6 yaş', badge: 'sale', tint: 'bg-primary/20',   img: '1560506840-ec148e82a604' },
        { name: 'Tavşan Desenli Pijama Takımı',   brand: 'Uykucu',   price: 259, old: 319,  sizes: '2-8 yaş', badge: 'sale', tint: 'bg-primary/15',   img: '1622290319146-7b63df48a635' },
        { name: 'Salopet Kot Tulum',              brand: 'Pamuk',    price: 389, old: null, sizes: '1-5 yaş', badge: 'new',  tint: 'bg-secondary/20', img: '1632337950445-ba446cb0e26f' },
        { name: 'Çizgili Bisiklet Yaka Tişört',   brand: 'Pamuk',    price: 149, old: null, sizes: '4-12 yaş', badge: 'new',  tint: 'bg-secondary/25', img: '1560859259-fcf2b952aed8' },
        { name: 'Kapüşonlu Yumuşak Sweat',        brand: 'Zekids',   price: 279, old: 359,  sizes: '4-10 yaş', badge: 'sale', tint: 'bg-accent/30',    img: '1622290291468-a28f7a7dc6a8' },
        { name: 'Organik Pamuk Body Seti',        brand: 'Minimini', price: 329, old: null, sizes: '0-12 ay', badge: 'best', tint: 'bg-secondary/25', img: '1622290291720-ac961c43ee30' },
        { name: 'Hastane Çıkışı 5\'li Set',       brand: 'Minimini', price: 699, old: 849,  sizes: '0-3 ay',  badge: 'best', tint: 'bg-primary/20',   img: '1632337948797-ba161d29532b' },
    ];

    /* ---- state ---- */
    var activeImg = 0;
    var selectedSize = 1;   // 104
    var selectedColor = 0;
    var qty = 1;

    /* ---- helpers ---- */
    function toast(msg) {
        if (window.toast) window.toast(msg);
    }

    function badgeHtml(type) {
        if (type === 'new') return '<span class="px-2.5 py-1 rounded-full bg-primary text-white text-[11px] font-semibold">Yeni</span>';
        if (type === 'best') return '<span class="px-2.5 py-1 rounded-full bg-accent text-charcoal text-[11px] font-semibold">Çok Satan</span>';
        if (type === 'sale') return '<span class="px-2.5 py-1 rounded-full bg-success text-white text-[11px] font-semibold">İndirim</span>';
        return '';
    }

    function discountPct(p) { return p.old ? Math.round((1 - p.price / p.old) * 100) : null; }

    /* ---- gallery ---- */
    function renderThumbs() {
        var html = G.map(function (g, i) {
            return '<button type="button" data-thumb="' + i + '" aria-label="Görsel ' + (i + 1) + '" class="ph relative aspect-square rounded-xl overflow-hidden bg-cream ring-2 ' + (i === activeImg ? 'ring-primary' : 'ring-transparent') + ' transition">' +
                '<img class="photo" loading="lazy" alt="" src="' + IMG(g.id, 160) + '" />' +
                '</button>';
        }).join('');
        document.getElementById('thumbs').innerHTML = html;
        document.querySelectorAll('[data-thumb]').forEach(function (btn) {
            btn.addEventListener('click', function () { setMainImage(parseInt(btn.dataset.thumb, 10)); });
        });
    }

    function setMainImage(i) {
        activeImg = i;
        var m = document.getElementById('mainImg');
        m.src = IMG(G[i].id, 800);
        var mt = document.getElementById('mobileThumb');
        if (mt) mt.src = IMG(G[i].id, 120);
        renderThumbs();
    }

    /* ---- size chips ---- */
    function renderSizes() {
        var html = SIZES.map(function (s, i) {
            var out = s.stock === 0;
            var active = i === selectedSize && !out;
            var cls = active
                ? 'bg-primary text-white border-primary'
                : (out ? 'border-cream-300 text-muted line-through opacity-40 cursor-not-allowed' : 'border-cream-300 text-charcoal hover:border-primary');
            return '<button type="button" data-size="' + i + '"' + (out ? ' disabled' : '') + ' class="px-4 py-2.5 rounded-xl border text-sm font-medium transition ' + cls + '">' + s.code + ' <span class="opacity-70">(' + s.age + ')</span></button>';
        }).join('');
        document.getElementById('sizeChips').innerHTML = html;
        document.querySelectorAll('[data-size]').forEach(function (btn) {
            btn.addEventListener('click', function () { selectSize(parseInt(btn.dataset.size, 10)); });
        });
    }

    function selectSize(i) {
        if (SIZES[i].stock === 0) return;
        selectedSize = i;
        var st = SIZES[i].stock;
        var badge = document.getElementById('stockBadge');
        var msg = document.getElementById('stockMsg');
        if (st <= 2) {
            msg.textContent = 'Son ' + st + ' ürün';
            badge.className = 'inline-flex items-center gap-1.5 text-sm font-medium text-warning bg-warning/10 px-3 py-1 rounded-full';
            badge.querySelector('span').className = 'w-1.5 h-1.5 rounded-full bg-warning';
        } else {
            msg.textContent = 'Stokta · ' + st + ' adet';
            badge.className = 'inline-flex items-center gap-1.5 text-sm font-medium text-success bg-success/10 px-3 py-1 rounded-full';
            badge.querySelector('span').className = 'w-1.5 h-1.5 rounded-full bg-success';
        }
        if (qty > st) { qty = st; document.getElementById('qty').textContent = qty; }
        renderSizes();
    }

    /* ---- color swatches ---- */
    function renderColors() {
        var html = COLORS.map(function (c, i) {
            var ring = i === selectedColor ? 'ring-2 ring-offset-2 ring-primary' : 'ring-1 ring-cream-300';
            return '<button type="button" data-color="' + i + '" title="' + c.name + '" aria-label="' + c.name + '" class="w-10 h-10 rounded-full border-2 border-white transition ' + ring + '" style="background:' + c.hex + '"></button>';
        }).join('');
        document.getElementById('colorSwatches').innerHTML = html;
        document.querySelectorAll('[data-color]').forEach(function (btn) {
            btn.addEventListener('click', function () { selectColor(parseInt(btn.dataset.color, 10)); });
        });
    }

    function selectColor(i) {
        selectedColor = i;
        document.getElementById('colorLabel').textContent = COLORS[i].name;
        setMainImage(COLORS[i].img);
        renderColors();
    }

    /* ---- qty ---- */
    document.querySelectorAll('[data-qty-change]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var d = parseInt(btn.dataset.qtyChange, 10);
            var max = SIZES[selectedSize].stock || 1;
            qty = Math.min(max, Math.max(1, qty + d));
            document.getElementById('qty').textContent = qty;
        });
    });

    /* ---- cart / bundle ---- */
    document.querySelectorAll('[data-add-to-cart]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var s = SIZES[selectedSize];
            toast('Sepete eklendi · ' + s.code + ' (' + s.age + ') · ' + qty + ' adet');
        });
    });
    document.querySelectorAll('[data-add-bundle]').forEach(function (btn) {
        btn.addEventListener('click', function () { toast('3 ürünlük paket sepete eklendi · 750₺'); });
    });

    /* ---- tabs ---- */
    function switchTab(name) {
        document.querySelectorAll('.tabBtn').forEach(function (b) {
            var on = b.dataset.tab === name;
            b.classList.toggle('border-primary', on);
            b.classList.toggle('text-charcoal', on);
            b.classList.toggle('border-transparent', !on);
            b.classList.toggle('text-muted', !on);
        });
        document.querySelectorAll('.tabPanel').forEach(function (p) {
            p.classList.toggle('hidden', p.dataset.panel !== name);
        });
    }
    document.querySelectorAll('.tabBtn').forEach(function (btn) {
        btn.addEventListener('click', function () { switchTab(btn.dataset.tab); });
    });
    document.querySelectorAll('[data-tab-switch]').forEach(function (link) {
        link.addEventListener('click', function () { switchTab(link.dataset.tabSwitch); });
    });

    /* ---- size table ---- */
    function sizeTableHTML() {
        var rows = SIZE_TABLE.map(function (r) {
            return '<tr class="hover:bg-cream/50"><td class="px-4 py-3 font-medium text-charcoal">' + r[0] + '</td><td class="px-4 py-3 text-charcoal/80">' + r[1] + '</td><td class="px-4 py-3 text-charcoal/80">' + r[2] + '</td><td class="px-4 py-3 text-charcoal/80">' + r[3] + '</td></tr>';
        }).join('');
        return '<div class="overflow-hidden rounded-2xl border border-cream-300">' +
            '<table class="w-full text-sm text-left">' +
            '<thead class="bg-cream text-charcoal"><tr><th class="px-4 py-3 font-semibold">Beden</th><th class="px-4 py-3 font-semibold">Yaş</th><th class="px-4 py-3 font-semibold">Boy (cm)</th><th class="px-4 py-3 font-semibold">Kilo (kg)</th></tr></thead>' +
            '<tbody class="divide-y divide-cream-300">' + rows + '</tbody></table></div>';
    }

    /* ---- reviews ---- */
    function starsHtml(n, size) {
        size = size || 16;
        var s = '';
        for (var i = 1; i <= 5; i++) {
            s += '<svg viewBox="0 0 24 24" width="' + size + '" height="' + size + '" fill="' + (i <= n ? 'currentColor' : 'none') + '" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path d="m12 2 3 6.3 6.9.9-5 4.8 1.2 6.8L12 17.8 5.9 20.8 7.1 14l-5-4.8L9 8.3Z"/></svg>';
        }
        return s;
    }

    function renderRatingBars() {
        var html = RATING_DIST.map(function (item) {
            var star = item[0], pct = item[1];
            return '<div class="flex items-center gap-2 text-xs">' +
                '<span class="w-4 text-charcoal/70">' + star + '</span>' +
                '<svg viewBox="0 0 24 24" width="12" height="12" fill="#FFD56B" aria-hidden="true"><path d="m12 2 3 6.3 6.9.9-5 4.8 1.2 6.8L12 17.8 5.9 20.8 7.1 14l-5-4.8L9 8.3Z"/></svg>' +
                '<div class="flex-1 h-1.5 rounded-full bg-cream-300 overflow-hidden"><div class="h-full bg-accent rounded-full" style="width:' + pct + '%"></div></div>' +
                '<span class="w-8 text-right text-muted">%' + pct + '</span></div>';
        }).join('');
        document.getElementById('ratingBars').innerHTML = html;
    }

    function renderReviews() {
        var html = REVIEWS.map(function (r) {
            var verified = r.verified
                ? '<span class="inline-flex items-center gap-1 text-[11px] font-medium text-success bg-success/10 px-2 py-0.5 rounded-full"><svg viewBox="0 0 24 24" width="11" height="11" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 6 9 17l-5-5"/></svg>Doğrulanmış Alıcı</span>'
                : '';
            return '<div class="border-b border-cream-300 pb-6 last:border-0">' +
                '<div class="flex items-center gap-3 mb-2">' +
                '<div class="w-10 h-10 rounded-full bg-primary/15 flex items-center justify-center font-semibold text-primary text-sm">' + r.name.charAt(0) + '</div>' +
                '<div><p class="font-semibold text-charcoal text-sm flex items-center gap-2">' + r.name + ' ' + verified + '</p>' +
                '<p class="text-xs text-muted">' + r.date + '</p></div></div>' +
                '<div class="flex gap-0.5 text-accent mb-2">' + starsHtml(r.rating, 14) + '</div>' +
                '<p class="text-charcoal/85 leading-relaxed text-sm">' + r.text + '</p>' +
                '<button type="button" data-review-helpful class="mt-3 inline-flex items-center gap-1.5 text-xs text-muted hover:text-primary transition">' +
                '<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M7 10v12"/><path d="M15 5.88 14 10h5.83a2 2 0 0 1 1.92 2.56l-2.33 8A2 2 0 0 1 17.5 22H4a2 2 0 0 1-2-2v-8a2 2 0 0 1 2-2h2.76a2 2 0 0 0 1.79-1.11L12 2a3.13 3.13 0 0 1 3 3.88Z"/></svg>' +
                'Faydalı (' + r.helpful + ')</button></div>';
        }).join('');
        document.getElementById('reviewList').innerHTML = html;
        document.querySelectorAll('[data-review-helpful]').forEach(function (b) {
            b.addEventListener('click', function () { toast('Geri bildiriminiz için teşekkürler'); });
        });
    }

    var reviewWriteBtn = document.querySelector('[data-review-write]');
    if (reviewWriteBtn) reviewWriteBtn.addEventListener('click', function () { toast('Yorum formu açılıyor'); });

    /* ---- product cards (similar / recently) ---- */
    function productCard(p) {
        var pct = discountPct(p);
        var dpct = pct ? '<span class="px-2.5 py-1 rounded-full bg-success text-white text-[11px] font-semibold">%' + pct + '</span>' : '';
        var oldHtml = p.old ? '<span class="text-sm text-muted line-through">' + p.old + '₺</span>' : '';
        return '<div class="group snap-start">' +
            '<a href="#" class="block" aria-label="' + p.name + ', ' + p.price + '₺">' +
            '<div class="ph relative aspect-[4/5] rounded-2xl ' + p.tint + ' overflow-hidden" data-ph="ürün görseli">' +
            '<img class="photo" loading="lazy" alt="' + p.name + '" src="' + IMG(p.img, 500) + '" />' +
            '<div class="absolute top-3 left-3 flex flex-col gap-1.5 items-start">' + badgeHtml(p.badge) + dpct + '</div>' +
            '<button type="button" data-wishlist-toggle data-on="0" aria-label="Favorilere ekle" class="absolute top-3 right-3 w-9 h-9 rounded-full bg-white/90 backdrop-blur flex items-center justify-center text-charcoal hover:text-primary transition shadow-sm">' +
            '<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M19 14c1.5-1.5 3-3.2 3-5.5A4.5 4.5 0 0 0 12 5.5 4.5 4.5 0 0 0 2 8.5C2 10.8 3.5 12.5 5 14l7 7Z"/></svg>' +
            '</button></div></a>' +
            '<div class="mt-3"><p class="text-xs text-muted">' + p.brand + '</p>' +
            '<a href="#" class="block font-medium text-charcoal line-clamp-2 leading-snug mt-0.5 hover:text-primary transition">' + p.name + '</a>' +
            '<span class="inline-block mt-2 px-2 py-0.5 rounded-md bg-cream text-[11px] text-muted">' + p.sizes + '</span>' +
            '<div class="mt-2 flex items-baseline gap-2">' + oldHtml + '<span class="text-lg font-semibold text-primary">' + p.price + '₺</span></div>' +
            '</div></div>';
    }

    /* ---- bundle ---- */
    function renderBundle() {
        var items = [
            { img: G[0].id, name: 'Çiçekli Yazlık Elbise', price: '349₺' },
            { img: SIMILAR[0].img, name: SIMILAR[0].name, price: '199₺' },
            { img: SIMILAR[2].img, name: SIMILAR[2].name, price: '259₺' },
        ];
        var html = items.map(function (it, i) {
            var plus = i > 0 ? '<span class="text-2xl text-muted font-light">+</span>' : '';
            return plus +
                '<div class="text-center w-28">' +
                '<div class="ph aspect-[4/5] rounded-2xl overflow-hidden bg-white"><img class="photo" loading="lazy" alt="' + it.name + '" src="' + IMG(it.img, 240) + '" /></div>' +
                '<p class="text-xs text-charcoal mt-2 line-clamp-2 leading-tight">' + it.name + '</p>' +
                '<p class="text-xs font-semibold text-primary mt-0.5">' + it.price + '</p>' +
                '</div>';
        }).join('');
        document.getElementById('bundle').innerHTML = html;
    }

    /* ---- size modal ---- */
    function openSizeModal() { document.getElementById('sizeModal').classList.remove('hidden'); document.body.style.overflow = 'hidden'; }
    function closeSizeModal() { document.getElementById('sizeModal').classList.add('hidden'); document.body.style.overflow = ''; }
    document.querySelectorAll('[data-size-modal-open]').forEach(function (b) { b.addEventListener('click', openSizeModal); });
    document.querySelectorAll('[data-size-modal-close]').forEach(function (b) { b.addEventListener('click', closeSizeModal); });

    /* ---- lightbox ---- */
    function openLightbox() {
        var el = document.getElementById('lightbox');
        document.getElementById('lightboxImg').src = IMG(G[activeImg].id, 1200);
        el.classList.remove('hidden');
        document.body.style.overflow = 'hidden';
    }
    function lightboxNav(d) {
        activeImg = (activeImg + d + G.length) % G.length;
        document.getElementById('lightboxImg').src = IMG(G[activeImg].id, 1200);
    }
    function closeLightbox() { document.getElementById('lightbox').classList.add('hidden'); document.body.style.overflow = ''; }
    document.querySelectorAll('[data-lightbox-open]').forEach(function (b) { b.addEventListener('click', openLightbox); });
    document.querySelectorAll('[data-lightbox-close]').forEach(function (b) { b.addEventListener('click', closeLightbox); });
    document.querySelectorAll('[data-lightbox-nav]').forEach(function (b) {
        b.addEventListener('click', function () { lightboxNav(parseInt(b.dataset.lightboxNav, 10)); });
    });

    /* ---- share ---- */
    document.querySelectorAll('[data-share]').forEach(function (b) {
        b.addEventListener('click', function () {
            var type = b.dataset.share;
            if (type === 'copy') {
                if (navigator.clipboard) navigator.clipboard.writeText(window.location.href);
                toast('Bağlantı kopyalandı');
            } else if (type === 'whatsapp') {
                toast('WhatsApp paylaşımı açılıyor');
            } else if (type === 'facebook') {
                toast('Facebook paylaşımı açılıyor');
            }
        });
    });

    /* ---- keyboard navigation ---- */
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeSizeModal();
            closeLightbox();
        }
        if (!document.getElementById('lightbox').classList.contains('hidden')) {
            if (e.key === 'ArrowLeft') lightboxNav(-1);
            if (e.key === 'ArrowRight') lightboxNav(1);
        }
    });

    /* ---- init ---- */
    setMainImage(0);
    renderSizes();
    renderColors();
    selectSize(1);
    renderRatingBars();
    renderReviews();
    document.getElementById('sizeTableTab').innerHTML = sizeTableHTML();
    document.getElementById('sizeTableModal').innerHTML = sizeTableHTML();
    document.getElementById('simScroll').innerHTML = SIMILAR.map(productCard).join('');
    document.getElementById('recentScroll').innerHTML = SIMILAR.slice(0, 6).map(productCard).join('');
    renderBundle();

    // Re-bind wishlist toggles for newly injected product cards.
    if (window.bindWishlistToggles) window.bindWishlistToggles();
})();
