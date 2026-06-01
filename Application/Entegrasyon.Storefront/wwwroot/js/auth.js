/* Zekids — Auth (Login + Register) ortak etkileşimler. */
(function () {
    'use strict';

    /* ---- şifre göz toggle ---- */
    document.querySelectorAll('[data-toggle-pw]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var target = document.getElementById(btn.dataset.togglePw);
            if (!target) return;
            var visible = target.type === 'text';
            target.type = visible ? 'password' : 'text';
            btn.classList.toggle('text-primary', !visible);
        });
    });

    /* ---- telefon mask (0(5XX) XXX XX XX) ---- */
    document.querySelectorAll('[data-mask-phone]').forEach(function (el) {
        el.addEventListener('input', function () {
            var d = el.value.replace(/\D/g, '').slice(0, 11);
            if (d && d[0] !== '0') d = '0' + d.slice(0, 10);
            var out = '';
            if (d.length > 0) out = '0';
            if (d.length > 1) out += '(' + d.slice(1, 4);
            if (d.length >= 4) out += ') ' + d.slice(4, 7);
            if (d.length >= 7) out += ' ' + d.slice(7, 9);
            if (d.length >= 9) out += ' ' + d.slice(9, 11);
            el.value = out;
        });
    });

    /* ---- şifre strength bar ---- */
    var strengthEl = document.querySelector('[data-pw-strength]');
    if (strengthEl) {
        var bars = document.querySelectorAll('.strbar');
        var label = document.getElementById('strLabel');
        strengthEl.addEventListener('input', function () {
            var v = strengthEl.value;
            var score = 0;
            if (v.length >= 8) score++;
            if (/[A-Z]/.test(v) && /[a-z]/.test(v)) score++;
            if (/\d/.test(v) || /[^A-Za-z0-9]/.test(v)) score++;
            bars.forEach(function (b, i) {
                b.classList.remove('bg-cream-300', 'bg-danger', 'bg-warning', 'bg-success');
                if (i < score) {
                    b.classList.add(score === 1 ? 'bg-danger' : score === 2 ? 'bg-warning' : 'bg-success');
                } else {
                    b.classList.add('bg-cream-300');
                }
            });
            if (label) {
                if (!v) { label.textContent = 'En az 8 karakter, bir büyük harf ve rakam önerilir.'; label.className = 'text-xs text-muted mt-1'; }
                else if (score === 1) { label.textContent = 'Zayıf — daha güçlü bir şifre kullanın.'; label.className = 'text-xs text-danger mt-1'; }
                else if (score === 2) { label.textContent = 'Orta — biraz daha karmaşık olabilir.'; label.className = 'text-xs text-warning mt-1'; }
                else { label.textContent = 'Güçlü.'; label.className = 'text-xs text-success mt-1'; }
            }
        });
    }

    /* ---- şifre eşleşme check ---- */
    var matchEl = document.querySelector('[data-pw-match]');
    if (matchEl) {
        var primary = document.getElementById('Password') || document.getElementById('password');
        var msg = document.getElementById('matchMsg');
        function check() {
            if (!msg) return;
            if (!matchEl.value) { msg.classList.add('hidden'); return; }
            msg.classList.remove('hidden');
            if (primary.value === matchEl.value) {
                msg.textContent = 'Şifreler eşleşiyor.';
                msg.className = 'text-xs mt-1 text-success';
            } else {
                msg.textContent = 'Şifreler eşleşmiyor.';
                msg.className = 'text-xs mt-1 text-danger';
            }
        }
        matchEl.addEventListener('input', check);
        if (primary) primary.addEventListener('input', check);
    }

    /* ---- legal modal (KVKK / Kullanım Koşulları) ---- */
    var LEGAL = {
        kosul: {
            title: 'Kullanım Koşulları',
            body: [
                'Bu sözleşme, Zekids üzerinden yapılan tüm üyelik, ürün satın alma ve etkileşimleri düzenler.',
                'Üye, hesap bilgilerinin gizliliğinden ve doğru bilgi girmekten sorumludur.',
                'Sipariş, ödeme, kargo ve iade süreçleri ayrı sayfalarda açıklanır; bu sözleşme onların ayrılmaz parçasıdır.'
            ]
        },
        kvkk: {
            title: 'KVKK Aydınlatma Metni',
            body: [
                '6698 sayılı Kişisel Verilerin Korunması Kanunu kapsamında, kişisel verileriniz Zekids tarafından üyelik, sipariş takibi, pazarlama ve hizmet kalitesi geliştirme amaçlarıyla işlenir.',
                'Verileriniz açık rızanız olmadan üçüncü kişilerle paylaşılmaz; KVKK madde 11 kapsamında haklarınızı her zaman kullanabilirsiniz.',
                'Detaylı bilgi için kvkk@zekids.com adresinden bize ulaşabilirsiniz.'
            ]
        }
    };
    function openLegal(k) {
        var d = LEGAL[k];
        if (!d) return;
        document.getElementById('legalTitle').textContent = d.title;
        document.getElementById('legalBody').innerHTML = d.body.map(function (p) { return '<p>' + p + '</p>'; }).join('');
        document.getElementById('legalModal').classList.remove('hidden');
        document.body.style.overflow = 'hidden';
    }
    function closeLegal() {
        var m = document.getElementById('legalModal');
        if (!m) return;
        m.classList.add('hidden');
        document.body.style.overflow = '';
    }
    document.querySelectorAll('[data-open-legal]').forEach(function (b) {
        b.addEventListener('click', function () { openLegal(b.dataset.openLegal); });
    });
    document.querySelectorAll('[data-close-legal]').forEach(function (b) {
        b.addEventListener('click', closeLegal);
    });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') closeLegal(); });
})();
