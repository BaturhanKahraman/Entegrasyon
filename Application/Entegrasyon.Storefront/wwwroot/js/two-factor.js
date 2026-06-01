/* Zekids — Giriş 2FA doğrulama ekranı.
   OTP kutuları tek bir gizli `code` alanına yazılır; kurtarma kodu moduna geçince
   OTP devre dışı bırakılıp tek metin girişi (name=code) aktif edilir. */
(function () {
    'use strict';

    var form = document.querySelector('[data-tfa-form]');
    if (!form) return;

    var boxes = Array.prototype.slice.call(form.querySelectorAll('.tfa-box'));
    var hiddenCode = document.getElementById('tfaCode');
    var otpMode = form.querySelector('[data-tfa-otp-mode]');
    var recoveryMode = form.querySelector('[data-tfa-recovery-mode]');
    var recoveryInput = document.getElementById('recoveryCode');
    var useRecovery = document.getElementById('useRecovery');
    var toggleBtn = document.querySelector('[data-tfa-toggle-recovery]');
    var hint = document.querySelector('[data-tfa-hint]');

    function syncCode() {
        if (hiddenCode) hiddenCode.value = boxes.map(function (b) { return b.value; }).join('');
    }

    boxes.forEach(function (box, i) {
        box.addEventListener('input', function () {
            box.value = box.value.replace(/\D/g, '');
            if (box.value && i < boxes.length - 1) boxes[i + 1].focus();
            syncCode();
        });
        box.addEventListener('keydown', function (e) {
            if (e.key === 'Backspace' && !box.value && i > 0) boxes[i - 1].focus();
        });
        box.addEventListener('paste', function (e) {
            e.preventDefault();
            var d = (e.clipboardData.getData('text') || '').replace(/\D/g, '').slice(0, boxes.length);
            d.split('').forEach(function (ch, k) { if (boxes[k]) boxes[k].value = ch; });
            var next = Math.min(d.length, boxes.length - 1);
            if (boxes[next]) boxes[next].focus();
            syncCode();
        });
    });

    var recovery = false;
    if (toggleBtn) {
        toggleBtn.addEventListener('click', function () {
            recovery = !recovery;
            if (useRecovery) useRecovery.value = recovery ? 'true' : 'false';

            otpMode.classList.toggle('hidden', recovery);
            recoveryMode.classList.toggle('hidden', !recovery);

            // Yalnızca aktif moddaki `code` alanı submit edilsin diye diğerini disable et.
            if (hiddenCode) hiddenCode.disabled = recovery;
            if (recoveryInput) {
                recoveryInput.disabled = !recovery;
                recoveryInput.name = recovery ? 'code' : '';
            }

            toggleBtn.textContent = recovery ? 'Doğrulama kodu kullan' : 'Kurtarma kodu kullan';
            if (hint) {
                hint.textContent = recovery
                    ? 'Kayıt sırasında aldığınız kurtarma kodlarından birini girin.'
                    : 'Doğrulama uygulamanızda görünen 6 haneli kodu girin.';
            }
            if (recovery && recoveryInput) recoveryInput.focus();
            else if (boxes[0]) boxes[0].focus();
        });
    }

    if (boxes[0]) boxes[0].focus();
})();
