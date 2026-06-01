/* Zekids — Checkout Başarılı: önerilen ürünler carousel. */
(function () {
    'use strict';

    var IMG = function (id, w) { return 'https://images.unsplash.com/photo-' + id + '?auto=format&fit=crop&w=' + (w || 500) + '&q=80'; };

    var REC = [
        { name: 'Fırfırlı Yazlık Tunik',         brand: 'Zekids',   price: 199, old: null, sizes: '3-7 yaş',  badge: 'new',  tint: 'bg-accent/25',    img: '1604482858862-1db908a653e4' },
        { name: 'Tavşan Desenli Pijama',         brand: 'Uykucu',   price: 259, old: 319,  sizes: '2-8 yaş',  badge: 'sale', tint: 'bg-primary/15',   img: '1622290319146-7b63df48a635' },
        { name: 'Salopet Kot Tulum',             brand: 'Pamuk',    price: 389, old: null, sizes: '1-5 yaş',  badge: 'new',  tint: 'bg-secondary/20', img: '1632337950445-ba446cb0e26f' },
        { name: 'Çiçekli Saç Bandı Seti',        brand: 'Mavi Kids', price: 89, old: 129, sizes: '0-6 yaş',  badge: 'sale', tint: 'bg-primary/20',   img: '1560506840-ec148e82a604' },
        { name: 'Hastane Çıkışı 5\'li Set',      brand: 'Minimini', price: 699, old: 849,  sizes: '0-3 ay',   badge: 'best', tint: 'bg-primary/20',   img: '1632337948797-ba161d29532b' },
    ];

    function badgeHtml(type) {
        if (type === 'new') return '<span class="px-2.5 py-1 rounded-full bg-primary text-white text-[11px] font-semibold">Yeni</span>';
        if (type === 'best') return '<span class="px-2.5 py-1 rounded-full bg-accent text-charcoal text-[11px] font-semibold">Çok Satan</span>';
        if (type === 'sale') return '<span class="px-2.5 py-1 rounded-full bg-success text-white text-[11px] font-semibold">İndirim</span>';
        return '';
    }
    function productCard(p) {
        var pct = p.old ? Math.round((1 - p.price / p.old) * 100) : null;
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

    var rec = document.getElementById('recScroll');
    if (rec) rec.innerHTML = REC.map(productCard).join('');
    if (window.bindWishlistToggles) window.bindWishlistToggles();
})();
