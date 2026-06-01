/* Zekids — Cüzdanım: filtre chip + accordion + mobil sheet. */
(function () {
    'use strict';

    var current = 'all';

    function setFilter(key) {
        current = key;
        document.querySelectorAll('[data-txn-filter]').forEach(function (b) {
            var on = b.dataset.txnFilter === key;
            b.classList.toggle('chip-active', on);
            b.classList.toggle('border-cream-300', !on);
            b.classList.toggle('text-charcoal', !on);
            b.classList.toggle('hover:border-primary', !on);
            b.classList.toggle('hover:text-primary', !on);
            var count = b.querySelector('.chip-count');
            if (count) {
                count.classList.toggle('bg-cream', !on);
                count.classList.toggle('text-muted', !on);
            }
        });

        var visible = 0;
        document.querySelectorAll('#txnList details[data-cat]').forEach(function (row) {
            var show = key === 'all' || row.dataset.cat === key;
            row.classList.toggle('hidden', !show);
            if (show) visible++;
        });

        var empty = document.getElementById('txnEmpty');
        if (empty) empty.classList.toggle('hidden', visible > 0);
    }

    document.querySelectorAll('[data-txn-filter]').forEach(function (b) {
        b.addEventListener('click', function () { setFilter(b.dataset.txnFilter); });
    });

    /* Bakiye yükle / çek — V1'de backend yok, toast */
    document.querySelectorAll('[data-wallet-action]').forEach(function (b) {
        b.addEventListener('click', function () {
            var msg = b.dataset.walletAction === 'topup'
                ? 'Bakiye yükleme özelliği yakında.'
                : 'Para çekme özelliği yakında.';
            if (window.toast) window.toast(msg); else alert(msg);
        });
    });

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

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            var s = document.getElementById('accSheet');
            if (s && !s.classList.contains('hidden')) { s.classList.add('hidden'); document.body.style.overflow = ''; }
        }
    });
})();
