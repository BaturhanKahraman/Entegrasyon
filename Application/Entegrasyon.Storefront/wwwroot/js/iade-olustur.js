/* ============ Zekids Bebe — İade Talebi Oluştur ============ */
(function () {
    'use strict';

    function notify(msg) { if (window.toast) window.toast(msg); }
    function fmt(n) { return n.toLocaleString('tr-TR') + '₺'; }

    /* ===== mobile account sheet ===== */
    var sheet = document.getElementById('accSheet');
    document.querySelectorAll('[data-acc-open]').forEach(function (b) {
        b.addEventListener('click', function () { if (sheet) { sheet.classList.remove('hidden'); document.body.style.overflow = 'hidden'; } });
    });
    document.querySelectorAll('[data-acc-close]').forEach(function (b) {
        b.addEventListener('click', function () { if (sheet) { sheet.classList.add('hidden'); document.body.style.overflow = ''; } });
    });

    var form = document.querySelector('[data-return-form]');
    if (!form) return;

    var items = Array.prototype.slice.call(form.querySelectorAll('[data-item]'));
    var summaryItemsEl = form.querySelector('[data-summary-items]');
    var summaryEmptyEl = form.querySelector('[data-summary-empty]');
    var summaryCountEl = form.querySelector('[data-summary-count]');
    var summaryTotalEl = form.querySelector('[data-summary-total]');
    var submitBtn = form.querySelector('[data-submit-btn]');

    function itemData(row) {
        return {
            row: row,
            check: row.querySelector('[data-item-check]'),
            box: row.querySelector('[data-check-box]'),
            icon: row.querySelector('[data-check-icon]'),
            qtyWrap: row.querySelector('[data-qty-wrap]'),
            qtyLabel: row.querySelector('[data-qty-label]'),
            qtyInput: row.querySelector('[data-qty-input]'),
            dec: row.querySelector('[data-qty-dec]'),
            inc: row.querySelector('[data-qty-inc]'),
            line: row.querySelector('[data-line]'),
            name: (row.querySelector('.font-medium') || {}).textContent || '',
            img: (row.querySelector('img.photo') || {}).src || '',
            price: parseInt(row.dataset.price, 10) || 0,
            max: parseInt(row.dataset.max, 10) || 1
        };
    }

    function selectedReason() {
        var r = form.querySelector('[data-reason-radio]:checked');
        return r ? r.value : null;
    }

    function renderSummary() {
        var rows = items.map(itemData).filter(function (d) { return d.check && d.check.checked; });
        var total = 0, count = 0;
        if (rows.length === 0) {
            if (summaryItemsEl) summaryItemsEl.innerHTML = '';
            if (summaryEmptyEl) summaryEmptyEl.classList.remove('hidden');
        } else {
            if (summaryEmptyEl) summaryEmptyEl.classList.add('hidden');
            if (summaryItemsEl) {
                summaryItemsEl.innerHTML = rows.map(function (d) {
                    var q = parseInt(d.qtyInput.value, 10) || 1;
                    total += q * d.price; count += q;
                    return '<div class="flex items-center gap-3">' +
                        '<div class="ph w-11 h-12 rounded-lg overflow-hidden bg-white shrink-0"><img class="photo" loading="lazy" alt="" src="' + d.img + '" /></div>' +
                        '<div class="min-w-0 flex-1"><p class="text-sm font-medium text-charcoal truncate">' + d.name + '</p>' +
                        '<p class="text-xs text-muted">' + q + ' adet · ' + d.price + '₺</p></div>' +
                        '<span class="text-sm font-semibold text-charcoal shrink-0">' + fmt(q * d.price) + '</span>' +
                        '</div>';
                }).join('');
            }
        }
        if (summaryCountEl) summaryCountEl.textContent = count + ' adet';
        if (summaryTotalEl) summaryTotalEl.textContent = fmt(total);
        if (submitBtn) submitBtn.disabled = !(count > 0 && selectedReason());
    }

    /* ===== item selection + qty ===== */
    items.forEach(function (row) {
        var d = itemData(row);
        if (!d.check) return;

        d.check.addEventListener('change', function () {
            var on = d.check.checked;
            if (d.qtyWrap) d.qtyWrap.classList.toggle('hidden', !on);
            if (d.qtyWrap) d.qtyWrap.classList.toggle('flex', on);
            if (d.icon) d.icon.classList.toggle('hidden', !on);
            if (d.box) {
                d.box.classList.toggle('bg-primary', on);
                d.box.classList.toggle('border-primary', on);
                d.box.classList.toggle('border-cream-300', !on);
            }
            row.classList.toggle('bg-primary/5', on);
            renderSummary();
        });

        function setQty(q) {
            q = Math.max(1, Math.min(d.max, q));
            if (d.qtyInput) d.qtyInput.value = q;
            if (d.qtyLabel) d.qtyLabel.textContent = q;
            if (d.line) d.line.textContent = (q * d.price) + '₺';
            if (d.dec) d.dec.disabled = q <= 1;
            if (d.inc) d.inc.disabled = q >= d.max;
            renderSummary();
        }
        if (d.dec) d.dec.addEventListener('click', function () { setQty((parseInt(d.qtyInput.value, 10) || 1) - 1); });
        if (d.inc) d.inc.addEventListener('click', function () { setQty((parseInt(d.qtyInput.value, 10) || 1) + 1); });
        // initial stepper disabled state
        setQty(parseInt(d.qtyInput.value, 10) || d.max);
    });

    /* ===== radio dot helper (shared by reason + solution) ===== */
    function paintDot(label, on) {
        var dot = label.querySelector('[data-radio-dot]');
        var fill = label.querySelector('[data-radio-fill]');
        if (dot) {
            dot.classList.toggle('border-primary', on);
            dot.classList.toggle('border-cream-300', !on);
        }
        if (fill) {
            fill.classList.toggle('opacity-100', on);
            fill.classList.toggle('opacity-0', !on);
        }
    }

    /* ===== reason radios ===== */
    var reasonOtherWrap = form.querySelector('[data-reason-other-wrap]');
    var reasonRadios = Array.prototype.slice.call(form.querySelectorAll('[data-reason-radio]'));
    function paintReasons() {
        reasonRadios.forEach(function (radio) {
            paintDot(radio.closest('label'), radio.checked);
        });
    }
    reasonRadios.forEach(function (radio) {
        radio.addEventListener('change', function () {
            paintReasons();
            if (reasonOtherWrap) reasonOtherWrap.classList.toggle('hidden', radio.value !== 'Diğer');
            renderSummary();
        });
    });

    /* ===== solution radios ===== */
    var solutionRadios = Array.prototype.slice.call(form.querySelectorAll('[data-solution-radio]'));
    function paintSolutions() {
        solutionRadios.forEach(function (radio) {
            var label = radio.closest('[data-solution-item]');
            if (!label) return;
            var on = radio.checked;
            label.classList.toggle('border-primary', on);
            label.classList.toggle('bg-primary/5', on);
            label.classList.toggle('border-cream-300', !on);
            var icon = label.querySelector('[data-sol-icon]');
            if (icon) {
                icon.classList.toggle('bg-primary', on);
                icon.classList.toggle('text-white', on);
                icon.classList.toggle('bg-cream', !on);
                icon.classList.toggle('text-primary', !on);
            }
            paintDot(label, on);
        });
    }
    solutionRadios.forEach(function (radio) {
        radio.addEventListener('change', paintSolutions);
    });

    /* ===== photo upload (preview + drag-drop + removal) ===== */
    var photoInput = form.querySelector('[data-photo-input]');
    var photoGrid = form.querySelector('[data-photo-grid]');
    var dropZone = form.querySelector('[data-drop-zone]');

    function syncInputFiles(files) {
        // Rebuild the input's FileList so removals persist on submit
        var dt = new DataTransfer();
        files.forEach(function (f) { dt.items.add(f); });
        photoInput.files = dt.files;
    }

    function renderPhotos() {
        if (!photoGrid || !photoInput) return;
        var files = Array.prototype.slice.call(photoInput.files);
        photoGrid.classList.toggle('hidden', files.length === 0);
        photoGrid.innerHTML = files.map(function (f, i) {
            var url = URL.createObjectURL(f);
            return '<div class="relative aspect-square rounded-xl overflow-hidden bg-cream border border-cream-300">' +
                '<img src="' + url + '" alt="' + f.name + '" class="w-full h-full object-cover" />' +
                '<button type="button" data-remove-photo="' + i + '" aria-label="Kaldır" class="absolute top-1.5 right-1.5 w-6 h-6 rounded-full bg-charcoal/70 text-white flex items-center justify-center hover:bg-charcoal transition">' +
                '<svg viewBox="0 0 24 24" width="13" height="13" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" aria-hidden="true"><path d="M18 6 6 18M6 6l12 12"/></svg>' +
                '</button></div>';
        }).join('');
        photoGrid.querySelectorAll('[data-remove-photo]').forEach(function (b) {
            b.addEventListener('click', function () {
                var files = Array.prototype.slice.call(photoInput.files);
                files.splice(parseInt(b.dataset.removePhoto, 10), 1);
                syncInputFiles(files);
                renderPhotos();
            });
        });
    }

    if (photoInput) {
        photoInput.addEventListener('change', renderPhotos);
    }
    if (dropZone && photoInput) {
        ['dragenter', 'dragover'].forEach(function (ev) {
            dropZone.addEventListener(ev, function (e) { e.preventDefault(); dropZone.classList.add('border-primary', 'bg-primary/5'); });
        });
        ['dragleave', 'drop'].forEach(function (ev) {
            dropZone.addEventListener(ev, function (e) { e.preventDefault(); dropZone.classList.remove('border-primary', 'bg-primary/5'); });
        });
        dropZone.addEventListener('drop', function (e) {
            if (!e.dataTransfer || !e.dataTransfer.files) return;
            var files = Array.prototype.slice.call(photoInput.files).concat(Array.prototype.slice.call(e.dataTransfer.files));
            files = files.filter(function (f) { return f.type.indexOf('image/') === 0; });
            syncInputFiles(files);
            renderPhotos();
        });
    }

    /* ===== submit guard ===== */
    form.addEventListener('submit', function (e) {
        var anyChecked = items.some(function (row) { var c = row.querySelector('[data-item-check]'); return c && c.checked; });
        if (!anyChecked) { e.preventDefault(); notify('Lütfen en az bir ürün seçin'); return; }
        if (!selectedReason()) { e.preventDefault(); notify('Lütfen bir iade sebebi seçin'); return; }
    });

    /* ===== init ===== */
    paintReasons();
    paintSolutions();
    renderSummary();
})();
