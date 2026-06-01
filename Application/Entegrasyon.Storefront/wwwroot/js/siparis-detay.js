/* ============ Zekids Bebe — Sipariş Detayı ============ */
(function () {
    'use strict';

    var IMG = function (id, w) { return 'https://images.unsplash.com/photo-' + id + '?auto=format&fit=crop&w=' + (w || 200) + '&q=80'; };
    var FB = function (s) { return 'https://loremflickr.com/200/250/' + s; };

    /* ===== mobile account sheet ===== */
    var accSheet = document.getElementById('accSheet');
    function openAcc() { if (accSheet) { accSheet.classList.remove('hidden'); document.body.style.overflow = 'hidden'; } }
    function closeAcc() { if (accSheet) { accSheet.classList.add('hidden'); document.body.style.overflow = ''; } }
    document.querySelectorAll('[data-acc-open]').forEach(function (b) { b.addEventListener('click', openAcc); });
    document.querySelectorAll('[data-acc-close]').forEach(function (b) { b.addEventListener('click', closeAcc); });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') closeAcc(); });

    /* ===== toast buttons ===== */
    document.querySelectorAll('[data-toast]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            if (window.toast) window.toast(btn.dataset.toast);
        });
    });

    /* ===== order items (sample) ===== */
    var OD_ITEMS = [
        { name: 'Çiçekli Yazlık Elbise', variant: 'Beden: 104 · Pudra Pembe', qty: 1, unit: 349, img: '1620774760711-caa4c94d683a', fb: 'girl,dress?lock=181' },
        { name: 'Eşofman Takımı', variant: 'Beden: 110 · Bebek Mavisi', qty: 2, unit: 199, img: '1604482858862-1db908a653e4', fb: 'kids,clothes?lock=182' },
        { name: 'Pamuklu Tişört (3\'lü Paket)', variant: 'Beden: 98', qty: 1, unit: 249, img: '1622290291468-a28f7a7dc6a8', fb: 'kids,clothes?lock=183' },
        { name: 'Çizgili Bisiklet Yaka Tişört', variant: 'Beden: 104', qty: 1, unit: 149, img: '1560859259-fcf2b952aed8', fb: 'kids,clothes?lock=184' },
        { name: 'Tavşan Desenli Pijama', variant: 'Beden: 98 · Krem', qty: 1, unit: 102, img: '1622290319146-7b63df48a635', fb: 'baby,clothes?lock=185' },
    ];

    function renderItems() {
        var container = document.getElementById('odItems');
        if (!container) return;
        container.innerHTML = OD_ITEMS.map(function (it) {
            return '' +
                '<div class="flex items-center gap-4 p-4 sm:p-5">' +
                '  <a href="/urun/detay" class="ph w-16 h-20 rounded-xl overflow-hidden bg-cream shrink-0 block"><img class="photo" loading="lazy" alt="' + it.name + '" src="' + IMG(it.img, 160) + '" onerror="this.onerror=null;this.src=\'' + FB(it.fb) + '\'" /></a>' +
                '  <div class="flex-1 min-w-0">' +
                '    <a href="/urun/detay" class="font-medium text-charcoal text-sm hover:text-primary transition">' + it.name + '</a>' +
                '    <p class="text-xs text-muted mt-0.5">' + it.variant + '</p>' +
                '    <p class="text-xs text-muted mt-0.5">' + it.qty + ' × ' + it.unit + '₺</p>' +
                '    <div class="flex gap-3 mt-1.5"><button type="button" data-buy-again class="text-xs font-medium text-primary hover:underline">Yine Al</button></div>' +
                '  </div>' +
                '  <span class="font-semibold text-charcoal text-sm whitespace-nowrap">' + (it.qty * it.unit) + '₺</span>' +
                '</div>';
        }).join('');

        container.querySelectorAll('[data-buy-again]').forEach(function (btn) {
            btn.addEventListener('click', function () { if (window.toast) window.toast('Sepete eklendi'); });
        });
    }

    renderItems();
})();
