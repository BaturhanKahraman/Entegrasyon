"use strict";

window.PrintAgent = {
    _baseUrl: "https://localhost:19100",
    _apiKey: "",

    configure: function (baseUrl, apiKey) {
        if (baseUrl) this._baseUrl = baseUrl.replace(/\/+$/, "");
        if (apiKey) this._apiKey = apiKey;
    },

    _fetch: async function (path, options) {
        options = options || {};
        var controller = new AbortController();
        var timeoutId = setTimeout(function () { controller.abort(); }, 5000);

        try {
            var headers = { "Content-Type": "application/json" };
            if (this._apiKey) {
                headers["X-Agent-Key"] = this._apiKey;
            }

            var response = await fetch(this._baseUrl + path, Object.assign({}, options, {
                headers: Object.assign({}, headers, options.headers),
                signal: controller.signal
            }));

            clearTimeout(timeoutId);
            return await response.json();
        } catch (error) {
            clearTimeout(timeoutId);
            return { error: true, message: error.name === "AbortError" ? "Zaman asimi" : "Baglanti hatasi" };
        }
    },

    // Agent çalışıyor mu kontrol et
    isAvailable: async function () {
        try {
            var controller = new AbortController();
            var timeoutId = setTimeout(function () { controller.abort(); }, 2000);
            var response = await fetch(this._baseUrl + "/health", { signal: controller.signal });
            clearTimeout(timeoutId);
            if (!response.ok) return false;
            var data = await response.json();
            return data.status === "ok";
        } catch (e) {
            return false;
        }
    },

    // Agent'tan yazıcı listesini al
    getPrinters: async function () {
        var result = await this._fetch("/printers");
        return result.error ? [] : result;
    },

    // Agent üzerinden yazdır
    print: async function (zplContent, rawBytesBase64, printerName, copies, language) {
        return await this._fetch("/print", {
            method: "POST",
            body: JSON.stringify({
                zplContent: zplContent || "",
                rawBytes: rawBytesBase64 || null,
                targetPrinter: printerName,
                copies: copies || 1,
                language: language === "ESCPOS" ? 1 : 0
            })
        });
    },

    // Tarayıcının print dialog'unu aç (ZPL'yi HTML olarak gösterip yazdır)
    browserPrint: function (htmlContent) {
        var printWindow = window.open("", "_blank", "width=400,height=600");
        printWindow.document.write(
            "<html><head><title>Yazdir</title>" +
            "<style>@media print { body { margin: 0; } @page { size: 100mm 60mm; margin: 0; } }</style>" +
            "</head><body>" + htmlContent + "</body></html>"
        );
        printWindow.document.close();
        printWindow.focus();
        printWindow.print();
        printWindow.close();
    },

    // ZPL/raw içeriği dosya olarak indir
    downloadFile: function (content, fileName, mimeType) {
        var blob;
        if (typeof content === "string") {
            blob = new Blob([content], { type: mimeType || "text/plain" });
        } else {
            // base64 encoded byte array
            var bytes = atob(content);
            var arr = new Uint8Array(bytes.length);
            for (var i = 0; i < bytes.length; i++) arr[i] = bytes.charCodeAt(i);
            blob = new Blob([arr], { type: mimeType || "application/octet-stream" });
        }

        var url = URL.createObjectURL(blob);
        var a = document.createElement("a");
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }
};
