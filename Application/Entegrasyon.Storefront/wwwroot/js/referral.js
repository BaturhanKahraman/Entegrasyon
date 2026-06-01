/* Zekids — Davet Et: kopyala / WhatsApp / native share + mobil sidebar sheet. */
(function () {
    'use strict';

    var notify = window.toast || function () {};

    var codeEl = document.querySelector('[data-referral-code]');
    var linkEl = document.querySelector('[data-referral-link]');

    function absoluteLink() {
        var link = linkEl ? linkEl.textContent.trim() : '';
        if (link && link.indexOf('http') !== 0) {
            link = window.location.origin + link;
        }
        return link;
    }

    function copy(text, msg) {
        if (!text) return;
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text).then(function () { notify(msg); }).catch(function () { notify('Kopyalanamadı'); });
        } else {
            var ta = document.createElement('textarea');
            ta.value = text;
            ta.style.position = 'fixed';
            ta.style.opacity = '0';
            document.body.appendChild(ta);
            ta.select();
            try { document.execCommand('copy'); notify(msg); } catch (e) { notify('Kopyalanamadı'); }
            document.body.removeChild(ta);
        }
    }

    var copyLink = document.querySelector('[data-copy-link]');
    if (copyLink) copyLink.addEventListener('click', function () { copy(absoluteLink(), 'Davet linki kopyalandı'); });

    var copyCode = document.querySelector('[data-copy-code]');
    if (copyCode) copyCode.addEventListener('click', function () {
        copy(codeEl ? codeEl.textContent.trim() : '', 'Davet kodu kopyalandı');
    });

    var shareWa = document.querySelector('[data-share-whatsapp]');
    if (shareWa) shareWa.addEventListener('click', function () {
        var text = 'Zekids Bebe\'ye davetlisin! Bu linkten üye ol, ikimiz de puan kazanalım: ' + absoluteLink();
        window.open('https://wa.me/?text=' + encodeURIComponent(text), '_blank', 'noopener');
    });

    var shareNative = document.querySelector('[data-share-native]');
    if (shareNative) shareNative.addEventListener('click', function () {
        var link = absoluteLink();
        if (navigator.share) {
            navigator.share({ title: 'Zekids Bebe', text: 'Zekids Bebe\'ye davetlisin!', url: link }).catch(function () {});
        } else {
            copy(link, 'Davet linki kopyalandı');
        }
    });

    /* mobil sidebar sheet */
    function setSheet(open) {
        var s = document.getElementById('accSheet');
        if (!s) return;
        s.classList.toggle('hidden', !open);
        document.body.style.overflow = open ? 'hidden' : '';
    }
    document.querySelectorAll('[data-acc-open]').forEach(function (b) { b.addEventListener('click', function () { setSheet(true); }); });
    document.querySelectorAll('[data-acc-close]').forEach(function (b) { b.addEventListener('click', function () { setSheet(false); }); });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') setSheet(false); });
})();
