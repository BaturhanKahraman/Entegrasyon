/* Zekids — Hediye Çeki: oluşturma (tema/tutar/önizleme), bakiye kod gruplama, created kopyalama. */
(function () {
    'use strict';

    var notify = window.toast || function () {};
    var trFmt = function (n) { return Number(n).toLocaleString('tr-TR') + '₺'; };

    function copy(text, msg) {
        if (!text) return;
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text).then(function () { notify(msg); }).catch(function () { notify('Kopyalanamadı'); });
        } else {
            var ta = document.createElement('textarea');
            ta.value = text; ta.style.position = 'fixed'; ta.style.opacity = '0';
            document.body.appendChild(ta); ta.select();
            try { document.execCommand('copy'); notify(msg); } catch (e) { notify('Kopyalanamadı'); }
            document.body.removeChild(ta);
        }
    }

    /* ===== Oluşturma formu ===== */
    var form = document.querySelector('[data-giftcard-form]');
    if (form) {
        var amountHidden = document.getElementById('gcAmount');
        var custom = document.getElementById('gcCustom');
        var chips = Array.prototype.slice.call(form.querySelectorAll('.gc-amt'));
        var themeBtns = Array.prototype.slice.call(form.querySelectorAll('.gc-theme'));
        var nameInput = form.querySelector('[data-gc-name]');
        var msgInput = form.querySelector('[data-gc-message]');

        var preview = form.querySelector('[data-gc-preview]');
        var pvTheme = form.querySelector('[data-gc-pv-theme]');
        var pvName = form.querySelector('[data-gc-pv-name]');
        var pvMessage = form.querySelector('[data-gc-pv-message]');
        var pvAmount = form.querySelector('[data-gc-pv-amount]');
        var ctaAmount = form.querySelector('[data-gc-cta-amount]');

        var state = { grad: 'linear-gradient(135deg, #FF8FB1, #FFD56B)', themeName: 'Doğum Günü' };

        function amountValue() {
            var v = parseInt(amountHidden.value || '0', 10);
            return isNaN(v) ? 0 : v;
        }
        function updatePreview() {
            if (preview) preview.style.backgroundImage = state.grad;
            if (pvTheme) pvTheme.textContent = state.themeName;
            if (pvName) pvName.textContent = (nameInput && nameInput.value.trim()) || 'Alıcı adı';
            if (pvMessage) pvMessage.textContent = (msgInput && msgInput.value.trim()) || 'Mesajınız burada görünecek.';
            var amt = amountValue() > 0 ? trFmt(amountValue()) : '—';
            if (pvAmount) pvAmount.textContent = amt;
            if (ctaAmount) ctaAmount.textContent = amt;
        }
        function markChips() {
            chips.forEach(function (c) {
                var on = c.getAttribute('data-gc-amount') === String(amountValue());
                c.classList.toggle('bg-primary', on);
                c.classList.toggle('text-white', on);
                c.classList.toggle('border-primary', on);
                c.classList.toggle('border-cream-300', !on);
                c.classList.toggle('text-charcoal', !on);
            });
        }

        chips.forEach(function (chip) {
            chip.addEventListener('click', function () {
                amountHidden.value = chip.getAttribute('data-gc-amount');
                if (custom) custom.value = '';
                markChips(); updatePreview();
            });
        });
        if (custom) {
            custom.addEventListener('input', function () {
                var n = Math.max(0, parseInt(custom.value || '0', 10) || 0);
                amountHidden.value = String(n);
                markChips(); updatePreview();
            });
        }
        themeBtns.forEach(function (btn) {
            btn.addEventListener('click', function () {
                state.grad = btn.getAttribute('data-grad');
                state.themeName = btn.getAttribute('data-name');
                themeBtns.forEach(function (b) {
                    var box = b.querySelector('div');
                    var on = b === btn;
                    if (box) { box.classList.toggle('ring-primary', on); box.classList.toggle('ring-transparent', !on); }
                });
                updatePreview();
            });
        });
        if (nameInput) nameInput.addEventListener('input', updatePreview);
        if (msgInput) msgInput.addEventListener('input', updatePreview);

        form.addEventListener('submit', function (e) {
            if (amountValue() < 50) {
                e.preventDefault();
                notify('Lütfen en az 50₺ tutar girin');
                if (custom) custom.focus();
            }
        });

        markChips();
        updatePreview();
    }

    /* ===== Bakiye sorgulama — ZBGC-XXXX-XXXX gruplama ===== */
    var balanceCode = document.querySelector('[data-gc-balance-code]');
    if (balanceCode) {
        balanceCode.addEventListener('input', function () {
            var v = balanceCode.value.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 12);
            var parts = [];
            if (v.length > 0) parts.push(v.slice(0, 4));
            if (v.length > 4) parts.push(v.slice(4, 8));
            if (v.length > 8) parts.push(v.slice(8, 12));
            balanceCode.value = parts.join('-');
        });
    }

    /* ===== Created — kod kopyalama ===== */
    document.querySelectorAll('[data-copy-giftcode]').forEach(function (btn) {
        btn.addEventListener('click', function () { copy(btn.getAttribute('data-code') || '', 'Kod kopyalandı'); });
    });

    /* ===== Genel karakter sayacı (gift card mesaj) ===== */
    document.querySelectorAll('[data-char-count]').forEach(function (el) {
        var out = document.getElementById(el.getAttribute('data-char-count'));
        if (!out) return;
        var u = function () { out.textContent = el.value.length; };
        el.addEventListener('input', u); u();
    });
})();
