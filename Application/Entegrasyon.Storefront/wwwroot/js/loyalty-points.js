/* Zekids — Sadakat Puanları: yalnızca mobil sidebar sheet toggle. */
(function () {
    'use strict';

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
