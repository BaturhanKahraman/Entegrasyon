/* Zekids — Şifre Değiştir: göz toggle + kural/strength bar + eşleşme kontrolü. */
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

    var pw = document.querySelector('[data-pw-strength]');
    var pw2 = document.querySelector('[data-pw-match]');
    if (!pw) return;

    /* ---- kurallar ---- */
    var RULES = {
        'r-len': function (v) { return v.length >= 8; },
        'r-upper': function (v) { return /[A-ZĞÜŞİÖÇ]/.test(v); },
        'r-num': function (v) { return /\d/.test(v); },
        'r-special': function (v) { return /[^A-Za-z0-9À-ſĞğİıŞş]/.test(v); }
    };
    var bars = document.querySelectorAll('.strbar');
    var BAR_COLORS = ['bg-danger', 'bg-warning', 'bg-success'];

    function checkRules() {
        var v = pw.value;
        var passed = 0;
        Object.keys(RULES).forEach(function (id) {
            var ok = RULES[id](v);
            if (ok) passed++;
            var li = document.getElementById(id);
            if (!li) return;
            li.className = 'flex items-center gap-2 text-sm ' + (ok ? 'text-success' : 'text-muted');
            var dot = li.querySelector('.rdot');
            if (dot) dot.textContent = ok ? '✓' : '';
        });

        // 4 kuralı 3 bara eşle
        var s = passed <= 1 ? (v ? 1 : 0) : passed === 2 ? 2 : 3;
        bars.forEach(function (b, i) {
            b.className = 'strbar flex-1 h-1.5 rounded-full ' + (v && i < s ? BAR_COLORS[s - 1] : 'bg-cream-300');
        });

        checkMatch();
    }

    /* ---- eşleşme ---- */
    function checkMatch() {
        if (!pw2) return;
        var msg = document.getElementById('matchMsg');
        if (!msg) return;
        if (!pw2.value) { msg.classList.add('hidden'); return; }
        msg.classList.remove('hidden');
        if (pw.value === pw2.value) {
            msg.textContent = '✓ Şifreler eşleşiyor';
            msg.className = 'text-xs mt-1.5 text-success';
        } else {
            msg.textContent = '✕ Şifreler eşleşmiyor';
            msg.className = 'text-xs mt-1.5 text-danger';
        }
    }

    pw.addEventListener('input', checkRules);
    if (pw2) pw2.addEventListener('input', checkMatch);
})();
