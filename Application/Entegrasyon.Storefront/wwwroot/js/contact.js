/* Zekids — İletişim: textarea karakter sayacı. */
(function () {
    'use strict';
    document.querySelectorAll('[data-char-count]').forEach(function (el) {
        var out = document.getElementById(el.getAttribute('data-char-count'));
        if (!out) return;
        var update = function () { out.textContent = el.value.length; };
        el.addEventListener('input', update);
        update();
    });
})();
