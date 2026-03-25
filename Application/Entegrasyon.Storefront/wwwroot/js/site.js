// Scroll to top button
(function () {
    var btn = document.createElement('button');
    btn.id = 'scroll-to-top';
    btn.setAttribute('aria-label', 'Yukari cik');
    btn.className = 'fixed bottom-20 right-6 z-40 hidden w-12 h-12 rounded-full bg-gray-800 text-white shadow-lg hover:bg-gray-700 transition-colors items-center justify-center';

    // Create SVG icon via DOM API (safe, no innerHTML)
    var svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('class', 'h-5 w-5');
    svg.setAttribute('viewBox', '0 0 20 20');
    svg.setAttribute('fill', 'currentColor');
    var path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
    path.setAttribute('fill-rule', 'evenodd');
    path.setAttribute('d', 'M14.707 12.707a1 1 0 01-1.414 0L10 9.414l-3.293 3.293a1 1 0 01-1.414-1.414l4-4a1 1 0 011.414 0l4 4a1 1 0 010 1.414z');
    path.setAttribute('clip-rule', 'evenodd');
    svg.appendChild(path);
    btn.appendChild(svg);

    document.body.appendChild(btn);

    window.addEventListener('scroll', function () {
        if (window.scrollY > 300) {
            btn.classList.remove('hidden');
            btn.classList.add('flex');
        } else {
            btn.classList.add('hidden');
            btn.classList.remove('flex');
        }
    });

    btn.addEventListener('click', function () {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    });

    // Announcement bar close
    var closeBtn = document.getElementById('close-announcement');
    if (closeBtn) {
        closeBtn.addEventListener('click', function () {
            var bar = document.getElementById('announcement-bar');
            if (bar) bar.style.display = 'none';
        });
    }

    // Cookie consent
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
