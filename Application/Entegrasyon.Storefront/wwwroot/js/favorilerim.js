/* ============ Zekids Bebe — Favorilerim ============ */
(function () {
    'use strict';

    function notify(msg) { if (window.toast) window.toast(msg); }

    /* ===== mobile account sheet ===== */
    var sheet = document.getElementById('accSheet');
    function openAcc() { if (sheet) { sheet.classList.remove('hidden'); document.body.style.overflow = 'hidden'; } }
    function closeAcc() { if (sheet) { sheet.classList.add('hidden'); document.body.style.overflow = ''; } }
    document.querySelectorAll('[data-acc-open]').forEach(function (b) { b.addEventListener('click', openAcc); });
    document.querySelectorAll('[data-acc-close]').forEach(function (b) { b.addEventListener('click', closeAcc); });

    /* ===== share dropdown ===== */
    var shareToggle = document.querySelector('[data-share-toggle]');
    var shareMenu = document.querySelector('[data-share-menu]');
    function closeShare() { if (shareMenu) shareMenu.classList.add('hidden'); }
    if (shareToggle && shareMenu) {
        shareToggle.addEventListener('click', function (e) {
            e.stopPropagation();
            shareMenu.classList.toggle('hidden');
        });
        document.addEventListener('click', function (e) {
            if (!shareMenu.contains(e.target) && !shareToggle.contains(e.target)) closeShare();
        });
    }
    var shareCopy = document.querySelector('[data-share-copy]');
    if (shareCopy) {
        shareCopy.addEventListener('click', function () {
            var url = window.location.href;
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(url).then(function () { notify('Bağlantı kopyalandı'); }, function () { notify('Bağlantı kopyalandı'); });
            } else {
                notify('Bağlantı kopyalandı');
            }
            closeShare();
        });
    }
    document.querySelectorAll('[data-share-close]').forEach(function (b) {
        b.addEventListener('click', closeShare);
    });

    /* ===== empty state + title count ===== */
    var grid = document.querySelector('[data-wish-grid]');
    var emptyEl = document.querySelector('[data-wish-empty]');
    var titleCount = document.querySelector('[data-wish-title-count]');

    function refreshState() {
        if (!grid) return;
        var cards = grid.querySelectorAll('[data-wish-card]');
        var n = cards.length;
        if (titleCount) titleCount.textContent = '(' + n + ' ürün)';
        if (n === 0) {
            grid.classList.add('hidden');
            if (emptyEl) emptyEl.classList.remove('hidden');
        }
    }

    /* ===== remove from wishlist on toggle-off =====
       site.js bindWishlistToggles handles the heart fill + nav badge + toast.
       Here we additionally remove the card from the wishlist grid. */
    document.querySelectorAll('[data-wish-card] [data-wishlist-toggle]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var card = btn.closest('[data-wish-card]');
            if (!card) return;
            // site.js already flipped data-on to '0' for this click
            card.style.transition = 'opacity .2s ease';
            card.style.opacity = '0';
            setTimeout(function () { card.remove(); refreshState(); }, 200);
        });
    });

    /* ===== per-card cart / notify buttons ===== */
    document.querySelectorAll('[data-add-cart]').forEach(function (b) {
        b.addEventListener('click', function () { notify('Ürün sepete eklendi'); });
    });
    document.querySelectorAll('[data-notify-stock]').forEach(function (b) {
        b.addEventListener('click', function () { notify('Stok gelince haber vereceğiz'); });
    });

    /* ===== add all in-stock ===== */
    var addAll = document.querySelector('[data-add-all]');
    if (addAll) {
        addAll.addEventListener('click', function () {
            var n = document.querySelectorAll('[data-add-cart]').length;
            notify(n > 0 ? (n + ' ürün sepete eklendi') : 'Sepete eklenecek stokta ürün yok');
        });
    }

    /* ===== ensure heart toggles are bound (site.js global) ===== */
    if (window.bindWishlistToggles) window.bindWishlistToggles();
})();
