/* Zekids — İki Aşamalı Doğrulama sihirbazı.
   Adım gezinme + yöntem seçimi + dekoratif QR + OTP auto-focus + yedek kod kopyala/indir. */
(function () {
    'use strict';

    var STEPS = ['Yöntem', 'Kurulum', 'Doğrula', 'Yedek Kodlar'];
    var stepperEl = document.querySelector('[data-stepper]');
    if (!stepperEl) return;

    var panels = document.querySelectorAll('.wz-panel');
    var curStep = 1;
    var method = 'auth';

    /* ---- toast (site geneli yoksa minimal fallback) ---- */
    function notify(msg) {
        if (window.notyf && typeof window.notyf.success === 'function') {
            window.notyf.success(msg);
            return;
        }
        var t = document.querySelector('[data-toast]');
        if (!t) {
            t = document.createElement('div');
            t.setAttribute('data-toast', '');
            t.className = 'fixed bottom-5 left-1/2 -translate-x-1/2 z-[70] bg-charcoal text-white text-sm px-5 py-3 rounded-full shadow-md';
            document.body.appendChild(t);
        }
        t.textContent = msg;
        t.classList.remove('hidden');
        clearTimeout(t._timer);
        t._timer = setTimeout(function () { t.classList.add('hidden'); }, 2000);
    }

    /* ---- stepper ---- */
    function renderStepper() {
        stepperEl.innerHTML = STEPS.map(function (label, i) {
            var n = i + 1;
            var done = n < curStep;
            var active = n === curStep;
            var circle = done
                ? '<span class="w-8 h-8 rounded-full bg-primary text-white flex items-center justify-center text-sm shrink-0"><svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5"/></svg></span>'
                : '<span class="w-8 h-8 rounded-full flex items-center justify-center text-sm font-semibold shrink-0 ' + (active ? 'bg-primary text-white' : 'bg-cream text-muted') + '">' + n + '</span>';
            var line = n < STEPS.length ? '<span class="flex-1 h-0.5 mx-2 ' + (n < curStep ? 'bg-primary' : 'bg-cream-300') + '"></span>' : '';
            return '<li class="flex items-center ' + (n < STEPS.length ? 'flex-1' : '') + '">' + circle +
                '<span class="hidden sm:inline ml-2.5 text-sm font-medium ' + (active || done ? 'text-charcoal' : 'text-muted') + '">' + label + '</span>' + line + '</li>';
        }).join('');
    }

    function showPanel() {
        panels.forEach(function (p) {
            p.classList.toggle('hidden', parseInt(p.dataset.panel, 10) !== curStep);
        });
    }

    function goStep(n) {
        curStep = n;
        renderStepper();
        showPanel();
        if (n === 2) {
            var authBox = document.querySelector('[data-setup="auth"]');
            var smsBox = document.querySelector('[data-setup="sms"]');
            if (authBox) authBox.classList.toggle('hidden', method !== 'auth');
            if (smsBox) smsBox.classList.toggle('hidden', method === 'auth');
            var desc = document.querySelector('[data-setup-desc]');
            if (desc) {
                desc.textContent = method === 'auth'
                    ? 'Google Authenticator, Authy veya 1Password gibi uygulamanızla QR kodu tarayın.'
                    : 'Telefon numaranızı doğrulayın, size bir kod gönderelim.';
            }
        }
        if (n === 3) {
            setTimeout(function () {
                var f = document.querySelector('[data-otp-input] input');
                if (f) f.focus();
            }, 60);
        }
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    document.querySelectorAll('[data-go-step]').forEach(function (btn) {
        btn.addEventListener('click', function () { goStep(parseInt(btn.dataset.goStep, 10)); });
    });

    /* ---- yöntem seçimi ---- */
    document.querySelectorAll('[data-tfa-method]').forEach(function (card) {
        card.addEventListener('click', function () {
            method = card.dataset.tfaMethod;
            document.querySelectorAll('[data-tfa-method]').forEach(function (c) {
                var on = c.dataset.tfaMethod === method;
                c.classList.toggle('border-primary', on);
                c.classList.toggle('bg-primary/5', on);
                c.classList.toggle('border-cream-300', !on);
                var radio = c.querySelector('input[type="radio"]');
                if (radio) radio.checked = on;
            });
        });
    });

    /* ---- telefon mask ---- */
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
    document.querySelectorAll('[data-send-code]').forEach(function (btn) {
        btn.addEventListener('click', function () { notify('Doğrulama kodu gönderildi'); });
    });

    /* ---- dekoratif QR (gerçek QR yoksa) ---- */
    function makeQR(el) {
        var N = 23;
        var seed = 1337;
        var rnd = function () { seed = (seed * 1103515245 + 12345) & 0x7fffffff; return seed / 0x7fffffff; };
        var finder = function (r, c) { var dr = Math.min(r, 6 - r), dc = Math.min(c, 6 - c), d = Math.min(dr, dc); return d !== 1; };
        var cells = '';
        for (var r = 0; r < N; r++) {
            for (var c = 0; c < N; c++) {
                var on;
                var inTL = r < 7 && c < 7, inTR = r < 7 && c >= N - 7, inBL = r >= N - 7 && c < 7;
                if (inTL) on = finder(r, c);
                else if (inTR) on = finder(r, c - (N - 7));
                else if (inBL) on = finder(r - (N - 7), c);
                else on = rnd() > 0.52;
                cells += '<span style="background:' + (on ? '#2C3E50' : 'transparent') + '"></span>';
            }
        }
        el.innerHTML = cells;
    }
    var qrEl = document.querySelector('[data-qr]');
    if (qrEl) makeQR(qrEl);

    /* ---- OTP girişleri ---- */
    var otpWrap = document.querySelector('[data-otp-input]');
    if (otpWrap) {
        var html = '';
        for (var i = 0; i < 6; i++) {
            html += '<input type="text" inputmode="numeric" maxlength="1" aria-label="Hane ' + (i + 1) + '" class="w-11 h-14 sm:w-12 sm:h-14 text-center text-xl font-semibold rounded-xl border border-cream-300 outline-none focus:border-primary text-charcoal" />';
        }
        otpWrap.innerHTML = html;
        var inputs = otpWrap.querySelectorAll('input');
        inputs.forEach(function (inp, idx) {
            inp.addEventListener('input', function () {
                inp.value = inp.value.replace(/\D/g, '');
                if (inp.value && idx < inputs.length - 1) inputs[idx + 1].focus();
            });
            inp.addEventListener('keydown', function (e) {
                if (e.key === 'Backspace' && !inp.value && idx > 0) inputs[idx - 1].focus();
            });
            inp.addEventListener('paste', function (e) {
                e.preventDefault();
                var d = ((e.clipboardData || window.clipboardData).getData('text') || '').replace(/\D/g, '').slice(0, 6);
                d.split('').forEach(function (ch, k) { if (inputs[k]) inputs[k].value = ch; });
                var last = Math.min(d.length, inputs.length - 1);
                if (inputs[last]) inputs[last].focus();
            });
        });
    }

    /* ---- yedek kodlar ---- */
    function getCodes() {
        return Array.prototype.map.call(
            document.querySelectorAll('[data-codes] > div'),
            function (d) { return d.textContent.trim(); }
        );
    }
    var copyBtn = document.querySelector('[data-copy-codes]');
    if (copyBtn) {
        copyBtn.addEventListener('click', function () {
            var text = getCodes().join('\n');
            if (navigator.clipboard) navigator.clipboard.writeText(text).catch(function () {});
            notify('Yedek kodlar kopyalandı');
        });
    }
    var dlBtn = document.querySelector('[data-download-codes]');
    if (dlBtn) {
        dlBtn.addEventListener('click', function () {
            var blob = new Blob(['Zekids Bebe — Yedek Kodlar\n\n' + getCodes().join('\n')], { type: 'text/plain' });
            var a = document.createElement('a');
            a.href = URL.createObjectURL(blob);
            a.download = 'zekids-yedek-kodlar.txt';
            a.click();
            URL.revokeObjectURL(a.href);
            notify('Kodlar indiriliyor');
        });
    }

    /* ---- secret kopyala ---- */
    var secretBtn = document.querySelector('[data-copy-secret]');
    if (secretBtn) {
        secretBtn.addEventListener('click', function () {
            var el = document.querySelector('[data-secret]');
            var val = el ? el.textContent.replace(/\s/g, '') : '';
            if (navigator.clipboard) navigator.clipboard.writeText(val).catch(function () {});
            notify('Anahtar kopyalandı');
        });
    }

    /* ---- init ---- */
    renderStepper();
    showPanel();
})();
