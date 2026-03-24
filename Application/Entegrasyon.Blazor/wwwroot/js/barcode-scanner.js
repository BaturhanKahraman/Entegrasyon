window.BarcodeScanner = {
    _dotnetHelper: null,
    _buffer: "",
    _lastKeyTime: 0,
    _timeout: null,
    _config: { timeout: 100, minLength: 6, enabled: true },
    _boundHandler: null,

    initialize: function (dotnetHelper, config) {
        this._dotnetHelper = dotnetHelper;
        this._config = { ...this._config, ...config };
        this._boundHandler = this._handleKeyDown.bind(this);
        document.addEventListener('keydown', this._boundHandler);
    },

    _handleKeyDown: function (e) {
        if (!this._config.enabled) return;

        // Input/textarea icindeyken calismasi — ancak barkod okuyucu cok hizli yazdigi icin timing ile ayirt et
        var now = Date.now();
        var timeDiff = now - this._lastKeyTime;

        if (e.key === 'Enter' && this._buffer.length >= this._config.minLength) {
            e.preventDefault();
            e.stopPropagation();
            var barcode = this._buffer;
            this._buffer = "";
            this._dotnetHelper.invokeMethodAsync('OnBarcodeScanned', barcode);
            return;
        }

        if (e.key.length === 1) { // Printable character
            if (timeDiff > this._config.timeout) {
                this._buffer = ""; // Cok yavas — yeni barkod baslangici
            }
            this._buffer += e.key;
            this._lastKeyTime = now;
        }
    },

    updateConfig: function (config) {
        this._config = { ...this._config, ...config };
    },

    dispose: function () {
        if (this._boundHandler) {
            document.removeEventListener('keydown', this._boundHandler);
        }
        this._dotnetHelper = null;
        this._buffer = "";
    }
};
