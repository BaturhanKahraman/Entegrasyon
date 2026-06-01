/* Zekids Bebe — Anasayfa için sayfa-özel etkileşimler. */
(function () {
    'use strict';

    /* Newsletter form (mock submit + success mesajı). Gerçek endpoint /bulten/abone-ol. */
    var form = document.getElementById('newsletterForm');
    var msg = document.getElementById('subMsg');
    if (form && msg) {
        form.addEventListener('submit', function (e) {
            // Geçici: ilk faz mock. Backend endpoint hazır olunca bu blok kaldırılır.
            e.preventDefault();
            form.reset();
            msg.classList.remove('hidden');
            setTimeout(function () { msg.classList.add('hidden'); }, 4000);
        });
    }
})();
