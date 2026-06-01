/* Storefront chrome — her sayfada yüklenir. */
(function () {
    'use strict';

    /* ============ Toast ============ */
    var toastEl = document.getElementById('toast');
    var toastTimer;
    window.toast = function (msg) {
        if (!toastEl) return;
        toastEl.textContent = msg;
        toastEl.classList.remove('hidden');
        toastEl.classList.add('toast-in');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () {
            toastEl.classList.add('hidden');
            toastEl.classList.remove('toast-in');
        }, 2000);
    };

    /* ============ Announcement bar close ============ */
    document.querySelectorAll('[data-announce-close]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var bar = document.getElementById('announce');
            if (bar) bar.remove();
        });
    });

    /* ============ Mega menu ============ */
    var mega = document.getElementById('mega');
    var megaTrigger = document.getElementById('megaTrigger');
    function closeMega() { if (mega) mega.classList.add('hidden'); }
    function toggleMega() { if (mega) mega.classList.toggle('hidden'); }

    document.querySelectorAll('[data-mega-toggle]').forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            toggleMega();
        });
    });

    document.addEventListener('click', function (e) {
        if (!mega || mega.classList.contains('hidden')) return;
        if (mega.contains(e.target)) return;
        if (megaTrigger && megaTrigger.contains(e.target)) return;
        closeMega();
    });

    /* ============ Mobile drawer ============ */
    var drawer = document.getElementById('drawerWrap');
    function openDrawer() {
        if (!drawer) return;
        drawer.classList.remove('hidden');
        document.body.style.overflow = 'hidden';
    }
    function closeDrawer() {
        if (!drawer) return;
        drawer.classList.add('hidden');
        document.body.style.overflow = '';
    }
    document.querySelectorAll('[data-drawer-open]').forEach(function (b) { b.addEventListener('click', openDrawer); });
    document.querySelectorAll('[data-drawer-close]').forEach(function (b) { b.addEventListener('click', closeDrawer); });

    /* ============ Search overlay ============ */
    var searchWrap = document.getElementById('searchWrap');
    var searchInput = document.getElementById('searchInput');
    function openSearch() {
        if (!searchWrap) return;
        searchWrap.classList.remove('hidden');
        if (searchInput) setTimeout(function () { searchInput.focus(); }, 50);
    }
    function closeSearch() {
        if (!searchWrap) return;
        searchWrap.classList.add('hidden');
    }
    document.querySelectorAll('[data-search-open]').forEach(function (b) { b.addEventListener('click', openSearch); });
    document.querySelectorAll('[data-search-close]').forEach(function (b) { b.addEventListener('click', closeSearch); });

    /* ============ ESC handler ============ */
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeMega();
            closeDrawer();
            closeSearch();
        }
    });

    /* ============ Wishlist heart toggle ============ */
    var wishCountEl = document.getElementById('wishCount');
    var wishCount = wishCountEl && wishCountEl.textContent ? parseInt(wishCountEl.textContent, 10) || 0 : 0;

    function updateWishCount(delta) {
        wishCount = Math.max(0, wishCount + delta);
        if (!wishCountEl) return;
        wishCountEl.textContent = wishCount;
        if (wishCount === 0) wishCountEl.classList.add('hidden');
        else wishCountEl.classList.remove('hidden');
    }

    function bindWishlistToggles(root) {
        var scope = root || document;
        scope.querySelectorAll('[data-wishlist-toggle]:not([data-wl-bound])').forEach(function (btn) {
            btn.dataset.wlBound = '1';
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                var on = btn.dataset.on === '1';
                var svg = btn.querySelector('svg');
                if (on) {
                    btn.dataset.on = '0';
                    if (svg) svg.setAttribute('fill', 'none');
                    btn.classList.remove('text-primary');
                    btn.classList.add('text-charcoal');
                    updateWishCount(-1);
                    window.toast('Favorilerden çıkarıldı');
                } else {
                    btn.dataset.on = '1';
                    if (svg) svg.setAttribute('fill', 'currentColor');
                    btn.classList.add('text-primary');
                    btn.classList.remove('text-charcoal');
                    updateWishCount(1);
                    window.toast('Favorilere eklendi');
                }
            });
        });
    }
    window.bindWishlistToggles = bindWishlistToggles;
    bindWishlistToggles();

    /* ============ Carousel scroll (yatay) ============ */
    document.querySelectorAll('[data-carousel-scroll]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var targetId = btn.dataset.carouselTarget;
            var target = targetId ? document.getElementById(targetId) : null;
            if (!target) return;
            var dir = btn.dataset.carouselScroll === 'prev' ? -1 : 1;
            target.scrollBy({ left: dir * (target.clientWidth * 0.6), behavior: 'smooth' });
        });
    });

    /* ============ Scroll to top ============ */
    var topBtn = document.createElement('button');
    topBtn.type = 'button';
    topBtn.id = 'scroll-to-top';
    topBtn.setAttribute('aria-label', 'Yukarı çık');
    topBtn.className = 'fixed bottom-6 right-6 z-40 hidden w-11 h-11 rounded-full bg-charcoal text-white shadow-md hover:bg-charcoal/90 transition items-center justify-center';
    var topSvg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    topSvg.setAttribute('viewBox', '0 0 24 24');
    topSvg.setAttribute('width', '18');
    topSvg.setAttribute('height', '18');
    topSvg.setAttribute('fill', 'none');
    topSvg.setAttribute('stroke', 'currentColor');
    topSvg.setAttribute('stroke-width', '2');
    topSvg.setAttribute('stroke-linecap', 'round');
    topSvg.setAttribute('aria-hidden', 'true');
    var topPath = document.createElementNS('http://www.w3.org/2000/svg', 'path');
    topPath.setAttribute('d', 'm18 15-6-6-6 6');
    topSvg.appendChild(topPath);
    topBtn.appendChild(topSvg);
    document.body.appendChild(topBtn);

    window.addEventListener('scroll', function () {
        if (window.scrollY > 300) {
            topBtn.classList.remove('hidden');
            topBtn.classList.add('flex');
        } else {
            topBtn.classList.add('hidden');
            topBtn.classList.remove('flex');
        }
    });
    topBtn.addEventListener('click', function () {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    });

    /* ============ Cookie consent (mevcut) ============ */
    var consentBanner = document.getElementById('cookie-consent');
    if (consentBanner && !localStorage.getItem('cookie-consent')) {
        consentBanner.classList.remove('hidden');
    }
    var acceptBtn = document.getElementById('cookie-accept');
    if (acceptBtn) {
        acceptBtn.addEventListener('click', function () {
            localStorage.setItem('cookie-consent', 'accepted');
            if (consentBanner) consentBanner.classList.add('hidden');
        });
    }
    var rejectBtn = document.getElementById('cookie-reject');
    if (rejectBtn) {
        rejectBtn.addEventListener('click', function () {
            localStorage.setItem('cookie-consent', 'rejected');
            if (consentBanner) consentBanner.classList.add('hidden');
        });
    }
})();
