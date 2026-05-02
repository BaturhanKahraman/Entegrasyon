// Yazdırma Agent köprüsü
//
// Kullanım:
//   await window.printAgent.printVariants([variantId1, variantId2], copies);
//
// Akış:
//   1. POST /print/batch  → { batchId, oneTimeBatchToken, deepLink }
//   2. window.location.href = deepLink  → tarayıcı entegrasyon-print:// şemasını
//      OS'a yönlendirir, kayıtlıysa Desktop uygulaması açılır.
//   3. Kullanıcıya Notyf bildirim gösterilir.

(function () {
    'use strict';

    function notyf() {
        if (window.notyf && typeof window.notyf.success === 'function') return window.notyf;
        return {
            success: function (msg) { console.log('[print-agent] ' + msg); },
            error: function (msg) { console.warn('[print-agent] ' + msg); }
        };
    }

    async function createBatch(variantIds, copies) {
        const response = await fetch('/print/batch', {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
            body: JSON.stringify({ variantIds: variantIds, copies: copies || 1 })
        });

        if (!response.ok) {
            let msg = 'Yazdırma işi oluşturulamadı.';
            try { const err = await response.json(); if (err && err.error) msg = err.error; } catch (_) { }
            throw new Error(msg);
        }

        return await response.json();
    }

    function triggerDeepLink(deepLink) {
        // Görünmez iframe yöntemi: eğer şema OS'ta kayıtlı değilse tarayıcı sessizce
        // başarısız olur, mevcut sayfayı bozmaz. Eğer kayıtlıysa OS yakalar.
        const frame = document.createElement('iframe');
        frame.style.display = 'none';
        frame.src = deepLink;
        document.body.appendChild(frame);
        setTimeout(() => { try { frame.remove(); } catch (_) { } }, 2000);
    }

    async function printVariants(variantIds, copies) {
        if (!Array.isArray(variantIds) || variantIds.length === 0) {
            notyf().error('Yazdırılacak varyant seçilmedi.');
            return null;
        }

        try {
            const result = await createBatch(variantIds, copies);
            triggerDeepLink(result.deepLink);
            notyf().success(
                `${result.totalItems} etiket yazdırma agent'ına gönderildi. ` +
                `Agent açılmazsa kurulum gerekebilir.`);
            return result;
        } catch (err) {
            notyf().error(err.message || 'Yazdırma başlatılamadı.');
            return null;
        }
    }

    window.printAgent = { printVariants: printVariants };
})();
