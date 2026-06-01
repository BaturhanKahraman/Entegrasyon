/* Zekids Bebe — Sepet etkileşimleri (mock state). */
(function () {
    'use strict';

    var IMG = function (id, w) { return 'https://images.unsplash.com/photo-' + id + '?auto=format&fit=crop&w=' + (w || 300) + '&q=80'; };
    var FREE_SHIP = 500;
    function toast(m) { if (window.toast) window.toast(m); }

    /* ---- cart data ---- */
    var CART = [
        { id: 1, name: 'Çiçekli Yazlık Elbise', size: '104 (3-4 yaş)', color: 'Pudra Pembe', unit: 349, old: 499, qty: 1, stock: 8, img: '1620774760711-caa4c94d683a' },
        { id: 2, name: 'Eşofman Takımı', size: '110 (4-5 yaş)', color: 'Bebek Mavisi', unit: 199, old: null, qty: 2, stock: 5, img: '1604482858862-1db908a653e4' },
        { id: 3, name: 'Pamuklu Tişört (3\'lü Paket)', size: '98 (2-3 yaş)', color: null, unit: 249, old: null, qty: 1, stock: 2, img: '1622290291468-a28f7a7dc6a8' },
    ];
    var LOYALTY = 50;
    var couponAmt = 0, couponName = '';
    var giftAmt = 0;
    var lastRemoved = null;
    var displayedTotal = 0;

    /* ---- render items ---- */
    function priceHTML(line, oldLine) {
        return (oldLine ? '<span class="block text-xs text-muted line-through">' + oldLine + '₺</span>' : '') +
            '<span class="text-lg font-semibold text-primary">' + line + '₺</span>';
    }
    function stepperHTML(it) {
        return '<div class="flex items-center border border-cream-300 rounded-full">' +
            '<button type="button" data-qty="-1" data-id="' + it.id + '" aria-label="Azalt" class="w-9 h-9 flex items-center justify-center text-charcoal hover:text-primary text-lg">−</button>' +
            '<span class="w-8 text-center text-sm font-semibold">' + it.qty + '</span>' +
            '<button type="button" data-qty="1" data-id="' + it.id + '" aria-label="Artır" class="w-9 h-9 flex items-center justify-center text-charcoal hover:text-primary text-lg">+</button>' +
            '</div>';
    }
    function renderItems() {
        var el = document.getElementById('cartItems');
        if (!el) return;
        el.innerHTML = CART.map(function (it, i) {
            var line = it.unit * it.qty;
            var oldLine = it.old ? it.old * it.qty : null;
            var lowStock = it.stock <= 2;
            var sep = i < CART.length - 1 ? 'border-b border-cream-300' : '';
            return '<div class="cart-row grid grid-cols-[84px_1fr] sm:grid-cols-[100px_1fr_auto] gap-4 p-5 sm:p-6 ' + sep + '" data-id="' + it.id + '">' +
                '<a href="/urun/' + it.id + '" class="ph aspect-[4/5] rounded-xl overflow-hidden bg-cream block"><img class="photo" loading="lazy" alt="' + it.name + '" src="' + IMG(it.img, 200) + '" /></a>' +
                '<div class="min-w-0">' +
                '<a href="/urun/' + it.id + '" class="font-medium text-charcoal hover:text-primary transition leading-snug">' + it.name + '</a>' +
                '<p class="text-sm text-muted mt-1">Beden: ' + it.size + (it.color ? ' · Renk: ' + it.color : '') + '</p>' +
                (lowStock ? '<p class="text-xs text-warning font-medium mt-1.5">Son ' + it.stock + ' ürün</p>' : '') +
                '<div class="flex flex-wrap gap-x-4 gap-y-1 mt-2.5 text-xs">' +
                '<button type="button" data-save-later data-id="' + it.id + '" class="text-muted hover:text-primary transition">Daha sonra için kaydet</button>' +
                '<button type="button" data-move-wish data-id="' + it.id + '" class="text-muted hover:text-primary transition">Favorilere taşı</button>' +
                '</div>' +
                '<div class="flex sm:hidden items-center justify-between mt-3">' + stepperHTML(it) + '<div class="text-right">' + priceHTML(line, oldLine) + '</div></div>' +
                '</div>' +
                '<div class="hidden sm:flex flex-col items-end justify-between">' +
                '<button type="button" data-remove data-id="' + it.id + '" aria-label="Ürünü kaldır" class="text-muted hover:text-danger transition p-1"><svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 6h18M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg></button>' +
                stepperHTML(it) +
                '<div class="text-right">' + priceHTML(line, oldLine) + '</div>' +
                '</div></div>';
        }).join('');
        bindItemEvents();
    }

    function bindItemEvents() {
        document.querySelectorAll('[data-qty]').forEach(function (b) {
            b.addEventListener('click', function () { updateQty(+b.dataset.id, +b.dataset.qty); });
        });
        document.querySelectorAll('[data-remove]').forEach(function (b) {
            b.addEventListener('click', function () { removeItem(+b.dataset.id); });
        });
        document.querySelectorAll('[data-save-later]').forEach(function (b) {
            b.addEventListener('click', function () { removeViaToast(+b.dataset.id, 'Daha sonrası için kaydedildi'); });
        });
        document.querySelectorAll('[data-move-wish]').forEach(function (b) {
            b.addEventListener('click', function () {
                var wc = document.getElementById('wishCount');
                if (wc) { wc.textContent = (parseInt(wc.textContent, 10) || 0) + 1; wc.classList.remove('hidden'); }
                removeViaToast(+b.dataset.id, 'Favorilere taşındı');
            });
        });
    }

    /* ---- calculations ---- */
    function subtotal() { return CART.reduce(function (s, it) { return s + it.unit * it.qty; }, 0); }
    function shipping() { return subtotal() >= FREE_SHIP ? 0 : 49; }
    function total() { return Math.max(0, subtotal() + shipping() - LOYALTY - couponAmt - giftAmt); }

    function recalc(animate) {
        var sub = subtotal();
        document.getElementById('subTotal').textContent = sub + '₺';
        var ship = shipping();
        var shipEl = document.getElementById('shipVal');
        shipEl.innerHTML = ship === 0 ? '<span class="text-success">Bedava</span>' : ship + '₺';
        shipEl.className = ship === 0 ? 'text-success font-medium' : 'text-charcoal font-medium';

        var cr = document.getElementById('couponRow');
        if (couponAmt > 0) {
            cr.classList.remove('hidden');
            document.getElementById('couponName').textContent = couponName;
            document.getElementById('couponAmt').textContent = '−' + couponAmt + '₺';
        } else cr.classList.add('hidden');

        var gr = document.getElementById('giftRow');
        if (giftAmt > 0) {
            gr.classList.remove('hidden');
            document.getElementById('giftAmt').textContent = '−' + giftAmt + '₺';
        } else gr.classList.add('hidden');

        var t = total();
        if (animate) animateNumber(document.getElementById('totalVal'), displayedTotal, t, 320);
        else document.getElementById('totalVal').textContent = t + '₺';
        displayedTotal = t;

        var fs = document.getElementById('freeShip');
        var pct = Math.min(100, Math.round((sub / FREE_SHIP) * 100));
        if (sub >= FREE_SHIP) {
            fs.className = 'rounded-2xl p-4 mb-6 bg-success/10';
            fs.innerHTML = '<div class="flex items-center gap-2.5 text-success font-medium text-sm mb-2.5"><svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 17h4V5H2v12h3"/><path d="M20 17h2v-3.3a2 2 0 0 0-.6-1.4L18 9h-4v8h2"/><circle cx="7.5" cy="17.5" r="2.5"/><circle cx="17.5" cy="17.5" r="2.5"/></svg>Kargonuz bedava!</div><div class="h-2 rounded-full bg-success/20 overflow-hidden"><div class="h-full bg-success rounded-full" style="width:100%"></div></div>';
        } else {
            fs.className = 'rounded-2xl p-4 mb-6 bg-primary/10';
            fs.innerHTML = '<div class="flex items-center gap-2.5 text-charcoal font-medium text-sm mb-2.5"><svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="#FF8FB1" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 17h4V5H2v12h3"/><path d="M20 17h2v-3.3a2 2 0 0 0-.6-1.4L18 9h-4v8h2"/><circle cx="7.5" cy="17.5" r="2.5"/><circle cx="17.5" cy="17.5" r="2.5"/></svg>Bedava kargoya <span class="text-primary">' + (FREE_SHIP - sub) + '₺</span> kaldı</div><div class="h-2 rounded-full bg-primary/15 overflow-hidden"><div class="h-full bg-primary rounded-full transition-all" style="width:' + pct + '%"></div></div>';
        }

        document.getElementById('cartCount').textContent = CART.length;
    }

    function animateNumber(el, from, to, dur) {
        var start = performance.now();
        function step(now) {
            var p = Math.min(1, (now - start) / dur);
            var e = 1 - Math.pow(1 - p, 3);
            el.textContent = Math.round(from + (to - from) * e) + '₺';
            if (p < 1) requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
    }

    /* ---- actions ---- */
    function updateQty(id, d) {
        var it = CART.find(function (x) { return x.id === id; });
        if (!it) return;
        var nq = it.qty + d;
        if (nq < 1) { removeItem(id); return; }
        if (nq > it.stock) { toast('Bu üründen en fazla ' + it.stock + ' adet ekleyebilirsiniz'); return; }
        it.qty = nq;
        renderItems(); recalc(true);
    }
    function removeItem(id) {
        var idx = CART.findIndex(function (x) { return x.id === id; });
        if (idx < 0) return;
        var row = document.querySelector('.cart-row[data-id="' + id + '"]');
        lastRemoved = { item: CART[idx], index: idx };
        if (row) {
            row.classList.add('removing');
            setTimeout(function () { CART.splice(idx, 1); afterChange(); showUndo('Üründen kaldırıldı'); }, 260);
        } else {
            CART.splice(idx, 1); afterChange(); showUndo('Üründen kaldırıldı');
        }
    }
    function removeViaToast(id, msg) {
        var idx = CART.findIndex(function (x) { return x.id === id; });
        if (idx < 0) return;
        lastRemoved = { item: CART[idx], index: idx };
        var row = document.querySelector('.cart-row[data-id="' + id + '"]');
        if (row) { row.classList.add('removing'); setTimeout(function () { CART.splice(idx, 1); afterChange(); showUndo(msg); }, 260); }
    }
    function clearCart() {
        if (CART.length === 0) return;
        lastRemoved = { all: CART.slice() };
        CART = [];
        afterChange();
        showUndo('Sepet boşaltıldı');
    }
    function undoRemove() {
        if (!lastRemoved) return;
        if (lastRemoved.all) CART = lastRemoved.all.slice();
        else CART.splice(lastRemoved.index, 0, lastRemoved.item);
        lastRemoved = null;
        document.getElementById('undoBar').classList.add('hidden');
        afterChange();
    }
    function showUndo(msg) {
        document.getElementById('undoMsg').textContent = msg;
        var u = document.getElementById('undoBar');
        u.classList.remove('hidden');
        clearTimeout(u._t);
        u._t = setTimeout(function () { u.classList.add('hidden'); }, 4000);
    }
    function afterChange() {
        if (CART.length === 0) {
            document.getElementById('cartArea').classList.add('hidden');
            document.getElementById('emptyCart').classList.remove('hidden');
            document.getElementById('cartCount').textContent = 0;
        } else {
            document.getElementById('cartArea').classList.remove('hidden');
            document.getElementById('emptyCart').classList.add('hidden');
            renderItems();
            recalc(true);
        }
    }

    document.querySelectorAll('[data-clear-cart]').forEach(function (b) { b.addEventListener('click', clearCart); });
    document.querySelectorAll('[data-undo-remove]').forEach(function (b) { b.addEventListener('click', undoRemove); });

    /* ---- coupon / gift ---- */
    document.querySelectorAll('[data-apply-coupon]').forEach(function (b) {
        b.addEventListener('click', function () {
            var v = document.getElementById('couponInput').value.trim().toUpperCase();
            if (!v) return;
            if (v === 'BAHAR50') {
                couponAmt = 50; couponName = 'BAHAR50';
                document.getElementById('couponInput').value = '';
                var chip = document.getElementById('couponChip');
                chip.classList.remove('hidden');
                chip.innerHTML = '<span class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-success/10 text-success text-xs font-medium">BAHAR50 · −50₺ <button type="button" data-remove-coupon aria-label="Kuponu kaldır"><svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" aria-hidden="true"><path d="M18 6 6 18M6 6l12 12"/></svg></button></span>';
                document.querySelector('[data-remove-coupon]').addEventListener('click', function () {
                    couponAmt = 0; couponName = '';
                    document.getElementById('couponChip').classList.add('hidden');
                    recalc(true);
                });
                recalc(true);
                toast('Kupon uygulandı · −50₺');
            } else toast('Geçersiz kupon kodu');
        });
    });
    document.querySelectorAll('[data-apply-gift]').forEach(function (b) {
        b.addEventListener('click', function () {
            var v = document.getElementById('giftInput').value.trim().toUpperCase();
            if (!v) return;
            if (v === 'HEDIYE100') {
                giftAmt = 100;
                document.getElementById('giftInput').value = '';
                var chip = document.getElementById('giftChip');
                chip.classList.remove('hidden');
                chip.innerHTML = '<span class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-success/10 text-success text-xs font-medium">HEDIYE100 · −100₺ <button type="button" data-remove-gift aria-label="Hediye kartını kaldır"><svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" aria-hidden="true"><path d="M18 6 6 18M6 6l12 12"/></svg></button></span>';
                document.querySelector('[data-remove-gift]').addEventListener('click', function () {
                    giftAmt = 0;
                    document.getElementById('giftChip').classList.add('hidden');
                    recalc(true);
                });
                recalc(true);
                toast('Hediye kartı uygulandı · −100₺');
            } else toast('Hediye kartı bakiyesi bulunamadı');
        });
    });

    /* ---- recommendations ---- */
    var REC = [
        { name: 'Fırfırlı Yazlık Tunik', brand: 'Zekids', price: 199, old: null, sizes: '3-7 yaş', badge: 'new',  tint: 'bg-accent/25',    img: '1604482858862-1db908a653e4' },
        { name: 'Tavşan Desenli Pijama', brand: 'Uykucu', price: 259, old: 319,  sizes: '2-8 yaş', badge: 'sale', tint: 'bg-primary/15',   img: '1622290319146-7b63df48a635' },
        { name: 'Salopet Kot Tulum',     brand: 'Pamuk',  price: 389, old: null, sizes: '1-5 yaş', badge: 'new',  tint: 'bg-secondary/20', img: '1632337950445-ba446cb0e26f' },
        { name: 'Çiçekli Saç Bandı Seti', brand: 'Mavi Kids', price: 89, old: 129, sizes: '0-6 yaş', badge: 'sale', tint: 'bg-primary/20', img: '1560506840-ec148e82a604' },
    ];
    function badgeHtml(type) {
        if (type === 'new') return '<span class="px-2.5 py-1 rounded-full bg-primary text-white text-[11px] font-semibold">Yeni</span>';
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
            '<button type="button" data-wishlist-toggle data-on="0" aria-label="Favorilere ekle" class="absolute top-3 right-3 w-9 h-9 rounded-full bg-white/90 backdrop-blur flex items-center justify-center text-charcoal hover:text-primary transition shadow-sm"><svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M19 14c1.5-1.5 3-3.2 3-5.5A4.5 4.5 0 0 0 12 5.5 4.5 4.5 0 0 0 2 8.5C2 10.8 3.5 12.5 5 14l7 7Z"/></svg></button>' +
            '</div></a>' +
            '<div class="mt-3"><p class="text-xs text-muted">' + p.brand + '</p>' +
            '<a href="#" class="block font-medium text-charcoal line-clamp-2 leading-snug mt-0.5 hover:text-primary transition">' + p.name + '</a>' +
            '<span class="inline-block mt-2 px-2 py-0.5 rounded-md bg-cream text-[11px] text-muted">' + p.sizes + '</span>' +
            '<div class="mt-2 flex items-baseline gap-2">' + oldHtml + '<span class="text-lg font-semibold text-primary">' + p.price + '₺</span></div>' +
            '</div></div>';
    }

    /* ---- init ---- */
    renderItems();
    displayedTotal = total();
    recalc(false);
    var rec = document.getElementById('recScroll');
    if (rec) rec.innerHTML = REC.map(productCard).join('');
    if (window.bindWishlistToggles) window.bindWishlistToggles();
})();
