/* Zekids Bebe — Ödeme (3 adımlı stepper). */
(function () {
    'use strict';

    var IMG = function (id, w) { return 'https://images.unsplash.com/photo-' + id + '?auto=format&fit=crop&w=' + (w || 120) + '&q=80'; };
    function toast(m) { if (window.toast) window.toast(m); }

    /* ---- order data ---- */
    var SUBTOTAL = 996;
    var ITEMS = [
        { name: 'Çiçekli Yazlık Elbise', variant: '104 · Pudra Pembe', qty: 1, line: 349, img: '1620774760711-caa4c94d683a' },
        { name: 'Eşofman Takımı',        variant: '110 · Bebek Mavisi', qty: 2, line: 398, img: '1604482858862-1db908a653e4' },
        { name: 'Pamuklu Tişört (3\'lü)', variant: '98',               qty: 1, line: 249, img: '1622290291468-a28f7a7dc6a8' },
    ];
    var SHIP = { standart: 0, express: 49.9, ayni: 79.9 };
    var shipMethod = 'standart';
    var giftOn = false;
    var step = 1;
    var done = new Set();
    var STEP_LABELS = ['Adres', 'Kargo', 'Ödeme'];

    /* ---- stepper ---- */
    function renderStepper() {
        var html = STEP_LABELS.map(function (label, i) {
            var n = i + 1;
            var isDone = done.has(n) && step !== n;
            var isActive = step === n;
            var circle = isDone
                ? '<span class="w-8 h-8 rounded-full bg-success text-white flex items-center justify-center"><svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 6 9 17l-5-5"/></svg></span>'
                : '<span class="w-8 h-8 rounded-full flex items-center justify-center text-sm font-semibold ' + (isActive ? 'bg-primary text-white' : 'border border-cream-300 text-muted') + '">' + n + '</span>';
            var connector = i < 2 ? '<span class="flex-1 h-0.5 mx-2 sm:mx-3 ' + (done.has(n) ? 'bg-success' : 'bg-cream-300') + '"></span>' : '';
            return '<button type="button" data-go-step="' + n + '" class="flex items-center ' + (i < 2 ? 'flex-1' : '') + '">' +
                '<span class="flex items-center gap-2">' + circle +
                '<span class="text-sm font-medium ' + (isActive ? 'text-charcoal' : 'text-muted') + ' hidden sm:inline">' + label + '</span></span>' +
                '</button>' + connector;
        }).join('');
        document.getElementById('stepper').innerHTML = html;
        document.querySelectorAll('#stepper [data-go-step]').forEach(function (b) {
            b.addEventListener('click', function () { goToStep(+b.dataset.goStep); });
        });
    }

    function goToStep(n) {
        for (var i = 1; i < n; i++) done.add(i);
        step = n;
        [1, 2, 3].forEach(function (i) {
            var body = document.getElementById('body' + i);
            var open = i === n;
            body.classList.toggle('hidden', !open);
            var sec = document.getElementById('sec' + i);
            var num = sec.querySelector('.secNum');
            var isDone = done.has(i) && !open;
            num.className = 'secNum w-7 h-7 rounded-full flex items-center justify-center text-sm font-semibold ' + (open ? 'bg-primary text-white' : (isDone ? 'bg-success text-white' : 'border border-cream-300 text-muted'));
            num.innerHTML = isDone
                ? '<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 6 9 17l-5-5"/></svg>'
                : i;
            var sum = document.getElementById('sum' + i);
            if (sum) sum.classList.toggle('hidden', !isDone);
        });
        renderStepper();
        var top = document.getElementById('sec' + n).getBoundingClientRect().top + window.scrollY - 90;
        window.scrollTo({ top: top, behavior: 'smooth' });
    }

    document.querySelectorAll('[data-go-step]').forEach(function (b) {
        b.addEventListener('click', function () { goToStep(+b.dataset.goStep); });
    });

    /* ---- address ---- */
    function pickAddress(which) {
        document.querySelectorAll('.addr-card').forEach(function (c) {
            var on = c.dataset.addr === which;
            c.classList.toggle('border-primary', on);
            c.classList.toggle('ring-2', on);
            c.classList.toggle('ring-primary', on);
            c.classList.toggle('border-cream-300', !on);
        });
    }
    document.querySelectorAll('[data-pick-address]').forEach(function (r) {
        r.addEventListener('change', function () { pickAddress(r.dataset.pickAddress); });
    });
    document.querySelectorAll('[data-edit-address]').forEach(function (b) {
        b.addEventListener('click', function () { toast('Adres düzenleme açılıyor'); });
    });

    function toggleNewAddr() { document.getElementById('newAddrForm').classList.toggle('hidden'); }
    document.querySelectorAll('[data-toggle-new-addr]').forEach(function (b) { b.addEventListener('click', toggleNewAddr); });
    document.querySelectorAll('[data-save-new-addr]').forEach(function (b) {
        b.addEventListener('click', function () { toggleNewAddr(); toast('Yeni adres kaydedildi'); });
    });

    document.querySelectorAll('[data-invoice]').forEach(function (b) {
        b.addEventListener('click', function () {
            var t = b.dataset.invoice;
            var bb = document.getElementById('invB'), kk = document.getElementById('invK');
            var isB = t === 'bireysel';
            bb.classList.toggle('seg-active', isB);
            bb.classList.toggle('text-charcoal', !isB);
            kk.classList.toggle('seg-active', !isB);
            kk.classList.toggle('text-charcoal', isB);
            document.getElementById('kurumsalFields').classList.toggle('hidden', isB);
        });
    });
    document.querySelectorAll('[data-toggle-switch]').forEach(function (el) {
        el.addEventListener('click', function () { el.dataset.on = el.dataset.on === '1' ? '0' : '1'; });
    });

    /* ---- il / ilçe ---- */
    var ILLER = {
        'İstanbul': ['Kadıköy', 'Maltepe', 'Ataşehir', 'Üsküdar', 'Beşiktaş', 'Bakırköy'],
        'Ankara':   ['Çankaya', 'Keçiören', 'Yenimahalle', 'Mamak'],
        'İzmir':    ['Konak', 'Karşıyaka', 'Bornova', 'Buca'],
        'Bursa':    ['Nilüfer', 'Osmangazi', 'Yıldırım'],
        'Antalya':  ['Muratpaşa', 'Konyaaltı', 'Kepez'],
    };
    var ilSel = document.getElementById('ilSelect');
    if (ilSel) {
        ilSel.innerHTML = '<option value="">İl seçin</option>' + Object.keys(ILLER).map(function (i) { return '<option>' + i + '</option>'; }).join('');
        ilSel.addEventListener('change', function () {
            var il = ilSel.value;
            var s = document.getElementById('ilceSelect');
            s.innerHTML = '<option value="">İlçe seçin</option>' + (ILLER[il] || []).map(function (i) { return '<option>' + i + '</option>'; }).join('');
        });
    }

    /* ---- masks ---- */
    document.querySelectorAll('[data-mask-digits]').forEach(function (el) {
        var len = +el.dataset.maskDigits;
        el.addEventListener('input', function () { el.value = el.value.replace(/\D/g, '').slice(0, len); });
    });
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
    document.querySelectorAll('[data-mask-card]').forEach(function (el) {
        el.addEventListener('input', function () {
            var d = el.value.replace(/\D/g, '').slice(0, 16);
            el.value = d.replace(/(.{4})/g, '$1 ').trim();
            var b = '';
            if (d[0] === '4') b = 'VISA';
            else if (d[0] === '5') b = 'Mastercard';
            else if (d[0] === '9') b = 'TROY';
            document.getElementById('cardBrand').textContent = b;
            document.getElementById('taksitWrap').classList.toggle('hidden', d.length < 6);
        });
    });
    document.querySelectorAll('[data-mask-expiry]').forEach(function (el) {
        el.addEventListener('input', function () {
            var d = el.value.replace(/\D/g, '').slice(0, 4);
            el.value = d.length >= 3 ? d.slice(0, 2) + '/' + d.slice(2) : d;
        });
    });
    document.querySelectorAll('[data-toggle-cvv]').forEach(function (b) {
        b.addEventListener('click', function () {
            var i = document.getElementById('cvv');
            i.type = i.type === 'password' ? 'text' : 'password';
        });
    });

    /* ---- shipping ---- */
    var SHIP_OPTS = [
        { id: 'standart', name: 'Standart Kargo', desc: '1-3 iş günü içinde teslim', price: 'Bedava', icon: '<path d="M10 17h4V5H2v12h3"/><path d="M20 17h2v-3.3a2 2 0 0 0-.6-1.4L18 9h-4v8h2"/><circle cx="7.5" cy="17.5" r="2.5"/><circle cx="17.5" cy="17.5" r="2.5"/>' },
        { id: 'express',  name: 'Express Kargo',  desc: 'Yarın elinizde',            price: '49,90₺', icon: '<path d="M13 2 3 14h7l-1 8 10-12h-7l1-8Z"/>' },
        { id: 'ayni',     name: 'Aynı Gün Teslimat', desc: 'İstanbul içi, bugün',    price: '79,90₺', icon: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>' },
    ];
    function renderShip() {
        var html = SHIP_OPTS.map(function (o) {
            var on = shipMethod === o.id;
            return '<label class="flex items-center gap-4 border-2 rounded-2xl p-4 cursor-pointer transition ' + (on ? 'border-primary bg-primary/5' : 'border-cream-300') + '">' +
                '<input type="radio" name="ship" value="' + o.id + '" ' + (on ? 'checked' : '') + ' data-ship="' + o.id + '" class="w-4 h-4 accent-[#FF8FB1]" />' +
                '<svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="' + (on ? '#FF8FB1' : '#7B8794') + '" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="shrink-0" aria-hidden="true">' + o.icon + '</svg>' +
                '<span class="flex-1"><span class="block font-medium text-charcoal text-sm">' + o.name + '</span><span class="block text-xs text-muted mt-0.5">' + o.desc + '</span></span>' +
                '<span class="font-semibold text-sm ' + (o.price === 'Bedava' ? 'text-success' : 'text-charcoal') + '">' + o.price + '</span>' +
                '</label>';
        }).join('');
        document.getElementById('shipOptions').innerHTML = html;
        document.querySelectorAll('[data-ship]').forEach(function (r) {
            r.addEventListener('change', function () { shipMethod = r.dataset.ship; renderShip(); recalc(); });
        });
    }

    /* ---- gift ---- */
    document.querySelectorAll('[data-toggle-gift]').forEach(function (el) {
        el.addEventListener('click', function () {
            giftOn = el.dataset.on !== '1';
            el.dataset.on = giftOn ? '1' : '0';
            document.getElementById('giftPanel').classList.toggle('hidden', !giftOn);
            recalc();
        });
    });
    document.querySelectorAll('[data-gift-color]').forEach(function (b) {
        b.addEventListener('click', function () {
            document.querySelectorAll('.giftc').forEach(function (x) { x.classList.remove('ring-2', 'ring-primary', 'ring-offset-2'); });
            b.classList.add('ring-2', 'ring-primary', 'ring-offset-2');
        });
    });
    var giftNote = document.getElementById('giftNote');
    if (giftNote) {
        giftNote.addEventListener('input', function () {
            document.getElementById('giftCount').textContent = giftNote.value.length;
        });
    }

    /* ---- recalc summary ---- */
    function recalc() {
        var ship = SHIP[shipMethod];
        var gift = giftOn ? 15 : 0;
        var total = SUBTOTAL + ship + gift;
        var shipEl = document.getElementById('ckShip');
        if (ship === 0) {
            shipEl.textContent = 'Bedava';
            shipEl.className = 'text-success font-medium';
        } else {
            shipEl.textContent = ship.toFixed(2).replace('.', ',') + '₺';
            shipEl.className = 'text-charcoal font-medium';
        }
        document.getElementById('ckGiftRow').classList.toggle('hidden', !giftOn);
        function fmt(n) { return Number.isInteger(n) ? n + '₺' : n.toFixed(2).replace('.', ',') + '₺'; }
        document.getElementById('ckTotal').textContent = fmt(total);
        var mob = document.getElementById('ckTotalMobile');
        if (mob) mob.textContent = fmt(total);
    }

    /* ---- payment tabs ---- */
    document.querySelectorAll('[data-pay-tab]').forEach(function (b) {
        b.addEventListener('click', function () {
            var name = b.dataset.payTab;
            document.querySelectorAll('.ptBtn').forEach(function (x) {
                var on = x.dataset.pt === name;
                x.classList.toggle('border-primary', on);
                x.classList.toggle('text-charcoal', on);
                x.classList.toggle('border-transparent', !on);
                x.classList.toggle('text-muted', !on);
            });
            document.querySelectorAll('.payPanel').forEach(function (p) {
                p.classList.toggle('hidden', p.dataset.pp !== name);
            });
        });
    });

    /* ---- taksit ---- */
    var TAKSIT = ['Tek Çekim', '3 Ay', '6 Ay', '9 Ay', '12 Ay'];
    var taksitSel = 0;
    function renderTaksit() {
        var html = TAKSIT.map(function (t, i) {
            var cls = i === taksitSel ? 'bg-primary text-white border-primary' : 'border-cream-300 text-charcoal hover:border-primary';
            return '<button type="button" data-taksit="' + i + '" class="px-3.5 py-2 rounded-xl border text-sm font-medium transition ' + cls + '">' + t + '</button>';
        }).join('');
        document.getElementById('taksit').innerHTML = html;
        document.querySelectorAll('[data-taksit]').forEach(function (b) {
            b.addEventListener('click', function () { taksitSel = +b.dataset.taksit; renderTaksit(); });
        });
    }

    document.querySelectorAll('[data-copy-iban]').forEach(function (b) {
        b.addEventListener('click', function () {
            if (navigator.clipboard) navigator.clipboard.writeText('TR12000100020003000400050006');
            toast('IBAN kopyalandı');
        });
    });

    /* ---- legal modal ---- */
    var LEGAL = {
        on: { title: 'Ön Bilgilendirme Formu', body: ['Bu form, 6502 sayılı Tüketicinin Korunması Hakkında Kanun kapsamında, satıcı ve alıcı bilgilerini, ürün/hizmet özelliklerini, cayma hakkını ve teslimat koşullarını düzenler.', 'Cayma hakkı: Teslim tarihinden itibaren 30 gün içinde gerekçe göstermeksizin sözleşmeden cayabilirsiniz. İade kargo ücreti tarafımızca karşılanır.', 'Teslimat: Siparişler onay sonrası 1-3 iş günü içinde kargoya verilir.'] },
        mesafeli: { title: 'Mesafeli Satış Sözleşmesi', body: ['İşbu sözleşme, alıcının elektronik ortamda sipariş verdiği ürünlerin satışı ve teslimi ile ilgili tarafların hak ve yükümlülüklerini kapsar.', 'Ödeme: Sipariş tutarı, seçilen ödeme yöntemiyle tahsil edilir. Kart bilgileriniz 256-bit SSL ile korunur ve saklanmaz.', 'Sözleşmenin onaylanmasıyla alıcı, ön bilgilendirme formundaki tüm koşulları kabul etmiş sayılır.'] },
    };
    function openLegal(k) {
        var d = LEGAL[k];
        document.getElementById('legalTitle').textContent = d.title;
        document.getElementById('legalBody').innerHTML = d.body.map(function (p) { return '<p>' + p + '</p>'; }).join('');
        document.getElementById('legalModal').classList.remove('hidden');
        document.body.style.overflow = 'hidden';
    }
    function closeLegal() {
        document.getElementById('legalModal').classList.add('hidden');
        document.body.style.overflow = '';
    }
    document.querySelectorAll('[data-open-legal]').forEach(function (b) {
        b.addEventListener('click', function () { openLegal(b.dataset.openLegal); });
    });
    document.querySelectorAll('[data-close-legal]').forEach(function (b) {
        b.addEventListener('click', closeLegal);
    });

    /* ---- complete ---- */
    document.querySelectorAll('[data-complete-order]').forEach(function (b) {
        b.addEventListener('click', function () {
            if (step < 3) { goToStep(3); return; }
            if (!document.getElementById('kvkk1').checked || !document.getElementById('kvkk2').checked) {
                toast('Lütfen sözleşmeleri onaylayın');
                return;
            }
            document.getElementById('orderSuccess').classList.remove('hidden');
            document.body.style.overflow = 'hidden';
        });
    });

    /* ---- summary items ---- */
    function renderSummaryItems() {
        var html = ITEMS.map(function (it) {
            return '<div class="flex items-center gap-3">' +
                '<div class="ph w-12 h-14 rounded-lg overflow-hidden bg-white shrink-0 relative"><img class="photo" loading="lazy" alt="' + it.name + '" src="' + IMG(it.img, 120) + '" /><span class="absolute -top-1.5 -right-1.5 bg-charcoal text-white text-[10px] w-4 h-4 rounded-full flex items-center justify-center">' + it.qty + '</span></div>' +
                '<div class="flex-1 min-w-0"><p class="text-sm text-charcoal truncate">' + it.name + '</p><p class="text-xs text-muted">' + it.variant + '</p></div>' +
                '<span class="text-sm font-medium text-charcoal whitespace-nowrap">' + it.line + '₺</span>' +
                '</div>';
        }).join('');
        document.getElementById('summaryItems').innerHTML = html;
    }

    /* ---- coupon mock ---- */
    document.querySelectorAll('[data-coupon-apply]').forEach(function (b) {
        b.addEventListener('click', function () { toast('Kupon kontrol ediliyor'); });
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') closeLegal();
    });

    /* ---- init ---- */
    renderShip();
    renderTaksit();
    renderSummaryItems();
    recalc();
    goToStep(1);
    pickAddress('ev');
})();
