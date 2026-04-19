// receipt-editor.js — fiş şablonu editörü
// Güvenlik: kullanıcı içeriği DOM API (.textContent, createElement) ile yazılır;
// server-rendered preview HTML'i iframe srcdoc üzerinden sandbox'lu render edilir
// (IReceiptRenderer HtmlEncoder.Create(UnicodeRanges.All) ile output encode ediyor).
(function () {
    'use strict';

    var state = window.__rcptState;
    var labels = {
        logo: 'Logo', store_info: 'Mağaza Bilgisi', text: 'Metin', divider: 'Ayırıcı',
        meta: 'Meta Satırı', items: 'Ürün Listesi', totals: 'Toplamlar',
        payments: 'Ödeme Özeti', vat_summary: 'KDV Özeti', return_code: 'İade Kodu', spacer: 'Boşluk'
    };

    function uuid() {
        return 'b-' + Math.random().toString(36).slice(2, 10) + Date.now().toString(36);
    }

    function defaultBlock(type) {
        var base = { id: uuid(), type: type, showInNormal: true, showInGift: true };
        switch (type) {
            case 'logo':        return Object.assign(base, { settings: {} });
            case 'store_info':  return Object.assign(base, { settings: { align: 'center', size: 'm' } });
            case 'text':        return Object.assign(base, { settings: { content: 'Metin giriniz', align: 'left', bold: false, size: 'm' } });
            case 'divider':     return Object.assign(base, { settings: { style: 'dashed', color: 'black' } });
            case 'meta':        return Object.assign(base, { settings: { showSaleNumber: true, showDate: true, showCashier: true, showCustomer: true } });
            case 'items':       return Object.assign(base, { settings: { showBarcode: false, showVatColumn: false } });
            case 'totals':      return Object.assign(base, { showInGift: false, settings: {} });
            case 'payments':    return Object.assign(base, { showInGift: false, settings: {} });
            case 'vat_summary': return Object.assign(base, { showInGift: false, settings: {} });
            case 'return_code': return Object.assign(base, { settings: { showLabel: true } });
            case 'spacer':      return Object.assign(base, { settings: { size: 'm' } });
        }
        return base;
    }

    function thermalBlocks() {
        try { return JSON.parse(state.template.thermalJson) || []; }
        catch (e) { return []; }
    }
    function a4Zones() {
        try { return JSON.parse(state.template.a4Json) || { hl: [], hr: [], body: [], fl: [], fr: [] }; }
        catch (e) { return { hl: [], hr: [], body: [], fl: [], fr: [] }; }
    }
    function saveThermalBlocks(blocks) { state.template.thermalJson = JSON.stringify(blocks); }
    function saveA4Zones(zones) { state.template.a4Json = JSON.stringify(zones); }

    function getAntiForgeryToken() {
        var t = document.querySelector('#anti-forgery-form input[name="__RequestVerificationToken"]');
        return t ? t.value : '';
    }

    function el(tag, className, attrs) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (attrs) {
            for (var k in attrs) {
                if (k === 'text') node.textContent = attrs[k];
                else node.setAttribute(k, attrs[k]);
            }
        }
        return node;
    }

    function iconBtn(iconClass, btnClass, title) {
        var btn = el('button', 'btn btn-sm ' + btnClass, { type: 'button', title: title || '' });
        var icon = el('i', 'ti ' + iconClass);
        btn.appendChild(icon);
        return btn;
    }

    function emptyHint() {
        return el('div', 'rcpt-zone-empty text-secondary small text-center py-2', { text: 'Buraya blok sürükle' });
    }

    function blockSummary(b) {
        switch (b.type) {
            case 'text': return (b.settings && b.settings.content ? b.settings.content.slice(0, 40) : '(boş)');
            case 'divider': return (b.settings && b.settings.style ? b.settings.style : 'dashed') + ' / ' + (b.settings && b.settings.color ? b.settings.color : 'black');
            case 'spacer': return 'boyut: ' + (b.settings && b.settings.size ? b.settings.size : 'm');
            case 'meta': {
                var flags = [];
                if (b.settings && b.settings.showSaleNumber) flags.push('no');
                if (b.settings && b.settings.showDate) flags.push('tarih');
                if (b.settings && b.settings.showCashier) flags.push('kasiyer');
                if (b.settings && b.settings.showCustomer) flags.push('müşteri');
                return flags.length ? flags.join(', ') : '(hiçbiri)';
            }
            default: return '';
        }
    }

    function buildBlockRow(block) {
        var row = el('div', 'rcpt-block-row d-flex align-items-center gap-2 p-2 border rounded mb-1');
        row.setAttribute('data-block-id', block.id);

        row.appendChild(el('i', 'ti ti-grip-vertical rcpt-drag-handle text-secondary'));

        var info = el('div', 'flex-grow-1');
        info.appendChild(el('div', 'fw-bold small', { text: labels[block.type] || block.type }));
        info.appendChild(el('div', 'text-secondary small', { text: blockSummary(block) }));
        row.appendChild(info);

        var normalBtn = iconBtn('ti-receipt', block.showInNormal ? 'btn-ghost-primary' : 'btn-ghost-secondary', 'Normal fişte göster');
        normalBtn.setAttribute('data-toggle-mode', 'normal');
        row.appendChild(normalBtn);

        var giftBtn = iconBtn('ti-gift', block.showInGift ? 'btn-ghost-primary' : 'btn-ghost-secondary', 'Hediye fişinde göster');
        giftBtn.setAttribute('data-toggle-mode', 'gift');
        row.appendChild(giftBtn);

        var editBtn = iconBtn('ti-settings', 'btn-ghost-secondary', 'Düzenle');
        editBtn.setAttribute('data-edit-block', '');
        row.appendChild(editBtn);

        var delBtn = iconBtn('ti-x', 'btn-ghost-danger', 'Kaldır');
        delBtn.setAttribute('data-remove-block', '');
        row.appendChild(delBtn);

        return row;
    }

    function buildZoneContainer(zoneKey, blocks) {
        var zone = el('div', 'rcpt-zone');
        zone.setAttribute('data-zone', zoneKey);
        if (blocks.length === 0) {
            zone.appendChild(emptyHint());
        } else {
            blocks.forEach(function (b) { zone.appendChild(buildBlockRow(b)); });
        }
        return zone;
    }

    function labelNode(txt) { return el('div', 'rcpt-zone-label', { text: txt }); }

    function renderEditor() {
        var container = document.getElementById('editor-zones');
        while (container.firstChild) container.removeChild(container.firstChild);

        if (state.activeSize === 'thermal') {
            container.appendChild(buildZoneContainer('thermal-body', thermalBlocks()));
        } else {
            var zones = a4Zones();
            var topRow = el('div', 'row g-2 mb-2');
            var hlCol = el('div', 'col-6');
            hlCol.appendChild(labelNode('Header-Left'));
            hlCol.appendChild(buildZoneContainer('hl', zones.hl));
            topRow.appendChild(hlCol);
            var hrCol = el('div', 'col-6');
            hrCol.appendChild(labelNode('Header-Right'));
            hrCol.appendChild(buildZoneContainer('hr', zones.hr));
            topRow.appendChild(hrCol);
            container.appendChild(topRow);

            container.appendChild(labelNode('Body'));
            var bodyZone = buildZoneContainer('body', zones.body);
            bodyZone.classList.add('mb-2');
            container.appendChild(bodyZone);

            var botRow = el('div', 'row g-2');
            var flCol = el('div', 'col-6');
            flCol.appendChild(labelNode('Footer-Left'));
            flCol.appendChild(buildZoneContainer('fl', zones.fl));
            botRow.appendChild(flCol);
            var frCol = el('div', 'col-6');
            frCol.appendChild(labelNode('Footer-Right'));
            frCol.appendChild(buildZoneContainer('fr', zones.fr));
            botRow.appendChild(frCol);
            container.appendChild(botRow);
        }

        container.querySelectorAll('[data-zone]').forEach(function (zoneEl) {
            new Sortable(zoneEl, {
                group: 'receipt-blocks',
                handle: '.rcpt-drag-handle',
                animation: 150,
                onEnd: captureZonesFromDom
            });
        });

        wireBlockActions();
    }

    function captureZonesFromDom() {
        var byId = {};
        var allOld = state.activeSize === 'thermal'
            ? thermalBlocks()
            : [].concat(a4Zones().hl, a4Zones().hr, a4Zones().body, a4Zones().fl, a4Zones().fr);
        allOld.forEach(function (b) { byId[b.id] = b; });

        if (state.activeSize === 'thermal') {
            var zone = document.querySelector('[data-zone="thermal-body"]');
            var ordered = [];
            zone.querySelectorAll('.rcpt-block-row').forEach(function (row) {
                var id = row.getAttribute('data-block-id');
                if (byId[id]) ordered.push(byId[id]);
            });
            saveThermalBlocks(ordered);
        } else {
            var zones = { hl: [], hr: [], body: [], fl: [], fr: [] };
            ['hl', 'hr', 'body', 'fl', 'fr'].forEach(function (key) {
                var zEl = document.querySelector('[data-zone="' + key + '"]');
                if (!zEl) return;
                zEl.querySelectorAll('.rcpt-block-row').forEach(function (row) {
                    var id = row.getAttribute('data-block-id');
                    if (byId[id]) zones[key].push(byId[id]);
                });
            });
            saveA4Zones(zones);
        }
        refreshPreview();
    }

    function addBlock(type) {
        var block = defaultBlock(type);
        if (state.activeSize === 'thermal') {
            var list = thermalBlocks();
            list.push(block);
            saveThermalBlocks(list);
        } else {
            var zones = a4Zones();
            zones.body.push(block);
            saveA4Zones(zones);
        }
        renderEditor();
        refreshPreview();
    }

    function removeBlock(id) {
        if (state.activeSize === 'thermal') {
            saveThermalBlocks(thermalBlocks().filter(function (b) { return b.id !== id; }));
        } else {
            var zones = a4Zones();
            ['hl', 'hr', 'body', 'fl', 'fr'].forEach(function (k) {
                zones[k] = zones[k].filter(function (b) { return b.id !== id; });
            });
            saveA4Zones(zones);
        }
        renderEditor();
        refreshPreview();
    }

    function findBlock(id) {
        if (state.activeSize === 'thermal') {
            return thermalBlocks().find(function (b) { return b.id === id; });
        }
        var z = a4Zones();
        return [].concat(z.hl, z.hr, z.body, z.fl, z.fr).find(function (b) { return b.id === id; });
    }

    function updateBlock(id, mutator) {
        function apply(arr) {
            var idx = arr.findIndex(function (b) { return b.id === id; });
            if (idx >= 0) arr[idx] = Object.assign({}, arr[idx], mutator(arr[idx]));
        }
        if (state.activeSize === 'thermal') {
            var list = thermalBlocks();
            apply(list);
            saveThermalBlocks(list);
        } else {
            var z = a4Zones();
            ['hl', 'hr', 'body', 'fl', 'fr'].forEach(function (k) { apply(z[k]); });
            saveA4Zones(z);
        }
        renderEditor();
        refreshPreview();
    }

    function toggleMode(id, which) {
        updateBlock(id, function (b) {
            return which === 'normal' ? { showInNormal: !b.showInNormal } : { showInGift: !b.showInGift };
        });
    }

    function wireBlockActions() {
        document.querySelectorAll('[data-block-id]').forEach(function (row) {
            var id = row.getAttribute('data-block-id');
            var remove = row.querySelector('[data-remove-block]');
            var edit = row.querySelector('[data-edit-block]');
            var normalT = row.querySelector('[data-toggle-mode="normal"]');
            var giftT = row.querySelector('[data-toggle-mode="gift"]');
            if (remove) remove.addEventListener('click', function () { removeBlock(id); });
            if (edit) edit.addEventListener('click', function () { openBlockSettings(id); });
            if (normalT) normalT.addEventListener('click', function () { toggleMode(id, 'normal'); });
            if (giftT) giftT.addEventListener('click', function () { toggleMode(id, 'gift'); });
        });
    }

    function buildSettingsForm(block) {
        var form = el('div');
        var s = block.settings || {};

        function addTextarea(key, label, val) {
            var wrap = el('div', 'mb-2');
            wrap.appendChild(el('label', 'form-label small', { text: label }));
            var ta = el('textarea', 'form-control form-control-sm', { rows: '3' });
            ta.setAttribute('data-setting', key);
            ta.value = val || '';
            wrap.appendChild(ta);
            form.appendChild(wrap);
        }
        function addRadio(key, label, opts, val) {
            var wrap = el('div', 'mb-2');
            wrap.appendChild(el('label', 'form-label small', { text: label }));
            var grp = el('div', 'btn-group w-100');
            grp.setAttribute('role', 'group');
            opts.forEach(function (o) {
                var inp = el('input', 'btn-check', { type: 'radio', name: 'rcpt-' + key + '-' + block.id, id: 'rcpt-' + key + '-' + o.v });
                inp.setAttribute('data-setting', key);
                inp.value = o.v;
                if (val === o.v) inp.checked = true;
                grp.appendChild(inp);
                grp.appendChild(el('label', 'btn btn-sm btn-outline-primary', { 'for': 'rcpt-' + key + '-' + o.v, text: o.l }));
            });
            wrap.appendChild(grp);
            form.appendChild(wrap);
        }
        function addCheck(key, label, val) {
            var wrap = el('div', 'form-check mb-2');
            var inp = el('input', 'form-check-input', { type: 'checkbox', id: 'rcpt-chk-' + key + '-' + block.id });
            inp.setAttribute('data-setting', key);
            if (val) inp.checked = true;
            wrap.appendChild(inp);
            wrap.appendChild(el('label', 'form-check-label small', { 'for': 'rcpt-chk-' + key + '-' + block.id, text: label }));
            form.appendChild(wrap);
        }

        switch (block.type) {
            case 'text':
                addTextarea('content', 'İçerik', s.content);
                addRadio('align', 'Hizalama', [{v:'left',l:'Sol'},{v:'center',l:'Orta'},{v:'right',l:'Sağ'}], s.align || 'left');
                addCheck('bold', 'Kalın', s.bold);
                addRadio('size', 'Boyut', [{v:'s',l:'S'},{v:'m',l:'M'},{v:'l',l:'L'}], s.size || 'm');
                break;
            case 'store_info':
                addRadio('align', 'Hizalama', [{v:'left',l:'Sol'},{v:'center',l:'Orta'},{v:'right',l:'Sağ'}], s.align || 'center');
                addRadio('size', 'Boyut', [{v:'s',l:'S'},{v:'m',l:'M'},{v:'l',l:'L'}], s.size || 'm');
                break;
            case 'divider':
                addRadio('style', 'Tip', [{v:'solid',l:'Düz'},{v:'dashed',l:'Kesik'},{v:'dotted',l:'Noktalı'}], s.style || 'dashed');
                addRadio('color', 'Renk', [{v:'black',l:'Siyah'},{v:'gray',l:'Gri'}], s.color || 'black');
                break;
            case 'meta':
                addCheck('showSaleNumber', 'Fiş No', s.showSaleNumber);
                addCheck('showDate', 'Tarih', s.showDate);
                addCheck('showCashier', 'Kasiyer', s.showCashier);
                addCheck('showCustomer', 'Müşteri', s.showCustomer);
                break;
            case 'items':
                addCheck('showBarcode', 'Barkod göster', s.showBarcode);
                addCheck('showVatColumn', 'KDV kolonu göster', s.showVatColumn);
                break;
            case 'return_code':
                addCheck('showLabel', '"İade Kodu" etiketini göster', s.showLabel);
                break;
            case 'spacer':
                addRadio('size', 'Boyut', [{v:'s',l:'S'},{v:'m',l:'M'},{v:'l',l:'L'}], s.size || 'm');
                break;
            default:
                form.appendChild(el('div', 'text-secondary small', { text: 'Bu blok için ek ayar yok.' }));
        }
        return form;
    }

    function openBlockSettings(id) {
        var block = findBlock(id);
        if (!block) return;

        var container = document.getElementById('block-settings-offcanvas-container');
        while (container.firstChild) container.removeChild(container.firstChild);

        var offcanvas = el('div', 'offcanvas offcanvas-end show');
        offcanvas.setAttribute('tabindex', '-1');
        offcanvas.style.visibility = 'visible';
        offcanvas.style.display = 'block';

        var header = el('div', 'offcanvas-header');
        header.appendChild(el('h5', 'offcanvas-title', { text: (labels[block.type] || block.type) + ' — Ayarlar' }));
        var closeBtn = el('button', 'btn-close', { type: 'button', 'aria-label': 'Close' });
        closeBtn.addEventListener('click', function () { container.removeChild(offcanvas); });
        header.appendChild(closeBtn);
        offcanvas.appendChild(header);

        var body = el('div', 'offcanvas-body');
        body.appendChild(buildSettingsForm(block));
        offcanvas.appendChild(body);

        container.appendChild(offcanvas);

        body.querySelectorAll('[data-setting]').forEach(function (input) {
            function handler() {
                var key = input.getAttribute('data-setting');
                var value;
                if (input.type === 'checkbox') value = input.checked;
                else if (input.type === 'radio') { if (!input.checked) return; value = input.value; }
                else value = input.value;

                updateBlock(id, function (b) {
                    var newSettings = Object.assign({}, b.settings || {});
                    newSettings[key] = value;
                    return { settings: newSettings };
                });
            }
            input.addEventListener('change', handler);
            if (input.tagName === 'INPUT' && input.type === 'text') input.addEventListener('input', handler);
            if (input.tagName === 'TEXTAREA') input.addEventListener('input', handler);
        });
    }

    // ── Preview (iframe srcdoc — CSS izolasyonu + XSS sandbox) ──

    var previewTimer = null;
    function refreshPreview() {
        clearTimeout(previewTimer);
        previewTimer = setTimeout(doRefreshPreview, 250);
    }
    function doRefreshPreview() {
        var token = getAntiForgeryToken();
        var fd = new FormData();
        fd.append('size', state.activeSize);
        fd.append('mode', state.activeMode);
        fd.append('__RequestVerificationToken', token);

        fetch('/settings/receipt-template/preview', { method: 'POST', body: fd })
            .then(function (r) { return r.text(); })
            .then(function (html) {
                var cssHref = state.activeSize === 'a4' ? '/css/receipt-a4.css' : '/css/receipt-thermal.css';
                var cont = document.getElementById('preview-container');
                while (cont.firstChild) cont.removeChild(cont.firstChild);

                var iframe = document.createElement('iframe');
                iframe.setAttribute('sandbox', 'allow-same-origin');
                iframe.style.width = '100%';
                iframe.style.minHeight = '400px';
                iframe.style.border = '0';
                // srcdoc — güvenli kullanıcı içeriği burada server-encoded HTML.
                iframe.srcdoc = '<!DOCTYPE html><html><head><meta charset="utf-8"><link rel="stylesheet" href="' + cssHref + '"></head><body>' + html + '</body></html>';
                cont.appendChild(iframe);
            })
            .catch(function () {
                var cont = document.getElementById('preview-container');
                cont.textContent = 'Önizleme yüklenemedi.';
            });
    }

    // ── Save ──

    function saveTemplate() {
        var token = getAntiForgeryToken();
        var fd = new FormData();
        fd.append('ThermalJson', state.template.thermalJson);
        fd.append('A4Json', state.template.a4Json);
        fd.append('LogoWidthPx', String(state.template.logoWidthPx || 120));
        fd.append('StoreName', document.getElementById('store-name-input').value);
        fd.append('StoreAddress', document.getElementById('store-address-input').value);
        fd.append('StorePhone', document.getElementById('store-phone-input').value);
        fd.append('__RequestVerificationToken', token);

        fetch('/settings/receipt-template', { method: 'POST', body: fd })
            .then(function (r) {
                if (r.ok) {
                    state.template.storeName = document.getElementById('store-name-input').value;
                    state.template.storeAddress = document.getElementById('store-address-input').value;
                    state.template.storePhone = document.getElementById('store-phone-input').value;
                    refreshPreview();
                }
            });
    }

    // ── Logo ──

    function uploadLogo(file) {
        var token = getAntiForgeryToken();
        var fd = new FormData();
        fd.append('file', file);
        fd.append('__RequestVerificationToken', token);

        fetch('/settings/receipt-template/upload-logo', { method: 'POST', body: fd })
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (data) {
                if (!data) return;
                state.template.logoUrl = data.url;
                var preview = document.getElementById('logo-preview');
                while (preview.firstChild) preview.removeChild(preview.firstChild);
                var img = document.createElement('img');
                img.src = data.url;
                img.style.maxWidth = '100%';
                img.style.maxHeight = '100px';
                preview.appendChild(img);
                document.getElementById('btn-delete-logo').disabled = false;
                refreshPreview();
            });
    }

    function deleteLogo() {
        var token = getAntiForgeryToken();
        var fd = new FormData();
        fd.append('__RequestVerificationToken', token);
        fetch('/settings/receipt-template/logo', { method: 'DELETE', body: fd })
            .then(function (r) {
                if (!r.ok) return;
                state.template.logoUrl = null;
                var preview = document.getElementById('logo-preview');
                while (preview.firstChild) preview.removeChild(preview.firstChild);
                preview.appendChild(el('div', 'text-secondary small', { text: '(Logo yüklü değil)' }));
                document.getElementById('btn-delete-logo').disabled = true;
                refreshPreview();
            });
    }

    // ── Init ──

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-add-block]').forEach(function (b) {
            b.addEventListener('click', function () { addBlock(b.getAttribute('data-add-block')); });
        });
        document.getElementById('btn-save-template').addEventListener('click', saveTemplate);
        document.getElementById('logo-file-input').addEventListener('change', function (e) {
            if (e.target.files[0]) uploadLogo(e.target.files[0]);
        });
        document.getElementById('btn-delete-logo').addEventListener('click', deleteLogo);
        document.getElementById('logo-width-range').addEventListener('input', function (e) {
            state.template.logoWidthPx = parseInt(e.target.value, 10);
            document.getElementById('logo-width-value').textContent = e.target.value;
            refreshPreview();
        });
        document.querySelectorAll('input[name="rcpt-size"]').forEach(function (r) {
            r.addEventListener('change', function (e) {
                state.activeSize = e.target.value;
                renderEditor();
                refreshPreview();
            });
        });
        document.querySelectorAll('input[name="rcpt-mode"]').forEach(function (r) {
            r.addEventListener('change', function (e) {
                state.activeMode = e.target.value;
                refreshPreview();
            });
        });

        renderEditor();
        refreshPreview();
    });
})();
